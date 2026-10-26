using System;

namespace Cryptforge.Progression
{
    // How strong the offered copy of a card is: the owner's four colours, low to high green, orange, red and gold
    // (docs/27 decision 12, docs/40 section 7.1). Still not Megabonk's five: a level-up shows two or three cards on a
    // phone, and a scale a player cannot tell apart at a glance is a label rather than a choice.
    //
    // The first three keep the names they shipped with and gold is appended, rather than all four being renamed: the
    // value is what is serialized, and the name is what telemetry writes (upgrade_offered's rarities, upgrade_selected's
    // rarity, chest_opened's book_rarity, with "Common" spelled out as a literal for a pick without an offer), so a
    // rename would split every report into before and after for no gain in play. Common is green, Rare orange, Epic red.
    // Append, never reorder.
    public enum UpgradeRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Gold = 3
    }

    // The odds a rarity is drawn with, what luck does to them, and how much stronger each tier is (docs/40 section 7).
    // Luck never touches combat: it moves weight from green towards gold, which is the only thing it does anywhere in
    // this game.
    //
    //   w_i(L) = p0_i * (1 + L/25)^(0.4 i),  p0 = [0.60, 0.25, 0.12, 0.03],  P_i = w_i / sum w
    //   L_eff  = L                         below 50
    //          = 50 + 0.25 * (L - 50)      above it, so the fiftieth point of luck is the last one worth a whole point
    //
    // Every number between the luck that comes in and the tier that comes out is an integer. The power is fractional,
    // and the obvious ways of taking one - Math.Pow, or exp and log - are library calls whose last bit is the
    // runtime's business: the Editor's Mono, the pure .NET runner and the phone's IL2CPP need not agree, and a weight
    // one ulp apart moves a boundary that a seeded draw can land on. So (1 + L/25)^0.4 is found as what it is, the fifth
    // root of (1 + L/25)^2, by bisection over fixed-point integers, and tier i takes that root to the power i. The same
    // seed then draws the same tier on every runtime and in every assembly, inlined or not, which is the property the
    // simulation's parity rests on (RunRandom, and the movement budget's lesson in docs/21).
    public static class RarityTable
    {
        public const int TierCount = 4;

        // The odds at no luck, in basis points: 60 %, 25 %, 12 %, 3 %.
        public const int GreenBasisPoints = 6000;
        public const int OrangeBasisPoints = 2500;
        public const int RedBasisPoints = 1200;
        public const int GoldBasisPoints = 300;

        // Luck past the soft cap counts a quarter (docs/40 section 7.5).
        public const float SoftCapLuck = 50f;
        public const float SoftCapSlope = 0.25f;

        // Luck is clamped here before anything else. No card stacks within reach of it - Fortune at gold three times is
        // 36 - and the clamp is what keeps the fixed-point arithmetic below inside a long: at 1000 the effective luck is
        // 287.5 and the root bisected for is under 3.
        public const float MaxLuck = 1000f;

        // How much stronger a card is at each tier than at green, for a card authored from one magnitude
        // (docs/40 sections 6.1 and 7.1). The authored assets carry these as numbers of their own (UpgradeDefinition),
        // so one card can still differ from the rule; this is the rule they were written from.
        public static float Multiplier(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common:
                    return 1f;
                case UpgradeRarity.Rare:
                    return 1.25f;
                case UpgradeRarity.Epic:
                    return 1.5f;
                case UpgradeRarity.Gold:
                    return 2f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity));
            }
        }

        // The luck the formula sees, for whoever wants to show it. The draw itself uses the integer form of the same
        // rule (EffectiveLuckQuarterCents), which this matches to the hundredth of a point.
        public static float EffectiveLuck(float luck)
        {
            float clamped = Clamp(luck);
            return clamped <= SoftCapLuck ? clamped : SoftCapLuck + SoftCapSlope * (clamped - SoftCapLuck);
        }

        // The chance of a tier at this luck, exactly as Draw weighs it. For tests and for a view that wants to print
        // odds; nothing in play reads it.
        public static double Probability(int tier, float luck)
        {
            if (tier < 0 || tier >= TierCount)
                throw new ArgumentOutOfRangeException(nameof(tier));

            Weights(luck, out int green, out int orange, out int red, out int gold);
            int weight = tier == 0 ? green : tier == 1 ? orange : tier == 2 ? red : gold;
            return (double)weight / ((long)green + orange + red + gold);
        }

        // One word of the stream, always exactly one NextBelow, so a change to the odds can never change how many words
        // the cards after it in the same offer see.
        public static UpgradeRarity Draw(RunRandom stream, float luck)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Weights(luck, out int green, out int orange, out int red, out int gold);
            int roll = stream.NextBelow(green + orange + red + gold);
            if (roll < green)
                return UpgradeRarity.Common;
            roll -= green;
            if (roll < orange)
                return UpgradeRarity.Rare;
            roll -= orange;
            return roll < red ? UpgradeRarity.Epic : UpgradeRarity.Gold;
        }

        // Fixed point with 24 fractional bits. The largest value ever multiplied is under 3 * One, so a product of two
        // fits a long with room to spare.
        private const int FractionBits = 24;
        private const long One = 1L << FractionBits;
        // The weights are shifted down this far so their sum fits an int for NextBelow: at MaxLuck the sum is under
        // 2^29, and at no luck gold alone still holds about five million parts, far finer than any odds a test can see.
        private const int WeightShift = 10;

        // The four integer weights at this luck. Draw and Probability both read them, so what a test measures is what a
        // run rolls.
        private static void Weights(float luck, out int green, out int orange, out int red, out int gold)
        {
            // (1 + L/25) in fixed point. L/25 = quarterCents / 10000, since a quarter-cent is 1/400 of a point.
            long x = One + EffectiveLuckQuarterCents(luck) * One / 10000L;
            long root = FifthRootOfSquare(x);
            long root2 = (root * root) >> FractionBits;
            long root3 = (root2 * root) >> FractionBits;

            green = (int)((GreenBasisPoints * One) >> WeightShift);
            orange = (int)((OrangeBasisPoints * root) >> WeightShift);
            red = (int)((RedBasisPoints * root2) >> WeightShift);
            gold = (int)((GoldBasisPoints * root3) >> WeightShift);
        }

        // Effective luck in quarter-hundredths of a point, exact: luck is first rounded to the hundredth, and the soft
        // cap's quarter is then a whole number of these units. The rounding is done in double, where a float's 24 bits
        // times 100 and plus a half are exact, so no runtime's choice of precision can move it.
        private static long EffectiveLuckQuarterCents(float luck)
        {
            long cents = (long)((double)Clamp(luck) * 100.0 + 0.5);
            const long capCents = (long)(SoftCapLuck * 100f);
            return cents <= capCents ? cents * 4L : capCents * 4L + (cents - capCents);
        }

        // The largest r with r^5 <= x^2, so r is x^0.4 to within one part in 2^24. Every step truncates, which keeps
        // Pow5 non-decreasing in r and so the bisection exact: the answer depends only on x.
        private static long FifthRootOfSquare(long x)
        {
            long target = (x * x) >> FractionBits;
            long low = One;            // x >= 1, so the root is at least 1, and Pow5(One) == One <= target.
            long high = 3L * One + 1L; // MaxLuck keeps the root under 3; exclusive.
            while (high - low > 1L)
            {
                long mid = low + ((high - low) >> 1);
                if (Pow5(mid) <= target)
                    low = mid;
                else
                    high = mid;
            }
            return low;
        }

        private static long Pow5(long r)
        {
            long p = (r * r) >> FractionBits;
            p = (p * r) >> FractionBits;
            p = (p * r) >> FractionBits;
            return (p * r) >> FractionBits;
        }

        private static float Clamp(float luck)
        {
            if (float.IsNaN(luck) || luck < 0f)
                throw new ArgumentOutOfRangeException(nameof(luck));
            return Math.Min(luck, MaxLuck);
        }
    }
}
