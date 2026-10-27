using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Borrows either imported front/rear poses or the session-cached procedural look. Animation and renderers belong
    // to the enemy; neither path owns its sprites. Imported walking follows travelled floor distance, attacks use the
    // actual struck target, and a brief body compression accompanies the existing silhouette flash. A horde newcomer
    // fades in on its arrival (Arrive); a fallen enemy's bar hides at once and its body is left to the pooled remains.
    // A set with the optional boss poses adds two more: a telegraphed blow raises the windup pose until it lands
    // (WindUp, Slam - the Forge Warden's slam, driven by WardenSlamView), carried at a walk by the distance travelled
    // when the set draws that, and the enemy's enrage swaps every pose for its second phase's (EnrageBehaviour). Every
    // other enemy behaves exactly as before.
    public sealed class EnemyLookView : MonoBehaviour, ILookSprites
    {
        private const string BarName = "Health Bar";
        // Below this squared distance per frame the enemy counts as standing (pack motion moves whole texels).
        private const float StillThreshold = 1e-8f;
        // PaintedFrame while the windup pose shows: after the set's six frames.
        public const int WindupFrame = 6;
        // How long the strike pose holds once a telegraphed blow lands: long enough to read as the blow, not a swing.
        private const float SlamFrameSeconds = .35f;

        [SerializeField] private EnemyLook _look = EnemyLook.Grunt;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Health _health;
        [SerializeField] private AttackController _attack;
        [SerializeField] private EnemyArtSet _paintedArt;
        private bool _painted;
        private float _walkDistance;
        private float _hitRemaining;
        public EnemyArtSet PaintedArt => _painted ? _paintedArt : null;
        public int PaintedFrame { get; private set; }
        public bool RearFacing { get; private set; }
        public bool Mirrored { get; private set; }
        // Frame swap intervals: the walking stride and the slow breath when standing.
        [SerializeField, Min(0.01f)] private float _walkFrameDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _breathFrameDuration = 1.1f;
        // Matches CombatantView's feedback duration so the wind-up and the nudge end together.
        [SerializeField, Min(0.01f)] private float _attackFrameDuration = 0.14f;
        // Gap between the top of the look's canvas and the bar, in world units.
        [SerializeField] private float _barClearance = 0.12f;
        [SerializeField] private int _barSortingOrder = 10;

        // How far below its rest the body starts an arrival, in world units: it rises out of the floor as it fades in.
        private const float ArrivalSink = .1f;

        private EnemySprites _sprites;
        private GameObject _bar;
        private SpriteRenderer _backRenderer;
        private SpriteRenderer _fillRenderer;
        private SpriteRenderer _shadow;
        private Vector3 _bodyRest;
        private float _arrivalDuration;
        private float _arrivalRemaining;
        private Vector3 _lastPosition;
        private bool _bobbed;
        private float _frameRemaining;
        private float _attackRemaining;
        private bool _subscribed;
        // Only with a painted second phase: the enrage that shows it.
        private EnrageBehaviour _enrage;

        public EnemyLook Look => _look;
        // A telegraphed blow is coming: the windup pose holds until Slam or CancelWindup.
        public bool IsWindingUp { get; private set; }
        // While the windup pose shows: the walking windup's quarter (EnemyArtSet.WalkingQuarter), or -1 standing.
        public int WindupStep { get; private set; } = -1;
        // The painted second phase is showing.
        public bool ShowsEnraged { get; private set; }
        // This look draws its enemy's enrage itself, so the body keeps its colours (CombatantView skips its tint).
        internal bool DrawsEnrage => _painted && _paintedArt.HasEnragedFrames;
        // The body renderer, for presentation that restyles it in place (the foundry's hidden-enemy silhouette).
        internal SpriteRenderer Body => _body;
        // The health bar root (a child of this enemy), for presentation that keeps it on top of scenery.
        internal Transform Bar => _bar != null ? _bar.transform : null;
        public EnemyPose CurrentPose { get; private set; }
        public bool HasSprites => _painted || (_sprites != null && _sprites.Frame(EnemyPose.IdleA) != null);

        private void Awake()
        {
            if (_body == null)
            {
                Debug.LogError("EnemyLookView needs the body renderer.", this);
                enabled = false;
                return;
            }
            if (_health == null)
                _health = GetComponent<Health>();
            if (_attack == null)
                _attack = GetComponent<AttackController>();

            _painted = _paintedArt != null && _paintedArt.IsValid;
            if (_paintedArt != null && !_painted) Debug.LogError("Enemy painted art set is incomplete.", this);
            if (!_painted) _sprites = EnemySpriteCache.Get(_look);
            if (DrawsEnrage) _enrage = GetComponent<EnrageBehaviour>();
            // The prefab variants tint and scale the old square placeholder; the drawn look carries its own colour
            // and is already the size it should be on the 32 texel grid.
            _body.color = Color.white;
            _body.transform.localScale = Vector3.one;
            _body.sprite = _painted ? _paintedArt.GetFrame(0, false).Body : _sprites.Frame(EnemyPose.IdleA);
            CurrentPose = EnemyPose.IdleA;
            GroundedSorting.Attach(transform, _body.sortingLayerID, 1);
            _shadow = ContactShadowView.Attach(transform, _body, _painted ? _paintedArt.ShadowWidth : EnemyArt.WidthOf(_look) / PixelSpriteFactory.PixelsPerUnit * .82f, _health).Renderer;
            _bodyRest = _body.transform.localPosition;
            BuildBar();
            _lastPosition = transform.position;
            _frameRemaining = _breathFrameDuration;
        }

        private void OnEnable()
        {
            if (!HasSprites)
                return;
            if (_health != null)
            {
                _health.Changed += RefreshBar;
                _health.Died += OnDied;
                _health.Damaged += OnDamaged;
            }
            if (_attack != null)
            {
                _attack.Attacked += OnAttacked;
                _attack.Struck += OnStruck;
            }
            if (_enrage != null)
            {
                _enrage.Enraged += OnEnraged;
                ShowsEnraged |= _enrage.IsEnraged;
            }
            _subscribed = true;
            RefreshBar();
        }

        // The encounter initialises the health right after Instantiate, so the bar shows its full width from the first frame.
        private void Start() => RefreshBar();

        // A pre-built white silhouette for any frame this view assigned to the body; null for anything else.
        public Sprite SilhouetteOf(Sprite bodySprite) => _painted ? _paintedArt.FlashOf(bodySprite) : _sprites?.SilhouetteOf(bodySprite);

        // The bar hangs off the enemy root, not the body, so attack nudges and the death hide do not move it; it is
        // hidden on death separately.
        private void BuildBar()
        {
            EnemySpriteCache.EnsureBars();
            _bar = new GameObject(BarName);
            _bar.transform.SetParent(transform, false);
            GroundedSorting.Attach(_bar.transform, _body.sortingLayerID, _barSortingOrder);
            // A Mite is much smaller than a Grunt. Scale the bar parent, retaining the shared sprites and fill ratio.
            if (_look == EnemyLook.Mite) _bar.transform.localScale = new Vector3(.65f, .8f, 1f);
            float top = _painted ? _paintedArt.VisualTop : EnemyArt.HeightOf(_look) / PixelSpriteFactory.PixelsPerUnit;
            _bar.transform.localPosition = new Vector3(0f, top + _barClearance, 0f);
            _backRenderer = AddBarRenderer("Back", EnemySpriteCache.BarBack, Vector3.zero, _barSortingOrder);
            // The fill's left edge sits one texel inside the backing's frame.
            float fillLeft = -EnemyArt.HealthFillWidth * 0.5f / PixelSpriteFactory.PixelsPerUnit;
            _fillRenderer = AddBarRenderer("Fill", EnemySpriteCache.BarFill, new Vector3(fillLeft, 0f, 0f), _barSortingOrder + 1);
        }

        private SpriteRenderer AddBarRenderer(string name, Sprite sprite, Vector3 localPosition, int sortingOrder)
        {
            var part = new GameObject(name);
            part.transform.SetParent(_bar.transform, false);
            part.transform.localPosition = localPosition;
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerID = _body.sortingLayerID;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void RefreshBar()
        {
            if (_bar == null || _health == null)
                return;
            bool alive = _health.IsAlive;
            if (_bar.activeSelf != alive)
                _bar.SetActive(alive);
            if (!alive)
                return;
            float fraction = _health.Maximum > 0f ? Mathf.Clamp01(_health.Current / _health.Maximum) : 1f;
            // Whole texels keep the fill on the pixel grid; ceiling keeps a one-texel sliver while any health is left.
            float texels = Mathf.Ceil(fraction * EnemyArt.HealthFillWidth);
            _fillRenderer.enabled = texels > 0f;
            _fillRenderer.transform.localScale = new Vector3(texels / EnemyArt.HealthFillWidth, 1f, 1f);
        }

        private void OnDied()
        {
            IsWindingUp = false;
            WindupStep = -1;
            if (_bar != null)
                _bar.SetActive(false);
            // The body is hidden at once (CombatantView) and its pooled remains take over; an arrival cut short ends here.
            if (_arrivalRemaining > 0f)
                EndArrival();
        }

        // A horde enemy walking onto the floor fades in over seconds, its body rising out of the floor, rather than
        // popping into being. Body, bar and contact shadow share the fade through their renderer colours (no material is
        // touched, so the foundry's shared x-ray material stays shared and its silhouette pass fades with the vertex alpha).
        // Scaled time, so a pause holds it.
        internal void Arrive(float seconds)
        {
            if (_body == null || seconds <= 0f || (_health != null && !_health.IsAlive))
                return;
            _arrivalDuration = seconds;
            _arrivalRemaining = seconds;
            ShowArrival(0f);
        }

        // From 0 as the enemy appears to 1 once it stands in full; 1 for any enemy that is not arriving.
        internal float ArrivalProgress => _arrivalRemaining > 0f ? 1f - _arrivalRemaining / _arrivalDuration : 1f;

        private void TickArrival()
        {
            _arrivalRemaining -= Time.deltaTime;
            if (_arrivalRemaining <= 0f)
            {
                EndArrival();
                return;
            }
            ShowArrival(ArrivalProgress);
        }

        private void EndArrival()
        {
            _arrivalRemaining = 0f;
            ShowArrival(1f);
        }

        // Eases out: most of the body is there early, so a newcomer reads as a threat at once and only settles after.
        private void ShowArrival(float progress)
        {
            float eased = 1f - (1f - progress) * (1f - progress);
            SetAlpha(_body, eased);
            SetAlpha(_backRenderer, eased);
            SetAlpha(_fillRenderer, eased);
            SetAlpha(_shadow, eased);
            _body.transform.localPosition = _bodyRest + Vector3.down * (ArrivalSink * (1f - eased));
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
                return;
            Color color = renderer.color;
            if (color.a == alpha)
                return;
            color.a = alpha;
            renderer.color = color;
        }

        private void OnAttacked()
        {
            // The raised hammer holds through the boss's own swings: the telegraph is the read that matters.
            if (IsWindingUp) return;
            // A landed blow's longer hold is not cut short by a swing (for any other enemy this is the plain duration).
            _attackRemaining = Mathf.Max(_attackRemaining, _attackFrameDuration);
            if (_painted) { ShowPainted(3); return; }
            Show(EnemyPose.Attack);
        }

        private void OnDamaged(DamageContext context) { if (_painted) _hitRemaining = .09f; }

        private void OnStruck(Health target)
        {
            if (!_painted || target == null || IsWindingUp) return;
            Face(target.transform.position - transform.position);
            ShowPainted(3);
        }

        // A telegraphed blow is coming (WardenSlamView, as the Warden's mark goes down): a painted look with a windup
        // pose raises it, turned toward where the blow will land, and holds it through walking and its own swings until
        // Slam or CancelWindup - carried at a walk if the set draws that, standing otherwise. Without one nothing
        // changes.
        internal void WindUp(Vector3 target)
        {
            if (!_painted || !_paintedArt.HasWindup || (_health != null && !_health.IsAlive)) return;
            Face(target - transform.position);
            IsWindingUp = true;
            // Caught mid-stride, the legs carry on where the walk had them.
            bool striding = _attackRemaining <= 0f && PaintedFrame != 0 && PaintedFrame != 3 && PaintedFrame < WindupFrame;
            _attackRemaining = 0f;
            ShowWindup(striding && _paintedArt.HasWalkingWindup(ShowsEnraged) ? _paintedArt.WalkingQuarter(_walkDistance) : -1);
        }

        // The telegraphed blow lands: the strike pose, turned toward where it landed, held a beat longer than a swing.
        internal void Slam(Vector3 target)
        {
            if (!_painted || (_health != null && !_health.IsAlive)) return;
            IsWindingUp = false;
            WindupStep = -1;
            Face(target - transform.position);
            _attackRemaining = SlamFrameSeconds;
            ShowPainted(3);
        }

        // The mark was withdrawn without a blow (the fight stopped): the pose goes back to walking or standing.
        internal void CancelWindup()
        {
            IsWindingUp = false;
            WindupStep = -1;
        }

        private void OnEnraged()
        {
            ShowsEnraged = true;
            if (IsWindingUp) ShowWindup(WindupStep >= 0 && _paintedArt.HasWalkingWindup(true) ? WindupStep : -1);
            else if (PaintedFrame < WindupFrame) ShowPainted(PaintedFrame);
        }

        private void Face(Vector3 delta)
        {
            var direction = new Vector2(delta.x, delta.y / ArenaFloor.DepthScale);
            if (direction.sqrMagnitude < StillThreshold) return;
            direction.Normalize();
            if (Mathf.Abs(direction.x) > .15f) Mirrored = direction.x > 0;
            if (Mathf.Abs(direction.y) > .15f) RearFacing = direction.y > 0;
        }

        private void TickPainted(Vector3 delta)
        {
            if (Time.deltaTime <= 0 || (_health != null && !_health.IsAlive)) return;
            _hitRemaining = Mathf.Max(0, _hitRemaining - Time.deltaTime);
            float compression = _hitRemaining / .09f;
            _body.transform.localScale = new Vector3((Mirrored ? -1 : 1) * (1 + .06f * compression), 1 - .08f * compression, 1);
            // Walking on under a raised hammer keeps it raised, facing where the blow will land.
            if (IsWindingUp) { TickWindup(delta); return; }
            _attackRemaining = Mathf.Max(0, _attackRemaining - Time.deltaTime);
            if (_attackRemaining > 0) return;
            float travel = new Vector2(delta.x, delta.y / ArenaFloor.DepthScale).magnitude;
            if (travel < .0001f) { _walkDistance = 0; ShowPainted(0); return; }
            Face(delta);
            _walkDistance = (_walkDistance + travel) % _paintedArt.WalkCycleDistance;
            ShowPainted(_paintedArt.WalkingFrame(_walkDistance));
            _body.transform.localScale = new Vector3((Mirrored ? -1 : 1) * (1 + .06f * compression), 1 - .08f * compression, 1);
        }

        private void ShowPainted(int frame)
        {
            PaintedFrame = frame;
            CurrentPose = frame == 3 ? EnemyPose.Attack : frame == 2 ? EnemyPose.IdleB : EnemyPose.IdleA;
            _body.sprite = _paintedArt.GetFrame(frame, RearFacing, ShowsEnraged).Body;
            var scale = _body.transform.localScale; scale.x = Mathf.Abs(scale.x) * (Mirrored ? -1 : 1); _body.transform.localScale = scale;
        }

        // The raised hammer steps with the legs by the distance travelled, as the walk does, in a set that draws it at a
        // walk (in the phase on show); standing still, or without those drawings, it holds the standing windup.
        private void TickWindup(Vector3 delta)
        {
            float travel = new Vector2(delta.x, delta.y / ArenaFloor.DepthScale).magnitude;
            if (travel < .0001f || !_paintedArt.HasWalkingWindup(ShowsEnraged)) { _walkDistance = 0; ShowWindup(-1); return; }
            _walkDistance = (_walkDistance + travel) % _paintedArt.WalkCycleDistance;
            ShowWindup(_paintedArt.WalkingQuarter(_walkDistance));
        }

        // step: the walking windup's quarter, or -1 for the standing windup.
        private void ShowWindup(int step)
        {
            PaintedFrame = WindupFrame;
            WindupStep = step;
            CurrentPose = EnemyPose.Attack;
            _body.sprite = (step < 0 ? _paintedArt.GetWindup(RearFacing, ShowsEnraged)
                : _paintedArt.GetWalkingWindup(step, RearFacing, ShowsEnraged)).Body;
            var scale = _body.transform.localScale; scale.x = Mathf.Abs(scale.x) * (Mirrored ? -1 : 1); _body.transform.localScale = scale;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            bool moving = (position - _lastPosition).sqrMagnitude > StillThreshold;
            _lastPosition = position;
            if (_arrivalRemaining > 0f) TickArrival();
            if (_painted) { TickPainted(delta); return; }

            if (_attackRemaining > 0f)
            {
                // Unscaled, like CombatantView's nudge, so a wind-up still settles during an upgrade pause.
                _attackRemaining -= Time.unscaledDeltaTime;
                if (_attackRemaining > 0f)
                    return;
                Show(_bobbed ? EnemyPose.IdleB : EnemyPose.IdleA);
            }

            // Starting to walk mid-breath must not wait out the rest of the slow breath.
            if (moving && _frameRemaining > _walkFrameDuration)
                _frameRemaining = _walkFrameDuration;
            _frameRemaining -= Time.deltaTime;
            if (_frameRemaining > 0f)
                return;
            _bobbed = !_bobbed;
            _frameRemaining = moving ? _walkFrameDuration : _breathFrameDuration;
            Show(_bobbed ? EnemyPose.IdleB : EnemyPose.IdleA);
        }

        private void Show(EnemyPose pose)
        {
            if (CurrentPose == pose)
                return;
            CurrentPose = pose;
            _body.sprite = _sprites.Frame(pose);
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;
            if (_health != null)
            {
                _health.Changed -= RefreshBar;
                _health.Died -= OnDied;
                _health.Damaged -= OnDamaged;
            }
            if (_attack != null)
            {
                _attack.Attacked -= OnAttacked;
                _attack.Struck -= OnStruck;
            }
            if (_enrage != null)
                _enrage.Enraged -= OnEnraged;
            _subscribed = false;
        }

    }
}
