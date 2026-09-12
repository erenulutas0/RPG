using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Presents the current run choice (an upgrade offer or a forge visit) as large touch cards. The services reject
    // stale or repeated selections; this view additionally disables the cards on the first tap and briefly after showing.
    public sealed class RunChoiceView : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private Text[] _nameLabels;
        [SerializeField] private Text[] _descriptionLabels;
        [SerializeField, Min(0f)] private float _inputDelay = 0.25f;
        private ChoicePrompt _shownPrompt;
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;

        public bool IsOpen => _shownPrompt != null;

        private void Start()
        {
            if (_setup == null || _setup.Choices == null || _text == null || _panel == null || _titleLabel == null ||
                !SlotsAreValid())
            {
                Debug.LogError("RunChoiceView is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                int slot = i;
                _buttons[i].onClick.AddListener(() => OnChoice(slot));
            }

            _setup.Choices.Changed += OnChoicesChanged;
            _subscribed = true;
            Show(_setup.Choices.Current);
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

        private void OnChoicesChanged() => Show(_setup.Choices.Current);

        private void Show(ChoicePrompt prompt)
        {
            _shownPrompt = prompt;
            _panel.SetActive(prompt != null);
            SetInteractable(false);
            _awaitingInputDelay = prompt != null;
            if (prompt == null)
                return;

            _titleLabel.text = prompt.Kind == ChoiceKind.Forge ? _text.ForgeChoiceTitle : _text.UpgradeChoiceTitle;
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool used = i < prompt.Cards.Count;
                _buttons[i].gameObject.SetActive(used);
                if (!used)
                    continue;

                _nameLabels[i].text = prompt.Cards[i].Name;
                _descriptionLabels[i].text = prompt.Cards[i].Description;
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
            if (_shownPrompt == null || _awaitingInputDelay)
                return;

            ChoicePrompt prompt = _shownPrompt;
            SetInteractable(false);
            // A successful selection raises Changed, which hides this prompt or shows the next one.
            if (!_setup.Choices.TrySelect(prompt, slot))
                Show(_setup.Choices.Current);
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

            _setup.Choices.Changed -= OnChoicesChanged;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] != null)
                    _buttons[i].onClick.RemoveAllListeners();
            }
        }
    }
}
