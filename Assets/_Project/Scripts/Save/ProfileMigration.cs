namespace Cryptforge.Save
{
    public static class ProfileMigration
    {
        // Returns the data in the current schema, or null when this build cannot read it: a missing version, or a
        // version written by a newer build, is never guessed at. Each case upgrades one version and falls through.
        public static ProfileSaveData Upgrade(ProfileSaveData data)
        {
            if (data == null)
                return null;

            switch (data.saveVersion)
            {
                case 1:
                    // Version 1 predates weapon unlocks: nothing is forged and the hero carries the starting weapon.
                    data.ownedWeaponIds = new string[0];
                    data.equippedWeaponId = string.Empty;
                    data.saveVersion = 2;
                    goto case 2;
                case 2:
                    // Version 2 predates heroes: only the ones open from the start, and the default chosen.
                    data.unlockedHeroIds = new string[0];
                    data.selectedHeroId = string.Empty;
                    data.saveVersion = 3;
                    goto case 3;
                case 3:
                    return data;
                default:
                    return null;
            }
        }
    }
}
