using System;
using System.Collections.Generic;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Runs the Descent one floor at a time: spawns each wave as a pack of up to PackLayout.MaxPackSize enemies with
    // floor-scaled health, damage and gold round the hero, at least a body apart, walks it in toward the hero, opens the
    // forge in non-combat rooms, and advances after a short delay once the current step is resolved, no choice is open
    // and the hero lives. The final wave's last kill clears the floor immediately. It runs before other scripts so
    // enemies move before anyone attacks in a frame, the same order the Descent simulation uses.
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
        // The platform the packs enter on and stop on.
        [SerializeField] private ArenaView _arena;
        [SerializeField, Min(0f)] private float _advanceDelay = 1f;
        // On the arena floor, whose centre is the origin: how far from the hero a pack enters at its corner, the unit of
        // its formation, and how close two enemies may come while walking in.
        [SerializeField, Min(0.5f)] private float _entryDepth = 5f;
        [SerializeField, Min(0.1f)] private float _formationSpacing = 1f;
        [SerializeField, Min(0f)] private float _bodySpacing = 0.9f;
        private readonly List<SpawnedEnemy> _wave = new List<SpawnedEnemy>();
        private readonly EntryPlacement[] _placements = new EntryPlacement[PackLayout.MaxPackSize];
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
        // Waves started on the current floor; each starts one corner further round the arena.
        private int _waveOrdinal;

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
                    float x = _motion.XOf(i) - _motion.HeroX;
                    float y = _motion.YOf(i) - _motion.HeroY;
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
            if (_floor == null || _hero == null || _heroTargeting == null || _arena == null)
                throw new InvalidOperationException("EncounterController needs a first floor, the hero, hero targeting and the arena.");
            if (_floorProgress != null)
                throw new InvalidOperationException("EncounterController has already been initialized.");

            _choices = choices ?? throw new ArgumentNullException(nameof(choices));
            _forge = forge ?? throw new ArgumentNullException(nameof(forge));
            // The Editor and development builds may start the Descent on a floor from Resources/Development instead of the
            // one this scene carries, so a scene test or a phone session can reach a proof encounter without editing
            // Gameplay.unity. A release build, and anything without that file, always gets the authored floor back. It is
            // resolved before validation, so whatever the run actually descends is the chain that gets checked.
            FloorDefinition first = DevelopmentStart.FirstFloor(_floor);
            var visited = new HashSet<FloorDefinition>();
            for (FloorDefinition floor = first; floor != null; floor = floor.NextFloor)
            {
                if (!visited.Add(floor))
                    throw new InvalidOperationException($"Floor {floor.name} links back into the descent.");
                ValidateFloor(floor);
            }

            _fight = new EncounterProgress(_advanceDelay);
            _currentFloor = first;
            FloorNumber = 1;
            _floorProgress = new FloorProgress(WavesOf(_currentFloor));
            _waveOrdinal = 0;
            AdvanceFloor();
        }

        // The hero's position on the arena floor, which the packs walk toward.
        public float HeroFloorX => _hero.transform.position.x;
        public float HeroFloorY => ArenaFloor.FloorY(_hero.transform.position.y);

        public void DescendToNextFloor()
        {
            if (!IsFloorCleared || !HasNextFloor || _descending)
                throw new InvalidOperationException("Descending requires a cleared floor with a next floor.");

            _roomsClearedOnEarlierFloors += _floorProgress.RoomsCleared;
            _currentFloor = _currentFloor.NextFloor;
            FloorNumber++;
            _floorProgress = new FloorProgress(WavesOf(_currentFloor));
            _waveOrdinal = 0;
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

        // Timed as a whole for performance sessions (FrameTimeProbe): instantiating, drawing and setting up every enemy and
        // everything that answers the new wave.
        private void StartWave(WaveDefinition waveDefinition)
        {
            using (PerformanceMarkers.StartWave.Auto())
                SpawnWave(waveDefinition);
        }

        private void SpawnWave(WaveDefinition waveDefinition)
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
            // The pack forms up round wherever the hero stands now, on the corners the platform leaves open.
            float heroX = HeroFloorX;
            float heroY = HeroFloorY;
            _motion = new PackMotion(_bodySpacing, PackLayout.HalfWidth * _formationSpacing, _arena.Geometry, heroX, heroY);
            // Spots keep the same body spacing the pack keeps while it walks, so no two enemies enter overlapping. The
            // Descent simulation passes the same value, so scene and simulation lay a wave out identically; with the hero
            // near the centre and a pack the formations already keep 1.1 spacing units apart, it changes nothing.
            EntrySides.Place(_waveOrdinal, count, heroX, heroY, _arena.Geometry, _entryDepth, _formationSpacing, _bodySpacing,
                _placements);
            for (int i = 0; i < count; i++)
            {
                EnemyDefinition definition = waveDefinition.EnemyAt(i);
                Health enemy = Instantiate(definition.Prefab, Vector3.zero, Quaternion.identity);
                enemy.name = count == 1 ? definition.Prefab.name : $"{definition.Prefab.name} {i + 1}";
                enemy.Initialize(FloorScaling.Health(definition.MaximumHealth, _currentFloor.EnemyHealthMultiplier, healthPercent));
                WeaponRuntime weapon = definition.Weapon.CreateRuntime();
                if (damageBonus != 0f)
                    weapon.AddModifier(WeaponStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
                enemy.GetComponent<Targeting>().SetCandidates(new[] { _hero });
                enemy.GetComponent<AttackController>().Initialize(weapon);
                int slot = _motion.Add(enemy, _placements[i], definition.MoveSpeed, weapon.Range);
                enemy.transform.position = WorldPosition(_motion.XOf(slot), _motion.YOf(slot));

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
            _waveOrdinal++;
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

        // Walks the living pack toward wherever the hero stands now and places each enemy at its floor position. Nothing
        // moves on the frame a wave spawns or while time is frozen.
        private void MovePack(float deltaTime)
        {
            if (_motion == null || IsCleared || deltaTime <= 0f)
                return;

            _motion.Step(deltaTime, HeroFloorX, HeroFloorY);
            for (int i = 0; i < _wave.Count; i++)
            {
                Health enemy = _wave[i].Health;
                if (!_wave[i].Defeated && enemy != null)
                    enemy.transform.position = WorldPosition(_motion.XOf(i), _motion.YOf(i));
            }
        }

        // The arena's centre is both the floor and the world origin, so floor and world positions convert exactly.
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
