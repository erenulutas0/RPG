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
    // The hero stays readable: structures in front of it (never those behind) fade over its body, and a faded
    // fragment writes no depth. Static meshes/batches remain intact. Each source material has one scene-owned variant.
    // Enemies hidden by a structure stay locatable: their bodies share one scene-owned material whose second pass draws
    // an ember silhouette only where structure depth covers them. Ground drops behind a structure stay findable the same way,
    // with a second scene-owned material of the same shader in a warm loot gold, and so does the room's chest. Disabling the
    // view gives the bodies their material back. The hero never gets a silhouette: structures fade over it instead.
    // The second pass costs every body a draw and a material switch, and a two-pass material never batches, so a body
    // wears the silhouette material only in a render where a structure may cover it. A camera that sees the region flat
    // (orthographic, straight along z, its depth cleared first, as the game's is) draws a body's pixels inside its
    // footprint - the box of its sprite's quad where the placement has just put it (Footprint) - over no depth but what
    // that render's structures write. Where the depth cover (FoundryDepthCover, made with the region) shows no structure
    // depth over the footprint nearer than the body's farthest depth plus CoverMargin, the second pass would draw nothing,
    // so the body wears its own material instead - one pass of Sprites/Default, which is what the first pass is. Every
    // other body, and every body in any other camera's render or a placement made with no camera, wears the silhouette
    // material as before: a hidden body shows its silhouette exactly where it did. What the last placement decided, and
    // why it was not gated when it was not, stays readable (GateRefusal, DescribeGate) for the tests and the probe.
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
        // How much farther than a body the nearest structure depth over it must be for the body to go without its second
        // pass: many steps of any depth buffer over the camera's .3 to 1000 range (a 16-bit one steps about .015), and five
        // times FoundryDepth.ActorBias.
        internal const float CoverMargin = .1f;
        // About 640 thousand cells, 2.5 MB; the foundry's structures span an estimated 70 by 55 units.
        private const float MaxCoverArea = 200f * 200f;
        // Why a placement is not gated (GateRefusal): the first of the gate's conditions that failed, one string each, so
        // that keeping the answer costs a frame nothing. Static fields, not constants: a constant string is a literal
        // (ldstr) in every method that names it, and ApplyActorDepth() is small enough for Mono to inline into its callers;
        // Mono allocates a temporary string for every literal it compiles in, before finding the interned one, so each new
        // caller of the cameraless placement allocated once when the JIT compiled it (FoundryActorDepthTests' measured
        // delegate, the first time it ran). A static field is read with ldsfld, which compiles to a plain load.
        internal static readonly string NoCamera = "no camera: a placement made without one";
        internal static readonly string PerspectiveCamera = "a perspective camera";
        internal static readonly string UnclearedCamera = "a camera that clears nothing, so earlier depth may remain";
        internal static readonly string TargetWithoutDepth = "a target without a depth buffer";
        internal static readonly string TurnedCamera = "a camera not looking straight along +z";
        internal static readonly string GateOff = "the gate turned off (GateByCover)";
        internal static readonly string CoverAbandoned =
            "the depth cover abandoned: over MaxCoverArea, or structure depth emitted without it";
        internal static readonly string NoCover = "no depth cover: the region never began one";
        internal static readonly string CoverUnfinished = "the depth cover unfinished";
        internal static readonly string RegionNotPlain = "the region rotated or scaled";
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
        private FoundryDepthCover _cover;
        private bool _coverAbandoned;
        // The placement on show gives the silhouette material only to bodies a structure may cover (the gate above).
        private bool _gated;

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

        // Off, every body wears its silhouette material in every render, as before the gate: for comparing the two.
        internal bool GateByCover { get; set; } = true;
        // Where structures can write depth: null until the region begins it, and for good once structure depth is emitted
        // without it (FoundryRegionView.CoverStructureDepth).
        internal FoundryDepthCover DepthCover => _coverAbandoned ? null : _cover;
        // Whether the placement on show gives the silhouette material only to bodies a structure may cover. For tests.
        internal bool IsGated => _applied && _gated;
        // Why the last placement was not gated - the first of the gate's conditions that failed - or null when it was.
        internal string GateRefusal { get; private set; } = NoCamera;
        // The enemy, drop and chest bodies the last placement looked at; when it was gated, how many it gave one pass, and
        // how many it kept in the silhouette: because a structure may cover them, because their footprint reaches beyond
        // the cover, or because it could not be measured (no sprite, or a draw mode other than Simple). A placement that is
        // not gated keeps every body in the silhouette and counts none of the four.
        internal int ShownBodies { get; private set; }
        internal int OnePassBodies { get; private set; }
        internal int CoveredBodies { get; private set; }
        internal int BeyondCoverBodies { get; private set; }
        internal int UnmeasuredBodies { get; private set; }

        // The last placement's gate in words, for a test's message or the probe's report (allocates).
        internal string DescribeGate() => GateRefusal == null
            ? "gated: " + ShownBodies + " bodies, " + OnePassBodies + " in one pass, " + CoveredBodies +
              " a structure may cover, " + BeyondCoverBodies + " beyond the cover, " + UnmeasuredBodies + " unmeasured"
            : "not gated (" + GateRefusal + "): all " + ShownBodies + " bodies in the silhouette";

        // A cover over more than MaxCoverArea square units (a stray vertex far off the map) is not made: every body then
        // keeps its silhouette, and FoundryActorDepthTests notices the gate is off.
        internal void BeginDepthCover(Vector2 min, Vector2 max)
        {
            // With a unit to spare each way, as the cover's own spare cells take, so a long thin span is counted too.
            if (!((max.x - min.x + 1f) * (max.y - min.y + 1f) <= MaxCoverArea))
                AbandonDepthCover();
            if (!_coverAbandoned)
                _cover = new FoundryDepthCover(min, max);
        }

        internal void CompleteDepthCover()
        {
            if (_cover != null)
                _cover.Complete = true;
        }

        internal void AbandonDepthCover()
        {
            _coverAbandoned = true;
            _cover = null;
        }

        private void OnEnable()
        {
            Camera.onPreCull += OnCameraPreCull;
            Camera.onPostRender += OnCameraPostRender;
        }

        // The ground projection's state, for the tests and the ramp-entry measurement: its material (null until made), the
        // grids made, whether a source sprite has one, and the floor sprites projected so far (every render's counted).
        internal Material GroundMaterial => _ground.Material;
        internal int GroundGridCount => _ground.GridCount;
        internal bool HasGroundGrid(Sprite source) => _ground.HasGrid(source);
        internal int GroundProjections => _ground.Projections;

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
            using (Cryptforge.Core.PerformanceMarkers.LateVisibility.Auto())
                LateTick();
        }

        private void LateTick()
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

        // Also covers cameras rendered by hand (review captures) after the last post-render restore. A placement gated for
        // a flat view is never reused for a camera that is not one.
        private void OnCameraPreCull(Camera camera)
        {
            using (Cryptforge.Core.PerformanceMarkers.DepthPlace.Auto())
            {
                string refusal = CameraRefusal(camera);
                if (!_applied || _appliedFrame != Time.frameCount || (_gated && refusal != null)) Place(refusal);
            }
        }

        private void OnCameraPostRender(Camera camera)
        {
            using (Cryptforge.Core.PerformanceMarkers.DepthRestore.Auto())
                RestoreActorDepth();
        }

        // The placement for a render whose camera is not known (tests, the probe's timing): every enemy, drop and chest body
        // wears its silhouette material.
        internal void ApplyActorDepth() => Place(NoCamera);

        // The placement for this camera's render: in a flat view, only the bodies a structure may cover wear it.
        internal void ApplyActorDepth(Camera camera) => Place(CameraRefusal(camera));

        // Moves each live actor's direct children (body, equipment, shadow, bar) to the depth of its feet; a childless root
        // (the chest, whose root is its sprite) moves itself. Then each enemy, drop and chest body, where it now stands,
        // takes the material this render needs. cameraRefusal: why the camera does not see the region flat, null if it does.
        private void Place(string cameraRefusal)
        {
            RestoreActorDepth();
            Vector3 region = transform.position;
            GateRefusal = cameraRefusal ?? CoverRefusal();
            _gated = GateRefusal == null;
            ShownBodies = OnePassBodies = CoveredBodies = BeyondCoverBodies = UnmeasuredBodies = 0;
            var actors = GroundedSorting.Actors;
            for (int i = actors.Count - 1; i >= 0; i--)
            {
                Transform actor = actors[i];
                if (actor == null) { actors.RemoveAt(i); continue; }
                if (!actor.gameObject.activeInHierarchy) continue;
                actor.TryGetComponent(out EnemyLookView look);
                // An enemy's health bar shares height but keeps its readout depth, even when a structure hides the body,
                // so a silhouetted enemy still shows how hurt it is.
                Transform bar = look != null ? look.Bar : null;
                SpriteRenderer body = look != null ? look.Body : null;
                Transform bodyTransform = body != null ? body.transform : null;
                Vector3 feet = actor.position;
                float lift = FoundrySurface.Lift(feet, region);
                float z = FoundryDepth.ActorZ(region, feet.y + lift);
                int children = actor.childCount;
                if (children == 0) MoveToDepth(actor, z, lift);
                // The hero's ability ring lies on the floor as a contact shadow does. It is found by its name, natively, on
                // every actor that is not an enemy (no enemy carries one): reading each child's name would allocate a string
                // for every child of every actor on every frame, and a failed GetComponent allocates in the Editor, so the
                // shadow is asked for with TryGetComponent - on every child but an enemy's body and bar, which never lie on
                // the floor. The pass allocates nothing once its lists have grown.
                Transform ring = look == null && children > 0 ? actor.Find(AbilityRingView.RingName) : null;
                for (int c = 0; c < children; c++)
                {
                    Transform child = actor.GetChild(c);
                    bool floor = !ReferenceEquals(child, bodyTransform) && !ReferenceEquals(child, bar) &&
                                 (child.TryGetComponent(out ContactShadowView _) || child == ring);
                    if (floor && child.TryGetComponent(out SpriteRenderer flat)) _ground.Apply(flat, region);
                    MoveToDepth(child, child == bar ? child.position.z : z, floor ? 0 : lift);
                }
                if (look != null) ShowWhenHidden(body, bodyTransform, EnemyXray(), region);
            }
            ShowLootWhenHidden(region);
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

        // Only the reward sprite of each drop on show (resting or flying), never its contact shadow. Pooled drops keep the
        // material they last wore while parked, so a re-used one is recognised and never registered twice. The chest's one
        // renderer lives as long as the scene and only swaps sprites, so it keeps its material from one render to the next.
        private void ShowLootWhenHidden(Vector3 region)
        {
            SpriteRenderer chest = _chest != null ? _chest.Body : null;
            int shown = _drops != null ? _drops.ShownCount : 0;
            if (chest == null && shown == 0) return;
            Material loot = LootXray();
            ShowWhenHidden(chest, null, loot, region);
            for (int i = 0; i < shown; i++) ShowWhenHidden(_drops.ShownBody(i), null, loot, region);
        }

        // A body a structure may cover in this render wears the silhouette material, registered with the material it had
        // so that it can be given back; a body none can cover wears its own. A body already wearing the silhouette
        // material is never registered again. at: the body's transform when the caller has it already, else null.
        private void ShowWhenHidden(SpriteRenderer body, Transform at, Material xray, Vector3 region)
        {
            if (body == null || xray == null) return;
            ShownBodies++;
            bool wearing = body.sharedMaterial == xray;
            if (MayBeHidden(body, at, region))
            {
                if (!wearing) Register(body, xray);
            }
            else if (wearing)
            {
                GiveBack(body);
            }
        }

        // Always, unless the placement is gated; then whenever the cover shows structure depth over the body's footprint
        // that is not CoverMargin farther than the body's farthest point, and whenever the footprint cannot be measured or
        // reaches beyond the cover (or is not a number). Counts the answer. Allocates nothing.
        private bool MayBeHidden(SpriteRenderer body, Transform at, Vector3 region)
        {
            if (!_gated) return true;
            if (ReferenceEquals(at, null)) at = body.transform;
            if (!Footprint(body, at, region, out Vector2 min, out Vector2 max, out float far))
            {
                UnmeasuredBodies++;
                return true;
            }
            FoundryDepthCover.Answer answer = _cover.Ask(min, max, far, CoverMargin);
            if (answer == FoundryDepthCover.Answer.Open)
            {
                OnePassBodies++;
                return false;
            }
            if (answer == FoundryDepthCover.Answer.Covered) CoveredBodies++;
            else BeyondCoverBodies++;
            return true;
        }

        // Where a sprite body draws, as it stands now: the box (region-local x and y) of its quad - its sprite's bounds,
        // flipped as the renderer flips them, through the body's own transform - and the farthest depth (region-local z)
        // of any of its pixels. The quad is flat: a sprite's vertices lie at z 0, so the depth its bounds report is no
        // geometry and is left out (Renderer.bounds gives every sprite a tenth either way: the court enemy's spanned 50.73
        // to 50.93 at z 50.83), and an unturned body's pixels all lie at its own z. Read from the transform rather than
        // Renderer.bounds, which for a body this pass has just moved cost about 10 us each in the probe (the pass took 584
        // against 176 us). False when it cannot be measured from the sprite: none, or a draw mode that sizes the quad by
        // the renderer. Allocates nothing.
        internal static bool Footprint(SpriteRenderer body, Vector3 region, out Vector2 min, out Vector2 max, out float far) =>
            Footprint(body, body.transform, region, out min, out max, out far);

        // The same, with the body's transform (at) already in hand.
        private static bool Footprint(SpriteRenderer body, Transform at, Vector3 region, out Vector2 min, out Vector2 max,
            out float far)
        {
            min = max = default;
            far = 0f;
            Sprite sprite = body.sprite;
            if (sprite == null || body.drawMode != SpriteDrawMode.Simple) return false;
            Bounds local = sprite.bounds;
            Vector3 centre = local.center, extents = local.extents;
            float x0 = centre.x - extents.x, x1 = centre.x + extents.x, y0 = centre.y - extents.y, y1 = centre.y + extents.y;
            // Sprites/Default flips the quad's vertices about the sprite's pivot, the transform's origin (_Flip).
            if (body.flipX) { float left = x0; x0 = -x1; x1 = -left; }
            if (body.flipY) { float low = y0; y0 = -y1; y1 = -low; }
            Matrix4x4 m = at.localToWorldMatrix;
            // The box round the quad's four corners: its centre through the matrix, its half sizes through the matrix's
            // absolute values (exact for an affine transform, whatever it turns or mirrors).
            float cx = (x0 + x1) * .5f, cy = (y0 + y1) * .5f, hx = (x1 - x0) * .5f, hy = (y1 - y0) * .5f;
            float wx = m.m00 * cx + m.m01 * cy + m.m03, wy = m.m10 * cx + m.m11 * cy + m.m13, wz = m.m20 * cx + m.m21 * cy + m.m23;
            float rx = Mathf.Abs(m.m00) * hx + Mathf.Abs(m.m01) * hy;
            float ry = Mathf.Abs(m.m10) * hx + Mathf.Abs(m.m11) * hy;
            float rz = Mathf.Abs(m.m20) * hx + Mathf.Abs(m.m21) * hy;
            min = new Vector2(wx - rx - region.x, wy - ry - region.y);
            max = new Vector2(wx + rx - region.x, wy + ry - region.y);
            far = wz + rz - region.z;
            return true;
        }

        private void Register(SpriteRenderer body, Material xray)
        {
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

        // A body no structure can cover in this render gets back the material it wore when it took the silhouette, and
        // is forgotten until it takes it again. One wearing it unregistered (someone else's doing) keeps it.
        private void GiveBack(SpriteRenderer body)
        {
            for (int i = _xrayed.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_xrayed[i], body)) continue;
                body.sharedMaterial = _xrayedFrom[i];
                _xrayed.RemoveAt(i);
                _xrayedFrom.RemoveAt(i);
                return;
            }
        }

        // Null for a camera that sees the region flat: orthographic, looking straight along +z, with depth to test against
        // that it clears before it draws. Its pixels then lie in the scene's x and y, and the only depth in them is what its
        // own render writes. Otherwise the first condition that fails. Internal for FoundryActorDepthTests' allocation count.
        internal static string CameraRefusal(Camera camera)
        {
            if (camera == null) return NoCamera;
            if (!camera.orthographic) return PerspectiveCamera;
            if (camera.clearFlags == CameraClearFlags.Nothing) return UnclearedCamera;
            RenderTexture target = camera.targetTexture;
            if (target != null && target.depth == 0) return TargetWithoutDepth;
            Vector3 forward = camera.transform.forward;
            return Mathf.Abs(forward.x) + Mathf.Abs(forward.y) < 1e-5f && forward.z > 0f ? null : TurnedCamera;
        }

        // Null when the cover can answer for the region: the gate on, the cover made over every structure and the region
        // plain. Otherwise the first condition that fails. Internal for FoundryActorDepthTests' allocation count.
        internal string CoverRefusal()
        {
            if (!GateByCover) return GateOff;
            if (_coverAbandoned) return CoverAbandoned;
            if (_cover == null) return NoCover;
            if (!_cover.Complete) return CoverUnfinished;
            return Plain(transform) ? null : RegionNotPlain;
        }

        // The depth cover is in the region's own space, where its meshes stand: an unrotated, unscaled region (as ArenaView
        // makes it) maps that to the world by its position alone.
        private static bool Plain(Transform region)
        {
            Quaternion r = region.rotation;
            Vector3 s = region.lossyScale;
            return Mathf.Abs(r.x) + Mathf.Abs(r.y) + Mathf.Abs(r.z) < 1e-6f &&
                   Mathf.Abs(s.x - 1f) + Mathf.Abs(s.y - 1f) + Mathf.Abs(s.z - 1f) < 1e-5f;
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
