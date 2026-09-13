using System;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The Relic Forge panel opened from the result screen: a card per weapon and per relic, forge or equip on tap, then
    // start a run. The shops decide what a tap does; this view only shows the state and briefly ignores taps after each
    // change.
    public sealed class RelicForgeView : MonoBehaviour
    {
        [Serializable]
        private struct ForgeCard
        {
            public Button Button;
            public Text Name;
            public Text Description;
            public Text State;

            public bool IsComplete => Button != null && Name != null && Description != null && State != null;
        }

        [SerializeField] private CombatSetup _setup;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Text _weaponsHeaderLabel;
        [SerializeField] private Text _relicsHeaderLabel;
        [SerializeField] private ForgeCard[] _weaponCards;
        [SerializeField] private ForgeCard[] _relicCards;
        [SerializeField] private Button _startButton;
        [SerializeField] private Text _startLabel;
        [SerializeField, Min(0f)] private float _inputDelay = 0.25f;
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void Start()
        {
            if (_setup == null || _setup.Relics == null || _setup.Weapons == null || _text == null || _panel == null ||
                _titleLabel == null || _statusLabel == null || _weaponsHeaderLabel == null || _relicsHeaderLabel == null ||
                _startButton == null || _startLabel == null || !CardsAreValid(_weaponCards, _setup.Weapons.Weapons.Count) ||
                !CardsAreValid(_relicCards, _setup.Relics.Relics.Count))
            {
                Debug.LogError("RelicForgeView is missing a scene or content reference, or has fewer cards than items.", this);
                enabled = false;
                return;
            }

            _panel.SetActive(false);
            _titleLabel.text = _text.ForgeTitle;
            _weaponsHeaderLabel.text = _text.ForgeWeaponsHeader;
            _relicsHeaderLabel.text = _text.ForgeRelicsHeader;
            _startLabel.text = _text.StartRunLabel;
            for (int i = 0; i < _weaponCards.Length; i++)
            {
                int slot = i;
                _weaponCards[i].Button.onClick.AddListener(() => OnWeaponCard(slot));
            }
            for (int i = 0; i < _relicCards.Length; i++)
            {
                int slot = i;
                _relicCards[i].Button.onClick.AddListener(() => OnRelicCard(slot));
            }
            _startButton.onClick.AddListener(OnStart);
            _setup.Profile.Changed += OnProfileChanged;
            _subscribed = true;
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
            _statusLabel.text = string.Format(_text.ForgeStatusFormat, _setup.Profile.Gold, _setup.Profile.DeepestFloorCleared);

            WeaponShop weapons = _setup.Weapons;
            for (int i = 0; i < _weaponCards.Length; i++)
            {
                bool used = i < weapons.Weapons.Count;
                _weaponCards[i].Button.gameObject.SetActive(used);
                if (!used)
                    continue;

                WeaponOption weapon = weapons.Weapons[i];
                Show(_weaponCards[i], weapon.DisplayName, weapon.Description, weapons.StatusOf(weapon), weapon.Price,
                    weapons.GoldNeededFor(weapon));
            }

            RelicShop relics = _setup.Relics;
            for (int i = 0; i < _relicCards.Length; i++)
            {
                bool used = i < relics.Relics.Count;
                _relicCards[i].Button.gameObject.SetActive(used);
                if (!used)
                    continue;

                RelicOption relic = relics.Relics[i];
                Show(_relicCards[i], relic.DisplayName, relic.Description, relics.StatusOf(relic), relic.Price,
                    relics.GoldNeededFor(relic));
            }

            SetInteractable(false);
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            _awaitingInputDelay = true;
        }

        private void Show(ForgeCard card, string displayName, string description, UnlockStatus status, int price, int goldNeeded)
        {
            card.Name.text = displayName;
            card.Description.text = description;
            switch (status)
            {
                case UnlockStatus.Affordable:
                    card.State.text = string.Format(_text.UnlockForgeFormat, price);
                    break;
                case UnlockStatus.TooExpensive:
                    card.State.text = string.Format(_text.UnlockNeedGoldFormat, price, goldNeeded);
                    break;
                case UnlockStatus.Owned:
                    card.State.text = _text.UnlockOwnedLabel;
                    break;
                default:
                    card.State.text = _text.UnlockEquippedLabel;
                    break;
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
            WeaponShop weapons = _setup.Weapons;
            for (int i = 0; i < _weaponCards.Length && i < weapons.Weapons.Count; i++)
                _weaponCards[i].Button.interactable = interactable && weapons.StatusOf(weapons.Weapons[i]) != UnlockStatus.TooExpensive;

            RelicShop relics = _setup.Relics;
            for (int i = 0; i < _relicCards.Length && i < relics.Relics.Count; i++)
                _relicCards[i].Button.interactable = interactable && relics.StatusOf(relics.Relics[i]) != UnlockStatus.TooExpensive;
            _startButton.interactable = interactable;
        }

        // A successful forge or equip changes the profile, which refreshes the cards and restarts the input delay.
        private void OnWeaponCard(int slot)
        {
            WeaponShop weapons = _setup.Weapons;
            if (IsOpen && !_awaitingInputDelay && slot < weapons.Weapons.Count)
                weapons.TrySelect(weapons.Weapons[slot]);
        }

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
