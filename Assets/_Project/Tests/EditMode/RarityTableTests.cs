using System;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The four colours and the luck that moves between them (docs/40 sections 7 and 17.4, docs/41 step 3). The draw is
    // integer arithmetic so that every runtime rolls the same tier from the same seed; these tests hold it against the
    // report's formula taken the obvious way, in doubles with Math.Pow, which is fine here and nowhere in play.
    public sealed class RarityTableTests
    {
        private static readonly double[] BaseOdds = { 0.60, 0.25, 0.12, 0.03 };

        // The report's formula, uncapped, as the report writes it.
        private static double Formula(int tier, double luck)
        {
            double total = 0d, mine = 0d;
            for (int i = 0; i < RarityTable.TierCount; i++)
            {
                double weight = BaseOdds[i] * Math.Pow(1d + luck / 25d, 0.4d * i);
                total += weight;
                if (i == tier)
                    mine = weight;
            }
            return mine / total;
        }

        private static double Percent(int tier, float luck) => RarityTable.Probability(tier, luck) * 100d;

        [TestCase(0f)]
        [TestCase(10f)]
        [TestCase(25f)]
        [TestCase(50f)]
        [TestCase(80f)]
        public void TheOddsSumToOneAndNoneIsNegative(float luck)
        {
            double sum = 0d;
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
            {
                double p = RarityTable.Probability(tier, luck);
                Assert.That(p, Is.GreaterThan(0d), $"Tier {tier} at luck {luck} must stay possible.");
                sum += p;
            }
            Assert.That(sum, Is.EqualTo(1d).Within(1e-5));
        }

        // The report's own table (7.3), to the hundredth of a percentage point.
        [TestCase(0f, 60.00, 25.00, 12.00, 3.00)]
        [TestCase(10f, 55.15, 26.29, 14.44, 4.13)]
        [TestCase(25f, 49.68, 27.31, 17.30, 5.71)]
        [TestCase(50f, 43.19, 27.93, 20.80, 8.07)]
        public void TheReportsTableHolds(float luck, double green, double orange, double red, double gold)
        {
            Assert.That(Percent(0, luck), Is.EqualTo(green).Within(0.01), "green");
            Assert.That(Percent(1, luck), Is.EqualTo(orange).Within(0.01), "orange");
            Assert.That(Percent(2, luck), Is.EqualTo(red).Within(0.01), "red");
            Assert.That(Percent(3, luck), Is.EqualTo(gold).Within(0.01), "gold");
        }

        // And its two derived tables (7.3 and 7.4), which are what a player and a balance pass actually feel: the chance
        // of at least one red or gold among three independent cards, and the expected multiplier of a card.
        [TestCase(0f, 38.6, 1.1525)]
        [TestCase(10f, 46.0, 1.1792)]
        [TestCase(25f, 54.4, 1.2118)]
        [TestCase(50f, 64.0, 1.2546)]
        public void TheDerivedTablesHold(float luck, double redOrGoldInThreePercent, double expectedMultiplier)
        {
            double highTier = RarityTable.Probability(2, luck) + RarityTable.Probability(3, luck);
            Assert.That((1d - Math.Pow(1d - highTier, 3)) * 100d, Is.EqualTo(redOrGoldInThreePercent).Within(0.05));

            double expected = 0d;
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
                expected += RarityTable.Probability(tier, luck) * RarityTable.Multiplier((UpgradeRarity)tier);
            Assert.That(expected, Is.EqualTo(expectedMultiplier).Within(1e-3));
        }

        [Test]
        public void GoldNeverFallsAsLuckRises()
        {
            double previous = RarityTable.Probability(3, 0f);
            for (int luck = 1; luck <= 200; luck++)
            {
                double gold = RarityTable.Probability(3, luck);
                Assert.That(gold, Is.GreaterThanOrEqualTo(previous), $"Gold fell between luck {luck - 1} and {luck}.");
                previous = gold;
            }
            Assert.That(RarityTable.Probability(3, 200f), Is.GreaterThan(RarityTable.Probability(3, 0f)));
        }

        [Test]
        public void GreenNeverRisesAsLuckRises()
        {
            double previous = RarityTable.Probability(0, 0f);
            for (int luck = 1; luck <= 200; luck++)
            {
                double green = RarityTable.Probability(0, luck);
                Assert.That(green, Is.LessThanOrEqualTo(previous), $"Green rose between luck {luck - 1} and {luck}.");
                previous = green;
            }
        }

        // Below fifty luck is luck; above it a point counts a quarter, so 80 draws as 57.5 and 250 as 100, and the curve
        // has no step at the cap.
        [TestCase(0f, 0f)]
        [TestCase(30f, 30f)]
        [TestCase(50f, 50f)]
        [TestCase(54f, 51f)]
        [TestCase(80f, 57.5f)]
        [TestCase(250f, 100f)]
        public void LuckPastFiftyCountsAQuarter(float luck, float effective)
        {
            Assert.That(RarityTable.EffectiveLuck(luck), Is.EqualTo(effective).Within(1e-4f));
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
            {
                Assert.That(Percent(tier, luck), Is.EqualTo(Formula(tier, effective) * 100d).Within(0.01),
                    $"tier {tier} at luck {luck} draws as the formula at {effective}");
            }
        }

        [Test]
        public void TheSoftCapBendsTheCurveWithoutBreakingIt()
        {
            // Continuous at the cap: the step from 49 to 50 and from 50 to 51 differ by the quarter and nothing else.
            double below = RarityTable.Probability(3, 50f) - RarityTable.Probability(3, 49f);
            double above = RarityTable.Probability(3, 51f) - RarityTable.Probability(3, 50f);
            Assert.That(above, Is.GreaterThan(0d));
            Assert.That(above / below, Is.EqualTo(0.25).Within(0.02), "Past the cap a point buys a quarter of one below it.");

            // Thirty points past the cap buy what seven and a half below would have, not what thirty would.
            Assert.That(RarityTable.Probability(3, 80f), Is.LessThan(Formula(3, 80d)));
            Assert.That(Percent(3, 80f), Is.EqualTo(Formula(3, 57.5d) * 100d).Within(0.01));
        }

        [Test]
        public void LuckBeyondTheClampDrawsAsTheClamp()
        {
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
            {
                Assert.That(RarityTable.Probability(tier, 1e9f), Is.EqualTo(RarityTable.Probability(tier, RarityTable.MaxLuck)));
                Assert.That(RarityTable.Probability(tier, float.PositiveInfinity),
                    Is.EqualTo(RarityTable.Probability(tier, RarityTable.MaxLuck)));
            }
            Assert.That(RarityTable.Probability(0, RarityTable.MaxLuck), Is.GreaterThan(0d), "Green is always possible.");
        }

        [Test]
        public void NegativeOrUndefinedLuckIsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RarityTable.Probability(0, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => RarityTable.Probability(0, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => RarityTable.Draw(RunRandom.Stream(1, RunRandom.Offers, 0), -0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => RarityTable.Probability(4, 0f));
            Assert.Throws<ArgumentNullException>(() => RarityTable.Draw(null, 0f));
        }

        [TestCase(0f)]
        [TestCase(37.5f)]
        [TestCase(80f)]
        public void ASeededDrawSequenceReproduces(float luck)
        {
            RunRandom first = RunRandom.Stream(9001, RunRandom.Offers, 3);
            RunRandom second = RunRandom.Stream(9001, RunRandom.Offers, 3);
            for (int i = 0; i < 500; i++)
                Assert.That(RarityTable.Draw(second, luck), Is.EqualTo(RarityTable.Draw(first, luck)), $"draw {i}");

            // And a different stream is a different sequence, so the reproduction above is not a constant.
            RunRandom again = RunRandom.Stream(9001, RunRandom.Offers, 3);
            RunRandom other = RunRandom.Stream(9001, RunRandom.Offers, 4);
            int differences = 0;
            for (int i = 0; i < 500; i++)
            {
                if (RarityTable.Draw(again, luck) != RarityTable.Draw(other, luck))
                    differences++;
            }
            Assert.That(differences, Is.GreaterThan(0));
        }

        // A known stream rolls known tiers. If a runtime ever rolled these differently, a seed would show one tier in the
        // scene and another in the simulation, and this is where it would show first (RunRandomTests pins the words).
        [Test]
        public void AKnownStreamRollsKnownTiers()
        {
            RunRandom stream = RunRandom.Stream(1234, RunRandom.Offers, 0);
            var tiers = new UpgradeRarity[12];
            for (int i = 0; i < tiers.Length; i++)
                tiers[i] = RarityTable.Draw(stream, 0f);
            RunRandom replay = RunRandom.Stream(1234, RunRandom.Offers, 0);
            for (int i = 0; i < tiers.Length; i++)
                Assert.That(RarityTable.Draw(replay, 0f), Is.EqualTo(tiers[i]));
            Assert.That(tiers, Is.EqualTo(KnownTiers), string.Join(",", tiers));
        }

        private static readonly UpgradeRarity[] KnownTiers =
        {
            UpgradeRarity.Rare, UpgradeRarity.Common, UpgradeRarity.Epic, UpgradeRarity.Rare,
            UpgradeRarity.Common, UpgradeRarity.Common, UpgradeRarity.Rare, UpgradeRarity.Common,
            UpgradeRarity.Rare, UpgradeRarity.Common, UpgradeRarity.Common, UpgradeRarity.Epic
        };

        // Twenty thousand seeded draws land within a percentage point of the odds Probability reports, so a test that
        // reads Probability is reading what a run rolls.
        [TestCase(0f)]
        [TestCase(50f)]
        public void ObservedFrequenciesMatchTheOdds(float luck)
        {
            const int draws = 20000;
            var counts = new int[RarityTable.TierCount];
            for (int i = 0; i < draws; i++)
            {
                // One stream per draw, the way offers derive theirs, so the sample spans many streams rather than one.
                RunRandom stream = RunRandom.Stream(77, RunRandom.Offers, i);
                counts[(int)RarityTable.Draw(stream, luck)]++;
            }
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
            {
                double observed = counts[tier] * 100d / draws;
                Assert.That(observed, Is.EqualTo(Percent(tier, luck)).Within(1d), $"tier {tier} at luck {luck}");
            }
        }

        [Test]
        public void EachTierScalesACardByTheReportsMultiplier()
        {
            Assert.That(RarityTable.Multiplier(UpgradeRarity.Common), Is.EqualTo(1f));
            Assert.That(RarityTable.Multiplier(UpgradeRarity.Rare), Is.EqualTo(1.25f));
            Assert.That(RarityTable.Multiplier(UpgradeRarity.Epic), Is.EqualTo(1.5f));
            Assert.That(RarityTable.Multiplier(UpgradeRarity.Gold), Is.EqualTo(2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => RarityTable.Multiplier((UpgradeRarity)4));
        }

        // The owner chose (2026-10-26) to keep each card's own green, orange and red values - today's Common, Rare and
        // Epic, which the Descent's measurements stand on - and to put gold above red by the report's red-to-gold step
        // (1.5 to 2, a third more). Fortune alone was re-authored, into the points the new Luck formula reads.
        [Test]
        public void EveryShippedCardKeepsItsTiersAndGoldIsAThirdAboveRed()
        {
            foreach (UpgradeOption card in DescentSimulation.Pool())
            {
                float green = card.ModifierFor(UpgradeRarity.Common).Amount;
                float orange = card.ModifierFor(UpgradeRarity.Rare).Amount;
                float red = card.ModifierFor(UpgradeRarity.Epic).Amount;
                float gold = card.ModifierFor(UpgradeRarity.Gold).Amount;
                Assert.That(green < orange && orange < red && red < gold, Is.True, $"{card.Id} rises with its tier");
                Assert.That(gold, Is.EqualTo(red * 4f / 3f).Within(red * 0.02f + 0.006f), $"{card.Id}: gold a third above red");
                for (int tier = 1; tier < RarityTable.TierCount; tier++)
                    Assert.That(card.ModifierFor((UpgradeRarity)tier).Operation, Is.EqualTo(card.Modifier.Operation));
            }
        }

        [Test]
        public void ACardAuthoredWithOneMagnitudeIsTheSameAtAllFourTiers()
        {
            var card = new UpgradeOption("upgrade_damage", "Damage", "+{0:0}", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Flat, 5f), 5);
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
                Assert.That(card.ModifierFor((UpgradeRarity)tier).Amount, Is.EqualTo(5f));
            Assert.That(card.DescriptionValueFor(UpgradeRarity.Gold), Is.EqualTo(5f));
        }

        [Test]
        public void AGoldCardIsItsOwnMagnitudeAndShowsIt()
        {
            var card = new UpgradeOption("upgrade_damage", "Damage", "+{0:0}%", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.5f), new StatModifier(ModifierOperation.Percent, 0.625f),
                new StatModifier(ModifierOperation.Percent, 0.75f), new StatModifier(ModifierOperation.Percent, 1f), 5);
            Assert.That(card.ModifierFor(UpgradeRarity.Gold).Amount, Is.EqualTo(1f));
            Assert.That(card.DescriptionValueFor(UpgradeRarity.Gold), Is.EqualTo(100f));
            Assert.Throws<ArgumentException>(() => new UpgradeOption("a", "A", "", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Flat, 1f), new StatModifier(ModifierOperation.Flat, 1f),
                new StatModifier(ModifierOperation.Flat, 1f), new StatModifier(ModifierOperation.Percent, 1f), 1),
                "A card's gold tier scales the same way as its others.");
        }

        // A level-up and a book both roll the new tiers through the same table, so a run with enough offers sees gold.
        [Test]
        public void AnOfferCanRollEveryTier()
        {
            var seen = new bool[RarityTable.TierCount];
            for (int seed = 0; seed < 400; seed++)
            {
                var run = new Cryptforge.Core.RunState(10, 0.5f, 0, seed);
                var service = new UpgradeService(run, new Cryptforge.Combat.WeaponRuntime(10f, 0.8f, 3f),
                    DescentSimulation.FiveCardPool(), 3);
                run.AddExperience(10);
                UpgradeOffer offer = service.CurrentOffer;
                for (int slot = 0; slot < offer.Choices.Count; slot++)
                    seen[(int)offer.RarityAt(slot)] = true;
            }
            Assert.That(seen, Is.All.True, "Four hundred offers of three cards should show every colour.");
        }
    }
}
