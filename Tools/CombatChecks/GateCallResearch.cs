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
    // docs/41's open decision 2 (2026-11-11): what fills the gate's wait. The floor holds its cleared room before the gate
    // until 3:30 on its clock, about 111-117 s of a 285 s floor, where the hero farms the horde almost risk-free. The idea
    // measured here: the player may call the Warden early - the gate's bell, the HUD's "Gate opens in" turned into a
    // prompt - once the hold begins, under a policy (at once, at a level, at a level with its health high), and the call
    // may pay a reward (a heal, upgrade choices, experience for the seconds skipped). Each configuration is played by every
    // cohort, frame-perfect and with a human's hands, down both floors, on the same seeds; FoundryLevers.GateCalls and its
    // rewards are research levers of the simulation alone, off by default. Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~GateCallResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (the processors), FOUNDRY_OUT
    // (the repository's TestResults), FOUNDRY_FLOORS (2), CALL_REALISM (comma separated: frame-perfect, typical, slow;
    // the first two by default), CALL_COHORTS (comma separated cohort names; every cohort by default), CALL_CONFIGS
    // (semicolon separated configurations in the words below; Configurations by default). Writes gate-call.md, the cells
    // in gate-call.csv and every run in gate-call-runs.csv.
    //
    // A configuration's words: "wait" (the shipped floor, no call); "once" (call as the hold begins); "level N" (call once
    // the hero is at level N or more); "health X" (and its health at X of its maximum or more); "heal X" (the call heals X
    // of the maximum); "upgrades N" (the call grants N upgrade choices, the forge's Temper); "xp X" (the call pays X of
    // DescentSimulation.GateWaitExperienceRate for each second of the hold skipped).
    [Explicit("The gate's early call research: run it by name.")]
    public sealed class GateCallResearch
    {
        // The policies - at once, at the careful pilot's level at about 2:20, 2:40 and 3:00 on the floor's clock (18, 20,
        // 22: it is at 19 at 2:30 and 22 at 3:00), at once or at level 20 with its health at 80 % or more - then the three
        // rewards on the call at once and at level 20 (docs/41, 2026-11-11).
        public const string Configurations =
            "wait;once;level 18;level 20;level 22;health 0.8;health 0.8 level 20;" +
            "once heal 1;once upgrades 1;once xp 0.5;level 20 heal 1;level 20 upgrades 1;level 20 xp 0.5";

        private int _parallel;
        private int[] _seeds;
        private int _floors;

        [Test]
        public void TheGatesEarlyCallOverASeedBatch()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            _parallel = Setting("FOUNDRY_PARALLEL", Environment.ProcessorCount);
            _floors = Setting("FOUNDRY_FLOORS", 2);
            _seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] pilots = List("CALL_REALISM", "frame-perfect,typical", ',');
            string[] cohortNames = List("CALL_COHORTS", string.Join(",", AutopilotCohort.Cohorts.Select(c => c.Name)), ',');
            AutopilotCohort[] cohorts = AutopilotCohort.Cohorts.Where(c => cohortNames.Contains(c.Name)).ToArray();
            Config[] configs = List("CALL_CONFIGS", Configurations, ';').Select(Config.Parse).ToArray();
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var cells = new List<Cell>();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (string pilot in pilots)
            {
                PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
                foreach (Config config in configs)
                    foreach (AutopilotCohort cohort in cohorts)
                        foreach (DescentSimulation.FoundryHero hero in heroes)
                        {
                            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("gate call: " + config.Name);
                            levers.Cohort = cohort;
                            levers.Realism = realism;
                            levers.Floors = _floors;
                            config.Set(levers);
                            FoundryBatch.Batch batch = FoundryBatch.Run(hero, levers, _seeds, _parallel);
                            cells.Add(new Cell { Config = config, Pilot = pilot, Cohort = cohort, Hero = hero, Runs = batch.Runs });
                        }
                TestContext.Progress.WriteLine($"{pilot}: {configs.Length} configurations in {watch.Elapsed.TotalMinutes:0.0} min");
            }

            var md = new StringBuilder();
            md.AppendLine("# The gate's wait: an early call").AppendLine();
            md.AppendLine($"Seeds {_seeds[0]}-{_seeds[_seeds.Length - 1]} a hero, {_floors} floor(s), pilots {string.Join(", ", pilots)}, " +
                          $"cohorts {string.Join(", ", cohorts.Select(c => c.Name))} ({DateTime.Now:yyyy-MM-dd HH:mm}, " +
                          $"{watch.Elapsed.TotalMinutes:0} min). Every cell is Vanguard / Ilya unless it names a hero. The experience " +
                          $"rate of the \"xp\" reward: {N(DescentSimulation.GateWaitExperienceRate, "0.##")} a second skipped.").AppendLine();
            md.AppendLine("| Configuration | Words |");
            md.AppendLine("|---|---|");
            foreach (Config config in configs)
                md.AppendLine($"| {config.Name} | {config.Words} |");
            md.AppendLine();
            if (configs.Any(c => c.Words == "wait"))
                md.Append(Wait(cells.Where(c => c.Config.Words == "wait").ToList(), pilots, cohorts));
            md.Append(Clears(cells, configs, pilots, cohorts));
            if (configs.Any(c => c.Words == "wait"))
                md.Append(AgainstWaiting(cells, configs, pilots, cohorts));
            md.Append(Careful(cells, configs, pilots));
            md.Append(Detail(cells));
            File.WriteAllText(Path.Combine(output, "gate-call.md"), md.ToString());
            File.WriteAllText(Path.Combine(output, "gate-call.csv"), CellCsv(cells));
            File.WriteAllText(Path.Combine(output, "gate-call-runs.csv"), RunCsv(cells));
            TestContext.WriteLine(md.ToString());
            Assert.That(cells.All(c => c.Runs.Length == _seeds.Length), "every seed ran");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Configurations
        // ---------------------------------------------------------------------------------------------------------

        private sealed class Config
        {
            public string Words;
            public string Name;
            public Action<DescentSimulation.FoundryLevers> Set;

            public static Config Parse(string words)
            {
                string[] w = words.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var sets = new List<Action<DescentSimulation.FoundryLevers>>();
                string policy = null;
                float health = 0f;
                var rewards = new List<string>();
                for (int i = 0; i < w.Length; i++)
                {
                    switch (w[i])
                    {
                        case "wait":
                            policy = "wait (shipped)";
                            break;
                        case "once":
                            sets.Add(l => l.GateCalls = true);
                            policy = "call at once";
                            break;
                        case "level":
                        {
                            int level = int.Parse(w[++i], CultureInfo.InvariantCulture);
                            sets.Add(l =>
                            {
                                l.GateCalls = true;
                                l.GateCallLevel = level;
                            });
                            policy = $"call at level {level}";
                            break;
                        }
                        case "health":
                        {
                            float share = Float(w[++i]);
                            sets.Add(l =>
                            {
                                l.GateCalls = true;
                                l.GateCallHealth = share;
                            });
                            health = share;
                            break;
                        }
                        case "heal":
                        {
                            float share = Float(w[++i]);
                            sets.Add(l => l.GateCallHeal = share);
                            rewards.Add($"heal {N(100f * share, "0")} %");
                            break;
                        }
                        case "upgrades":
                        {
                            int choices = int.Parse(w[++i], CultureInfo.InvariantCulture);
                            sets.Add(l => l.GateCallUpgrades = choices);
                            rewards.Add(choices == 1 ? "1 upgrade choice" : $"{choices} upgrade choices");
                            break;
                        }
                        case "xp":
                        {
                            float share = Float(w[++i]);
                            sets.Add(l => l.GateCallExperienceShare = share);
                            rewards.Add($"XP {N(100f * share, "0")} % of the wait's");
                            break;
                        }
                        default:
                            throw new ArgumentException($"No such word in a configuration: {w[i]} ({words})");
                    }
                }
                if (policy == null)
                {
                    sets.Add(l => l.GateCalls = true);
                    policy = "call at once";
                }
                if (health > 0f)
                    policy += $" with health {N(100f * health, "0")} %";
                string name = rewards.Count == 0 ? policy : policy + " + " + string.Join(" + ", rewards);
                return new Config
                {
                    Words = string.Join(" ", w), Name = name, Set = l =>
                    {
                        foreach (Action<DescentSimulation.FoundryLevers> set in sets)
                            set(l);
                    }
                };
            }
        }

        // One configuration, pilot, cohort and hero over the seeds.
        private sealed class Cell
        {
            public Config Config;
            public string Pilot;
            public AutopilotCohort Cohort;
            public DescentSimulation.FoundryHero Hero;
            public DescentSimulation.FoundryResult[] Runs;

            public int Cleared => Runs.Count(r => r.Cleared);
            public int BothCleared => Runs.Count(BothFloors);
            public IEnumerable<DescentSimulation.FoundryResult> Clears => Runs.Where(r => r.Cleared);
            public IEnumerable<DescentSimulation.FoundryResult> Gated => Runs.Where(r => r.LevelAtGate >= 0);
            public IEnumerable<DescentSimulation.FoundryResult> Felled => Runs.Where(r => r.BossSeconds >= 0f);
            public IEnumerable<DescentSimulation.FoundryResult> Ready => Runs.Where(r => r.GateReadyClock >= 0f);
            public IEnumerable<DescentSimulation.FoundryResult> Second => Runs.Where(r => r.NextFloor != null).Select(r => r.NextFloor);
        }

        private static bool BothFloors(DescentSimulation.FoundryResult r) => r.Cleared && r.NextFloor != null && r.NextFloor.Cleared;

        // ---------------------------------------------------------------------------------------------------------
        // The tables
        // ---------------------------------------------------------------------------------------------------------

        // What the wait gives, read off the shipped floor's cells: when the room before the gate clears, the level by the
        // floor's clock (the 30 s bucket ending at 2:00, 2:30, 3:00 and 3:30 of the floor), the level at the gate, and the
        // wait's seconds, levels, experience a second and health.
        private static string Wait(List<Cell> cells, string[] pilots, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("## What the wait gives (the shipped floor)").AppendLine();
            md.AppendLine("Ready: the floor's clock, the hero's level and its health (a share of its maximum) as the room before the gate cleared, " +
                          "P50 of the runs that got there. Level by the floor's time: P50 (P10-P90) at the end of the 30 s buckets ending at 2:00, 2:30, " +
                          "3:00 and 3:30 of the floor's active time (the clock runs about a second behind it, the forge's), of the runs still alive then. " +
                          "Wait: the clears' P50. Levels, health lost and kills in the wait: mean of the clears that waited. XP a second: the wait's " +
                          "experience over its seconds, P50 of the clears that waited 10 s or more. Died in the wait: of every run.").AppendLine();
            md.AppendLine("| Pilot | Cohort | Hero | Ready: clock / level / health | Level at 2:00 | 2:30 | 3:00 | 3:30 | Level at the gate | Wait | Levels in the wait | XP a second | Health lost in the wait | Kills in the wait | Died in the wait |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (string pilot in pilots)
                foreach (AutopilotCohort cohort in cohorts)
                    foreach (Cell c in cells.Where(x => x.Pilot == pilot && x.Cohort == cohort))
                    {
                        DescentSimulation.FoundryResult[] ready = c.Ready.ToArray();
                        DescentSimulation.FoundryResult[] waited = c.Clears.Where(r => r.GateWaitSeconds > 0f).ToArray();
                        DescentSimulation.FoundryResult[] longWaits = waited.Where(r => r.GateWaitSeconds >= 10f).ToArray();
                        md.AppendLine($"| {pilot} | {cohort.Name} | {c.Hero.Name} | " +
                                      $"{P(ready.Select(r => r.GateReadyClock), 50)} s / {P(ready.Select(r => (float)r.GateReadyLevel), 50)} / " +
                                      $"{P(ready.Select(r => 100f * r.GateReadyHealth), 50)} % | " +
                                      $"{LevelAt(c, 3)} | {LevelAt(c, 4)} | {LevelAt(c, 5)} | {LevelAt(c, 6)} | " +
                                      $"{P(c.Gated.Select(r => (float)r.LevelAtGate), 50)} | {P(c.Clears.Select(r => r.GateWaitSeconds), 50)} s | " +
                                      $"{Mean(waited.Select(r => (float)r.WaitLevels), "0.0")} | " +
                                      $"{P(longWaits.Select(r => r.WaitExperience / r.GateWaitSeconds), 50, "0.00")} | " +
                                      $"{Mean(waited.Select(r => r.WaitTaken))} | {Mean(waited.Select(r => (float)r.WaitKills))} | " +
                                      $"{c.Runs.Count(r => r.DeathPhase == DescentSimulation.GateWaitPhase)} |");
                    }
            md.AppendLine();
            // The careful clears' experience a second of the wait over every pilot and hero, as GateWaitExperienceRate reads.
            float[] rates = cells.Where(c => c.Cohort == AutopilotCohort.Careful)
                .SelectMany(c => c.Clears.Where(r => r.GateWaitSeconds >= 10f).Select(r => r.WaitExperience / r.GateWaitSeconds)).ToArray();
            md.AppendLine($"The careful clears' experience a second of the wait, every pilot and hero together: P50 {P(rates, 50, "0.00")} " +
                          $"(P10 {P(rates, 10, "0.00")}, P90 {P(rates, 90, "0.00")}, {rates.Length} clears).").AppendLine();
            return md.ToString();
        }

        // The level at the end of a 30 s bucket of the floor's time, P50 (P10-P90) of the runs that lived into the next one.
        private static string LevelAt(Cell c, int bucket)
        {
            float[] levels = c.Runs.Where(r => r.Buckets > bucket + 1).Select(r => (float)r.BucketLevel[bucket]).ToArray();
            return levels.Length == 0 ? "-" : $"{P(levels, 50)} ({P(levels, 10)}-{P(levels, 90)})";
        }

        // Floor-1 and both-floor clears, every configuration by every cohort.
        private static string Clears(List<Cell> cells, Config[] configs, string[] pilots, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("## Clears").AppendLine();
            foreach (string pilot in pilots)
                foreach (bool both in new[] { false, true })
                {
                    md.AppendLine($"### {pilot}: {(both ? "both floors" : "floor 1")} cleared of {cells[0].Runs.Length}").AppendLine();
                    md.AppendLine("| Configuration | " + string.Join(" | ", cohorts.Select(c => c.Name)) + " |");
                    md.AppendLine("|---|" + string.Concat(cohorts.Select(_ => "---|")));
                    foreach (Config config in configs)
                        md.AppendLine($"| {config.Name} | " + string.Join(" | ", cohorts.Select(cohort =>
                            Pair(cells, config, pilot, cohort, c => (both ? c.BothCleared : c.Cleared).ToString(CultureInfo.InvariantCulture)))) + " |");
                    md.AppendLine();
                }
            return md.ToString();
        }

        // The behavioural question: each configuration's clears less the wait's, seed for seed the same floor up to the
        // hold, and the floor-1 time a clear saves; then each cohort's best configuration.
        private static string AgainstWaiting(List<Cell> cells, Config[] configs, string[] pilots, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("## Against waiting").AppendLine();
            md.AppendLine("Each configuration's clears less the wait's, floor 1 ; both floors, Vanguard / Ilya; then the floor-1 clears' P50 " +
                          "time less the wait's (negative: faster). The runs are the same until the hold, so a difference is the call's.").AppendLine();
            foreach (string pilot in pilots)
            {
                md.AppendLine($"### {pilot}").AppendLine();
                md.AppendLine("| Configuration | " + string.Join(" | ", cohorts.Select(c => c.Name)) + " | careful floor-1 clear time |");
                md.AppendLine("|---|" + string.Concat(cohorts.Select(_ => "---|")) + "---|");
                foreach (Config config in configs.Where(c => c.Words != "wait"))
                {
                    var cellsOut = new List<string>();
                    foreach (AutopilotCohort cohort in cohorts)
                    {
                        Cell[] wait = Of(cells, "wait", pilot, cohort);
                        Cell[] call = Of(cells, config.Words, pilot, cohort);
                        string f1 = string.Join(" / ", call.Zip(wait, (a, b) => Signed(a.Cleared - b.Cleared)));
                        string bothFloors = string.Join(" / ", call.Zip(wait, (a, b) => Signed(a.BothCleared - b.BothCleared)));
                        cellsOut.Add($"{f1} ; {bothFloors}");
                    }
                    Cell[] carefulWait = Of(cells, "wait", pilot, AutopilotCohort.Careful);
                    Cell[] carefulCall = Of(cells, config.Words, pilot, AutopilotCohort.Careful);
                    string saved = carefulWait.Length == 0 || carefulCall.Length == 0 ? "-" : string.Join(" / ", carefulCall.Zip(carefulWait, (a, b) =>
                        Signed(FoundryBatch.Median(a.Clears.Select(r => r.FloorSeconds)) - FoundryBatch.Median(b.Clears.Select(r => r.FloorSeconds))) + " s"));
                    md.AppendLine($"| {config.Name} | {string.Join(" | ", cellsOut)} | {saved} |");
                }
                md.AppendLine();

                md.AppendLine($"Each cohort's best configuration by both floors' clears, the two heroes together (ties: floor 1's, then the " +
                              "faster floor), against the wait:").AppendLine();
                md.AppendLine("| Cohort | Wait: floor 1 ; both | Best | Its clears: floor 1 ; both | Best call | Its clears: floor 1 ; both |");
                md.AppendLine("|---|---|---|---|---|---|");
                foreach (AutopilotCohort cohort in cohorts)
                {
                    Config Best(IEnumerable<Config> pool) => pool
                        .OrderByDescending(cf => Of(cells, cf.Words, pilot, cohort).Sum(c => c.BothCleared))
                        .ThenByDescending(cf => Of(cells, cf.Words, pilot, cohort).Sum(c => c.Cleared))
                        .ThenBy(cf => Of(cells, cf.Words, pilot, cohort).Sum(c => FoundryBatch.Median(c.Clears.Select(r => r.FloorSeconds))))
                        .First();
                    string Clears2(Config cf) => Pair(cells, cf, pilot, cohort, c => c.Cleared.ToString(CultureInfo.InvariantCulture)) + " ; " +
                                                 Pair(cells, cf, pilot, cohort, c => c.BothCleared.ToString(CultureInfo.InvariantCulture));
                    Config wait = configs.First(c => c.Words == "wait");
                    Config best = Best(configs);
                    Config bestCall = Best(configs.Where(c => c.Words != "wait"));
                    md.AppendLine($"| {cohort.Name} | {Clears2(wait)} | {best.Name} | {Clears2(best)} | {bestCall.Name} | {Clears2(bestCall)} |");
                }
                md.AppendLine();
            }
            return md.ToString();
        }

        // The careful cohort, every configuration: the floor's time, the level and health at the gate, the Warden, the
        // deaths, the wait and the call.
        private static string Careful(List<Cell> cells, Config[] configs, string[] pilots)
        {
            var md = new StringBuilder();
            md.AppendLine("## The careful cohort").AppendLine();
            md.AppendLine(Legend()).AppendLine();
            md.AppendLine(Header(false));
            md.AppendLine(Rule(false));
            foreach (string pilot in pilots)
                foreach (Config config in configs)
                {
                    Cell[] pair = Of(cells, config.Words, pilot, AutopilotCohort.Careful);
                    if (pair.Length > 0)
                        md.AppendLine($"| {config.Name} | {pilot} | {Columns(pair)} |");
                }
            md.AppendLine();
            return md.ToString();
        }

        // Every cohort, every configuration, as the careful table reads.
        private static string Detail(List<Cell> cells)
        {
            var md = new StringBuilder();
            md.AppendLine("## Every cohort").AppendLine();
            md.AppendLine("<details><summary>Every configuration, pilot and cohort</summary>").AppendLine();
            md.AppendLine(Header(true));
            md.AppendLine(Rule(true));
            foreach (IGrouping<string, Cell> group in cells.GroupBy(c => c.Config.Words + "|" + c.Pilot + "|" + c.Cohort.Name))
            {
                Cell[] pair = group.ToArray();
                md.AppendLine($"| {pair[0].Config.Name} | {pair[0].Pilot} | {pair[0].Cohort.Name} | {Columns(pair)} |");
            }
            md.AppendLine().AppendLine("</details>").AppendLine();
            return md.ToString();
        }

        private static string Legend() =>
            "Cleared: floor 1 ; both floors, of the seeds. Floor 1: active seconds, P50 of every run (of the clears). Both floors: floor 1 and " +
            "floor 2's seconds together, P50 of the runs that cleared both. Level and health at the gate: as the Warden walked out (P50 of the runs " +
            "that got there; health a share of the maximum). Warden's health: its maximum as it walked out, P50 - the run's difficulty, " +
            "which altars and evolutions raise, multiplies it. Warden: floor 1's fight from walking out of the gate to its fall, P50 (P90) of the " +
            "felled fights; floor 2's Warden's own. Died at the Warden: floor 1 / floor 2. Died in the wait: while the gate held. Wait: the clears' " +
            "P50. Called: the runs that called of those whose room before the gate cleared; the floor's clock at the call and the hold's seconds " +
            "skipped, P50 of the calls. Paid: the call's experience, P50 of the calls.";

        private static string Header(bool cohort) =>
            "| Configuration | Pilot | " + (cohort ? "Cohort | " : string.Empty) +
            "Cleared | Floor 1 | Both floors | Level at the gate | Health at the gate | Warden's health | Warden floor 1 | Warden floor 2 | Died at the Warden | Died in the wait | Wait | Called | Call clock | Skipped | Paid |";

        private static string Rule(bool cohort) => "|---|---|" + (cohort ? "---|" : string.Empty) + string.Concat(Enumerable.Repeat("---|", 15));

        private static string Columns(Cell[] pair)
        {
            string Both(Func<Cell, string> read) => string.Join(" / ", pair.Select(read));
            return string.Join(" | ", new[]
            {
                Both(c => c.Cleared.ToString(CultureInfo.InvariantCulture)) + " ; " + Both(c => c.BothCleared.ToString(CultureInfo.InvariantCulture)),
                Both(c => $"{P(c.Runs.Select(r => r.FloorSeconds), 50)} ({P(c.Clears.Select(r => r.FloorSeconds), 50)})"),
                Both(c => P(c.Runs.Where(BothFloors).Select(r => r.FloorSeconds + r.NextFloor.FloorSeconds), 50)),
                Both(c => P(c.Gated.Select(r => (float)r.LevelAtGate), 50)),
                Both(c => P(c.Gated.Select(r => 100f * r.HeroHealthAtGate / Math.Max(1f, r.HeroMaximumAtGate)), 50) + " %"),
                Both(c => P(c.Gated.Select(r => r.WardenHealth), 50)),
                Both(c => $"{P(c.Felled.Select(r => r.BossSeconds), 50)} ({P(c.Felled.Select(r => r.BossSeconds), 90)})"),
                Both(c => $"{P(c.Second.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds), 50)} ({P(c.Second.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds), 90)})"),
                Both(c => c.Runs.Count(r => r.DiedAtWarden).ToString(CultureInfo.InvariantCulture)) + " ; " +
                Both(c => c.Second.Count(r => r.DiedAtWarden).ToString(CultureInfo.InvariantCulture)),
                Both(c => c.Runs.Count(r => r.DeathPhase == DescentSimulation.GateWaitPhase).ToString(CultureInfo.InvariantCulture)),
                Both(c => P(c.Clears.Select(r => r.GateWaitSeconds), 50)),
                Both(c => $"{c.Runs.Count(r => r.GateCalled)}/{c.Ready.Count()}"),
                Both(c => P(c.Runs.Where(r => r.GateCalled).Select(r => r.GateCallClock), 50)),
                Both(c => P(c.Runs.Where(r => r.GateCalled).Select(r => r.GateCallSkipped), 50)),
                Both(c => P(c.Runs.Where(r => r.GateCalled).Select(r => (float)r.GateCallExperience), 50))
            });
        }

        private static string CellCsv(List<Cell> cells)
        {
            var csv = new StringBuilder();
            csv.AppendLine("config,words,pilot,cohort,hero,runs,cleared,both_cleared,floor_p50,cleared_floor_p50,both_floors_p50,level_at_gate_p50," +
                           "health_at_gate_p50,warden_health_p50,warden_p50,warden_p90,f2_warden_p50,f2_warden_p90,died_at_warden,f2_died_at_warden,died_in_wait," +
                           "wait_p50,called,ready,call_clock_p50,skipped_p50,paid_p50");
            foreach (Cell c in cells)
                csv.AppendLine(string.Join(",", new[]
                {
                    Quote(c.Config.Name), Quote(c.Config.Words), c.Pilot, c.Cohort.Name, c.Hero.Id, I(c.Runs.Length), I(c.Cleared), I(c.BothCleared),
                    P(c.Runs.Select(r => r.FloorSeconds), 50, "0.0"), P(c.Clears.Select(r => r.FloorSeconds), 50, "0.0"),
                    P(c.Runs.Where(BothFloors).Select(r => r.FloorSeconds + r.NextFloor.FloorSeconds), 50, "0.0"),
                    P(c.Gated.Select(r => (float)r.LevelAtGate), 50, "0.0"),
                    P(c.Gated.Select(r => r.HeroHealthAtGate / Math.Max(1f, r.HeroMaximumAtGate)), 50, "0.000"),
                    P(c.Gated.Select(r => r.WardenHealth), 50, "0"),
                    P(c.Felled.Select(r => r.BossSeconds), 50, "0.0"), P(c.Felled.Select(r => r.BossSeconds), 90, "0.0"),
                    P(c.Second.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds), 50, "0.0"),
                    P(c.Second.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds), 90, "0.0"),
                    I(c.Runs.Count(r => r.DiedAtWarden)), I(c.Second.Count(r => r.DiedAtWarden)),
                    I(c.Runs.Count(r => r.DeathPhase == DescentSimulation.GateWaitPhase)),
                    P(c.Clears.Select(r => r.GateWaitSeconds), 50, "0.0"), I(c.Runs.Count(r => r.GateCalled)), I(c.Ready.Count()),
                    P(c.Runs.Where(r => r.GateCalled).Select(r => r.GateCallClock), 50, "0.0"),
                    P(c.Runs.Where(r => r.GateCalled).Select(r => r.GateCallSkipped), 50, "0.0"),
                    P(c.Runs.Where(r => r.GateCalled).Select(r => (float)r.GateCallExperience), 50, "0")
                }));
            return csv.ToString();
        }

        private static string RunCsv(List<Cell> cells)
        {
            var csv = new StringBuilder();
            csv.AppendLine("words,pilot,cohort,hero,seed,f1_outcome,f1_floor_s,death_phase,death_room,killer,ready_clock,ready_level,ready_health," +
                           "gate_wait_s,wait_levels,wait_xp,wait_taken,called,call_clock,skipped,paid_xp,level_at_gate,health_at_gate,max_at_gate," +
                           "difficulty_at_gate,warden_health,warden_s,died_at_warden,f2_outcome,f2_floor_s,f2_level_at_gate,f2_warden_s,f2_died_at_warden");
            foreach (Cell c in cells)
                foreach (DescentSimulation.FoundryResult r in c.Runs)
                {
                    DescentSimulation.FoundryResult two = r.NextFloor;
                    csv.AppendLine(string.Join(",", new[]
                    {
                        Quote(c.Config.Words), c.Pilot, c.Cohort.Name, c.Hero.Id, I(r.Seed), r.Outcome.ToString(), F(r.FloorSeconds),
                        Quote(r.DeathPhase ?? string.Empty), I(r.DeathRoom), Quote(r.Killer ?? string.Empty), F(r.GateReadyClock),
                        I(r.GateReadyLevel), F(r.GateReadyHealth), F(r.GateWaitSeconds), I(r.WaitLevels), I(r.WaitExperience), F(r.WaitTaken),
                        r.GateCalled ? "1" : "0", F(r.GateCallClock), F(r.GateCallSkipped), I(r.GateCallExperience), I(r.LevelAtGate),
                        F(r.HeroHealthAtGate), F(r.HeroMaximumAtGate), F(r.DifficultyAtGate), F(r.WardenHealth), F(r.BossSeconds),
                        r.DiedAtWarden ? "1" : "0",
                        two != null ? two.Outcome.ToString() : "", two != null ? F(two.FloorSeconds) : "", two != null ? I(two.LevelAtGate) : "",
                        two != null ? F(two.WardenSeconds) : "", two != null ? (two.DiedAtWarden ? "1" : "0") : ""
                    }));
                }
            return csv.ToString();
        }

        // ---------------------------------------------------------------------------------------------------------

        private static Cell[] Of(List<Cell> cells, string words, string pilot, AutopilotCohort cohort) =>
            cells.Where(c => c.Config.Words == words && c.Pilot == pilot && c.Cohort == cohort).ToArray();

        // "Vanguard / Ilya" of a configuration, pilot and cohort; "-" where it was not run.
        private static string Pair(List<Cell> cells, Config config, string pilot, AutopilotCohort cohort, Func<Cell, string> read)
        {
            Cell[] pair = Of(cells, config.Words, pilot, cohort);
            return pair.Length == 0 ? "-" : string.Join(" / ", pair.Select(read));
        }

        private static string Signed(double value) =>
            double.IsNaN(value) ? "-" : value.ToString("+0;-0;0", CultureInfo.InvariantCulture);

        private static string P(IEnumerable<float> values, double percentile, string format = "0")
        {
            double value = FoundryBatch.Percentile(values, percentile);
            return double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Mean(IEnumerable<float> values, string format = "0")
        {
            float[] list = values.ToArray();
            return list.Length == 0 ? "-" : list.Average().ToString(format, CultureInfo.InvariantCulture);
        }

        private static string N(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static float Float(string word) => float.Parse(word, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static string Quote(string field) =>
            field.IndexOfAny(new[] { ',', '"' }) < 0 ? field : "\"" + field.Replace("\"", "\"\"") + "\"";

        private static string[] List(string name, string fallback, char separator)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                value = fallback;
            return value.Split(separator).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
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
