using System;
using UnityEngine;

namespace Cryptforge.Analytics
{
    // Holds the one telemetry session of an app launch. The gameplay scene is rebuilt on every restart, so the session
    // (its id, sequence counter and run ordinal) must outlive the scene or a restart would look like a new launch. A
    // deliberate exception to the no-static-state rule, like ProfileLocation: one session per telemetry folder, replaced
    // only when the folder changes, which in practice means a scene test pointed the profile somewhere else.
    public static class TelemetryRuntime
    {
        private static TelemetrySession _session;
        private static string _directory;
        private static bool _quitHooked;

        // The session writing into this folder, created (with its session_start) the first time the folder is asked for.
        // A different folder flushes the previous session first, so its buffered lines land where they belong.
        public static TelemetrySession SessionFor(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("A telemetry directory is required.", nameof(directory));
            if (_session != null && string.Equals(_directory, directory, StringComparison.Ordinal))
                return _session;

            _session?.Flush();
            var clock = new StopwatchTelemetryClock();
            var session = new TelemetrySession(new TelemetryLog(new FileTelemetryStore(directory), clock), clock);
            // Build facts only: nothing here names the device or the player.
            session.Start(new TelemetryFields()
                .Add("app_version", Application.version)
                .Add("platform", Application.platform.ToString().ToLowerInvariant())
                .Add("development", Debug.isDebugBuild));
            _session = session;
            _directory = directory;

            if (!_quitHooked)
            {
                Application.quitting += FlushOnQuit;
                _quitHooked = true;
            }
            return session;
        }

        // A normal quit gets one last write; a killed process relies on the flushes at choices, run ends and backgrounding.
        private static void FlushOnQuit()
        {
            _session?.Flush();
        }

        // With domain reload disabled in the Editor, statics survive into the next Play session; a new Play session is a
        // new launch, so it starts without the previous session or a second quit handler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewLaunch()
        {
            Application.quitting -= FlushOnQuit;
            _quitHooked = false;
            _session = null;
            _directory = null;
        }
    }
}
