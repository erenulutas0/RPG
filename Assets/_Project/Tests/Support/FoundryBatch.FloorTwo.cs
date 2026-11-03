using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cryptforge.Combat;

namespace Cryptforge.Tests
{
    // Floor 2 of the climb in the batch's report (FoundryLevers.Floors = 2; FOUNDRY_FLOORS=2 for the batch report): what
    // floor 1's clears meet below its checkpoint - floor 2's clears and deaths, its length and where it goes, its Warden,
    // the level and the floor's clock at its gate - over the runs that cleared floor 1 and went down.
    internal static partial class FoundryBatch
    {
        // A batch's floor 2: the floor-2 results of its runs that cleared floor 1 and went down, as a batch of its own.
        public static Batch FloorTwo(Batch batch) => new Batch
        {
            Hero = batch.Hero,
            Levers = batch.Levers,
            WallSeconds = batch.WallSeconds,
            Runs = batch.Runs.Where(run => run.NextFloor != null).Select(run => run.NextFloor).ToArray()
        };

        public static string FloorTwoReport(IReadOnlyList<Batch> batches)
        {
            var md = new StringBuilder();
            md.AppendLine("## Floor 2 (Quicksilver Vaults) after a floor-1 clear").AppendLine();
            md.AppendLine("FoundryLevers.Floors = 2: every run that cleared floor 1 answers the checkpoint with Descend, as the autopilot does, and plays floor 2 - " +
                          "its first hall where the descent leaves the hero, the next two and the Sentry Hall (its elite) at the north court, the workyard and the pit's rim, the Deep Forge between, " +
                          "the Warden's Vault last - climb-scaled at floor 2 (1.4 times floor 1's enemy health and damage), with Cursed Gold's +20% enemy damage, no hall surges, no second elite " +
                          "and the gate open as its room clears. Its Forge Warden has WardenRule's 8.5 times its health and holds its weapon through each slam, and two Cinder Mites walk out of the gate beside it. " +
                          "The hero goes down with its level, weapons, tomes, difficulty and health; the altars answer again; the horde and the floor's clock start again with the first room. " +
                          "Floor 2's seconds run from the checkpoint, the descent's 1 s advance delay included. Nothing in the Editor has measured floor 2: no parity table stands behind these numbers.").AppendLine();
            FloorTwoOutcomes(md, batches);
            FloorTwoTime(md, batches);
            FloorTwoWarden(md, batches);
            FloorTwoGate(md, batches);
            return md.ToString();
        }

        private static void FloorTwoOutcomes(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Floor 2: who goes down and how it ends").AppendLine();
            md.AppendLine("Health going down: what floor 1 left the hero, as a share of its maximum - nothing heals it on the way. " +
                          "Where: floor 2's room (1-3 its halls, 4 the forge, 5 the Sentry Hall, 6 the Warden's Vault) and what the hero was doing.").AppendLine();
            md.AppendLine("| Hero | Runs | Went down (cleared floor 1) | Health going down P10 / P50 / P90 (% of maximum) | Cleared floor 2 (of those) | Cleared both (of all runs) | Died on floor 2 | Where (room: phase, count) | Killing blow (count) | Timed out |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                Batch two = FloorTwo(b);
                string health = Spread(b.Runs.Where(r => r.NextFloor != null).Select(r => 100f * r.HeroHealth / Math.Max(1f, r.HeroMaximum)));
                DescentSimulation.FoundryResult[] died = two.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
                string where = string.Join(", ", died.GroupBy(r => $"{r.DeathRoom}: {r.DeathPhase}").OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key} x{g.Count()}"));
                string killers = string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key} x{g.Count()}"));
                int timedOut = two.Runs.Count(r => r.Outcome == DescentSimulation.FoundryOutcome.Timeout);
                md.AppendLine($"| {b.Hero.Name} | {b.Runs.Length} | {two.Runs.Length} | {health} | {two.ClearedCount} ({Pct(two.ClearedCount, two.Runs.Length)}) | " +
                              $"{two.ClearedCount} ({Pct(two.ClearedCount, b.Runs.Length)}) | {died.Length} ({Pct(died.Length, two.Runs.Length)}) | " +
                              $"{(where.Length > 0 ? where : "-")} | {(killers.Length > 0 ? killers : "-")} | {timedOut} |");
            }
            md.AppendLine();
        }

