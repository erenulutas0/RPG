using System;
using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // One piece of gear the profile owns (docs/54 section 3): a dropped armour, amulet or ring in the bag, or a weapon's own
    // upgrades (Slot Weapon, Uid 0, ItemId the weapon's id). Its basma level and pity belong to it (the lead, 2026-11-16:
    // item-bound basma); its lines are its efsun. The profile is what changes it (ProfileGear, Anvil); everything else reads.
    public sealed class ItemInstance
    {
        private readonly List<EfsunLine> _lines = new List<EfsunLine>(EfsunRule.MaxLines);

        public int Uid { get; internal set; }
        public string ItemId { get; }
        public GearSlot Slot { get; }
        public UpgradeRarity Rarity { get; }
        public int Level { get; internal set; }
        // Failures in a row at the current level (AnvilRule), reset by a success.
        public int Pity { get; internal set; }
        public IReadOnlyList<EfsunLine> Lines => _lines;
        public GearStat MainStat => GearStats.MainStatOf(Slot);
        public bool IsWeapon => Slot == GearSlot.Weapon;

        public ItemInstance(int uid, string itemId, GearSlot slot, UpgradeRarity rarity, int level, int pity,
            IEnumerable<EfsunLine> lines)
        {
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("An item id is required.", nameof(itemId));
            if (slot < GearSlot.Weapon || slot > GearSlot.Ring)
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (rarity < UpgradeRarity.Common || rarity > UpgradeRarity.Gold)
                throw new ArgumentOutOfRangeException(nameof(rarity));
            if (level < 0 || level > AnvilTable.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            if (pity < 0)
                throw new ArgumentOutOfRangeException(nameof(pity));
            if (uid < 0 || (slot == GearSlot.Weapon) != (uid == 0))
                throw new ArgumentOutOfRangeException(nameof(uid), "A weapon's gear has uid 0, a bag item a positive one.");

            Uid = uid;
            ItemId = itemId;
            Slot = slot;
            Rarity = rarity;
            Level = level;
            Pity = pity;
            SetLines(lines);
        }

        // Replaces the lines, which must be at most five, of distinct stats and none the main stat.
        internal void SetLines(IEnumerable<EfsunLine> lines)
        {
            var next = new List<EfsunLine>(EfsunRule.MaxLines);
            if (lines != null)
            {
                foreach (EfsunLine line in lines)
                {
                    if (line.Tier < 1 || line.Stat == MainStat || next.Exists(l => l.Stat == line.Stat))
                        throw new ArgumentException("Lines must be distinct stats other than the main stat.", nameof(lines));
                    next.Add(line);
                }
            }
            if (next.Count > EfsunRule.MaxLines)
                throw new ArgumentException("An item carries five lines at most.", nameof(lines));
            _lines.Clear();
            _lines.AddRange(next);
        }

        internal void AddLine(EfsunLine line)
        {
            var next = new List<EfsunLine>(_lines) { line };
            SetLines(next);
        }
    }
}
