using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/54 section 13's geared cohorts on Forged (phase 5, 2026-11-17): each geared cohort of FoundryGear (careful at
    // +5, +7, +9 and +9 with good lines, clumsy and the auto-farmer at +5) and the ungeared cohort it is drawn from, both
    // heroes, down the whole shipped Descent (floors 1-3 and the finale) on the same seeds with human hands, as the batch's
    // cohorts play it (FoundryCohorts.Levers, FOUNDRY_FLOORS=4's chain). The gear reaches the simulated run through
    // GearStats.Apply, the scene's own body. Reads section 13's Forged rows: completion against the ungeared cohort's (and
    // seed by seed), the bosses' fights (floor 1's Warden from walking out to falling, floor 2's Regent and floor 3's
    // Warden their own fights), the Descent's minutes, and the gold banked a run as GearEconomyResearch rebuilds it - the
    // income the economy model reads, now with gear (gear-cohorts.csv has gear-economy.csv's columns first).
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~GearedCohortsResearch"
    //
    // Environment, all optional: FOUNDRY_GEAR (the geared cohorts by name, comma separated, or "all": the default),
    // FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors), FOUNDRY_REALISM (typical),
    // FOUNDRY_OUT (the repository's TestResults). Writes gear-cohorts.md and gear-cohorts.csv (one row a run).
    [Explicit("docs/54 section 13's geared cohorts: run it by name.")]
    public sealed class GearedCohortsResearch
    {
        private sealed class Row
        {
            public string Label;
            public string Hero;
            public GearEconomyResearch.Run Income;
            public DescentSimulation.FoundryResult Result;
            public float Warden1 = -1f;
            public float Regent2 = -1f;
            public float Warden3 = -1f;
            public int Power;
        }

        [Test]
        public void TheGearedCohortsOnForged()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";
            List<GearedCohort> geared = FoundryGear.Named(Environment.GetEnvironmentVariable("FOUNDRY_GEAR"));
            Dictionary<string, int> gold = GearEconomyResearch.BaseGold();
            float[] modifiers = GearEconomyResearch.Modifiers(DescentSimulation.FoundryFloorOne());
            var unknown = new HashSet<string>();
            Func<DescentSimulation.FoundryHero>[] heroes = { DescentSimulation.Vanguard, DescentSimulation.CinderArcanist };

            // Each ungeared cohort a geared one is drawn from, first, then the geared, in FoundryGear's order.
            var plays = new List<(string Label, AutopilotCohort Cohort, GearedCohort Gear)>();
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
                if (geared.Any(g => g.Cohort == cohort))
                    plays.Add((cohort.Name, cohort, null));
            foreach (GearedCohort g in geared)
                plays.Add((g.Name, g.Cohort, g));

            var rows = new List<Row>();
            foreach ((string label, AutopilotCohort cohort, GearedCohort gear) in plays)
            foreach (Func<DescentSimulation.FoundryHero> make in heroes)
            {
                DescentSimulation.FoundryHero hero = make();
                if (gear != null)
                    gear.Wear(hero);
                DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism);
                FoundryBatch.Batch batch = FoundryBatch.Run(hero, levers, seeds, parallel);
                for (int i = 0; i < batch.Runs.Length; i++)
                    rows.Add(Read(label, hero, seeds[i], batch.Runs[i], modifiers, gold, unknown));
                TestContext.Progress.WriteLine($"{label} {hero.Name}: done in {batch.WallSeconds:0} s");
            }

            string report = Report(rows, plays, first, count, hands, unknown);
            File.WriteAllText(Path.Combine(output, "gear-cohorts.md"), report);
            File.WriteAllText(Path.Combine(output, "gear-cohorts.csv"), Csv(rows));
            TestContext.WriteLine(report);
            Assert.That(rows.Count, Is.EqualTo(plays.Count * heroes.Length * count), "every seed ran");
        }

        private static Row Read(string label, DescentSimulation.FoundryHero hero, int seed, DescentSimulation.FoundryResult result,
            float[] modifiers, Dictionary<string, int> gold, HashSet<string> unknown)
        {
            var row = new Row
            {
                Label = label, Hero = hero.Name, Result = result, Power = hero.Gear.Power,
                Income = GearEconomyResearch.Read(label, hero.Name, seed, result, modifiers, gold, unknown)
            };
            if (result.LevelAtGate >= 0 && result.BossSeconds >= 0f)
                row.Warden1 = result.BossSeconds;
            DescentSimulation.FoundryResult two = FoundryBatch.FloorOf(result, 2);
            if (two != null && two.WardenSeconds >= 0f)
                row.Regent2 = two.WardenSeconds;
            DescentSimulation.FoundryResult three = FoundryBatch.FloorOf(result, 3);
            if (three != null && three.WardenSeconds >= 0f)
                row.Warden3 = three.WardenSeconds;
            return row;
        }

        private static string Report(List<Row> rows, List<(string Label, AutopilotCohort Cohort, GearedCohort Gear)> plays,
            int first, int count, string hands, HashSet<string> unknown)
        {
            var md = new StringBuilder();
            md.AppendLine($"# docs/54 section 13: the geared cohorts on Forged, seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("The whole shipped Descent (floors 1-3 and the finale) in each cohort's hands, the gear applied at the run's start " +
                          "through GearStats.Apply. Gear: section 7.2's index of the set worn. Completed: of the seeds; paired with the ungeared " +
                          "cohort on the same seeds: both completed / only ungeared / only geared. Reached 2 / 3 / 4: cleared floor n-1. " +
                          "Bosses: P50 / P90 of the felled fights - floor 1's Warden from walking out of the gate to falling (the gate's " +
                          "band, 45-65 s), floor 2's Regent and floor 3's Warden their own fights. Minutes: the Descent's active minutes, P50 " +
                          "of the completed runs and of every run. Banked: forge gold a run, rebuilt as GearEconomyResearch does.").AppendLine();
            md.AppendLine("| Cohort | Hero | Gear | Completed | vs ungeared | Paired | Reached 2 / 3 / 4 | Floor 3 cleared | Warden (floor 1) P50 / P90 | Regent (floor 2) P50 | Warden (floor 3) P50 | Minutes P50, completed / all | Banked mean / P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach ((string label, AutopilotCohort cohort, GearedCohort gear) in plays)
            foreach (string hero in rows.Select(r => r.Hero).Distinct())
            {
                Row[] g = rows.Where(r => r.Label == label && r.Hero == hero).OrderBy(r => r.Income.Seed).ToArray();
                Row[] plain = rows.Where(r => r.Label == cohort.Name && r.Hero == hero).OrderBy(r => r.Income.Seed).ToArray();
                int completed = g.Count(r => r.Income.Completed);
                int plainCompleted = plain.Count(r => r.Income.Completed);
                string versus = gear == null ? "-" : Signed(completed - plainCompleted);
                string paired = gear == null || plain.Length != g.Length ? "-" : Paired(plain, g);
                md.AppendLine($"| {label} | {hero} | {F(g.Length == 0 ? 0 : g[0].Power / 100.0, "+0.0")} % | {completed} ({Pct(completed, g.Length)}) | " +
                              $"{versus} | {paired} | {Share(g, 1)} / {Share(g, 2)} / {Share(g, 3)} | {g.Count(r => r.Income.FloorsCleared >= 3)} | " +
                              $"{P(g.Select(r => r.Warden1), 50)} / {P(g.Select(r => r.Warden1), 90)} s | {P(g.Select(r => r.Regent2), 50)} s | " +
                              $"{P(g.Select(r => r.Warden3), 50)} s | " +
                              $"{P(g.Where(r => r.Income.Completed).Select(r => r.Income.Seconds / 60f), 50, "0.0")} / " +
                              $"{P(g.Select(r => r.Income.Seconds / 60f), 50, "0.0")} | " +
                              $"{F(g.Average(r => r.Income.Banked), "0")} / {P(g.Select(r => (float)r.Income.Banked), 50, "0")} |");
            }
            md.AppendLine();
            if (unknown.Count > 0)
                md.AppendLine("Kinds without a base gold (counted as 0): " + string.Join(", ", unknown.OrderBy(s => s)) + ".").AppendLine();
            return md.ToString();
        }

        private static string Paired(Row[] plain, Row[] geared)
        {
            int both = 0, onlyPlain = 0, onlyGeared = 0;
            for (int i = 0; i < plain.Length; i++)
            {
                bool a = plain[i].Income.Completed, b = geared[i].Income.Completed;
                if (a && b)
                    both++;
                else if (a)
                    onlyPlain++;
                else if (b)
                    onlyGeared++;
            }
            return $"{both} / {onlyPlain} / {onlyGeared}";
        }

        private static string Csv(List<Row> rows)
        {
            var csv = new StringBuilder();
            string income = GearEconomyResearch.Csv(rows.Select(r => r.Income).ToList());
            string[] lines = income.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            csv.AppendLine(lines[0] + ",gear_power,warden_f1,regent_f2,warden_f3");
            for (int i = 0; i < rows.Count; i++)
                csv.AppendLine(lines[i + 1] + "," + string.Join(",", rows[i].Power, N(rows[i].Warden1), N(rows[i].Regent2), N(rows[i].Warden3)));
            return csv.ToString();
        }

        private static string N(float value) => value < 0f ? "" : value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string P(IEnumerable<float> values, int percentile, string format = "0.0")
        {
            float[] sorted = values.Where(v => v >= 0f).OrderBy(v => v).ToArray();
            if (sorted.Length == 0)
                return "-";
            return sorted[Math.Min(sorted.Length - 1, (int)(percentile / 100.0 * sorted.Length))].ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Share(Row[] g, int cleared) =>
            g.Length == 0 ? "-" : Pct(g.Count(r => r.Income.FloorsCleared >= cleared), g.Length);

        private static string Pct(int part, int whole) =>
            whole == 0 ? "-" : (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture) + " %";

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString(CultureInfo.InvariantCulture);

        private static string F(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        private static int Setting(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;

        private static string RepositoryRoot()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            return directory ?? TestContext.CurrentContext.TestDirectory;
        }
    }
}
