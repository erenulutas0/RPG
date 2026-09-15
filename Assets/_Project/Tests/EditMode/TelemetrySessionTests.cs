using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Cryptforge.Analytics;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The session stamps order and identity: session_start first and once, one sequence across runs, one id per run, and
    // lost lines reported in order.
    public sealed class TelemetrySessionTests
    {
        private static readonly Regex SequencePattern = new Regex("\"seq\":(\\d+),");
        private static readonly Regex RunPattern = new Regex("\"run\":\"([^\"]*)\"");
        private static readonly Regex EventPattern = new Regex("\"event\":\"([a-z0-9_]+)\"");

        [Test]
        public void SessionStartIsTheFirstLineAndIsWrittenOnce()
        {
            var clock = new TelemetryLogTests.FakeClock();
            var store = new TelemetryLogTests.MemoryStore();
            var session = new TelemetrySession(new TelemetryLog(store, clock), clock, "s1");

            Assert.Throws<InvalidOperationException>(() => session.Emit("run_start"), "Nothing may come before session_start.");
            Assert.That((session.LastSequence, session.Log.Buffered, session.HasStarted), Is.EqualTo((0L, 0, false)));

            clock.Advance(0.25);
            session.Start(new TelemetryFields().Add("app_version", "0.1.0").Add("platform", "android").Add("development", true));
            session.Start(new TelemetryFields().Add("app_version", "again"));
            session.Emit("app_background", new TelemetryFields().Add("run_active", false));
            Assert.That(session.Flush(), Is.True);

            Assert.That(store.Lines(), Is.EqualTo(new[]
            {
                "{\"v\":1,\"seq\":1,\"t\":\"2026-09-15T12:00:00.250Z\",\"st\":0.250,\"session\":\"s1\",\"event\":\"session_start\"," +
                "\"schema\":1,\"app_version\":\"0.1.0\",\"platform\":\"android\",\"development\":true}",
                "{\"v\":1,\"seq\":2,\"t\":\"2026-09-15T12:00:00.250Z\",\"st\":0.250,\"session\":\"s1\",\"event\":\"app_background\"," +
                "\"run_active\":false}"
            }));
            Assert.That(session.Clock, Is.SameAs(clock));

            var noFields = new TelemetrySession(new TelemetryLog(store, clock), clock, "s2");
            Assert.Throws<ArgumentException>(() => noFields.Start(new TelemetryFields().Add("schema", 5L)), "schema is the session's own.");
            Assert.That(noFields.HasStarted, Is.False);
            noFields.Start(null);
            noFields.Flush();
            Assert.That(store.Lines()[2], Does.EndWith("\"session\":\"s2\",\"event\":\"session_start\",\"schema\":1}"));
        }

        [Test]
        public void TheSequenceIncreasesStrictlyAcrossRunsAndFlushesAndRunIdsAreUnique()
        {
            var clock = new TelemetryLogTests.FakeClock();
            var store = new TelemetryLogTests.MemoryStore();
            var session = new TelemetrySession(new TelemetryLog(store, clock), clock, "abc");
            session.Start(null);
            Assert.That((session.RunId, session.RunOrdinal), Is.EqualTo(((string)null, 0)));

            var runIds = new List<string>();
            for (int run = 1; run <= 3; run++)
            {
                string runId = session.BeginRun();
                Assert.That(runId, Is.EqualTo("abc-" + run.ToString(CultureInfo.InvariantCulture)));
                Assert.That((session.RunId, session.RunOrdinal), Is.EqualTo((runId, run)));
                runIds.Add(runId);

                session.Emit("run_start", new TelemetryFields().Add("run_ordinal", run));
                clock.Advance(1);
                session.Emit("room_start");
                session.FlushIfDue();
                session.Emit("run_end");
                session.EndRun();
                Assert.That(session.RunId, Is.Null);
                session.Emit("currency_spent");
                if (run == 2)
                    session.Flush();
            }
            session.Flush();

            List<string> lines = store.Lines();
            Assert.That(lines.Count, Is.EqualTo(13));
            Assert.That(session.LastSequence, Is.EqualTo(13));
            Assert.That(store.Appends.Count, Is.GreaterThan(1), "The lines crossed several writes.");
            for (int i = 0; i < lines.Count; i++)
                Assert.That(Sequence(lines[i]), Is.EqualTo(i + 1), $"Line {i + 1} keeps the session's order.");

            Assert.That(runIds, Is.Unique);
            for (int i = 1; i < lines.Count; i++)
            {
                int run = (i - 1) / 4;
                bool inRun = (i - 1) % 4 != 3;
                Match match = RunPattern.Match(lines[i]);
                Assert.That(match.Success, Is.EqualTo(inRun), lines[i]);
                if (inRun)
                    Assert.That(match.Groups[1].Value, Is.EqualTo(runIds[run]), lines[i]);
            }

            var first = new TelemetrySession(new TelemetryLog(store, clock), clock);
            var second = new TelemetrySession(new TelemetryLog(store, clock), clock);
            Assert.That(first.SessionId, Does.Match("^[0-9a-f]{32}$"), "A random GUID, nothing from the device.");
            Assert.That(second.SessionId, Is.Not.EqualTo(first.SessionId));
            Assert.That(first.BeginRun(), Is.Not.EqualTo(second.BeginRun()), "Run ids differ across sessions.");
        }

        [Test]
        public void TelemetryDroppedIsWrittenBeforeTheNextEventAfterLinesWereLost()
        {
            var clock = new TelemetryLogTests.FakeClock();
            var store = new TelemetryLogTests.MemoryStore();
            store.Failures.Enqueue(new IOException("blocked"));
            var session = new TelemetrySession(new TelemetryLog(store, clock, 3, 3, 10), clock, "s");

            session.Start(null);
            session.Emit("room_start");
            session.Emit("first_kill");
            Assert.That(session.Flush(), Is.False, "The store is blocked.");
            session.BeginRun();
            session.Emit("chest_opened");
            Assert.That(session.Log.Dropped, Is.EqualTo(1), "session_start fell out of the full buffer.");
            Assert.That(session.Flush(), Is.True);

            session.Emit("ability_used");
            session.Emit("room_complete");
            Assert.That(session.Flush(), Is.True);

            List<string> lines = store.Lines();
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "room_start", "first_kill", "chest_opened", "telemetry_dropped", "ability_used", "room_complete"
            }), "The loss is reported once, right before the first event after it.");
            Assert.That(lines[3], Does.EndWith("\"session\":\"s\",\"run\":\"s-1\",\"event\":\"telemetry_dropped\",\"count\":1,\"failed_writes\":1}"));
            var sequences = new List<long>();
            foreach (string line in lines)
                sequences.Add(Sequence(line));
            Assert.That(sequences, Is.EqualTo(new long[] { 2, 3, 4, 5, 6, 7 }), "The gap at 1 shows where the lost line was.");
        }

        [Test]
        public void EmitForRunStampsTheGivenRunAndLeavesTheCurrentRunAlone()
        {
            var clock = new TelemetryLogTests.FakeClock();
            var store = new TelemetryLogTests.MemoryStore();
            store.Failures.Enqueue(new IOException("blocked"));
            var session = new TelemetrySession(new TelemetryLog(store, clock, 3, 3, 10), clock, "s");
            Assert.Throws<InvalidOperationException>(() => session.EmitForRun("s-1", "run_interrupted"));

            session.Start(null);
            string older = session.BeginRun();
            session.Emit("room_start");
            Assert.That(session.Flush(), Is.False, "The store is blocked.");
            string newer = session.BeginRun();
            session.Emit("run_start");
            session.Emit("first_kill");
            session.EmitForRun(older, "run_interrupted");
            Assert.That(session.RunId, Is.EqualTo(newer), "Stamping an older run does not change the current one.");
            Assert.Throws<ArgumentException>(() => session.EmitForRun(string.Empty, "run_interrupted"));
            Assert.Throws<ArgumentException>(() => session.EmitForRun(older, "RunInterrupted"));
            Assert.That(session.LastSequence, Is.EqualTo(6), "Rejected calls use no sequence number.");
            Assert.That(session.Flush(), Is.True);
            session.EmitForRun(null, "currency_spent");
            Assert.That(session.Flush(), Is.True);

            List<string> lines = store.Lines();
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "first_kill", "telemetry_dropped", "run_interrupted", "telemetry_dropped", "currency_spent"
            }), "The buffer of three kept the newest lines, and each loss is reported before the next event.");
            Assert.That(RunPattern.Match(lines[0]).Groups[1].Value, Is.EqualTo("s-2"));
            Assert.That(RunPattern.Match(lines[1]).Groups[1].Value, Is.EqualTo("s-1"), "The loss report is stamped like the event it precedes.");
            Assert.That(RunPattern.Match(lines[2]).Groups[1].Value, Is.EqualTo("s-1"));
            Assert.That(RunPattern.IsMatch(lines[3]), Is.False);
            Assert.That(RunPattern.IsMatch(lines[4]), Is.False, "A null run id writes no run key.");
            var sequences = new List<long>();
            foreach (string line in lines)
                sequences.Add(Sequence(line));
            Assert.That(sequences, Is.EqualTo(new long[] { 4, 5, 6, 7, 8 }));
            Assert.That(session.RunId, Is.EqualTo(newer));
        }

        [Test]
        public void BadNamesAndArgumentsAreRejectedWithoutUsingASequenceNumber()
        {
            var clock = new TelemetryLogTests.FakeClock();
            var log = new TelemetryLog(new TelemetryLogTests.MemoryStore(), clock);
            var session = new TelemetrySession(log, clock, "s");
            session.Start(null);

            Assert.Throws<ArgumentNullException>(() => session.Emit(null));
            Assert.Throws<ArgumentException>(() => session.Emit("RunStart"));
            Assert.Throws<ArgumentException>(() => session.Emit("run-start"));
            Assert.That(session.LastSequence, Is.EqualTo(1));
            Assert.That(log.Buffered, Is.EqualTo(1));

            Assert.Throws<ArgumentNullException>(() => new TelemetrySession(null, clock));
            Assert.Throws<ArgumentNullException>(() => new TelemetrySession(log, null));
            Assert.Throws<ArgumentException>(() => new TelemetrySession(log, clock, string.Empty));
        }

        private static long Sequence(string line)
        {
            Match match = SequencePattern.Match(line);
            Assert.That(match.Success, Is.True, line);
            return long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static List<string> Events(List<string> lines)
        {
            var events = new List<string>();
            foreach (string line in lines)
                events.Add(EventPattern.Match(line).Groups[1].Value);
            return events;
        }
    }
}