        private static void FloorTwoTime(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Floor 2 time (active seconds from the checkpoint; the segments over its clears, P10 / P50 / P90)").AppendLine();
            md.AppendLine("Descent: the checkpoint to the first room's wave. Walk, node fights, gate wait and exit as in floor 1's table; Warden is its whole wave, from walking out of the gate to the last of the three falling, " +
                          "the reveal included (the Warden's own fight is the next table's). Both floors: floor 1 and floor 2 together, over the runs that cleared both.").AppendLine();
            RoomKind[] kinds = batches.SelectMany(b => FloorTwo(b).Runs).Select(r => r.Kinds).FirstOrDefault() ?? new RoomKind[0];
            md.AppendLine("| Hero | Floor 2 (all who went down) | Floor 2 cleared | Descent | Walk | Node fights | Gate wait | Warden | Exit | Rooms " + RoomNames(kinds) +
                          " (P50, clears) | Uptime / engaged P50 | Both floors, cleared |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                Batch two = FloorTwo(b);
                DescentSimulation.FoundryResult[] cleared = two.Cleared.ToArray();
                int rooms = cleared.Length > 0 ? cleared[0].RoomSeconds.Length : 0;
                var perRoom = new List<string>();
                for (int i = 0; i < rooms; i++)
                    perRoom.Add(F(Median(cleared.Select(r => r.RoomSeconds[i]))));
                IEnumerable<float> both = b.Runs.Where(r => r.NextFloor != null && r.NextFloor.Cleared)
                    .Select(r => r.FloorSeconds + r.NextFloor.FloorSeconds);
                md.AppendLine($"| {b.Hero.Name} | {Spread(two.Runs.Select(r => r.FloorSeconds))} | {Spread(cleared.Select(r => r.FloorSeconds))} | " +
                              $"{Spread(cleared.Select(r => r.DescentSeconds), "0.0")} | {Spread(cleared.Select(r => r.WalkTotal))} | {Spread(cleared.Select(r => r.NodeSeconds))} | " +
                              $"{Spread(cleared.Select(r => r.GateWaitSeconds))} | {Spread(cleared.Select(r => r.BossSeconds))} | {Spread(cleared.Select(r => r.ExitSeconds))} | " +
                              $"{string.Join(" / ", perRoom)} | {F(Median(two.Runs.Select(r => r.Uptime)), "0.00")} / {F(Median(two.Runs.Select(r => r.Engaged)), "0.00")} | " +
                              $"{Spread(both)} |");
            }
            md.AppendLine();
        }

