using System;

namespace Cryptforge.Progression
{
    // Immutable runtime snapshot of an UpgradeDefinition. Stack counts are tracked by reference identity.
    public sealed class UpgradeOption
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string DescriptionFormat { get; }
        public UpgradeStat Stat { get; }
        // The card's magnitude at each tier. A card authored with one magnitude has the same at all three, which is how
        // every option built before rarity existed still behaves exactly as it did.
        public StatModifier Modifier { get; }
        private readonly StatModifier _rare;
        private readonly StatModifier _epic;
        public int MaxStacks { get; }
        // The id of a card that must hold at least one stack before this one is offered; null for none.
        public string RequiresId { get; }

        // Percent amounts are authored as fractions but read as whole percentages ("+25%").
        public float DescriptionValue =>
            Modifier.Operation == ModifierOperation.Percent ? Modifier.Amount * 100f : Modifier.Amount;

        public StatModifier ModifierFor(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common:
                    return Modifier;
                case UpgradeRarity.Rare:
                    return _rare;
                case UpgradeRarity.Epic:
                    return _epic;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity));
            }
        }

        // What the card's text shows at a tier, with percents read as whole percentages.
        public float DescriptionValueFor(UpgradeRarity rarity)
        {
            StatModifier modifier = ModifierFor(rarity);
            return modifier.Operation == ModifierOperation.Percent ? modifier.Amount * 100f : modifier.Amount;
        }

        public UpgradeOption(string id, string displayName, string descriptionFormat, UpgradeStat stat,
            StatModifier modifier, int maxStacks, string requiresId = null)
            : this(id, displayName, descriptionFormat, stat, modifier, modifier, modifier, maxStacks, requiresId)
        {
        }

        public UpgradeOption(string id, string displayName, string descriptionFormat, UpgradeStat stat,
            StatModifier common, StatModifier rare, StatModifier epic, int maxStacks, string requiresId = null)
        {
            StatModifier modifier = common;
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Upgrade id is required.", nameof(id));
            if (requiresId != null && (requiresId.Length == 0 || requiresId == id))
                throw new ArgumentException("A card may require another card, never nothing and never itself.", nameof(requiresId));
            if (!Enum.IsDefined(typeof(UpgradeStat), stat))
                throw new ArgumentOutOfRangeException(nameof(stat));
            if (maxStacks < 1)
                throw new ArgumentOutOfRangeException(nameof(maxStacks));

            Id = id;
            DisplayName = displayName ?? string.Empty;
            DescriptionFormat = descriptionFormat ?? string.Empty;
            Stat = stat;
            if (rare.Operation != common.Operation || epic.Operation != common.Operation)
                throw new ArgumentException("A card's tiers scale the same way.", nameof(rare));

            Modifier = common;
            _rare = rare;
            _epic = epic;
            MaxStacks = maxStacks;
            RequiresId = requiresId;
        }
    }
}
