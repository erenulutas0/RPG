using System;

namespace Cryptforge.Save
{
    // The on-disk profile schema. Field names are the JSON keys (06_TECH_ARCHITECTURE_UNITY), so they stay camelCase
    // and stable; any schema change bumps CurrentVersion and adds a step to ProfileMigration.
    [Serializable]
    public sealed class ProfileSaveData
    {
        public const int CurrentVersion = 1;

        public int saveVersion;
        // Increases with every save; loading prefers the readable file with the highest revision.
        public int revision;
        public int gold;
        public string[] ownedRelicIds;
        public string equippedRelicId;
        public int deepestFloorCleared;
    }
}
