using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The round ability button at the bottom right: a tap fires the burst; a radial fill drains as the cooldown runs. It
    // only answers while the run is live, no choice is open and the run is not paused.
    public sealed class AbilityButtonView : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private AbilityController _controller;
        [SerializeField] private Button _button;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private Image _icon;
        [SerializeField] private Color _readyColor = Color.white;
        [SerializeField] private Color _coolingColor = new Color(1f, 1f, 1f, 0.35f);
        private Sprite _glyph;
        private bool _subscribed;

        public bool IsUsable => _setup != null && _controller != null && _controller.Ability != null && _controller.Ability.IsReady &&
                                !_setup.Run.HasEnded && !_setup.Choices.IsOpen && !_setup.Pause.IsPlayerPaused;

        private void Start()
        {
            if (_setup == null || _setup.Run == null || _controller == null || _controller.Ability == null || _button == null ||
                _cooldownFill == null || _icon == null)
            {
                Debug.LogError("AbilityButtonView is missing a scene reference or the ability.", this);
                enabled = false;
                return;
            }

            _glyph = PixelSpriteFactory.CreateSprite(AbilityArt.DrawGlyph(), "Ability Glyph", PixelSpriteFactory.Centre);
            _icon.sprite = _glyph;
            _button.onClick.AddListener(OnTap);
            _subscribed = true;
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            AbilityRuntime ability = _controller.Ability;
            _cooldownFill.fillAmount = ability.Cooldown > 0f ? ability.Remaining / ability.Cooldown : 0f;
            bool usable = IsUsable;
            _button.interactable = usable;
            _icon.color = usable ? _readyColor : _coolingColor;
        }

        private void OnTap()
        {
            if (IsUsable)
                _controller.TryUse();
        }

        private void OnDestroy()
        {
            if (_subscribed && _button != null)
                _button.onClick.RemoveListener(OnTap);
            PixelSpriteFactory.Destroy(_glyph);
        }
    }
}
