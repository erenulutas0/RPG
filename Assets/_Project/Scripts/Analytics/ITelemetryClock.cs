using System;

namespace Cryptforge.Analytics
{
    // Where telemetry reads time, so tests step a fake clock instead of waiting.
    public interface ITelemetryClock
    {
        // Wall-clock time for the "t" stamp; it may jump when the device clock changes.
        DateTime UtcNow { get; }

        // Monotonic seconds since the clock was created; durations and flush timing use this, never UtcNow.
        double Seconds { get; }
    }
}
