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
    // The first-card cohort on floor 2 (docs/47, research 2026-11-11 night). With floor 2's gate at 3:30 (docs/47's build)
    // every cohort's floor 2 got safer but the first-card one, whose floor-2 deaths went from 6 to 16. This plays every
    // cohort and both heroes down the whole Descent (floor 1, floor 2, the finale) on the same seeds with human hands, once
    // as shipped, once with floor 2's gate as its room clears (before the build), and once a candidate lever, each on the
    // floors after the first only, so floor 1 is the same run in every option:
    //
    // - floor 2's Warden share (FoundryLevers.LaterWardenHealthShare against the asset's 6.071429);
    // - floor 2's horde stops hardening at a clock (FoundryLevers.LaterHordeClockCap; no overrun on floor 2);
    // - floor 2's Deep Forge Mend (FoundryLevers.LaterMendAmount against Forge_Mend's 0.4);
    // - the gate room's chest a heal under the hero's feet as the Warden walks out (FoundryLevers.LaterGateChestHeal).
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~FirstCardFloorTwoResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), FOUNDRY_REALISM (typical), FIRSTCARD_OPTIONS (comma separated option keys;
    // all). Writes first-card-floor2.md and a row a run's floor 2 in first-card-floor2.csv, both rewritten after each option.
    [Explicit("The first-card cohort on floor 2: run it by name.")]
    public sealed class FirstCardFloorTwoResearch
    {
        // docs/41 step 4's band for a Warden's fight, in seconds.
        private const float WardenBandFrom = 45f;
        private const float WardenBandTo = 65f;

        private sealed class Option
        {
            public string Key;
            public string What;
            public float FloorTwoGate = 210f;
            public Action<DescentSimulation.FoundryLevers> Set = _ => { };
        }

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "as shipped since docs/47's build: floor 2's gate at 3:30, the finale's chest where the hero arrives" };
            yield return new Option { Key = "gate0", What = "floor 2's gate as its room clears, as before docs/47's build (the finale's chest kept)", FloorTwoGate = 0f };
            yield return new Option { Key = "warden85", What = "floor 2's Warden at 85 % of its health (share 5.160715 against the asset's 6.071429)", Set = l => l.LaterWardenHealthShare = 5.160715f };
            yield return new Option { Key = "warden75", What = "floor 2's Warden at 75 % of its health (share 4.553572)", Set = l => l.LaterWardenHealthShare = 4.553572f };
            yield return new Option { Key = "horde180", What = "floor 2's horde stops hardening at 3:00 on its clock (its health, damage and experience as at 180 s; no overrun)", Set = l => l.LaterHordeClockCap = 180f };
            yield return new Option { Key = "horde210", What = "floor 2's horde stops hardening at 3:30, as the Warden walks out (no overrun)", Set = l => l.LaterHordeClockCap = 210f };
            yield return new Option { Key = "mend70", What = "floor 2's Deep Forge Mend heals 70 % (Forge_Mend's 40 %)", Set = l => l.LaterMendAmount = .7f };
            yield return new Option { Key = "gatechest", What = "floor 2's gate room chest a quarter heal under the hero's feet as the Warden walks out (a gold chest at ChestRule's spot today)", Set = l => l.LaterGateChestHeal = .25f };
        }

        private static DescentSimulation.FoundryFloor Chain(Option option)
        {
            DescentSimulation.FoundryFloor one = DescentSimulation.FoundryFloorOne();
            one.Next.GateNotBeforeSeconds = option.FloorTwoGate;
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
        public void TheFirstCardCohortOnFloorTwo()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = List("FIRSTCARD_OPTIONS", string.Join(",", Options().Select(o => o.Key)));
            Option[] options = Options().Where(o => keys.Contains(o.Key)).ToArray();
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var cells = new List<Cell>();
            foreach (Option option in options)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
                foreach (DescentSimulation.FoundryHero hero in heroes)
                {
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, 3, realism)
                        .Copy($"cohort {cohort.Name}, {option.Key}" + (realism != null ? ", " + realism.Name : string.Empty));
                    option.Set(levers);
                    var runs = new DescentSimulation.FoundryResult[seeds.Length];
                    Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) },
                        i => runs[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers, Chain(option)));
                    cells.Add(new Cell { Realism = realism != null ? realism.Name : "frame-perfect", Option = option, Cohort = cohort.Name, Hero = hero.Name, Runs = runs });
                }
                TestContext.Progress.WriteLine($"{option.Key}: {watch.Elapsed.TotalSeconds:0} s");
                File.WriteAllText(Path.Combine(output, "first-card-floor2.md"), Report(cells, first, count));
                File.WriteAllText(Path.Combine(output, "first-card-floor2.csv"), Csv(cells));
            }
            TestContext.WriteLine(Report(cells, first, count));
            Assert.That(cells.All(c => c.Runs.Length == count), "every seed ran");
        }

        private static DescentSimulation.FoundryResult Two(DescentSimulation.FoundryResult run) => run.NextFloor;

        private static DescentSimulation.FoundryResult Finale(DescentSimulation.FoundryResult run) => run.NextFloor?.NextFloor;

        private static float Descent(DescentSimulation.FoundryResult run)
        {
            float sum = 0f;
            for (DescentSimulation.FoundryResult r = run; r != null; r = r.NextFloor)
                sum += r.FloorSeconds;
            return sum;
        }

        private static bool Completed(DescentSimulation.FoundryResult run) => Finale(run) is DescentSimulation.FoundryResult f && f.WalkedOut;

        private static string Report(List<Cell> cells, int first, int count)
        {
            var md = new StringBuilder();
            md.AppendLine($"# The first-card cohort on floor 2: seeds {first}-{first + count - 1} a hero ({cells.FirstOrDefault()?.Realism})").AppendLine();
            md.AppendLine("Each run plays floor 1 as shipped, floor 2 with the option's knobs and the finale (FOUNDRY_FLOORS=3's chain). Went down: floor 1's clears. Cleared: floor 2, of those that went down. " +
                          "Deaths: floor 2's, by room (1-3 halls, 4 the forge, 5 the Sentry Hall, boss the Warden's Vault) and phase. Past the overrun: deaths at floor 2's clock 300 s or later. " +
                          "Warden: floor 2's Warden's own fight over the fights in which it fell, P50 / P90 and the share inside docs/41's 45-65 s. Floor 2: P50 of its clears (s). " +
                          "Descent: P50 minutes of the runs that walked out of the finale. Finale deaths: of the runs that reached it.").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Went down | Cleared | Deaths: room / phase | Killers | Past the overrun | Warden P50 / P90 (in band) | Floor 2 P50 | Descent P50 | Finale deaths |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Cell c in cells)
                md.AppendLine(Row(c));
            md.AppendLine();
            md.AppendLine("## At floor 2's gate").AppendLine();
            md.AppendLine("The runs whose floor-2 Warden walked out: level, health (of maximum), difficulty and the Warden's health as it walked out (P50); the floor's clock then; the weapons and tomes held (P50); " +
                          "the health and armour cards taken on floors 1-2 (mean, Vital Surge and Iron Skin; Vitality's levels apart); evolutions by then (mean, the run's); the fights still on at the overrun (the clock at the walk-out plus the fight past 300 s).").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Reached | Level | Health % | Difficulty | Warden health | Clock | Weapons / tomes | Health / armour cards | Vitality levels | Evolutions | Fight past 300 s |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Cell c in cells)
                md.AppendLine(GateRow(c));
            md.AppendLine();
            md.AppendLine("## The options").AppendLine();
            foreach (Option option in cells.Select(c => c.Option).Distinct())
                md.AppendLine($"- `{option.Key}`: {option.What}.");
            return md.ToString();
        }

        private static string Row(Cell c)
        {
            DescentSimulation.FoundryResult[] down = c.Runs.Select(Two).Where(r => r != null).ToArray();
            DescentSimulation.FoundryResult[] died = down.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            int cleared = down.Count(r => r.Cleared);
            string rooms = died.Length == 0 ? "-" : string.Join(", ", died.GroupBy(r => Room(r) + " " + r.DeathPhase).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} x{g.Count()}"));
            string killers = died.Length == 0 ? "-" : string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} {g.Count()}"));
            int overrun = died.Count(r => r.Clock >= 300f);
            float[] wardens = down.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds).ToArray();
            int inBand = wardens.Count(s => s >= WardenBandFrom && s <= WardenBandTo);
            DescentSimulation.FoundryResult[] reached = c.Runs.Select(Finale).Where(f => f != null).ToArray();
            int finaleDeaths = reached.Count(f => f.Outcome == DescentSimulation.FoundryOutcome.Died);
            return $"| `{c.Option.Key}` | {c.Cohort} | {c.Hero} | {down.Length} | {cleared} ({Pct(cleared, down.Length)}) | {died.Length}: {rooms} | {killers} | {overrun} | " +
                   $"{F(FoundryBatch.Percentile(wardens, 50))} / {F(FoundryBatch.Percentile(wardens, 90))} ({Pct(inBand, wardens.Length)}) | " +
                   $"{F(FoundryBatch.Percentile(down.Where(r => r.Cleared).Select(r => r.FloorSeconds), 50))} | " +
                   $"{M(FoundryBatch.Percentile(c.Runs.Where(Completed).Select(Descent), 50))} | {finaleDeaths} of {reached.Length} |";
        }

        private static string GateRow(Cell c)
        {
            var gated = c.Runs.Where(r => Two(r) != null && Two(r).LevelAtGate >= 0).ToArray();
            DescentSimulation.FoundryResult[] two = gated.Select(Two).ToArray();
            int pastOverrun = two.Count(r => r.ClockAtGate + Math.Max(0f, r.WardenSeconds >= 0f ? r.WardenSeconds : r.Clock - r.ClockAtGate) >= 300f);
            return $"| `{c.Option.Key}` | {c.Cohort} | {c.Hero} | {two.Length} | {F(FoundryBatch.Percentile(two.Select(r => (float)r.LevelAtGate), 50))} | " +
                   $"{F(FoundryBatch.Percentile(two.Select(r => 100f * r.HeroHealthAtGate / Math.Max(1f, r.HeroMaximumAtGate)), 50))} | " +
                   $"{FoundryBatch.Percentile(two.Select(r => r.DifficultyAtGate), 50).ToString("0.0", CultureInfo.InvariantCulture)} | " +
                   $"{F(FoundryBatch.Percentile(two.Select(r => r.WardenHealth), 50))} | {F(FoundryBatch.Percentile(two.Select(r => r.ClockAtGate), 50))} | " +
                   $"{F(FoundryBatch.Percentile(two.Select(r => (float)r.WeaponsAtGate), 50))} / {F(FoundryBatch.Percentile(two.Select(r => (float)r.TomesAtGate), 50))} | " +
                   $"{Mean(gated.Select(r => (float)Cards(r, "upgrade_health")))} / {Mean(gated.Select(r => (float)Cards(r, "upgrade_armor")))} | " +
                   $"{Mean(gated.Select(r => (float)Cards(r, "tome_vitality")))} | {Mean(gated.Select(r => (float)Two(r).Evolutions))} | " +
                   $"{pastOverrun} ({Pct(pastOverrun, two.Length)}) |";
        }

        // How many times the run took this card on floors 1 and 2.
        private static int Cards(DescentSimulation.FoundryResult run, string id) =>
            run.Picks.Count(p => p == id) + (run.NextFloor != null ? run.NextFloor.Picks.Count(p => p == id) : 0);

        private static string Room(DescentSimulation.FoundryResult run) =>
            run.DeathRoom < 1 ? "descent"
            : run.DeathRoom <= run.Kinds.Length && run.Kinds[run.DeathRoom - 1] == Cryptforge.Combat.RoomKind.Boss ? "boss"
            : "room " + run.DeathRoom.ToString(CultureInfo.InvariantCulture);

        private static string Csv(List<Cell> cells)
        {
            var csv = new StringBuilder();
            csv.AppendLine("realism,option,cohort,hero,seed,f1_outcome,f2_outcome,f2_s,f2_clock,death_room,death_phase,killer,level_at_gate,hp_at_gate,max_at_gate,clock_at_gate," +
                           "difficulty_at_gate,warden_health,warden_s,boss_left,gate_ready_clock,gate_ready_hp,gate_ready_level,gate_wait_s,wait_taken,lowest_phase,lowest_harm," +
                           "taken_wave,taken_horde,taken_warden,taken_slams,taken_pit,fight_horde,fight_escort,fight_pit,weapons_at_gate,tomes_at_gate,weapons,tomes," +
                           "evolutions_f1,evolutions_f2,health_cards,armor_cards,vitality,f1_hp_end,f1_max_end,f1_level,chests_f2,finale_outcome,descent_s");
            foreach (Cell cell in cells)
            foreach (DescentSimulation.FoundryResult run in cell.Runs)
            {
                DescentSimulation.FoundryResult t = Two(run);
                DescentSimulation.FoundryResult f = Finale(run);
                if (t == null)
                {
                    csv.AppendLine(string.Join(",", cell.Realism, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, run.Outcome) + new string(',', 44));
                    continue;
                }
                csv.AppendLine(string.Join(",", cell.Realism, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, run.Outcome, t.Outcome, Num(t.FloorSeconds), Num(t.Clock),
                    t.Outcome == DescentSimulation.FoundryOutcome.Died ? Room(t) : string.Empty, Quote(t.DeathPhase), Quote(t.Killer), t.LevelAtGate, Num(t.HeroHealthAtGate),
                    Num(t.HeroMaximumAtGate), Num(t.ClockAtGate), Num(t.DifficultyAtGate), Num(t.WardenHealth), Num(t.WardenSeconds), Num(t.BossLeft), Num(t.GateReadyClock),
                    Num(t.GateReadyHealth), t.GateReadyLevel, Num(t.GateWaitSeconds), Num(t.WaitTaken), Quote(t.LowestPhase), Quote(t.LowestHarm), Num(t.TakenFromWave),
                    Num(t.TakenFromHorde), Num(t.TakenFromWarden), Num(t.TakenFromSlams), Num(t.TakenFromPit), Num(t.FightTakenFromHorde), Num(t.FightTakenFromEscort),
                    Num(t.FightTakenFromPit), t.WeaponsAtGate, t.TomesAtGate, Quote(t.Weapons), Quote(t.Tomes), run.Evolutions, t.Evolutions, Cards(run, "upgrade_health"),
                    Cards(run, "upgrade_armor"), Cards(run, "tome_vitality"), Num(run.HeroHealth), Num(run.HeroMaximum), run.Level, t.ChestsOpened,
                    f != null ? (f.WalkedOut ? "WalkedOut" : f.Outcome.ToString()) : string.Empty, Completed(run) ? Num(Descent(run)) : string.Empty));
            }
            return csv.ToString();
        }

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string Num(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string F(double value) => double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

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
