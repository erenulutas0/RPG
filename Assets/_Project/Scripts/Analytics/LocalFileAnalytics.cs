using System;

namespace Cryptforge.Analytics
{
    // The local file log as an IAnalytics sink: the existing telemetry writer, unchanged, behind the seam. Every call is
    // the session's own (TelemetrySession.Emit, EmitForRun, Flush), so a line written through here is byte for byte the
    // line the session writes when called directly - the same envelope, sequence, clock and run stamp - into the same
    // events.jsonl (TelemetryRuntime.SessionFor(TelemetryLocation.Resolve()) in the game). The session must have started,
    // as for any direct call.
    public sealed class LocalFileAnalytics : IAnalytics
    {
        public TelemetrySession Session { get; }

        public LocalFileAnalytics(TelemetrySession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public void Emit(string name, TelemetryFields fields = null) => Session.Emit(name, fields);

        public void EmitForRun(string runId, string name, TelemetryFields fields = null) =>
            Session.EmitForRun(runId, name, fields);

        public bool Flush() => Session.Flush();
    }
}
