using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // The hero as a cut-out puppet (docs/45 section 4 B): one painting per facing, cut into parts and animated by
    // PuppetMotion and PuppetGait, so walk, strike, cast, flinch, death and idle all come from the same pieces with nothing
    // boiling between frames. Development builds and the Editor only for now: development/puppet.txt asks for it
    // (DevelopmentStart.Puppet) and HeroLookView hands the hero over and steps aside. Without a PuppetArtSet it builds
    // code-drawn placeholder parts.
    //
    // VanguardAnimator's contract, kept: presentation only - it observes the hero's floor displacement, its first weapon's
    // cooldown and strikes, its ability and its health, and never ticks a weapon, moves the hero or applies damage. A
    // paused combat (no scaled time) freezes it. An attack moves the arms and chest only, so a walking hero keeps its
    // stride; the draw before it reads the weapon's cooldown, as the Vanguard's anticipation did. It faces the travel, or
    // the target while it stands. It allocates nothing per frame.
    //
    // The puppet hangs beside the body rather than under it: CombatantView hides the body, and the weapons under it, at
    // death, and the puppet stays to play the collapse. The body keeps its renderer with no sprite, so CombatantView's
    // nudge and tint have nothing to draw; the puppet's parts show their own silhouettes while CombatantView flashes. The
    // weapons stay where HeroWeaponView shows and hides them and are placed on the puppet's hands each frame.
    [DisallowMultipleComponent]
    public sealed class PuppetAnimator : MonoBehaviour
    {
        public const string RootName = "Hero Puppet";
        // HeroArt's left dagger is the right one mirrored, pointing up and back; a quarter turn points it forward too.
        private const float MirroredDaggerGrip = -90f;
        // The orb's centre above the staff's grip: three texels over the cap its cradle rests on (HeroArt.Anchor).
        private const float StaffCap = (HeroArt.StaffCapRow - HeroArt.StaffGripRow + 4) / PixelSpriteFactory.PixelsPerUnit;
        // Seen from the front the painted Vanguard frames carried the sword point-down and back and the daggers tilted out
        // from the body, so neither crosses the chest; from behind each is carried as drawn, pointing up and forward. As
        // a weapon is used (PuppetPose.Wield) it turns back to the angle it was drawn at, so strikes play as authored.
        public const float FrontSwordCarry = -180f;
        public const float FrontDaggerCarry = 35f;

        private readonly List<Sprite> _owned = new List<Sprite>();
        private readonly PuppetMotion _motion = new PuppetMotion();
        private SpriteRenderer _body;
        // HeroLookView's weapon renderers in HeroWeaponPart order, borrowed: HeroWeaponView still shows and hides them.
        private SpriteRenderer[] _weapons;
        private AttackController _attack;
        private AbilityController _ability;
        private Targeting _targeting;
        private Health _health;
        private CombatantView _combatant;
        private Sprite _orb;
        private Sprite _litOrb;
        private PuppetArtSet _art;
        private PuppetArtSet _ownedArt;
        private Transform _root;
        private PuppetRig _rear;
        private PuppetRig _front;
        private Vector3 _previous;
        private float _leftDaggerGrip;
        private bool _paintedStaff;
        private bool _orbLit;
        private bool _flashing;
        private Color _flashColor = Color.white;
        private bool _subscribed;
        // Painted parts swap whole to their white copies and cannot fade, so the evolution flash shows while strong.
        private const float OverlayFlashShown = .3f;

        // RunFeedbackView's evolution flash, from 0 to 1, while no hit flash shows: the body this puppet hides cannot
        // carry it.
        internal float OverlayFlash { get; set; }

        // The pose shown this frame and the clocks it came from, so a test can rebuild it with PuppetGait.
        public PuppetPose Pose => _motion.Pose;
        public PuppetGaitInput GaitInput => _motion.Input;
        public bool IsAttacking => _motion.IsAttacking;
        public bool IsWalking => _motion.IsWalking;
        public bool IsCasting => _motion.IsCasting;
        public bool IsDying => _motion.IsDying;
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
            _ability = GetComponent<AbilityController>();
            _targeting = GetComponent<Targeting>();
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
            {
                _motion.Interrupt();
                UnlightOrb();
            }
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

        // After every Update, so the hero's movement and its weapon's cooldown this frame are already in.
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

            if (!_motion.IsDying && _health != null && !_health.IsAlive)
                OnDied();
            if (_motion.IsDying)
            {
                _motion.Tick(dt, 0f);
                ApplyPose();
                return;
            }

            float travel = new Vector2(delta.x, delta.y / ArenaFloor.DepthScale).magnitude;
            if (travel >= PuppetMotion.MinimumTravel)
                Face(delta);
            Anticipate();
            _motion.Tick(dt, travel);
            if (!_motion.IsAttacking)
                UnlightOrb();
            ApplyPose();
        }

        // The damage has already happened at this event: the arms show the blow at once, never a delayed windup.
        public void Attacked()
        {
            if (_rear == null || _motion.IsDying)
                return;
            PuppetStrike strike = CarriedStrike();
            _motion.Strike(strike, _attack.Weapon?.Interval ?? 1f);
            if (strike == PuppetStrike.Cast && !_paintedStaff)
            {
                _weapons[(int)HeroWeaponPart.StaffOrb].sprite = _litOrb;
                _orbLit = true;
            }
            ApplyPose();
        }

        // The ability fired: the crouch, raise and slam play over whatever the legs and the weapon are doing.
        public void Cast()
        {
            if (_rear == null || _motion.IsDying)
                return;
            _motion.Cast();
            ApplyPose();
        }

        // Struck supplies the actual target, including one this hit killed. A walking hero keeps its travel facing so a
        // nearby enemy cannot turn the stride round every swing.
        public void FaceAttack(Vector3 target)
        {
            if (_rear == null || _motion.IsWalking || _motion.IsDying)
                return;
            Face(target - transform.position);
            ApplyPose();
        }

        // Back to standing, as VanguardAnimator.Reset: no stride, no strike, no draw, no cast, no flinch. Not named
        // Reset, which Unity reserves for a component's Inspector defaults.
        public void ResetPose()
        {
            if (_rear == null)
                return;
            _motion.Reset();
            UnlightOrb();
            _previous = transform.position;
            ApplyPose();
        }

        // The draw before the next attack, read off the first weapon's cooldown as AttackController will fire it: once
        // the cooldown is inside its draw and a target stands in reach, the weapon rises so its top meets the damage
        // event. A disabled controller ticks no cooldown, so it draws nothing.
        private void Anticipate()
        {
            WeaponRuntime weapon = _attack != null && _attack.isActiveAndEnabled ? _attack.Weapon : null;
            float progress = 0f;
            if (weapon != null && weapon.CooldownRemaining > 0f)
            {
                float window = PuppetGait.WindupSeconds(weapon.Interval);
                if (weapon.CooldownRemaining <= window && _targeting != null && _targeting.Acquire(weapon.Range) != null)
                    progress = 1f - weapon.CooldownRemaining / window;
            }
            _motion.Anticipate(progress > 0f ? CarriedStrike() : PuppetStrike.None, progress);
        }

        // The strike the shown loadout plays.
        private PuppetStrike CarriedStrike() =>
            _weapons[(int)HeroWeaponPart.StaffShaft].gameObject.activeInHierarchy ? PuppetStrike.Cast
            : _weapons[(int)HeroWeaponPart.DaggerLeft].gameObject.activeInHierarchy ? PuppetStrike.Thrust
            : PuppetStrike.Swing;

        private void ApplyPose()
        {
            PuppetRig rig = ActiveRig;
            rig.Apply(_motion.Pose);
            PlaceWeapons(rig);
        }

        private void UnlightOrb()
        {
            if (!_orbLit)
                return;
            _orbLit = false;
            // A scene unload may already have destroyed the orb when this runs from OnDisable.
            SpriteRenderer orb = _weapons[(int)HeroWeaponPart.StaffOrb];
            if (orb != null)
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

        // Mirrors the puppet and the body (so the weapons under it) together and stands the bob on the shown facing's
        // legs; movement, targeting and the shadow are not touched.
        private void ShowFacing()
        {
            _rear.Root.SetActive(!FrontFacing);
            _front.Root.SetActive(FrontFacing);
            var scale = new Vector3(Mirrored ? -1f : 1f, 1f, 1f);
            _root.localScale = scale;
            _body.transform.localScale = scale;
            _motion.Legs = ActiveRig.Legs;
            ActiveRig.Flash(_flashing, _flashColor);
        }

        private void RefreshFlash()
        {
            bool hit = _combatant != null && _combatant.IsFlashing;
            bool on = hit || OverlayFlash > OverlayFlashShown;
            Color color = hit ? _combatant.FlashColor : Color.white;
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
            float carry = FrontFacing ? 1f - _motion.Pose.Wield : 0f;
            Hold(HeroWeaponPart.Sword, right, rightAngle + carry * FrontSwordCarry, rightOrder);
            Hold(HeroWeaponPart.StaffShaft, right, rightAngle, rightOrder);
            Hold(HeroWeaponPart.DaggerRight, right, rightAngle + carry * FrontDaggerCarry, rightOrder);
            Hold(HeroWeaponPart.Shield, left, leftAngle, leftOrder);
            Hold(HeroWeaponPart.DaggerLeft, left, leftAngle + _leftDaggerGrip - carry * FrontDaggerCarry, leftOrder);
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

        private void OnCast() => Cast();

        private void OnStruck(Health target)
        {
            if (target != null)
                FaceAttack(target.transform.position);
        }

        private void OnDamaged(DamageContext context) => _motion.Hit();

        private void OnDied()
        {
            _motion.Die();
            UnlightOrb();
        }

        private void Subscribe()
        {
            if (_subscribed || _attack == null)
                return;
            _attack.Attacked += OnAttacked;
            _attack.Struck += OnStruck;
            if (_ability != null)
                _ability.Used += OnCast;
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
                _ability.Used -= OnCast;
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }
            _subscribed = false;
        }
    }
}
