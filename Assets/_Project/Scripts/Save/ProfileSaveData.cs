using System;

namespace Cryptforge.Save
{
    // The on-disk profile schema. Field names are the JSON keys (06_TECH_ARCHITECTURE_UNITY), so they stay camelCase
    // and stable; any schema change bumps CurrentVersion and adds a step to ProfileMigration.
    [Serializable]
    public sealed class ProfileSaveData
    {
        // Version 2 added forged weapons; version 3 added heroes (docs/45).
        public const int CurrentVersion = 3;

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
    }
}
