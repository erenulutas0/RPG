using System;

namespace Cryptforge.Progression
{
    // Immutable runtime snapshot of an UpgradeDefinition. Stack counts are tracked by reference identity.
    public sealed class UpgradeOption
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string DescriptionFormat { get; }
        public WeaponStat Stat { get; }
        public StatModifier Modifier { get; }
        public int MaxStacks { get; }
        // The id of a card that must hold at least one stack before this one is offered; null for none.
        public string RequiresId { get; }

        // Percent amounts are authored as fractions but read as whole percentages ("+25%").
        public float DescriptionValue =>
            Modifier.Operation == ModifierOperation.Percent ? Modifier.Amount * 100f : Modifier.Amount;

        public UpgradeOption(string id, string displayName, string descriptionFormat, WeaponStat stat,
            StatModifier modifier, int maxStacks, string requiresId = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Upgrade id is required.", nameof(id));
            if (requiresId != null && (requiresId.Length == 0 || requiresId == id))
                throw new ArgumentException("A card may require another card, never nothing and never itself.", nameof(requiresId));
            if (stat != WeaponStat.Damage && stat != WeaponStat.AttackSpeed)
                throw new ArgumentOutOfRangeException(nameof(stat));
            if (maxStacks < 1)
                throw new ArgumentOutOfRangeException(nameof(maxStacks));

            Id = id;
            DisplayName = displayName ?? string.Empty;
            DescriptionFormat = descriptionFormat ?? string.Empty;
            Stat = stat;
            Modifier = modifier;
            MaxStacks = maxStacks;
            RequiresId = requiresId;
        }
    }
}
