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
    // docs/64's Rootmother Myrr measured (stage 3, 2026-11-23): floor 3's own gate boss in the Forge Warden's place, against
    // the Warden on the same seeds. Plays the four-floor Descent as GloamEnemiesResearch and RegentRoundResearch play it
    // (DescentSimulation.GloamChain), both heroes, the cohorts asked, typical hands, and writes floor 3's gate fight as the
    // hero stood in it: its length, her beats and snares and the hero's planted seconds, her feet, her saplings and
    // Thornlings, what each part took, the hero's health at the gate and leaving floor 3, and where each run ended.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~MyrrRoundResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (4), FOUNDRY_OUT (the
    // repository's TestResults), FOUNDRY_REALISM (typical), MYRR_OPTIONS (comma separated option keys or "mr:" lever strings;
    // shipped,myrr), MYRR_COHORTS (careful, standing, first-card, clumsy). Writes myrr-round.md and a row a run in
    // myrr-round.csv (the leading columns paired_bootstrap.py reads), rewritten after each option.
    [Explicit("Rootmother Myrr's round: run it by name.")]
    public sealed class MyrrRoundResearch
    {
        private sealed class Option
        {
            public string Key;
            public string What;
            public Action<DescentSimulation.FoundryLevers> Set = _ => { };
        }

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "the shipped game: the Forge Warden at floor 3's gate (share 5.102041)" };
            yield return new Option { Key = "myrr", What = "Rootmother Myrr at floor 3's gate at docs/64 section 3's starting values (FoundryLevers.GloamMyrr)", Set = l => l.GloamMyrr = true };
            yield return new Option { Key = "aa-f3", What = "A/A: the shipped game, one unread draw of the pilot's dice on entering floor 3", Set = l => l.NudgeFloor = 3 };
            // Her parts one at a time (exploration seeds only).
            yield return new Option { Key = "myrr-nosaplings", What = "Myrr without her saplings (MyrrSaplings 0)", Set = l => { l.GloamMyrr = true; l.MyrrSaplings = 0; } };
            yield return new Option { Key = "myrr-still", What = "Myrr whose snare stays where it went down (MyrrCreepSpeed 0)", Set = l => { l.GloamMyrr = true; l.MyrrCreepSpeed = 0f; } };
            yield return new Option { Key = "myrr-nofeet", What = "Myrr holding her hook for a hero at her feet too (MyrrFeetReach 0)", Set = l => { l.GloamMyrr = true; l.MyrrFeetReach = 0f; } };
            yield return new Option { Key = "myrr-noplant", What = "Myrr whose roots plant nobody (MyrrPlantSeconds 0, the hook held through the recovery)", Set = l => { l.GloamMyrr = true; l.MyrrPlantSeconds = 0f; l.MyrrPlantedFreesHook = false; } };
        }

        // A variant named by its levers, for exploration without a build: "mr:" then colon-separated name=value pairs off
        // docs/64's starting Myrr - share (MyrrHealthShare), hook (MyrrHookDamage), speed (MyrrSpeed), every, every2, t
        // (MyrrTelegraph), r (MyrrSnareRadius), creep, s (MyrrSnareShare), plant, free (MyrrPlantedFreesHook 0/1), feet,
        // sap (MyrrSaplings), sapevery, grow, sapshare, sapdist, sapmost, thmost - e.g. "mr:share=4.6:sap=0".
        private static Option Variant(string key)
        {
            var sets = new List<Action<DescentSimulation.FoundryLevers>> { l => l.GloamMyrr = true };
            foreach (string part in key.Substring("mr:".Length).Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = part.Split('=');
                float value = float.Parse(pair[1], CultureInfo.InvariantCulture);
                switch (pair[0])
                {
                    case "share": sets.Add(l => l.MyrrHealthShare = value); break;
                    case "hook": sets.Add(l => l.MyrrHookDamage = value); break;
                    case "speed": sets.Add(l => l.MyrrSpeed = value); break;
                    case "every": sets.Add(l => l.MyrrEvery = value); break;
                    case "every2": sets.Add(l => l.MyrrEveryPhaseTwo = value); break;
                    case "t": sets.Add(l => l.MyrrTelegraph = value); break;
                    case "r": sets.Add(l => l.MyrrSnareRadius = value); break;
                    case "creep": sets.Add(l => l.MyrrCreepSpeed = value); break;
                    case "s": sets.Add(l => l.MyrrSnareShare = value); break;
                    case "plant": sets.Add(l => l.MyrrPlantSeconds = value); break;
                    case "free": sets.Add(l => l.MyrrPlantedFreesHook = value != 0f); break;
                    case "feet": sets.Add(l => l.MyrrFeetReach = value); break;
                    case "sap": sets.Add(l => l.MyrrSaplings = (int)value); break;
                    case "sapevery": sets.Add(l => l.MyrrSaplingEvery = value); break;
                    case "grow": sets.Add(l => l.MyrrSaplingGrow = value); break;
                    case "sapshare": sets.Add(l => l.MyrrSaplingShare = value); break;
                    case "sapdist": sets.Add(l => l.MyrrSaplingDistance = value); break;
                    case "sapmost": sets.Add(l => l.MyrrSaplingsMost = (int)value); break;
                    case "thmost": sets.Add(l => l.MyrrThornlingsMost = (int)value); break;
                    default: throw new ArgumentException("Unknown lever: " + part);
                }
            }
            return new Option
            {
                Key = key, What = "Myrr with " + key.Substring("mr:".Length).Replace(":", ", "),
                Set = l => { foreach (Action<DescentSimulation.FoundryLevers> set in sets) set(l); }
            };
        }

        private static Option Named(string key) =>
            key.StartsWith("mr:", StringComparison.Ordinal) ? Variant(key) : Options().FirstOrDefault(o => o.Key == key);

        private sealed class Cell
        {
            public Option Option;
            public string Cohort;
            public string Hero;
            public DescentSimulation.FoundryResult[] Runs;
        }

        [Test]
        public void MyrrAgainstTheWardenOnFloorThree()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", 4);
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = List("MYRR_OPTIONS", "shipped,myrr");
            Option[] options = keys.Select(Named).Where(o => o != null).ToArray();
            Assert.That(options.Length, Is.EqualTo(keys.Length), "every option named exists");
            string[] cohorts = List("MYRR_COHORTS", "careful,standing,first-card,clumsy");
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
                File.WriteAllText(Path.Combine(output, "myrr-round.md"), Report(cells, first, count, hands));
                File.WriteAllText(Path.Combine(output, "myrr-round.csv"), Csv(cells, hands));
            }
            TestContext.WriteLine(Report(cells, first, count, hands));
            Assert.That(cells.All(c => c.Runs.Length == count), "every seed ran");
        }

        private static DescentSimulation.FoundryResult Three(DescentSimulation.FoundryResult run)
        {
            DescentSimulation.FoundryResult three = run.NextFloor?.NextFloor;
            return three != null && !FoundryBatch.IsFinale(three) ? three : null;
        }

        private static bool Completed(DescentSimulation.FoundryResult run) => FoundryBatch.FinaleOf(run) is DescentSimulation.FoundryResult f && f.WalkedOut;

        // Where the run ended: f1, f2, f3 (its gate boss or elsewhere), finale, or completed.
        private static string End(DescentSimulation.FoundryResult run)
        {
            if (Completed(run))
                return "completed";
            if (!run.Cleared || run.NextFloor == null)
                return "f1";
            DescentSimulation.FoundryResult two = run.NextFloor;
            if (!two.Cleared)
                return "f2";
            DescentSimulation.FoundryResult three = Three(run);
            if (three == null)
                return "f2";
            if (!three.Cleared)
                return three.DiedAtWarden ? "f3-boss" : "f3-other";
            return "finale";
        }

        private static float Share(float part, float whole) => whole > 0f ? part / whole : float.NaN;

        private static string Report(List<Cell> cells, int first, int count, string hands)
        {
            var md = new StringBuilder();
            md.AppendLine($"# Rootmother Myrr against the Forge Warden at floor 3's gate: seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("Completed: walked out of the finale, of the runs a cohort and hero (both heroes pooled in the first columns). Ends: where the other runs " +
                          "ended (f3-boss: died at floor 3's gate boss). Gate fight, over the floor 3s whose gate boss walked out: its length P10 / P50 / P90 s for each hero " +
                          "(the fights it fell in); its beats, the beats whose mark caught the hero, the seconds planted, the beats at her feet; her saplings planted, cut " +
                          "and grown; what the mark, the boss's own weapon, her grown Thornlings, the horde and the escort took a fight, as a share of the hero's maximum; " +
                          "the hero's health at the gate and leaving floor 3 (cleared ones).").AppendLine();
            md.AppendLine("| Option | Cohort | Completed | V / I | Ends (f3-boss / f3-other / finale) | Fight P10/P50/P90 s, V | I | In 45-65 s, V / I | Beats | Caught | Planted s | Feet beats | Saplings planted / cut / grown | Mark taken | Weapon taken | Grafted taken | Horde taken | Escort taken | Health at gate | Leaving F3 |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var group in cells.GroupBy(c => (c.Option, c.Cohort)))
            {
                DescentSimulation.FoundryResult[] runs = group.SelectMany(c => c.Runs).ToArray();
                DescentSimulation.FoundryResult[] fights = runs.Select(Three).Where(t => t != null && (t.WardenSeconds >= 0f || t.DiedAtWarden)).ToArray();
                DescentSimulation.FoundryResult[] cleared = runs.Select(Three).Where(t => t != null && t.Cleared).ToArray();
                string Mean(IEnumerable<float> values)
                {
                    float[] list = values.Where(v => !float.IsNaN(v)).ToArray();
                    return list.Length == 0 ? "-" : list.Average().ToString("0.000", CultureInfo.InvariantCulture);
                }
                string Fight(string hero)
                {
                    float[] seconds = group.Where(c => c.Hero == hero).SelectMany(c => c.Runs).Select(Three)
                        .Where(t => t != null && t.WardenSeconds >= 0f).Select(t => t.WardenSeconds).ToArray();
                    return seconds.Length == 0 ? "-" :
                        $"{F1(FoundryBatch.Percentile(seconds, 10))} / {F1(FoundryBatch.Percentile(seconds, 50))} / {F1(FoundryBatch.Percentile(seconds, 90))}";
                }
                string Band(string hero)
                {
                    float[] seconds = group.Where(c => c.Hero == hero).SelectMany(c => c.Runs).Select(Three)
                        .Where(t => t != null && t.WardenSeconds >= 0f).Select(t => t.WardenSeconds).ToArray();
                    return seconds.Length == 0 ? "-" : (100.0 * seconds.Count(s => s >= 45f && s <= 65f) / seconds.Length).ToString("0", CultureInfo.InvariantCulture) + " %";
                }
                int Done(string hero) => group.Where(c => c.Hero == hero).SelectMany(c => c.Runs).Count(Completed);
                var ends = runs.GroupBy(End).ToDictionary(g => g.Key, g => g.Count());
                int E(string k) => ends.TryGetValue(k, out int n) ? n : 0;
                int done = E("completed");
                md.AppendLine($"| `{group.Key.Option.Key}` | {group.Key.Cohort} | {done} ({(100.0 * done / runs.Length).ToString("0.0", CultureInfo.InvariantCulture)} %) | " +
                              $"{Done("Vanguard")} / {Done("Ilya")} | {E("f3-boss")} / {E("f3-other")} / {E("finale")} | {Fight("Vanguard")} | {Fight("Ilya")} | {Band("Vanguard")} / {Band("Ilya")} | " +
                              $"{Mean(fights.Select(t => (float)t.Slams))} | {Mean(fights.Select(t => (float)t.SlamHits))} | {Mean(fights.Select(t => t.MyrrPlantedSeconds))} | " +
                              $"{Mean(fights.Select(t => (float)t.MyrrFeetBeats))} | {Mean(fights.Select(t => (float)t.MyrrSaplingsPlanted))} / {Mean(fights.Select(t => (float)t.MyrrSaplingsCut))} / " +
                              $"{Mean(fights.Select(t => (float)t.MyrrThornlingsGrown))} | {Mean(fights.Select(t => Share(t.TakenFromSlams, t.HeroMaximumAtGate)))} | " +
                              $"{Mean(fights.Select(t => Share(t.TakenFromWarden, t.HeroMaximumAtGate)))} | {Mean(fights.Select(t => Share(t.TakenFromGrafted, t.HeroMaximumAtGate)))} | " +
                              $"{Mean(fights.Select(t => Share(t.FightTakenFromHorde, t.HeroMaximumAtGate)))} | {Mean(fights.Select(t => Share(t.FightTakenFromEscort, t.HeroMaximumAtGate)))} | " +
                              $"{Mean(fights.Select(t => Share(t.HeroHealthAtGate, t.HeroMaximumAtGate)))} | {Mean(cleared.Select(t => Share(t.HeroHealth, t.HeroMaximum)))} |");
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
            csv.AppendLine("realism,option,cohort,hero,seed,end,finale_outcome,f3_cleared,f3_died_boss,gate_s,beats,caught,planted_s,feet_beats,planted_beats,crept," +
                           "saplings,cut,grown,felled,taken_mark,taken_boss_weapon,taken_grafted,fight_horde,fight_escort,max_at_gate,hp_at_gate,hp_f3_end,max_f3_end,f3_killer,descent_s");
            foreach (Cell cell in cells)
            foreach (DescentSimulation.FoundryResult run in cell.Runs)
            {
                DescentSimulation.FoundryResult t = Three(run);
                DescentSimulation.FoundryResult f = FoundryBatch.FinaleOf(run);
                float descent = 0f;
                for (DescentSimulation.FoundryResult r = run; r != null; r = r.NextFloor)
                    descent += r.FloorSeconds;
                string head = string.Join(",", hands, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, End(run),
                    f != null ? (f.WalkedOut ? "WalkedOut" : f.Outcome.ToString()) : string.Empty);
                if (t == null)
                {
                    csv.AppendLine(head + new string(',', 24));
                    continue;
                }
                csv.AppendLine(string.Join(",", head, t.Cleared ? 1 : 0, t.DiedAtWarden ? 1 : 0, Num(t.WardenSeconds), t.Slams, t.SlamHits,
                    Num(t.MyrrPlantedSeconds), t.MyrrFeetBeats, t.MyrrPlantedBeats, Num(t.MyrrCrept), t.MyrrSaplingsPlanted, t.MyrrSaplingsCut,
                    t.MyrrThornlingsGrown, t.MyrrThornlingsFelled, Num(t.TakenFromSlams), Num(t.TakenFromWarden), Num(t.TakenFromGrafted),
                    Num(t.FightTakenFromHorde), Num(t.FightTakenFromEscort), Num(t.HeroMaximumAtGate), Num(t.HeroHealthAtGate), Num(t.HeroHealth),
                    Num(t.HeroMaximum), Quote(t.Killer), Completed(run) ? Num(descent) : string.Empty));
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
