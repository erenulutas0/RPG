using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Hit and attack feedback for one combatant: a flash on damage, a nudge on attack, hiding the body on death. With a
    // generated look (an ILookSprites on the same object) the flash is a white silhouette overlay over the body, since
    // tinting a textured sprite white shows nothing; the old square placeholders keep the colour tint.
    public sealed class CombatantView : MonoBehaviour
    {
        private const string FlashName = "Flash";

        [SerializeField] private Health _health;
        [SerializeField] private AttackController _attack;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField, Min(0.01f)] private float _feedbackDuration = 0.14f;
        [SerializeField] private float _attackNudge = 0.2f;
        // Optional: bosses with an enrage behaviour keep a new resting color once enraged.
        [SerializeField] private EnrageBehaviour _enrage;
        [SerializeField] private Color _enragedColor = new Color(1f, 0.35f, 0.2f);
        // A critical hit flashes this color instead of white.
        [SerializeField] private Color _criticalColor = new Color(1f, 0.82f, 0.2f);
        private ILookSprites _look;
        private SpriteRenderer _flash;
        private Sprite _flashSource;
        private Color _baseColor;
        private Vector3 _basePosition;
        private float _feedbackRemaining;
        private bool _tinted;
        private bool _subscribed;

        // True from a hit until the feedback ends; FlashColor is the tint of that flash and white while idle.
        public bool IsFlashing { get; private set; }
        public Color FlashColor { get; private set; } = Color.white;

        private void OnEnable()
        {
            if (_health == null || _body == null)
            {
                Debug.LogError("CombatantView needs health and a body renderer.", this);
                enabled = false;
                return;
            }

            _look = GetComponent<ILookSprites>();
            // A look view owns the body colour and sets it white in its Awake, which may run after this OnEnable.
            _baseColor = _look != null ? Color.white : _body.color;
            _basePosition = _body.transform.localPosition;
            // One delegate per handler for the view's life: a pooled enemy is enabled and disabled once a life
            // (EncounterController's horde pool), and a method group makes a new delegate each time it is named.
            _health.Damaged += _onDamaged ??= OnDamaged;
            _health.Died += _onDied ??= OnDied;
            if (_attack != null)
                _attack.Attacked += _onAttacked ??= OnAttacked;
            if (_enrage != null)
                _enrage.Enraged += _onEnraged ??= OnEnraged;
            _subscribed = true;
        }

        private System.Action<DamageContext> _onDamaged;
        private System.Action _onDied;
        private System.Action _onAttacked;
        private System.Action _onEnraged;

        private void OnEnraged()
        {
            // A painted look with its own second phase (the Forge Warden's hotter frames) shows the enrage itself; the
            // tint would bury the cool steel that keeps it readable on the lava.
            if (_look is EnemyLookView enemy && enemy.DrawsEnrage)
                return;
            _baseColor = _enragedColor;
            if (!_tinted)
                _body.color = _enragedColor;
        }

        private void Start() => _body.enabled = _health.IsAlive;

        // A pooled body back on the floor (EncounterController's horde pool): the body its death hid stands again, as Start
        // shows a new one's; its flash and nudge already ended as it was disabled. Only while it is off the floor.
        internal void ResetForReuse()
        {
            if (_body == null)
                return;
            _body.gameObject.SetActive(true);
            _body.enabled = true;
        }

        private void OnDamaged(DamageContext context)
        {
            Color color = context.IsCritical ? _criticalColor : Color.white;
            Sprite silhouette = _look != null ? _look.SilhouetteOf(_body.sprite) : null;
            if (silhouette != null)
                ShowFlash(silhouette, color);
            else
            {
                _body.color = color;
                _tinted = true;
            }
            IsFlashing = true;
            FlashColor = color;
            _feedbackRemaining = _feedbackDuration;
        }

        private void OnAttacked()
        {
            _body.transform.localPosition = _basePosition + Vector3.up * _attackNudge;
            _feedbackRemaining = _feedbackDuration;
        }

        private void OnDied()
        {
            _body.enabled = false;
            // Hide child equipment as well as the body when its owner dies.
            _body.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_feedbackRemaining <= 0f)
                return;

            // The look may swap the body's frame mid-flash; keep the overlay the silhouette of what is shown.
            if (_flash != null && _flash.enabled && _body.sprite != _flashSource)
            {
                Sprite silhouette = _look.SilhouetteOf(_body.sprite);
                if (silhouette != null)
                {
                    _flash.sprite = silhouette;
                    _flashSource = _body.sprite;
                }
            }

            // Unscaled so a hit flash or nudge still settles while an upgrade choice pauses combat.
            _feedbackRemaining -= Time.unscaledDeltaTime;
            if (_feedbackRemaining <= 0f)
                ResetFeedback();
        }

        // The overlay lives under the body transform so the attack nudge carries it and the death hide covers it.
        private void ShowFlash(Sprite silhouette, Color color)
        {
            if (_flash == null)
                _flash = FindOrCreateFlash();
            _flash.sprite = silhouette;
            _flash.color = color;
            _flash.enabled = true;
            _flashSource = _body.sprite;
        }

        private SpriteRenderer FindOrCreateFlash()
        {
            Transform existing = _body.transform.Find(FlashName);
            GameObject flash = existing != null ? existing.gameObject : new GameObject(FlashName);
            if (existing == null)
                flash.transform.SetParent(_body.transform, false);
            // TryGetComponent: a new overlay has none yet, and a failed GetComponent allocates in the Editor - once for every
            // enemy, on its first hit.
            if (!flash.TryGetComponent(out SpriteRenderer renderer))
                renderer = flash.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = _body.sortingLayerID;
            // The imported hero's front knuckle overlay sits above its sword; flash must cover that hand too.
            renderer.sortingOrder = _body.sortingOrder + (_look is HeroLookView hero && hero.UsesPaintedArt ? 5 : 1);
            return renderer;
        }

        private void ResetFeedback()
        {
            if (_body == null)
                return;
            if (_tinted)
            {
                _body.color = _baseColor;
                _tinted = false;
            }
            if (_flash != null)
                _flash.enabled = false;
            _body.transform.localPosition = _basePosition;
            _feedbackRemaining = 0f;
            IsFlashing = false;
            FlashColor = Color.white;
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _health.Damaged -= _onDamaged;
                _health.Died -= _onDied;
                if (_attack != null)
                    _attack.Attacked -= _onAttacked;
                if (_enrage != null)
                    _enrage.Enraged -= _onEnraged;
                _subscribed = false;
            }
            ResetFeedback();
        }
    }
}
