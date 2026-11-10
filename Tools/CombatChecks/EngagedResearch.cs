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
    // Engaged uptime (docs/41, 2026-11-11): the V0.1 gate's "Engaged uptime" row read 0.60 / 0.58 (Vanguard / Ilya,
    // frame-perfect) against its proposed 0.75 while combat uptime read 0.95. This splits a floor's active seconds by phase
    // (the walk to a hall, a node fight, between, the gate's wait, the Warden, the exit) and reads each phase under every
    // reading of engaged FoundryEngagement names - the one before 2026-11-11 (an enemy within the first weapon's reach and a
    // unit), the longest weapon's reach in place of the first's, the first weapon's authored reach, the longest weapon's
    // reach without the margin, and the gate's since (a strike landed) - beside where the nearest enemy stood on the frames
    // the reading before 2026-11-11 leaves out; and plays the research's candidate levers on the same seeds. Explicit, so a
    // plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~EngagedResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (the processors),
    // FOUNDRY_OUT (the repository's TestResults), ENGAGED_REALISM (comma separated: frame-perfect, typical, slow; both of
    // the first two by default), ENGAGED_COHORTS (comma separated cohort names; careful by default), ENGAGED_LEVERS (comma
    // separated names of Candidates below; shipped by default). Writes engaged-research.md.
    [Explicit("The engaged uptime research: run it by name.")]
    public sealed class EngagedResearch
    {
        private int _parallel;
        private int[] _seeds;

        // A candidate: a name and what it sets on the shipped levers. "before-cards" is the careful pilot's card score of
        // main 18715c94 (CardScoreResearch's "before"), for the cause; the rest are the floor's own levers, each a
        // foundry-only change a scene would make in one constant (HordeRule, FloorDefinition's foundry knobs).
        private static IEnumerable<(string Name, Action<DescentSimulation.FoundryLevers> Set)> Candidates()
        {
            yield return ("shipped", l => { });
            yield return ("before-cards", l => l.Picker = CardScoreResearch.Pickers().First(p => p.Name == "before").Pick);
            // The horde stands longer within reach: more of its authored health (HordeRule.HealthShare 0.3).
            yield return ("horde-health-0.4", l => l.HordeHealthShare = .4f);
            yield return ("horde-health-0.5", l => l.HordeHealthShare = .5f);
            // More of it stands at once (HordeRule.TargetByState 4/10/16/6), fills faster (MaxSpawnsPerSecond 2), grows more
            // with the clock (HordeClock.TargetGrowth 0.5).
            yield return ("horde-targets-x1.5", l => l.HordeTargets = new[] { 6, 15, 24, 9 });
            yield return ("horde-targets-x2", l => l.HordeTargets = new[] { 8, 20, 32, 12 });
            yield return ("horde-fill-3", l => l.HordeFillRate = 3f);
            yield return ("horde-growth-1.5", l => l.HordeTargetGrowth = 1.5f);
            // The gate's wait filled with hall 1's waves (FoundryBatch.GateLevers' siege), and the halls held 30 s.
            yield return ("siege-hall-1", l => l.GateSiegeHall = 1);
            yield return ("halls-held-30", l => l.HallHoldSeconds = 30f);
        }

        [Test]
        public void EngagedUptimeOverASeedBatch()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            _parallel = Setting("FOUNDRY_PARALLEL", Environment.ProcessorCount);
            _seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] realisms = List("ENGAGED_REALISM", "frame-perfect,typical");
            AutopilotCohort[] cohorts = List("ENGAGED_COHORTS", "careful")
                .Select(n => AutopilotCohort.Cohorts.FirstOrDefault(c => c.Name == n) ?? throw new ArgumentException("No such cohort: " + n))
                .ToArray();
            var candidates = List("ENGAGED_LEVERS", "shipped")
                .Select(n => Candidates().Where(c => c.Name == n).Select(c => ((string, Action<DescentSimulation.FoundryLevers>)?)c)
                    .FirstOrDefault() ?? throw new ArgumentException("No such candidate: " + n))
                .ToArray();

            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };
            var md = new StringBuilder();
            md.AppendLine("# Engaged uptime").AppendLine();
            md.AppendLine($"Seeds {_seeds[0]}-{_seeds[_seeds.Length - 1]} a hero, floor 1, {DateTime.Now:yyyy-MM-dd HH:mm}.").AppendLine();
            md.AppendLine("Readings (FoundryEngagement.Rules):").AppendLine();
            for (int r = 0; r < DescentSimulation.FoundryEngagement.Rules.Length; r++)
                md.AppendLine($"- R{r}: {DescentSimulation.FoundryEngagement.Rules[r]}");
            md.AppendLine();
            var rows = new List<(string Realism, string Cohort, string Lever, DescentSimulation.FoundryHero Hero, DescentSimulation.FoundryResult[] Runs)>();
            foreach (string realismName in realisms)
            {
                PilotRealism realism = realismName == "frame-perfect" ? null : PilotRealism.Named(realismName);
                foreach (AutopilotCohort cohort in cohorts)
                    foreach (var candidate in candidates)
                    {
                        (string name, Action<DescentSimulation.FoundryLevers> set) = candidate;
                        foreach (DescentSimulation.FoundryHero hero in heroes)
                            rows.Add((realismName, cohort.Name, name, hero, Play(hero, cohort, realism, set, name)));
                    }
            }
            md.Append(Summary(rows));
            md.Append(ByPhase(rows));
            md.Append(Outside(rows));
            File.WriteAllText(Path.Combine(output, "engaged-research.md"), md.ToString());
            TestContext.WriteLine(md.ToString());
            Assert.Pass();
        }

        private DescentSimulation.FoundryResult[] Play(DescentSimulation.FoundryHero hero, AutopilotCohort cohort, PilotRealism realism,
            Action<DescentSimulation.FoundryLevers> set, string name)
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("engaged " + name);
            levers.Cohort = cohort;
            levers.Realism = realism;
            levers.CountsEngagement = true;
            set(levers);
            var results = new DescentSimulation.FoundryResult[_seeds.Length];
            Parallel.For(0, _seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, _parallel) },
                i => results[i] = DescentSimulation.RunFoundry(hero, _seeds[i], levers));
            return results;
        }

        // ---------------------------------------------------------------------------------------------------------
        // The tables
        // ---------------------------------------------------------------------------------------------------------

        private static string Summary(List<(string Realism, string Cohort, string Lever, DescentSimulation.FoundryHero Hero, DescentSimulation.FoundryResult[] Runs)> rows)
        {
            var md = new StringBuilder();
            md.AppendLine("## Every reading over the floor").AppendLine();
            md.AppendLine("P50 of every run (the gate's reading), each reading's share of the floor's active seconds; FoundryResult's Engaged (R4) and Uptime (R5) " +
                          "beside them as the check. Cleared, deaths, the floor's P50, the Warden's fight P50 of the felled fights, " +
                          "and the first and the longest weapon's reach at the floor's end (P50).").AppendLine();
            int rules = DescentSimulation.FoundryEngagement.Rules.Length;
            md.AppendLine("| Realism | Cohort | Lever | Hero | Cleared | Floor P50 | Warden P50 | " +
                          string.Join(" | ", Enumerable.Range(0, rules).Select(r => "R" + r)) + " | Engaged / Uptime | Reach: first / longest |");
            md.AppendLine("|---|---|---|---|---|---|---|" + string.Concat(Enumerable.Repeat("---|", rules)) + "---|---|");
            foreach (var row in rows)
            {
                DescentSimulation.FoundryResult[] r = row.Runs;
                var felled = r.Where(x => x.LevelAtGate >= 0 && x.BossSeconds >= 0f).Select(x => x.BossSeconds);
                md.AppendLine($"| {row.Realism} | {row.Cohort} | {row.Lever} | {row.Hero.Name} | {r.Count(x => x.Cleared)} | " +
                              $"{P(r.Select(x => x.FloorSeconds), 50, "0")} | {P(felled, 50, "0")} | " +
                              string.Join(" | ", Enumerable.Range(0, rules).Select(k => P(r.Select(x => x.Engagement.Share(k)), 50, "0.00"))) + " | " +
                              $"{P(r.Select(x => x.Engaged), 50, "0.00")} / {P(r.Select(x => x.Uptime), 50, "0.00")} | " +
                              $"{P(r.Select(x => x.Engagement.FirstReach), 50, "0.0")} / {P(r.Select(x => x.Engagement.LongestReach), 50, "0.0")} |");
            }
            md.AppendLine();
            return md.ToString();
        }

        private static string ByPhase(List<(string Realism, string Cohort, string Lever, DescentSimulation.FoundryHero Hero, DescentSimulation.FoundryResult[] Runs)> rows)
        {
            var md = new StringBuilder();
            md.AppendLine("## By phase").AppendLine();
            md.AppendLine("Seconds: the phase's mean seconds a run. Each reading: the share of the phase's seconds it counts, pooled over the runs. " +
                          "Lost: the phase's seconds a run the reading before 2026-11-11 (R0) leaves out (mean), and their share of all the floor's seconds it leaves out.").AppendLine();
            int rules = DescentSimulation.FoundryEngagement.Rules.Length;
            md.AppendLine("| Realism | Cohort | Lever | Hero | Phase | Seconds | Lost (share) | " + string.Join(" | ", Enumerable.Range(0, rules).Select(r => "R" + r)) + " |");
            md.AppendLine("|---|---|---|---|---|---|---|" + string.Concat(Enumerable.Repeat("---|", rules)));
            foreach (var row in rows)
            {
                DescentSimulation.FoundryResult[] r = row.Runs;
                double lostAll = r.Sum(x => x.Engagement.Total - x.Engagement.EngagedSeconds(0));
                for (int p = 0; p < DescentSimulation.FoundryEngagement.Phases.Length; p++)
                {
                    int phase = p;
                    double seconds = r.Sum(x => x.Engagement.Seconds[phase]);
                    if (seconds <= 0)
                        continue;
                    double lost = seconds - r.Sum(x => x.Engagement.Engaged[0, phase]);
                    md.AppendLine($"| {row.Realism} | {row.Cohort} | {row.Lever} | {row.Hero.Name} | {DescentSimulation.FoundryEngagement.Phases[p]} | " +
                                  $"{N(seconds / r.Length, "0.0")} | {N(lost / r.Length, "0.0")} ({N(lostAll > 0 ? lost / lostAll * 100 : 0, "0")}%) | " +
                                  string.Join(" | ", Enumerable.Range(0, rules).Select(k => N(r.Sum(x => x.Engagement.Engaged[k, phase]) / seconds, "0.00"))) + " |");
                }
            }
            md.AppendLine();
            return md.ToString();
        }

        private static string Outside(List<(string Realism, string Cohort, string Lever, DescentSimulation.FoundryHero Hero, DescentSimulation.FoundryResult[] Runs)> rows)
        {
            var md = new StringBuilder();
            md.AppendLine("## Where the nearest enemy stood when the reading before 2026-11-11 (R0) leaves a frame out").AppendLine();
            md.AppendLine("Shares of the left-out seconds, pooled over the runs: no enemy standing, then the nearest within each distance (floor units).").AppendLine();
            float[] d = DescentSimulation.FoundryEngagement.Distances;
            md.AppendLine("| Realism | Cohort | Lever | Hero | None standing | " +
                          string.Join(" | ", d.Select(x => float.IsPositiveInfinity(x) ? "Farther" : "Within " + N(x, "0"))) + " |");
            md.AppendLine("|---|---|---|---|---|" + string.Concat(Enumerable.Repeat("---|", d.Length)));
            foreach (var row in rows)
            {
                DescentSimulation.FoundryResult[] r = row.Runs;
                double all = r.Sum(x => x.Engagement.Outside.Sum());
                md.AppendLine($"| {row.Realism} | {row.Cohort} | {row.Lever} | {row.Hero.Name} | " +
                              string.Join(" | ", Enumerable.Range(0, d.Length + 1).Select(k => N(all > 0 ? r.Sum(x => x.Engagement.Outside[k]) / all * 100 : 0, "0") + "%")) + " |");
            }
            md.AppendLine();
            return md.ToString();
        }

        // ---------------------------------------------------------------------------------------------------------

        private static string P(IEnumerable<float> values, double percentile, string format)
        {
            float[] sorted = values.Where(v => !float.IsNaN(v)).OrderBy(v => v).ToArray();
            if (sorted.Length == 0)
                return "-";
            double rank = percentile / 100.0 * (sorted.Length - 1);
            int low = (int)Math.Floor(rank);
            int high = Math.Min(sorted.Length - 1, low + 1);
            double value = sorted[low] + (sorted[high] - sorted[low]) * (rank - low);
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string N(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        private static string[] List(string name, string fallback) =>
            (Environment.GetEnvironmentVariable(name) ?? fallback).Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

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
