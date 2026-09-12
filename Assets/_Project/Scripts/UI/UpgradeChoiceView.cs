using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Presents the current upgrade offer as large touch cards. The service rejects stale or repeated
    // selections; this view additionally disables the cards on the first tap and briefly after showing.
    public sealed class UpgradeChoiceView : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private Text[] _nameLabels;
        [SerializeField] private Text[] _descriptionLabels;
        [SerializeField, Min(0f)] private float _inputDelay = 0.25f;
        private UpgradeOffer _shownOffer;
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;

        public bool IsOpen => _shownOffer != null;

        private void Start()
        {
            if (_setup == null || _setup.Upgrades == null || _text == null || _panel == null || _titleLabel == null ||
                !SlotsAreValid())
            {
                Debug.LogError("UpgradeChoiceView is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            _titleLabel.text = _text.UpgradeChoiceTitle;
            for (int i = 0; i < _buttons.Length; i++)
            {
                int slot = i;
                _buttons[i].onClick.AddListener(() => OnChoice(slot));
            }

            _setup.Upgrades.OfferChanged += OnOfferChanged;
            _subscribed = true;
            Show(_setup.Upgrades.CurrentOffer);
        }

        private bool SlotsAreValid()
        {
            if (_buttons == null || _nameLabels == null || _descriptionLabels == null || _buttons.Length == 0 ||
                _nameLabels.Length != _buttons.Length || _descriptionLabels.Length != _buttons.Length)
                return false;

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] == null || _nameLabels[i] == null || _descriptionLabels[i] == null)
                    return false;
            }
            return true;
        }

        private void OnOfferChanged() => Show(_setup.Upgrades.CurrentOffer);

        private void Show(UpgradeOffer offer)
        {
            _shownOffer = offer;
            _panel.SetActive(offer != null);
            SetInteractable(false);
            _awaitingInputDelay = offer != null;
            if (offer == null)
                return;

            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool used = i < offer.Choices.Count;
                _buttons[i].gameObject.SetActive(used);
                if (!used)
                    continue;

                UpgradeOption choice = offer.Choices[i];
                _nameLabels[i].text = choice.DisplayName;
                _descriptionLabels[i].text = string.Format(choice.DescriptionFormat, choice.DescriptionValue);
            }
        }

        private void Update()
        {
            if (!_awaitingInputDelay || Time.unscaledTime < _inputEnabledAt)
                return;

            _awaitingInputDelay = false;
            SetInteractable(true);
        }

        private void OnChoice(int slot)
        {
            if (_shownOffer == null || _awaitingInputDelay)
                return;

            UpgradeOffer offer = _shownOffer;
            SetInteractable(false);
            // A successful selection raises OfferChanged, which hides this offer or shows the next one.
            if (!_setup.Upgrades.TrySelect(offer, slot))
                Show(_setup.Upgrades.CurrentOffer);
        }

        private void SetInteractable(bool interactable)
        {
            for (int i = 0; i < _buttons.Length; i++)
                _buttons[i].interactable = interactable;
        }

        private void OnDestroy()
        {
            if (!_subscribed)
                return;

            _setup.Upgrades.OfferChanged -= OnOfferChanged;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] != null)
                    _buttons[i].onClick.RemoveAllListeners();
            }
        }
    }
}
