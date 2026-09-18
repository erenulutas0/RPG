using Cryptforge.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Shows what the hero has left to run on. The budget itself lives in HeroStamina and is spent by HeroMovementInput;
    // this only draws it, on the bar's own change event rather than every frame, the way the rest of the HUD works.
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

        // For tests and for anything that wants to read what the player is being shown.
        public Image Fill => _fill;
        public Image Track => _track;

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
            _track.color = _stamina.IsEmpty ? _spentTrack : _readyTrack;
        }
    }
}
