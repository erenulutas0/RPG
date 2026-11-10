using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Combat;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // The regression harness's batches (docs/41 step 5): a hero's foundry floor over a seed batch, read as the report's
    // tables in Markdown - floor time, uptime, the segments, levels, the Warden, deaths, observed TTK, the
    // time_bucket_summary and offer_health of docs/40 section 17.3 - with docs/40 section 9.6's quality gates as report
    // lines, and each lever of the research's table (v01-research report.md section 3) against the floor it was estimated
    // on, the pre-V0.1 one, and V0.1's own knobs against the shipped floor, on the same seeds. Pure: it only runs
    // DescentSimulation.RunFoundry and counts.
    internal static partial class FoundryBatch
    {
        internal sealed class Batch
        {
            public DescentSimulation.FoundryHero Hero;
            public DescentSimulation.FoundryLevers Levers;
            public DescentSimulation.FoundryResult[] Runs;
            public double WallSeconds;

            public IEnumerable<DescentSimulation.FoundryResult> Cleared => Runs.Where(run => run.Cleared);
            public int ClearedCount => Runs.Count(run => run.Cleared);
        }

        // One lever of the research's table: what it sets, and the research's own estimate of what it does to floor 1
        // (NotMeasured where nothing measured it).
        internal sealed class Lever
        {
            public string Name;
            public string Estimate;
            public DescentSimulation.FoundryLevers Levers;
            // What the lever is read against: null for the shipped floor, else the levers' name of another row of its list
            // (PreV01Name, the floor the research's estimates were made on).
            public string Basis;
        }

        public const string NotMeasured = "not measured";

        public static int[] Seeds(int first, int count)
        {
            var seeds = new int[count];
            for (int i = 0; i < count; i++)
                seeds[i] = first + i;
            return seeds;
        }

        // A floor whose walks added up past this was the autopilot's drop-detour loop: the horde kept dying within reach of
        // the hero, every fall dropped something closer than the next hall, the autopilot walked to each in turn and the
        // hall waited - the Editor's Ilya seed 22 spent 195 s so in its second room, and of seeds 1-200 the Vanguard's 53
        // and Ilya's 81 walked this long on the shipped floor, 153 and 191 with the horde filling at 3 a second. The detour
        // is bounded now (DropDetourRule.Walk) and no run of those batches walks this long; the reports still count such
        // runs, as a guard, and read the levers with and without them.
        public const float LongWalkSeconds = 150f;

        public static bool WalkedLong(DescentSimulation.FoundryResult run) => run.WalkTotal > LongWalkSeconds;

        // The Editor measurement's seeds (HordePacingMeasurement.DefaultSeeds).
        public static readonly int[] EditorSeeds = { 11, 22, 33, 44, 55, 66 };

        public static DescentSimulation.FoundryLevers Shipped() => new DescentSimulation.FoundryLevers();

        public const string PreV01Name = "pre-V0.1";

        // The floor before V0.1 (2026-11-02): no hall surges, no second elite, the gate open as its room clears - the
        // floor the research's table (v01-research report.md section 3) estimated its levers on, from its 170 s baseline -
        // with the shipped Warden (WardenLevers' first row reads the Warden before 2026-11-03).
        public static DescentSimulation.FoundryLevers PreV01()
        {
            DescentSimulation.FoundryLevers levers = Shipped().Copy(PreV01Name);
            levers.HallSurges = 0;
            levers.ExtraElites = 0;
            levers.GateNotBeforeSeconds = 0f;
            return levers;
        }

        // The research's ranked levers that the floor's knobs reach, at the values its table names, each set on the
        // pre-V0.1 floor its estimate was made on and read against it (the first row, itself read against the shipped
        // floor); then V0.1's three knobs, each taken away from the shipped floor. Ambushes, a timed boss phase and a longer
        // route are not knobs of this harness: each is new content or new art rather than a value.
        public static Lever[] ResearchLevers()
        {
            Lever Research(string name, string estimate, Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = PreV01().Copy(name);
                set(levers);
                return new Lever { Name = name, Estimate = estimate, Levers = levers, Basis = PreV01Name };
            }

            Lever TakenAway(string name, Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = Shipped().Copy(name);
                set(levers);
                return new Lever { Name = name, Estimate = NotMeasured, Levers = levers };
            }

            return new[]
            {
                new Lever { Name = "The pre-V0.1 floor (no surges, no second elite, no gate)", Estimate = NotMeasured, Levers = PreV01() },
                Research("Gate not before 4:00", "+119..+124", l => l.GateNotBeforeSeconds = 240f),
                Research("Halls held 45 s", "+99..+111", l => l.HallHoldSeconds = 45f),
                Research("+2 halls", "+34..+54", l => l.ExtraHalls = 2),
                // Half again the shipped Warden's health; the research estimated it on the Warden before 2026-11-03.
                Research("Warden health x1.5", "+17..+24 (Warden before 2026-11-03)",
                    l => l.WardenHealthShare = WardenRule.GateHealthShare * 1.5f),
                Research("Hall surges 0 -> 2", "+12..+26", l => l.HallSurges = 2),
                Research("Second elite", "+12..+26", l => l.ExtraElites = 1),
                Research("Node waves x1.5 health", "+5..+15", l => l.NodeHealthScale = 1.5f),
                Research("ExperienceShare 0.2 -> 0.1", "+4..+10", l => l.HordeExperienceShare = 0.1f),
                Research("Horde health share 0.3 -> 0.6", "0..+3", l => l.HordeHealthShare = 0.6f),
                Research("Horde targets x1.5 (6/15/24/9)", "0..+2", l => l.HordeTargets = new[] { 6, 15, 24, 9 }),
                Research("Horde targets x2 (8/20/32/12)", "0..+2", l => l.HordeTargets = new[] { 8, 20, 32, 12 }),
                Research("Horde fill rate 2 -> 3 /s", "-6..0", l => l.HordeFillRate = 3f),
                TakenAway("V0.1 without its hall surges (2 -> 0)", l => l.HallSurges = 0),
                TakenAway("V0.1 without its second elite", l => l.ExtraElites = 0),
                TakenAway("V0.1 with the gate open as its room clears (no 3:30)", l => l.GateNotBeforeSeconds = 0f)
            };
        }

        // Combinations of the cheap levers on the shipped floor, toward the band's upper half and the Warden's 45-65 s
        // without the deaths a single big lever brings (FOUNDRY_LEVERS=2 runs these instead of the research's list). The
        // combination V0.1 took - surges 2, a second elite, the gate at 3:30 - is the shipped floor itself.
        public static Lever[] ComboLevers()
        {
            Lever Make(string name, Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = Shipped().Copy(name);
                set(levers);
                return new Lever { Name = name, Estimate = NotMeasured, Levers = levers };
            }

            return new[]
            {
                Make("V0.1 + 2 halls", l => l.ExtraHalls = 2),
                Make("V0.1 + Warden x1.25", l => l.WardenHealthShare = WardenRule.GateHealthShare * 1.25f),
                Make("V0.1 + halls held 30 s", l => l.HallHoldSeconds = 30f),
                Make("V0.1 + halls held 45 s + XP 0.1", l => { l.HallHoldSeconds = 45f; l.HordeExperienceShare = 0.1f; })
            };
        }

        // docs/41 step 4's candidates for a 45-65 s Warden (FOUNDRY_LEVERS=3), as the 2026-11-03 measurement ran them
        // (docs/41, status): the floor before it, then health alone, health that grows with the floor's clock, a blow
        // landing in full only in a window after the Warden's own, and the Warden holding its weapon through the windup
        // and a recovery after the blow - the shipped floor holds it WardenRule.RecoverySeconds at GateHealthShare.
        public static Lever[] WardenLevers()
        {
            Lever Make(string name, Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = Shipped().Copy(name);
                set(levers);
                return new Lever { Name = name, Estimate = "warden", Levers = levers };
            }

            // The Warden as it was before 2026-11-03, its weapon never held, at a health share.
            Action<DescentSimulation.FoundryLevers> Unheld(float share) => l =>
            {
                l.WardenHealthShare = share;
                l.WardenRecoverySeconds = -1f;
            };
            Action<DescentSimulation.FoundryLevers> Held(float recovery, float share) => l =>
            {
                l.WardenHealthShare = share;
                l.WardenRecoverySeconds = recovery;
            };

            return new[]
            {
                Make("Before 2026-11-03: share 5, never held", Unheld(5f)),
                Make("A: share 6", Unheld(6f)),
                Make("A: share 7", Unheld(7f)),
                Make("A: share 8", Unheld(8f)),
                Make("A: share 9", Unheld(9f)),
                Make("A: share 10", Unheld(10f)),
                Make("B: share 7 at 3:30, growing with the clock", l => { Unheld(7f)(l); l.WardenClockFrom = 210f; }),
                Make("B: share 8 at 3:30, growing with the clock", l => { Unheld(8f)(l); l.WardenClockFrom = 210f; }),
                Make("C: share 5, blows at 0.5 outside 2 s after its own", l => { Unheld(5f)(l); l.WardenGuardShare = .5f; l.WardenGuardOpenSeconds = 2f; }),
                Make("C: share 5, blows at 0.6 outside 2 s after its own", l => { Unheld(5f)(l); l.WardenGuardShare = .6f; l.WardenGuardOpenSeconds = 2f; }),
                Make("D: share 5, held through the windup", Held(0f, 5f)),
                Make("D: share 5, held through the windup and 1.5 s after", Held(1.5f, 5f)),
                Make("W: share 8.5, held through the windup", Held(0f, 8.5f)),
                Make("W: share 8.5, held + 1 s", Held(1f, 8.5f)),
                Make("W: share 8.5, held + 2 s", Held(2f, 8.5f)),
                Make("W: share 8, held + 1.5 s", Held(1.5f, 8f)),
                Make("W: share 9, held + 1.5 s", Held(1.5f, 9f)),
                Make("W: share 9, held + 2 s", Held(2f, 9f))
            };
        }

        public static Batch Run(DescentSimulation.FoundryHero hero, DescentSimulation.FoundryLevers levers, int[] seeds,
            int parallelism)
        {
            var runs = new DescentSimulation.FoundryResult[seeds.Length];
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallelism) },
                i => runs[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers));
            watch.Stop();
            return new Batch { Hero = hero, Levers = levers, Runs = runs, WallSeconds = watch.Elapsed.TotalSeconds };
        }

        // The Vanguard started with each weapon of the catalogue on the shipped floor, as the Editor's horde-weapon.txt
        // starts it (the research's M1), for the V0.1 gate's three weapons (FoundryGate): the offers may still bring the
        // other two, as in the game. A batch of the Vanguard's own weapon on the same seeds is that weapon's.
        public static Batch[] RunWeapons(Batch vanguard, int[] seeds, int parallelism)
        {
            var batches = new List<Batch>();
            foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
            {
                if (vanguard != null && vanguard.Hero.Weapon.Id == weapon.Id && vanguard.Levers.Name == Shipped().Name &&
                    vanguard.Runs.Select(run => run.Seed).SequenceEqual(seeds))
                {
                    batches.Add(vanguard);
                    continue;
                }
                DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
                hero.Weapon = weapon;
                hero.Name = "Vanguard + " + weapon.DisplayName;
                batches.Add(Run(hero, Shipped().Copy("shipped, starting " + weapon.Id), seeds, parallelism));
            }
            return batches.ToArray();
        }

        // ---------------------------------------------------------------------------------------------------------
        // Statistics
        // ---------------------------------------------------------------------------------------------------------

        // Linear between the two nearest ranks, as numpy's default; NaN for nothing.
        public static double Percentile(IEnumerable<double> values, double p)
        {
            double[] sorted = values.Where(v => !double.IsNaN(v)).OrderBy(v => v).ToArray();
            if (sorted.Length == 0)
                return double.NaN;
            double rank = p / 100.0 * (sorted.Length - 1);
            int low = (int)Math.Floor(rank);
            int high = Math.Min(sorted.Length - 1, low + 1);
            return sorted[low] + (sorted[high] - sorted[low]) * (rank - low);
        }

        public static double Percentile(IEnumerable<float> values, double p) => Percentile(values.Select(v => (double)v), p);

        public static double Median(IEnumerable<float> values) => Percentile(values, 50);

        private static string F(double value, string format = "0") =>
            double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);

        private static string Spread(IEnumerable<float> values, string format = "0")
        {
            float[] list = values.ToArray();
            return list.Length == 0
                ? "-"
                : $"{F(Percentile(list, 10), format)} / {F(Percentile(list, 50), format)} / {F(Percentile(list, 90), format)}";
        }

        private static string Pct(int part, int whole) => whole == 0 ? "-" : (100.0 * part / whole).ToString("0", CultureInfo.InvariantCulture) + "%";

        // ---------------------------------------------------------------------------------------------------------
        // The report
        // ---------------------------------------------------------------------------------------------------------

        public static string Report(IReadOnlyList<Batch> baselines, IReadOnlyList<(Lever Lever, Batch[] Batches)> levers,
            string preface = null)
        {
            var md = new StringBuilder();
            if (!string.IsNullOrEmpty(preface))
                md.AppendLine(preface).AppendLine();
            FloorTable(md, baselines);
            SegmentTable(md, baselines);
            LevelTable(md, baselines);
            WardenTable(md, baselines);
            WardenBudgetTable(md, baselines);
            DeathTable(md, baselines);
            TtkTable(md, baselines);
            BucketTable(md, baselines);
            OfferTable(md, baselines);
            GateLines(md, baselines);
            if (levers != null && levers.Count > 0)
            {
                LeverTable(md, baselines, levers);
                WardenLeverTable(md, baselines, levers);
            }
            return md.ToString();
        }

        // Each lever's Warden against the shipped floor's on the same seeds: the fight, the floor's clears and the deaths at
        // the Warden, the gate's acceptance (docs/41 step 4: a P50 fight of 45-65 s) and the clears the lever costs.
        private static void WardenLeverTable(StringBuilder md, IReadOnlyList<Batch> baselines,
            IReadOnlyList<(Lever Lever, Batch[] Batches)> levers)
        {
            md.AppendLine("### The Warden's fight by lever (same seeds)").AppendLine();
            md.AppendLine("Fight: felled fights, P10 / P50 / P90 seconds from walking out of the gate to falling. Cleared: the floor's clears and the change in points against the floor the lever is read against (Against, as in the levers' table). At the Warden: deaths there of the runs that reached it. Cost: the hero's health lost in the fight, mean over the runs that reached it. " +
                          "Clock at clear: the floor's clock as the Warden fell, P50 / P90 of the clears. Into the overrun: the runs that reached the Warden before the clock's " + HordeClock.TargetSeconds.ToString("0", CultureInfo.InvariantCulture) + " s and were still fighting it at " + HordeClock.TargetSeconds.ToString("0", CultureInfo.InvariantCulture) + " s, then the clears that ended past it.").AppendLine();
            md.AppendLine("| Lever | Against | " + string.Join(" | ", baselines.Select(b => $"{b.Hero.Name}: fight P10 / P50 / P90 / cleared (change) / at the Warden / Warden health P50 / cost / clock at clear P50, P90 / into the overrun: fights, clears")) + " |");
            md.AppendLine("|---|---|" + string.Join("|", baselines.Select(_ => "---")) + "|");

            string Cells(Batch with, Batch baseline)
            {
                DescentSimulation.FoundryResult[] reached = with.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                DescentSimulation.FoundryResult[] felled = reached.Where(r => r.BossSeconds >= 0f).ToArray();
                DescentSimulation.FoundryResult[] cleared = with.Cleared.ToArray();
                int died = reached.Count(r => r.DiedAtWarden);
                double change = 100.0 * (with.ClearedCount - baseline.ClearedCount) / Math.Max(1, with.Runs.Length);
                double cost = reached.Length == 0 ? double.NaN
                    : reached.Average(r => r.TakenFromWarden + r.TakenFromSlams + r.FightTakenFromHorde + r.FightTakenFromPit);
                int crossed = reached.Count(r => r.ClockAtGate < HordeClock.TargetSeconds && r.Clock >= HordeClock.TargetSeconds);
                int overrun = cleared.Count(r => r.Clock >= HordeClock.TargetSeconds);
                return $"{Spread(felled.Select(r => r.BossSeconds))} / {Pct(with.ClearedCount, with.Runs.Length)} ({Signed(change)}) / " +
                       $"{died} of {reached.Length} / {F(Median(reached.Select(r => r.WardenHealth)))} / {F(cost)} / " +
                       $"{F(Median(cleared.Select(r => r.Clock)))}, {F(Percentile(cleared.Select(r => r.Clock), 90))} / " +
                       $"{crossed} ({Pct(crossed, reached.Length)}), {overrun} ({Pct(overrun, cleared.Length)})";
            }

            md.AppendLine("| shipped | - | " + string.Join(" | ", baselines.Select(b => Cells(b, b))) + " |");
            foreach ((Lever lever, Batch[] batches) in levers)
            {
                IReadOnlyList<Batch> against = Against(lever, baselines, levers, out string againstName);
                md.AppendLine($"| {lever.Name} | {againstName} | " + string.Join(" | ", baselines.Select((b, h) => Cells(batches[h], against[h]))) + " |");
            }
            md.AppendLine();
        }

        private static void FloorTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Floor 1 time (active seconds)").AppendLine();
            md.AppendLine("| Hero | Seeds | Cleared | Floor P10 / P50 / P90 (all) | Cleared P10 / P50 / P90 | Uptime P50 | Engaged uptime P50 | Horde standing (mean, P50) | Horde max (P50) | Wall s |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                md.AppendLine($"| {b.Hero.Name} | {b.Runs.Length} | {b.ClearedCount} ({Pct(b.ClearedCount, b.Runs.Length)}) | " +
                              $"{Spread(b.Runs.Select(r => r.FloorSeconds))} | {Spread(b.Cleared.Select(r => r.FloorSeconds))} | " +
                              $"{F(Median(b.Runs.Select(r => r.Uptime)), "0.00")} | {F(Median(b.Runs.Select(r => r.Engaged)), "0.00")} | " +
                              $"{F(Median(b.Runs.Select(r => r.HordeMeanAlive)), "0.0")} | {F(Median(b.Runs.Select(r => (float)r.HordeMaxAlive)))} | " +
                              $"{F(b.WallSeconds, "0.0")} |");
            }
            md.AppendLine();
        }

        private static void SegmentTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Where the time goes (cleared runs; P10 / P50 / P90 seconds)").AppendLine();
            md.AppendLine("Walk: from a hall marked to its district reached (hall_reached). Node fights: the non-boss rooms less their walks and the gate wait (the waves, their 1 s advance delays and the forge's second). Warden: the boss wave from walking out of the gate to its fall, the 2.6 s reveal included. Exit: the Warden's fall to the gate held. Gate wait: the cleared room before the gate holding for the floor's clock; the rooms' own seconds include it in the room before the gate.").AppendLine();
            // The rooms by kind, as the runs played them (a batch on another floor names its own in its row).
            RoomKind[] kinds = batches.SelectMany(b => b.Cleared).Select(r => r.Kinds).FirstOrDefault() ?? new RoomKind[0];
            md.AppendLine("| Hero | Walk | Node fights | Hall waves only | Warden | Exit | Gate wait | Rooms " + RoomNames(kinds) + " (P50) | Walks over " + LongWalkSeconds.ToString("0", CultureInfo.InvariantCulture) + " s (all runs) |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                DescentSimulation.FoundryResult[] cleared = b.Cleared.ToArray();
                int rooms = cleared.Length > 0 ? cleared[0].RoomSeconds.Length : 0;
                var perRoom = new List<string>();
                for (int i = 0; i < rooms; i++)
                    perRoom.Add(F(Median(cleared.Select(r => r.RoomSeconds[i]))));
                string roomCell = string.Join(" / ", perRoom);
                if (cleared.Length > 0 && !cleared[0].Kinds.SequenceEqual(kinds))
                    roomCell = RoomNames(cleared[0].Kinds) + ": " + roomCell;
                md.AppendLine($"| {b.Hero.Name} | {Spread(cleared.Select(r => r.WalkTotal))} | {Spread(cleared.Select(r => r.NodeSeconds))} | " +
                              $"{Spread(cleared.Select(r => r.WaveSeconds.Where(w => w > 0f).Sum()))} | " +
                              $"{Spread(cleared.Select(r => r.BossSeconds))} | {Spread(cleared.Select(r => r.ExitSeconds))} | " +
                              $"{Spread(cleared.Select(r => r.GateWaitSeconds))} | {roomCell} | " +
                              $"{b.Runs.Count(WalkedLong)} ({Pct(b.Runs.Count(WalkedLong), b.Runs.Length)}) |");
            }
            md.AppendLine();
        }

        // "hall / hall / hall / forge / elite / elite / boss".
        private static string RoomNames(IEnumerable<RoomKind> kinds) =>
            string.Join(" / ", kinds.Select(k => k == RoomKind.Combat ? "hall" : k.ToString().ToLowerInvariant()));

        private static void LevelTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Level and build").AppendLine();
            md.AppendLine("| Hero | Level at the gate P10 / P50 / P90 | Level at the end (after the sweep) | Weapons at the gate (P50) | Tomes at the gate (P50) | >= 2 weapons and >= 2 tomes at the gate | Difficulty at the end (P50) | Clock at the gate P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                DescentSimulation.FoundryResult[] gated = b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                int built = gated.Count(r => r.WeaponsAtGate >= 2 && r.TomesAtGate >= 2);
                md.AppendLine($"| {b.Hero.Name} | {Spread(gated.Select(r => (float)r.LevelAtGate))} | {Spread(b.Runs.Select(r => (float)r.Level))} | " +
                              $"{F(Median(gated.Select(r => (float)r.WeaponsAtGate)))} | {F(Median(gated.Select(r => (float)r.TomesAtGate)))} | " +
                              $"{Pct(built, gated.Length)} | {F(Median(b.Runs.Select(r => r.Difficulty)), "0.00")} | {F(Median(gated.Select(r => r.ClockAtGate)))} |");
            }
            md.AppendLine();
        }

        private static void WardenTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### The Warden").AppendLine();
            md.AppendLine("| Hero | Reached | Felled | Fight s P10 / P50 / P90 (felled) | Felled within 5 s | Died at the Warden (health left P50) | Warden health P50 | Slams / hits a fight (mean) | Hero health lost in the fight P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                DescentSimulation.FoundryResult[] reached = b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                DescentSimulation.FoundryResult[] felled = reached.Where(r => r.BossSeconds >= 0f).ToArray();
                DescentSimulation.FoundryResult[] diedThere = reached.Where(r => r.DiedAtWarden).ToArray();
                double slams = reached.Length > 0 ? reached.Average(r => r.Slams) : double.NaN;
                double hits = reached.Length > 0 ? reached.Average(r => r.SlamHits) : double.NaN;
                md.AppendLine($"| {b.Hero.Name} | {reached.Length} | {felled.Length} | {Spread(felled.Select(r => r.BossSeconds))} | " +
                              $"{felled.Count(r => r.BossSeconds <= 5f)} | {diedThere.Length} ({F(Median(diedThere.Select(r => r.BossLeft)), "0.00")}) | " +
                              $"{F(Median(reached.Select(r => r.WardenHealth)))} | {F(slams, "0.0")} / {F(hits, "0.00")} | " +
                              $"{F(Median(reached.Select(r => r.TakenFromWarden + r.TakenFromSlams)))} |");
            }
            md.AppendLine();
        }

        // docs/41 step 4's budget, HP_boss = D_focus * uptime * damageable_seconds, read from the fights the hero won: its
        // own damage on the Warden per second the Warden stood within the reach of its longest weapon, the share of the
        // fight after the reveal the Warden stood there, and those seconds; what each source took off the Warden; and,
        // over every run that reached it, the hero's health as it walked out, what the fight cost and who killed whom.
        private static void WardenBudgetTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### The Warden's health budget (docs/41 step 4)").AppendLine();
            md.AppendLine("Felled fights: D_focus is the hero's own damage on the Warden (weapons, burn, ability) per second the Warden stood within the reach of the hero's longest weapon; uptime is those seconds over the fight after the 2.6 s reveal. " +
                          "Taken off: the Warden's health by source, as a share of its maximum (mean). Health and cost: every run that reached the Warden; cost is the hero's health lost in the fight by what dealt it.").AppendLine();
            md.AppendLine("| Hero | Felled | Level at the gate P50 | D_focus P10 / P50 / P90 | Uptime P10 / P50 / P90 | After the reveal s P10 / P50 / P90 | Taken off: weapons / burn / ability / pit | Hero health at the gate P50 (maximum P50) | Fight cost mean: Warden / slams / horde / pit | Died at the Warden (of reached), killing blow |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                DescentSimulation.FoundryResult[] reached = b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                DescentSimulation.FoundryResult[] felled = reached.Where(r => r.BossSeconds >= 0f).ToArray();
                DescentSimulation.FoundryResult[] diedThere = reached.Where(r => r.DiedAtWarden).ToArray();
                string Share(Func<DescentSimulation.FoundryResult, float> part) =>
                    felled.Length == 0 ? "-" : F(100.0 * felled.Average(r => part(r) / Math.Max(1f, r.WardenHealth))) + "%";
                double Mean(Func<DescentSimulation.FoundryResult, float> value) =>
                    reached.Length == 0 ? double.NaN : reached.Average(value);
                string killers = string.Join(", ", diedThere.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key} x{g.Count()}"));
                md.AppendLine($"| {b.Hero.Name} | {felled.Length} | {F(Median(reached.Select(r => (float)r.LevelAtGate)))} | " +
                              $"{Spread(felled.Where(r => r.WardenInReach > 0f).Select(r => (r.WardenFromWeapons + r.WardenFromBurn + r.WardenFromAbility) / r.WardenInReach))} | " +
                              $"{Spread(felled.Where(r => r.WardenFought > 0f).Select(r => r.WardenInReach / r.WardenFought), "0.00")} | " +
                              $"{Spread(felled.Select(r => r.WardenFought))} | " +
                              $"{Share(r => r.WardenFromWeapons)} / {Share(r => r.WardenFromBurn)} / {Share(r => r.WardenFromAbility)} / {Share(r => r.WardenFromPit)} | " +
                              $"{F(Median(reached.Select(r => r.HeroHealthAtGate)))} ({F(Median(reached.Select(r => r.HeroMaximumAtGate)))}) | " +
                              $"{F(Mean(r => r.TakenFromWarden))} / {F(Mean(r => r.TakenFromSlams))} / {F(Mean(r => r.FightTakenFromHorde))} / {F(Mean(r => r.FightTakenFromPit))} | " +
                              $"{diedThere.Length} ({Pct(diedThere.Length, reached.Length)}){(killers.Length > 0 ? ": " + killers : string.Empty)} |");
            }
            md.AppendLine();
        }

        private static void DeathTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Deaths and where").AppendLine();
            md.AppendLine("| Hero | Died | Where (room: phase, count) | Killing blow (count) | Damage taken P50: wave / horde / Warden / slams / pit |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                DescentSimulation.FoundryResult[] died = b.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
                string where = string.Join(", ", died.GroupBy(r => $"{r.DeathRoom}: {r.DeathPhase}").OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key} x{g.Count()}"));
                string killers = string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key} x{g.Count()}"));
                md.AppendLine($"| {b.Hero.Name} | {died.Length} ({Pct(died.Length, b.Runs.Length)}) | {(where.Length > 0 ? where : "-")} | {(killers.Length > 0 ? killers : "-")} | " +
                              $"{F(Median(b.Runs.Select(r => r.TakenFromWave)))} / {F(Median(b.Runs.Select(r => r.TakenFromHorde)))} / " +
                              $"{F(Median(b.Runs.Select(r => r.TakenFromWarden)))} / {F(Median(b.Runs.Select(r => r.TakenFromSlams)))} / " +
                              $"{F(Median(b.Runs.Select(r => r.TakenFromPit)))} |");
            }
            md.AppendLine();
        }

        private static void TtkTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Observed TTK by enemy kind (seconds, P10 / P50 / P90)").AppendLine();
            md.AppendLine("Lifetime: from walking in to falling. Under fire: from the first damage it took to falling (docs/40 section 8.6's exposure).").AppendLine();
            md.AppendLine("| Hero | Enemy | Of | Killed | Lifetime | Under fire |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                var kills = b.Runs.SelectMany(r => r.Kills).ToArray();
                foreach (var group in kills.GroupBy(k => (k.Kind, k.Horde)).OrderBy(g => g.Key.Horde).ThenByDescending(g => g.Count()))
                {
                    md.AppendLine($"| {b.Hero.Name} | {group.Key.Kind} | {(group.Key.Horde ? "horde" : "node wave")} | {group.Count()} | " +
                                  $"{Spread(group.Select(k => k.Lifetime), "0.0")} | {Spread(group.Select(k => k.UnderFire), "0.0")} |");
                }
            }
            md.AppendLine();
        }

        private static void BucketTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### time_bucket_summary (30 s buckets of active time; runs still on the floor)").AppendLine();
            md.AppendLine("| Hero | t (s) | Runs | Hero DPS P10 / P50 / P90 | Enemies standing (mean) P50 | Kills / s P50 | Level at the bucket's end P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                int buckets = b.Runs.Max(r => r.Buckets);
                for (int i = 0; i < buckets; i++)
                {
                    // Only whole buckets: a run that ended inside one is counted in the buckets before it.
                    DescentSimulation.FoundryResult[] alive =
                        b.Runs.Where(r => r.FloorSeconds >= (i + 1) * DescentSimulation.FoundryBucketSeconds).ToArray();
                    if (alive.Length < Math.Max(3, b.Runs.Length / 20))
                        break;
                    float span = DescentSimulation.FoundryBucketSeconds;
                    md.AppendLine($"| {b.Hero.Name} | {i * span:0}-{(i + 1) * span:0} | {alive.Length} | " +
                                  $"{Spread(alive.Select(r => r.BucketDamage[i] / span))} | {F(Median(alive.Select(r => r.BucketAlive[i] / span)), "0.0")} | " +
                                  $"{F(Median(alive.Select(r => r.BucketKills[i] / span)), "0.00")} | {F(Median(alive.Select(r => (float)r.BucketLevel[i])))} |");
                }
            }
            md.AppendLine();
        }

        private static void OfferTable(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### offer_health").AppendLine();
            md.AppendLine("Cards offered and taken by kind of pick, over every offer of every run (the autopilot's CardScore takes them).").AppendLine();
            md.AppendLine("| Hero | Offers a run (P50) | Offered: new weapon / weapon upgrade / new tome / tome upgrade / general | Taken: same order | Evolutions a run (mean) | Runs ending with 3 weapons |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach (Batch b in batches)
            {
                string Shares(Func<DescentSimulation.FoundryResult, int[]> counts)
                {
                    var totals = new long[OfferWeights.CategoryCount];
                    foreach (DescentSimulation.FoundryResult r in b.Runs)
                        for (int c = 0; c < totals.Length; c++)
                            totals[c] += counts(r)[c];
                    long all = totals.Sum();
                    OfferCategory[] order = { OfferCategory.NewWeapon, OfferCategory.WeaponUpgrade, OfferCategory.NewTome, OfferCategory.TomeUpgrade, OfferCategory.General };
                    return string.Join(" / ", order.Select(c => all == 0 ? "-" : (100.0 * totals[(int)c] / all).ToString("0", CultureInfo.InvariantCulture) + "%"));
                }
                int three = b.Runs.Count(r => r.Weapons != null && r.Weapons.Split(',').Length >= 3);
                md.AppendLine($"| {b.Hero.Name} | {F(Median(b.Runs.Select(r => (float)r.Offers)))} | {Shares(r => r.OfferedByCategory)} | {Shares(r => r.PicksByCategory)} | " +
                              $"{F(b.Runs.Average(r => r.Evolutions), "0.00")} | {Pct(three, b.Runs.Length)} |");
            }
            md.AppendLine();
        }

        // docs/40 section 9.6 and docs/41 steps 2 and 4, as report lines. Not tests: the gate is not met yet, and the owner
        // decides when a line becomes a failing test.
        private static void GateLines(StringBuilder md, IReadOnlyList<Batch> batches)
        {
            md.AppendLine("### Quality gates (report lines, not tests)").AppendLine();
            md.AppendLine("| Gate | Target | " + string.Join(" | ", batches.Select(b => b.Hero.Name)) + " |");
            md.AppendLine("|---|---|" + string.Join("|", batches.Select(_ => "---")) + "|");

            void Line(string gate, string target, Func<Batch, (string Value, bool Met)> read)
            {
                md.AppendLine($"| {gate} | {target} | " + string.Join(" | ", batches.Select(b =>
                {
                    (string value, bool met) = read(b);
                    return value + (met ? " (met)" : " (**not met**)");
                })) + " |");
            }

            Line("Floor time P50 (all runs)", "240-360 s", b =>
            {
                double p50 = Median(b.Runs.Select(r => r.FloorSeconds));
                return ($"{F(p50)} s", p50 >= 240 && p50 <= 360);
            });
            Line("Combat uptime P50", ">= 0.75", b =>
            {
                double p50 = Median(b.Runs.Select(r => r.Uptime));
                return (F(p50, "0.00"), p50 >= 0.75);
            });
            Line("Engaged uptime P50 (proposed)", ">= 0.75", b =>
            {
                double p50 = Median(b.Runs.Select(r => r.Engaged));
                return (F(p50, "0.00"), p50 >= 0.75);
            });
            Line("First combat (first strike) P90", "<= 10 s", b =>
            {
                double p90 = Percentile(b.Runs.Select(r => r.FirstStrikeSeconds), 90);
                return ($"{F(p90, "0.0")} s", p90 <= 10);
            });
            Line("Warden fight P50 (felled)", "45-65 s", b =>
            {
                double p50 = Median(b.Runs.Where(r => r.BossSeconds >= 0f).Select(r => r.BossSeconds));
                return ($"{F(p50)} s", p50 >= 45 && p50 <= 65);
            });
            Line("Warden felled within 5 s", "near 0", b =>
            {
                int fast = b.Runs.Count(r => r.BossSeconds >= 0f && r.BossSeconds <= 5f);
                return (fast.ToString(CultureInfo.InvariantCulture), fast == 0);
            });
            Line("Early Grunt under fire P50 (node waves, first 120 s)", "0.8-1.3 s", b =>
            {
                double p50 = Median(b.Runs.SelectMany(r => r.Kills).Where(k => k.Kind == DescentSimulation.Grunt.Name && !k.Horde).Select(k => k.UnderFire));
                return ($"{F(p50, "0.00")} s", p50 >= 0.8 && p50 <= 1.3);
            });
            Line("Runs that clear floor 1", "(reported)", b => (Pct(b.ClearedCount, b.Runs.Length), true));
            md.AppendLine();
        }

        // The floor a lever is read against: the shipped one, or the row of its list that its Basis names.
        private static IReadOnlyList<Batch> Against(Lever lever, IReadOnlyList<Batch> baselines,
            IReadOnlyList<(Lever Lever, Batch[] Batches)> levers, out string name)
        {
            name = "shipped";
            if (lever.Basis == null)
                return baselines;
            foreach ((Lever row, Batch[] rowBatches) in levers)
                if (row.Levers.Name == lever.Basis)
                {
                    name = lever.Basis;
                    return rowBatches;
                }
            name = $"shipped (no {lever.Basis} row)";
            return baselines;
        }

        private static void LeverTable(StringBuilder md, IReadOnlyList<Batch> baselines,
            IReadOnlyList<(Lever Lever, Batch[] Batches)> levers)
        {
            md.AppendLine("### Levers against their floors (same seeds)").AppendLine();
            md.AppendLine("Against: the floor a lever is read against - the shipped floor, or for the research's levers the pre-V0.1 floor (no surges, no second elite, no gate; its own row reads it against the shipped one), the floor the research estimated them on. Delta P50 is the lever's median floor time less that floor's median over all runs (a death shortens a floor). Paired is the median of per-seed differences over the seeds both runs cleared; paired, short walks leaves out the seeds where either run's walks passed " + LongWalkSeconds.ToString("0", CultureInfo.InvariantCulture) + " s, which the autopilot's drop-detour loop did until it was bounded (2026-11-03): now it should leave none out, and any it does are worth a look. Long walks counts the lever's runs that passed it. Research: v01-research report.md section 3, against its 170 s baseline; " + NotMeasured + " where nothing measured it.").AppendLine();
            md.AppendLine("| Lever | Against | Research estimate | " + string.Join(" | ", baselines.Select(b => $"{b.Hero.Name}: P50 (delta) / paired / paired, short walks / cleared / long walks / gate level P50")) + " |");
            md.AppendLine("|---|---|---|" + string.Join("|", baselines.Select(_ => "---")) + "|");
            foreach ((Lever lever, Batch[] batches) in levers)
            {
                IReadOnlyList<Batch> against = Against(lever, baselines, levers, out string againstName);
                var cells = new List<string>();
                for (int h = 0; h < baselines.Count; h++)
                {
                    Batch baseline = against[h];
                    Batch with = batches[h];
                    double p50 = Median(with.Runs.Select(r => r.FloorSeconds));
                    double basis = Median(baseline.Runs.Select(r => r.FloorSeconds));
                    var paired = new List<float>();
                    var shortWalks = new List<float>();
                    for (int i = 0; i < with.Runs.Length && i < baseline.Runs.Length; i++)
                    {
                        if (!with.Runs[i].Cleared || !baseline.Runs[i].Cleared)
                            continue;
                        float delta = with.Runs[i].FloorSeconds - baseline.Runs[i].FloorSeconds;
                        paired.Add(delta);
                        if (!WalkedLong(with.Runs[i]) && !WalkedLong(baseline.Runs[i]))
                            shortWalks.Add(delta);
                    }
                    double gate = Median(with.Runs.Where(r => r.LevelAtGate >= 0).Select(r => (float)r.LevelAtGate));
                    cells.Add($"{F(p50)} ({Signed(p50 - basis)}) / {Signed(Median(paired))} / {Signed(Median(shortWalks))} (n={shortWalks.Count}) / " +
                              $"{Pct(with.ClearedCount, with.Runs.Length)} / {Pct(with.Runs.Count(WalkedLong), with.Runs.Length)} / {F(gate)}");
                }
                md.AppendLine($"| {lever.Name} | {againstName} | {lever.Estimate} | " + string.Join(" | ", cells) + " |");
            }
            md.AppendLine();
        }

        private static string Signed(double value) =>
            double.IsNaN(value) ? "-" : (value >= 0 ? "+" : string.Empty) + value.ToString("0", CultureInfo.InvariantCulture);

        // Every run, one line each, for whoever reads the batch beyond its tables.
        public static string Csv(IEnumerable<Batch> batches)
        {
            var csv = new StringBuilder();
            csv.AppendLine("hero,levers,seed,outcome,floor_s,uptime,engaged,walk_s,node_s,boss_s,exit_s,gate_wait_s,level_gate,level_end,hp,max_hp,wave_kills,horde_kills,horde_mean,horde_max,clock_gate,clock_end,slams,slam_hits,eruptions,pit_hero,warden_hp,boss_left,death_room,death_phase,killer,first_strike_s,weapons,tomes,offers,drops,swept,altars,chests,room_s,walks," +
                           "difficulty_gate,hp_gate,max_hp_gate,warden_weapons,warden_burn,warden_ability,warden_pit,warden_fought_s,warden_in_reach_s,fight_warden,fight_slams,fight_horde,fight_pit");
            foreach (Batch b in batches)
            foreach (DescentSimulation.FoundryResult r in b.Runs)
            {
                string Num(float v, string format = "0.00") => v.ToString(format, CultureInfo.InvariantCulture);
                csv.AppendLine(string.Join(",", r.Hero, Quote(r.Levers), r.Seed, r.Outcome, Num(r.FloorSeconds), Num(r.Uptime, "0.000"),
                    Num(r.Engaged, "0.000"), Num(r.WalkTotal), Num(r.NodeSeconds), Num(r.BossSeconds), Num(r.ExitSeconds),
                    Num(r.GateWaitSeconds), r.LevelAtGate, r.Level, Num(r.HeroHealth, "0.0"), Num(r.HeroMaximum, "0"), r.WaveKills,
                    r.HordeKills, Num(r.HordeMeanAlive), r.HordeMaxAlive, Num(r.ClockAtGate), Num(r.Clock), r.Slams, r.SlamHits,
                    r.Eruptions, r.EruptionsOnHero, Num(r.WardenHealth, "0"), Num(r.BossLeft), r.DeathRoom, Quote(r.DeathPhase),
                    Quote(r.Killer), Num(r.FirstStrikeSeconds), Quote(r.Weapons), Quote(r.Tomes), r.Offers, r.DropsCollected,
                    r.DropsSwept, r.AltarsTaken, r.ChestsOpened,
                    Quote(string.Join(" ", r.RoomSeconds.Select(s => Num(s, "0.0")))),
                    Quote(string.Join(" ", r.WalkSeconds.Select(s => Num(s, "0.0")))),
                    Num(r.DifficultyAtGate), Num(r.HeroHealthAtGate, "0.0"), Num(r.HeroMaximumAtGate, "0"), Num(r.WardenFromWeapons, "0"),
                    Num(r.WardenFromBurn, "0"), Num(r.WardenFromAbility, "0"), Num(r.WardenFromPit, "0"), Num(r.WardenFought),
                    Num(r.WardenInReach), Num(r.TakenFromWarden, "0.0"), Num(r.TakenFromSlams, "0.0"), Num(r.FightTakenFromHorde, "0.0"),
                    Num(r.FightTakenFromPit, "0.0")));
            }
            return csv.ToString();
        }

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "'") + "\"";
    }
}
