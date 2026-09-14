using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Dresses an enemy in its generated placeholder look: the three EnemyArt frames (two idle/walk, one attack) drawn
    // once in Awake, a white silhouette of each for CombatantView's hit flash, and a small health bar floating above
    // the head. Walking alternates the idle frames at a stride cadence; standing still alternates them slowly as a
    // breath; an attack shows the wind-up frame for a moment. Everything the view builds is destroyed with it.
    public sealed class EnemyLookView : MonoBehaviour, ILookSprites
    {
        private const string BarName = "Health Bar";
        private const int PoseCount = 3;
        // Below this squared distance per frame the enemy counts as standing (pack motion moves whole texels).
        private const float StillThreshold = 1e-8f;

        [SerializeField] private EnemyLook _look = EnemyLook.Grunt;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Health _health;
        [SerializeField] private AttackController _attack;
        // Frame swap intervals: the walking stride and the slow breath when standing.
        [SerializeField, Min(0.01f)] private float _walkFrameDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _breathFrameDuration = 1.1f;
        // Matches CombatantView's feedback duration so the wind-up and the nudge end together.
        [SerializeField, Min(0.01f)] private float _attackFrameDuration = 0.14f;
        // Gap between the top of the look's canvas and the bar, in world units.
        [SerializeField] private float _barClearance = 0.12f;
        [SerializeField] private int _barSortingOrder = 10;

        private readonly Sprite[] _frames = new Sprite[PoseCount];
        private readonly Sprite[] _silhouettes = new Sprite[PoseCount];
        private Sprite _barBack;
        private Sprite _barFill;
        private GameObject _bar;
        private SpriteRenderer _fillRenderer;
        private Vector3 _lastPosition;
        private bool _bobbed;
        private float _frameRemaining;
        private float _attackRemaining;
        private bool _subscribed;

        public EnemyLook Look => _look;
        public EnemyPose CurrentPose { get; private set; }
        public bool HasSprites => _frames[0] != null;

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

            BuildSprites();
            // The prefab variants tint and scale the old square placeholder; the drawn look carries its own colour
            // and is already the size it should be on the 32 texel grid.
            _body.color = Color.white;
            _body.transform.localScale = Vector3.one;
            _body.sprite = _frames[(int)EnemyPose.IdleA];
            CurrentPose = EnemyPose.IdleA;
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
            }
            if (_attack != null)
                _attack.Attacked += OnAttacked;
            _subscribed = true;
            RefreshBar();
        }

        // The encounter initialises the health right after Instantiate, so the bar shows its full width from the first frame.
        private void Start() => RefreshBar();

        // A pre-built white silhouette for any frame this view assigned to the body; null for anything else.
        public Sprite SilhouetteOf(Sprite bodySprite)
        {
            if (bodySprite == null)
                return null;
            for (int i = 0; i < PoseCount; i++)
            {
                if (_frames[i] == bodySprite)
                    return _silhouettes[i];
            }
            return null;
        }

        private void BuildSprites()
        {
            for (int i = 0; i < PoseCount; i++)
            {
                var pose = (EnemyPose)i;
                PixelCanvas canvas = EnemyArt.Draw(_look, pose);
                string name = _look + " " + pose;
                _frames[i] = PixelSpriteFactory.CreateSprite(canvas, name, PixelSpriteFactory.BottomCentre);
                _silhouettes[i] = PixelSpriteFactory.CreateSprite(canvas.Silhouette(Rgba.White), name + " Flash",
                    PixelSpriteFactory.BottomCentre);
            }
            _barBack = PixelSpriteFactory.CreateSprite(EnemyArt.DrawHealthBarBack(), "Health Bar Back", PixelSpriteFactory.Centre);
            // The fill pivots on its left edge so scaling it by the health fraction drains it toward the left.
            _barFill = PixelSpriteFactory.CreateSprite(EnemyArt.DrawHealthBarFill(), "Health Bar Fill", new Vector2(0f, 0.5f));
        }

        // The bar hangs off the enemy root, not the body, so attack nudges and the death hide do not move it; it is
        // hidden on death separately.
        private void BuildBar()
        {
            _bar = new GameObject(BarName);
            _bar.transform.SetParent(transform, false);
            float top = EnemyArt.HeightOf(_look) / PixelSpriteFactory.PixelsPerUnit;
            _bar.transform.localPosition = new Vector3(0f, top + _barClearance, 0f);
            AddBarRenderer("Back", _barBack, Vector3.zero, _barSortingOrder);
            // The fill's left edge sits one texel inside the backing's frame.
            float fillLeft = -EnemyArt.HealthFillWidth * 0.5f / PixelSpriteFactory.PixelsPerUnit;
            _fillRenderer = AddBarRenderer("Fill", _barFill, new Vector3(fillLeft, 0f, 0f), _barSortingOrder + 1);
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
            Show(EnemyPose.Attack);
        }

        private void Update()
        {
            Vector3 position = transform.position;
            bool moving = (position - _lastPosition).sqrMagnitude > StillThreshold;
            _lastPosition = position;

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
            _body.sprite = _frames[(int)pose];
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;
            if (_health != null)
            {
                _health.Changed -= RefreshBar;
                _health.Died -= OnDied;
            }
            if (_attack != null)
                _attack.Attacked -= OnAttacked;
            _subscribed = false;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < PoseCount; i++)
            {
                PixelSpriteFactory.Destroy(_frames[i]);
                PixelSpriteFactory.Destroy(_silhouettes[i]);
            }
            PixelSpriteFactory.Destroy(_barBack);
            PixelSpriteFactory.Destroy(_barFill);
        }
    }
}
