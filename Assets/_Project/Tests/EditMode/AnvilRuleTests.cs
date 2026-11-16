using System;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Basma's rule (docs/54 section 4, the lead's decisions of 2026-11-16): the lead's odds, the uneven pity and the hard
    // ceiling, a failure that keeps the item and its level, and the numbers Tools/Economy/gear_economy.py prints for one
    // item (expected strikes, worst case, the gold from +0). Also in Tools/CombatChecks.
    public sealed class AnvilRuleTests
    {
        [Test]
        public void TheTableIsTheLeadsOdds()
        {
            int[] chances = new int[9];
            for (int target = 1; target <= 9; target++)
                chances[target - 1] = AnvilTable.BaseChancePercent(target);
            Assert.That(chances, Is.EqualTo(new[] { 100, 100, 100, 90, 75, 60, 45, 30, 20 }));
            Assert.That(AnvilTable.GoldCost(9), Is.EqualTo(2400));
            Assert.That(AnvilTable.TryMaterialCost(3, out _, out _), Is.False, "+1..+3 spend gold alone.");
            Assert.That(AnvilTable.TryMaterialCost(6, out MaterialKind six, out int sixAmount), Is.True);
            Assert.That((six, sixAmount), Is.EqualTo((MaterialKind.EmberShard, 4)));
            Assert.That(AnvilTable.TryMaterialCost(7, out MaterialKind seven, out _) && seven == MaterialKind.QuicksilverBead);
            Assert.That(AnvilTable.TryMaterialCost(9, out MaterialKind nine, out int nineAmount), Is.True);
            Assert.That((nine, nineAmount), Is.EqualTo((MaterialKind.GloamSpore, 6)));
            Assert.Throws<ArgumentOutOfRangeException>(() => AnvilTable.GoldCost(10));
        }

        [Test]
        public void AFailureKeepsTheLevelAndAddsThePity()
        {
            // +8 -> +9 at 20 %: a roll of 20 fails.
            AnvilStrike failed = AnvilRule.Strike(8, 0, 0, false, 20);
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.LevelAfter, Is.EqualTo(8), "No downgrade, no break.");
            Assert.That(failed.PityAfter, Is.EqualTo(1));
            Assert.That(AnvilRule.ChancePercent(8, 1, 0, false), Is.EqualTo(22), "+2 points a failure at +9.");

            AnvilStrike held = AnvilRule.Strike(8, 3, 0, false, 25);
            Assert.That(held.Success, Is.True, "20 + 3 x 2 = 26 > 25.");
            Assert.That(held.LevelAfter, Is.EqualTo(9));
            Assert.That(held.PityAfter, Is.Zero, "A success resets the pity.");
        }

        [Test]
        public void TheCeilingHoldsTheStrikeAfterItsFailuresInARow()
        {
            Assert.That(AnvilRule.ChancePercent(8, 9, 0, false), Is.EqualTo(38));
            Assert.That(AnvilRule.ChancePercent(8, 10, 0, false), Is.EqualTo(100), "Ten failures at +9: the eleventh holds.");
            AnvilStrike ceiling = AnvilRule.Strike(8, 10, 0, false, 99);
            Assert.That(ceiling.Success && ceiling.CeilingHeld, Is.True);
            Assert.That(AnvilRule.ChancePercent(3, 2, 0, false), Is.EqualTo(100), "Two failures at +4.");
        }

        [Test]
        public void ASealAddsTenPointsAndAHammerHoldsUpToSix()
        {
            Assert.That(AnvilRule.ChancePercent(8, 0, 1, false), Is.EqualTo(30));
            Assert.Throws<ArgumentOutOfRangeException>(() => AnvilRule.ChancePercent(8, 0, 2, false), "One Seal a strike.");
            Assert.That(AnvilRule.HammerAllowed(5), Is.True, "+5 -> +6.");
            Assert.That(AnvilRule.HammerAllowed(6), Is.False, "+6 -> +7 is past the Hammer.");
            Assert.That(AnvilRule.Strike(5, 0, 0, true, 99).Success, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => AnvilRule.Strike(6, 0, 0, true, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => AnvilRule.ChancePercent(9, 0, 0, false), "+9 is the top.");
        }

        // gear_economy.py's level table: attempts (mean) 1.10, 1.31, 1.59, 2.05, 2.86, 3.96 for +4..+9, worst case 3..11,
        // and the gold from +0 449, 776, 1413, 2847, 6568, 16073.
        [Test]
        public void TheExpectedCostsAreTheModels()
        {
            double[] strikes = { 1.00, 1.00, 1.00, 1.10, 1.31, 1.59, 2.05, 2.86, 3.96 };
            for (int target = 1; target <= 9; target++)
                Assert.That(AnvilRule.ExpectedStrikesMillionths(target) / 1e6, Is.EqualTo(strikes[target - 1]).Within(0.006), "+" + target);
            int[] gold = { 50, 130, 250, 449, 776, 1413, 2847, 6568, 16073 };
            for (int level = 1; level <= 9; level++)
                Assert.That(AnvilRule.ExpectedGoldTo(level), Is.EqualTo(gold[level - 1]).Within(1), "+" + level);
            Assert.That(AnvilRule.InheritGold(9), Is.EqualTo(1607), "A tenth of +9's expected gold.");
            Assert.That(AnvilRule.InheritGold(0), Is.Zero);
        }

        [Test]
        public void TheWorstCaseIsTheCeilingPlusOne()
        {
            int[] worst = { 1, 1, 1, 3, 4, 5, 7, 9, 11 };
            for (int target = 1; target <= 9; target++)
            {
                int pity = 0;
                int strikes = 0;
                AnvilStrike strike;
                do
                {
                    strike = AnvilRule.Strike(target - 1, pity, 0, false, 99);
                    pity = strike.PityAfter;
                    strikes++;
                }
                while (!strike.Success && strikes < 50);
                Assert.That(strikes, Is.EqualTo(worst[target - 1]), "+" + target + " with every roll as bad as it can be.");
            }
        }

        [Test]
        public void TheAnvilStreamIsTheSameForTheSameDraw()
        {
            Assert.That(AnvilRule.Roll(1234, 7), Is.EqualTo(AnvilRule.Roll(1234, 7)));
            int differ = 0;
            for (int draw = 0; draw < 20; draw++)
                if (AnvilRule.Roll(1234, draw) != AnvilRule.Roll(1234, draw + 1))
                    differ++;
            Assert.That(differ, Is.GreaterThan(10));
        }
    }
}
