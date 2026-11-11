using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cryptforge.Tests
{
    // The phone's evidence for the V0.1 gate's two device rows (FoundryGate.DeviceEvidence), from the folders
    // Tools/Device/Pull-DeviceEvidence.ps1 pulls into TestResults/device-<yyyyMMdd-HHmmss>/: pull.txt (the phone, the
    // installed APK's SHA-256 and install time, the phone's UTC offset, the development flags), the game's telemetry
    // (telemetry/events*.jsonl, RunTelemetry's schema 1) and the Unity log (logcat*.txt: FrameTimeProbe's [Perf] lines and
    // RenderStatsProbe's [RenderStats] ones). Every pull is read together - the phone keeps about three files of 256 KB of
    // telemetry, so a pull between runs keeps what its rotation drops - and a line two pulls share is read once.
    //
    // What is read is what the newest pull's APK ran. The telemetry names no build (session_start's app_version is the
    // project's bundle version, the same on every APK), so a run is that APK's when it began after the install, and a log
    // line when it was written after it: both carry the phone's clock. Pure .NET; the batch report hands the result to
    // FoundryGate.
    internal static class DevicePull
    {
        public const string ManifestName = "pull.txt";
        public const string FolderPrefix = "device-";
        // horde-probe.txt's hero (CombatSetup) stands with a million health more, so the density probe measures the fight.
        public const double ProbeHealth = 1000000;
        // A window's alive count reads the telemetry's samples this far either side of it, and a run's end this late.
        private const double SlackSeconds = 1;
        // A run's opening, where its telemetry names no wave: its first seconds. With one, its first wave's entry and this.
        private const double OpeningSeconds = 5;
        private const double WaveSettleSeconds = 1;
        // A long frame's alive count reads the samples this far either side of it.
        private const double FrameSampleSeconds = 3;
        // A ground projection line and the long frame line of the same frame are written together; their log times agree
        // to within this.
        private const double RampPairSeconds = .5;

        // The pulls to read. FOUNDRY_DEVICE (setting) names a folder or several, separated by ';', each absolute or within
        // the Editor folder (TestResults); "none" reads none. Unset: every device-* folder of the Editor folder that holds a
        // pull.txt, oldest first - the folders of the device sessions before the pull script hold none and stay unread.
        public static string[] Folders(string editorFolder, string setting)
        {
            if (!string.IsNullOrWhiteSpace(setting))
            {
                if (string.Equals(setting.Trim(), "none", StringComparison.OrdinalIgnoreCase))
                    return new string[0];
                string[] named = setting.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0)
                    .Select(s => Path.IsPathRooted(s) ? s : Path.Combine(editorFolder ?? string.Empty, s)).ToArray();
                foreach (string folder in named)
                    if (!Directory.Exists(folder))
                        throw new DirectoryNotFoundException("FOUNDRY_DEVICE names a folder that does not exist: " + folder);
                return named;
            }
            if (string.IsNullOrEmpty(editorFolder) || !Directory.Exists(editorFolder))
                return new string[0];
            return Directory.GetDirectories(editorFolder, FolderPrefix + "*")
                .Where(folder => File.Exists(Path.Combine(folder, ManifestName)))
                .OrderBy(folder => Path.GetFileName(folder), StringComparer.Ordinal)
                .ToArray();
        }

        // The pulls read together; null when there are none.
        public static FoundryGate.DeviceEvidence Read(IReadOnlyList<string> folders)
        {
            if (folders == null || folders.Count == 0)
                return null;
            var pulls = folders.Select(folder => new Pull(folder)).ToList();
            Pull newest = pulls.OrderBy(p => p.PulledUtc ?? DateTime.MinValue).ThenBy(p => p.Name, StringComparer.Ordinal).Last();
            DateTime? installed = newest.InstalledUtc;
            var device = new FoundryGate.DeviceEvidence
            {
                Source = newest.Name + (pulls.Count > 1 ? $" and {pulls.Count - 1} earlier pull{(pulls.Count > 2 ? "s" : string.Empty)}" : string.Empty),
                Model = newest.Value("model"),
                ApkSha256 = newest.Value("apk_sha256"),
                InstalledUtc = installed,
                Flags = (newest.Value("development_flags") ?? string.Empty).Split(',').Select(f => f.Trim())
                    .Where(f => f.Length > 0 && f != "none").ToList()
            };

            List<RunRecord> runs = Runs(ReadEvents(pulls.Select(p => Path.Combine(p.Folder, "telemetry"))));
            foreach (RunRecord run in runs)
            {
                if (installed.HasValue && run.Start < installed.Value)
                {
                    device.EarlierRuns++;
                    continue;
                }
                if (run.Probe)
                {
                    device.ProbeRuns++;
                    continue;
                }
                if (!run.Started)
                {
                    // Its start rotated away on the phone: no hero to read its floors under.
                    device.UnfinishedFloors += Math.Max(1, run.Cleared.Count + (run.DiedFloor > 0 ? 1 : 0) + (run.Unfinished ? 1 : 0));
                    continue;
                }
                device.Runs++;
                foreach ((int floor, float seconds) in run.Cleared)
                    device.Floors.Add(new FoundryGate.DeviceFloor
                        { HeroId = run.HeroId, Floor = floor, Cleared = true, Seconds = seconds, Finale = floor == run.FinaleFloor });
                if (run.DiedFloor > 0)
                    device.Floors.Add(new FoundryGate.DeviceFloor
                    {
                        HeroId = run.HeroId, Floor = run.DiedFloor, Seconds = run.DiedSeconds, Finale = run.DiedFloor == run.FinaleFloor,
                        Cause = run.Cause
                    });
                if (run.Result == "victory" && !float.IsNaN(run.Seconds))
                    device.Descents.Add(new FoundryGate.DeviceDescent { HeroId = run.HeroId, Floors = run.Cleared.Count, Seconds = run.Seconds });
                if (run.Unfinished)
                    device.UnfinishedFloors++;
            }

            ReadFrames(pulls, runs, installed, device);
            return device;
        }

        // ---------------------------------------------------------------------------------------------------------
        // pull.txt
        // ---------------------------------------------------------------------------------------------------------

        // One pull: its folder and pull.txt's "key: value" lines (# starts a comment).
        internal sealed class Pull
        {
            public readonly string Folder;
            public readonly string Name;
            public readonly Dictionary<string, string> Values;

            public Pull(string folder)
            {
                Folder = folder;
                Name = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                Values = ReadManifest(Path.Combine(folder, ManifestName));
            }

            public string Value(string key) =>
                Values.TryGetValue(key, out string value) && value.Length > 0 && value != "?" ? value : null;

            public DateTime? PulledUtc => Utc(Value("pulled_utc"));
            public DateTime? InstalledUtc => Utc(Value("installed_utc"));
            // The offset of the log's times where a line names no zone: logcat_offset, else the phone's.
            public TimeSpan? Offset => ParseOffset(Value("logcat_offset") ?? Value("device_utc_offset"));
        }

        public static Dictionary<string, string> ReadManifest(string path)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(path))
                return values;
            foreach (string line in File.ReadLines(path))
            {
                if (line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                    continue;
                int colon = line.IndexOf(':');
                if (colon > 0)
                    values[line.Substring(0, colon).Trim()] = line.Substring(colon + 1).Trim();
            }
            return values;
        }

        private static DateTime? Utc(string text) =>
            text != null && DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime value)
                ? value
                : (DateTime?)null;

        // "+0300", "-0430", "+03:00"; null for anything else.
        public static TimeSpan? ParseOffset(string text)
        {
            Match m = Regex.Match(text ?? string.Empty, @"^(?<sign>[+-])(?<hours>\d{2}):?(?<minutes>\d{2})$");
            if (!m.Success)
                return null;
            var offset = new TimeSpan(int.Parse(m.Groups["hours"].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups["minutes"].Value, CultureInfo.InvariantCulture), 0);
            return m.Groups["sign"].Value == "-" ? -offset : offset;
        }

        // ---------------------------------------------------------------------------------------------------------
        // The telemetry
        // ---------------------------------------------------------------------------------------------------------

        internal sealed class TelemetryEvent
        {
            public string Session;
            public long Seq;
            public DateTime Utc;
            public string Run;
            public string Name;
            public JsonElement Fields;
        }

        // Every line of the folders' events*.jsonl, a line two pulls share (its session and seq) once; a line cut short
        // (a power loss on the phone) is skipped.
        public static List<TelemetryEvent> ReadEvents(IEnumerable<string> telemetryFolders)
        {
            var seen = new HashSet<(string, long)>();
            var events = new List<TelemetryEvent>();
            foreach (string folder in telemetryFolders)
            {
                if (!Directory.Exists(folder))
                    continue;
                foreach (string file in Directory.GetFiles(folder, "*.jsonl").OrderBy(f => f, StringComparer.Ordinal))
                foreach (string line in SharedLines(file))
                {
                    if (line.Trim().Length == 0)
                        continue;
                    JsonElement e;
                    try
                    {
                        using (JsonDocument document = JsonDocument.Parse(line))
                            e = document.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                        continue;
                    }
                    string session = e.ValueKind == JsonValueKind.Object ? Text(e, "session") : null;
                    string name = session != null ? Text(e, "event") : null;
                    DateTime? utc = name != null ? Utc(Text(e, "t")) : null;
                    if (!utc.HasValue || !e.TryGetProperty("seq", out JsonElement seq) || seq.ValueKind != JsonValueKind.Number ||
                        !seq.TryGetInt64(out long number) || !seen.Add((session, number)))
                        continue;
                    events.Add(new TelemetryEvent
                    {
                        Session = session, Seq = number, Utc = utc.Value, Name = name, Fields = e, Run = Text(e, "run")
                    });
                }
            }
            return events.OrderBy(e => e.Session, StringComparer.Ordinal).ThenBy(e => e.Seq).ToList();
        }

        // A file's lines, read while a writer may hold it open: FileTelemetryStore holds the current events.jsonl open
        // between appends (2026-11-11) and shares only reading and deletion, so a reader on Windows - a pull copied off the
        // phone while the game ran, or the Editor's own folder - must share writing (File.ReadLines does not).
        public static IEnumerable<string> SharedLines(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    yield return line;
            }
        }

        // One run as its telemetry tells it: who, when, its floors and how it ended, and its horde samples.
        internal sealed class RunRecord
        {
            public string Id;
            public string HeroId;
            // run_start was read: without it the run's start rotated away on the phone, and Start is its first line's.
            public bool Started;
            public DateTime Start;
            // Its load and first wave: the first wave's entry and a second, or its first seconds without one.
            public DateTime OpeningEnd;
            // run_end's or run_interrupted's time, else its last line's (still on at the pull, or the process killed).
            public DateTime End;
            public bool Probe;
            // run_end's result, "interrupted" for run_interrupted, null for neither; run_end's cause (cleared, extracted,
            // killed, killed_by:<id> - the pit's is killed_by:pit_eruption) and its active_sec, the whole run's.
            public string Result;
            public string Cause;
            public float Seconds = float.NaN;
            // The first room_start of a swarm room (the finale, floor 3's floor_final_swarm) and its floor: the swarm writes
            // swarm_step, no horde_sample, so a window in it has no alive count from the telemetry. Null when never reached.
            public DateTime? FinaleStart;
            public int FinaleFloor = -1;
            public List<(int Floor, float Seconds)> Cleared = new List<(int, float)>();
            public int DiedFloor = -1;
            public float DiedSeconds = float.NaN;
            // A floor begun and never finished, or one the run died on whose floors before it are not all in the log.
            public bool Unfinished;
            public List<(DateTime Utc, int Alive)> Samples = new List<(DateTime, int)>();
        }

        // The runs of the events: a floor cleared is floor_complete's active_sec (which restarts at each floor_complete,
        // the checkpoint and descent before it counted); the floor a run died on is run_end's active_sec less the floors
        // before it, on the floor of its last room_start.
        public static List<RunRecord> Runs(IEnumerable<TelemetryEvent> events)
        {
            var runs = new List<RunRecord>();
            foreach (IGrouping<string, TelemetryEvent> group in events.Where(e => e.Run != null).GroupBy(e => e.Run))
            {
                TelemetryEvent[] lines = group.OrderBy(e => e.Seq).ToArray();
                var run = new RunRecord { Id = group.Key, Start = lines[0].Utc, End = lines[lines.Length - 1].Utc };
                int floor = 0, lastCleared = 0;
                float clearedSeconds = 0f;
                DateTime? firstWave = null;
                foreach (TelemetryEvent e in lines)
                {
                    JsonElement f = e.Fields;
                    if (f.TryGetProperty("hero_max_hp", out JsonElement maximum) && maximum.ValueKind == JsonValueKind.Number &&
                        maximum.GetDouble() >= ProbeHealth)
                        run.Probe = true;
                    switch (e.Name)
                    {
                        case "run_start":
                            run.Started = true;
                            run.Start = e.Utc;
                            run.HeroId = Text(f, "hero_id");
                            break;
                        case "room_start":
                            floor = Int(f, "floor_index", floor);
                            if (Text(f, "room_kind") == "swarm" && !run.FinaleStart.HasValue)
                            {
                                run.FinaleStart = e.Utc;
                                run.FinaleFloor = floor;
                            }
                            break;
                        case "wave_start":
                            if (!firstWave.HasValue)
                                firstWave = e.Utc;
                            break;
                        case "horde_sample":
                            run.Samples.Add((e.Utc, Int(f, "horde_alive", 0) + Int(f, "wave_alive", 0)));
                            break;
                        case "floor_complete":
                            int index = Int(f, "floor_index", floor);
                            float seconds = Float(f, "active_sec");
                            run.Cleared.Add((index, seconds));
                            clearedSeconds += seconds;
                            lastCleared = Math.Max(lastCleared, index);
                            break;
                        case "run_end":
                            run.End = e.Utc;
                            run.Result = Text(f, "result");
                            run.Cause = Text(f, "cause");
                            run.Seconds = Float(f, "active_sec");
                            if (run.Result == "defeat")
                            {
                                if (Int(f, "floors_completed", -1) == run.Cleared.Count)
                                {
                                    run.DiedFloor = floor > lastCleared ? floor : lastCleared + 1;
                                    run.DiedSeconds = Float(f, "active_sec") - clearedSeconds;
                                }
                                else
                                {
                                    run.Unfinished = true;
                                }
                            }
                            break;
                        case "run_interrupted":
                            run.End = e.Utc;
                            run.Result = "interrupted";
                            break;
                    }
                }
                if (run.Result != "defeat" && run.Result != "victory" && run.Result != "extracted" && floor > lastCleared)
                    run.Unfinished = true;
                run.OpeningEnd = firstWave.HasValue && firstWave.Value >= run.Start
                    ? firstWave.Value.AddSeconds(WaveSettleSeconds)
                    : run.Start.AddSeconds(OpeningSeconds);
                runs.Add(run);
            }
            return runs.OrderBy(r => r.Start).ToList();
        }

        private static string Text(JsonElement e, string key) =>
            e.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static int Int(JsonElement e, string key, int fallback) =>
            e.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n) ? n : fallback;

        private static float Float(JsonElement e, string key) =>
            e.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.Number ? (float)value.GetDouble() : float.NaN;

        // ---------------------------------------------------------------------------------------------------------
        // The log
        // ---------------------------------------------------------------------------------------------------------

        // A logcat line's time: "MM-DD HH:MM:SS.mmm" (-v time or threadtime), with "YYYY-" before it under -v year and the
        // zone after it under -v zone or -v UTC; or seconds since 1970 under -v epoch. Where the line names no year it is
        // the pull's (a line later than the pull is the year before), and where it names no zone the offset is the
        // phone's at the pull (pull.txt's device_utc_offset). Null when neither says.
        private static readonly Regex Stamp = new Regex(
            @"^(?:(?<year>\d{4})-)?(?<month>\d{2})-(?<day>\d{2})\s+(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})(?:\.(?<fraction>\d{1,9}))?(?:\s+(?<zone>[+-]\d{4}|UTC|GMT))?\s",
            RegexOptions.CultureInvariant);
        private static readonly Regex Epoch = new Regex(@"^\s*(?<seconds>\d{9,})(?:\.(?<fraction>\d{1,9}))?\s", RegexOptions.CultureInvariant);

        public static DateTime? LineTime(string line, TimeSpan? offset, DateTime? pulledUtc)
        {
            Match epoch = Epoch.Match(line);
            if (epoch.Success)
                return DateTime.UnixEpoch.AddSeconds(long.Parse(epoch.Groups["seconds"].Value, CultureInfo.InvariantCulture))
                    .AddTicks(FractionTicks(epoch.Groups["fraction"].Value));
            Match m = Stamp.Match(line);
            if (!m.Success)
                return null;
            int Part(string name) => int.Parse(m.Groups[name].Value, CultureInfo.InvariantCulture);
            bool namedYear = m.Groups["year"].Success;
            if (!namedYear && !pulledUtc.HasValue)
                return null;
            TimeSpan? zone = m.Groups["zone"].Success
                ? m.Groups["zone"].Value == "UTC" || m.Groups["zone"].Value == "GMT" ? TimeSpan.Zero : ParseOffset(m.Groups["zone"].Value)
                : offset;
            if (!zone.HasValue)
                return null;
            DateTime local;
            try
            {
                local = new DateTime(namedYear ? Part("year") : pulledUtc.Value.Year, Part("month"), Part("day"), Part("hour"), Part("minute"),
                    Part("second"), DateTimeKind.Utc).AddTicks(FractionTicks(m.Groups["fraction"].Value));
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
            DateTime utc = local - zone.Value;
            if (!namedYear && utc > pulledUtc.Value.AddDays(1))
                utc = utc.AddYears(-1);
            return utc;
        }

        private static long FractionTicks(string digits) =>
            string.IsNullOrEmpty(digits) ? 0 : (long)(double.Parse("0." + digits, CultureInfo.InvariantCulture) * TimeSpan.TicksPerSecond);

        // FrameTimeProbe's lines (Scripts/Core/FrameTimeProbe.cs) and RenderStatsProbe's (RenderStatsProbe.AppendWindow).
        private static readonly Regex PerfWindow = new Regex(
            @"\[Perf\] window \d+: (?<frames>\d+) frames in (?<seconds>[\d.]+) s\b.*?\bp95 (?<p95>[\d.]+),.*?\bmax (?<max>[\d.]+) ms, over 20 ms (?<over20>\d+), over 35 ms (?<over35>\d+);.*?\ballocated (?<kb>[\d.]+) KB\b.*?\btime scale (?<scale>[\d.]+)",
            RegexOptions.CultureInvariant);
        private static readonly Regex PerfWave = new Regex(@"\[Perf\] wave: (?<enemies>\d+) enemies,.*?\bframe (?<frame>[\d.]+) ms", RegexOptions.CultureInvariant);
        private static readonly Regex PerfLong = new Regex(@"\[Perf\] long frame (?<frame>[\d.]+) ms.*?\btime scale (?<scale>[\d.]+)", RegexOptions.CultureInvariant);
        // The ground projection's one-time work (FoundryVisibilityView, 2026-11-12): with a prewarm, the load's; without one, a
        // floor sprite's first ramp overlap made its material or a grid in play - the stall the pre-warm removes.
        private static readonly Regex PerfGround = new Regex(
            @"\[Perf\] ground projection: prewarm (?<prewarm>[\d.]+) ms, material (?<material>[\d.]+) ms, grids (?<grids>[\d.]+) ms; frame (?<frame>[\d.]+) ms",
            RegexOptions.CultureInvariant);
        private static readonly Regex Token = new Regex(@"(?<key>[a-z_0-9]+)=(?<value>\S+)", RegexOptions.CultureInvariant);

        // One probe line of the log, its time on the phone's clock (null where it cannot be set) and its text from the tag on.
        internal sealed class LogLine
        {
            public DateTime? Utc;
            public string Text;
            public string File;
        }

        // The probes' lines of every pull's logcat*.txt, a line two pulls share once, in time order.
        public static List<LogLine> ReadLog(IEnumerable<Pull> pulls)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var lines = new List<LogLine>();
            foreach (Pull pull in pulls)
            foreach (string file in Directory.GetFiles(pull.Folder, "logcat*.txt").OrderBy(f => f, StringComparer.Ordinal))
            foreach (string raw in SharedLines(file))
            {
                int at = raw.IndexOf("[Perf]", StringComparison.Ordinal);
                if (at < 0)
                    at = raw.IndexOf("[RenderStats]", StringComparison.Ordinal);
                if (at < 0)
                    continue;
                DateTime? utc = LineTime(raw, pull.Offset, pull.PulledUtc);
                string text = raw.Substring(at).TrimEnd();
                string key = utc.HasValue ? utc.Value.Ticks.ToString(CultureInfo.InvariantCulture) + " " + text : raw;
                if (seen.Add(key))
                    lines.Add(new LogLine { Utc = utc, Text = text, File = pull.Name + "/" + Path.GetFileName(file) });
            }
            return lines.OrderBy(l => l.Utc ?? DateTime.MinValue).ToList();
        }

        // The log's windows and long frames on the installed APK, each set against the runs: in play when the time scale is
        // 1 inside a run past its opening; alive the most enemies a horde_sample or a [Perf] wave line counted in it.
        private static void ReadFrames(List<Pull> pulls, List<RunRecord> runs, DateTime? installed, FoundryGate.DeviceEvidence device)
        {
            var lines = new List<LogLine>();
            foreach (LogLine line in ReadLog(pulls))
            {
                if (installed.HasValue && line.Utc.HasValue && line.Utc.Value < installed.Value)
                {
                    if (PerfWindow.IsMatch(line.Text))
                        device.EarlierWindows++;
                    continue;
                }
                lines.Add(line);
            }
            device.FrameSource = lines.Count == 0 ? null : string.Join(", ", lines.Select(l => l.File).Distinct());
            device.Placed = !lines.Any(l => PerfWindow.IsMatch(l.Text)) || lines.Any(l => l.Utc.HasValue && PerfWindow.IsMatch(l.Text));
            var waves = new List<(DateTime Utc, int Enemies)>();
            foreach (LogLine line in lines)
            {
                Match wave = PerfWave.Match(line.Text);
                if (wave.Success && line.Utc.HasValue)
                    waves.Add((line.Utc.Value, int.Parse(wave.Groups["enemies"].Value, CultureInfo.InvariantCulture)));
            }

            double renderSeconds = 5;
            var rampTimes = new List<(DateTime? Utc, float Ms)>();
            foreach (LogLine line in lines)
            {
                Match m = PerfWindow.Match(line.Text);
                if (m.Success)
                {
                    var window = new FoundryGate.DeviceWindow
                    {
                        Frames = int.Parse(m.Groups["frames"].Value, CultureInfo.InvariantCulture),
                        P95 = Number(m.Groups["p95"].Value), Max = Number(m.Groups["max"].Value),
                        Over20 = int.Parse(m.Groups["over20"].Value, CultureInfo.InvariantCulture),
                        Over35 = int.Parse(m.Groups["over35"].Value, CultureInfo.InvariantCulture),
                        AllocatedKb = Number(m.Groups["kb"].Value)
                    };
                    Place(window, line.Utc, Number(m.Groups["seconds"].Value), Number(m.Groups["scale"].Value), runs, waves);
                    device.Windows.Add(window);
                    continue;
                }
                if (line.Text.StartsWith("[RenderStats]", StringComparison.Ordinal))
                {
                    Dictionary<string, string> tokens = Token.Matches(line.Text).Cast<Match>()
                        .GroupBy(t => t.Groups["key"].Value).ToDictionary(g => g.Key, g => g.First().Groups["value"].Value);
                    if (line.Text.StartsWith("[RenderStats] start", StringComparison.Ordinal))
                    {
                        float seconds = Value(tokens, "window_s");
                        renderSeconds = float.IsNaN(seconds) || seconds <= 0f ? 5 : seconds;
                        continue;
                    }
                    float fps = Value(tokens, "fps");
                    var window = new FoundryGate.DeviceWindow
                    {
                        Frames = float.IsNaN(fps) ? 0 : (int)Math.Round(fps * renderSeconds),
                        P95 = Value(tokens, "ms_p95"), Max = Value(tokens, "ms_max"), GarbageBytes = Value(tokens, "gc_frame_avg"),
                        GpuMs = Value(tokens, "gpu_ms"), DrawCalls = Value(tokens, "draw")
                    };
                    float scale = Value(tokens, "timescale");
                    Place(window, line.Utc, renderSeconds, float.IsNaN(scale) ? 0f : scale, runs, waves);
                    device.RenderWindows.Add(window);
                    continue;
                }
                Match ground = PerfGround.Match(line.Text);
                if (ground.Success)
                {
                    if (Number(ground.Groups["prewarm"].Value) > 0f)
                    {
                        device.GroundLoad++;
                        continue;
                    }
                    if (!(Number(ground.Groups["material"].Value) > 0f) && !(Number(ground.Groups["grids"].Value) > 0f))
                        continue;
                    var work = new FoundryGate.DeviceFrame { Ms = Number(ground.Groups["frame"].Value), Ramp = true };
                    if (line.Utc.HasValue)
                    {
                        DateTime at = line.Utc.Value;
                        work.Play = runs.Any(r => r.OpeningEnd <= at && at <= r.End.AddSeconds(SlackSeconds));
                        work.Alive = Alive(runs, waves, at.AddSeconds(-FrameSampleSeconds), at.AddSeconds(FrameSampleSeconds));
                    }
                    device.RampWork.Add(work);
                    rampTimes.Add((line.Utc, work.Ms));
                    continue;
                }
                Match wave = PerfWave.Match(line.Text);
                Match slow = wave.Success ? Match.Empty : PerfLong.Match(line.Text);
                if (!wave.Success && !slow.Success)
                    continue;
                var frame = new FoundryGate.DeviceFrame { Ms = Number((wave.Success ? wave : slow).Groups["frame"].Value), Wave = wave.Success };
                if (frame.Ms <= FoundryGate.DeviceLongMs)
                    continue;
                // The long frame the ramp's one-time work ran in: FrameTimeProbe writes both lines in the same Update.
                frame.Ramp = !wave.Success && rampTimes.Any(r => r.Ms == frame.Ms &&
                    (r.Utc == line.Utc || r.Utc.HasValue && line.Utc.HasValue && Math.Abs((r.Utc.Value - line.Utc.Value).TotalSeconds) <= RampPairSeconds));
                if (line.Utc.HasValue)
                {
                    DateTime at = line.Utc.Value;
                    frame.Play = (wave.Success || Number(slow.Groups["scale"].Value) >= .5f) &&
                                 runs.Any(r => r.OpeningEnd <= at && at <= r.End.AddSeconds(SlackSeconds));
                    frame.Alive = wave.Success
                        ? int.Parse(wave.Groups["enemies"].Value, CultureInfo.InvariantCulture)
                        : Alive(runs, waves, at.AddSeconds(-FrameSampleSeconds), at.AddSeconds(FrameSampleSeconds));
                }
                device.LongFrames.Add(frame);
            }
        }

        // A window ending at end and lasting seconds: in play when its time scale is 1 and it lies inside a run past the
        // run's opening; alive, the most enemies counted in it.
        private static void Place(FoundryGate.DeviceWindow window, DateTime? end, double seconds, double scale, List<RunRecord> runs,
            List<(DateTime Utc, int Enemies)> waves)
        {
            if (!end.HasValue)
                return;
            DateTime start = end.Value.AddSeconds(-seconds);
            window.Play = scale >= .5 && runs.Any(r => r.OpeningEnd <= start && end.Value <= r.End.AddSeconds(SlackSeconds));
            window.Finale = window.Play && runs.Any(r => r.FinaleStart.HasValue && r.FinaleStart.Value <= start && end.Value <= r.End.AddSeconds(SlackSeconds));
            window.Alive = Alive(runs, waves, start.AddSeconds(-SlackSeconds), end.Value.AddSeconds(SlackSeconds));
        }

        private static int Alive(List<RunRecord> runs, List<(DateTime Utc, int Enemies)> waves, DateTime from, DateTime to)
        {
            int alive = -1;
            foreach (RunRecord run in runs)
            foreach ((DateTime utc, int count) in run.Samples)
                if (utc >= from && utc <= to)
                    alive = Math.Max(alive, count);
            foreach ((DateTime utc, int enemies) in waves)
                if (utc >= from && utc <= to)
                    alive = Math.Max(alive, enemies);
            return alive;
        }

        private static float Number(string text) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : float.NaN;

        // A [RenderStats] value: a number with its unit (B, k, MB) or na.
        private static float Value(Dictionary<string, string> tokens, string key)
        {
            if (!tokens.TryGetValue(key, out string text))
                return float.NaN;
            Match number = Regex.Match(text, @"^[\d.]+");
            return number.Success ? Number(number.Value) : float.NaN;
        }
    }
}
