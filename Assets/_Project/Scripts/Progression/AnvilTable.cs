using System;

namespace Cryptforge.Progression
{
    // Basma's numbers (docs/54 section 4.1, the lead's odds and decisions of 2026-11-16), by the level a strike aims at:
    // the strike to +L. A failure keeps the item and its level and spends what the strike cost; each failure adds the
    // level's pity to the next strike on the same item until a success resets it, and after the ceiling's count of
    // failures in a row the next strike cannot fail. Tools/Economy/gear_economy.py carries the same tables; AnvilRuleTests
    // pins both to the model's numbers.
    public static class AnvilTable
    {
        public const int MaxLevel = 9;
        // A Forge Seal's points on one strike, and how many one strike may take.
        public const int SealBonusPercent = 10;
        public const int SealsPerStrike = 1;
        // A Master's Hammer holds a strike to this level or below.
        public const int HammerMaxLevel = 6;
        // Inheritance (docs/54 section 3.4): this share of the gold the level is expected to cost.
        public const int InheritPercent = 10;

        // Index 0 is the strike to +1, index 8 the strike to +9.
        private static readonly int[] BaseChance = { 100, 100, 100, 90, 75, 60, 45, 30, 20 };
        private static readonly int[] PityStep = { 0, 0, 0, 5, 5, 5, 4, 3, 2 };
        private static readonly int[] Ceiling = { 0, 0, 0, 2, 3, 4, 6, 8, 10 };
        private static readonly int[] Gold = { 50, 80, 120, 180, 250, 400, 700, 1300, 2400 };
        private static readonly int[] MaterialAmount = { 0, 0, 0, 2, 3, 4, 3, 4, 6 };
        private static readonly MaterialKind[] Material =
        {
            MaterialKind.EmberShard, MaterialKind.EmberShard, MaterialKind.EmberShard, MaterialKind.EmberShard,
            MaterialKind.EmberShard, MaterialKind.EmberShard, MaterialKind.QuicksilverBead, MaterialKind.GloamSpore,
            MaterialKind.GloamSpore
        };

        // Melting an item at the Anvil (docs/54 section 3.3), by rarity: gold and Ember Shards back.
        private static readonly int[] MeltGoldByRarity = { 20, 40, 80, 150 };
        private static readonly int[] MeltEmberByRarity = { 1, 2, 3, 4 };

        public static int MeltGold(UpgradeRarity rarity) => MeltGoldByRarity[RarityIndex(rarity)];
        public static int MeltEmber(UpgradeRarity rarity) => MeltEmberByRarity[RarityIndex(rarity)];

        private static int RarityIndex(UpgradeRarity rarity)
        {
            int index = (int)rarity;
            if (index < 0 || index >= MeltGoldByRarity.Length)
                throw new ArgumentOutOfRangeException(nameof(rarity));
            return index;
        }

        public static int BaseChancePercent(int targetLevel) => BaseChance[Index(targetLevel)];
        public static int PityStepPercent(int targetLevel) => PityStep[Index(targetLevel)];
        // Failures in a row after which the next strike holds; 0 where every strike already holds.
        public static int CeilingFailures(int targetLevel) => Ceiling[Index(targetLevel)];
        public static int GoldCost(int targetLevel) => Gold[Index(targetLevel)];

        // The floor material a strike spends; false for +1..+3, which spend gold alone.
        public static bool TryMaterialCost(int targetLevel, out MaterialKind kind, out int amount)
        {
            int index = Index(targetLevel);
            kind = Material[index];
            amount = MaterialAmount[index];
            return amount > 0;
        }

        private static int Index(int targetLevel)
        {
            if (targetLevel < 1 || targetLevel > MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(targetLevel), "A strike aims at +1..+9.");
            return targetLevel - 1;
        }
    }
}
