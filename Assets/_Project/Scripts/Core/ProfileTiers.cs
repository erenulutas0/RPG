using System;
using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // The profile's difficulty tiers (docs/54 sections 8 and 21): each tier's record - the deepest floor a run on it has
    // cleared, and whether a run on it has walked out of the Descent - and the tier chosen for the next run. A tier opens
    // once the Descent is completed on the tier below (Forged is always open); a deeper map start on a tier opens once a
    // run on that tier has reached it (MapUnlock with the tier's own record). PlayerProfile raises Changed once per change;
    // Restore repairs a damaged save instead of rejecting it, as ProfileGear does.
    public sealed class ProfileTiers
    {
        private readonly int[] _deepest = new int[DifficultyTiers.Count];
        private readonly bool[] _completed = new bool[DifficultyTiers.Count];

        // The tier the next run is played on; always an open one.
        public DifficultyTier Selected { get; private set; }

        public int DeepestFloorCleared(DifficultyTier tier) => _deepest[Index(tier)];

        public bool HasCompleted(DifficultyTier tier) => _completed[Index(tier)];

        // Forged always; another tier once the Descent is completed on the one below it.
        public bool IsOpen(DifficultyTier tier) =>
            !DifficultyTiers.TryBelow(tier, out DifficultyTier below) || HasCompleted(below);

        // Whether a map may start a run on this tier: the tier open, and the floor reached on it (MapUnlock).
        public bool IsMapOpen(int floorNumber, DifficultyTier tier) =>
            IsOpen(tier) && MapUnlock.IsOpen(floorNumber, DeepestFloorCleared(tier));

        // The tiers completed in order, counted: 0 none, 1 Forged, 2 Tempered, ... (version 4's highestTierCompleted).
        public int CompletedInOrder
        {
            get
            {
                int count = 0;
                while (count < DifficultyTiers.Count && _completed[count])
                    count++;
                return count;
            }
        }

        // ---- Changes: PlayerProfile calls these and raises Changed when one returns true. ----

        internal bool RecordFloorCleared(DifficultyTier tier, int floorNumber)
        {
            int i = Index(tier);
            if (floorNumber <= _deepest[i])
                return false;
            _deepest[i] = floorNumber;
            return true;
        }

        internal bool RecordCompleted(DifficultyTier tier)
        {
            int i = Index(tier);
            bool changed = !_completed[i] || _deepest[i] < DescentStart.FinaleFloor;
            _completed[i] = true;
            _deepest[i] = Math.Max(_deepest[i], DescentStart.FinaleFloor);
            return changed;
        }

        internal bool Select(DifficultyTier tier)
        {
            if (!IsOpen(tier) || tier == Selected)
                return false;
            Selected = tier;
            return true;
        }

        // Every run before the tiers was a Forged run: the profile's own depth record (PlayerProfile.DeepestFloorCleared)
        // is Forged's at the least, and a record through the finale completed it.
        internal void Reconcile(int deepestFloorCleared)
        {
            if (deepestFloorCleared > _deepest[0])
                _deepest[0] = deepestFloorCleared;
            if (_deepest[0] >= DescentStart.FinaleFloor)
                _completed[0] = true;
        }

        private static int Index(DifficultyTier tier)
        {
            if (!DifficultyTiers.IsDefined((int)tier))
                throw new ArgumentOutOfRangeException(nameof(tier));
            return (int)tier;
        }

        // ---- The save. ----

        public TierSnapshot Snapshot()
        {
            var snapshot = new TierSnapshot
            {
                Records = new TierRecord[DifficultyTiers.Count],
                SelectedTier = (int)Selected,
                CompletedInOrder = CompletedInOrder
            };
            for (int i = 0; i < DifficultyTiers.Count; i++)
                snapshot.Records[i] = new TierRecord
                {
                    Tier = DifficultyTiers.Id((DifficultyTier)i), DeepestFloorCleared = _deepest[i], Completed = _completed[i]
                };
            return snapshot;
        }

        // Rebuilds the tiers from a save, repairing what is damaged: unknown or empty tier ids dropped, a repeated tier's
        // later records dropped (the first wins), negative depths zeroed, a completed tier raised to the finale; a tier that
        // is not open (the one below it not completed) keeps no record, so a damaged save never opens what was not earned;
        // a chosen tier that is unknown or not open falls back to Forged. profileDeepest is the profile's own depth record
        // (PlayerProfile.DeepestFloorCleared), Forged's at the least (Reconcile). Never throws.
        public static ProfileTiers Restore(TierSnapshot snapshot, int profileDeepest = 0)
        {
            var tiers = new ProfileTiers();
            if (snapshot == null)
            {
                tiers.Reconcile(profileDeepest);
                return tiers;
            }
            var seen = new bool[DifficultyTiers.Count];
            if (snapshot.Records != null)
            {
                foreach (TierRecord record in snapshot.Records)
                {
                    if (record == null || !DifficultyTiers.TryParse(record.Tier, out DifficultyTier tier) || seen[(int)tier])
                        continue;
                    seen[(int)tier] = true;
                    tiers._deepest[(int)tier] = Math.Max(0, record.DeepestFloorCleared);
                    tiers._completed[(int)tier] = record.Completed;
                }
            }
            tiers.Reconcile(profileDeepest);
            for (int i = 0; i < DifficultyTiers.Count; i++)
            {
                if (!tiers.IsOpen((DifficultyTier)i))
                {
                    tiers._deepest[i] = 0;
                    tiers._completed[i] = false;
                }
                else if (tiers._completed[i])
                {
                    tiers._deepest[i] = Math.Max(tiers._deepest[i], DescentStart.FinaleFloor);
                }
            }
            if (DifficultyTiers.IsDefined(snapshot.SelectedTier) && tiers.IsOpen((DifficultyTier)snapshot.SelectedTier))
                tiers.Selected = (DifficultyTier)snapshot.SelectedTier;
            return tiers;
        }
    }

    // The tiers as plain values, as the save holds them and before ProfileTiers.Restore has repaired them. Nothing here is
    // trusted.
    public sealed class TierSnapshot
    {
        public TierRecord[] Records = new TierRecord[0];
        public int SelectedTier;
        // Written for a reader's eye (ProfileSaveData.highestTierCompleted); Restore reads the records alone.
        public int CompletedInOrder;
    }

    // One tier's record: its id (DifficultyTiers.Id), the deepest floor cleared on it and whether its Descent was completed.
    public sealed class TierRecord
    {
        public string Tier;
        public int DeepestFloorCleared;
        public bool Completed;
    }
}
