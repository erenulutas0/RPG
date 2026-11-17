using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The income docs/54's economy model reads (items, basma and efsun, 2026-11-16): what a run of the shipped Descent pays
    // in forge gold, by cohort and hero, and how many elites and floor bosses it fells - the drops docs/54's boosters and
    // floor materials hang on. Plays every cohort and both heroes down the whole Descent (floors 1-3 and the finale) on the
    // same seeds with human hands, as shipped.
    //
    // The simulation's FoundryResult keeps no gold, so the gold is rebuilt from what it does keep: each node-wave kill's enemy (its
    // base gold, read from DescentSimulation's Enemy fields, times the floor's ModifierGoldPercent through FloorScaling.Gold,
    // as the run pays it; a horde kill pays none) and half the chests opened (ChestRule: odd rooms pay ChestRule.GoldReward, even rooms heal). A
    // floor's gold is secured as the next floor begins; a death loses half the dying floor's gold (RunState's atRiskGoldLoss
    // 0.5). A drop still on the ground at a death is counted as paid, so a defeat reads slightly high.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~GearEconomyResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (100), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), FOUNDRY_REALISM (typical). Writes gear-economy.md and gear-economy.csv
    // (one row a run), which Tools/Economy/gear_economy.py reads.
    [Explicit("docs/54's income: run it by name.")]
    public sealed class GearEconomyResearch
    {
        private static readonly HashSet<string> Elites = new HashSet<string> { "Grunt Captain", "Mirror Sentry", "Cinder Bloom" };
        private static readonly HashSet<string> Bosses = new HashSet<string> { "Forge Warden", "Glass Regent" };

        internal sealed class Run
        {
            public string Cohort;
            public string Hero;
            public int Seed;
            public int FloorsCleared;
            public bool Completed;
            public int[] FloorGold = new int[4];
            public int[] FloorElites = new int[4];
            public int[] FloorBosses = new int[4];
            public int[] FloorChests = new int[4];
            public int Banked;
            public int Lost;
            public float Seconds;
        }

        [Test]
        public void TheDescentsIncome()
        {
            int count = Setting("FOUNDRY_SEEDS", 100);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";
            Dictionary<string, int> gold = BaseGold();
            var unknown = new HashSet<string>();
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var runs = new List<Run>();
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
            foreach (DescentSimulation.FoundryHero hero in heroes)
            {
                DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                    .Copy($"cohort {cohort.Name}, gear economy, {hands}");
                DescentSimulation.FoundryFloor chain = DescentSimulation.GloamChain(levers);
                float[] modifiers = Modifiers(chain);
                var results = new DescentSimulation.FoundryResult[seeds.Length];
                Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) },
                    i => results[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers, DescentSimulation.GloamChain(levers)));
                for (int i = 0; i < results.Length; i++)
                    runs.Add(Read(cohort.Name, hero.Name, seeds[i], results[i], modifiers, gold, unknown));
                TestContext.Progress.WriteLine($"{cohort.Name} {hero.Name}: done");
            }

            File.WriteAllText(Path.Combine(output, "gear-economy.md"), Report(runs, first, count, hands, unknown));
            File.WriteAllText(Path.Combine(output, "gear-economy.csv"), Csv(runs));
            TestContext.WriteLine(Report(runs, first, count, hands, unknown));
            Assert.That(runs.Count, Is.EqualTo(AutopilotCohort.Cohorts.Count * heroes.Length * count), "every seed ran");
        }

        // Every Enemy the simulation names, by name, with its base gold (the first field wins on a shared name).
        internal static Dictionary<string, int> BaseGold()
        {
            var gold = new Dictionary<string, int>();
            foreach (FieldInfo field in typeof(DescentSimulation).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType != typeof(DescentSimulation.Enemy))
                    continue;
                var enemy = (DescentSimulation.Enemy)field.GetValue(null);
                if (enemy != null && !gold.ContainsKey(enemy.Name))
                    gold[enemy.Name] = enemy.Gold;
            }
            return gold;
        }

        internal static float[] Modifiers(DescentSimulation.FoundryFloor chain)
        {
            var modifiers = new float[4];
            int i = 0;
            for (DescentSimulation.FoundryFloor f = chain; f != null && i < modifiers.Length; f = f.Next, i++)
                modifiers[i] = f.ModifierGoldPercent;
            return modifiers;
        }

        // tier: the difficulty tier the run was played on (docs/54 section 21), whose reward multiplies each kill's and
        // chest's gold as the scene pays it (FloorScaling.Gold with the tier); Forged by default.
        internal static Run Read(string cohort, string hero, int seed, DescentSimulation.FoundryResult result, float[] modifiers,
            Dictionary<string, int> gold, HashSet<string> unknown, Cryptforge.Progression.DifficultyTier tier = default)
        {
            var run = new Run { Cohort = cohort, Hero = hero, Seed = seed };
            int index = 0;
            int secured = 0;
            for (DescentSimulation.FoundryResult floor = result; floor != null && index < 4; floor = floor.NextFloor, index++)
            {
                int paid = 0;
                foreach (DescentSimulation.FoundryKill kill in floor.Kills)
                {
                    // A horde enemy pays a share of its experience and no gold (HordeDirector, EncounterController.Spawn).
                    if (kill.Horde)
                        continue;
                    if (!gold.TryGetValue(kill.Kind, out int baseGold))
                    {
                        lock (unknown)
                            unknown.Add(kill.Kind);
                        continue;
                    }
                    paid += FloorScaling.Gold(baseGold, modifiers[index], tier);
                    if (Elites.Contains(kill.Kind))
                        run.FloorElites[index]++;
                    if (Bosses.Contains(kill.Kind))
                        run.FloorBosses[index]++;
                }
                // Odd rooms' chests pay, even rooms' heal: half the chests opened, on average.
                paid += floor.ChestsOpened * FloorScaling.Gold(ChestRule.GoldReward, modifiers[index], tier) / 2;
                run.FloorChests[index] = floor.ChestsOpened;
                run.FloorGold[index] = paid;
                run.Seconds += floor.FloorSeconds;
                if (floor.Outcome == DescentSimulation.FoundryOutcome.Cleared)
                {
                    secured += paid;
                    run.FloorsCleared++;
                    if (FoundryBatch.IsFinale(floor))
                        run.Completed = true;
                }
                else
                {
                    run.Lost = floor.Outcome == DescentSimulation.FoundryOutcome.Died ? paid / 2 : 0;
                    secured += paid - run.Lost;
                }
            }
            run.Banked = secured;
            return run;
        }

        private static string Report(List<Run> runs, int first, int count, string hands, HashSet<string> unknown)
        {
            var md = new StringBuilder();
            md.AppendLine($"# The Descent's income: seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("Gold rebuilt from the kills (base gold times the floor's modifier) and half the chests; banked after a death's loss " +
                          "(half the dying floor's gold). Reached n: the share of runs that cleared floor n-1 and played floor n (4 = the finale). " +
                          "Elites / bosses: felled a run, mean.").AppendLine();
            md.AppendLine("| Cohort | Hero | Banked P25 / P50 / P75 | Banked mean | Lost mean | Reached 2 / 3 / 4 | Completed | Gold by floor (mean of runs that played it) | Elites | Bosses | Minutes P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var group in runs.GroupBy(r => (r.Cohort, r.Hero)))
            {
                Run[] g = group.ToArray();
                int[] banked = g.Select(r => r.Banked).OrderBy(v => v).ToArray();
                string byFloor = string.Join(" / ", Enumerable.Range(0, 4).Select(f =>
                {
                    Run[] played = g.Where(r => r.FloorsCleared >= f).ToArray();
                    return played.Length == 0 ? "-" : played.Average(r => r.FloorGold[f]).ToString("0", CultureInfo.InvariantCulture);
                }));
                md.AppendLine($"| {group.Key.Cohort} | {group.Key.Hero} | {P(banked, .25)} / {P(banked, .5)} / {P(banked, .75)} | " +
                              $"{F(g.Average(r => r.Banked), "0")} | {F(g.Average(r => r.Lost), "0")} | " +
                              $"{Share(g, 1)} / {Share(g, 2)} / {Share(g, 3)} | {F(100.0 * g.Count(r => r.Completed) / g.Length, "0")} % | {byFloor} | " +
                              $"{F(g.Average(r => r.FloorElites.Sum()), "0.0")} | {F(g.Average(r => r.FloorBosses.Sum()), "0.0")} | " +
                              $"{F(P(g.Select(r => (int)(r.Seconds / 60f * 10f)).OrderBy(v => v).ToArray(), .5) / 10.0, "0.0")} |");
            }
            md.AppendLine();
            if (unknown.Count > 0)
                md.AppendLine("Kinds without a base gold (counted as 0): " + string.Join(", ", unknown.OrderBy(s => s)) + ".").AppendLine();
            return md.ToString();
        }

        private static string F(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        private static string Share(Run[] g, int cleared) =>
            (100.0 * g.Count(r => r.FloorsCleared >= cleared) / g.Length).ToString("0", CultureInfo.InvariantCulture) + " %";

        private static int P(int[] sorted, double q) => sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)(q * sorted.Length))];

        internal static string Csv(List<Run> runs)
        {
            var csv = new StringBuilder();
            csv.AppendLine("cohort,hero,seed,floors_cleared,completed,banked,lost,gold_f1,gold_f2,gold_f3,gold_f4,elites_f1,elites_f2,elites_f3,elites_f4,bosses_f1,bosses_f2,bosses_f3,chests,minutes");
            foreach (Run r in runs)
                csv.AppendLine(string.Join(",", r.Cohort, r.Hero.Replace(',', ' '), r.Seed, r.FloorsCleared, r.Completed ? 1 : 0, r.Banked, r.Lost,
                    r.FloorGold[0], r.FloorGold[1], r.FloorGold[2], r.FloorGold[3], r.FloorElites[0], r.FloorElites[1], r.FloorElites[2],
                    r.FloorElites[3], r.FloorBosses[0], r.FloorBosses[1], r.FloorBosses[2], r.FloorChests.Sum(),
                    (r.Seconds / 60f).ToString("0.00", CultureInfo.InvariantCulture)));
            return csv.ToString();
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
    }
}
