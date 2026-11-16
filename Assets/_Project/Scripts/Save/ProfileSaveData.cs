using System;

namespace Cryptforge.Save
{
    // The on-disk profile schema. Field names are the JSON keys (06_TECH_ARCHITECTURE_UNITY), so they stay camelCase
    // and stable; any schema change bumps CurrentVersion and adds a step to ProfileMigration.
    [Serializable]
    public sealed class ProfileSaveData
    {
        // Version 2 added forged weapons; version 3 added heroes (docs/45); version 4 added gear (docs/54).
        public const int CurrentVersion = 4;

        public int saveVersion;
        // Increases with every save; loading prefers the readable file with the highest revision.
        public int revision;
        public int gold;
        public string[] ownedRelicIds;
        public string equippedRelicId;
        public int deepestFloorCleared;
        public string[] ownedWeaponIds;
        // Empty while the hero carries the starting weapon.
        public string equippedWeaponId;
        // Heroes unlocked beyond the ones open from the start, and the hero chosen for the next run (empty: the default).
        public string[] unlockedHeroIds;
        public string selectedHeroId;

        // Version 4 (docs/54 section 10): the bag, each weapon's upgrades, the equipped armour, amulet and ring (uids, 0 for
        // none), the floor materials and boosters, the anvil's stream, the first armour's gift and the difficulty tiers.
        public ItemSaveData[] items;
        public WeaponGearSaveData[] weaponGear;
        public int[] equippedItemUids;
        public int nextItemUid;
        public int emberShards;
        public int quicksilverBeads;
        public int gloamSpores;
        public int forgeSeals;
        public int mastersHammers;
        public int efsunScrolls;
        public int bindingRunes;
        public int anvilSeed;
        public int anvilDraws;
        public bool firstArmourGiven;
        public int highestTierCompleted;
        public int selectedTier;
        // A reroll shown and not yet kept or discarded: "item:<uid>" or "weapon:<id>" (empty for none), and its new lines.
        public string pendingRerollKey;
        public LineSaveData[] pendingRerollLines;
    }

    [Serializable]
    public sealed class ItemSaveData
    {
        public int uid;
        public string itemId;
        // GearSlot: 1 armour, 2 amulet, 3 ring.
        public int slot;
        // UpgradeRarity: 0 Common .. 3 Gold.
        public int rarity;
        public int level;
        public int pity;
        public LineSaveData[] lines;
    }

    [Serializable]
    public sealed class WeaponGearSaveData
    {
        public string weaponId;
        public int level;
        public int pity;
        public LineSaveData[] lines;
    }

    // A line's stat id (GearStatIds) and tier; its value is the tier's, so a retune moves every saved line.
    [Serializable]
    public sealed class LineSaveData
    {
        public string stat;
        public int tier;
    }
}
