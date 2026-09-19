using System;

namespace Cryptforge.Progression
{
    // How strong the offered copy of a card is. Three tiers, not Megabonk's five: a level-up shows two or three cards
    // on a phone, and a scale a player cannot tell apart at a glance is a label rather than a choice (docs/27, S2b).
    // Serialized by value where it is stored; append, never reorder.
    public enum UpgradeRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2
    }

    // The weights a rarity is drawn with, and what luck does to them. Luck never touches combat: it lowers how much of
    // the draw the common tier takes, which is the only thing it does anywhere in this game.
    public static class RarityTable
    {
        // Out of a hundred, before luck.
        public const int CommonWeight = 70;
        public const int RareWeight = 25;
        public const int EpicWeight = 5;

        // Luck of 1 halves the common tier's weight, luck of 3 quarters it; the other two keep theirs and so take a
        // larger share of what is left. It can never reach zero, so a common card is always possible.
        public static int CommonWeightAt(float luck)
        {
            if (float.IsNaN(luck) || luck < 0f)
                throw new ArgumentOutOfRangeException(nameof(luck));

            // Integer arithmetic once luck is quantised, so every runtime draws the same tier from the same weights.
            int steps = (int)(Math.Min(luck, 100f) * 100f);
            long weighted = (long)CommonWeight * 10000L / (10000L + steps);
            return (int)Math.Max(1L, weighted);
        }

        public static UpgradeRarity Draw(RunRandom stream, float luck)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            int common = CommonWeightAt(luck);
            int total = common + RareWeight + EpicWeight;
            int roll = stream.NextBelow(total);
            if (roll < common)
                return UpgradeRarity.Common;
            return roll < common + RareWeight ? UpgradeRarity.Rare : UpgradeRarity.Epic;
        }
    }
}
