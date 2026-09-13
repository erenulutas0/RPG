using System;

namespace Cryptforge.Progression
{
    // A hero weapon as the Relic Forge sells it; combat stats stay in its definition.
    public sealed class WeaponOption
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Price { get; }

        public WeaponOption(string id, string displayName, string description, int price)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Weapon id is required.", nameof(id));
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price));

            Id = id;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Price = price;
        }
    }
}
