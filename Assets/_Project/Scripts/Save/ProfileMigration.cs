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
                    return data;
                default:
                    return null;
            }
        }
    }
}
