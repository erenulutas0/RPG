using System.Collections.Generic;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Shared depth scale of the foundry. Structure vertices store the region-local screen height of their ground
    // contact in z (FoundryRegionView.ApplyStructureDepth); actor sprites and above-floor combat effects get the same value
    // from their feet while a camera renders, and the furnace pit's sparks and haze (FoundryAtmosphereView) stand at the
    // pit's depth. Everything else in the scene stays at z 0, nearer than any structure, so it keeps passing its default
    // depth test unchanged.
    internal static class FoundryDepth
    {
        internal const float Base = 50f;
        // Actors win exact ties with the structure they touch.
        internal const float ActorBias = .02f;
        internal static float Z(float groundY) => Base + groundY;
        internal static float ActorZ(Vector3 region, float worldY) => region.z + Z(worldY - region.y) - ActorBias;
    }

    // Presentation-only surface height and depth. Positions/sprite variants are applied only
    // during camera rendering and restored afterwards, including the childless chest. Gameplay
    // always reads flat roots; actor bodies, bars and airborne effects share their floor contact.
    // Ground sprites instead bend over the surface using a scene-owned grid/shader cache.
    // The hero stays readable: structures in front of him (never those behind) fade over his body, and a faded
    // fragment writes no depth. Static meshes/batches remain intact. Each source material has one scene-owned variant.
    // Enemies hidden by a structure stay locatable: their bodies share one scene-owned material whose second pass draws
    // an ember silhouette only where structure depth covers them. Ground drops behind a structure stay findable the same way,
    // with a second scene-owned material of the same shader in a warm loot gold, and so does the room's chest. Disabling the
    // view gives the bodies their material back. The hero never gets a silhouette: structures fade over him instead.
    // Combat effects drawn above the floor (sparks, strikes, collapses, smoke, coins) stand at the depth of the floor they
    // stand on in the same render window, so the structure that hides an enemy hides its hit and its fall too. Their pooled
    // transforms are asked for afresh on every apply, never remembered. Floor pulses already sort under the structures and
    // the numbers stay readable over everything, so both keep z 0.
    [DefaultExecutionOrder(10000)]
    public sealed class FoundryVisibilityView : MonoBehaviour
    {
        private static readonly int FocusId = Shader.PropertyToID("_HeroFocus");
        private static readonly int XrayColorId = Shader.PropertyToID("_XrayColor");
        internal const string XrayShaderName = "Cryptforge/Foundry Actor Xray";
        // Reads as reward, not threat: the gold of coins and chests rather than the enemies' ember.
        internal static readonly Color LootXrayColor = new Color(1f, .8f, .32f, 1f);
        private readonly Dictionary<Material, Material> _variants = new Dictionary<Material, Material>();
        private readonly List<Transform> _moved = new List<Transform>();
        private readonly List<Vector3> _restPosition = new List<Vector3>();
        private readonly FoundryGroundProjection _ground = new FoundryGroundProjection();
        private readonly List<SpriteRenderer> _xrayed = new List<SpriteRenderer>();
        private readonly List<Material> _xrayedFrom = new List<Material>();
        private Material _xray;
        private Material _lootXray;
        private bool _xrayMissing;
        private int _pruneAt = 64;
        private GroundDropView _drops;
        private ChestSpawner _chest;
        private CombatEffectsView _effects;
        private bool _applied;
        // The frame the current placement was made in: a camera rendered by hand in a later frame, before that frame's
        // LateUpdate (a capture coroutine after `yield return null`), must not reuse a stale placement.
        private int _appliedFrame = -1;
        private Transform _hero;
        private Health _health;
        private Shader _shader;

        internal Material ForStructure(Material source)
        {
            // A missing material slot renders as before (pink) instead of aborting the region build.
            if (source == null) return null;
            if (_variants.TryGetValue(source, out var existing)) return existing;
            if (_shader == null) _shader = Resources.Load<Shader>("FoundryCutaway");
            if (_shader == null) return source;
            var variant = new Material(source) { shader = _shader, name = source.name + " (scene cutaway)" };
            variant.SetVector(FocusId, Vector4.zero);
            _variants.Add(source, variant);
            return variant;
        }

        private void OnEnable()
        {
            Camera.onPreCull += OnCameraPreCull;
            Camera.onPostRender += OnCameraPostRender;
        }

        // Bodies remembered for restoring (enemies, drops and the chest; destroyed ones until pruned), so a test can see nothing registers twice.
        internal int XrayedCount => _xrayed.Count;
        // Transforms the last apply placed in depth (actor sprites and effects), so a test can see none is remembered twice.
        internal int PlacedCount => _moved.Count;

        private void Start()
        {
            _drops = FindFirstObjectByType<GroundDropView>();
            _chest = FindFirstObjectByType<ChestSpawner>();
            _effects = FindFirstObjectByType<CombatEffectsView>();
            var hero = FindFirstObjectByType<HeroLookView>();
            if (hero == null) return;
            _hero = hero.transform;
            _health = hero.GetComponent<Health>();
        }

        private void LateUpdate()
        {
            Vector4 focus = Vector4.zero;
            if (_hero != null && (_health == null || _health.IsAlive))
            {
                Vector3 p = _hero.position;
                float lift = FoundrySurface.Lift(p, transform.position);
                focus = new Vector4(p.x, p.y + lift + .35f, FoundryDepth.ActorZ(transform.position, p.y + lift), 1);
            }
            foreach (var variant in _variants.Values) variant.SetVector(FocusId, focus);
            // Height is applied only in camera callbacks, never left for the next gameplay Update.
        }

        // Also covers cameras rendered by hand (review captures) after the last post-render restore.
        private void OnCameraPreCull(Camera camera)
        {
            if (!_applied || _appliedFrame != Time.frameCount) ApplyActorDepth();
        }

        private void OnCameraPostRender(Camera camera) => RestoreActorDepth();

        // Moves each live actor's direct children (body, equipment, shadow, bar) to the depth of its feet; a childless root
        // (the chest, whose root is its sprite) moves itself.
        internal void ApplyActorDepth()
        {
            RestoreActorDepth();
            Vector3 region = transform.position;
            var actors = GroundedSorting.Actors;
            for (int i = actors.Count - 1; i >= 0; i--)
            {
                Transform actor = actors[i];
                if (actor == null) { actors.RemoveAt(i); continue; }
                if (!actor.gameObject.activeInHierarchy) continue;
                Transform bar = ShowEnemyWhenHidden(actor);
                float lift = FoundrySurface.Lift(actor.position, region);
                float z = FoundryDepth.ActorZ(region, actor.position.y + lift);
                int children = actor.childCount;
                if (children == 0) MoveToDepth(actor, z, lift);
                // An enemy's health bar shares height but keeps its readout depth, even when a structure
                // hides the body, so a silhouetted enemy still shows how hurt it is.
                for (int c = 0; c < children; c++)
                {
                    Transform child = actor.GetChild(c);
                    bool floor = child.GetComponent<ContactShadowView>() != null || child.name == "Ability Ring";
                    if (floor) _ground.Apply(child.GetComponent<SpriteRenderer>(), region);
                    MoveToDepth(child, child == bar ? child.position.z : z, floor ? 0 : lift);
                }
            }
            ShowLootWhenHidden();
            PlaceEffects(region);
            _applied = true;
            _appliedFrame = Time.frameCount;
        }

        // Each combat effect on show above the floor moves to the depth of the floor it stands on: a hit spark to its
        // enemy's feet rather than its own height on the body, so it shares that enemy's depth.
        private void PlaceEffects(Vector3 region)
        {
            if (_effects == null) return;
            int slots = _effects.EffectSlotCount;
            for (int i = 0; i < slots; i++)
            {
                Transform effect = _effects.SurfaceEffect(i, out Vector3 contact, out bool above, out var floor);
                if (effect == null) continue;
                if (floor != null) _ground.Apply(floor, region);
                else
                {
                    float lift = FoundrySurface.Lift(contact, region);
                    MoveToDepth(effect, FoundryDepth.ActorZ(region, contact.y + lift), lift);
                }
            }
            for (int i = 0; i < _effects.NumberSlotCount; i++)
            {
                var number = _effects.SurfaceNumber(i, out Vector3 contact);
                if (number != null) MoveToDepth(number, number.position.z, FoundrySurface.Lift(contact, region));
            }
        }

        private void MoveToDepth(Transform moved, float z, float lift = 0)
        {
            _moved.Add(moved);
            _restPosition.Add(moved.localPosition);
            Vector3 p = moved.position;
            moved.position = new Vector3(p.x, p.y + lift, z);
        }

        // Gives an enemy's body the silhouette material, once; the original is kept for OnDisable.
        // Returns the enemy's health bar, which keeps its authored depth; null for anything that is not an enemy.
        private Transform ShowEnemyWhenHidden(Transform actor)
        {
            if (!actor.TryGetComponent(out EnemyLookView look)) return null;
            ShowWhenHidden(look.Body, EnemyXray());
            return look.Bar;
        }

        // Only the reward sprite of each drop on show (resting or flying), never its contact shadow. Pooled drops keep the
        // loot material while parked, so a re-used one is recognised and never registered twice. The chest's one renderer
        // lives as long as the scene and only swaps sprites, so it keeps the material from its first registration on.
        private void ShowLootWhenHidden()
        {
            SpriteRenderer chest = _chest != null ? _chest.Body : null;
            int shown = _drops != null ? _drops.ShownCount : 0;
            if (chest == null && shown == 0) return;
            Material loot = LootXray();
            ShowWhenHidden(chest, loot);
            for (int i = 0; i < shown; i++) ShowWhenHidden(_drops.ShownBody(i), loot);
        }

        private void ShowWhenHidden(SpriteRenderer body, Material xray)
        {
            if (body == null || xray == null || body.sharedMaterial == xray) return;
            // Enemies are spawned and destroyed all run long; drop the dead before remembering another. Pooled drops
            // live as long as the scene, so the threshold grows with what survives rather than pruning on every add.
            if (_xrayed.Count >= _pruneAt)
            {
                PruneXrayed();
                _pruneAt = Mathf.Max(64, _xrayed.Count * 2);
            }
            _xrayed.Add(body);
            _xrayedFrom.Add(body.sharedMaterial);
            body.sharedMaterial = xray;
        }

        private Material EnemyXray()
        {
            if (_xray == null && !_xrayMissing) _xray = CreateXray("Foundry hidden enemy silhouette (scene)");
            return _xray;
        }

        private Material LootXray()
        {
            if (_lootXray == null && !_xrayMissing)
            {
                _lootXray = CreateXray("Foundry hidden loot silhouette (scene)");
                if (_lootXray != null) _lootXray.SetColor(XrayColorId, LootXrayColor);
            }
            return _lootXray;
        }

        private Material CreateXray(string name)
        {
            var shader = Resources.Load<Shader>("FoundryActorXray");
            if (shader == null || !shader.isSupported) { _xrayMissing = true; return null; }
            return new Material(shader) { name = name };
        }

        private void PruneXrayed()
        {
            int kept = 0;
            for (int i = 0; i < _xrayed.Count; i++)
            {
                if (_xrayed[i] == null) continue;
                _xrayed[kept] = _xrayed[i];
                _xrayedFrom[kept] = _xrayedFrom[i];
                kept++;
            }
            _xrayed.RemoveRange(kept, _xrayed.Count - kept);
            _xrayedFrom.RemoveRange(kept, _xrayedFrom.Count - kept);
        }

        private void RestoreXrayed()
        {
            // Newest first, so a body registered twice ends with the material it had before its first registration.
            for (int i = _xrayed.Count - 1; i >= 0; i--)
            {
                SpriteRenderer body = _xrayed[i];
                if (body == null) continue;
                // Leave alone a body whose material someone else has changed since.
                Material current = body.sharedMaterial;
                if (current != null && (current == _xray || current == _lootXray)) body.sharedMaterial = _xrayedFrom[i];
            }
            _xrayed.Clear();
            _xrayedFrom.Clear();
            _pruneAt = 64;
        }

        // Restores every moved transform and ground sprite to its pre-render state.
        internal void RestoreActorDepth()
        {
            if (!_applied) return;
            // Newest first, so a transform recorded twice ends at its original position.
            for (int i = _moved.Count - 1; i >= 0; i--)
            {
                Transform moved = _moved[i];
                if (moved == null) continue;
                moved.localPosition = _restPosition[i];
            }
            _moved.Clear();
            _restPosition.Clear();
            _ground.Restore();
            _applied = false;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPostRender -= OnCameraPostRender;
            RestoreActorDepth();
            RestoreXrayed();
            foreach (var variant in _variants.Values) variant.SetVector(FocusId, Vector4.zero);
        }

        private void OnDestroy()
        {
            _ground.Dispose();
            foreach (var variant in _variants.Values) if (variant != null) Destroy(variant);
            _variants.Clear();
            if (_xray != null) Destroy(_xray);
            if (_lootXray != null) Destroy(_lootXray);
        }
    }
}
