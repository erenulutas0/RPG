using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Economy;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cryptforge.Core
{
    // Scene composition only. Combat and presentation own their own updates/events.
    public sealed class CombatSetup : MonoBehaviour
    {
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private UpgradeDefinition[] _upgrades;
        [SerializeField] private Health _hero;
        [SerializeField] private AttackController _attack;
        [SerializeField] private EncounterController _encounters;
        private RewardService _rewards;
        private bool _pausedForChoice;
        private bool _restarting;

        public RunState Run { get; private set; }
        public WeaponRuntime Weapon { get; private set; }
        public UpgradeService Upgrades { get; private set; }
        public ForgeService Forge { get; private set; }
        public RunChoices Choices { get; private set; }

        private void Awake()
        {
            if (_heroDefinition == null || _economy == null || _hero == null || _attack == null ||
                _encounters == null || _heroDefinition.StartingWeapon == null || !HasUpgrades())
            {
                Debug.LogError("CombatSetup is missing required scene or definition references.", this);
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            _hero.Initialize(_heroDefinition.MaximumHealth);
            Weapon = _heroDefinition.StartingWeapon.CreateRuntime();
            _attack.Initialize(Weapon);
            Run = new RunState(_economy.ExperiencePerLevel);
            _rewards = new RewardService(Run, _economy.ExperiencePerKill);

            var options = new UpgradeOption[_upgrades.Length];
            for (int i = 0; i < _upgrades.Length; i++)
                options[i] = _upgrades[i].CreateOption();
            Upgrades = new UpgradeService(Run, Weapon, options, _economy.UpgradeChoiceCount);
            Forge = new ForgeService(Run, _hero);
            Choices = new RunChoices(Upgrades, Forge);
            Choices.Changed += OnChoicesChanged;
            _hero.Died += OnHeroDied;

            // Subscribe before the first spawn so every defeated enemy reaches the reward service.
            _encounters.EnemyDefeated += OnEnemyDefeated;
            _encounters.FloorCleared += OnFloorCleared;
            _encounters.Initialize(Choices, Forge);
        }

        // A reload rebuilds every runtime object from definitions, so nothing from the ended run carries over.
        public void RestartRun()
        {
            if (_restarting)
                return;

            _restarting = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private bool HasUpgrades()
        {
            if (_upgrades == null || _upgrades.Length == 0)
                return false;
            for (int i = 0; i < _upgrades.Length; i++)
            {
                if (_upgrades[i] == null)
                    return false;
            }
            return true;
        }

        private void OnEnemyDefeated(Health enemy) => _rewards.TryAwardKill(enemy);

        private void OnHeroDied() => Run.End(RunOutcome.Defeat);

        private void OnFloorCleared() => Run.End(RunOutcome.Victory);

        // Scaled time freezes combat cadence while any choice is open; uGUI input runs on unscaled time.
        private void OnChoicesChanged()
        {
            bool choosing = Choices.IsOpen;
            if (choosing == _pausedForChoice)
                return;

            _pausedForChoice = choosing;
            Time.timeScale = choosing ? 0f : 1f;
        }

        private void OnDestroy()
        {
            if (_hero != null)
                _hero.Died -= OnHeroDied;
            if (_encounters != null)
            {
                _encounters.EnemyDefeated -= OnEnemyDefeated;
                _encounters.FloorCleared -= OnFloorCleared;
            }
            if (Choices != null)
                Choices.Changed -= OnChoicesChanged;
            if (_pausedForChoice)
                Time.timeScale = 1f;
        }
    }
}
