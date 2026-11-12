using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Tests
{
    // The autopilot's cohorts over a seed batch (docs/41 step 5; docs/40 section 9.4, whose cohorts are run on the same
    // seeds so that a policy is compared with a policy and not with luck): the shipped floor 1 played by each cohort
    // (AutopilotCohort) for each hero, read as one table - clears, deaths, the floor's length, the Warden, engagement,
    // the level at the gate and the clock - and a second of what the cohort's steer did; with the climb's floor 2 on
    // (FoundryLevers.Floors = 2), a third of floor 2 after each cohort's floor-1 clears. FoundryBatchReport writes it on
    // demand (FOUNDRY_COHORTS=1, FOUNDRY_FLOORS=2). Pure: it only runs DescentSimulation.RunFoundry and counts.
    internal static class FoundryCohorts
    {
        // Where each cohort comes from, for the table's legend.
        private static string Source(AutopilotCohort cohort)
        {
            if (cohort == AutopilotCohort.Careful)
                return "the default: the pilot of 2026-11-04 (docs/40 9.4's Skilled bot reads the telegraphs)";
            if (cohort == AutopilotCohort.Standing)
                return "docs/41 step 5's standing policy";
            if (cohort == AutopilotCohort.FirstCard)
                return "docs/41 step 5's first-card picker, the careful footwork";
            if (cohort == AutopilotCohort.Clumsy)
                return "the pilot before 2026-11-04's fixes (main d6664d2); docs/40 9.4's Casual in shape";
            if (cohort == AutopilotCohort.AutoFarmer)
                return "docs/40 9.4's Auto Farmer: it moves, no active skill, the first card, no dodge";
            return "-";
        }

        // The shipped floor in the cohort's hands, down as many floors of the climb as asked (FoundryLevers.Floors).
        // With realism (PilotRealism, research 2026-11-04), the cohort plays with a human's reaction, aim and attention.
        public static DescentSimulation.FoundryLevers Levers(AutopilotCohort cohort, int floors = 1, PilotRealism realism = null)
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("cohort " + cohort.Name +
                                                                                (realism != null ? ", " + realism.Name : string.Empty));
            levers.Cohort = cohort;
            levers.Floors = floors;
            levers.Realism = realism;
            return levers;
        }

        // Every cohort's batch for each hero, in AutopilotCohort.Cohorts' order, each down the same floors. The careful
        // cohort is the shipped floor's own batch where one is given for the same seeds and floors (careful, in the heroes'
        // order).
        public static List<(AutopilotCohort Cohort, FoundryBatch.Batch[] Batches)> Run(
            IReadOnlyList<DescentSimulation.FoundryHero> heroes, int[] seeds, int parallelism,
            IReadOnlyList<FoundryBatch.Batch> careful = null, int floors = 1, PilotRealism realism = null)
        {
            var cohorts = new List<(AutopilotCohort, FoundryBatch.Batch[])>();
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
            {
                var batches = new FoundryBatch.Batch[heroes.Count];
                for (int h = 0; h < heroes.Count; h++)
                {
                    FoundryBatch.Batch shipped = careful != null && h < careful.Count ? careful[h] : null;
                    batches[h] = realism == null && cohort == AutopilotCohort.Careful && shipped != null && shipped.Hero.Id == heroes[h].Id &&
                                 shipped.Levers.Name == FoundryBatch.Shipped().Name && shipped.Levers.Cohort == AutopilotCohort.Careful &&
                                 shipped.Levers.Floors == floors && shipped.Runs.Select(r => r.Seed).SequenceEqual(seeds)
                        ? shipped
                        : FoundryBatch.Run(heroes[h], Levers(cohort, floors, realism), seeds, parallelism);
                }
                cohorts.Add((cohort, batches));
            }
            return cohorts;
        }

        public static string Section(IReadOnlyList<(AutopilotCohort Cohort, FoundryBatch.Batch[] Batches)> cohorts)
        {
            var md = new StringBuilder();
            FoundryBatch.Batch any = cohorts.SelectMany(c => c.Batches).FirstOrDefault();
            int[] seeds = any != null ? any.Runs.Select(r => r.Seed).ToArray() : new int[0];
            md.AppendLine("## The autopilot's cohorts (docs/41 step 5, docs/40 section 9.4)").AppendLine();
            md.AppendLine($"The shipped floor 1 played by each cohort of the development autopilot (AutopilotCohort), seeds {SeedRange(seeds)} a hero, " +
                          "the same seeds for every cohort. A cohort is the careful pilot less habits it has; none is invented. In the scene a word of " +
                          "development/autopilot.txt names one, in HordePacingMeasurement TestResults/horde-cohort.txt.").AppendLine();
            md.AppendLine("| Cohort | Where it comes from | The careful pilot's habits it drops |");
            md.AppendLine("|---|---|---|");
            foreach ((AutopilotCohort cohort, FoundryBatch.Batch[] _) in cohorts)
                md.AppendLine($"| {cohort.Name} | {Source(cohort)} | {Dropped(cohort)} |");
            md.AppendLine();

            md.AppendLine("Cleared: of the seeds. Deaths: in which room (the boss's room as boss) and by what blow. Floor: active seconds, every run; the cleared floor P50 of the clears. " +
                          "Warden: the felled fights, from walking out of the gate to falling. Engaged: a strike of any weapon landed, or a hit taken, in the last 1.5 s, " +
                          "over the floor (P50 of every run). Level at the gate: as the Warden walked out (P50 of the runs that got there). Clock at clear: the floor's clock at the end of a clear. " +
                          "Paired with careful: seeds both cleared / only the careful pilot cleared / only this cohort cleared.").AppendLine();
            md.AppendLine("| Cohort | Hero | Cleared | Deaths: where, killing blow | Floor P50 / P90 | Cleared floor P50 | Warden fight P50 / P90 | Engaged uptime P50 | Level at the gate P50 | Clock at clear P50 / P90 | Paired with careful |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            FoundryBatch.Batch[] careful = cohorts.FirstOrDefault(c => c.Cohort == AutopilotCohort.Careful).Batches;
            foreach ((AutopilotCohort cohort, FoundryBatch.Batch[] batches) in cohorts)
                for (int h = 0; h < batches.Length; h++)
                {
                    FoundryBatch.Batch b = batches[h];
                    DescentSimulation.FoundryResult[] cleared = b.Cleared.ToArray();
                    DescentSimulation.FoundryResult[] felled = b.Runs.Where(r => r.BossSeconds >= 0f).ToArray();
                    DescentSimulation.FoundryResult[] gated = b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                    md.AppendLine($"| {cohort.Name} | {b.Hero.Name} | {b.ClearedCount} ({Pct(b.ClearedCount, b.Runs.Length)}) | {Deaths(b)} | " +
                                  $"{P(b.Runs.Select(r => r.FloorSeconds), 50)} / {P(b.Runs.Select(r => r.FloorSeconds), 90)} | {P(cleared.Select(r => r.FloorSeconds), 50)} | " +
                                  $"{P(felled.Select(r => r.BossSeconds), 50)} / {P(felled.Select(r => r.BossSeconds), 90)} | " +
                                  $"{P(b.Runs.Select(r => r.Engaged), 50, "0.00")} | {P(gated.Select(r => (float)r.LevelAtGate), 50)} | " +
                                  $"{P(cleared.Select(r => r.Clock), 50)} / {P(cleared.Select(r => r.Clock), 90)} | " +
                                  $"{(cohort == AutopilotCohort.Careful || careful == null || h >= careful.Length ? "-" : Paired(careful[h], b))} |");
                }
            md.AppendLine();

            md.AppendLine("### What each cohort's steer did").AppendLine();
            md.AppendLine("Seconds a floor the autopilot's steer spent kiting, leaving the Warden's mark, sheltering from a spill and drawing a node fight to the pit's rim (P50 of every run); " +
                          "the ability's uses; the Warden's slams that landed and the spills that burnt the hero (mean a run that reached the Warden, mean a run); the weapons held at the gate; " +
                          "and the hero's health lost a run (mean) by what dealt it.").AppendLine();
            md.AppendLine("| Cohort | Hero | Kite / escape / shelter / lure s P50 | Ability uses P50 | Slam hits a Warden fight | Spills on the hero | Weapons at the gate P50 | Health lost mean: wave / horde / Warden / slams / pit |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach ((AutopilotCohort cohort, FoundryBatch.Batch[] batches) in cohorts)
                foreach (FoundryBatch.Batch b in batches)
                {
                    DescentSimulation.FoundryResult[] gated = b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                    md.AppendLine($"| {cohort.Name} | {b.Hero.Name} | {P(b.Runs.Select(r => r.KiteSeconds), 50)} / {P(b.Runs.Select(r => r.EscapeSeconds), 50)} / " +
                                  $"{P(b.Runs.Select(r => r.ShelterSeconds), 50)} / {P(b.Runs.Select(r => r.LureSeconds), 50)} | " +
                                  $"{P(b.Runs.Select(r => (float)r.AbilityUses), 50)} | {Mean(gated.Select(r => (float)r.SlamHits), "0.00")} | " +
                                  $"{Mean(b.Runs.Select(r => (float)r.EruptionsOnHero), "0.0")} | {P(gated.Select(r => (float)r.WeaponsAtGate), 50)} | " +
                                  $"{Mean(b.Runs.Select(r => r.TakenFromWave))} / {Mean(b.Runs.Select(r => r.TakenFromHorde))} / {Mean(b.Runs.Select(r => r.TakenFromWarden))} / " +
                                  $"{Mean(b.Runs.Select(r => r.TakenFromSlams))} / {Mean(b.Runs.Select(r => r.TakenFromPit))} |");
                }
            md.AppendLine();
            if (cohorts.Any(c => c.Batches.Any(b => b.Levers.Floors >= 2)))
                FloorTwo(md, cohorts, careful);
            if (cohorts.Any(c => c.Batches.Any(b => b.Levers.Floors >= 3)))
                FloorThree(md, cohorts, careful);
            if (cohorts.Any(c => c.Batches.Any(b => b.Levers.Floors >= DescentSimulation.FinaleFloor)))
                md.Append(FoundryBatch.FinaleSection(FinaleRows(cohorts)));
            return md.ToString();
        }

        // Floor 2 (Quicksilver Vaults) after each cohort's floor-1 clears, as FoundryBatch.FloorTwoReport reads the shipped
        // batch: who went down and with what health, who cleared it and both floors, where and by what blow the rest fell,
        // floor 2's Warden, and the floor-2 clears' length and level at the gate.
        private static void FloorTwo(StringBuilder md, IReadOnlyList<(AutopilotCohort Cohort, FoundryBatch.Batch[] Batches)> cohorts,
            FoundryBatch.Batch[] careful)
        {
            md.AppendLine("### Floor 2 (Quicksilver Vaults) after a floor-1 clear").AppendLine();
            md.AppendLine("Every run that cleared floor 1 answers the checkpoint with Descend and plays floor 2 in the same hands, with the health, level, weapons and tomes floor 1 left it " +
                          "(FoundryBatch.FloorTwoReport has the shipped pilot's floor 2 in full). Went down: floor 1's clears. Health going down: what floor 1 left the hero, as a share of its maximum (P50). " +
                          "Cleared floor 2: of those who went down; both floors: of all the seeds. Deaths: floor 2's room (1-3 its halls, 4 the Deep Forge, 5 the Sentry Hall, the boss the Warden's Vault) and the blow. " +
                          "Warden: floor 2's Warden's own fight, from walking out of the gate to its fall, over the fights in which it fell. Floor 2: active seconds from the checkpoint, its clears. " +
                          "Paired with careful: seeds on which both / only the careful pilot / only this cohort cleared both floors.").AppendLine();
            md.AppendLine("| Cohort | Hero | Went down | Health going down P50 | Cleared floor 2 (of those) | Cleared both (of all) | Deaths on floor 2: where, killing blow | Floor 2 Warden fight P50 / P90 | Floor 2 cleared P50 | Level at floor 2's gate P50 | Paired with careful, both floors |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach ((AutopilotCohort cohort, FoundryBatch.Batch[] batches) in cohorts)
                for (int h = 0; h < batches.Length; h++)
                {
                    FoundryBatch.Batch b = batches[h];
                    DescentSimulation.FoundryResult[] down = b.Runs.Where(r => r.NextFloor != null).ToArray();
                    FoundryBatch.Batch two = FoundryBatch.FloorTwo(b);
                    DescentSimulation.FoundryResult[] felled = two.Runs.Where(r => r.WardenSeconds >= 0f).ToArray();
                    DescentSimulation.FoundryResult[] gated = two.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                    md.AppendLine($"| {cohort.Name} | {b.Hero.Name} | {down.Length} | " +
                                  $"{P(down.Select(r => 100f * r.HeroHealth / Math.Max(1f, r.HeroMaximum)), 50)}% | " +
                                  $"{two.ClearedCount} ({Pct(two.ClearedCount, two.Runs.Length)}) | {two.ClearedCount} ({Pct(two.ClearedCount, b.Runs.Length)}) | " +
                                  $"{Deaths(two)} | {P(felled.Select(r => r.WardenSeconds), 50)} / {P(felled.Select(r => r.WardenSeconds), 90)} | " +
                                  $"{P(two.Cleared.Select(r => r.FloorSeconds), 50)} | {P(gated.Select(r => (float)r.LevelAtGate), 50)} | " +
                                  $"{(cohort == AutopilotCohort.Careful || careful == null || h >= careful.Length ? "-" : Paired(careful[h], b, BothFloors))} |");
                }
            md.AppendLine();
        }

        private static bool BothFloors(DescentSimulation.FoundryResult run) => run.Cleared && run.NextFloor != null && run.NextFloor.Cleared;

        // Floor 3 (Gloam Gardens, docs/48) after each cohort's floor-2 clears, as FloorTwo reads floor 2: who went down and
        // with what health, who cleared it and all three floors, where and by what blow the rest fell, floor 3's Warden (its
        // fight against docs/40 section 9.6's 45-65 s), and the floor-3 clears' length and level at the gate.
        private static void FloorThree(StringBuilder md, IReadOnlyList<(AutopilotCohort Cohort, FoundryBatch.Batch[] Batches)> cohorts,
            FoundryBatch.Batch[] careful)
        {
            md.AppendLine("### Floor 3 (Gloam Gardens) after a floor-2 clear").AppendLine();
            md.AppendLine("Every run that cleared floor 2 answers its checkpoint with Descend and plays floor 3 in the same hands - floor 2's six rooms under the garden's names, climb-scaled at floor 3 " +
                          "(x1.96 health, x1.8032 damage, the Warden's Grove x1.15 more), no modifier, its gate held for the floor's clock to read 210 s, its Warden at its own share (DescentSimulation.FloorThreeGateShare). " +
                          "Went down: floor 2's clears. Health going down: what floor 2 left the hero, as a share of its maximum (P50). Cleared floor 3: of those who went down; all three: of all the seeds. " +
                          "Deaths: floor 3's room (1-3 its halls, 4 the Grafting Forge, 5 Thorn Post, the boss the Warden's Grove) and the blow. Warden: floor 3's Warden's own fight, from walking out of the gate to its fall, " +
                          "over the fights in which it fell (docs/40 section 9.6: P50 45-65 s). Floor 3: active seconds from the checkpoint, its clears. " +
                          "Paired with careful: seeds on which both / only the careful pilot / only this cohort cleared all three floors.").AppendLine();
            md.AppendLine("| Cohort | Hero | Went down | Health going down P50 | Cleared floor 3 (of those) | Cleared all three (of all) | Deaths on floor 3: where, killing blow | Floor 3 Warden fight P50 / P90 | Floor 3 cleared P50 | Level at floor 3's gate P50 | Paired with careful, three floors |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach ((AutopilotCohort cohort, FoundryBatch.Batch[] batches) in cohorts)
                for (int h = 0; h < batches.Length; h++)
                {
                    FoundryBatch.Batch b = batches[h];
                    FoundryBatch.Batch two = FoundryBatch.FloorTwo(b);
                    DescentSimulation.FoundryResult[] down = two.Runs.Where(r => r.NextFloor != null).ToArray();
                    FoundryBatch.Batch three = FoundryBatch.FloorTwo(two);
                    DescentSimulation.FoundryResult[] felled = three.Runs.Where(r => r.WardenSeconds >= 0f).ToArray();
                    DescentSimulation.FoundryResult[] gated = three.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                    md.AppendLine($"| {cohort.Name} | {b.Hero.Name} | {down.Length} | " +
                                  $"{P(down.Select(r => 100f * r.HeroHealth / Math.Max(1f, r.HeroMaximum)), 50)}% | " +
                                  $"{three.ClearedCount} ({Pct(three.ClearedCount, three.Runs.Length)}) | {three.ClearedCount} ({Pct(three.ClearedCount, b.Runs.Length)}) | " +
                                  $"{Deaths(three)} | {P(felled.Select(r => r.WardenSeconds), 50)} / {P(felled.Select(r => r.WardenSeconds), 90)} | " +
                                  $"{P(three.Cleared.Select(r => r.FloorSeconds), 50)} | {P(gated.Select(r => (float)r.LevelAtGate), 50)} | " +
                                  $"{(cohort == AutopilotCohort.Careful || careful == null || h >= careful.Length ? "-" : Paired(careful[h], b, ThreeFloors))} |");
                }
            md.AppendLine();
        }

        private static bool ThreeFloors(DescentSimulation.FoundryResult run) =>
            BothFloors(run) && run.NextFloor.NextFloor != null && run.NextFloor.NextFloor.Cleared;

        // The finale's rows (FoundryBatch.FinaleSection): every cohort's batch for each hero, labelled with the cohort.
        public static List<(string Label, FoundryBatch.Batch Batch)> FinaleRows(
            IReadOnlyList<(AutopilotCohort Cohort, FoundryBatch.Batch[] Batches)> cohorts) =>
            cohorts.SelectMany(c => c.Batches.Select(b => (c.Cohort.Name, b))).ToList();

        // "126: room 3 x72, room 2 x29, boss x20; pit 36, horde Grunt 33", as docs/41's tables read; "-" for none.
        private static string Deaths(FoundryBatch.Batch b)
        {
            DescentSimulation.FoundryResult[] died = b.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            if (died.Length == 0)
                return "-";
            string rooms = string.Join(", ", died.GroupBy(Room).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => $"{g.Key} x{g.Count()}"));
            string blows = string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                .Take(5).Select(g => $"{g.Key} {g.Count()}"));
            return $"{died.Length}: {rooms}; {blows}";
        }

        private static string Room(DescentSimulation.FoundryResult run) =>
            run.DeathRoom < 1 ? "the descent"
            : run.DeathRoom <= run.Kinds.Length && run.Kinds[run.DeathRoom - 1] == RoomKind.Boss ? "boss"
            : "room " + run.DeathRoom.ToString(CultureInfo.InvariantCulture);

        // Seed for seed, the seeds both batches won (by default, cleared the floor), only the careful one, only the other.
        private static string Paired(FoundryBatch.Batch careful, FoundryBatch.Batch other,
            Func<DescentSimulation.FoundryResult, bool> won = null)
        {
            won = won ?? (run => run.Cleared);
            int both = 0, carefulOnly = 0, otherOnly = 0;
            for (int i = 0; i < careful.Runs.Length && i < other.Runs.Length; i++)
            {
                if (careful.Runs[i].Seed != other.Runs[i].Seed)
                    return "-";
                bool a = won(careful.Runs[i]), b = won(other.Runs[i]);
                if (a && b)
                    both++;
                else if (a)
                    carefulOnly++;
                else if (b)
                    otherOnly++;
            }
            return $"{both} / {carefulOnly} / {otherOnly}";
        }

        // The habits a cohort lacks, as words: "kites with room, fills weapon slots".
        private static string Dropped(AutopilotCohort cohort)
        {
            var dropped = new List<string>();
            foreach (AutopilotCohort.Habit habit in Enum.GetValues(typeof(AutopilotCohort.Habit)))
                if (habit != AutopilotCohort.Habit.None && (habit & AutopilotCohort.Habit.All) == habit &&
                    habit != AutopilotCohort.Habit.All && !cohort.Has(habit))
                    dropped.Add(Words(habit.ToString()));
            return dropped.Count == 0 ? "-" : string.Join(", ", dropped);
        }

        private static string Words(string name)
        {
            var words = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                    words.Append(' ');
                words.Append(char.ToLowerInvariant(name[i]));
            }
            return words.ToString();
        }

        private static string SeedRange(int[] seeds) =>
            seeds.Length == 0 ? "-" : seeds.Length == seeds[seeds.Length - 1] - seeds[0] + 1
                ? $"{seeds[0]}-{seeds[seeds.Length - 1]}"
                : string.Join(", ", seeds);

        private static string P(IEnumerable<float> values, double p, string format = "0")
        {
            double value = FoundryBatch.Percentile(values, p);
            return double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Mean(IEnumerable<float> values, string format = "0")
        {
            float[] list = values.ToArray();
            return list.Length == 0 ? "-" : list.Average().ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Pct(int part, int whole) =>
            whole == 0 ? "-" : (100.0 * part / whole).ToString("0", CultureInfo.InvariantCulture) + "%";
    }
}
