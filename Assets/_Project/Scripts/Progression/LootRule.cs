using System;
using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // One drop moment's loot: the material, the boosters and the item, any of which may be none.
    public sealed class LootDrop
    {
        private readonly int[] _boosters = new int[GearCurrency.BoosterCount];

        public LootSource Source { get; }
        public int Floor { get; }
        public bool HasMaterial { get; internal set; }
        public MaterialKind Material { get; internal set; }
        public int MaterialAmount { get; internal set; }
        public ItemDrop Item { get; internal set; }

        public LootDrop(LootSource source, int floor)
        {
            Source = source;
            Floor = floor;
        }

        public int Booster(BoosterKind kind) => _boosters[(int)kind];
        internal void AddBooster(BoosterKind kind) => _boosters[(int)kind]++;

        public bool IsEmpty
        {
            get
            {
                if ((HasMaterial && MaterialAmount > 0) || Item != null)
                    return false;
                for (int i = 0; i < _boosters.Length; i++)
                    if (_boosters[i] > 0)
                        return false;
                return true;
            }
        }
    }

    // What a run drops (docs/54 sections 3.2, 4.2 and 5, Forged tier): a floor clear its floor's material and a chance at
    // an Efsun Scroll; an elite leader one material, a chance at a Seal and at an item; a gate boss chances at a Seal, a
    // Hammer, a Rune and an item; the finale's walk-out a chance at a Rune and at an item; and the first floor-1 boss a
    // profile fells an armour for certain. Rates in parts of 10,000. Every draw is from the run's loot stream
    // (RunRandom.Loot), one index per drop moment, so loot never reads a word of the offers', chests' or crits' streams.
    // On a difficulty tier above Forged (docs/54 sections 8 and 21) every chance is the tier's reward times its Forged one
    // (DifficultyTiers.Chance), a material's amount likewise (its fraction made whole by one word drawn last, so the
    // moment's other draws stay where they were), and an item's rarity is drawn at the tier's item luck; Forged draws
    // exactly the words it always drew.
    public static class LootRule
    {
        public const int Parts = 10000;
        public const int MaterialPerClear = 2;
        public const int MaterialPerElite = 1;
        public const int SealPerElite = 800;
        public const int SealPerBoss = 400;
        public const int HammerPerBoss = 400;
        public const int ScrollPerClear = 2500;
        public const int RunePerBoss = 200;
        public const int RunePerWalkOut = 300;
        public const int ItemPerBoss = 2500;
        public const int ItemPerElite = 500;
        public const int ItemPerWalkOut = 6000;
        // The deepest floor that drops its own items; the finale drops this floor's.
        public const int DeepestItemFloor = 3;

        // The loot of one moment. items is the run's item list (EconomyConfig's), giftArmour that this is the first floor-1
        // boss the profile fells (the gift replaces that boss's item chance with a certain armour).
        public static LootDrop Roll(LootSource source, int floor, int seed, int index, IReadOnlyList<ItemTemplate> items,
            bool giftArmour = false, DifficultyTier tier = DifficultyTier.Forged)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            var drop = new LootDrop(giftArmour ? LootSource.FirstBoss : source, floor);
            RunRandom random = RunRandom.Stream(seed, RunRandom.Loot, index);
            switch (source)
            {
                case LootSource.FloorClear:
                    if (GearCurrency.TryMaterialOfFloor(floor, out MaterialKind cleared))
                        SetMaterial(drop, cleared, MaterialPerClear);
                    if (Chance(random, ScrollPerClear, tier))
                        drop.AddBooster(BoosterKind.EfsunScroll);
                    break;
                case LootSource.Elite:
                    if (GearCurrency.TryMaterialOfFloor(floor, out MaterialKind elite))
                        SetMaterial(drop, elite, MaterialPerElite);
                    if (Chance(random, SealPerElite, tier))
                        drop.AddBooster(BoosterKind.ForgeSeal);
                    if (Chance(random, ItemPerElite))
                        drop.Item = RollItem(random, items, floor, source, null, tier);
                    break;
                case LootSource.Boss:
                case LootSource.FirstBoss:
                    if (Chance(random, SealPerBoss, tier))
                        drop.AddBooster(BoosterKind.ForgeSeal);
                    if (Chance(random, HammerPerBoss, tier))
                        drop.AddBooster(BoosterKind.MastersHammer);
                    if (Chance(random, RunePerBoss, tier))
                        drop.AddBooster(BoosterKind.BindingRune);
                    // The word is drawn either way, so a gift never moves the draws after it.
                    bool item = Chance(random, ItemPerBoss);
                    if (giftArmour)
                        drop.Item = RollItem(random, items, floor, LootSource.FirstBoss, GearSlot.Armour, tier);
                    else if (item)
                        drop.Item = RollItem(random, items, floor, source, null, tier);
                    break;
                case LootSource.WalkOut:
                    if (Chance(random, RunePerWalkOut, tier))
                        drop.AddBooster(BoosterKind.BindingRune);
                    if (Chance(random, ItemPerWalkOut))
                        drop.Item = RollItem(random, items, floor, source, null, tier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source));
            }
            // The tier's share of the material, last, so a fraction's word moves no draw before it.
            if (drop.HasMaterial && tier != DifficultyTier.Forged)
                drop.MaterialAmount = DifficultyTiers.Amount(drop.MaterialAmount, tier, random);
            return drop;
        }

        // Whether a booster dropped on the floor a death ended is kept (the gold's rule, docs/54 section 5): a coin from the
        // loot stream past every drop moment's index, one per booster in the order they dropped. lossParts of 10,000 lose.
        public static bool KeepsOnDeath(int seed, int coin, int lossParts)
        {
            if (coin < 0)
                throw new ArgumentOutOfRangeException(nameof(coin));
            return RunRandom.Stream(seed, RunRandom.Loot, SettlementIndex + coin).NextBelow(Parts) >= lossParts;
        }

        // Settlement coins start here, far past any run's drop moments.
        public const int SettlementIndex = 1000000;

        private static void SetMaterial(LootDrop drop, MaterialKind kind, int amount)
        {
            drop.HasMaterial = true;
            drop.Material = kind;
            drop.MaterialAmount = amount;
        }

        // An item's chance is the same on every tier (the tier pays in item luck); a booster's is the tier's reward times it.
        private static bool Chance(RunRandom random, int parts) => random.NextBelow(Parts) < parts;

        private static bool Chance(RunRandom random, int parts, DifficultyTier tier) =>
            random.NextBelow(Parts) < DifficultyTiers.Chance(parts, tier);

        // An item of the slot (drawn evenly from armour, amulet and ring unless given) for the floor (the finale's is the
        // third floor's), its rarity from RarityTable at the tier's item luck (none on Forged) and its lines from the same
        // stream. Null when the run's list has no item for that slot.
        private static ItemDrop RollItem(RunRandom random, IReadOnlyList<ItemTemplate> items, int floor, LootSource source, GearSlot? slot,
            DifficultyTier tier)
        {
            GearSlot chosen = slot ?? (GearSlot)(1 + random.NextBelow(3));
            UpgradeRarity rarity = RarityTable.Draw(random, DifficultyTiers.ItemLuck(tier));
            EfsunLine[] lines = EfsunRule.Roll(GearStats.MainStatOf(chosen), EfsunRule.LinesFor(rarity), random);
            ItemTemplate template = Find(items, chosen, Math.Max(1, Math.Min(DeepestItemFloor, floor)));
            return template == null ? null
                : new ItemDrop(template.Id, template.DisplayName, template.Slot, rarity, lines, floor, source);
        }

        private static ItemTemplate Find(IReadOnlyList<ItemTemplate> items, GearSlot slot, int floor)
        {
            if (items == null)
                return null;
            ItemTemplate any = null;
            for (int i = 0; i < items.Count; i++)
            {
                ItemTemplate item = items[i];
                if (item == null || item.Slot != slot)
                    continue;
                if (item.Floor == floor)
                    return item;
                if (any == null || Math.Abs(item.Floor - floor) < Math.Abs(any.Floor - floor))
                    any = item;
            }
            return any;
        }
    }
}
