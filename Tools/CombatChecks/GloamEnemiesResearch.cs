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
    // The Gloam Gardens' own enemies (docs/52 section 8, step 1; the lead's decisions 2026-11-12: the Cinder Bloom's research
    // first). Plays every cohort and both heroes down the whole Descent (floors 1-3 and the finale) on the same seeds with
    // human hands, once as shipped - floor 3's Thorn Post led by the Grunt Captain, floor 2's Sentry Hall by the shipped
    // Mirror Sentry - and once for each option: the Cinder Bloom in the Captain's place (DescentSimulation.GloamChain) at
    // its starting values or a swept variant, or either with floor 2's Grunt Captain back (`-f2c`, VaultsCaptain: floor 2
    // before the Sentry, so the Bloom's own cost reads apart from the Sentry's). Where an option reads the roles, the
    // careful Vanguard is also started with the Staff and with the Daggers beside its Sword. No pilot habit: the Bloom
    // asks for none.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~GloamEnemiesResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), FOUNDRY_REALISM (typical; slow; frame-perfect for none), GLOAM_OPTIONS
    // (comma separated option keys; all), GLOAM_COHORTS (comma separated cohort names; all five), GLOAM_ROLES (0 skips the
    // roles row). Writes gloam-enemies.md and a row a run in gloam-enemies.csv, rewritten after each option.
    [Explicit("The Gloam Gardens' own enemies: run it by name.")]
    public sealed class GloamEnemiesResearch
    {
        private const float BandFrom = 45f;
        private const float BandTo = 65f;
        // Thorn Post: floor 3's fifth room (Ash Orchard, Rootbound Walk, Bloom Gate, the Grafting Forge, Thorn Post, the Grove).
        private const int ThornPost = 5;

        private sealed class Option
        {
            public string Key;
            public string What;
            public bool Roles;
            public Action<DescentSimulation.FoundryLevers> Set = _ => { };
        }

        private static Action<DescentSimulation.FoundryLevers> Bloom(Action<DescentSimulation.FoundryLevers> more = null) => l =>
        {
            l.GloamBloom = true;
            more?.Invoke(l);
        };

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "floor 3 as shipped: the Grunt Captain leads Thorn Post; floor 2's Sentry Hall the shipped Mirror Sentry (sentry-t45-h125-c2-f50)", Roles = true };
            yield return new Option { Key = "bloom", What = "the Cinder Bloom leads Thorn Post at docs/52's starting values: 280 health (549 on floor 3), Bark Maul 10.8 / 1.8 s at reach 1.5 (0.6 s), speed 1.2; closed 3.5 s taking 25 %, open 2.5 s rooted and taking 100 %", Roles = true, Set = Bloom() };
            yield return new Option { Key = "shipped-f2c", What = "`shipped` with floor 2's Grunt Captain back in Sentry Hall (VaultsCaptain: floor 2 before 2026-11-12)", Set = l => l.VaultsCaptain = true };
            yield return new Option { Key = "bloom-f2c", What = "`bloom` with floor 2's Grunt Captain back in Sentry Hall", Set = Bloom(l => l.VaultsCaptain = true) };
            // docs/52 section 5's sweep and section 8's fallbacks, one lever at a time off the starting values.
            foreach (int health in new[] { 60, 80, 125 })
                yield return new Option { Key = $"bloom-h{health}", What = $"the Bloom at {health} % of its 280 health", Roles = true, Set = Bloom(l => l.BloomHealthScale = health / 100f) };
            foreach (int maul in new[] { 75, 125 })
                yield return new Option { Key = $"bloom-m{maul}", What = $"the Bloom's maul at {maul} % of 10.8", Roles = true, Set = Bloom(l => l.BloomMaulScale = maul / 100f) };
            foreach (float open in new[] { 2.0f, 3.0f })
                yield return new Option { Key = "bloom-o" + open.ToString("0.0", CultureInfo.InvariantCulture), What = $"the Bloom open {open.ToString("0.0", CultureInfo.InvariantCulture)} s", Roles = true, Set = Bloom(l => l.BloomOpen = open) };
            foreach (float closed in new[] { 3.0f, 4.0f })
                yield return new Option { Key = "bloom-c" + closed.ToString("0.0", CultureInfo.InvariantCulture), What = $"the Bloom closed {closed.ToString("0.0", CultureInfo.InvariantCulture)} s", Roles = true, Set = Bloom(l => l.BloomClosed = closed) };
            foreach (int bark in new[] { 15, 35, 50 })
                yield return new Option { Key = $"bloom-b{bark}", What = $"the closed bark letting {bark} % through", Roles = true, Set = Bloom(l => l.BloomBark = bark / 100f) };
            yield return new Option { Key = "bloom-r1.4", What = "the Bloom's maul reaching 1.4", Roles = true, Set = Bloom(l => l.BloomMaulReach = 1.4f) };
            // Combinations, for a variant that meets several rows at once (read after the single levers).
            yield return new Option { Key = "bloom-h80-m125", What = "the Bloom at 80 % health with its maul at 125 %", Roles = true, Set = Bloom(l => { l.BloomHealthScale = .8f; l.BloomMaulScale = 1.25f; }) };
            yield return new Option { Key = "bloom-h60-m125", What = "the Bloom at 60 % health with its maul at 125 %", Roles = true, Set = Bloom(l => { l.BloomHealthScale = .6f; l.BloomMaulScale = 1.25f; }) };
            yield return new Option { Key = "bloom-h80-m75", What = "the Bloom at 80 % health with its maul at 75 %", Roles = true, Set = Bloom(l => { l.BloomHealthScale = .8f; l.BloomMaulScale = .75f; }) };
            yield return new Option { Key = "bloom-h60-m75", What = "the Bloom at 60 % health with its maul at 75 %", Roles = true, Set = Bloom(l => { l.BloomHealthScale = .6f; l.BloomMaulScale = .75f; }) };
            yield return new Option { Key = "bloom-h60", What = "the Bloom at 60 % health", Roles = true, Set = Bloom(l => l.BloomHealthScale = .6f) };
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
        public void TheGloamEnemiesOnFloorThree()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            bool readRoles = Setting("GLOAM_ROLES", 1) != 0;
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = List("GLOAM_OPTIONS", string.Join(",", Options().Select(o => o.Key)));
            Option[] options = keys.Select(k => Options().FirstOrDefault(o => o.Key == k)).Where(o => o != null).ToArray();
            Assert.That(options.Length, Is.EqualTo(keys.Length), "every option named exists");
            string[] cohorts = List("GLOAM_COHORTS", string.Join(",", AutopilotCohort.Cohorts.Select(c => c.Name)));
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
                foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts.Where(c => cohorts.Contains(c.Name)))
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
                if (readRoles && option.Roles && carefulVanguard != null)
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
                File.WriteAllText(Path.Combine(output, "gloam-enemies.md"), Report(cells, roles, first, count, hands));
                File.WriteAllText(Path.Combine(output, "gloam-enemies.csv"), Csv(cells, hands));
            }
            TestContext.WriteLine(Report(cells, roles, first, count, hands));
            Assert.That(cells.All(c => c.Runs.Length == count), "every seed ran");
        }

        private static Cell Play(Option option, string cohort, DescentSimulation.FoundryHero hero, DescentSimulation.FoundryLevers levers,
            int[] seeds, int parallel)
        {
            var runs = new DescentSimulation.FoundryResult[seeds.Length];
            Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) },
                i => runs[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers, DescentSimulation.GloamChain(levers)));
            return new Cell { Option = option, Cohort = cohort, Hero = hero.Name, HeroDefinition = hero, Levers = levers, Runs = runs };
        }

        // A cell's floor 3: the floor-3 results of its runs that cleared floor 2 and went down, as a batch of its own.
        private static FoundryBatch.Batch Batch(Cell cell) =>
            new FoundryBatch.Batch { Hero = cell.HeroDefinition, Levers = cell.Levers, Runs = cell.Runs };

        private static FoundryBatch.Batch ThreeOf(FoundryBatch.Batch b) => new FoundryBatch.Batch
        {
            Hero = b.Hero, Levers = b.Levers, WallSeconds = b.WallSeconds, Runs = b.Runs.Select(Three).Where(r => r != null).ToArray()
        };

        private static DescentSimulation.FoundryResult Two(DescentSimulation.FoundryResult run) => run.NextFloor;

        private static DescentSimulation.FoundryResult Three(DescentSimulation.FoundryResult run)
        {
            DescentSimulation.FoundryResult three = run.NextFloor?.NextFloor;
            return three != null && !FoundryBatch.IsFinale(three) ? three : null;
        }

        private static DescentSimulation.FoundryResult Finale(DescentSimulation.FoundryResult run) => FoundryBatch.FinaleOf(run);

        private static float Descent(DescentSimulation.FoundryResult run)
        {
            float sum = 0f;
            for (DescentSimulation.FoundryResult r = run; r != null; r = r.NextFloor)
                sum += r.FloorSeconds;
            return sum;
        }

        private static bool Completed(DescentSimulation.FoundryResult run) => Finale(run) is DescentSimulation.FoundryResult f && f.WalkedOut;

        // Floor 3's elite leaders felled (one a floor: floor 3 has no extra elite).
        private static IEnumerable<DescentSimulation.EliteFight> Fights(IEnumerable<DescentSimulation.FoundryResult> floors) =>
            floors.Where(r => r.EliteFights != null).SelectMany(r => r.EliteFights);

        // The floor 3s that met Thorn Post (its wave stood with the hero alive).
        private static DescentSimulation.FoundryResult[] MetThornPost(IEnumerable<DescentSimulation.FoundryResult> floors) =>
            floors.Where(r => r.EliteFought > 0f).ToArray();

        private static bool DiedAtThornPost(DescentSimulation.FoundryResult r) =>
            r.Outcome == DescentSimulation.FoundryOutcome.Died && r.DeathRoom == ThornPost;

        private static string Report(List<Cell> cells, List<(Option Option, FoundryBatch.Batch[] Weapons)> roles, int first, int count, string hands)
        {
            var md = new StringBuilder();
            md.AppendLine($"# The Gloam Gardens' own enemies: seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("Each run plays floors 1 and 2 as the option has them, floor 3 with the option's creatures and the finale. Went down: floor 2's clears. Cleared: floor 3, of those that went down. " +
                          "Deaths: floor 3's, by room (1-3 halls, 5 Thorn Post, boss the Grove) and the killers. Elite fight: Thorn Post's leader from walking in to falling, P50 / P90 s; " +
                          "in reach: of it, the seconds it stood within the hero's longest weapon's reach, P50; D: its health over those seconds, P50 (docs/52 section 5). " +
                          "Thorn Post taken: health the hero lost while its wave stood, mean over the floor 3s that met it (from the leader / its escort / the horde). " +
                          "Windows: the Bloom's open windows begun by its fall, 1 / 2 / 3+ (fell open). Gate: floor 3's Warden fight P50 (share in 45-65 s). Completed: walked out of the finale.").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Went down | Cleared | Deaths | Thorn Post deaths | Killers | Elite fight P50 / P90 s | In reach P50 s | D P50 | Thorn Post taken (leader / escort / horde) | Windows | Gate P50 (in band) | Floor 3 P50 s | Completed | Descent P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Cell c in cells)
                md.AppendLine(Row(c));
            md.AppendLine();

            md.AppendLine("## By option (Vanguard / Ilya)").AppendLine();
            md.AppendLine("Floor-3 clears of those that went down; Thorn Post taken: mean health lost while its wave stood; completion: walked out of the finale, pooled over both heroes.").AppendLine();
            string[] names = cells.Select(c => c.Cohort).Distinct().ToArray();
            md.AppendLine("| Option | " + string.Join(" | ", names.Select(n => n + " F3 cleared")) + " | " + string.Join(" | ", names.Select(n => n + " completed")) +
                          " | Elite fight P50 careful / standing | Thorn Post taken careful / standing / clumsy | Standing / careful taken |");
            md.AppendLine("|---|" + string.Concat(Enumerable.Repeat("---|", names.Length * 2)) + "---|---|---|");
            foreach (Option option in cells.Select(c => c.Option).Distinct())
            {
                Cell[] mine = cells.Where(c => c.Option == option).ToArray();
                string Pair(string cohort, Func<Cell, string> read) =>
                    string.Join(" / ", mine.Where(c => c.Cohort == cohort).Select(read));
                string Pooled(string cohort)
                {
                    Cell[] two = mine.Where(c => c.Cohort == cohort).ToArray();
                    int done = two.Sum(c => c.Runs.Count(Completed)), all = two.Sum(c => c.Runs.Length);
                    return all == 0 ? "-" : $"{done} ({(100.0 * done / all).ToString("0.0", CultureInfo.InvariantCulture)} %)";
                }
                double Taken(string cohort)
                {
                    var met = MetThornPost(mine.Where(c => c.Cohort == cohort).SelectMany(c => c.Runs.Select(Three).Where(r => r != null)));
                    return met.Length == 0 ? double.NaN : met.Average(r => r.EliteTaken);
                }
                string Fight(string cohort) =>
                    F1(FoundryBatch.Percentile(Fights(mine.Where(c => c.Cohort == cohort).SelectMany(c => c.Runs.Select(Three).Where(r => r != null))).Select(f => f.Lifetime), 50));
                double careful = Taken("careful"), standing = Taken("standing");
                md.AppendLine($"| `{option.Key}` | " +
                              string.Join(" | ", names.Select(n => Pair(n, c => c.Runs.Select(Three).Count(r => r != null && r.Cleared).ToString(CultureInfo.InvariantCulture)))) + " | " +
                              string.Join(" | ", names.Select(Pooled)) + $" | {Fight("careful")} / {Fight("standing")} | " +
                              $"{F1(careful)} / {F1(standing)} / {F1(Taken("clumsy"))} | {(careful > 0 ? (standing / careful).ToString("0.00", CultureInfo.InvariantCulture) + "x" : "-")} |");
            }
            md.AppendLine();

            if (roles.Count > 0)
            {
                md.AppendLine("## Floor 3's roles row (careful hands; the Vanguard started with each weapon)").AppendLine();
                md.AppendLine("A starting weapon's own damage a second on floor 3: the horde (a second held), the elite room's leader (a second of the elite fight), the gate boss (a second of its fight). " +
                              "Roles: each weapon's lead job (FoundryGate.LeadsJob) over horde / elite leader / gate boss. Elite fight: Thorn Post's leader walk-in to fall P50 s; D: its health over its seconds in reach, P50.").AppendLine();
                md.AppendLine("| Option | Horde: Sword / Staff / Daggers | Elite leader | Gate boss | Roles | Staff's lead on the horde | Daggers' lead on the elite leader | Sword's lead on the gate boss | Elite fight P50 s | D P50 | Gate fight P50 s | Floor 3 cleared |");
                md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
                foreach ((Option option, FoundryBatch.Batch[] weapons) in roles)
                {
                    var three = weapons.Select(ThreeOf).ToArray();
                    var jobs = three.Select(b => (Name: b.Hero.Weapon.DisplayName, Jobs: FoundryBatch.RoleJobsOf(b, true))).ToArray();
                    string Three3(int job) => string.Join(" / ", jobs.Select(j => F1(j.Jobs[job])));
                    md.AppendLine($"| `{option.Key}` | {Three3(0)} | {Three3(1)} | {Three3(2)} | {FoundryBatch.Roles(jobs)} | " +
                                  $"{Lead(jobs, "Staff", 0)} | {Lead(jobs, "Daggers", 1)} | {Lead(jobs, "Sword", 2)} | " +
                                  $"{string.Join(" / ", three.Select(b => F1(FoundryBatch.Percentile(Fights(b.Runs).Select(f => f.Lifetime), 50))))} | " +
                                  $"{string.Join(" / ", three.Select(b => F1(FoundryBatch.Percentile(Fights(b.Runs).Select(f => f.DamageInReach).Where(d => !float.IsNaN(d)), 50))))} | " +
                                  $"{string.Join(" / ", three.Select(b => F(FoundryBatch.Percentile(b.Runs.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds), 50))))} | " +
                                  $"{string.Join(" / ", three.Select(b => b.Runs.Count(r => r.Cleared) + "/" + b.Runs.Length))} |");
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
            DescentSimulation.FoundryResult[] down = c.Runs.Select(Three).Where(r => r != null).ToArray();
            DescentSimulation.FoundryResult[] died = down.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            int cleared = down.Count(r => r.Cleared);
            string rooms = died.Length == 0 ? "-" : string.Join(", ", died.GroupBy(Room).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} x{g.Count()}"));
            string killers = died.Length == 0 ? "-" : string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(4).Select(g => $"{g.Key} {g.Count()}"));
            DescentSimulation.EliteFight[] fights = Fights(down).ToArray();
            DescentSimulation.FoundryResult[] met = MetThornPost(down);
            string taken = met.Length == 0 ? "-"
                : $"{F1(met.Average(r => r.EliteTaken))} ({F1(met.Average(r => r.EliteTakenFromLeader))} / {F1(met.Average(r => r.EliteTakenFromEscort))} / {F1(met.Average(r => r.EliteTakenFromHorde))})";
            string windows = !c.Levers.GloamBloom || fights.Length == 0 ? "-"
                : $"{fights.Count(f => f.Windows <= 1)} / {fights.Count(f => f.Windows == 2)} / {fights.Count(f => f.Windows >= 3)} ({Pct(fights.Count(f => f.FellOpen), fights.Length)})";
            float[] gate = down.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds).ToArray();
            int completed = c.Runs.Count(Completed);
            return $"| `{c.Option.Key}` | {c.Cohort} | {c.Hero} | {down.Length} | {cleared} ({Pct(cleared, down.Length)}) | {died.Length}: {rooms} | {died.Count(DiedAtThornPost)} | {killers} | " +
                   $"{F1(FoundryBatch.Percentile(fights.Select(f => f.Lifetime), 50))} / {F1(FoundryBatch.Percentile(fights.Select(f => f.Lifetime), 90))} | " +
                   $"{F1(FoundryBatch.Percentile(fights.Select(f => f.InReach), 50))} | {F1(FoundryBatch.Percentile(fights.Select(f => f.DamageInReach).Where(d => !float.IsNaN(d)), 50))} | " +
                   $"{taken} | {windows} | {F(FoundryBatch.Percentile(gate, 50))} ({Pct(gate.Count(s => s >= BandFrom && s <= BandTo), gate.Length)}) | " +
                   $"{F(FoundryBatch.Percentile(down.Where(r => r.Cleared).Select(r => r.FloorSeconds), 50))} | {completed} ({Pct(completed, c.Runs.Length)}) | " +
                   $"{M(FoundryBatch.Percentile(c.Runs.Where(Completed).Select(Descent), 50))} |";
        }

        private static string Room(DescentSimulation.FoundryResult run) =>
            run.DeathRoom < 1 ? "descent"
            : run.DeathRoom <= run.Kinds.Length && run.Kinds[run.DeathRoom - 1] == Cryptforge.Combat.RoomKind.Boss ? "boss"
            : "room " + run.DeathRoom.ToString(CultureInfo.InvariantCulture);

        private static string Csv(List<Cell> cells, string hands)
        {
            var csv = new StringBuilder();
            csv.AppendLine("realism,option,cohort,hero,seed,f1_outcome,f2_outcome,f3_outcome,f3_s,death_room,killer,elite_kind,elite_fight_s,elite_in_reach_s,elite_windows,elite_fell_open," +
                           "thorn_post_taken,taken_leader,taken_escort,taken_horde,bloom_closed_landings,bloom_open_landings,bloom_barked,gate_s,finale_outcome,descent_s");
            foreach (Cell cell in cells)
            foreach (DescentSimulation.FoundryResult run in cell.Runs)
            {
                DescentSimulation.FoundryResult two = Two(run);
                DescentSimulation.FoundryResult t = Three(run);
                DescentSimulation.FoundryResult f = Finale(run);
                string head = string.Join(",", hands, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, run.Outcome, two != null ? two.Outcome.ToString() : string.Empty);
                if (t == null)
                {
                    csv.AppendLine(head + new string(',', 19));
                    continue;
                }
                DescentSimulation.EliteFight? fight = t.EliteFights != null && t.EliteFights.Count > 0 ? t.EliteFights[0] : (DescentSimulation.EliteFight?)null;
                csv.AppendLine(string.Join(",", head, t.Outcome, Num(t.FloorSeconds),
                    t.Outcome == DescentSimulation.FoundryOutcome.Died ? Room(t) : string.Empty, Quote(t.Killer),
                    fight?.Kind ?? string.Empty, fight != null ? Num(fight.Value.Lifetime) : string.Empty, fight != null ? Num(fight.Value.InReach) : string.Empty,
                    fight?.Windows.ToString(CultureInfo.InvariantCulture) ?? string.Empty, fight != null ? (fight.Value.FellOpen ? "1" : "0") : string.Empty,
                    Num(t.EliteTaken), Num(t.EliteTakenFromLeader), Num(t.EliteTakenFromEscort), Num(t.EliteTakenFromHorde),
                    t.BloomClosedLandings, t.BloomOpenLandings, Num(t.BloomBarked), Num(t.WardenSeconds),
                    f != null ? (f.WalkedOut ? "WalkedOut" : f.Outcome.ToString()) : string.Empty, Completed(run) ? Num(Descent(run)) : string.Empty));
            }
            return csv.ToString();
        }

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string Num(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string F(double value) => double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

        private static string F1(double value) => double.IsNaN(value) ? "-" : value.ToString("0.0", CultureInfo.InvariantCulture);

        private static string M(double seconds) => double.IsNaN(seconds) ? "-" : (seconds / 60.0).ToString("0.0", CultureInfo.InvariantCulture);

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
