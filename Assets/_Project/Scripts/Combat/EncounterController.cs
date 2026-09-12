using System;
using System.Collections.Generic;
using Cryptforge.Content;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Runs the Descent one floor at a time: spawns each wave's enemy with floor-scaled health, damage and gold, opens the
    // forge in non-combat rooms, and advances after a short delay once the current step is resolved, no choice is open
    // and the hero lives. The final wave's kill clears the floor immediately; descending loads the next floor.
    public sealed class EncounterController : MonoBehaviour
    {
        [SerializeField] private FloorDefinition _floor;
        [SerializeField] private Health _hero;
        [SerializeField] private Targeting _heroTargeting;
        [SerializeField, Min(0f)] private float _advanceDelay = 1f;
        private RunChoices _choices;
        private ForgeService _forge;
        private FloorDefinition _currentFloor;
        private FloorProgress _floorProgress;
        private EncounterProgress _fight;
        private EnrageBehaviour _currentEnrage;
        private float _transitionWait;
        private bool _descending;
        private int _roomsClearedOnEarlierFloors;

        public FloorDefinition Floor => _currentFloor;
        public int FloorNumber { get; private set; }
        public bool HasNextFloor => _currentFloor != null && _currentFloor.NextFloor != null;
        public RoomDefinition CurrentRoom =>
            _floorProgress != null && _floorProgress.RoomIndex >= 0 ? _currentFloor.RoomAt(_floorProgress.RoomIndex) : null;
        public int RoomNumber => _floorProgress != null ? _floorProgress.RoomIndex + 1 : 0;
        public int RoomCount => _currentFloor != null ? _currentFloor.RoomCount : 0;
        public int WaveNumber => _floorProgress != null ? _floorProgress.WaveIndex + 1 : 0;
        public int WaveCount => CurrentRoom?.WaveCount ?? 0;
        public int RoomsCleared => _floorProgress?.RoomsCleared ?? 0;
        public int TotalRoomsCleared => _roomsClearedOnEarlierFloors + RoomsCleared;
        public bool IsFloorCleared => _floorProgress != null && _floorProgress.IsComplete;
        public bool IsDescending => _descending;
        public bool IsInNonCombatRoom { get; private set; }
        public Health CurrentEnemy { get; private set; }
        public EnemyDefinition CurrentDefinition { get; private set; }
        public int CurrentGoldReward { get; private set; }
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
            if (_floor == null || _hero == null || _heroTargeting == null)
                throw new InvalidOperationException("EncounterController needs a first floor, the hero and hero targeting.");
            if (_floorProgress != null)
                throw new InvalidOperationException("EncounterController has already been initialized.");

            _choices = choices ?? throw new ArgumentNullException(nameof(choices));
            _forge = forge ?? throw new ArgumentNullException(nameof(forge));
            var visited = new HashSet<FloorDefinition>();
            for (FloorDefinition floor = _floor; floor != null; floor = floor.NextFloor)
            {
                if (!visited.Add(floor))
                    throw new InvalidOperationException($"Floor {floor.name} links back into the descent.");
                ValidateFloor(floor);
            }

            _fight = new EncounterProgress(_advanceDelay);
            _currentFloor = _floor;
            FloorNumber = 1;
            _floorProgress = new FloorProgress(WavesOf(_currentFloor));
            AdvanceFloor();
        }

        public void DescendToNextFloor()
        {
            if (!IsFloorCleared || !HasNextFloor || _descending)
                throw new InvalidOperationException("Descending requires a cleared floor with a next floor.");

            _roomsClearedOnEarlierFloors += _floorProgress.RoomsCleared;
            _currentFloor = _currentFloor.NextFloor;
            FloorNumber++;
            _floorProgress = new FloorProgress(WavesOf(_currentFloor));
            _descending = true;
            _transitionWait = 0f;
            ProgressChanged?.Invoke();
        }

        // Fail at startup with the offending floor and room rather than mid-run when it is first reached.
        private static void ValidateFloor(FloorDefinition floor)
        {
            if (floor.RoomCount == 0)
                throw new InvalidOperationException($"Floor {floor.name} has no rooms.");
            FloorModifierDefinition modifier = floor.Modifier;
            if (modifier != null && (modifier.EnemyHealthPercent <= -1f || modifier.EnemyDamagePercent <= -1f || modifier.GoldPercent <= -1f))
                throw new InvalidOperationException($"Floor {floor.name} has a modifier that removes all health, damage or gold.");

            for (int index = 0; index < floor.RoomCount; index++)
            {
                RoomDefinition room = floor.RoomAt(index);
                if (room == null)
                    throw new InvalidOperationException($"Floor {floor.name} room {index} is missing.");

                if (room.Kind == RoomKind.Forge)
                {
                    if (room.WaveCount != 0 || room.ForgeOptionCount == 0)
                        throw new InvalidOperationException($"Forge room {index} needs forge options and no waves.");
                    for (int i = 0; i < room.ForgeOptionCount; i++)
                    {
                        if (room.ForgeOptionAt(i) == null)
                            throw new InvalidOperationException($"Forge room {index} has an empty option slot.");
                    }
                    continue;
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
            }
        }

        private static int[] WavesOf(FloorDefinition floor)
        {
            var waves = new int[floor.RoomCount];
            for (int i = 0; i < waves.Length; i++)
                waves[i] = floor.RoomAt(i).Kind == RoomKind.Forge ? 0 : floor.RoomAt(i).WaveCount;
            return waves;
        }

        private void Update()
        {
            if (_floorProgress == null || !_hero.IsAlive)
                return;

            bool canAdvance = !_choices.IsOpen;
            if (_descending || IsInNonCombatRoom)
            {
                if (!canAdvance)
                    return;
                _transitionWait += Time.deltaTime;
                if (_transitionWait < _advanceDelay)
                    return;
                _descending = false;
                AdvanceFloor();
                return;
            }

            if (!_floorProgress.IsComplete && _fight.Tick(Time.deltaTime, canAdvance))
                AdvanceFloor();
        }

        private void AdvanceFloor()
        {
            FloorStep step = _floorProgress.Advance();
            switch (step.Kind)
            {
                case FloorStepKind.Wave:
                    StartWave(_currentFloor.RoomAt(step.RoomIndex).WaveAt(step.WaveIndex));
                    break;
                case FloorStepKind.NonCombatRoom:
                    EnterForge(_currentFloor.RoomAt(step.RoomIndex));
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
            FloorModifierDefinition modifier = _currentFloor.Modifier;
            float healthPercent = modifier != null ? modifier.EnemyHealthPercent : 0f;
            float damagePercent = modifier != null ? modifier.EnemyDamagePercent : 0f;
            float goldPercent = modifier != null ? modifier.GoldPercent : 0f;

            Health enemy = Instantiate(definition.Prefab, transform.position, Quaternion.identity);
            enemy.name = definition.Prefab.name;
            enemy.Initialize(FloorScaling.Health(definition.MaximumHealth, _currentFloor.EnemyHealthMultiplier, healthPercent));
            WeaponRuntime weapon = definition.Weapon.CreateRuntime();
            float damageBonus = FloorScaling.DamageBonus(_currentFloor.EnemyDamageMultiplier, damagePercent);
            if (damageBonus != 0f)
                weapon.AddModifier(WeaponStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
            enemy.GetComponent<Targeting>().SetCandidates(new[] { _hero });
            enemy.GetComponent<AttackController>().Initialize(weapon);
            enemy.Changed += OnEnemyChanged;
            enemy.Died += OnEnemyDied;
            _currentEnrage = enemy.GetComponent<EnrageBehaviour>();
            if (_currentEnrage != null)
                _currentEnrage.Enraged += OnEnemyEnraged;
            CurrentEnemy = enemy;
            CurrentDefinition = definition;
            CurrentGoldReward = FloorScaling.Gold(definition.GoldReward, goldPercent);
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
            CurrentGoldReward = 0;
            _heroTargeting.SetCandidates(Array.Empty<Health>());
            IsInNonCombatRoom = true;
            _transitionWait = 0f;

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
