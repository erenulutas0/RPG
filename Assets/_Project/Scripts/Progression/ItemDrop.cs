using System;
using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // Where a run's loot came from (docs/54 sections 3.2 and 5), as telemetry writes it.
    public enum LootSource
    {
        Boss = 0,
        Elite = 1,
        FloorClear = 2,
        WalkOut = 3,
        // The first floor-1 boss a profile fells: its guaranteed armour (docs/54 section 3.2).
        FirstBoss = 4
    }

    // An item a run dropped, before the profile gives it a uid: what it is, its rarity and its first lines (drawn from the
    // run's loot stream when it dropped), the floor and what dropped it.
    public sealed class ItemDrop
    {
        private readonly EfsunLine[] _lines;

        public string ItemId { get; }
        public string DisplayName { get; }
        public GearSlot Slot { get; }
        public UpgradeRarity Rarity { get; }
        public IReadOnlyList<EfsunLine> Lines => _lines;
        public int Floor { get; }
        public LootSource Source { get; }

        public ItemDrop(string itemId, string displayName, GearSlot slot, UpgradeRarity rarity, IEnumerable<EfsunLine> lines,
            int floor, LootSource source)
        {
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("An item id is required.", nameof(itemId));
            if (slot == GearSlot.Weapon || slot < GearSlot.Weapon || slot > GearSlot.Ring)
                throw new ArgumentOutOfRangeException(nameof(slot));
            ItemId = itemId;
            DisplayName = displayName ?? string.Empty;
            Slot = slot;
            Rarity = rarity;
            _lines = lines != null ? new List<EfsunLine>(lines).ToArray() : new EfsunLine[0];
            Floor = floor;
            Source = source;
        }
    }
}
