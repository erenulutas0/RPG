using System;
using System.Collections.Generic;
using System.Globalization;
using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // The profile's gear (docs/54): the bag of dropped items, each weapon's own upgrades, what is equipped, the floor
    // materials and boosters banked from runs, and the Anvil's random stream. It holds state and the rules of what may be
    // stored; the Anvil (Progression) and the run's bank decide what changes, through the internal methods, and the
    // profile raises Changed once per change. Restore repairs a damaged save instead of rejecting it, as PlayerProfile does.
    public sealed class ProfileGear
    {
        public const int BagCapacity = 30;
        // Armour, amulet and ring; the weapon slot is the forged weapon itself (docs/54 section 3.1).
        public const int EquipSlotCount = 3;
        public const string ItemKeyPrefix = "item:";
        public const string WeaponKeyPrefix = "weapon:";

        private readonly List<ItemInstance> _bag = new List<ItemInstance>();
        private readonly List<ItemInstance> _weapons = new List<ItemInstance>();
        private readonly int[] _equipped = new int[EquipSlotCount];
        private readonly int[] _materials = new int[GearCurrency.MaterialCount];
        private readonly int[] _boosters = new int[GearCurrency.BoosterCount];
        private readonly List<EfsunLine> _pendingLines = new List<EfsunLine>();

        public IReadOnlyList<ItemInstance> Bag => _bag;
        public IReadOnlyList<ItemInstance> Weapons => _weapons;
        public int NextItemUid { get; private set; } = 1;
        // 0 until the Anvil first draws; then a fixed seed for the profile's life (docs/54 section 4.3).
        public int AnvilSeed { get; private set; }
        public int AnvilDraws { get; private set; }
        // The first floor-1 boss a profile fells drops an armour (docs/54 section 3.2): given once.
        public bool FirstArmourGiven { get; private set; }
        // A reroll shown and not yet kept or discarded (docs/54 section 6.2); null when none.
        public ItemInstance PendingRerollPiece { get; private set; }
        public IReadOnlyList<EfsunLine> PendingRerollLines => _pendingLines;
        public bool IsBagFull => _bag.Count >= BagCapacity;

        public int Material(MaterialKind kind) => _materials[(int)kind];
        public int Booster(BoosterKind kind) => _boosters[(int)kind];

        public ItemInstance FindItem(int uid)
        {
            if (uid <= 0)
                return null;
            for (int i = 0; i < _bag.Count; i++)
            {
                if (_bag[i].Uid == uid)
                    return _bag[i];
            }
            return null;
        }

        // A weapon's upgrades; null until the Anvil first touches that weapon.
        public ItemInstance WeaponGear(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return null;
            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i].ItemId == weaponId)
                    return _weapons[i];
            }
            return null;
        }

        // The bag item equipped in an armour, amulet or ring slot; null for an empty slot or the weapon slot.
        public ItemInstance Equipped(GearSlot slot) => slot == GearSlot.Weapon ? null : FindItem(_equipped[(int)slot - 1]);

        public bool IsEquipped(ItemInstance item) =>
            item != null && !item.IsWeapon && item.Uid > 0 && _equipped[(int)item.Slot - 1] == item.Uid;

        public bool Owns(ItemInstance piece) => piece != null && (piece.IsWeapon ? _weapons.Contains(piece) : _bag.Contains(piece));

        // What a run with this weapon would wear: its gear (if any) and the three equipped items. For GearStats.Totals.
        public List<ItemInstance> EquippedPieces(string weaponId)
        {
            var pieces = new List<ItemInstance>(EquipSlotCount + 1);
            ItemInstance weapon = WeaponGear(weaponId);
            if (weapon != null)
                pieces.Add(weapon);
            for (GearSlot slot = GearSlot.Armour; slot <= GearSlot.Ring; slot++)
            {
                ItemInstance item = Equipped(slot);
                if (item != null)
                    pieces.Add(item);
            }
            return pieces;
        }

        // ---- Changes: the Anvil and the run's bank call these; the profile raises Changed once for each operation. ----

        internal void SeedAnvil(int seed)
        {
            if (seed == 0)
                throw new ArgumentOutOfRangeException(nameof(seed), "0 means not yet seeded.");
            AnvilSeed = seed;
        }

        // The next index of the anvil stream (RunRandom.Anvil), one per operation.
        internal int TakeDraw()
        {
            if (AnvilSeed == 0)
                throw new InvalidOperationException("Seed the anvil before drawing.");
            if (AnvilDraws == int.MaxValue)
                AnvilDraws = 0;
            return AnvilDraws++;
        }

        internal ItemInstance AddItem(string itemId, GearSlot slot, UpgradeRarity rarity, IEnumerable<EfsunLine> lines)
        {
            if (slot == GearSlot.Weapon)
                throw new ArgumentException("Weapons are forged, not dropped.", nameof(slot));
            var item = new ItemInstance(NextItemUid, itemId, slot, rarity, 0, 0, lines);
            NextItemUid = NextItemUid == int.MaxValue ? 1 : NextItemUid + 1;
            _bag.Add(item);
            return item;
        }

        internal void RemoveItem(ItemInstance item)
        {
            if (item == null || !_bag.Remove(item))
                return;
            if (_equipped[(int)item.Slot - 1] == item.Uid)
                _equipped[(int)item.Slot - 1] = 0;
            if (PendingRerollPiece == item)
                ClearPending();
        }

        internal ItemInstance AddWeaponGear(string weaponId, IEnumerable<EfsunLine> lines)
        {
            if (WeaponGear(weaponId) != null)
                throw new InvalidOperationException("The weapon's gear exists.");
            var gear = new ItemInstance(0, weaponId, GearSlot.Weapon, UpgradeRarity.Rare, 0, 0, lines);
            _weapons.Add(gear);
            return gear;
        }

        internal void Equip(ItemInstance item)
        {
            if (item == null || item.IsWeapon || !_bag.Contains(item))
                throw new ArgumentException("Only a bag item can be equipped.", nameof(item));
            _equipped[(int)item.Slot - 1] = item.Uid;
        }

        internal void AddMaterial(MaterialKind kind, int amount) => _materials[(int)kind] = Add(_materials[(int)kind], amount);
        internal void AddBooster(BoosterKind kind, int amount) => _boosters[(int)kind] = Add(_boosters[(int)kind], amount);

        internal void SpendMaterial(MaterialKind kind, int amount)
        {
            if (amount < 0 || _materials[(int)kind] < amount)
                throw new InvalidOperationException("Not enough material.");
            _materials[(int)kind] -= amount;
        }

        internal void SpendBooster(BoosterKind kind, int amount)
        {
            if (amount < 0 || _boosters[(int)kind] < amount)
                throw new InvalidOperationException("Not enough boosters.");
            _boosters[(int)kind] -= amount;
        }

        internal void SetPending(ItemInstance piece, IEnumerable<EfsunLine> lines)
        {
            PendingRerollPiece = piece ?? throw new ArgumentNullException(nameof(piece));
            _pendingLines.Clear();
            _pendingLines.AddRange(lines);
        }

        internal void ClearPending()
        {
            PendingRerollPiece = null;
            _pendingLines.Clear();
        }

        internal void MarkFirstArmourGiven() => FirstArmourGiven = true;

        private static int Add(int have, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            return (int)Math.Min(int.MaxValue, (long)have + amount);
        }

        // ---- The save. ----

        public GearSnapshot Snapshot()
        {
            var snapshot = new GearSnapshot
            {
                Items = new GearRecord[_bag.Count],
                Weapons = new GearRecord[_weapons.Count],
                EquippedUids = (int[])_equipped.Clone(),
                Materials = (int[])_materials.Clone(),
                Boosters = (int[])_boosters.Clone(),
                NextItemUid = NextItemUid,
                AnvilSeed = AnvilSeed,
                AnvilDraws = AnvilDraws,
                FirstArmourGiven = FirstArmourGiven,
                PendingRerollKey = PendingRerollPiece != null ? KeyOf(PendingRerollPiece) : null,
                PendingRerollLines = Records(_pendingLines)
            };
            for (int i = 0; i < _bag.Count; i++)
                snapshot.Items[i] = Record(_bag[i]);
            for (int i = 0; i < _weapons.Count; i++)
                snapshot.Weapons[i] = Record(_weapons[i]);
            return snapshot;
        }

        public static string KeyOf(ItemInstance piece) =>
            piece.IsWeapon ? WeaponKeyPrefix + piece.ItemId : ItemKeyPrefix + piece.Uid.ToString(CultureInfo.InvariantCulture);

        private static GearRecord Record(ItemInstance piece) => new GearRecord
        {
            Uid = piece.Uid,
            ItemId = piece.ItemId,
            Slot = (int)piece.Slot,
            Rarity = (int)piece.Rarity,
            Level = piece.Level,
            Pity = piece.Pity,
            Lines = Records(piece.Lines)
        };

        private static GearLineRecord[] Records(IReadOnlyList<EfsunLine> lines)
        {
            var records = new GearLineRecord[lines.Count];
            for (int i = 0; i < lines.Count; i++)
                records[i] = new GearLineRecord(GearStatIds.Id(lines[i].Stat), lines[i].Tier);
            return records;
        }

        // Rebuilds the gear from a save, repairing what is damaged: unknown stats, repeated stats and the main stat dropped
        // from lines, tiers clamped to 1..5, at most five lines; levels clamped to 0..9 and pity to the next strike's
        // ceiling; bag items with no id or a slot that is not armour, amulet or ring dropped; missing or repeated uids
        // renumbered; equipped uids that are not in the bag, or in the wrong slot, unequipped; repeated weapons dropped;
        // negative counts zeroed; a pending reroll that no longer resolves cleared. Never throws on a non-null snapshot.
        public static ProfileGear Restore(GearSnapshot snapshot)
        {
            var gear = new ProfileGear();
            if (snapshot == null)
                return gear;

            var needUid = new List<ItemInstance>();
            var uids = new HashSet<int>();
            int highest = 0;
            if (snapshot.Items != null)
            {
                foreach (GearRecord record in snapshot.Items)
                {
                    if (record == null || string.IsNullOrEmpty(record.ItemId) || record.Slot < (int)GearSlot.Armour ||
                        record.Slot > (int)GearSlot.Ring)
                        continue;
                    bool keepsUid = record.Uid > 0 && uids.Add(record.Uid);
                    ItemInstance item = Build(keepsUid ? record.Uid : int.MaxValue, record.ItemId, (GearSlot)record.Slot,
                        record.Rarity, record.Level, record.Pity, record.Lines);
                    gear._bag.Add(item);
                    if (keepsUid)
                        highest = Math.Max(highest, record.Uid);
                    else
                        needUid.Add(item);
                }
            }
            gear.NextItemUid = Math.Max(1, Math.Max(snapshot.NextItemUid, highest == int.MaxValue ? 1 : highest + 1));
            foreach (ItemInstance item in needUid)
            {
                while (uids.Contains(gear.NextItemUid))
                    gear.NextItemUid++;
                item.Uid = gear.NextItemUid;
                uids.Add(gear.NextItemUid);
                gear.NextItemUid++;
            }

            if (snapshot.Weapons != null)
            {
                foreach (GearRecord record in snapshot.Weapons)
                {
                    if (record == null || string.IsNullOrEmpty(record.ItemId) || gear.WeaponGear(record.ItemId) != null)
                        continue;
                    gear._weapons.Add(Build(0, record.ItemId, GearSlot.Weapon, (int)UpgradeRarity.Rare, record.Level, record.Pity,
                        record.Lines));
                }
            }

            for (int slot = 0; slot < EquipSlotCount; slot++)
            {
                int uid = snapshot.EquippedUids != null && slot < snapshot.EquippedUids.Length ? snapshot.EquippedUids[slot] : 0;
                ItemInstance item = gear.FindItem(uid);
                gear._equipped[slot] = item != null && (int)item.Slot == slot + 1 ? uid : 0;
            }
            CopyCounts(snapshot.Materials, gear._materials);
            CopyCounts(snapshot.Boosters, gear._boosters);
            gear.AnvilSeed = snapshot.AnvilSeed;
            gear.AnvilDraws = Math.Max(0, snapshot.AnvilDraws);
            gear.FirstArmourGiven = snapshot.FirstArmourGiven;

            ItemInstance pending = Resolve(gear, snapshot.PendingRerollKey);
            if (pending != null && snapshot.PendingRerollLines != null && snapshot.PendingRerollLines.Length > 0)
                gear.SetPending(pending, Lines(pending.MainStat, snapshot.PendingRerollLines));
            return gear;
        }

        private static ItemInstance Resolve(ProfileGear gear, string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            if (key.StartsWith(WeaponKeyPrefix, StringComparison.Ordinal))
                return gear.WeaponGear(key.Substring(WeaponKeyPrefix.Length));
            if (key.StartsWith(ItemKeyPrefix, StringComparison.Ordinal) &&
                int.TryParse(key.Substring(ItemKeyPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int uid))
                return gear.FindItem(uid);
            return null;
        }

        private static ItemInstance Build(int uid, string itemId, GearSlot slot, int rarity, int level, int pity, GearLineRecord[] lines)
        {
            var clampedRarity = (UpgradeRarity)Math.Max((int)UpgradeRarity.Common, Math.Min((int)UpgradeRarity.Gold, rarity));
            int clampedLevel = Math.Max(0, Math.Min(AnvilTable.MaxLevel, level));
            int ceiling = clampedLevel < AnvilTable.MaxLevel ? AnvilTable.CeilingFailures(clampedLevel + 1) : 0;
            int clampedPity = Math.Max(0, Math.Min(ceiling, pity));
            return new ItemInstance(uid, itemId, slot, clampedRarity, clampedLevel, clampedPity,
                Lines(GearStats.MainStatOf(slot), lines));
        }

        private static List<EfsunLine> Lines(GearStat mainStat, GearLineRecord[] records)
        {
            var lines = new List<EfsunLine>(EfsunRule.MaxLines);
            if (records == null)
                return lines;
            foreach (GearLineRecord record in records)
            {
                if (lines.Count >= EfsunRule.MaxLines)
                    break;
                if (!GearStatIds.TryParse(record.Stat, out GearStat stat) || stat == mainStat || lines.Exists(l => l.Stat == stat))
                    continue;
                lines.Add(new EfsunLine(stat, Math.Max(1, Math.Min(EfsunRule.TierCount, record.Tier))));
            }
            return lines;
        }

        private static void CopyCounts(int[] from, int[] to)
        {
            for (int i = 0; i < to.Length; i++)
                to[i] = from != null && i < from.Length ? Math.Max(0, from[i]) : 0;
        }
    }
}
