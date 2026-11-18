using Cryptforge.Arena;

namespace Cryptforge.Analytics
{
    // The arena's session-level writer (docs/55 section 11): the run's recorder is per run and the arena is fought between
    // runs, so a fight goes straight onto the launch's one telemetry session (TelemetryRuntime, in the profile folder's
    // telemetry folder) under no run id, and is flushed at once - a fight is seconds apart from the next, and the
    // owner's session on the phone may end on the result screen. Local log only (docs/10); it never logs to the console.
    public static class ArenaTelemetryWriter
    {
        public static void Record(ArenaFightOutcome outcome)
        {
            if (outcome == null)
                return;
            TelemetrySession session = TelemetryRuntime.SessionFor(TelemetryLocation.Resolve());
            ArenaTelemetryEvent.Emit(session, outcome);
            session.Flush();
        }
    }
}
