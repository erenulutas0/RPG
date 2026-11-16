using System;

namespace Cryptforge.Progression
{
    // One strike's outcome: whether it held, the chance it was struck at, and the item's level and pity after it.
    public readonly struct AnvilStrike
    {
        public readonly bool Success;
        public readonly int ChancePercent;
        public readonly int LevelAfter;
        public readonly int PityAfter;
        // The ceiling made this strike certain (the failures in a row reached it).
        public readonly bool CeilingHeld;

        public AnvilStrike(bool success, int chancePercent, int levelAfter, int pityAfter, bool ceilingHeld)
        {
            Success = success;
            ChancePercent = chancePercent;
            LevelAfter = levelAfter;
            PityAfter = pityAfter;
            CeilingHeld = ceilingHeld;
        }
    }

    // Basma's rule over AnvilTable (docs/54 section 4): the chance a strike is made at and what it does to the item. Pure
    // and integer throughout, so the Editor, the .NET runner and the phone agree on every strike; the roll comes from the
    // profile's anvil stream (RunRandom.Anvil), never from System.Random.
    public static class AnvilRule
    {
        // Fixed-point scale for the expected-attempt arithmetic: millionths of a millionth.
        private const long Scale = 1000000000000L;

        // Whether the ceiling holds the strike from currentLevel: as many failures in a row as the level's ceiling.
        public static bool CeilingHolds(int currentLevel, int pity)
        {
            int ceiling = AnvilTable.CeilingFailures(currentLevel + 1);
            return ceiling > 0 && pity >= ceiling;
        }

        // A Hammer may serve a strike to +6 or below.
        public static bool HammerAllowed(int currentLevel) => currentLevel >= 0 && currentLevel + 1 <= AnvilTable.HammerMaxLevel;

        // The chance, in whole percent, of the strike from currentLevel to the next: the base, the pity's points, a Seal's,
        // capped at 100; 100 when the ceiling or a Hammer holds it.
        public static int ChancePercent(int currentLevel, int pity, int seals, bool hammer)
        {
            Validate(currentLevel, pity, seals, hammer);
            int target = currentLevel + 1;
            if (hammer || CeilingHolds(currentLevel, pity))
                return 100;
            int chance = AnvilTable.BaseChancePercent(target) + AnvilTable.PityStepPercent(target) * pity +
                         AnvilTable.SealBonusPercent * seals;
            return Math.Min(100, chance);
        }

        // The strike with roll in [0, 100): it holds when roll < chance. A success raises the level and resets the pity;
        // a failure keeps the level and adds one to the pity (never past the ceiling, which already holds the next strike).
        public static AnvilStrike Strike(int currentLevel, int pity, int seals, bool hammer, int roll)
        {
            if (roll < 0 || roll >= 100)
                throw new ArgumentOutOfRangeException(nameof(roll));
            int chance = ChancePercent(currentLevel, pity, seals, hammer);
            bool ceiling = !hammer && CeilingHolds(currentLevel, pity);
            if (roll < chance)
                return new AnvilStrike(true, chance, currentLevel + 1, 0, ceiling);
            return new AnvilStrike(false, chance, currentLevel, pity + 1, false);
        }

        // The roll of the anvil stream's draw-th operation, in [0, 100).
        public static int Roll(int anvilSeed, int draw) => RunRandom.Stream(anvilSeed, RunRandom.Anvil, draw).NextBelow(100);

        // The mean number of strikes to make targetLevel from the level below, with the pity and the ceiling and no
        // boosters, in millionths (3960000 is 3.96 strikes). Exact fixed-point arithmetic, so it is the same everywhere.
        public static long ExpectedStrikesMillionths(int targetLevel)
        {
            int ceiling = AnvilTable.CeilingFailures(targetLevel);
            long failedSoFar = Scale;
            long mean = 0;
            for (int failures = 0; failedSoFar > 0; failures++)
            {
                mean += failedSoFar;
                int chance = ceiling > 0 && failures >= ceiling
                    ? 100
                    : Math.Min(100, AnvilTable.BaseChancePercent(targetLevel) + AnvilTable.PityStepPercent(targetLevel) * failures);
                failedSoFar = failedSoFar * (100 - chance) / 100;
            }
            return mean / (Scale / 1000000L);
        }

        // The gold an item is expected to cost from +0 to level, with the pity and the ceiling and no boosters.
        public static int ExpectedGoldTo(int level)
        {
            if (level < 0 || level > AnvilTable.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            long millionths = 0;
            for (int target = 1; target <= level; target++)
                millionths += ExpectedStrikesMillionths(target) * AnvilTable.GoldCost(target);
            return (int)((millionths + 500000L) / 1000000L);
        }

        // What inheritance costs to move a level onto another item (docs/54 section 3.4): a tenth of that expected gold.
        public static int InheritGold(int level) =>
            (int)(((long)ExpectedGoldTo(level) * AnvilTable.InheritPercent + 50L) / 100L);

        private static void Validate(int currentLevel, int pity, int seals, bool hammer)
        {
            if (currentLevel < 0 || currentLevel >= AnvilTable.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(currentLevel), "An item at +9 cannot be struck again.");
            if (pity < 0)
                throw new ArgumentOutOfRangeException(nameof(pity));
            if (seals < 0 || seals > AnvilTable.SealsPerStrike)
                throw new ArgumentOutOfRangeException(nameof(seals));
            if (hammer && !HammerAllowed(currentLevel))
                throw new ArgumentOutOfRangeException(nameof(hammer), "A Master's Hammer serves strikes to +6 or below.");
        }
    }
}
