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
            public int Experience;
            public EnrageBehaviour Enrage;
            public bool Defeated;
            // When a horde enemy fell, on the horde's clock, so its body can be taken away a while after.
            public float FellAt;
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
        // The current floor's rooms as this arena plays them (RoomsAsPlayed), set with _currentFloor and only with it.
        private RoomDefinition[] _rooms = Array.Empty<RoomDefinition>();
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
        // The foundry floor's continuous horde (docs/27 decision 9, HordeDirector): its own pack, walking the map from the
        // floor's first step to its boss, beside the node waves' pack. Its enemies are the hero's targets with the wave's,
        // pay a share of their experience and no gold, and never count towards a wave being cleared. Null off the foundry,
        // on a floor with no horde roles, in a swarm room and once the gate is open.
        private HordeDirector _horde;
        private PackMotion _hordeMotion;
        // The gate boss has fallen below WardenRule.PhaseTwoAt: the boss room's horde thickens instead of thinning.
        private bool _bossEnraged;
        private int _hordeFloor;
        private int _hordeSeed;
        private float _hordeSeconds;
        private readonly List<SpawnedEnemy> _hordeEnemies = new List<SpawnedEnemy>(PackLayout.MaxPackSize);
        private readonly List<HordeDirector.Spawn> _hordeSpawns = new List<HordeDirector.Spawn>(4);
        // The hero's targets: the wave's (or the swarm's) as handed over, and while a horde stands, those and the horde's
        // living enemies together in one array the hero's targeting keeps.
        private Health[] _waveTargets = Array.Empty<Health>();
        private readonly Health[] _targets = new Health[PackLayout.MaxPackSize * 2];

        public FloorDefinition Floor => _currentFloor;
        public int FloorNumber { get; private set; }
        public bool HasNextFloor => _currentFloor != null && _currentFloor.NextFloor != null;
        public RoomDefinition CurrentRoom =>
            _floorProgress != null && _floorProgress.RoomIndex >= 0 ? _rooms[_floorProgress.RoomIndex] : null;
        public int RoomNumber => _floorProgress != null ? _floorProgress.RoomIndex + 1 : 0;
        public int RoomCount => _rooms.Length;
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
        // A horde enemy has walked onto the floor into this place of the horde (HordeEnemyAt), and the horde has left the
        // floor: for views that follow every enemy, not only the wave's.
        public event Action<int> HordeEnemySpawned;
        // A swarm room's enemy walked into its place in the wave (WaveEnemyAt), the opening ladder's and every refill's
        // after it, so presentation that follows the wave can follow the newcomer.
        public event Action<int> SwarmEnemySpawned;
        public event Action HordeReleased;
        public event Action ProgressChanged;
        public event Action<Health> EnemyDefeated;
        // An ordinary wave's last enemy has fallen, once per wave and before the floor moves on; a swarm never raises it.
        public event Action WaveCleared;
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
            UpgradeService upgrades = null, GroundDrops drops = null, bool deferFirstEncounter = false, int hordeSeed = 0)
        {
            _difficulty = difficulty;
            _hordeSeed = hordeSeed;
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
                ValidateHorde(floor);
            }

            _fight = new EncounterProgress(_advanceDelay);
            FloorNumber = 1;
            _hallsOnEarlierFloors = 0;
            EnterFloor(first);
            // A fallen hero ends the gate's hold at once, so nothing reads a hold the run has left behind.
            _hero.Died += OnHeroDied;
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
            if (enemy == null)
                return false;
            for (int i = 0; _hordeMotion != null && i < _hordeEnemies.Count; i++)
            {
                SpawnedEnemy spawned = _hordeEnemies[i];
                if (!ReferenceEquals(spawned.Health, enemy) || spawned.Slot >= _hordeMotion.Count)
                    continue;
                floorX = _hordeMotion.XOf(spawned.Slot);
                floorY = _hordeMotion.YOf(spawned.Slot);
                return true;
            }
            if (_motion == null)
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

            ReleaseHorde(true);
            _roomsClearedOnEarlierFloors += _floorProgress.RoomsCleared;
            _hallsOnEarlierFloors += LaterHalls(_currentFloor);
            FloorNumber++;
            EnterFloor(_currentFloor.NextFloor);
            _descending = true;
            _transitionWait = 0f;
            ProgressChanged?.Invoke();
        }

        // The current wave's enemies in slot order, alive or defeated, until the next wave or room replaces them.
        public Health WaveEnemyAt(int index) => _wave[index].Health;

        // The floor's horde: whether one stands, its enemies by place (alive, fallen, or taken away - null), how many
        // stand, the crowd it aims for and the state its director is in.
        public bool IsHordeActive => _horde != null;
        public int HordeEnemyCount => _hordeEnemies.Count;
        public Health HordeEnemyAt(int index) => _hordeEnemies[index].Health;
        public int HordeAliveCount { get; private set; }
        public int HordeTarget => _horde != null ? _horde.TargetNow : 0;
        public HordeState HordeStateNow => _horde != null ? _horde.State : HordeState.Ramp;
        // The floor's clock: active seconds of this floor's horde, frozen through choices and the forge (HordeClock).
        // Zero where no horde runs - the finale, the walk up after the gate, a floor without horde roles.
        public float FloorClockSeconds => _horde != null ? _horde.Seconds : 0f;
        // The room before the gate is cleared and holds for the floor's clock (FloorDefinition.FoundryGateNotBeforeSeconds);
        // GateOpensIn says how long is left. The hold ends as the floor moves on, the floor changes or the hero falls.
        public bool IsHoldingForGate => _holdingForGate;
        private bool _holdingForGate;
        public float GateOpensIn => IsHoldingForGate && _currentFloor != null
            ? Mathf.Max(0f, _currentFloor.FoundryGateNotBeforeSeconds - FloorClockSeconds) : 0f;
        // Seconds before the floor is overrun, and whether it already is.
        public float FloorClockRemaining => HordeClock.Remaining(FloorClockSeconds);
        public bool IsFloorOvertime => _horde != null && _horde.IsOvertime;
        // Every enemy on the floor, the wave's first, then the horde's - what a hazard or a view that reads the whole
        // floor walks. An entry may be fallen or, for the horde, null.
        public int EnemyCount => _wave.Count + _hordeEnemies.Count;
        public Health EnemyAt(int index) => index < _wave.Count ? _wave[index].Health : _hordeEnemies[index - _wave.Count].Health;
        public int AllAliveCount => AliveEnemyCount + HordeAliveCount;
        // The gate boss's second phase (WardenSlamService): the boss room's horde presses round it.
        public void SetBossEnraged(bool enraged) => _bossEnraged = enraged;
        // The boss the foundry's gate sent out, while its wave stands; null otherwise.
        public Health GateBoss => _gateBossWave && _wave.Count > 0 && !_wave[0].Defeated ? _wave[0].Health : null;
        private bool _gateBossWave;
        // The Grunt Captain of a foundry elite room's wave while it stands - the wave's first enemy - for the development
        // autopilot's step-in (AutopilotCohort.Habit.StepsIn, FootworkRule.StepIn); null otherwise.
        public Health EliteLeader => _eliteWave && _wave.Count > 0 && !_wave[0].Defeated ? _wave[0].Health : null;
        private bool _eliteWave;
        // The foundry's elites (EliteRule): the Grunt Captain an elite room's wave is built round - its first enemy - from
        // its spawn until the wave is released, fallen or not; empty otherwise - the escort, the halls, the horde, the gate,
        // the hall arena. One list for the run, so the hero's attacker holds it once (CombatSetup, AttackController.Elites)
        // and every strike reads it as it stands.
        public IReadOnlyList<Health> Elites => _elites;
        private readonly List<Health> _elites = new List<Health>(1);
        public bool IsBossEnraged => _bossEnraged;

        // Null for an enemy that is not part of the current wave, for example the hero or a test's own damage.
        public EnemyDefinition DefinitionOf(IDamageable enemy) => Find(enemy)?.Definition;

        public int GoldRewardOf(IDamageable enemy) => Find(enemy)?.Gold ?? 0;

        // The experience an enemy of the floor pays: its authored reward, or the horde's share of it.
        public int ExperienceRewardOf(IDamageable enemy) => Find(enemy)?.Experience ?? 0;

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
                    if (wave == null || wave.EnemyCount < 1 || wave.EnemyCount > PackLayout.MaxFormationSize)
                        throw new InvalidOperationException($"Room {index} wave {i} needs one to {PackLayout.MaxFormationSize} enemies.");
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
            ValidateGateHold(floor);
            ValidateGateHealthShare(floor);
        }

        // The gate boss's share on the floor (FloorDefinition.FoundryGateHealthShare, WardenRule.GateHealthShareOn): 0 for
        // WardenRule.GateHealthShare, or a finite share above 0. A NaN or a negative share would give the Warden no health
        // or none that counts down.
        private static void ValidateGateHealthShare(FloorDefinition floor)
        {
            float share = floor.FoundryGateHealthShare;
            if (!WardenRule.IsFloorGateHealthShare(share))
                throw new InvalidOperationException(
                    $"Floor {floor.name} gives its gate boss a health share of {share}; the share must be 0 (WardenRule.GateHealthShare) or a finite share above 0.");
        }

        // The gate's clock holds the cleared room before the gate boss on the foundry (GateHoldsForClock): a time inside
        // the floor's clock, before the overrun, on a floor whose horde runs that clock, and a combat or elite room before
        // every boss room as the foundry plays them. Neither a forge nor a swarm room holds, so the gate would open early
        // after one; nor could a forge hold, as it freezes the floor's clock and the wait would never end.
        private static void ValidateGateHold(FloorDefinition floor)
        {
            float gate = floor.FoundryGateNotBeforeSeconds;
            if (gate == 0f)
                return;
            if (!(gate > 0f && gate < HordeClock.TargetSeconds))
                throw new InvalidOperationException(
                    $"Floor {floor.name} holds its gate until {gate} s; the time must be 0 or inside the floor's {HordeClock.TargetSeconds} s clock.");
            if (floor.HordeRoleCount == 0)
                throw new InvalidOperationException($"Floor {floor.name} holds its gate for the floor's clock but has no horde to run it.");
            RoomDefinition[] rooms = FoundryRooms(floor);
            bool gated = false;
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i].Kind != RoomKind.Boss)
                    continue;
                gated = true;
                if (i == 0 || !IsHallKind(rooms[i - 1].Kind))
                    throw new InvalidOperationException(
                        $"Floor {floor.name} holds its gate for the floor's clock, but no combat or elite room stands before its boss room {i}.");
            }
            if (!gated)
                throw new InvalidOperationException($"Floor {floor.name} holds its gate for the floor's clock but has no boss room.");
        }

        // A floor's horde roles must each be an enemy the scene can spawn, as a wave's enemies must.
        private static void ValidateHorde(FloorDefinition floor)
        {
            for (int i = 0; i < floor.HordeRoleCount; i++)
            {
                EnemyDefinition enemy = floor.HordeRoleAt(i);
                if (enemy == null || enemy.Weapon == null || enemy.Prefab == null ||
                    enemy.Prefab.GetComponent<AttackController>() == null || enemy.Prefab.GetComponent<Targeting>() == null)
                    throw new InvalidOperationException(
                        $"Floor {floor.name} horde role {i} needs a definition with a weapon and a prefab carrying Health, Targeting and AttackController.");
            }
        }

        // A floor's rooms as the foundry plays them: its extra elites (FloorDefinition.FoundryExtraElites, copies of its first
        // elite room) stand before its gate boss, its last boss room. A floor without an elite or a boss plays as authored.
        public static RoomDefinition[] FoundryRooms(FloorDefinition floor)
        {
            int elite = -1, boss = -1;
            for (int i = 0; i < floor.RoomCount; i++)
            {
                RoomDefinition room = floor.RoomAt(i);
                if (room != null && room.Kind == RoomKind.Elite && elite < 0)
                    elite = i;
                if (room != null && room.Kind == RoomKind.Boss)
                    boss = i;
            }
            int extra = elite >= 0 && boss >= 0 ? Math.Max(0, floor.FoundryExtraElites) : 0;
            var rooms = new RoomDefinition[floor.RoomCount + extra];
            for (int i = 0, r = 0; i < floor.RoomCount; i++)
            {
                if (i == boss)
                    for (int e = 0; e < extra; e++)
                        rooms[r++] = floor.RoomAt(elite);
                rooms[r++] = floor.RoomAt(i);
            }
            return rooms;
        }

        // The floor's rooms as this arena plays them: the foundry's (FoundryRooms), the authored ones on the hall arena.
        private RoomDefinition[] RoomsAsPlayed(FloorDefinition floor)
        {
            if (_arena != null && _arena.IsFoundryRegion)
                return FoundryRooms(floor);
            var rooms = new RoomDefinition[floor.RoomCount];
            for (int i = 0; i < rooms.Length; i++)
                rooms[i] = floor.RoomAt(i);
            return rooms;
        }

        // The floor the Descent is on, its rooms as played and its progress through them change together, only here.
        private void EnterFloor(FloorDefinition floor)
        {
            _currentFloor = floor;
            _rooms = RoomsAsPlayed(floor);
            _floorProgress = new FloorProgress(WavesOf(_rooms), EndlessOf(_rooms));
            _waveOrdinal = 0;
            _holdingForGate = false;
        }

        private void OnHeroDied() => _holdingForGate = false;

        private int[] WavesOf(RoomDefinition[] rooms)
        {
            var waves = new int[rooms.Length];
            for (int i = 0; i < waves.Length; i++)
                waves[i] = rooms[i].Kind == RoomKind.Forge ? 0 : WavesIn(_currentFloor, rooms[i]);
            return waves;
        }

        // A room's waves: its authored ones, and on the foundry a combat room's surges after them (HallSurgeRule).
        private int WavesIn(FloorDefinition floor, RoomDefinition room) => room.WaveCount + SurgesIn(floor, room);

        private int SurgesIn(FloorDefinition floor, RoomDefinition room) =>
            room.Kind == RoomKind.Combat && _arena != null && _arena.IsFoundryRegion ? floor.FoundryHallSurges : 0;

        // The enemies of a room's wave into _waveEnemies: an authored wave as authored, a surge as the wave it repeats
        // with that wave's last enemy added until the surge's size.
        private List<EnemyDefinition> WaveEnemies(int roomIndex, int waveIndex)
        {
            _waveEnemies.Clear();
            RoomDefinition room = _rooms[roomIndex];
            int authored = room.WaveCount;
            int surge = waveIndex - authored;
            WaveDefinition wave = room.WaveAt(surge < 0 ? waveIndex : HallSurgeRule.SourceWave(authored, surge));
            for (int e = 0; e < wave.EnemyCount; e++)
                _waveEnemies.Add(wave.EnemyAt(e));
            if (surge >= 0)
                for (int e = wave.EnemyCount; e < HallSurgeRule.Size(wave.EnemyCount, surge); e++)
                    _waveEnemies.Add(wave.EnemyAt(wave.EnemyCount - 1));
            // The density probe (development builds, DevelopmentStart.HordeProbe): the floor's first wave padded with
            // copies of its first enemy, up to the pack's capacity, to measure the phone at the horde's crowd. By index: an
            // extra elite is the same room as the elite it copies.
            if (roomIndex == 0 && waveIndex == 0 && _arena.IsFoundryRegion)
                for (int extra = DevelopmentStart.HordeProbe(); extra > 0 && _waveEnemies.Count < PackLayout.MaxPackSize; extra--)
                    _waveEnemies.Add(wave.EnemyAt(0));
            return _waveEnemies;
        }

        // A swarm room's waves are a ladder rather than a list that finishes, so the floor never clears it on its own.
        private static bool[] EndlessOf(RoomDefinition[] rooms)
        {
            var endless = new bool[rooms.Length];
            for (int i = 0; i < endless.Length; i++)
                endless[i] = rooms[i].Kind == RoomKind.Swarm;
            return endless;
        }

        private void Update()
        {
            if (_floorProgress == null || _firstEncounterPending || _revealHold || !_hero.IsAlive)
                return;

            MovePack(Time.deltaTime);
            TickHorde(Time.deltaTime);
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
                    StartWave(WaveEnemies(_roomSiteStep.RoomIndex, _roomSiteStep.WaveIndex), false,
                        _rooms[_roomSiteStep.RoomIndex].Kind == RoomKind.Boss, EliteRoom(_rooms[_roomSiteStep.RoomIndex]));
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

            // The gate opens on the floor's clock, not before: the cleared room before it holds while the horde presses.
            bool holding = GateHoldsForClock();
            if (holding != _holdingForGate)
            {
                _holdingForGate = holding;
                ProgressChanged?.Invoke();
            }
            if (holding)
                return;

            AdvanceFloor();
        }

        // The next step is the gate boss's room and the floor's clock has not reached the time the floor sets for it.
        // ValidateGateHold makes that room's predecessor a combat or elite room, whose last wave this follows.
        private bool GateHoldsForClock()
        {
            if (_currentFloor == null || _horde == null || _arena == null || !_arena.IsFoundryRegion ||
                _currentFloor.FoundryGateNotBeforeSeconds <= 0f || !_floorProgress.IsLastWaveOfRoom)
                return false;
            int next = _floorProgress.RoomIndex + 1;
            return next < _rooms.Length && _rooms[next].Kind == RoomKind.Boss &&
                   FloorClockSeconds < _currentFloor.FoundryGateNotBeforeSeconds;
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
            ReleaseHorde(true);
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
            SetWaveTargets(_swarmCandidates);
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
            SwarmEnemySpawned?.Invoke(spawn.Slot);
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
            SetWaveTargets(Array.Empty<Health>());
            // Whatever the room still held back is offered now, before the floor moves on.
            _upgrades?.Release();
            AdvanceFloor();
        }

        private void AdvanceFloor()
        {
            // Whatever held the floor no longer does once it moves on.
            _holdingForGate = false;
            FloorStep step = _floorProgress.Advance();
            switch (step.Kind)
            {
                case FloorStepKind.Wave:
                {
                    RoomDefinition room = _rooms[step.RoomIndex];
                    if (room.Kind == RoomKind.Swarm)
                        BeginSwarm(room);
                    else if (step.WaveIndex == 0 && TryRoomSite(step.RoomIndex))
                    {
                        ReleaseWave(true);
                        // A forge before it is behind the hero: the walk to the hall is not the forge's, so the horde and
                        // the floor's clock run on it (TickHorde). Only a spawn cleared the flag, and a marked hall spawns
                        // nothing until it is reached (the run flow's review, 2026-11-11).
                        IsInNonCombatRoom = false;
                        _roomSiteStep = step;
                        _roomSiteOpen = true;
                        _roomSiteWait = 0f;
                        ProgressChanged?.Invoke();
                    }
                    else
                        StartWave(WaveEnemies(step.RoomIndex, step.WaveIndex), GateRoom(room), room.Kind == RoomKind.Boss,
                            EliteRoom(room));
                    break;
                }
                case FloorStepKind.NonCombatRoom:
                    EnterForge(_rooms[step.RoomIndex]);
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

        private void StartWave(List<EnemyDefinition> waveDefinition, bool atGate = false, bool boss = false, bool elite = false)
        {
            using (PerformanceMarkers.StartWave.Auto())
                SpawnWave(waveDefinition, atGate, boss, elite);
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

        // An elite room on the foundry: its wave's first enemy is its Captain, an elite (EliteLeader, Elites).
        private bool EliteRoom(RoomDefinition room) =>
            room.Kind == RoomKind.Elite && _arena != null && _arena.IsFoundryRegion;

        // A foundry floor's combat room after its first opens at its own district, the round going on from the
        // districts the run's earlier floors opened.
        private bool TryRoomSite(int roomIndex)
        {
            if (_arena == null || !_arena.IsFoundryRegion ||
                !TryRoomSite(_rooms, roomIndex, _hallsOnEarlierFloors, out float x, out float y))
                return false;
            _roomSiteX = x;
            _roomSiteY = y;
            return true;
        }

        // Where a floor's room opens on the foundry: a combat or elite room after the floor's first, rooms as the foundry
        // plays them (FoundryRooms), at the district of its order among them after hallsBefore, the halls the run's
        // earlier floors added to the round (LaterHalls). False for anything else.
        public static bool TryRoomSite(IReadOnlyList<RoomDefinition> rooms, int roomIndex, int hallsBefore, out float x,
            out float y)
        {
            x = y = 0f;
            if (!IsHallKind(rooms[roomIndex].Kind))
                return false;
            int ordinal = 0;
            for (int i = 0; i < roomIndex; i++)
            {
                if (IsHallKind(rooms[i].Kind))
                    ordinal++;
            }
            if (ordinal == 0)
                return false;
            FoundryMap.RoomSite(hallsBefore, ordinal, out x, out y);
            return true;
        }

        private static bool IsHallKind(RoomKind kind) => kind == RoomKind.Combat || kind == RoomKind.Elite;

        // The halls a floor adds to the round across the climb: its authored combat and elite rooms after the first. A
        // foundry's extra elite (V0.1) takes the next district on its own floor, but the floors above go on from the
        // authored count, so each keeps the route it had before (FoundryMap.RoomSite).
        public static int LaterHalls(FloorDefinition floor)
        {
            int halls = 0;
            for (int i = 0; i < floor.RoomCount; i++)
            {
                if (IsHallKind(floor.RoomAt(i).Kind))
                    halls++;
            }
            return Math.Max(0, halls - 1);
        }

        private bool GateClosesFloor() => _rooms.Length > 0 && GateRoom(_rooms[_rooms.Length - 1]);

        private void OpenGateExit()
        {
            // The boss has fallen: the floor's fight is over, and its horde goes with it.
            ReleaseHorde(true);
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

        private void SpawnWave(List<EnemyDefinition> waveDefinition, bool atGate = false, bool boss = false, bool elite = false)
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
            // The gate boss's share on this floor: the floor's own, or WardenRule.GateHealthShare.
            float gateShare = atGate ? WardenRule.GateHealthShareOn(_currentFloor.FoundryGateHealthShare) : 1f;
            // The pack forms up round wherever the hero stands now, on the corners the platform leaves open.
            float heroX = HeroFloorX;
            float heroY = HeroFloorY;
            _motion = new PackMotion(_bodySpacing, PackLayout.HalfWidth * _formationSpacing, _arena.Geometry, heroX, heroY);
            // Spots keep the same body spacing the pack keeps while it walks, so no two enemies enter overlapping. The
            // Descent simulation passes the same value, so scene and simulation lay a wave out identically; with the hero
            // near the centre and a pack the formations already keep 1.1 spacing units apart, it changes nothing.
            EntrySides.Place(_waveOrdinal, count, heroX, heroY, _arena.Geometry, _entryDepth, _formationSpacing, _bodySpacing,
                _placements);
            _gateBossWave = atGate;
            _eliteWave = elite;
            if (atGate)
                FoundryMap.PlaceAtGate(count, _placements);
            for (int i = 0; i < count; i++)
            {
                EnemyDefinition definition = waveDefinition[i];
                SpawnedEnemy spawn = CreateEnemy(definition,
                    count == 1 ? definition.Prefab.name : $"{definition.Prefab.name} {i + 1}",
                    floorHealth * scale * (atGate && i == 0 ? gateShare : 1f),
                    healthPercent, damageBonus, goldPercent, out float reach);
                int slot = _motion.Add(spawn.Health, _placements[i], definition.MoveSpeed, reach);
                if (atGate)
                    _motion.SetMarch(slot, GateMarchFactor, GateMarchRange);
                spawn.Slot = slot;
                spawn.Health.transform.position = WorldPosition(_motion.XOf(slot), _motion.YOf(slot));
                _wave.Add(spawn);
                candidates[i] = spawn.Health;
                if (elite && i == 0)
                    _elites.Add(spawn.Health);
            }

            AliveEnemyCount = count;
            _waveOrdinal++;
            SetWaveTargets(candidates);
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
            float damageBonus, float goldPercent, out float reach, bool horde = false)
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
                Gold = horde ? 0 : FloorScaling.Gold(definition.GoldReward, goldPercent),
                // An overrun floor's horde pays nothing (HordeClock.PaysExperience): the overrun is a reason to leave.
                Experience = !horde ? definition.ExperienceReward :
                    _horde != null && !HordeClock.PaysExperience(_horde.Seconds) ? 0 :
                    HordeRule.Experience(definition.ExperienceReward),
                Enrage = enemy.GetComponent<EnrageBehaviour>()
            };
            // A horde enemy's hits and death are the horde's: they never count towards the node wave's fight.
            if (horde)
                spawn.OnDied = () => OnHordeEnemyDied(spawn);
            else
            {
                spawn.OnDied = () => OnEnemyDied(spawn);
                enemy.Changed += OnEnemyChanged;
            }
            enemy.Died += spawn.OnDied;
            if (spawn.Enrage != null)
                spawn.Enrage.Enraged += OnEnemyEnraged;
            return spawn;
        }

        private void EnterForge(RoomDefinition room)
        {
            ReleaseWave(true);
            SetWaveTargets(Array.Empty<Health>());
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
            if (AliveEnemyCount == 0)
                WaveCleared?.Invoke();
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
            for (int i = 0; i < _hordeEnemies.Count; i++)
            {
                if (ReferenceEquals(_hordeEnemies[i].Health, enemy))
                    return _hordeEnemies[i];
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
            _elites.Clear();
            // A gate's or an elite room's wave is gone with its flags: a swarm refills the wave without SpawnWave, and a
            // flag left standing made its first enemy the gate boss (GateBoss, WardenSlamService) or the Captain
            // (EliteLeader) - the review of 2026-11-11, the Descent's finale after floor 2's gate on the foundry.
            _gateBossWave = false;
            _eliteWave = false;
            _motion = null;
            _lastDefeated = null;
            AliveEnemyCount = 0;
        }

        private void OnDestroy()
        {
            ReleaseWave(false);
            ReleaseHorde(false);
            if (!ReferenceEquals(_hero, null))
                _hero.Died -= OnHeroDied;
        }

        // The hero's targets: the wave's, and while a horde stands, the horde's living enemies after them.
        private void SetWaveTargets(Health[] wave)
        {
            _waveTargets = wave ?? Array.Empty<Health>();
            RebuildTargets();
        }

        private void RebuildTargets()
        {
            if (_horde == null)
            {
                _heroTargeting.SetCandidates(_waveTargets);
                return;
            }
            int n = 0;
            for (int i = 0; i < _waveTargets.Length && n < _targets.Length; i++)
                _targets[n++] = _waveTargets[i];
            for (int i = 0; i < _hordeEnemies.Count && n < _targets.Length; i++)
            {
                SpawnedEnemy enemy = _hordeEnemies[i];
                if (!enemy.Defeated && enemy.Health != null)
                    _targets[n++] = enemy.Health;
            }
            Array.Clear(_targets, n, _targets.Length - n);
            _heroTargeting.SetCandidates(_targets);
        }

        // Whether this moment of the floor has a horde: the foundry, a floor with horde roles, between its first step and
        // its boss's fall, never in a swarm room or on the way up to the next floor.
        private bool HordeWanted() =>
            _arena != null && _arena.IsFoundryRegion && _currentFloor.HordeRoleCount > 0 && _swarm == null &&
            !_floorProgress.IsComplete && !_gateExitOpen && !_descending;

        // One frame of the horde: its pack walks, its director says what walks in, and fallen bodies are taken away. A
        // frozen frame - an open choice, the forge, a level-up - moves nothing.
        private void TickHorde(float deltaTime)
        {
            if (!HordeWanted())
            {
                if (_horde != null)
                    ReleaseHorde(true);
                return;
            }
            if (_horde == null || _hordeFloor != FloorNumber)
                BeginHorde();
            if (deltaTime <= 0f || _choices.IsOpen || IsInNonCombatRoom)
                return;

            _hordeSeconds += deltaTime;
            _hordeMotion.Held = PackHeld;
            _hordeMotion.Step(deltaTime, HeroFloorX, HeroFloorY);
            for (int i = 0; i < _hordeEnemies.Count; i++)
            {
                SpawnedEnemy enemy = _hordeEnemies[i];
                if (enemy.Health == null)
                    continue;
                if (!enemy.Defeated)
                    enemy.Health.transform.position = WorldPosition(_hordeMotion.XOf(enemy.Slot), _hordeMotion.YOf(enemy.Slot));
                else if (_hordeSeconds - enemy.FellAt >= HordeRule.RetireSeconds)
                    RetireHordeBody(enemy);
            }

            bool bossRoom = _rooms[_floorProgress.RoomIndex].Kind == RoomKind.Boss;
            // An enraged gate boss draws the horde's Horde crowd round it; Pressure there killed the autopilot in three
            // fights of six (docs/41, 2026-10-26).
            HordeState state = bossRoom && _bossEnraged ? HordeState.Horde : HordeRule.StateFor(HallsCleared(), bossRoom);
            if (state != _horde.State)
                _horde.SetState(state);
            _horde.Tick(deltaTime, HordeAliveCount, AliveEnemyCount, _hordeSpawns);
            bool spawned = false;
            for (int i = 0; i < _hordeSpawns.Count; i++)
                spawned |= SpawnHordeEnemy(_hordeSpawns[i]);
            if (spawned)
                RebuildTargets();
        }

        // The floor's halls and elites already behind the hero: the horde's state reads how far the floor has got.
        private int HallsCleared()
        {
            int cleared = 0;
            int before = Math.Min(_floorProgress.RoomIndex, _rooms.Length);
            for (int i = 0; i < before; i++)
            {
                if (IsHallKind(_rooms[i].Kind))
                    cleared++;
            }
            return cleared;
        }

        private void BeginHorde()
        {
            ReleaseHorde(true);
            _hordeFloor = FloorNumber;
            _hordeSeconds = 0f;
            _bossEnraged = false;
            int roles = _currentFloor.HordeRoleCount;
            // Clocked: the floor's clock grows the horde and overruns the floor after HordeClock.TargetSeconds.
            _horde = new HordeDirector(HordeRule.SceneCap, roles, _hordeSeed + FloorNumber, true);
            float clockStart = DevelopmentStart.FloorClockStart();
            if (clockStart > 0f)
                _horde.SkipClock(clockStart);
            for (HordeState state = HordeState.Ramp; state <= HordeState.Boss; state++)
            {
                var weights = new int[roles];
                for (int role = 0; role < roles; role++)
                    weights[role] = HordeRule.Weight(state, role);
                _horde.SetRoleWeights(state, weights);
            }
            _hordeMotion = new PackMotion(_bodySpacing, PackLayout.HalfWidth * _formationSpacing, _arena.Geometry, HeroFloorX,
                HeroFloorY);
            RebuildTargets();
        }

        // One horde enemy walking in from the edge of the view, at the floor's tier and the run's difficulty, into a fallen
        // enemy's place when there is one. False when the floor had no clear spot for it.
        private bool SpawnHordeEnemy(HordeDirector.Spawn spawn)
        {
            if (!TryHordeSpot(spawn.Ordinal, out float x, out float y))
                return false;
            int slot = -1;
            for (int i = 0; i < _hordeEnemies.Count; i++)
            {
                if (_hordeEnemies[i].Defeated)
                {
                    slot = i;
                    break;
                }
            }
            if (slot < 0 && _hordeMotion.Count >= PackLayout.MaxPackSize)
                return false;

            EnemyDefinition definition = _currentFloor.HordeRoleAt(spawn.Role);
            FloorModifierDefinition modifier = _currentFloor.Modifier;
            float healthPercent = modifier != null ? modifier.EnemyHealthPercent : 0f;
            float damagePercent = modifier != null ? modifier.EnemyDamagePercent : 0f;
            float scale = RunDifficulty;
            // The floor's clock: a horde enemy that walks in later on the floor is tougher (HordeClock).
            float damageBonus = FloorScaling.DamageBonus(
                FloorDamageMultiplier * scale * HordeRule.DamageShare * _horde.DamageMultiplier, damagePercent);
            SpawnedEnemy enemy = CreateEnemy(definition, $"Horde {definition.Prefab.name} {spawn.Ordinal + 1}",
                FloorHealthMultiplier(false) * scale * HordeRule.HealthShare * _horde.HealthMultiplier, healthPercent,
                damageBonus, 0f, out float reach, true);
            var placement = new EntryPlacement(EntrySides.SideOf(spawn.Ordinal, 0), 0f, x, y);
            if (slot >= 0)
            {
                RetireHordeBody(_hordeEnemies[slot]);
                _hordeMotion.Replace(slot, enemy.Health, placement, definition.MoveSpeed, reach);
                _hordeEnemies[slot] = enemy;
            }
            else
            {
                slot = _hordeMotion.Add(enemy.Health, placement, definition.MoveSpeed, reach);
                _hordeEnemies.Add(enemy);
            }
            enemy.Slot = slot;
            enemy.Health.transform.position = WorldPosition(x, y);
            HordeAliveCount++;
            HordeEnemySpawned?.Invoke(slot);
            return true;
        }

        // The first of the floor's horde spots that is on the floor and a body clear of every living horde enemy.
        private bool TryHordeSpot(int ordinal, out float x, out float y)
        {
            float clearSquared = _bodySpacing * _bodySpacing;
            for (int attempt = 0; attempt < FoundryMap.HordeSpotAttempts; attempt++)
            {
                if (!FoundryMap.HordeSpot(ordinal, attempt, HeroFloorX, HeroFloorY, out x, out y))
                    continue;
                bool clear = true;
                for (int i = 0; clear && i < _hordeEnemies.Count; i++)
                {
                    SpawnedEnemy other = _hordeEnemies[i];
                    clear = other.Defeated || ArenaFloor.FloorDistanceSquared(x, y, _hordeMotion.XOf(other.Slot),
                        _hordeMotion.YOf(other.Slot)) >= clearSquared;
                }
                if (clear)
                    return true;
            }
            x = y = 0f;
            return false;
        }

        private void OnHordeEnemyDied(SpawnedEnemy enemy)
        {
            if (enemy.Defeated)
                return;
            enemy.Defeated = true;
            enemy.FellAt = _hordeSeconds;
            HordeAliveCount--;
            _horde?.Fell();
            EnemyDefeated?.Invoke(enemy.Health);
        }

        // A fallen horde enemy's body leaves the floor; its place stays for the next to fill.
        private void RetireHordeBody(SpawnedEnemy fallen)
        {
            if (ReferenceEquals(fallen.Health, null))
                return;
            fallen.Health.Died -= fallen.OnDied;
            if (fallen.Enrage != null)
                fallen.Enrage.Enraged -= OnEnemyEnraged;
            if (fallen.Health != null)
                Destroy(fallen.Health.gameObject);
            fallen.Health = null;
        }

        // The horde leaves the floor: every body goes, and the hero's targets are the wave's again.
        private void ReleaseHorde(bool destroy)
        {
            for (int i = 0; i < _hordeEnemies.Count; i++)
            {
                SpawnedEnemy enemy = _hordeEnemies[i];
                if (ReferenceEquals(enemy.Health, null))
                    continue;
                enemy.Health.Died -= enemy.OnDied;
                if (enemy.Enrage != null)
                    enemy.Enrage.Enraged -= OnEnemyEnraged;
                if (destroy && enemy.Health != null)
                    Destroy(enemy.Health.gameObject);
            }
            _hordeEnemies.Clear();
            bool had = _horde != null;
            _horde = null;
            _hordeMotion = null;
            HordeAliveCount = 0;
            if (!had)
                return;
            if (_heroTargeting != null)
                RebuildTargets();
            HordeReleased?.Invoke();
        }
    }
}
