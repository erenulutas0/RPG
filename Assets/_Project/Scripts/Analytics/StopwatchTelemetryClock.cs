using System;
using System.Diagnostics;

namespace Cryptforge.Analytics
{
    // The real clock: the system UTC time for stamps and a stopwatch for durations, which keeps counting while the app
    // is in the background and never runs backwards when the user changes the device time.
    public sealed class StopwatchTelemetryClock : ITelemetryClock
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public DateTime UtcNow => DateTime.UtcNow;
        public double Seconds => _stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
    }
}
