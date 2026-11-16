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
                    // Version 3 predates gear (docs/54): an empty bag, no weapon upgrades, nothing equipped, no materials or
                    // boosters, the anvil not yet seeded (it seeds itself on its first draw). Gold is untouched (the lead,
                    // 2026-11-16: the owner's gold stays).
                    data.items = new ItemSaveData[0];
                    data.weaponGear = new WeaponGearSaveData[0];
                    data.equippedItemUids = new int[3];
                    data.nextItemUid = 1;
                    data.emberShards = data.quicksilverBeads = data.gloamSpores = 0;
                    data.forgeSeals = data.mastersHammers = data.efsunScrolls = data.bindingRunes = 0;
                    data.anvilSeed = 0;
                    data.anvilDraws = 0;
                    data.firstArmourGiven = false;
                    data.highestTierCompleted = 0;
                    data.selectedTier = 0;
                    data.pendingRerollKey = string.Empty;
                    data.pendingRerollLines = new LineSaveData[0];
                    data.saveVersion = 4;
                    goto case 4;
                case 4:
                    return data;
                default:
                    return null;
            }
        }
    }
}
