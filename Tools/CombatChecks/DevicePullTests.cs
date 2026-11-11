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

        // ---------------------------------------------------------------------------------------------------------
        // Today's formats (2026-11-12): the Descent's three floors, a pit death, the held telemetry file, the ground
        // projection's [Perf] lines, several runs and heroes in one pull.
        // ---------------------------------------------------------------------------------------------------------

        private static readonly DateTime Day = new DateTime(2026, 11, 13, 9, 0, 0, DateTimeKind.Utc);

        private static string Event(string session, int seq, double seconds, string run, string name, string fields = "") =>
            $"{{\"v\":1,\"seq\":{seq},\"t\":\"{Day.AddSeconds(seconds):yyyy-MM-dd'T'HH:mm:ss.fff'Z'}\",\"st\":{seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"session\":\"{session}\"" +
            (run != null ? $",\"run\":\"{run}\"" : string.Empty) + $",\"event\":\"{name}\"" + (fields.Length > 0 ? "," + fields : string.Empty) + "}";

        private static string Room(int floor, string kind) =>
            $"\"floor_index\":{floor},\"floor_id\":\"f{floor}\",\"room_index\":1,\"room_kind\":\"{kind}\",\"hero_hp\":100.000,\"hero_max_hp\":100.000";

        private static string Log(double seconds, string text) =>
            (new DateTimeOffset(Day.AddSeconds(seconds)).ToUnixTimeMilliseconds() / 1000.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) +
            " I/Unity   (23456): " + text;

        // One pull of a session of two runs - the Vanguard's whole Descent and Ilya's death to the pit on floor 2 - rotated
        // into events.1.jsonl, and a second session's Vanguard run still on in events.jsonl, which the game holds open.
        private static string TodaysPull(string root, params string[] log)
        {
            string folder = Path.Combine(root, "device-20261113-094000");
            Directory.CreateDirectory(Path.Combine(folder, "telemetry"));
            File.WriteAllLines(Path.Combine(folder, "telemetry", "events.1.jsonl"), new[]
            {
                Event("s1", 1, 0, null, "session_start", "\"schema\":1,\"app_version\":\"0.1.0\",\"platform\":\"android\",\"development\":true"),
                Event("s1", 2, 1, "s1-1", "run_start", "\"run_ordinal\":1,\"hero_id\":\"hero_vanguard\""),
                Event("s1", 3, 1, "s1-1", "room_start", Room(1, "combat")),
                Event("s1", 4, 2, "s1-1", "wave_start", "\"floor_index\":1,\"room_index\":1,\"wave_index\":1,\"enemy_count\":2"),
                Event("s1", 5, 30, "s1-1", "horde_sample", "\"floor_index\":1,\"horde_alive\":20,\"wave_alive\":5"),
                Event("s1", 6, 300, "s1-1", "floor_complete", "\"floor_index\":1,\"floor_id\":\"f1\",\"active_sec\":280.000,\"rooms\":7"),
                Event("s1", 7, 310, "s1-1", "room_start", Room(2, "combat")),
                Event("s1", 8, 600, "s1-1", "floor_complete", "\"floor_index\":2,\"floor_id\":\"f2\",\"active_sec\":270.000,\"rooms\":6"),
                Event("s1", 9, 605, "s1-1", "room_start", Room(3, "swarm")),
                Event("s1", 10, 675, "s1-1", "floor_complete", "\"floor_index\":3,\"floor_id\":\"floor_final_swarm\",\"active_sec\":68.000,\"rooms\":1"),
                Event("s1", 11, 676, "s1-1", "run_end", "\"result\":\"victory\",\"cause\":\"cleared\",\"floors_completed\":3,\"active_sec\":618.000"),
                Event("s1", 12, 720, "s1-2", "run_start", "\"run_ordinal\":2,\"hero_id\":\"hero_cinder_arcanist\""),
                Event("s1", 13, 720, "s1-2", "room_start", Room(1, "combat")),
                Event("s1", 14, 721, "s1-2", "wave_start", "\"floor_index\":1,\"room_index\":1,\"wave_index\":1,\"enemy_count\":2"),
                Event("s1", 15, 990, "s1-2", "floor_complete", "\"floor_index\":1,\"floor_id\":\"f1\",\"active_sec\":265.000,\"rooms\":7"),
                Event("s1", 16, 1000, "s1-2", "room_start", Room(2, "combat")),
                Event("s1", 17, 1140, "s1-2", "run_end", "\"result\":\"defeat\",\"cause\":\"killed_by:pit_eruption\",\"floors_completed\":1,\"active_sec\":400.000")
            });
            File.WriteAllLines(Path.Combine(folder, "telemetry", "events.jsonl"), new[]
            {
                Event("s2", 1, 1800, null, "session_start", "\"schema\":1"),
                Event("s2", 2, 1801, "s2-1", "run_start", "\"run_ordinal\":1,\"hero_id\":\"hero_vanguard\""),
                Event("s2", 3, 1801, "s2-1", "room_start", Room(1, "combat")),
                Event("s2", 4, 1802, "s2-1", "wave_start", "\"floor_index\":1,\"room_index\":1,\"wave_index\":1,\"enemy_count\":2"),
                Event("s2", 5, 2090, "s2-1", "floor_complete", "\"floor_index\":1,\"floor_id\":\"f1\",\"active_sec\":290.000,\"rooms\":7"),
                Event("s2", 6, 2100, "s2-1", "room_start", Room(2, "combat"))
            });
            File.WriteAllLines(Path.Combine(folder, DevicePull.ManifestName), new[]
            {
                "pulled_utc: 2026-11-13T09:40:00Z", "model: SM-S911B", "apk_sha256: da3d13fd515c89866dbf2724091a36227d362d679cf664a4534d38461df7b5b4",
                "installed_utc: 2026-11-13T08:00:00Z", "device_utc_offset: +0300", "game_running: yes", "development_flags: none", "logcat_offset: +0000"
            });
            File.WriteAllLines(Path.Combine(folder, "logcat-unity.txt"), log);
            return folder;
        }

        private const string Window = "[Perf] window 24: 299 frames in 5.00 s (59.8 fps), avg 16.8 ms, p50 16.7, p95 17.0, p99 18.0, max {0} ms, over 20 ms {1}, over 35 ms {1}; main thread max 12.0 ms; allocated 150.0 KB, most in a frame 10.0 KB, GC runs 0; managed 2.6 MB, total 152.3 MB; time scale 1";

        private static T InTemp<T>(Func<string, T> body)
        {
            string root = Path.Combine(Path.GetTempPath(), "cryptforge-device-" + Guid.NewGuid().ToString("N"));
            try
            {
                return body(root);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        // Several runs and heroes in one pull, across a rotation: each floor under its run's hero, the finale (the swarm
        // room, floor 3) named, floor 2 - its gate at 210 s as floor 1's - read beside the row, the pit's death by its
        // cause, the whole Descent by run_end's active_sec, and the run still on as a floor never finished.
        [Test]
        public void ThreeFloorsAPitDeathAndTwoHeroesInOnePullAreRead()
        {
            InTemp(root =>
            {
                FoundryGate.DeviceEvidence device = DevicePull.Read(new[] { TodaysPull(root) });
                Assert.That(device.Runs, Is.EqualTo(3));
                Assert.That(device.UnfinishedFloors, Is.EqualTo(1), "the second session's floor 2, still on at the pull");
                Assert.That(device.Floors.Select(f => (f.HeroId, f.Floor, f.Cleared, f.Seconds, f.Finale, f.Cause)), Is.EquivalentTo(new[]
                {
                    ("hero_vanguard", 1, true, 280f, false, (string)null), ("hero_vanguard", 2, true, 270f, false, null), ("hero_vanguard", 3, true, 68f, true, null),
                    ("hero_cinder_arcanist", 1, true, 265f, false, null), ("hero_cinder_arcanist", 2, false, 135f, false, "killed_by:pit_eruption"),
                    ("hero_vanguard", 1, true, 290f, false, null)
                }));
                Assert.That(device.Descents.Select(d => (d.HeroId, d.Floors, d.Seconds)), Is.EqualTo(new[] { ("hero_vanguard", 3, 618f) }));

                FoundryGate.Row owner = FoundryGate.Rows(Batches(), null, new FoundryGate.Evidence { Device = device }).Single(r => r.Criterion == FoundryGate.OwnersFloor);
                Assert.That(owner.Status, Is.EqualTo(FoundryGate.Met), "the Vanguard's floor 1 P50 285 s");
                Assert.That(owner.Basis, Does.Contain("Vanguard floor 1 P50 285 s over 2 runs (2 cleared, P50 285 s), floor 2 P50 270 s over 1 run (1 cleared, P50 270 s), " +
                                                      "the finale (floor 3) P50 68 s over 1 run (1 cleared, P50 68 s); Ilya floor 1 P50 265 s over 1 run (1 cleared, P50 265 s), floor 2 P50 135 s over 1 run (0 cleared)."));
                Assert.That(owner.Basis, Does.Contain("The Vanguard's floor 2, its gate open at 210 s as floor 1's: P50 270 s, within 240-360 s (beside the row; floor 1 decides)."));
                Assert.That(owner.Basis, Does.Contain("Deaths by run_end's cause: killed_by:pit_eruption 1 (Ilya floor 2)."));
                Assert.That(owner.Basis, Does.Contain("Whole Descents (run_end victory, its active_sec): Vanguard 1, P50 618 s (10.3 min)."));
                Assert.That(owner.Basis, Does.Contain("1 floor never finished"));
                return 0;
            });
        }

        // FileTelemetryStore holds events.jsonl open while the game runs, sharing only reading and deletion: the pull's
        // reader shares writing, so a folder the game still writes reads the same.
        [Test]
        public void ATelemetryFileTheGameHoldsOpenIsRead()
        {
            InTemp(root =>
            {
                string folder = TodaysPull(root);
                string current = Path.Combine(folder, "telemetry", "events.jsonl");
                using (new FileStream(current, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read | FileShare.Delete, 1))
                {
                    Assert.That(DevicePull.ReadEvents(new[] { Path.Combine(folder, "telemetry") }).Count(e => e.Session == "s2"), Is.EqualTo(6));
                    Assert.That(DevicePull.Read(new[] { folder }).Runs, Is.EqualTo(3));
                }
                return 0;
            });
        }

        // The ground projection's [Perf] lines: the load's pre-warm is left out; one-time work at a ramp is named, and the
        // long frame it ran in is named as the ramp's. Frames past 35 ms out of play are counted apart, and a window in the
        // finale's swarm (no horde_sample) is named as having no alive count.
        [Test]
        public void TheGroundProjectionAndLongFramesAreReadAtLoadAtARampAndOutOfPlay()
        {
            InTemp(root =>
            {
                FoundryGate.Row Frames(FoundryGate.DeviceEvidence device) =>
                    FoundryGate.Rows(Batches(), null, new FoundryGate.Evidence { Device = device }).Single(r => r.Criterion == FoundryGate.DeviceFrames);
                string[] load =
                {
                    Log(.5, "[Perf] ground projection: prewarm 4.12 ms, material 1.05 ms, grids 2.31 ms; frame 46.8 ms, main thread 44.9 ms, allocated 612.4 KB"),
                    Log(.5, "[Perf] long frame 46.8 ms, main thread 44.9 ms, allocated 612.4 KB, time scale 1"),
                    Log(125, string.Format(System.Globalization.CultureInfo.InvariantCulture, Window, "18.0", 0)),
                    Log(630, string.Format(System.Globalization.CultureInfo.InvariantCulture, Window, "18.0", 0))
                };

                FoundryGate.DeviceEvidence clean = DevicePull.Read(new[] { TodaysPull(Path.Combine(root, "clean"), load) });
                Assert.That(clean.GroundLoad, Is.EqualTo(1));
                Assert.That(clean.RampWork, Is.Empty);
                Assert.That(clean.LongFrames.Select(f => (f.Ms, f.Play, f.Ramp)), Is.EqualTo(new[] { (46.8f, false, false) }), "the load's, before the first wave");
                Assert.That(clean.Windows.Select(w => (w.Play, w.Finale, w.Alive)), Is.EqualTo(new[] { (true, false, -1), (true, true, -1) }));
                string basis = Frames(clean).Basis;
                Assert.That(basis, Does.Contain("The ground projection's one-time work: 1 load with the pre-warm (left out), none at a ramp."));
                Assert.That(basis, Does.Contain("Past 35 ms out of play (loads, menus, card offers; not judged): 1 frame, the longest 46.8 ms."));
                Assert.That(basis, Does.Contain("1 window in play in the finale's swarm, which writes no horde_sample: alive only where a [Perf] wave line counted it (0 did)."));
                Assert.That(basis, Does.Contain("no frame past 35 ms in play"));

                // A long frame in a run's last second of slack, with no run loading, stays in play.
                string[] back = { Log(676.5, "[Perf] long frame 44.0 ms, main thread 42.0 ms, allocated 300.0 KB, time scale 1") };
                Assert.That(DevicePull.Read(new[] { TodaysPull(Path.Combine(root, "back"), back) }).LongFrames.Single().Play, Is.True,
                    "inside s1-1's slack and no run's load: read in play");

                string[] ramp = load.Concat(new[]
                {
                    Log(120, "[Perf] ground projection: prewarm 0.00 ms, material 1.20 ms, grids 2.40 ms; frame 41.3 ms, main thread 39.0 ms, allocated 80.2 KB"),
                    Log(120, "[Perf] long frame 41.3 ms, main thread 39.0 ms, allocated 80.2 KB, time scale 1"),
                    Log(123, string.Format(System.Globalization.CultureInfo.InvariantCulture, Window, "41.3", 1))
                }).ToArray();
                FoundryGate.DeviceEvidence stall = DevicePull.Read(new[] { TodaysPull(Path.Combine(root, "stall"), ramp) });
                Assert.That(stall.RampWork.Select(f => (f.Ms, f.Play)), Is.EqualTo(new[] { (41.3f, true) }));
                Assert.That(stall.LongFrames.Where(f => f.Play).Select(f => (f.Ms, f.Ramp)), Is.EqualTo(new[] { (41.3f, true) }));
                FoundryGate.Row row = Frames(stall);
                Assert.That(row.Status, Is.EqualTo(FoundryGate.NotMet), "a frame past 35 ms in play");
                Assert.That(row.Basis, Does.Contain("Past 35 ms in play: 41.3 ms (a long frame at a ramp's first entry)."));
                Assert.That(row.Basis, Does.Contain("1 load with the pre-warm (left out); 1 at a ramp's first entry, the pre-warm missing it, in frames of 41.3 ms."));
                return 0;
            });
        }

        // A ramp's one-time work and its long frame inside the next run's load, which falls in the last run's second of
        // slack, are read by the same rule: neither is in play.
        [Test]
        public void RampWorkAndLongFramesInTheNextRunsLoadAreNotInPlay()
        {
            InTemp(root =>
            {
                string folder = Path.Combine(root, "device-20261113-094000");
                Directory.CreateDirectory(Path.Combine(folder, "telemetry"));
                File.WriteAllLines(Path.Combine(folder, "telemetry", "events.jsonl"), new[]
                {
                    Event("s1", 1, 0, null, "session_start", "\"schema\":1"),
                    Event("s1", 2, 0, "s1-1", "run_start", "\"run_ordinal\":1,\"hero_id\":\"hero_vanguard\""),
                    Event("s1", 3, 0, "s1-1", "room_start", Room(1, "combat")),
                    Event("s1", 4, 1, "s1-1", "wave_start", "\"floor_index\":1,\"room_index\":1,\"wave_index\":1,\"enemy_count\":2"),
                    Event("s1", 5, 100, "s1-1", "run_end", "\"result\":\"extracted\",\"cause\":\"extracted\",\"floors_completed\":0,\"active_sec\":99.000"),
                    Event("s1", 6, 101.5, "s1-2", "run_start", "\"run_ordinal\":2,\"hero_id\":\"hero_vanguard\""),
                    Event("s1", 7, 101.5, "s1-2", "room_start", Room(1, "combat")),
                    Event("s1", 8, 102.5, "s1-2", "wave_start", "\"floor_index\":1,\"room_index\":1,\"wave_index\":1,\"enemy_count\":2")
                });
                File.WriteAllLines(Path.Combine(folder, DevicePull.ManifestName), new[] { "pulled_utc: 2026-11-13T09:40:00Z", "logcat_offset: +0000" });
                File.WriteAllLines(Path.Combine(folder, "logcat-unity.txt"), new[]
                {
                    Log(50, "[Perf] ground projection: prewarm 0.00 ms, material 1.20 ms, grids 2.40 ms; frame 41.3 ms, main thread 39.0 ms, allocated 80.2 KB"),
                    Log(50, "[Perf] long frame 41.3 ms, main thread 39.0 ms, allocated 80.2 KB, time scale 1"),
                    Log(100.8, "[Perf] ground projection: prewarm 0.00 ms, material 1.10 ms, grids 2.20 ms; frame 39.9 ms, main thread 38.0 ms, allocated 70.1 KB"),
                    Log(100.8, "[Perf] long frame 39.9 ms, main thread 38.0 ms, allocated 70.1 KB, time scale 1")
                });

                FoundryGate.DeviceEvidence device = DevicePull.Read(new[] { folder });
                Assert.That(device.RampWork.Select(f => (f.Ms, f.Play)), Is.EqualTo(new[] { (41.3f, true), (39.9f, false) }));
                Assert.That(device.LongFrames.Select(f => (f.Ms, f.Ramp, f.Play)), Is.EqualTo(new[] { (41.3f, true, true), (39.9f, true, false) }));
                return 0;
            });
        }

        // The sample pulls' two frames past 35 ms are both out of play: counted apart, never judged.
        [Test]
        public void LongFramesOutOfPlayAreCountedApart()
        {
            FoundryGate.Row frames = FoundryGate.Rows(Batches(), null, new FoundryGate.Evidence { Device = ReadBoth() }).Single(r => r.Criterion == FoundryGate.DeviceFrames);
            Assert.That(frames.Basis, Does.Contain("Past 35 ms out of play (loads, menus, card offers; not judged): 2 frames, the longest 52.1 ms."));
            Assert.That(frames.Basis, Does.Not.Contain("ground projection"), "no ground line in the samples");
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
