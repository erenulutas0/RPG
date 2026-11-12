using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Cryptforge.Tests
{
    // The Editor's Descent (HordePacingMeasurement with TestResults/horde-floors.txt "3" or "4"): the scene's autopilot
    // down every floor of the climb - floor 1, floor 2, floor 3 since docs/48, the finale - a row a seed after the floor-1
    // rows of its pacing file. Each line starts with "descent", so a floor-1
    // reader (FoundryBatchReport.ReadEditor: the first "seed\t" line is the header, a line whose first cell is no seed is
    // skipped) never reads it. Missing values are "-"; numbers are invariant, though a decimal comma is read too.
    //
    //   descent seed result died_at floor1_s floor2_s finale_s descent_s warden1_s warden2_s level_gate1 level_gate2
    //           uptime1 engaged1 uptime2 engaged2 uptime3 engaged3 hold2_s clock2 finale_hp finale_max_hp finale_level
    //           chest_s swarm_s level hp max_hp kills2 kills3 rooms2
    //
    // result: walked_out, died or timeout; died_at: floor.room:phase. floor2_s stands where floor 1 was cleared, finale_s
    // where the floor before it was; descent_s where the hero walked out. Since docs/48 the row adds floor3_s, warden3_s,
    // level_gate3, uptime_floor3, engaged_floor3, hold3_s, clock3 and kills_floor3 for floor 3, "-" in a file written
    // before it (floor 2 straight into the finale), which reads as it did. The finale's report (FOUNDRY_FLOORS=4) lays
    // these rows beside the simulation's on the same seeds (Section).
    internal static class EditorDescent
    {
        public const string Prefix = "descent";

        internal sealed class Run
        {
            public int Seed;
            public string Result;
            public string DiedAt;
            public float Floor1 = float.NaN;
            public float Floor2 = float.NaN;
            public float Finale = float.NaN;
            public float Descent = float.NaN;
            public float Warden1 = float.NaN;
            public float Warden2 = float.NaN;
            public int LevelGate1 = -1;
            public int LevelGate2 = -1;
            public float Uptime1 = float.NaN, Engaged1 = float.NaN;
            public float Uptime2 = float.NaN, Engaged2 = float.NaN;
            public float Uptime3 = float.NaN, Engaged3 = float.NaN;
            // Floor 2's hold for its clock (the gate's wait) and its clock at the end.
            public float Hold2 = float.NaN;
            public float Clock2 = float.NaN;
            // The hero as the swarm opened, before the arrival chest.
            public float FinaleHealth = float.NaN;
            public float FinaleMaximum = float.NaN;
            public int FinaleLevel = -1;
            // The swarm's clock as the arrival chest opened; NaN when it never did.
            public float Chest = float.NaN;
            public float Swarm = float.NaN;
            public int Level = -1;
            public float Health = float.NaN;
            public float Maximum = float.NaN;
            public int Kills2 = -1;
            public int Kills3 = -1;
            public string Rooms2;
            // Floor 3 (docs/48): its seconds from floor 2's clear, its Warden's fight, the level at its gate, its gate's
            // hold; NaN or -1 in a file written before floor 3, or where the run never got there.
            public float Floor3 = float.NaN;
            public float Warden3 = float.NaN;
            public int LevelGate3 = -1;
            public float Hold3 = float.NaN;

            public bool ClearedFloor1 => !float.IsNaN(Floor2);
            public bool ClearedFloor2 => !float.IsNaN(Floor3) || !float.IsNaN(Finale);
            public bool ReachedFinale => !float.IsNaN(Finale);
            public bool WalkedOut => Result == "walked_out";
            public bool Died => Result == "died";
            public bool ChestOpened => !float.IsNaN(Chest);
        }

        public static List<Run> Read(string path) =>
            path != null && File.Exists(path) ? Read(File.ReadAllLines(path)) : new List<Run>();

        // The descent rows of a pacing file's lines, by their header's names; none in a file without them.
        public static List<Run> Read(IEnumerable<string> lines)
        {
            var runs = new List<Run>();
            string[] header = null;
            foreach (string line in lines)
            {
                string[] cells = line.Split('\t');
                if (cells.Length < 2 || cells[0] != Prefix)
                    continue;
                if (header == null)
                {
                    if (cells[1] == "seed")
                        header = cells;
                    continue;
                }
                if (!int.TryParse(cells[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
                    continue;
                string Cell(string name)
                {
                    int i = Array.IndexOf(header, name);
                    return i >= 0 && i < cells.Length ? cells[i].Trim() : "-";
                }
                float Number(string name) => Float(Cell(name));
                int Whole(string name) =>
                    int.TryParse(Cell(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : -1;
                string died = Cell("died_at");
                string rooms = Cell("rooms2");
                runs.Add(new Run
                {
                    Seed = seed,
                    Result = Cell("result"),
                    DiedAt = died == "-" ? null : died,
                    Floor1 = Number("floor1_s"),
                    Floor2 = Number("floor2_s"),
                    Finale = Number("finale_s"),
                    Descent = Number("descent_s"),
                    Warden1 = Number("warden1_s"),
                    Warden2 = Number("warden2_s"),
                    LevelGate1 = Whole("level_gate1"),
                    LevelGate2 = Whole("level_gate2"),
                    Uptime1 = Number("uptime1"),
                    Engaged1 = Number("engaged1"),
                    Uptime2 = Number("uptime2"),
                    Engaged2 = Number("engaged2"),
                    Uptime3 = Number("uptime3"),
                    Engaged3 = Number("engaged3"),
                    Hold2 = Number("hold2_s"),
                    Clock2 = Number("clock2"),
                    FinaleHealth = Number("finale_hp"),
                    FinaleMaximum = Number("finale_max_hp"),
                    FinaleLevel = Whole("finale_level"),
                    Chest = Number("chest_s"),
                    Swarm = Number("swarm_s"),
                    Level = Whole("level"),
                    Health = Number("hp"),
                    Maximum = Number("max_hp"),
                    Kills2 = Whole("kills2"),
                    Kills3 = Whole("kills3"),
                    Rooms2 = rooms == "-" ? null : rooms,
                    Floor3 = Number("floor3_s"),
                    Warden3 = Number("warden3_s"),
                    LevelGate3 = Whole("level_gate3"),
                    Hold3 = Number("hold3_s")
                });
            }
            return runs;
        }

        // "-" (or anything that is no number) is NaN; a Turkish Editor's decimal comma reads as a point.
        private static float Float(string text) =>
            float.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value : float.NaN;

        // One hero's rows: the Editor's file, the simulation on the same seeds down three floors (FoundryLevers.Floors = 3,
        // the file's cohort, frame-perfect as the Editor's autopilot is), and the batch's whole run where it went down
        // three floors (null otherwise).
        internal sealed class HeroRows
        {
            public string Hero;
            public string Source;
            public string Cohort;
            public List<Run> Editor;
            public FoundryBatch.Batch SameSeeds;
            public FoundryBatch.Batch Batch;
        }

        public static string Section(IReadOnlyList<HeroRows> heroes)
        {
            var md = new StringBuilder();
            md.AppendLine("## The Editor's Descent beside the simulation (HordePacingMeasurement with horde-floors.txt \"3\" or \"4\")").AppendLine();
            md.AppendLine("Editor: the scene's development autopilot down all three floors at a fixed 1/60 s, frame-perfect - floor 1, the checkpoint's Descend, floor 2 (its halls, the Deep Forge's first option, " +
                          "the Sentry Hall, the gate's hold for its clock, its Warden and gate), Descend again, then the Final Swarm to its exit or the hero's fall. Simulation: RunFoundry on the same seeds with " +
                          "FoundryLevers.Floors = 3 and the file's cohort (frame-perfect), then the batch's careful pilot where it went down three floors (P10 / P50 / P90). Floor 2 and the finale run from the " +
                          "checkpoint before them, the descent's advance delay included, as the simulation's do. Warden: the Warden's own fight, from walking out of the gate to its fall (the simulation's " +
                          "WardenSeconds), over the fights in which it fell. Level at a gate: as the Warden walks out. Gate wait: floor 2's hold for its clock, over its clears. Entering the finale: as the swarm " +
                          "opens, before its chest - health as a share of the maximum, and the level. Arrival chest: opened, of the runs that reached the finale, and the swarm's clock then (the Editor only). " +
                          "Engaged: a strike landed or a hit taken in the last 1.5 s, as a share of each floor's active seconds. The Descent: every floor together, over the walk-outs. " +
                          "Floor 3 (docs/48, Gloam Gardens): its seconds and its Warden's fight P50 over the runs that cleared floor 2, \"-\" for a file written before floor 3; the simulation plays FoundryLevers.Floors = 4.").AppendLine();
            md.AppendLine("| Hero | Source | Runs | Cleared floor 1 | Cleared floor 2 | Walked out | Deaths (floor.room:phase) | Floor 1 P50 | Floor 2 P50 | Floor 2 gate wait P50 | Floor 3 / its Warden P50 | Warden 1 / 2 P50 | Level at gate 1 / 2 P50 | Finale P50 | Entering the finale: health % / level P50 | Arrival chest opened, swarm clock P50 | The Descent P50 (min) | Engaged 1 / 2 / finale P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            var perSeed = new StringBuilder();
            foreach (HeroRows hero in heroes)
            {
                List<Run> e = hero.Editor;
                Run[] down = e.Where(r => r.ClearedFloor1).ToArray();
                Run[] two = e.Where(r => r.ClearedFloor2).ToArray();
                Run[] finale = e.Where(r => r.ReachedFinale).ToArray();
                Run[] walked = e.Where(r => r.WalkedOut).ToArray();
                md.AppendLine($"| {hero.Hero} | Editor ({hero.Source}{(hero.Cohort != null ? ", " + hero.Cohort : string.Empty)}) | {e.Count} | {Of(down.Length, e.Count)} | " +
                              $"{Of(two.Length, e.Count)} | {Of(walked.Length, e.Count)} | {Deaths(e.Where(r => r.Died).Select(r => r.DiedAt ?? "?"))} | " +
                              $"{M(e.Select(r => r.Floor1))} | {M(down.Select(r => r.Floor2))} | {M(two.Select(r => r.Hold2))} | " +
                              $"{M(two.Select(r => r.Floor3))} / {M(two.Select(r => r.Warden3))} | " +
                              $"{M(down.Select(r => r.Warden1))} / {M(two.Select(r => r.Warden2))} | " +
                              $"{M(e.Select(r => (float)r.LevelGate1))} / {M(down.Select(r => (float)r.LevelGate2))} | {M(finale.Select(r => r.Finale))} | " +
                              $"{M(finale.Select(r => 100f * r.FinaleHealth / Math.Max(1f, r.FinaleMaximum)))}% / {M(finale.Select(r => (float)r.FinaleLevel))} | " +
                              $"{Of(finale.Count(r => r.ChestOpened), finale.Length)}, {M(finale.Where(r => r.ChestOpened).Select(r => r.Chest), "0.0")} | " +
                              $"{Minutes(walked.Select(r => r.Descent))} | " +
                              $"{M(e.Select(r => r.Engaged1), "0.00")} / {M(down.Select(r => r.Engaged2), "0.00")} / {M(finale.Select(r => r.Engaged3), "0.00")} |");
                if (hero.SameSeeds != null)
                    md.AppendLine(SimulationRow(hero.Hero, "Simulation, the same seeds", hero.SameSeeds, M, " / "));
                if (hero.Batch != null)
                    md.AppendLine(SimulationRow(hero.Hero, $"Simulation, {hero.Batch.Runs.Length} seeds (P10 / P50 / P90)", hero.Batch, S, "; "));

                perSeed.AppendLine($"| {hero.Hero} | Seed | Editor: result, floors 1 / 2 / finale, the Descent, Warden 1 / 2, level at gate 1 / 2, entering the finale (health / maximum, level), arrival chest | Simulation: the same |");
                perSeed.AppendLine("|---|---|---|---|");
                foreach (Run r in e)
                {
                    DescentSimulation.FoundryResult s = hero.SameSeeds?.Runs.FirstOrDefault(x => x.Seed == r.Seed);
                    perSeed.AppendLine($"| {hero.Hero} | {r.Seed} | {(r.Died ? "died " + (r.DiedAt ?? "?") : r.Result)}, {N(r.Floor1)} / {N(r.Floor2)} / {N(r.Finale)}, {N(r.Descent)}, " +
                                       $"{N(r.Warden1)} / {N(r.Warden2)}, L{L(r.LevelGate1)} / L{L(r.LevelGate2)}, {N(r.FinaleHealth)} / {N(r.FinaleMaximum)} L{L(r.FinaleLevel)}, " +
                                       $"{(r.ChestOpened ? "at " + r.Chest.ToString("0.0", CultureInfo.InvariantCulture) + " s" : r.ClearedFloor2 ? "not opened" : "-")} | " +
                                       $"{(s == null ? "-" : SimulationSeed(s))} |");
                }
                perSeed.AppendLine();
            }
            md.AppendLine().Append(perSeed);
            return md.ToString();
        }

        private static string SimulationRow(string hero, string source, FoundryBatch.Batch b, Func<IEnumerable<float>, string, string> stat, string pair)
        {
            DescentSimulation.FoundryResult[] runs = b.Runs;
            DescentSimulation.FoundryResult[] down = runs.Where(r => r.NextFloor != null).ToArray();
            DescentSimulation.FoundryResult[] finale = runs.Where(r => FoundryBatch.FinaleOf(r) != null).ToArray();
            DescentSimulation.FoundryResult[] walked = finale.Where(r => FoundryBatch.FinaleOf(r).WalkedOut).ToArray();
            DescentSimulation.FoundryResult[] floorTwoClears = down.Where(r => r.NextFloor.Cleared).Select(r => r.NextFloor).ToArray();
            DescentSimulation.FoundryResult[] three = floorTwoClears.Select(r => r.NextFloor)
                .Where(r => r != null && !FoundryBatch.IsFinale(r)).ToArray();
            DescentSimulation.FoundryResult[] finales = finale.Select(FoundryBatch.FinaleOf).ToArray();
            return $"| {hero} | {source} | {runs.Length} | {Of(down.Length, runs.Length)} | {Of(floorTwoClears.Length, runs.Length)} | {Of(walked.Length, runs.Length)} | " +
                   $"{Deaths(runs.Select(Death).Where(d => d != null))} | " +
                   $"{stat(runs.Select(r => r.FloorSeconds), "0")} | {stat(down.Select(r => r.NextFloor.FloorSeconds), "0")} | " +
                   $"{stat(floorTwoClears.Select(r => r.GateWaitSeconds), "0")} | " +
                   $"{stat(three.Select(r => r.FloorSeconds), "0")}{pair}{stat(three.Select(r => r.WardenSeconds), "0")} | " +
                   $"{stat(runs.Select(r => r.WardenSeconds), "0")}{pair}{stat(down.Select(r => r.NextFloor.WardenSeconds), "0")} | " +
                   $"{stat(runs.Select(r => (float)r.LevelAtGate), "0")}{pair}{stat(down.Select(r => (float)r.NextFloor.LevelAtGate), "0")} | " +
                   $"{stat(finales.Select(r => r.FloorSeconds), "0")} | " +
                   $"{stat(finales.Select(r => 100f * r.SwarmEntryHealth / Math.Max(1f, r.SwarmEntryMaximum)), "0")}%{pair}{stat(finales.Select(r => (float)r.SwarmEntryLevel), "0")} | " +
                   $"{Of(finales.Count(r => r.ChestsOpened > 0), finales.Length)}, - | " +
                   $"{Minutes(walked.Select(FoundryBatch.DescentSeconds))} | " +
                   $"{stat(runs.Select(r => r.Engaged), "0.00")}{pair}{stat(down.Select(r => r.NextFloor.Engaged), "0.00")}{pair}{stat(finales.Select(r => r.Engaged), "0.00")} |";
        }

        private static string SimulationSeed(DescentSimulation.FoundryResult s)
        {
            DescentSimulation.FoundryResult two = s.NextFloor;
            DescentSimulation.FoundryResult finale = FoundryBatch.FinaleOf(s);
            string result = finale != null && finale.WalkedOut ? "walked_out"
                : Death(s) != null ? "died " + Death(s)
                : "timeout";
            return $"{result}, {N(s.FloorSeconds)} / {N(two?.FloorSeconds ?? float.NaN)} / {N(finale?.FloorSeconds ?? float.NaN)}, " +
                   $"{N(finale != null && finale.WalkedOut ? FoundryBatch.DescentSeconds(s) : float.NaN)}, " +
                   $"{N(s.WardenSeconds)} / {N(two?.WardenSeconds ?? float.NaN)}, L{L(s.LevelAtGate)} / L{L(two?.LevelAtGate ?? -1)}, " +
                   $"{N(finale?.SwarmEntryHealth ?? float.NaN)} / {N(finale?.SwarmEntryMaximum ?? float.NaN)} L{L(finale?.SwarmEntryLevel ?? -1)}, " +
                   $"{(finale == null ? "-" : finale.ChestsOpened > 0 ? "opened" : "not opened")}";
        }

        // Where a simulated run down the floors died, as the Editor names it (floor.room: phase); null for none.
        private static string Death(DescentSimulation.FoundryResult run)
        {
            int floor = 1;
            for (DescentSimulation.FoundryResult r = run; r != null; r = r.NextFloor, floor++)
                if (r.Outcome == DescentSimulation.FoundryOutcome.Died)
                    return $"{floor}.{(r.DeathRoom > 0 ? r.DeathRoom : 0)}:{r.DeathPhase ?? "?"}";
            return null;
        }

        private static string Deaths(IEnumerable<string> where)
        {
            string[] all = where.ToArray();
            return all.Length == 0 ? "-"
                : string.Join(", ", all.GroupBy(w => w).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                    .Select(g => $"{g.Key} x{g.Count()}"));
        }

        private static string Of(int part, int whole) =>
            whole == 0 ? "-" : $"{part} ({(100.0 * part / whole).ToString("0", CultureInfo.InvariantCulture)}%)";

        private static IEnumerable<float> Kept(IEnumerable<float> values) => values.Where(v => !float.IsNaN(v) && v >= 0f);

        private static string M(IEnumerable<float> values, string format = "0")
        {
            double value = FoundryBatch.Median(Kept(values));
            return double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string S(IEnumerable<float> values, string format = "0")
        {
            float[] kept = Kept(values).ToArray();
            if (kept.Length == 0)
                return "-";
            string P(double p) => FoundryBatch.Percentile(kept, p).ToString(format, CultureInfo.InvariantCulture);
            return $"{P(10)} / {P(50)} / {P(90)}";
        }

        // Seconds P50 and, beside it, minutes.
        private static string Minutes(IEnumerable<float> seconds)
        {
            double value = FoundryBatch.Median(Kept(seconds));
            return double.IsNaN(value) ? "-"
                : $"{value.ToString("0", CultureInfo.InvariantCulture)} ({(value / 60.0).ToString("0.0", CultureInfo.InvariantCulture)})";
        }

        private static string N(float value) =>
            float.IsNaN(value) || value < 0f ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

        private static string L(int level) => level >= 0 ? level.ToString(CultureInfo.InvariantCulture) : "-";
    }
}
