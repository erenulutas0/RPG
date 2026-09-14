using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Shows why and how far the run went, what the Forge holds now, and offers a single restart or the Relic Forge.
    // Both buttons are briefly disabled after opening so a tap aimed at combat cannot act by accident.
    public sealed class RunResultView : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private EncounterController _encounters;
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private RelicForgeView _forge;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _causeLabel;
        [SerializeField] private Text _progressLabel;
        [SerializeField] private Text _goldLabel;
        [SerializeField] private Text _buildLabel;
        [SerializeField] private Text _forgeHintLabel;
        [SerializeField] private Text _restartLabel;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Text _forgeButtonLabel;
        [SerializeField] private Button _forgeButton;
        [SerializeField, Min(0f)] private float _inputDelay = 0.5f;
        [SerializeField] private Color _victoryColor = new Color(0.95f, 0.8f, 0.35f);
        [SerializeField] private Color _defeatColor = new Color(1f, 0.55f, 0.35f);
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void Start()
        {
            if (_setup == null || _setup.Run == null || _setup.Upgrades == null || _setup.Relics == null || _setup.Weapons == null ||
                _encounters == null ||
                _encounters.Floor == null || _heroDefinition == null || _text == null || _forge == null || _panel == null ||
                _titleLabel == null || _causeLabel == null || _progressLabel == null || _goldLabel == null || _buildLabel == null ||
                _forgeHintLabel == null || _restartLabel == null || _restartButton == null || _forgeButtonLabel == null ||
                _forgeButton == null)
            {
                Debug.LogError("RunResultView is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            _panel.SetActive(false);
            _restartLabel.text = _text.RestartLabel;
            _forgeButtonLabel.text = _text.ForgeButtonLabel;
            _restartButton.onClick.AddListener(OnRestart);
            _forgeButton.onClick.AddListener(OnForge);
            // RunBank subscribed in CombatSetup.Awake, so the run's gold is already in the profile when Show runs.
            _setup.Run.Ended += Show;
            _subscribed = true;
            if (_setup.Run.HasEnded)
                Show();
        }

        private void Show()
        {
            RunState run = _setup.Run;
            string hero = _heroDefinition.DisplayName;
            // A defeat names the enemy whose hit landed last; a victory names the final wave's boss, even when its pack
            // outlived it.
            EnemyDefinition killer = run.Outcome != RunOutcome.Defeat
                ? _encounters.ToughestDefinition
                : _setup.LastAttacker != null ? _setup.LastAttacker : _encounters.CurrentDefinition;
            string enemyName = killer != null ? killer.DisplayName : string.Empty;
            string roomName = _encounters.CurrentRoom != null ? _encounters.CurrentRoom.DisplayName : string.Empty;
            string floorName = _encounters.Floor.DisplayName;
            switch (run.Outcome)
            {
                case RunOutcome.Victory:
                    _titleLabel.text = _text.ResultVictoryTitle;
                    _titleLabel.color = _victoryColor;
                    _causeLabel.text = string.Format(_text.ResultVictoryCauseFormat, hero, enemyName, floorName);
                    break;
                case RunOutcome.Extracted:
                    _titleLabel.text = _text.ResultExtractedTitle;
                    _titleLabel.color = _victoryColor;
                    _causeLabel.text = string.Format(_text.ResultExtractedCauseFormat, hero, floorName);
                    break;
                default:
                    _titleLabel.text = _text.ResultDefeatTitle;
                    _titleLabel.color = _defeatColor;
                    _causeLabel.text = string.Format(_text.ResultDefeatCauseFormat, hero, enemyName, roomName);
                    break;
            }

            _progressLabel.text = string.Format(_text.ResultProgressFormat, _encounters.FloorNumber, _encounters.TotalRoomsCleared,
                run.Level, run.Experience);
            _goldLabel.text = run.GoldLost > 0
                ? string.Format(_text.ResultGoldLostFormat, run.GoldBanked, run.GoldLost)
                : string.Format(_text.ResultGoldFormat, run.GoldBanked);
            _buildLabel.text = string.Format(_text.ResultBuildFormat, _setup.HeroWeapon.DisplayName, DescribeBuild());
            _forgeHintLabel.text = DescribeForge();

            _panel.SetActive(true);
            SetButtonsInteractable(false);
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            _awaitingInputDelay = true;
        }

        private string DescribeBuild()
        {
            var entries = new List<string>();
            if (_setup.Relic != null)
                entries.Add(string.Format(_text.ResultRelicFormat, _setup.Relic.Relic.DisplayName, _setup.Relic.Triggers));

            UpgradeService upgrades = _setup.Upgrades;
            for (int i = 0; i < upgrades.Pool.Count; i++)
            {
                UpgradeOption option = upgrades.Pool[i];
                int stacks = upgrades.StacksOf(option);
                if (stacks > 0)
                    entries.Add(string.Format(_text.ResultUpgradeFormat, option.DisplayName, stacks));
            }

            return entries.Count > 0 ? string.Join(", ", entries) : _text.ResultNoUpgrades;
        }

        // The "one more run" hook from 04_CORE_LOOP_RETENTION: always name the nearest unlock, relic or weapon.
        private string DescribeForge()
        {
            int gold = _setup.Profile.Gold;
            RelicOption relic = _setup.Relics.NextUnlock;
            WeaponOption weapon = _setup.Weapons.NextUnlock;
            if (relic == null && weapon == null)
                return string.Format(_text.ForgeHintCompleteFormat, gold);

            // The cheaper one is nearer; a relic wins a tie.
            bool relicFirst = weapon == null || (relic != null && relic.Price <= weapon.Price);
            string name = relicFirst ? relic.DisplayName : weapon.DisplayName;
            int price = relicFirst ? relic.Price : weapon.Price;
            return gold >= price
                ? string.Format(_text.ForgeHintReadyFormat, gold, name)
                : string.Format(_text.ForgeHintNextFormat, gold, name, price);
        }

        private void Update()
        {
            if (!_awaitingInputDelay || Time.unscaledTime < _inputEnabledAt)
                return;

            _awaitingInputDelay = false;
            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _restartButton.interactable = interactable;
            _forgeButton.interactable = interactable;
        }

        private void OnRestart()
        {
            if (_awaitingInputDelay || !_restartButton.interactable)
                return;

            SetButtonsInteractable(false);
            _setup.RestartRun();
        }

        private void OnForge()
        {
            if (_awaitingInputDelay || !_forgeButton.interactable)
                return;

            _forge.Open();
        }

        private void OnDestroy()
        {
            if (!_subscribed)
                return;

            _setup.Run.Ended -= Show;
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestart);
            if (_forgeButton != null)
                _forgeButton.onClick.RemoveListener(OnForge);
        }
    }
}
