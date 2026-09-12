using Cryptforge.Combat;
using Cryptforge.Content;
using UnityEngine;

namespace Cryptforge.Core
{
    // Scene composition only. Combat and presentation own their own updates/events.
    public sealed class CombatSetup : MonoBehaviour
    {
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private EnemyDefinition _enemyDefinition;
        [SerializeField] private Health _hero;
        [SerializeField] private Health _enemy;
        [SerializeField] private AttackController _attack;

        private void Awake()
        {
            if (_heroDefinition == null || _enemyDefinition == null || _hero == null ||
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
        }
    }
}
