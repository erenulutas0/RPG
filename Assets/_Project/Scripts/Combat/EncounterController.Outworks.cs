using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.Combat
{
    // docs/62's P0a, the Outworks mechanics probe (2026-11-22): with development/outworks.txt in the Editor or a development
    // build (DevelopmentStart.Outworks), a run that begins on floor 1 of the foundry plays the Outworks phase in today's court
    // before the floor's first hall. Off by default: without the file nothing here runs, and the shipped floor is the floor.
    //
    // The phase (OutworksRule, OutworksPhase - pure, and the foundry simulation's DescentSimulation.Outworks.cs plays the
    // same frames): the floor's horde from the first frame, its state by the phase's clock; three Tithe Anvils, each captured
    // by standing in its ring, each capture a level-up's offer of cards (AnvilCaptured; CombatSetup grants it as an altar
    // does); at OutworksRule.KeeperSpawnSeconds the Keeper - the floor's first elite room's wave, its Captain at the Keeper's
    // share - walks onto its loop round the pit, turns on the hero within its aggro radius or once struck, and its Captain's
    // fall hands over the Stair Key, which opens the Ascent Stair early; at the Bell the stair opens for everyone, a Keeper
    // still roaming leaves, and the Bell's swarm rises in the wave's pack (SwarmDirector over OutworksRule's ladder); the
    // stair held as the opened gate is (standing still on it) ends the phase, and the floor's first hall begins where the
    // hero stands, the horde and the floor's clock running on. The pads (HeroLeap) are the hero's walk's (HeroMovementInput),
    // open while the phase runs (IsInOutworks); this placement reads the leap only to place a spawn round its landing.
    //
    // With the switch on, floor 1's halls are trimmed by OutworksRule's knobs (HallSurgesOn, ExtraElitesOn, GateNotBeforeOn),
    // measured in docs/62 section 12; the floors above and every switch-off run read their asset's own.
    // The hero's health on an Outworks run (CombatSetup sets it, Health.Guard): while a pad's leap is mid-arc no blow lands
    // - an enemy's strike, the pit's spill - as the simulation's HeroDamaged turns it away. Every other moment, untouched.
    public sealed class AirborneGuard : IDamageGuard
    {
        private readonly HeroLeap _leap;

        public AirborneGuard(HeroLeap leap)
        {
            _leap = leap ?? throw new ArgumentNullException(nameof(leap));
        }

        // Blows turned away mid-arc, for tests.
        public int Turned { get; private set; }

        public DamageContext Guard(DamageContext context)
        {
            if (!_leap.IsAirborne || context.Amount == 0f)
                return context;
            Turned++;
            return new DamageContext(0f, context.Source, context.IsCritical, context.IsTick);
        }
    }

    public sealed partial class EncounterController
    {
        // Whether this run plays the Outworks: the switch on, the foundry, and the run begun on the authored floor 1.
        private bool _outworksRun;
        private OutworksPhase _outworks;
        private SwarmDirector _bell;
        private readonly List<SwarmDirector.Spawn> _bellSpawns = new List<SwarmDirector.Spawn>(PackLayout.MaxPackSize);
        // The wave standing is the Keeper's pack; a strike on it has landed; it has turned (its pace restored).
        private bool _keeperPack;
        private bool _keeperStruck;
        private bool _keeperHunting;
        // Where the hero stood at the end of the last phase frame: the stair's hold counts only while it stood still.
        private float _stairHeroX;
        private float _stairHeroY;

        // The hero's pad leap, made with the run when it plays the Outworks (CombatSetup hands it to HeroMovementInput and
        // guards the hero's health with it); null otherwise.
        public HeroLeap Leap { get; private set; }
        public bool PlaysOutworks => _outworksRun;
        // The phase, while and after it runs (null without the switch); whether it runs now.
        public OutworksPhase Outworks => _outworks;
        public bool IsInOutworks => _outworks != null && !_outworks.Left;
        public bool IsStairOpen => IsInOutworks && _outworks.StairOpen;
        public bool IsBellSwarm => IsInOutworks && _bell != null;
        // The Keeper's Captain while it stands on the court; null otherwise.
        public Health KeeperLeader => IsInOutworks && _keeperPack && _wave.Count > 0 && !_wave[0].Defeated ? _wave[0].Health : null;
        // An anvil was captured (its index): CombatSetup grants a level-up's offer of cards. The Keeper's Captain fell (the
        // key), the Bell rang, the stair was held.
        public event Action<int> AnvilCaptured;
        public event Action KeeperFell;
        public event Action BellRang;
        public event Action OutworksLeft;

        // Initialize, before the first floor is entered: whether this run plays the phase (and so whether floor 1's halls are
        // trimmed). first is the floor the run begins on, firstNumber its number.
        private void DecideOutworks(FloorDefinition first, int firstNumber)
        {
            _outworksRun = _arena != null && _arena.IsFoundryRegion && firstNumber == 1 && ReferenceEquals(first, _floor) &&
                           DevelopmentStart.Outworks();
            if (_outworksRun)
                Leap = new HeroLeap();
        }

        // What a horde enemy walking in now pays: the phase's share while it runs (OutworksRule.HordeExperience, the parity
        // lever), HordeRule's everywhere else.
        private int HordeExperienceOf(EnemyDefinition definition) => IsInOutworks
            ? OutworksRule.HordeExperience(definition.ExperienceReward)
            : HordeRule.Experience(definition.ExperienceReward);

        // Floor 1 of an Outworks run: its halls play OutworksRule's trim.
        private bool TrimsHalls => _outworksRun && FloorNumber == 1;

        private int HallSurgesOn(FloorDefinition floor) => TrimsHalls ? OutworksRule.TrimmedHallSurges : floor.FoundryHallSurges;

        private int ExtraElitesOn(FloorDefinition floor) => TrimsHalls ? OutworksRule.TrimmedExtraElites : floor.FoundryExtraElites;

        private float GateNotBeforeOn(FloorDefinition floor) =>
            TrimsHalls ? OutworksRule.TrimmedGateSeconds : floor.FoundryGateNotBeforeSeconds;

        // BeginFirstEncounter on an Outworks run's floor 1: the phase begins instead of the first hall. False, and nothing
        // touched, otherwise.
        private bool BeginOutworks()
        {
            if (!_outworksRun || _outworks != null || FloorNumber != 1)
                return false;
            _outworks = new OutworksPhase();
            _stairHeroX = HeroFloorX;
            _stairHeroY = HeroFloorY;
            ProgressChanged?.Invoke();
            return true;
        }

        // The horde's state while the phase runs: by its clock, Pressure from the Bell.
        private bool OutworksHordeState(out HordeState state)
        {
            state = HordeState.Ramp;
            if (!IsInOutworks)
                return false;
            state = _outworks.BellRung ? HordeState.Pressure : OutworksRule.HordeStateAt(_outworks.Seconds);
            return true;
        }

        // MovePack: the Keeper's pack walks its loop at its patrol pace until it turns on the hero; the escort of a fallen
        // Captain hunts as a wave.
        private bool KeeperPackGoal(out float goalX, out float goalY)
        {
            goalX = goalY = 0f;
            if (!IsInOutworks || !_keeperPack || _wave.Count == 0)
                return false;
            if (_wave[0].Defeated)
            {
                RestoreKeeperPace();
                return false;
            }
            if (!_outworks.KeeperGoal(_motion.XOf(0), _motion.YOf(0), HeroFloorX, HeroFloorY, _keeperStruck, out goalX,
                    out goalY))
                return false;
            if (_outworks.Keeper == KeeperState.Hunting)
                RestoreKeeperPace();
            return true;
        }

        private void RestoreKeeperPace()
        {
            if (_keeperHunting || _motion == null)
                return;
            _keeperHunting = true;
            for (int i = 0; i < _wave.Count; i++)
            {
                SpawnedEnemy enemy = _wave[i];
                if (!enemy.Defeated && enemy.Keeper && enemy.Slot < _motion.Count)
                    _motion.SetSpeed(enemy.Slot, enemy.Definition.MoveSpeed);
            }
        }

        // A strike on the Keeper's pack (a wave enemy's health changed) turns it on the hero.
        private void NoteKeeperStruck()
        {
            if (_keeperPack && IsInOutworks)
                _keeperStruck = true;
        }

        // One frame of the phase, after the packs, the horde and the drops - the simulation's OutworksFrame: nothing behind an
        // open choice or a frozen frame; the clock (the Bell), the Keeper walking on, the anvils where the hero stands, the
        // Bell's swarm and the stair's hold.
        private void TickOutworks(float deltaTime)
        {
            if (deltaTime <= 0f || _choices.IsOpen)
                return;
            if (_outworks.Tick(deltaTime))
                BeginBell();
            if (_outworks.KeeperDue)
                SpawnKeeper();
            float heroX = HeroFloorX;
            float heroY = HeroFloorY;
            int captured = _outworks.StepAnvils(heroX, heroY, deltaTime);
            if (captured >= 0)
                AnvilCaptured?.Invoke(captured);
            if (_bell != null)
            {
                _bell.Tick(deltaTime, AliveEnemyCount, _bellSpawns);
                for (int i = 0; i < _bellSpawns.Count; i++)
                    SpawnBellEnemy(_bellSpawns[i]);
                if (_bellSpawns.Count > 0)
                {
                    // The hero's targets by place, copied beside the horde's (RebuildTargets): the newcomers among them.
                    RebuildTargets();
                    ProgressChanged?.Invoke();
                }
            }
            bool moved = heroX != _stairHeroX || heroY != _stairHeroY;
            _stairHeroX = heroX;
            _stairHeroY = heroY;
            if (_outworks.StepStair(deltaTime, heroX, heroY, moved))
                LeaveOutworks();
        }

        // The Keeper walks on: the floor's first elite room's first wave, round the waypoint of its loop farthest from the
        // hero (FoundryMap.PlacePack), its Captain at OutworksRule.KeeperHealthShare of health and its pay, the pack at its
        // patrol pace; an elite wave (the Daggers' share, the elite's loot) that starts no room - no EncounterStarted.
        private void SpawnKeeper()
        {
            int start = OutworksRule.KeeperStart(HeroFloorX, HeroFloorY);
            _outworks.KeeperArrived(start);
            RoomDefinition elite = FirstEliteRoom();
            if (elite == null || elite.WaveCount == 0)
                return;
            WaveDefinition wave = elite.WaveAt(0);
            ReleaseWave(true);
            IsInNonCombatRoom = false;
            FloorModifierDefinition modifier = _currentFloor.Modifier;
            float healthPercent = modifier != null ? modifier.EnemyHealthPercent : 0f;
            float damagePercent = modifier != null ? modifier.EnemyDamagePercent : 0f;
            float goldPercent = modifier != null ? modifier.GoldPercent : 0f;
            float scale = RunDifficulty;
            float damageBonus = FloorScaling.DamageBonus(FloorDamageMultiplier * scale, damagePercent);
            float floorHealth = FloorHealthMultiplier(false);
            OutworksRule.KeeperWaypoint(start, out float keeperX, out float keeperY);
            int count = wave.EnemyCount;
            _motion = new PackMotion(_bodySpacing, PackLayout.HalfWidth * _formationSpacing, _arena.Geometry, keeperX, keeperY);
            FoundryMap.PlacePack(_waveOrdinal, count, keeperX, keeperY, _bodySpacing, _placements);
            _gateBossWave = false;
            _eliteWave = true;
            var candidates = new Health[count];
            for (int i = 0; i < count; i++)
            {
                EnemyDefinition definition = wave.EnemyAt(i);
                bool captain = i == 0;
                SpawnedEnemy spawn = CreateEnemy(definition, count == 1 ? definition.Prefab.name : $"{definition.Prefab.name} {i + 1}",
                    floorHealth * scale * (captain ? OutworksRule.KeeperHealthShare : 1f), healthPercent, damageBonus,
                    goldPercent, out float reach);
                if (captain)
                {
                    spawn.Gold = OutworksRule.KeeperGold(spawn.Gold);
                    spawn.Experience = OutworksRule.KeeperExperience(definition.ExperienceReward, OutworksRule.KeeperExperienceShare);
                    spawn.Health.name = "Keeper " + spawn.Health.name;
                }
                spawn.Keeper = true;
                int slot = _motion.Add(spawn.Health, _placements[i], definition.MoveSpeed * OutworksRule.KeeperPatrolShare, reach);
                spawn.Slot = slot;
                spawn.Health.transform.position = WorldPosition(_motion.XOf(slot), _motion.YOf(slot));
                _wave.Add(spawn);
                candidates[i] = spawn.Health;
                if (captain)
                    _elites.Add(spawn.Health);
                // No room starts (EncounterStarted): the views that follow a wave's places follow the Keeper's place by place.
                SwarmEnemySpawned?.Invoke(slot);
            }
            _keeperPack = true;
            _keeperStruck = false;
            _keeperHunting = false;
            AliveEnemyCount = count;
            _waveOrdinal++;
            SetWaveTargets(candidates);
            _fight.Begin();
            ProgressChanged?.Invoke();
        }

        private RoomDefinition FirstEliteRoom()
        {
            for (int i = 0; i < _rooms.Length; i++)
            {
                if (_rooms[i].Kind == RoomKind.Elite)
                    return _rooms[i];
            }
            return null;
        }

        // The Bell: a Keeper still standing leaves the court, the stair opens (OutworksPhase), and the swarm rises in the
        // wave's pack, its first wave walking in round the hero - round its landing while it is mid-arc.
        private void BeginBell()
        {
            ReleaseWave(true);
            _keeperPack = false;
            _bell = new SwarmDirector(OutworksRule.BellSizes());
            GroundOf(out float groundX, out float groundY);
            _motion = new PackMotion(_bodySpacing, PackLayout.HalfWidth * _formationSpacing, _arena.Geometry, groundX, groundY);
            Array.Clear(_swarmCandidates, 0, _swarmCandidates.Length);
            AliveEnemyCount = 0;
            _bell.Open(_bellSpawns);
            for (int i = 0; i < _bellSpawns.Count; i++)
                SpawnBellEnemy(_bellSpawns[i]);
            SetWaveTargets(_swarmCandidates);
            _fight.Begin();
            BellRang?.Invoke();
            ProgressChanged?.Invoke();
        }

        // Where a spawn round the hero is placed: the hero's feet, or its leap's landing while it is mid-arc over the pit.
        private void GroundOf(out float x, out float y)
        {
            bool airborne = Leap != null && Leap.IsAirborne;
            x = airborne ? Leap.LandingX : HeroFloorX;
            y = airborne ? Leap.LandingY : HeroFloorY;
        }

        // One Bell enemy into one place, as a swarm's (SpawnSwarmEnemy): the horde role the ladder names, fodder as the
        // horde's (its shares, the floor's clock grown) hardened by the swarm's ramp; the horde's share of experience and no
        // gold. A fallen enemy's body leaves as its place fills again.
        private void SpawnBellEnemy(SwarmDirector.Spawn spawn)
        {
            int role = OutworksRule.BellRole(spawn.Wave, spawn.Index, _currentFloor.HordeRoleCount);
            EnemyDefinition definition = _currentFloor.HordeRoleAt(role);
            GroundOf(out float groundX, out float groundY);
            EntrySides.Place(_waveOrdinal, 1, groundX, groundY, _arena.Geometry, _entryDepth, _formationSpacing, _bodySpacing,
                _placements);
            _waveOrdinal++;
            FloorModifierDefinition modifier = _currentFloor.Modifier;
            float healthPercent = modifier != null ? modifier.EnemyHealthPercent : 0f;
            float damagePercent = modifier != null ? modifier.EnemyDamagePercent : 0f;
            float scale = spawn.Difficulty * RunDifficulty;
            float healthNow = _horde != null ? _horde.HealthMultiplier : 1f;
            float damageNow = _horde != null ? _horde.DamageMultiplier : 1f;
            float damageBonus = FloorScaling.DamageBonus(FloorDamageMultiplier * scale * HordeRule.DamageShare * damageNow,
                damagePercent);
            SpawnedEnemy enemy = CreateEnemy(definition, $"Bell {definition.Prefab.name} {spawn.Slot + 1}",
                FloorHealthMultiplier(false) * scale * HordeRule.HealthShare * healthNow, healthPercent, damageBonus, 0f,
                out float reach, pooled: true);
            enemy.Slot = spawn.Slot;
            enemy.Bell = true;
            enemy.Gold = 0;
            enemy.Experience = _horde != null && !HordeClock.PaysExperience(_horde.Seconds)
                ? 0
                : OutworksRule.HordeExperience(definition.ExperienceReward);
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

        // OnEnemyDied for the phase's enemies: a Bell enemy's place fills again after its breath; the Keeper's Captain hands
        // over the key. True when the enemy was the phase's (its death fully answered here).
        private bool OnOutworksEnemyDied(SpawnedEnemy enemy)
        {
            if (!enemy.Bell && !enemy.Keeper)
                return false;
            enemy.Defeated = true;
            _lastDefeated = enemy;
            AliveEnemyCount--;
            if (enemy.Keeper && IsInOutworks && _wave.Count > 0 && ReferenceEquals(_wave[0], enemy))
            {
                _outworks.KeeperFell();
                KeeperFell?.Invoke();
            }
            if (enemy.Bell && _bell != null)
                _bell.Fell(enemy.Slot);
            if (AliveEnemyCount == 0 && !enemy.Bell)
                _fight.Clear();
            EnemyDefeated?.Invoke(enemy.Health);
            ProgressChanged?.Invoke();
            return true;
        }

        // The stair held: the Bell's swarm and anything of the Keeper's leave, and the floor's first hall begins where the
        // hero stands, the horde and the floor's clock running on.
        private void LeaveOutworks()
        {
            _bell = null;
            _keeperPack = false;
            ReleaseWave(true);
            TrimBodyPools();
            Array.Clear(_swarmCandidates, 0, _swarmCandidates.Length);
            SetWaveTargets(Array.Empty<Health>());
            OutworksLeft?.Invoke();
            AdvanceFloor();
        }

        // For tests: every phase enemy's own name in the scene, as its kind (the Keeper's pack, the Bell's swarm) - null for
        // an enemy that is neither or no longer stands.
        internal string OutworksKindOf(Health enemy)
        {
            for (int i = 0; i < _wave.Count; i++)
            {
                if (!ReferenceEquals(_wave[i].Health, enemy))
                    continue;
                return _wave[i].Keeper ? "keeper" : _wave[i].Bell ? "bell" : null;
            }
            return null;
        }

        // For tests and captures: the phase's clock moved on by seconds (to the Keeper's walk-on, to the Bell), and the
        // floor's horde clock with it, so the floor keeps one clock - as DevelopmentStart's floor clock moves the horde's.
        // Nothing outside the phase.
        internal void SkipOutworksClock(float seconds)
        {
            if (!IsInOutworks || !(seconds > 0f))
                return;
            _horde?.SkipClock(seconds);
            if (_outworks.Tick(seconds))
                BeginBell();
        }
    }
}
