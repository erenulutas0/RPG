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
    public sealed class RelicForgeView : MonoBehaviour
    {
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

        public bool IsOpen => _panel != null && _panel.activeSelf;
        // The hero chooser beside the start button; null while the roster has one hero. For tests.
        public HeroChoiceView HeroChoice => _heroChoice;
        // The Anvil, Efsun and Bag tabs (docs/54 phase 4), opened from the buttons beside the nameplate.
        public AnvilView Anvil => _anvil;
        private AnvilView _anvil;
        // Each card shown, with the state it shows, as the panel refreshes: the painted screens dress the card by it.
        public event Action<Button, UnlockStatus> CardShown;

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
            // docs/54 phase 4: the Anvil's and the Bag's buttons beside the nameplate, and the panel they open; docs/57 phase 4:
            // the Skills tab's at the right end of the weapons' header.
            _anvil = gameObject.AddComponent<AnvilView>();
            _anvil.Initialize(_setup, _titleLabel, _startLabel, _weaponsHeaderLabel);
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

            _anvil.RefreshSkillsEntry();
            SetInteractable(false);
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            _awaitingInputDelay = true;
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
        // hero's own weapon's card always does (it has no listener).
        private void SetInteractable(bool interactable)
        {
            for (int i = 0; i < _weaponCards.Length; i++)
                _weaponCards[i].Button.interactable = interactable;

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
