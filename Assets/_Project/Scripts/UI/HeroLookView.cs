using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Borrows an optional imported Vanguard set; HeroArt remains the fallback and supplies Staff/Daggers equipment.
    // The imported animator observes movement and weapon timing without changing either. CombatantView hides the body
    // on death; the scene disables its old upward attack nudge to retain the imported hero's floor contact.
    // It runs before the other hero components so CombatantView reads a white body colour as the resting tint.
    [DefaultExecutionOrder(-50)]
    public sealed class HeroLookView : MonoBehaviour, ILookSprites
    {
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private AttackController _attack;
        [SerializeField] private SpriteRenderer _sword;
        [SerializeField] private SpriteRenderer _shield;
        [SerializeField] private SpriteRenderer _staffShaft;
        [SerializeField] private SpriteRenderer _staffOrb;
        [SerializeField] private SpriteRenderer _daggerLeft;
        [SerializeField] private SpriteRenderer _daggerRight;
        [SerializeField] private VanguardArtSet _paintedArt;
        private VanguardAnimator _painted;
        // Idle frames alternate at this interval; a swing lasts about as long as CombatantView's attack nudge.
        [SerializeField, Min(0.05f)] private float _breathInterval = 0.5f;
        [SerializeField, Min(0.01f)] private float _swingDuration = 0.12f;
        // The sword sweeps from +angle to -angle around its grip, the daggers thrust this far (world units) and back,
        // the staff leans by this angle while its orb lights.
        [SerializeField] private float _swingAngle = 40f;
        [SerializeField] private float _daggerThrust = 0.12f;
        [SerializeField] private float _staffLean = -14f;

        private const int PoseCount = 3;
        private const int PartCount = 6;

        private readonly Sprite[] _bodySprites = new Sprite[PoseCount];
        private readonly Sprite[] _silhouettes = new Sprite[PoseCount];
        private readonly Sprite[] _weaponSprites = new Sprite[PartCount];
        private readonly SpriteRenderer[] _weapons = new SpriteRenderer[PartCount];
        // Each part's local position on the body per pose, so the weapons follow the gauntlet between frames.
        private readonly Vector3[] _anchors = new Vector3[PoseCount * PartCount];
        private Sprite _litOrb;
        private PixelCanvas _bodyCanvas;
        private HeroPose _pose;
        private float _breathElapsed;
        private float _swingRemaining;
        private bool _casting;
        private bool _built;
        private bool _subscribed;

        // The idle body as drawn, for anything that wants to derive from it; the hit-flash silhouettes are pre-built.
        public PixelCanvas BodyCanvas => _bodyCanvas;
        public HeroPose Pose => _pose;
        public bool IsSwinging => _painted != null ? _painted.IsAttacking : _swingRemaining > 0f;
        public bool UsesPaintedArt => _painted != null;
        public int PaintedFrame => _painted?.FrameIndex ?? -1;
        public bool FrontFacing => _painted != null && _painted.FrontFacing;
        public bool Mirrored => _painted != null && _painted.Mirrored;
        public VanguardArtSet PaintedArt => _paintedArt;

        private void Awake()
        {
            if (_body == null || _attack == null || _sword == null || _shield == null || _staffShaft == null
                || _staffOrb == null || _daggerLeft == null || _daggerRight == null)
            {
                Debug.LogError("HeroLookView needs the body renderer, the attack controller and the six weapon renderers.", this);
                enabled = false;
                return;
            }
            Build();
            GroundedSorting.Attach(transform, _body.sortingLayerID, 1);
            ContactShadowView.Attach(transform, _body, .78f, GetComponent<Health>());
        }

        private void OnEnable()
        {
            if (!_built)
                return;
            _attack.Attacked += OnAttacked;
            _attack.Struck += OnStruck;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _attack.Attacked -= OnAttacked;
                _attack.Struck -= OnStruck;
                _subscribed = false;
            }
            if (_built)
                EndSwing();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < PoseCount; i++)
            {
                PixelSpriteFactory.Destroy(_bodySprites[i]);
                PixelSpriteFactory.Destroy(_silhouettes[i]);
            }
            for (int i = 0; i < PartCount; i++)
                PixelSpriteFactory.Destroy(_weaponSprites[i]);
            PixelSpriteFactory.Destroy(_litOrb);
        }

        // The pre-built white silhouette of one of the body frames, for CombatantView's hit flash; null for any other sprite.
        public Sprite SilhouetteOf(Sprite bodySprite)
        {
            if (bodySprite == null)
                return null;
            if (_painted != null)
            {
                Sprite paintedFlash = _paintedArt.FlashOf(bodySprite);
                if (paintedFlash != null) return paintedFlash;
            }
            for (int i = 0; i < PoseCount; i++)
            {
                if (_bodySprites[i] == bodySprite)
                    return _silhouettes[i];
            }
            return null;
        }

        private void Build()
        {
            for (int i = 0; i < PoseCount; i++)
            {
                var pose = (HeroPose)i;
                PixelCanvas canvas = HeroArt.DrawBody(pose);
                if (pose == HeroPose.IdleA)
                    _bodyCanvas = canvas;
                _bodySprites[i] = PixelSpriteFactory.CreateSprite(canvas, "Hero Body " + pose, PixelSpriteFactory.BottomCentre);
                _silhouettes[i] = PixelSpriteFactory.CreateSprite(canvas.Silhouette(Rgba.White), "Hero Flash " + pose,
                    PixelSpriteFactory.BottomCentre);
            }

            _weapons[(int)HeroWeaponPart.Sword] = _sword;
            _weapons[(int)HeroWeaponPart.Shield] = _shield;
            _weapons[(int)HeroWeaponPart.StaffShaft] = _staffShaft;
            _weapons[(int)HeroWeaponPart.StaffOrb] = _staffOrb;
            _weapons[(int)HeroWeaponPart.DaggerLeft] = _daggerLeft;
            _weapons[(int)HeroWeaponPart.DaggerRight] = _daggerRight;

            int bodyOrder = _body.sortingOrder;
            for (int i = 0; i < PartCount; i++)
            {
                var part = (HeroWeaponPart)i;
                PixelCanvas canvas = HeroArt.DrawWeapon(part);
                HeroArt.Pivot(part, out int pivotX, out int pivotY);
                _weaponSprites[i] = PixelSpriteFactory.CreateSprite(canvas, "Hero " + part, PivotOf(canvas, pivotX, pivotY));

                // The placeholders were tinted, scaled squares: reset them to plain sprites on the body's texel grid.
                SpriteRenderer renderer = _weapons[i];
                renderer.sprite = _weaponSprites[i];
                renderer.color = Color.white;
                renderer.sortingLayerID = _body.sortingLayerID;
                renderer.sortingOrder = bodyOrder + HeroArt.SortingOffset(part);
                Transform child = renderer.transform;
                child.localScale = Vector3.one;
                child.localRotation = Quaternion.identity;
                // The anchors are body-relative, so the loadout root between the body and the part must not offset them.
                // The body itself is left alone: CombatantView owns its local position.
                Transform root = child.parent;
                if (root != null && root != _body.transform)
                {
                    root.localPosition = Vector3.zero;
                    root.localRotation = Quaternion.identity;
                    root.localScale = Vector3.one;
                }
                for (int p = 0; p < PoseCount; p++)
                {
                    HeroArt.Anchor(part, (HeroPose)p, out int anchorX, out int anchorY);
                    _anchors[p * PartCount + i] = new Vector3(
                        (anchorX + 0.5f - HeroArt.BodyWidth * 0.5f) / PixelSpriteFactory.PixelsPerUnit,
                        (anchorY + 0.5f) / PixelSpriteFactory.PixelsPerUnit,
                        0f);
                }
            }

            PixelCanvas litOrb = HeroArt.DrawStaffOrb(true);
            HeroArt.Pivot(HeroWeaponPart.StaffOrb, out int orbX, out int orbY);
            _litOrb = PixelSpriteFactory.CreateSprite(litOrb, "Hero Staff Orb Lit", PivotOf(litOrb, orbX, orbY));

            _body.color = Color.white;
            _body.transform.localScale = Vector3.one;
            _built = true;
            ShowPose(HeroPose.IdleA);
            if (_paintedArt != null)
            {
                if (!_paintedArt.IsValid) Debug.LogError("Vanguard painted art set is incomplete.", this);
                else _painted = new VanguardAnimator(_paintedArt, _body, _weapons, transform, _attack, _litOrb);
            }
        }

        private void Update()
        {
            if (_painted != null)
            {
                _painted.Tick(Time.deltaTime);
                _pose = _painted.FrameIndex >= 5 ? HeroPose.Attack : HeroPose.IdleA;
                return;
            }
            if (_swingRemaining > 0f)
            {
                // Unscaled, like CombatantView's nudge, so a swing settles even while an upgrade choice pauses combat.
                _swingRemaining -= Time.unscaledDeltaTime;
                if (_swingRemaining <= 0f)
                    EndSwing();
                else
                    AnimateSwing(1f - _swingRemaining / _swingDuration);
                return;
            }

            _breathElapsed += Time.deltaTime;
            if (_breathElapsed >= _breathInterval)
            {
                _breathElapsed -= _breathInterval;
                ShowPose(_pose == HeroPose.IdleA ? HeroPose.IdleB : HeroPose.IdleA);
            }
        }

        private void OnStruck(Health target)
        {
            if (_painted != null && target != null) _painted.FaceAttack(target.transform.position);
        }

        private void OnAttacked()
        {
            if (_painted != null) { _painted.Attacked(); return; }
            // The staff casts standing: its orb lights and the shaft leans while the body keeps its idle frame, since
            // the raised-arm frame would leave the staff floating at the hip.
            _casting = _staffShaft.gameObject.activeInHierarchy;
            if (_casting)
                _staffOrb.sprite = _litOrb;
            else
                ShowPose(HeroPose.Attack);
            _swingRemaining = _swingDuration;
            AnimateSwing(0f);
        }

        // t runs from 0 at the start of the swing to 1 at its end.
        private void AnimateSwing(float t)
        {
            float pulse = Mathf.Sin(t * Mathf.PI);
            int row = (int)_pose * PartCount;
            if (_casting)
            {
                // The orb is not a child of the shaft, so it is turned about the grip by hand to stay on the cap.
                Quaternion lean = Quaternion.Euler(0f, 0f, _staffLean * pulse);
                Vector3 grip = _anchors[row + (int)HeroWeaponPart.StaffShaft];
                Vector3 orb = _anchors[row + (int)HeroWeaponPart.StaffOrb];
                _staffShaft.transform.localRotation = lean;
                _staffOrb.transform.localRotation = lean;
                _staffOrb.transform.localPosition = grip + lean * (orb - grip);
                return;
            }

            _sword.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(_swingAngle, -_swingAngle, t));
            Vector3 thrust = Vector3.up * (_daggerThrust * pulse);
            _daggerLeft.transform.localPosition = _anchors[row + (int)HeroWeaponPart.DaggerLeft] + thrust;
            _daggerRight.transform.localPosition = _anchors[row + (int)HeroWeaponPart.DaggerRight] + thrust;
        }

        private void EndSwing()
        {
            if (_painted != null) { _painted.Reset(); return; }
            _swingRemaining = 0f;
            _casting = false;
            _sword.transform.localRotation = Quaternion.identity;
            _staffShaft.transform.localRotation = Quaternion.identity;
            _staffOrb.transform.localRotation = Quaternion.identity;
            _staffOrb.sprite = _weaponSprites[(int)HeroWeaponPart.StaffOrb];
            _breathElapsed = 0f;
            ShowPose(HeroPose.IdleA);
        }

        private void ShowPose(HeroPose pose)
        {
            _pose = pose;
            _body.sprite = _bodySprites[(int)pose];
            int row = (int)pose * PartCount;
            for (int i = 0; i < PartCount; i++)
                _weapons[i].transform.localPosition = _anchors[row + i];
        }

        // The centre of the given texel in normalised sprite coordinates.
        private static Vector2 PivotOf(PixelCanvas canvas, int x, int y) =>
            new Vector2((x + 0.5f) / canvas.Width, (y + 0.5f) / canvas.Height);
    }
}
