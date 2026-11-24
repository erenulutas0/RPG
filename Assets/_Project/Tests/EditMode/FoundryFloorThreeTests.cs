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
        // tier, with no modifier, the plain horde, no surges or extra elite, its gate held for 3:00 and its gate boss's own
        // share (Rootmother Myrr's 4.7 since 2026-11-25, docs/64); the finale the climb's fourth floor.
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
            // The Pollen Wraith takes the Mite's role (the lead's decision 2026-11-17): the Mite reskinned, so the simulation
            // plays it as the Mite itself and every run is the one it was.
            Assert.That(three.HordeRoles, Is.EqualTo(new[] { DescentSimulation.PollenWraith, DescentSimulation.Grunt, DescentSimulation.Runner }));
            Assert.That(DescentSimulation.PollenWraith, Is.SameAs(DescentSimulation.Mite), "the Wraith is the Mite reskinned");
            Assert.That((three.ModifierHealthPercent, three.ModifierDamagePercent, three.ModifierGoldPercent), Is.EqualTo((0f, 0f, 0f)));
            Assert.That((three.HallSurges, three.ExtraElites, three.GateNotBeforeSeconds), Is.EqualTo((0, 0, 180f)));
            Assert.That(DescentSimulation.FloorThreeGateSeconds, Is.EqualTo(180f), "half a minute before floors 1 and 2");
            Assert.That(three.GateHealthShare, Is.EqualTo(DescentSimulation.FloorThreeGateShare));
            Assert.That(WardenRule.IsFloorGateHealthShare(three.GateHealthShare) && three.GateHealthShare > 0f, Is.True);
            Assert.That(DescentSimulation.GloamGardens.HealthMultiplier, Is.EqualTo(ClimbScaling.Health(3)).Within(1e-5f));
            Assert.That(DescentSimulation.GloamGardens.DamageMultiplier, Is.EqualTo(ClimbScaling.Damage(3)).Within(1e-5f));

            // Floor 2's rooms wave for wave: the same parts, the garden's names (the asset holds them), the halls' Mites the
            // Pollen Wraiths the simulation plays as Mites - but its elite room, Thorn Post, is led by the Cinder Bloom where
            // floor 2's Sentry Hall has the Mirror Sentry (docs/52, 2026-11-15), and its gate, the Warden's Grove, holds
            // Rootmother Myrr since 2026-11-25 (docs/64; the Forge Warden before) where floor 2's has the Glass Regent; the
            // escorts are the same, Cinder Mites both.
            Assert.That(three.Rooms.Length, Is.EqualTo(DescentSimulation.QuicksilverVaults.Rooms.Length));
            int elite = System.Array.IndexOf(three.Kinds, RoomKind.Elite);
            int gate = System.Array.IndexOf(three.Kinds, RoomKind.Boss);
            for (int room = 0; room < three.Rooms.Length; room++)
            {
                if (room == elite || room == gate)
                    continue;
                Assert.That(three.Rooms[room], Is.EqualTo(DescentSimulation.QuicksilverVaults.Rooms[room]), $"room {room + 1}");
            }
            Assert.That(three.Rooms[elite][0][0], Is.SameAs(DescentSimulation.CinderBloom), "Thorn Post's Cinder Bloom since 2026-11-15");
            Assert.That(DescentSimulation.QuicksilverVaults.Rooms[elite][0][0], Is.SameAs(DescentSimulation.MirrorSentry));
            Assert.That(three.Rooms[elite][0].Skip(1), Is.EqualTo(DescentSimulation.QuicksilverVaults.Rooms[elite][0].Skip(1)));
            Assert.That(three.Rooms[gate][0][0], Is.SameAs(DescentSimulation.RootmotherMyrr));
            Assert.That(DescentSimulation.QuicksilverVaults.Rooms[gate][0][0], Is.SameAs(DescentSimulation.GlassRegent));
            Assert.That(three.Rooms[gate][0].Skip(1), Is.EqualTo(DescentSimulation.QuicksilverVaults.Rooms[gate][0].Skip(1)));

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

        // Floor 3's gate boss is the first the climb's boss growth reaches (ClimbScaling.Boss(3), x1.15): her authored 300
        // times x1.96, x1.15, the floor's own share and the run's difficulty, through the scene's WardenRule.GateHealthShareOn.
        // Rootmother Myrr since 2026-11-25 (docs/64) at 4.7; the Forge Warden's 5.102041 before (FloorThreeWardenShare).
        [Test]
        public void FloorThreesGateBossTakesTheBossGrowthAndItsOwnShare()
        {
            DescentSimulation.FoundryFloor three = DescentSimulation.FoundryFloorOne().Next.Next;
            float share = DescentSimulation.LaterFloorLevers(FoundryBatch.Shipped(), three).WardenHealthShare;
            Assert.That(share, Is.EqualTo(WardenRule.GateHealthShareOn(three.GateHealthShare)));
            Assert.That(share, Is.EqualTo(DescentSimulation.FloorThreeGateShare));
            Assert.That(share, Is.EqualTo(4.7f), "docs/64 section 15's confirmed share");
            Assert.That(share, Is.LessThan(DescentSimulation.FloorThreeWardenShare), "her fight's seconds taken back (docs/64 section 10)");
            Assert.That(ClimbScaling.Boss(3), Is.EqualTo(ClimbScaling.BossGrowth));
            Assert.That(ClimbScaling.Boss(2), Is.EqualTo(1f), "floor 2's gate boss has none");
        }

        // A floor-2 clear goes down to floor 3 and plays its rooms in order at their districts; floor 3's gate holds for its
        // clock's 3:00; its gate boss, Rootmother Myrr, walks out at its share; with three floors asked for nothing follows
        // it, with four a floor-3 clear goes down into the finale; and floors 1 and 2 are the same runs either way.
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
                    Assert.That(run.WardenHealth, Is.EqualTo(DescentSimulation.RootmotherMyrr.Health * DescentSimulation.FloorThreeGateShare *
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
            Assert.That(gated, Is.GreaterThan(0), "a run met floor 3's gate boss");
            Assert.That(finales, Is.GreaterThan(0), "a run went on into the finale");
        }

        // docs/52's Cinder Bloom leads Thorn Post as shipped (`bloom-m20-h70-r1.2`, the lead's decisions 2026-11-15): 196 health
        // (384 on floor 3), its Bark Maul 2.16 every 1.8 s at reach 1.2 (3.9 a blow on floor 3). With the levers at the shipped
        // Bloom, GloamChain is the shipped chain and the same runs; FoundryLevers.GloamCaptain puts the Grunt Captain back,
        // and the Bloom's levers tune it under its own name; the escort and every other room stay.
        [Test]
        public void ThornPostIsTheCinderBloomsAndTheLeversMoveOnlyIt()
        {
            DescentSimulation.FoundryLevers off = Floors(DescentSimulation.FinaleFloor);
            DescentSimulation.FoundryFloor shipped = DescentSimulation.FoundryFloorOne().Next.Next;
            int elite = Array.IndexOf(shipped.Kinds, RoomKind.Elite);
            Assert.That(shipped.Rooms[elite][0][0], Is.SameAs(DescentSimulation.CinderBloom));
            Assert.That(off.Gloam, Is.False, "the shipped levers leave Thorn Post as it is");
            Assert.That(DescentSimulation.GloamChain(off).Next.Next.Rooms[elite][0][0], Is.SameAs(DescentSimulation.CinderBloom));

            DescentSimulation.Enemy bloom = DescentSimulation.CinderBloom;
            Assert.That((bloom.Health, bloom.Damage, bloom.Interval, bloom.InitialDelay, bloom.Reach, bloom.Speed, bloom.Gold, bloom.Experience),
                Is.EqualTo((196f, 2.16f, 1.8f, .6f, 1.2f, 1.2f, 20, 10)));
            Assert.That(bloom.Damage, Is.EqualTo(DescentSimulation.Docs52MaulDamage * .2f), "20 % of docs/52's maul");
            Assert.That(bloom.Health, Is.EqualTo(DescentSimulation.Docs52Health * .7f), "70 % of docs/52's health");
            Assert.That(bloom.Health * ClimbScaling.Health(3), Is.EqualTo(384f).Within(.5f));
            Assert.That(bloom.Damage * ClimbScaling.Damage(3), Is.EqualTo(3.9f).Within(.05f));

            DescentSimulation.FoundryLevers captain = off.Copy("captain");
            captain.GloamCaptain = true;
            DescentSimulation.FoundryFloor before = DescentSimulation.GloamChain(captain).Next.Next;
            Assert.That(before.Id, Is.EqualTo(shipped.Id));
            Assert.That(before.Kinds, Is.EqualTo(shipped.Kinds));
            for (int room = 0; room < shipped.Rooms.Length; room++)
                if (room != elite)
                    Assert.That(before.Rooms[room], Is.EqualTo(shipped.Rooms[room]), $"room {room + 1}");
            Assert.That(before.Rooms[elite][0][0], Is.SameAs(DescentSimulation.Captain));
            Assert.That(before.Rooms[elite][0].Skip(1), Is.EqualTo(shipped.Rooms[elite][0].Skip(1)), "the Grunt and two Cinder Mites stay");
            Assert.That(before.Next.Id, Is.EqualTo("floor_final_swarm"));
            Assert.That(shipped.Rooms[elite][0][0], Is.SameAs(DescentSimulation.CinderBloom), "the shipped chain is untouched");

            DescentSimulation.FoundryLevers tuned = off.Copy("tuned");
            tuned.BloomHealthScale = .8f;
            tuned.BloomMaulDamage = DescentSimulation.Docs52MaulDamage;
            tuned.BloomMaulReach = DescentSimulation.Docs52MaulReach;
            DescentSimulation.Enemy other = DescentSimulation.GloamChain(tuned).Next.Next.Rooms[elite][0][0];
            Assert.That(other.Name, Is.EqualTo(bloom.Name), "the Bloom's rules read its name");
            Assert.That(other.Health, Is.EqualTo(224f).Within(1e-3f));
            Assert.That((other.Damage, other.Reach), Is.EqualTo((10.8f, 1.5f)));

            DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
            foreach (int seed in Seeds.Take(2))
            {
                DescentSimulation.FoundryResult a = DescentSimulation.RunFoundry(hero, seed, off);
                DescentSimulation.FoundryResult b = DescentSimulation.RunFoundry(hero, seed, off, DescentSimulation.GloamChain(off));
                for (int floor = 1; a != null; floor++, a = a.NextFloor, b = b.NextFloor)
                    AssertSame(b, a, $"seed {seed} floor {floor}: the shipped levers, the shipped run");
            }
        }

        // The Bloom's cycle in a fight: it walks in closed, so no fight is shorter than its closed phase; the bark blunts the
        // closed landings and lets the open ones through. Read over every run that met it, both heroes, rather than one seed.
        [Test]
        public void TheBloomsCycleShapesThornPostsFight()
        {
            DescentSimulation.FoundryLevers levers = Floors(3);
            int met = 0, closed = 0, open = 0;
            float barked = 0f;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult three = DescentSimulation.RunFoundry(hero, seed, levers).NextFloor?.NextFloor;
                if (three?.EliteFights == null)
                    continue;
                foreach (DescentSimulation.EliteFight fight in three.EliteFights)
                {
                    met++;
                    Assert.That(fight.Kind, Is.EqualTo(DescentSimulation.CinderBloom.Name));
                    Assert.That(fight.Lifetime, Is.GreaterThanOrEqualTo(BloomRule.ClosedSeconds), $"{hero.Name} seed {seed}: it walks in closed");
                    Assert.That(fight.Windows, Is.GreaterThanOrEqualTo(1), $"{hero.Name} seed {seed}");
                    Assert.That(fight.Health, Is.GreaterThanOrEqualTo(DescentSimulation.CinderBloom.Health * ClimbScaling.Health(3) - .01f),
                        "floor 3's tier, and the altars' difficulty above it");
                }
                closed += three.BloomClosedLandings;
                open += three.BloomOpenLandings;
                barked += three.BloomBarked;
            }
            Assert.That(met, Is.GreaterThan(0), "a run met the Bloom");
            Assert.That(closed, Is.GreaterThan(0));
            Assert.That(open, Is.GreaterThan(0));
            Assert.That(barked, Is.GreaterThan(0f));
        }

        // docs/52's Pollen Wraith and Thornling (research levers, off by default): the Wraith takes the Mite's place in floor
        // 3's horde (role 0) and its three halls, the Thornling the Runner's in the horde (role 2) and Ash Orchard's second
        // wave; Thorn Post's and the Grove's Cinder Mites stay, and floors 1 and 2 are untouched. Each keeps the body it
        // replaces, so with its addition switched off (no pollen rises, no Thornling ever dives) the floor is the shipped one.
        [Test]
        public void TheWraithsAndTheThornlingsTakeTheMitesAndTheRunnersPlaces()
        {
            DescentSimulation.FoundryLevers off = Floors(DescentSimulation.FinaleFloor);
            DescentSimulation.FoundryLevers on = off.Copy("on");
            on.GloamWraiths = true;
            on.GloamThornlings = true;
            Assert.That((off.GloamWraiths, off.GloamThornlings), Is.EqualTo((false, false)), "off by default");
            Assert.That(on.Gloam, Is.True);
            DescentSimulation.FoundryFloor shipped = DescentSimulation.FoundryFloorOne();
            DescentSimulation.FoundryFloor chain = DescentSimulation.GloamChain(on);
            Assert.That(chain.Rooms, Is.EqualTo(shipped.Rooms), "floor 1");
            Assert.That(chain.Next.Rooms, Is.EqualTo(shipped.Next.Rooms), "floor 2");
            DescentSimulation.FoundryFloor three = chain.Next.Next, before = shipped.Next.Next;
            Assert.That(three.HordeRoles, Is.EqualTo(new[] { DescentSimulation.PollinatingWraith, DescentSimulation.Grunt, DescentSimulation.Thornling }));
            Assert.That(before.HordeRoles, Is.EqualTo(new[] { DescentSimulation.Mite, DescentSimulation.Grunt, DescentSimulation.Runner }), "the shipped chain is untouched");
            int wraiths = 0, thornlings = 0;
            for (int room = 0; room < three.Rooms.Length; room++)
            for (int wave = 0; three.Rooms[room] != null && wave < three.Rooms[room].Length; wave++)
            for (int i = 0; i < three.Rooms[room][wave].Length; i++)
            {
                DescentSimulation.Enemy was = before.Rooms[room][wave][i], now = three.Rooms[room][wave][i];
                bool hall = three.Kinds[room] == RoomKind.Combat;
                if (hall && was == DescentSimulation.Mite)
                {
                    Assert.That(now, Is.SameAs(DescentSimulation.PollinatingWraith), $"room {room + 1}");
                    wraiths++;
                }
                else if (hall && was == DescentSimulation.Runner)
                {
                    Assert.That(now, Is.SameAs(DescentSimulation.Thornling), $"room {room + 1}");
                    thornlings++;
                }
                else
                    Assert.That(now, Is.SameAs(was), $"room {room + 1}: Thorn Post's and the Grove's stay");
            }
            Assert.That((wraiths, thornlings), Is.EqualTo((6 + 8 + 3, 1)), "Ash Orchard 6, Rootbound Walk 8, Bloom Gate 3; Ash Orchard's Runner");
            foreach ((DescentSimulation.Enemy a, DescentSimulation.Enemy b) in new[]
                     { (DescentSimulation.PollinatingWraith, DescentSimulation.Mite), (DescentSimulation.Thornling, DescentSimulation.Runner) })
                Assert.That((a.Health, a.Damage, a.Interval, a.InitialDelay, a.Speed, a.Reach, a.Gold, a.Experience),
                    Is.EqualTo((b.Health, b.Damage, b.Interval, b.InitialDelay, b.Speed, b.Reach, b.Gold, b.Experience)), a.Name);

            DescentSimulation.FoundryLevers quiet = on.Copy("quiet");
            quiet.WraithRate = 0f;
            quiet.ThornlingWalk = float.PositiveInfinity;
            DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
            foreach (int seed in Seeds.Take(2))
            {
                DescentSimulation.FoundryResult a = DescentSimulation.RunFoundry(hero, seed, off);
                DescentSimulation.FoundryResult b = DescentSimulation.RunFoundry(hero, seed, quiet, DescentSimulation.GloamChain(quiet));
                for (int floor = 1; a != null; floor++, a = a.NextFloor, b = b.NextFloor)
                {
                    AssertSame(b, a, $"seed {seed} floor {floor}: the bodies they replace");
                    Assert.That((b.PollenBlooms, b.ThornRings), Is.EqualTo((0, 0)));
                }
            }
        }

        // With the levers on (the pollen at a radius and rise a floor-3 hero meets), the pollen blooms on a hero that stands
        // (the standing cohort, which never kites) and the Thornlings' rings go down and land no sooner than their telegraph; the ring's mark is the pilot's to leave.
        [Test]
        public void ThePollenBloomsOnAStandingHeroAndTheRingsKeepTheirTelegraph()
        {
            DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(Cryptforge.Core.AutopilotCohort.Standing, 3, null).Copy("standing, both");
            levers.GloamWraiths = true;
            levers.GloamThornlings = true;
            // A pollen the floor-3 hero meets (docs/52's measured section: at the starting 1.6 the horde's bodies fall before
            // they come near enough, and nothing blooms).
            levers.WraithRadius = 4f;
            levers.WraithRate = .3f;
            levers.WraithFall = .1f;
            int blooms = 0, rings = 0, hits = 0, threes = 0;
            float stillNear = 0f;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult three = DescentSimulation.RunFoundry(hero, seed, levers, DescentSimulation.GloamChain(levers)).NextFloor?.NextFloor;
                if (three == null)
                    continue;
                threes++;
                blooms += three.PollenBlooms;
                stillNear += three.StillNearSeconds;
                rings += three.ThornRings;
                hits += three.ThornHits;
                if (three.ThornHits > 0)
                    Assert.That(three.ThornShortestHit, Is.GreaterThanOrEqualTo(ThornlingRule.Telegraph - 1e-3f), $"{hero.Name} seed {seed}");
                Assert.That(three.TakenFromPollen, Is.LessThanOrEqualTo(three.PollenBlooms * levers.WraithShare * three.HeroMaximum + 1e-3f),
                    "a bloom takes its share of the hero's maximum, which only grows");
            }
            Assert.That(threes, Is.GreaterThan(0), "a run went down to floor 3");
            Assert.That(stillNear, Is.GreaterThan(0f));
            Assert.That(blooms, Is.GreaterThan(0), "the standing hero blooms");
            Assert.That(rings, Is.GreaterThan(0));
            Assert.That(hits, Is.LessThanOrEqualTo(rings));
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
