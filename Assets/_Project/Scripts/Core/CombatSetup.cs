using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Economy;
using UnityEngine;

namespace Cryptforge.Core
{
    // Scene composition only. Combat and presentation own their own updates/events.
    public sealed class CombatSetup : MonoBehaviour
    {
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private EnemyDefinition _enemyDefinition;
        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private Health _hero;
        [SerializeField] private Health _enemy;
        [SerializeField] private AttackController _attack;
        private RewardService _rewards;

        public RunState Run { get; private set; }

        private void Awake()
        {
            if (_heroDefinition == null || _enemyDefinition == null || _economy == null || _hero == null ||
                _enemy == null || _attack == null || _heroDefinition.StartingWeapon == null)
            {
                Debug.LogError("CombatSetup is missing required scene or definition references.", this);
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            _hero.Initialize(_heroDefinition.MaximumHealth);
            _enemy.Initialize(_enemyDefinition.MaximumHealth);
            _attack.Initialize(_heroDefinition.StartingWeapon.CreateRuntime());
            Run = new RunState();
            _rewards = new RewardService(Run, _economy.ExperiencePerKill);
            _enemy.Died += OnEnemyDied;
        }

        private void OnEnemyDied() => _rewards.TryAwardKill(_enemy);

        private void OnDestroy()
        {
            if (_enemy != null)
                _enemy.Died -= OnEnemyDied;
        }
    }
}
