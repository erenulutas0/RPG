using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/54 phase 6, the difficulty tiers (section 21): each tier played by its matched geared cohort - Tempered with the
    // +5 set, Inferno with the +9 set, Abyssal with the +9 set and good efsun (FoundryGear's sets, the cohort's own hands) -
    // against the same cohort ungeared on Forged (`shipped`), both heroes, down the whole shipped Descent (floors 1-3 and
    // the finale) on the same seeds with human hands. The tier reaches the simulated run as it reaches the scene's
    // (FoundryHero.Tier: every enemy's health and damage through FloorScaling.Tiered, its gold through FloorScaling.Gold).
    // Reads docs/56's rule (the four-floor completion change per cohort, paired by seed; the CI from
    // Tools/BalanceRule/paired_bootstrap.py, which reads difficulty-tiers.csv), the bosses' 45-65 s bands per tier and the
    // Descent's minutes against Forged's.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~DifficultyTiersResearch"
    //
    // Environment, all optional: TIER_OPTIONS (comma separated: shipped, tempered, inferno, abyssal - all by default - and
    // any candidate), TIER_CANDIDATES (research: "tempered@1.10/1.08" plays Tempered's gear and rewards at enemy health x1.10
    // and damage x1.08 through the same body, as an option of that name), TIER_COHORTS (clumsy, careful, first-card),
    // FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors), FOUNDRY_REALISM (typical),
    // FOUNDRY_OUT (the repository's TestResults). Writes difficulty-tiers.md and difficulty-tiers.csv (a row a run, rewritten
    // after each option).
    [Explicit("docs/54 section 21's difficulty tiers: run it by name.")]
    public sealed class DifficultyTiersResearch
    {
        public const float BandFrom = 45f;
        public const float BandTo = 65f;

        private sealed class Option
        {
            public string Key;
            public DifficultyTier Tier;
            // The matched set's basma level (-1: ungeared) and whether each piece carries the good efsun.
            public int Level = -1;
            public bool Good;
            public float Health = float.NaN;
            public float Damage = float.NaN;

            public string Gear => Level < 0 ? "-" : "+" + Level.ToString(CultureInfo.InvariantCulture) + (Good ? "g" : string.Empty);

            public string Multipliers => Tier == DifficultyTier.Forged && float.IsNaN(Health)
                ? "x1 / x1"
                : $"x{F(float.IsNaN(Health) ? DifficultyTiers.EnemyHealth(Tier) : Health, "0.###")} / " +
                  $"x{F(float.IsNaN(Damage) ? DifficultyTiers.EnemyDamage(Tier) : Damage, "0.###")}";
        }

        private sealed class Row
        {
            public Option Option;
            public string Cohort;
            public string Hero;
            public int Seed;
            public int Power;
            public DescentSimulation.FoundryResult Result;
            public GearEconomyResearch.Run Income;
            public bool Completed;
            public float Descent;
            public float Warden1 = -1f;
            public float Regent2 = -1f;
            public float Warden3 = -1f;
        }

        // The tiers as shipped, each with its matched set (docs/54 section 8: Tempered ~+15 %, a +5 set; Inferno ~+30 %, +9;
        // Abyssal ~+42 %, +9 with good efsun).
        private static IEnumerable<Option> Shipped()
        {
            yield return new Option { Key = "shipped", Tier = DifficultyTier.Forged };
            yield return new Option { Key = "tempered", Tier = DifficultyTier.Tempered, Level = 5 };
            yield return new Option { Key = "inferno", Tier = DifficultyTier.Inferno, Level = 9 };
            yield return new Option { Key = "abyssal", Tier = DifficultyTier.Abyssal, Level = 9, Good = true };
        }

        // A candidate "tier@health/damage": the tier's own set and rewards, at these enemy multipliers.
        private static Option Candidate(string text)
        {
            string[] parts = text.Split('@');
            string[] multipliers = parts.Length == 2 ? parts[1].Split('/') : new string[0];
            Option tier = Shipped().FirstOrDefault(o => parts.Length == 2 && o.Key == parts[0]);
            if (tier == null || tier.Tier == DifficultyTier.Forged || multipliers.Length != 2)
                throw new ArgumentException("A candidate reads tier@health/damage, e.g. tempered@1.10/1.08: " + text);
            return new Option
            {
                Key = text, Tier = tier.Tier, Level = tier.Level, Good = tier.Good,
                Health = float.Parse(multipliers[0], CultureInfo.InvariantCulture),
                Damage = float.Parse(multipliers[1], CultureInfo.InvariantCulture)
            };
        }

        [Test]
        public void TheTiersWithTheirMatchedGear()
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

            var known = Shipped().ToList();
            foreach (string candidate in List("TIER_CANDIDATES", string.Empty))
                known.Add(Candidate(candidate));
            string[] keys = List("TIER_OPTIONS", string.Join(",", known.Select(o => o.Key)));
            Option[] options = keys.Select(k => known.FirstOrDefault(o => o.Key == k)).ToArray();
            Assert.That(options, Has.None.Null, "every option named exists: " + string.Join(", ", keys));
            string[] cohortNames = List("TIER_COHORTS", "clumsy,careful,first-card");
            AutopilotCohort[] cohorts = cohortNames.Select(n => AutopilotCohort.Cohorts.FirstOrDefault(c => c.Name == n)).ToArray();
            Assert.That(cohorts, Has.None.Null, "every cohort named exists: " + string.Join(", ", cohortNames));

            Dictionary<string, int> gold = GearEconomyResearch.BaseGold();
            float[] modifiers = GearEconomyResearch.Modifiers(DescentSimulation.FoundryFloorOne());
            var unknown = new HashSet<string>();
            Func<DescentSimulation.FoundryHero>[] heroes = { DescentSimulation.Vanguard, DescentSimulation.CinderArcanist };
            var rows = new List<Row>();
            foreach (Option option in options)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (AutopilotCohort cohort in cohorts)
                foreach (Func<DescentSimulation.FoundryHero> make in heroes)
                {
                    DescentSimulation.FoundryHero hero = make();
                    if (option.Level >= 0)
                        hero.Gear = option.Good
                            ? FoundryGear.Set(hero.Weapon.Id, option.Level, FoundryGear.GoodLines)
                            : FoundryGear.Set(hero.Weapon.Id, option.Level);
                    hero.Tier = option.Tier;
                    hero.TierHealthOverride = option.Health;
                    hero.TierDamageOverride = option.Damage;
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                        .Copy($"cohort {cohort.Name}, {option.Key}, {hands}");
                    FoundryBatch.Batch batch = FoundryBatch.Run(hero, levers, seeds, parallel);
                    for (int i = 0; i < batch.Runs.Length; i++)
                        rows.Add(Read(option, cohort.Name, hero, seeds[i], batch.Runs[i], modifiers, gold, unknown));
                }
                File.WriteAllText(Path.Combine(output, "difficulty-tiers.csv"), Csv(rows, hands));
                TestContext.Progress.WriteLine($"{option.Key}: {watch.Elapsed.TotalSeconds:0} s");
            }

            string report = Report(rows, options, cohortNames, first, count, hands, unknown);
            File.WriteAllText(Path.Combine(output, "difficulty-tiers.md"), report);
            TestContext.WriteLine(report);
            Assert.That(rows.Count, Is.EqualTo(options.Length * cohorts.Length * heroes.Length * count), "every seed ran");
        }

        private static Row Read(Option option, string cohort, DescentSimulation.FoundryHero hero, int seed,
            DescentSimulation.FoundryResult result, float[] modifiers, Dictionary<string, int> gold, HashSet<string> unknown)
        {
            var row = new Row
            {
                Option = option, Cohort = cohort, Hero = hero.Name, Seed = seed, Power = hero.Gear.Power, Result = result,
                Income = GearEconomyResearch.Read(cohort, hero.Name, seed, result, modifiers, gold, unknown, option.Tier),
                Completed = FoundryBatch.FinaleOf(result) is DescentSimulation.FoundryResult finale && finale.WalkedOut
            };
            for (DescentSimulation.FoundryResult r = result; r != null; r = r.NextFloor)
                row.Descent += r.FloorSeconds;
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

        private static string Report(List<Row> rows, Option[] options, string[] cohorts, int first, int count, string hands,
            HashSet<string> unknown)
        {
            string[] heroes = rows.Select(r => r.Hero).Distinct().ToArray();
            var md = new StringBuilder();
            md.AppendLine($"# docs/54 section 21: the difficulty tiers with their matched gear, seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("The whole shipped Descent (floors 1-3 and the finale) in each cohort's hands, on the option's tier wearing its matched " +
                          "set (FoundryGear: the hero's own weapon and a Gold armour, amulet and ring at the level, +9g with good efsun), against " +
                          "the same cohort ungeared on Forged (`shipped`) on the same seeds. Completed: walked out of the finale. Change: " +
                          "the option's completions less shipped's on the same seeds, in points of rate (docs/56's read; the CI is " +
                          "Tools/BalanceRule/paired_bootstrap.py's on difficulty-tiers.csv). Bosses: P50 of the felled fights and the share " +
                          $"inside {F(BandFrom, "0")}-{F(BandTo, "0")} s - floor 1's Warden from walking out of the gate to falling (the gate's " +
                          "band), floor 2's Regent and floor 3's Warden their own fights. Descent: the active minutes of the completed runs, " +
                          "P50, and its difference from shipped's. Banked: forge gold a run, mean, with the tier's reward.").AppendLine();

            md.AppendLine("## The rule's read, both heroes pooled").AppendLine();
            md.AppendLine("| Option | Enemy health / damage | Gear | Cohort | Completed | Shipped | Change (points) | Descent P50 min (vs shipped) |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (Option option in options.Where(o => o.Key != "shipped"))
            foreach (string cohort in cohorts)
            {
                Row[] mine = Of(rows, option.Key, cohort, null);
                Row[] plain = Of(rows, "shipped", cohort, null);
                if (mine.Length == 0)
                    continue;
                int done = mine.Count(r => r.Completed);
                string versus = "-", minutes = M(Median(mine.Where(r => r.Completed).Select(r => r.Descent)));
                string shippedDone = "-";
                if (plain.Length == mine.Length)
                {
                    int base0 = plain.Count(r => r.Completed);
                    shippedDone = $"{base0} ({Pct(base0, plain.Length)})";
                    versus = F(100.0 * (done - base0) / mine.Length, "+0.00;-0.00;0.00");
                    double delta = Median(mine.Where(r => r.Completed).Select(r => r.Descent)) -
                                   Median(plain.Where(r => r.Completed).Select(r => r.Descent));
                    minutes += $" ({F(delta / 60.0, "+0.00;-0.00;0.00")})";
                }
                md.AppendLine($"| `{option.Key}` | {option.Multipliers} | {option.Gear} | {cohort} | {done} ({Pct(done, mine.Length)}) | {shippedDone} | {versus} | {minutes} |");
            }
            md.AppendLine();

            md.AppendLine("## By hero").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Gear power | Completed | vs shipped | Paired (both / only shipped / only option) | Cleared 1 / 2 / 3 | " +
                          "Warden (f1) P50 (in band) | Regent (f2) P50 (in band) | Warden (f3) P50 (in band) | Descent P50 min | Banked mean |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Option option in options)
            foreach (string cohort in cohorts)
            foreach (string hero in heroes)
            {
                Row[] g = Of(rows, option.Key, cohort, hero);
                Row[] plain = Of(rows, "shipped", cohort, hero);
                if (g.Length == 0)
                    continue;
                int done = g.Count(r => r.Completed);
                bool paired = option.Key != "shipped" && plain.Length == g.Length;
                md.AppendLine($"| `{option.Key}` | {cohort} | {hero} | {F(g[0].Power / 100.0, "+0.0")} % | {done} ({Pct(done, g.Length)}) | " +
                              $"{(paired ? Signed(done - plain.Count(r => r.Completed)) : "-")} | {(paired ? Paired(plain, g) : "-")} | " +
                              $"{g.Count(r => r.Income.FloorsCleared >= 1)} / {g.Count(r => r.Income.FloorsCleared >= 2)} / {g.Count(r => r.Income.FloorsCleared >= 3)} | " +
                              $"{Band(g.Select(r => r.Warden1))} | {Band(g.Select(r => r.Regent2))} | {Band(g.Select(r => r.Warden3))} | " +
                              $"{M(Median(g.Where(r => r.Completed).Select(r => r.Descent)))} | {F(g.Average(r => r.Income.Banked), "0")} |");
            }
            md.AppendLine();
            md.AppendLine("## The options").AppendLine();
            foreach (Option option in options)
                md.AppendLine($"- `{option.Key}`: {DifficultyTiers.Name(option.Tier)}, enemies {option.Multipliers}, gear {option.Gear}, " +
                              $"rewards {DifficultyTiers.RewardText(option.Tier)}.");
            md.AppendLine();
            if (unknown.Count > 0)
                md.AppendLine("Kinds without a base gold (counted as 0): " + string.Join(", ", unknown.OrderBy(s => s)) + ".").AppendLine();
            return md.ToString();
        }

        private static Row[] Of(List<Row> rows, string option, string cohort, string hero) =>
            rows.Where(r => r.Option.Key == option && r.Cohort == cohort && (hero == null || r.Hero == hero))
                .OrderBy(r => r.Hero, StringComparer.Ordinal).ThenBy(r => r.Seed).ToArray();

        private static string Paired(Row[] plain, Row[] option)
        {
            int both = 0, onlyPlain = 0, onlyOption = 0;
            for (int i = 0; i < plain.Length; i++)
            {
                bool a = plain[i].Completed, b = option[i].Completed;
                if (a && b)
                    both++;
                else if (a)
                    onlyPlain++;
                else if (b)
                    onlyOption++;
            }
            return $"{both} / {onlyPlain} / {onlyOption}";
        }

        private static string Band(IEnumerable<float> fights)
        {
            float[] felled = fights.Where(v => v >= 0f).ToArray();
            if (felled.Length == 0)
                return "-";
            return $"{F(Median(felled), "0.0")} s ({Pct(felled.Count(s => s >= BandFrom && s <= BandTo), felled.Length)})";
        }

        private static string Csv(List<Row> rows, string hands)
        {
            var csv = new StringBuilder();
            csv.AppendLine("realism,option,tier,enemy_health,enemy_damage,gear,cohort,hero,seed,gear_power,f1_outcome,f2_outcome,f3_outcome," +
                           "finale_outcome,floors_cleared,descent_s,warden_f1,regent_f2,warden_f3,banked");
            foreach (Row r in rows)
            {
                DescentSimulation.FoundryResult finale = FoundryBatch.FinaleOf(r.Result);
                csv.AppendLine(string.Join(",", hands, r.Option.Key, DifficultyTiers.Id(r.Option.Tier),
                    F(float.IsNaN(r.Option.Health) ? DifficultyTiers.EnemyHealth(r.Option.Tier) : r.Option.Health, "0.###"),
                    F(float.IsNaN(r.Option.Damage) ? DifficultyTiers.EnemyDamage(r.Option.Tier) : r.Option.Damage, "0.###"),
                    r.Option.Gear, r.Cohort, r.Hero, r.Seed, r.Power,
                    Outcome(r.Result), Outcome(FoundryBatch.FloorOf(r.Result, 2)), Outcome(FoundryBatch.FloorOf(r.Result, 3)),
                    finale != null ? (finale.WalkedOut ? "WalkedOut" : finale.Outcome.ToString()) : string.Empty,
                    r.Income.FloorsCleared, r.Completed ? F(r.Descent, "0.00") : string.Empty,
                    N(r.Warden1), N(r.Regent2), N(r.Warden3), r.Income.Banked));
            }
            return csv.ToString();
        }

        private static string Outcome(DescentSimulation.FoundryResult floor) => floor != null ? floor.Outcome.ToString() : string.Empty;

        private static double Median(IEnumerable<float> values)
        {
            float[] sorted = values.OrderBy(v => v).ToArray();
            if (sorted.Length == 0)
                return double.NaN;
            return sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0;
        }

        private static string M(double seconds) => double.IsNaN(seconds) ? "-" : F(seconds / 60.0, "0.00");

        private static string N(float value) => value < 0f ? string.Empty : F(value, "0.00");

        private static string Pct(int part, int whole) =>
            whole == 0 ? "-" : F(100.0 * part / whole, "0.0") + " %";

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString(CultureInfo.InvariantCulture);

        private static string F(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        private static string[] List(string name, string fallback) =>
            (Environment.GetEnvironmentVariable(name) ?? fallback).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

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
