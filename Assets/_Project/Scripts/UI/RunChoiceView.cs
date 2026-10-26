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

        // The kind line's colours: what a pick adds to the build in a warm gold, a level in pale steel, an evolution in the
        // ember the danger readout wears, since an evolution is also what makes the run harder.
        private static readonly Color NewKindColour = new Color(1f, .86f, .45f, 1f);
        private static readonly Color LevelKindColour = new Color(.78f, .84f, .92f, 1f);
        private static readonly Color EvolutionKindColour = new Color(1f, .55f, .22f, 1f);

        private Color[] _authoredNameColours;
        // One per card, built in code in category mode only (docs/42 section 4); null on the legacy cards, which stay as
        // they were.
        private Text[] _kindLabels;
        private ChoicePrompt _shownPrompt;
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;

        public bool IsOpen => _shownPrompt != null;
        // For tests: how many cards the panel can show, and the kind line a card shows (empty when it shows none).
        public int CardSlots => _buttons != null ? _buttons.Length : 0;

        public string CardKindText(int slot)
        {
            if (_kindLabels == null || slot < 0 || slot >= _kindLabels.Length || !_kindLabels[slot].gameObject.activeSelf)
                return string.Empty;
            return _kindLabels[slot].text;
        }

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
            if (_setup.Upgrades != null && _setup.Upgrades.IsCategoryMode)
                BuildKindLabels();

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
                ShowKind(i, prompt.Kind == ChoiceKind.Upgrade);
            }
        }

        // A small line in each card's top-right corner. On the 896x252 card it spans x 488-868 and y 8-36 from the card's
        // top: above the name label (from 36) and the description (from 116), right of the icon (to 194), and inside
        // the card's right edge, so it touches none of the authored slots. Not a raycast target, so a tap on it still
        // reaches the card's button.
        private void BuildKindLabels()
        {
            _kindLabels = new Text[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                var labelObject = new GameObject("Card Kind", typeof(RectTransform));
                labelObject.layer = _buttons[i].gameObject.layer;
                var rect = (RectTransform)labelObject.transform;
                rect.SetParent(_buttons[i].transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-28f, -8f);
                rect.sizeDelta = new Vector2(380f, 28f);
                Text label = labelObject.AddComponent<Text>();
                label.font = _nameLabels[i].font;
                label.fontSize = 24;
                label.fontStyle = FontStyle.Bold;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 18;
                label.resizeTextMaxSize = 24;
                label.alignment = TextAnchor.MiddleRight;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.raycastTarget = false;
                labelObject.SetActive(false);
                _kindLabels[i] = label;
            }
        }

        // What kind of pick a category-mode card is: a weapon or tome the hero does not have yet, the next level of one it
        // has (its level read through StacksOf, which in category mode is the carried weapon's or the held tome's), or an
        // evolution, which outranks its category because it is the card that also raises the danger. A general card says
        // nothing: its name and effect are the whole of it, as on the legacy cards.
        private void ShowKind(int slot, bool upgrade)
        {
            if (_kindLabels == null)
                return;

            Text label = _kindLabels[slot];
            UpgradeOffer offer = _setup.Upgrades.CurrentOffer;
            if (!upgrade || offer == null || slot >= offer.Choices.Count)
            {
                label.gameObject.SetActive(false);
                return;
            }

            UpgradeOption option = offer.Choices[slot];
            string kind = null;
            Color colour = LevelKindColour;
            if (option.IsEvolution)
            {
                kind = _text.CardEvolution;
                colour = EvolutionKindColour;
            }
            else
            {
                switch (option.Category)
                {
                    case OfferCategory.NewWeapon:
                        kind = _text.CardNewWeapon;
                        colour = NewKindColour;
                        break;
                    case OfferCategory.NewTome:
                        kind = _text.CardNewTome;
                        colour = NewKindColour;
                        break;
                    case OfferCategory.WeaponUpgrade:
                    case OfferCategory.TomeUpgrade:
                        int level = _setup.Upgrades.StacksOf(option);
                        kind = string.Format(_text.CardLevelUpFormat, level, level + 1);
                        break;
                }
            }

            label.gameObject.SetActive(!string.IsNullOrEmpty(kind));
            if (string.IsNullOrEmpty(kind))
                return;
            label.text = kind;
            label.color = colour;
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
