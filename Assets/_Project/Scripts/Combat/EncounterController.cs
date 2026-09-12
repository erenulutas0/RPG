using System;
using Cryptforge.Content;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Runs one floor: spawns each wave's enemy with fresh health and weapon, opens the forge in non-combat rooms, and
    // advances after a short delay once the current step is resolved, no choice is open and the hero lives.
    // The final wave's kill clears the floor immediately.
    public sealed class EncounterController : MonoBehaviour
    {
        [SerializeField] private FloorDefinition _floor;
        [SerializeField] private Health _hero;
        [SerializeField] private Targeting _heroTargeting;
        [SerializeField, Min(0f)] private float _advanceDelay = 1f;
        private RunChoices _choices;
        private ForgeService _forge;
        private FloorProgress _floorProgress;
        private EncounterProgress _fight;
        private EnrageBehaviour _currentEnrage;
        private float _nonCombatWait;

        public FloorDefinition Floor => _floor;
        public RoomDefinition CurrentRoom =>
            _floorProgress != null && _floorProgress.RoomIndex >= 0 ? _floor.RoomAt(_floorProgress.RoomIndex) : null;
        public int RoomNumber => _floorProgress != null ? _floorProgress.RoomIndex + 1 : 0;
        public int RoomCount => _floor != null ? _floor.RoomCount : 0;
        public int WaveNumber => _floorProgress != null ? _floorProgress.WaveIndex + 1 : 0;
        public int WaveCount => CurrentRoom?.WaveCount ?? 0;
        public int RoomsCleared => _floorProgress?.RoomsCleared ?? 0;
        public bool IsFloorCleared => _floorProgress != null && _floorProgress.IsComplete;
        public bool IsInNonCombatRoom { get; private set; }
        public Health CurrentEnemy { get; private set; }
        public EnemyDefinition CurrentDefinition { get; private set; }
        public bool IsCurrentEnemyEnraged => _currentEnrage != null && _currentEnrage.IsEnraged;
        public int EncounterNumber => _fight?.EncounterNumber ?? 0;
        public int HitsTaken => _fight?.HitsTaken ?? 0;
        public float Elapsed => _fight?.Elapsed ?? 0f;
        public bool IsCleared => _fight != null && _fight.IsCleared;

        public event Action EncounterStarted;
        public event Action ProgressChanged;
        public event Action<Health> EnemyDefeated;
        public event Action FloorCleared;

        public void Initialize(RunChoices choices, ForgeService forge)
        {
            if (_floor == null || _floor.RoomCount == 0 || _hero == null || _heroTargeting == null)
                throw new InvalidOperationException("EncounterController needs a floor with rooms, the hero and hero targeting.");
            if (_floorProgress != null)
                throw new InvalidOperationException("EncounterController has already been initialized.");

            _choices = choices ?? throw new ArgumentNullException(nameof(choices));
            _forge = forge ?? throw new ArgumentNullException(nameof(forge));
            var waves = new int[_floor.RoomCount];
            for (int i = 0; i < waves.Length; i++)
                waves[i] = ValidateRoom(_floor.RoomAt(i), i);

            _floorProgress = new FloorProgress(waves);
            _fight = new EncounterProgress(_advanceDelay);
            AdvanceFloor();
        }

        // Fail at startup with the offending room rather than mid-run when it is first reached.
        private static int ValidateRoom(RoomDefinition room, int index)
        {
            if (room == null)
                throw new InvalidOperationException($"Floor room {index} is missing.");

            if (room.Kind == RoomKind.Forge)
            {
                if (room.WaveCount != 0 || room.ForgeOptionCount == 0)
                    throw new InvalidOperationException($"Forge room {index} needs forge options and no waves.");
                for (int i = 0; i < room.ForgeOptionCount; i++)
                {
                    if (room.ForgeOptionAt(i) == null)
                        throw new InvalidOperationException($"Forge room {index} has an empty option slot.");
                }
                return 0;
            }

            if (room.WaveCount == 0)
                throw new InvalidOperationException($"Room {index} needs at least one wave.");
            for (int i = 0; i < room.WaveCount; i++)
            {
                EnemyDefinition enemy = room.WaveAt(i);
                if (enemy == null || enemy.Weapon == null || enemy.Prefab == null ||
                    enemy.Prefab.GetComponent<AttackController>() == null || enemy.Prefab.GetComponent<Targeting>() == null)
                    throw new InvalidOperationException(
                        $"Room {index} wave {i} needs an enemy definition with a weapon and a prefab carrying Health, Targeting and AttackController.");
            }
            return room.WaveCount;
        }

        private void Update()
        {
            if (_floorProgress == null || _floorProgress.IsComplete || !_hero.IsAlive)
                return;

            bool canAdvance = !_choices.IsOpen;
            if (IsInNonCombatRoom)
            {
                if (!canAdvance)
                    return;
                _nonCombatWait += Time.deltaTime;
                if (_nonCombatWait >= _advanceDelay)
                    AdvanceFloor();
                return;
            }

            if (_fight.Tick(Time.deltaTime, canAdvance))
                AdvanceFloor();
        }

        private void AdvanceFloor()
        {
            FloorStep step = _floorProgress.Advance();
            switch (step.Kind)
            {
                case FloorStepKind.Wave:
                    StartWave(_floor.RoomAt(step.RoomIndex).WaveAt(step.WaveIndex));
                    break;
                case FloorStepKind.NonCombatRoom:
                    EnterForge(_floor.RoomAt(step.RoomIndex));
                    break;
                default:
                    IsInNonCombatRoom = false;
                    ProgressChanged?.Invoke();
                    FloorCleared?.Invoke();
                    break;
            }
        }

        private void StartWave(EnemyDefinition definition)
        {
            ReleaseEnemy(true);
            IsInNonCombatRoom = false;

            Health enemy = Instantiate(definition.Prefab, transform.position, Quaternion.identity);
            enemy.name = definition.Prefab.name;
            enemy.Initialize(definition.MaximumHealth);
            enemy.GetComponent<Targeting>().SetCandidates(new[] { _hero });
            enemy.GetComponent<AttackController>().Initialize(definition.Weapon.CreateRuntime());
            enemy.Changed += OnEnemyChanged;
            enemy.Died += OnEnemyDied;
            _currentEnrage = enemy.GetComponent<EnrageBehaviour>();
            if (_currentEnrage != null)
                _currentEnrage.Enraged += OnEnemyEnraged;
            CurrentEnemy = enemy;
            CurrentDefinition = definition;
            _heroTargeting.SetCandidates(new[] { enemy });

            _fight.Begin();
            EncounterStarted?.Invoke();
            ProgressChanged?.Invoke();
        }

        private void EnterForge(RoomDefinition room)
        {
            ReleaseEnemy(true);
            CurrentEnemy = null;
            CurrentDefinition = null;
            _heroTargeting.SetCandidates(Array.Empty<Health>());
            IsInNonCombatRoom = true;
            _nonCombatWait = 0f;

            var options = new ForgeOption[room.ForgeOptionCount];
            for (int i = 0; i < options.Length; i++)
                options[i] = room.ForgeOptionAt(i).CreateOption();
            _forge.Open(options);
            ProgressChanged?.Invoke();
        }

        private void OnEnemyChanged()
        {
            _fight.RecordHit();
            ProgressChanged?.Invoke();
        }

        private void OnEnemyDied()
        {
            if (!_fight.Clear())
                return;

            EnemyDefeated?.Invoke(CurrentEnemy);
            ProgressChanged?.Invoke();
            if (_floorProgress.IsFinalWave)
                AdvanceFloor();
        }

        private void OnEnemyEnraged() => ProgressChanged?.Invoke();

        // Unsubscribing first guarantees an earlier enemy can never report into a later step.
        private void ReleaseEnemy(bool destroy)
        {
            Health enemy = CurrentEnemy;
            if (_currentEnrage != null)
                _currentEnrage.Enraged -= OnEnemyEnraged;
            _currentEnrage = null;
            if (ReferenceEquals(enemy, null))
                return;

            enemy.Changed -= OnEnemyChanged;
            enemy.Died -= OnEnemyDied;
            if (destroy && enemy != null)
                Destroy(enemy.gameObject);
        }

        private void OnDestroy() => ReleaseEnemy(false);
    }
}
