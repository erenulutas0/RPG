using System;
using System.Collections.Generic;
using Cryptforge.Content;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Runs the Descent one floor at a time: spawns each wave as a pack of up to seven enemies with floor-scaled health,
    // damage and gold at the far end of the arena, walks it in toward the hero, opens the forge in non-combat rooms, and
    // advances after a short delay once the current step is resolved, no choice is open and the hero lives. The final
    // wave's last kill clears the floor immediately. It runs before other scripts so enemies move before anyone attacks
    // in a frame, the same order the Descent simulation uses.
    [DefaultExecutionOrder(-50)]
    public sealed class EncounterController : MonoBehaviour
    {
        private sealed class SpawnedEnemy
        {
            public Health Health;
            public EnemyDefinition Definition;
            public int Gold;
            public EnrageBehaviour Enrage;
            public bool Defeated;
            public Action OnDied;
        }

        [SerializeField] private FloorDefinition _floor;
        [SerializeField] private Health _hero;
        [SerializeField] private Targeting _heroTargeting;
        [SerializeField, Min(0f)] private float _advanceDelay = 1f;
        // On the arena floor, with the hero at the origin: how far away a pack enters, the unit of its formation, and how
        // close two enemies may come while walking in.
        [SerializeField, Min(0.5f)] private float _entryDepth = 6f;
        [SerializeField, Min(0.1f)] private float _formationSpacing = 1f;
        [SerializeField, Min(0f)] private float _bodySpacing = 0.9f;
        private readonly List<SpawnedEnemy> _wave = new List<SpawnedEnemy>();
        private PackMotion _motion;
        private RunChoices _choices;
        private ForgeService _forge;
        private FloorDefinition _currentFloor;
        private FloorProgress _floorProgress;
        private EncounterProgress _fight;
        private SpawnedEnemy _lastDefeated;
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
        public int WaveEnemyCount => _wave.Count;
        public int AliveEnemyCount { get; private set; }
        // The living enemy nearest the hero on the floor, which the hero strikes once it is in reach (the earlier slot on equal
        // distance); after the wave falls, the last one defeated.
        public Health CurrentEnemy => CurrentSpawn?.Health;
        public EnemyDefinition CurrentDefinition => CurrentSpawn?.Definition;
        // The current wave's enemy with the most authored health, the earlier slot on a tie: a boss wave's boss, whichever
        // enemy fell last.
        public EnemyDefinition ToughestDefinition
        {
            get
            {
                EnemyDefinition toughest = null;
                for (int i = 0; i < _wave.Count; i++)
                {
                    if (toughest == null || _wave[i].Definition.MaximumHealth > toughest.MaximumHealth)
                        toughest = _wave[i].Definition;
                }
                return toughest;
            }
        }
        public EnemyDefinition EnragedDefinition => FindEnraged()?.Definition;
        public bool IsCurrentEnemyEnraged => FindEnraged() != null;
        public int EncounterNumber => _fight?.EncounterNumber ?? 0;
        public int HitsTaken => _fight?.HitsTaken ?? 0;
        public float Elapsed => _fight?.Elapsed ?? 0f;
        public bool IsCleared => _fight != null && _fight.IsCleared;

        public event Action EncounterStarted;
        public event Action ProgressChanged;
        public event Action<Health> EnemyDefeated;
        public event Action FloorCleared;

        private SpawnedEnemy CurrentSpawn
        {
            get
            {
                SpawnedEnemy nearest = null;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < _wave.Count; i++)
                {
                    if (_wave[i].Defeated)
                        continue;
                    float x = _motion.XOf(i);
                    float y = _motion.YOf(i);
                    float distance = x * x + y * y;
                    if (distance < nearestDistance)
                    {
                        nearest = _wave[i];
                        nearestDistance = distance;
                    }
                }
                return nearest ?? _lastDefeated;
            }
        }

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

        // The current wave's enemies in slot order, alive or defeated, until the next wave or room replaces them.
        public Health WaveEnemyAt(int index) => _wave[index].Health;

        // Null for an enemy that is not part of the current wave, for example the hero or a test's own damage.
        public EnemyDefinition DefinitionOf(IDamageable enemy) => Find(enemy)?.Definition;

        public int GoldRewardOf(IDamageable enemy) => Find(enemy)?.Gold ?? 0;

        // Fail at startup with the offending floor, room and wave rather than mid-run when it is first reached.
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
                    WaveDefinition wave = room.WaveAt(i);
                    if (wave == null || wave.EnemyCount < 1 || wave.EnemyCount > PackLayout.MaxPackSize)
                        throw new InvalidOperationException($"Room {index} wave {i} needs one to {PackLayout.MaxPackSize} enemies.");
                    for (int e = 0; e < wave.EnemyCount; e++)
                    {
                        EnemyDefinition enemy = wave.EnemyAt(e);
                        if (enemy == null || enemy.Weapon == null || enemy.Prefab == null ||
                            enemy.Prefab.GetComponent<AttackController>() == null || enemy.Prefab.GetComponent<Targeting>() == null)
                            throw new InvalidOperationException(
                                $"Room {index} wave {i} enemy {e} needs a definition with a weapon and a prefab carrying Health, Targeting and AttackController.");
                    }
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

            MovePack(Time.deltaTime);
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

        private void StartWave(WaveDefinition waveDefinition)
        {
            ReleaseWave(true);
            IsInNonCombatRoom = false;
            FloorModifierDefinition modifier = _currentFloor.Modifier;
            float healthPercent = modifier != null ? modifier.EnemyHealthPercent : 0f;
            float damagePercent = modifier != null ? modifier.EnemyDamagePercent : 0f;
            float goldPercent = modifier != null ? modifier.GoldPercent : 0f;
            float damageBonus = FloorScaling.DamageBonus(_currentFloor.EnemyDamageMultiplier, damagePercent);

            int count = waveDefinition.EnemyCount;
            var candidates = new Health[count];
            _motion = new PackMotion(_bodySpacing, PackLayout.HalfWidth * _formationSpacing);
            for (int i = 0; i < count; i++)
            {
                EnemyDefinition definition = waveDefinition.EnemyAt(i);
                PackLayout.Offset(i, count, _formationSpacing, out float floorX, out float floorY);
                floorY += _entryDepth;
                Health enemy = Instantiate(definition.Prefab, WorldPosition(floorX, floorY), Quaternion.identity);
                enemy.name = count == 1 ? definition.Prefab.name : $"{definition.Prefab.name} {i + 1}";
                enemy.Initialize(FloorScaling.Health(definition.MaximumHealth, _currentFloor.EnemyHealthMultiplier, healthPercent));
                WeaponRuntime weapon = definition.Weapon.CreateRuntime();
                if (damageBonus != 0f)
                    weapon.AddModifier(WeaponStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
                enemy.GetComponent<Targeting>().SetCandidates(new[] { _hero });
                enemy.GetComponent<AttackController>().Initialize(weapon);
                _motion.Add(enemy, floorX, floorY, definition.MoveSpeed, weapon.Range);

                var spawn = new SpawnedEnemy
                {
                    Health = enemy,
                    Definition = definition,
                    Gold = FloorScaling.Gold(definition.GoldReward, goldPercent),
                    Enrage = enemy.GetComponent<EnrageBehaviour>()
                };
                spawn.OnDied = () => OnEnemyDied(spawn);
                enemy.Changed += OnEnemyChanged;
                enemy.Died += spawn.OnDied;
                if (spawn.Enrage != null)
                    spawn.Enrage.Enraged += OnEnemyEnraged;
                _wave.Add(spawn);
                candidates[i] = enemy;
            }

            AliveEnemyCount = count;
            _heroTargeting.SetCandidates(candidates);
            _fight.Begin();
            EncounterStarted?.Invoke();
            ProgressChanged?.Invoke();
        }

        private void EnterForge(RoomDefinition room)
        {
            ReleaseWave(true);
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

        private void OnEnemyDied(SpawnedEnemy enemy)
        {
            if (enemy.Defeated)
                return;

            enemy.Defeated = true;
            _lastDefeated = enemy;
            AliveEnemyCount--;
            if (AliveEnemyCount == 0)
                _fight.Clear();

            EnemyDefeated?.Invoke(enemy.Health);
            ProgressChanged?.Invoke();
            if (AliveEnemyCount == 0 && _floorProgress.IsFinalWave)
                AdvanceFloor();
        }

        private void OnEnemyEnraged() => ProgressChanged?.Invoke();

        // Walks the living pack in and places each enemy at its floor position. Nothing moves on the frame a wave spawns or
        // while time is frozen.
        private void MovePack(float deltaTime)
        {
            if (_motion == null || IsCleared || deltaTime <= 0f)
                return;

            _motion.Step(deltaTime);
            for (int i = 0; i < _wave.Count; i++)
            {
                Health enemy = _wave[i].Health;
                if (!_wave[i].Defeated && enemy != null)
                    enemy.transform.position = WorldPosition(_motion.XOf(i), _motion.YOf(i));
            }
        }

        // The hero stands at the floor origin, which is the world origin, so floor and world positions convert exactly.
        private static Vector3 WorldPosition(float floorX, float floorY) => new Vector3(floorX, ArenaFloor.WorldY(floorY), 0f);

        private SpawnedEnemy Find(IDamageable enemy)
        {
            if (enemy == null)
                return null;
            for (int i = 0; i < _wave.Count; i++)
            {
                if (ReferenceEquals(_wave[i].Health, enemy))
                    return _wave[i];
            }
            return null;
        }

        private SpawnedEnemy FindEnraged()
        {
            for (int i = 0; i < _wave.Count; i++)
            {
                if (!_wave[i].Defeated && _wave[i].Enrage != null && _wave[i].Enrage.IsEnraged)
                    return _wave[i];
            }
            return null;
        }

        // Unsubscribing first guarantees an earlier enemy can never report into a later step.
        private void ReleaseWave(bool destroy)
        {
            for (int i = 0; i < _wave.Count; i++)
            {
                SpawnedEnemy spawn = _wave[i];
                if (spawn.Enrage != null)
                    spawn.Enrage.Enraged -= OnEnemyEnraged;
                if (ReferenceEquals(spawn.Health, null))
                    continue;

                spawn.Health.Changed -= OnEnemyChanged;
                spawn.Health.Died -= spawn.OnDied;
                if (destroy && spawn.Health != null)
                    Destroy(spawn.Health.gameObject);
            }

            _wave.Clear();
            _motion = null;
            _lastDefeated = null;
            AliveEnemyCount = 0;
        }

        private void OnDestroy() => ReleaseWave(false);
    }
}