        // Floor 2's Warden beside the same runs' floor-1 Warden: the fight, its health, the hero's health and what the fight
        // cost by source, and the deaths at it.
        private static void FloorTwoWarden(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Floor 2's Warden").AppendLine();
            md.AppendLine("Fight: the fights in which the Warden fell, from walking out of the gate to its own fall, the reveal included. Cost: the hero's health lost while the Warden stood, by what dealt it (escort: floor 2's two Cinder Mites), " +
                          "mean over the runs that reached it. At the Warden: the heroes who fell while its wave stood, of the runs that reached it - with, in brackets, those the pit burnt in the eruption that also felled the wave, which count as felled too. " +
                          "The floor-1 rows are the same runs' floor-1 Warden.").AppendLine();
            md.AppendLine("| Hero | Floor | Reached | Felled | Fight P10 / P50 / P90 | Felled within 5 s | Warden health P50 | Hero health at the gate P50 (maximum P50) | Fight cost mean: Warden / slams / escort / horde / pit (total) | Died at the Warden: killing blow | Slams / hits a fight (mean) |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");

            void Row(string hero, string floor, DescentSimulation.FoundryResult[] runs)
            {
                DescentSimulation.FoundryResult[] reached = runs.Where(r => r.LevelAtGate >= 0).ToArray();
                DescentSimulation.FoundryResult[] felled = reached.Where(r => r.WardenSeconds >= 0f).ToArray();
                DescentSimulation.FoundryResult[] diedThere = reached
                    .Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died && r.DeathPhase == "boss").ToArray();
                int together = diedThere.Count(r => r.BossSeconds >= 0f);
                double Mean(Func<DescentSimulation.FoundryResult, float> value) =>
                    reached.Length == 0 ? double.NaN : reached.Average(value);
                double total = Mean(r => r.TakenFromWarden + r.TakenFromSlams + r.FightTakenFromEscort + r.FightTakenFromHorde + r.FightTakenFromPit);
                string killers = string.Join(", ", diedThere.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key} x{g.Count()}")) + (together > 0 ? $" ({together} with its wave)" : string.Empty);
                md.AppendLine($"| {hero} | {floor} | {reached.Length} | {felled.Length} | {Spread(felled.Select(r => r.WardenSeconds))} | " +
                              $"{felled.Count(r => r.WardenSeconds <= 5f)} | {F(Median(reached.Select(r => r.WardenHealth)))} | " +
                              $"{F(Median(reached.Select(r => r.HeroHealthAtGate)))} ({F(Median(reached.Select(r => r.HeroMaximumAtGate)))}) | " +
                              $"{F(Mean(r => r.TakenFromWarden))} / {F(Mean(r => r.TakenFromSlams))} / {F(Mean(r => r.FightTakenFromEscort))} / " +
                              $"{F(Mean(r => r.FightTakenFromHorde))} / {F(Mean(r => r.FightTakenFromPit))} ({F(total)}) | " +
                              $"{diedThere.Length} ({Pct(diedThere.Length, reached.Length)}){(killers.Length > 0 ? ": " + killers : string.Empty)} | " +
                              $"{F(reached.Length > 0 ? reached.Average(r => r.Slams) : double.NaN, "0.0")} / " +
                              $"{F(reached.Length > 0 ? reached.Average(r => r.SlamHits) : double.NaN, "0.00")} |");
            }

