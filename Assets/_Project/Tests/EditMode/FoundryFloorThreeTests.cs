using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The climb's floor 3 in the foundry simulation (docs/48 phase 1, Gloam Gardens; DescentSimulation.FoundryFloorThree):
    // floor 2's next floor and the finale's last before it, played after a floor-2 clear with FoundryLevers.Floors = 3 and
    // on into the finale with 4 (DescentSimulation.FinaleFloor). Played over the Editor measurement's seeds, both heroes,
    // and read over all of them rather than pinned to one. FoundrySimulationSceneValueTests holds the floor to its asset.
    public sealed class FoundryFloorThreeTests
    {
        private static readonly int[] Seeds = { 11, 22, 33, 44 };

        private static DescentSimulation.FoundryLevers Floors(int floors)
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped();
            levers.Floors = floors;
            return levers;
        }

        // The chain: floor 1, floor 2, floor 3, the finale - floor 3 floor 2's six rooms under its own names, at floor 3's
        // tier, with no modifier, the plain horde, no surges or extra elite, its gate held for 3:00 and its Warden's own
        // share; the finale the climb's fourth floor.
        [Test]
        public void FloorThreeStandsBetweenFloorTwoAndTheFinale()
        {
            DescentSimulation.FoundryFloor one = DescentSimulation.FoundryFloorOne();
            DescentSimulation.FoundryFloor two = one.Next;
            DescentSimulation.FoundryFloor three = two.Next;
            Assert.That(two.Id, Is.EqualTo("floor_quicksilver_vaults"));
            Assert.That(three, Is.Not.Null);
            Assert.That(three.Id, Is.EqualTo("floor_gloam_gardens"));
            Assert.That(three.FloorNumber, Is.EqualTo(3));
            Assert.That(three.ClimbScaled, Is.True);
            Assert.That(three.Rooms, Is.SameAs(DescentSimulation.GloamGardens.Rooms));
            Assert.That(three.Kinds, Is.EqualTo(new[] { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Boss }));
            Assert.That(three.HordeRoles, Is.EqualTo(new[] { DescentSimulation.Mite, DescentSimulation.Grunt, DescentSimulation.Runner }));
            Assert.That((three.ModifierHealthPercent, three.ModifierDamagePercent, three.ModifierGoldPercent), Is.EqualTo((0f, 0f, 0f)));
            Assert.That((three.HallSurges, three.ExtraElites, three.GateNotBeforeSeconds), Is.EqualTo((0, 0, 180f)));
            Assert.That(DescentSimulation.FloorThreeGateSeconds, Is.EqualTo(180f), "half a minute before floors 1 and 2");
            Assert.That(three.GateHealthShare, Is.EqualTo(DescentSimulation.FloorThreeGateShare));
            Assert.That(WardenRule.IsFloorGateHealthShare(three.GateHealthShare) && three.GateHealthShare > 0f, Is.True);
            Assert.That(DescentSimulation.GloamGardens.HealthMultiplier, Is.EqualTo(ClimbScaling.Health(3)).Within(1e-5f));
            Assert.That(DescentSimulation.GloamGardens.DamageMultiplier, Is.EqualTo(ClimbScaling.Damage(3)).Within(1e-5f));

            // Floor 2's rooms wave for wave: the same parts, the garden's names (the asset holds them) - but its elite room,
            // Thorn Post, keeps the Grunt Captain where floor 2's Sentry Hall has the Mirror Sentry since 2026-11-12 (docs/50,
            // the lead's decisions); the escort is the same.
            Assert.That(three.Rooms.Length, Is.EqualTo(DescentSimulation.QuicksilverVaults.Rooms.Length));
            int elite = System.Array.IndexOf(three.Kinds, RoomKind.Elite);
            for (int room = 0; room < three.Rooms.Length; room++)
            {
                if (room == elite)
                    continue;
                Assert.That(three.Rooms[room], Is.EqualTo(DescentSimulation.QuicksilverVaults.Rooms[room]), $"room {room + 1}");
            }
            Assert.That(three.Rooms[elite][0][0], Is.SameAs(DescentSimulation.Captain));
            Assert.That(DescentSimulation.QuicksilverVaults.Rooms[elite][0][0], Is.SameAs(DescentSimulation.MirrorSentry));
            Assert.That(three.Rooms[elite][0].Skip(1), Is.EqualTo(DescentSimulation.QuicksilverVaults.Rooms[elite][0].Skip(1)));

            Assert.That(three.Next, Is.Not.Null);
            Assert.That(three.Next.Id, Is.EqualTo("floor_final_swarm"));
            Assert.That(three.Next.FloorNumber, Is.EqualTo(DescentSimulation.FinaleFloor));
            Assert.That(DescentSimulation.FinaleFloor, Is.EqualTo(4));
            Assert.That(three.Next.Next, Is.Null);

            // The districts: floor 3's later halls go on from floors 1 and 2's authored ones - the east court, the north
            // court, the workyard.
            int before = DescentSimulation.LaterHalls(one) + DescentSimulation.LaterHalls(two);
            Assert.That(before, Is.EqualTo(6));
            float[][] expected = { new[] { 8f, -8f }, new[] { 4f, 12f }, new[] { -25f, 7f } };
            for (int hall = 0; hall < expected.Length; hall++)
            {
                FoundryMap.RoomSite(before, hall + 1, out float x, out float y);
                FoundryMap.ToFloor(expected[hall][0], expected[hall][1], out float ex, out float ey);
                Assert.That((x, y), Is.EqualTo((ex, ey)), $"floor 3's later hall {hall + 1}");
            }
        }

        // Floor 3's Warden is the first the climb's boss growth reaches (ClimbScaling.Boss(3), x1.15): its authored 300
        // times x1.96, x1.15, the floor's own share and the run's difficulty, through the scene's WardenRule.GateHealthShareOn.
        [Test]
        public void FloorThreesWardenTakesTheBossGrowthAndItsOwnShare()
        {
            DescentSimulation.FoundryFloor three = DescentSimulation.FoundryFloorOne().Next.Next;
            float share = DescentSimulation.LaterFloorLevers(FoundryBatch.Shipped(), three).WardenHealthShare;
            Assert.That(share, Is.EqualTo(WardenRule.GateHealthShareOn(three.GateHealthShare)));
            Assert.That(share, Is.EqualTo(DescentSimulation.FloorThreeGateShare));
            Assert.That(ClimbScaling.Boss(3), Is.EqualTo(ClimbScaling.BossGrowth));
            Assert.That(ClimbScaling.Boss(2), Is.EqualTo(1f), "floor 2's gate boss has none");
        }

        // A floor-2 clear goes down to floor 3 and plays its rooms in order at their districts; floor 3's gate holds for its
        // clock's 3:00; its Warden walks out at its share; with three floors asked for nothing follows it, with four a
        // floor-3 clear goes down into the finale; and floors 1 and 2 are the same runs either way.
        [Test]
        public void AFloorTwoClearGoesDownToFloorThreeAndAFloorThreeClearToTheFinale()
        {
            RoomKind[] rooms = { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Boss };
            float[][] districts = { new[] { 8f, -8f }, new[] { 4f, 12f }, new[] { -25f, 7f } };
            int wentDown = 0, gated = 0, finales = 0;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                string where = $"{hero.Name} seed {seed}";
                var lines = new List<string>();
                DescentSimulation.FoundryResult first = DescentSimulation.RunFoundry(hero, seed, Floors(3), trace: lines.Add);
                DescentSimulation.FoundryResult two = first.NextFloor;
                if (two == null)
                    continue;
                DescentSimulation.FoundryResult run = two.NextFloor;
                Assert.That(run != null, Is.EqualTo(two.Cleared), where + ": a floor-2 clear goes down, anything else ends the run");
                if (run == null)
                    continue;
                wentDown++;
                Assert.That(run.Floor, Is.EqualTo(3), where);
                Assert.That(run.NextFloor, Is.Null, where + ": three floors asked for");
                Assert.That(run.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout), where);
                Assert.That(run.Kinds, Is.EqualTo(rooms), where);
                int descent = lines.FindIndex(l => l.Contains(" floor_descend floor=3 "));
                Assert.That(descent, Is.GreaterThanOrEqualTo(0), where);
                Assert.That(lines[descent], Does.EndWith($"floor=3 level={two.Level} hp={two.HeroHealth.ToString("0.###", CultureInfo.InvariantCulture)}"),
                    where + ": the hero goes down as floor 2 left it");
                Assert.That(run.DescentSeconds, Is.EqualTo(DescentSimulation.AdvanceDelay).Within(2.5f / 60f), where);

                var started = lines.Skip(descent).Where(l => l.Contains(" room_start ")).ToList();
                for (int i = 0; i < started.Count; i++)
                    Assert.That(started[i], Does.Contain($"room_start room={i + 1} kind={rooms[i]} "), where);
                int hall = 0;
                for (int i = descent; i < lines.Count; i++)
                {
                    if (!lines[i].Contains(" hall_reached "))
                        continue;
                    Assert.That(hall, Is.LessThan(districts.Length), where + ": floor 3's later halls");
                    string frame = lines.Skip(i + 1).First(l => l.Contains(" frame "));
                    FoundryMap.ToFloor(districts[hall][0], districts[hall][1], out float x, out float y);
                    float dx = Field(frame, "x") - x, dy = Field(frame, "y") - y;
                    Assert.That(Math.Sqrt(dx * dx + dy * dy), Is.LessThanOrEqualTo(FoundryMap.RoomSiteRadius + .01f),
                        $"{where}: floor 3's hall {hall + 2} at its district");
                    hall++;
                }
                if (run.Cleared)
                    Assert.That(hall, Is.EqualTo(districts.Length), where);

                if (run.LevelAtGate >= 0)
                {
                    gated++;
                    Assert.That(run.ClockAtGate, Is.GreaterThanOrEqualTo(180f), where + ": floor 3's gate holds for its clock's 3:00");
                    Assert.That(run.LevelAtGate, Is.GreaterThanOrEqualTo(two.Level), where);
                    Assert.That(run.WardenHealth, Is.EqualTo(DescentSimulation.Warden.Health * DescentSimulation.FloorThreeGateShare *
                                                             ClimbScaling.Health(3) * ClimbScaling.Boss(3) * run.DifficultyAtGate).Within(.05f), where);
                }

                // Asked for four floors, the same three floors and then the finale after a floor-3 clear.
                DescentSimulation.FoundryResult whole = DescentSimulation.RunFoundry(hero, seed, Floors(DescentSimulation.FinaleFloor));
                AssertSame(whole, first, where + " floor 1");
                AssertSame(whole.NextFloor, two, where + " floor 2");
                AssertSame(whole.NextFloor.NextFloor, run, where + " floor 3");
                DescentSimulation.FoundryResult finale = whole.NextFloor.NextFloor.NextFloor;
                Assert.That(finale != null, Is.EqualTo(run.Cleared), where + ": a floor-3 clear goes down into the finale");
                Assert.That(FoundryBatch.FinaleOf(whole), Is.SameAs(finale), where);
                if (finale == null)
                    continue;
                finales++;
                Assert.That(finale.Floor, Is.EqualTo(DescentSimulation.FinaleFloor), where);
                Assert.That(finale.SwarmEntryLevel, Is.EqualTo(run.Level), where + ": the hero goes down as floor 3 left it");
                Assert.That(FoundryBatch.DescentSeconds(whole),
                    Is.EqualTo(first.FloorSeconds + two.FloorSeconds + run.FloorSeconds + finale.FloorSeconds).Within(1e-3f), where);
            }
            Assert.That(wentDown, Is.GreaterThan(0), "a run went down to floor 3");
            Assert.That(gated, Is.GreaterThan(0), "a run met floor 3's Warden");
            Assert.That(finales, Is.GreaterThan(0), "a run went on into the finale");
        }

        // The batch report's floors: FOUNDRY_FLOORS=2 stops after floor 2 as before, 3 after floor 3, and only 4 reaches the
        // finale, which FoundryBatch.FinaleOf finds however many floors stand before it.
        [Test]
        public void TheFloorsAskedForStopWhereTheySay()
        {
            DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
            DescentSimulation.FoundryResult two = DescentSimulation.RunFoundry(hero, 11, Floors(2));
            Assert.That(two.NextFloor?.NextFloor, Is.Null, "two floors");
            Assert.That(FoundryBatch.FinaleOf(two), Is.Null);
            DescentSimulation.FoundryResult three = DescentSimulation.RunFoundry(hero, 11, Floors(3));
            Assert.That(three.NextFloor?.NextFloor?.NextFloor, Is.Null, "three floors");
            Assert.That(FoundryBatch.FinaleOf(three), Is.Null, "floor 3 is no finale");

            // A research chain from before floor 3 (FoundryTwoFloorClimb) still finds its finale after floor 2.
            DescentSimulation.FoundryFloor old = DescentSimulation.FoundryTwoFloorClimb();
            Assert.That(old.Next.Next.Id, Is.EqualTo("floor_final_swarm"));
            Assert.That(old.Next.Next.FloorNumber, Is.EqualTo(3));
            DescentSimulation.FoundryResult before = DescentSimulation.RunFoundry(hero, 11, Floors(3), old);
            if (before.NextFloor != null && before.NextFloor.Cleared)
                Assert.That(FoundryBatch.FinaleOf(before), Is.SameAs(before.NextFloor.NextFloor));
        }

        private static float Field(string line, string name)
        {
            int at = line.IndexOf(" " + name + "=", StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), line);
            int from = at + name.Length + 2;
            int to = line.IndexOf(' ', from);
            return float.Parse(to < 0 ? line.Substring(from) : line.Substring(from, to - from), CultureInfo.InvariantCulture);
        }

        // Two floors' results alike in what the reports read: the same floor, outcome, time, level, health and kills.
        private static void AssertSame(DescentSimulation.FoundryResult actual, DescentSimulation.FoundryResult expected, string where)
        {
            Assert.That(actual, Is.Not.Null, where);
            Assert.That((actual.Floor, actual.Outcome, actual.Cleared, actual.Level, actual.LevelAtGate),
                Is.EqualTo((expected.Floor, expected.Outcome, expected.Cleared, expected.Level, expected.LevelAtGate)), where);
            Assert.That((actual.FloorSeconds, actual.HeroHealth, actual.HeroMaximum, actual.WardenSeconds, actual.Kills.Count),
                Is.EqualTo((expected.FloorSeconds, expected.HeroHealth, expected.HeroMaximum, expected.WardenSeconds, expected.Kills.Count)), where);
        }
    }
}
