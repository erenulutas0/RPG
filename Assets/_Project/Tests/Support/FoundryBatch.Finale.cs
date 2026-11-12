using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cryptforge.Core;

namespace Cryptforge.Tests
{
    // The foundry's finale in the batch's report (FoundryLevers.Floors = 4, DescentSimulation.FinaleFloor; FOUNDRY_FLOORS=4
    // for the batch report, 3 before floor 3 was built): what a floor-3 clear meets below its checkpoint - the Final
    // Swarm's one swarm room on the foundry's arena - who reaches it, survives it and walks out, how long it lasts, what it kills the rest with, what the hero brings into it, and the whole
    // Descent's length against docs/40 phase 2's 13-16 minutes. Pure: it only reads the runs' FoundryResults.
    internal static partial class FoundryBatch
    {
        // docs/40 section 3.2 and phase 2 (section 15.2): a normal full run of 13-16 minutes, a Final Swarm of 45-90 s.
        public const float FullRunFrom = 13f * 60f;
        public const float FullRunTo = 16f * 60f;
        public const float FinaleFrom = 45f;
        public const float FinaleTo = 90f;

        // The finale of a run that went down into it, null otherwise: the swarm floor of its chain, however many floors stand
        // before it (two before docs/48's floor 3, three since; a research chain may hold either).
        public static DescentSimulation.FoundryResult FinaleOf(DescentSimulation.FoundryResult run)
        {
            for (DescentSimulation.FoundryResult floor = run.NextFloor; floor != null; floor = floor.NextFloor)
                if (IsFinale(floor))
                    return floor;
            return null;
        }

        // A floor's result is the finale's: its last room the swarm's.
        public static bool IsFinale(DescentSimulation.FoundryResult floor) =>
            floor.Kinds != null && floor.Kinds.Length > 0 && floor.Kinds[floor.Kinds.Length - 1] == Cryptforge.Combat.RoomKind.Swarm;

        // The run's n-th floor (1 its first), null where it never went down that far.
        public static DescentSimulation.FoundryResult FloorOf(DescentSimulation.FoundryResult run, int floor)
        {
            DescentSimulation.FoundryResult result = run;
            for (int i = 1; i < floor && result != null; i++)
                result = result.NextFloor;
            return result;
        }

        // A batch's finale: the finale's results of its runs that cleared every floor before it and went down, as a batch of
        // its own.
        public static Batch Finale(Batch batch) => new Batch
        {
            Hero = batch.Hero,
            Levers = batch.Levers,
            WallSeconds = batch.WallSeconds,
            Runs = batch.Runs.Select(FinaleOf).Where(run => run != null).ToArray()
        };

        // The whole Descent's active seconds of a run down every floor it played: floor 1 from its first frame, each floor
        // after it and the finale from their checkpoints.
        public static float DescentSeconds(DescentSimulation.FoundryResult run)
        {
            float seconds = 0f;
            for (DescentSimulation.FoundryResult floor = run; floor != null; floor = floor.NextFloor)
                seconds += floor.FloorSeconds;
            return seconds;
        }

        private static bool WalkedOut(DescentSimulation.FoundryResult run) => FinaleOf(run) != null && FinaleOf(run).WalkedOut;

