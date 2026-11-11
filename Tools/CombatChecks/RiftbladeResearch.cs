using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/46's Riftblade (Nix Vane) measured in the foundry simulation before anything is built (research 2026-11-11). The
    // proposal - 85 health, the Daggers, Quicksilver Stride on them, Phase Dash - and its variants as the simulation's
    // levers on the hero (DescentSimulation.RiftbladeKit): the passive on every weapon, Sever in the dash's place, 90
    // health, no active at all (R_skill's denominator), each played by every cohort, frame-perfect and with a human's hands,
    // down both floors, beside the Vanguard and Ilya on the same seeds - each of them also in the careful hands less the
    // ability, its own R_skill's denominator. Nothing here is the game's: no hero asset, no roster entry, no scene code.
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~RiftbladeResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (the processors), FOUNDRY_OUT
    // (the repository's TestResults), FOUNDRY_FLOORS (2), RIFT_REALISM (comma separated: frame-perfect, typical, slow; the
    // first two by default), RIFT_COHORTS (comma separated cohort names; every cohort by default), RIFT_HEROES (semicolon
    // separated heroes in the words below; Heroes by default). Writes riftblade.md, a row a cell in riftblade.csv and a row
    // a run in riftblade-runs.csv.
    //
    // A hero's words: "vanguard", "ilya" or "nix" (the Riftblade as proposed), then for nix "every" (Quicksilver Stride on
    // every weapon), "sever" (Sever in the dash's place), "none" (no active), "health N", "wound X" (the wound's share),
    // "dash N" (the dash's cooldown), "escape" (the dash as an escape only, no hold at the Warden), "hold" (the hold at the
    // Warden's feet whenever the dash is ready, not through the telegraph alone); "+bare" adds the careful cohort less the
    // ability (careful-no-ability); "only careful" plays the careful cohort alone.
    //
    // docs/46's measured section read TheRiftbladeOverASeedBatch's TestResults/riftblade-1111/full (seeds 1-200, every hero
    // of Heroes and the careful-only levers), holdout (FOUNDRY_FIRST_SEED=201, RIFT_COHORTS=careful) and hold-ready ("nix
    // hold;nix none"), all on main ba11a36f, and cebf8775 ("vanguard +bare;ilya +bare;nix;nix none" on main cebf8775).
    // OneRiftbladeSeedAsTheTelemetryWouldWriteIt traces one seed's careful run instead.
    [Explicit("The Riftblade's research: run it by name.")]
    public sealed class RiftbladeResearch
    {
        public const string Heroes =
            "vanguard +bare;ilya +bare;nix;nix every;nix sever;nix health 90;nix none;nix every none only careful";

        // The careful pilot less its ability: R_skill's denominator for a hero whose active is its AbilityRuntime.
        public static readonly AutopilotCohort Bare =
            new AutopilotCohort("careful-no-ability", AutopilotCohort.Careful.Habits & ~AutopilotCohort.Habit.UsesTheAbility);

        private const string Captain = "Grunt Captain";
        private const float Frame = 1f / 60f;

        [Test]
        public void TheRiftbladeOverASeedBatch()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Environment.ProcessorCount);
            int floors = Setting("FOUNDRY_FLOORS", 2);
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] pilots = List("RIFT_REALISM", "frame-perfect,typical", ',');
            string[] cohortNames = List("RIFT_COHORTS", string.Join(",", AutopilotCohort.Cohorts.Select(c => c.Name)), ',');
            AutopilotCohort[] cohorts = AutopilotCohort.Cohorts.Where(c => cohortNames.Contains(c.Name)).ToArray();
            HeroSpec[] heroes = List("RIFT_HEROES", Heroes, ';').Select(HeroSpec.Parse).ToArray();

            var cells = new List<Cell>();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            string head = $"Seeds {seeds[0]}-{seeds[seeds.Length - 1]} a hero, {floors} floor(s), pilots {string.Join(", ", pilots)}, " +
                          $"cohorts {string.Join(", ", cohorts.Select(c => c.Name))}";
            foreach (string pilot in pilots)
            {
                PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
                foreach (HeroSpec spec in heroes)
                    foreach (AutopilotCohort cohort in spec.CohortsOf(cohorts))
                    {
                        DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("riftblade: " + spec.Name);
                        levers.Cohort = cohort;
                        levers.Realism = realism;
                        levers.Floors = floors;
                        FoundryBatch.Batch batch = FoundryBatch.Run(spec.Build(), levers, seeds, parallel);
                        cells.Add(new Cell { Pilot = pilot, Spec = spec, Cohort = cohort, Runs = batch.Runs });
                    }
                TestContext.Progress.WriteLine($"{pilot}: {heroes.Length} heroes in {watch.Elapsed.TotalMinutes:0.0} min");
                Write(output, head, watch, pilots, cohorts, heroes, cells);
            }
            Write(output, head, watch, pilots, cohorts, heroes, cells);
            Assert.That(cells.All(c => c.Runs.Length == seeds.Length), "every seed ran");
        }

        // One seed of one hero's careful run as the scene's telemetry would write it, the Riftblade's dashes, turned hits and
        // presses among the lines: RIFT_TRACE_SEED (11) and the first hero of RIFT_HEROES ("nix"), FOUNDRY_FLOORS (2) and
        // RIFT_REALISM's first pilot, into FOUNDRY_OUT as riftblade-trace-<seed>.txt.
        [Test]
        public void OneRiftbladeSeedAsTheTelemetryWouldWriteIt()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            int seed = Setting("RIFT_TRACE_SEED", 11);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            HeroSpec spec = HeroSpec.Parse(List("RIFT_HEROES", "nix", ';')[0]);
            string pilot = List("RIFT_REALISM", "frame-perfect", ',')[0];
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("riftblade trace: " + spec.Name);
            levers.Floors = Setting("FOUNDRY_FLOORS", 2);
            levers.Realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            var lines = new List<string>();
            DescentSimulation.FoundryResult result = DescentSimulation.RunFoundry(spec.Build(), seed, levers, trace: lines.Add);
            for (DescentSimulation.FoundryResult floor = result; floor != null; floor = floor.NextFloor)
                lines.Add($"floor {floor.Floor}: {floor.FloorSeconds:0.000} end {floor.Outcome} level={floor.Level} dashes={floor.Dashes} " +
                          $"turned={floor.DashTurned} slams_turned={floor.DashSlamsTurned} hold={floor.DashHoldSeconds:0.00} primed={floor.MomentumCrits} " +
                          $"wounds={floor.WoundsOpened} bleed={floor.DealtByBleed:0}");
            File.WriteAllLines(Path.Combine(output, $"riftblade-trace-{seed}.txt"), lines);
            Assert.That(lines.Count, Is.GreaterThan(1));
        }

        private static void Write(string output, string head, System.Diagnostics.Stopwatch watch, string[] pilots,
            AutopilotCohort[] cohorts, HeroSpec[] heroes, List<Cell> cells)
        {
            var md = new StringBuilder();
            md.AppendLine("# The Riftblade (Nix Vane) in the foundry simulation").AppendLine();
            md.AppendLine($"{head} ({DateTime.Now:yyyy-MM-dd HH:mm}, {watch.Elapsed.TotalMinutes:0} min). docs/46 measured: research only, " +
                          "nothing of the game changes.").AppendLine();
            md.AppendLine("| Hero | Words | Cohorts |");
            md.AppendLine("|---|---|---|");
            foreach (HeroSpec spec in heroes)
                md.AppendLine($"| {spec.Name} | {spec.Words} | {string.Join(", ", spec.CohortsOf(cohorts).Select(c => c.Name))} |");
            md.AppendLine();
            foreach (string pilot in pilots)
            {
                List<Cell> mine = cells.Where(c => c.Pilot == pilot).ToList();
                if (mine.Count == 0)
                    continue;
                md.AppendLine($"## {pilot}").AppendLine();
                md.Append(Clears(mine, heroes, cohorts));
                md.Append(Wardens(mine, heroes, cohorts));
                md.Append(Skill(mine, heroes));
                md.Append(Elites(mine, heroes, cohorts));
                md.Append(Jobs(mine, heroes));
                md.Append(Deaths(mine, heroes, cohorts));
                md.Append(FloorTime(mine, heroes, cohorts));
                md.Append(Riftblade(mine, heroes, cohorts));
                md.Append(BossStruggle(mine, heroes));
            }
            File.WriteAllText(Path.Combine(output, "riftblade.md"), md.ToString());
            File.WriteAllText(Path.Combine(output, "riftblade.csv"), CellCsv(cells));
            File.WriteAllText(Path.Combine(output, "riftblade-runs.csv"), RunCsv(cells));
        }

        // ---------------------------------------------------------------------------------------------------------
        // Heroes and cells
        // ---------------------------------------------------------------------------------------------------------

        private sealed class HeroSpec
        {
            public string Words;
            public string Name;
            public Func<DescentSimulation.FoundryHero> Build;
            public bool Bare;
            public bool OnlyCareful;
            public bool Riftblade;

            public IEnumerable<AutopilotCohort> CohortsOf(AutopilotCohort[] cohorts)
            {
                foreach (AutopilotCohort cohort in cohorts)
                    if (!OnlyCareful || cohort == AutopilotCohort.Careful)
                        yield return cohort;
                if (Bare && cohorts.Contains(AutopilotCohort.Careful))
                    yield return RiftbladeResearch.Bare;
            }

            public static HeroSpec Parse(string words)
            {
                string[] w = words.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var spec = new HeroSpec { Words = string.Join(" ", w) };
                var sets = new List<Action<DescentSimulation.FoundryHero>>();
                var names = new List<string>();
                string hero = w.Length > 0 ? w[0] : string.Empty;
                for (int i = 1; i < w.Length; i++)
                {
                    switch (w[i])
                    {
                        case "+bare":
                            spec.Bare = true;
                            break;
                        case "only":
                            if (i + 1 < w.Length && w[i + 1] == "careful")
                                i++;
                            spec.OnlyCareful = true;
                            break;
                        case "every":
                            sets.Add(h => h.Rift.StrideOnEveryWeapon = true);
                            names.Add("stride on every weapon");
                            break;
                        case "sever":
                            sets.Add(h => h.Rift.Active = DescentSimulation.RiftActive.Sever);
                            names.Add("Sever");
                            break;
                        case "none":
                            sets.Add(h => h.Rift.Active = DescentSimulation.RiftActive.None);
                            names.Add("no active");
                            break;
                        case "escape":
                            sets.Add(h => h.Rift.HoldsForTheDash = false);
                            names.Add("dash as an escape only");
                            break;
                        case "hold":
                            sets.Add(h => h.Rift.HoldsWhileReady = true);
                            names.Add("holds at the Warden while the dash is ready");
                            break;
                        case "health":
                        {
                            float health = Float(w[++i]);
                            sets.Add(h => h.MaximumHealth = health);
                            names.Add("health " + N(health, "0"));
                            break;
                        }
                        case "wound":
                        {
                            float share = Float(w[++i]);
                            sets.Add(h => h.Rift.WoundShare = share);
                            names.Add("wound " + N(share, "0.0#"));
                            break;
                        }
                        case "dash":
                        {
                            float cooldown = Float(w[++i]);
                            sets.Add(h => h.Rift.DashCooldown = cooldown);
                            names.Add("dash " + N(cooldown, "0.#") + " s");
                            break;
                        }
                        default:
                            throw new ArgumentException($"No such word in a hero: {w[i]} ({words})");
                    }
                }
                Func<DescentSimulation.FoundryHero> make;
                switch (hero)
                {
                    case "vanguard":
                        make = DescentSimulation.Vanguard;
                        spec.Name = "Vanguard";
                        break;
                    case "ilya":
                        make = DescentSimulation.CinderArcanist;
                        spec.Name = "Ilya";
                        break;
                    case "nix":
                        make = DescentSimulation.Riftblade;
                        spec.Name = names.Count == 0 ? "Nix" : "Nix, " + string.Join(", ", names);
                        spec.Riftblade = true;
                        break;
                    default:
                        throw new ArgumentException("No such hero: " + words);
                }
                if (!spec.Riftblade && sets.Count > 0)
                    throw new ArgumentException("Only the Riftblade takes those words: " + words);
                spec.Build = () =>
                {
                    DescentSimulation.FoundryHero built = make();
                    if (built.Rift != null)
                        built.Rift = built.Rift.Copy();
                    foreach (Action<DescentSimulation.FoundryHero> set in sets)
                        set(built);
                    built.Name = spec.Name;
                    return built;
                };
                return spec;
            }
        }

        private sealed class Cell
        {
            public string Pilot;
            public HeroSpec Spec;
            public AutopilotCohort Cohort;
            public DescentSimulation.FoundryResult[] Runs;

            public int Cleared => Runs.Count(r => r.Cleared);
            public int BothCleared => Runs.Count(BothFloors);
            public IEnumerable<DescentSimulation.FoundryResult> Second => Runs.Where(r => r.NextFloor != null).Select(r => r.NextFloor);
            // Every floor played: floor 1 of each run, and floor 2 where it went down.
            public IEnumerable<DescentSimulation.FoundryResult> Floors => Runs.Concat(Second);
        }

        private static bool BothFloors(DescentSimulation.FoundryResult r) => r.Cleared && r.NextFloor != null && r.NextFloor.Cleared;

        private static Cell Find(List<Cell> cells, HeroSpec spec, AutopilotCohort cohort) =>
            cells.FirstOrDefault(c => c.Spec == spec && c.Cohort == cohort);

        // The hero's own damage a second on the Warden - every weapon, its burn, its bleed and its ability, the pit apart
        // (docs/46 section 4's R_skill) - over the seconds of the fight after the reveal; NaN where it never fought it.
        private static float OwnOnWarden(DescentSimulation.FoundryResult r) =>
            r.WardenFought > 0f
                ? (r.WardenFromWeapons + r.WardenFromBurn + r.WardenFromBleed + r.WardenFromAbility) / r.WardenFought
                : float.NaN;

        // ---------------------------------------------------------------------------------------------------------
        // The tables
        // ---------------------------------------------------------------------------------------------------------

        private static string Clears(List<Cell> cells, HeroSpec[] heroes, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("### Clears").AppendLine();
            md.AppendLine("Of the seeds: floor 1 ; both floors (a floor-1 clear goes down to floor 2).").AppendLine();
            md.AppendLine("| Hero | " + string.Join(" | ", cohorts.Select(c => c.Name)) + " | careful-no-ability |");
            md.AppendLine("|---|" + string.Concat(cohorts.Select(_ => "---|")) + "---|");
            foreach (HeroSpec spec in heroes)
            {
                string Of(AutopilotCohort cohort)
                {
                    Cell c = Find(cells, spec, cohort);
                    return c == null ? "-" : $"{c.Cleared} ; {c.BothCleared}";
                }
                md.AppendLine($"| {spec.Name} | {string.Join(" | ", cohorts.Select(Of))} | {Of(Bare)} |");
            }
            md.AppendLine();
            return md.ToString();
        }

        private static string Wardens(List<Cell> cells, HeroSpec[] heroes, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("### The Warden's fight").AppendLine();
            md.AppendLine("P50 (P90) of the felled fights, from walking out of the gate to falling, floor 1 ; floor 2 (its own fight, the escort " +
                          "apart), and the shortest; the gate's band is 45-65 s, docs/40 section 8.8's for the Riftblade 35-55 s. Died at the Warden: " +
                          "of the runs that reached it, floor 1 ; floor 2.").AppendLine();
            md.AppendLine("| Hero | Cohort | Warden P50 (P90) floor 1 ; floor 2 | Shortest | Died at the Warden |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (HeroSpec spec in heroes)
                foreach (AutopilotCohort cohort in spec.CohortsOf(cohorts))
                {
                    Cell c = Find(cells, spec, cohort);
                    if (c == null)
                        continue;
                    float[] one = c.Runs.Where(r => r.LevelAtGate >= 0 && r.BossSeconds >= 0f).Select(r => r.BossSeconds).ToArray();
                    float[] two = c.Second.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds).ToArray();
                    DescentSimulation.FoundryResult[] reachedOne = c.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                    DescentSimulation.FoundryResult[] reachedTwo = c.Second.Where(r => r.LevelAtGate >= 0).ToArray();
                    md.AppendLine($"| {spec.Name} | {cohort.Name} | {P(one, 50)} ({P(one, 90)}) ; {P(two, 50)} ({P(two, 90)}) | " +
                                  $"{Min(one)} ; {Min(two)} | {reachedOne.Count(r => r.DiedAtWarden)} of {reachedOne.Length} ; " +
                                  $"{reachedTwo.Count(r => r.DiedAtWarden)} of {reachedTwo.Length} |");
                }
            md.AppendLine();
            return md.ToString();
        }

        // R_skill (docs/40 section 4.2, docs/46 section 4): the careful cohort with its active over the same cohort without it,
        // the hero's own damage a second on the Warden, P50 of each and the median of the seeds that fought it both ways;
        // and the same on the elite rooms' Captains, and the whole floor's damage a second.
        private static string Skill(List<Cell> cells, HeroSpec[] heroes)
        {
            var md = new StringBuilder();
            md.AppendLine("### R_skill").AppendLine();
            md.AppendLine("The careful cohort with its active over the same hero without it: the Vanguard and Ilya less the ability's habit, " +
                          "the Riftblade's variants over the Riftblade with no active (its passive's own where it differs: the passive on every weapon " +
                          "over itself without the dash). Own on the Warden: every weapon, burn, bleed and ability a second of the fight after the " +
                          "reveal, the pit apart, P50 of each batch (and the median of the paired seeds' ratios). Captains: an elite room's Captain's " +
                          "time under fire, P50 without / with = how much sooner it falls, both floors. Floor: the hero's damage a second of floor 1. " +
                          "docs/40's targets: Vanguard 1.05-1.15x, Ilya 1.35-1.6x, Riftblade 1.6-2.0x.").AppendLine();
            md.AppendLine("| Hero | Over | Own on the Warden, floor 1: with / without = R (paired) | Floor 2 | Captains under fire | Floor 1's damage a second |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach (HeroSpec spec in heroes)
            {
                Cell with = Find(cells, spec, AutopilotCohort.Careful);
                if (with == null)
                    continue;
                Cell without = null;
                string over;
                if (!spec.Riftblade)
                {
                    without = Find(cells, spec, Bare);
                    over = "careful-no-ability";
                }
                else
                {
                    HeroSpec denominator = Denominator(heroes, spec);
                    if (denominator == spec)
                        continue;
                    without = denominator != null ? Find(cells, denominator, AutopilotCohort.Careful) : null;
                    over = denominator != null ? denominator.Name : "-";
                }
                if (without == null)
                    continue;
                md.AppendLine($"| {spec.Name} | {over} | {Ratio(with.Runs, without.Runs, OwnOnWarden)} | " +
                              $"{Ratio(with.Second.ToArray(), without.Second.ToArray(), OwnOnWarden)} | " +
                              $"{UnderFire(without)} / {UnderFire(with)} = {Speedup(without, with)} | {Ratio(with.Runs, without.Runs, OwnOnFloor)} |");
            }
            md.AppendLine();
            return md.ToString();
        }

        // The Riftblade variant a variant's R_skill reads over: the same words with no active ("nix none" for the proposal,
        // "nix every none" for the passive on every weapon), else the proposal's denominator; itself for one with no active.
        private static HeroSpec Denominator(HeroSpec[] heroes, HeroSpec spec)
        {
            if (spec.Words.Split(' ').Contains("none"))
                return spec;
            string[] kept = spec.Words.Split(' ').Where(w => w == "nix" || w == "every").ToArray();
            string wanted = string.Join(" ", kept) + " none";
            return heroes.FirstOrDefault(h => h.Riftblade && h.Words.StartsWith(wanted, StringComparison.Ordinal)) ??
                   heroes.FirstOrDefault(h => h.Riftblade && h.Words.StartsWith("nix none", StringComparison.Ordinal));
        }

        // An elite room's Captain's time under fire, P50 of every Captain felled on either floor.
        private static double CaptainUnderFire(Cell c) =>
            FoundryBatch.Median(c.Floors.SelectMany(r => r.Kills).Where(k => !k.Horde && k.Kind == Captain).Select(k => k.UnderFire));

        private static string UnderFire(Cell c) => N(CaptainUnderFire(c), "0.00");

        private static string Speedup(Cell without, Cell with)
        {
            double a = CaptainUnderFire(without), b = CaptainUnderFire(with);
            return b > 0 && !double.IsNaN(a) ? (a / b).ToString("0.00", CultureInfo.InvariantCulture) + "x" : "-";
        }

        private static float OwnOnFloor(DescentSimulation.FoundryResult r) =>
            r.FloorSeconds > 0f ? (r.DealtByWeapons + r.DealtByBurn + r.DealtByBleed + r.DealtByAbility) / r.FloorSeconds : float.NaN;

        private static string Ratio(DescentSimulation.FoundryResult[] with, DescentSimulation.FoundryResult[] without,
            Func<DescentSimulation.FoundryResult, float> read)
        {
            double a = FoundryBatch.Median(with.Select(read).Where(v => !float.IsNaN(v)));
            double b = FoundryBatch.Median(without.Select(read).Where(v => !float.IsNaN(v)));
            var paired = new List<float>();
            var bySeed = without.GroupBy(r => r.Seed).ToDictionary(g => g.Key, g => g.First());
            foreach (DescentSimulation.FoundryResult r in with)
                if (bySeed.TryGetValue(r.Seed, out DescentSimulation.FoundryResult o))
                {
                    float x = read(r), y = read(o);
                    if (!float.IsNaN(x) && !float.IsNaN(y) && y > 0f)
                        paired.Add(x / y);
                }
            string ratio = b > 0 && !double.IsNaN(a) ? (a / b).ToString("0.00", CultureInfo.InvariantCulture) + "x" : "-";
            return $"{N(a, "0.0")} / {N(b, "0.0")} = {ratio} ({P(paired, 50, "0.00")}x, {paired.Count})";
        }

        // docs/40 section 13.1's quest, an elite felled within 30 s, and section 8.6's elite TTK: the elite rooms' Grunt
        // Captains felled, floor 1 and floor 2 together, from walking in to falling and under fire; how many fell within 30 s
        // of walking in, and within a frame of their first damage (a one-shot).
        private static string Elites(List<Cell> cells, HeroSpec[] heroes, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("### The elites: a Captain's kill time").AppendLine();
            md.AppendLine("Every elite room's Grunt Captain felled, both floors: from walking in to falling P50 / P90 / longest, the share felled " +
                          "within 30 s of walking in (docs/40 13.1's quest), and under fire (its first damage to its fall) P10 / P50, with how many " +
                          "fell within a frame of their first damage (none: no one-shot).").AppendLine();
            md.AppendLine("| Hero | Cohort | Felled | Walking in to falling P50 / P90 / longest | Within 30 s | Under fire P10 / P50 | Within a frame |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach (HeroSpec spec in heroes)
                foreach (AutopilotCohort cohort in spec.CohortsOf(cohorts))
                {
                    Cell c = Find(cells, spec, cohort);
                    if (c == null)
                        continue;
                    DescentSimulation.FoundryKill[] felled = c.Floors.SelectMany(r => r.Kills).Where(k => !k.Horde && k.Kind == Captain).ToArray();
                    float[] life = felled.Select(k => k.Lifetime).ToArray();
                    float[] fire = felled.Select(k => k.UnderFire).ToArray();
                    md.AppendLine($"| {spec.Name} | {cohort.Name} | {felled.Length} | {P(life, 50, "0.0")} / {P(life, 90, "0.0")} / {Max(life)} | " +
                                  $"{Pct(life.Count(l => l <= 30f), life.Length)} | {P(fire, 10, "0.0")} / {P(fire, 50, "0.0")} | " +
                                  $"{fire.Count(f => f <= Frame + 1e-4f)} |");
                }
            md.AppendLine();
            return md.ToString();
        }

        // The careful cohort's damage by job and by weapon, floor 1: each weapon the hero held, its own damage on the horde a
        // second it was held, on the elite rooms' Captains a second of their fights and on the Warden a second of its fight
        // (P50 of the runs that held it and met the job; the gate's Efficiency for every weapon of the loadout), and of that
        // the bleed's; then the Warden's health by source over the felled fights.
        private static string Jobs(List<Cell> cells, HeroSpec[] heroes)
        {
            var md = new StringBuilder();
            md.AppendLine("### Damage by job and by weapon (careful, floor 1)").AppendLine();
            md.AppendLine("A weapon's own damage a second on each job, P50 of the runs that held it: the horde (a second it was held), the elite rooms' " +
                          "Captains (a second of their fights), the Warden (a second of its fight after the reveal); in brackets the part its wounds' " +
                          "ticks paid (their P50 a second), and how many runs held it at the floor's end. The Warden's health: mean share over the felled " +
                          "fights by what took it.").AppendLine();
            md.AppendLine("| Hero | Weapon (runs) | Horde | Captains | Warden |");
            md.AppendLine("|---|---|---|---|---|");
            string[] ids = DescentSimulation.WeaponCatalogue().Select(w => w.Id).ToArray();
            foreach (HeroSpec spec in heroes)
            {
                Cell c = Find(cells, spec, AutopilotCohort.Careful);
                if (c == null)
                    continue;
                foreach (string id in ids)
                {
                    var held = c.Runs.Select(r => (Run: r, Work: r.WeaponWork.FirstOrDefault(w => w.Id == id))).Where(x => x.Work != null).ToArray();
                    if (held.Length == 0)
                        continue;
                    float[] horde = held.Where(x => x.Run.FloorSeconds - x.Work.HeldFrom > 0f)
                        .Select(x => x.Work.DealtHorde / (x.Run.FloorSeconds - x.Work.HeldFrom)).ToArray();
                    float[] captains = held.Where(x => x.Run.EliteFought > 0f).Select(x => x.Work.DealtEliteLeader / x.Run.EliteFought).ToArray();
                    float[] captainsBleed = held.Where(x => x.Run.EliteFought > 0f).Select(x => x.Work.BleedEliteLeader / x.Run.EliteFought).ToArray();
                    float[] warden = held.Where(x => x.Run.WardenFought > 0f).Select(x => x.Work.DealtWarden / x.Run.WardenFought).ToArray();
                    float[] wardenBleed = held.Where(x => x.Run.WardenFought > 0f).Select(x => x.Work.BleedWarden / x.Run.WardenFought).ToArray();
                    bool bleeds = held.Any(x => x.Work.DealtByBleed > 0f);
                    md.AppendLine($"| {spec.Name} | {Name(id)} ({held.Length}) | {P(horde, 50, "0.0")} | " +
                                  $"{P(captains, 50, "0.0")}{(bleeds ? " (" + P(captainsBleed, 50, "0.0") + ")" : string.Empty)} | " +
                                  $"{P(warden, 50, "0.0")}{(bleeds ? " (" + P(wardenBleed, 50, "0.0") + ")" : string.Empty)} |");
                }
                DescentSimulation.FoundryResult[] felled = c.Runs.Where(r => r.BossSeconds >= 0f && r.WardenHealth > 0f).ToArray();
                if (felled.Length == 0)
                    continue;
                var shares = new List<string>();
                foreach (string id in ids)
                {
                    double share = felled.Average(r => r.WeaponWork.Where(w => w.Id == id).Sum(w => w.DealtWarden - w.BleedWarden) / r.WardenHealth);
                    if (share > 0)
                        shares.Add($"{Name(id)} {N(100 * share, "0")} %");
                }
                shares.Add($"bleed {N(100 * felled.Average(r => r.WardenFromBleed / r.WardenHealth), "0")} %");
                shares.Add($"burn {N(100 * felled.Average(r => r.WardenFromBurn / r.WardenHealth), "0")} %");
                shares.Add($"ability {N(100 * felled.Average(r => r.WardenFromAbility / r.WardenHealth), "0")} %");
                shares.Add($"pit {N(100 * felled.Average(r => r.WardenFromPit / r.WardenHealth), "0")} %");
                md.AppendLine($"| {spec.Name} | the Warden's health ({felled.Length} felled) | {string.Join(", ", shares)} | | |");
            }
            md.AppendLine();
            return md.ToString();
        }

        private static string Name(string id) =>
            DescentSimulation.WeaponCatalogue().FirstOrDefault(w => w.Id == id)?.DisplayName ?? id;

        // Where the runs died: floor 1's halls (their walks included), its elite rooms, the gate's wait, its Warden and
        // anything else (the forge, the exit); floor 2's the same, its gate never holding.
        private static string Deaths(List<Cell> cells, HeroSpec[] heroes, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("### Deaths by place").AppendLine();
            md.AppendLine("Of the seeds: floor 1 halls / elite rooms / the gate's wait / the Warden / elsewhere ; floor 2 halls / elite room / the " +
                          "Warden / elsewhere (a hall's walk counts with it).").AppendLine();
            md.AppendLine("| Hero | Cohort | Floor 1 | Floor 2 |");
            md.AppendLine("|---|---|---|---|");
            foreach (HeroSpec spec in heroes)
                foreach (AutopilotCohort cohort in spec.CohortsOf(cohorts))
                {
                    Cell c = Find(cells, spec, cohort);
                    if (c == null)
                        continue;
                    string One(IEnumerable<DescentSimulation.FoundryResult> runs, bool wait)
                    {
                        var died = runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).Select(Place).ToArray();
                        var parts = new List<int> { died.Count(p => p == "halls"), died.Count(p => p == "elite") };
                        if (wait)
                            parts.Add(died.Count(p => p == "wait"));
                        parts.Add(died.Count(p => p == "Warden"));
                        parts.Add(died.Count(p => p == "other"));
                        return string.Join(" / ", parts);
                    }
                    md.AppendLine($"| {spec.Name} | {cohort.Name} | {One(c.Runs, true)} | {One(c.Second, false)} |");
                }
            md.AppendLine();
            return md.ToString();
        }

        private static string Place(DescentSimulation.FoundryResult r)
        {
            if (r.DeathPhase == DescentSimulation.GateWaitPhase)
                return "wait";
            if (r.DiedAtWarden)
                return "Warden";
            if (r.DeathRoom < 1 || r.DeathRoom > r.Kinds.Length)
                return "other";
            switch (r.Kinds[r.DeathRoom - 1])
            {
                case RoomKind.Combat:
                    return "halls";
                case RoomKind.Elite:
                    return "elite";
                case RoomKind.Boss:
                    return "Warden";
                default:
                    return "other";
            }
        }

        private static string FloorTime(List<Cell> cells, HeroSpec[] heroes, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("### Floor time").AppendLine();
            md.AppendLine("Active seconds, P50 (P90) of the clears: floor 1 ; floor 2 ; both floors (floor 1 and floor 2 of the runs that cleared both). " +
                          "Level at the gate: P50 as floor 1's Warden walked out.").AppendLine();
            md.AppendLine("| Hero | Cohort | Floor 1 | Floor 2 | Both | Level at the gate |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach (HeroSpec spec in heroes)
                foreach (AutopilotCohort cohort in spec.CohortsOf(cohorts))
                {
                    Cell c = Find(cells, spec, cohort);
                    if (c == null)
                        continue;
                    float[] one = c.Runs.Where(r => r.Cleared).Select(r => r.FloorSeconds).ToArray();
                    float[] two = c.Second.Where(r => r.Cleared).Select(r => r.FloorSeconds).ToArray();
                    float[] both = c.Runs.Where(BothFloors).Select(r => r.FloorSeconds + r.NextFloor.FloorSeconds).ToArray();
                    md.AppendLine($"| {spec.Name} | {cohort.Name} | {P(one, 50)} ({P(one, 90)}) | {P(two, 50)} ({P(two, 90)}) | " +
                                  $"{P(both, 50)} ({P(both, 90)}) | {P(c.Runs.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate), 50)} |");
                }
            md.AppendLine();
            return md.ToString();
        }

        // The Riftblade's own readings, both floors: the primed crits and the wounds a minute of the floor (docs/46 section
        // 4's "the walk": careful against standing), the dashes and Sever's presses a floor, the seconds held at the Warden's
        // feet for a ready dash, the hits the guard turned and the slams among them against the slams that found the hero's
        // mark, the bleed's share of the hero's damage and its kills.
        private static string Riftblade(List<Cell> cells, HeroSpec[] heroes, AutopilotCohort[] cohorts)
        {
            var md = new StringBuilder();
            md.AppendLine("### The Riftblade's passive and active").AppendLine();
            md.AppendLine("Every floor played (floor 1, and floor 2 where it went down). Primed crits and wounds: a minute of the floor, P50. Dashes, " +
                          "Severs: a floor, P50. Held: seconds at the Warden's feet for a ready dash, P50 of the floors that fought it. Turned: hits the " +
                          "guard turned away a floor (mean), and of the Warden's slams that came down on the hero's mark, the share turned. Bleed: its " +
                          "share of the hero's damage (all floors summed) and its kills a floor (mean).").AppendLine();
            md.AppendLine("| Hero | Cohort | Primed crits a minute | Wounds a minute | Dashes ; Severs a floor | Held at the Warden s | Turned a floor ; slams turned | Bleed's share ; kills |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (HeroSpec spec in heroes.Where(h => h.Riftblade))
                foreach (AutopilotCohort cohort in spec.CohortsOf(cohorts))
                {
                    Cell c = Find(cells, spec, cohort);
                    if (c == null)
                        continue;
                    DescentSimulation.FoundryResult[] floors = c.Floors.Where(r => r.FloorSeconds > 0f).ToArray();
                    DescentSimulation.FoundryResult[] fought = floors.Where(r => r.WardenFought > 0f).ToArray();
                    int slamsOnTheHero = floors.Sum(r => r.SlamHits);
                    int turned = floors.Sum(r => r.DashSlamsTurned);
                    double bleed = floors.Sum(r => (double)r.DealtByBleed);
                    double all = floors.Sum(r => (double)(r.DealtByWeapons + r.DealtByBurn + r.DealtByBleed + r.DealtByAbility));
                    md.AppendLine($"| {spec.Name} | {cohort.Name} | {P(floors.Select(r => 60f * r.MomentumCrits / r.FloorSeconds), 50, "0.0")} | " +
                                  $"{P(floors.Select(r => 60f * r.WoundsOpened / r.FloorSeconds), 50, "0.0")} | " +
                                  $"{P(floors.Select(r => (float)r.Dashes), 50)} ; {P(floors.Select(r => (float)r.Severs), 50)} | " +
                                  $"{P(fought.Select(r => r.DashHoldSeconds), 50, "0.0")} | {Mean(floors.Select(r => (float)r.DashTurned), "0.0")} ; " +
                                  $"{Pct(turned, slamsOnTheHero)} of {slamsOnTheHero} | {(all > 0 ? N(100 * bleed / all, "0.0") : "-")} % ; " +
                                  $"{Mean(floors.Select(r => (float)r.Kills.Count(k => k.By == DescentSimulation.FoundryKill.KilledByBleed)), "0.0")} |");
                }
            md.AppendLine();
            return md.ToString();
        }

        // docs/40 section 4.2's "boss'ta zorlanmalı" read as the auto-farmer cohort (docs/46 section 4): of its floor-1 runs that
        // reached the Warden, the share that died there, and its floor-1 clears.
        private static string BossStruggle(List<Cell> cells, HeroSpec[] heroes)
        {
            var md = new StringBuilder();
            md.AppendLine("### The auto-farmer at the Warden").AppendLine();
            md.AppendLine("| Hero | Reached floor 1's Warden | Died there | Floor 1 cleared |");
            md.AppendLine("|---|---|---|---|");
            foreach (HeroSpec spec in heroes)
            {
                Cell c = Find(cells, spec, AutopilotCohort.AutoFarmer);
                if (c == null)
                    continue;
                DescentSimulation.FoundryResult[] reached = c.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                int died = reached.Count(r => r.DiedAtWarden);
                md.AppendLine($"| {spec.Name} | {reached.Length} | {died} ({Pct(died, reached.Length)}) | {c.Cleared} |");
            }
            md.AppendLine();
            return md.ToString();
        }

        // ---------------------------------------------------------------------------------------------------------
        // The CSVs
        // ---------------------------------------------------------------------------------------------------------

        private static string CellCsv(List<Cell> cells)
        {
            var csv = new StringBuilder();
            csv.AppendLine("pilot,hero,cohort,runs,floor1_cleared,both_cleared,warden_p50_f1,warden_p90_f1,warden_p50_f2,warden_p90_f2," +
                           "own_on_warden_p50_f1,own_on_warden_p50_f2,captain_life_p50,captain_under_fire_p50,captain_one_shots," +
                           "floor1_clear_p50,floor2_clear_p50,both_p50,momentum_crits_min_p50,wounds_min_p50,dashes_p50,bleed_share");
            foreach (Cell c in cells)
            {
                DescentSimulation.FoundryKill[] felled = c.Floors.SelectMany(r => r.Kills).Where(k => !k.Horde && k.Kind == Captain).ToArray();
                DescentSimulation.FoundryResult[] floors = c.Floors.Where(r => r.FloorSeconds > 0f).ToArray();
                double bleed = floors.Sum(r => (double)r.DealtByBleed);
                double all = floors.Sum(r => (double)(r.DealtByWeapons + r.DealtByBurn + r.DealtByBleed + r.DealtByAbility));
                float[] one = c.Runs.Where(r => r.LevelAtGate >= 0 && r.BossSeconds >= 0f).Select(r => r.BossSeconds).ToArray();
                float[] two = c.Second.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds).ToArray();
                csv.AppendLine(string.Join(",", c.Pilot, Quote(c.Spec.Name), c.Cohort.Name, c.Runs.Length, c.Cleared, c.BothCleared,
                    P(one, 50, "0.0"), P(one, 90, "0.0"), P(two, 50, "0.0"), P(two, 90, "0.0"),
                    P(c.Runs.Select(OwnOnWarden).Where(v => !float.IsNaN(v)), 50, "0.0"),
                    P(c.Second.Select(OwnOnWarden).Where(v => !float.IsNaN(v)), 50, "0.0"),
                    P(felled.Select(k => k.Lifetime), 50, "0.0"), P(felled.Select(k => k.UnderFire), 50, "0.00"),
                    felled.Count(k => k.UnderFire <= Frame + 1e-4f),
                    P(c.Runs.Where(r => r.Cleared).Select(r => r.FloorSeconds), 50, "0.0"),
                    P(c.Second.Where(r => r.Cleared).Select(r => r.FloorSeconds), 50, "0.0"),
                    P(c.Runs.Where(BothFloors).Select(r => r.FloorSeconds + r.NextFloor.FloorSeconds), 50, "0.0"),
                    P(floors.Select(r => 60f * r.MomentumCrits / r.FloorSeconds), 50, "0.0"),
                    P(floors.Select(r => 60f * r.WoundsOpened / r.FloorSeconds), 50, "0.0"),
                    P(floors.Select(r => (float)r.Dashes), 50, "0"), all > 0 ? N(bleed / all, "0.000") : "-"));
            }
            return csv.ToString();
        }

        private static string RunCsv(List<Cell> cells)
        {
            var csv = new StringBuilder();
            csv.Append("pilot,hero,cohort,seed");
            foreach (string f in new[] { "f1", "f2" })
                csv.Append($",{f}_outcome,{f}_floor_s,{f}_death,{f}_death_phase,{f}_killer,{f}_level_gate,{f}_warden_s,{f}_warden_fought_s," +
                           $"{f}_warden_hp,{f}_warden_weapons,{f}_warden_bleed,{f}_warden_burn,{f}_warden_ability,{f}_warden_pit,{f}_slams,{f}_slam_hits," +
                           $"{f}_dealt_weapons,{f}_dealt_bleed,{f}_dealt_ability,{f}_elite_fought_s,{f}_captains_dealt,{f}_momentum_crits,{f}_wounds," +
                           $"{f}_dashes,{f}_turned,{f}_slams_turned,{f}_hold_s,{f}_severs,{f}_ability_uses,{f}_weapons,{f}_daggers_warden,{f}_sword_warden,{f}_staff_warden");
            csv.AppendLine();
            foreach (Cell c in cells)
                foreach (DescentSimulation.FoundryResult r in c.Runs)
                {
                    csv.Append(string.Join(",", c.Pilot, Quote(c.Spec.Name), c.Cohort.Name, r.Seed));
                    foreach (DescentSimulation.FoundryResult f in new[] { r, r.NextFloor })
                    {
                        if (f == null)
                        {
                            csv.Append(string.Concat(Enumerable.Repeat(",", 33)));
                            continue;
                        }
                        float Warden(string id) => f.WeaponWork.Where(w => w.Id == id).Sum(w => w.DealtWarden);
                        csv.Append("," + string.Join(",", f.Outcome, N(f.FloorSeconds, "0.00"), f.Outcome == DescentSimulation.FoundryOutcome.Died ? Place(f) : string.Empty,
                            Quote(f.DeathPhase), Quote(f.Killer), f.LevelAtGate, N(f.WardenSeconds, "0.00"), N(f.WardenFought, "0.00"),
                            N(f.WardenHealth, "0"), N(f.WardenFromWeapons, "0"), N(f.WardenFromBleed, "0"), N(f.WardenFromBurn, "0"),
                            N(f.WardenFromAbility, "0"), N(f.WardenFromPit, "0"), f.Slams, f.SlamHits, N(f.DealtByWeapons, "0"),
                            N(f.DealtByBleed, "0"), N(f.DealtByAbility, "0"), N(f.EliteFought, "0.00"),
                            N(f.WeaponWork.Sum(w => w.DealtEliteLeader), "0"), f.MomentumCrits, f.WoundsOpened, f.Dashes, f.DashTurned,
                            f.DashSlamsTurned, N(f.DashHoldSeconds, "0.00"), f.Severs, f.AbilityUses, Quote(f.Weapons),
                            N(Warden(DescentSimulation.DaggersId), "0"), N(Warden(DescentSimulation.SwordId), "0"), N(Warden("weapon_staff"), "0")));
                    }
                    csv.AppendLine();
                }
            return csv.ToString();
        }

        // ---------------------------------------------------------------------------------------------------------
        // Plumbing
        // ---------------------------------------------------------------------------------------------------------

        private static string P(IEnumerable<float> values, double p, string format = "0")
        {
            double value = FoundryBatch.Percentile(values, p);
            return double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Min(float[] values) => values.Length == 0 ? "-" : values.Min().ToString("0", CultureInfo.InvariantCulture);

        private static string Max(float[] values) => values.Length == 0 ? "-" : values.Max().ToString("0", CultureInfo.InvariantCulture);

        private static string Mean(IEnumerable<float> values, string format = "0")
        {
            float[] list = values.ToArray();
            return list.Length == 0 ? "-" : list.Average().ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Pct(int part, int whole) =>
            whole == 0 ? "-" : (100.0 * part / whole).ToString("0", CultureInfo.InvariantCulture) + " %";

        private static string N(double value, string format) =>
            double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "'") + "\"";

        private static float Float(string word) => float.Parse(word, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static string[] List(string name, string fallback, char separator) =>
            (Environment.GetEnvironmentVariable(name) ?? fallback).Split(separator).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

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
