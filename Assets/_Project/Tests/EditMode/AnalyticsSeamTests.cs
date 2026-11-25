using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cryptforge.Analytics;
using Cryptforge.Monetisation;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The analytics seam (docs/59 3.3): the local file log behind IAnalytics writes exactly what the session writes, a
    // fan-out reaches every sink whatever one of them does, and the monetisation's events carry their fields in order.
    public sealed class AnalyticsSeamTests
    {
        [Test]
        public void TheLocalFileSinkWritesTheSessionsOwnLinesByteForByte()
        {
            var directClock = new TelemetryLogTests.FakeClock();
            var directStore = new TelemetryLogTests.MemoryStore();
            var direct = new TelemetrySession(new TelemetryLog(directStore, directClock), directClock, "s1");
            var seamClock = new TelemetryLogTests.FakeClock();
            var seamStore = new TelemetryLogTests.MemoryStore();
            var seamSession = new TelemetrySession(new TelemetryLog(seamStore, seamClock), seamClock, "s1");
            IAnalytics seam = new LocalFileAnalytics(seamSession);
            Assert.That(((LocalFileAnalytics)seam).Session, Is.SameAs(seamSession));

            direct.Start(null);
            seamSession.Start(null);
            string directRun = direct.BeginRun();
            string seamRun = seamSession.BeginRun();
            Assert.That(seamRun, Is.EqualTo(directRun));

            directClock.Advance(1.5);
            seamClock.Advance(1.5);
            direct.Emit("run_start", new TelemetryFields().Add("hero_id", "hero_vanguard"));
            seam.Emit("run_start", new TelemetryFields().Add("hero_id", "hero_vanguard"));
            direct.EndRun();
            seamSession.EndRun();
            direct.EmitForRun(directRun, "reward_offer", MonetisationTelemetry.RewardOffer(RewardOfferState.Video, 15, 8, 150, 2));
            seam.EmitForRun(seamRun, "reward_offer", MonetisationTelemetry.RewardOffer(RewardOfferState.Video, 15, 8, 150, 2));
            direct.EmitForRun(null, "ad_counters", MonetisationTelemetry.AdCounters(new AdCounters()));
            seam.EmitForRun(null, "ad_counters", MonetisationTelemetry.AdCounters(new AdCounters()));
            Assert.That(direct.Flush(), Is.True);
            Assert.That(seam.Flush(), Is.True);

            Assert.That(seamStore.Lines(), Is.EqualTo(directStore.Lines()));
            List<string> lines = seamStore.Lines();
            Assert.That(lines.Count, Is.EqualTo(4));
            Assert.That(lines[2], Is.EqualTo(
                "{\"v\":2,\"seq\":3,\"t\":\"2026-09-15T12:00:01.500Z\",\"st\":1.500,\"session\":\"s1\",\"run\":\"s1-1\"," +
                "\"event\":\"reward_offer\",\"offer\":\"video\",\"run_gold\":15,\"bonus_gold\":8,\"multiplier_pct\":150,\"completed_runs\":2}"));
            Assert.That(lines[3], Does.EndWith(
                "\"session\":\"s1\",\"event\":\"ad_counters\",\"requests\":0,\"matches\":0,\"shows\":0,\"show_failures\":0," +
                "\"rewards\":0,\"attempts\":0,\"attempts_ready\":0}"));
            Assert.Throws<ArgumentNullException>(() => new LocalFileAnalytics(null));
        }

        [Test]
        public void AFanOutReachesEverySinkEvenPastOneThatThrows()
        {
            var failing = new RecordingAnalytics { Failure = new IOException("backend down") };
            var local = new RecordingAnalytics();
            var both = new AnalyticsFanOut(failing, local);
            Assert.That(both.Count, Is.EqualTo(2));

            Assert.Throws<IOException>(() => both.Emit("run_start", new TelemetryFields().Add("seed", 7)));
            Assert.Throws<IOException>(() => both.EmitForRun("r1", "reward_result"));
            Assert.Throws<IOException>(() => both.Flush());
            Assert.That(local.Lines, Is.EqualTo(new[] { "run_start @current seed=7", "reward_result @r1" }));
            Assert.That(local.Flushes, Is.EqualTo(1));

            failing.Failure = null;
            local.FlushAnswer = false;
            Assert.That(both.Flush(), Is.False, "Something was left in one sink.");
            Assert.That(new AnalyticsFanOut().Flush(), Is.True);
            Assert.Throws<ArgumentException>(() => new AnalyticsFanOut(local, null));
            Assert.Throws<ArgumentNullException>(() => new AnalyticsFanOut(null));
        }

        [Test]
        public void TheMonetisationEventsCarryTheirFieldsInOrder()
        {
            Assert.That(RecordingAnalytics.Format("r2", "reward_offer", MonetisationTelemetry.RewardOffer(RewardOfferState.Skip, 40, 20, 150, 7)),
                Is.EqualTo("reward_offer @r2 offer=\"skip\" run_gold=40 bonus_gold=20 multiplier_pct=150 completed_runs=7"));
            Assert.Throws<ArgumentException>(() => MonetisationTelemetry.RewardOffer(RewardOfferState.NoAd, 40, 20, 150, 7));
            Assert.Throws<ArgumentException>(() => MonetisationTelemetry.RewardOffer(RewardOfferState.Resolved, 40, 20, 150, 7));

            Assert.That(RecordingAnalytics.Format("r2", "reward_result", MonetisationTelemetry.RewardResult(RewardResultKind.Watched, true, 20)),
                Is.EqualTo("reward_result @r2 result=\"watched\" credited=true bonus_gold=20"));
            Assert.Throws<ArgumentOutOfRangeException>(() => MonetisationTelemetry.RewardResult(RewardResultKind.Skipped, false, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => MonetisationTelemetry.RewardResult(RewardResultKind.Watched, true, -1));

            string[] results = { "watched", "skipped", "failed", "skip_owned", "declined", "no_ad" };
            for (int i = 0; i < results.Length; i++)
                Assert.That(MonetisationTelemetry.Code((RewardResultKind)i), Is.EqualTo(results[i]));
            string[] purchases = { "purchased", "pending", "cancelled", "failed", "already_owned" };
            for (int i = 0; i < purchases.Length; i++)
                Assert.That(MonetisationTelemetry.Code((StorePurchaseResult)i), Is.EqualTo(purchases[i]));

            Assert.That(RecordingAnalytics.Format(null, "store_purchase",
                    MonetisationTelemetry.StorePurchase(StoreProducts.SkipRewardVideos, StorePurchaseResult.Purchased)),
                Is.EqualTo("store_purchase @- product=\"skip_reward_videos\" result=\"purchased\""));

            var counters = new AdCounters();
            Assert.That(double.IsNaN(counters.MatchRate) && double.IsNaN(counters.ShowRate) && double.IsNaN(counters.Availability), Is.True);
            counters.RecordRequest();
            counters.RecordRequest();
            counters.RecordMatch();
            counters.RecordShow(RewardedVideoResult.Skipped);
            counters.RecordShow(RewardedVideoResult.Failed);
            counters.RecordAttempt(true);
            counters.RecordAttempt(false);
            counters.RecordAttempt(false);
            counters.RecordAttempt(true);
            Assert.That((counters.MatchRate, counters.ShowRate, counters.Availability), Is.EqualTo((0.5, 1.0, 0.5)));
            Assert.That(RecordingAnalytics.Format(null, "ad_counters", MonetisationTelemetry.AdCounters(counters)),
                Is.EqualTo("ad_counters @- requests=2 matches=1 shows=1 show_failures=1 rewards=0 attempts=4 attempts_ready=2"));
        }
    }

    // An IAnalytics that keeps each event as "name @run key=value ...", the value as the line would hold it ("@current":
    // Emit, under the current run; "@-": outside any run).
    internal sealed class RecordingAnalytics : IAnalytics
    {
        public readonly List<string> Lines = new List<string>();
        public int Flushes;
        public bool FlushAnswer = true;
        public Exception Failure;

        public void Emit(string name, TelemetryFields fields = null)
        {
            Record("current", name, fields);
        }

        public void EmitForRun(string runId, string name, TelemetryFields fields = null)
        {
            Record(runId ?? "-", name, fields);
        }

        public bool Flush()
        {
            if (Failure != null)
                throw Failure;
            Flushes++;
            return FlushAnswer;
        }

        // The lines of one event name, in order.
        public List<string> Named(string name)
        {
            var named = new List<string>();
            foreach (string line in Lines)
            {
                if (line.StartsWith(name + " @", StringComparison.Ordinal))
                    named.Add(line);
            }
            return named;
        }

        public static string Format(string run, string name, TelemetryFields fields)
        {
            var text = new StringBuilder(name).Append(" @").Append(run ?? "-");
            if (fields != null)
            {
                for (int i = 0; i < fields.Count; i++)
                    text.Append(' ').Append(fields.KeyAt(i)).Append('=').Append(fields.JsonValueAt(i));
            }
            return text.ToString();
        }

        private void Record(string run, string name, TelemetryFields fields)
        {
            if (Failure != null)
                throw Failure;
            Lines.Add(Format(run, name, fields));
        }
    }
}
