using Cryptforge.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Shows what the hero has left to run on. The budget itself lives in HeroStamina and is spent by HeroMovementInput;
    // this draws on the bar's change event, with a brief scaled-time update only for depletion feedback.
    //
    // A fill alone cannot say why a hero is slow, because an empty bar has no fill left to look at. So the track behind
    // it carries that: dark and cool while there is anything to spend, dull ember once there is not.
    public sealed class HeroStaminaView : MonoBehaviour
    {
        [SerializeField] private HeroMovementInput _movement;
        [SerializeField] private Image _track;
        [SerializeField] private Image _fill;
        [SerializeField] private Color _readyTrack = new Color(0.18f, 0.22f, 0.29f, 1f);
        [SerializeField] private Color _spentTrack = new Color(0.36f, 0.17f, 0.13f, 1f);
        private HeroStamina _stamina;
        private Color _fillColor;
        private bool _initialized;
        private bool _wasEmpty;
        private float _pulseRemaining;
        private const float PulseDuration = .24f;
        private static readonly Color SpentPulse = new Color(.72f, .37f, .19f, 1f);

        // For tests and for anything that wants to read what the player is being shown.
        public Image Fill => _fill;
        public Image Track => _track;
        public bool IsPulsing => _pulseRemaining > 0f;

        // HeroMovementInput builds the bar in Awake, so Start is the earliest safe binding point, as it is for RunHud.
        private void Start()
        {
            if (_movement == null || _track == null || _fill == null || _movement.Stamina == null)
            {
                Debug.LogError("HeroStaminaView needs the hero's movement and both bar images.", this);
                enabled = false;
                return;
            }

            _stamina = _movement.Stamina;
            _fillColor = _fill.color;
            _stamina.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_stamina != null)
                _stamina.Changed -= Refresh;
        }

        private void Refresh()
        {
            _fill.fillAmount = _stamina.Fraction;
            bool empty = _stamina.IsEmpty;
            if (_initialized && empty && !_wasEmpty) _pulseRemaining = PulseDuration;
            if (!empty) _pulseRemaining = 0f;
            _wasEmpty = empty;
            _initialized = true;
            // Capacity remains exact; only visual emphasis changes when the budget is full.
            Color fill = _fillColor;
            fill.a *= _stamina.Fraction >= 1f ? .60f : 1f;
            _fill.color = fill;
            PaintTrack();
        }

        private void Update()
        {
            if (_pulseRemaining <= 0f || Time.deltaTime <= 0f) return;
            _pulseRemaining = Mathf.Max(0f, _pulseRemaining - Time.deltaTime);
            PaintTrack();
        }

        private void PaintTrack()
        {
            if (_stamina == null) return;
            _track.color = _stamina.IsEmpty
                ? Color.Lerp(_spentTrack, SpentPulse, _pulseRemaining / PulseDuration)
                : _readyTrack;
        }
    }
}
