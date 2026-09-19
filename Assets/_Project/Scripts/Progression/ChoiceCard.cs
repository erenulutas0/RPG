namespace Cryptforge.Progression
{
    public readonly struct ChoiceCard
    {
        public string Name { get; }
        public string Description { get; }

        // The tier this card is offered at, for a view that wants to show it. Common for every choice that has no
        // tiers of its own, which is every forge and checkpoint card.
        public UpgradeRarity Rarity { get; }

        public ChoiceCard(string name, string description, UpgradeRarity rarity = UpgradeRarity.Common)
        {
            Name = name;
            Description = description;
            Rarity = rarity;
        }
    }
}
