using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The foundry floor of the Descent simulation (docs/41 step 5): it plays a seed the same way every time, it never
    // stands more enemies than the scene's cap, its levers move only what they name, and where it had to copy a rule of the
    // game to make a lever of it - the horde director's targets and fill rate, the horde's experience share - the copy is
    // held to the game's own body at the shipped values. Balance is not pinned here: the floor is being tuned, and the
    // batch report (Tools/CombatChecks/FoundryBatchReport.cs) reads it.
    public sealed class FoundrySimulationTests
    {
        // Two of the Editor measurement's seeds: a floor is about a second of the pure runner's time.
        private static readonly int[] Seeds = { 11, 33 };

        [Test]
        public void ASeedPlaysTheSameFloorEveryTime()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            {
                DescentSimulation.FoundryResult first = DescentSimulation.RunFoundry(hero, 11);
                DescentSimulation.FoundryResult second = DescentSimulation.RunFoundry(hero, 11);
                Assert.That(second.FloorSeconds, Is.EqualTo(first.FloorSeconds), hero.Name);
                Assert.That(second.Outcome, Is.EqualTo(first.Outcome), hero.Name);
                Assert.That(second.Picks, Is.EqualTo(first.Picks), hero.Name);
                Assert.That(second.HordeKills, Is.EqualTo(first.HordeKills), hero.Name);
                Assert.That(second.HeroHealth, Is.EqualTo(first.HeroHealth), hero.Name);
                Assert.That(second.RoomSeconds, Is.EqualTo(first.RoomSeconds), hero.Name);
            }
        }

        // The rooms in order, the walks to the districts, the Warden at the gate and the exit held: whatever the outcome,
        // the floor's accounting adds up.
        [Test]
        public void TheFloorIsPlayedThroughItsRoomsAndAccountedForWhole()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed);
                string where = $"{hero.Name} seed {seed}";
                Assert.That(run.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout), where);
                float rooms = run.RoomSeconds.Where(s => s >= 0f).Sum();
                Assert.That(rooms, Is.EqualTo(run.FloorSeconds).Within(0.05f), where + ": the rooms are the floor");
                Assert.That(run.RoomSeconds[0], Is.GreaterThan(0f), where);
                Assert.That(run.WalkSeconds[0], Is.EqualTo(-1f), where + ": the first hall is fought where the floor begins");
                if (run.Cleared)
                {
                    Assert.That(run.WalkSeconds[1], Is.GreaterThan(0f), where + ": the second hall is walked to");
                    Assert.That(run.BossSeconds, Is.GreaterThan(DescentSimulation.RevealTurnSeconds * 2f + DescentSimulation.RevealHoldSeconds), where);
                    Assert.That(run.ExitSeconds, Is.GreaterThanOrEqualTo(ExitRule.DwellSeconds), where + ": the gate is held");
                    Assert.That(run.LevelAtGate, Is.GreaterThan(0), where);
                    Assert.That(run.Level, Is.GreaterThanOrEqualTo(run.LevelAtGate), where);
                    Assert.That(run.WardenHealth, Is.EqualTo(DescentSimulation.Warden.Health * WardenRule.GateHealthShare * run.DifficultyAtGate).Within(1e-3f), where);
                }
                else
                {
                    Assert.That(run.DeathRoom, Is.InRange(1, run.RoomSeconds.Length), where);
                    Assert.That(run.HeroHealth, Is.EqualTo(0f), where);
                }
                Assert.That(run.Uptime, Is.InRange(0f, 1f), where);
                Assert.That(run.Engaged, Is.InRange(0f, run.Uptime + 1e-4f), where + ": engaged is a part of the uptime");
                Assert.That(run.HordeMaxAlive + PackLayout.MaxFormationSize, Is.LessThanOrEqualTo(HordeRule.SceneCap + PackLayout.MaxFormationSize), where);
                Assert.That(run.HordeMaxAlive, Is.LessThanOrEqualTo(HordeRule.SceneCap), where + ": the scene's cap");
                Assert.That(run.FirstStrikeSeconds, Is.InRange(0f, 10f), where + ": the first hall's first wave is fought at once");
            }
        }

        // A run plays floor 1 alone unless its levers ask for more (FoundryLevers.Floors), and asking changes nothing before
        // the checkpoint: a seed's floor 1 is the same floor, field for field, whether or not the run goes on to floor 2. On
        // the floor as it ships both heroes die on floor 1 on seed 2 and clear it on seed 11.
        [Test]
        public void FloorOneIsTheSameFloorWhetherOrNotTheRunGoesOnToFloorTwo()
        {
            Assert.That(FoundryBatch.Shipped().Floors, Is.EqualTo(1), "the shipped floor is floor 1 alone");
            DescentSimulation.FoundryLevers two = FoundryBatch.Shipped();
            two.Floors = 2;
            int wentDown = 0;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in new[] { 2, 11 })
            {
                string where = $"{hero.Name} seed {seed}";
                DescentSimulation.FoundryResult alone = DescentSimulation.RunFoundry(hero, seed);
                DescentSimulation.FoundryResult climb = DescentSimulation.RunFoundry(hero, seed, two);
                Assert.That(alone.NextFloor, Is.Null, where + ": one floor");
                foreach (FieldInfo field in typeof(DescentSimulation.FoundryResult).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (field.Name != nameof(DescentSimulation.FoundryResult.NextFloor))
                        Assert.That(field.GetValue(climb), Is.EqualTo(field.GetValue(alone)), $"{where}: {field.Name}");
                Assert.That(climb.NextFloor != null, Is.EqualTo(climb.Cleared), where + ": a clear goes down, anything else ends the run");
                if (climb.NextFloor != null)
                    wentDown++;
            }
            Assert.That(wentDown, Is.GreaterThan(0), "a run went down to floor 2, so floor 1 was compared with a floor 2 after it");
        }

        // A floor-1 clear goes down to floor 2 as the scene does: after the checkpoint and its advance delay, floor 2's rooms
        // in the asset's order - three halls, the forge, the elite, the Warden - the first fought where the hero stands, the
        // later halls at the districts the climb's round reaches next (the north court, the workyard, the pit's rim; the
        // scene's own route in ClimbFloorContentTests), the gate open as its room clears, its Warden at floor 2's tier and
        // the run's difficulty; the hero carries its level and health down, and the floor's clock starts again.
        [Test]
        public void AFloorOneClearGoesDownToFloorTwoAndPlaysItsRoomsInTheScenesOrder()
        {
            DescentSimulation.FoundryLevers two = FoundryBatch.Shipped();
            two.Floors = 2;
            RoomKind[] rooms = { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Boss };
            float[][] districts = { new[] { 4f, 12f }, new[] { -25f, 7f }, new[] { -10f, -1.5f } };
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            {
                int wentDown = 0;
                foreach (int seed in Seeds)
                {
                    string where = $"{hero.Name} seed {seed}";
                    var lines = new List<string>();
                    DescentSimulation.FoundryResult first = DescentSimulation.RunFoundry(hero, seed, two, trace: lines.Add);
                    DescentSimulation.FoundryResult run = first.NextFloor;
                    if (run == null)
                        continue;
                    wentDown++;
                    Assert.That(run.Floor, Is.EqualTo(2), where);
                    Assert.That(run.NextFloor, Is.Null, where + ": two floors asked for");
                    Assert.That(run.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout), where);
                    Assert.That(run.Kinds, Is.EqualTo(rooms), where + ": the asset's rooms, no extra elite");

                    // The checkpoint: the hero goes down as floor 1 left it, and the first room waits the advance delay.
                    int descent = lines.FindIndex(l => l.Contains(" floor_descend "));
                    Assert.That(lines[descent], Does.EndWith($"floor=2 level={first.Level} hp={first.HeroHealth.ToString("0.###", CultureInfo.InvariantCulture)}"), where);
                    Assert.That(run.DescentSeconds, Is.EqualTo(DescentSimulation.AdvanceDelay).Within(2.5f / 60f), where);
                    float roomTime = run.RoomSeconds.Where(s => s >= 0f).Sum();
                    Assert.That(roomTime + run.DescentSeconds, Is.EqualTo(run.FloorSeconds).Within(0.05f), where + ": the descent and the rooms are the floor");

                    // Its rooms in order, as far as the run got.
                    var started = lines.Skip(descent).Where(l => l.Contains(" room_start ")).ToList();
                    Assert.That(started.Count, Is.InRange(1, rooms.Length), where);
                    for (int i = 0; i < started.Count; i++)
                        Assert.That(started[i], Does.Contain($"room_start room={i + 1} kind={rooms[i]} "), where);
                    if (run.Cleared)
                        Assert.That(started.Count, Is.EqualTo(rooms.Length), where + ": every room played");
                    Assert.That(run.WalkSeconds[0], Is.EqualTo(-1f), where + ": the first hall is fought where the hero stands");

                    // Every hall reached at its district, the round going on from floor 1's.
                    int hall = 0;
                    for (int i = descent; i < lines.Count; i++)
                    {
                        if (!lines[i].Contains(" hall_reached "))
                            continue;
                        Assert.That(hall, Is.LessThan(districts.Length), where + ": floor 2's later halls");
                        string frame = lines.Skip(i + 1).First(l => l.Contains(" frame "));
                        FoundryMap.ToFloor(districts[hall][0], districts[hall][1], out float x, out float y);
                        float dx = Field(frame, "x") - x, dy = Field(frame, "y") - y;
                        Assert.That(Math.Sqrt(dx * dx + dy * dy), Is.LessThanOrEqualTo(FoundryMap.RoomSiteRadius + .01f),
                            $"{where}: floor 2's hall {hall + 2} at its district");
                        hall++;
                    }
                    if (run.Cleared)
                        Assert.That(hall, Is.EqualTo(districts.Length), where);

                    Assert.That(run.GateWaitSeconds, Is.Zero, where + ": floor 2's gate opens as its room clears");
                    if (run.LevelAtGate >= 0)
                    {
                        Assert.That(run.LevelAtGate, Is.GreaterThanOrEqualTo(first.Level), where + ": the level carried down");
                        Assert.That(run.DifficultyAtGate, Is.GreaterThanOrEqualTo(first.Difficulty), where + ": the difficulty carried down");
                        Assert.That(run.WardenHealth, Is.EqualTo(DescentSimulation.Warden.Health * WardenRule.GateHealthShare *
                                                                 ClimbScaling.Health(2) * run.DifficultyAtGate).Within(.01f), where);
                        Assert.That(run.ClockAtGate, Is.InRange(1f, run.FloorSeconds - run.DescentSeconds), where + ": the floor's own clock");
                    }
                    if (run.Cleared)
                    {
                        Assert.That(run.WardenSeconds, Is.GreaterThan(DescentSimulation.RevealTurnSeconds * 2f + DescentSimulation.RevealHoldSeconds), where);
                        Assert.That(run.BossSeconds, Is.GreaterThanOrEqualTo(run.WardenSeconds), where + ": the wave ends with the last of the three");
                        Assert.That(run.ExitSeconds, Is.GreaterThanOrEqualTo(ExitRule.DwellSeconds), where + ": the gate is held");
                    }
                    else
                    {
                        Assert.That(run.DeathRoom, Is.InRange(1, rooms.Length), where);
                    }
                }
                Assert.That(wentDown, Is.GreaterThan(0), hero.Name + ": a seed went down to floor 2");
            }
        }

        // The floor-1 knobs a run's levers set stay on floor 1: floor 2 plays its asset's own (FoundryFloor), under the run's
        // Warden and horde levers, which stand for rules every floor of the scene reads.
        [Test]
        public void AFloorAfterTheFirstPlaysItsOwnKnobsUnderTheRunsRuleLevers()
        {
            DescentSimulation.FoundryLevers run = FoundryBatch.Shipped().Copy("every knob");
            run.HallSurges = 4;
            run.ExtraElites = 2;
            run.ExtraHalls = 2;
            run.HallHoldSeconds = 30f;
            run.NodeHealthScale = 1.5f;
            run.GateNotBeforeSeconds = 240f;
            run.WardenHealthShare = 7f;
            run.WardenRecoverySeconds = 1f;
            run.HordeHealthShare = .6f;
            run.HordeFillRate = 3f;
            run.Floors = 2;
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next;
            DescentSimulation.FoundryLevers later = DescentSimulation.LaterFloorLevers(run, floor);
            Assert.That((later.HallSurges, later.ExtraElites, later.GateNotBeforeSeconds), Is.EqualTo((floor.HallSurges, floor.ExtraElites, floor.GateNotBeforeSeconds)));
            Assert.That((later.ExtraHalls, later.HallHoldSeconds, later.NodeHealthScale), Is.EqualTo((0, 0f, 1f)));
            Assert.That((later.WardenHealthShare, later.WardenRecoverySeconds, later.HordeHealthShare, later.HordeFillRate, later.Floors),
                Is.EqualTo((7f, 1f, .6f, 3f, 2)));
            Assert.That(later.Name, Is.EqualTo(run.Name));
            Assert.That(DescentSimulation.LaterHalls(DescentSimulation.FoundryFloorOne()), Is.EqualTo(3),
                "floor 1's later halls - the workyard, the pit's rim, the east - its extra elite not counted");
        }

        // A number from a trace line's "name=value".
        private static float Field(string line, string name)
        {
            int start = line.IndexOf(" " + name + "=", StringComparison.Ordinal) + name.Length + 2;
            int end = line.IndexOf(' ', start);
            return float.Parse(end < 0 ? line.Substring(start) : line.Substring(start, end - start), CultureInfo.InvariantCulture);
        }

        // The autopilot's walks to the halls are bounded (DropDetourRule.Walk): with the horde filling at 3 a second, the
        // thickest of the research's levers, the drop chase it had before walked each of these floors 440-466 s, and the
        // Vanguard's 153 and Ilya's 191 of seeds 1-200 past FoundryBatch.LongWalkSeconds. Now none walks long.
        [Test]
        public void OnAThickHordeTheWalksToTheHallsStayShort()
        {
            DescentSimulation.FoundryLevers thick = FoundryBatch.Shipped().Copy("fill rate 3");
            thick.HordeFillRate = 3f;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in new[] { 3, 12 })
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed, thick);
                Assert.That(FoundryBatch.WalkedLong(run), Is.False, $"{hero.Name} seed {seed}: walked {run.WalkTotal:0} s");
            }
        }

        // The tuned director with no tuning is the game's HordeDirector: frame for frame, over a floor's worth of frames
        // whose crowd, node waves, state and kills are drawn at random, the same spawns, roles and targets.
        [Test]
        public void TheTunedDirectorAtTheShippedValuesIsTheGamesDirector()
        {
            foreach (int seed in new[] { 1, 11, 22, 4242 })
            {
                var shipped = new HordeDirector(HordeRule.SceneCap, 3, seed, true);
                var tuned = new TunedHordeDirector(HordeRule.SceneCap, 3, seed, null, 1f);
                for (HordeState state = HordeState.Ramp; state <= HordeState.Boss; state++)
                {
                    var weights = new[] { HordeRule.Weight(state, 0), HordeRule.Weight(state, 1), HordeRule.Weight(state, 2) };
                    shipped.SetRoleWeights(state, weights);
                    tuned.SetRoleWeights(state, weights);
                }
                var random = new Random(seed);
                var a = new List<HordeDirector.Spawn>();
                var b = new List<HordeDirector.Spawn>();
                int alive = 0;
                for (int frame = 0; frame < 60 * 400; frame++)
                {
                    if (frame % 600 == 0)
                    {
                        var state = (HordeState)random.Next(4);
                        shipped.SetState(state);
                        tuned.SetState(state);
                    }
                    int deaths = random.Next(100) < 4 ? Math.Min(alive, random.Next(1, 3)) : 0;
                    for (int d = 0; d < deaths; d++)
                    {
                        shipped.Fell();
                        tuned.Fell();
                        alive--;
                    }
                    int other = random.Next(100) < 30 ? random.Next(0, 6) : 0;
                    float delta = random.Next(100) < 2 ? 0.5f : 1f / 60f;
                    shipped.Tick(delta, alive, other, a);
                    tuned.Tick(delta, alive, other, b);
                    Assert.That(b.Select(s => (s.Role, s.Ordinal)), Is.EqualTo(a.Select(s => (s.Role, s.Ordinal))), $"seed {seed} frame {frame}");
                    Assert.That(tuned.TargetNow, Is.EqualTo(shipped.TargetNow), $"seed {seed} frame {frame}");
                    Assert.That(tuned.Seconds, Is.EqualTo(shipped.Seconds), $"seed {seed} frame {frame}");
                    Assert.That(tuned.HealthMultiplier, Is.EqualTo(shipped.HealthMultiplier), $"seed {seed} frame {frame}");
                    Assert.That(tuned.DamageMultiplier, Is.EqualTo(shipped.DamageMultiplier), $"seed {seed} frame {frame}");
                    alive += a.Count;
                }
                Assert.That(tuned.Spawned, Is.EqualTo(shipped.Spawned), $"seed {seed}");
            }
        }

        // The copy of HordeRule.EffectiveTarget's body over given crowds is the original at the shipped crowds, over every
        // state, a window's kills and spawns, and the floor's clock to past its overrun.
        [Test]
        public void TheTunedTargetAtTheShippedCrowdsIsTheGamesTarget()
        {
            int[] shipped = (int[])HordeRule.TargetByState.Clone();
            for (HordeState state = HordeState.Ramp; state <= HordeState.Boss; state++)
            for (int kills = 0; kills <= 24; kills += 3)
            for (int spawns = 0; spawns <= 24; spawns += 4)
            for (float clock = 0f; clock <= 330f; clock += 7.3f)
                Assert.That(TunedHordeDirector.EffectiveTarget(shipped, state, kills, spawns, HordeRule.SceneCap, clock),
                    Is.EqualTo(HordeRule.EffectiveTarget(state, kills, spawns, HordeRule.SceneCap, clock)),
                    $"{state} {kills}/{spawns} at {clock}");
        }

        [Test]
        public void TheTunedExperienceAtTheShippedShareIsTheGamesExperience()
        {
            for (int authored = 0; authored <= 1000; authored++)
            {
                Assert.That(TunedHordeExperience.Copy(authored, HordeRule.ExperienceShare), Is.EqualTo(HordeRule.Experience(authored)), $"{authored}");
                Assert.That(TunedHordeExperience.Experience(authored, HordeRule.ExperienceShare), Is.EqualTo(HordeRule.Experience(authored)), $"{authored}");
            }
            // The research's step: at 0.1 a Grunt and a Runner pay 1, as a Mite does.
            Assert.That(TunedHordeExperience.Experience(10, 0.1f), Is.EqualTo(1));
            Assert.That(TunedHordeExperience.Experience(1, 0.1f), Is.EqualTo(1));
        }

        // Forcing the tuned director on with the shipped crowds and rate plays the floor the game's director plays, to the
        // frame: so every director lever is a delta against the game's floor and nothing else.
        [Test]
        public void TheFloorWithTheTunedDirectorAtTheShippedValuesIsTheShippedFloor()
        {
            DescentSimulation.FoundryLevers tuned = FoundryBatch.Shipped().Copy("tuned at shipped");
            tuned.HordeTargets = (int[])HordeRule.TargetByState.Clone();
            Assert.That(tuned.TunesDirector, Is.True);
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult shipped = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), seed);
                DescentSimulation.FoundryResult copy = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), seed, tuned);
                Assert.That(copy.FloorSeconds, Is.EqualTo(shipped.FloorSeconds), $"seed {seed}");
                Assert.That(copy.Picks, Is.EqualTo(shipped.Picks), $"seed {seed}");
                Assert.That(copy.HordeSpawned, Is.EqualTo(shipped.HordeSpawned), $"seed {seed}");
            }
        }

        // Each lever does what it names and the floor it makes is still played through.
        [Test]
        public void TheLeversChangeWhatTheyName()
        {
            const int seed = 33;
            DescentSimulation.FoundryHero ilya = DescentSimulation.CinderArcanist();
            DescentSimulation.FoundryResult shipped = DescentSimulation.RunFoundry(ilya, seed);
            DescentSimulation.FoundryResult Run(Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("lever");
                set(levers);
                return DescentSimulation.RunFoundry(ilya, seed, levers);
            }

            DescentSimulation.FoundryResult elite = Run(l => l.ExtraElites += 1);
            Assert.That(elite.Kinds.Count(k => k == RoomKind.Elite), Is.EqualTo(shipped.Kinds.Count(k => k == RoomKind.Elite) + 1),
                "one more elite room");
            Assert.That(elite.Kinds, Is.EqualTo(new[] { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Elite, RoomKind.Elite, RoomKind.Boss }),
                "the copies stand before the gate");

            DescentSimulation.FoundryResult halls = Run(l => l.ExtraHalls = 2);
            Assert.That(halls.Kinds.Count(k => k == RoomKind.Combat), Is.EqualTo(5));

            DescentSimulation.FoundryResult surges = Run(l => l.HallSurges += 2);
            Assert.That(surges.WaveKills, Is.GreaterThan(shipped.WaveKills), "two more surges a hall send more node enemies");

            DescentSimulation.FoundryResult held = Run(l => l.HallHoldSeconds = 45f);
            for (int room = 0; room < 3; room++)
                if (held.RoomSeconds[room] >= 0f && (held.Cleared || room + 1 < held.DeathRoom))
                    Assert.That(held.RoomSeconds[room] - Math.Max(0f, held.WalkSeconds[room]), Is.GreaterThanOrEqualTo(45f), $"hall {room + 1} is held");

            DescentSimulation.FoundryResult warden = Run(l => l.WardenHealthShare = 7.5f);
            if (warden.LevelAtGate >= 0)
                Assert.That(warden.WardenHealth / warden.DifficultyAtGate, Is.EqualTo(DescentSimulation.Warden.Health * 7.5f).Within(1e-3f));

            // The Warden's health grown with the floor's clock as it walks out, as a horde enemy's is before the overrun.
            DescentSimulation.FoundryResult clocked = Run(l => { l.WardenHealthShare = 7f; l.WardenClockFrom = 210f; });
            float Grown(float seconds) => 1f + HordeClock.HealthGrowth * Math.Min(seconds, HordeClock.TargetSeconds) / HordeClock.TargetSeconds;
            if (clocked.LevelAtGate >= 0)
                Assert.That(clocked.WardenHealth / clocked.DifficultyAtGate,
                    Is.EqualTo(DescentSimulation.Warden.Health * 7f * Grown(clocked.ClockAtGate) / Grown(210f)).Within(.01f));

            DescentSimulation.FoundryResult gate = Run(l => l.GateNotBeforeSeconds = 240f);
            if (gate.LevelAtGate >= 0)
                Assert.That(gate.ClockAtGate, Is.GreaterThanOrEqualTo(240f), "the gate waits for the clock");

            DescentSimulation.FoundryResult thin = Run(l => l.HordeExperienceShare = 0.1f);
            Assert.That(thin.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout));
        }

        // The shipped floor's Warden is the game's - WardenRule's health share and recovery, no other candidate on - and every
        // candidate of the Warden's batch (FOUNDRY_LEVERS=3) is another Warden.
        [Test]
        public void TheShippedWardenIsTheGamesAndEveryWardenLeverMovesIt()
        {
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            Assert.That(shipped.WardenHealthShare, Is.EqualTo(WardenRule.GateHealthShare));
            Assert.That(shipped.WardenRecoverySeconds, Is.EqualTo(WardenRule.RecoverySeconds));
            Assert.That(shipped.WardenClockFrom, Is.Zero);
            Assert.That(shipped.WardenGuardShare, Is.EqualTo(1f));
            foreach (FoundryBatch.Lever lever in FoundryBatch.WardenLevers())
            {
                DescentSimulation.FoundryLevers l = lever.Levers;
                bool moves = l.WardenHealthShare != shipped.WardenHealthShare ||
                             l.WardenRecoverySeconds != shipped.WardenRecoverySeconds ||
                             l.WardenClockFrom != shipped.WardenClockFrom || l.WardenGuardShare != shipped.WardenGuardShare;
                Assert.That(moves, Is.True, lever.Name);
            }
        }

        // Every lever, combination and Warden candidate names a floor the shipped one is not, and a research lever one its
        // own floor (the pre-V0.1 row it is read against) is not either, so no row of the tables reads a floor against itself.
        [Test]
        public void EveryLeverAndComboMovesAKnob()
        {
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            FoundryBatch.Lever[] research = FoundryBatch.ResearchLevers();
            foreach (FoundryBatch.Lever lever in research.Concat(FoundryBatch.ComboLevers()).Concat(FoundryBatch.WardenLevers()))
            {
                Assert.That(SameKnobs(lever.Levers, shipped), Is.False, lever.Name + " moves a knob of the shipped floor");
                if (lever.Basis == null)
                    continue;
                FoundryBatch.Lever basis = research.SingleOrDefault(row => row.Levers.Name == lever.Basis);
                Assert.That(basis, Is.Not.Null, lever.Name + " is read against a row of its list");
                Assert.That(SameKnobs(lever.Levers, basis.Levers), Is.False, lever.Name + " moves a knob of " + lever.Basis);
            }

            DescentSimulation.FoundryLevers before = FoundryBatch.PreV01();
            Assert.That((before.HallSurges, before.ExtraElites, before.GateNotBeforeSeconds), Is.EqualTo((0, 0, 0f)),
                "the floor before V0.1: no surges, no second elite, no gate");
            Assert.That(SameKnobs(shipped, shipped.Copy("again")), Is.True, "a copy is the same floor");
            DescentSimulation.FoundryLevers tuned = shipped.Copy("tuned at shipped");
            tuned.HordeTargets = (int[])HordeRule.TargetByState.Clone();
            Assert.That(SameKnobs(shipped, tuned), Is.True, "the director's shipped crowds are the shipped floor's");
        }

        // Every knob of a lever set, read off its fields, so a knob added later is compared too; the director's crowds
        // unset are the shipped ones (HordeRule.TargetByState).
        private static bool SameKnobs(DescentSimulation.FoundryLevers a, DescentSimulation.FoundryLevers b)
        {
            foreach (FieldInfo field in typeof(DescentSimulation.FoundryLevers).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.Name == nameof(DescentSimulation.FoundryLevers.Name))
                    continue;
                object x = field.GetValue(a), y = field.GetValue(b);
                if (field.Name == nameof(DescentSimulation.FoundryLevers.HordeTargets))
                {
                    x = x ?? HordeRule.TargetByState;
                    y = y ?? HordeRule.TargetByState;
                }
                bool same = x is int[] xs && y is int[] ys ? xs.SequenceEqual(ys) : Equals(x, y);
                if (!same)
                    return false;
            }
            return true;
        }
    }
}
