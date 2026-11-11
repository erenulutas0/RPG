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
    // The Descent's length and the finale's heal (docs/47, research 2026-11-11). Two findings of docs/41's finale status
    // wait for the owner: nothing heals the hero between floor 2's Warden and the Final Swarm, and the whole foundry Descent
    // runs about 8 minutes against docs/40's 13-16, floor 2 the short one. This plays the menu of options, each cohort and
    // both heroes down every floor on the same seeds, so each option is read against the same heroes:
    //
    // - a heal as the finale opens (FoundryLevers.FinaleHealShare) or the finale's own quarter-heal chest where the hero
    //   arrives (FoundryLevers.FinaleChestAtArrival);
    // - floor 2's gate later (its FoundryFloor.GateNotBeforeSeconds; 0 on the asset, the gate open as its room clears), or
    //   floor 2 with all of floor 1's V0.1 knobs (the gate at 3:30, two surges a hall, an extra elite room);
    // - the finale's stay longer (FoundryLevers.FinaleStaySeconds), or a third climb floor before it - floor 2's asset again,
    //   at floor 2's tier or at floor 3's (ClimbScaling) with its Warden held to floor 2's 8.5;
    // - and the combinations the doc reads.
    //
    // Floors' knobs and the chain are built here (FoundryFloor's fields and Next) and played through RunFoundry's floor, so
    // the shipped floors and the default batch are untouched. Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~DescentLengthResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), DESCENT_OPTIONS (comma separated option keys; all), DESCENT_COHORTS (comma
    // separated cohort names; all five), DESCENT_REALISM (comma separated: frame-perfect, typical, slow; typical). Writes
    // descent-length.md and a row a run in descent-length.csv, both rewritten after each option.
    [Explicit("The Descent's length research: run it by name.")]
    public sealed class DescentLengthResearch
    {
        // Floor 3 at ClimbScaling's floor-3 tier (health x1.96, boss room x1.15 more): the gate share that keeps its Warden
        // at floor 2's 8.5 times authored (8.5 / (1.96 * 1.15)), as floor 2's 6.071429 cancels its own x1.4.
        private const float FloorThreeGateShare = 3.771074f;

        private sealed class Option
        {
            public string Key;
            public string What;
            public int Floors = 3;
            public float FloorTwoGate;
            public bool FloorTwoAsFloorOne;
            // 0: no third floor; 2 or 3: floor 2's asset again at that climb tier.
            public int ThirdFloorTier;
            public float ThirdFloorGate;
            public float HealShare;
            public bool ChestAtArrival;
            public float Stay = DescentSimulation.PilotSwarmStay;
        }

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "as shipped: floor 2's gate as its room clears, no heal, the 60 s stay" };
            yield return new Option { Key = "heal30", What = "a heal of 30 % of the maximum as the finale opens", HealShare = .3f };
            yield return new Option { Key = "heal50", What = "a heal of 50 % as the finale opens", HealShare = .5f };
            yield return new Option { Key = "healfull", What = "a full heal as the finale opens", HealShare = 1f };
            yield return new Option { Key = "chest", What = "the finale's own chest (a quarter heal) where the hero arrives", ChestAtArrival = true };
            yield return new Option { Key = "gate180", What = "floor 2's gate not before 3:00", FloorTwoGate = 180f };
            yield return new Option { Key = "gate210", What = "floor 2's gate not before 3:30, as floor 1's", FloorTwoGate = 210f };
            yield return new Option { Key = "f2asf1", What = "floor 2 with floor 1's V0.1 knobs: the gate at 3:30, two surges a hall, an extra elite room", FloorTwoGate = 210f, FloorTwoAsFloorOne = true };
            yield return new Option { Key = "stay90", What = "the finale's stay 90 s (docs/40's typical upper bound)", Stay = 90f };
            yield return new Option { Key = "third2", What = "a third floor: floor 2's asset again at floor 2's tier, its gate as the room clears", Floors = 4, ThirdFloorTier = 2 };
            yield return new Option { Key = "third3", What = "a third floor: floor 2's asset at floor 3's tier (x1.96 / x1.80, no modifier, its Warden held to 8.5), its gate as the room clears", Floors = 4, ThirdFloorTier = 3 };
            yield return new Option { Key = "set-a", What = "floor 2's gate at 3:30, the chest where the hero arrives, the 90 s stay", FloorTwoGate = 210f, ChestAtArrival = true, Stay = 90f };
            yield return new Option { Key = "set-b", What = "floor 2's gate at 3:30, a third floor at floor 3's tier with its gate at 3:30, a 50 % heal before the finale", Floors = 4, FloorTwoGate = 210f, ThirdFloorTier = 3, ThirdFloorGate = 210f, HealShare = .5f };
        }

        // The floors' chain an option plays, fresh for each run: floor 1 as shipped, floor 2 with the option's knobs, the
        // third floor where it asks for one, then the finale.
        private static DescentSimulation.FoundryFloor Chain(Option option)
        {
            DescentSimulation.FoundryFloor one = DescentSimulation.FoundryFloorOne();
            DescentSimulation.FoundryFloor two = one.Next;
            if (option.FloorTwoGate > 0f)
                two.GateNotBeforeSeconds = option.FloorTwoGate;
            if (option.FloorTwoAsFloorOne)
            {
                two.HallSurges = one.HallSurges;
                two.ExtraElites = one.ExtraElites;
            }
            if (option.ThirdFloorTier > 0)
            {
                DescentSimulation.FoundryFloor three = DescentSimulation.FoundryFloorTwo();
                three.Id = "floor_three_research";
                three.FloorNumber = option.ThirdFloorTier;
                if (option.ThirdFloorTier == 3)
                {
                    three.GateHealthShare = FloorThreeGateShare;
                    three.ModifierDamagePercent = 0f;
                    three.ModifierGoldPercent = 0f;
                }
                three.GateNotBeforeSeconds = option.ThirdFloorGate;
                DescentSimulation.FoundryFloor finale = DescentSimulation.FoundryFinale();
                finale.FloorNumber = 4;
                three.Next = finale;
                two.Next = three;
            }
            return one;
        }

        private sealed class Cell
        {
            public string Realism;
            public Option Option;
            public string Cohort;
            public string Hero;
            public DescentSimulation.FoundryResult[] Runs;
        }

        [Test]
        public void TheDescentsLengthByOption()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = List("DESCENT_OPTIONS", string.Join(",", Options().Select(o => o.Key)));
            Option[] options = Options().Where(o => keys.Contains(o.Key)).ToArray();
            string[] names = List("DESCENT_COHORTS", string.Join(",", AutopilotCohort.Cohorts.Select(c => c.Name)));
            AutopilotCohort[] cohorts = AutopilotCohort.Cohorts.Where(c => names.Contains(c.Name)).ToArray();
            string[] pilots = List("DESCENT_REALISM", "typical");
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var cells = new List<Cell>();
            foreach (string pilot in pilots)
            {
                PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
                foreach (Option option in options)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    foreach (AutopilotCohort cohort in cohorts)
                    foreach (DescentSimulation.FoundryHero hero in heroes)
                    {
                        DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, option.Floors, realism)
                            .Copy($"cohort {cohort.Name}, {option.Key}" + (realism != null ? ", " + realism.Name : string.Empty));
                        levers.FinaleStaySeconds = option.Stay;
                        levers.FinaleHealShare = option.HealShare;
                        levers.FinaleChestAtArrival = option.ChestAtArrival;
                        var runs = new DescentSimulation.FoundryResult[seeds.Length];
                        Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) },
                            i => runs[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers, Chain(option)));
                        cells.Add(new Cell { Realism = realism != null ? realism.Name : "frame-perfect", Option = option, Cohort = cohort.Name, Hero = hero.Name, Runs = runs });
                    }
                    TestContext.Progress.WriteLine($"{pilot} {option.Key}: {watch.Elapsed.TotalSeconds:0} s");
                    File.WriteAllText(Path.Combine(output, "descent-length.md"), Report(cells, first, count));
                    File.WriteAllText(Path.Combine(output, "descent-length.csv"), Csv(cells));
                }
            }
            TestContext.WriteLine(Report(cells, first, count));
            Assert.That(cells.All(c => c.Runs.Length == count), "every seed ran");
        }

        // A run's floors in order: floor 1, then each floor it went down to.
        private static DescentSimulation.FoundryResult[] FloorsOf(DescentSimulation.FoundryResult run)
        {
            var floors = new List<DescentSimulation.FoundryResult>();
            for (DescentSimulation.FoundryResult r = run; r != null; r = r.NextFloor)
                floors.Add(r);
            return floors.ToArray();
        }

        private static DescentSimulation.FoundryResult FinaleOf(DescentSimulation.FoundryResult run, Option option)
        {
            DescentSimulation.FoundryResult[] floors = FloorsOf(run);
            return floors.Length == option.Floors ? floors[floors.Length - 1] : null;
        }

        private static bool Completed(DescentSimulation.FoundryResult run, Option option) =>
            FinaleOf(run, option) is DescentSimulation.FoundryResult f && f.WalkedOut;

        private static float DescentSeconds(DescentSimulation.FoundryResult run) => FloorsOf(run).Sum(r => r.FloorSeconds);

        private static float EntryShare(DescentSimulation.FoundryResult finale) =>
            100f * finale.SwarmEntryHealth / Math.Max(1f, finale.SwarmEntryMaximum);

        private static float HealedEntryShare(DescentSimulation.FoundryResult finale) =>
            100f * Math.Min(finale.SwarmEntryMaximum, finale.SwarmEntryHealth + finale.SwarmHealed) / Math.Max(1f, finale.SwarmEntryMaximum);

        private static string Report(List<Cell> cells, int first, int count)
        {
            var md = new StringBuilder();
            md.AppendLine($"# The Descent's length: seeds {first}-{first + count - 1} a hero ({DateTime.Now:yyyy-MM-dd HH:mm})").AppendLine();
            md.AppendLine("Each run plays floor 1 as shipped, floor 2 with the option's knobs, a third floor where the option has one, then the finale's swarm (DescentSimulation.FoundryFinale), " +
                          "its pilot walking out at the option's stay. Both heroes pooled (Vanguard and Ilya, 2 x seeds a cell). Reached: the finale, of all runs. Walked out: of those that reached it. " +
                          "Finale deaths: each death's health entering the finale as a share of its maximum, before any heal (after the option's heal in brackets). " +
                          "Completed: walked out of the finale, of all runs. The Descent: every floor's active seconds of the completed runs, in minutes.").AppendLine();
            foreach (IGrouping<string, Cell> byRealism in cells.GroupBy(c => c.Realism))
            {
                md.AppendLine($"## {byRealism.Key}").AppendLine();
                md.AppendLine("| Option | Cohort | Reached finale | Walked out (of reached) | Finale deaths: entering health % | Completed | Descent P10 / P50 / P90 (min) | Within 13-16 min |");
                md.AppendLine("|---|---|---|---|---|---|---|---|");
                foreach (IGrouping<string, Cell> byOption in byRealism.GroupBy(c => c.Option.Key))
                foreach (IGrouping<string, Cell> byCohort in byOption.GroupBy(c => c.Cohort))
                    md.AppendLine(Row(byCohort.First().Option, byCohort.Key, byCohort.SelectMany(c => c.Runs).ToArray()));
                md.AppendLine();
                md.AppendLine("Floors: P50 active seconds of the completed runs. Cleared floor 2 / floor 3: of the runs that entered it. Finale entering health: P10 / P50 of the runs that reached it, after the option's heal. Chest: the finale's chest opened, of the runs that reached it.").AppendLine();
                md.AppendLine("| Option | Cohort | Floor 1 / 2 / 3 / finale P50 (s) | Cleared floor 2 | Cleared floor 3 | Finale entering health P10 / P50 | Chest opened |");
                md.AppendLine("|---|---|---|---|---|---|---|");
                foreach (IGrouping<string, Cell> byOption in byRealism.GroupBy(c => c.Option.Key))
                foreach (IGrouping<string, Cell> byCohort in byOption.GroupBy(c => c.Cohort))
                    md.AppendLine(FloorRow(byCohort.First().Option, byCohort.Key, byCohort.SelectMany(c => c.Runs).ToArray()));
                md.AppendLine();
            }
            md.AppendLine("## The options").AppendLine();
            foreach (Option option in cells.Select(c => c.Option).Distinct())
                md.AppendLine($"- `{option.Key}`: {option.What}.");
            return md.ToString();
        }

        private static string Row(Option option, string cohort, DescentSimulation.FoundryResult[] runs)
        {
            DescentSimulation.FoundryResult[] reached = runs.Select(r => FinaleOf(r, option)).Where(f => f != null).ToArray();
            DescentSimulation.FoundryResult[] died = reached.Where(f => f.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            int walked = reached.Count(f => f.WalkedOut);
            float[] descent = runs.Where(r => Completed(r, option)).Select(DescentSeconds).ToArray();
            int within = descent.Count(s => s >= FoundryBatch.FullRunFrom && s <= FoundryBatch.FullRunTo);
            string deaths = died.Length == 0 ? "0"
                : $"{died.Length}: " + string.Join(", ", died.OrderBy(EntryShare).Take(12).Select(f => option.HealShare > 0f
                    ? $"{EntryShare(f):0} ({HealedEntryShare(f):0})" : $"{EntryShare(f):0}")) +
                  (died.Length > 12 ? ", ..." : string.Empty);
            return $"| `{option.Key}` | {cohort} | {reached.Length} ({Pct(reached.Length, runs.Length)}) | {walked} ({Pct(walked, reached.Length)}) | {deaths} | " +
                   $"{descent.Length} ({Pct(descent.Length, runs.Length)}) | {P(descent, 10)} / {P(descent, 50)} / {P(descent, 90)} | {within} ({Pct(within, descent.Length)}) |";
        }

        private static string FloorRow(Option option, string cohort, DescentSimulation.FoundryResult[] runs)
        {
            DescentSimulation.FoundryResult[][] done = runs.Where(r => Completed(r, option)).Select(FloorsOf).ToArray();
            string Floor(int index) => done.Length == 0 || done[0].Length <= index ? "-"
                : F(FoundryBatch.Median(done.Select(f => f[index].FloorSeconds)));
            string floors = option.Floors == 4
                ? $"{Floor(0)} / {Floor(1)} / {Floor(2)} / {Floor(3)}"
                : $"{Floor(0)} / {Floor(1)} / - / {Floor(2)}";
            DescentSimulation.FoundryResult[][] all = runs.Select(FloorsOf).ToArray();
            int enteredTwo = all.Count(f => f.Length >= 2), clearedTwo = all.Count(f => f.Length >= 2 && f[1].Cleared);
            string three = "-";
            if (option.Floors == 4)
            {
                int enteredThree = all.Count(f => f.Length >= 3), clearedThree = all.Count(f => f.Length >= 3 && f[2].Cleared);
                three = $"{clearedThree} / {enteredThree} ({Pct(clearedThree, enteredThree)})";
            }
            DescentSimulation.FoundryResult[] reached = runs.Select(r => FinaleOf(r, option)).Where(f => f != null).ToArray();
            int chest = reached.Count(f => f.ChestsOpened > 0);
            return $"| `{option.Key}` | {cohort} | {floors} | {clearedTwo} / {enteredTwo} ({Pct(clearedTwo, enteredTwo)}) | {three} | " +
                   $"{F(FoundryBatch.Percentile(reached.Select(HealedEntryShare), 10))} / {F(FoundryBatch.Percentile(reached.Select(HealedEntryShare), 50))} % | " +
                   $"{chest} ({Pct(chest, reached.Length)}) |";
        }

        private static string Csv(List<Cell> cells)
        {
            var csv = new StringBuilder();
            csv.AppendLine("realism,option,cohort,hero,seed,floors_played,floor1_s,floor2_s,floor3_s,finale_s,descent_s,last_outcome,reached_finale,walked_out," +
                           "finale_entry_hp_pct,finale_healed_pct,finale_chest,death_floor,death_phase,killer");
            foreach (Cell cell in cells)
            foreach (DescentSimulation.FoundryResult run in cell.Runs)
            {
                DescentSimulation.FoundryResult[] floors = FloorsOf(run);
                DescentSimulation.FoundryResult last = floors[floors.Length - 1];
                DescentSimulation.FoundryResult finale = FinaleOf(run, cell.Option);
                string S(int index) => floors.Length > index ? Num(floors[index].FloorSeconds) : string.Empty;
                bool four = cell.Option.Floors == 4;
                csv.AppendLine(string.Join(",", cell.Realism, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, floors.Length,
                    S(0), S(1), four ? S(2) : string.Empty, finale != null ? Num(finale.FloorSeconds) : string.Empty,
                    Num(DescentSeconds(run)), last.Outcome, finale != null ? 1 : 0, finale != null && finale.WalkedOut ? 1 : 0,
                    finale != null ? Num(EntryShare(finale), "0.0") : string.Empty,
                    finale != null ? Num(HealedEntryShare(finale), "0.0") : string.Empty,
                    finale != null ? finale.ChestsOpened : 0,
                    last.Outcome == DescentSimulation.FoundryOutcome.Died ? floors.Length : 0,
                    Quote(last.Outcome == DescentSimulation.FoundryOutcome.Died ? last.DeathPhase : null),
                    Quote(last.Outcome == DescentSimulation.FoundryOutcome.Died ? last.Killer : null)));
            }
            return csv.ToString();
        }

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string Num(float value, string format = "0.00") => value.ToString(format, CultureInfo.InvariantCulture);

        private static string P(float[] seconds, double p)
        {
            double value = FoundryBatch.Percentile(seconds, p);
            return double.IsNaN(value) ? "-" : (value / 60.0).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string F(double value) => double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

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
