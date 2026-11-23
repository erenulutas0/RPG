using System;
using System.Linq;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/64's Rootmother Myrr in the foundry simulation (research lever FoundryLevers.GloamMyrr, off by default): with the
    // lever off the chain and every run are the shipped ones; on, she takes the Forge Warden's place at floor 3's gate alone
    // (its two Cinder Mites kept, floors 1 and 2 and every other room untouched), at the floor's share unless hers is given,
    // and her fight plays her rules - snares that creep and plant, her feet, saplings in phase two that grow into Thornlings
    // when they stand. Read over the Editor measurement's seeds, both heroes.
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
        public void MyrrTakesTheWardensPlaceAtFloorThreesGateAlone()
        {
            DescentSimulation.FoundryLevers off = Climb();
            Assert.That(off.GloamMyrr, Is.False, "off by default");
            DescentSimulation.FoundryFloor shipped = DescentSimulation.FoundryFloorOne();
            int boss = Array.IndexOf(shipped.Next.Next.Kinds, RoomKind.Boss);
            Assert.That(DescentSimulation.GloamChain(off).Next.Next.Rooms[boss][0][0], Is.SameAs(DescentSimulation.Warden));

            DescentSimulation.FoundryLevers on = off.Copy("myrr");
            on.GloamMyrr = true;
            Assert.That(on.Gloam, Is.True);
            DescentSimulation.FoundryFloor chain = DescentSimulation.GloamChain(on);
            DescentSimulation.FoundryFloor three = chain.Next.Next;
            Assert.That(three.Rooms[boss][0][0], Is.SameAs(DescentSimulation.RootmotherMyrr));
            Assert.That(three.Rooms[boss][0].Skip(1), Is.EqualTo(shipped.Next.Next.Rooms[boss][0].Skip(1)), "the Grove's two Cinder Mites stay");
            for (int room = 0; room < three.Rooms.Length; room++)
                if (room != boss)
                    Assert.That(three.Rooms[room], Is.EqualTo(shipped.Next.Next.Rooms[room]), $"room {room + 1}");
            Assert.That(three.GateHealthShare, Is.EqualTo(DescentSimulation.FloorThreeGateShare), "the floor's share");
            Assert.That(chain.Next.Rooms[Array.IndexOf(chain.Next.Kinds, RoomKind.Boss)][0][0], Is.SameAs(DescentSimulation.GlassRegent),
                "floor 2's gate is the Regent's");
            Assert.That(chain.Rooms[Array.IndexOf(chain.Kinds, RoomKind.Boss)][0][0], Is.SameAs(DescentSimulation.Warden),
                "floor 1's gate is the Warden's");
            Assert.That(shipped.Next.Next.Rooms[boss][0][0], Is.SameAs(DescentSimulation.Warden), "the shipped chain is untouched");

            DescentSimulation.FoundryLevers share = on.Copy("share");
            share.MyrrHealthShare = 4.5f;
            share.MyrrHookDamage = 7f;
            DescentSimulation.FoundryFloor other = DescentSimulation.GloamChain(share).Next.Next;
            Assert.That(other.GateHealthShare, Is.EqualTo(4.5f));
            Assert.That(other.Rooms[boss][0][0].Name, Is.EqualTo(DescentSimulation.RootmotherMyrr.Name), "her rules read her name");
            Assert.That(other.Rooms[boss][0][0].Damage, Is.EqualTo(7f));

            DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
            foreach (int seed in Seeds.Take(2))
            {
                DescentSimulation.FoundryResult a = DescentSimulation.RunFoundry(hero, seed, off);
                DescentSimulation.FoundryResult b = DescentSimulation.RunFoundry(hero, seed, off, DescentSimulation.GloamChain(off));
                for (int floor = 1; a != null; floor++, a = a.NextFloor, b = b.NextFloor)
                {
                    Assert.That(b, Is.Not.Null);
                    Assert.That((b.Outcome, b.Cleared, b.FloorSeconds, b.HeroHealth, b.WardenSeconds, b.Kills.Count),
                        Is.EqualTo((a.Outcome, a.Cleared, a.FloorSeconds, a.HeroHealth, a.WardenSeconds, a.Kills.Count)),
                        $"seed {seed} floor {floor}: the lever off, the shipped run");
                    Assert.That(b.MyrrBeats, Is.Zero);
                }
            }
        }

        // Her fight, read over every floor 3 that reached her gate: her beats and the snares' creep, her feet read as each
        // mark goes down, and in a phase two her saplings - cut, or grown into Thornlings that join her and wither with her.
        // Floors 1 and 2 play as with the lever off, seed for seed.
        [Test]
        public void HerFightPlaysHerRules()
        {
            DescentSimulation.FoundryLevers off = Climb();
            DescentSimulation.FoundryLevers on = off.Copy("myrr");
            on.GloamMyrr = true;
            int fought = 0, beats = 0, phaseTwo = 0, planted = 0, grown = 0, cut = 0;
            float crept = 0f;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult a = DescentSimulation.RunFoundry(hero, seed, off, DescentSimulation.GloamChain(off));
                DescentSimulation.FoundryResult b = DescentSimulation.RunFoundry(hero, seed, on, DescentSimulation.GloamChain(on));
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
                if (three.Cleared)
                    Assert.That(three.WardenSeconds, Is.GreaterThan(0f), "her fight's length is read as the Warden's");
            }
            Assert.That(fought, Is.GreaterThan(0), "a run reached her gate");
            Assert.That(beats, Is.GreaterThan(0), "her beats");
            Assert.That(crept, Is.GreaterThan(0f), "her snares crept");
            Assert.That(phaseTwo, Is.GreaterThan(0), "a fight reached her phase two");
            Assert.That(cut + grown, Is.GreaterThan(0), "her saplings were cut or grew");
            TestContext.WriteLine($"fought {fought}, beats {beats}, caught {planted}, crept {crept:0.0}, phase two {phaseTwo}, cut {cut}, grown {grown}");
        }

        // The same seed twice is the same run: her garden and her snare are deterministic.
        [Test]
        public void HerFightRepeats()
        {
            DescentSimulation.FoundryLevers on = Climb().Copy("myrr");
            on.GloamMyrr = true;
            DescentSimulation.FoundryHero hero = DescentSimulation.CinderArcanist();
            DescentSimulation.FoundryResult a = DescentSimulation.RunFoundry(hero, 33, on, DescentSimulation.GloamChain(on));
            DescentSimulation.FoundryResult b = DescentSimulation.RunFoundry(hero, 33, on, DescentSimulation.GloamChain(on));
            for (; a != null; a = a.NextFloor, b = b.NextFloor)
                Assert.That((b.Outcome, b.FloorSeconds, b.HeroHealth, b.MyrrBeats, b.MyrrSnareHits, b.MyrrSaplingsPlanted),
                    Is.EqualTo((a.Outcome, a.FloorSeconds, a.HeroHealth, a.MyrrBeats, a.MyrrSnareHits, a.MyrrSaplingsPlanted)));
        }
    }
}
