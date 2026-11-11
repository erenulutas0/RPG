using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The regression harness on demand (docs/41 step 5): the V0.1 gate read from one report (FoundryGate, first, and alone
    // in foundry-gate.md), both heroes' foundry floor 1 over a seed batch, the research's levers against it on the same
    // seeds, and the simulation beside the Editor's own measurement on the Editor's seeds, written to
    // TestResults/foundry-batch.md and foundry-batch.csv. Explicit, so a plain `dotnet test` skips it; run it by name:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch"
    //
    // A floor costs about a third of a second of one core in Release (two to three times that in Debug): the shipped floor
    // for both heroes at 200 seeds takes about fifteen seconds on ten threads, the gate's Vanguard started with the Staff
    // and the Daggers about as long again, and each lever about fifteen seconds more (FoundryBatch.ResearchLevers: the
    // pre-V0.1 floor, the research's twelve on it and V0.1's three taken away; FOUNDRY_LEVERS=0 skips them, 2 runs
    // FoundryBatch.ComboLevers instead, 3 the Warden's candidates, FoundryBatch.WardenLevers, 4 the Sword's research
    // patterns, FoundryBatch.SwordLevers - TheSwordsRoleOverASeedBatch reads those as the gate's roles row; 5 the gate's
    // levers of docs/41's open decision 2, FoundryBatch.GateLevers, and 6 the lever sets made of them,
    // FoundryBatch.GateLeverSets - each played by every cohort, or those FOUNDRY_SWEEP_COHORTS names, into
    // foundry-gate-levers.md and .csv; FOUNDRY_LEVER_FILTER keeps the levers whose names hold one of its comma-separated
    // words; 7 the difficulty research's structural candidates, FoundryBatch.DifficultyLevers; 8 floor 2's Warden options,
    // FoundryBatch.FloorTwoWardenLevers, played as 5 and 6 are - with FOUNDRY_FLOORS=2 - and with 5, 6 and 8 each lever's
    // cohort section into foundry-gate-levers-cohorts.md and every run's two floors into foundry-gate-levers-floors.csv;
    // FOUNDRY_REALISM plays their cohorts with a human's hands too). FOUNDRY_COHORTS=1 adds the
    // autopilot's cohorts on the same seeds and floors (FoundryCohorts: four more batches a hero, alone in
    // foundry-cohorts.md too; with FOUNDRY_FLOORS=2 each cohort goes down to floor 2 as well, and the section reads it;
    // FOUNDRY_REALISM - typical, slow or a reaction in seconds - plays every cohort with a human's hands, PilotRealism).
    // OneSeedAsTheTelemetryWouldWriteIt traces one seed instead (below).
    //
    // Environment, all optional: FOUNDRY_SEEDS (seeds a hero, 200), FOUNDRY_FIRST_SEED (1), FOUNDRY_LEVERS (0 skips the
    // lever runs), FOUNDRY_COHORTS (1 adds the cohorts), FOUNDRY_PARALLEL (threads, half the processors), FOUNDRY_OUT (folder for the report, the repository's
    // TestResults), FOUNDRY_EDITOR (folder holding the Editor measurement - horde-pacing-*.txt, horde-telemetry/ and the
    // perf probe's summary - the repository's TestResults; nothing there skips the parity table and leaves the gate's Editor
    // column empty), FOUNDRY_EDITOR_FILES (the two pacing files the gate and the parity read, Vanguard's then Ilya's, comma
    // separated; by default each hero's newest file of the plain autopilot, the newest naming the simulated floor's levers
    // first), FOUNDRY_PERF_PROBE (the perf probe's file; the newest perf-probe-baseline-*.txt by default). Both flag a
    // pacing file whose header names other floor levers than the simulation's shipped ones, or none (written before
    // 2026-11-03). FOUNDRY_DEVICE (the phone's pulls, Tools/Device/Pull-DeviceEvidence.ps1's folders, read by DevicePull
    // for the gate's rows of the owner's floor time and the device's frames: a folder or several separated by ';',
    // absolute or within the Editor folder, or "none"; by default every device-* folder there holding a pull.txt - with
    // none both rows stay not measured, as before the script). FOUNDRY_FLOORS (1): 2 has the shipped floor's runs go down
    // to floor 2 after a floor-1 clear (FoundryLevers.Floors) and adds floor 2's section and foundry-batch-floor2.csv; the
    // gate, floor 1's tables, the levers' and the parity table stay floor 1's.
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
            int floors = Setting("FOUNDRY_FLOORS", 1);
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            shipped.Floors = floors;
            var baselines = heroes.Select(hero => FoundryBatch.Run(hero, shipped, batchSeeds, parallel)).ToArray();
            FoundryBatch.Batch[] weapons = FoundryBatch.RunWeapons(baselines[0], batchSeeds, parallel);
            var leverRuns = new List<(FoundryBatch.Lever, FoundryBatch.Batch[])>();
            // docs/41's open decision 2: the gate's levers (5) or the lever sets made of them (6), each played by the
            // autopilot's cohorts (FOUNDRY_SWEEP_COHORTS, all by default), floor 1 alone; FOUNDRY_LEVER_FILTER keeps the
            // levers whose names hold one of its comma-separated words.
            // 8: floor 2's Warden options (FoundryBatch.FloorTwoWardenLevers), the same way. FOUNDRY_REALISM plays these
            // cohorts with a human's hands too, as it does the cohort section's.
            var gateLevers = new List<FoundryBatch.CohortLever>();
            if (leverSet == 5 || leverSet == 6 || leverSet == 8)
            {
                levers = false;
                List<Cryptforge.Core.AutopilotCohort> sweepCohorts =
                    FoundryBatch.SweepCohorts(Environment.GetEnvironmentVariable("FOUNDRY_SWEEP_COHORTS"));
                FoundryBatch.Lever[] list = FoundryBatch.Filtered(
                    leverSet == 5 ? FoundryBatch.GateLevers() : leverSet == 8 ? FoundryBatch.FloorTwoWardenLevers() : FoundryBatch.GateLeverSets(),
                    Environment.GetEnvironmentVariable("FOUNDRY_LEVER_FILTER"));
                PilotRealism leverRealism = PilotRealism.Named(Environment.GetEnvironmentVariable("FOUNDRY_REALISM"));
                foreach (FoundryBatch.Lever lever in list)
                    gateLevers.Add(FoundryBatch.RunCohorts(lever, heroes, batchSeeds, parallel, sweepCohorts, floors, leverRealism));
            }
            if (levers)
                foreach (FoundryBatch.Lever lever in leverSet == 7 ? FoundryBatch.DifficultyLevers() :
                         leverSet == 4 ? FoundryBatch.SwordLevers() :
                         leverSet == 3 ? FoundryBatch.WardenLevers() :
                         leverSet == 2 ? FoundryBatch.ComboLevers() : FoundryBatch.ResearchLevers())
                    leverRuns.Add((lever, heroes.Select(hero => FoundryBatch.Run(hero, lever.Levers, batchSeeds, parallel)).ToArray()));

            string gate = FoundryGate.Section(baselines, weapons, GateEvidence(root, editor, heroes));
            var md = new StringBuilder();
            md.AppendLine("# Foundry floor 1: the regression harness's batch").AppendLine();
            md.AppendLine($"Seeds {first}-{first + seeds - 1} a hero, the development autopilot's policy, DescentSimulation.RunFoundry " +
                          $"({DateTime.Now:yyyy-MM-dd HH:mm}). Computed, not measured: see the parity table for how far it stands from the Editor.").AppendLine();
            md.Append(gate);
            md.AppendLine("## The batch").AppendLine();
            md.Append(FoundryBatch.Report(baselines, leverRuns));
            if (floors >= 2)
            {
                md.Append(FoundryBatch.FloorTwoReport(baselines));
                File.WriteAllText(Path.Combine(output, "foundry-batch-floor2.csv"), FoundryBatch.FloorTwoCsv(baselines));
            }
            if (gateLevers.Count > 0)
            {
                string section = FoundryBatch.GateLeverSection(gateLevers,
                    leverSet == 5 ? "The gate's levers (docs/41's open decision 2)"
                    : leverSet == 8 ? "Floor 2's Warden (docs/41, what waits)" : "The lever sets for the owner (docs/41's open decision 2)");
                md.Append(section);
                File.WriteAllText(Path.Combine(output, "foundry-gate-levers.md"), section);
                File.WriteAllText(Path.Combine(output, "foundry-gate-levers.csv"), FoundryBatch.GateLeverCsv(gateLevers));
                // Each lever's cohort section (both floors when FOUNDRY_FLOORS=2) and every run's two floors on one line.
                var perLever = new StringBuilder();
                foreach (FoundryBatch.CohortLever lever in gateLevers)
                    perLever.AppendLine("# " + lever.Lever.Name).AppendLine().Append(FoundryCohorts.Section(lever.Cohorts));
                File.WriteAllText(Path.Combine(output, "foundry-gate-levers-cohorts.md"), perLever.ToString());
                File.WriteAllText(Path.Combine(output, "foundry-gate-levers-floors.csv"), FoundryBatch.GateLeverFloorsCsv(gateLevers));
            }
            string parity = Parity(editor, heroes, baselines, parallel, out List<FoundryBatch.Batch> editorSeedBatches);
            if (parity != null)
                md.AppendLine(parity);
            // The autopilot's cohorts on the same seeds (docs/41 step 5), on demand.
            var cohortRuns = new List<(Cryptforge.Core.AutopilotCohort Cohort, FoundryBatch.Batch[] Batches)>();
            if (Setting("FOUNDRY_COHORTS", 0) != 0)
            {
                cohortRuns = FoundryCohorts.Run(heroes, batchSeeds, parallel, baselines, floors,
                    PilotRealism.Named(Environment.GetEnvironmentVariable("FOUNDRY_REALISM")));
                string cohorts = FoundryCohorts.Section(cohortRuns);
                md.Append(cohorts);
                File.WriteAllText(Path.Combine(output, "foundry-cohorts.md"), cohorts);
                if (floors >= 2)
                    File.WriteAllText(Path.Combine(output, "foundry-cohorts-floor2.csv"),
                        FoundryBatch.FloorTwoCsv(cohortRuns.SelectMany(c => c.Batches)));
            }
            // The finale after floor 2 (FOUNDRY_FLOORS=3): every cohort's when they ran (their section carries it), else the
            // shipped pilot's, alone in foundry-finale.md and a row a run that reached it in foundry-finale.csv.
            if (floors >= 3)
            {
                List<(string Label, FoundryBatch.Batch Batch)> finaleRows = cohortRuns.Count > 0
                    ? FoundryCohorts.FinaleRows(cohortRuns)
                    : baselines.Select(b => ("careful", b)).ToList();
                // The Editor's Descent beside it, where the pacing files the gate reads hold one (horde-floors.txt "3").
                string editorDescent = EditorDescentSection(editor, heroes, baselines, parallel);
                string finale = FoundryBatch.FinaleSection(finaleRows) + editorDescent;
                if (cohortRuns.Count == 0)
                    md.Append(finale);
                else
                    md.Append(editorDescent);
                File.WriteAllText(Path.Combine(output, "foundry-finale.md"), finale);
                File.WriteAllText(Path.Combine(output, "foundry-finale.csv"), FoundryBatch.FinaleCsv(finaleRows.Select(r => r.Batch)));
            }

            File.WriteAllText(Path.Combine(output, "foundry-batch.md"), md.ToString());
            File.WriteAllText(Path.Combine(output, "foundry-gate.md"), gate);
            var all = new List<FoundryBatch.Batch>(baselines);
            all.AddRange(weapons.Where(batch => !baselines.Contains(batch)));
            foreach ((FoundryBatch.Lever _, FoundryBatch.Batch[] batches) in leverRuns)
                all.AddRange(batches);
            all.AddRange(editorSeedBatches);
            all.AddRange(cohortRuns.SelectMany(c => c.Batches).Where(batch => !baselines.Contains(batch)));
            File.WriteAllText(Path.Combine(output, "foundry-batch.csv"), FoundryBatch.Csv(all));
            TestContext.WriteLine(md.ToString());
            Assert.That(baselines.All(b => b.Runs.Length == seeds), "every seed ran");
        }

        // The Sword's role (docs/cloud/2026-11-04-sword-role.md): each of FoundryBatch.SwordLevers - foundry-only Sword
        // patterns behind research levers - for both heroes in every cohort's hands and with the Vanguard started with each
        // weapon, read as the gate's roles row beside clears, floor, Warden and its cost. Into FOUNDRY_OUT: sword-role.md
        // (a row a lever) and sword-role.csv. SWORD_LEVERS picks rows by index (comma separated, all by default);
        // FOUNDRY_SEEDS, FOUNDRY_FIRST_SEED and FOUNDRY_PARALLEL as above.
        [Test]
        public void TheSwordsRoleOverASeedBatch()
        {
            int seeds = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            FoundryBatch.Lever[] all = FoundryBatch.SwordLevers();
            string pick = Environment.GetEnvironmentVariable("SWORD_LEVERS");
            IEnumerable<int> chosen = string.IsNullOrEmpty(pick)
                ? Enumerable.Range(0, all.Length)
                : pick.Split(',').Select(p => int.Parse(p.Trim(), CultureInfo.InvariantCulture));
            int[] batchSeeds = FoundryBatch.Seeds(first, seeds);
            var md = new StringBuilder();
            md.AppendLine($"# The Sword's role: seeds {first}-{first + seeds - 1} a hero ({DateTime.Now:yyyy-MM-dd HH:mm})").AppendLine();
            md.AppendLine(FoundryBatch.SwordHeader());
            var csv = new StringBuilder().AppendLine(FoundryBatch.SwordCsvHeader());
            foreach (int index in chosen)
            {
                FoundryBatch.SwordRun run = FoundryBatch.RunSword(all[index], batchSeeds, parallel);
                md.AppendLine(FoundryBatch.SwordRow(run));
                foreach (string line in FoundryBatch.SwordCsv(run))
                    csv.AppendLine(line);
                File.WriteAllText(Path.Combine(output, "sword-role.md"), md.ToString());
                File.WriteAllText(Path.Combine(output, "sword-role.csv"), csv.ToString());
            }
            TestContext.WriteLine(md.ToString());
            Assert.That(md.Length, Is.GreaterThan(0));
        }

        // The Daggers' role (docs/41, status 2026-11-10 night): each of FoundryBatch.DaggersLevers for both heroes in every
        // cohort's hands and with the Vanguard started with each weapon, down FOUNDRY_FLOORS floors (2 by default), read as
        // the gate's three jobs - horde, elites, Warden - beside the clears and the Warden's fight; frame-perfect, or with
        // FOUNDRY_REALISM's hands. Into FOUNDRY_OUT: daggers-role.md (a row a lever) and daggers-role.csv. DAGGERS_LEVERS
        // picks rows by index (comma separated, all by default); DAGGERS_COHORTS plays only the cohorts it names (comma
        // separated, the careful one always first; every cohort by default), a screen of the jobs; FOUNDRY_SEEDS,
        // FOUNDRY_FIRST_SEED and FOUNDRY_PARALLEL as above.
        [Test]
        public void TheDaggersRoleOverASeedBatch()
        {
            int seeds = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int floors = Setting("FOUNDRY_FLOORS", 2);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            PilotRealism realism = PilotRealism.Named(Environment.GetEnvironmentVariable("FOUNDRY_REALISM"));
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            FoundryBatch.Lever[] all = FoundryBatch.DaggersLevers();
            string pick = Environment.GetEnvironmentVariable("DAGGERS_LEVERS");
            IEnumerable<int> chosen = string.IsNullOrEmpty(pick)
                ? Enumerable.Range(0, all.Length)
                : pick.Split(',').Select(p => int.Parse(p.Trim(), CultureInfo.InvariantCulture));
            string screen = Environment.GetEnvironmentVariable("DAGGERS_COHORTS");
            List<Cryptforge.Core.AutopilotCohort> cohorts = string.IsNullOrWhiteSpace(screen) ? null : FoundryBatch.SweepCohorts(screen);
            int[] batchSeeds = FoundryBatch.Seeds(first, seeds);
            var md = new StringBuilder();
            md.AppendLine($"# The Daggers' role: seeds {first}-{first + seeds - 1} a hero, {floors} floor(s) ({DateTime.Now:yyyy-MM-dd HH:mm})").AppendLine();
            md.AppendLine(FoundryBatch.RoleHeader());
            var csv = new StringBuilder().AppendLine(FoundryBatch.RoleCsvHeader());
            foreach (int index in chosen)
            {
                FoundryBatch.RoleRun run = FoundryBatch.RunRoles(all[index], batchSeeds, parallel, floors, realism, cohorts);
                md.AppendLine(FoundryBatch.RoleRow(run));
                foreach (string line in FoundryBatch.RoleCsv(run))
                    csv.AppendLine(line);
                File.WriteAllText(Path.Combine(output, "daggers-role.md"), md.ToString());
                File.WriteAllText(Path.Combine(output, "daggers-role.csv"), csv.ToString());
            }
            TestContext.WriteLine(md.ToString());
            Assert.That(md.Length, Is.GreaterThan(0));
        }

        // One seed's run as the scene's telemetry would write it, to lay beside the Editor's copy of the same seed's events:
        // FOUNDRY_TRACE_HERO (hero_vanguard or hero_cinder_arcanist) and FOUNDRY_TRACE_SEED, into FOUNDRY_OUT; FOUNDRY_FLOORS=2
        // traces floor 2 after a floor-1 clear too.
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
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped();
            levers.Floors = Setting("FOUNDRY_FLOORS", 1);
            DescentSimulation.FoundryResult result = DescentSimulation.RunFoundry(hero, seed, levers, trace: lines.Add);
            lines.Add($"{result.FloorSeconds:0.000} end {result.Outcome} level={result.Level}");
            for (DescentSimulation.FoundryResult floor = result.NextFloor; floor != null; floor = floor.NextFloor)
                lines.Add($"floor {floor.Floor}: {floor.FloorSeconds:0.000} end {floor.Outcome} level={floor.Level}");
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
        // The V0.1 gate's evidence (FoundryGate)
        // ---------------------------------------------------------------------------------------------------------

        // What the gate reads beside the batch: each hero's pacing file (as the parity's), the newest file a weapon started
        // (horde-weapon.txt, the research's M1), the perf probe's summary, floor 1's asset and the phone's pulls.
        private static FoundryGate.Evidence GateEvidence(string root, string folder, DescentSimulation.FoundryHero[] heroes)
        {
            var evidence = new FoundryGate.Evidence();
            ReadNextFloor(root, evidence);
            evidence.Device = DevicePull.Read(DevicePull.Folders(folder, Environment.GetEnvironmentVariable("FOUNDRY_DEVICE")));
            if (!Directory.Exists(folder))
                return evidence;
            string[] files = PacingFiles(folder, heroes) ?? heroes.Select(hero => NewestPacing(folder, hero.Id, null)).ToArray();
            for (int h = 0; h < heroes.Length && h < files.Length; h++)
                if (files[h] != null && File.Exists(files[h]))
                    evidence.Files.Add(GateFile(folder, files[h], heroes[h].Id, null));
            foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
            {
                string file = NewestPacing(folder, FoundryGate.V01HeroId, weapon.Id);
                if (file != null)
                    evidence.Files.Add(GateFile(folder, file, FoundryGate.V01HeroId, weapon.Id));
            }
            evidence.Frames = ReadFrames(folder);
            return evidence;
        }

        private static FoundryGate.EditorFile GateFile(string folder, string pacing, string heroId, string weaponId)
        {
            Dictionary<string, string> levers = ReadLevers(pacing);
            string mismatch = LeverMismatch(levers, FoundryBatch.Shipped());
            var file = new FoundryGate.EditorFile
            {
                Source = Path.GetFileName(pacing),
                HeroId = heroId,
                WeaponId = weaponId,
                OtherFloor = mismatch == null ? null
                    : levers == null ? "it names no floor levers (written before 2026-11-03, before that day's Warden and bounded drop detour)"
                    : "it was " + mismatch
            };
            foreach (EditorRun run in ReadEditor(folder, pacing, heroId))
                file.Seeds.Add(new FoundryGate.EditorSeed
                {
                    Seed = run.Seed, Cleared = run.Cleared, Floor = run.Floor, Uptime = run.Uptime, Engaged = run.Engaged,
                    Boss = run.BossColumn, Clock = run.Clock, LastRoom = run.LastRoom,
                    Evolutions = run.TelemetryRead ? run.Evolutions : -1, GateWait = run.TelemetryRead ? run.GateWait : float.NaN
                });
            return file;
        }

        // The Editor perf probe's summary (Tests/PlayMode/PerfProbe.cs): FOUNDRY_PERF_PROBE names the file in the Editor
        // folder; by default the newest perf-probe-baseline-*.txt, the baseline kept, else the probe's own perf-probe.txt.
        private static FoundryGate.EditorFrames ReadFrames(string folder)
        {
            string chosen = Environment.GetEnvironmentVariable("FOUNDRY_PERF_PROBE");
            string path = !string.IsNullOrEmpty(chosen) ? Path.Combine(folder, chosen)
                : Directory.GetFiles(folder, "perf-probe-baseline-*.txt").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                  ?? Path.Combine(folder, "perf-probe.txt");
            if (!File.Exists(path))
                return null;
            const string title = "Cryptforge perf probe, ";
            var frames = new FoundryGate.EditorFrames { Source = Path.GetFileName(path), Machine = "?" };
            FoundryGate.FrameWindow window = null;
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith(title, StringComparison.Ordinal))
                {
                    frames.Machine = line.Substring(title.Length);
                    continue;
                }
                Match section = Regex.Match(line, @"^== (.+?): (\d+) frames measured");
                if (section.Success)
                {
                    window = new FoundryGate.FrameWindow
                    {
                        Name = section.Groups[1].Value, Frames = int.Parse(section.Groups[2].Value, CultureInfo.InvariantCulture)
                    };
                    frames.Windows.Add(window);
                    continue;
                }
                if (window == null)
                    continue;
                Match crowd = Regex.Match(line, @"crowd: enemies alive [\d.]+ / [\d.]+ / ([\d.]+)");
                if (crowd.Success)
                    window.AliveMax = Float(crowd.Groups[1].Value);
                Match wall = Regex.Match(line, @"frame ms \(wall\): median [\d.]+, p95 ([\d.]+), p99 [\d.]+, max ([\d.]+).*over 16\.7 ms: (\d+), over 33\.3 ms: (\d+)");
                if (wall.Success)
                {
                    window.P95 = Float(wall.Groups[1].Value);
                    window.Max = Float(wall.Groups[2].Value);
                    window.Over16 = int.Parse(wall.Groups[3].Value, CultureInfo.InvariantCulture);
                    window.Over33 = int.Parse(wall.Groups[4].Value, CultureInfo.InvariantCulture);
                }
            }
            return frames;
        }

        // Floor 1's asset (the foundry simulation's FoundryFloorOne): the floor it leads to, by the asset that holds the
        // _nextFloor reference's guid, or none.
        private static void ReadNextFloor(string root, FoundryGate.Evidence evidence)
        {
            string floors = Path.Combine(root, "Assets", "_Project", "Data", "Floors");
            string asset = Path.Combine(floors, "Floor_EmberHalls.asset");
            if (!File.Exists(asset))
                return;
            evidence.FloorRead = true;
            evidence.FloorAsset = Path.GetFileNameWithoutExtension(asset);
            string reference = File.ReadLines(asset).FirstOrDefault(l => l.TrimStart().StartsWith("_nextFloor:", StringComparison.Ordinal));
            Match next = Regex.Match(reference ?? string.Empty, @"fileID:\s*(-?\d+)(?:,\s*guid:\s*([0-9a-f]{32}))?");
            if (!next.Success || next.Groups[1].Value == "0")
                return;
            string guid = next.Groups[2].Value;
            string meta = Directory.GetFiles(Path.Combine(root, "Assets", "_Project", "Data"), "*.asset.meta", SearchOption.AllDirectories)
                .FirstOrDefault(path => guid.Length > 0 && File.ReadLines(path).Any(l => l.Trim() == "guid: " + guid));
            evidence.NextFloor = meta != null ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(meta)) : "guid " + guid;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Parity with the Editor's HordePacingMeasurement
        // ---------------------------------------------------------------------------------------------------------

        // One Editor seed, from its pacing line and, where it is this line's run, its copied telemetry.
        internal sealed class EditorRun
        {
            public int Seed;
            public bool Cleared;
            public float Floor;
            public float Uptime;
            public float Engaged = float.NaN;
            public int Level;
            public float Walk = float.NaN;
            public float Node = float.NaN;
            // The room before the gate holding for the floor's clock: room_complete's gate_hold_sec, which telemetry
            // before 2026-11-03 never wrote (its node fights include the wait).
            public float GateWait = float.NaN;
            public float Boss = float.NaN;
            public float Exit = float.NaN;
            public int LevelAtGate = -1;
            public string Death;
            public float[] Rooms = new float[0];
            // The room the run ended in, the rooms column's last; 0 where it names none.
            public int LastRoom;
            // The pacing line's boss_s (the Warden's fight) and clock (the floor's clock at the end).
            public float BossColumn = float.NaN;
            public float Clock = float.NaN;
            // Whether the seed's telemetry folder held this line's run (the measurement overwrites it with every run), and
            // the evolutions it took (upgrade_selected of an evolution).
            public bool TelemetryRead;
            public int Evolutions = -1;
        }

        // A seed's telemetry is this pacing line's run when the run's last active second is the line's floor_s, which the
        // pacing file rounds to the second; a later measurement of the same seed overwrote any other.
        private const float TelemetryMatchSeconds = 1.5f;

        private static string Parity(string editorFolder, DescentSimulation.FoundryHero[] heroes,
            IReadOnlyList<FoundryBatch.Batch> baselines, int parallel, out List<FoundryBatch.Batch> simulated)
        {
            simulated = new List<FoundryBatch.Batch>();
            if (!Directory.Exists(editorFolder))
                return null;
            string[] files = PacingFiles(editorFolder, heroes);
            if (files == null)
                return null;

            var md = new StringBuilder();
            md.AppendLine("### Parity with the Editor measurement (the same seeds)").AppendLine();
            md.AppendLine("Editor: HordePacingMeasurement at a fixed 1/60 s, its per-seed telemetry split as the research's floor_breakdown splits it, the gate wait (room_complete's gate_hold_sec) apart from the node fights. " +
                          "Simulation: RunFoundry on the same seeds. Medians over the six seeds (cleared runs for the segments), then the seeds one by one.").AppendLine();
            // A file measured on another floor than the simulation's - another V0.1 lever, or written before files named
            // theirs - is flagged rather than refused: its rows still read, but not as the same floor.
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            var otherFloor = new bool[files.Length];
            var mismatches = new List<string>();
            for (int h = 0; h < heroes.Length && h < files.Length; h++)
            {
                string mismatch = LeverMismatch(ReadLevers(files[h]), shipped);
                otherFloor[h] = mismatch != null;
                if (mismatch != null)
                    mismatches.Add($"{heroes[h].Name}'s {Path.GetFileName(files[h])} {mismatch}");
            }
            if (mismatches.Count > 0)
                md.AppendLine($"**Not the simulated floor** (hall_surges {shipped.HallSurges}, extra_elites {shipped.ExtraElites}, gate_s " +
                              $"{shipped.GateNotBeforeSeconds.ToString("0.###", CultureInfo.InvariantCulture)}): " +
                              string.Join("; ", mismatches) + ". Read those rows as another floor's.").AppendLine();
            md.AppendLine("| Hero | Source | Cleared | Floor P50 (all) | Walk | Node fights | Gate wait | Warden | Exit | Level at the gate | Level at the end | Uptime | Engaged | Deaths (room) |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
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
                md.AppendLine($"| {heroes[h].Name} | Editor ({Path.GetFileName(files[h])}{(otherFloor[h] ? ", **another floor**" : string.Empty)}) | {editorClears.Length}/{editor.Count} | " +
                              $"{M(editor.Select(r => r.Floor))} | {M(editorClears.Select(r => r.Walk))} | {M(editorClears.Select(r => r.Node))} | " +
                              $"{M(editorClears.Select(r => r.GateWait))} | {M(editorClears.Select(r => r.Boss))} | {M(editorClears.Select(r => r.Exit))} | " +
                              $"{M(editor.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate))} | {M(editor.Select(r => (float)r.Level))} | " +
                              $"{M(editor.Select(r => r.Uptime), "0.00")} | {M(editor.Select(r => r.Engaged), "0.00")} | " +
                              $"{string.Join(", ", editor.Where(r => !r.Cleared).Select(r => r.Death))} |");
                md.AppendLine($"| {heroes[h].Name} | Simulation | {simClears.Length}/{batch.Runs.Length} | " +
                              $"{M(batch.Runs.Select(r => r.FloorSeconds))} | {M(simClears.Select(r => r.WalkTotal))} | {M(simClears.Select(r => r.NodeSeconds))} | " +
                              $"{M(simClears.Select(r => r.GateWaitSeconds))} | {M(simClears.Select(r => r.BossSeconds))} | {M(simClears.Select(r => r.ExitSeconds))} | " +
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
                                  $"{S(allClears.Select(r => r.GateWaitSeconds))} | {S(allClears.Select(r => r.BossSeconds))} | {S(allClears.Select(r => r.ExitSeconds))} | " +
                                  $"{S(all.Runs.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate))} | {S(all.Runs.Select(r => (float)r.Level))} | " +
                                  $"{S(all.Runs.Select(r => r.Uptime), "0.00")} | {S(all.Runs.Select(r => r.Engaged), "0.00")} | " +
                                  $"{all.Runs.Count(r => r.DiedAtWarden)} of " +
                                  $"{all.Runs.Count(r => r.Outcome == DescentSimulation.FoundryOutcome.Died)} deaths at the Warden |");
                }

                perSeed.AppendLine($"| {heroes[h].Name} | seed | Editor: result, floor, walk / node / gate wait / Warden / exit, level gate / end, rooms | Simulation: the same |");
                perSeed.AppendLine("|---|---|---|---|");
                for (int i = 0; i < editor.Count; i++)
                {
                    EditorRun e = editor[i];
                    DescentSimulation.FoundryResult s = batch.Runs[i];
                    perSeed.AppendLine($"| {heroes[h].Name} | {e.Seed} | {(e.Cleared ? "cleared" : "died " + e.Death)}, {N(e.Floor)}, " +
                                       $"{N(e.Walk)} / {N(e.Node)} / {N(e.GateWait)} / {N(e.Boss)} / {N(e.Exit)}, L{e.LevelAtGate} / L{e.Level}, {string.Join(" ", e.Rooms.Select(r => N(r)))} | " +
                                       $"{(s.Cleared ? "cleared" : $"died {s.DeathRoom} {s.DeathPhase}")}, {N(s.FloorSeconds)}, " +
                                       $"{N(s.WalkTotal)} / {N(s.NodeSeconds)} / {N(s.GateWaitSeconds)} / {N(s.BossSeconds)} / {N(s.ExitSeconds)}, L{s.LevelAtGate} / L{s.Level}, " +
                                       $"{string.Join(" ", s.RoomSeconds.Select(r => N(r)))} |");
                }
                perSeed.AppendLine();
            }
            md.AppendLine().Append(perSeed);
            return md.ToString();
        }

        // The Editor's Descent (HordePacingMeasurement with horde-floors.txt "3", EditorDescent): the descent rows of the
        // pacing files the gate and the parity read (FOUNDRY_EDITOR_FILES, or each hero's newest), each beside RunFoundry
        // on its seeds down three floors - the file's hero, starting weapon and cohort, frame-perfect as the Editor's
        // autopilot - and the batch's own careful pilot where it went down three floors. Empty where no file holds any.
        private static string EditorDescentSection(string folder, DescentSimulation.FoundryHero[] heroes,
            IReadOnlyList<FoundryBatch.Batch> baselines, int parallel)
        {
            if (!Directory.Exists(folder))
                return string.Empty;
            string[] files = PacingFiles(folder, heroes);
            if (files == null)
                return string.Empty;
            var rows = new List<EditorDescent.HeroRows>();
            for (int h = 0; h < files.Length; h++)
            {
                List<EditorDescent.Run> runs = EditorDescent.Read(files[h]);
                if (runs.Count == 0)
                    continue;
                (string _, string heroId, string weaponId, string cohortName) = PacingHeader(files[h]);
                int index = Array.FindIndex(heroes, hero => hero.Id == heroId);
                if (index < 0)
                    index = h < heroes.Length ? h : 0;
                DescentSimulation.FoundryHero hero = heroes[index];
                DescentSimulation.HeroWeapon weapon = weaponId == null ? null
                    : DescentSimulation.WeaponCatalogue().FirstOrDefault(w => w.Id == weaponId);
                if (weapon != null && weapon.Id != hero.Weapon.Id)
                {
                    hero = hero.Id == heroes[0].Id ? DescentSimulation.Vanguard() : DescentSimulation.CinderArcanist();
                    hero.Weapon = weapon;
                    hero.Name += " + " + weapon.DisplayName;
                }
                DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("editor seeds, three floors");
                levers.Floors = 3;
                bool careful = true;
                if (cohortName != null && Cryptforge.Core.AutopilotCohort.TryNamed(cohortName, out Cryptforge.Core.AutopilotCohort cohort))
                {
                    levers.Cohort = cohort;
                    careful = cohort.Name == FoundryBatch.Shipped().Cohort.Name;
                }
                FoundryBatch.Batch batch = careful && weapon == null && index < baselines.Count && baselines[index].Levers.Floors >= 3
                    ? baselines[index] : null;
                rows.Add(new EditorDescent.HeroRows
                {
                    Hero = hero.Name,
                    Source = Path.GetFileName(files[h]),
                    Cohort = cohortName,
                    Editor = runs,
                    SameSeeds = FoundryBatch.Run(hero, levers, runs.Select(run => run.Seed).ToArray(), parallel),
                    Batch = batch
                });
            }
            return rows.Count == 0 ? string.Empty : EditorDescent.Section(rows);
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

        // The heroes' pacing files, in the heroes' order: FOUNDRY_EDITOR_FILES names them; by default each hero's newest
        // plain autopilot file (NewestPacing). Null unless every hero has one.
        private static string[] PacingFiles(string folder, DescentSimulation.FoundryHero[] heroes)
        {
            string chosen = Environment.GetEnvironmentVariable("FOUNDRY_EDITOR_FILES");
            if (!string.IsNullOrEmpty(chosen))
                return chosen.Split(',').Select(name => Path.Combine(folder, name.Trim())).ToArray();
            string[] files = heroes.Select(hero => NewestPacing(folder, hero.Id, null)).ToArray();
            return files.All(file => file != null) ? files : null;
        }

        // A hero's newest pacing file of the autopilot playing the floor - its mode line "mode on", naming that hero and
        // the weapon it started with (null for the hero's own) and no cohort but the careful pilot, the simulation's
        // default - the newest naming the simulated floor's levers before any other, and of two written together the
        // named copy rather than the measurement's own horde-pacing.txt.
        private static string NewestPacing(string folder, string heroId, string weaponId)
        {
            if (!Directory.Exists(folder))
                return null;
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            return Directory.GetFiles(folder, "horde-pacing*.txt")
                .Select(path => (Path: path, Header: PacingHeader(path)))
                .Where(file => file.Header.Mode == "on" && file.Header.Hero == heroId && file.Header.Weapon == weaponId &&
                               (file.Header.Cohort == null || file.Header.Cohort == shipped.Cohort.Name))
                .OrderBy(file => LeverMismatch(ReadLevers(file.Path), shipped) == null ? 0 : 1)
                .ThenByDescending(file => File.GetLastWriteTimeUtc(file.Path))
                .ThenByDescending(file => Path.GetFileName(file.Path).Length)
                .Select(file => file.Path)
                .FirstOrDefault();
        }

        // A pacing file's first line, "mode <mode>[ hero <id>][ weapon <id>][ cohort <name>]" (HordePacingMeasurement): the
        // scene's default hero, the Vanguard, when it names none, and no cohort - the careful pilot - when it names none.
        internal static (string Mode, string Hero, string Weapon, string Cohort) PacingHeader(string path)
        {
            string[] words = (File.ReadLines(path).FirstOrDefault() ?? string.Empty).Split(' ');
            if (words.Length < 2 || words[0] != "mode")
                return (null, null, null, null);
            string hero = FoundryGate.V01HeroId, weapon = null, cohort = null;
            for (int i = 2; i + 1 < words.Length; i += 2)
            {
                if (words[i] == "hero")
                    hero = words[i + 1];
                else if (words[i] == "weapon")
                    weapon = words[i + 1];
                else if (words[i] == "cohort")
                    cohort = words[i + 1];
            }
            return (words[1], hero, weapon, cohort);
        }

        // The pacing file's tab-separated lines (seed, result, floor_s, uptime[, engaged_strike], ..., level, ..., rooms),
        // and, where the seed's telemetry was kept, its walks, node fights, the Warden's wave and the level at the gate.
        // Engaged is the engaged_strike column (since 2026-11-11, the simulation's rule); a file's older engaged column read
        // an enemy within the first weapon's reach and a unit, and is left unread.
        internal static List<EditorRun> ReadEditor(string folder, string pacing, string heroId)
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
                if (Column("engaged_strike") >= 0)
                    run.Engaged = Float(cells[Column("engaged_strike")]);
                if (Column("boss_s") >= 0)
                    run.BossColumn = Float(cells[Column("boss_s")]);
                if (Column("clock") >= 0)
                    run.Clock = Float(cells[Column("clock")]);
                if (Column("rooms") >= 0)
                {
                    string[] rooms = cells[Column("rooms")].Split(' ').Where(r => r.Contains(":")).ToArray();
                    run.Rooms = rooms.Select(r => Float(r.Substring(r.IndexOf(':') + 1).TrimEnd('s'))).ToArray();
                    if (rooms.Length > 0 && int.TryParse(rooms[rooms.Length - 1].Substring(0, rooms[rooms.Length - 1].IndexOf(':')),
                            NumberStyles.Integer, CultureInfo.InvariantCulture, out int last))
                        run.LastRoom = last;
                }
                // gate_hold_sec came with the levers line (2026-11-03): before it, no wait was ever written, not none.
                ReadTelemetry(Path.Combine(folder, "horde-telemetry", heroId + "-" + seed), run, ReadLevers(pacing) != null);
                if (!run.Cleared && run.Death == null)
                    run.Death = run.LastRoom > 0 ? run.LastRoom.ToString(CultureInfo.InvariantCulture) : "?";
                runs.Add(run);
            }
            return runs;
        }

        // The pacing file's floor levers, from its header line "levers floor <id> hall_surges <n> extra_elites <n> gate_s
        // <seconds>" (HordePacingMeasurement, since 2026-11-03); null for a file that names none.
        private static Dictionary<string, string> ReadLevers(string pacing)
        {
            if (!File.Exists(pacing))
                return null;
            string line = File.ReadLines(pacing).FirstOrDefault(l => l.StartsWith("levers ", StringComparison.Ordinal));
            if (line == null)
                return null;
            string[] words = line.Split(' ');
            var levers = new Dictionary<string, string>();
            for (int i = 1; i + 1 < words.Length; i += 2)
                levers[words[i]] = words[i + 1];
            return levers;
        }

        // Why a pacing file's floor is not the one the simulation plays (its surges, extra elites or gate time differ, or
        // it names none); null when it is.
        private static string LeverMismatch(Dictionary<string, string> levers, DescentSimulation.FoundryLevers shipped)
        {
            if (levers == null)
                return "names no floor levers (written before 2026-11-03; the m0 files are the floor before V0.1)";
            string Read(string key) => levers.TryGetValue(key, out string value) ? value : "?";
            bool same = int.TryParse(Read("hall_surges"), out int surges) && surges == shipped.HallSurges &&
                        int.TryParse(Read("extra_elites"), out int elites) && elites == shipped.ExtraElites &&
                        float.TryParse(Read("gate_s").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float gate) &&
                        Math.Abs(gate - shipped.GateNotBeforeSeconds) < 1e-3f;
            return same
                ? null
                : $"measured hall_surges {Read("hall_surges")}, extra_elites {Read("extra_elites")}, gate_s {Read("gate_s")}";
        }

        // Pacing files from a Turkish-locale Editor write a decimal comma.
        private static float Float(string text) =>
            float.Parse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

        // The seed's telemetry, if it is this pacing line's run: walks, node fights, the gate's hold where the telemetry
        // wrote it (gateHoldWritten), the Warden's wave and exit, the level at the gate and the cause of a death.
        private static void ReadTelemetry(string folder, EditorRun run, bool gateHoldWritten)
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
            float walk = 0f, node = 0f, held = 0f;
            int level = 0, levelAtGate = -1, evolutions = 0;
            string roomKind = null, death = null;
            float roomWalk = 0f;
            float bossWave = float.NaN, boss = float.NaN, exit = float.NaN, end = float.NaN;
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
                        levelAtGate = level;
                        break;
                    case "wave_clear" when roomKind == "boss":
                        bossWave = (float)e.GetProperty("active_sec").GetDouble();
                        break;
                    case "room_complete":
                        float active = (float)e.GetProperty("active_sec").GetDouble();
                        // The wait for the gate is part of its room's active seconds, but no node fight.
                        float hold = e.TryGetProperty("gate_hold_sec", out JsonElement gateHold) ? (float)gateHold.GetDouble() : 0f;
                        held += hold;
                        if (roomKind == "boss")
                        {
                            boss = bossWave;
                            exit = active - bossWave;
                        }
                        else
                        {
                            node += active - roomWalk - hold;
                        }
                        break;
                    case "run_end":
                        if (e.TryGetProperty("result", out JsonElement result) && result.GetString() == "defeat")
                            death = $"{e.GetProperty("rooms_completed").GetInt32() + 1} ({e.GetProperty("cause").GetString()})";
                        break;
                    case "upgrade_selected":
                        if (e.TryGetProperty("upgrade_id", out JsonElement picked) &&
                            (picked.GetString() ?? string.Empty).StartsWith("evolution_", StringComparison.Ordinal))
                            evolutions++;
                        break;
                }
                if ((name == "floor_complete" || name == "run_end" || name == "run_interrupted") &&
                    e.TryGetProperty("active_sec", out JsonElement endSec))
                    end = float.IsNaN(end) ? (float)endSec.GetDouble() : Math.Max(end, (float)endSec.GetDouble());
            }
            if (float.IsNaN(end) || Math.Abs(end - run.Floor) > TelemetryMatchSeconds)
                return;
            run.TelemetryRead = true;
            run.Walk = walk;
            run.Node = node;
            run.GateWait = gateHoldWritten ? held : float.NaN;
            run.Boss = boss;
            run.Exit = exit;
            run.LevelAtGate = levelAtGate;
            run.Evolutions = evolutions;
            if (death != null)
                run.Death = death;
        }
    }
}
