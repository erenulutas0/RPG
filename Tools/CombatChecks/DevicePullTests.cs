using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The gate's two device rows on two sample pulls (DeviceSamples/, written in the formats the phone writes: RunTelemetry's
    // lines, FrameTimeProbe's and RenderStatsProbe's logcat lines, the pull script's pull.txt). The older pull is the old
    // APK's, its log in plain -v time (the phone's local time, as the device sessions before the script kept it); the
    // newer was pulled after a 17:00 UTC install and holds the tail of the old session (its head rotated away), the
    // owner's four runs and the density probe's run, its log in -v time -v epoch as Tools/Device/Pull-DeviceEvidence.ps1
    // dumps it. docs/23 (2026-11-11) has the session they stand for.
    public sealed class DevicePullTests
    {
        private const string Older = "device-20261112-163000";
        private const string Newer = "device-20261112-182500";

        private static string Samples()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            Assert.That(directory, Is.Not.Null, "the repository holding Assets/_Project");
            return Path.Combine(directory, "Tools", "CombatChecks", "DeviceSamples");
        }

        private static FoundryGate.DeviceEvidence ReadBoth() =>
            DevicePull.Read(DevicePull.Folders(Samples(), null));

        [Test]
        public void TheDefaultReadsOnlyThePullScriptsFolders()
        {
            Assert.That(DevicePull.Folders(Samples(), null).Select(Path.GetFileName), Is.EqualTo(new[] { Older, Newer }), "oldest first");
            Assert.That(DevicePull.Folders(Samples(), "none"), Is.Empty);
            Assert.That(DevicePull.Folders(Samples(), Newer).Single(), Is.EqualTo(Path.Combine(Samples(), Newer)), "within the Editor folder");
            Assert.That(DevicePull.Read(DevicePull.Folders(Samples(), "none")), Is.Null, "nothing pulled, nothing read");
            Assert.Throws<DirectoryNotFoundException>(() => DevicePull.Folders(Samples(), "device-missing"));

            // The device sessions' folders from before the script hold no pull.txt, telemetry or not: never read by default.
            string root = Path.Combine(Path.GetTempPath(), "cryptforge-device-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "device-telemetry", "telemetry"));
                File.WriteAllText(Path.Combine(root, "device-telemetry", "telemetry", "events.jsonl"), "{}\n");
                Directory.CreateDirectory(Path.Combine(root, "device-perf"));
                Assert.That(DevicePull.Folders(root, null), Is.Empty);
                Directory.CreateDirectory(Path.Combine(root, "device-20261113-090000"));
                File.WriteAllText(Path.Combine(root, "device-20261113-090000", DevicePull.ManifestName), "pulled_utc: 2026-11-13T09:00:00Z\n");
                Assert.That(DevicePull.Folders(root, null).Select(Path.GetFileName), Is.EqualTo(new[] { "device-20261113-090000" }));
                Assert.That(DevicePull.Folders(Path.Combine(root, "missing"), null), Is.Empty, "no Editor folder, no pull");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        // A line two pulls share is read once: the old session's last three lines are in both pulls.
        [Test]
        public void TheTelemetryOfEveryPullIsReadOnce()
        {
            List<DevicePull.TelemetryEvent> events = DevicePull.ReadEvents(new[] { Older, Newer }.Select(p => Path.Combine(Samples(), p, "telemetry")));
            Assert.That(events.Count, Is.EqualTo(51));
            Assert.That(events.Where(e => e.Session == "5f0c2d9e8b7a4c31a2e6d0f4b9c8e712").Select(e => e.Seq), Is.EqualTo(new long[] { 1, 2, 3, 4, 5, 6 }));

            List<DevicePull.RunRecord> runs = DevicePull.Runs(events);
            Assert.That(runs.Count, Is.EqualTo(6));
            DevicePull.RunRecord old = runs[0];
            Assert.That(old.HeroId, Is.EqualTo(FoundryGate.V01HeroId));
            Assert.That(old.DiedFloor, Is.EqualTo(2), "the old run died on floor 2");
            Assert.That(old.DiedSeconds, Is.EqualTo(420.5f - 301.25f).Within(1e-3f));
            DevicePull.RunRecord probe = runs[5];
            Assert.That(probe.Probe, Is.True, "horde-probe.txt's hero, a million health more");
            Assert.That(probe.Result, Is.EqualTo("interrupted"));
            Assert.That(probe.OpeningEnd, Is.EqualTo(new DateTime(2026, 11, 12, 18, 20, 1, 20, DateTimeKind.Utc)), "its first wave and a second");
            Assert.That(probe.Samples.Select(s => s.Alive).Take(3), Is.EqualTo(new[] { 41, 41, 40 }), "horde_alive and wave_alive");

            // Read alone, the newer pull's old run has lost its start: no hero to read it under.
            List<DevicePull.RunRecord> alone = DevicePull.Runs(DevicePull.ReadEvents(new[] { Path.Combine(Samples(), Newer, "telemetry") }));
            Assert.That(alone.First().Started, Is.False);
        }

        [Test]
        public void TheOwnersFloorsAreTheInstalledApksRunsByHeroAndFloor()
        {
            FoundryGate.DeviceEvidence device = ReadBoth();
            Assert.That(device.Source, Is.EqualTo(Newer + " and 1 earlier pull"));
            Assert.That(device.Model, Is.EqualTo("SM-S911B"));
            Assert.That(device.ApkSha256, Does.StartWith("9c1e5b7a"), "the newest pull's APK");
            Assert.That(device.InstalledUtc, Is.EqualTo(new DateTime(2026, 11, 12, 17, 0, 0, DateTimeKind.Utc)));
            Assert.That(device.Flags, Is.EqualTo(new[] { "horde-probe.txt", "render-stats.txt" }));

            Assert.That(device.Runs, Is.EqualTo(4), "the owner's four runs after the install");
            Assert.That(device.EarlierRuns, Is.EqualTo(1), "the old APK's run");
            Assert.That(device.ProbeRuns, Is.EqualTo(1), "the density probe's");
            Assert.That(device.UnfinishedFloors, Is.EqualTo(1), "the fourth run's floor 2, the app closed on it");
            string Floors(string hero, int floor) => string.Join(" ", device.Floors.Where(f => f.HeroId == hero && f.Floor == floor)
                .Select(f => (f.Cleared ? "cleared " : "died ") + f.Seconds.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(Floors(FoundryGate.V01HeroId, 1), Is.EqualTo("cleared 310.5 died 150.25 cleared 290"));
            Assert.That(Floors(FoundryGate.V01HeroId, 2), Is.EqualTo("died 220"), "run_end's 530.5 less floor 1's 310.5");
            Assert.That(Floors("hero_cinder_arcanist", 1), Is.EqualTo("cleared 280"), "an extract after floor 1 adds no floor");
        }

        [Test]
        public void TheFramesAreTheProbesWindowsSetAgainstTheTelemetry()
        {
            FoundryGate.DeviceEvidence device = ReadBoth();
            Assert.That(device.Placed, Is.True);
            Assert.That(device.FrameSource, Is.EqualTo(Newer + "/logcat-unity.txt"));
            Assert.That(device.EarlierWindows, Is.EqualTo(2), "the old pull's log, local time at +0300, before the install");
            Assert.That(device.Windows.Count, Is.EqualTo(15));
            Assert.That(device.RenderWindows.Count, Is.EqualTo(15));

            FoundryGate.DeviceWindow[] w = device.Windows.ToArray();
            Assert.That(new[] { w[0].Frames, w[0].Over20, w[0].Over35 }, Is.EqualTo(new[] { 280, 3, 1 }));
            Assert.That(w[0].P95, Is.EqualTo(33.4f));
            Assert.That(w[0].Max, Is.EqualTo(52.1f));
            Assert.That(w[0].AllocatedKb, Is.EqualTo(18234.5f));
            Assert.That(w[0].Play, Is.False, "the run's load and first wave");
            Assert.That(w[0].Alive, Is.EqualTo(41), "the [Perf] wave line");
            Assert.That(w.Select(x => x.Play), Is.EqualTo(new[] { false, true, true, true, true, true, true, true, false, true, true, true, true, false, false }),
                "the card offer's window ends at time scale 0, and the last two end after the run");
            Assert.That(w.Select(x => x.Alive), Is.EqualTo(new[] { 41, 41, 41, 40, 40, 39, 39, 38, -1, 37, 36, 35, 26, 22, -1 }),
                "no horde_sample while the card's offer stops the run's clock");

            FoundryGate.DeviceWindow r = device.RenderWindows[1];
            Assert.That(new[] { r.P95, r.Max, r.GarbageBytes, r.GpuMs, r.DrawCalls }, Is.EqualTo(new[] { 16.7f, 17.1f, 622f, 3.8f, 132f }));
            Assert.That(r.Frames, Is.EqualTo(300), "fps 59.9 over the start line's 5 s");
            Assert.That(device.RenderWindows.Select(x => x.Play), Is.EqualTo(w.Select(x => x.Play)));

            // The frames past 35 ms its lines name: the run's load and the card offer's, neither in play; the wave's 33.4 ms
            // frame is not past 35.
            Assert.That(device.LongFrames.Select(f => (f.Ms, f.Wave, f.Play)), Is.EqualTo(new[] { (52.1f, false, false), (41.0f, false, false) }));
        }

        // RunTelemetry and logcat formats: -v time (the phone's local time, no year), threadtime, -v year, -v UTC and
        // -v epoch.
        [Test]
        public void ALogLinesTimeIsReadInEveryFormatAdbWrites()
        {
            var expected = new DateTime(2026, 11, 12, 18, 20, 3, 500, DateTimeKind.Utc);
            TimeSpan offset = TimeSpan.FromHours(3);
            DateTime pulled = new DateTime(2026, 11, 12, 18, 25, 0, DateTimeKind.Utc);
            Assert.That(DevicePull.LineTime("2026-11-12 18:20:03.500 +0000 I/Unity   (23456): [Perf] window 3", null, null), Is.EqualTo(expected));
            Assert.That(DevicePull.LineTime("11-12 21:20:03.500 I/Unity   (23456): [Perf] window 3", offset, pulled), Is.EqualTo(expected));
            Assert.That(DevicePull.LineTime("11-12 21:20:03.500 23456 23480 I Unity   : [Perf] window 3", offset, pulled), Is.EqualTo(expected));
            Assert.That(DevicePull.LineTime("2026-11-12 21:20:03.500 I/Unity   (23456): [Perf] window 3", offset, null), Is.EqualTo(expected));
            Assert.That(DevicePull.LineTime("2026-11-12 21:20:03.500 +0300 I/Unity   (23456): [Perf] window 3", null, null), Is.EqualTo(expected));
            long epoch = new DateTimeOffset(expected).ToUnixTimeSeconds();
            Assert.That(DevicePull.LineTime($"{epoch}.500 I/Unity   (23456): [Perf] window 3", null, null), Is.EqualTo(expected));
            Assert.That(DevicePull.LineTime("11-12 21:20:03.500 I/Unity   (23456): [Perf] window 3", null, pulled), Is.Null, "no zone and no offset");
            Assert.That(DevicePull.LineTime("--------- beginning of main", offset, pulled), Is.Null);
            // A line from December read in a January pull is the year before.
            Assert.That(DevicePull.LineTime("12-31 23:59:59.000 I/Unity   (1): x", TimeSpan.Zero, new DateTime(2027, 1, 1, 0, 10, 0, DateTimeKind.Utc)),
                Is.EqualTo(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)));
            Assert.That(DevicePull.ParseOffset("-0430"), Is.EqualTo(-new TimeSpan(4, 30, 0)));
        }

        // The gate's two rows on the sample pulls: the Vanguard's floor 1 P50 decides the owner's row, and the probe's
        // windows at 30 alive and more the frames row.
        [Test]
        public void TheGatesDeviceRowsReadThePulls()
        {
            var evidence = new FoundryGate.Evidence { Device = ReadBoth() };
            Dictionary<string, FoundryGate.Row> rows = FoundryGate.Rows(Batches(), null, evidence).ToDictionary(r => r.Criterion);

            FoundryGate.Row owner = rows[FoundryGate.OwnersFloor];
            Assert.That(owner.Status, Is.EqualTo(FoundryGate.Met), "P50 290 s");
            Assert.That(owner.Simulation, Is.EqualTo("-"));
            Assert.That(owner.Basis, Does.Contain("(" + Newer + " and 1 earlier pull, SM-S911B), 4 runs on the APK 9c1e5b7a3f02 installed 2026-11-12 17:00 UTC: " +
                                                  "Vanguard floor 1 P50 290 s over 3 runs (2 cleared, P50 300 s), floor 2 P50 220 s over 1 run (0 cleared); " +
                                                  "Ilya floor 1 P50 280 s over 1 run (1 cleared, P50 280 s)."));
            Assert.That(owner.Basis, Does.Contain("Left out: 1 begun before the install (another APK); 1 of the density probe")
                .And.Contain("1 floor never finished").And.Contain("The telemetry names no build"));
            Assert.That(owner.Basis, Does.Not.Contain("autopilot.txt"));

            FoundryGate.Row frames = rows[FoundryGate.DeviceFrames];
            Assert.That(frames.Status, Is.EqualTo(FoundryGate.Met));
            Assert.That(frames.Editor, Is.EqualTo("no perf probe file"));
            Assert.That(frames.Basis, Does.Contain("At the crowd (30 alive and more, the owner's 30-40 on the S23; HordeRule.SceneCap 40): " +
                                                   "10 windows, 2998 frames, 35-41 alive (P50 39): past 20 ms 3 (0.1%), past 35 ms 0, max 33.4 ms"));
            Assert.That(frames.Basis, Does.Contain("Every window in play: 11 windows, 3298 frames, 26-41 alive"));
            Assert.That(frames.Basis, Does.Contain("garbage 0.60 KB a frame P50 (earlier half 0.59, later 0.60)"));
            Assert.That(frames.Basis, Does.Contain("[RenderStats] at the crowd (10 windows), P50s: GPU"));
            Assert.That(frames.Basis, Does.Contain("Read: p95 at most 20 ms at the crowd; no frame past 35 ms in play; garbage flat at the crowd."));
            Assert.That(frames.Basis, Does.Contain("Left out: 2 windows from before the install."));

            string section = FoundryGate.Section(Batches(), null, evidence);
            Assert.That(section, Does.Contain("Device: " + Newer + " and 1 earlier pull (SM-S911B), read on the APK 9c1e5b7a3f02 installed 2026-11-12 17:00 UTC: " +
                                              "4 runs of the owner's (5 floors), 15 [Perf] windows and 15 [RenderStats] windows."));
        }

        // Without a pull both rows read as they did before the pull script, word for word, and the section names no device.
        [Test]
        public void WithoutAPullBothRowsReadAsBefore()
        {
            foreach (FoundryGate.Evidence evidence in new[] { null, new FoundryGate.Evidence() })
            {
                Dictionary<string, FoundryGate.Row> rows = FoundryGate.Rows(Batches(), null, evidence).ToDictionary(r => r.Criterion);
                FoundryGate.Row owner = rows[FoundryGate.OwnersFloor];
                Assert.That(new[] { owner.Target, owner.Simulation, owner.Editor, owner.Status, owner.Basis }, Is.EqualTo(new[]
                {
                    "P50 240-360 s on the owner too (docs/41 step 2)", "-", "-", FoundryGate.NotMeasured,
                    "This report reads no owner telemetry: floor_complete and run_end active_sec from the owner's runs on a current APK would measure it."
                }));
                FoundryGate.Row frames = rows[FoundryGate.DeviceFrames];
                Assert.That(new[] { frames.Target, frames.Simulation, frames.Editor, frames.Status, frames.Basis }, Is.EqualTo(new[]
                {
                    "the S23: p95 <= 20 ms at the chosen crowd, no frame over 35 ms in a steady fight, garbage per frame flat (docs/41 step 1)",
                    "-", "no perf probe file", FoundryGate.NotMeasured,
                    "The perf probe's times are the Editor's on a desktop CPU, for comparing builds, not the phone (its own caution). RenderStatsProbe (development/render-stats.txt) on the S23 with a current APK at 30-40 enemies alive would measure it; docs/21 has the S23's last reading at 10-40, on 2026-10-26, before V0.1's floor."
                }));
                Assert.That(FoundryGate.Section(Batches(), null, evidence), Does.Not.Contain("Device:"));
            }
        }

        // What a pull measured and missed is not met; what it could not measure leaves the row not measured (constructed).
        [Test]
        public void ARowReadsNotMetOnAMissAndNotMeasuredOnAGap()
        {
            FoundryGate.DeviceWindow Window(int alive, int over20 = 0, int over35 = 0, float kb = 180f, bool play = true) => new FoundryGate.DeviceWindow
            {
                Frames = 300, P95 = over20 > 15 ? 21f : 16.7f, Max = over35 > 0 ? 40f : over20 > 0 ? 25f : 17f, Over20 = over20, Over35 = over35,
                AllocatedKb = kb, Play = play, Alive = alive
            };
            (string Status, string Basis) Frames(params FoundryGate.DeviceWindow[] windows)
            {
                var device = new FoundryGate.DeviceEvidence { Source = "device-test", Placed = true };
                device.Windows.AddRange(windows);
                FoundryGate.Row row = FoundryGate.Rows(Batches(), null, new FoundryGate.Evidence { Device = device }).Single(r => r.Criterion == FoundryGate.DeviceFrames);
                return (row.Status, row.Basis);
            }

            Assert.That(Frames(Window(40), Window(38), Window(36), Window(35)).Status, Is.EqualTo(FoundryGate.Met));
            Assert.That(Frames(Window(40), Window(38, over35: 1), Window(36), Window(35)).Basis, Does.Contain("1 frame past 35 ms in play"));
            Assert.That(Frames(Window(40), Window(38), Window(20, over35: 1), Window(35)).Status, Is.EqualTo(FoundryGate.NotMet),
                "past 35 ms in any fight, at the crowd or not");
            Assert.That(Frames(Window(40), Window(38), Window(10, over35: 1, play: false), Window(35)).Status, Is.EqualTo(FoundryGate.Met),
                "not in play: a load, a menu or a card's offer");
            Assert.That(Frames(Window(40, over20: 30), Window(38, over20: 30), Window(36), Window(35)).Status, Is.EqualTo(FoundryGate.Met),
                "60 of 1200 frames past 20 ms is a twentieth: p95 at 20 ms");
            Assert.That(Frames(Window(40, over20: 31), Window(38, over20: 30), Window(36), Window(35)).Basis, Does.Contain("p95 past 20 ms at the crowd"),
                "61 is more than a twentieth");
            Assert.That(Frames(Window(40, over20: 31), Window(38, over20: 30), Window(36), Window(35)).Status, Is.EqualTo(FoundryGate.NotMet));
            Assert.That(Frames(Window(40, kb: 150), Window(38, kb: 150), Window(36, kb: 300), Window(35, kb: 300)).Basis, Does.Contain("garbage growing at the crowd"),
                "0.5 KB a frame to 1 KB");
            (string status, string basis) = Frames(Window(25), Window(22), Window(-1));
            Assert.That(status, Is.EqualTo(FoundryGate.NotMeasured));
            Assert.That(basis, Does.Contain("no window in play at the crowd (at most 25 alive)"));
            Assert.That(Frames(Window(40)).Basis, Does.Contain("one window at the crowd cannot show garbage flat"));
            Assert.That(Frames(Window(40)).Status, Is.EqualTo(FoundryGate.NotMeasured));
            Assert.That(Frames().Basis, Does.Contain("no [Perf] window"));

            (string Status, string Basis) Owner(params float[] vanguardFloorOne)
            {
                var device = new FoundryGate.DeviceEvidence { Source = "device-test" };
                device.Floors.AddRange(vanguardFloorOne.Select(s => new FoundryGate.DeviceFloor { HeroId = FoundryGate.V01HeroId, Floor = 1, Cleared = true, Seconds = s }));
                device.Floors.Add(new FoundryGate.DeviceFloor { HeroId = "hero_cinder_arcanist", Floor = 1, Cleared = true, Seconds = 300f });
                device.Flags.Add("autopilot.txt");
                FoundryGate.Row row = FoundryGate.Rows(Batches(), null, new FoundryGate.Evidence { Device = device }).Single(r => r.Criterion == FoundryGate.OwnersFloor);
                return (row.Status, row.Basis);
            }

            Assert.That(Owner(400f, 420f, 380f).Status, Is.EqualTo(FoundryGate.NotMet), "P50 400 s");
            Assert.That(Owner(400f, 420f, 380f).Basis, Does.Contain("development/autopilot.txt was on the phone at the pull"));
            Assert.That(Owner().Status, Is.EqualTo(FoundryGate.NotMeasured), "Ilya's runs alone");
            Assert.That(Owner().Basis, Does.Contain("No run of the Vanguard's finished floor 1 or died on it").And.Contain("its install time not read"));
        }

        // Both heroes' constructed batches: the device rows read the pulls, not the simulation.
        private static FoundryBatch.Batch[] Batches()
        {
            FoundryBatch.Batch Batch(DescentSimulation.FoundryHero hero) => new FoundryBatch.Batch
            {
                Hero = hero, Levers = FoundryBatch.Shipped(),
                Runs = new[]
                {
                    new DescentSimulation.FoundryResult
                    {
                        Seed = 1, Hero = hero.Id, Outcome = DescentSimulation.FoundryOutcome.Cleared, Kinds = new[] { RoomKind.Combat, RoomKind.Boss },
                        FloorSeconds = 280f, RoomSeconds = new[] { 200f, 80f }, WalkSeconds = new[] { 5f, -1f }, WaveSeconds = new[] { 150f, -1f },
                        LevelAtGate = 20, BossSeconds = 50f, WardenSeconds = 50f
                    }
                }
            };

            return new[] { Batch(DescentSimulation.Vanguard()), Batch(DescentSimulation.CinderArcanist()) };
        }
    }
}
