using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Floor 3's tuning (docs/48 phase 1, 2026-11-12): the shipped Descent - floor 1, floor 2, Gloam Gardens, the finale -
    // played by every cohort and both heroes on the same seeds, with floor 3's knobs as each option sets them: its Warden's
    // share (FoundryFloor.GateHealthShare, the asset's _foundryGateHealthShare), its gate's time, and a heal as the finale
    // opens (FoundryLevers.FinaleHealShare, docs/47's lever). Floors 1 and 2 are the same runs in every option. It reads
    // floor 3's Warden fight against docs/40 section 9.6's 45-65 s, floor 3's clears, the finale's deaths and the whole
    // Descent against docs/40's 13-16 minutes. Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~ThirdFloorResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), FLOOR3_OPTIONS (comma separated option keys; all), FOUNDRY_REALISM
    // (typical; frame-perfect, slow). Writes floor-three.md, rewritten after each option.
    [Explicit("Floor 3's tuning research: run it by name.")]
    public sealed class ThirdFloorResearch
    {
        private sealed class Option
        {
            public string Key;
            // The Warden's share on floor 3; its effective share against floor 1's authored 300 is this x1.96 x1.15.
            public float Share = DescentSimulation.FloorThreeGateShare;
            public float Gate = 210f;
            public float Heal;
        }

        // Effective shares (x 2.254 = 1.96 x 1.15): 8.5 floor 2's, then up in steps.
        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped" };
            yield return new Option { Key = "s8.5", Share = 3.771074f };
            yield return new Option { Key = "s9.5", Share = 4.214729f };
            yield return new Option { Key = "s10", Share = 4.436557f };
            yield return new Option { Key = "s10.5", Share = 4.658385f };
            yield return new Option { Key = "s11", Share = 4.880213f };
            yield return new Option { Key = "s11.5", Share = 5.102041f };
            yield return new Option { Key = "s12", Share = 5.323869f };
            yield return new Option { Key = "s13", Share = 5.767524f };
            yield return new Option { Key = "gate180", Gate = 180f };
            yield return new Option { Key = "heal30", Heal = .3f };
        }

        private static DescentSimulation.FoundryFloor Chain(Option option)
        {
            DescentSimulation.FoundryFloor one = DescentSimulation.FoundryFloorOne();
            DescentSimulation.FoundryFloor three = one.Next.Next;
            three.GateHealthShare = option.Share;
            three.GateNotBeforeSeconds = option.Gate;
            return one;
        }

        private sealed class Cell
        {
            public Option Option;
            public string Cohort;
            public string Hero;
            public DescentSimulation.FoundryResult[] Runs;
        }

        [Test]
        public void FloorThreesWardenAndTheDescent()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = (Environment.GetEnvironmentVariable("FLOOR3_OPTIONS") ?? string.Join(",", Options().Select(o => o.Key)))
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(k => k.Trim()).ToArray();
            Option[] options = Options().Where(o => keys.Contains(o.Key)).ToArray();
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var cells = new List<Cell>();
            foreach (Option option in options)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
                foreach (DescentSimulation.FoundryHero hero in heroes)
                {
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                        .Copy($"cohort {cohort.Name}, {option.Key}" + (realism != null ? ", " + realism.Name : string.Empty));
                    levers.FinaleHealShare = option.Heal;
                    var runs = new DescentSimulation.FoundryResult[seeds.Length];
                    Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) },
                        i => runs[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers, Chain(option)));
                    cells.Add(new Cell { Option = option, Cohort = cohort.Name, Hero = hero.Name, Runs = runs });
                }
                TestContext.Progress.WriteLine($"{option.Key}: {watch.Elapsed.TotalSeconds:0} s");
                File.WriteAllText(Path.Combine(output, "floor-three.md"), Report(cells, first, count, pilot));
            }
            TestContext.WriteLine(Report(cells, first, count, pilot));
            Assert.That(cells.All(c => c.Runs.Length == count), "every seed ran");
        }

        private static string Report(List<Cell> cells, int first, int count, string pilot)
        {
            var md = new StringBuilder();
            md.AppendLine($"# Floor 3's tuning: seeds {first}-{first + count - 1} a hero, {pilot} ({DateTime.Now:yyyy-MM-dd HH:mm})").AppendLine();
            md.AppendLine("Cells: Vanguard / Ilya. Down: floor 2's clears. Cleared: floor 3, of those that went down. Warden: floor 3's fight P50 (s), the fights in which it fell. " +
                          "Floor 3: P50 of its clears (s). Finale deaths: of those that reached it. Completed: walked out of the finale, of all.").AppendLine();
            md.AppendLine("| Option | Cohort | Down | Cleared floor 3 | Warden P50 | Floor 3 P50 | Finale deaths | Completed |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (IGrouping<string, Cell> byOption in cells.GroupBy(c => c.Option.Key))
            foreach (IGrouping<string, Cell> byCohort in byOption.GroupBy(c => c.Cohort))
            {
                Cell[] pair = byCohort.ToArray();
                string Both(Func<Cell, string> read) => string.Join(" / ", pair.Select(read));
                md.AppendLine($"| `{byOption.Key}` | {byCohort.Key} | {Both(c => Three(c).Length.ToString(CultureInfo.InvariantCulture))} | " +
                              $"{Both(c => Pct(Three(c).Count(r => r.Cleared), Three(c).Length))} | " +
                              $"{Both(c => F(FoundryBatch.Median(Three(c).Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds))))} | " +
                              $"{Both(c => F(FoundryBatch.Median(Three(c).Where(r => r.Cleared).Select(r => r.FloorSeconds))))} | " +
                              $"{Both(c => Finales(c).Count(f => f.Outcome == DescentSimulation.FoundryOutcome.Died) + " of " + Finales(c).Length)} | " +
                              $"{Both(c => Pct(Finales(c).Count(f => f.WalkedOut), c.Runs.Length))} |");
            }
            md.AppendLine();
            md.AppendLine("The whole Descent of the completed runs, all cohorts and both heroes pooled (minutes), and deaths on floors 2-3.").AppendLine();
            md.AppendLine("| Option | Descent P10 / P50 / P90 | Within 13-16 | Deaths on floor 2 / floor 3 | Finale deaths |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (IGrouping<string, Cell> byOption in cells.GroupBy(c => c.Option.Key))
            {
                DescentSimulation.FoundryResult[] runs = byOption.SelectMany(c => c.Runs).ToArray();
                float[] descent = runs.Where(r => FoundryBatch.FinaleOf(r) is DescentSimulation.FoundryResult f && f.WalkedOut)
                    .Select(FoundryBatch.DescentSeconds).ToArray();
                int within = descent.Count(s => s >= FoundryBatch.FullRunFrom && s <= FoundryBatch.FullRunTo);
                int twoDeaths = runs.Count(r => r.NextFloor != null && r.NextFloor.Outcome == DescentSimulation.FoundryOutcome.Died);
                int threeDeaths = runs.Count(r => r.NextFloor?.NextFloor != null && r.NextFloor.NextFloor.Outcome == DescentSimulation.FoundryOutcome.Died);
                DescentSimulation.FoundryResult[] finales = runs.Select(FoundryBatch.FinaleOf).Where(f => f != null).ToArray();
                md.AppendLine($"| `{byOption.Key}` | {M(descent, 10)} / {M(descent, 50)} / {M(descent, 90)} | {within} of {descent.Length} | {twoDeaths} / {threeDeaths} | " +
                              $"{finales.Count(f => f.Outcome == DescentSimulation.FoundryOutcome.Died)} of {finales.Length} |");
            }
            return md.ToString();
        }

        private static DescentSimulation.FoundryResult[] Three(Cell c) =>
            c.Runs.Where(r => r.NextFloor?.NextFloor != null).Select(r => r.NextFloor.NextFloor).ToArray();

        private static DescentSimulation.FoundryResult[] Finales(Cell c) =>
            c.Runs.Select(FoundryBatch.FinaleOf).Where(f => f != null).ToArray();

        private static string M(float[] seconds, double p)
        {
            double value = FoundryBatch.Percentile(seconds, p);
            return double.IsNaN(value) ? "-" : (value / 60.0).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string F(double value) => double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

        private static string Pct(int part, int whole) =>
            whole == 0 ? "-" : (100.0 * part / whole).ToString("0", CultureInfo.InvariantCulture) + "%";

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
