using Cryptforge.Core;
using Cryptforge.Progression;

namespace Cryptforge.Save
{
    // The gear between its JSON fields (ProfileSaveData version 4) and the plain GearSnapshot ProfileGear restores from and
    // writes to. Copies values only; ProfileGear.Restore does the repairing, so a damaged field here is never fatal.
    public static class ProfileGearSave
    {
        public static GearSnapshot ToSnapshot(ProfileSaveData data)
        {
            var snapshot = new GearSnapshot();
            if (data == null)
                return snapshot;
            snapshot.Items = new GearRecord[data.items?.Length ?? 0];
            for (int i = 0; i < snapshot.Items.Length; i++)
            {
                ItemSaveData item = data.items[i];
                snapshot.Items[i] = item == null ? null : new GearRecord
                {
                    Uid = item.uid,
                    ItemId = item.itemId,
                    Slot = item.slot,
                    Rarity = item.rarity,
                    Level = item.level,
                    Pity = item.pity,
                    Lines = Lines(item.lines)
                };
            }
            snapshot.Weapons = new GearRecord[data.weaponGear?.Length ?? 0];
            for (int i = 0; i < snapshot.Weapons.Length; i++)
            {
                WeaponGearSaveData weapon = data.weaponGear[i];
                snapshot.Weapons[i] = weapon == null ? null : new GearRecord
                {
                    ItemId = weapon.weaponId,
                    Slot = (int)GearSlot.Weapon,
                    Rarity = (int)UpgradeRarity.Rare,
                    Level = weapon.level,
                    Pity = weapon.pity,
                    Lines = Lines(weapon.lines)
                };
            }
            snapshot.EquippedUids = data.equippedItemUids ?? new int[ProfileGear.EquipSlotCount];
            snapshot.Materials = new[] { data.emberShards, data.quicksilverBeads, data.gloamSpores };
            snapshot.Boosters = new[] { data.forgeSeals, data.mastersHammers, data.efsunScrolls, data.bindingRunes };
            snapshot.NextItemUid = data.nextItemUid;
            snapshot.AnvilSeed = data.anvilSeed;
            snapshot.AnvilDraws = data.anvilDraws;
            snapshot.FirstArmourGiven = data.firstArmourGiven;
            snapshot.HighestTierCompleted = data.highestTierCompleted;
            snapshot.SelectedTier = data.selectedTier;
            snapshot.PendingRerollKey = data.pendingRerollKey;
            snapshot.PendingRerollLines = Lines(data.pendingRerollLines);
            return snapshot;
        }

        public static void Write(GearSnapshot snapshot, ProfileSaveData data)
        {
            data.items = new ItemSaveData[snapshot.Items.Length];
            for (int i = 0; i < data.items.Length; i++)
            {
                GearRecord record = snapshot.Items[i];
                data.items[i] = new ItemSaveData
                {
                    uid = record.Uid,
                    itemId = record.ItemId,
                    slot = record.Slot,
                    rarity = record.Rarity,
                    level = record.Level,
                    pity = record.Pity,
                    lines = Lines(record.Lines)
                };
            }
            data.weaponGear = new WeaponGearSaveData[snapshot.Weapons.Length];
            for (int i = 0; i < data.weaponGear.Length; i++)
            {
                GearRecord record = snapshot.Weapons[i];
                data.weaponGear[i] = new WeaponGearSaveData
                {
                    weaponId = record.ItemId,
                    level = record.Level,
                    pity = record.Pity,
                    lines = Lines(record.Lines)
                };
            }
            data.equippedItemUids = (int[])snapshot.EquippedUids.Clone();
            data.nextItemUid = snapshot.NextItemUid;
            data.emberShards = snapshot.Materials[(int)MaterialKind.EmberShard];
            data.quicksilverBeads = snapshot.Materials[(int)MaterialKind.QuicksilverBead];
            data.gloamSpores = snapshot.Materials[(int)MaterialKind.GloamSpore];
            data.forgeSeals = snapshot.Boosters[(int)BoosterKind.ForgeSeal];
            data.mastersHammers = snapshot.Boosters[(int)BoosterKind.MastersHammer];
            data.efsunScrolls = snapshot.Boosters[(int)BoosterKind.EfsunScroll];
            data.bindingRunes = snapshot.Boosters[(int)BoosterKind.BindingRune];
            data.anvilSeed = snapshot.AnvilSeed;
            data.anvilDraws = snapshot.AnvilDraws;
            data.firstArmourGiven = snapshot.FirstArmourGiven;
            data.highestTierCompleted = snapshot.HighestTierCompleted;
            data.selectedTier = snapshot.SelectedTier;
            data.pendingRerollKey = snapshot.PendingRerollKey ?? string.Empty;
            data.pendingRerollLines = Lines(snapshot.PendingRerollLines);
        }

        private static GearLineRecord[] Lines(LineSaveData[] lines)
        {
            var records = new GearLineRecord[lines?.Length ?? 0];
            for (int i = 0; i < records.Length; i++)
                records[i] = lines[i] == null ? default : new GearLineRecord(lines[i].stat, lines[i].tier);
            return records;
        }

        private static LineSaveData[] Lines(GearLineRecord[] lines)
        {
            var saved = new LineSaveData[lines?.Length ?? 0];
            for (int i = 0; i < saved.Length; i++)
                saved[i] = new LineSaveData { stat = lines[i].Stat, tier = lines[i].Tier };
            return saved;
        }
    }
}
