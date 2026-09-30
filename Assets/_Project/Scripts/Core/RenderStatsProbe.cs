using System;
using System.IO;
using System.Text;
using Cryptforge.Save;
using Cryptforge.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cryptforge.Core
{
    // Measures what the renderer does on a device: per frame draw calls, batches, SetPass calls, triangles and vertices,
    // the GPU and render-thread frame times, the frame-time percentiles, garbage per frame and memory in use, as one
    // logcat line every five seconds of real time. It runs only in the Editor or a development player (Debug.isDebugBuild)
    // and only when the development flags folder holds a file named render-stats.txt (any content); a release build never
    // looks, and a development run without the file creates and allocates nothing. It reads counters and logs; it changes
    // no gameplay, timing, save or telemetry. A batch-mode Editor (Tools/Run-UnityTests.ps1) never starts it, so a flag
    // left in the Editor's folder cannot put lines into scene tests that assert no unexpected logs; the interactive Test
    // Runner is not protected, so remove the Editor flag before running PlayMode suites there. FrameTimeProbe ("[Perf]")
    // keeps measuring main-thread time and wave spawns beside it; the two log independently.
    //
    // The flag lives beside the other development flags, <persistentDataPath>/development/, read once at startup:
    //   on      adb shell "mkdir -p /sdcard/Android/data/com.cryptforge.prototype/files/development && touch /sdcard/Android/data/com.cryptforge.prototype/files/development/render-stats.txt"
    //           then adb shell am force-stop com.cryptforge.prototype and launch the app again
    //   read    adb logcat -v time -s Unity:V | findstr /L "[RenderStats]"   (/L, or findstr reads the brackets as a class)
    //   off     adb shell rm /sdcard/Android/data/com.cryptforge.prototype/files/development/render-stats.txt, then restart
    //
    // One "start" line describes the device, then each window writes key=value tokens separated by single spaces, numbers
    // in the invariant culture, na for a counter this player does not provide:
    //   [RenderStats] t=35.0 scene=Gameplay region=foundry fps=59.2 ms_p50=16.7 ms_p95=18.9 ms_max=33.4 draw=412
    //   batches=380 setpass=41 tris=61.2k verts=88.0k gc_frame_avg=1234B gpu_ms=9.5 gpu_ms_max=14.0 rt_ms=4.2
    //   total=312.5MB sys=512.0MB gc_used=12.5MB gc_res=24.0MB gfx=na tex=80.5MB timescale=1.00
    // Render counts, garbage and GPU/render-thread times are per-frame averages over the window (gpu_ms_max its longest);
    // memory is the value at the report, in MiB; k is thousands. The report frame itself allocates the line, the scene
    // name and the log call, so every window's garbage includes a kilobyte or two of this probe's own (and FrameTimeProbe's).
    [AddComponentMenu("")]
    public sealed class RenderStatsProbe : MonoBehaviour
    {
        public const string Tag = "[RenderStats]";
        public const string FlagFileName = "render-stats.txt";
        public const float WindowSeconds = 5f;
        // Any negative or non-finite number in a report is a counter this player did not provide, printed as na.
        public const double Unavailable = -1d;
        private const double Mebibyte = 1024d * 1024d;

        // One window's numbers as the probe hands them to the formatter.
        public struct WindowReport
        {
            public float SinceStartup;
            public string Scene;
            public string Region;
            public int Frames;
            public float WindowSeconds;
            public float P50Seconds;
            public float P95Seconds;
            public float MaxSeconds;
            // Per-frame averages over the window.
            public double DrawCalls;
            public double Batches;
            public double SetPassCalls;
            public double Triangles;
            public double Vertices;
            public double GcAllocatedBytes;
            public double GpuMilliseconds;
            public double GpuMaxMilliseconds;
            public double RenderThreadMilliseconds;
            // Bytes in use at the report.
            public long TotalUsedBytes;
            public long SystemUsedBytes;
            public long GcUsedBytes;
            public long GcReservedBytes;
            public long GfxUsedBytes;
            public long TextureBytes;
            public float TimeScale;
        }

        // What the start line records about the player once.
        public struct DeviceReport
        {
            public string Model;
            public string GraphicsApi;
            public string Gpu;
            public int ScreenWidth;
            public int ScreenHeight;
            public double RefreshHz;
            public int TargetFrameRate;
            public int VSyncCount;
            public string Quality;
            public bool MultithreadedRendering;
            public bool FrameTiming;
        }

        // A render or garbage counter averaged over the window. A recorder can be valid for a counter this player never
        // writes, so a frame counts only once the recorder has held a sample.
        private sealed class WindowCounter
        {
            private ProfilerRecorder _recorder;
            private double _sum;
            private int _samples;

            public WindowCounter(ProfilerCategory category, string name) =>
                _recorder = ProfilerRecorder.StartNew(category, name);

            public double Average => _samples > 0 ? _sum / _samples : Unavailable;

            public void Sample()
            {
                if (!_recorder.Valid || _recorder.Count == 0)
                    return;
                _sum += _recorder.LastValue;
                _samples++;
            }

            public void Reset()
            {
                _sum = 0d;
                _samples = 0;
            }

            public void Dispose() => _recorder.Dispose();
        }

        // Up to 400 fps over a five-second window keeps every frame for the percentiles.
        private readonly FrameStats _frames = new FrameStats(2048);
        private readonly StringBuilder _line = new StringBuilder(512);
        private readonly FrameTiming[] _timing = new FrameTiming[1];
        private WindowCounter _drawCalls;
        private WindowCounter _batches;
        private WindowCounter _setPassCalls;
        private WindowCounter _triangles;
        private WindowCounter _vertices;
        private WindowCounter _gcAllocated;
        private WindowCounter[] _counters;
        private ProfilerRecorder _totalUsed;
        private ProfilerRecorder _systemUsed;
        private ProfilerRecorder _gcUsed;
        private ProfilerRecorder _gcReserved;
        private ProfilerRecorder _gfxUsed;
        private ProfilerRecorder _textureMemory;
        private bool _frameTiming;
        private double _gpuSum;
        private double _gpuMax;
        private int _gpuSamples;
        private double _renderThreadSum;
        private int _renderThreadSamples;
        private float _windowSeconds;
        private bool _skipFrame;
        private ArenaView _arena;

        public static string FlagPath => Path.Combine(ProfileLocation.Resolve(), DevelopmentStart.FolderName, FlagFileName);

        // True when a player with these properties runs the probe. The flag path is only built, and the file only looked
        // for, in a development build outside batch mode, so a release build pays one branch.
        public static bool IsRequested(bool developmentBuild, bool batchMode) =>
            developmentBuild && !batchMode && File.Exists(FlagPath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install() => TryInstall();

        // The bootstrap: the running probe, a new one when the flag asks for it, or null and nothing created. Public for
        // tests only; game code never calls it.
        public static RenderStatsProbe TryInstall()
        {
            if (!IsRequested(Debug.isDebugBuild, Application.isBatchMode))
                return null;

            var existing = FindAnyObjectByType<RenderStatsProbe>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;
            var probe = new GameObject("Render Stats Probe") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(probe);
            return probe.AddComponent<RenderStatsProbe>();
        }

        private void OnEnable()
        {
            _drawCalls = new WindowCounter(ProfilerCategory.Render, "Draw Calls Count");
            _batches = new WindowCounter(ProfilerCategory.Render, "Batches Count");
            _setPassCalls = new WindowCounter(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = new WindowCounter(ProfilerCategory.Render, "Triangles Count");
            _vertices = new WindowCounter(ProfilerCategory.Render, "Vertices Count");
            _gcAllocated = new WindowCounter(ProfilerCategory.Memory, "GC Allocated In Frame");
            _counters = new[] { _drawCalls, _batches, _setPassCalls, _triangles, _vertices, _gcAllocated };
            _totalUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _systemUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            _gcUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
            _gcReserved = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            _gfxUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Gfx Used Memory");
            _textureMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
            _frameTiming = FrameTimingManager.IsFeatureEnabled();
            ResetWindow();
        }

        // OnDisable also runs when the probe is destroyed, so every recorder is released either way.
        private void OnDisable()
        {
            for (int i = 0; i < _counters.Length; i++)
                _counters[i].Dispose();
            _totalUsed.Dispose();
            _systemUsed.Dispose();
            _gcUsed.Dispose();
            _gcReserved.Dispose();
            _gfxUsed.Dispose();
            _textureMemory.Dispose();
        }

        // Start, not OnEnable: the scene's own setup has run by then, so the target frame rate is the one it chose.
        private void Start()
        {
            var device = new DeviceReport
            {
                Model = SystemInfo.deviceModel,
                GraphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                Gpu = SystemInfo.graphicsDeviceName,
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                RefreshHz = Screen.currentResolution.refreshRateRatio.value,
                TargetFrameRate = Application.targetFrameRate,
                VSyncCount = QualitySettings.vSyncCount,
                Quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                MultithreadedRendering = SystemInfo.graphicsMultiThreaded,
                FrameTiming = _frameTiming,
            };
            _line.Clear();
            AppendDevice(_line, device);
            Write(_line.ToString());
        }

        // Allocation-free: numbers only accumulate here, and the line is built at the report. The unscaled delta and every
        // recorder's last value describe the frame before this one.
        private void Update()
        {
            float frame = Time.unscaledDeltaTime;
            if (_skipFrame)
            {
                _skipFrame = false;
                return;
            }

            _frames.Add(frame);
            _windowSeconds += frame;
            for (int i = 0; i < _counters.Length; i++)
                _counters[i].Sample();
            if (_frameTiming)
                SampleFrameTiming();

            if (_windowSeconds >= WindowSeconds)
                WriteWindow();
        }

        // The frame after a return from the background spans the time away, and the window around it says nothing about
        // rendering, so both are dropped.
        private void OnApplicationPause(bool paused)
        {
            ResetWindow();
            _skipFrame = true;
        }

        // The GPU result arrives a few frames late and may repeat; a zero means the device reported no timing for it.
        private void SampleFrameTiming()
        {
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timing) == 0)
                return;
            double gpu = _timing[0].gpuFrameTime;
            if (gpu > 0d)
            {
                _gpuSum += gpu;
                _gpuSamples++;
                if (gpu > _gpuMax)
                    _gpuMax = gpu;
            }
            double renderThread = _timing[0].cpuRenderThreadFrameTime;
            if (renderThread > 0d)
            {
                _renderThreadSum += renderThread;
                _renderThreadSamples++;
            }
        }

        private void WriteWindow()
        {
            var report = new WindowReport
            {
                SinceStartup = Time.unscaledTime,
                Scene = SceneManager.GetActiveScene().name,
                Region = ActiveRegion(),
                Frames = _frames.Count,
                WindowSeconds = _windowSeconds,
                P50Seconds = _frames.Percentile(0.5f),
                P95Seconds = _frames.Percentile(0.95f),
                MaxSeconds = _frames.MaxSeconds,
                DrawCalls = _drawCalls.Average,
                Batches = _batches.Average,
                SetPassCalls = _setPassCalls.Average,
                Triangles = _triangles.Average,
                Vertices = _vertices.Average,
                GcAllocatedBytes = _gcAllocated.Average,
                GpuMilliseconds = _gpuSamples > 0 ? _gpuSum / _gpuSamples : Unavailable,
                GpuMaxMilliseconds = _gpuSamples > 0 ? _gpuMax : Unavailable,
                RenderThreadMilliseconds = _renderThreadSamples > 0 ? _renderThreadSum / _renderThreadSamples : Unavailable,
                TotalUsedBytes = LastBytes(_totalUsed),
                SystemUsedBytes = LastBytes(_systemUsed),
                GcUsedBytes = LastBytes(_gcUsed),
                GcReservedBytes = LastBytes(_gcReserved),
                GfxUsedBytes = LastBytes(_gfxUsed),
                TextureBytes = LastBytes(_textureMemory),
                TimeScale = Time.timeScale,
            };
            _line.Clear();
            AppendWindow(_line, report);
            Write(_line.ToString());
            ResetWindow();
        }

        private void ResetWindow()
        {
            _frames.Clear();
            _windowSeconds = 0f;
            if (_counters != null)
            {
                for (int i = 0; i < _counters.Length; i++)
                    _counters[i].Reset();
            }
            _gpuSum = 0d;
            _gpuMax = 0d;
            _gpuSamples = 0;
            _renderThreadSum = 0d;
            _renderThreadSamples = 0;
        }

        // Which arena the window drew, looked up once per report and kept until its scene unloads. ArenaView resolves its
        // region once from the development flags, so reading it here does no file IO after the arena's own Awake.
        private string ActiveRegion()
        {
            if (_arena == null)
                _arena = FindFirstObjectByType<ArenaView>();
            if (_arena == null)
                return "none";
            return _arena.IsFoundryRegion ? "foundry" : _arena.IsWideRegionPreview ? "wide" : "hall";
        }

        private static long LastBytes(in ProfilerRecorder recorder) =>
            recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1L;

        private static void Write(string message) =>
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);

        // The window line. Pure, so the format is tested without a device; digits are written by hand, so a device set
        // to a language with a decimal comma (Turkish) still prints points.
        public static void AppendWindow(StringBuilder line, in WindowReport report)
        {
            bool frames = report.Frames > 0 && report.WindowSeconds > 0f;
            line.Append(Tag);
            AppendNumber(line, "t", report.SinceStartup, 1, null);
            AppendWord(line, "scene", report.Scene);
            AppendWord(line, "region", report.Region);
            AppendNumber(line, "fps", frames ? report.Frames / (double)report.WindowSeconds : Unavailable, 1, null);
            AppendNumber(line, "ms_p50", frames ? report.P50Seconds * 1000d : Unavailable, 1, null);
            AppendNumber(line, "ms_p95", frames ? report.P95Seconds * 1000d : Unavailable, 1, null);
            AppendNumber(line, "ms_max", frames ? report.MaxSeconds * 1000d : Unavailable, 1, null);
            AppendNumber(line, "draw", report.DrawCalls, 0, null);
            AppendNumber(line, "batches", report.Batches, 0, null);
            AppendNumber(line, "setpass", report.SetPassCalls, 0, null);
            AppendNumber(line, "tris", Thousands(report.Triangles), 1, "k");
            AppendNumber(line, "verts", Thousands(report.Vertices), 1, "k");
            AppendNumber(line, "gc_frame_avg", report.GcAllocatedBytes, 0, "B");
            AppendNumber(line, "gpu_ms", report.GpuMilliseconds, 1, null);
            AppendNumber(line, "gpu_ms_max", report.GpuMaxMilliseconds, 1, null);
            AppendNumber(line, "rt_ms", report.RenderThreadMilliseconds, 1, null);
            AppendMemory(line, "total", report.TotalUsedBytes);
            AppendMemory(line, "sys", report.SystemUsedBytes);
            AppendMemory(line, "gc_used", report.GcUsedBytes);
            AppendMemory(line, "gc_res", report.GcReservedBytes);
            AppendMemory(line, "gfx", report.GfxUsedBytes);
            AppendMemory(line, "tex", report.TextureBytes);
            AppendNumber(line, "timescale", report.TimeScale, 2, null);
        }

        // The start line, written once.
        public static void AppendDevice(StringBuilder line, in DeviceReport device)
        {
            line.Append(Tag).Append(" start");
            AppendWord(line, "device", device.Model);
            AppendWord(line, "api", device.GraphicsApi);
            AppendWord(line, "gpu", device.Gpu);
            line.Append(" screen=");
            AppendInteger(line, device.ScreenWidth);
            line.Append('x');
            AppendInteger(line, device.ScreenHeight);
            AppendNumber(line, "refresh_hz", device.RefreshHz, 1, null);
            line.Append(" target_fps=");
            AppendInteger(line, device.TargetFrameRate);
            line.Append(" vsync=");
            AppendInteger(line, device.VSyncCount);
            AppendWord(line, "quality", device.Quality);
            AppendWord(line, "mt_render", device.MultithreadedRendering ? "on" : "off");
            AppendWord(line, "frame_timing", device.FrameTiming ? "on" : "off");
            AppendNumber(line, "window_s", WindowSeconds, 0, null);
        }

        private static double Thousands(double count) => count < 0d ? count : count / 1000d;

        private static void AppendMemory(StringBuilder line, string key, long bytes) =>
            AppendNumber(line, key, bytes < 0L ? Unavailable : bytes / Mebibyte, 1, "MB");

        // A value with no spaces, so every token stays key=value; a missing one is na.
        private static void AppendWord(StringBuilder line, string key, string value)
        {
            line.Append(' ').Append(key).Append('=');
            if (string.IsNullOrEmpty(value))
            {
                line.Append("na");
                return;
            }
            for (int i = 0; i < value.Length; i++)
                line.Append(char.IsWhiteSpace(value[i]) ? '_' : value[i]);
        }

        // A non-negative value rounded half away from zero to 0, 1 or 2 decimals, then its unit; na for anything
        // negative or not finite.
        private static void AppendNumber(StringBuilder line, string key, double value, int decimals, string unit)
        {
            line.Append(' ').Append(key).Append('=');
            long scale = decimals >= 2 ? 100L : decimals == 1 ? 10L : 1L;
            double scaled = Math.Round(value * scale, MidpointRounding.AwayFromZero);
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || scaled >= long.MaxValue)
            {
                line.Append("na");
                return;
            }

            long units = (long)scaled;
            AppendDigits(line, units / scale);
            if (scale > 1L)
            {
                line.Append('.');
                long fraction = units % scale;
                for (long digit = scale / 10L; digit > 0L; digit /= 10L)
                    line.Append((char)('0' + (int)(fraction / digit % 10L)));
            }
            if (unit != null)
                line.Append(unit);
        }

        private static void AppendInteger(StringBuilder line, int value)
        {
            long magnitude = value;
            if (magnitude < 0L)
            {
                line.Append('-');
                magnitude = -magnitude;
            }
            AppendDigits(line, magnitude);
        }

        private static void AppendDigits(StringBuilder line, long value)
        {
            if (value >= 10L)
                AppendDigits(line, value / 10L);
            line.Append((char)('0' + (int)(value % 10L)));
        }
    }
}
