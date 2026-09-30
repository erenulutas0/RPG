using System;
using System.Globalization;
using System.IO;
using System.Text;
using Cryptforge.Core;
using Cryptforge.Save;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The render-stats probe's line format, parsed from logcat, and its flag: the probe is only ever asked for in a
    // development player outside batch mode with the file present, and without the file the bootstrap creates nothing.
    // The profile points at a temporary folder, so the Editor's real development flags are never read or written.
    public sealed class RenderStatsProbeTests
    {
        private const long Mebibyte = 1024L * 1024L;
        private string _folder;

        [SetUp]
        public void PointTheProfileAtATemporaryFolder()
        {
            _folder = Path.Combine(Path.GetTempPath(), "cryptforge-render-stats-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            ProfileLocation.DirectoryOverride = _folder;
        }

        [TearDown]
        public void RestoreTheProfile()
        {
            ProfileLocation.DirectoryOverride = null;
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        [Test]
        public void AWindowLinePrintsEveryFieldWithPointsWhateverTheLanguage()
        {
            var report = new RenderStatsProbe.WindowReport
            {
                SinceStartup = 35.04f,
                Scene = "Gameplay",
                Region = "foundry",
                Frames = 296,
                WindowSeconds = 5f,
                P50Seconds = 0.01667f,
                P95Seconds = 0.01894f,
                MaxSeconds = 0.03341f,
                DrawCalls = 412.4,
                Batches = 380,
                SetPassCalls = 41.2,
                Triangles = 61234,
                Vertices = 88049,
                GcAllocatedBytes = 1234.4,
                GpuMilliseconds = 9.46,
                GpuMaxMilliseconds = 14.03,
                RenderThreadMilliseconds = 4.21,
                TotalUsedBytes = 327680000, // 312.5 MiB
                SystemUsedBytes = 512 * Mebibyte,
                GcUsedBytes = 13107200, // 12.5 MiB
                GcReservedBytes = 24 * Mebibyte,
                GfxUsedBytes = -1,
                TextureBytes = 84410368, // 80.5 MiB
                TimeScale = 1f,
            };

            string line = WithDecimalComma(() =>
            {
                var builder = new StringBuilder();
                RenderStatsProbe.AppendWindow(builder, report);
                return builder.ToString();
            });

            Assert.That(line, Is.EqualTo(
                "[RenderStats] t=35.0 scene=Gameplay region=foundry fps=59.2 ms_p50=16.7 ms_p95=18.9 ms_max=33.4 " +
                "draw=412 batches=380 setpass=41 tris=61.2k verts=88.0k gc_frame_avg=1234B gpu_ms=9.5 gpu_ms_max=14.0 " +
                "rt_ms=4.2 total=312.5MB sys=512.0MB gc_used=12.5MB gc_res=24.0MB gfx=na tex=80.5MB timescale=1.00"));
        }

        [Test]
        public void AnEmptyWindowAndMissingCountersPrintNaAndNamesKeepToOneToken()
        {
            var report = new RenderStatsProbe.WindowReport
            {
                SinceStartup = 0.5f,
                Scene = "Main Menu",
                Region = null,
                Frames = 0,
                WindowSeconds = 0f,
                DrawCalls = RenderStatsProbe.Unavailable,
                Batches = RenderStatsProbe.Unavailable,
                SetPassCalls = RenderStatsProbe.Unavailable,
                Triangles = RenderStatsProbe.Unavailable,
                Vertices = RenderStatsProbe.Unavailable,
                GcAllocatedBytes = RenderStatsProbe.Unavailable,
                GpuMilliseconds = double.NaN,
                GpuMaxMilliseconds = double.PositiveInfinity,
                RenderThreadMilliseconds = RenderStatsProbe.Unavailable,
                TotalUsedBytes = -1,
                SystemUsedBytes = -1,
                GcUsedBytes = -1,
                GcReservedBytes = -1,
                GfxUsedBytes = -1,
                TextureBytes = -1,
                TimeScale = 0f,
            };

            var builder = new StringBuilder();
            RenderStatsProbe.AppendWindow(builder, report);

            Assert.That(builder.ToString(), Is.EqualTo(
                "[RenderStats] t=0.5 scene=Main_Menu region=na fps=na ms_p50=na ms_p95=na ms_max=na draw=na batches=na " +
                "setpass=na tris=na verts=na gc_frame_avg=na gpu_ms=na gpu_ms_max=na rt_ms=na total=na sys=na gc_used=na " +
                "gc_res=na gfx=na tex=na timescale=0.00"));
        }

        [Test]
        public void TheStartLineDescribesTheDevice()
        {
            var device = new RenderStatsProbe.DeviceReport
            {
                Model = "SM-S911B",
                GraphicsApi = "Vulkan",
                Gpu = "Adreno (TM) 740",
                ScreenWidth = 1080,
                ScreenHeight = 2340,
                RefreshHz = 120.0,
                TargetFrameRate = -1,
                VSyncCount = 0,
                Quality = "Medium",
                MultithreadedRendering = true,
                FrameTiming = false,
            };

            string line = WithDecimalComma(() =>
            {
                var builder = new StringBuilder();
                RenderStatsProbe.AppendDevice(builder, device);
                return builder.ToString();
            });

            Assert.That(line, Is.EqualTo(
                "[RenderStats] start device=SM-S911B api=Vulkan gpu=Adreno_(TM)_740 screen=1080x2340 refresh_hz=120.0 " +
                "target_fps=-1 vsync=0 quality=Medium mt_render=on frame_timing=off window_s=5"));
        }

        [Test]
        public void WithoutTheFlagFileTheBootstrapCreatesNothing()
        {
            Assert.That(File.Exists(RenderStatsProbe.FlagPath), Is.False);
            int before = Resources.FindObjectsOfTypeAll<RenderStatsProbe>().Length;

            Assert.That(RenderStatsProbe.IsRequested(developmentBuild: true, batchMode: false), Is.False);
            Assert.That(RenderStatsProbe.TryInstall(), Is.Null);
            Assert.That(Resources.FindObjectsOfTypeAll<RenderStatsProbe>().Length, Is.EqualTo(before));
        }

        [Test]
        public void OnlyADevelopmentPlayerOutsideBatchModeWithTheFlagAsksForTheProbe()
        {
            string path = RenderStatsProbe.FlagPath;
            Assert.That(Path.GetDirectoryName(path), Is.EqualTo(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath)),
                "The flag sits beside the other development flags.");
            Assert.That(path, Is.EqualTo(Path.Combine(_folder, "development", "render-stats.txt")));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, string.Empty);

            Assert.That(RenderStatsProbe.IsRequested(developmentBuild: true, batchMode: false), Is.True,
                "Any file, even an empty one, turns it on.");
            Assert.That(RenderStatsProbe.IsRequested(developmentBuild: false, batchMode: false), Is.False,
                "A release build never runs it.");
            Assert.That(RenderStatsProbe.IsRequested(developmentBuild: true, batchMode: true), Is.False,
                "A batch-mode test run never runs it.");
        }

        // Formats under a culture whose decimal separator is a comma, as a phone set to Turkish would.
        private static string WithDecimalComma(Func<string> format)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            var comma = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            comma.NumberFormat.NumberDecimalSeparator = ",";
            comma.NumberFormat.NumberGroupSeparator = ".";
            try
            {
                CultureInfo.CurrentCulture = comma;
                return format();
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