        // The finale's tables, a row a labelled batch (a cohort and a hero): its outcomes and deaths, its time, what the
        // hero brought into it and took from it, and the whole Descent.
        public static string FinaleSection(IReadOnlyList<(string Label, Batch Batch)> rows)
        {
            var md = new StringBuilder();
            PilotRealism realism = rows.Count > 0 ? rows[0].Batch.Levers.Realism : null;
            string pilot = realism != null ? "with " + realism.Name + "'s hands" : "frame-perfect";
            float stay = rows.Count > 0 ? rows[0].Batch.Levers.FinaleStaySeconds : DescentSimulation.PilotSwarmStay;
            string stayed = stay == DescentSimulation.PilotSwarmStay
                ? $"{Num(stay)} s (DevelopmentAutopilot.SwarmStay)"
                : $"{Num(stay)} s (FoundryLevers.FinaleStaySeconds, research; the autopilot's is {Num(DescentSimulation.PilotSwarmStay)} s)";
            md.AppendLine("## The finale (the Final Swarm) after a floor-3 clear").AppendLine();
            md.AppendLine("FoundryLevers.Floors = 4: every run that cleared floor 3 (Gloam Gardens) answers its checkpoint with Descend and plays Floor_FinalSwarm's one swarm room on the foundry's arena " +
                          $"({pilot}). The swarm's ladder walks in round the hero by SwarmDirector - the twelve authored waves a wave every {Num(Cryptforge.Combat.SwarmRule.LadderSeconds)} s of its clock, " +
                          $"x{Num(Cryptforge.Combat.SwarmRule.GrowthPerWave, "0.00")} every {Num(Cryptforge.Combat.SwarmRule.StepSeconds)} s, a fallen place refilled after {Num(Cryptforge.Combat.SwarmRule.RespawnSeconds)} s - " +
                          "at the floor's authored tier (health 1, damage 0.92: the finale is not climb-scaled) times the run's difficulty. No horde, no floor clock and no pit there; a kill pays at once, " +
                          $"its level-ups wait for a sitting every {Num(Cryptforge.Combat.SwarmRule.OfferEverySeconds)} s. The autopilot fights it with its cohort's habits, takes the floor's two altars between kills, and once the swarm's clock reaches " +
                          $"{stayed} walks to the exit (ExitRule.SpotFor on the foundry: floor (0, 6), the court east of the pit) and holds it " +
                          $"{Num(Cryptforge.Combat.ExitRule.DwellSeconds)} s; walking out is the run's victory.").AppendLine();
            FinaleOutcomes(md, rows);
            FinaleTime(md, rows);
            FinaleHero(md, rows);
            FinaleDescent(md, rows);
            return md.ToString();
        }

