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
    // The Glass Regent's design round of 2026-11-17 (docs/50 "The Regent's bite for the standing hero"; docs/56 section 9,
    // the lead's decision 3): why the standing cohort does better against the Regent than against the Forge Warden it
    // replaced, and the round's candidates read beside them. Plays the four-floor Descent as GloamEnemiesResearch plays it
    // (DescentSimulation.GloamChain), both heroes, the cohorts asked, typical hands, and writes floor 2's gate fight as the
    // hero stood in it: the blows and what they took, the boss's own weapon, the horde and the escort in the fight, the
    // fight's length, the hero's health at the gate and leaving the floor, the seconds it stood still and within the boss's
    // reach, and where each run ended.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~RegentRoundResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (4), FOUNDRY_OUT (the
    // repository's TestResults), FOUNDRY_REALISM (typical), REGENT_OPTIONS (comma separated option keys; shipped,f2-warden),
    // REGENT_COHORTS (comma separated cohort names; careful, standing, first-card, clumsy). Writes regent-round.md and a row
    // a run in regent-round.csv, rewritten after each option.
    [Explicit("The Glass Regent's design round: run it by name.")]
    public sealed class RegentRoundResearch
    {
        private sealed class Option
        {
            public string Key;
            public string What;
            public Action<DescentSimulation.FoundryLevers> Set = _ => { };
        }

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "the shipped game: the Glass Regent at floor 2's gate (RegentRule), its rod at its feet since 2026-11-17" };
            yield return new Option { Key = "regent-before", What = "the Glass Regent before the round of 2026-11-17: its rod held through every cast (FoundryLevers.RegentFeetReach 0)", Set = l => l.RegentFeetReach = 0f };
            yield return new Option { Key = "f2-warden", What = "floor 2's Forge Warden back at its gate at its share then (VaultsWarden)", Set = l => l.VaultsWarden = true };
            yield return new Option { Key = "aa-f2", What = "A/A: the shipped game, one unread draw of the pilot's dice on entering floor 2", Set = l => l.NudgeFloor = 2 };
            // Diagnosis (exploration seeds only): the Regent with one of the Warden's parts back at a time.
            yield return new Option { Key = "regent-l20", What = "the shipped Regent, its lane 20 % of the hero's maximum (the Warden's blow)", Set = l => l.RegentLaneShare = .2f };
            yield return new Option { Key = "regent-t1.1", What = "the shipped Regent, its telegraph 1.1 s (the Warden's)", Set = l => l.RegentTelegraph = 1.1f };
            yield return new Option { Key = "regent-l20-t1.1", What = "the shipped Regent, its lane 20 % and its telegraph 1.1 s", Set = l => { l.RegentLaneShare = .2f; l.RegentTelegraph = 1.1f; } };
            yield return new Option { Key = "regent-nosidestep", What = "the shipped Regent, no pilot sidesteps: a lane's mark left as the Warden's, away from it", Set = l => l.SidestepsTheRegent = false };
            yield return new Option { Key = "regent-s6.07", What = "the shipped Regent at the Warden's floor-2 share, 6.071429", Set = l => l.RegentHealthShare = DescentSimulation.FloorTwoWardenShare };
        }

        // A variant named by its levers, for exploration without a build: "rr:" then colon-separated name=value pairs off the
        // shipped Regent - walk (RegentStoodWalk), near (RegentNearReach), lanes (RegentStoodLanes), turn (RegentStoodTurn),
        // share (RegentStoodShare), l (the lane's share), t (its telegraph), w (its width) - e.g. "rr:walk=1:lanes=2".
        private static Option Variant(string key)
        {
            var sets = new List<Action<DescentSimulation.FoundryLevers>>();
            foreach (string part in key.Substring("rr:".Length).Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = part.Split('=');
                float value = float.Parse(pair[1], CultureInfo.InvariantCulture);
                switch (pair[0])
                {
                    case "walk": sets.Add(l => l.RegentStoodWalk = value); break;
                    case "near": sets.Add(l => l.RegentNearReach = value); break;
                    case "lanes": sets.Add(l => l.RegentStoodLanes = (int)value); break;
                    case "turn": sets.Add(l => l.RegentStoodTurn = value); break;
                    case "share": sets.Add(l => l.RegentStoodShare = value); break;
                    case "rod": sets.Add(l => l.RegentRodInTelegraph = value != 0f); break;
                    case "rodnear": sets.Add(l => l.RegentRodNearReach = value); break;
                    case "rec": sets.Add(l => l.RegentRodRecovery = value); break;
                    case "nearafter": sets.Add(l => l.RegentRodNearAfterBlow = value != 0f); break;
                    case "blownear": sets.Add(l => l.RegentRodBlowReach = value); break;
                    case "stayed": sets.Add(l => l.RegentRodBlowStayed = value != 0f); break;
                    case "feet": sets.Add(l => l.RegentFeetReach = value); break;
                    case "frame": sets.Add(l => l.RegentFeetEveryFrame = value != 0f); break;
                    case "streak": sets.Add(l => l.RegentRodStreakShare = value); break;
                    case "most": sets.Add(l => l.RegentRodStreakMost = (int)value); break;
                    case "l": sets.Add(l => l.RegentLaneShare = value); break;
                    case "t": sets.Add(l => l.RegentTelegraph = value); break;
                    case "w": sets.Add(l => l.RegentLaneWidth = value); break;
                    default: throw new ArgumentException("Unknown lever: " + part);
                }
            }
            return new Option
            {
                Key = key, What = "the shipped Regent with " + key.Substring("rr:".Length).Replace(":", ", "),
                Set = l => { foreach (Action<DescentSimulation.FoundryLevers> set in sets) set(l); }
            };
        }

        private static Option Named(string key) =>
            key.StartsWith("rr:", StringComparison.Ordinal) ? Variant(key) : Options().FirstOrDefault(o => o.Key == key);

        private sealed class Cell
        {
            public Option Option;
            public string Cohort;
            public string Hero;
            public DescentSimulation.FoundryResult[] Runs;
        }

        [Test]
        public void TheRegentAgainstTheStandingHero()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", 4);
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = List("REGENT_OPTIONS", "shipped,f2-warden");
            Option[] options = keys.Select(Named).Where(o => o != null).ToArray();
            Assert.That(options.Length, Is.EqualTo(keys.Length), "every option named exists");
            string[] cohorts = List("REGENT_COHORTS", "careful,standing,first-card,clumsy");
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var cells = new List<Cell>();
            foreach (Option option in options)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts.Where(c => cohorts.Contains(c.Name)))
                foreach (DescentSimulation.FoundryHero hero in heroes)
                {
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                        .Copy($"cohort {cohort.Name}, {option.Key}, {hands}");
                    option.Set(levers);
                    var runs = new DescentSimulation.FoundryResult[seeds.Length];
                    Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) },
                        i => runs[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers, DescentSimulation.GloamChain(levers)));
                    cells.Add(new Cell { Option = option, Cohort = cohort.Name, Hero = hero.Name, Runs = runs });
                }
                TestContext.Progress.WriteLine($"{option.Key}: {watch.Elapsed.TotalSeconds:0} s");
                File.WriteAllText(Path.Combine(output, "regent-round.md"), Report(cells, first, count, hands));
                File.WriteAllText(Path.Combine(output, "regent-round.csv"), Csv(cells, hands));
            }
            TestContext.WriteLine(Report(cells, first, count, hands));
            Assert.That(cells.All(c => c.Runs.Length == count), "every seed ran");
        }

        private static DescentSimulation.FoundryResult Two(DescentSimulation.FoundryResult run) => run.NextFloor;

        private static DescentSimulation.FoundryResult Three(DescentSimulation.FoundryResult run)
        {
            DescentSimulation.FoundryResult three = run.NextFloor?.NextFloor;
            return three != null && !FoundryBatch.IsFinale(three) ? three : null;
        }

        private static bool Completed(DescentSimulation.FoundryResult run) => FoundryBatch.FinaleOf(run) is DescentSimulation.FoundryResult f && f.WalkedOut;

        // Where the run ended: f1, f2 (gate boss or elsewhere), f3, finale, or completed.
        private static string End(DescentSimulation.FoundryResult run)
        {
            if (Completed(run))
                return "completed";
            if (!run.Cleared)
                return "f1";
            DescentSimulation.FoundryResult two = Two(run);
            if (two == null)
                return "f1";
            if (!two.Cleared)
                return two.DiedAtWarden ? "f2-boss" : "f2-other";
            DescentSimulation.FoundryResult three = Three(run);
            if (three == null || !three.Cleared)
                return three != null && three.DiedAtWarden ? "f3-boss" : "f3-other";
            return "finale";
        }

        private static float Share(float part, float whole) => whole > 0f ? part / whole : float.NaN;

        private static string Report(List<Cell> cells, int first, int count, string hands)
        {
            var md = new StringBuilder();
            md.AppendLine($"# The Glass Regent against the standing hero: seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("Pooled over both heroes. Completed: walked out of the finale. Ends: where the other runs ended (f2-boss: died at floor 2's gate boss). " +
                          "Gate fight, a mean over the floor 2s whose gate boss walked out: its length P50 s; blows landed on the hero a fight; the health it took, as a share of the hero's maximum, " +
                          "from the blows, the boss's own weapon, the horde and the escort in the fight; the hero's health at the gate and leaving floor 2 (cleared ones); still: share of the fight's seconds walked under 1 a second; " +
                          "in reach: share within the boss's own weapon's reach; beat walk: the hero's walk from the last blow's spot to the next mark; mark distance: from the boss at the mark.").AppendLine();
            md.AppendLine("| Option | Cohort | Completed | Ends (f2-boss / f2-other / f3 / finale) | Gate fights | Fight P50 s | Blow hits | Blows taken | Weapon taken | Horde taken | Escort taken | Health at gate | Health leaving F2 | Still | In reach | Beat walk | Mark distance | Stood beats / hits |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var group in cells.GroupBy(c => (c.Option, c.Cohort)))
            {
                DescentSimulation.FoundryResult[] runs = group.SelectMany(c => c.Runs).ToArray();
                DescentSimulation.FoundryResult[] fights = runs.Select(Two).Where(t => t != null && t.WardenSeconds >= 0f || t != null && t.DiedAtWarden).ToArray();
                DescentSimulation.FoundryResult[] cleared = runs.Select(Two).Where(t => t != null && t.Cleared).ToArray();
                string Mean(IEnumerable<float> values)
                {
                    float[] list = values.Where(v => !float.IsNaN(v)).ToArray();
                    return list.Length == 0 ? "-" : list.Average().ToString("0.000", CultureInfo.InvariantCulture);
                }
                var ends = runs.GroupBy(End).ToDictionary(g => g.Key, g => g.Count());
                int E(string k) => ends.TryGetValue(k, out int n) ? n : 0;
                int done = E("completed");
                md.AppendLine($"| `{group.Key.Option.Key}` | {group.Key.Cohort} | {done} ({(100.0 * done / runs.Length).ToString("0.0", CultureInfo.InvariantCulture)} %) | " +
                              $"{E("f2-boss")} / {E("f2-other")} / {E("f3-boss") + E("f3-other")} / {E("finale")} | {fights.Length} | " +
                              $"{F1(FoundryBatch.Percentile(fights.Where(t => t.WardenSeconds >= 0f).Select(t => t.WardenSeconds), 50))} | " +
                              $"{Mean(fights.Select(t => (float)t.SlamHits))} | {Mean(fights.Select(t => Share(t.TakenFromSlams, t.HeroMaximumAtGate)))} | " +
                              $"{Mean(fights.Select(t => Share(t.TakenFromWarden, t.HeroMaximumAtGate)))} | {Mean(fights.Select(t => Share(t.FightTakenFromHorde, t.HeroMaximumAtGate)))} | " +
                              $"{Mean(fights.Select(t => Share(t.FightTakenFromEscort, t.HeroMaximumAtGate)))} | {Mean(fights.Select(t => Share(t.HeroHealthAtGate, t.HeroMaximumAtGate)))} | " +
                              $"{Mean(cleared.Select(t => Share(t.HeroHealth, t.HeroMaximum)))} | {Mean(fights.Select(t => Share(t.GateStillSeconds, t.GateWatchSeconds)))} | " +
                              $"{Mean(fights.Select(t => Share(t.GateInBossReachSeconds, t.GateWatchSeconds)))} | {Mean(fights.Where(t => t.GateBeatsWatched > 0).Select(t => t.GateBeatWalkSum / t.GateBeatsWatched))} | " +
                              $"{Mean(fights.Where(t => t.GateBeatsWatched > 0).Select(t => t.GateMarkDistanceSum / t.GateBeatsWatched))} | " +
                              $"{Mean(fights.Select(t => (float)t.RegentStoodBeats))} / {Mean(fights.Select(t => (float)t.RegentStoodHits))} |");
            }
            md.AppendLine();
            md.AppendLine("## The options").AppendLine();
            foreach (Option option in cells.Select(c => c.Option).Distinct())
                md.AppendLine($"- `{option.Key}`: {option.What}.");
            return md.ToString();
        }

        private static string Csv(List<Cell> cells, string hands)
        {
            var csv = new StringBuilder();
            csv.AppendLine("realism,option,cohort,hero,seed,end,finale_outcome,f2_cleared,f2_died_boss,gate_s,slams,slam_hits,taken_slams,taken_boss_weapon,fight_horde,fight_escort,fight_pit," +
                           "max_at_gate,hp_at_gate,hp_f2_end,max_f2_end,watch_s,still_s,in_reach_s,threat_s,beats_watched,beat_walk_sum,mark_dist_sum,regent_lane_hits,stood_beats,stood_hits,rod_near_beats,feet_beats,streak_strikes,mark_within,f2_killer,f3_cleared,f3_killer,descent_s");
            foreach (Cell cell in cells)
            foreach (DescentSimulation.FoundryResult run in cell.Runs)
            {
                DescentSimulation.FoundryResult t = Two(run);
                DescentSimulation.FoundryResult three = Three(run);
                DescentSimulation.FoundryResult f = FoundryBatch.FinaleOf(run);
                float descent = 0f;
                for (DescentSimulation.FoundryResult r = run; r != null; r = r.NextFloor)
                    descent += r.FloorSeconds;
                string head = string.Join(",", hands, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, End(run),
                    f != null ? (f.WalkedOut ? "WalkedOut" : f.Outcome.ToString()) : string.Empty);
                if (t == null)
                {
                    csv.AppendLine(head + new string(',', 32));
                    continue;
                }
                csv.AppendLine(string.Join(",", head, t.Cleared ? 1 : 0, t.DiedAtWarden ? 1 : 0, Num(t.WardenSeconds), t.Slams, t.SlamHits,
                    Num(t.TakenFromSlams), Num(t.TakenFromWarden), Num(t.FightTakenFromHorde), Num(t.FightTakenFromEscort), Num(t.FightTakenFromPit),
                    Num(t.HeroMaximumAtGate), Num(t.HeroHealthAtGate), Num(t.HeroHealth), Num(t.HeroMaximum), Num(t.GateWatchSeconds),
                    Num(t.GateStillSeconds), Num(t.GateInBossReachSeconds), Num(t.GateThreatSeconds), t.GateBeatsWatched, Num(t.GateBeatWalkSum), Num(t.GateMarkDistanceSum),
                    t.RegentLaneHits, t.RegentStoodBeats, t.RegentStoodHits, t.RegentRodNearBeats, t.RegentFeetBeats, t.RegentStreakStrikes, string.Join(";", t.GateMarkWithin), Quote(t.Killer), three != null ? (three.Cleared ? "1" : "0") : string.Empty, Quote(three?.Killer),
                    Completed(run) ? Num(descent) : string.Empty));
            }
            return csv.ToString();
        }

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string Num(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string F1(double value) => double.IsNaN(value) ? "-" : value.ToString("0.0", CultureInfo.InvariantCulture);

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
