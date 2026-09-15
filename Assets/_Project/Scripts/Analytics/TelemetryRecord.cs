using System;

namespace Cryptforge.Analytics
{
    // One line of the log as stamped by the session: where it sits in the session's order, when it happened by the wall
    // clock and by the monotonic session clock, which session and run it belongs to, and what happened.
    public readonly struct TelemetryRecord
    {
        public long Sequence { get; }
        public DateTime UtcTime { get; }
        public double SessionSeconds { get; }
        public string SessionId { get; }
        // Null outside a run; the line then has no "run" key.
        public string RunId { get; }
        public string Name { get; }
        // Null reads as no fields.
        public TelemetryFields Fields { get; }

        public TelemetryRecord(long sequence, DateTime utcTime, double sessionSeconds, string sessionId, string runId,
            string name, TelemetryFields fields)
        {
            if (sequence < 1)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            if (sessionId == null)
                throw new ArgumentNullException(nameof(sessionId));
            if (sessionId.Length == 0)
                throw new ArgumentException("A telemetry record needs a session id.", nameof(sessionId));
            if (runId != null && runId.Length == 0)
                throw new ArgumentException("A run id is either null or not empty.", nameof(runId));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!TelemetryFields.IsValidKey(name))
                throw new ArgumentException($"Telemetry event name '{name}' is not snake_case.", nameof(name));

            Sequence = sequence;
            UtcTime = utcTime;
            SessionSeconds = sessionSeconds;
            SessionId = sessionId;
            RunId = runId;
            Name = name;
            Fields = fields;
        }
    }
}
