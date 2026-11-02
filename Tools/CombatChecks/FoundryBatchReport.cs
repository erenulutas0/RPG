using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The regression harness on demand (docs/41 step 5): both heroes' foundry floor 1 over a seed batch, the research's
    // levers against it on the same seeds, and the simulation beside the Editor's own measurement on the Editor's seeds,
    // written to TestResults/foundry-batch.md and foundry-batch.csv. Explicit, so a plain `dotnet test` skips it; run it by
    // name:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch"
    //
    // A floor costs about a third of a second of one core in Release (two to three times that in Debug): the shipped floor
    // for both heroes at 200 seeds takes about fifteen seconds on ten threads, the twelve levers about three minutes more
    // (FOUNDRY_LEVERS=0 skips them, 2 runs FoundryBatch.ComboLevers instead). OneSeedAsTheTelemetryWouldWriteIt traces one seed instead (below).
    //
    // Environment, all optional: FOUNDRY_SEEDS (seeds a hero, 200), FOUNDRY_FIRST_SEED (1), FOUNDRY_LEVERS (0 skips the
    // lever runs), FOUNDRY_PARALLEL (threads, half the processors), FOUNDRY_OUT (folder for the report, the repository's
    // TestResults), FOUNDRY_EDITOR (folder holding the Editor measurement - horde-pacing-*.txt and horde-telemetry/ - the
    // repository's TestResults; nothing there skips the parity table), FOUNDRY_EDITOR_FILES (the two pacing files to read,
    // Vanguard's then Ilya's, comma separated; the newest horde-pacing-m0-* by default).
    [Explicit("The regression harness's batch report: run it by name.")]
    public sealed class FoundryBatchReport
    {
        [Test]
        public void TheFoundryFloorOverASeedBatch()
        {
            int seeds = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int leverSet = Setting("FOUNDRY_LEVERS", 1);
            bool levers = leverSet != 0;
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            string root = RepositoryRoot();
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(root, "TestResults");
            string editor = Environment.GetEnvironmentVariable("FOUNDRY_EDITOR") ?? Path.Combine(root, "TestResults");
            Directory.CreateDirectory(output);

            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };
            int[] batchSeeds = FoundryBatch.Seeds(first, seeds);
            var baselines = heroes.Select(hero => FoundryBatch.Run(hero, FoundryBatch.Shipped(), batchSeeds, parallel)).ToArray();
            var leverRuns = new List<(FoundryBatch.Lever, FoundryBatch.Batch[])>();
            if (levers)
                foreach (FoundryBatch.Lever lever in leverSet == 3 ? FoundryBatch.WardenLevers() :
                         leverSet == 2 ? FoundryBatch.ComboLevers() : FoundryBatch.ResearchLevers())
                    leverRuns.Add((lever, heroes.Select(hero => FoundryBatch.Run(hero, lever.Levers, batchSeeds, parallel)).ToArray()));

            var md = new StringBuilder();
            md.AppendLine("# Foundry floor 1: the regression harness's batch").AppendLine();
            md.AppendLine($"Seeds {first}-{first + seeds - 1} a hero, the development autopilot's policy, DescentSimulation.RunFoundry " +
                          $"({DateTime.Now:yyyy-MM-dd HH:mm}). Computed, not measured: see the parity table for how far it stands from the Editor.").AppendLine();
            md.Append(FoundryBatch.Report(baselines, leverRuns));
            string parity = Parity(editor, heroes, baselines, parallel, out List<FoundryBatch.Batch> editorSeedBatches);
            if (parity != null)
                md.AppendLine(parity);

            File.WriteAllText(Path.Combine(output, "foundry-batch.md"), md.ToString());
            var all = new List<FoundryBatch.Batch>(baselines);
            foreach ((FoundryBatch.Lever _, FoundryBatch.Batch[] batches) in leverRuns)
                all.AddRange(batches);
            all.AddRange(editorSeedBatches);
            File.WriteAllText(Path.Combine(output, "foundry-batch.csv"), FoundryBatch.Csv(all));
            TestContext.WriteLine(md.ToString());
            Assert.That(baselines.All(b => b.Runs.Length == seeds), "every seed ran");
        }

        // One seed's run as the scene's telemetry would write it, to lay beside the Editor's copy of the same seed's events:
        // FOUNDRY_TRACE_HERO (hero_vanguard or hero_cinder_arcanist) and FOUNDRY_TRACE_SEED, into FOUNDRY_OUT.
        [Test]
        public void OneSeedAsTheTelemetryWouldWriteIt()
        {
            string heroId = Environment.GetEnvironmentVariable("FOUNDRY_TRACE_HERO") ?? "hero_vanguard";
            int seed = Setting("FOUNDRY_TRACE_SEED", 11);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            DescentSimulation.FoundryHero hero = heroId == "hero_cinder_arcanist"
                ? DescentSimulation.CinderArcanist()
                : DescentSimulation.Vanguard();
            var lines = new List<string>();
            DescentSimulation.FoundryResult result = DescentSimulation.RunFoundry(hero, seed, trace: lines.Add);
            lines.Add($"{result.FloorSeconds:0.000} end {result.Outcome} level={result.Level}");
            File.WriteAllLines(Path.Combine(output, $"foundry-trace-{heroId}-{seed}.txt"), lines);
            Assert.That(lines.Count, Is.GreaterThan(1));
        }

        private static int Setting(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;

        private static string RepositoryRoot()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            return directory ?? TestContext.CurrentContext.TestDirectory;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Parity with the Editor's HordePacingMeasurement
        // ---------------------------------------------------------------------------------------------------------

        // One Editor seed, from its pacing line and its copied telemetry.
        private sealed class EditorRun
        {
            public int Seed;
            public bool Cleared;
            public float Floor;
            public float Uptime;
            public float Engaged = float.NaN;
            public int Level;
            public float Walk = float.NaN;
            public float Node = float.NaN;
            public float Boss = float.NaN;
            public float Exit = float.NaN;
            public int LevelAtGate = -1;
            public string Death;
            public float[] Rooms = new float[0];
        }

        private static string Parity(string editorFolder, DescentSimulation.FoundryHero[] heroes,
            IReadOnlyList<FoundryBatch.Batch> baselines, int parallel, out List<FoundryBatch.Batch> simulated)
        {
            simulated = new List<FoundryBatch.Batch>();
            if (!Directory.Exists(editorFolder))
                return null;
            string[] files = PacingFiles(editorFolder);
            if (files == null)
                return null;

            var md = new StringBuilder();
            md.AppendLine("### Parity with the Editor measurement (the same seeds)").AppendLine();
            md.AppendLine("Editor: HordePacingMeasurement at a fixed 1/60 s, its per-seed telemetry split as the research's floor_breakdown splits it. " +
                          "Simulation: RunFoundry on the same seeds. Medians over the six seeds (cleared runs for the segments), then the seeds one by one.").AppendLine();
            md.AppendLine("| Hero | Source | Cleared | Floor P50 (all) | Walk | Node fights | Warden | Exit | Level at the gate | Level at the end | Uptime | Engaged | Deaths (room) |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            var perSeed = new StringBuilder();
            for (int h = 0; h < heroes.Length && h < files.Length; h++)
            {
                List<EditorRun> editor = ReadEditor(editorFolder, files[h], heroes[h].Id);
                if (editor.Count == 0)
                    continue;
                int[] seeds = editor.Select(run => run.Seed).ToArray();
                FoundryBatch.Batch batch = FoundryBatch.Run(heroes[h], FoundryBatch.Shipped().Copy("editor seeds"), seeds, parallel);
                simulated.Add(batch);
                EditorRun[] editorClears = editor.Where(run => run.Cleared).ToArray();
                DescentSimulation.FoundryResult[] simClears = batch.Cleared.ToArray();
                md.AppendLine($"| {heroes[h].Name} | Editor ({Path.GetFileName(files[h])}) | {editorClears.Length}/{editor.Count} | " +
                              $"{M(editor.Select(r => r.Floor))} | {M(editorClears.Select(r => r.Walk))} | {M(editorClears.Select(r => r.Node))} | " +
                              $"{M(editorClears.Select(r => r.Boss))} | {M(editorClears.Select(r => r.Exit))} | " +
                              $"{M(editor.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate))} | {M(editor.Select(r => (float)r.Level))} | " +
                              $"{M(editor.Select(r => r.Uptime), "0.00")} | {M(editor.Select(r => r.Engaged), "0.00")} | " +
                              $"{string.Join(", ", editor.Where(r => !r.Cleared).Select(r => r.Death))} |");
                md.AppendLine($"| {heroes[h].Name} | Simulation | {simClears.Length}/{batch.Runs.Length} | " +
                              $"{M(batch.Runs.Select(r => r.FloorSeconds))} | {M(simClears.Select(r => r.WalkTotal))} | {M(simClears.Select(r => r.NodeSeconds))} | " +
                              $"{M(simClears.Select(r => r.BossSeconds))} | {M(simClears.Select(r => r.ExitSeconds))} | " +
                              $"{M(batch.Runs.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate))} | {M(batch.Runs.Select(r => (float)r.Level))} | " +
                              $"{M(batch.Runs.Select(r => r.Uptime), "0.00")} | {M(batch.Runs.Select(r => r.Engaged), "0.00")} | " +
                              $"{string.Join(", ", batch.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).Select(r => $"{r.DeathRoom} {r.DeathPhase}"))} |");
                // Six Editor seeds against the whole batch's spread: each cell P10 / P50 / P90.
                if (h < baselines.Count)
                {
                    FoundryBatch.Batch all = baselines[h];
                    DescentSimulation.FoundryResult[] allClears = all.Cleared.ToArray();
                    md.AppendLine($"| {heroes[h].Name} | Simulation, {all.Runs.Length} seeds (P10 / P50 / P90) | {all.ClearedCount}/{all.Runs.Length} | " +
                                  $"{S(all.Runs.Select(r => r.FloorSeconds))} | {S(allClears.Select(r => r.WalkTotal))} | {S(allClears.Select(r => r.NodeSeconds))} | " +
                                  $"{S(allClears.Select(r => r.BossSeconds))} | {S(allClears.Select(r => r.ExitSeconds))} | " +
                                  $"{S(all.Runs.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate))} | {S(all.Runs.Select(r => (float)r.Level))} | " +
                                  $"{S(all.Runs.Select(r => r.Uptime), "0.00")} | {S(all.Runs.Select(r => r.Engaged), "0.00")} | " +
                                  $"{all.Runs.Count(r => r.Outcome == DescentSimulation.FoundryOutcome.Died && r.BossSeconds < 0f && r.LevelAtGate >= 0)} of " +
                                  $"{all.Runs.Count(r => r.Outcome == DescentSimulation.FoundryOutcome.Died)} deaths at the Warden |");
                }

                perSeed.AppendLine($"| {heroes[h].Name} | seed | Editor: result, floor, walk / node / Warden / exit, level gate / end, rooms | Simulation: the same |");
                perSeed.AppendLine("|---|---|---|---|");
                for (int i = 0; i < editor.Count; i++)
                {
                    EditorRun e = editor[i];
                    DescentSimulation.FoundryResult s = batch.Runs[i];
                    perSeed.AppendLine($"| {heroes[h].Name} | {e.Seed} | {(e.Cleared ? "cleared" : "died " + e.Death)}, {N(e.Floor)}, " +
                                       $"{N(e.Walk)} / {N(e.Node)} / {N(e.Boss)} / {N(e.Exit)}, L{e.LevelAtGate} / L{e.Level}, {string.Join(" ", e.Rooms.Select(r => N(r)))} | " +
                                       $"{(s.Cleared ? "cleared" : $"died {s.DeathRoom} {s.DeathPhase}")}, {N(s.FloorSeconds)}, " +
                                       $"{N(s.WalkTotal)} / {N(s.NodeSeconds)} / {N(s.BossSeconds)} / {N(s.ExitSeconds)}, L{s.LevelAtGate} / L{s.Level}, " +
                                       $"{string.Join(" ", s.RoomSeconds.Select(r => N(r)))} |");
                }
                perSeed.AppendLine();
            }
            md.AppendLine().Append(perSeed);
            return md.ToString();
        }

        private static string M(IEnumerable<float> values, string format = "0")
        {
            double value = FoundryBatch.Median(values.Where(v => !float.IsNaN(v) && v >= 0f));
            return double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string S(IEnumerable<float> values, string format = "0")
        {
            float[] kept = values.Where(v => !float.IsNaN(v) && v >= 0f).ToArray();
            if (kept.Length == 0)
                return "-";
            string P(double p) => FoundryBatch.Percentile(kept, p).ToString(format, CultureInfo.InvariantCulture);
            return $"{P(10)} / {P(50)} / {P(90)}";
        }

        private static string N(float value) =>
            float.IsNaN(value) || value < 0f ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

        private static string[] PacingFiles(string folder)
        {
            string chosen = Environment.GetEnvironmentVariable("FOUNDRY_EDITOR_FILES");
            if (!string.IsNullOrEmpty(chosen))
                return chosen.Split(',').Select(name => Path.Combine(folder, name.Trim())).ToArray();
            string vanguard = Newest(folder, "horde-pacing-m0-vanguard*.txt") ?? Newest(folder, "horde-pacing-hero_vanguard*.txt");
            string ilya = Newest(folder, "horde-pacing-m0-ilya*.txt") ?? Newest(folder, "horde-pacing-hero_cinder_arcanist*.txt");
            return vanguard != null && ilya != null ? new[] { vanguard, ilya } : null;
        }

        private static string Newest(string folder, string pattern) =>
            Directory.GetFiles(folder, pattern).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();

        // The pacing file's tab-separated lines (seed, result, floor_s, uptime[, engaged], ..., level, ..., rooms), and,
        // where the seed's telemetry was kept, its walks, node fights, the Warden's wave and the level at the gate.
        private static List<EditorRun> ReadEditor(string folder, string pacing, string heroId)
        {
            var runs = new List<EditorRun>();
            if (!File.Exists(pacing))
                return runs;
            string[] lines = File.ReadAllLines(pacing);
            string[] header = lines.FirstOrDefault(l => l.StartsWith("seed\t", StringComparison.Ordinal))?.Split('\t');
            if (header == null)
                return runs;
            int Column(string name) => Array.IndexOf(header, name);
            foreach (string line in lines)
            {
                string[] cells = line.Split('\t');
                if (cells.Length < header.Length - 1 || !int.TryParse(cells[0], out int seed))
                    continue;
                var run = new EditorRun
                {
                    Seed = seed,
                    Cleared = cells[Column("result")] == "cleared",
                    Floor = Float(cells[Column("floor_s")]),
                    Uptime = Float(cells[Column("uptime")]),
                    Level = int.Parse(cells[Column("level")], CultureInfo.InvariantCulture)
                };
                if (Column("engaged") >= 0)
                    run.Engaged = Float(cells[Column("engaged")]);
                if (Column("rooms") >= 0)
                    run.Rooms = cells[Column("rooms")].Split(' ').Where(r => r.Contains(":"))
                        .Select(r => Float(r.Substring(r.IndexOf(':') + 1).TrimEnd('s'))).ToArray();
                ReadTelemetry(Path.Combine(folder, "horde-telemetry", heroId + "-" + seed), run);
                runs.Add(run);
            }
            return runs;
        }

        // Pacing files from a Turkish-locale Editor write a decimal comma.
        private static float Float(string text) =>
            float.Parse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

        private static void ReadTelemetry(string folder, EditorRun run)
        {
            if (!Directory.Exists(folder))
                return;
            var events = new SortedDictionary<long, JsonElement>();
            foreach (string file in Directory.GetFiles(folder, "*.jsonl"))
            foreach (string line in File.ReadLines(file))
            {
                if (line.Trim().Length == 0)
                    continue;
                JsonElement e = JsonDocument.Parse(line).RootElement;
                if (e.TryGetProperty("seq", out JsonElement seq))
                    events[seq.GetInt64()] = e;
            }
            float walk = 0f, node = 0f;
            int level = 0;
            string roomKind = null;
            float roomWalk = 0f;
            float bossWave = float.NaN;
            foreach (JsonElement e in events.Values)
            {
                string name = e.GetProperty("event").GetString();
                if (e.TryGetProperty("run_level", out JsonElement runLevel))
                    level = Math.Max(level, runLevel.GetInt32());
                if (e.TryGetProperty("level", out JsonElement eventLevel) && eventLevel.ValueKind == JsonValueKind.Number)
                    level = Math.Max(level, eventLevel.GetInt32());
                switch (name)
                {
                    case "room_start":
                        roomKind = e.GetProperty("room_kind").GetString();
                        roomWalk = 0f;
                        break;
                    case "hall_reached":
                        roomWalk += (float)e.GetProperty("walk_sec").GetDouble();
                        walk += (float)e.GetProperty("walk_sec").GetDouble();
                        break;
                    case "boss_start":
                        run.LevelAtGate = level;
                        break;
                    case "wave_clear" when roomKind == "boss":
                        bossWave = (float)e.GetProperty("active_sec").GetDouble();
                        break;
                    case "room_complete":
                        float active = (float)e.GetProperty("active_sec").GetDouble();
                        if (roomKind == "boss")
                        {
                            run.Boss = bossWave;
                            run.Exit = active - bossWave;
                        }
                        else
                        {
                            node += active - roomWalk;
                        }
                        break;
                    case "run_end":
                        if (e.TryGetProperty("result", out JsonElement result) && result.GetString() == "defeat")
                            run.Death = $"{e.GetProperty("rooms_completed").GetInt32() + 1} ({e.GetProperty("cause").GetString()})";
                        break;
                }
            }
            run.Walk = walk;
            run.Node = node;
        }
    }
}
