using System;
using System.Linq;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/64's Rootmother Myrr in the foundry simulation, floor 3's gate boss since 2026-11-25 (sections 15-16): the shipped
    // chain holds her at the Warden's Grove (its two Cinder Mites beside her) at the floor's share 4.7 and without saplings,
    // and GloamChain at the shipped levers is that chain, run for run. FoundryLevers.GloamWarden puts the Forge Warden back
    // at its share then (floor 3 before her), alone - floors 1 and 2 and every other room untouched - and her levers tune
    // her under her own name. Her fight plays her rules - snares that creep and plant, her feet, no saplings as she ships -
    // and docs/64 section 3's saplings, for the research, grow into Thornlings when they stand. Read over the Editor
    // measurement's seeds, both heroes.
    public sealed class FoundryMyrrTests
    {
        private static readonly int[] Seeds = { 11, 22, 33, 44 };

        private static DescentSimulation.FoundryLevers Climb()
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped();
            levers.Floors = DescentSimulation.FinaleFloor;
            return levers;
        }

        [Test]
        public void MyrrHoldsFloorThreesGateAndTheWardenComesBackAlone()
        {
            DescentSimulation.FoundryLevers shippedLevers = Climb();
            Assert.That(shippedLevers.GloamWarden, Is.False, "Myrr ships");
            Assert.That(shippedLevers.Gloam, Is.False, "the shipped levers leave floor 3 as it is");
            Assert.That(shippedLevers.MyrrSaplings, Is.EqualTo(MyrrRule.ShippedSaplings).And.Zero, "no saplings as she ships");
            Assert.That(shippedLevers.MyrrHealthShare, Is.Zero, "the floor's own share");
            DescentSimulation.FoundryFloor shipped = DescentSimulation.FoundryFloorOne();
            int boss = Array.IndexOf(shipped.Next.Next.Kinds, RoomKind.Boss);
            Assert.That(shipped.Next.Next.Rooms[boss][0][0], Is.SameAs(DescentSimulation.RootmotherMyrr));
            Assert.That(shipped.Next.Next.GateHealthShare, Is.EqualTo(DescentSimulation.FloorThreeGateShare));
            Assert.That(DescentSimulation.FloorThreeGateShare, Is.EqualTo(4.7f), "docs/64 section 15's confirmed share");
            Assert.That(DescentSimulation.GloamChain(shippedLevers).Next.Next.Rooms[boss][0][0], Is.SameAs(DescentSimulation.RootmotherMyrr));

            DescentSimulation.FoundryLevers warden = shippedLevers.Copy("warden-f3");
            warden.GloamWarden = true;
            Assert.That(warden.Gloam, Is.True);
            DescentSimulation.FoundryFloor chain = DescentSimulation.GloamChain(warden);
            DescentSimulation.FoundryFloor three = chain.Next.Next;
            Assert.That(three.Rooms[boss][0][0], Is.SameAs(DescentSimulation.Warden));
            Assert.That(three.Rooms[boss][0].Skip(1), Is.EqualTo(shipped.Next.Next.Rooms[boss][0].Skip(1)), "the Grove's two Cinder Mites stay");
            for (int room = 0; room < three.Rooms.Length; room++)
                if (room != boss)
                    Assert.That(three.Rooms[room], Is.EqualTo(shipped.Next.Next.Rooms[room]), $"room {room + 1}");
            Assert.That(three.GateHealthShare, Is.EqualTo(DescentSimulation.FloorThreeWardenShare), "the Warden's share then");
            Assert.That(DescentSimulation.FloorThreeWardenShare, Is.EqualTo(5.102041f));
            Assert.That(chain.Next.Rooms[Array.IndexOf(chain.Next.Kinds, RoomKind.Boss)][0][0], Is.SameAs(DescentSimulation.GlassRegent),
                "floor 2's gate is the Regent's");
            Assert.That(chain.Rooms[Array.IndexOf(chain.Kinds, RoomKind.Boss)][0][0], Is.SameAs(DescentSimulation.Warden),
                "floor 1's gate is the Warden's");
            Assert.That(shipped.Next.Next.Rooms[boss][0][0], Is.SameAs(DescentSimulation.RootmotherMyrr), "the shipped chain is untouched");

            DescentSimulation.FoundryLevers share = shippedLevers.Copy("share");
            share.MyrrHealthShare = 4.5f;
            share.MyrrHookDamage = 7f;
            Assert.That(share.Gloam, Is.True);
            DescentSimulation.FoundryFloor other = DescentSimulation.GloamChain(share).Next.Next;
            Assert.That(other.GateHealthShare, Is.EqualTo(4.5f));
            Assert.That(other.Rooms[boss][0][0].Name, Is.EqualTo(DescentSimulation.RootmotherMyrr.Name), "her rules read her name");
            Assert.That(other.Rooms[boss][0][0].Damage, Is.EqualTo(7f));

            // The shipped levers through GloamChain are the shipped chain, run for run, and so is the candidate the
            // confirmation read (docs/64 section 15, `mr:share=4.7:sap=0`: her share and no saplings given as levers); the
            // Warden back plays floors 1 and 2 as they were.
            DescentSimulation.FoundryLevers candidate = shippedLevers.Copy("mr:share=4.7:sap=0");
            candidate.MyrrHealthShare = 4.7f;
            candidate.MyrrSaplings = 0;
            DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
            foreach (int seed in Seeds.Take(2))
            {
                DescentSimulation.FoundryResult a = DescentSimulation.RunFoundry(hero, seed, shippedLevers);
                DescentSimulation.FoundryResult b = DescentSimulation.RunFoundry(hero, seed, shippedLevers, DescentSimulation.GloamChain(shippedLevers));
                DescentSimulation.FoundryResult c = DescentSimulation.RunFoundry(hero, seed, candidate, DescentSimulation.GloamChain(candidate));
                DescentSimulation.FoundryResult w = DescentSimulation.RunFoundry(hero, seed, warden, DescentSimulation.GloamChain(warden));
                for (int floor = 1; a != null; floor++, a = a.NextFloor, b = b.NextFloor, c = c.NextFloor, w = w?.NextFloor)
                {
                    Assert.That(b, Is.Not.Null);
                    Assert.That((b.Outcome, b.Cleared, b.FloorSeconds, b.HeroHealth, b.WardenSeconds, b.Kills.Count),
                        Is.EqualTo((a.Outcome, a.Cleared, a.FloorSeconds, a.HeroHealth, a.WardenSeconds, a.Kills.Count)),
                        $"seed {seed} floor {floor}: the shipped levers, the shipped run");
                    Assert.That((c.Outcome, c.Cleared, c.FloorSeconds, c.HeroHealth, c.WardenSeconds, c.MyrrBeats, c.MyrrSnareHits),
                        Is.EqualTo((a.Outcome, a.Cleared, a.FloorSeconds, a.HeroHealth, a.WardenSeconds, a.MyrrBeats, a.MyrrSnareHits)),
                        $"seed {seed} floor {floor}: the confirmed candidate is the shipped run");
                    if (floor <= 2)
                    {
                        Assert.That(b.MyrrBeats, Is.Zero);
                        Assert.That((w.Outcome, w.FloorSeconds, w.HeroHealth), Is.EqualTo((a.Outcome, a.FloorSeconds, a.HeroHealth)),
                            $"seed {seed} floor {floor}: the Warden back changes nothing before floor 3");
                    }
                    else if (floor == 3)
                        Assert.That(w.MyrrBeats, Is.Zero, $"seed {seed}: the Warden lays no snare");
                }
            }
        }

        // Her fight as she ships, read over every floor 3 that reached her gate: her beats and the snares' creep, the
        // planted seconds, her feet read as each mark goes down, and no saplings at phase two. Floors 1 and 2 play as with
        // the Forge Warden back, seed for seed.
        [Test]
        public void HerFightPlaysHerRules()
        {
            DescentSimulation.FoundryLevers shipped = Climb();
            DescentSimulation.FoundryLevers warden = shipped.Copy("warden-f3");
            warden.GloamWarden = true;
            int fought = 0, beats = 0, phaseTwo = 0, planted = 0;
            float crept = 0f;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult a = DescentSimulation.RunFoundry(hero, seed, warden, DescentSimulation.GloamChain(warden));
                DescentSimulation.FoundryResult b = DescentSimulation.RunFoundry(hero, seed, shipped);
                for (int floor = 1; floor <= 2 && a != null; floor++, a = a.NextFloor, b = b.NextFloor)
                    Assert.That((b.Outcome, b.FloorSeconds, b.HeroHealth), Is.EqualTo((a.Outcome, a.FloorSeconds, a.HeroHealth)),
                        $"{hero.Name} seed {seed} floor {floor}");
                DescentSimulation.FoundryResult three = b;
                if (three == null || three.LevelAtGate < 0)
                    continue;
                fought++;
                beats += three.MyrrBeats;
                crept += three.MyrrCrept;
                planted += three.MyrrSnareHits;
                Assert.That(three.MyrrSnareHits, Is.LessThanOrEqualTo(three.MyrrBeats));
                Assert.That(three.MyrrFeetBeats, Is.LessThanOrEqualTo(three.MyrrBeats));
                Assert.That(three.MyrrPlantedSeconds, Is.EqualTo(three.MyrrSnareHits * MyrrRule.PlantSeconds).Within(1e-3f));
                if (three.MyrrPhaseTwoAt >= 0f)
                    phaseTwo++;
                Assert.That((three.MyrrSaplingsPlanted, three.MyrrThornlingsGrown), Is.EqualTo((0, 0)), "no saplings as she ships");
                if (three.Cleared)
                    Assert.That(three.WardenSeconds, Is.GreaterThan(0f), "her fight's length is read as the Warden's");
            }
            Assert.That(fought, Is.GreaterThan(0), "a run reached her gate");
            Assert.That(beats, Is.GreaterThan(0), "her beats");
            Assert.That(crept, Is.GreaterThan(0f), "her snares crept");
            Assert.That(phaseTwo, Is.GreaterThan(0), "a fight reached her phase two");
            TestContext.WriteLine($"fought {fought}, beats {beats}, caught {planted}, crept {crept:0.0}, phase two {phaseTwo}");
        }

        // docs/64 section 3's saplings (research only: MyrrSaplings at MyrrRule.Saplings): at phase two she plants them, and a
        // sapling that stands grows into a Thornling that joins her and withers with her.
        [Test]
        public void HerSaplingsForTheResearchAreCutOrGrow()
        {
            DescentSimulation.FoundryLevers levers = Climb().Copy("saplings");
            levers.MyrrSaplings = MyrrRule.Saplings;
            int phaseTwo = 0, grown = 0, cut = 0;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult three = DescentSimulation.RunFoundry(hero, seed, levers).NextFloor?.NextFloor;
                if (three == null || three.LevelAtGate < 0)
                    continue;
                if (three.MyrrPhaseTwoAt >= 0f)
                {
                    phaseTwo++;
                    Assert.That(three.MyrrSaplingsPlanted, Is.GreaterThanOrEqualTo(MyrrRule.Saplings), $"{hero.Name} seed {seed}");
                }
                else
                    Assert.That(three.MyrrSaplingsPlanted, Is.Zero, "no saplings before phase two");
                Assert.That(three.MyrrSaplingsCut + three.MyrrThornlingsGrown, Is.LessThanOrEqualTo(three.MyrrSaplingsPlanted));
                Assert.That(three.MyrrThornlingsFelled, Is.LessThanOrEqualTo(three.MyrrThornlingsGrown));
                grown += three.MyrrThornlingsGrown;
                cut += three.MyrrSaplingsCut;
            }
            Assert.That(phaseTwo, Is.GreaterThan(0), "a fight reached her phase two");
            Assert.That(cut + grown, Is.GreaterThan(0), "her saplings were cut or grew");
        }

        // The same seed twice is the same run: her garden and her snare are deterministic.
        [Test]
        public void HerFightRepeats()
        {
            DescentSimulation.FoundryLevers on = Climb().Copy("myrr");
            on.MyrrSaplings = MyrrRule.Saplings;
            DescentSimulation.FoundryHero hero = DescentSimulation.CinderArcanist();
            DescentSimulation.FoundryResult a = DescentSimulation.RunFoundry(hero, 33, on);
            DescentSimulation.FoundryResult b = DescentSimulation.RunFoundry(hero, 33, on);
            for (; a != null; a = a.NextFloor, b = b.NextFloor)
                Assert.That((b.Outcome, b.FloorSeconds, b.HeroHealth, b.MyrrBeats, b.MyrrSnareHits, b.MyrrSaplingsPlanted),
                    Is.EqualTo((a.Outcome, a.FloorSeconds, a.HeroHealth, a.MyrrBeats, a.MyrrSnareHits, a.MyrrSaplingsPlanted)));
        }
    }
}
