using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The foundry's finale in the foundry simulation (FoundryLevers.Floors = 3, DescentSimulation.FoundryFinale.cs): a
    // floor-2 clear goes down into Floor_FinalSwarm's swarm room on the foundry's arena, which ends when the hero walks
    // out of its exit - a victory, the climb's last floor - or falls. Played over the Editor measurement's seeds, both
    // heroes, and read over all of them rather than pinned to one: the Editor's Mono and the .NET runner walk a seed apart.
    // FoundrySimulationSceneValueTests holds the finale's floor and the autopilot's stay in it to the scene's.
    public sealed class FoundryFinaleTests
    {
        private static readonly int[] Seeds = { 11, 22, 33, 44 };

        private sealed class Climb
        {
            public DescentSimulation.FoundryHero Hero;
            public int Seed;
            public DescentSimulation.FoundryResult First;
            public List<string> Trace;
            public string Where => $"{Hero.Name} seed {Seed}";
            public DescentSimulation.FoundryResult Second => First.NextFloor;
            public DescentSimulation.FoundryResult Finale => First.NextFloor?.NextFloor;
            // The trace from the descent into the finale on.
            public IEnumerable<string> FinaleTrace => Trace.SkipWhile(line => !line.Contains(" floor_descend floor=3 "));
        }

        // Every hero on every seed down all three floors, with the telemetry's trace: about a second a run, played once for
        // the tests below.
        private static readonly Lazy<Climb[]> Climbs = new Lazy<Climb[]>(() =>
        {
            var climbs = new List<Climb>();
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                var trace = new List<string>();
                DescentSimulation.FoundryResult first = DescentSimulation.RunFoundry(hero, seed, Floors(3), trace: trace.Add);
                climbs.Add(new Climb { Hero = hero, Seed = seed, First = first, Trace = trace });
            }
            return climbs.ToArray();
        });

        private static DescentSimulation.FoundryLevers Floors(int floors)
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped();
            levers.Floors = floors;
            return levers;
        }

        private static Climb[] Reached()
        {
            Climb[] reached = Climbs.Value.Where(c => c.Finale != null).ToArray();
            Assert.That(reached.Length, Is.GreaterThan(0), "a run cleared both floors and went down into the finale");
            return reached;
        }

        // The climb's last floor: floor 2's next, the Final Swarm - one swarm room whose ladder is the hall Descent's finale
        // (DescentSimulation.FinalSwarm, which DescentFlowTests holds to the asset), its authored multipliers rather than the
        // climb's, no horde, no modifier, none of the foundry's knobs and nothing below it. Its exit stands on the foundry's
        // floor where the hero can hold it, clear of the pit's band.
        [Test]
        public void TheFinaleIsFloorTwosNextFloorAndTheHallDescentsFinale()
        {
            DescentSimulation.FoundryFloor two = DescentSimulation.FoundryFloorOne().Next;
            DescentSimulation.FoundryFloor finale = two.Next;
            Assert.That(finale, Is.Not.Null);
            Assert.That(finale.Id, Is.EqualTo("floor_final_swarm"));
            Assert.That(finale.FloorNumber, Is.EqualTo(3));
            Assert.That(finale.Next, Is.Null, "the climb ends there");
            Assert.That(finale.Kinds, Is.EqualTo(new[] { RoomKind.Swarm }));
            Assert.That(finale.HordeRoles, Is.Empty, "no horde in the finale");
            Assert.That(finale.ClimbScaled, Is.False);
            Assert.That(two.ClimbScaled && DescentSimulation.FoundryFloorOne().ClimbScaled, Is.True, "floors 1 and 2 are climb-scaled");
            Assert.That(finale.EnemyHealthMultiplier, Is.EqualTo(DescentSimulation.FinalSwarm.HealthMultiplier));
            Assert.That(finale.EnemyDamageMultiplier, Is.EqualTo(DescentSimulation.FinalSwarm.DamageMultiplier));
            Assert.That((finale.ModifierHealthPercent, finale.ModifierDamagePercent, finale.ModifierGoldPercent), Is.EqualTo((0f, 0f, 0f)));
            Assert.That((finale.HallSurges, finale.ExtraElites, finale.GateNotBeforeSeconds, finale.GateHealthShare), Is.EqualTo((0, 0, 0f, 0f)));
            Assert.That(finale.Rooms.Length, Is.EqualTo(1));
            Assert.That(finale.Rooms[0].Length, Is.EqualTo(DescentSimulation.FinalSwarm.Rooms[0].Length));
            for (int wave = 0; wave < finale.Rooms[0].Length; wave++)
                Assert.That(finale.Rooms[0][wave], Is.EqualTo(DescentSimulation.FinalSwarm.Rooms[0][wave]), $"ladder wave {wave + 1}");
            Assert.That(finale.Rooms[0].All(w => w.Length >= 1 && w.Length <= PackLayout.MaxFormationSize), Is.True);
            Assert.That(Array.IndexOf(finale.Rooms[0][finale.Rooms[0].Length - 1], DescentSimulation.Warden) >= 0, Is.True,
                "the ladder's last wave walks a Forge Warden in as one of its crowd");

            ExitRule.SpotFor(ArenaGeometry.Foundry, out float x, out float y);
            Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.True, "the hero can stand on the exit");
            Assert.That(PitEruptionRule.InBand(x, y), Is.False);
            Assert.That(DescentSimulation.PilotSwarmStay, Is.GreaterThan(ExitRule.DwellSeconds));
        }

        // A floor-2 clear goes down into the finale after the checkpoint's advance delay, with the level, health and
        // difficulty floor 2 left; the swarm opens with its ladder's first wave and runs on its own clock - no horde, no
        // floor clock, no pit - its enemies at most the ten the ladder's waves hold, its kills paid at once and its
        // level-ups offered in sittings; and the floor ends, walked out of or fallen in, never at the limit.
        [Test]
        public void TheFinaleIsPlayedAfterAFloorTwoClearAndEnds()
        {
            foreach (Climb climb in Climbs.Value)
            {
                string where = climb.Where;
                DescentSimulation.FoundryResult two = climb.Second;
                if (two == null)
                    continue;
                Assert.That(climb.Finale != null, Is.EqualTo(two.Cleared), where + ": a floor-2 clear goes down, anything else ends the run");
            }

            foreach (Climb climb in Reached())
            {
                string where = climb.Where;
                DescentSimulation.FoundryResult two = climb.Second, finale = climb.Finale;
                Assert.That(finale.Floor, Is.EqualTo(3), where);
                Assert.That(finale.Kinds, Is.EqualTo(new[] { RoomKind.Swarm }), where);
                Assert.That(finale.NextFloor, Is.Null, where + ": nothing below it");
                Assert.That(finale.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout), where + ": the finale ends");
                Assert.That(finale.DescentSeconds, Is.EqualTo(DescentSimulation.AdvanceDelay).Within(2.5f / 60f), where);
                Assert.That(finale.RoomSeconds[0] + finale.DescentSeconds, Is.EqualTo(finale.FloorSeconds).Within(0.05f),
                    where + ": the descent and the swarm room are the floor");

                // The hero goes down as floor 2 left it.
                Assert.That(finale.SwarmEntryLevel, Is.EqualTo(two.Level), where);
                Assert.That(finale.SwarmEntryHealth, Is.EqualTo(two.HeroHealth), where);
                Assert.That(finale.SwarmEntryMaximum, Is.EqualTo(two.HeroMaximum), where);
                Assert.That(finale.SwarmEntryDifficulty, Is.EqualTo(two.Difficulty), where);
                List<string> descent = climb.FinaleTrace.ToList();
                Assert.That(descent.FirstOrDefault(l => l.Contains(" room_start ")), Does.Contain("room_start room=1 kind=Swarm "), where);
                Assert.That(descent.FirstOrDefault(l => l.Contains(" swarm_step ")), Does.Contain("swarm_step step=1 seconds=0 difficulty=1 "), where);

                // The swarm on its own clock: the ramp and the ladder where its clock says, the opening's three and more,
                // never more than a wave holds standing, and none of the floors' horde, clock or pit.
                Assert.That(finale.SwarmSeconds, Is.GreaterThan(0f), where);
                Assert.That(finale.SwarmStep, Is.EqualTo((int)(finale.SwarmSeconds / SwarmRule.StepSeconds) + 1), where);
                Assert.That(finale.SwarmDifficulty, Is.EqualTo(SwarmRule.DifficultyAtSeconds(finale.SwarmSeconds)), where);
                Assert.That(finale.SwarmLadderWave,
                    Is.EqualTo(SwarmRule.LadderWaveAt(finale.SwarmSeconds, DescentSimulation.FinalSwarm.Rooms[0].Length) + 1), where);
                Assert.That(finale.SwarmSpawned, Is.GreaterThanOrEqualTo(DescentSimulation.FinalSwarm.Rooms[0][0].Length), where);
                Assert.That(finale.SwarmMaxAlive, Is.InRange(1, PackLayout.MaxFormationSize), where);
                Assert.That((finale.HordeSpawned, finale.HordeMaxAlive, finale.HordeKills), Is.EqualTo((0, 0, 0)), where + ": no horde");
                Assert.That(finale.Clock, Is.Zero, where + ": no floor clock");
                Assert.That(finale.Eruptions, Is.Zero, where + ": the pit does not run in a swarm room");
                Assert.That(finale.Kills.Count, Is.EqualTo(finale.WaveKills), where + ": every kill is the swarm's");
                Assert.That(finale.Kills.Count, Is.GreaterThan(0), where);
                int sittings = (int)(finale.SwarmSeconds / SwarmRule.OfferEverySeconds);
                Assert.That(finale.Sittings, Is.InRange(sittings - 1, sittings), where + ": a sitting every ten seconds of the clock");
                Assert.That(finale.Level, Is.GreaterThanOrEqualTo(finale.SwarmEntryLevel), where);

                if (finale.Outcome == DescentSimulation.FoundryOutcome.Died)
                {
                    Assert.That(finale.WalkedOut, Is.False, where);
                    Assert.That(finale.HeroHealth, Is.EqualTo(0f), where);
                    Assert.That(finale.DeathPhase, Is.EqualTo(DescentSimulation.SwarmFightPhase).Or.EqualTo(DescentSimulation.SwarmExitPhase), where);
                    Assert.That(finale.Killer, Does.StartWith(DescentSimulation.SwarmHarm), where);
                }
            }
        }

        // The pit does not run in a swarm room, so the pilot never draws the swarm to its rim, as DevelopmentAutopilot.Goal
        // reads PitEruptionService.IsRunning: the careful pilot spent about 44 s of the finale on a rim that never spills
        // (the review of 2026-11-11). Floors 1 and 2 still lure their node fights.
        [Test]
        public void ThePilotNeverLuresTheSwarmToThePitsRim()
        {
            foreach (Climb climb in Reached())
            {
                Assert.That(climb.Finale.LureSeconds, Is.Zero, climb.Where);
                Assert.That(climb.First.LureSeconds + climb.Second.LureSeconds, Is.GreaterThan(0f), climb.Where + ": floors 1 and 2");
            }
        }

        // Floors 1 and 2 are the same floors, field for field, whether or not the run goes on into the finale; asked for
        // two floors, floor 2 has nothing below it.
        [Test]
        public void FloorsOneAndTwoAreTheSameWhetherOrNotTheRunGoesOnIntoTheFinale()
        {
            foreach (Climb climb in Climbs.Value)
            {
                string where = climb.Where;
                DescentSimulation.FoundryResult two = DescentSimulation.RunFoundry(climb.Hero, climb.Seed, Floors(2));
                AssertSameFloor(climb.First, two, where + " floor 1");
                Assert.That(two.NextFloor != null, Is.EqualTo(climb.Second != null), where);
                if (two.NextFloor == null)
                    continue;
                Assert.That(two.NextFloor.NextFloor, Is.Null, where + ": two floors asked for");
                AssertSameFloor(climb.Second, two.NextFloor, where + " floor 2");
            }
        }

        // The swarm after floor 2's gate has no gate boss (EncounterController.ReleaseWave since main 6a09574f): floor 2
        // ended on its gate boss's wave, the flag the scene left standing until that fix, and in the finale nothing is read
        // as the gate boss or a Captain - no slam, no reveal, none of the Sword's share on the Warden, none of the Daggers'
        // on an elite, nothing taken from a Warden.
        [Test]
        public void TheFinaleHasNoGateBoss()
        {
            foreach (Climb climb in Reached())
            {
                string where = climb.Where;
                DescentSimulation.FoundryResult two = climb.Second, finale = climb.Finale;
                Assert.That(two.BossSeconds, Is.GreaterThanOrEqualTo(0f), where + ": floor 2 ended on its gate boss's wave");
                Assert.That(finale.GateBossFrames, Is.Zero, where);
                Assert.That((finale.Slams, finale.SlamHits, finale.LevelAtGate), Is.EqualTo((0, 0, -1)), where);
                Assert.That((finale.WardenFromWeapons, finale.WardenFought, finale.TakenFromWarden, finale.TakenFromSlams),
                    Is.EqualTo((0f, 0f, 0f, 0f)), where);
                Assert.That(finale.WeaponWork.Sum(w => w.DealtWarden), Is.Zero, where);
                Assert.That(finale.WeaponWork.Sum(w => w.DealtElite), Is.Zero, where);
                Assert.That(finale.EliteFought, Is.Zero, where);
                string boss = climb.FinaleTrace.FirstOrDefault(l => l.Contains(" slam_mark ") || l.Contains(" boss_start ") || l.Contains(" warden_strike"));
                Assert.That(boss, Is.Null, where + ": no gate boss's event in the finale");
            }
        }

        // Walking out is the only way the finale clears: the autopilot turns for the exit once the swarm's clock reaches its
        // stay, stands still on it for the hold, and the run ends in victory - the climb's last floor - where it stood.
        [Test]
        public void WalkingOutOfTheFinaleEndsTheRunInVictory()
        {
            ExitRule.SpotFor(ArenaGeometry.Foundry, out float exitX, out float exitY);
            int walkedOut = 0;
            foreach (Climb climb in Reached())
            {
                string where = climb.Where;
                DescentSimulation.FoundryResult finale = climb.Finale;
                Assert.That(finale.WalkedOut, Is.EqualTo(finale.Cleared), where + ": the exit is the only way out");
                if (!finale.WalkedOut)
                {
                    Assert.That(finale.RunEnd, Is.EqualTo(RunOutcome.None), where);
                    continue;
                }
                walkedOut++;
                Assert.That(finale.RunEnd, Is.EqualTo(RunOutcome.Victory), where);
                Assert.That(finale.SwarmSeconds, Is.GreaterThanOrEqualTo(DescentSimulation.PilotSwarmStay + ExitRule.DwellSeconds - 1e-3f),
                    where + ": it left after its stay, and after holding the exit");
                Assert.That(finale.SwarmExitSeconds, Is.GreaterThanOrEqualTo(ExitRule.DwellSeconds - 1e-3f), where);
                Assert.That(ExitRule.IsWithinReach(finale.LeftAtX, finale.LeftAtY, exitX, exitY), Is.True, where + ": it left from the exit");
                Assert.That(finale.HeroHealth, Is.GreaterThan(0f), where);
                List<string> events = climb.FinaleTrace.Where(l => !l.Contains(" frame ")).ToList();
                int exit = events.FindIndex(l => l.Contains(" swarm_exit "));
                Assert.That(exit, Is.GreaterThanOrEqualTo(0), where);
                Assert.That(events.Skip(exit).Any(l => l.Contains(" room_complete room=1 ") && l.Contains("(floor cleared")), Is.True, where);
                Assert.That(events[events.Count - 1], Does.Contain(" run_end victory "), where + ": the run's last event");
            }
            Assert.That(walkedOut, Is.GreaterThan(0), "a run walked out of the finale");
        }

        // The finale's stay (FoundryLevers.FinaleStaySeconds, research only) is the development autopilot's by default; a
        // shorter one turns the pilot for the exit at its own time on the swarm's clock, and changes nothing before the
        // finale.
        [Test]
        public void AStayLeverTurnsThePilotForTheExitAtItsTime()
        {
            Assert.That(new DescentSimulation.FoundryLevers().FinaleStaySeconds, Is.EqualTo(DescentSimulation.PilotSwarmStay));
            DescentSimulation.FoundryLevers shorter = Floors(3);
            shorter.FinaleStaySeconds = 30f;
            int walkedOut = 0;
            foreach (Climb climb in Climbs.Value.Where(c => c.Hero.Id == DescentSimulation.Vanguard().Id && c.Finale != null).Take(2))
            {
                string where = climb.Where;
                DescentSimulation.FoundryResult first = DescentSimulation.RunFoundry(climb.Hero, climb.Seed, shorter);
                AssertSameFloor(first.NextFloor, climb.Second, where + " floor 2");
                DescentSimulation.FoundryResult finale = first.NextFloor.NextFloor;
                Assert.That(finale, Is.Not.Null, where);
                if (!finale.WalkedOut)
                    continue;
                walkedOut++;
                Assert.That(finale.SwarmSeconds, Is.GreaterThanOrEqualTo(30f + ExitRule.DwellSeconds - 1e-3f), where);
                Assert.That(finale.SwarmSeconds, Is.LessThan(DescentSimulation.PilotSwarmStay), where + ": out before the autopilot's stay");
            }
            Assert.That(walkedOut, Is.GreaterThan(0), "a run walked out at the shorter stay");
        }

        // Two floors' results alike field by field, NextFloor aside: a value, a string or a struct equal, a list element by
        // element, a class (a weapon's work) field by field.
        private static void AssertSameFloor(DescentSimulation.FoundryResult actual, DescentSimulation.FoundryResult expected, string where)
        {
            foreach (FieldInfo field in typeof(DescentSimulation.FoundryResult).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (field.Name != nameof(DescentSimulation.FoundryResult.NextFloor))
                    AssertSameValue(field.GetValue(actual), field.GetValue(expected), $"{where}: {field.Name}");
        }

        private static void AssertSameValue(object actual, object expected, string where)
        {
            if (actual == null || expected == null || actual is string || actual.GetType().IsValueType)
            {
                Assert.That(actual, Is.EqualTo(expected), where);
                return;
            }
            Assert.That(actual.GetType(), Is.EqualTo(expected.GetType()), where);
            if (actual is System.Collections.IEnumerable list)
            {
                object[] a = list.Cast<object>().ToArray(), b = ((System.Collections.IEnumerable)expected).Cast<object>().ToArray();
                Assert.That(a.Length, Is.EqualTo(b.Length), where + " (count)");
                for (int i = 0; i < a.Length; i++)
                    AssertSameValue(a[i], b[i], $"{where}[{i}]");
                return;
            }
            foreach (FieldInfo field in actual.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                AssertSameValue(field.GetValue(actual), field.GetValue(expected), where + "." + field.Name);
        }
    }
}
