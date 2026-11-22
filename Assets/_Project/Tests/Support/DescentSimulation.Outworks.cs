using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // docs/62's P0a in the foundry simulation (2026-11-22): the Outworks phase before floor 1's halls, as the scene plays it
    // behind development/outworks.txt (EncounterController's Outworks, HeroMovementInput's leap). Off by default
    // (FoundryLevers.Outworks), so every shipped number stays what it was; nothing here runs unless a run asks for it.
    //
    // What it mirrors, frame by frame, in the scene's order: the walk step carries an airborne hero along its pad's arc
    // and, after a walk, launches it from a pad whose centre it has stood still on for the pad's beat (HeroMovementInput,
    // HeroLeap); EncounterController steps
    // the wave's pack - the Keeper's on its loop until it turns, or the Bell's swarm - and the horde, whose state follows the
    // phase's clock (OutworksRule.HordeStateAt), picks up the drops, then ticks the phase: the clock (the Bell), the Keeper
    // walking on, the anvils where the hero stands (a capture grants a level-up's offer, RunState.GrantBonusUpgrade), the
    // Bell's swarm and the stair's hold, after which the floor's first hall begins where the hero stands, on the same
    // floor clock. A hit on an airborne hero never lands (the scene's guard on the hero's Health).
    //
    // The pilot (OutworksPilot, the scene autopilot's own choice): leaving - after the Bell, or with the key once done - the
    // stair before anything else; otherwise, after the kite, the Keeper or the nearest anvil, by a pad when the pad saves
    // the walk (OutworksRule.PadShortcut) and the cohort takes pads, with the drops beside the way as between halls.
    internal static partial class DescentSimulation
    {
        public const string OutworksPhaseName = "outworks";
        public const string BellPhaseName = "bell";
        // The realistic pilot's notice of the anvils, a purpose of its own so no other draw moves (PilotDice is 4101).
        public const int OutworksNoticePurpose = 4162;

        internal sealed partial class FoundryLevers
        {
            // docs/62's P0a: floor 1 begins with the Outworks phase in the court, then its halls (development/outworks.txt
            // in the scene). Off by default. With it, the trim docs/62 section 12 proposes is FoundryLevers.OutworksTrim's
            // knobs (TrimmedForOutworks), not these levers' defaults.
            public bool Outworks;
            // The phase's numbers against OutworksRule's: the Bell's time, the Keeper's health and experience shares, and the
            // horde's experience share while the phase runs (the doc's one parity lever; HordeExperienceShare otherwise).
            public float OutworksBellSeconds = OutworksRule.BellSeconds;
            public float OutworksKeeperHealthShare = OutworksRule.KeeperHealthShare;
            public float OutworksKeeperExperienceShare = OutworksRule.KeeperExperienceShare;
            public float OutworksHordeExperienceShare = OutworksRule.HordeExperienceShare;
            public float OutworksKeeperSeconds = OutworksRule.KeeperSpawnSeconds;
            // The realistic pilot's chance to notice an anvil off the screen (OutworksPilot.NoticeShare); 1 notices all.
            public float OutworksNoticeShare = OutworksPilot.NoticeShare;
        }

        // The shipped levers with the Outworks on and floor 1's halls trimmed as the scene's switch plays them
        // (OutworksRule.TrimmedHallSurges, TrimmedExtraElites, TrimmedGateSeconds).
        internal static FoundryLevers OutworksLevers(FoundryLevers shipped, string name = "outworks")
        {
            FoundryLevers levers = (shipped ?? new FoundryLevers()).Copy(name);
            levers.Outworks = true;
            levers.HallSurges = OutworksRule.TrimmedHallSurges;
            levers.ExtraElites = OutworksRule.TrimmedExtraElites;
            levers.GateNotBeforeSeconds = OutworksRule.TrimmedGateSeconds;
            return levers;
        }

        internal sealed partial class FoundryResult
        {
            // The Outworks phase (docs/62 P0a), -1 or 0 where it was not played: its length from the floor's first frame
            // to the stair held; the anvils captured; whether the Keeper walked on, fell (and when, on the phase's clock),
            // and whether its key took the hero out before the Bell; whether the Bell rang, the hero lived to see it, and
            // the walk from it to the stair held; the pads taken; the hero's level and health share as it held the stair;
            // the phase's kills and the Bell's swarm's spawns.
            public float OutworksSeconds = -1f;
            public int AnvilsCaptured;
            public bool KeeperSpawned;
            public bool KeeperKilled;
            public float KeeperKilledAt = -1f;
            public bool KeyUsed;
            public bool BellRung;
            public bool BellEntered;
            public float BellEscapeSeconds = -1f;
            public int PadLaunches;
            public int LevelAtStair = -1;
            public float HealthAtStair = -1f;
            public int OutworksKills;
            public int BellSpawned;
            public int HitsTurnedAirborne;
        }

        private sealed partial class Foe
        {
            // Of the Keeper's pack (its first, the Captain, carries the key), and of the Bell's swarm.
            public bool Keeper;
            public bool Bell;
        }

        private sealed partial class FoundryRun
        {
            private OutworksPhase _outworks;
            private HeroLeap _leap;
            private SwarmDirector _bell;
            private readonly List<SwarmDirector.Spawn> _bellSpawns = new List<SwarmDirector.Spawn>(PackLayout.MaxPackSize);
            private bool _keeperPack;
            private bool _keeperStruck;
            private bool _keeperHunting;
            private float _stairHeroX;
            private float _stairHeroY;
            private bool[] _anvilNoticed;
            private RunRandom _notice;
            private OutworksPilot.Habits _outworksHabits;

            private bool InOutworks => _outworks != null && !_outworks.Left;

            // The horde's experience share a spawn pays: the phase's lever while it runs (the doc's one parity lever), the
            // run's everywhere else - the same value at the default levers.
            private float HordeExperienceShareNow =>
                InOutworks ? _levers.OutworksHordeExperienceShare : _levers.HordeExperienceShare;

            // EncounterController.BeginFirstEncounter with the switch on: on the run's first floor the phase begins instead
            // of the first hall. False, and nothing touched, without the lever or on a later floor.
            private bool BeginOutworks()
            {
                if (!_levers.Outworks || _floor.FloorNumber != 1 || _outworks != null)
                    return false;
                _outworks = new OutworksPhase(_levers.OutworksBellSeconds, _levers.OutworksKeeperSeconds);
                _leap = new HeroLeap();
                _stairHeroX = HeroX;
                _stairHeroY = HeroY;
                _outworksHabits = OutworksPilot.Of(_levers.Cohort);
                _anvilNoticed = new bool[_outworks.Anvils.Count];
                for (int i = 0; i < _anvilNoticed.Length; i++)
                    _anvilNoticed[i] = true;
                if (_realism != null && _levers.OutworksNoticeShare < 1f)
                {
                    _notice = RunRandom.Stream(_run.Seed, OutworksNoticePurpose, 0);
                    for (int i = 0; i < _anvilNoticed.Length; i++)
                        _anvilNoticed[i] = (_notice.Next() >> 8) * (1f / 16777216f) < _levers.OutworksNoticeShare;
                }
                if (_trace != null)
                    Emit($"outworks_start level={_run.Level} hp={N(_heroHealth.Current)}");
                return true;
            }

            // HeroMovementInput: an airborne hero's step is its arc's (HeroLeap.Move), the stick and the walls apart. True
            // when the leap had the step.
            private bool LeapWalk() => _leap != null && _leap.IsAirborne && _leap.Move(_heroMotion, Step);

            // HeroMovementInput after the walk: a pad launches the hero once it has stood still on the pad's centre for the
            // pad's beat (HeroLeap.TryLaunch, one body), while the phase runs - the Bell's swarm too.
            private void LeapLaunch()
            {
                if (_leap == null || !InOutworks || !_heroHealth.IsAlive || _leap.IsAirborne)
                    return;
                if (!_leap.TryLaunch(HeroX, HeroY, Step))
                    return;
                _result.PadLaunches++;
                if (_trace != null)
                    Emit($"pad_launch pad={_leap.Pad} hp={N(_heroHealth.Current)}");
            }

            private bool Airborne => _leap != null && _leap.IsAirborne;

            // EncounterController.TickHorde in the phase: the state by its clock, not by halls cleared.
            private bool OutworksHordeState(out HordeState state)
            {
                state = HordeState.Ramp;
                if (!InOutworks)
                    return false;
                state = _outworks.BellRung ? HordeState.Pressure : OutworksRule.HordeStateAt(_outworks.Seconds);
                return true;
            }

            // EncounterController.MovePack: the Keeper's pack walks its loop until it turns on the hero.
            private bool KeeperPackGoal(out float goalX, out float goalY)
            {
                goalX = goalY = 0f;
                if (!InOutworks || !_keeperPack || _wave.Count == 0 || _wave[0].Defeated)
                {
                    if (InOutworks && _keeperPack && _wave.Count > 0 && _wave[0].Defeated)
                    {
                        // Its Captain has fallen: the escort hunts on as a wave.
                        RestoreKeeperPace();
                    }
                    return false;
                }
                if (!_outworks.KeeperGoal(_wave[0].X, _wave[0].Y, HeroX, HeroY, _keeperStruck, out goalX, out goalY))
                    return false;
                if (_outworks.Keeper == KeeperState.Hunting)
                    RestoreKeeperPace();
                return true;
            }

            private void RestoreKeeperPace()
            {
                if (_keeperHunting || _waveMotion == null)
                    return;
                _keeperHunting = true;
                for (int i = 0; i < _wave.Count; i++)
                    if (!_wave[i].Defeated && _wave[i].Keeper)
                        _waveMotion.SetSpeed(_wave[i].Slot, _wave[i].Definition.Speed);
            }

            // EncounterController's phase, after the horde and the drops: nothing advances behind an open choice; the clock
            // (the Bell), the Keeper walking on, the anvils, the Bell's swarm and the stair's hold. True while the phase runs,
            // so the rest of the floor's frame (a marked hall, the gate, the advance) waits.
            private bool OutworksFrame()
            {
                if (!InOutworks)
                    return false;
                if (_choices.IsOpen)
                    return true;
                if (_outworks.Tick(Step))
                    BeginBell();
                if (_outworks.KeeperDue)
                    SpawnKeeper();
                int captured = _outworks.StepAnvils(HeroX, HeroY, Step);
                if (captured >= 0)
                {
                    _result.AnvilsCaptured++;
                    if (_trace != null)
                        Emit($"anvil_capture anvil={captured} clock={N(_outworks.Seconds)} level={_run.Level} hp={N(_heroHealth.Current)}");
                    _run.GrantBonusUpgrade();
                }
                if (_bell != null)
                {
                    _bell.Tick(Step, _waveAlive, _bellSpawns);
                    for (int i = 0; i < _bellSpawns.Count; i++)
                        SpawnBellFoe(_bellSpawns[i]);
                }
                float heroX = HeroX;
                float heroY = HeroY;
                bool moved = heroX != _stairHeroX || heroY != _stairHeroY;
                _stairHeroX = heroX;
                _stairHeroY = heroY;
                if (_outworks.StepStair(Step, heroX, heroY, moved))
                    EndOutworks();
                return true;
            }

            // The Keeper walks on (EncounterController.SpawnKeeper): the floor's first elite room's first wave round the
            // loop's waypoint farthest from the hero, its Captain at the Keeper's share of health and pay, the pack at its
            // patrol pace, an elite wave (the Daggers' share, the elite's loot) that raises no room's start.
            private void SpawnKeeper()
            {
                int start = OutworksRule.KeeperStart(HeroX, HeroY);
                _outworks.KeeperArrived(start);
                _result.KeeperSpawned = true;
                int eliteRoom = Array.IndexOf(_floor.Kinds, RoomKind.Elite);
                Enemy[] definitions = _floor.Rooms[eliteRoom][0];
                ReleaseWave();
                _nonCombat = false;
                int n = _floor.FloorNumber;
                float scale = _heroStats.Difficulty;
                float damageBonus = FloorScaling.DamageBonus(FloorScaling.Tiered(ClimbScaling.Damage(n), _tierDamage) * scale,
                    _floor.ModifierDamagePercent);
                float floorHealth = FloorScaling.Tiered(ClimbScaling.Health(n), _tierHealth);
                OutworksRule.KeeperWaypoint(start, out float kx, out float ky);
                int count = definitions.Length;
                _waveMotion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing, Map, kx, ky);
                FoundryMap.PlacePack(_waveOrdinal, count, kx, ky, BodySpacing, _placements);
                for (int i = 0; i < count; i++)
                {
                    Enemy definition = definitions[i];
                    bool captain = i == 0;
                    float healthScale = floorHealth * scale * (captain ? _levers.OutworksKeeperHealthShare : 1f);
                    Foe foe = CreateFoe(definition,
                        FloorScaling.Health(definition.Health, healthScale, _floor.ModifierHealthPercent), damageBonus, false);
                    int gold = FloorScaling.Gold(definition.Gold, _floor.ModifierGoldPercent, _tier);
                    foe.Gold = captain ? OutworksRule.KeeperGold(gold) : gold;
                    foe.Experience = captain
                        ? OutworksRule.KeeperExperience(definition.Experience, _levers.OutworksKeeperExperienceShare)
                        : definition.Experience;
                    foe.Motion = _waveMotion;
                    foe.Slot = _waveMotion.Add(foe, _placements[i], definition.Speed * OutworksRule.KeeperPatrolShare,
                        foe.Weapon.Range);
                    foe.Elite = true;
                    foe.EliteLeader = captain;
                    foe.Keeper = true;
                    if (captain)
                        _elites.Add(foe);
                    _wave.Add(foe);
                }
                _eliteWave = true;
                _gateBossWave = false;
                _keeperPack = true;
                _keeperStruck = false;
                _keeperHunting = false;
                _waveAlive = count;
                _waveOrdinal++;
                _fight.Begin();
                _waveStartedAt = _seconds;
                if (_trace != null)
                    Emit($"keeper_spawn waypoint={start} clock={N(_outworks.Seconds)} level={_run.Level}");
            }

            // The Bell (EncounterController.BeginBell): a Keeper still standing leaves the court, and the swarm rises in
            // the wave's pack - SwarmDirector over OutworksRule's ladder, its first wave walking in round the hero.
            private void BeginBell()
            {
                _result.BellRung = true;
                _result.BellEntered = _heroHealth.IsAlive;
                if (_trace != null)
                    Emit($"bell clock={N(_outworks.Seconds)} level={_run.Level} hp={N(_heroHealth.Current)} key={_outworks.KeyHeld}");
                ReleaseWave();
                _keeperPack = false;
                _eliteWave = false;
                _gateBossWave = false;
                _bell = new SwarmDirector(OutworksRule.BellSizes());
                _waveMotion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing, Map,
                    Airborne ? _leap.LandingX : HeroX, Airborne ? _leap.LandingY : HeroY);
                _waveAlive = 0;
                _bell.Open(_bellSpawns);
                for (int i = 0; i < _bellSpawns.Count; i++)
                    SpawnBellFoe(_bellSpawns[i]);
                _fight.Begin();
                _waveStartedAt = _seconds;
            }

            // EncounterController.SpawnBellEnemy: one enemy into one place, round the hero, the horde's role the ladder
            // names, as fodder as the horde's (its shares, the floor's clock grown) hardened by the swarm's ramp; it pays the
            // horde's share of experience and no gold.
            private void SpawnBellFoe(SwarmDirector.Spawn spawn)
            {
                int role = OutworksRule.BellRole(spawn.Wave, spawn.Index, _floor.HordeRoles.Length);
                Enemy definition = _floor.HordeRoles[role];
                // Round the hero, or round its landing while it is mid-arc over the pit (no floor round it there).
                float groundX = Airborne ? _leap.LandingX : HeroX;
                float groundY = Airborne ? _leap.LandingY : HeroY;
                EntrySides.Place(_waveOrdinal, 1, groundX, groundY, Map, EntryDepth, FormationSpacing, BodySpacing, _placements);
                _waveOrdinal++;
                int n = _floor.FloorNumber;
                float scale = spawn.Difficulty * _heroStats.Difficulty;
                float healthNow = _horde != null ? HordeHealthNow : 1f;
                float damageNow = _horde != null ? HordeDamageNow : 1f;
                float damageBonus = FloorScaling.DamageBonus(
                    FloorScaling.Tiered(ClimbScaling.Damage(n), _tierDamage) * scale * HordeRule.DamageShare * damageNow,
                    _floor.ModifierDamagePercent);
                Foe foe = CreateFoe(definition,
                    FloorScaling.Health(definition.Health,
                        FloorScaling.Tiered(ClimbScaling.Health(n), _tierHealth) * scale * _levers.HordeHealthShare * healthNow,
                        _floor.ModifierHealthPercent),
                    damageBonus, false);
                foe.Gold = 0;
                foe.Experience = _horde != null && !HordeClock.PaysExperience(HordeClockNow)
                    ? 0
                    : TunedHordeExperience.Experience(definition.Experience, _levers.OutworksHordeExperienceShare);
                foe.Bell = true;
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
                _result.BellSpawned++;
            }

            // FoeDied for the phase's enemies: a Bell enemy's place fills again after its breath and its kill drops as the
            // horde's; the Keeper's Captain hands over the key. False, and nothing done, for any other.
            private bool OutworksFoeDied(Foe foe)
            {
                if (!foe.Bell && !foe.Keeper)
                    return false;
                if (InOutworks)
                    _result.OutworksKills++;
                if (foe.Keeper && foe.EliteLeader && InOutworks)
                {
                    _outworks.KeeperFell();
                    _result.KeeperKilled = true;
                    _result.KeeperKilledAt = _outworks.Seconds;
                    if (_trace != null)
                        Emit($"keeper_killed clock={N(_outworks.Seconds)} level={_run.Level} hp={N(_heroHealth.Current)}");
                }
                _waveAlive--;
                _result.WaveKills++;
                if (foe.Bell && _bell != null)
                    _bell.Fell(foe.Slot);
                if (_waveAlive == 0 && !foe.Bell)
                    _fight.Clear();
                _rewards.TryDropKill(foe, foe.Experience, foe.Gold, foe.X, foe.Y, _drops);
                return true;
            }

            // A strike on the Keeper's pack turns it on the hero.
            private void KeeperStruck(Foe foe)
            {
                if (foe.Keeper && InOutworks)
                    _keeperStruck = true;
            }

            // The stair held (EncounterController.LeaveOutworks): the Bell's swarm and anything of the Keeper's leave, and
            // the floor's first hall begins where the hero stands, the horde and its clock running on.
            private void EndOutworks()
            {
                _result.OutworksSeconds = FloorTime;
                _result.KeyUsed = _outworks.KeyHeld && !_outworks.BellRung;
                _result.BellEscapeSeconds = _outworks.BellRung ? _outworks.Seconds - _outworks.BellAt : -1f;
                _result.LevelAtStair = _run.Level;
                _result.HealthAtStair = _heroHealth.Current / Math.Max(1f, _heroHealth.Maximum);
                if (_trace != null)
                    Emit($"stair_held clock={N(_outworks.Seconds)} level={_run.Level} hp={N(_heroHealth.Current)} anvils={_outworks.Anvils.Captured} key={_outworks.KeyHeld} bell={_outworks.BellRung}");
                _bell = null;
                _keeperPack = false;
                ReleaseWave();
                _eliteWave = false;
                _gateBossWave = false;
                AdvanceFloor();
            }

            // A death in the phase: before the Bell, or after it.
            private bool RecordOutworksDeath()
            {
                if (!InOutworks)
                    return false;
                _result.DeathPhase = _outworks.BellRung ? BellPhaseName : OutworksPhaseName;
                return true;
            }

            // DevelopmentAutopilot in the phase, first of all: leaving - the stair, before the kite, as the finale's exit is.
            private bool OutworksLeaveGoal(float x, float y, out float gx, out float gy, out float stop)
            {
                gx = x;
                gy = y;
                stop = OutworksPilot.StairStop;
                if (!InOutworks || !OutworksPilot.Leaving(_outworks, _outworksHabits, x, y, _anvilNoticed))
                    return false;
                Notice(x, y);
                gx = _outworks.StairX;
                gy = _outworks.StairY;
                ByPad(x, y, ref gx, ref gy, ref stop);
                _doing = PilotDoing.Walk;
                return true;
            }

            // ...and after the kite: the Keeper or the nearest anvil, by a pad when it saves the walk, with the drops beside
            // the way as between halls; nothing (the altars, the drops) when the pilot has no objective.
            private bool OutworksObjectiveGoal(float x, float y, out float gx, out float gy, out float stop)
            {
                gx = x;
                gy = y;
                stop = PilotArrived;
                if (!InOutworks)
                    return false;
                Notice(x, y);
                bool keeperStands = _keeperPack && _wave.Count > 0 && !_wave[0].Defeated &&
                                    (_outworks.Keeper == KeeperState.Patrolling || _outworks.Keeper == KeeperState.Hunting);
                float keeperX = keeperStands ? _wave[0].X : 0f;
                float keeperY = keeperStands ? _wave[0].Y : 0f;
                OutworksPilot.Objective objective = OutworksPilot.Choose(_outworks, _outworksHabits, x, y, keeperStands,
                    keeperX, keeperY, _anvilNoticed, out float wx, out float wy, out stop);
                if (objective == OutworksPilot.Objective.None)
                {
                    // Nothing to go for: the altars, then the drops in sight, else hold ground and fight where it stands -
                    // never the pit's lure (as P0a was measured), nor a Keeper still on its loop taken for a fight.
                    if (NearestAltar(x, y, out gx, out gy))
                    {
                        stop = .1f;
                        _doing = PilotDoing.Walk;
                        return true;
                    }
                    if (DropDetourRule.Nearest(_drops, x, y, PilotDropDetour, out gx, out gy))
                    {
                        stop = DropRule.PickupRadius * .5f;
                        _doing = PilotDoing.Drop;
                        return true;
                    }
                    gx = x;
                    gy = y;
                    stop = PilotArrived;
                    _doing = _waveAlive + _hordeAlive > 0 ? PilotDoing.Fight : PilotDoing.Still;
                    return true;
                }
                float before = stop;
                gx = wx;
                gy = wy;
                ByPad(x, y, ref gx, ref gy, ref stop);
                bool byPad = stop != before;
                // Drops beside the way to an objective, not while charging in a ring.
                bool inRing = objective == OutworksPilot.Objective.Anvil &&
                              OutworksRule.Within(x, y, wx, wy, OutworksRule.AnvilRadius);
                if (!byPad && !inRing && _detour.Choose(_drops, x, y, wx, wy, Step, out float dx, out float dy))
                {
                    gx = dx;
                    gy = dy;
                    stop = DropRule.PickupRadius * .5f;
                    _doing = PilotDoing.Drop;
                    return true;
                }
                _doing = objective == OutworksPilot.Objective.Keeper ? PilotDoing.Fight : PilotDoing.Walk;
                return true;
            }

            // A goal across the pit taken by a pad when the cohort takes pads and the pad saves the walk: the pad's launch,
            // stopped on well inside its centre (OutworksPilot.PadStop), where the hero stands for the pad's beat.
            private void ByPad(float x, float y, ref float gx, ref float gy, ref float stop)
            {
                if (!_outworksHabits.UsesPads || Airborne)
                    return;
                int pad = OutworksRule.PadShortcut(x, y, gx, gy);
                if (pad < 0)
                    return;
                OutworksRule.PadLaunch(pad, out gx, out gy);
                stop = OutworksPilot.PadStop;
            }

            // A realistic pilot that missed an off-screen anvil notices it once it comes on the screen.
            private void Notice(float x, float y)
            {
                if (_notice == null)
                    return;
                for (int i = 0; i < _anvilNoticed.Length; i++)
                    if (!_anvilNoticed[i] && OutworksRule.Within(x, y, _outworks.Anvils.XOf(i), _outworks.Anvils.YOf(i),
                            OutworksPilot.OnScreenReach))
                        _anvilNoticed[i] = true;
            }
        }
    }
}
