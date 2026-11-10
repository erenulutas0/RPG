using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Tests
{
    // docs/41's open decision 2 (2026-11-04): V0.1's floor holds its gate until the floor's clock reads 3:30, so about
    // 110 s of a clear is the wait and the gate, not the fighting, carries the floor's length. These are the levers that
    // could move the length into fighting - more or denser surges, a horde that grows faster with the clock, more elites
    // or halls, longer halls, an earlier gate or none, a gate that opens on a kill quota or on the hero's level, and a
    // siege that fills the wait with node waves - each against the shipped floor on the same seeds, every cohort of the
    // autopilot playing it (FOUNDRY_LEVERS=5), and the lever sets made of them for the owner (FOUNDRY_LEVERS=6).
    // docs/cloud/2026-11-04-v01-levers.md reads the tables. Research values only: the shipped floor is FoundryLevers'
    // defaults, and none of these changes Floor_EmberHalls.
    internal static partial class FoundryBatch
    {
        private static Lever OnShipped(string name, Action<DescentSimulation.FoundryLevers> set)
        {
            DescentSimulation.FoundryLevers levers = Shipped().Copy(name);
            set(levers);
            return new Lever { Name = name, Estimate = NotMeasured, Levers = levers };
        }

        // Each lever alone on the shipped floor; the content levers once with the shipped gate (does the wait shrink?) and
        // once with no time on the gate (how long is the floor when only the fighting makes it?).
        public static Lever[] GateLevers() => new[]
        {
            OnShipped("Shipped: surges 2, elites +1, gate 3:30", l => { }),
            OnShipped("Gate 3:00", l => l.GateNotBeforeSeconds = 180f),
            OnShipped("Gate 2:45", l => l.GateNotBeforeSeconds = 165f),
            OnShipped("Gate 2:30", l => l.GateNotBeforeSeconds = 150f),
            OnShipped("No gate time", l => l.GateNotBeforeSeconds = 0f),
            OnShipped("Surges 4, gate 3:30", l => l.HallSurges = 4),
            OnShipped("Halls held 45 s, gate 3:30", l => l.HallHoldSeconds = 45f),
            OnShipped("Surges 3, no gate time", l => { l.HallSurges = 3; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Surges 4, no gate time", l => { l.HallSurges = 4; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Surges 6, no gate time", l => { l.HallSurges = 6; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("+2 halls, gate 3:30", l => l.ExtraHalls = 2),
            OnShipped("+1 hall, no gate time", l => { l.ExtraHalls = 1; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("+2 halls, no gate time", l => { l.ExtraHalls = 2; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("+3 halls, no gate time", l => { l.ExtraHalls = 3; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Elites +2 (a third elite), no gate time", l => { l.ExtraElites = 2; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Halls held 30 s, no gate time", l => { l.HallHoldSeconds = 30f; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Halls held 45 s, no gate time", l => { l.HallHoldSeconds = 45f; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Horde growth 0.5 -> 1.5, gate 3:30", l => l.HordeTargetGrowth = 1.5f),
            OnShipped("Horde growth 0.5 -> 1.5, no gate time", l => { l.HordeTargetGrowth = 1.5f; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Horde growth 0.5 -> 3, no gate time", l => { l.HordeTargetGrowth = 3f; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Horde targets x2 (8/20/32/12), no gate time", l => { l.HordeTargets = new[] { 8, 20, 32, 12 }; l.GateNotBeforeSeconds = 0f; }),
            OnShipped("Gate on 400 kills (no time)", l => { l.GateNotBeforeSeconds = 0f; l.GateKillQuota = 400; }),
            OnShipped("Gate on 600 kills (no time)", l => { l.GateNotBeforeSeconds = 0f; l.GateKillQuota = 600; }),
            OnShipped("Gate on 800 kills (no time)", l => { l.GateNotBeforeSeconds = 0f; l.GateKillQuota = 800; }),
            OnShipped("Gate 3:30 or level 24", l => l.GateOpenLevel = 24),
            OnShipped("Gate 3:30 or level 22", l => l.GateOpenLevel = 22),
            OnShipped("Gate on level 20 (no time)", l => { l.GateNotBeforeSeconds = 0f; l.GateOpenLevel = 20; }),
            OnShipped("Gate 3:30, siege of hall 1", l => l.GateSiegeHall = 1),
            OnShipped("Gate 3:30, siege of hall 3", l => l.GateSiegeHall = 3),
            OnShipped("Gate 2:45, siege of hall 1", l => { l.GateNotBeforeSeconds = 165f; l.GateSiegeHall = 1; })
        };

        // The lever sets for the owner (docs/cloud/2026-11-04-v01-levers.md), each against the shipped floor.
        // The lever sets for the owner (docs/cloud/2026-11-04-v01-levers.md), each against the shipped floor: the first
        // round, then the second that tuned its best two.
        public static Lever[] GateLeverSets() => new[]
        {
            OnShipped("Shipped: surges 2, elites +1, gate 3:30", l => { }),
            OnShipped("+2 halls, surges 3, gate 2:45", l => { l.ExtraHalls = 2; l.HallSurges = 3; l.GateNotBeforeSeconds = 165f; }),
            OnShipped("+2 halls, surges 3, gate 3:00", l => { l.ExtraHalls = 2; l.HallSurges = 3; l.GateNotBeforeSeconds = 180f; }),
            OnShipped("+2 halls, surges 3, gate 2:45, horde XP 0.25", l => { l.ExtraHalls = 2; l.HallSurges = 3; l.GateNotBeforeSeconds = 165f; l.HordeExperienceShare = .25f; }),
            OnShipped("Halls held 30 s, surges 3, gate 2:45", l => { l.HallHoldSeconds = 30f; l.HallSurges = 3; l.GateNotBeforeSeconds = 165f; }),
            OnShipped("Halls held 30 s, surges 3, gate 2:45, horde XP 0.25", l => { l.HallHoldSeconds = 30f; l.HallSurges = 3; l.GateNotBeforeSeconds = 165f; l.HordeExperienceShare = .25f; }),
            OnShipped("+1 hall, halls held 30 s, gate 2:45", l => { l.ExtraHalls = 1; l.HallHoldSeconds = 30f; l.GateNotBeforeSeconds = 165f; }),
            OnShipped("+2 halls, gate on 450 kills (no time)", l => { l.ExtraHalls = 2; l.GateNotBeforeSeconds = 0f; l.GateKillQuota = 450; }),
            OnShipped("+3 halls, gate 3:00", l => { l.ExtraHalls = 3; l.GateNotBeforeSeconds = 180f; }),
            // The second round.
            OnShipped("R2 +2 halls, surges 2, gate 2:45, horde XP 0.25", l => { l.ExtraHalls = 2; l.GateNotBeforeSeconds = 165f; l.HordeExperienceShare = .25f; }),
            OnShipped("R2 +2 halls, surges 3, gate 2:45, horde XP 0.3", l => { l.ExtraHalls = 2; l.HallSurges = 3; l.GateNotBeforeSeconds = 165f; l.HordeExperienceShare = .3f; }),
            OnShipped("R2 +2 halls, surges 3, gate 2:45, horde XP 0.25, siege of hall 1", l => { l.ExtraHalls = 2; l.HallSurges = 3; l.GateNotBeforeSeconds = 165f; l.HordeExperienceShare = .25f; l.GateSiegeHall = 1; }),
            OnShipped("R2 Halls held 30 s, surges 3, gate 2:30, horde XP 0.25", l => { l.HallHoldSeconds = 30f; l.HallSurges = 3; l.GateNotBeforeSeconds = 150f; l.HordeExperienceShare = .25f; }),
            OnShipped("R2 Halls held 30 s, surges 2, gate 2:30, horde XP 0.25", l => { l.HallHoldSeconds = 30f; l.GateNotBeforeSeconds = 150f; l.HordeExperienceShare = .25f; }),
            OnShipped("R2 Halls held 30 s, surges 3, gate 2:30, horde XP 0.25, siege of hall 1", l => { l.HallHoldSeconds = 30f; l.HallSurges = 3; l.GateNotBeforeSeconds = 150f; l.HordeExperienceShare = .25f; l.GateSiegeHall = 1; })
        };

        // docs/41's open question of floor 2's Warden (FOUNDRY_LEVERS=8, both floors, every cohort): the 2026-11-04 options
        // - a floor-2 share of about 6 (8.5 / 1.4, the climb's x1.4 cancelled) instead of 8.5, floor 2's Mend at 100 %, a
        // 2.5 s recovery on floor 2 - and their combinations, plus a share of 7 now that the Sword deals the Warden half
        // again. Floor 1 is the shipped floor in every row but the one naming both floors.
        public static Lever[] FloorTwoWardenLevers() => new[]
        {
            OnShipped("Shipped: floor 2 share 8.5, recovery 1.5 s, Mend 40 %", l => { }),
            OnShipped("F2 share 6.07", l => l.LaterWardenHealthShare = 8.5f / 1.4f),
            OnShipped("F2 share 7", l => l.LaterWardenHealthShare = 7f),
            OnShipped("F2 Mend 100 %", l => l.LaterMendAmount = 1f),
            OnShipped("F2 recovery 2.5 s", l => l.LaterWardenRecoverySeconds = 2.5f),
            OnShipped("F2 share 6.07 + F2 Mend 100 %", l => { l.LaterWardenHealthShare = 8.5f / 1.4f; l.LaterMendAmount = 1f; }),
            OnShipped("F2 share 6.07 + F2 recovery 2.5 s", l => { l.LaterWardenHealthShare = 8.5f / 1.4f; l.LaterWardenRecoverySeconds = 2.5f; }),
            OnShipped("F2 share 6.07 + recovery 2.5 s on both floors", l => { l.LaterWardenHealthShare = 8.5f / 1.4f; l.WardenRecoverySeconds = 2.5f; }),
            OnShipped("F2 share 7 + F2 Mend 100 %", l => { l.LaterWardenHealthShare = 7f; l.LaterMendAmount = 1f; }),
            OnShipped("F2 share 7 + F2 recovery 2.5 s", l => { l.LaterWardenHealthShare = 7f; l.LaterWardenRecoverySeconds = 2.5f; })
        };

        // Only the levers whose names hold one of FOUNDRY_LEVER_FILTER's comma-separated words, the shipped row kept.
        public static Lever[] Filtered(Lever[] levers, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return levers;
            string[] words = filter.Split(',').Select(w => w.Trim()).Where(w => w.Length > 0).ToArray();
            return levers.Where((lever, i) => i == 0 ||
                                              words.Any(w => lever.Name.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray();
        }

        // One lever's batches: each cohort's, the heroes in order, the careful cohort first.
        internal sealed class CohortLever
        {
            public Lever Lever;
            public List<(AutopilotCohort Cohort, Batch[] Batches)> Cohorts = new List<(AutopilotCohort, Batch[])>();

            public Batch[] Careful => Cohorts[0].Batches;
        }

        public static CohortLever RunCohorts(Lever lever, IReadOnlyList<DescentSimulation.FoundryHero> heroes, int[] seeds,
            int parallelism, IReadOnlyList<AutopilotCohort> cohorts, int floors = 1, PilotRealism realism = null)
        {
            var run = new CohortLever { Lever = lever };
            foreach (AutopilotCohort cohort in cohorts)
            {
                var batches = new Batch[heroes.Count];
                for (int h = 0; h < heroes.Count; h++)
                {
                    DescentSimulation.FoundryLevers levers = lever.Levers.Copy(lever.Levers.Name);
                    levers.Cohort = cohort;
                    levers.Floors = floors;
                    levers.Realism = realism;
                    batches[h] = Run(heroes[h], levers, seeds, parallelism);
                }
                run.Cohorts.Add((cohort, batches));
            }
            return run;
        }

        // The cohorts FOUNDRY_SWEEP_COHORTS names (comma separated, AutopilotCohort's names), careful always first; all of
        // them by default.
        public static List<AutopilotCohort> SweepCohorts(string names)
        {
            var cohorts = new List<AutopilotCohort> { AutopilotCohort.Careful };
            IEnumerable<AutopilotCohort> asked = string.IsNullOrWhiteSpace(names)
                ? AutopilotCohort.Cohorts
                : names.Split(',').Select(n => AutopilotCohort.TryNamed(n, out AutopilotCohort c) ? c : null).Where(c => c != null);
            foreach (AutopilotCohort cohort in asked)
                if (!cohorts.Contains(cohort))
                    cohorts.Add(cohort);
            return cohorts;
        }

        public static string GateLeverSection(IReadOnlyList<CohortLever> levers, string title)
        {
            var md = new StringBuilder();
            md.AppendLine($"## {title}").AppendLine();
            md.AppendLine("Each row the careful cohort, unless named; Vanguard / Ilya in each cell. Floor: active seconds, every " +
                          "run, and the clears' P50. Wait: the gate's wait in the clears (P50), and a clear less its wait (P50). " +
                          "Clock P90: the floor's clock at clear, against the 300 s overrun. Uptime: combat, engaged (every run, " +
                          "P50). First: the first strike's P90. Warden: the felled fights' P50. Gate: the V0.1 gate's statuses " +
                          "for the Vanguard - floor time, engaged uptime, Warden fight (FoundryGate).").AppendLine();
            md.AppendLine("| Lever | Floor P50 | Clears' floor P50 | Wait P50 | Clear less wait P50 | Clock at clear P90 | Combat uptime | Engaged uptime | First P90 | Warden P50 | Gate: floor / engaged / Warden |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (CohortLever lever in levers)
            {
                Batch[] b = lever.Careful;
                md.AppendLine($"| {lever.Lever.Name} | {Pair(b, x => Median(x.Runs.Select(r => r.FloorSeconds)))} | " +
                              $"{Pair(b, x => Median(x.Cleared.Select(r => r.FloorSeconds)))} | " +
                              $"{Pair(b, x => Median(x.Cleared.Select(r => r.GateWaitSeconds)))} | " +
                              $"{Pair(b, x => Median(x.Cleared.Select(r => r.FloorSeconds - r.GateWaitSeconds)))} | " +
                              $"{Pair(b, x => Percentile(x.Cleared.Select(r => r.Clock), 90))} | " +
                              $"{Pair(b, x => Median(x.Runs.Select(r => r.Uptime)), "0.00")} | " +
                              $"{Pair(b, x => Median(x.Runs.Select(r => r.Engaged)), "0.00")} | " +
                              $"{Pair(b, x => Percentile(x.Runs.Select(r => r.FirstStrikeSeconds), 90), "0.0")} | " +
                              $"{Pair(b, x => Median(x.Cleared.Where(r => r.BossSeconds > 0f).Select(r => r.BossSeconds)))} | " +
                              $"{GateStatuses(b)} |");
            }
            md.AppendLine();

            List<AutopilotCohort> cohorts = levers[0].Cohorts.Select(c => c.Cohort).ToList();
            md.AppendLine("Clears of the 200 seeds by cohort (Vanguard / Ilya), and where the careful and the clumsy Vanguard died " +
                          "(room kind; the walk to a room counts as the room).").AppendLine();
            bool twoFloors = levers.Any(l => l.Careful.Any(b => b.Runs.Any(r => r.NextFloor != null)));
            md.AppendLine("| Lever | " + string.Join(" | ", cohorts.Select(c => c.Name)) + " | Careful Vanguard deaths | Clumsy Vanguard deaths |" +
                          (twoFloors ? " Both floors cleared: " + string.Join(" / ", cohorts.Select(c => c.Name)) + " |" : string.Empty));
            md.AppendLine("|---|" + string.Concat(cohorts.Select(_ => "---|")) + "---|---|" + (twoFloors ? "---|" : string.Empty));
            foreach (CohortLever lever in levers)
            {
                var cells = new List<string>();
                foreach (AutopilotCohort cohort in cohorts)
                {
                    Batch[] b = lever.Cohorts.FirstOrDefault(c => c.Cohort == cohort).Batches;
                    cells.Add(b == null ? "-" : string.Join(" / ", b.Select(x => Pct(x.ClearedCount, x.Runs.Length))));
                }
                Batch[] clumsy = lever.Cohorts.FirstOrDefault(c => c.Cohort == AutopilotCohort.Clumsy).Batches;
                string both = !twoFloors ? string.Empty : " " + string.Join("; ", lever.Cohorts.Select(c => string.Join(" / ",
                    c.Batches.Select(x => Pct(x.Runs.Count(r => r.Cleared && r.NextFloor != null && r.NextFloor.Cleared), x.Runs.Length))))) + " |";
                md.AppendLine($"| {lever.Lever.Name} | {string.Join(" | ", cells)} | {Deaths(lever.Careful[0])} | " +
                              $"{(clumsy == null ? "-" : Deaths(clumsy[0]))} |" + both);
            }
            md.AppendLine();

            md.AppendLine("What fills the gate's wait, the careful cohort's clears that waited (Vanguard / Ilya): the share of the " +
                          "wait engaged (the engaged rule), and of the autopilot's steer kiting, walking to drops, fighting a node " +
                          "wave, walking elsewhere, standing with nothing to do; over the wait, mean kills, levels gained, health " +
                          "lost, siege waves. Ready: the floor's clock, kills and level as the room before the gate cleared " +
                          "(P50, every run that got there).").AppendLine();
            md.AppendLine("| Lever | Waited | Engaged | Kite | Drops | Node wave | Walk | Still | Kills | Levels | Health lost | Siege waves | Ready: clock / kills / level |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (CohortLever lever in levers)
            {
                Batch[] b = lever.Careful;
                md.AppendLine($"| {lever.Lever.Name} | {string.Join(" / ", b.Select(x => Waited(x).Length.ToString(CultureInfo.InvariantCulture)))} | " +
                              $"{Share(b, r => r.WaitEngaged)} | {Share(b, r => r.WaitKite)} | {Share(b, r => r.WaitDrop)} | " +
                              $"{Share(b, r => r.WaitFight)} | {Share(b, r => r.WaitWalk)} | {Share(b, r => r.WaitStill)} | " +
                              $"{WaitMean(b, r => r.WaitKills)} | {WaitMean(b, r => r.WaitLevels, "0.0")} | {WaitMean(b, r => r.WaitTaken)} | " +
                              $"{WaitMean(b, r => r.WaitSiegeWaves, "0.0")} | " +
                              string.Join(" / ", b.Select(x => Ready(x))) + " |");
            }
            md.AppendLine();
            return md.ToString();
        }

        public static string GateLeverCsv(IEnumerable<CohortLever> levers)
        {
            var csv = new StringBuilder();
            csv.AppendLine("lever,cohort,hero,seed,outcome,floor_s,gate_wait_s,clock,engaged,uptime,first_strike_s,warden_s," +
                           "death_place,ready_clock,ready_kills,ready_level,wait_engaged,wait_kite,wait_drop,wait_fight,wait_walk," +
                           "wait_still,wait_kills,wait_levels,wait_taken,siege_waves,level");
            foreach (CohortLever lever in levers)
            foreach ((AutopilotCohort cohort, Batch[] batches) in lever.Cohorts)
            foreach (Batch b in batches)
            foreach (DescentSimulation.FoundryResult r in b.Runs)
                csv.AppendLine(string.Join(",", new[]
                {
                    "\"" + lever.Lever.Name + "\"", cohort.Name, b.Hero.Id, N(r.Seed), r.Outcome.ToString(), N(r.FloorSeconds),
                    N(r.GateWaitSeconds), N(r.Clock), N(r.Engaged), N(r.Uptime), N(r.FirstStrikeSeconds), N(r.BossSeconds),
                    r.Outcome == DescentSimulation.FoundryOutcome.Died ? FoundryGate.DeathPlace(r) : "", N(r.GateReadyClock),
                    N(r.GateReadyKills), N(r.GateReadyLevel), N(r.WaitEngaged), N(r.WaitKite), N(r.WaitDrop), N(r.WaitFight),
                    N(r.WaitWalk), N(r.WaitStill), N(r.WaitKills), N(r.WaitLevels), N(r.WaitTaken), N(r.WaitSiegeWaves), N(r.Level)
                }));
            return csv.ToString();
        }

        // Every run of every lever and cohort, floor 1 and (where it went down) floor 2 on one line: the outcome, the
        // floor's length, the gate's wait, the boss room's and the Warden's own fight, where and by what it died, the
        // slams that landed, and the health it went down with.
        public static string GateLeverFloorsCsv(IEnumerable<CohortLever> levers)
        {
            var csv = new StringBuilder();
            csv.AppendLine("lever,cohort,hero,seed,f1_outcome,f1_floor_s,f1_gate_wait_s,f1_boss_s,f1_warden_s,f1_death_place,f1_death_room,f1_killer,f1_slam_hits," +
                           "down_hp_share,f2_outcome,f2_floor_s,f2_gate_wait_s,f2_boss_s,f2_warden_s,f2_death_place,f2_death_room,f2_killer,f2_slam_hits,f2_level_gate");
            string Q(string s) => "\"" + (s ?? string.Empty) + "\"";
            foreach (CohortLever lever in levers)
            foreach ((AutopilotCohort cohort, Batch[] batches) in lever.Cohorts)
            foreach (Batch b in batches)
            foreach (DescentSimulation.FoundryResult r in b.Runs)
            {
                DescentSimulation.FoundryResult two = r.NextFloor;
                bool died1 = r.Outcome == DescentSimulation.FoundryOutcome.Died;
                bool died2 = two != null && two.Outcome == DescentSimulation.FoundryOutcome.Died;
                csv.AppendLine(string.Join(",", new[]
                {
                    Q(lever.Lever.Name), cohort.Name, b.Hero.Id, N(r.Seed), r.Outcome.ToString(), N(r.FloorSeconds), N(r.GateWaitSeconds),
                    N(r.BossSeconds), N(r.WardenSeconds), died1 ? Q(FoundryGate.DeathPlace(r)) : "", died1 ? N(r.DeathRoom) : "",
                    died1 ? Q(r.Killer) : "", N(r.SlamHits),
                    two != null ? N(r.HeroHealth / Math.Max(1f, r.HeroMaximum)) : "",
                    two != null ? two.Outcome.ToString() : "", two != null ? N(two.FloorSeconds) : "", two != null ? N(two.GateWaitSeconds) : "",
                    two != null ? N(two.BossSeconds) : "", two != null ? N(two.WardenSeconds) : "",
                    died2 ? Q(FoundryGate.DeathPlace(two)) : "", died2 ? N(two.DeathRoom) : "", died2 ? Q(two.Killer) : "",
                    two != null ? N(two.SlamHits) : "", two != null ? N(two.LevelAtGate) : ""
                }));
            }
            return csv.ToString();
        }

        private static string N(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Pair(Batch[] batches, Func<Batch, double> read, string format = "0") =>
            string.Join(" / ", batches.Select(b =>
            {
                double value = read(b);
                return double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);
            }));

        private static string GateStatuses(Batch[] batches)
        {
            List<FoundryGate.Row> rows = FoundryGate.Rows(batches, new Batch[0], new FoundryGate.Evidence());
            string Of(string criterion) => rows.First(r => r.Criterion == criterion).Status;
            return $"{Of(FoundryGate.FloorTime)} / {Of(FoundryGate.EngagedUptime)} / {Of(FoundryGate.WardenFight)}";
        }

        private static string Deaths(Batch b)
        {
            DescentSimulation.FoundryResult[] died = b.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            if (died.Length == 0)
                return "-";
            return died.Length.ToString(CultureInfo.InvariantCulture) + ": " + string.Join(", ",
                died.GroupBy(FoundryGate.DeathPlace).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}"));
        }

        private static DescentSimulation.FoundryResult[] Waited(Batch b) =>
            b.Cleared.Where(r => r.GateWaitSeconds > 0f).ToArray();

        private static string Share(Batch[] batches, Func<DescentSimulation.FoundryResult, float> part) =>
            string.Join(" / ", batches.Select(b =>
            {
                DescentSimulation.FoundryResult[] waited = Waited(b);
                double wait = waited.Sum(r => (double)r.GateWaitSeconds);
                return wait > 0 ? (waited.Sum(r => (double)part(r)) / wait).ToString("0.00", CultureInfo.InvariantCulture) : "-";
            }));

        private static string WaitMean(Batch[] batches, Func<DescentSimulation.FoundryResult, float> read, string format = "0") =>
            string.Join(" / ", batches.Select(b =>
            {
                DescentSimulation.FoundryResult[] waited = Waited(b);
                return waited.Length > 0 ? waited.Average(r => (double)read(r)).ToString(format, CultureInfo.InvariantCulture) : "-";
            }));

        private static string Ready(Batch b)
        {
            DescentSimulation.FoundryResult[] ready = b.Runs.Where(r => r.GateReadyClock >= 0f).ToArray();
            if (ready.Length == 0)
                return "-";
            return $"{Median(ready.Select(r => r.GateReadyClock)).ToString("0", CultureInfo.InvariantCulture)} s, " +
                   $"{Median(ready.Select(r => (float)r.GateReadyKills)).ToString("0", CultureInfo.InvariantCulture)}, " +
                   $"{Median(ready.Select(r => (float)r.GateReadyLevel)).ToString("0", CultureInfo.InvariantCulture)}";
        }
    }
}
