using System;

namespace Cryptforge.Progression
{
    // What an ItemDefinition authors, without Unity: the item's id, its name, the slot it fills and the floor that drops it
    // (docs/54 section 3.2). A dropped item is an ItemInstance of a template, with a rarity and lines of its own.
    public sealed class ItemTemplate
    {
        public string Id { get; }
        public string DisplayName { get; }
        public GearSlot Slot { get; }
        // The Descent's floor (1-3) this item drops on; the finale drops the third floor's.
        public int Floor { get; }

        public ItemTemplate(string id, string displayName, GearSlot slot, int floor)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("An item id is required.", nameof(id));
            if (slot == GearSlot.Weapon || slot < GearSlot.Weapon || slot > GearSlot.Ring)
                throw new ArgumentOutOfRangeException(nameof(slot), "Weapons are forged, not dropped.");
            if (floor < 1 || floor > 3)
                throw new ArgumentOutOfRangeException(nameof(floor));
            Id = id;
            DisplayName = displayName ?? string.Empty;
            Slot = slot;
            Floor = floor;
        }
    }
}
