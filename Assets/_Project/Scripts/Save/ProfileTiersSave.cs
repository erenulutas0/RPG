using Cryptforge.Core;

namespace Cryptforge.Save
{
    // The difficulty tiers between their JSON fields (ProfileSaveData version 5: tierProgress, selectedTier and the
    // derived highestTierCompleted) and the plain TierSnapshot ProfileTiers restores from and writes to. Copies values only;
    // ProfileTiers.Restore does the repairing, so a damaged field here is never fatal.
    public static class ProfileTiersSave
    {
        public static TierSnapshot ToSnapshot(ProfileSaveData data)
        {
            var snapshot = new TierSnapshot();
            if (data == null)
                return snapshot;
            snapshot.Records = new TierRecord[data.tierProgress?.Length ?? 0];
            for (int i = 0; i < snapshot.Records.Length; i++)
            {
                TierProgressSaveData record = data.tierProgress[i];
                snapshot.Records[i] = record == null ? null : new TierRecord
                {
                    Tier = record.tier, DeepestFloorCleared = record.deepestFloorCleared, Completed = record.completed
                };
            }
            snapshot.SelectedTier = data.selectedTier;
            snapshot.CompletedInOrder = data.highestTierCompleted;
            return snapshot;
        }

        public static void Write(TierSnapshot snapshot, ProfileSaveData data)
        {
            data.tierProgress = new TierProgressSaveData[snapshot.Records.Length];
            for (int i = 0; i < data.tierProgress.Length; i++)
            {
                TierRecord record = snapshot.Records[i];
                data.tierProgress[i] = new TierProgressSaveData
                {
                    tier = record.Tier, deepestFloorCleared = record.DeepestFloorCleared, completed = record.Completed
                };
            }
            data.selectedTier = snapshot.SelectedTier;
            data.highestTierCompleted = snapshot.CompletedInOrder;
        }
    }
}