        private static void FinaleOutcomes(StringBuilder md, IReadOnlyList<(string Label, Batch Batch)> rows)
        {
            md.AppendLine("### Who reaches the finale, and how it ends").AppendLine();
            md.AppendLine("Reached: the runs that cleared all three floors, of all. Survived and walked out: of those that reached it (a survivor that never walks out times out at the floor's limit). " +
                          "Died there: in the fight (\"swarm\") or after the pilot turned for the exit (\"swarm exit\"), with the killing blow.").AppendLine();
            md.AppendLine("| Cohort | Hero | Runs | Reached (of all) | Survived (of reached) | Walked out (of reached) | Walked out (of all) | Died there: phase, killing blow | Timed out |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach ((string label, Batch b) in rows)
            {
                Batch finale = Finale(b);
                DescentSimulation.FoundryResult[] died = finale.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
                int walked = finale.Runs.Count(r => r.WalkedOut);
                int timedOut = finale.Runs.Count(r => r.Outcome == DescentSimulation.FoundryOutcome.Timeout);
                string deaths = died.Length == 0 ? "-"
                    : string.Join(", ", died.GroupBy(r => r.DeathPhase ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                          .Select(g => $"{g.Key} x{g.Count()}")) + "; " +
                      string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                          .Select(g => $"{g.Key} x{g.Count()}"));
                md.AppendLine($"| {label} | {b.Hero.Name} | {b.Runs.Length} | {finale.Runs.Length} ({Pct(finale.Runs.Length, b.Runs.Length)}) | " +
                              $"{finale.Runs.Length - died.Length} ({Pct(finale.Runs.Length - died.Length, finale.Runs.Length)}) | {walked} ({Pct(walked, finale.Runs.Length)}) | " +
                              $"{walked} ({Pct(walked, b.Runs.Length)}) | {died.Length}{(died.Length > 0 ? ": " + deaths : string.Empty)} | {timedOut} |");
            }
            md.AppendLine();
        }

        private static void FinaleTime(StringBuilder md, IReadOnlyList<(string Label, Batch Batch)> rows)
        {
            md.AppendLine("### The finale's time (over the runs that reached it, P10 / P50 / P90)").AppendLine();
            md.AppendLine("Finale: active seconds from floor 3's checkpoint to the exit held or the hero's fall, the descent's 1 s advance delay included; within 45-90 s: docs/40 section 3.2's typical Final Swarm, of the walk-outs. " +
                          "Swarm clock: the room's own, at the end. The walk out: from the pilot's turn for the exit to leaving, the hold included. Ladder wave and ramp step: where the clock had got to at the end (P50), " +
                          "and the ramp's multiplier on an enemy walking in then. Kills, sittings and the most of the swarm standing at once: P50.").AppendLine();
            md.AppendLine("| Cohort | Hero | Finale | Walk-outs within 45-90 s | Swarm clock at the end | The walk out | Ladder wave / ramp step at the end | Ramp at the end | Kills | Sittings | Most standing |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach ((string label, Batch b) in rows)
            {
                DescentSimulation.FoundryResult[] runs = Finale(b).Runs;
                DescentSimulation.FoundryResult[] walked = runs.Where(r => r.WalkedOut).ToArray();
                int within = walked.Count(r => r.FloorSeconds >= FinaleFrom && r.FloorSeconds <= FinaleTo);
                md.AppendLine($"| {label} | {b.Hero.Name} | {Spread(runs.Select(r => r.FloorSeconds), "0.0")} | {within} ({Pct(within, walked.Length)}) | " +
                              $"{Spread(runs.Select(r => r.SwarmSeconds), "0.0")} | {Spread(walked.Select(r => r.SwarmExitSeconds), "0.0")} | " +
                              $"{F(Median(runs.Select(r => (float)r.SwarmLadderWave)))} / {F(Median(runs.Select(r => (float)r.SwarmStep)))} | " +
                              $"x{F(Median(runs.Select(r => r.SwarmDifficulty)), "0.00")} | {F(Median(runs.Select(r => (float)r.WaveKills)))} | " +
                              $"{F(Median(runs.Select(r => (float)r.Sittings)))} | {F(Median(runs.Select(r => (float)r.SwarmMaxAlive)))} |");
            }
            md.AppendLine();
        }

        private static void FinaleHero(StringBuilder md, IReadOnlyList<(string Label, Batch Batch)> rows)
        {
            md.AppendLine("### The hero entering the finale, and what the finale costs").AppendLine();
            md.AppendLine("Entering: as the swarm opens, after floor 3 and the 1 s descent, which heal nothing (P10 / P50 / P90): its level, its health as a share of its maximum, its maximum (P50), and the run's difficulty (P50), " +
                          "which hardens every enemy of the swarm beside the ramp. Health lost: in the finale, mean and P90 of the runs that reached it; by what: the swarm's blows, the chest's heal set against them aside. " +
                          "Level at the end: P50 of the walk-outs. Altars and the chest: the finale's own (each altar answers again on a new floor; the room's chest stands where the hero arrives, docs/47), mean a run.").AppendLine();
            md.AppendLine("| Cohort | Hero | Level entering | Health entering (% of maximum) | Maximum P50 | Difficulty P50 | Health lost mean / P90 | Level at the end P50 | Altars taken | Chest opened |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach ((string label, Batch b) in rows)
            {
                DescentSimulation.FoundryResult[] runs = Finale(b).Runs;
                md.AppendLine($"| {label} | {b.Hero.Name} | {Spread(runs.Select(r => (float)r.SwarmEntryLevel))} | " +
                              $"{Spread(runs.Select(r => 100f * r.SwarmEntryHealth / Math.Max(1f, r.SwarmEntryMaximum)))} | " +
                              $"{F(Median(runs.Select(r => r.SwarmEntryMaximum)))} | {F(Median(runs.Select(r => r.SwarmEntryDifficulty)), "0.00")} | " +
                              $"{F(runs.Length == 0 ? double.NaN : runs.Average(r => r.TakenFromWave), "0.0")} / {F(Percentile(runs.Select(r => r.TakenFromWave), 90), "0.0")} | " +
                              $"{F(Median(runs.Where(r => r.WalkedOut).Select(r => (float)r.Level)))} | " +
                              $"{F(runs.Length == 0 ? double.NaN : runs.Average(r => r.AltarsTaken), "0.00")} | " +
                              $"{F(runs.Length == 0 ? double.NaN : runs.Average(r => r.ChestsOpened), "0.00")} |");
            }
            md.AppendLine();
        }

        private static void FinaleDescent(StringBuilder md, IReadOnlyList<(string Label, Batch Batch)> rows)
        {
            md.AppendLine("### The whole Descent against docs/40 phase 2's 13-16 minutes").AppendLine();
            md.AppendLine("Over the runs that walked out of the finale: each floor's active seconds (P50), and all of them together (P10 / P50 / P90, then P50 in minutes), " +
                          "against a full normal run of 13-16 minutes (docs/40 section 3.2: three floors of 4-5 minutes each and a 45-90 s Final Swarm; this Descent has three floors before its finale since docs/48). " +
                          "Completed: the walk-outs, of all runs.").AppendLine();
            md.AppendLine("| Cohort | Hero | Completed (of all) | Floor 1 P50 | Floor 2 P50 | Floor 3 P50 | Finale P50 | The Descent P10 / P50 / P90 | P50 (min) | Within 13-16 min | Short of 13 min |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach ((string label, Batch b) in rows)
            {
                DescentSimulation.FoundryResult[] done = b.Runs.Where(WalkedOut).ToArray();
                float[] descent = done.Select(DescentSeconds).ToArray();
                int within = descent.Count(s => s >= FullRunFrom && s <= FullRunTo);
                int shorter = descent.Count(s => s < FullRunFrom);
                double median = Median(descent);
                md.AppendLine($"| {label} | {b.Hero.Name} | {done.Length} ({Pct(done.Length, b.Runs.Length)}) | {F(Median(done.Select(r => r.FloorSeconds)))} | " +
                              $"{F(Median(done.Select(r => r.NextFloor.FloorSeconds)))} | " +
                              $"{F(Median(done.Select(r => FloorOf(r, 3)).Where(f => f != null && !IsFinale(f)).Select(f => f.FloorSeconds)))} | " +
                              $"{F(Median(done.Select(r => FinaleOf(r).FloorSeconds)))} | " +
                              $"{Spread(descent)} | {F(median / 60.0, "0.0")} | {within} ({Pct(within, done.Length)}) | {shorter} ({Pct(shorter, done.Length)}) |");
            }
            md.AppendLine();
        }

        // Every run that went down into the finale, one line each: floors 1 and 2 as the run brought them, then the finale.
        public static string FinaleCsv(IEnumerable<Batch> batches)
        {
            var csv = new StringBuilder();
            csv.AppendLine("hero,levers,seed,floor1_s,floor2_s,floor3_s,descent_s,outcome,walked_out,run_end,finale_s,swarm_s,swarm_step,ladder_wave,ramp_end,exit_walk_s," +
                           "entry_level,entry_hp,entry_max_hp,entry_difficulty,level_end,hp_end,max_hp_end,kills,spawned,max_alive,sittings,taken,death_phase,killer," +
                           "altars,chests,lure_s,kite_s,gate_boss_frames,left_x,left_y,weapons,tomes");
            foreach (Batch b in batches)
            foreach (DescentSimulation.FoundryResult one in b.Runs)
            {
                DescentSimulation.FoundryResult r = FinaleOf(one);
                if (r == null)
                    continue;
                string Num(float v, string format = "0.00") => v.ToString(format, CultureInfo.InvariantCulture);
                DescentSimulation.FoundryResult three = FloorOf(one, 3);
                csv.AppendLine(string.Join(",", one.Hero, Quote(one.Levers), one.Seed, Num(one.FloorSeconds), Num(one.NextFloor.FloorSeconds),
                    Num(three != null && !IsFinale(three) ? three.FloorSeconds : -1f), Num(r.WalkedOut ? DescentSeconds(one) : -1f), r.Outcome, r.WalkedOut ? 1 : 0, r.RunEnd, Num(r.FloorSeconds), Num(r.SwarmSeconds),
                    r.SwarmStep, r.SwarmLadderWave, Num(r.SwarmDifficulty, "0.000"), Num(r.SwarmExitSeconds), r.SwarmEntryLevel,
                    Num(r.SwarmEntryHealth, "0.0"), Num(r.SwarmEntryMaximum, "0"), Num(r.SwarmEntryDifficulty), r.Level, Num(r.HeroHealth, "0.0"),
                    Num(r.HeroMaximum, "0"), r.WaveKills, r.SwarmSpawned, r.SwarmMaxAlive, r.Sittings, Num(r.TakenFromWave, "0.0"),
                    Quote(r.DeathPhase), Quote(r.Killer), r.AltarsTaken, r.ChestsOpened, Num(r.LureSeconds, "0.0"), Num(r.KiteSeconds, "0.0"),
                    r.GateBossFrames, Num(r.LeftAtX), Num(r.LeftAtY), Quote(r.Weapons), Quote(r.Tomes)));
            }
            return csv.ToString();
        }

        private static string Num(float value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
    }
}
