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

        // Where a card and the parts that move when it is shortened sit, as RectTransform anchored positions and sizes.
        private struct CardLayout
        {
            public Vector2 Position;
            public Vector2 Size;
            public Vector2 NamePosition;
            public Vector2 NameSize;
            public Vector2 DescriptionPosition;
            public Vector2 DescriptionSize;
            public Vector2 IconSize;
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

        // A prompt with more cards than the scene has buttons - a category offer draws three (docs/40 9.2), the scene
        // authors two - clones the last card, up to this many. The legacy hall's offers and the forge and checkpoint
        // prompts never ask for more than the scene has, so they keep its two cards exactly as authored.
        private const int MaxCards = 3;
        // Three cards stacked in the 992x860 Choice Surface: the first keeps its authored top edge (y 227 from the
        // surface's centre, 32 below the title) and the last ends 31 above the surface's bottom edge (y -399, clear of
        // its ~18-unit frame), 28 apart - three 190-unit cards where the scene's two are 252 with 40 between.
        private const float StackedGap = 28f;
        private const float StackedBottomMargin = 31f;

        private Color[] _authoredNameColours;
        // The scene's own card layout, one per slot (a clone's copies the card it was cloned from, a stride lower), and
        // how many cards the scene authored; up to that many are shown exactly where the scene put them.
        private CardLayout[] _authoredLayouts;
        private int _authoredSlots;
        private int _laidOutFor = -1;
        private bool _canRestack;
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
        public Button CardButton(int slot) => _buttons[slot];

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
            _authoredLayouts = new CardLayout[_buttons.Length];
            _authoredSlots = _buttons.Length;
            _canRestack = CanRestack();
            for (int i = 0; i < _buttons.Length; i++)
            {
                int slot = i;
                _buttons[i].onClick.AddListener(() => OnChoice(slot));
                _authoredNameColours[i] = _nameLabels[i].color;
                _authoredLayouts[i] = ReadLayout(i);
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

        // A card can be cloned and the cards restacked only as the scene authors them: point-anchored cards on one
        // surface, each with its name and description inside it, anchored to its top with a top pivot. Otherwise the
        // panel keeps the scene's cards, and a prompt with more shows as many as there are, as it always did.
        private bool CanRestack()
        {
            if (!(_buttons[0].transform.parent is RectTransform))
                return false;
            for (int i = 0; i < _buttons.Length; i++)
            {
                var card = (RectTransform)_buttons[i].transform;
                if (card.parent != _buttons[0].transform.parent || card.anchorMin != card.anchorMax ||
                    !TopAnchoredIn(_nameLabels[i].rectTransform, card) ||
                    !TopAnchoredIn(_descriptionLabels[i].rectTransform, card))
                    return false;
            }
            return true;
        }

        private static bool TopAnchoredIn(RectTransform part, RectTransform card) =>
            part.parent == card && part.anchorMin.y == 1f && part.anchorMax.y == 1f && part.pivot.y == 1f;

        private Image IconAt(int slot) => _cardIcons != null && slot < _cardIcons.Length ? _cardIcons[slot] : null;

        private CardLayout ReadLayout(int slot)
        {
            var card = (RectTransform)_buttons[slot].transform;
            RectTransform name = _nameLabels[slot].rectTransform;
            RectTransform description = _descriptionLabels[slot].rectTransform;
            Image icon = IconAt(slot);
            return new CardLayout
            {
                Position = card.anchoredPosition,
                Size = card.sizeDelta,
                NamePosition = name.anchoredPosition,
                NameSize = name.sizeDelta,
                DescriptionPosition = description.anchoredPosition,
                DescriptionSize = description.sizeDelta,
                IconSize = icon != null ? icon.rectTransform.sizeDelta : Vector2.zero
            };
        }

        // A copy of the last card under the same surface, registered in every per-slot array the way the scene's cards
        // are: its own name, description, icon, rarity accent and kind line (found in the copy where the original's
        // were), the original's authored name colour, and a click that selects its slot. It starts disabled - it is made
        // while Show holds the cards for the input delay - and is enabled with the rest when the delay ends. Made once,
        // the first time a prompt needs it, then shown or hidden like the others.
        private void AddSlot()
        {
            int source = _buttons.Length - 1;
            int slot = _buttons.Length;
            Button original = _buttons[source];
            Transform from = original.transform;
            Button clone = Instantiate(original, from.parent, false);
            clone.name = "Choice Button " + (slot + 1);
            clone.transform.SetSiblingIndex(from.GetSiblingIndex() + 1);
            clone.interactable = false;
            Transform to = clone.transform;

            Append(ref _buttons, clone);
            Append(ref _nameLabels, Counterpart(_nameLabels[source], from, to));
            Append(ref _descriptionLabels, Counterpart(_descriptionLabels[source], from, to));
            if (_cardIcons != null && _cardIcons.Length == slot)
                Append(ref _cardIcons, Counterpart(_cardIcons[source], from, to));
            if (_rarityAccents != null && _rarityAccents.Length == slot)
                Append(ref _rarityAccents, Counterpart(_rarityAccents[source], from, to));
            if (_kindLabels != null)
                Append(ref _kindLabels, Counterpart(_kindLabels[source], from, to));
            Append(ref _authoredNameColours, _authoredNameColours[source]);

            // Hidden whenever the scene's layout is in use; a stride below the card it copies all the same.
            CardLayout layout = _authoredLayouts[source];
            layout.Position.y -= source > 0 ? _authoredLayouts[source - 1].Position.y - layout.Position.y : layout.Size.y;
            Append(ref _authoredLayouts, layout);

            clone.onClick.AddListener(() => OnChoice(slot));
            _laidOutFor = -1;
        }

        private static void Append<T>(ref T[] array, T value)
        {
            System.Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = value;
        }

        // The part of a cloned card that stands where the original's part stood, found by the same child indices.
        private static T Counterpart<T>(T part, Transform original, Transform clone) where T : Component
        {
            if (part == null || !part.transform.IsChildOf(original))
                return null;
            Transform found = Mirror(part.transform, original, clone);
            return found != null ? found.GetComponent(part.GetType()) as T : null;
        }

        private static Transform Mirror(Transform part, Transform original, Transform clone)
        {
            if (part == original)
                return clone;
            Transform parent = Mirror(part.parent, original, clone);
            int index = part.GetSiblingIndex();
            return parent != null && index < parent.childCount ? parent.GetChild(index) : null;
        }

        // Up to the scene's count of cards, every card sits exactly where the scene put it. More are stacked from the
        // first card's top edge to StackedBottomMargin above the surface's bottom, StackedGap apart, each shortened
        // alike (never lengthened): the top band the kind line uses keeps its height, the name and description share
        // what is left in their authored proportions with the bottom padding shrunk like the card, and the icon shrinks
        // with the card so it keeps its margins. The labels' best-fit and minimum sizes are left as authored. Only on a
        // change of count, so a prompt like the last one touches no layout.
        private void LayOut(int shown)
        {
            if (shown == _laidOutFor)
                return;
            _laidOutFor = shown;

            if (shown <= _authoredSlots || !_canRestack)
            {
                for (int i = 0; i < _buttons.Length; i++)
                    Apply(i, _authoredLayouts[i]);
                return;
            }

            var surface = (RectTransform)_buttons[0].transform.parent;
            Rect bounds = surface.rect;
            float top = TopOf((RectTransform)_buttons[0].transform, _authoredLayouts[0], bounds);
            float bottom = bounds.yMin + StackedBottomMargin;
            float height = Mathf.Min(_authoredLayouts[0].Size.y, (top - bottom - StackedGap * (shown - 1)) / shown);
            for (int i = 0; i < _buttons.Length; i++)
            {
                CardLayout authored = _authoredLayouts[i];
                if (i >= shown)
                {
                    Apply(i, authored);
                    continue;
                }

                var card = (RectTransform)_buttons[i].transform;
                float cardTop = top - i * (height + StackedGap);
                float anchorY = bounds.yMin + card.anchorMin.y * bounds.height;
                CardLayout stacked = authored;
                stacked.Size.y = height;
                stacked.Position.y = cardTop - anchorY - (1f - card.pivot.y) * height;

                float scale = height / authored.Size.y;
                float nameTop = -authored.NamePosition.y;
                float descriptionTop = -authored.DescriptionPosition.y;
                float header = Mathf.Min(nameTop, descriptionTop);
                float blockBottom = Mathf.Max(nameTop + authored.NameSize.y, descriptionTop + authored.DescriptionSize.y);
                float footer = (authored.Size.y - blockBottom) * scale;
                float block = (height - header - footer) / (blockBottom - header);
                stacked.NamePosition.y = -(header + (nameTop - header) * block);
                stacked.NameSize.y = authored.NameSize.y * block;
                stacked.DescriptionPosition.y = -(header + (descriptionTop - header) * block);
                stacked.DescriptionSize.y = authored.DescriptionSize.y * block;
                stacked.IconSize = authored.IconSize * scale;
                Apply(i, stacked);
            }
        }

        // A card's top edge in its surface's rect, from its authored layout.
        private static float TopOf(RectTransform card, CardLayout layout, Rect bounds)
        {
            float anchorY = bounds.yMin + card.anchorMin.y * bounds.height;
            return anchorY + layout.Position.y + (1f - card.pivot.y) * layout.Size.y;
        }

        private void Apply(int slot, CardLayout layout)
        {
            var card = (RectTransform)_buttons[slot].transform;
            card.anchoredPosition = layout.Position;
            card.sizeDelta = layout.Size;
            RectTransform name = _nameLabels[slot].rectTransform;
            name.anchoredPosition = layout.NamePosition;
            name.sizeDelta = layout.NameSize;
            RectTransform description = _descriptionLabels[slot].rectTransform;
            description.anchoredPosition = layout.DescriptionPosition;
            description.sizeDelta = layout.DescriptionSize;
            Image icon = IconAt(slot);
            if (icon != null)
                icon.rectTransform.sizeDelta = layout.IconSize;
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
            int wanted = Mathf.Min(prompt.Cards.Count, MaxCards);
            while (_canRestack && _buttons.Length < wanted)
                AddSlot();
            LayOut(Mathf.Min(prompt.Cards.Count, _buttons.Length));
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
        // the card's right edge, so it touches none of the authored slots. A shortened card keeps that top band (see
        // LayOut), so it touches none there either. Not a raycast target, so a tap on it still reaches the card's button.
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
