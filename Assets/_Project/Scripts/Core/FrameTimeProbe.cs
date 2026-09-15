using System;
using Cryptforge.Combat;
using Unity.Profiling;
using UnityEngine;

namespace Cryptforge.Core
{
    // Measures a development build on a device. Every five seconds of real time it logs the window's frame times, the
    // longest main-thread frame, the managed memory allocated and collected and the memory in use; it logs each wave that
    // enters with what it cost, and any other frame that ran past a 30 Hz deadline. It installs itself only in a
    // development player, never in the Editor or a release build, and draws nothing. Read it with
    // adb logcat -s Unity and the "[Perf]" tag; numbers use the invariant culture.
    public sealed class FrameTimeProbe : MonoBehaviour
    {
        public const string Tag = "[Perf]";
        private const float WindowSeconds = 5f;
        // A frame past the first missed a 60 Hz deadline; past the second it missed a 30 Hz deadline too.
        private const float SlowFrameSeconds = 0.020f;
        private const float LongFrameSeconds = 0.035f;

        private readonly FrameStats _frames = new FrameStats(4096);
        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _allocatedInFrame;
        private ProfilerRecorder _managedInUse;
        private ProfilerRecorder _totalInUse;
        private ProfilerRecorder _startWave;
        private int _window;
        private float _windowSeconds;
        private long _windowAllocated;
        private long _windowMostAllocated;
        private long _windowLongestMainThread;
        private int _collections;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
                return;
            var probe = new GameObject("Frame Time Probe");
            DontDestroyOnLoad(probe);
            probe.AddComponent<FrameTimeProbe>();
        }

        private void OnEnable()
        {
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            _allocatedInFrame = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _managedInUse = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
            _totalInUse = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _startWave = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, PerformanceMarkers.StartWaveName);
            _collections = GC.CollectionCount(0);
            Write(FormattableString.Invariant(
                $"{Tag} probe on: {SystemInfo.deviceModel}, {Screen.width}x{Screen.height} at {Screen.currentResolution.refreshRateRatio.value:0} Hz, target {Application.targetFrameRate} fps; recorders main thread {_mainThread.Valid}, allocated {_allocatedInFrame.Valid}, wave {_startWave.Valid}"));
        }

        private void OnDisable()
        {
            _mainThread.Dispose();
            _allocatedInFrame.Dispose();
            _managedInUse.Dispose();
            _totalInUse.Dispose();
            _startWave.Dispose();
        }

        // The unscaled delta and every recorder's last value describe the frame before this one.
        private void Update()
        {
            float frame = Time.unscaledDeltaTime;
            long mainThread = _mainThread.Valid ? _mainThread.LastValue : 0;
            long allocated = _allocatedInFrame.Valid ? _allocatedInFrame.LastValue : 0;
            long wave = _startWave.Valid ? _startWave.LastValue : 0;
            _frames.Add(frame);
            _windowSeconds += frame;
            _windowAllocated += allocated;
            _windowMostAllocated = Math.Max(_windowMostAllocated, allocated);
            _windowLongestMainThread = Math.Max(_windowLongestMainThread, mainThread);

            if (wave > 0)
            {
                var encounters = FindFirstObjectByType<EncounterController>();
                int enemies = encounters != null ? encounters.WaveEnemyCount : 0;
                Write(FormattableString.Invariant(
                    $"{Tag} wave: {enemies} enemies, StartWave {Milliseconds(wave):0.00} ms, frame {frame * 1000f:0.0} ms, main thread {Milliseconds(mainThread):0.0} ms, allocated {allocated / 1024f:0.0} KB"));
            }
            else if (frame > LongFrameSeconds)
            {
                Write(FormattableString.Invariant(
                    $"{Tag} long frame {frame * 1000f:0.0} ms, main thread {Milliseconds(mainThread):0.0} ms, allocated {allocated / 1024f:0.0} KB, time scale {Time.timeScale:0}"));
            }

            if (_windowSeconds >= WindowSeconds)
                WriteWindow();
        }

        private void WriteWindow()
        {
            _window++;
            int collections = GC.CollectionCount(0);
            Write(FormattableString.Invariant(
                $"{Tag} window {_window}: {_frames.Count} frames in {_windowSeconds:0.00} s ({_frames.Count / _windowSeconds:0.0} fps), avg {_frames.AverageSeconds * 1000f:0.0} ms, p50 {_frames.Percentile(0.5f) * 1000f:0.0}, p95 {_frames.Percentile(0.95f) * 1000f:0.0}, p99 {_frames.Percentile(0.99f) * 1000f:0.0}, max {_frames.MaxSeconds * 1000f:0.0} ms, over 20 ms {_frames.CountOver(SlowFrameSeconds)}, over 35 ms {_frames.CountOver(LongFrameSeconds)}; main thread max {Milliseconds(_windowLongestMainThread):0.0} ms; allocated {_windowAllocated / 1024f:0.0} KB, most in a frame {_windowMostAllocated / 1024f:0.0} KB, GC runs {collections - _collections}; managed {Megabytes(_managedInUse):0.0} MB, total {Megabytes(_totalInUse):0.0} MB; time scale {Time.timeScale:0}"));

            _collections = collections;
            _frames.Clear();
            _windowSeconds = 0f;
            _windowAllocated = 0;
            _windowMostAllocated = 0;
            _windowLongestMainThread = 0;
        }

        private static void Write(string message) =>
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);

        private static float Milliseconds(long nanoseconds) => nanoseconds / 1e6f;

        private static float Megabytes(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue / (1024f * 1024f) : 0f;
    }
}
