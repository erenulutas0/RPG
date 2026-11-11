using System;
using UnityEngine;

namespace Cryptforge.Analytics
{
    // Holds the one telemetry session of an app launch. The gameplay scene is rebuilt on every restart, so the session
    // (its id, sequence counter and run ordinal) must outlive the scene or a restart would look like a new launch. A
    // deliberate exception to the no-static-state rule, like ProfileLocation: one session per telemetry folder, replaced
    // only when the folder changes, which in practice means a scene test pointed the profile somewhere else.
    //
    // While a gameplay scene's recorder lives (HoldFile, from RunTelemetryBridge.Attach to its OnDestroy) the session's
    // file stays open between writes (FileTelemetryStore.Hold): each flush - every level-up's among them - is then one
    // write instead of an open, a write and a close. Outside a scene the file is opened for each write, as it always was,
    // so whatever reads or removes the folder between scenes (the tests, the measurements) finds it closed.
    public static class TelemetryRuntime
    {
        private static TelemetrySession _session;
        private static FileTelemetryStore _store;
        private static string _directory;
        private static bool _quitHooked;
        private static int _holds;

        // The session writing into this folder, created (with its session_start) the first time the folder is asked for.
        // A different folder flushes the previous session first, so its buffered lines land where they belong, and lets
        // its file go.
        public static TelemetrySession SessionFor(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("A telemetry directory is required.", nameof(directory));
            if (_session != null && string.Equals(_directory, directory, StringComparison.Ordinal))
                return _session;

            _session?.Flush();
            _store?.Release();
            var clock = new StopwatchTelemetryClock();
            var store = new FileTelemetryStore(directory);
            if (_holds > 0)
                store.Hold();
            var session = new TelemetrySession(new TelemetryLog(new TimedStore(store), clock), clock);
            // Build facts only: nothing here names the device or the player.
            session.Start(new TelemetryFields()
                .Add("app_version", Application.version)
                .Add("platform", Application.platform.ToString().ToLowerInvariant())
                .Add("development", Debug.isDebugBuild));
            _session = session;
            _store = store;
            _directory = directory;

            if (!_quitHooked)
            {
                Application.quitting += FlushOnQuit;
                _quitHooked = true;
            }
            return session;
        }

        // A gameplay scene's recorder begins: the session's file stays open until every scene holding it has ended.
        public static void HoldFile()
        {
            _holds++;
            _store?.Hold();
        }

        // A gameplay scene's recorder has written its last lines: once no scene holds it, the file is closed.
        public static void ReleaseFile()
        {
            if (_holds > 0)
                _holds--;
            if (_holds == 0)
                _store?.Release();
        }

        // The file store with every write to disk timed for the perf probe (PerformanceMarkers.TelemetryWrite): the store
        // itself is pure and holds no Unity marker.
        private sealed class TimedStore : ITelemetryStore
        {
            private readonly ITelemetryStore _store;

            public TimedStore(ITelemetryStore store) => _store = store;

            public string Location => _store.Location;

            public void Append(string text)
            {
                using (Cryptforge.Core.PerformanceMarkers.TelemetryWrite.Auto())
                    _store.Append(text);
            }
        }

        // A normal quit gets one last write; a killed process relies on the flushes at choices, run ends and backgrounding.
        private static void FlushOnQuit()
        {
            _session?.Flush();
            _store?.Release();
        }

        // With domain reload disabled in the Editor, statics survive into the next Play session; a new Play session is a
        // new launch, so it starts without the previous session, its file or a second quit handler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewLaunch()
        {
            Application.quitting -= FlushOnQuit;
            _quitHooked = false;
            _store?.Release();
            _store = null;
            _holds = 0;
            _session = null;
            _directory = null;
        }
    }
}
