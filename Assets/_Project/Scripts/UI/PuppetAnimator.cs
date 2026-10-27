using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // The hero as a cut-out puppet (docs/45 section 4 B): one painting per facing, cut into parts and animated by
    // PuppetGait, so walk, strike, flinch, death and idle all come from the same pieces with nothing boiling between
    // frames. Development builds and the Editor only for now: development/puppet.txt asks for it (DevelopmentStart.Puppet)
    // and HeroLookView hands the hero over and steps aside. Without a PuppetArtSet it builds code-drawn placeholder parts.
    //
    // VanguardAnimator's contract, kept: presentation only - it observes the hero's floor displacement, its first weapon's
    // strikes and its health, and never ticks a weapon, moves the hero or applies damage. A paused combat (no scaled time)
    // freezes it. An attack moves the arms only, so a walking hero keeps its stride. It faces the travel, or the target
    // while it stands. It allocates nothing per frame.
    //
    // The puppet hangs beside the body rather than under it: CombatantView hides the body, and the weapons under it, at
    // death, and the puppet stays to play the collapse. The body keeps its renderer with no sprite, so CombatantView's
    // nudge and tint have nothing to draw; the puppet's parts show their own silhouettes while CombatantView flashes. The
    // weapons stay where HeroWeaponView shows and hides them and are placed on the puppet's hands each frame.
    [DisallowMultipleComponent]
    public sealed class PuppetAnimator : MonoBehaviour
    {
        public const string RootName = "Hero Puppet";
        // Below this a frame's floor travel is standing, as for VanguardAnimator.
        private const float MinimumTravel = .0001f;
        private const float FlinchRate = 1f / PuppetGait.FlinchSeconds;
        private const float DeathRate = 1f / PuppetGait.DeathSeconds;
        // HeroArt's left dagger is the right one mirrored, pointing up and back; a quarter turn points it forward too.
        private const float MirroredDaggerGrip = -90f;
        // The orb's centre above the staff's grip: three texels over the cap its cradle rests on (HeroArt.Anchor).
        private const float StaffCap = (HeroArt.StaffCapRow - HeroArt.StaffGripRow + 4) / PixelSpriteFactory.PixelsPerUnit;

        private readonly List<Sprite> _owned = new List<Sprite>();
        private SpriteRenderer _body;
        // HeroLookView's weapon renderers in HeroWeaponPart order, borrowed: HeroWeaponView still shows and hides them.
        private SpriteRenderer[] _weapons;
        private AttackController _attack;
        private Health _health;
        private CombatantView _combatant;
        private Sprite _orb;
        private Sprite _litOrb;
        private PuppetArtSet _art;
        private PuppetArtSet _ownedArt;
        private Transform _root;
        private PuppetRig _rear;
        private PuppetRig _front;
        private PuppetGaitInput _input;
        private PuppetPose _pose;
        private Vector3 _previous;
        private float _strikeRate;
        private float _leftDaggerGrip;
        private bool _paintedStaff;
        private bool _moving;
        private bool _dying;
        private bool _flashing;
        private Color _flashColor = Color.white;
        private bool _subscribed;

        // The pose shown this frame and the clocks it came from, so a test can rebuild it with PuppetGait.
        public PuppetPose Pose => _pose;
        public PuppetGaitInput GaitInput => _input;
        public bool IsAttacking => _input.Strike != PuppetStrike.None;
        public bool IsWalking => _moving;
        public bool IsDying => _dying;
        public bool FrontFacing { get; private set; }
        public bool Mirrored { get; private set; }
        public bool UsesPlaceholder => _ownedArt != null;
        public PuppetArtSet Art => _art;
        public Transform Root => _root;
        internal PuppetRig RearRig => _rear;
        internal PuppetRig FrontRig => _front;
        internal PuppetRig ActiveRig => FrontFacing ? _front : _rear;

        // Whether this run asked for the puppet: only the Editor and a development build ever look.
        public static bool Requested => DevelopmentStart.Puppet();

        // Builds the puppet on the hero beside its body. HeroLookView calls it from its own Awake, after building the
        // weapons, and is disabled before this runs so the two never drive the same renderers.
        internal static PuppetAnimator Attach(GameObject hero, SpriteRenderer body, SpriteRenderer[] weapons,
            AttackController attack, Sprite litOrb, PuppetArtSet art)
        {
            var puppet = hero.GetComponent<PuppetAnimator>();
            if (puppet == null)
                puppet = hero.AddComponent<PuppetAnimator>();
            puppet.Initialize(body, weapons, attack, litOrb, art);
            return puppet;
        }

        private void Initialize(SpriteRenderer body, SpriteRenderer[] weapons, AttackController attack, Sprite litOrb,
            PuppetArtSet art)
        {
            _body = body;
            _weapons = weapons;
            _attack = attack;
            _litOrb = litOrb;
            _health = GetComponent<Health>();
            _combatant = GetComponent<CombatantView>();
            _orb = weapons[(int)HeroWeaponPart.StaffOrb].sprite;
            if (art != null && !art.IsValid)
            {
                Debug.LogError("The puppet art set is incomplete; the placeholder parts stand in for it.", this);
                art = null;
            }
            if (art == null)
                art = _ownedArt = PuppetPlaceholder.Create(_owned);
            _art = art;

            var root = new GameObject(RootName);
            root.transform.SetParent(transform, false);
            _root = root.transform;
            _rear = PuppetRig.Build(art.Rear, _root, "Puppet Rear", body.sharedMaterial, body.sortingLayerID, body.sortingOrder);
            _front = PuppetRig.Build(art.Front, _root, "Puppet Front", body.sharedMaterial, body.sortingLayerID, body.sortingOrder);

            // Painted weapons, when the set brings them, are registered on their grips as the parts are on their joints.
            if (art.Sword != null)
                weapons[(int)HeroWeaponPart.Sword].sprite = art.Sword;
            if (art.Shield != null)
                weapons[(int)HeroWeaponPart.Shield].sprite = art.Shield;
            if (art.Staff != null)
            {
                weapons[(int)HeroWeaponPart.StaffShaft].sprite = art.Staff;
                weapons[(int)HeroWeaponPart.StaffOrb].enabled = false;
                _paintedStaff = true;
            }
            if (art.Dagger != null)
                weapons[(int)HeroWeaponPart.DaggerLeft].sprite = weapons[(int)HeroWeaponPart.DaggerRight].sprite = art.Dagger;
            _leftDaggerGrip = art.Dagger != null ? 0f : MirroredDaggerGrip;

            body.sprite = null;
            _previous = transform.position;
            ShowFacing();
            ApplyPose();
            Subscribe();
        }

        // AddComponent enables the component before Initialize has run; Initialize subscribes then.
        private void OnEnable()
        {
            if (_rear != null)
                Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_rear != null)
                EndStrike();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            foreach (Sprite sprite in _owned)
                PixelSpriteFactory.Destroy(sprite);
            _owned.Clear();
            if (_ownedArt != null)
                Destroy(_ownedArt);
        }

        // After every Update, so the hero's movement this frame is already in its position.
        private void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (_rear == null)
                return;
            Vector3 position = transform.position;
            Vector3 delta = position - _previous;
            _previous = position;
            // CombatantView's flash runs on unscaled time, so it settles even while combat is paused.
            RefreshFlash();
            if (dt <= 0f)
                return;

            if (_dying || (_health != null && !_health.IsAlive))
            {
                if (!_dying)
                    OnDied();
                _input.DeathProgress = Mathf.Min(1f, _input.DeathProgress + dt * DeathRate);
                ApplyPose();
                return;
            }

            float travel = new Vector2(delta.x, delta.y / ArenaFloor.DepthScale).magnitude;
            _moving = travel >= MinimumTravel;
            if (_moving)
            {
                _input.WalkPhase = PuppetGait.Advance(_input.WalkPhase, travel);
                Face(delta);
            }
            _input.WalkWeight = PuppetGait.Blend(_input.WalkWeight, _moving, dt);
            // Settled: the next walk starts on a contact, whatever the last one stopped on.
            if (_input.WalkWeight <= 0f)
                _input.WalkPhase = 0f;
            _input.IdleTime += dt;
            if (_input.IdleTime >= PuppetGait.BreathPeriod)
                _input.IdleTime -= PuppetGait.BreathPeriod;
            if (_input.Strike != PuppetStrike.None)
            {
                _input.StrikeProgress += dt * _strikeRate;
                if (_input.StrikeProgress >= 1f)
                    EndStrike();
            }
            if (_input.Flinch > 0f)
                _input.Flinch = Mathf.Max(0f, _input.Flinch - dt * FlinchRate);
            ApplyPose();
        }

        // The damage has already happened at this event: the arms show the strike at once, never a delayed windup.
        public void Attacked()
        {
            if (_rear == null || _dying)
                return;
            _input.Strike = _weapons[(int)HeroWeaponPart.StaffShaft].gameObject.activeInHierarchy ? PuppetStrike.Cast
                : _weapons[(int)HeroWeaponPart.DaggerLeft].gameObject.activeInHierarchy ? PuppetStrike.Thrust
                : PuppetStrike.Swing;
            _input.StrikeProgress = 0f;
            _strikeRate = 1f / PuppetGait.StrikeSeconds(_attack.Weapon?.Interval ?? 1f);
            if (_input.Strike == PuppetStrike.Cast && !_paintedStaff)
                _weapons[(int)HeroWeaponPart.StaffOrb].sprite = _litOrb;
            ApplyPose();
        }

        // Struck supplies the actual target, including one this hit killed. A walking hero keeps its travel facing so a
        // nearby enemy cannot turn the stride round every swing.
        public void FaceAttack(Vector3 target)
        {
            if (_rear == null || _moving || _dying)
                return;
            Face(target - transform.position);
            ApplyPose();
        }

        // Back to standing, as VanguardAnimator.Reset: no stride, no strike, no flinch. Not named Reset, which Unity reserves
        // for a component's Inspector defaults.
        public void ResetPose()
        {
            if (_rear == null)
                return;
            EndStrike();
            _input.WalkWeight = 0f;
            _input.WalkPhase = 0f;
            _input.Flinch = 0f;
            _moving = false;
            _previous = transform.position;
            ApplyPose();
        }

        private void ApplyPose()
        {
            _pose = PuppetGait.Evaluate(_input);
            PuppetRig rig = ActiveRig;
            rig.Apply(_pose);
            PlaceWeapons(rig);
        }

        private void EndStrike()
        {
            _input.Strike = PuppetStrike.None;
            _input.StrikeProgress = 0f;
            // A scene unload may already have destroyed the orb when this runs from OnDisable.
            SpriteRenderer orb = _weapons[(int)HeroWeaponPart.StaffOrb];
            if (!_paintedStaff && orb != null)
                orb.sprite = _orb;
        }

        private void Face(Vector3 delta)
        {
            var direction = new Vector2(delta.x, delta.y / ArenaFloor.DepthScale);
            if (direction.sqrMagnitude < .00000001f)
                return;
            direction.Normalize();
            bool mirrored = Mirrored;
            bool front = FrontFacing;
            // Each axis keeps its facing inside a small dead band, so nearly horizontal or vertical travel never flickers.
            if (Mathf.Abs(direction.x) > .15f)
                mirrored = direction.x < 0f;
            if (Mathf.Abs(direction.y) > .15f)
                front = direction.y < 0f;
            if (mirrored == Mirrored && front == FrontFacing)
                return;
            Mirrored = mirrored;
            FrontFacing = front;
            ShowFacing();
        }

        // Mirrors the puppet and the body (so the weapons under it) together; movement, targeting and the shadow are not
        // touched.
        private void ShowFacing()
        {
            _rear.Root.SetActive(!FrontFacing);
            _front.Root.SetActive(FrontFacing);
            var scale = new Vector3(Mirrored ? -1f : 1f, 1f, 1f);
            _root.localScale = scale;
            _body.transform.localScale = scale;
            ActiveRig.Flash(_flashing, _flashColor);
        }

        private void RefreshFlash()
        {
            bool on = _combatant != null && _combatant.IsFlashing;
            Color color = on ? _combatant.FlashColor : Color.white;
            if (on == _flashing && color == _flashColor)
                return;
            _flashing = on;
            _flashColor = color;
            ActiveRig.Flash(on, color);
        }

        // Each shown weapon on its hand's grip, turned with the hand. Worked out in the body's space, where the weapons
        // live under their loadouts: the body and the puppet are mirrored together, so only the rig's own chain remains.
        private void PlaceWeapons(PuppetRig rig)
        {
            if (!_body.gameObject.activeInHierarchy)
                return;
            Matrix4x4 toBody = _body.transform.worldToLocalMatrix;
            GripPose(toBody, rig.RightGrip, out Vector3 right, out float rightAngle);
            GripPose(toBody, rig.LeftGrip, out Vector3 left, out float leftAngle);
            int rightOrder = _body.sortingOrder + rig.RightItemSorting;
            int leftOrder = _body.sortingOrder + rig.LeftItemSorting;
            Hold(HeroWeaponPart.Sword, right, rightAngle, rightOrder);
            Hold(HeroWeaponPart.StaffShaft, right, rightAngle, rightOrder);
            Hold(HeroWeaponPart.DaggerRight, right, rightAngle, rightOrder);
            Hold(HeroWeaponPart.Shield, left, leftAngle, leftOrder);
            Hold(HeroWeaponPart.DaggerLeft, left, leftAngle + _leftDaggerGrip, leftOrder);
            if (_paintedStaff)
                return;
            // The orb is its own renderer, not a child of the shaft: carried on the cap and turned with it.
            float radians = rightAngle * Mathf.Deg2Rad;
            var cap = new Vector3(-Mathf.Sin(radians) * StaffCap, Mathf.Cos(radians) * StaffCap, 0f);
            Hold(HeroWeaponPart.StaffOrb, right + cap, rightAngle, rightOrder + 1);
        }

        // The grip's position and turn in the body's space. The turn is read from the grip's up axis, which stays true
        // inside a left-painted rig's mirror where its right axis would not.
        private static void GripPose(Matrix4x4 toBody, Transform grip, out Vector3 position, out float angle)
        {
            Matrix4x4 m = toBody * grip.localToWorldMatrix;
            position = new Vector3(m.m03, m.m13, 0f);
            angle = Mathf.Atan2(-m.m01, m.m11) * Mathf.Rad2Deg;
        }

        private void Hold(HeroWeaponPart part, Vector3 position, float angle, int order)
        {
            SpriteRenderer weapon = _weapons[(int)part];
            if (!weapon.gameObject.activeInHierarchy)
                return;
            Transform held = weapon.transform;
            held.localPosition = position;
            held.localRotation = Quaternion.Euler(0f, 0f, angle);
            weapon.sortingOrder = order;
        }

        private void OnAttacked() => Attacked();

        private void OnStruck(Health target)
        {
            if (target != null)
                FaceAttack(target.transform.position);
        }

        private void OnDamaged(DamageContext context)
        {
            if (!_dying)
                _input.Flinch = 1f;
        }

        private void OnDied()
        {
            _dying = true;
            EndStrike();
            _input.Flinch = 0f;
        }

        private void Subscribe()
        {
            if (_subscribed || _attack == null)
                return;
            _attack.Attacked += OnAttacked;
            _attack.Struck += OnStruck;
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
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }
            _subscribed = false;
        }
    }
}
