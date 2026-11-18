using System;
using System.Collections.Generic;

namespace Cryptforge.Monetisation
{
    // What the result screen's offer has done, kept apart from the profile (docs/58 3.2): the completed runs the offer
    // counts (3.4: it starts at the second), and the run ids whose bonus was credited, so a bonus is credited once per run
    // id however often a network reports the reward. Pure: RewardLedgerStore moves it to and from rewards.json.
    //
    // Only the newest MaxCreditedRunIds ids are kept. That bounds the file; it is enough because a run's offer lives on its
    // own result screen and a run id is never offered again after it (the telemetry's run id is the session's GUID and an
    // ordinal, docs/10). A completed run is counted once per id against the last one counted, for the same reason.
    public sealed class RewardLedger
    {
        public const int CurrentVersion = 1;
        public const int MaxCreditedRunIds = 32;

        private readonly List<string> _creditedRunIds = new List<string>();

        // Bumped by every save (RewardLedgerStore); the copy with the highest revision is the one loaded.
        public long Revision { get; internal set; }
        public int CompletedRuns { get; private set; }
        public string LastCompletedRunId { get; private set; }
        public int BonusesCredited { get; private set; }
        public long BonusGoldCredited { get; private set; }
        // Oldest first.
        public IReadOnlyList<string> CreditedRunIds => _creditedRunIds;

        // A run reached its result screen. False, and nothing counted, when this run was the last one counted.
        public bool RecordCompletedRun(string runId)
        {
            RequireRunId(runId);
            if (string.Equals(runId, LastCompletedRunId, StringComparison.Ordinal))
                return false;
            if (CompletedRuns < int.MaxValue)
                CompletedRuns++;
            LastCompletedRunId = runId;
            return true;
        }

        public bool HasCredited(string runId) =>
            !string.IsNullOrEmpty(runId) && _creditedRunIds.Contains(runId);

        // Credits a run's bonus once. False, and nothing changed, when this run id was credited already.
        public bool TryCredit(string runId, int bonusGold)
        {
            RequireRunId(runId);
            if (bonusGold <= 0)
                throw new ArgumentOutOfRangeException(nameof(bonusGold), "A credited bonus is at least one gold.");
            if (HasCredited(runId))
                return false;
            _creditedRunIds.Add(runId);
            while (_creditedRunIds.Count > MaxCreditedRunIds)
                _creditedRunIds.RemoveAt(0);
            if (BonusesCredited < int.MaxValue)
                BonusesCredited++;
            BonusGoldCredited += bonusGold;
            return true;
        }

        // A ledger as a file held it; RewardLedgerStore's reader is the one caller.
        internal static RewardLedger Restore(long revision, int completedRuns, string lastCompletedRunId, int bonusesCredited,
            long bonusGoldCredited, IEnumerable<string> creditedRunIds)
        {
            var ledger = new RewardLedger
            {
                Revision = Math.Max(0, revision),
                CompletedRuns = Math.Max(0, completedRuns),
                LastCompletedRunId = string.IsNullOrEmpty(lastCompletedRunId) ? null : lastCompletedRunId,
                BonusesCredited = Math.Max(0, bonusesCredited),
                BonusGoldCredited = Math.Max(0, bonusGoldCredited)
            };
            if (creditedRunIds != null)
            {
                foreach (string id in creditedRunIds)
                {
                    if (!string.IsNullOrEmpty(id) && !ledger._creditedRunIds.Contains(id))
                        ledger._creditedRunIds.Add(id);
                }
            }
            while (ledger._creditedRunIds.Count > MaxCreditedRunIds)
                ledger._creditedRunIds.RemoveAt(0);
            return ledger;
        }

        private static void RequireRunId(string runId)
        {
            if (string.IsNullOrEmpty(runId))
                throw new ArgumentException("A run id is required.", nameof(runId));
        }
    }
}
