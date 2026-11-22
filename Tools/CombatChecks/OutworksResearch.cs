using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/62's P0a measured (docs/62 section 12): the Outworks phase before floor 1's halls (FoundryLevers.Outworks,
    // DescentSimulation.Outworks.cs) against the shipped climb, both heroes, every cohort, a human's hands, on the
    // exploration seeds 1-2000 only (docs/56 rule 1: P0a is a probe, no confirmation block is spent). Each variant plays the
    // whole climb (floors 1-3 and the finale) on the same seeds as the shipped climb, so every row reads against today:
    //
    // - the Outworks phase's own time (start to the stair held), its anvils, the Keeper and its key, the pads, the Bell;
    // - floor 1's time and the climb's (13-16 minutes, docs/47; docs/62 section 11);
    // - power parity at the Warden: the hero's level and health share as floor 1's Warden walks out, against the shipped
    //   P25-P75 (docs/62 section 1.3);
    // - completion by cohort: floor 1 and the whole climb, and the Bell's deaths over the Bell's entries.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~OutworksResearch.TheOutworksProbe"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1; the probe refuses a seed past 2000),
    // FOUNDRY_PARALLEL (half the processors), FOUNDRY_OUT (the repository's TestResults), FOUNDRY_FLOORS (4: the climb),
    // OUTWORKS_VARIANTS (comma separated variant keys; "p0a" alone by default - "all" plays every one), OUTWORKS_COHORTS
    // (comma separated cohort names; all five), FOUNDRY_REALISM (typical by default; "none" for the frame-perfect pilot).
    // Writes outworks-p0a.md and a row a run in outworks-p0a.csv.
    [Explicit("docs/62 P0a's measurement: run it by name.")]
    public sealed class OutworksResearch
    {
        private sealed class Variant
        {
            public string Key;
            public string What;
            public Func<DescentSimulation.FoundryLevers, DescentSimulation.FoundryLevers> Shape;
        }

        private static IEnumerable<Variant> Variants()
        {
            yield return new Variant
            {
                Key = "p0a", What = "the Outworks as the scene's switch plays it (DescentSimulation.OutworksLevers): OutworksRule's numbers - the phase's horde paying 40 % of its experience, the Keeper's Captain at 12 times its health from 1:00, the Bell at 1:45 - and floor 1's halls as OutworksRule's trim leaves them (as authored)",
                Shape = levers => DescentSimulation.OutworksLevers(levers, "p0a")
            };
            foreach (float share in new[] { .2f, .3f, .5f })
            {
                float xp = share;
                string key = "p0a-xp" + (xp * 100f).ToString("0", CultureInfo.InvariantCulture);
                yield return new Variant
                {
                    Key = key, What = "p0a with the phase's horde paying " + (xp * 100f).ToString("0", CultureInfo.InvariantCulture) + " % of its experience (HordeRule's 20 % is the shipped horde's)",
                    Shape = levers => { var copy = DescentSimulation.OutworksLevers(levers, key); copy.OutworksHordeExperienceShare = xp; return copy; }
                };
            }
            yield return new Variant
            {
                Key = "p0a-keeper45", What = "p0a with the Keeper walking on at 0:45 (OutworksRule's first value)",
                Shape = levers => { var copy = DescentSimulation.OutworksLevers(levers, "p0a-keeper45"); copy.OutworksKeeperSeconds = 45f; return copy; }
            };
            yield return new Variant
            {
                Key = "p0a-keeper60", What = "p0a with the Keeper walking on at 1:00 (OutworksRule's since the measurement)",
                Shape = levers => { var copy = DescentSimulation.OutworksLevers(levers, "p0a-keeper60"); copy.OutworksKeeperSeconds = 60f; return copy; }
            };
            yield return new Variant
            {
                Key = "p0a-keeper24", What = "p0a with the Keeper's Captain at 24 times its health",
                Shape = levers => { var copy = DescentSimulation.OutworksLevers(levers, "p0a-keeper24"); copy.OutworksKeeperHealthShare = 24f; return copy; }
            };
            yield return new Variant
            {
                Key = "p0a-trim", What = "p0a with floor 1's halls trimmed: one surge a hall, no extra elite, the gate at 3:30",
                Shape = levers => { var copy = DescentSimulation.OutworksLevers(levers, "p0a-trim"); copy.HallSurges = 1; copy.ExtraElites = 0; return copy; }
            };
            yield return new Variant
            {
                Key = "p0a-lean", What = "p0a with floor 1's halls lean: no surges, no extra elite, the gate at 3:30",
                Shape = levers => { var copy = DescentSimulation.OutworksLevers(levers, "p0a-lean"); copy.HallSurges = 0; copy.ExtraElites = 0; return copy; }
            };
            foreach (float bell in new[] { 90f, 120f })
            {
                float b = bell;
                string key = "p0a-bell" + b.ToString("0", CultureInfo.InvariantCulture);
                yield return new Variant
                {
                    Key = key, What = "p0a with the Bell at " + b.ToString("0", CultureInfo.InvariantCulture) + " s",
                    Shape = levers => { var copy = DescentSimulation.OutworksLevers(levers, key); copy.OutworksBellSeconds = b; return copy; }
                };
            }
        }

        private static DescentSimulation.FoundryLevers Outworks(DescentSimulation.FoundryLevers levers, string name, int surges,
            int extraElites, float gate)
        {
            DescentSimulation.FoundryLevers copy = levers.Copy(name);
            copy.Outworks = true;
            copy.HallSurges = surges;
            copy.ExtraElites = extraElites;
            copy.GateNotBeforeSeconds = gate;
            return copy;
        }

        private sealed class Row
        {
            public string Variant;
            public string Hero;
            public string Cohort;
            public DescentSimulation.FoundryResult Run;
        }

        [Test]
        public void TheOutworksProbe()
        {
            int seeds = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            Assert.That(first >= 1 && first + seeds - 1 <= 2000, Is.True,
                "P0a is a probe: the exploration seeds 1-2000 only (docs/56 rule 1), no confirmation block.");
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int floors = Setting("FOUNDRY_FLOORS", DescentSimulation.FinaleFloor);
            string realismWord = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = realismWord == "none" ? null : PilotRealism.Named(realismWord);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string variantWord = Environment.GetEnvironmentVariable("OUTWORKS_VARIANTS") ?? "p0a";
            var variants = Variants().Where(v => variantWord == "all" ||
                variantWord.Split(',').Select(w => w.Trim()).Contains(v.Key)).ToList();
            string cohortWord = Environment.GetEnvironmentVariable("OUTWORKS_COHORTS");
            var cohorts = AutopilotCohort.Cohorts.Where(c => string.IsNullOrEmpty(cohortWord) ||
                cohortWord.Split(',').Select(w => w.Trim()).Contains(c.Name)).ToList();
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };
            int[] seedList = FoundryBatch.Seeds(first, seeds);

            var rows = new List<Row>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = parallel };
            foreach (var hero in heroes)
            foreach (AutopilotCohort cohort in cohorts)
            {
                DescentSimulation.FoundryLevers shipped = FoundryCohorts.Levers(cohort, floors, realism);
                var plays = new List<(string Key, DescentSimulation.FoundryLevers Levers)> { ("shipped", shipped) };
                foreach (Variant variant in variants)
                {
                    DescentSimulation.FoundryLevers levers = variant.Shape(shipped);
                    levers.Cohort = cohort;
                    levers.Floors = floors;
                    levers.Realism = realism;
                    plays.Add((variant.Key, levers));
                }
                foreach (var play in plays)
                {
                    var results = new DescentSimulation.FoundryResult[seedList.Length];
                    Parallel.For(0, seedList.Length, options, i =>
                        results[i] = DescentSimulation.RunFoundry(hero, seedList[i], play.Levers));
                    foreach (var result in results)
                        rows.Add(new Row { Variant = play.Key, Hero = hero.Id, Cohort = cohort.Name, Run = result });
                }
            }

            File.WriteAllText(Path.Combine(output, "outworks-p0a.md"),
                Report(rows, variants, cohorts, heroes, seedList, floors, realism));
            File.WriteAllText(Path.Combine(output, "outworks-p0a.csv"), Csv(rows));
            Assert.That(rows.Count, Is.GreaterThan(0));
        }

        // One seed's Outworks run traced, as FoundryBatchReport's OneSeedAsTheTelemetryWouldWriteIt: OUTWORKS_TRACE_HERO
        // (hero_vanguard or hero_cinder_arcanist), OUTWORKS_TRACE_SEED (1), OUTWORKS_TRACE_COHORT (careful), FOUNDRY_REALISM
        // (none by default), FOUNDRY_FLOORS (1), into FOUNDRY_OUT.
        [Test]
        public void OneOutworksRunTraced()
        {
            string heroId = Environment.GetEnvironmentVariable("OUTWORKS_TRACE_HERO") ?? "hero_vanguard";
            int seed = Setting("OUTWORKS_TRACE_SEED", 1);
            string cohortName = Environment.GetEnvironmentVariable("OUTWORKS_TRACE_COHORT") ?? "careful";
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            DescentSimulation.FoundryHero hero = heroId == "hero_cinder_arcanist"
                ? DescentSimulation.CinderArcanist()
                : DescentSimulation.Vanguard();
            AutopilotCohort.TryNamed(cohortName, out AutopilotCohort cohort);
            string realismWord = Environment.GetEnvironmentVariable("FOUNDRY_REALISM");
            DescentSimulation.FoundryLevers levers = DescentSimulation.OutworksLevers(
                FoundryCohorts.Levers(cohort ?? AutopilotCohort.Careful, Setting("FOUNDRY_FLOORS", 1), PilotRealism.Named(realismWord)));
            var lines = new List<string>();
            DescentSimulation.FoundryResult result = DescentSimulation.RunFoundry(hero, seed, levers, trace: lines.Add);
            lines.Add($"{result.FloorSeconds:0.000} end {result.Outcome} level={result.Level} outworks={result.OutworksSeconds:0.0} anvils={result.AnvilsCaptured} keeper={result.KeeperKilled} key={result.KeyUsed} bell={result.BellRung} pads={result.PadLaunches} death={result.DeathPhase}");
            for (DescentSimulation.FoundryResult floor = result.NextFloor; floor != null; floor = floor.NextFloor)
                lines.Add($"floor {floor.Floor}: {floor.FloorSeconds:0.000} end {floor.Outcome} level={floor.Level}");
            File.WriteAllLines(Path.Combine(output, $"outworks-trace-{heroId}-{cohortName}-{seed}.txt"), lines);
            Assert.That(lines.Count, Is.GreaterThan(1));
        }

        // ---------------------------------------------------------------------------------------------------------------
        // The report
        // ---------------------------------------------------------------------------------------------------------------

        private static IEnumerable<DescentSimulation.FoundryResult> Floors(DescentSimulation.FoundryResult run)
        {
            for (DescentSimulation.FoundryResult floor = run; floor != null; floor = floor.NextFloor)
                yield return floor;
        }

        // The climb completed: every floor cleared down to the finale's walk-out.
        private static bool Climbed(DescentSimulation.FoundryResult run, int floors)
        {
            var all = Floors(run).ToList();
            return all.Count == floors && all.All(f => f.Outcome == DescentSimulation.FoundryOutcome.Cleared);
        }

        private static float ClimbSeconds(DescentSimulation.FoundryResult run) => Floors(run).Sum(f => f.FloorSeconds);

        private static float Percentile(IReadOnlyList<float> values, float p)
        {
            if (values.Count == 0)
                return float.NaN;
            var sorted = values.OrderBy(v => v).ToList();
            float index = p * (sorted.Count - 1);
            int low = (int)Math.Floor(index);
            int high = Math.Min(sorted.Count - 1, low + 1);
            return sorted[low] + (sorted[high] - sorted[low]) * (index - low);
        }

        private static string F(float value, string format = "0") =>
            float.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);

        private static string Mmss(float seconds)
        {
            if (float.IsNaN(seconds))
                return "-";
            int whole = (int)Math.Round(seconds);
            return (whole / 60).ToString(CultureInfo.InvariantCulture) + ":" + (whole % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private static string Share(int count, int of) =>
            of == 0 ? "-" : count + " (" + (100f * count / of).ToString("0", CultureInfo.InvariantCulture) + "%)";

        private static string Report(List<Row> rows, List<Variant> variants, List<AutopilotCohort> cohorts,
            DescentSimulation.FoundryHero[] heroes, int[] seeds, int floors, PilotRealism realism)
        {
            var text = new StringBuilder();
            text.AppendLine("## docs/62 P0a: the Outworks probe in the simulation");
            text.AppendLine();
            text.AppendLine($"Seeds {seeds.First()}-{seeds.Last()} (exploration; docs/56 rule 1), both heroes, cohorts {string.Join(", ", cohorts.Select(c => c.Name))}, " +
                            $"{(realism != null ? realism.Name + " hands" : "the frame-perfect pilot")}, {floors} floors (the climb with the finale). " +
                            "Every variant plays the same seeds as the shipped climb. P50 unless named; times in active seconds.");
            text.AppendLine();
            text.AppendLine("| Variant | What |");
            text.AppendLine("|---|---|");
            text.AppendLine("| shipped | today's climb, no Outworks |");
            foreach (Variant variant in variants)
                text.AppendLine($"| {variant.Key} | {variant.What} |");
            text.AppendLine();

            // Power parity's band: the shipped level and health share at floor 1's Warden, P25-P75, per hero and cohort.
            text.AppendLine("### Pacing, power at the Warden and completion");
            text.AppendLine();
            text.AppendLine("Outworks: the phase from the floor's first frame to the stair held (the runs that held it), P10 / P50 / P90. Floor 1: the clears' active seconds, P50 / P90. " +
                            "Climb: the four floors' active seconds summed over the runs that completed the climb, P50 / P90. Level and health at the Warden: as floor 1's Warden walks out, P50 (P25-P75), " +
                            "against the shipped band of the same hero and cohort beside it. Floor 1 and climb: clears of all seeds. Bell deaths: deaths after the Bell, before the stair, of the runs alive as it rang.");
            text.AppendLine();
            text.AppendLine("| Hero | Cohort | Variant | Outworks P10/P50/P90 | Floor 1 P50/P90 | Climb P50/P90 | Level at Warden | Shipped band | Health at Warden | Shipped band | Clock at Warden P50/P90 | Warden fight P50 | Floor 1 cleared | Climb completed | Bell deaths / entries |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var hero in heroes)
            foreach (AutopilotCohort cohort in cohorts)
            {
                var shipped = rows.Where(r => r.Hero == hero.Id && r.Cohort == cohort.Name && r.Variant == "shipped").Select(r => r.Run).ToList();
                var shippedLevels = shipped.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate).ToList();
                var shippedHealth = shipped.Where(r => r.HeroHealthAtGate >= 0f).Select(r => r.HeroHealthAtGate / Math.Max(1f, r.HeroMaximumAtGate)).ToList();
                string levelBand = F(Percentile(shippedLevels, .25f)) + "-" + F(Percentile(shippedLevels, .75f));
                string healthBand = F(Percentile(shippedHealth, .25f), "0.00") + "-" + F(Percentile(shippedHealth, .75f), "0.00");
                foreach (string key in new[] { "shipped" }.Concat(variants.Select(v => v.Key)))
                {
                    var runs = rows.Where(r => r.Hero == hero.Id && r.Cohort == cohort.Name && r.Variant == key).Select(r => r.Run).ToList();
                    var outworks = runs.Where(r => r.OutworksSeconds >= 0f).Select(r => r.OutworksSeconds).ToList();
                    var floor1 = runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Cleared).Select(r => r.FloorSeconds).ToList();
                    var climb = runs.Where(r => Climbed(r, floors)).Select(ClimbSeconds).ToList();
                    var levels = runs.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate).ToList();
                    var health = runs.Where(r => r.HeroHealthAtGate >= 0f).Select(r => r.HeroHealthAtGate / Math.Max(1f, r.HeroMaximumAtGate)).ToList();
                    var clock = runs.Where(r => r.ClockAtGate >= 0f && r.LevelAtGate >= 0).Select(r => r.ClockAtGate).ToList();
                    var fight = runs.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds).ToList();
                    int bellEntries = runs.Count(r => r.BellRung && r.BellEntered);
                    int bellDeaths = runs.Count(r => r.DeathPhase == DescentSimulation.BellPhaseName);
                    text.AppendLine($"| {hero.Id.Replace("hero_", string.Empty)} | {cohort.Name} | {key} | " +
                                    (outworks.Count > 0 ? $"{Mmss(Percentile(outworks, .1f))} / {Mmss(Percentile(outworks, .5f))} / {Mmss(Percentile(outworks, .9f))}" : "-") + " | " +
                                    $"{Mmss(Percentile(floor1, .5f))} / {Mmss(Percentile(floor1, .9f))} | {Mmss(Percentile(climb, .5f))} / {Mmss(Percentile(climb, .9f))} | " +
                                    $"{F(Percentile(levels, .5f))} ({F(Percentile(levels, .25f))}-{F(Percentile(levels, .75f))}) | {levelBand} | " +
                                    $"{F(Percentile(health, .5f), "0.00")} ({F(Percentile(health, .25f), "0.00")}-{F(Percentile(health, .75f), "0.00")}) | {healthBand} | " +
                                    $"{Mmss(Percentile(clock, .5f))} / {Mmss(Percentile(clock, .9f))} | {F(Percentile(fight, .5f))} s | " +
                                    $"{Share(floor1.Count, runs.Count)} | {Share(climb.Count, runs.Count)} | " +
                                    (key == "shipped" ? "-" : $"{bellDeaths} / {bellEntries}") + " |");
                }
            }
            text.AppendLine();

            text.AppendLine("### The phase itself");
            text.AppendLine();
            text.AppendLine("Of the runs that played the phase. Anvils: captured a run, P50 (mean). Keeper: killed of the runs it walked on in, and the phase's clock at its fall, P50. Key out: runs that left by the key before the Bell. " +
                            "Bell: runs that heard it. Escape: Bell to the stair held, P50 / P90. Pads: launches a run, P50 (mean). Level and health share at the stair, P50. Deaths in the phase: before the Bell / after it. Hits turned: blows that met the hero mid-arc, mean a run.");
            text.AppendLine();
            text.AppendLine("| Hero | Cohort | Variant | Anvils P50 (mean) | Keeper killed | Keeper fell at | Key out | Bell rang | Escape P50/P90 | Pads P50 (mean) | Level at stair | Health at stair | Deaths: outworks / bell | Hits turned |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var hero in heroes)
            foreach (AutopilotCohort cohort in cohorts)
            foreach (Variant variant in variants)
            {
                var runs = rows.Where(r => r.Hero == hero.Id && r.Cohort == cohort.Name && r.Variant == variant.Key).Select(r => r.Run).ToList();
                var anvils = runs.Select(r => (float)r.AnvilsCaptured).ToList();
                int spawned = runs.Count(r => r.KeeperSpawned);
                int killed = runs.Count(r => r.KeeperKilled);
                var fellAt = runs.Where(r => r.KeeperKilled).Select(r => r.KeeperKilledAt).ToList();
                var escape = runs.Where(r => r.BellEscapeSeconds >= 0f).Select(r => r.BellEscapeSeconds).ToList();
                var pads = runs.Select(r => (float)r.PadLaunches).ToList();
                var level = runs.Where(r => r.LevelAtStair >= 0).Select(r => (float)r.LevelAtStair).ToList();
                var health = runs.Where(r => r.HealthAtStair >= 0f).Select(r => r.HealthAtStair).ToList();
                text.AppendLine($"| {hero.Id.Replace("hero_", string.Empty)} | {cohort.Name} | {variant.Key} | {F(Percentile(anvils, .5f))} ({F(anvils.Count > 0 ? anvils.Average() : float.NaN, "0.00")}) | " +
                                $"{Share(killed, spawned)} | {Mmss(Percentile(fellAt, .5f))} | {Share(runs.Count(r => r.KeyUsed), runs.Count)} | {Share(runs.Count(r => r.BellRung), runs.Count)} | " +
                                $"{F(Percentile(escape, .5f))} / {F(Percentile(escape, .9f))} s | {F(Percentile(pads, .5f))} ({F(pads.Count > 0 ? pads.Average() : float.NaN, "0.0")}) | " +
                                $"{F(Percentile(level, .5f))} | {F(Percentile(health, .5f), "0.00")} | " +
                                $"{runs.Count(r => r.DeathPhase == DescentSimulation.OutworksPhaseName)} / {runs.Count(r => r.DeathPhase == DescentSimulation.BellPhaseName)} | " +
                                $"{F(runs.Count > 0 ? (float)runs.Average(r => r.HitsTurnedAirborne) : float.NaN, "0.0")} |");
            }
            text.AppendLine();

            text.AppendLine("### Deaths in the phase, by the killing blow");
            text.AppendLine();
            text.AppendLine("| Hero | Cohort | Variant | Deaths (outworks + bell) | Killing blows |");
            text.AppendLine("|---|---|---|---|---|");
            foreach (var hero in heroes)
            foreach (AutopilotCohort cohort in cohorts)
            foreach (Variant variant in variants)
            {
                var deaths = rows.Where(r => r.Hero == hero.Id && r.Cohort == cohort.Name && r.Variant == variant.Key &&
                                             (r.Run.DeathPhase == DescentSimulation.OutworksPhaseName || r.Run.DeathPhase == DescentSimulation.BellPhaseName))
                    .Select(r => r.Run).ToList();
                if (deaths.Count == 0)
                    continue;
                string blows = string.Join(", ", deaths.GroupBy(r => r.Killer ?? "-").OrderByDescending(g => g.Count())
                    .Select(g => g.Key + " " + g.Count()));
                text.AppendLine($"| {hero.Id.Replace("hero_", string.Empty)} | {cohort.Name} | {variant.Key} | {deaths.Count} | {blows} |");
            }
            text.AppendLine();
            return text.ToString();
        }

        private static string Csv(List<Row> rows)
        {
            var text = new StringBuilder();
            text.AppendLine("variant,hero,cohort,seed,floor1_outcome,floor1_seconds,outworks_seconds,anvils,keeper_spawned,keeper_killed,keeper_fell_at,key_used,bell_rung,bell_escape,pads,level_at_stair,health_at_stair,level_at_warden,health_at_warden,clock_at_warden,warden_fight,death_phase,killer,floors_cleared,climb_seconds,climb_outcome");
            foreach (Row row in rows)
            {
                DescentSimulation.FoundryResult run = row.Run;
                var floors = Floors(run).ToList();
                int cleared = floors.Count(f => f.Outcome == DescentSimulation.FoundryOutcome.Cleared);
                DescentSimulation.FoundryResult last = floors.Last();
                text.AppendLine(string.Join(",",
                    row.Variant, row.Hero, row.Cohort, run.Seed.ToString(CultureInfo.InvariantCulture), run.Outcome,
                    F(run.FloorSeconds, "0.00"), F(run.OutworksSeconds, "0.00"), run.AnvilsCaptured.ToString(CultureInfo.InvariantCulture),
                    run.KeeperSpawned ? "1" : "0", run.KeeperKilled ? "1" : "0", F(run.KeeperKilledAt, "0.00"), run.KeyUsed ? "1" : "0",
                    run.BellRung ? "1" : "0", F(run.BellEscapeSeconds, "0.00"), run.PadLaunches.ToString(CultureInfo.InvariantCulture),
                    run.LevelAtStair.ToString(CultureInfo.InvariantCulture), F(run.HealthAtStair, "0.000"),
                    run.LevelAtGate.ToString(CultureInfo.InvariantCulture),
                    F(run.HeroHealthAtGate >= 0f ? run.HeroHealthAtGate / Math.Max(1f, run.HeroMaximumAtGate) : -1f, "0.000"),
                    F(run.ClockAtGate, "0.00"), F(run.WardenSeconds, "0.00"),
                    run.DeathPhase ?? string.Empty, (run.Killer ?? string.Empty).Replace(',', ' '), cleared.ToString(CultureInfo.InvariantCulture),
                    F(floors.Sum(f => f.FloorSeconds), "0.00"), last.Outcome + (last.RunEnd == RunOutcome.Victory ? " victory" : string.Empty)));
            }
            return text.ToString();
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
