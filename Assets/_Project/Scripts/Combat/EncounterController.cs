using System;
using Cryptforge.Content;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Owns one active enemy at a time: spawns it from the prefab with fresh health and weapon, points the hero and
    // the enemy at each other, and starts the next encounter once the current one is cleared, no upgrade choice is
    // open and the hero is still alive.
    public sealed class EncounterController : MonoBehaviour
    {
        [SerializeField] private Health _enemyPrefab;
        [SerializeField] private EnemyDefinition _enemyDefinition;
        [SerializeField] private Health _hero;
        [SerializeField] private Targeting _heroTargeting;
        [SerializeField, Min(0f)] private float _advanceDelay = 1f;
        private UpgradeService _upgrades;
        private EncounterProgress _progress;

        public Health CurrentEnemy { get; private set; }
        public EnemyDefinition EnemyDefinition => _enemyDefinition;
        public int EncounterNumber => _progress?.EncounterNumber ?? 0;
        public int HitsTaken => _progress?.HitsTaken ?? 0;
        public float Elapsed => _progress?.Elapsed ?? 0f;
        public bool IsCleared => _progress != null && _progress.IsCleared;
        public int EncountersCleared => _progress?.EncountersCleared ?? 0;

        public event Action EncounterStarted;
        public event Action ProgressChanged;
        public event Action<Health> EnemyDefeated;

        public void Initialize(UpgradeService upgrades)
        {
            if (_enemyPrefab == null || _enemyDefinition == null || _enemyDefinition.Weapon == null || _hero == null ||
                _heroTargeting == null)
                throw new InvalidOperationException("EncounterController needs an enemy prefab, an armed definition, the hero and hero targeting.");
            if (_enemyPrefab.GetComponent<AttackController>() == null || _enemyPrefab.GetComponent<Targeting>() == null)
                throw new InvalidOperationException("The enemy prefab needs AttackController and Targeting components.");
            if (_progress != null)
                throw new InvalidOperationException("EncounterController has already been initialized.");

            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _progress = new EncounterProgress(_advanceDelay);
            StartNext();
        }

        private void Update()
        {
            if (_progress != null && _progress.Tick(Time.deltaTime, _upgrades.CurrentOffer == null && _hero.IsAlive))
                StartNext();
        }

        private void StartNext()
        {
            Release(CurrentEnemy, true);

            Health enemy = Instantiate(_enemyPrefab, transform.position, Quaternion.identity);
            enemy.name = _enemyPrefab.name;
            enemy.Initialize(_enemyDefinition.MaximumHealth);
            enemy.GetComponent<Targeting>().SetCandidates(new[] { _hero });
            enemy.GetComponent<AttackController>().Initialize(_enemyDefinition.Weapon.CreateRuntime());
            enemy.Changed += OnEnemyChanged;
            enemy.Died += OnEnemyDied;
            CurrentEnemy = enemy;
            _heroTargeting.SetCandidates(new[] { enemy });

            _progress.Begin();
            EncounterStarted?.Invoke();
            ProgressChanged?.Invoke();
        }

        private void OnEnemyChanged()
        {
            _progress.RecordHit();
            ProgressChanged?.Invoke();
        }

        private void OnEnemyDied()
        {
            if (!_progress.Clear())
                return;

            EnemyDefeated?.Invoke(CurrentEnemy);
            ProgressChanged?.Invoke();
        }

        // Unsubscribing first guarantees an earlier enemy can never report a death into a later encounter.
        private void Release(Health enemy, bool destroy)
        {
            if (ReferenceEquals(enemy, null))
                return;

            enemy.Changed -= OnEnemyChanged;
            enemy.Died -= OnEnemyDied;
            if (destroy && enemy != null)
                Destroy(enemy.gameObject);
        }

        private void OnDestroy() => Release(CurrentEnemy, false);
    }
}
