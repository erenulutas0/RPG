using System;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Analytics;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The log batches writes, stays bounded, and turns store IO failures into counters instead of exceptions.
    public sealed class TelemetryLogTests
    {
        [Test]
        public void TheLogFlushesOnceEnoughLinesWait()
        {
            var clock = new FakeClock();
            var store = new MemoryStore();
            var log = new TelemetryLog(store, clock, 512, 3, 10);

            log.Enqueue("a");
            log.Enqueue("b");
            Assert.That(log.FlushIfDue(), Is.False, "Two of three lines wait and no time has passed.");
            Assert.That(store.Appends, Is.Empty);

            log.Enqueue("c");
            Assert.That(log.FlushIfDue(), Is.True);
            Assert.That(store.Appends, Is.EqualTo(new[] { "a\nb\nc\n" }), "One write for the whole batch.");
            Assert.That((log.Buffered, log.Written), Is.EqualTo((0, 3L)));

            Assert.That(log.FlushIfDue(), Is.True, "An empty buffer has nothing due.");
            Assert.That(log.Flush(), Is.True);
            Assert.That(store.Appends.Count, Is.EqualTo(1), "Nothing to write writes nothing.");
        }

        [Test]
        public void TheLogFlushesOnceEnoughTimePassedSinceTheLastAttempt()
        {
            var clock = new FakeClock();
            var store = new MemoryStore();
            var log = new TelemetryLog(store, clock, 512, 64, 10);

            // Steps are exact binary fractions so the ten-second boundary is not blurred by rounding.
            log.Enqueue("a");
            clock.Advance(9.5);
            Assert.That(log.FlushIfDue(), Is.False);
            clock.Advance(0.5);
            Assert.That(log.FlushIfDue(), Is.True, "Ten seconds since the log was created.");
            Assert.That(store.Appends, Is.EqualTo(new[] { "a\n" }));

            clock.Advance(4);
            log.Enqueue("b");
            clock.Advance(5.5);
            Assert.That(log.FlushIfDue(), Is.False, "9.5 seconds since the last attempt.");
            clock.Advance(0.5);
            Assert.That(log.FlushIfDue(), Is.True);

            log.Enqueue("c");
            Assert.That(log.Flush(), Is.True, "An explicit flush does not wait.");
            Assert.That(store.Appends, Is.EqualTo(new[] { "a\n", "b\n", "c\n" }));
            Assert.That(log.Written, Is.EqualTo(3));
        }

        [Test]
        public void TheBufferDropsTheOldestLinesPastItsBoundAndCountsThem()
        {
            var store = new MemoryStore();
            var log = new TelemetryLog(store, new FakeClock(), 4, 4, 10);

            for (int i = 0; i < 10; i++)
                log.Enqueue("line" + i);

            Assert.That(log.Buffered, Is.EqualTo(4));
            Assert.That(log.Dropped, Is.EqualTo(6));
            Assert.That(log.TakeDroppedSinceLastReport(), Is.EqualTo(6));
            Assert.That(log.TakeDroppedSinceLastReport(), Is.Zero, "Taking the count resets it.");
            Assert.That(log.Dropped, Is.EqualTo(6), "The total stays.");

            Assert.That(log.Flush(), Is.True);
            Assert.That(store.Appends, Is.EqualTo(new[] { "line6\nline7\nline8\nline9\n" }));
        }

        [Test]
        public void AStoreThatFailsKTimesThenSucceedsWritesEveryRetainedLineInOrder()
        {
            const int k = 3;
            var clock = new FakeClock();
            var store = new MemoryStore();
            for (int i = 0; i < k; i++)
                store.Failures.Enqueue(new IOException("disk full " + i));
            var log = new TelemetryLog(store, clock, 5, 5, 10);

            int next = 0;
            for (int attempt = 0; attempt < k; attempt++)
            {
                log.Enqueue("line" + next++);
                log.Enqueue("line" + next++);
                Assert.That(log.Flush(), Is.False, $"Attempt {attempt + 1} fails.");
                Assert.That(log.FailedWrites, Is.EqualTo(attempt + 1));
            }
            Assert.That(log.LastError, Is.EqualTo("IOException: disk full 2"));
            Assert.That(log.Buffered, Is.EqualTo(5), "Six lines waited in a buffer of five.");
            Assert.That(log.Dropped, Is.EqualTo(1));
            Assert.That(log.Written, Is.Zero);

            log.Enqueue("line" + next);
            Assert.That(log.Flush(), Is.True);
            Assert.That(store.Appends, Is.EqualTo(new[] { "line2\nline3\nline4\nline5\nline6\n" }),
                "The oldest two were dropped; the rest arrive once, in order.");
            Assert.That(log.FailedWrites, Is.EqualTo(k));
            Assert.That((log.Buffered, log.Written, log.Dropped), Is.EqualTo((0, 5L, 2L)));
            Assert.That(log.LastError, Is.EqualTo("IOException: disk full 2"), "The last error stays readable.");
        }

        [Test]
        public void AfterAFailedWriteOnlyTheTimeTriggerRetries()
        {
            var clock = new FakeClock();
            var store = new MemoryStore();
            store.Failures.Enqueue(new UnauthorizedAccessException("read-only"));
            var log = new TelemetryLog(store, clock, 8, 2, 10);

            log.Enqueue("a");
            log.Enqueue("b");
            Assert.That(log.FlushIfDue(), Is.False, "The count trigger attempts and fails.");
            Assert.That(store.Attempts, Is.EqualTo(1));
            Assert.That(log.FailedWrites, Is.EqualTo(1), "An access failure is caught like an IO failure.");
            Assert.That(log.LastError, Is.EqualTo("UnauthorizedAccessException: read-only"));

            log.Enqueue("c");
            clock.Advance(9);
            Assert.That(log.FlushIfDue(), Is.False, "No retry every frame while the full count keeps waiting.");
            Assert.That(store.Attempts, Is.EqualTo(1));

            clock.Advance(1);
            Assert.That(log.FlushIfDue(), Is.True, "Ten seconds after the failure it retries.");
            Assert.That(store.Appends, Is.EqualTo(new[] { "a\nb\nc\n" }));

            log.Enqueue("d");
            log.Enqueue("e");
            Assert.That(log.FlushIfDue(), Is.True, "A success restores the count trigger.");
            Assert.That(store.Attempts, Is.EqualTo(3));
        }

        [Test]
        public void AnExceptionOtherThanIoPropagatesAndKeepsTheLines()
        {
            var store = new MemoryStore();
            store.Failures.Enqueue(new InvalidOperationException("a bug in the store"));
            var log = new TelemetryLog(store, new FakeClock(), 8, 8, 10);
            log.Enqueue("a");

            Assert.Throws<InvalidOperationException>(() => log.Flush());
            Assert.That(log.FailedWrites, Is.Zero, "Only IO failures are counted.");
            Assert.That(log.LastError, Is.Null);
            Assert.That(log.Buffered, Is.EqualTo(1));

            Assert.That(log.Flush(), Is.True);
            Assert.That(store.Appends, Is.EqualTo(new[] { "a\n" }));
        }

        [Test]
        public void BadLinesAndLimitsAreRejected()
        {
            var store = new MemoryStore();
            var clock = new FakeClock();
            var log = new TelemetryLog(store, clock);
            Assert.Throws<ArgumentNullException>(() => log.Enqueue(null));
            Assert.Throws<ArgumentException>(() => log.Enqueue(string.Empty));
            Assert.Throws<ArgumentException>(() => log.Enqueue("a\nb"));
            Assert.Throws<ArgumentException>(() => log.Enqueue("a\r"));
            Assert.That(log.Buffered, Is.Zero);

            Assert.Throws<ArgumentNullException>(() => new TelemetryLog(null, clock));
            Assert.Throws<ArgumentNullException>(() => new TelemetryLog(store, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryLog(store, clock, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryLog(store, clock, 8, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryLog(store, clock, 8, 9));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryLog(store, clock, 8, 8, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryLog(store, clock, 8, 8, double.NaN));
        }

        [Test]
        public void TheStopwatchClockNeverRunsBackwardsAndStampsUtc()
        {
            var clock = new StopwatchTelemetryClock();
            double first = clock.Seconds;
            double second = clock.Seconds;
            Assert.That(first, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(second, Is.GreaterThanOrEqualTo(first));
            Assert.That(clock.UtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        // A clock the test steps by hand; wall time moves with the monotonic seconds.
        internal sealed class FakeClock : ITelemetryClock
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
            public double Seconds { get; set; }

            public void Advance(double seconds)
            {
                Seconds += seconds;
                UtcNow = UtcNow.AddSeconds(seconds);
            }
        }

        // Keeps each successful append; throws the queued failures first, one per attempt.
        internal sealed class MemoryStore : ITelemetryStore
        {
            public readonly List<string> Appends = new List<string>();
            public readonly Queue<Exception> Failures = new Queue<Exception>();

            public string Location => "memory";
            public int Attempts { get; private set; }

            public void Append(string text)
            {
                Attempts++;
                if (Failures.Count > 0)
                    throw Failures.Dequeue();
                Appends.Add(text);
            }

            // Every appended line, in order, without newlines.
            public List<string> Lines()
            {
                var lines = new List<string>();
                foreach (string append in Appends)
                {
                    string[] parts = append.Split('\n');
                    Assert.That(parts[parts.Length - 1], Is.Empty, "Appended text ends with a newline.");
                    for (int i = 0; i < parts.Length - 1; i++)
                        lines.Add(parts[i]);
                }
                return lines;
            }
        }
    }
}
