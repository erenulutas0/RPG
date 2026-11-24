using System;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The Relic Forge panel opened from the result screen: the next run's hero's own weapon, shown and never sold; a card
    // per relic, forge or equip on tap; then start a run. The relic shop decides what a tap does; this view only shows the
    // state and briefly ignores taps after each change.
    //
    // The weapon rack that sold the Staff and the Daggers is gone (docs/63, 2026-11-23): every hero starts with its own
    // weapon (Ilya its Staff since 2026-10-29, the Vanguard its Sword since 2026-11-22), so the weapons' place shows that
    // weapon alone - its name, what it does and "Signature weapon" - following the hero chooser, and a tap on it does
    // nothing.
    //
    // The band the rack left above the start button holds the hero's skills (docs/63 section 12 answer 7, 2026-11-25): a
    // header and one card in the relics' own look, the way into the Forge's Skills tab (docs/57 section 14.5). The header
    // counts the skills the next run's hero carries ("Skills: 2 of 3 ready"); the card shows its next skill - the first of
    // skills 2 and 3 not yet forged, or its last once all are - with its picture, name, what it does and its state in the
    // Skills tab's own words, its frame by the state as a relic's (the worn frame once forged, the call's while the Forge
    // can forge it, the grey one out of reach). A tap opens the tab, where a tap forges. The two are copies of the relics' header and last card made in
    // Awake, before any painter has dressed the scene's (ScreenArtView paints them as it paints those), placed in the
    // relics' own rhythm under the last relic card; the header's row keeps its words alone.
    public sealed class RelicForgeView : MonoBehaviour
    {
        public const string SkillsHeaderName = "Skills Header";
        public const string SkillsPictureName = "Skill Artwork";

        [Serializable]
        private struct ForgeCard
        {
            public Button Button;
            public Text Name;
            public Text Description;
            public Text State;
            public Image Icon;
            public Image StateAccent;

            public bool IsComplete => Button != null && Name != null && Description != null && State != null;
        }

        [SerializeField] private CombatSetup _setup;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Text _weaponsHeaderLabel;
        [SerializeField] private Text _relicsHeaderLabel;
        // The hero's own weapon's card, the first; any others are left hidden (the scene holds one since the rack went).
        [SerializeField] private ForgeCard[] _weaponCards;
        [SerializeField] private ForgeCard[] _relicCards;
        [SerializeField] private Button _startButton;
        [SerializeField] private Text _startLabel;
        [SerializeField] private Sprite _secondWindIcon;
        [SerializeField] private Sprite _counterweightIcon;
        [SerializeField, Min(0f)] private float _inputDelay = 0.25f;
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;
        private HeroChoiceView _heroChoice;
        // The band's header and card (BuildSkillsRow); null, and an incomplete card, when the scene lacks the relics' parts.
        private Text _skillsHeader;
        private ForgeCard _skillsCard;
        private int _skillsSlot;

        public bool IsOpen => _panel != null && _panel.activeSelf;
        // The skills' header and card in the band above the start button, the card the Skills tab's entry; for the painter
        // and tests.
        public Text SkillsHeader => _skillsHeader;
        public Button SkillsCard => _skillsCard.Button;
        public Image SkillsPicture => _skillsCard.Icon;
        public Image SkillsStateAccent => _skillsCard.StateAccent;
        // The skill slot (1-3) the card shows, as last refreshed; 0 before. Its words: "name | description | state".
        public int SkillsCardSlot => _skillsSlot;
        public string SkillsCardText => _skillsCard.IsComplete
            ? _skillsCard.Name.text + " | " + _skillsCard.Description.text + " | " + _skillsCard.State.text
            : string.Empty;
        // The hero chooser beside the start button; null while the roster has one hero. For tests.
        public HeroChoiceView HeroChoice => _heroChoice;
        // The Anvil, Efsun and Bag tabs (docs/54 phase 4), opened from the buttons beside the nameplate.
        public AnvilView Anvil => _anvil;
        private AnvilView _anvil;
        // Each card shown, with the state it shows, as the panel refreshes: the painted screens dress the card by it.
        public event Action<Button, UnlockStatus> CardShown;

        // Before any Start: the painters dress the scene's cards in theirs, and these copies must be made from the plain ones.
        private void Awake() => BuildSkillsRow();

        // The skills' header and card under the last relic card, in the relics' rhythm: the gap between two relic cards, a
        // header as tall as theirs, the gap from their header to their first card, a card as tall as theirs. Nothing is
        // made when the scene lacks the parts or the band cannot hold them clear of the start button.
        private void BuildSkillsRow()
        {
            if (_relicsHeaderLabel == null || _relicCards == null || _relicCards.Length == 0 || _startButton == null)
                return;
            ForgeCard first = _relicCards[0];
            ForgeCard last = _relicCards[_relicCards.Length - 1];
            if (!first.IsComplete || !last.IsComplete)
                return;
            RectTransform header = _relicsHeaderLabel.rectTransform;
            var firstRect = (RectTransform)first.Button.transform;
            var lastRect = (RectTransform)last.Button.transform;
            var startRect = (RectTransform)_startButton.transform;
            float cardHeight = lastRect.anchorMax.y - lastRect.anchorMin.y;
            float cardGap = _relicCards.Length > 1
                ? ((RectTransform)_relicCards[_relicCards.Length - 2].Button.transform).anchorMin.y - lastRect.anchorMax.y
                : header.anchorMin.y - firstRect.anchorMax.y;
            float headerHeight = header.anchorMax.y - header.anchorMin.y;
            float headerGap = header.anchorMin.y - firstRect.anchorMax.y;
            float headerTop = lastRect.anchorMin.y - cardGap;
            float cardTop = headerTop - headerHeight - headerGap;
            if (cardHeight <= 0f || cardTop - cardHeight < startRect.anchorMax.y + cardGap)
                return;

            GameObject headerCopy = Instantiate(_relicsHeaderLabel.gameObject, header.parent, false);
            headerCopy.name = SkillsHeaderName;
            _skillsHeader = headerCopy.GetComponent<Text>();
            Band(_skillsHeader.rectTransform, header, headerTop - headerHeight, headerTop);
            _skillsHeader.transform.SetSiblingIndex(lastRect.GetSiblingIndex() + 1);

            GameObject cardCopy = Instantiate(last.Button.gameObject, lastRect.parent, false);
            cardCopy.name = AnvilView.SkillsEntryName;
            Transform copy = cardCopy.transform;
            _skillsCard = new ForgeCard
            {
                Button = cardCopy.GetComponent<Button>(),
                Name = Twin(last.Name, lastRect, copy),
                Description = Twin(last.Description, lastRect, copy),
                State = Twin(last.State, lastRect, copy),
                Icon = Twin(last.Icon, lastRect, copy),
                StateAccent = Twin(last.StateAccent, lastRect, copy)
            };
            if (_skillsCard.Icon != null)
                _skillsCard.Icon.name = SkillsPictureName;
            Band((RectTransform)copy, lastRect, cardTop - cardHeight, cardTop);
            copy.SetSiblingIndex(_skillsHeader.transform.GetSiblingIndex() + 1);
        }

        // A copy laid between two heights of the safe area, keeping the source's sides.
        private static void Band(RectTransform rect, RectTransform source, float bottom, float top)
        {
            rect.anchorMin = new Vector2(source.anchorMin.x, bottom);
            rect.anchorMax = new Vector2(source.anchorMax.x, top);
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            rect.anchoredPosition = source.anchoredPosition;
        }

        // The part of a copy that stands where part stands in the original.
        private static T Twin<T>(T part, Transform original, Transform copy) where T : Component
        {
            if (part == null)
                return null;
            var path = new System.Collections.Generic.List<int>();
            for (Transform at = part.transform; at != original; at = at.parent)
            {
                if (at == null)
                    return null;
                path.Add(at.GetSiblingIndex());
            }
            Transform found = copy;
            for (int i = path.Count - 1; i >= 0; i--)
                found = found.GetChild(path[i]);
            return found.GetComponent<T>();
        }

        private void Start()
        {
            if (_setup == null || _setup.Relics == null || _text == null || _panel == null ||
                _titleLabel == null || _statusLabel == null || _weaponsHeaderLabel == null || _relicsHeaderLabel == null ||
                _startButton == null || _startLabel == null || !CardsAreValid(_weaponCards, 1) ||
                !CardsAreValid(_relicCards, _setup.Relics.Relics.Count))
            {
                Debug.LogError("RelicForgeView is missing a scene or content reference, or has fewer cards than items.", this);
                enabled = false;
                return;
            }

            _panel.SetActive(false);
            // The panel's fixed words follow the language (docs/60); its cards are written again by Refresh.
            LocalisedLabel.Bind(_titleLabel, () => _text.ForgeTitle);
            LocalisedLabel.Bind(_weaponsHeaderLabel, () => _text.ForgeWeaponsHeader);
            LocalisedLabel.Bind(_relicsHeaderLabel, () => _text.ForgeRelicsHeader);
            LocalisedLabel.Bind(_startLabel, () => _text.StartRunLabel);
            for (int i = 0; i < _relicCards.Length; i++)
            {
                int slot = i;
                _relicCards[i].Button.onClick.AddListener(() => OnRelicCard(slot));
            }
            _startButton.onClick.AddListener(OnStart);
            if (_setup.Heroes.Count > 1)
            {
                _heroChoice = gameObject.AddComponent<HeroChoiceView>();
                _heroChoice.Initialize(_setup, _text, _startButton, _startLabel);
            }
            // docs/54 phase 4: the Anvil's and the Bag's buttons beside the nameplate, and the panel they open; docs/57 phase 4
            // and docs/63 section 12 answer 7: the Skills tab's entry, the band's card.
            _anvil = gameObject.AddComponent<AnvilView>();
            _anvil.Initialize(_setup, _titleLabel, _startLabel, _skillsCard.Button);
            _setup.Profile.Changed += OnProfileChanged;
            Loc.Changed += OnLanguageChanged;
            _subscribed = true;
        }

        // The language changed: the status and each card's state are written again at once. A card's name and description
        // are its shop option's, made as the scene loaded, and follow on the next load (the next run).
        private void OnLanguageChanged()
        {
            if (IsOpen)
                Refresh();
        }

        private static bool CardsAreValid(ForgeCard[] cards, int itemCount)
        {
            if (cards == null || cards.Length < itemCount)
                return false;

            for (int i = 0; i < cards.Length; i++)
            {
                if (!cards[i].IsComplete)
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
            _statusLabel.text = string.Format(Loc.Numbers, _text.ForgeStatusFormat, _setup.Profile.Gold, _setup.Profile.DeepestFloorCleared);

            // The next run's hero's own weapon, as the hero chooser leaves it (CombatSetup.NextWeapon): carried, never sold.
            WeaponDefinition own = _setup.NextWeapon;
            for (int i = 0; i < _weaponCards.Length; i++)
            {
                bool used = i == 0 && own != null;
                _weaponCards[i].Button.gameObject.SetActive(used);
                if (used)
                    Show(_weaponCards[i], own.DisplayName, own.CreateForgeOption().Description, UnlockStatus.Equipped, 0, 0,
                        _text.ForgeOwnWeaponLabel);
            }

            RelicShop relics = _setup.Relics;
            for (int i = 0; i < _relicCards.Length; i++)
            {
                bool used = i < relics.Relics.Count;
                _relicCards[i].Button.gameObject.SetActive(used);
                if (!used)
                    continue;

                RelicOption relic = relics.Relics[i];
                if (_relicCards[i].Icon != null)
                {
                    _relicCards[i].Icon.sprite = relic.Effect switch
                    {
                        RelicEffect.SecondWind => _secondWindIcon,
                        RelicEffect.CounterStrike => _counterweightIcon,
                        _ => null
                    };
                    _relicCards[i].Icon.enabled = _relicCards[i].Icon.sprite != null;
                }
                Show(_relicCards[i], relic.DisplayName, relic.Description, relics.StatusOf(relic), relic.Price,
                    relics.GoldNeededFor(relic));
            }

            ShowSkills();
            _anvil.RefreshSkillsEntry();
            SetInteractable(false);
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            _awaitingInputDelay = true;
        }

        // The band's header and card for the next run's hero (the hero chooser's): how many of its skills it carries, then its
        // next skill - the first of skills 2 and 3 not yet forged, or its last once every one is - in the Skills tab's words.
        private void ShowSkills()
        {
            if (_skillsHeader == null || !_skillsCard.IsComplete)
                return;
            HeroDefinition hero = _setup.NextRunHero;
            int slots = 0;
            while (hero != null && slots < HeroSkillUnlock.LastSlot && hero.SkillAt(slots + 1) != null)
                slots++;
            int ready = hero != null ? Math.Min(_setup.Profile.Skills.SlotsFor(hero.Id), slots) : 0;
            _skillsHeader.text = Loc.Format("skills.forge_header", "Skills: {0} of {1} ready", ready, slots);
            _skillsSlot = Math.Min(ready + 1, slots);
            AbilityDefinition skill = hero != null && _skillsSlot > 0 ? hero.SkillAt(_skillsSlot) : null;
            _skillsCard.Button.gameObject.SetActive(skill != null);
            if (skill == null)
                return;
            if (_skillsCard.Icon != null)
            {
                _skillsCard.Icon.sprite = skill.Icon;
                _skillsCard.Icon.enabled = skill.Icon != null;
            }
            SkillForgeStatus status = _setup.Profile.SkillStatusOf(hero.Id, _skillsSlot);
            // The relics' looks for the skill's states, as the Skills tab colours them: forged in the worn green, the call's
            // gold while the Forge can forge it, waiting on skill 2 in blue, grey out of reach (locked or short of gold).
            UnlockStatus look = status switch
            {
                SkillForgeStatus.Forged => UnlockStatus.Equipped,
                SkillForgeStatus.Affordable => UnlockStatus.Affordable,
                SkillForgeStatus.NeedsEarlierSkill => UnlockStatus.Owned,
                _ => UnlockStatus.TooExpensive
            };
            Show(_skillsCard, AbilityText.Name(skill), AbilityText.Description(skill), look, 0, 0,
                AnvilView.SkillStateText(_setup, hero, _skillsSlot, status));
        }

        // stateText, when given, is the state line in place of the status's own words (the hero's own weapon's).
        private void Show(ForgeCard card, string displayName, string description, UnlockStatus status, int price, int goldNeeded,
            string stateText = null)
        {
            card.Name.text = displayName;
            card.Description.text = description;
            // The written state remains the primary cue; colour makes the equipped/affordable rows easy to scan.
            Color stateColor = status switch
            {
                UnlockStatus.Equipped => new Color32(137, 213, 180, 255),
                UnlockStatus.Affordable => new Color32(238, 191, 108, 255),
                UnlockStatus.Owned => new Color32(145, 188, 225, 255),
                _ => new Color32(163, 167, 183, 255)
            };
            card.State.color = stateColor;
            if (card.StateAccent != null)
                card.StateAccent.color = stateColor;
            switch (status)
            {
                case UnlockStatus.Affordable:
                    card.State.text = string.Format(Loc.Numbers, _text.UnlockForgeFormat, price);
                    break;
                case UnlockStatus.TooExpensive:
                    card.State.text = string.Format(Loc.Numbers, _text.UnlockNeedGoldFormat, price, goldNeeded);
                    break;
                case UnlockStatus.Owned:
                    card.State.text = _text.UnlockOwnedLabel;
                    break;
                default:
                    card.State.text = _text.UnlockEquippedLabel;
                    break;
            }
            if (stateText != null)
                card.State.text = stateText;
            CardShown?.Invoke(card.Button, status);
        }

        private void Update()
        {
            if (!_awaitingInputDelay || Time.unscaledTime < _inputEnabledAt)
                return;

            _awaitingInputDelay = false;
            SetInteractable(true);
        }

        // Cards the player cannot afford are disabled so they read as locked; equipped cards stay lit and do nothing, as the
        // hero's own weapon's card always does (it has no listener). The skills' card is an entry: always a tap's, whatever
        // its skill's state, once the input delay is over.
        private void SetInteractable(bool interactable)
        {
            for (int i = 0; i < _weaponCards.Length; i++)
                _weaponCards[i].Button.interactable = interactable;
            if (_skillsCard.Button != null)
                _skillsCard.Button.interactable = interactable;

            RelicShop relics = _setup.Relics;
            for (int i = 0; i < _relicCards.Length && i < relics.Relics.Count; i++)
                _relicCards[i].Button.interactable = interactable && relics.StatusOf(relics.Relics[i]) != UnlockStatus.TooExpensive;
            _startButton.interactable = interactable;
            if (_heroChoice != null)
                _heroChoice.SetInteractable(interactable);
        }

        // A successful forge or equip changes the profile, which refreshes the cards and restarts the input delay.
        private void OnRelicCard(int slot)
        {
            RelicShop relics = _setup.Relics;
            if (IsOpen && !_awaitingInputDelay && slot < relics.Relics.Count)
                relics.TrySelect(relics.Relics[slot]);
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
            Loc.Changed -= OnLanguageChanged;
            if (_startButton != null)
                _startButton.onClick.RemoveListener(OnStart);
            RemoveListeners(_weaponCards);
            RemoveListeners(_relicCards);
            if (_skillsCard.Button != null)
                _skillsCard.Button.onClick.RemoveAllListeners();
        }

        private static void RemoveListeners(ForgeCard[] cards)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].Button != null)
                    cards[i].Button.onClick.RemoveAllListeners();
            }
        }
    }
}
