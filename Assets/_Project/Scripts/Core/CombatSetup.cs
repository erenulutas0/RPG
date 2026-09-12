using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Economy;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Core
{
    // Scene composition only. Combat and presentation own their own updates/events.
    public sealed class CombatSetup : MonoBehaviour
    {
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private EnemyDefinition _enemyDefinition;
        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private UpgradeDefinition[] _upgrades;
        [SerializeField] private Health _hero;
        [SerializeField] private Health _enemy;
        [SerializeField] private AttackController _attack;
        private RewardService _rewards;
        private bool _pausedForChoice;

        public RunState Run { get; private set; }
        public WeaponRuntime Weapon { get; private set; }
        public UpgradeService Upgrades { get; private set; }

        private void Awake()
        {
            if (_heroDefinition == null || _enemyDefinition == null || _economy == null || _hero == null ||
                _enemy == null || _attack == null || _heroDefinition.StartingWeapon == null || !HasUpgrades())
            {
                Debug.LogError("CombatSetup is missing required scene or definition references.", this);
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            _hero.Initialize(_heroDefinition.MaximumHealth);
            _enemy.Initialize(_enemyDefinition.MaximumHealth);
            Weapon = _heroDefinition.StartingWeapon.CreateRuntime();
            _attack.Initialize(Weapon);
            Run = new RunState(_economy.ExperiencePerLevel);
            _rewards = new RewardService(Run, _economy.ExperiencePerKill);

            var options = new UpgradeOption[_upgrades.Length];
            for (int i = 0; i < _upgrades.Length; i++)
                options[i] = _upgrades[i].CreateOption();
            Upgrades = new UpgradeService(Run, Weapon, options, _economy.UpgradeChoiceCount);
            Upgrades.OfferChanged += OnOfferChanged;
            _enemy.Died += OnEnemyDied;
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

        private void OnEnemyDied() => _rewards.TryAwardKill(_enemy);

        // Scaled time freezes combat cadence while a choice is open; uGUI input runs on unscaled time.
        private void OnOfferChanged()
        {
            bool choosing = Upgrades.CurrentOffer != null;
            if (choosing == _pausedForChoice)
                return;

            _pausedForChoice = choosing;
            Time.timeScale = choosing ? 0f : 1f;
        }

        private void OnDestroy()
        {
            if (_enemy != null)
                _enemy.Died -= OnEnemyDied;
            if (Upgrades != null)
                Upgrades.OfferChanged -= OnOfferChanged;
            if (_pausedForChoice)
                Time.timeScale = 1f;
        }
    }
}
