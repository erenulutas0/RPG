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
    // docs/57 phase 2: the skills measured in the foundry simulation (DescentSimulation.HeroSkills.cs). Every playing cohort
    // and both heroes go down the whole Descent (floors 1-3 and the finale) on the same seeds with human hands, once as
    // shipped (skill 1 alone) and once for each option: skill 2 beside skill 1 (`s2`), skill 3 beside it (`s3`), all three
    // (`s23`), the tuned candidates (`c*`), and the A/A probe from floor 1 (`aa-f1`: the shipped game, one unread draw of the
    // pilot's dice on entering floor 1 - a change that is on from the first frame diverges from there). Writes
    // hero-skills-rule.md (section 6.2's rows against `shipped`) and hero-skills-rule.csv, a row a run, which
    // Tools/BalanceRule/paired_bootstrap.py reads as it reads gloam-enemies.csv (docs/56's rule).
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillsDescentResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), FOUNDRY_REALISM (typical; slow; frame-perfect for none), SKILLS_OPTIONS
    // (comma separated option keys; shipped,s2,s3,s23), SKILLS_COHORTS (comma separated cohort names; careful, standing,
    // first-card, clumsy - the auto-farmer presses no skill, so its runs are the shipped ones).
    [Explicit("docs/57 phase 2, the skills in the Descent: run it by name.")]
    public sealed class HeroSkillsDescentResearch
    {
        private const float BandFrom = 45f;
        private const float BandTo = 65f;

        private sealed class Option
        {
            public string Key;
            public string What;
            public Action<DescentSimulation.FoundryLevers> Set = _ => { };
        }

        // A tuning: the skill of this kind's numbers as the candidate has them.
        private static Action<DescentSimulation.HeroAbility> Tune(params (Cryptforge.Combat.AbilityKind Kind, Action<DescentSimulation.HeroAbility> Set)[] tunes) =>
            skill =>
            {
                foreach ((Cryptforge.Combat.AbilityKind kind, Action<DescentSimulation.HeroAbility> set) in tunes)
                    if (skill.Kind == kind)
                        set(skill);
            };

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "the shipped kit: skill 1 alone (FoundryLevers.Skills 1)" };
            yield return new Option { Key = "aa-f1", What = "A/A: the shipped game, one unread draw of the pilot's dice on entering floor 1", Set = l => l.NudgeFloor = 1 };
            yield return new Option { Key = "s2", What = "skill 2 beside skill 1 at section 3's numbers: Iron Resolve (+100 armour 4 s, 20 s) / Flame Sigil (2.5 glyph 5 s, slow 40 %, burn x2, 20 s)", Set = l => l.Skills = 2 };
            yield return new Option { Key = "s3", What = "skill 3 beside skill 1 at section 3's numbers: Shield Rush (4 units in 0.3 s, 15 scaled within 0.8, daze 0.5 s, 12 s) / Cinder Ward (20 % of the maximum 2 s, 2 stacks a blow, 20 s)", Set = l => { l.Skills = 3; l.SkillTwoOff = true; } };
            yield return new Option { Key = "s23", What = "all three at section 3's numbers", Set = l => l.Skills = 3 };
        }

        // One run as the report reads it: the Descent's outcome, its bosses and, floor by floor, what the skills did.
        private sealed class RunRead
        {
            public string Option;
            public string Cohort;
            public string Hero;
            public int Seed;
            public string[] Outcomes = new string[4];
            public bool Completed;
            public int FloorsCleared;
            public int DiedOn;
            public string DeathPhase;
            public string Killer;
            public float Seconds;
            public float[] Boss = { -1f, -1f, -1f };
            public FloorRead[] Floors = new FloorRead[4];
        }

        private sealed class FloorRead
        {
            public float Seconds;
            public float Dealt;
            public float Ability;
            public float Burn;
            public float SigilExtra;
            public float[] Skill = new float[2];
            public int[] Uses = new int[2];
            public int AbilityUses;
            public float Taken;
            public float GuardPrevented;
            public float WardAbsorbed;
            public int GuardedBlows;
            public int WardedBlows;
            public int RushStruck;
            public int RushDazed;
            public int RushEscapes;
            public int SigilSlowed;
            public int SigilTicks;
            public float HeroMaximum;
        }

        [Test]
        public void TheSkillsInTheDescent()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = List("SKILLS_OPTIONS", "shipped,s2,s3,s23");
            Option[] options = keys.Select(k => Options().FirstOrDefault(o => o.Key == k)).Where(o => o != null).ToArray();
            Assert.That(options.Length, Is.EqualTo(keys.Length), "every option named exists");
            string[] cohorts = List("SKILLS_COHORTS", "careful,standing,first-card,clumsy");
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var reads = new List<RunRead>();
            foreach (Option option in options)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts.Where(c => cohorts.Contains(c.Name)))
                foreach (DescentSimulation.FoundryHero hero in heroes)
                {
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                        .Copy($"cohort {cohort.Name}, {option.Key}, {hands}");
                    option.Set(levers);
                    var cell = new RunRead[seeds.Length];
                    Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) }, i =>
                        cell[i] = Read(option.Key, cohort.Name, hero.Name, seeds[i],
                            DescentSimulation.RunFoundry(hero, seeds[i], levers, DescentSimulation.GloamChain(levers))));
                    reads.AddRange(cell);
                }
                TestContext.Progress.WriteLine($"{option.Key}: {watch.Elapsed.TotalSeconds:0} s");
                File.WriteAllText(Path.Combine(output, "hero-skills-rule.md"), Report(reads, options, first, count, hands));
                File.WriteAllText(Path.Combine(output, "hero-skills-rule.csv"), Csv(reads, hands));
            }
            TestContext.WriteLine(Report(reads, options, first, count, hands));
            Assert.That(reads.Count, Is.EqualTo(options.Length * cohorts.Length * heroes.Length * count), "every seed ran");
        }

        // The parity table's seeds with the skills on (docs/57 section 6.2's Parity row): the Editor's six (11-66, the
        // HordePacingMeasurement files of 2026-11-11), the careful pilot frame-perfect as the Editor's autopilot plays, floor
        // 1 alone and the whole Descent, skill 1 alone beside all three. The Editor's own rows with the skills on are the
        // lead's to measure (development/skills.txt "3", HordePacingMeasurement); its rows without them are the pacing
        // files'. Writes hero-skills-parity.md.
        [Test]
        public void TheEditorSeedsWithTheSkills()
        {
            int[] seeds = { 11, 22, 33, 44, 55, 66 };
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            var md = new StringBuilder();
            md.AppendLine("# docs/57's skills on the Editor's seeds (the simulation's side of the parity table)").AppendLine();
            md.AppendLine("The careful pilot, frame-perfect as the Editor's autopilot. Floor 1 alone: cleared, floor s, the Warden's fight s, the level at the end, uptime, " +
                          "and the presses of skills 1 / 2 / 3. The Descent (floors 1-3 and the finale): the outcome, its seconds, the gate bosses' fights.").AppendLine();
            md.AppendLine("| Hero | Skills | Seed | Floor 1: result, floor s, Warden s, level, uptime, presses | Descent: result, s, bosses s |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int skills in new[] { 1, 3 })
            foreach (int seed in seeds)
            {
                DescentSimulation.FoundryLevers one = FoundryBatch.Shipped().Copy($"editor seeds, skills {skills}");
                one.Skills = skills;
                DescentSimulation.FoundryResult floor = DescentSimulation.RunFoundry(hero, seed, one);
                DescentSimulation.FoundryLevers four = one.Copy($"editor seeds, descent, skills {skills}");
                four.Floors = DescentSimulation.FinaleFloor;
                RunRead descent = Read("parity", "careful", hero.Name, seed,
                    DescentSimulation.RunFoundry(hero, seed, four, DescentSimulation.GloamChain(four)));
                md.AppendLine($"| {hero.Name} | {skills} | {seed} | {(floor.Cleared ? "cleared" : $"died {floor.DeathRoom} {floor.DeathPhase}")}, " +
                              $"{F(floor.FloorSeconds)}, {F(floor.WardenSeconds)}, L{floor.Level}, {floor.Uptime.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                              $"{floor.AbilityUses} / {floor.SkillUses[0]} / {floor.SkillUses[1]} | " +
                              $"{(descent.Completed ? "walked out" : $"died on {descent.DiedOn}")}, {F(descent.Seconds)}, " +
                              $"{string.Join(" / ", descent.Boss.Select(b => b < 0f ? "-" : F(b)))} |");
            }
            File.WriteAllText(Path.Combine(output, "hero-skills-parity.md"), md.ToString());
            TestContext.WriteLine(md.ToString());
        }

        private static RunRead Read(string option, string cohort, string hero, int seed, DescentSimulation.FoundryResult run)
        {
            var read = new RunRead { Option = option, Cohort = cohort, Hero = hero, Seed = seed };
            int index = 0;
            for (DescentSimulation.FoundryResult floor = run; floor != null && index < 4; floor = floor.NextFloor, index++)
            {
                bool finale = FoundryBatch.IsFinale(floor);
                read.Outcomes[index] = finale && floor.WalkedOut ? "WalkedOut" : floor.Outcome.ToString();
                read.Seconds += floor.FloorSeconds;
                if (floor.Cleared)
                    read.FloorsCleared++;
                if (finale && floor.WalkedOut)
                    read.Completed = true;
                if (floor.Outcome == DescentSimulation.FoundryOutcome.Died)
                {
                    read.DiedOn = index + 1;
                    read.DeathPhase = floor.DeathPhase;
                    read.Killer = floor.Killer;
                }
                if (!finale && index < 3)
                    read.Boss[index] = floor.WardenSeconds;
                read.Floors[index] = new FloorRead
                {
                    Seconds = floor.FloorSeconds,
                    Dealt = floor.DealtByWeapons + floor.DealtByBurn + floor.DealtByAbility + floor.DealtByBleed + floor.DealtBySkills,
                    Ability = floor.DealtByAbility, Burn = floor.DealtByBurn, SigilExtra = floor.SigilBurnExtra,
                    Skill = (float[])floor.SkillDealt.Clone(), Uses = (int[])floor.SkillUses.Clone(), AbilityUses = floor.AbilityUses,
                    Taken = floor.TakenFromWave + floor.TakenFromHorde + floor.TakenFromWarden + floor.TakenFromSlams +
                            floor.TakenFromPit + floor.TakenFromHazards,
                    GuardPrevented = floor.GuardPrevented, WardAbsorbed = floor.WardAbsorbed, GuardedBlows = floor.GuardedBlows,
                    WardedBlows = floor.WardedBlows, RushStruck = floor.RushStruck, RushDazed = floor.RushDazed,
                    RushEscapes = floor.RushEscapes, SigilSlowed = floor.SigilSlowed, SigilTicks = floor.SigilTicks,
                    HeroMaximum = floor.HeroMaximum
                };
            }
            return read;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // The report: section 6.2's rows against `shipped` on the same seeds
        // ---------------------------------------------------------------------------------------------------------------

        private static string Report(List<RunRead> reads, Option[] options, int first, int count, string hands)
        {
            var md = new StringBuilder();
            md.AppendLine($"# docs/57's skills in the Descent: seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("Each run plays the whole Descent (floors 1-3 and the finale) with the option's skills from its first frame. Completed: walked out " +
                          "of the finale. Change: against `shipped` on the same seeds, in completions and in points of the rate (both heroes). The " +
                          "confirmation's CI is Tools/BalanceRule/paired_bootstrap.py's over hero-skills-rule.csv.").AppendLine();
            string[] cohorts = reads.Select(r => r.Cohort).Distinct().ToArray();
            string[] heroes = reads.Select(r => r.Hero).Distinct().ToArray();
            Dictionary<(string, string, string, int), RunRead> shipped = reads.Where(r => r.Option == "shipped")
                .ToDictionary(r => (r.Option, r.Cohort, r.Hero, r.Seed));

            md.AppendLine("## 1. Completion (the relative ship rule's read) and careful floor-3 clears").AppendLine();
            md.AppendLine("| Option | Cohort | Vanguard completed | Ilya completed | Both | Change (completions) | Change (points) | Floor-3 clears V / I |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (Option option in options)
            foreach (string cohort in cohorts)
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Cohort == cohort).ToArray();
                if (mine.Length == 0)
                    continue;
                int Done(string hero) => mine.Count(r => r.Hero == hero && r.Completed);
                int both = mine.Count(r => r.Completed);
                string change = "-", points = "-";
                if (option.Key != "shipped")
                {
                    int paired = 0, delta = 0;
                    foreach (RunRead r in mine)
                        if (shipped.TryGetValue(("shipped", r.Cohort, r.Hero, r.Seed), out RunRead s))
                        {
                            paired++;
                            delta += (r.Completed ? 1 : 0) - (s.Completed ? 1 : 0);
                        }
                    if (paired > 0)
                    {
                        change = delta.ToString("+0;-0;0", CultureInfo.InvariantCulture);
                        points = (100.0 * delta / paired).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
                    }
                }
                int Three(string hero) => mine.Count(r => r.Hero == hero && r.FloorsCleared >= 3);
                md.AppendLine($"| `{option.Key}` | {cohort} | {Done(heroes[0])} | {Done(heroes[1])} | {both} ({Pct(both, mine.Length)}) | {change} | {points} | {Three(heroes[0])} / {Three(heroes[1])} |");
            }
            md.AppendLine();

            md.AppendLine("## 2. The bosses' bands and the Descent's length").AppendLine();
            md.AppendLine("The gate boss's fight from walking out to falling, P50 s (share in 45-65 s; felled within 5 s): floor 1's Forge Warden, floor 2's Glass Regent, floor 3's Warden. " +
                          "Descent: completed runs' active minutes, P10 / P50 / P90, and the P50's change against `shipped`.").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Floor 1 Warden | Floor 2 Regent | Floor 3 Warden | Descent P10 / P50 / P90 min | P50 change |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (Option option in options)
            foreach (string cohort in cohorts)
            foreach (string hero in heroes)
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Cohort == cohort && r.Hero == hero).ToArray();
                if (mine.Length == 0)
                    continue;
                string Boss(int floor)
                {
                    float[] fights = mine.Select(r => r.Boss[floor]).Where(s => s >= 0f).ToArray();
                    if (fights.Length == 0)
                        return "-";
                    return $"{F(FoundryBatch.Percentile(fights, 50))} ({Pct(fights.Count(s => s >= BandFrom && s <= BandTo), fights.Length)}; {fights.Count(s => s < 5f)})";
                }
                float[] minutes = mine.Where(r => r.Completed).Select(r => r.Seconds / 60f).ToArray();
                float[] own = reads.Where(r => r.Option == "shipped" && r.Cohort == cohort && r.Hero == hero && r.Completed).Select(r => r.Seconds / 60f).ToArray();
                double p50 = FoundryBatch.Percentile(minutes, 50);
                string shift = option.Key == "shipped" || own.Length == 0 ? "-" : (p50 - FoundryBatch.Percentile(own, 50)).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
                md.AppendLine($"| `{option.Key}` | {cohort} | {hero} | {Boss(0)} | {Boss(1)} | {Boss(2)} | " +
                              $"{M(FoundryBatch.Percentile(minutes, 10))} / {M(p50)} / {M(FoundryBatch.Percentile(minutes, 90))} | {shift} |");
            }
            md.AppendLine();

            md.AppendLine("## 3. What each skill did, floor by floor (floors 1 / 2 / 3; a mean over the floors played)").AppendLine();
            md.AppendLine("Uses a minute: skill 1 / skill 2 / skill 3. Skill 3's share: the Rush's damage over the hero's (skill 1's beside it). " +
                          "Guard: what Iron Resolve kept off, over the damage taken; ward: what Cinder Ward absorbed, over it. Glyph: the extra its heat put on burns, over the hero's damage; enemies slowed a tick. " +
                          "Rush: enemies struck a press (of which dazed); escapes of its presses.").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Uses a minute (1 / 2 / 3) | Skill 1 share | Skill 3 share | Guard kept off | Ward absorbed | Glyph's heat share | Slowed a tick | Rush struck (dazed) a press | Rush escapes |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Option option in options.Where(o => o.Key != "shipped" && o.Key != "aa-f1"))
            foreach (string cohort in cohorts)
            foreach (string hero in heroes)
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Cohort == cohort && r.Hero == hero).ToArray();
                if (mine.Length == 0)
                    continue;
                string ByFloor(Func<FloorRead[], double> read) => string.Join(" / ", Enumerable.Range(0, 3).Select(f =>
                {
                    FloorRead[] floors = mine.Select(r => r.Floors[f]).Where(x => x != null).ToArray();
                    return floors.Length == 0 ? "-" : Fmt(read(floors));
                }));
                double Sum(FloorRead[] f, Func<FloorRead, double> g) => f.Sum(g);
                string uses = ByFloor(f => Sum(f, x => x.AbilityUses) * 60.0 / Math.Max(1, Sum(f, x => x.Seconds)));
                string Uses(int slot) => ByFloor(f => Sum(f, x => x.Uses[slot]) * 60.0 / Math.Max(1, Sum(f, x => x.Seconds)));
                string Share(Func<FloorRead, double> part, Func<FloorRead, double> whole) =>
                    ByFloorPct(mine, part, whole);
                md.AppendLine($"| `{option.Key}` | {cohort} | {hero} | {uses}; {Uses(0)}; {Uses(1)} | " +
                              $"{Share(x => x.Ability, x => x.Dealt)} | {Share(x => x.Skill[1], x => x.Dealt)} | " +
                              $"{Share(x => x.GuardPrevented, x => x.Taken + x.GuardPrevented)} | {Share(x => x.WardAbsorbed, x => x.Taken + x.WardAbsorbed)} | " +
                              $"{Share(x => x.SigilExtra, x => x.Dealt)} | {ByFloor(f => Sum(f, x => x.SigilSlowed) / Math.Max(1, Sum(f, x => x.SigilTicks)))} | " +
                              (hero != "Vanguard" ? "- | - |" :
                                  $"{ByFloor(f => Sum(f, x => x.RushStruck) / Math.Max(1, Sum(f, x => x.Uses[1])))} ({ByFloor(f => Sum(f, x => x.RushDazed) / Math.Max(1, Sum(f, x => x.Uses[1])))}) | " +
                                  $"{mine.Sum(r => r.Floors.Where(x => x != null).Sum(x => x.RushEscapes))} of {mine.Sum(r => r.Floors.Where(x => x != null).Sum(x => x.Uses[1]))} |"));
            }
            md.AppendLine();

            md.AppendLine("## 4. Deaths by floor and phase").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Died on floor 1 / 2 / 3 / finale | At the boss / in a hall's waves / elsewhere |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (Option option in options)
            foreach (string cohort in cohorts)
            foreach (string hero in heroes)
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Cohort == cohort && r.Hero == hero).ToArray();
                if (mine.Length == 0)
                    continue;
                bool InWaves(RunRead r) => r.DeathPhase != null && (r.DeathPhase.StartsWith("wave", StringComparison.Ordinal) || r.DeathPhase == "between waves");
                md.AppendLine($"| `{option.Key}` | {cohort} | {hero} | {string.Join(" / ", Enumerable.Range(1, 4).Select(f => mine.Count(r => r.DiedOn == f)))} | " +
                              $"{mine.Count(r => r.DeathPhase == "boss")} / {mine.Count(InWaves)} / {mine.Count(r => r.DiedOn > 0 && r.DeathPhase != "boss" && !InWaves(r))} |");
            }
            md.AppendLine();
            md.AppendLine("## The options").AppendLine();
            foreach (Option option in options)
                md.AppendLine($"- `{option.Key}`: {option.What}.");
            return md.ToString();
        }

        private static string ByFloorPct(RunRead[] mine, Func<FloorRead, double> part, Func<FloorRead, double> whole) =>
            string.Join(" / ", Enumerable.Range(0, 3).Select(f =>
            {
                FloorRead[] floors = mine.Select(r => r.Floors[f]).Where(x => x != null).ToArray();
                double w = floors.Sum(whole);
                return floors.Length == 0 || w <= 0 ? "-" : (100.0 * floors.Sum(part) / w).ToString("0.0", CultureInfo.InvariantCulture) + " %";
            }));

        private static string Csv(List<RunRead> reads, string hands)
        {
            var csv = new StringBuilder();
            csv.AppendLine("realism,option,cohort,hero,seed,f1_outcome,f2_outcome,f3_outcome,finale_outcome,floors_cleared,died_on,death_phase,killer," +
                           "descent_s,boss1_s,boss2_s,boss3_s,uses2,uses3,ability_uses,dealt,skill3_dealt,guard_prevented,ward_absorbed,sigil_extra,taken");
            foreach (RunRead r in reads)
            {
                FloorRead[] f = r.Floors.Where(x => x != null).ToArray();
                csv.AppendLine(string.Join(",", hands, r.Option, r.Cohort, r.Hero, r.Seed.ToString(CultureInfo.InvariantCulture),
                    r.Outcomes[0] ?? string.Empty, r.Outcomes[1] ?? string.Empty, r.Outcomes[2] ?? string.Empty, r.Outcomes[3] ?? string.Empty,
                    r.FloorsCleared.ToString(CultureInfo.InvariantCulture), r.DiedOn.ToString(CultureInfo.InvariantCulture),
                    Quote(r.DeathPhase), Quote(r.Killer), r.Completed ? Num(r.Seconds) : string.Empty,
                    Num(r.Boss[0]), Num(r.Boss[1]), Num(r.Boss[2]),
                    f.Sum(x => x.Uses[0]).ToString(CultureInfo.InvariantCulture), f.Sum(x => x.Uses[1]).ToString(CultureInfo.InvariantCulture),
                    f.Sum(x => x.AbilityUses).ToString(CultureInfo.InvariantCulture), Num(f.Sum(x => x.Dealt)), Num(f.Sum(x => x.Skill[1])),
                    Num(f.Sum(x => x.GuardPrevented)), Num(f.Sum(x => x.WardAbsorbed)), Num(f.Sum(x => x.SigilExtra)), Num(f.Sum(x => x.Taken))));
            }
            return csv.ToString();
        }

        private static string Fmt(double value) => double.IsNaN(value) ? "-" : value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string Num(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string F(double value) => double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

        private static string M(double minutes) => double.IsNaN(minutes) ? "-" : minutes.ToString("0.0", CultureInfo.InvariantCulture);

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
