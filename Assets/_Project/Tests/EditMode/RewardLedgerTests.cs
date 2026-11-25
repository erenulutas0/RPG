using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Cryptforge.Monetisation;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The offer's ledger (docs/58 3.2) and its file, rewards.json, apart from the profile: once per run id, bounded, the
    // newest readable copy loaded, and the same bytes whatever the phone's culture.
    public sealed class RewardLedgerTests
    {
        private string _root;

        [SetUp]
        public void MakeFolder()
        {
            _root = Path.Combine(Path.GetTempPath(), "cryptforge-ledger-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void ARunIsCountedAndCreditedOnce()
        {
            var ledger = new RewardLedger();
            Assert.That(ledger.RecordCompletedRun("a-1"), Is.True);
            Assert.That(ledger.RecordCompletedRun("a-1"), Is.False, "The same screen again.");
            Assert.That(ledger.RecordCompletedRun("a-2"), Is.True);
            Assert.That((ledger.CompletedRuns, ledger.LastCompletedRunId), Is.EqualTo((2, "a-2")));

            Assert.That(ledger.TryCredit("a-2", 8), Is.True);
            Assert.That(ledger.TryCredit("a-2", 8), Is.False);
            Assert.That((ledger.HasCredited("a-2"), ledger.HasCredited("a-1"), ledger.HasCredited(null)), Is.EqualTo((true, false, false)));
            Assert.That((ledger.BonusesCredited, ledger.BonusGoldCredited), Is.EqualTo((1, 8L)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.TryCredit("a-3", 0));
            Assert.Throws<ArgumentException>(() => ledger.TryCredit("", 5));
            Assert.Throws<ArgumentException>(() => ledger.RecordCompletedRun(null));
        }

        [Test]
        public void OnlyTheNewestCreditedRunIdsAreKept()
        {
            var ledger = new RewardLedger();
            for (int i = 1; i <= RewardLedger.MaxCreditedRunIds + 8; i++)
                Assert.That(ledger.TryCredit("r-" + i.ToString(CultureInfo.InvariantCulture), 1), Is.True);
            Assert.That(ledger.CreditedRunIds.Count, Is.EqualTo(RewardLedger.MaxCreditedRunIds));
            Assert.That(ledger.CreditedRunIds[0], Is.EqualTo("r-9"), "The oldest eight went.");
            Assert.That(ledger.HasCredited("r-40"), Is.True);
            Assert.That((ledger.BonusesCredited, ledger.BonusGoldCredited), Is.EqualTo((40, 40L)), "The totals keep every credit.");
        }

        [Test]
        public void TheFileIsFixedTextAndReadsBack()
        {
            var ledger = new RewardLedger();
            ledger.RecordCompletedRun("s\"1-1");
            ledger.TryCredit("s\"1-1", 12345);
            ledger.TryCredit("s1-0", 5);
            string text = RewardLedgerStore.Write(ledger);
            Assert.That(text, Is.EqualTo(
                "{\n" +
                "  \"saveVersion\": 1,\n" +
                "  \"revision\": 0,\n" +
                "  \"completedRuns\": 1,\n" +
                "  \"lastCompletedRunId\": \"s\\\"1-1\",\n" +
                "  \"bonusesCredited\": 2,\n" +
                "  \"bonusGoldCredited\": 12350,\n" +
                "  \"creditedRunIds\": [\"s\\\"1-1\", \"s1-0\"]\n" +
                "}\n"));

            RewardLedger back = RewardLedgerStore.Read(text);
            Assert.That(back, Is.Not.Null);
            Assert.That((back.CompletedRuns, back.LastCompletedRunId, back.BonusesCredited, back.BonusGoldCredited),
                Is.EqualTo((1, "s\"1-1", 2, 12350L)));
            Assert.That(back.CreditedRunIds, Is.EqualTo(new[] { "s\"1-1", "s1-0" }));
            Assert.That(RewardLedgerStore.Write(back), Is.EqualTo(text));

            Assert.That(RewardLedgerStore.Read("{}"), Is.Null, "No version.");
            Assert.That(RewardLedgerStore.Read("{\"saveVersion\": 2}"), Is.Null, "A newer version is refused, never guessed at.");
            Assert.That(RewardLedgerStore.Read("[1]"), Is.Null);
            Assert.That(RewardLedgerStore.Read("{\"saveVersion\": 1,"), Is.Null);
            Assert.That(RewardLedgerStore.Read(null), Is.Null);
            RewardLedger odd = RewardLedgerStore.Read(
                "{\"saveVersion\": 1, \"completedRuns\": -4, \"bonusGoldCredited\": 2.5, \"creditedRunIds\": [\"x\", 3, null, \"x\", \"y\"], \"extra\": {\"a\": [1]}}");
            Assert.That((odd.CompletedRuns, odd.BonusGoldCredited, odd.LastCompletedRunId), Is.EqualTo((0, 0L, (string)null)));
            Assert.That(odd.CreditedRunIds, Is.EqualTo(new[] { "x", "y" }), "Forgiving fields: wrong types skipped, duplicates dropped.");
        }

        [Test]
        public void TheStoreLoadsTheNewestReadableCopy()
        {
            var store = new RewardLedgerStore(_root);
            RewardLedger fresh = store.Load();
            Assert.That((store.LastLoadWasNew, fresh.CompletedRuns, store.LastLoadProblems.Count), Is.EqualTo((true, 0, 0)));

            fresh.RecordCompletedRun("a-1");
            store.Save(fresh);
            fresh.RecordCompletedRun("a-2");
            store.Save(fresh);
            Assert.That(fresh.Revision, Is.EqualTo(2L));
            string path = Path.Combine(_root, RewardLedgerStore.FileName);
            Assert.That(File.Exists(path) && File.Exists(path + ".bak") && !File.Exists(path + ".tmp"), Is.True);

            RewardLedger loaded = new RewardLedgerStore(_root).Load();
            Assert.That((loaded.Revision, loaded.CompletedRuns), Is.EqualTo((2L, 2)));

            // A crash after the backup moved and before the temp file moved in: the temp copy is newest.
            loaded.RecordCompletedRun("a-3");
            string elsewhere = Path.Combine(_root, "elsewhere");
            new RewardLedgerStore(elsewhere).Save(loaded);
            Assert.That(loaded.Revision, Is.EqualTo(3L));
            File.Copy(Path.Combine(elsewhere, RewardLedgerStore.FileName), path + ".tmp");
            File.Delete(path);
            Assert.That(new RewardLedgerStore(_root).Load().CompletedRuns, Is.EqualTo(3));

            // A torn main file: the backup stands, and the problem is reported.
            File.WriteAllText(path, "{\"saveVersion\": 1, \"revis");
            File.Delete(path + ".tmp");
            var torn = new RewardLedgerStore(_root);
            RewardLedger fromBackup = torn.Load();
            Assert.That((fromBackup.CompletedRuns, torn.LastLoadWasNew), Is.EqualTo((1, false)));
            Assert.That(torn.LastLoadProblems, Is.EqualTo(new[] { "rewards.json is not a readable reward ledger of version 1 or older." }));

            Assert.Throws<ArgumentException>(() => new RewardLedgerStore(""));
            Assert.Throws<ArgumentNullException>(() => store.Save(null));
        }

        // An older APK installed over a newer one: the newer build's ledger, whichever copy it is (the backup beside a damaged
        // main too), is never read, written over or deleted; the store is read only and the launch's ledger lives in memory.
        [TestCase(RewardLedgerStore.FileName, false)]
        [TestCase(RewardLedgerStore.FileName + ".tmp", false)]
        [TestCase(RewardLedgerStore.FileName + ".bak", false)]
        [TestCase(RewardLedgerStore.FileName + ".bak", true)]
        public void ANewerBuildsLedgerIsNeverWrittenOver(string newerFile, bool corruptMain)
        {
            Directory.CreateDirectory(_root);
            const string newer = "{\"saveVersion\": 2, \"revision\": 9, \"completedRuns\": 40, \"futureKey\": [\"x\"]}";
            var older = new RewardLedger();
            older.RecordCompletedRun("old-1");
            string current = RewardLedgerStore.Write(older);
            Assert.That(RewardLedgerStore.IsNewerVersion(newer), Is.True);
            Assert.That(RewardLedgerStore.IsNewerVersion(current), Is.False);
            Assert.That(RewardLedgerStore.IsNewerVersion("{\"saveVersion\": 1, \"revis"), Is.False, "Damaged, not newer.");
            Assert.That(RewardLedgerStore.IsNewerVersion(null), Is.False);
            string main = Path.Combine(_root, RewardLedgerStore.FileName);
            foreach (string file in new[] { main, main + ".tmp", main + ".bak" })
            {
                string name = Path.GetFileName(file);
                File.WriteAllText(file, name == newerFile ? newer : corruptMain && file == main ? "{\"saveVersion\": 1, \"revis" : current);
            }
            var before = new Dictionary<string, string>();
            foreach (string file in Directory.GetFiles(_root))
                before[Path.GetFileName(file)] = File.ReadAllText(file);

            var store = new RewardLedgerStore(_root);
            RewardLedger ledger = store.Load();
            Assert.That((store.IsReadOnly, store.LastLoadWasNew, ledger.CompletedRuns), Is.EqualTo((true, true, 0)),
                "A new ledger in memory, not the older copy beside the newer one.");
            Assert.That(store.LastLoadProblems, Has.Some.Contains(newerFile + " was written by a newer build"));
            for (int run = 0; run < 3; run++)
            {
                ledger.RecordCompletedRun("run-" + run);
                store.Save(ledger);
            }
            Assert.That(ledger.CompletedRuns, Is.EqualTo(3), "Kept in memory.");
            Assert.That(store.LastLoadProblems, Has.Exactly(1).Contains("Nothing was written"), "Said once.");
            string[] files = Directory.GetFiles(_root);
            Assert.That(files.Length, Is.EqualTo(before.Count), "No file made or deleted: " + string.Join(", ", files));
            foreach (KeyValuePair<string, string> file in before)
                Assert.That(File.ReadAllText(Path.Combine(_root, file.Key)), Is.EqualTo(file.Value), file.Key + " is unchanged.");
            Assert.That(new RewardLedgerStore(_root).Load().CompletedRuns, Is.Zero, "Still the newer build's: read by none.");
        }

        [Test]
        public void TheFileIsTheSameBytesWhateverTheThreadsCulture()
        {
            var odd = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            odd.NumberFormat.NegativeSign = "−";
            odd.NumberFormat.NumberGroupSeparator = ".";
            byte[] invariant = SaveUnder(CultureInfo.InvariantCulture, "invariant");
            byte[] turkish = SaveUnder(new CultureInfo("tr-TR"), "turkish");
            byte[] other = SaveUnder(odd, "odd");
            Assert.That(turkish, Is.EqualTo(invariant));
            Assert.That(other, Is.EqualTo(invariant));

            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                RewardLedger back = new RewardLedgerStore(Path.Combine(_root, "invariant")).Load();
                Assert.That((back.BonusGoldCredited, back.CompletedRuns, back.HasCredited("İi-1")), Is.EqualTo((1234567L, 1, true)));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
        }

        private byte[] SaveUnder(CultureInfo culture, string name)
        {
            string folder = Path.Combine(_root, name);
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = culture;
                var ledger = new RewardLedger();
                ledger.RecordCompletedRun("İi-1");
                ledger.TryCredit("İi-1", 1234567);
                new RewardLedgerStore(folder).Save(ledger);
                return File.ReadAllBytes(Path.Combine(folder, RewardLedgerStore.FileName));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
        }
    }
}
