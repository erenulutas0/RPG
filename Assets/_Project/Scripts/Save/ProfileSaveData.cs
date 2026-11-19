using System;

namespace Cryptforge.Save
{
    // The on-disk profile schema. Field names are the JSON keys (06_TECH_ARCHITECTURE_UNITY), so they stay camelCase
    // and stable; any schema change bumps CurrentVersion and adds a step to ProfileMigration.
    [Serializable]
    public sealed class ProfileSaveData
    {
        // Version 2 added forged weapons; version 3 added heroes (docs/45); version 4 added gear (docs/54); version 5 added
        // the difficulty tiers' records (docs/54 section 21); version 6 added the heroes' forged skills (docs/57 phase 4).
        public const int CurrentVersion = 6;

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
        // The tiers (version 4 stored these two ahead of the tiers' build): highestTierCompleted counts the tiers completed
        // in order (0 none, 1 Forged, 2 Tempered, ...) - from version 5 written from tierProgress for a reader's eye and
        // read only by the migration from 4; selectedTier is the tier chosen for the next run (DifficultyTier: 0 Forged).
        public int highestTierCompleted;
        public int selectedTier;
        // A reroll shown and not yet kept or discarded: "item:<uid>" or "weapon:<id>" (empty for none), and its new lines.
        public string pendingRerollKey;
        public LineSaveData[] pendingRerollLines;

        // Version 5 (docs/54 section 21): each difficulty tier's record by its id.
        public TierProgressSaveData[] tierProgress;

        // Version 6 (docs/57 phase 4): each hero's forged skills 2 and 3; a hero with none forged is not written.
        public HeroSkillsSaveData[] heroSkills;
    }

    // A hero's forged skills: its id ("hero_vanguard", "hero_cinder_arcanist") and whether the Forge has forged its skill 2
    // and its skill 3 (docs/57 section 12: 300 and 500 gold, once floors 2 and 3 have been cleared).
    [Serializable]
    public sealed class HeroSkillsSaveData
    {
        public string hero;
        public bool skillTwo;
        public bool skillThree;
    }

    // A difficulty tier's record: its id ("forged", "tempered", "inferno", "abyssal"), the deepest floor a run on it has
    // cleared, and whether a run on it walked out of the Descent (which opens the tier above it).
    [Serializable]
    public sealed class TierProgressSaveData
    {
        public string tier;
        public int deepestFloorCleared;
        public bool completed;
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
