using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Economy;
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
            // Its place in the pack, which a continuous swarm fills again once the enemy has fallen.
            public int Slot;
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
        private readonly List<EnemyDefinition> _waveEnemies = new List<EnemyDefinition>(PackLayout.MaxPackSize);
        private readonly EntryPlacement[] _placements = new EntryPlacement[PackLayout.MaxPackSize];
        private PackMotion _motion;
        private RunChoices _choices;
        private ForgeService _forge;
        // The run's difficulty, which every evolution raises (EvolutionRule). Null until Initialize, and 1 for any
        // caller that never hands one over, so a scene or a test without a stat sheet fights the floor as authored.
        private Func<float> _difficulty;
        private FloorDefinition _currentFloor;
        private FloorProgress _floorProgress;
        private EncounterProgress _fight;
        private SpawnedEnemy _lastDefeated;
        private float _transitionWait;
        private bool _descending;
        private bool _firstEncounterPending;
        // A continuous swarm (docs/27, S6c): the director SwarmDirector says what walks in and when, the room's ladder says
        // who, and the exit is where the hero leaves by standing still. Null outside a swarm room.
        private SwarmDirector _swarm;
        private RoomDefinition _swarmRoom;
        private UpgradeService _upgrades;
        private GroundDrops _drops;
        private readonly List<SwarmDirector.Spawn> _swarmSpawns = new List<SwarmDirector.Spawn>(PackLayout.MaxPackSize);
        // The hero's targets by place, so a refilled place is a refilled candidate and the earlier place still wins a tie.
        private readonly Health[] _swarmCandidates = new Health[PackLayout.MaxPackSize];
        private ExitDwell _exitDwell;
        private float _exitX;
        private float _exitY;
        // Where the hero stood at the end of the last swarm frame, to tell whether it moved in this one: it has only held
        // the exit while it stood still.
        private float _swarmHeroX;
        private float _swarmHeroY;
        private int _swarmStep = -1;
        private bool _gateExitOpen;
        private bool _roomSiteOpen;
        private float _roomSiteX;
        private float _roomSiteY;
        private FloorStep _roomSiteStep;
        private bool _revealHold;
        private int _roomsClearedOnEarlierFloors;
        // The districts the run's earlier floors opened, so the next floor's halls go on round from where they left off.
        private int _hallsOnEarlierFloors;
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
        public int WaveCount => CurrentRoom != null ? WavesIn(_currentFloor, CurrentRoom) : 0;
        public int RoomsCleared => _floorProgress?.RoomsCleared ?? 0;
        public int TotalRoomsCleared => _roomsClearedOnEarlierFloors + RoomsCleared;
        public bool IsFloorCleared => _floorProgress != null && _floorProgress.IsComplete;
        public bool IsDescending => _descending;
        public bool IsInNonCombatRoom { get; private set; }
        public int WaveEnemyCount => _wave.Count;
        public int AliveEnemyCount { get; private set; }
        public bool IsFirstEncounterPending => _firstEncounterPending;
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
        // A swarm room's state, for whoever reports or shows it: whether this room is one, how long the hero has stood in
        // it, which step of the ramp that is (one-based - what a wave number used to mean), how much harder an enemy
        // walking in now is than the floor authored, and where the way out stands and how long it has been held.
        public bool IsInSwarmRoom => _swarm != null;
        public float SwarmSeconds => _swarm != null ? _swarm.Seconds : 0f;
        public int SwarmWave => _swarm != null ? (int)(_swarm.Seconds / SwarmRule.StepSeconds) + 1 : 0;
        public float SwarmDifficulty => _swarm != null ? SwarmRule.DifficultyAtSeconds(_swarm.Seconds) : 1f;
        // On the foundry a floor that ends in a boss is left through the gate: once the boss falls the gate opens and the
        // floor clears only when the hero walks to it and holds still there (ExitRule), so the climb is a walk out.
        public bool IsGateExitOpen => _gateExitOpen;
        // Either way out the hero currently has to walk to: the finale's or the opened gate's.
        public bool IsExitOpen => _swarm != null || _gateExitOpen;
        public float ExitFloorX => _exitX;
        public float ExitFloorY => _exitY;
        public float ExitHeldSeconds => _exitDwell != null ? _exitDwell.Held : 0f;
        public float ExitRadius => _gateExitOpen ? ExitRule.GateRadius : ExitRule.Radius;
        // On the foundry a floor's later combat rooms open at their own districts (FoundryMap.RoomSite): the room waits,
        // marked, until the hero walks onto it, and its pack then forms up round the hero there.
        public bool IsRoomSiteOpen => _roomSiteOpen;
        public float RoomSiteFloorX => _roomSiteX;
        public float RoomSiteFloorY => _roomSiteY;
        // A marked hall has been walked onto; how long it stood marked first.
        public event Action<float> HallReached;
        private float _roomSiteWait;

        public event Action EncounterStarted;
        public event Action ProgressChanged;
        public event Action<Health> EnemyDefeated;
        public event Action FloorCleared;
        // The foundry's boss has walked out of the gate (BossRevealView turns the camera to it).
        public event Action GateBossArrived;
        // While a view presents the arriving boss, the encounter holds still: no pack moves, no fight clock runs.
        public bool IsRevealHeld => _revealHold;
        // Floor points where the pack cannot walk (PackMotion.Held); the foundry pit sets it, nothing else does.
        public Func<float, float, bool> PackHeld { get; set; }
        public void HoldForReveal(bool hold) => _revealHold = hold;
        // A swarm room's ramp has moved to its next step, and the hero has walked out of a swarm room: for telemetry.
        public event Action SwarmStepped;
        public event Action SwarmLeft;

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

        // upgrades is what a swarm room holds level-ups back on and opens its sittings with; without it a swarm offers each
        // level-up as it comes. drops are the Descent's rewards on the floor, which the hero picks up as it walks.
        public void Initialize(RunChoices choices, ForgeService forge, Func<float> difficulty = null,
            UpgradeService upgrades = null, GroundDrops drops = null, bool deferFirstEncounter = false)
        {
            _difficulty = difficulty;
            _upgrades = upgrades;
            _drops = drops;
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
            _hallsOnEarlierFloors = 0;
            _floorProgress = new FloorProgress(WavesOf(_currentFloor), EndlessOf(_currentFloor));
            _waveOrdinal = 0;
            _firstEncounterPending = true;
            if (!deferFirstEncounter)
                BeginFirstEncounter();
        }

        // Arrival presentation may hold only the initial step. Validation and floor setup have already completed;
        // repeated completion/skip callbacks cannot spawn another wave or advance a later floor.
        public void BeginFirstEncounter()
        {
            if (_floorProgress == null)
                throw new InvalidOperationException("EncounterController must be initialized before its first encounter begins.");
            if (!_firstEncounterPending)
                return;

            // Clear before events fire: an EncounterStarted subscriber may call this method again.
            _firstEncounterPending = false;
            AdvanceFloor();
        }

        // The hero's position on the arena floor, which the packs walk toward.
        // Where an enemy of the pack on the floor stands, on the pack's own floor coordinates - what a drop falls at.
        public bool TryFloorPositionOf(Health enemy, out float floorX, out float floorY)
        {
            floorX = 0f;
            floorY = 0f;
            if (enemy == null || _motion == null)
                return false;
            for (int i = 0; i < _wave.Count; i++)
            {
                SpawnedEnemy spawned = _wave[i];
                if (spawned == null || !ReferenceEquals(spawned.Health, enemy) || spawned.Slot >= _motion.Count)
                    continue;
                floorX = _motion.XOf(spawned.Slot);
                floorY = _motion.YOf(spawned.Slot);
                return true;
            }
            return false;
        }

        public float HeroFloorX => _hero.transform.position.x;
        public float HeroFloorY => ArenaFloor.FloorY(_hero.transform.position.y);

        public void DescendToNextFloor()
        {
            if (!IsFloorCleared || !HasNextFloor || _descending)
                throw new InvalidOperationException("Descending requires a cleared floor with a next floor.");

            _roomsClearedOnEarlierFloors += _floorProgress.RoomsCleared;
            _hallsOnEarlierFloors += LaterHalls(_currentFloor);
            _currentFloor = _currentFloor.NextFloor;
            FloorNumber++;
            _floorProgress = new FloorProgress(WavesOf(_currentFloor), EndlessOf(_currentFloor));
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

        private int[] WavesOf(FloorDefinition floor)
        {
            var waves = new int[floor.RoomCount];
            for (int i = 0; i < waves.Length; i++)
                waves[i] = floor.RoomAt(i).Kind == RoomKind.Forge ? 0 : WavesIn(floor, floor.RoomAt(i));
            return waves;
        }

        // A room's waves: its authored ones, and on the foundry a combat room's surges after them (HallSurgeRule).
        private int WavesIn(FloorDefinition floor, RoomDefinition room) => room.WaveCount + SurgesIn(floor, room);

        private int SurgesIn(FloorDefinition floor, RoomDefinition room) =>
            room.Kind == RoomKind.Combat && _arena != null && _arena.IsFoundryRegion ? floor.FoundryHallSurges : 0;

        // The enemies of a room's wave into _waveEnemies: an authored wave as authored, a surge as the wave it repeats
        // with that wave's last enemy added until the surge's size.
        private List<EnemyDefinition> WaveEnemies(RoomDefinition room, int waveIndex)
        {
            _waveEnemies.Clear();
            int authored = room.WaveCount;
            int surge = waveIndex - authored;
            WaveDefinition wave = room.WaveAt(surge < 0 ? waveIndex : HallSurgeRule.SourceWave(authored, surge));
            for (int e = 0; e < wave.EnemyCount; e++)
                _waveEnemies.Add(wave.EnemyAt(e));
            if (surge >= 0)
                for (int e = wave.EnemyCount; e < HallSurgeRule.Size(wave.EnemyCount, surge); e++)
                    _waveEnemies.Add(wave.EnemyAt(wave.EnemyCount - 1));
            return _waveEnemies;
        }

        // A swarm room's waves are a ladder rather than a list that finishes, so the floor never clears it on its own.
        private static bool[] EndlessOf(FloorDefinition floor)
        {
            var endless = new bool[floor.RoomCount];
            for (int i = 0; i < endless.Length; i++)
                endless[i] = floor.RoomAt(i).Kind == RoomKind.Swarm;
            return endless;
        }

        private void Update()
        {
            if (_floorProgress == null || _firstEncounterPending || _revealHold || !_hero.IsAlive)
                return;

            MovePack(Time.deltaTime);
            if (_swarm != null)
            {
                TickSwarm(Time.deltaTime);
                return;
            }

            // The Descent's drops, read where the hero has just walked: after it moves and before the chest is read or
            // anything strikes, on every frame of the floor - the pauses between waves and a forge's included, where a
            // player walks to fetch them. A frozen frame is no frame, as the simulation has none while a choice is open.
            if (_drops != null && Time.deltaTime > 0f)
                _drops.CollectNear(HeroFloorX, HeroFloorY);

            // A marked room waits for the hero to walk onto its district; an open choice keeps it waiting.
            if (_roomSiteOpen)
            {
                _roomSiteWait += Time.deltaTime;
                if (!_choices.IsOpen && ExitRule.IsWithinReach(HeroFloorX, HeroFloorY, _roomSiteX, _roomSiteY, FoundryMap.RoomSiteRadius))
                {
                    _roomSiteOpen = false;
                    HallReached?.Invoke(_roomSiteWait);
                    RoomDefinition sited = _currentFloor.RoomAt(_roomSiteStep.RoomIndex);
                    StartWave(WaveEnemies(sited, _roomSiteStep.WaveIndex), false, sited.Kind == RoomKind.Boss);
                }
                return;
            }

            // The opened gate waits for the hero: nothing else advances, and an open choice freezes the hold.
            if (_gateExitOpen)
            {
                if (Time.deltaTime > 0f && !_choices.IsOpen)
                    TickGateExit(Time.deltaTime);
                return;
            }

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

            if (_floorProgress.IsComplete || !_fight.Tick(Time.deltaTime, canAdvance))
                return;

            AdvanceFloor();
        }

        // One frame of a continuous swarm, at the point in the frame the Descent simulation's swarm loop mirrors: after
        // the pack has stepped and before anything strikes. A frozen frame is no frame at all - the simulation has none
        // while a choice is open - so nothing here moves then, not even the room's clock.
        private void TickSwarm(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            _swarm.Tick(deltaTime, AliveEnemyCount, _swarmSpawns);
            for (int i = 0; i < _swarmSpawns.Count; i++)
                SpawnSwarmEnemy(_swarmSpawns[i]);
            ReportSwarmStep();

            // The way out, read where the hero stands now; it counts only while the hero stood still this frame, which is
            // the position not having changed since the last one. Leaving ends the room before anything strikes.
            float heroX = HeroFloorX;
            float heroY = HeroFloorY;
            bool moved = heroX != _swarmHeroX || heroY != _swarmHeroY;
            _swarmHeroX = heroX;
            _swarmHeroY = heroY;
            if (_exitDwell.Step(deltaTime, !moved && ExitRule.IsWithinReach(heroX, heroY, _exitX, _exitY)))
            {
                LeaveSwarm();
                return;
            }
            // The level-ups the room has held back, offered together; the offer pauses the game from the next frame, as
            // every level-up does.
            if (_swarm.OfferDue)
                _upgrades?.OpenSitting();
            if (_swarmSpawns.Count > 0)
                ProgressChanged?.Invoke();
        }

        private void ReportSwarmStep()
        {
            int step = (int)(_swarm.Seconds / SwarmRule.StepSeconds);
            if (step == _swarmStep)
                return;
            _swarmStep = step;
            SwarmStepped?.Invoke();
        }

        // A swarm room opens: one pack for the room's whole life, the ladder's first wave walking in, the way out placed,
        // and the encounter started once - so the chest, the bar and the room's telemetry all see one room, not a wave.
        private void BeginSwarm(RoomDefinition room)
        {
            ReleaseWave(true);
            IsInNonCombatRoom = false;
            _swarmRoom = room;
            var sizes = new int[room.WaveCount];
            for (int i = 0; i < sizes.Length; i++)
                sizes[i] = room.WaveAt(i).EnemyCount;
            _swarm = new SwarmDirector(sizes);
            _motion = new PackMotion(_bodySpacing, PackLayout.HalfWidth * _formationSpacing, _arena.Geometry, HeroFloorX,
                HeroFloorY);
            Array.Clear(_swarmCandidates, 0, _swarmCandidates.Length);
            ExitRule.SpotFor(_arena.Geometry, out _exitX, out _exitY);
            _exitDwell = new ExitDwell();
            _swarmHeroX = HeroFloorX;
            _swarmHeroY = HeroFloorY;
            _swarmStep = -1;
            AliveEnemyCount = 0;
            _upgrades?.Hold();

            _swarm.Open(_swarmSpawns);
            for (int i = 0; i < _swarmSpawns.Count; i++)
                SpawnSwarmEnemy(_swarmSpawns[i]);
            _heroTargeting.SetCandidates(_swarmCandidates);
            _fight.Begin();
            ReportSwarmStep();
            EncounterStarted?.Invoke();
            ProgressChanged?.Invoke();
        }

        // One enemy walking into one place: its own one-enemy entry, one corner further round the arena than the last,
        // placed round wherever the hero stands now, at the difficulty the director gave it. A place whose enemy has
        // fallen has its body taken away as the new one arrives.
        private void SpawnSwarmEnemy(SwarmDirector.Spawn spawn)
        {
            EnemyDefinition definition = _swarmRoom.WaveAt(spawn.Wave).EnemyAt(spawn.Index);
            EntrySides.Place(_waveOrdinal, 1, HeroFloorX, HeroFloorY, _arena.Geometry, _entryDepth, _formationSpacing,
                _bodySpacing, _placements);
            _waveOrdinal++;
            FloorModifierDefinition modifier = _currentFloor.Modifier;
            float healthPercent = modifier != null ? modifier.EnemyHealthPercent : 0f;
            float damagePercent = modifier != null ? modifier.EnemyDamagePercent : 0f;
            float goldPercent = modifier != null ? modifier.GoldPercent : 0f;
            float scale = spawn.Difficulty * RunDifficulty;
            float damageBonus = FloorScaling.DamageBonus(FloorDamageMultiplier * scale, damagePercent);
            SpawnedEnemy enemy = CreateEnemy(definition, $"{definition.Prefab.name} {spawn.Slot + 1}", FloorHealthMultiplier(false) * scale, healthPercent,
                damageBonus, goldPercent, out float reach);
            enemy.Slot = spawn.Slot;

            if (spawn.Slot < _motion.Count)
            {
                RetireFallen(_wave[spawn.Slot]);
                _motion.Replace(spawn.Slot, enemy.Health, _placements[0], definition.MoveSpeed, reach);
                _wave[spawn.Slot] = enemy;
            }
            else
            {
                _motion.Add(enemy.Health, _placements[0], definition.MoveSpeed, reach);
                _wave.Add(enemy);
            }
            enemy.Health.transform.position = WorldPosition(_motion.XOf(spawn.Slot), _motion.YOf(spawn.Slot));
            _swarmCandidates[spawn.Slot] = enemy.Health;
            AliveEnemyCount++;
        }

        // A fallen enemy's body leaves as its place is filled again: a second after it fell, long enough to be seen to.
        private void RetireFallen(SpawnedEnemy fallen)
        {
            if (fallen == null)
                return;
            if (ReferenceEquals(_lastDefeated, fallen))
                _lastDefeated = null;
            if (fallen.Enrage != null)
                fallen.Enrage.Enraged -= OnEnemyEnraged;
            if (ReferenceEquals(fallen.Health, null))
                return;
            fallen.Health.Changed -= OnEnemyChanged;
            fallen.Health.Died -= fallen.OnDied;
            if (fallen.Health != null)
                Destroy(fallen.Health.gameObject);
        }

        // The hero stood still on the way out long enough: the room ends, the pack goes with it, and the floor moves on.
        private void LeaveSwarm()
        {
            SwarmLeft?.Invoke();
            _swarm = null;
            _swarmRoom = null;
            _floorProgress.LeaveEndlessRoom();
            ReleaseWave(true);
            Array.Clear(_swarmCandidates, 0, _swarmCandidates.Length);
            _heroTargeting.SetCandidates(Array.Empty<Health>());
            // Whatever the room still held back is offered now, before the floor moves on.
            _upgrades?.Release();
            AdvanceFloor();
        }

        private void AdvanceFloor()
        {
            FloorStep step = _floorProgress.Advance();
            switch (step.Kind)
            {
                case FloorStepKind.Wave:
                {
                    RoomDefinition room = _currentFloor.RoomAt(step.RoomIndex);
                    if (room.Kind == RoomKind.Swarm)
                        BeginSwarm(room);
                    else if (step.WaveIndex == 0 && TryRoomSite(step.RoomIndex))
                    {
                        ReleaseWave(true);
                        _roomSiteStep = step;
                        _roomSiteOpen = true;
                        _roomSiteWait = 0f;
                        ProgressChanged?.Invoke();
                    }
                    else
                        StartWave(WaveEnemies(room, step.WaveIndex), GateRoom(room), room.Kind == RoomKind.Boss);
                    break;
                }
                case FloorStepKind.NonCombatRoom:
                    EnterForge(_currentFloor.RoomAt(step.RoomIndex));
                    break;
                default:
                    IsInNonCombatRoom = false;
                    if (GateClosesFloor())
                    {
                        OpenGateExit();
                        break;
                    }
                    ProgressChanged?.Invoke();
                    FloorCleared?.Invoke();
                    break;
            }
        }

        // Timed as a whole for performance sessions (FrameTimeProbe): instantiating, drawing and setting up every enemy and
        // everything that answers the new wave.
        // What every enemy multiplies its authored health and damage by beyond its floor: the run's difficulty, raised
        // only by taking an evolution.
        public float RunDifficulty => _difficulty != null ? _difficulty() : 1f;

        private void StartWave(List<EnemyDefinition> waveDefinition, bool atGate = false, bool boss = false)
        {
            using (PerformanceMarkers.StartWave.Auto())
                SpawnWave(waveDefinition, atGate, boss);
        }

        // The floor's tier: authored, or the climb's formula by the floor's height with a boss room's own hardening.
        private float FloorHealthMultiplier(bool boss) => _currentFloor.ClimbScaled
            ? ClimbScaling.Health(FloorNumber) * (boss ? ClimbScaling.Boss(FloorNumber) : 1f)
            : _currentFloor.EnemyHealthMultiplier;

        private float FloorDamageMultiplier =>
            _currentFloor.ClimbScaled ? ClimbScaling.Damage(FloorNumber) : _currentFloor.EnemyDamageMultiplier;

        // The gate is far from the court: its pack marches across at GateMarchFactor times its speed until within
        // GateMarchRange of the hero, so a waiting hero meets the boss in about ten seconds instead of twenty.
        public const float GateMarchFactor = 3.5f, GateMarchRange = 4f;

        // A boss room on the foundry: its pack walks out of the gate instead of entering round the hero.
        private bool GateRoom(RoomDefinition room) =>
            room.Kind == RoomKind.Boss && _arena != null && _arena.IsFoundryRegion;

        // A foundry floor's combat room after its first opens at its own district, the round going on from the
        // districts the run's earlier floors opened.
        private bool TryRoomSite(int roomIndex)
        {
            if (_arena == null || !_arena.IsFoundryRegion || !IsHallKind(_currentFloor.RoomAt(roomIndex).Kind))
                return false;
            int ordinal = 0;
            for (int i = 0; i < roomIndex; i++)
            {
                if (IsHallKind(_currentFloor.RoomAt(i).Kind))
                    ordinal++;
            }
            if (ordinal == 0)
                return false;
            FoundryMap.RoomSite(_hallsOnEarlierFloors, ordinal, out _roomSiteX, out _roomSiteY);
            return true;
        }

        private static bool IsHallKind(RoomKind kind) => kind == RoomKind.Combat || kind == RoomKind.Elite;

        // The rooms of a floor that open at a district: its combat and elite rooms after the first.
        private static int LaterHalls(FloorDefinition floor)
        {
            int halls = 0;
            for (int i = 0; i < floor.RoomCount; i++)
            {
                if (IsHallKind(floor.RoomAt(i).Kind))
                    halls++;
            }
            return Math.Max(0, halls - 1);
        }

        private bool GateClosesFloor() =>
            _currentFloor.RoomCount > 0 && GateRoom(_currentFloor.RoomAt(_currentFloor.RoomCount - 1));

        private void OpenGateExit()
        {
            _gateExitOpen = true;
            FoundryMap.GateExitSpot(out _exitX, out _exitY);
            _exitDwell = new ExitDwell();
            _swarmHeroX = HeroFloorX;
            _swarmHeroY = HeroFloorY;
            ProgressChanged?.Invoke();
        }

        // The same hold as the finale's exit: it counts only while the hero stood still on it this frame.
        private void TickGateExit(float deltaTime)
        {
            float heroX = HeroFloorX;
            float heroY = HeroFloorY;
            bool moved = heroX != _swarmHeroX || heroY != _swarmHeroY;
            _swarmHeroX = heroX;
            _swarmHeroY = heroY;
            if (!_exitDwell.Step(deltaTime, !moved && ExitRule.IsWithinReach(heroX, heroY, _exitX, _exitY, ExitRule.GateRadius)))
                return;
            _gateExitOpen = false;
            ProgressChanged?.Invoke();
            FloorCleared?.Invoke();
        }

        private void SpawnWave(List<EnemyDefinition> waveDefinition, bool atGate = false, bool boss = false)
        {
            ReleaseWave(true);
            IsInNonCombatRoom = false;
            FloorModifierDefinition modifier = _currentFloor.Modifier;
            float healthPercent = modifier != null ? modifier.EnemyHealthPercent : 0f;
            float damagePercent = modifier != null ? modifier.EnemyDamagePercent : 0f;
            float goldPercent = modifier != null ? modifier.GoldPercent : 0f;
            // An ordinary wave hardens only by the run's difficulty, which every evolution raises.
            float scale = RunDifficulty;
            float damageBonus = FloorScaling.DamageBonus(FloorDamageMultiplier * scale, damagePercent);
            float floorHealth = FloorHealthMultiplier(boss);

            int count = waveDefinition.Count;
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
            if (atGate)
                FoundryMap.PlaceAtGate(count, _placements);
            for (int i = 0; i < count; i++)
            {
                EnemyDefinition definition = waveDefinition[i];
                SpawnedEnemy spawn = CreateEnemy(definition,
                    count == 1 ? definition.Prefab.name : $"{definition.Prefab.name} {i + 1}", floorHealth * scale,
                    healthPercent, damageBonus, goldPercent, out float reach);
                int slot = _motion.Add(spawn.Health, _placements[i], definition.MoveSpeed, reach);
                if (atGate)
                    _motion.SetMarch(slot, GateMarchFactor, GateMarchRange);
                spawn.Slot = slot;
                spawn.Health.transform.position = WorldPosition(_motion.XOf(slot), _motion.YOf(slot));
                _wave.Add(spawn);
                candidates[i] = spawn.Health;
            }

            AliveEnemyCount = count;
            _waveOrdinal++;
            _heroTargeting.SetCandidates(candidates);
            _fight.Begin();
            EncounterStarted?.Invoke();
            ProgressChanged?.Invoke();
            if (atGate)
                GateBossArrived?.Invoke();
        }

        // One enemy, built and wired but not yet placed: the setup a wave and a swarm share, so the two can never drift apart.
        // Its weapon's reach comes back for the pack, which stops each enemy just inside it.
        // healthScale is the floor's health tier times the difficulty the enemy walks in at.
        private SpawnedEnemy CreateEnemy(EnemyDefinition definition, string name, float healthScale, float healthPercent,
            float damageBonus, float goldPercent, out float reach)
        {
            Health enemy = Instantiate(definition.Prefab, Vector3.zero, Quaternion.identity);
            enemy.name = name;
            enemy.Initialize(FloorScaling.Health(definition.MaximumHealth, healthScale,
                healthPercent));
            WeaponRuntime weapon = definition.Weapon.CreateRuntime();
            if (damageBonus != 0f)
                weapon.AddModifier(UpgradeStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
            enemy.GetComponent<Targeting>().SetCandidates(new[] { _hero });
            enemy.GetComponent<AttackController>().Initialize(weapon);
            reach = weapon.Range;

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
            return spawn;
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
            // A swarm's place fills again after its breath; the room itself never clears, however empty it gets.
            if (_swarm != null)
            {
                _swarm.Fell(enemy.Slot);
                EnemyDefeated?.Invoke(enemy.Health);
                ProgressChanged?.Invoke();
                return;
            }
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

            _motion.Held = PackHeld;
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
