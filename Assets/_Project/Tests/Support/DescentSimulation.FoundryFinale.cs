using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Tests
{
    // The foundry's finale in the foundry simulation (FoundryLevers.Floors = 3): Data/Floors/Floor_FinalSwarm.asset, the
    // climb's last floor below Quicksilver Vaults' checkpoint, on the foundry's arena. What the scene does there, and so
    // what this plays, in DescentSimulation.Foundry's frame:
    //
    // - The checkpoint's Descend and its advance delay, as below floor 1; then EncounterController.BeginSwarm: one pack for
    //   the room's whole life, SwarmDirector - the one body the scene asks - saying which of its places fills, with which
    //   enemy of the asset's twelve-wave ladder and at which step of SwarmRule's ramp (GrowthPerWave every StepSeconds, the
    //   ladder's next wave every LadderSeconds, a fallen place refilled RespawnSeconds after its fall). Each enemy walks in
    //   alone round the hero, through the foundry's PlacePack (EntrySides.Place), at the floor's authored tier:
    //   Floor_FinalSwarm is not climb-scaled, so its enemies take 1 and 0.92, times the ramp and the run's difficulty.
    // - No horde: EncounterController.HordeWanted needs a floor with horde roles and no swarm room, and the finale has
    //   neither; the floor's clock reads 0 there. No pit: PitEruptionService runs only outside a swarm room. No gate boss:
    //   the swarm fills its wave without SpawnWave, and the gate's and the elite room's flags go with the wave released
    //   before it (main 6a09574f, the fix of a Warden made of the swarm's first enemy).
    // - A kill pays at once (CombatSetup.OnEnemyDefeated in a swarm room) and its level-ups wait for the room's sittings,
    //   one every SwarmRule.OfferEverySeconds of its clock (UpgradeService.Hold, OpenSitting).
    // - The way out: ExitRule.SpotFor on the foundry's geometry, left by standing still on it for ExitRule.DwellSeconds.
    //   Leaving releases the held level-ups and advances the floor; the swarm was its last room and no gate closes a floor
    //   that ends in one (EncounterController.GateClosesFloor), so FloorCleared ends the run in victory
    //   (CombatSetup.OnFloorCleared with no floor below), and the offers just released are withdrawn with the run.
    // - The development autopilot plays it with its cohort's habits as on every floor - it kites or stands, and never lures
    //   the swarm to the pit's rim, which does not run here - takes the floor's two altars, which answer again on a new floor,
    //   whenever nothing of the swarm stands, and from PilotSwarmStay seconds on the swarm's clock walks to the exit before
    //   anything else and holds it. Its "linger" and "idle" modes never apply in a swarm room (DevelopmentAutopilot.Lingering).
    internal static partial class DescentSimulation
    {
        // DevelopmentAutopilot.SwarmStay: the seconds of the swarm's clock the autopilot fights before it walks out.
        public const float PilotSwarmStay = 60f;
        // FoundryResult.DeathPhase of a death in the finale: fighting the swarm, or after the pilot turned for its exit.
        public const string SwarmFightPhase = "swarm";
        public const string SwarmExitPhase = "swarm exit";
        // FoundryResult.Killer's word for the swarm's enemies, where the floor's own reads "wave ".
        public const string SwarmHarm = "swarm ";

        // Data/Floors/Floor_FinalSwarm.asset as the foundry plays it: Floor_QuicksilverVaults' next floor and the climb's
        // last - one swarm room, the Unending Crowd, whose ladder is FinalSwarm's (the hall Descent's finale, which
        // DescentFlowTests holds to the asset) - at the asset's own multipliers rather than ClimbScaling's, with no
        // modifier, no horde, none of the foundry's knobs and nothing below it.
        public static FoundryFloor FoundryFinale() => new FoundryFloor
        {
            Id = "floor_final_swarm",
            Rooms = FinalSwarm.Rooms,
            Kinds = new[] { RoomKind.Swarm },
            HordeRoles = new Enemy[0],
            FloorNumber = 3,
            ClimbScaled = false,
            EnemyHealthMultiplier = FinalSwarm.HealthMultiplier,
            EnemyDamageMultiplier = FinalSwarm.DamageMultiplier
        };

        internal sealed partial class FoundryFloor
        {
            // FloorDefinition._climbScaled and its two authored multipliers: a climb-scaled floor's enemies take
            // ClimbScaling's tier by the floor's number, any other floor's the asset's own
            // (EncounterController.FloorHealthMultiplier, FloorDamageMultiplier). Floors 1 and 2 are climb-scaled and their
            // waves and horde read ClimbScaling directly; only the finale is not, and only its swarm reads these.
            public bool ClimbScaled = true;
            public float EnemyHealthMultiplier = 1f;
            public float EnemyDamageMultiplier = 1f;
        }

        internal sealed partial class FoundryLevers
        {
            // Research only (docs/41, 2026-11-11, the finale's stay; FinaleStayResearch): the seconds of the finale's swarm
            // clock after which the pilot turns for its exit, instead of the development autopilot's SwarmStay
            // (PilotSwarmStay, the default) - a player who stays for more of the ladder and its ramp, or leaves sooner. Read
            // by this simulation alone; the scene's autopilot always stays its 60 s.
            public float FinaleStaySeconds = PilotSwarmStay;
        }

        internal sealed partial class FoundryResult
        {
            // The finale's swarm room (FoundryFinale); on every other floor these keep their defaults. The swarm's clock as
            // the floor ended - its exit held, the hero fallen or the floor's limit - and -1 where it never opened; the
            // ramp's step it reached (one-based, SwarmRule.StepSeconds), what an enemy walking in then was hardened by
            // (SwarmRule.DifficultyAtSeconds), and the ladder's wave the clock had reached (one-based).
            public float SwarmSeconds = -1f;
            public int SwarmStep;
            public float SwarmDifficulty;
            public int SwarmLadderWave;
            // Enemies that walked in, the opening's three included; the most that stood at once; the level-up sittings
            // the room opened.
            public int SwarmSpawned;
            public int SwarmMaxAlive;
            public int Sittings;
            // The hero as the swarm opened: its level, health, maximum and the run's difficulty.
            public int SwarmEntryLevel = -1;
            public float SwarmEntryHealth = -1f;
            public float SwarmEntryMaximum = -1f;
            public float SwarmEntryDifficulty = -1f;
            // Left by the exit; the seconds from the pilot's turn for it (its stay on the swarm's clock, PilotSwarmStay
            // unless FoundryLevers.FinaleStaySeconds says otherwise) to leaving, -1 without a leave; where the hero stood as
            // it left; and the run's end the exit brought, Victory below a floor with nothing beneath it (RunOutcome.None
            // otherwise: a death's defeat is the Outcome's).
            public bool WalkedOut;
            public float SwarmExitSeconds = -1f;
            public float LeftAtX;
            public float LeftAtY;
            public RunOutcome RunEnd;
            // Frames of the swarm on which a gate boss or an elite room's Captain stood, or the Warden's slam clock held a
            // boss: 0 in the scene since main 6a09574f, and here.
            public int GateBossFrames;
        }

        private sealed partial class FoundryRun
        {
            // EncounterController's swarm room: its director, the room's ladder, the spawns a frame, where the hero stood
            // at the last swarm frame (the exit's hold counts only while it stands still), the ramp's step reported last,
            // and when the pilot turned for the exit (PilotSwarmStay on the swarm's clock), in run seconds. Null and -1
            // outside a swarm room.
            private SwarmDirector _swarm;
            private Enemy[][] _swarmLadder;
            private readonly List<SwarmDirector.Spawn> _swarmSpawns = new List<SwarmDirector.Spawn>(PackLayout.MaxPackSize);
            private float _swarmHeroX;
            private float _swarmHeroY;
            private int _swarmStep = -1;
            private float _swarmTurnedAt = -1f;

            // DevelopmentAutopilot.Goal: the finale's exit is open from the start, and the autopilot fights the swarm
            // SwarmStay seconds of its clock first, as a player after depth does; then the exit, before anything else. The
            // research lever FoundryLevers.FinaleStaySeconds stays longer or shorter.
            private bool LeavesSwarm => _swarm != null && _swarm.Seconds >= _levers.FinaleStaySeconds;

            // EncounterController.EndlessOf: a swarm room's waves are a ladder that never runs out; null for a floor
            // without one, the progress every floor before the finale had.
            private bool[] EndlessRooms()
            {
                bool[] endless = null;
                for (int i = 0; i < _floor.Kinds.Length; i++)
                    if (_floor.Kinds[i] == RoomKind.Swarm)
                        (endless ?? (endless = new bool[_floor.Kinds.Length]))[i] = true;
                return endless;
            }

            // EncounterController.AdvanceFloor: a swarm room opens as a swarm, never as a wave or a district.
            private bool BeginsSwarm(FloorStep step)
            {
                if (_floor.Kinds[step.RoomIndex] != RoomKind.Swarm)
                    return false;
                BeginSwarm(step.RoomIndex);
                return true;
            }

            // EncounterController.Update: in a swarm room the frame is TickSwarm's once the pack has stepped; the horde's
            // tick, which releases a horde no swarm room has, and the drops, which a swarm's kills never leave, are not
            // reached.
            private bool SwarmFrame()
            {
                if (_swarm == null)
                    return false;
                TickSwarm();
                return true;
            }

            // EncounterController.BeginSwarm: one pack for the room's whole life, the ladder's first wave walking in, the
            // way out placed, the level-ups held for the sittings, and the encounter started once - the chest and the bar
            // see one room.
            private void BeginSwarm(int roomIndex)
            {
                ReleaseHorde();
                ReleaseWave();
                // EncounterController.ReleaseWave since main 6a09574f: the gate's and the elite room's flags go with their
                // wave, so no enemy of the swarm is read as the gate boss floor 2 left behind, or as a Captain.
                _gateBossWave = false;
                _eliteWave = false;
                _nonCombat = false;
                _swarmLadder = _floor.Rooms[roomIndex];
                var sizes = new int[_swarmLadder.Length];
                for (int i = 0; i < sizes.Length; i++)
                    sizes[i] = _swarmLadder[i].Length;
                _swarm = new SwarmDirector(sizes);
                _waveMotion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing, Map, HeroX, HeroY);
                ExitRule.SpotFor(Map, out _exitX, out _exitY);
                _exitDwell = new ExitDwell();
                _swarmHeroX = HeroX;
                _swarmHeroY = HeroY;
                _swarmStep = -1;
                _swarmTurnedAt = -1f;
                _waveAlive = 0;
                _result.SwarmEntryLevel = _run.Level;
                _result.SwarmEntryHealth = _heroHealth.Current;
                _result.SwarmEntryMaximum = _heroHealth.Maximum;
                _result.SwarmEntryDifficulty = _heroStats.Difficulty;
                _upgrades.Hold();

                _swarm.Open(_swarmSpawns);
                for (int i = 0; i < _swarmSpawns.Count; i++)
                    SpawnSwarmFoe(_swarmSpawns[i]);
                _fight.Begin();
                _waveStartedAt = _seconds;
                ReportSwarmStep();
                ObserveSwarm();
                EncounterStarted();
                // Research only (docs/47, DescentSimulation.DescentLength.cs): nothing at the default levers.
                FinaleArrival();
            }

            // EncounterController.SpawnSwarmEnemy: one enemy walking into one place, its own one-enemy entry one corner
            // further round than the last, placed round wherever the hero stands now, at the floor's tier, the ramp's
            // difficulty it was born with and the run's; a fallen enemy's body leaves as its place fills again.
            private void SpawnSwarmFoe(SwarmDirector.Spawn spawn)
            {
                Enemy definition = _swarmLadder[spawn.Wave][spawn.Index];
                EntrySides.Place(_waveOrdinal, 1, HeroX, HeroY, Map, EntryDepth, FormationSpacing, BodySpacing, _placements);
                _waveOrdinal++;
                float scale = spawn.Difficulty * _heroStats.Difficulty;
                float damageBonus = FloorScaling.DamageBonus(SwarmDamageMultiplier() * scale, _floor.ModifierDamagePercent);
                Foe foe = CreateFoe(definition,
                    FloorScaling.Health(definition.Health, SwarmHealthMultiplier() * scale, _floor.ModifierHealthPercent),
                    damageBonus, false);
                foe.Gold = FloorScaling.Gold(definition.Gold, _floor.ModifierGoldPercent);
                foe.Experience = definition.Experience;
                foe.Motion = _waveMotion;
                foe.Slot = spawn.Slot;
                if (spawn.Slot < _waveMotion.Count)
                {
                    _wave[spawn.Slot].Released = true;
                    _waveMotion.Replace(spawn.Slot, foe, _placements[0], definition.Speed, foe.Weapon.Range);
                    _wave[spawn.Slot] = foe;
                }
                else
                {
                    _waveMotion.Add(foe, _placements[0], definition.Speed, foe.Weapon.Range);
                    _wave.Add(foe);
                }
                _waveAlive++;
                _result.SwarmSpawned++;
            }

            // EncounterController.FloorHealthMultiplier(false) and FloorDamageMultiplier: the climb's tier, or the floor's
            // authored one.
            private float SwarmHealthMultiplier() =>
                _floor.ClimbScaled ? ClimbScaling.Health(_floor.FloorNumber) : _floor.EnemyHealthMultiplier;

            private float SwarmDamageMultiplier() =>
                _floor.ClimbScaled ? ClimbScaling.Damage(_floor.FloorNumber) : _floor.EnemyDamageMultiplier;

            // EncounterController.TickSwarm, after the pack has stepped and before anything strikes: the clock moves on and
            // whatever is due walks in, the ramp's step is reported, the way out is read where the hero stands now - it
            // counts only while the hero stood still this frame, and leaving ends the room before anything strikes - and a
            // sitting due opens the level-ups the room held back.
            private void TickSwarm()
            {
                _swarm.Tick(Step, _waveAlive, _swarmSpawns);
                for (int i = 0; i < _swarmSpawns.Count; i++)
                    SpawnSwarmFoe(_swarmSpawns[i]);
                ReportSwarmStep();
                ObserveSwarm();

                float heroX = HeroX;
                float heroY = HeroY;
                bool moved = heroX != _swarmHeroX || heroY != _swarmHeroY;
                _swarmHeroX = heroX;
                _swarmHeroY = heroY;
                if (_exitDwell.Step(Step, !moved && ExitRule.IsWithinReach(heroX, heroY, _exitX, _exitY)))
                {
                    LeaveSwarm();
                    return;
                }
                if (_swarm.OfferDue)
                {
                    _upgrades.OpenSitting();
                    _result.Sittings++;
                }
            }

            // EncounterController.ReportSwarmStep, the telemetry's swarm_step: once as the room opens and at each step of the
            // ramp.
            private void ReportSwarmStep()
            {
                int step = (int)(_swarm.Seconds / SwarmRule.StepSeconds);
                if (step == _swarmStep)
                    return;
                _swarmStep = step;
                _result.SwarmStep = step + 1;
                _result.SwarmDifficulty = SwarmRule.DifficultyAtSeconds(_swarm.Seconds);
                if (_trace != null)
                    Emit($"swarm_step step={step + 1} seconds={N(_swarm.Seconds)} difficulty={N(_result.SwarmDifficulty)} hp={N(_heroHealth.Current)} level={_run.Level}");
            }

            // The swarm's accounting, each swarm frame: its clock and the ladder's wave, the crowd, the pilot's turn for the
            // exit, and whether anything of it is read as a gate boss or a Captain (never, since main 6a09574f).
            private void ObserveSwarm()
            {
                _result.SwarmSeconds = _swarm.Seconds;
                _result.SwarmLadderWave = _swarm.Wave + 1;
                if (_waveAlive > _result.SwarmMaxAlive)
                    _result.SwarmMaxAlive = _waveAlive;
                if (GateBoss != null || _slamBoss != null || _eliteWave)
                    _result.GateBossFrames++;
                if (_swarmTurnedAt < 0f && LeavesSwarm)
                    _swarmTurnedAt = _seconds;
            }

            // EncounterController.LeaveSwarm, then the floor's advance: the room ends, the pack goes with it, the held
            // level-ups are offered, and the swarm being the floor's last room with no gate to close it, FloorCleared -
            // CombatSetup.OnFloorCleared sweeps the drops and, on the climb's last floor, ends the run in victory, which
            // withdraws the offers just made.
            private void LeaveSwarm()
            {
                _result.WalkedOut = true;
                _result.LeftAtX = HeroX;
                _result.LeftAtY = HeroY;
                _result.SwarmExitSeconds = _swarmTurnedAt >= 0f ? _seconds - _swarmTurnedAt : -1f;
                if (_trace != null)
                    Emit($"swarm_exit seconds={N(_swarm.Seconds)} step={_swarmStep + 1} hp={N(_heroHealth.Current)} level={_run.Level} kills={_result.WaveKills}");
                _swarm = null;
                _progress.LeaveEndlessRoom();
                ReleaseWave();
                _upgrades.Release();

                _gateHeld = false;
                if (_progress.Advance().Kind != FloorStepKind.Cleared)
                    throw new InvalidOperationException("The foundry simulation plays a swarm room only as its floor's last room, the finale.");
                _result.DropsSwept += _drops.CollectAll();
                _cleared = true;
                if (_trace != null)
                    Emit($"room_complete room={_roomIndex + 1} active={N(_seconds - _roomStartedAt)} kills={_roomKills} hp={N(_heroHealth.Current)} (floor cleared, level {_run.Level})");
                if (_authored.Next != null)
                    return;
                _run.End(RunOutcome.Victory);
                _result.RunEnd = _run.Outcome;
                if (_trace != null)
                    Emit($"run_end victory level={_run.Level} hp={N(_heroHealth.Current)}");
            }

            // EncounterController.OnEnemyDied in a swarm room - the place fills again after its breath, and the room never
            // clears however empty it gets - and CombatSetup.OnEnemyDefeated there: the kill pays at once.
            private void SwarmFoeDied(Foe foe)
            {
                _waveAlive--;
                _result.WaveKills++;
                _swarm.Fell(foe.Slot);
                _rewards.TryAwardKill(foe, foe.Experience, foe.Gold);
            }

            // A death in the swarm: fighting it, or after the pilot turned for the exit; the killing blow named as the
            // swarm's.
            private void RecordSwarmDeath()
            {
                _result.DeathPhase = LeavesSwarm ? SwarmExitPhase : SwarmFightPhase;
                const string wave = "wave ";
                if (_lastHarm != null && _lastHarm.StartsWith(wave, StringComparison.Ordinal))
                    _result.Killer = SwarmHarm + _lastHarm.Substring(wave.Length);
            }
        }
    }
}
