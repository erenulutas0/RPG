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
    // The finale's stay (docs/41, 2026-11-11): the development autopilot walks out of the foundry's finale once its swarm's
    // clock reaches 60 s (DevelopmentAutopilot.SwarmStay), at the ladder's sixth wave and x1.49 of the ramp - before the
    // ladder's Tanks (108 s) and its Warden (132 s) walk in. This plays the finale with the pilot staying longer, or
    // shorter (FoundryLevers.FinaleStaySeconds, research only), each cohort asked for and both heroes down all three
    // floors on the same seeds, so what a stay costs is read against the same heroes. Explicit, so a plain `dotnet test`
    // skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~FinaleStayResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), FINALE_STAYS (comma separated seconds; 60,120,180,300), FINALE_COHORTS
    // (comma separated cohort names; careful,clumsy), FINALE_REALISM (comma separated: frame-perfect, typical, slow;
    // typical). Writes finale-stay.md and a row a run that reached the finale in finale-stay.csv.
    [Explicit("The finale's stay research: run it by name.")]
    public sealed class FinaleStayResearch
    {
        [Test]
        public void TheFinaleByHowLongThePilotStays()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            float[] stays = List("FINALE_STAYS", "60,120,180,300").Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            string[] names = List("FINALE_COHORTS", "careful,clumsy");
            AutopilotCohort[] cohorts = AutopilotCohort.Cohorts.Where(c => names.Contains(c.Name)).ToArray();
            string[] pilots = List("FINALE_REALISM", "typical");
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var md = new StringBuilder();
            md.AppendLine($"# The finale's stay: seeds {first}-{first + count - 1} a hero ({DateTime.Now:yyyy-MM-dd HH:mm})").AppendLine();
            md.AppendLine("Each run plays floors 1 and 2 and, after a floor-2 clear, the finale's swarm, its pilot turning for the exit once the swarm's clock reaches the stay " +
                          "(FoundryLevers.FinaleStaySeconds, research only; the development autopilot's is 60 s). Floors 1 and 2 are the same runs whatever the stay. " +
                          "Reached: of all runs. Survived and walked out: of those that reached the finale. Finale: active seconds from floor 2's checkpoint, the runs that reached it (P50 / P90). " +
                          "Ladder wave and ramp: where the swarm's clock got to (P50). Health lost: in the finale (mean / P90). Health leaving: the walk-outs' health as they left, as a share of their maximum (P10 / P50). " +
                          "Descent: floors 1-3 of the walk-outs (P50, minutes).").AppendLine();
            var all = new List<FoundryBatch.Batch>();
            foreach (string pilot in pilots)
            {
                PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
                md.AppendLine($"## {(realism != null ? realism.Name : "frame-perfect")}").AppendLine();
                md.AppendLine("| Stay | Cohort | Hero | Reached | Survived | Walked out | Died there: phase, killing blow | Finale P50 / P90 | Ladder wave / ramp at the end | Health lost mean / P90 | Health leaving P10 / P50 | Descent P50 |");
                md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
                foreach (float stay in stays)
                foreach (AutopilotCohort cohort in cohorts)
                foreach (DescentSimulation.FoundryHero hero in heroes)
                {
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, 3, realism)
                        .Copy($"cohort {cohort.Name}, stay {stay.ToString("0", CultureInfo.InvariantCulture)}" +
                              (realism != null ? ", " + realism.Name : string.Empty));
                    levers.FinaleStaySeconds = stay;
                    FoundryBatch.Batch batch = FoundryBatch.Run(hero, levers, seeds, parallel);
                    all.Add(batch);
                    md.AppendLine(Row(stay, cohort, batch));
                }
                md.AppendLine();
                TestContext.Progress.WriteLine($"{pilot}: {stays.Length} stays done");
            }

            File.WriteAllText(Path.Combine(output, "finale-stay.md"), md.ToString());
            File.WriteAllText(Path.Combine(output, "finale-stay.csv"), FoundryBatch.FinaleCsv(all));
            TestContext.WriteLine(md.ToString());
            Assert.That(all.All(b => b.Runs.Length == count), "every seed ran");
        }

        private static string Row(float stay, AutopilotCohort cohort, FoundryBatch.Batch batch)
        {
            FoundryBatch.Batch finale = FoundryBatch.Finale(batch);
            DescentSimulation.FoundryResult[] runs = finale.Runs;
            DescentSimulation.FoundryResult[] died = runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            DescentSimulation.FoundryResult[] walked = runs.Where(r => r.WalkedOut).ToArray();
            string deaths = died.Length == 0 ? "0"
                : $"{died.Length}: " +
                  string.Join(", ", died.GroupBy(r => r.DeathPhase ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                      .Select(g => $"{g.Key} x{g.Count()}")) + "; " +
                  string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                      .Take(4).Select(g => $"{g.Key} x{g.Count()}"));
            float[] descent = batch.Runs.Where(r => FoundryBatch.FinaleOf(r) != null && FoundryBatch.FinaleOf(r).WalkedOut)
                .Select(FoundryBatch.DescentSeconds).ToArray();
            return $"| {stay.ToString("0", CultureInfo.InvariantCulture)} s | {cohort.Name} | {batch.Hero.Name} | " +
                   $"{runs.Length} ({Pct(runs.Length, batch.Runs.Length)}) | {runs.Length - died.Length} ({Pct(runs.Length - died.Length, runs.Length)}) | " +
                   $"{walked.Length} ({Pct(walked.Length, runs.Length)}) | {deaths} | " +
                   $"{P(runs.Select(r => r.FloorSeconds), 50)} / {P(runs.Select(r => r.FloorSeconds), 90)} | " +
                   $"{P(runs.Select(r => (float)r.SwarmLadderWave), 50)} / x{P(runs.Select(r => r.SwarmDifficulty), 50, "0.00")} | " +
                   $"{(runs.Length == 0 ? "-" : runs.Average(r => r.TakenFromWave).ToString("0", CultureInfo.InvariantCulture))} / {P(runs.Select(r => r.TakenFromWave), 90)} | " +
                   $"{P(walked.Select(r => 100f * r.HeroHealth / Math.Max(1f, r.HeroMaximum)), 10)}% / {P(walked.Select(r => 100f * r.HeroHealth / Math.Max(1f, r.HeroMaximum)), 50)}% | " +
                   $"{P(descent.Select(s => s / 60f), 50, "0.0")} min |";
        }

        private static string P(IEnumerable<float> values, double p, string format = "0")
        {
            double value = FoundryBatch.Percentile(values, p);
            return double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Pct(int part, int whole) =>
            whole == 0 ? "-" : (100.0 * part / whole).ToString("0", CultureInfo.InvariantCulture) + "%";

        private static string[] List(string name, string fallback) =>
            (Environment.GetEnvironmentVariable(name) ?? fallback).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

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
