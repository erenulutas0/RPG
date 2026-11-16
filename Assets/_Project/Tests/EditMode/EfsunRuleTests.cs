using System;
using System.Linq;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Efsun's rule (docs/54 section 6): lines by rarity, distinct stats other than the main stat, the tier weights, the
    // reroll's price by rarity, a fifth line and no more, and gold find gone from the pool (the lead, 2026-11-16). Also in
    // Tools/CombatChecks.
    public sealed class EfsunRuleTests
    {
        [Test]
        public void LinesAndPricesFollowTheRarity()
        {
            Assert.That(new[] { UpgradeRarity.Common, UpgradeRarity.Rare, UpgradeRarity.Epic, UpgradeRarity.Gold }
                .Select(EfsunRule.LinesFor), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(new[] { UpgradeRarity.Common, UpgradeRarity.Rare, UpgradeRarity.Epic, UpgradeRarity.Gold }
                .Select(EfsunRule.RerollGold), Is.EqualTo(new[] { 60, 100, 160, 250 }));
            Assert.That(EfsunRule.WeaponLines, Is.EqualTo(2));
        }

        [Test]
        public void ARollNeverRepeatsAStatNorTheMainStat()
        {
            for (int index = 0; index < 500; index++)
            {
                EfsunLine[] lines = EfsunRule.Roll(GearStat.CritChance, 5, RunRandom.Stream(42, RunRandom.Anvil, index));
                Assert.That(lines.Length, Is.EqualTo(5));
                Assert.That(lines.Select(l => l.Stat).Distinct().Count(), Is.EqualTo(5));
                Assert.That(lines.Any(l => l.Stat == GearStat.CritChance), Is.False);
                Assert.That(lines.All(l => l.Tier >= 1 && l.Tier <= 5), Is.True);
            }
        }

        [Test]
        public void TheSameStreamWritesTheSameLines()
        {
            EfsunLine[] a = EfsunRule.Roll(GearStat.Damage, 4, RunRandom.Stream(7, RunRandom.Anvil, 3));
            EfsunLine[] b = EfsunRule.Roll(GearStat.Damage, 4, RunRandom.Stream(7, RunRandom.Anvil, 3));
            Assert.That(a, Is.EqualTo(b));
        }

        // 40 / 30 / 18 / 9 / 3 in a hundred, read over 20,000 lines.
        [Test]
        public void TiersFollowTheirWeights()
        {
            var counts = new int[6];
            for (int index = 0; index < 20000; index++)
                counts[EfsunRule.Roll(GearStat.Damage, 1, RunRandom.Stream(9, RunRandom.Anvil, index))[0].Tier]++;
            double[] expected = { 0, .40, .30, .18, .09, .03 };
            for (int tier = 1; tier <= 5; tier++)
                Assert.That(counts[tier] / 20000.0, Is.EqualTo(expected[tier]).Within(0.015), "tier " + tier);
        }

        [Test]
        public void ABindingRuneAddsAStatTheItemDoesNotCarryUpToFive()
        {
            EfsunLine[] four = EfsunRule.Roll(GearStat.MaxHealth, 4, RunRandom.Stream(1, RunRandom.Anvil, 0));
            EfsunLine fifth = EfsunRule.AddLine(four, GearStat.MaxHealth, RunRandom.Stream(1, RunRandom.Anvil, 1));
            Assert.That(four.Any(l => l.Stat == fifth.Stat) || fifth.Stat == GearStat.MaxHealth, Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                EfsunRule.AddLine(four.Append(fifth).ToArray(), GearStat.MaxHealth, RunRandom.Stream(1, RunRandom.Anvil, 2)));
        }

        [Test]
        public void ThePoolHasThirteenStatsAndNoGoldFind()
        {
            Assert.That(GearStatIds.Count, Is.EqualTo(13));
            Assert.That(Enum.GetValues(typeof(GearStat)).Length, Is.EqualTo(GearStatIds.Count));
            Assert.That(GearStatIds.TryParse("gold_find", out _), Is.False, "Dropped by the lead.");
            foreach (GearStat stat in Enum.GetValues(typeof(GearStat)))
            {
                Assert.That(GearStatIds.TryParse(GearStatIds.Id(stat), out GearStat parsed) && parsed == stat, Is.True);
                Assert.That(EfsunRule.ValueOf(stat, 5), Is.GreaterThan(EfsunRule.ValueOf(stat, 1)));
            }
            Assert.That(EfsunRule.ValueOf(GearStat.Damage, 3), Is.EqualTo(200), "+2 % at tier 3.");
        }
    }
}
