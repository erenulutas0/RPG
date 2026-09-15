using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cryptforge.Analytics
{
    // Buffers serialized lines and hands them to the store in batches, so an event costs a string and never a file
    // write. Flush() writes everything buffered in one Append; FlushIfDue() does so when enough lines wait or enough time
    // passed since the last attempt. Only IOException and UnauthorizedAccessException from the store are caught: the
    // lines stay buffered for the next attempt, and FailedWrites and LastError record it, so a full disk never reaches
    // gameplay. Any other exception is a bug and propagates. The buffer is bounded: past maxBufferedEvents the oldest line
    // is dropped and counted. Nothing here logs to a console. Used from the main thread only.
    public sealed class TelemetryLog
    {
        private readonly ITelemetryStore _store;
        private readonly ITelemetryClock _clock;
        private readonly int _maxBufferedEvents;
        private readonly int _flushEveryEvents;
        private readonly double _flushEverySeconds;
        private readonly Queue<string> _lines;
        private readonly StringBuilder _batch = new StringBuilder();
        private double _lastAttemptSeconds;
        private bool _lastAttemptFailed;
        private long _droppedSinceReport;

        public int Buffered => _lines.Count;
        public long Written { get; private set; }
        public long Dropped { get; private set; }
        public int FailedWrites { get; private set; }
        // The type and message of the most recent caught store failure; it stays after a later success.
        public string LastError { get; private set; }

        public TelemetryLog(ITelemetryStore store, ITelemetryClock clock, int maxBufferedEvents = 512,
            int flushEveryEvents = 64, double flushEverySeconds = 10)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (maxBufferedEvents < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBufferedEvents));
            // A count trigger above the bound could never be reached.
            if (flushEveryEvents < 1 || flushEveryEvents > maxBufferedEvents)
                throw new ArgumentOutOfRangeException(nameof(flushEveryEvents));
            if (!(flushEverySeconds > 0))
                throw new ArgumentOutOfRangeException(nameof(flushEverySeconds));

            _maxBufferedEvents = maxBufferedEvents;
            _flushEveryEvents = flushEveryEvents;
            _flushEverySeconds = flushEverySeconds;
            _lines = new Queue<string>(Math.Min(maxBufferedEvents, 64));
            _lastAttemptSeconds = clock.Seconds;
        }

        // Takes one serialized line without its newline.
        public void Enqueue(string line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));
            if (line.Length == 0 || line.IndexOf('\n') >= 0 || line.IndexOf('\r') >= 0)
                throw new ArgumentException("A telemetry line is one non-empty line.", nameof(line));

            if (_lines.Count >= _maxBufferedEvents)
            {
                _lines.Dequeue();
                Dropped++;
                _droppedSinceReport++;
            }
            _lines.Enqueue(line);
        }

        // Writes every buffered line in one Append. True when the buffer is empty afterwards.
        public bool Flush()
        {
            if (_lines.Count == 0)
                return true;

            _batch.Clear();
            foreach (string line in _lines)
                _batch.Append(line).Append('\n');
            string text = _batch.ToString();

            _lastAttemptSeconds = _clock.Seconds;
            // Stays set if the store throws anything, caught or not, so the count trigger backs off (see FlushIfDue).
            _lastAttemptFailed = true;
            try
            {
                _store.Append(text);
            }
            catch (IOException exception)
            {
                return RecordFailure(exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                return RecordFailure(exception);
            }

            _lastAttemptFailed = false;
            Written += _lines.Count;
            _lines.Clear();
            return true;
        }

        // Flushes when at least flushEveryEvents lines wait or flushEverySeconds passed since the last attempt. After a
        // failed attempt only the time trigger retries, so a broken folder costs one attempt per interval, not one per
        // frame while the full buffer keeps the count trigger true. True when the buffer is empty afterwards.
        public bool FlushIfDue()
        {
            if (_lines.Count == 0)
                return true;

            bool countDue = !_lastAttemptFailed && _lines.Count >= _flushEveryEvents;
            bool timeDue = _clock.Seconds - _lastAttemptSeconds >= _flushEverySeconds;
            if (!countDue && !timeDue)
                return false;
            return Flush();
        }

        // Returns and resets the number of lines dropped since the last call, for the telemetry_dropped event.
        public long TakeDroppedSinceLastReport()
        {
            long dropped = _droppedSinceReport;
            _droppedSinceReport = 0;
            return dropped;
        }

        private bool RecordFailure(Exception exception)
        {
            FailedWrites++;
            LastError = exception.GetType().Name + ": " + exception.Message;
            return false;
        }
    }
}