            foreach (Batch b in batches)
            {
                DescentSimulation.FoundryResult[] wentDown = b.Runs.Where(r => r.NextFloor != null).ToArray();
                Row(b.Hero.Name, "2", wentDown.Select(r => r.NextFloor).ToArray());
                Row(b.Hero.Name, "1 (the same runs)", wentDown);
            }
            md.AppendLine();
        }

        // The level the hero brings down and the one it meets floor 2's Warden at, and floor 2's clock at the gate and at
        // the clear, against the overrun.
        private static void FloorTwoGate(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            string overrun = HordeClock.TargetSeconds.ToString("0", CultureInfo.InvariantCulture);
            md.AppendLine("### Level and the floor's clock at floor 2's gate").AppendLine();
            md.AppendLine("Level at the start: floor 1's at its end, after its sweep. Clock: floor 2's own, from its first room, frozen through choices and the forge. " +
                          $"Clock at clear: as its Warden's wave fell. Into the overrun: fights reached before {overrun} s and still on at {overrun} s (of the runs that reached the Warden), then the clears past {overrun} s.").AppendLine();
            md.AppendLine("| Hero | Level at the start P50 | Level at the gate P10 / P50 / P90 | Weapons / tomes at the gate P50 | Difficulty at the gate P50 | Clock at the gate P10 / P50 / P90 | Clock at clear P10 / P50 / P90 | Into the overrun: fights, clears |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                Batch two = FloorTwo(b);
                DescentSimulation.FoundryResult[] gated = two.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                DescentSimulation.FoundryResult[] cleared = two.Cleared.ToArray();
                int crossed = gated.Count(r => r.ClockAtGate < HordeClock.TargetSeconds && r.Clock >= HordeClock.TargetSeconds);
                int late = cleared.Count(r => r.Clock >= HordeClock.TargetSeconds);
                md.AppendLine($"| {b.Hero.Name} | {F(Median(b.Runs.Where(r => r.NextFloor != null).Select(r => (float)r.Level)))} | " +
                              $"{Spread(gated.Select(r => (float)r.LevelAtGate))} | " +
                              $"{F(Median(gated.Select(r => (float)r.WeaponsAtGate)))} / {F(Median(gated.Select(r => (float)r.TomesAtGate)))} | " +
                              $"{F(Median(gated.Select(r => r.DifficultyAtGate)), "0.00")} | {Spread(gated.Select(r => r.ClockAtGate))} | " +
                              $"{Spread(cleared.Select(r => r.Clock))} | {crossed} ({Pct(crossed, gated.Length)}), {late} ({Pct(late, cleared.Length)}) |");
            }
            md.AppendLine();
        }

        // Every run that went down, one line each: what it brought from floor 1, then floor 2 as the report reads it.
        public static string FloorTwoCsv(IEnumerable<Batch> batches)
        {
            var csv = new StringBuilder();
            csv.AppendLine("hero,levers,seed,floor1_s,floor1_level_end,floor1_hp_end,floor1_max_hp_end,outcome,floor_s,descent_s,walk_s,node_s,gate_wait_s,boss_s,warden_s,exit_s," +
                           "level_gate,level_end,hp,max_hp,uptime,engaged,clock_gate,clock_end,warden_hp,boss_left,death_room,death_phase,killer,difficulty_gate,hp_gate,max_hp_gate," +
                           "fight_warden,fight_slams,fight_escort,fight_horde,fight_pit,slams,slam_hits,eruptions,pit_hero,altars,chests,weapons,tomes,room_s,walks");
            foreach (Batch b in batches)
            foreach (DescentSimulation.FoundryResult one in b.Runs)
            {
                DescentSimulation.FoundryResult r = one.NextFloor;
                if (r == null)
                    continue;
                string Num(float v, string format = "0.00") => v.ToString(format, CultureInfo.InvariantCulture);
                csv.AppendLine(string.Join(",", one.Hero, Quote(one.Levers), one.Seed, Num(one.FloorSeconds), one.Level, Num(one.HeroHealth, "0.0"),
                    Num(one.HeroMaximum, "0"), r.Outcome, Num(r.FloorSeconds), Num(r.DescentSeconds), Num(r.WalkTotal), Num(r.NodeSeconds),
                    Num(r.GateWaitSeconds), Num(r.BossSeconds), Num(r.WardenSeconds), Num(r.ExitSeconds), r.LevelAtGate, r.Level,
                    Num(r.HeroHealth, "0.0"), Num(r.HeroMaximum, "0"), Num(r.Uptime, "0.000"), Num(r.Engaged, "0.000"), Num(r.ClockAtGate),
                    Num(r.Clock), Num(r.WardenHealth, "0"), Num(r.BossLeft), r.DeathRoom, Quote(r.DeathPhase), Quote(r.Killer),
                    Num(r.DifficultyAtGate), Num(r.HeroHealthAtGate, "0.0"), Num(r.HeroMaximumAtGate, "0"), Num(r.TakenFromWarden, "0.0"),
                    Num(r.TakenFromSlams, "0.0"), Num(r.FightTakenFromEscort, "0.0"), Num(r.FightTakenFromHorde, "0.0"),
                    Num(r.FightTakenFromPit, "0.0"), r.Slams, r.SlamHits, r.Eruptions, r.EruptionsOnHero, r.AltarsTaken, r.ChestsOpened,
                    Quote(r.Weapons), Quote(r.Tomes), Quote(string.Join(" ", r.RoomSeconds.Select(s => Num(s, "0.0")))),
                    Quote(string.Join(" ", r.WalkSeconds.Select(s => Num(s, "0.0"))))));
            }
            return csv.ToString();
        }
    }
}
