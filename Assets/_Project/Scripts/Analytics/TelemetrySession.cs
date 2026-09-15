using System;
using System.Globalization;

namespace Cryptforge.Analytics
{
    // One launch of the app. It owns the random session id, the sequence counter and the current run id, stamps each
    // event into a record and hands the line to the log. The sequence never restarts within a session, so lines keep
    // their order across runs, scene restarts and file rotations, and a gap in it shows exactly where lines were lost.
    // The session id is a per-launch GUID and nothing here reads the device or the player.
    public sealed class TelemetrySession
    {
        public TelemetryLog Log { get; }
        public ITelemetryClock Clock { get; }
        public string SessionId { get; }
        // The current run's id, or null between runs.
        public string RunId { get; private set; }
        // How many runs this session began; the current or last run's ordinal.
        public int RunOrdinal { get; private set; }
        // The sequence number of the latest record, 0 before session_start.
        public long LastSequence { get; private set; }
        public bool HasStarted { get; private set; }

        public TelemetrySession(TelemetryLog log, ITelemetryClock clock, string sessionId = null)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (sessionId != null && sessionId.Length == 0)
                throw new ArgumentException("A session id is either null or not empty.", nameof(sessionId));
            SessionId = sessionId ?? Guid.NewGuid().ToString("N");
        }

        // Emits session_start with "schema" first and the given fields after it. Only the first call writes; the session
        // start is the first line of the session, so every other event waits for it (see Emit).
        public void Start(TelemetryFields fields)
        {
            if (HasStarted)
                return;

            var all = new TelemetryFields().Add("schema", TelemetryJson.SchemaVersion);
            if (fields != null)
            {
                for (int i = 0; i < fields.Count; i++)
                    all.AddToken(fields.KeyAt(i), fields.JsonValueAt(i));
            }
            HasStarted = true;
            Write("session_start", all, RunId);
        }

        // Starts a new run: "<sessionId>-<ordinal>", counting from 1 within the session.
        public string BeginRun()
        {
            RunOrdinal++;
            RunId = SessionId + "-" + RunOrdinal.ToString(CultureInfo.InvariantCulture);
            return RunId;
        }

        // Later events carry no run id until the next BeginRun.
        public void EndRun()
        {
            RunId = null;
        }

        // Stamps and enqueues one event. If the log dropped lines since the last report, telemetry_dropped goes first so
        // the loss is recorded in order. Throws when called before Start: a line ahead of session_start is a wiring bug.
        public void Emit(string name, TelemetryFields fields = null)
        {
            EmitStamped(RunId, name, fields);
        }

        // Emits under a given run id (null: outside any run) instead of the current one, without changing RunId. A scene
        // destroyed after the next scene already began its run uses it, so its run_interrupted closes its own run and
        // never lands inside the newer one.
        public void EmitForRun(string runId, string name, TelemetryFields fields = null)
        {
            if (runId != null && runId.Length == 0)
                throw new ArgumentException("A run id is either null or not empty.", nameof(runId));
            EmitStamped(runId, name, fields);
        }

        private void EmitStamped(string runId, string name, TelemetryFields fields)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!TelemetryFields.IsValidKey(name))
                throw new ArgumentException($"Telemetry event name '{name}' is not snake_case.", nameof(name));
            if (!HasStarted)
                throw new InvalidOperationException("Start the telemetry session before emitting events.");

            long dropped = Log.TakeDroppedSinceLastReport();
            if (dropped > 0)
                Write("telemetry_dropped", new TelemetryFields().Add("count", dropped).Add("failed_writes", Log.FailedWrites), runId);
            Write(name, fields, runId);
        }

        public bool Flush()
        {
            return Log.Flush();
        }

        public bool FlushIfDue()
        {
            return Log.FlushIfDue();
        }

        private void Write(string name, TelemetryFields fields, string runId)
        {
            LastSequence++;
            var record = new TelemetryRecord(LastSequence, Clock.UtcNow, Clock.Seconds, SessionId, runId, name, fields);
            Log.Enqueue(TelemetryJson.Line(record));
        }
    }
}
