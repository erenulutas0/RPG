using System;

namespace Cryptforge.Progression
{
    // How often a level-up card is each kind of pick, once a run carries a loadout (docs/42, offers; the report's 40
    // section 9.2). Early the run is offered weapons and tomes it does not have yet, so a build forms; late it is offered
    // levels of what it has, so the build deepens. Pure and integer-only, like RarityTable, so the scene and the
    // simulation draw the same category from the same word of the same stream.
    //
    // A category with nothing in it - every weapon slot held, every carried weapon at its last level, three tomes held,
    // every tome full, every general card full - is closed, and the draw is renormalised over the ones still open: its
    // weight is not handed to any one neighbour, it simply leaves the sum.
    public static class OfferWeights
    {
        public const int CategoryCount = 5;

        // Early and late switch on the run's level rather than its clock, so the scene and the simulation agree without
        // sharing a timer. The report puts level 3-4 at the first minute and 11-13 at the fifth (40 section 9.1);
        // level 6 is about the second.
        public const int LateFromLevel = 6;

        // Weights in percent, early then late, in OfferCategory order. Each phase sums to 100, which a test holds.
        private static readonly int[] Early = { 10, 27, 28, 25, 10 };
        private static readonly int[] Late = { 10, 15, 35, 15, 25 };

        public static bool IsLate(int runLevel) => runLevel >= LateFromLevel;

        public static int Weight(OfferCategory category, int runLevel)
        {
            int index = (int)category;
            if (index < 0 || index >= CategoryCount)
                throw new ArgumentOutOfRangeException(nameof(category));
            return IsLate(runLevel) ? Late[index] : Early[index];
        }

        // The chance of a category at this level with only the open ones in the sum, exactly as Draw weighs it; 0 for a
        // closed one. For tests and for a view that wants to print odds; nothing in play reads it.
        public static double Probability(OfferCategory category, int runLevel, bool[] open)
        {
            int total = OpenWeight(runLevel, open);
            if (total == 0 || !open[(int)category])
                return 0.0;
            return (double)Weight(category, runLevel) / total;
        }

        public static bool AnyOpen(int runLevel, bool[] open) => OpenWeight(runLevel, open) > 0;

        // One word of the stream, always exactly one NextBelow, over the open categories in OfferCategory order. open is
        // indexed by OfferCategory; at least one entry must be true.
        public static OfferCategory Draw(RunRandom stream, int runLevel, bool[] open)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            int total = OpenWeight(runLevel, open);
            if (total == 0)
                throw new InvalidOperationException("Every category is closed; there is nothing to draw.");

            int roll = stream.NextBelow(total);
            for (int i = 0; i < CategoryCount; i++)
            {
                if (!open[i])
                    continue;
                int weight = Weight((OfferCategory)i, runLevel);
                if (roll < weight)
                    return (OfferCategory)i;
                roll -= weight;
            }
            // Unreachable: the roll is below the sum of the weights walked above.
            throw new InvalidOperationException("The category draw ran past its weights.");
        }

        private static int OpenWeight(int runLevel, bool[] open)
        {
            if (open == null)
                throw new ArgumentNullException(nameof(open));
            if (open.Length != CategoryCount)
                throw new ArgumentException("One flag per category.", nameof(open));

            int total = 0;
            for (int i = 0; i < CategoryCount; i++)
                if (open[i])
                    total += Weight((OfferCategory)i, runLevel);
            return total;
        }
    }
}
