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
    // The Quicksilver Vaults' own enemies (docs/50 section 7, research 2026-11-12). Plays every cohort and both heroes down
    // the whole Descent (floor 1 as shipped, floor 2, floor 3 since docs/48, the finale) on the same seeds with human hands,
    // once as shipped - the Mirror Sentry leads floor 2's Sentry Hall since the lead's decisions of 2026-11-12 - and once for
    // each option - the Grunt Captain back (`captain`, floor 2 before), the Sentry tuned otherwise, or a research creature of
    // docs/50 on floor 2 (DescentSimulation.VaultsChain), with or without the pilot's habit it asks for - and, where an
    // option reads the roles, the Vanguard started with the Staff and with the Daggers in the careful hands beside the
    // careful Vanguard's Sword. Measurements before 2026-11-12 played floor 1, floor 2 and the finale only.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~VaultsEnemiesResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), FOUNDRY_REALISM (typical; frame-perfect for none), VAULTS_OPTIONS (comma
    // separated option keys; all). Writes vaults-enemies.md and a row a run in vaults-enemies.csv, rewritten after each option.
    [Explicit("The Quicksilver Vaults' own enemies: run it by name.")]
    public sealed class VaultsEnemiesResearch
    {
        private const float BandFrom = 45f;
        private const float BandTo = 65f;

        private sealed class Option
        {
            public string Key;
            public string What;
            public bool Roles;
            public Action<DescentSimulation.FoundryLevers> Set = _ => { };
        }

        private static Action<DescentSimulation.FoundryLevers> Regent(float share, int copies, bool sidesteps) => l =>
        {
            l.VaultsRegent = true;
            l.LaterWardenHealthShare = share;
            l.RegentCopies = copies;
            l.VaultsHabits |= sidesteps ? DescentSimulation.VaultsHabit.Sidesteps : DescentSimulation.VaultsHabit.None;
        };

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "floor 2 as shipped: the Mirror Sentry leads Sentry Hall (MirrorGuardRule, sentry-t45-h125-c2-f50: 45 deg/s, 200 health, half a front landing taken, 20 % reflected up to 2 % of the hero's maximum a second), the careful and first-card pilots flank it (FoundryLevers.FlanksTheSentry); the foundry's roster and the Forge Warden (share 6.071429)", Roles = true };
            yield return new Option { Key = "captain", What = "floor 2 before 2026-11-12: the Grunt Captain leads Sentry Hall (FoundryLevers.VaultsCaptain)", Roles = true, Set = l => l.VaultsCaptain = true };
            yield return new Option { Key = "sentry-noflank", What = "the shipped Mirror Sentry, no pilot flanks: the step-in holds still in front of it", Roles = true, Set = l => l.FlanksTheSentry = false };
            // docs/50 section 5's starting values (round 1): 90 deg/s, 160 health (0.8 of the shipped 200), 35 % taken from the
            // front, the reflection capped at 4 %.
            yield return new Option
            {
                Key = "sentry-docs50", What = "the Mirror Sentry with docs/50's starting values: 90 deg/s, 160 health, 35 % of a front landing taken, its reflection capped at 4 %; the flank habit", Roles = true,
                Set = l =>
                {
                    l.SentryTurnDegrees = 90f;
                    l.SentryHealthScale = .8f;
                    l.SentryFrontShare = .35f;
                    l.SentryReflectCap = .04f;
                }
            };
            // The lead's sweep after the four-floor Descent (2026-11-12): each one lever off the shipped t45-h125-c2-f50, for
            // clumsy completion of the whole Descent at 40 % or more.
            yield return new Option { Key = "sentry-f60", What = "the shipped Mirror Sentry taking 60 % of a front landing", Roles = true, Set = l => l.SentryFrontShare = .6f };
            yield return new Option { Key = "sentry-c1", What = "the shipped Mirror Sentry with its reflection capped at 1 % of the hero's maximum a second", Roles = true, Set = l => l.SentryReflectCap = .01f };
            yield return new Option { Key = "sentry-h100", What = "the shipped Mirror Sentry at docs/50's 160 health (0.8 of the shipped 200)", Roles = true, Set = l => l.SentryHealthScale = .8f };
            yield return new Option { Key = "sentry-r0", What = "the shipped Mirror Sentry reflecting nothing: the guard alone", Roles = true, Set = l => l.SentryReflectShare = 0f };
            yield return new Option { Key = "wisp", What = "the Silver Wisp in place of the Runner: the horde's third role and Mercury Stair's second wave", Roles = true, Set = l => l.VaultsWisps = true };
            yield return new Option { Key = "clerk", What = "the Faceless Clerk (option A, area damage lands) heads Mercury Stair's first wave and replaces a Grunt in Vault Gate's", Roles = true, Set = l => l.VaultsClerks = true };
            // Round 2's alternatives (docs/50 Measured), simulation only.
            yield return new Option
            {
                Key = "wisp-kite", What = "the Silver Wisp blinks only once the hero has walked a straight line for 1.5 s, lands 3 units ahead along it and strikes on landing (no hold)",
                Set = l => { l.VaultsWisps = true; l.WispKiteBlink = true; l.WispArrivalHoldSeconds = 0f; }
            };
            yield return new Option
            {
                Key = "clerk-reach", What = "the Faceless Clerk's copy has no ring or timer: it draws the packs and bursts as the first comes within 1.8 of it (a pack halts at its reach, 1.4-1.6, from its goal), hitting the hero within 2.4 for 10 %; untouched for 3 s it fades",
                Set = l => { l.VaultsClerks = true; l.ClerkReachBlast = true; l.ClerkTouch = 1.8f; }
            };
            yield return new Option { Key = "regent-lanes", What = "the Glass Regent, stage 1: lanes on the Warden's clock, no copies, share 5.2, the sidestep habit", Set = Regent(5.2f, 0, true) };
            // Round 2 (docs/50 Measured): the Sentry's turn, health (of docs/50's 160: 125 % is the shipped 200) and reflection
            // cap, 35 % of a front landing taken as round 2 had it, the flank habit on. The shipped Sentry is t45-h125-c2 with
            // half a front landing taken (`shipped`).
            foreach (int turn in new[] { 45, 52, 60 })
            foreach (int health in new[] { 125, 150 })
            foreach (int cap in new[] { 2, 3 })
                yield return new Option
                {
                    Key = $"sentry-t{turn}-h{health}-c{cap}",
                    What = $"the Mirror Sentry turning {turn} degrees a second, {health} % of docs/50's 160 health, its reflection capped at {cap} % of the hero's maximum a second, 35 % of a front landing taken; the flank habit",
                    Roles = true,
                    Set = l =>
                    {
                        l.SentryTurnDegrees = turn;
                        l.SentryHealthScale = health / 125f;
                        l.SentryReflectCap = cap / 100f;
                        l.SentryFrontShare = .35f;
                    }
                };
            foreach (float share in new[] { 4.6f, 4.8f, 5.0f, 5.2f, 5.4f, 5.6f, 5.8f, 6.2f, 6.4f, 6.6f, 6.7f, 7.0f })
                yield return new Option
                {
                    Key = "regent-" + share.ToString("0.0", CultureInfo.InvariantCulture),
                    What = $"the Glass Regent, lanes and two copies at half health, share {share.ToString("0.0", CultureInfo.InvariantCulture)}, the sidestep habit",
                    Roles = share == 5.2f || share == 6.4f || share == 6.7f || share == 7.0f, Set = Regent(share, 2, true)
                };
            yield return new Option { Key = "regent-5.2-nosidestep", What = "the Glass Regent with copies, share 5.2, no sidestep: the shipped escape out of a circle", Set = Regent(5.2f, 2, false) };
            yield return new Option
            {
                Key = "all", What = "all four together (the shipped Sentry with its flank, Wisp, Clerk, Regent with copies at 5.2 and sidestep)", Roles = true,
                Set = l =>
                {
                    l.VaultsWisps = l.VaultsClerks = true;
                    Regent(5.2f, 2, true)(l);
                }
            };
        }

        private sealed class Cell
        {
            public Option Option;
            public string Cohort;
            public string Hero;
            public DescentSimulation.FoundryHero HeroDefinition;
            public DescentSimulation.FoundryLevers Levers;
            public DescentSimulation.FoundryResult[] Runs;
        }

        [Test]
        public void TheVaultsEnemiesOnFloorTwo()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = List("VAULTS_OPTIONS", string.Join(",", Options().Select(o => o.Key)));
            Option[] options = Options().Where(o => keys.Contains(o.Key)).ToArray();
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var cells = new List<Cell>();
            var roles = new List<(Option Option, FoundryBatch.Batch[] Weapons)>();
            foreach (Option option in options)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Cell carefulVanguard = null;
                foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
                foreach (DescentSimulation.FoundryHero hero in heroes)
                {
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                        .Copy($"cohort {cohort.Name}, {option.Key}, {hands}");
                    option.Set(levers);
                    Cell cell = Play(option, cohort.Name, hero, levers, seeds, parallel);
                    cells.Add(cell);
                    if (cohort == AutopilotCohort.Careful && hero.Name == heroes[0].Name)
                        carefulVanguard = cell;
                }
                if (option.Roles && carefulVanguard != null)
                {
                    var weapons = new List<FoundryBatch.Batch>();
                    foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
                    {
                        if (weapon.Id == DescentSimulation.SwordId)
                        {
                            weapons.Add(Batch(carefulVanguard));
                            continue;
                        }
                        DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
                        hero.Weapon = weapon;
                        hero.Name = "Vanguard + " + weapon.DisplayName;
                        DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(AutopilotCohort.Careful, DescentSimulation.FinaleFloor, realism)
                            .Copy($"starting {weapon.Id}, {option.Key}, {hands}");
                        option.Set(levers);
                        weapons.Add(Batch(Play(option, "careful", hero, levers, seeds, parallel)));
                    }
                    roles.Add((option, weapons.ToArray()));
                }
                TestContext.Progress.WriteLine($"{option.Key}: {watch.Elapsed.TotalSeconds:0} s");
                File.WriteAllText(Path.Combine(output, "vaults-enemies.md"), Report(cells, roles, first, count, hands));
                File.WriteAllText(Path.Combine(output, "vaults-enemies.csv"), Csv(cells, hands));
            }
            TestContext.WriteLine(Report(cells, roles, first, count, hands));
            Assert.That(cells.All(c => c.Runs.Length == count), "every seed ran");
        }

        // The flank habit's own reading: the careful Vanguard started with the Daggers against the Sentry, with and without
        // the habit - the step-in's seconds on floor 2, the Sentry's landings from its front and its time under fire.
        [Test]
        public void TheSentryFlankWithTheDaggers()
        {
            int count = Setting("FOUNDRY_SEEDS", 60);
            int[] seeds = FoundryBatch.Seeds(Setting("FOUNDRY_FIRST_SEED", 1), count);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            foreach (string key in new[] { "shipped", "sentry-noflank" })
            {
                Option option = Options().First(o => o.Key == key);
                DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
                hero.Weapon = DescentSimulation.WeaponCatalogue().First(w => w.DisplayName == "Daggers");
                DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(AutopilotCohort.Careful, DescentSimulation.FinaleFloor, realism).Copy(key);
                option.Set(levers);
                // SENTRY_TURN (degrees a second, the shipped 45 by default) and SENTRY_HEALTH_PERCENT (of the shipped 200, 100)
                // for the habit's sensitivity.
                levers.SentryTurnDegrees = Setting("SENTRY_TURN", (int)MirrorGuardRule.TurnDegrees);
                levers.SentryHealthScale = Setting("SENTRY_HEALTH_PERCENT", 100) / 100f;
                DescentSimulation.FoundryResult[] two = Play(option, "careful", hero, levers, seeds, parallel).Runs
                    .Select(Two).Where(r => r != null && r.SentryLandings > 0).ToArray();
                float[] underFire = two.SelectMany(r => r.Kills).Where(k => !k.Horde && k.Kind == "Mirror Sentry").Select(k => k.UnderFire).ToArray();
                TestContext.WriteLine($"{key} ({pilot}): met {two.Length}, step-in s {Mean(two.Select(r => r.StepInSeconds))}, " +
                                      $"front {Pct(two.Sum(r => r.SentryFrontLandings), two.Sum(r => r.SentryLandings))}, " +
                                      $"under fire P50 {F1(FoundryBatch.Percentile(underFire, 50))} s, reflected {Mean(two.Select(r => r.SentryReflected))}, cleared {two.Count(r => r.Cleared)}");
            }
        }

        private static Cell Play(Option option, string cohort, DescentSimulation.FoundryHero hero, DescentSimulation.FoundryLevers levers,
            int[] seeds, int parallel)
        {
            var runs = new DescentSimulation.FoundryResult[seeds.Length];
            Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) },
                i => runs[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers, DescentSimulation.VaultsChain(levers)));
            return new Cell { Option = option, Cohort = cohort, Hero = hero.Name, HeroDefinition = hero, Levers = levers, Runs = runs };
        }

        private static FoundryBatch.Batch Batch(Cell cell) =>
            new FoundryBatch.Batch { Hero = cell.HeroDefinition, Levers = cell.Levers, Runs = cell.Runs };

        private static DescentSimulation.FoundryResult Two(DescentSimulation.FoundryResult run) => run.NextFloor;

        private static DescentSimulation.FoundryResult Finale(DescentSimulation.FoundryResult run) => FoundryBatch.FinaleOf(run);

        private static float Descent(DescentSimulation.FoundryResult run)
        {
            float sum = 0f;
            for (DescentSimulation.FoundryResult r = run; r != null; r = r.NextFloor)
                sum += r.FloorSeconds;
            return sum;
        }

        private static bool Completed(DescentSimulation.FoundryResult run) => Finale(run) is DescentSimulation.FoundryResult f && f.WalkedOut;

        private static string Report(List<Cell> cells, List<(Option Option, FoundryBatch.Batch[] Weapons)> roles, int first, int count, string hands)
        {
            var md = new StringBuilder();
            md.AppendLine($"# The Quicksilver Vaults' own enemies: seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("Each run plays floor 1 as shipped, floor 2 with the option's creatures and the finale. Went down: floor 1's clears. Cleared: floor 2, of those that went down. " +
                          "Deaths: floor 2's, by room (1-3 halls, 5 the Sentry Hall, boss the gate) and the killers. Gate: floor 2's gate boss's fight (the Warden or the Regent), P50 / P90 over the fights it fell, the share in 45-65 s and how many fell within 5 s. " +
                          "Descent: completed (walked out of the finale) and P50 minutes of those. Creature: the option's own reading, per floor 2 that met it.").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Went down | Cleared | Deaths | Killers | Gate P50 / P90 (in band; within 5 s) | Floor 2 P50 s | Completed | Descent P50 | Creature |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Cell c in cells)
                md.AppendLine(Row(c));
            md.AppendLine();
            if (roles.Count > 0)
            {
                md.AppendLine("## Floor 2's roles row (careful hands; the Vanguard started with each weapon)").AppendLine();
                md.AppendLine("A starting weapon's own damage a second on floor 2: the horde (a second held), the elite room's leader (a second of the elite fight), the elite room's whole wave, the gate boss (a second of its fight). " +
                              "Roles: each weapon's lead job (FoundryGate.LeadsJob) over horde / elite leader / gate boss. " +
                              "Leader under fire: P50 s from its first damage to its fall; front: the Sentry's landings made from its front arc; gate fight: P50 s; floor 2 cleared of those that went down.").AppendLine();
                md.AppendLine("| Option | Horde: Sword / Staff / Daggers | Elite leader | Elite waves | Gate boss | Roles | Sword's lead on the gate boss | Daggers' lead on the elite leader | Leader under fire P50 s | Sentry front | Gate fight P50 s | Floor 2 cleared |");
                md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
                foreach ((Option option, FoundryBatch.Batch[] weapons) in roles)
                {
                    var two = weapons.Select(FoundryBatch.FloorTwo).ToArray();
                    var jobs = two.Select(b => (Name: b.Hero.Weapon.DisplayName, Jobs: FoundryBatch.RoleJobsOf(b, true))).ToArray();
                    var waves = two.Select(b => FoundryBatch.RoleJobsOf(b)[1]).ToArray();
                    string Three(int job) => string.Join(" / ", jobs.Select(j => F1(j.Jobs[job])));
                    md.AppendLine($"| `{option.Key}` | {Three(0)} | {Three(1)} | {string.Join(" / ", waves.Select(F1))} | {Three(2)} | {FoundryBatch.Roles(jobs)} | " +
                                  $"{Lead(jobs, "Sword", 2)} | {Lead(jobs, "Daggers", 1)} | " +
                                  $"{string.Join(" / ", two.Select(b => F1(FoundryBatch.Percentile(b.Runs.SelectMany(r => r.Kills).Where(k => !k.Horde && (k.Kind == "Mirror Sentry" || k.Kind == "Grunt Captain")).Select(k => k.UnderFire), 50))))} | " +
                                  $"{string.Join(" / ", two.Select(b => Pct(b.Runs.Sum(r => r.SentryFrontLandings), b.Runs.Sum(r => r.SentryLandings))))} | " +
                                  $"{string.Join(" / ", two.Select(b => F(FoundryBatch.Percentile(b.Runs.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds), 50))))} | " +
                                  $"{string.Join(" / ", two.Select(b => b.Runs.Count(r => r.Cleared) + "/" + b.Runs.Length))} |");
                }
                md.AppendLine();
            }
            md.AppendLine("## The options").AppendLine();
            foreach (Option option in cells.Select(c => c.Option).Distinct())
                md.AppendLine($"- `{option.Key}`: {option.What}.");
            return md.ToString();
        }

        private static string Lead(IReadOnlyList<(string Name, double[] Jobs)> jobs, string name, int job)
        {
            double own = jobs.First(j => j.Name == name).Jobs[job];
            double best = jobs.Where(j => j.Name != name).Max(j => j.Jobs[job]);
            return best > 0 && !double.IsNaN(own) ? (100.0 * (own / best - 1.0)).ToString("0", CultureInfo.InvariantCulture) + " %" : "-";
        }

        private static string Row(Cell c)
        {
            DescentSimulation.FoundryResult[] down = c.Runs.Select(Two).Where(r => r != null).ToArray();
            DescentSimulation.FoundryResult[] died = down.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            int cleared = down.Count(r => r.Cleared);
            string rooms = died.Length == 0 ? "-" : string.Join(", ", died.GroupBy(Room).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} x{g.Count()}"));
            string killers = died.Length == 0 ? "-" : string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(4).Select(g => $"{g.Key} {g.Count()}"));
            float[] fights = down.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds).ToArray();
            int inBand = fights.Count(s => s >= BandFrom && s <= BandTo);
            int quick = fights.Count(s => s <= 5f);
            int completed = c.Runs.Count(Completed);
            return $"| `{c.Option.Key}` | {c.Cohort} | {c.Hero} | {down.Length} | {cleared} ({Pct(cleared, down.Length)}) | {died.Length}: {rooms} | {killers} | " +
                   $"{F(FoundryBatch.Percentile(fights, 50))} / {F(FoundryBatch.Percentile(fights, 90))} ({Pct(inBand, fights.Length)}; {quick}) | " +
                   $"{F(FoundryBatch.Percentile(down.Where(r => r.Cleared).Select(r => r.FloorSeconds), 50))} | {completed} ({Pct(completed, c.Runs.Length)}) | " +
                   $"{M(FoundryBatch.Percentile(c.Runs.Where(Completed).Select(Descent), 50))} | {Creature(c, down)} |";
        }

        // The option's own reading over the floor 2s that met it.
        private static string Creature(Cell c, DescentSimulation.FoundryResult[] down)
        {
            var l = c.Levers;
            var parts = new List<string>();
            if (!l.VaultsCaptain)
            {
                var met = down.Where(r => r.SentryLandings > 0).ToArray();
                int landings = met.Sum(r => r.SentryLandings), front = met.Sum(r => r.SentryFrontLandings);
                float[] underFire = met.SelectMany(r => r.Kills).Where(k => !k.Horde && k.Kind == "Mirror Sentry").Select(k => k.UnderFire).ToArray();
                float[] fight = met.Where(r => r.EliteFought > 0f).Select(r => r.EliteFought).ToArray();
                parts.Add($"Sentry: front {Pct(front, landings)}, reflected {Mean(met.Select(r => r.SentryReflected))}, under fire P50 {F1(FoundryBatch.Percentile(underFire, 50))} s, elite fight P50 {F1(FoundryBatch.Percentile(fight, 50))} s");
            }
            if (l.VaultsWisps)
                parts.Add($"Wisp: blinks {Mean(down.Select(r => (float)r.WispBlinks))}, landed in reach {down.Sum(r => r.WispArrivalsInReach)}, taken a minute {Mean(down.Where(r => r.FloorSeconds > 0f).Select(r => 60f * r.TakenFromWisps / r.FloorSeconds))}");
            if (l.VaultsClerks)
            {
                float[] underFire = down.SelectMany(r => r.Kills).Where(k => !k.Horde && k.Kind == "Faceless Clerk").Select(k => k.UnderFire).ToArray();
                parts.Add($"Clerk: copies {Mean(down.Select(r => (float)r.ClerkCopies))}, shatter hits {Mean(down.Select(r => (float)r.ClerkCopyHits))}, drawn {Mean(down.Select(r => r.DrawnSeconds))} s, under fire P50 {F1(FoundryBatch.Percentile(underFire, 50))} s" + (l.ClerkReachBlast ? $", bursts {Mean(down.Select(r => (float)r.ClerkBursts))}, fades {Mean(down.Select(r => (float)r.ClerkFades))}" : string.Empty));
            }
            if (l.VaultsRegent)
            {
                var met = down.Where(r => r.RegentBeats > 0).ToArray();
                parts.Add($"Regent: lane hits a fight {Mean(met.Select(r => (float)r.RegentLaneHits))} of {Mean(met.Select(r => (float)r.RegentBeats))} beats, copies shattered {Mean(met.Select(r => (float)r.RegentCopiesShattered))}, phase 2 at P50 {F(FoundryBatch.Percentile(met.Where(r => r.RegentPhaseTwoAt >= 0f).Select(r => r.RegentPhaseTwoAt), 50))} s");
            }
            return parts.Count == 0 ? "-" : string.Join("; ", parts);
        }

        private static string Room(DescentSimulation.FoundryResult run) =>
            run.DeathRoom < 1 ? "descent"
            : run.DeathRoom <= run.Kinds.Length && run.Kinds[run.DeathRoom - 1] == Cryptforge.Combat.RoomKind.Boss ? "boss"
            : "room " + run.DeathRoom.ToString(CultureInfo.InvariantCulture);

        private static string Csv(List<Cell> cells, string hands)
        {
            var csv = new StringBuilder();
            csv.AppendLine("realism,option,cohort,hero,seed,f1_outcome,f2_outcome,f2_s,death_room,killer,gate_s,sentry_landings,sentry_front,sentry_reflected," +
                           "wisp_blinks,wisp_in_reach,taken_wisps,clerk_copies,clerk_hits,drawn_s,regent_beats,regent_lane_hits,copies_shattered,finale_outcome,descent_s");
            foreach (Cell cell in cells)
            foreach (DescentSimulation.FoundryResult run in cell.Runs)
            {
                DescentSimulation.FoundryResult t = Two(run);
                DescentSimulation.FoundryResult f = Finale(run);
                if (t == null)
                {
                    csv.AppendLine(string.Join(",", hands, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, run.Outcome) + new string(',', 19));
                    continue;
                }
                csv.AppendLine(string.Join(",", hands, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, run.Outcome, t.Outcome, Num(t.FloorSeconds),
                    t.Outcome == DescentSimulation.FoundryOutcome.Died ? Room(t) : string.Empty, Quote(t.Killer), Num(t.WardenSeconds),
                    t.SentryLandings, t.SentryFrontLandings, Num(t.SentryReflected), t.WispBlinks, t.WispArrivalsInReach, Num(t.TakenFromWisps),
                    t.ClerkCopies, t.ClerkCopyHits, Num(t.DrawnSeconds), t.RegentBeats, t.RegentLaneHits, t.RegentCopiesShattered,
                    f != null ? (f.WalkedOut ? "WalkedOut" : f.Outcome.ToString()) : string.Empty, Completed(run) ? Num(Descent(run)) : string.Empty));
            }
            return csv.ToString();
        }

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string Num(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string F(double value) => double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

        private static string F1(double value) => double.IsNaN(value) ? "-" : value.ToString("0.0", CultureInfo.InvariantCulture);

        private static string M(double seconds) => double.IsNaN(seconds) ? "-" : (seconds / 60.0).ToString("0.0", CultureInfo.InvariantCulture);

        private static string Mean(IEnumerable<float> values)
        {
            float[] list = values.ToArray();
            return list.Length == 0 ? "-" : list.Average().ToString("0.0", CultureInfo.InvariantCulture);
        }

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
