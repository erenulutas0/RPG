using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Borrows either imported front/rear poses or the session-cached procedural look. Animation and renderers belong
    // to the enemy; neither path owns its sprites. Imported walking follows travelled floor distance, attacks use the
    // actual struck target, and a brief body compression accompanies the existing silhouette flash.
    public sealed class EnemyLookView : MonoBehaviour, ILookSprites
    {
        private const string BarName = "Health Bar";
        // Below this squared distance per frame the enemy counts as standing (pack motion moves whole texels).
        private const float StillThreshold = 1e-8f;

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

        private EnemySprites _sprites;
        private GameObject _bar;
        private SpriteRenderer _fillRenderer;
        private Vector3 _lastPosition;
        private bool _bobbed;
        private float _frameRemaining;
        private float _attackRemaining;
        private bool _subscribed;

        public EnemyLook Look => _look;
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
            // The prefab variants tint and scale the old square placeholder; the drawn look carries its own colour
            // and is already the size it should be on the 32 texel grid.
            _body.color = Color.white;
            _body.transform.localScale = Vector3.one;
            _body.sprite = _painted ? _paintedArt.GetFrame(0, false).Body : _sprites.Frame(EnemyPose.IdleA);
            CurrentPose = EnemyPose.IdleA;
            ContactShadowView.Attach(transform, _body, _painted ? _paintedArt.ShadowWidth : EnemyArt.WidthOf(_look) / PixelSpriteFactory.PixelsPerUnit * .82f, _health);
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
            // A Mite is much smaller than a Grunt. Scale the bar parent, retaining the shared sprites and fill ratio.
            if (_look == EnemyLook.Mite) _bar.transform.localScale = new Vector3(.65f, .8f, 1f);
            float top = _painted ? _paintedArt.VisualTop : EnemyArt.HeightOf(_look) / PixelSpriteFactory.PixelsPerUnit;
            _bar.transform.localPosition = new Vector3(0f, top + _barClearance, 0f);
            AddBarRenderer("Back", EnemySpriteCache.BarBack, Vector3.zero, _barSortingOrder);
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
            if (_bar != null)
                _bar.SetActive(false);
        }

        private void OnAttacked()
        {
            _attackRemaining = _attackFrameDuration;
            if (_painted) { ShowPainted(3); return; }
            Show(EnemyPose.Attack);
        }

        private void OnDamaged(DamageContext context) { if (_painted) _hitRemaining = .09f; }

        private void OnStruck(Health target)
        {
            if (!_painted || target == null) return;
            Face(target.transform.position - transform.position);
            ShowPainted(3);
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
            _body.sprite = _paintedArt.GetFrame(frame, RearFacing).Body;
            var scale = _body.transform.localScale; scale.x = Mathf.Abs(scale.x) * (Mirrored ? -1 : 1); _body.transform.localScale = scale;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            bool moving = (position - _lastPosition).sqrMagnitude > StillThreshold;
            _lastPosition = position;
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
            _subscribed = false;
        }

    }
}
