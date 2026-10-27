using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // A hero drawn from a pre-rendered art set (docs/45: Ilya Kest, rendered from one Blender model in five facings) in
    // place of HeroLookView's painted Vanguard. HeroLookView hands over in its Start when the run's hero (CombatSetup.Hero)
    // carries a RenderedCharacterArtSet, and steps aside; without one nothing here runs.
    //
    // VanguardAnimator's contract, kept: presentation only - it observes the hero's floor displacement, its first
    // weapon's strikes, its ability and its health, and never ticks a weapon, moves the hero or applies damage. The floor
    // travel (ArenaFloor.DepthScale) picks one of eight facings and drives the walk by distance; a strike while walking
    // keeps the stride; a paused combat (no scaled time) freezes the frame; nothing is allocated per frame. The rules live
    // in RenderedCharacterPlayer, pure and tested without Unity; this only feeds it and draws what it says.
    //
    // The drawing hangs beside the body rather than under it, as PuppetAnimator's does: CombatantView hides the body, and
    // the weapons under it, at death, and the drawing stays to play the fall. The body keeps its renderer with no sprite
    // and the six loadout renderers stop drawing, as the staff is part of the render. CombatantView's hit flash is drawn
    // here: the shown frame's silhouette in the flash colour (Resources/RenderedCharacterFlash.shader), or a tint of the
    // frame where that shader is missing.
    [DisallowMultipleComponent]
    public sealed class RenderedCharacterAnimator : MonoBehaviour
    {
        public const string RootName = "Hero Rendered";
        public const string FlashName = "Hero Rendered Flash";
        // The silhouette's strength over the frame while CombatantView flashes: the shape stays readable under it.
        public const float FlashAlpha = .85f;
        // Without the flash shader the frame itself is tinted this way instead (multiplying can only darken).
        private static readonly Color FallbackTint = new Color(1f, .55f, .5f, 1f);

        private RenderedCharacterArtSet _art;
        private RenderedCharacterPlayer _player;
        private SpriteRenderer _renderer;
        private SpriteRenderer _flash;
        private Material _flashMaterial;
        private AttackController _attack;
        private AbilityController _ability;
        private Health _health;
        private CombatantView _combatant;
        private Transform _root;
        private Vector3 _previous;
        private bool _tinted;
        private bool _subscribed;

        public RenderedCharacterArtSet Art => _art;
        // The pure state the frame comes from, so a test can follow its rules.
        public RenderedCharacterPlayer Player => _player;
        public SpriteRenderer Renderer => _renderer;
        public SpriteRenderer FlashRenderer => _flash;
        public Transform Root => _root;
        public RenderedMotion Motion => _player != null ? _player.Motion : RenderedMotion.Idle;
        public RenderedFacing Facing => _player != null ? _player.Facing : RenderedFacing.South;
        public bool Mirrored => _player != null && _player.Mirrored;
        public int FrameIndex => _player != null ? _player.Frame : 0;
        public bool IsFlashing => (_flash != null && _flash.enabled) || _tinted;

        // The run's hero's rendered set, when it has one; null keeps the painted Vanguard. An incomplete set is reported
        // and ignored, so a broken import never leaves the hero invisible.
        public static RenderedCharacterArtSet ArtFor(CombatSetup setup)
        {
            RenderedCharacterArtSet art = setup != null && setup.Hero != null ? setup.Hero.RenderedArt : null;
            if (art != null && !art.IsValid)
            {
                Debug.LogError("The hero's rendered art set is incomplete; the painted hero stands in for it.", art);
                return null;
            }
            return art;
        }

        // HeroLookView's hook. With a rendered set for this run's hero the look is disabled first - its OnDisable resets
        // the body's frame, which must happen before the body is cleared - and the drawing takes over. Null otherwise.
        internal static RenderedCharacterAnimator TakeOver(HeroLookView look, SpriteRenderer body, SpriteRenderer[] weapons,
            AttackController attack)
        {
            RenderedCharacterArtSet art = ArtFor(FindFirstObjectByType<CombatSetup>());
            if (art == null || body == null || attack == null)
                return null;
            look.enabled = false;
            return Attach(look.gameObject, body, weapons, attack, art);
        }

        internal static RenderedCharacterAnimator Attach(GameObject hero, SpriteRenderer body, SpriteRenderer[] weapons,
            AttackController attack, RenderedCharacterArtSet art)
        {
            var drawn = hero.GetComponent<RenderedCharacterAnimator>();
            if (drawn == null)
                drawn = hero.AddComponent<RenderedCharacterAnimator>();
            drawn.Initialize(body, weapons, attack, art);
            return drawn;
        }

        private void Initialize(SpriteRenderer body, SpriteRenderer[] weapons, AttackController attack,
            RenderedCharacterArtSet art)
        {
            if (_player != null)
                return;
            _art = art;
            _attack = attack;
            _health = GetComponent<Health>();
            _ability = GetComponent<AbilityController>();
            _combatant = GetComponent<CombatantView>();
            _player = new RenderedCharacterPlayer(art.Timings(), art.StrideLength);

            var root = new GameObject(RootName);
            root.transform.SetParent(transform, false);
            _root = root.transform;
            _renderer = root.AddComponent<SpriteRenderer>();
            if (body.sharedMaterial != null)
                _renderer.sharedMaterial = body.sharedMaterial;
            _renderer.sortingLayerID = body.sortingLayerID;
            _renderer.sortingOrder = body.sortingOrder;

            var shader = Resources.Load<Shader>("RenderedCharacterFlash");
            if (shader != null && shader.isSupported)
            {
                _flashMaterial = new Material(shader) { name = "Rendered hero flash (scene)" };
                var flash = new GameObject(FlashName);
                flash.transform.SetParent(_root, false);
                _flash = flash.AddComponent<SpriteRenderer>();
                _flash.sharedMaterial = _flashMaterial;
                _flash.sortingLayerID = body.sortingLayerID;
                _flash.sortingOrder = body.sortingOrder + 1;
                _flash.enabled = false;
            }

            // The staff is in the render: the body and every loadout stop drawing. HeroWeaponView still shows and hides
            // the loadouts' objects; a disabled renderer stays dark whichever it shows.
            body.sprite = null;
            if (weapons != null)
                foreach (SpriteRenderer weapon in weapons)
                    if (weapon != null)
                        weapon.enabled = false;
            _previous = transform.position;
            Show();
            Subscribe();
        }

        // AddComponent enables the component before Initialize has run; Initialize subscribes then.
        private void OnEnable()
        {
            if (_player != null)
                Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy()
        {
            Unsubscribe();
            if (_flashMaterial != null)
                Destroy(_flashMaterial);
        }

        // After every Update, so the hero's movement this frame is already in its position.
        private void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (_player == null)
                return;
            Vector3 position = transform.position;
            Vector3 delta = position - _previous;
            _previous = position;
            // CombatantView's flash runs on unscaled time, so it settles even while combat is paused.
            RefreshFlash();
            if (dt <= 0f)
                return;
            if (_health != null && !_health.IsAlive)
                _player.Die();
            _player.Step(dt, delta.x, delta.y / ArenaFloor.DepthScale);
            Show();
        }

        private void Show()
        {
            Sprite sprite = _art.Frame(_player.Motion, _player.Facing, _player.Frame);
            if (_renderer.sprite != sprite)
                _renderer.sprite = sprite;
            if (_flash != null && _flash.enabled && _flash.sprite != sprite)
                _flash.sprite = sprite;
            // Mirror the drawing only; movement, targeting and the floor shadow are not touched.
            float x = _player.Mirrored ? -1f : 1f;
            if (_root.localScale.x != x)
                _root.localScale = new Vector3(x, 1f, 1f);
        }

        private void RefreshFlash()
        {
            bool on = _combatant != null && _combatant.IsFlashing && !_player.IsDying;
            if (_flash != null)
            {
                if (on)
                {
                    Color colour = _combatant.FlashColor;
                    colour.a = FlashAlpha;
                    _flash.color = colour;
                    _flash.sprite = _renderer.sprite;
                }
                if (_flash.enabled != on)
                    _flash.enabled = on;
                return;
            }
            if (on == _tinted)
                return;
            _tinted = on;
            _renderer.color = on ? FallbackTint : Color.white;
        }

        private void OnAttacked()
        {
            _player.Strike(RenderedCharacterFrames.StrikeSeconds(_attack.Weapon?.Interval ?? 1f));
            Show();
        }

        // Struck supplies the actual target, including one this hit killed.
        private void OnStruck(Health target)
        {
            if (target == null)
                return;
            Vector3 toward = target.transform.position - transform.position;
            _player.FaceToward(toward.x, toward.y / ArenaFloor.DepthScale);
            Show();
        }

        private void OnUsed()
        {
            _player.Cast();
            Show();
        }

        private void OnDamaged(DamageContext context)
        {
            _player.Hit();
            Show();
        }

        private void OnDied()
        {
            _player.Die();
            Show();
        }

        private void Subscribe()
        {
            if (_subscribed || _attack == null)
                return;
            _attack.Attacked += OnAttacked;
            _attack.Struck += OnStruck;
            if (_ability != null)
                _ability.Used += OnUsed;
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
                _health.Died += OnDied;
            }
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            if (_attack != null)
            {
                _attack.Attacked -= OnAttacked;
                _attack.Struck -= OnStruck;
            }
            if (_ability != null)
                _ability.Used -= OnUsed;
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }
            _subscribed = false;
        }
    }
}
