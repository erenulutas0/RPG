using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Shows why and how far the run went, then offers a single restart. The button is briefly disabled
    // after opening so a tap aimed at combat cannot restart by accident, and disables itself on first use.
    public sealed class RunResultView : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private EncounterController _encounters;
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _causeLabel;
        [SerializeField] private Text _progressLabel;
        [SerializeField] private Text _buildLabel;
        [SerializeField] private Text _restartLabel;
        [SerializeField] private Button _restartButton;
        [SerializeField, Min(0f)] private float _inputDelay = 0.5f;
        [SerializeField] private Color _victoryColor = new Color(0.95f, 0.8f, 0.35f);
        [SerializeField] private Color _defeatColor = new Color(1f, 0.55f, 0.35f);
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void Start()
        {
            if (_setup == null || _setup.Run == null || _setup.Upgrades == null || _encounters == null || _encounters.Floor == null ||
                _heroDefinition == null || _text == null || _panel == null || _titleLabel == null ||
                _causeLabel == null || _progressLabel == null || _buildLabel == null || _restartLabel == null ||
                _restartButton == null)
            {
                Debug.LogError("RunResultView is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            _panel.SetActive(false);
            _restartLabel.text = _text.RestartLabel;
            _restartButton.onClick.AddListener(OnRestart);
            _setup.Run.Ended += Show;
            _subscribed = true;
            if (_setup.Run.HasEnded)
                Show();
        }

        private void Show()
        {
            RunState run = _setup.Run;
            bool victory = run.Outcome == RunOutcome.Victory;
            string enemyName = _encounters.CurrentDefinition != null ? _encounters.CurrentDefinition.DisplayName : string.Empty;
            string roomName = _encounters.CurrentRoom != null ? _encounters.CurrentRoom.DisplayName : string.Empty;
            _titleLabel.text = victory ? _text.ResultVictoryTitle : _text.ResultDefeatTitle;
            _titleLabel.color = victory ? _victoryColor : _defeatColor;
            _causeLabel.text = victory
                ? string.Format(_text.ResultVictoryCauseFormat, _heroDefinition.DisplayName, enemyName, _encounters.Floor.DisplayName)
                : string.Format(_text.ResultDefeatCauseFormat, _heroDefinition.DisplayName, enemyName, roomName);
            _progressLabel.text = string.Format(_text.ResultProgressFormat, _encounters.RoomsCleared, _encounters.RoomCount,
                run.Level, run.Experience);
            _buildLabel.text = string.Format(_text.ResultBuildFormat, DescribeBuild());

            _panel.SetActive(true);
            _restartButton.interactable = false;
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            _awaitingInputDelay = true;
        }

        private string DescribeBuild()
        {
            UpgradeService upgrades = _setup.Upgrades;
            var entries = new List<string>(upgrades.Pool.Count);
            for (int i = 0; i < upgrades.Pool.Count; i++)
            {
                UpgradeOption option = upgrades.Pool[i];
                int stacks = upgrades.StacksOf(option);
                if (stacks > 0)
                    entries.Add(string.Format(_text.ResultUpgradeFormat, option.DisplayName, stacks));
            }

            return entries.Count > 0 ? string.Join(", ", entries) : _text.ResultNoUpgrades;
        }

        private void Update()
        {
            if (!_awaitingInputDelay || Time.unscaledTime < _inputEnabledAt)
                return;

            _awaitingInputDelay = false;
            _restartButton.interactable = true;
        }

        private void OnRestart()
        {
            if (_awaitingInputDelay || !_restartButton.interactable)
                return;

            _restartButton.interactable = false;
            _setup.RestartRun();
        }

        private void OnDestroy()
        {
            if (!_subscribed)
                return;

            _setup.Run.Ended -= Show;
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestart);
        }
    }
}
