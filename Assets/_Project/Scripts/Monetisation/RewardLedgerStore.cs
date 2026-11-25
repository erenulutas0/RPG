using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cryptforge.Monetisation
{
    // rewards.json beside the profile (the wiring passes ProfileLocation.Resolve(), as for arena.json), in ArenaSaveStore's
    // shape (SmallSaveFile): a temp file, a backup, the copy with the highest revision loaded. The profile never reads it
    // and its store never touches it. A missing or unreadable ledger never touches the profile either: the ledger starts
    // afresh, which costs at most one result screen's offer (its completed-run count restarts at 0).
    // A copy written by a newer build (IsNewerVersion) is not damaged: it is never read, and while one stands the store is
    // read only - Save writes, moves and deletes nothing (IsReadOnly) - so the newer build finds its ledger whole. The
    // ledger itself lives in memory for the launch (MonetisationRuntime), so a write that fails or is not made loses
    // nothing of it before the app closes.
    public sealed class RewardLedgerStore
    {
        public const string FileName = "rewards.json";

        private readonly SmallSaveFile _file;
        private readonly List<string> _problems = new List<string>();
        private bool _readOnlyNoted;

        public string Directory => _file.Directory;
        // Why copies were skipped during the last load, and that a save wrote nothing while the store is read only, for the
        // caller to log.
        public IReadOnlyList<string> LastLoadProblems => _problems;
        // True when the last load found no readable ledger and made a new one.
        public bool LastLoadWasNew { get; private set; }
        // A newer build's ledger stands among the copies: Save writes nothing this session.
        public bool IsReadOnly { get; private set; }

        public RewardLedgerStore(string directory)
        {
            _file = new SmallSaveFile(directory, FileName);
        }

        public RewardLedger Load()
        {
            _problems.Clear();
            IsReadOnly = false;
            RewardLedger best = null;
            foreach (KeyValuePair<string, string> copy in _file.ReadCopies(_problems))
            {
                RewardLedger ledger = Read(copy.Value);
                if (ledger == null)
                {
                    if (IsNewerVersion(copy.Value))
                    {
                        IsReadOnly = true;
                        _problems.Add(copy.Key + " was written by a newer build (this build reads up to version " +
                                      RewardLedger.CurrentVersion.ToString(CultureInfo.InvariantCulture) +
                                      "): it is kept as it is, and nothing is written this session.");
                        continue;
                    }
                    _problems.Add(copy.Key + " is not a readable reward ledger of version " +
                                  RewardLedger.CurrentVersion.ToString(CultureInfo.InvariantCulture) + " or older.");
                    continue;
                }
                if (best == null || ledger.Revision > best.Revision)
                    best = ledger;
            }
            // A newer build's ledger is not this build's to read or to replace: none of the copies is read.
            if (IsReadOnly)
                best = null;
            LastLoadWasNew = best == null;
            return best ?? new RewardLedger();
        }

        // Bumps the ledger's revision and writes it. May throw IOException or UnauthorizedAccessException. While the store
        // is read only nothing is written, and the problems say so once.
        public void Save(RewardLedger ledger)
        {
            if (ledger == null)
                throw new ArgumentNullException(nameof(ledger));
            if (IsReadOnly)
            {
                if (!_readOnlyNoted)
                {
                    _readOnlyNoted = true;
                    _problems.Add("Nothing was written to " + FileName + ": a newer build's ledger stands in " + Directory +
                                  ", and this session's is kept in memory only.");
                }
                return;
            }
            ledger.Revision++;
            _file.Write(Write(ledger));
        }

        // The file's text: a fixed key order, invariant numbers (FlatJson).
        public static string Write(RewardLedger ledger)
        {
            if (ledger == null)
                throw new ArgumentNullException(nameof(ledger));
            return new FlatJson.Writer()
                .Field("saveVersion", RewardLedger.CurrentVersion)
                .Field("revision", ledger.Revision)
                .Field("completedRuns", ledger.CompletedRuns)
                .Field("lastCompletedRunId", ledger.LastCompletedRunId)
                .Field("bonusesCredited", ledger.BonusesCredited)
                .Field("bonusGoldCredited", ledger.BonusGoldCredited)
                .Field("creditedRunIds", ledger.CreditedRunIds)
                .Finish();
        }

        // The ledger the text holds; null for text that is not a JSON object, or a ledger of a version this build does not
        // know (never guessed at).
        public static RewardLedger Read(string json)
        {
            Dictionary<string, object> o = FlatJson.Read(json);
            if (o == null)
                return null;
            int version = FlatJson.Int(o, "saveVersion", 0);
            if (version < 1 || version > RewardLedger.CurrentVersion)
                return null;
            return RewardLedger.Restore(
                FlatJson.Long(o, "revision", 0),
                FlatJson.Int(o, "completedRuns", 0),
                FlatJson.String(o, "lastCompletedRunId"),
                FlatJson.Int(o, "bonusesCredited", 0),
                FlatJson.Long(o, "bonusGoldCredited", 0),
                FlatJson.Strings(o, "creditedRunIds"));
        }

        // Whether the text is a whole ledger of a version above this build's (an older APK installed over a newer one),
        // which Read refuses as it refuses a damaged one but which must never be written over.
        public static bool IsNewerVersion(string json)
        {
            Dictionary<string, object> o = FlatJson.Read(json);
            return o != null && FlatJson.Int(o, "saveVersion", 0) > RewardLedger.CurrentVersion;
        }
    }
}
