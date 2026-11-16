using System;
using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // One efsun line: a stat and its tier, 1..5. The value is the tier's (EfsunRule.ValueOf), never stored, so a retune
    // moves every saved line with it (docs/54 section 10).
    public readonly struct EfsunLine : IEquatable<EfsunLine>
    {
        public readonly GearStat Stat;
        public readonly int Tier;

        public EfsunLine(GearStat stat, int tier)
        {
            if (tier < 1 || tier > EfsunRule.TierCount)
                throw new ArgumentOutOfRangeException(nameof(tier));
            Stat = stat;
            Tier = tier;
        }

        // In hundredths: 150 is +1.5 % (or 1.5 points of crit or armour, 1.5 luck).
        public int Value => EfsunRule.ValueOf(Stat, Tier);

        public bool Equals(EfsunLine other) => Stat == other.Stat && Tier == other.Tier;
        public override bool Equals(object obj) => obj is EfsunLine other && Equals(other);
        public override int GetHashCode() => ((int)Stat * 31) ^ Tier;
    }

    // Efsun's rule (docs/54 section 6): how many lines an item has, how a line is drawn, what a tier is worth and what a
    // reroll costs. Draws come from a RunRandom stream the caller owns (the profile's anvil stream, or a run's loot
    // stream for a dropped item's first lines), so the same draw writes the same lines on every runtime.
    public static class EfsunRule
    {
        public const int TierCount = 5;
        public const int MaxLines = 5;
        // A forged weapon starts with a Rare's lines (docs/54 section 3.1).
        public const int WeaponLines = 2;

        // A tier's weight in a hundred: tier 1 40, tier 2 30, tier 3 18, tier 4 9, tier 5 3.
        private static readonly int[] TierWeights = { 40, 30, 18, 9, 3 };
        private static readonly int[] RerollGoldByRarity = { 60, 100, 160, 250 };

        // Each stat's value per tier, in hundredths (GearStat's order).
        private static readonly int[][] Values =
        {
            new[] { 100, 150, 200, 250, 300 },  // damage %
            new[] { 50, 100, 150, 200, 250 },   // attack speed %
            new[] { 40, 60, 80, 100, 120 },     // crit chance, points
            new[] { 100, 150, 200, 250, 300 },  // max health %
            new[] { 40, 60, 80, 100, 120 },     // armour (damage taken), points
            new[] { 200, 300, 400, 500, 600 },  // damage vs elites and bosses %
            new[] { 300, 450, 600, 750, 900 },  // burn damage %
            new[] { 100, 150, 200, 250, 300 },  // area %
            new[] { 100, 150, 200, 250, 300 },  // move speed %
            new[] { 400, 600, 800, 1000, 1200 }, // pickup radius %
            new[] { 200, 300, 400, 500, 600 },  // ability cooldown, % less
            new[] { 300, 450, 600, 750, 900 },  // healing received %
            new[] { 100, 200, 300, 400, 500 }   // luck
        };

        public static int ValueOf(GearStat stat, int tier)
        {
            int index = (int)stat;
            if (index < 0 || index >= Values.Length)
                throw new ArgumentOutOfRangeException(nameof(stat));
            if (tier < 1 || tier > TierCount)
                throw new ArgumentOutOfRangeException(nameof(tier));
            return Values[index][tier - 1];
        }

        // Common 1, Rare 2, Epic 3, Gold 4; a Binding Rune adds the fifth.
        public static int LinesFor(UpgradeRarity rarity) => Math.Min(MaxLines - 1, Math.Max(1, (int)rarity + 1));

        // A reroll's gold, by the item's rarity and never by its level (docs/54 section 6.2).
        public static int RerollGold(UpgradeRarity rarity)
        {
            int index = (int)rarity;
            if (index < 0 || index >= RerollGoldByRarity.Length)
                throw new ArgumentOutOfRangeException(nameof(rarity));
            return RerollGoldByRarity[index];
        }

        // A fresh set of count lines: distinct stats, none the item's main stat (Metin2's rule), each with a drawn tier.
        public static EfsunLine[] Roll(GearStat mainStat, int count, RunRandom random)
        {
            if (count < 0 || count > MaxLines)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            var lines = new EfsunLine[count];
            var taken = new List<GearStat>(count + 1) { mainStat };
            for (int i = 0; i < count; i++)
            {
                GearStat stat = DrawStat(taken, random);
                taken.Add(stat);
                lines[i] = new EfsunLine(stat, DrawTier(random));
            }
            return lines;
        }

        // One more line for an item that has fewer than five (a Binding Rune): a stat it does not carry yet.
        public static EfsunLine AddLine(IReadOnlyList<EfsunLine> lines, GearStat mainStat, RunRandom random)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));
            if (lines.Count >= MaxLines)
                throw new InvalidOperationException("An item carries five lines at most.");
            var taken = new List<GearStat>(lines.Count + 1) { mainStat };
            for (int i = 0; i < lines.Count; i++)
                taken.Add(lines[i].Stat);
            return new EfsunLine(DrawStat(taken, random), DrawTier(random));
        }

        private static GearStat DrawStat(List<GearStat> taken, RunRandom random)
        {
            int open = GearStatIds.Count - taken.Count;
            int pick = random.NextBelow(open);
            for (int i = 0; i < GearStatIds.Count; i++)
            {
                if (taken.Contains((GearStat)i))
                    continue;
                if (pick == 0)
                    return (GearStat)i;
                pick--;
            }
            throw new InvalidOperationException("No stat left to draw.");
        }

        private static int DrawTier(RunRandom random)
        {
            int draw = random.NextBelow(100);
            for (int tier = 0; tier < TierWeights.Length; tier++)
            {
                if (draw < TierWeights[tier])
                    return tier + 1;
                draw -= TierWeights[tier];
            }
            return TierCount;
        }
    }
}
