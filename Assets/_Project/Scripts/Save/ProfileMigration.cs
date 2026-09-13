namespace Cryptforge.Save
{
    public static class ProfileMigration
    {
        // Returns the data in the current schema, or null when this build cannot read it: a missing version, or a
        // version written by a newer build, is never guessed at. Add one case per schema change and fall through.
        public static ProfileSaveData Upgrade(ProfileSaveData data)
        {
            if (data == null)
                return null;

            switch (data.saveVersion)
            {
                case ProfileSaveData.CurrentVersion:
                    return data;
                default:
                    return null;
            }
        }
    }
}
