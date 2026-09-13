using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The Relic Forge panel opened from the result screen: one card per relic, forge or equip on tap, then start a run.
    // RelicShop decides what a tap does; this view only shows the state and briefly ignores taps after each change.
    public sealed class RelicForgeView : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Button[] _cards;
        [SerializeField] private Text[] _nameLabels;
        [SerializeField] private Text[] _descriptionLabels;
        [SerializeField] private Text[] _stateLabels;
        [SerializeField] private Button _startButton;
        [SerializeField] private Text _startLabel;
        [SerializeField, Min(0f)] private float _inputDelay = 0.25f;
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void Start()
        {
            if (_setup == null || _setup.Relics == null || _text == null || _panel == null || _titleLabel == null ||
                _statusLabel == null || _startButton == null || _startLabel == null || !CardsAreValid())
            {
                Debug.LogError("RelicForgeView is missing a scene or content reference, or has fewer cards than relics.", this);
                enabled = false;
                return;
            }

            _panel.SetActive(false);
            _titleLabel.text = _text.ForgeTitle;
            _startLabel.text = _text.StartRunLabel;
            for (int i = 0; i < _cards.Length; i++)
            {
                int slot = i;
                _cards[i].onClick.AddListener(() => OnCard(slot));
            }
            _startButton.onClick.AddListener(OnStart);
            _setup.Profile.Changed += OnProfileChanged;
            _subscribed = true;
        }

        private bool CardsAreValid()
        {
            if (_cards == null || _nameLabels == null || _descriptionLabels == null || _stateLabels == null ||
                _cards.Length < _setup.Relics.Relics.Count || _nameLabels.Length != _cards.Length ||
                _descriptionLabels.Length != _cards.Length || _stateLabels.Length != _cards.Length)
                return false;

            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null || _nameLabels[i] == null || _descriptionLabels[i] == null || _stateLabels[i] == null)
                    return false;
            }
            return true;
        }

        public void Open()
        {
            if (!enabled || IsOpen)
                return;

            _panel.SetActive(true);
            Refresh();
        }

        private void OnProfileChanged()
        {
            if (IsOpen)
                Refresh();
        }

        private void Refresh()
        {
            RelicShop shop = _setup.Relics;
            _statusLabel.text = string.Format(_text.ForgeStatusFormat, shop.Profile.Gold, shop.Profile.DeepestFloorCleared);
            for (int i = 0; i < _cards.Length; i++)
            {
                bool used = i < shop.Relics.Count;
                _cards[i].gameObject.SetActive(used);
                if (!used)
                    continue;

                RelicOption relic = shop.Relics[i];
                _nameLabels[i].text = relic.DisplayName;
                _descriptionLabels[i].text = relic.Description;
                _stateLabels[i].text = DescribeState(shop, relic);
            }

            SetInteractable(false);
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            _awaitingInputDelay = true;
        }

        private string DescribeState(RelicShop shop, RelicOption relic)
        {
            switch (shop.StatusOf(relic))
            {
                case RelicStatus.Affordable:
                    return string.Format(_text.RelicForgeFormat, relic.Price);
                case RelicStatus.TooExpensive:
                    return string.Format(_text.RelicNeedGoldFormat, relic.Price, shop.GoldNeededFor(relic));
                case RelicStatus.Owned:
                    return _text.RelicOwnedLabel;
                default:
                    return _text.RelicEquippedLabel;
            }
        }

        private void Update()
        {
            if (!_awaitingInputDelay || Time.unscaledTime < _inputEnabledAt)
                return;

            _awaitingInputDelay = false;
            SetInteractable(true);
        }

        // Cards the player cannot afford are disabled so they read as locked; equipped cards stay lit and do nothing.
        private void SetInteractable(bool interactable)
        {
            RelicShop shop = _setup.Relics;
            for (int i = 0; i < _cards.Length && i < shop.Relics.Count; i++)
                _cards[i].interactable = interactable && shop.StatusOf(shop.Relics[i]) != RelicStatus.TooExpensive;
            _startButton.interactable = interactable;
        }

        private void OnCard(int slot)
        {
            RelicShop shop = _setup.Relics;
            if (!IsOpen || _awaitingInputDelay || slot >= shop.Relics.Count)
                return;

            // A successful forge or equip changes the profile, which refreshes the cards and restarts the input delay.
            shop.TrySelect(shop.Relics[slot]);
        }

        private void OnStart()
        {
            if (!IsOpen || _awaitingInputDelay || !_startButton.interactable)
                return;

            _startButton.interactable = false;
            _setup.RestartRun();
        }

        private void OnDestroy()
        {
            if (!_subscribed)
                return;

            _setup.Profile.Changed -= OnProfileChanged;
            if (_startButton != null)
                _startButton.onClick.RemoveListener(OnStart);
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null)
                    _cards[i].onClick.RemoveAllListeners();
            }
        }
    }
}
