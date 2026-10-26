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
        [System.Serializable]
        private struct UpgradeIcon
        {
            public UpgradeDefinition Definition;
            public Sprite Sprite;
        }

        [SerializeField] private CombatSetup _setup;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private Text[] _nameLabels;
        [SerializeField] private Text[] _descriptionLabels;
        [SerializeField] private Image[] _cardIcons;
        [SerializeField] private UpgradeIcon[] _upgradeIcons;
        // Optional, one per slot like the icons: a strip or frame the card's rarity colour is painted on. A slot without
        // one still shows its tier, on the card's name.
        [SerializeField] private Graphic[] _rarityAccents;
        [SerializeField, Min(0f)] private float _inputDelay = 0.25f;

        // The owner's four colours, green to gold, indexed by UpgradeRarity (docs/27 decision 12, docs/40 section 7.1).
        // Saturated and far apart in hue, so the tier reads at a glance on a phone; gold is a warm yellow rather than the
        // orange's neighbour, which is the pair a player most needs to tell apart.
        private static readonly Color32[] RarityColours =
        {
            new Color32(0x4C, 0xC3, 0x6A, 0xFF), // green
            new Color32(0xF0, 0x8A, 0x24, 0xFF), // orange
            new Color32(0xE0, 0x39, 0x3E, 0xFF), // red
            new Color32(0xF5, 0xC5, 0x42, 0xFF)  // gold
        };

        private Color[] _authoredNameColours;
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

            _authoredNameColours = new Color[_nameLabels.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                int slot = i;
                _buttons[i].onClick.AddListener(() => OnChoice(slot));
                _authoredNameColours[i] = _nameLabels[i].color;
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

            _titleLabel.text = prompt.Kind switch
            {
                ChoiceKind.Forge => _text.ForgeChoiceTitle,
                ChoiceKind.Checkpoint => _text.CheckpointChoiceTitle,
                _ => _text.UpgradeChoiceTitle
            };
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool used = i < prompt.Cards.Count;
                _buttons[i].gameObject.SetActive(used);
                if (!used)
                    continue;

                _nameLabels[i].text = prompt.Cards[i].Name;
                _descriptionLabels[i].text = prompt.Cards[i].Description;
                if (_cardIcons != null && i < _cardIcons.Length && _cardIcons[i] != null)
                {
                    Sprite icon = IconFor(prompt, i);
                    _cardIcons[i].sprite = icon;
                    _cardIcons[i].enabled = icon != null;
                }
                ShowRarity(i, prompt.Kind == ChoiceKind.Upgrade, prompt.Cards[i].Rarity);
            }
        }

        // An upgrade card wears its tier's colour on its name and on its accent, if it has one. A forge or checkpoint
        // card has no tier - its Rarity is Common only because that is the default - so it keeps the authored colour
        // and hides the accent rather than claiming to be green.
        private void ShowRarity(int slot, bool tiered, UpgradeRarity rarity)
        {
            int tier = (int)rarity;
            bool known = tiered && tier >= 0 && tier < RarityColours.Length;
            _nameLabels[slot].color = known ? (Color)RarityColours[tier] : _authoredNameColours[slot];
            if (_rarityAccents == null || slot >= _rarityAccents.Length || _rarityAccents[slot] == null)
                return;

            _rarityAccents[slot].enabled = known;
            if (known)
                _rarityAccents[slot].color = RarityColours[tier];
        }

        private Sprite IconFor(ChoicePrompt prompt, int slot)
        {
            // Resolve by content identity, not slot order or translated display names. Forge/checkpoint and future
            // upgrades without artwork keep their text cards instead of inheriting an unrelated weapon icon.
            UpgradeOffer offer = _setup.Upgrades.CurrentOffer;
            if (prompt.Kind != ChoiceKind.Upgrade || offer == null || slot >= offer.Choices.Count || _upgradeIcons == null)
                return null;
            foreach (UpgradeIcon icon in _upgradeIcons)
                if (icon.Definition != null && icon.Definition.Id == offer.Choices[slot].Id)
                    return icon.Sprite;
            return null;
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
