using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // docs/41 step 5's acceptance - "the whole V0.1 gate read from one report" - as the first section of the batch report:
    // docs/40 section 17.5's 4-6 min floor, uptime >= 75 %, three weapons with distinct roles, one synergy, a 45-60 s
    // Warden, Extract available and an acceptable frame time on the device, with docs/41 steps 1-4's own acceptance lines
    // beside them, one row a criterion: its target, what the foundry simulation measured over the batch, what the Editor's
    // own measurement measured where a file of it exists, a status and the basis of the status.
    //
    // V0.1 is one character, the Vanguard (docs/27 decision 12), so a row's status reads the Vanguard's batch; Ilya's
    // numbers stand beside it and the basis says where Ilya's, or an Editor file of the simulated floor, read otherwise.
    // Nothing here is estimated: a criterion nothing measures is "not measured" with what would measure it, and a number
    // the plan asks for without a target (docs/41 step 4's "reported, not targeted") is "reported". Pure: the batch report
    // (Tools/CombatChecks/FoundryBatchReport.cs) reads the Editor's files and the floor's asset and hands them in.
    internal static class FoundryGate
    {
        public const string Met = "met";
        public const string NotMet = "not met";
        public const string NotMeasured = "not measured";
        public const string Reported = "reported";
        public static readonly string[] Statuses = { Met, NotMet, NotMeasured, Reported };

        public const string V01HeroId = "hero_vanguard";

        // docs/41 steps 2 and 4, docs/40 sections 9.6 and 17.5.
        public const float FloorLow = 240f;
        public const float FloorHigh = 360f;
        public const float UptimeTarget = .75f;
        public const float FirstCombatSeconds = 10f;
        public const float WardenLow = 45f;
        public const float WardenHigh = 65f;
        public const float WardenFastSeconds = 5f;
        // "Near zero": at most this share of the felled fights.
        public const double WardenFastShare = .01;
        // docs/40 section 9.3: a meaningful synergy in 80 % of runs - by minute 10 there, by the floor's end here.
        public const double SynergyShare = .8;
        // A weapon's lead on another under this share of the other's figure is read as a tie (proposed).
        public const double RoleMargin = .1;

        // The criteria, in the table's order; a row's name is its key.
        public const string FloorTime = "Floor time (the autopilot)";
        public const string GateWait = "Time waiting for the gate";
        public const string OwnersFloor = "Floor time (the owner's play)";
        public const string CombatUptime = "Combat uptime";
        public const string EngagedUptime = "Engaged uptime";
        public const string FirstCombat = "First combat";
        public const string WeaponRoles = "Three weapons with distinct roles";
        public const string Synergy = "One synergy";
        public const string WardenFight = "Warden fight";
        public const string WardenFast = "Warden felled within 5 s";
        public const string WardenCost = "The hero's health at the Warden";
        public const string Clears = "Clears and deaths";
        public const string Overrun = "Clock at clear and the overrun";
        public const string Extract = "Extract available";
        public const string DeviceFrames = "Frame time on the device";

        public static readonly string[] Criteria =
        {
            FloorTime, GateWait, OwnersFloor, CombatUptime, EngagedUptime, FirstCombat, WeaponRoles, Synergy, WardenFight,
            WardenFast, WardenCost, Clears, Overrun, Extract, DeviceFrames
        };

        internal sealed class Row
        {
            public string Criterion;
            public string Target;
            public string Simulation;
            public string Editor;
            public string Status;
            public string Basis;
        }

        // One Editor measurement as the gate reads it: HordePacingMeasurement's file - its name, the hero and the weapon it
        // started with (null for the hero's own), why its floor is not the simulated one (null when it is) - and its seeds.
        internal sealed class EditorFile
        {
            public string Source;
            public string HeroId;
            public string WeaponId;
            public string OtherFloor;
            public List<EditorSeed> Seeds = new List<EditorSeed>();
        }

        internal sealed class EditorSeed
        {
            public int Seed;
            public bool Cleared;
            // floor_s, uptime, engaged, boss_s (the Warden's fight) and clock (the floor's clock at the end).
            public float Floor;
            public float Uptime = float.NaN;
            public float Engaged = float.NaN;
            public float Boss = float.NaN;
            public float Clock = float.NaN;
            // The room the run ended in, from the rooms column; 0 where it names none.
            public int LastRoom;
            // From the seed's own telemetry, where it was kept for this file's run: evolutions taken, and the gate's
            // hold (room_complete's gate_hold_sec, which telemetry before 2026-11-03 never wrote); -1 and NaN where not.
            public int Evolutions = -1;
            public float GateWait = float.NaN;
        }

        // The Editor perf probe's summary (Tests/PlayMode/PerfProbe.cs): its file, its header (when, which Editor, which
        // machine) and each window it measured.
        internal sealed class EditorFrames
        {
            public string Source;
            public string Machine;
            public List<FrameWindow> Windows = new List<FrameWindow>();
        }

        internal sealed class FrameWindow
        {
            public string Name;
            public int Frames;
            public float AliveMax = float.NaN;
            public float P95 = float.NaN;
            public float Max = float.NaN;
            public int Over16 = -1;
            public int Over33 = -1;
        }

        internal sealed class Evidence
        {
            public List<EditorFile> Files = new List<EditorFile>();
            public EditorFrames Frames;
            // Floor 1's asset as read: its name, and the asset of the floor it leads to (null when it leads nowhere).
            // FloorRead is false when nothing read it.
            public bool FloorRead;
            public string FloorAsset;
            public string NextFloor;

            public EditorFile HeroFile(string heroId) => Files.FirstOrDefault(f => f.HeroId == heroId && f.WeaponId == null);

            public EditorFile WeaponFile(string weaponId) => Files.FirstOrDefault(f => f.WeaponId == weaponId);
        }

        // ---------------------------------------------------------------------------------------------------------
        // The rows
        // ---------------------------------------------------------------------------------------------------------

        // heroes: each hero's batch on the shipped floor, the Vanguard's among them; weapons: the Vanguard started with each
        // weapon (FoundryBatch.RunWeapons), empty where they were not run.
        public static List<Row> Rows(IReadOnlyList<FoundryBatch.Batch> heroes, IReadOnlyList<FoundryBatch.Batch> weapons,
            Evidence evidence)
        {
            if (heroes == null || heroes.Count == 0)
                throw new ArgumentException("The gate reads at least one hero's batch.", nameof(heroes));
            evidence = evidence ?? new Evidence();
            weapons = weapons ?? new FoundryBatch.Batch[0];
            FoundryBatch.Batch v01 = heroes.FirstOrDefault(b => b.Hero.Id == V01HeroId) ?? heroes[0];
            var gate = new Reading { Heroes = heroes, V01 = v01, Evidence = evidence };
            string gateClock = Clock(v01.Levers.GateNotBeforeSeconds);
            string overrun = F(HordeClock.TargetSeconds);

            return new List<Row>
            {
                PerHero(gate, FloorTime, $"P50 {F(FloorLow)}-{F(FloorHigh)} s (4-6 min), over every run",
                    "Active seconds from the floor's first frame to the gate held or a death (docs/41 step 2, docs/40 section 9.6): a death ends a floor early, so the P50 of every run falls with the deaths. A clear's floor counts the wait for the gate (the next row).",
                    b => Band(b.Runs.Select(r => r.FloorSeconds), FloorLow, FloorHigh, $"s, clears' P50 {F(Median(b.Cleared.Select(r => r.FloorSeconds)))} s"),
                    e => BandEditor(e.Seeds.Select(s => s.Floor), FloorLow, FloorHigh,
                        $"s ({Many(e.Seeds.Count, "seed")}), clears' {F(Median(e.Seeds.Where(s => s.Cleared).Select(s => s.Floor)))} s"),
                    note: b => ClearsNote(b, Median(b.Runs.Select(r => r.FloorSeconds)), Median(b.Cleared.Select(r => r.FloorSeconds)),
                        p50 => p50 >= FloorLow && p50 <= FloorHigh, "0", " s")),
                PerHero(gate, GateWait, $"none set: the gate opens at the floor's {gateClock} (FloorDefinition's foundry gate time), a part of the 4-6 min",
                    $"The cleared room before the gate holds, the horde still on, until the floor's clock reads {gateClock} (docs/41's 2026-11-02 status); the floor's time counts the wait.",
                    b => (WaitText(b), (bool?)null),
                    e => (EditorWaitText(e), null), Reported),
                Unmeasured(OwnersFloor, $"P50 {F(FloorLow)}-{F(FloorHigh)} s on the owner too (docs/41 step 2)",
                    "This report reads no owner telemetry: floor_complete and run_end active_sec from the owner's runs on a current APK would measure it."),
                PerHero(gate, CombatUptime, $">= {F(UptimeTarget, "0.00")} of active time with an enemy alive (docs/41 step 2), over every run",
                    "HordePacingMeasurement's uptime and the simulation's: with the horde on, some enemy stands nearly always, so it says little about fighting.",
                    b => AtLeast(b.Runs.Select(r => r.Uptime), b.Cleared.Select(r => r.Uptime), UptimeTarget),
                    e => AtLeastEditor(e.Seeds.Select(s => s.Uptime), e.Seeds.Where(s => s.Cleared).Select(s => s.Uptime), UptimeTarget),
                    note: b => ClearsNote(b, Median(b.Runs.Select(r => r.Uptime)), Median(b.Cleared.Select(r => r.Uptime)),
                        p50 => p50 >= UptimeTarget, "0.00")),
                PerHero(gate, EngagedUptime, $">= {F(UptimeTarget, "0.00")} over every run (proposed: a strike of any weapon held landed, or a hit taken, in the last {F(DescentSimulation.EngagedSeconds, "0.0")} s - docs/40 section 8.4's attack and target-finding uptime, section 9.6's attack uptime)",
                    "The lead's stricter reading of docs/41 step 2's uptime, the one its 2026-11-02 status lists as open. Until 2026-11-11 it read an enemy within the first weapon's reach and a unit: that read how long the horde, which falls to a strike or two, stood near the hero rather than whether the hero fought, and it moved with the Sword's reach line (docs/41's status of that day). The Editor's engaged_strike column is the same rule; a pacing file written before it has none. A run that dies early fought most of its short floor, so the clears' P50 stands beside.",
                    b => AtLeast(b.Runs.Select(r => r.Engaged), b.Cleared.Select(r => r.Engaged), UptimeTarget),
                    e => e.Seeds.Any(s => !float.IsNaN(s.Engaged))
                        ? AtLeastEditor(e.Seeds.Select(s => s.Engaged), e.Seeds.Where(s => s.Cleared).Select(s => s.Engaged), UptimeTarget)
                        : ("not in this file (written before 2026-11-11's rule)", (bool?)null),
                    note: b => ClearsNote(b, Median(b.Runs.Select(r => r.Engaged)), Median(b.Cleared.Select(r => r.Engaged)),
                        p50 => p50 >= UptimeTarget, "0.00")),
                PerHero(gate, FirstCombat, $"<= {F(FirstCombatSeconds)} s (docs/41 step 2, docs/40 section 9.6), read as the P90",
                    "The hero's first landed strike, in active seconds.",
                    b =>
                    {
                        double p90 = Percentile(b.Runs.Select(r => r.FirstStrikeSeconds), 90);
                        return ($"P50 {F(Median(b.Runs.Select(r => r.FirstStrikeSeconds)), "0.0")} s, P90 {F(p90, "0.0")} s",
                            double.IsNaN(p90) ? (bool?)null : p90 <= FirstCombatSeconds);
                    },
                    e => ("not in the pacing file", null)),
                Roles(weapons, evidence),
                SynergyRow(gate),
                PerHero(gate, WardenFight, $"P50 {F(WardenLow)}-{F(WardenHigh)} s of the felled fights (docs/41 step 4; docs/40 section 17.5: about 45-60)",
                    "From walking out of the gate to falling, the 2.6 s reveal included.",
                    b => Band(Felled(b).Select(r => r.BossSeconds), WardenLow, WardenHigh, $"s ({Felled(b).Length} felled)"),
                    e => BandEditor(e.Seeds.Where(s => s.Cleared).Select(s => s.Boss), WardenLow, WardenHigh,
                        $"s ({e.Seeds.Count(s => s.Cleared && !float.IsNaN(s.Boss))} felled)")),
                PerHero(gate, WardenFast, $"near 0 (read as at most {F(WardenFastShare * 100)} % of the felled fights)",
                    "docs/41 step 4 and docs/40 section 9.6: a boss dead in 5 s near zero in the normal cohort.",
                    b =>
                    {
                        DescentSimulation.FoundryResult[] felled = Felled(b);
                        int fast = felled.Count(r => r.BossSeconds <= WardenFastSeconds);
                        return ($"{fast} of {felled.Length}", felled.Length == 0 ? (bool?)null : fast <= felled.Length * WardenFastShare);
                    },
                    e =>
                    {
                        EditorSeed[] felled = e.Seeds.Where(s => s.Cleared && !float.IsNaN(s.Boss)).ToArray();
                        int fast = felled.Count(s => s.Boss <= WardenFastSeconds);
                        return ($"{fast} of {felled.Length}", felled.Length == 0 ? (bool?)null : fast <= felled.Length * WardenFastShare);
                    }),
                PerHero(gate, WardenCost, "reported, not targeted (docs/41 step 4; the owner's cost was 6-11 health before V0.1)",
                    "The autopilot's, mean over the runs that reached the Warden; the owner's own cost is not measured: boss_start and boss_end hero_hp in the owner's telemetry on a current APK would measure it.",
                    b => (CostText(b), null),
                    e => ("the pacing file keeps no cost", null), Reported),
                PerHero(gate, Clears, "none set (docs/41's 2026-11-02 status lists the Vanguard's deaths as open against the gate)",
                    "Where: the kind of room the hero died in, the walk to it included, and apart the wait for the gate in the cleared room before it and the exit after the Warden (an Editor file of another floor: its room numbers).",
                    b => (ClearsText(b), null),
                    e => (EditorClearsText(e, e.OtherFloor == null ? v01.Runs.FirstOrDefault()?.Kinds : null), null), Reported),
                PerHero(gate, Overrun, $"none set: the owner's call how many clears may end past the overrun at {overrun} s (docs/41's 2026-11-03 status)",
                    "The floor's clock as the Warden fell (HordeClock); past the overrun the horde hardens.",
                    b => (OverrunText(b.Cleared.Select(r => r.Clock).ToArray()), null),
                    e => (OverrunText(e.Seeds.Where(s => s.Cleared && !float.IsNaN(s.Clock)).Select(s => s.Clock).ToArray()), null),
                    Reported),
                ExtractRow(gate),
                FramesRow(evidence)
            };
        }

        // The batches and files a row reads, and the hero whose batch decides it.
        private sealed class Reading
        {
            public IReadOnlyList<FoundryBatch.Batch> Heroes;
            public FoundryBatch.Batch V01;
            public Evidence Evidence;
        }

        // A row read on every hero's batch and Editor file. Its status is the V0.1 hero's simulation (a fixed one for a
        // reported row); its basis adds where another hero, or the V0.1 hero's Editor file of the simulated floor, reads
        // otherwise, and what note says of the V0.1 hero's batch.
        private static Row PerHero(Reading gate, string criterion, string target, string basis,
            Func<FoundryBatch.Batch, (string Text, bool? Met)> sim, Func<EditorFile, (string Text, bool? Met)> editor,
            string fixedStatus = null, Func<FoundryBatch.Batch, string> note = null)
        {
            var simCells = new List<string>();
            var editorCells = new List<string>();
            var notes = new List<string>();
            string status = null;
            foreach (FoundryBatch.Batch b in gate.Heroes)
            {
                (string text, bool? met) = sim(b);
                simCells.Add($"{b.Hero.Name} {text}");
                string heroStatus = fixedStatus ?? StatusOf(met);
                if (ReferenceEquals(b, gate.V01))
                    status = heroStatus;

                EditorFile file = gate.Evidence.HeroFile(b.Hero.Id);
                if (file == null || file.Seeds.Count == 0)
                    continue;
                (string editorText, bool? editorMet) = editor(file);
                editorCells.Add($"{b.Hero.Name} {editorText}");
                if (fixedStatus == null && ReferenceEquals(b, gate.V01) && file.OtherFloor == null && editorMet.HasValue &&
                    StatusOf(editorMet) != heroStatus)
                    notes.Add($"the Editor's {Many(file.Seeds.Count, "seed")} read {StatusOf(editorMet)}");
            }
            if (fixedStatus == null)
                foreach (FoundryBatch.Batch b in gate.Heroes)
                {
                    if (ReferenceEquals(b, gate.V01))
                        continue;
                    string other = StatusOf(sim(b).Met);
                    if (other != status)
                        notes.Insert(0, $"{b.Hero.Name}: {other}");
                }
            string own = note?.Invoke(gate.V01);
            if (!string.IsNullOrEmpty(own))
                notes.Insert(0, own);
            return new Row
            {
                Criterion = criterion,
                Target = target,
                Simulation = string.Join("; ", simCells),
                Editor = editorCells.Count > 0 ? string.Join("; ", editorCells) : "no Editor file",
                Status = status,
                Basis = notes.Count > 0 ? basis + " " + Sentence(string.Join("; ", notes)) : basis
            };
        }

        private static Row Unmeasured(string criterion, string target, string basis) => new Row
        {
            Criterion = criterion, Target = target, Simulation = "-", Editor = "-", Status = NotMeasured, Basis = basis
        };

        // docs/40 section 17.5's three weapons with distinct roles, read as section 17.3's weapon_effectiveness: each
        // weapon's efficiency on three jobs when the Vanguard starts with it - its own damage on the horde a second of the
        // floor, on the elites (the elite rooms' whole waves) a second of the elite rooms' fights, and on the Warden a
        // second of the fight after the reveal - and a weapon's role is a job on which it leads every other weapon by more
        // than RoleMargin. Three jobs, so that three weapons can each lead one (docs/41, status 2026-11-10 night).
        private static Row Roles(IReadOnlyList<FoundryBatch.Batch> weapons, Evidence evidence)
        {
            const string criterion = WeaponRoles;
            string target = $"Sword, Staff and Daggers each lead every other by more than {F(RoleMargin * 100)} % on a job of its own: the horde, the elites or the Warden (docs/40 section 17.5's roles, read as section 17.3's horde and boss efficiency with the elites beside them; the {F(RoleMargin * 100)} % is proposed)";
            DescentSimulation.HeroWeapon[] catalogue = DescentSimulation.WeaponCatalogue();
            var editorCells = new List<string>();
            foreach (DescentSimulation.HeroWeapon weapon in catalogue)
            {
                EditorFile file = evidence.WeaponFile(weapon.Id);
                if (file != null && file.Seeds.Count > 0)
                    editorCells.Add($"{weapon.DisplayName} start ({file.Source}{(file.OtherFloor != null ? ", another floor" : string.Empty)}): " +
                                    $"{file.Seeds.Count(s => s.Cleared)}/{file.Seeds.Count} cleared, floor P50 {F(Median(file.Seeds.Select(s => s.Floor)))} s, " +
                                    $"Warden P50 {F(Median(file.Seeds.Where(s => s.Cleared).Select(s => s.Boss)))} s");
            }
            string editor = editorCells.Count > 0 ? string.Join("; ", editorCells) : "no Editor file started with a weapon";
            var rows = new List<(string Name, double[] Jobs)>();
            foreach (DescentSimulation.HeroWeapon weapon in catalogue)
            {
                FoundryBatch.Batch b = weapons.FirstOrDefault(w => w.Hero.Weapon.Id == weapon.Id);
                if (b == null)
                    continue;
                (double horde, double elites, double warden) = Efficiency(b);
                rows.Add((weapon.DisplayName, new[] { horde, elites, warden }));
            }
            string simulation = rows.Count > 0
                ? string.Join("; ", rows.Select(r => $"{r.Name} horde {F(r.Jobs[0])}, elites {F(r.Jobs[1])}, Warden {F(r.Jobs[2])} a second"))
                : "-";
            string how = "The Vanguard started with each weapon on the batch's seeds, the others still offered (the weapons table below); each weapon's own damage, what it picked up apart, P50s: " +
                         "horde, its damage on the horde a second of the floor it held it; elites, on the elite rooms' waves (RoomKind.Elite: a Grunt Captain with a Grunt and two Mites, the horde round them apart; the halls hold no elite) a second of those rooms' fights, from the wave's spawn to its last fall; " +
                         $"Warden, on the Warden a second of the fight after the reveal. A weapon's role is a job on which it leads every other weapon by more than {F(RoleMargin * 100)} %, and the three have distinct roles when each has one (at most one weapon leads a job).";
            if (rows.Count < catalogue.Length || rows.Any(r => r.Jobs.Any(double.IsNaN)))
                return new Row
                {
                    Criterion = criterion, Target = target, Simulation = simulation, Editor = editor, Status = NotMeasured,
                    Basis = "The batch did not run every weapon, or a weapon never met the elites or the Warden: FoundryBatch.RunWeapons on seeds that reach them would measure it."
                };

            // Each weapon's role: the job on which it leads every other by more than the margin.
            var roles = new List<string>();
            var without = new List<string>();
            foreach ((string Name, double[] Jobs) a in rows)
            {
                (string Name, double[] Jobs)[] others = rows.Where(b => b.Name != a.Name).ToArray();
                int job = LeadsJob(a.Jobs, others.Select(b => b.Jobs).ToArray());
                if (job >= 0)
                {
                    roles.Add($"the {a.Name} {JobRoles[job]}");
                    continue;
                }
                string[] behind = Enumerable.Range(0, JobNames.Length).Select(j =>
                {
                    (string Name, double[] Jobs) best = others.OrderByDescending(b => b.Jobs[j]).First();
                    return $"{JobNames[j]} {F(a.Jobs[j])} to the {best.Name}'s {F(best.Jobs[j])}";
                }).ToArray();
                without.Add($"the {a.Name} ({string.Join(", ", behind)})");
            }
            string verdict = without.Count == 0
                ? $"Roles: {string.Join(", ", roles)}."
                : $"No job of its own, another weapon ahead or within {F(RoleMargin * 100)} % on each: {string.Join("; ", without)}" +
                  (roles.Count > 0 ? $". Roles: {string.Join(", ", roles)}." : ".");
            return new Row
            {
                Criterion = criterion, Target = target, Simulation = simulation, Editor = editor,
                Status = without.Count == 0 ? Met : NotMet, Basis = how + " " + verdict
            };
        }

        // The roles row's jobs, in Efficiency's order, and a weapon's role on each.
        public static readonly string[] JobNames = { "horde", "elites", "Warden" };
        private static readonly string[] JobRoles = { "the horde's", "the elites'", "the Warden's" };

        // The job (an index of JobNames) on which jobs lead each of others by more than RoleMargin; -1 for none. At most one
        // weapon leads a job so, so weapons that each have one have distinct roles.
        public static int LeadsJob(double[] jobs, IReadOnlyList<double[]> others)
        {
            for (int j = 0; j < jobs.Length; j++)
                if (!double.IsNaN(jobs[j]) && others.All(o => jobs[j] > o[j] * (1 + RoleMargin)))
                    return j;
            return -1;
        }

        // P50s over the batch of the starting weapon's own damage: on the horde a second it was held; on the elites - the
        // elite rooms' whole waves, FoundryWeaponWork.DealtElite - a second of the elite rooms' fights
        // (FoundryResult.EliteFought), over the runs that met one; and on the Warden a second of the fight after the
        // reveal, over the runs that reached it.
        public static (double Horde, double Elites, double Warden) Efficiency(FoundryBatch.Batch b)
        {
            string id = b.Hero.Weapon.Id;
            var horde = new List<float>();
            var elites = new List<float>();
            var warden = new List<float>();
            foreach (DescentSimulation.FoundryResult r in b.Runs)
            {
                DescentSimulation.FoundryWeaponWork w = r.WeaponWork.FirstOrDefault(x => x.Id == id);
                if (w == null)
                    continue;
                float held = r.FloorSeconds - w.HeldFrom;
                if (held > 0f)
                    horde.Add(w.DealtHorde / held);
                if (r.EliteFought > 0f)
                    elites.Add(w.DealtElite / r.EliteFought);
                if (r.WardenFought > 0f)
                    warden.Add(w.DealtWarden / r.WardenFought);
            }
            return (Median(horde), Median(elites), Median(warden));
        }

        // docs/41 step 3's visible synergy - a tome that changes a weapon's kill pattern, not only its number - is the
        // game's weapon evolution (EvolutionRule: a weapon at level 5 with its tome at level 2), each a behaviour; read as
        // docs/40 section 9.3's share of runs, at the floor's end over the clears.
        private static Row SynergyRow(Reading gate)
        {
            string target = $"a tome's evolution that changes a weapon's kill pattern (docs/41 step 3) in >= {F(SynergyShare * 100)} % of the Vanguard's clears by the floor's end (docs/40 section 9.3's share, read there at minute 10)";
            string built = "Built: " + string.Join(", ", DescentSimulation.WeaponCatalogue().Where(w => w.Evolution != null)
                .Select(w => $"{w.DisplayName} + {TomeName(w.EvolvesWithTome)} -> {w.Evolution.DisplayName} ({Behaviour(w.Evolution.Stat)})")) +
                $", at weapon level {EvolutionRule.WeaponEvolutionLevel} and tome level {EvolutionRule.WeaponEvolutionTomeLevel}; the evolutions table below.";
            return PerHero(gate, Synergy, target, built,
                b =>
                {
                    Evolving e = Evolutions(b);
                    bool? met = e.Clears == 0 ? (bool?)null : e.EvolvedClears >= e.Clears * SynergyShare && e.ProcKills > 0;
                    return ($"{e.EvolvedClears} of {Many(e.Clears, "clear")} ({Pct(e.EvolvedClears, e.Clears, "0.0")}), first at P50 {F(e.FirstP50)} s; " +
                            $"procs land {Pct(e.ProcKills, e.KillsAfter)} of the evolved weapons' kills after", met);
                },
                file =>
                {
                    EditorSeed[] read = file.Seeds.Where(s => s.Evolutions >= 0).ToArray();
                    if (read.Length == 0)
                        return ("no telemetry of this file's runs", null);
                    int clears = read.Count(s => s.Cleared);
                    int evolvedClears = read.Count(s => s.Cleared && s.Evolutions > 0);
                    return ($"{read.Count(s => s.Evolutions > 0)} of {Many(read.Length, "seed")} evolved ({evolvedClears} of {Many(clears, "clear")})",
                        clears == 0 ? (bool?)null : evolvedClears >= clears * SynergyShare);
                });
        }

        private static Row ExtractRow(Reading gate)
        {
            Evidence evidence = gate.Evidence;
            string status = !evidence.FloorRead ? NotMeasured : evidence.NextFloor != null ? Met : NotMet;
            string basis = !evidence.FloorRead
                ? "Floor 1's asset was not read: FloorFlowTests (Unity PlayMode) holds floor 1's checkpoint and Extract's bank."
                : evidence.NextFloor != null
                    ? $"Built: the gate's hold raises FloorCleared (GateBossFlowTests), and CombatSetup.OnFloorCleared offers Extract or Climb while the floor leads on - {evidence.FloorAsset} leads to {evidence.NextFloor}; FloorFlowTests holds the offer and Extract's bank on the hall arena's floor 1. Offered at the clear only, and neither measurement chooses it."
                    : $"{evidence.FloorAsset} leads nowhere: its clear ends the run as a victory, with no choice to make.";
            return PerHero(gate, Extract, "offered once floor 1 is cleared (docs/40 section 17.5: a selectable Extract)", basis,
                b => ($"cleared, so offered, in {Pct(b.ClearedCount, b.Runs.Length)}", null),
                e => ($"{e.Seeds.Count(s => s.Cleared)} of {e.Seeds.Count} cleared", null), status);
        }

        private static Row FramesRow(Evidence evidence)
        {
            EditorFrames frames = evidence.Frames;
            string editor = frames == null || frames.Windows.Count == 0
                ? "no perf probe file"
                : string.Join("; ", frames.Windows.Select(w =>
                    $"{w.Name}: p95 {F(w.P95, "0.0")} ms, max {F(w.Max, "0.0")} ms, {Count(w.Over16)} of {w.Frames} frames over 16.7 ms and {Count(w.Over33)} over 33.3 ms, up to {F(w.AliveMax)} alive")) +
                  $" ({frames.Source}: {frames.Machine})";
            return new Row
            {
                Criterion = DeviceFrames,
                Target = "the S23: p95 <= 20 ms at the chosen crowd, no frame over 35 ms in a steady fight, garbage per frame flat (docs/41 step 1)",
                Simulation = "-",
                Editor = editor,
                Status = NotMeasured,
                Basis = "The perf probe's times are the Editor's on a desktop CPU, for comparing builds, not the phone (its own caution). RenderStatsProbe (development/render-stats.txt) on the S23 with a current APK at 30-40 enemies alive would measure it; docs/21 has the S23's last reading at 10-40, on 2026-10-26, before V0.1's floor."
            };
        }

        // ---------------------------------------------------------------------------------------------------------
        // What the rows read
        // ---------------------------------------------------------------------------------------------------------

        private static DescentSimulation.FoundryResult[] Felled(FoundryBatch.Batch b) =>
            b.Runs.Where(r => r.LevelAtGate >= 0 && r.BossSeconds >= 0f).ToArray();

        private static DescentSimulation.FoundryResult[] Reached(FoundryBatch.Batch b) =>
            b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();

        private static string WaitText(FoundryBatch.Batch b)
        {
            DescentSimulation.FoundryResult[] cleared = b.Cleared.ToArray();
            if (cleared.Length == 0)
                return "no clear";
            float floor = cleared.Sum(r => r.FloorSeconds);
            float wait = cleared.Sum(r => r.GateWaitSeconds);
            return $"{Spread(cleared.Select(r => r.GateWaitSeconds))} s of the clears ({Pct(wait, floor)} of their floor time), " +
                   $"clears less the wait P50 {F(Median(cleared.Select(r => r.FloorSeconds - r.GateWaitSeconds)))} s";
        }

        private static string EditorWaitText(EditorFile file)
        {
            EditorSeed[] cleared = file.Seeds.Where(s => s.Cleared && !float.IsNaN(s.GateWait)).ToArray();
            if (cleared.Length == 0)
                return file.Seeds.Any(s => s.Cleared) ? "not in this file's telemetry (written before 2026-11-03)" : "no clear";
            return $"P50 {F(Median(cleared.Select(s => s.GateWait)))} s of {Many(cleared.Length, "clear")}";
        }

        private static string CostText(FoundryBatch.Batch b)
        {
            DescentSimulation.FoundryResult[] reached = Reached(b);
            if (reached.Length == 0)
                return "never reached the Warden";
            int died = reached.Count(r => r.DiedAtWarden);
            double Mean(Func<DescentSimulation.FoundryResult, float> part) => reached.Average(part);
            return $"{F(Mean(r => r.TakenFromWarden + r.TakenFromSlams + r.FightTakenFromHorde + r.FightTakenFromPit))} health lost of " +
                   $"{F(Median(reached.Select(r => r.HeroMaximumAtGate)))} (Warden {F(Mean(r => r.TakenFromWarden))}, slams {F(Mean(r => r.TakenFromSlams))}, " +
                   $"horde {F(Mean(r => r.FightTakenFromHorde))}, pit {F(Mean(r => r.FightTakenFromPit))}); died there {died} of {reached.Length}";
        }

        private static string ClearsText(FoundryBatch.Batch b)
        {
            DescentSimulation.FoundryResult[] died = b.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            int timedOut = b.Runs.Count(r => r.Outcome == DescentSimulation.FoundryOutcome.Timeout);
            string where = string.Join(", ", died.GroupBy(DeathPlace).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}"));
            return $"cleared {b.ClearedCount} of {b.Runs.Length} ({Pct(b.ClearedCount, b.Runs.Length)}); died {died.Length}" +
                   (where.Length > 0 ? ": " + where : string.Empty) + (timedOut > 0 ? $"; timed out {timedOut}" : string.Empty);
        }

        internal static string DeathPlace(DescentSimulation.FoundryResult r) =>
            r.DeathPhase == "exit" ? "exit"
            : r.DeathPhase == DescentSimulation.GateWaitPhase ? "the gate's wait"
            : RoomPlace(r.Kinds, r.DeathRoom);

        // A room by its kind, 1-based: "halls", "elites", "Warden".
        private static string RoomPlace(RoomKind[] kinds, int room)
        {
            if (kinds == null || room < 1 || room > kinds.Length)
                return "?";
            switch (kinds[room - 1])
            {
                case RoomKind.Combat:
                    return "halls";
                case RoomKind.Elite:
                    return "elites";
                case RoomKind.Boss:
                    return "Warden";
                default:
                    return kinds[room - 1].ToString().ToLowerInvariant();
            }
        }

        // An Editor file's clears and deaths: the deaths by kind of room where the file is the simulated floor (kinds),
        // else by the rooms' numbers.
        private static string EditorClearsText(EditorFile file, RoomKind[] kinds)
        {
            EditorSeed[] died = file.Seeds.Where(s => !s.Cleared).ToArray();
            string where = kinds != null
                ? string.Join(", ", died.GroupBy(s => RoomPlace(kinds, s.LastRoom)).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}"))
                : "rooms " + string.Join(", ", died.Select(s => s.LastRoom > 0 ? s.LastRoom.ToString(CultureInfo.InvariantCulture) : "?"));
            return $"cleared {file.Seeds.Count(s => s.Cleared)} of {file.Seeds.Count}" + (died.Length > 0 ? $"; died {died.Length}: {where}" : string.Empty);
        }

        private static string OverrunText(float[] clocks)
        {
            if (clocks.Length == 0)
                return "no clear";
            int past = clocks.Count(c => c >= HordeClock.TargetSeconds);
            return $"P50 {F(Median(clocks))} / P90 {F(Percentile(clocks, 90))} s; past {F(HordeClock.TargetSeconds)} s {past} of {Many(clocks.Length, "clear")} ({Pct(past, clocks.Length)})";
        }

        // A batch's evolutions: the clears that took one, when the first was taken, and after each weapon's, how much of
        // its damage and its kills its procs - the pulse, the splinter, the kill burst - dealt.
        private sealed class Evolving
        {
            public int Runs;
            public int Evolved;
            public int Clears;
            public int EvolvedClears;
            public double FirstP50 = double.NaN;
            public int ProcKills;
            public int KillsAfter;
            public float ProcDealt;
            public float DealtAfter;
            public Dictionary<string, int> Which = new Dictionary<string, int>();
        }

        private static Evolving Evolutions(FoundryBatch.Batch b)
        {
            var e = new Evolving { Runs = b.Runs.Length, Clears = b.ClearedCount };
            var first = new List<float>();
            foreach (DescentSimulation.FoundryResult r in b.Runs)
            {
                DescentSimulation.FoundryWeaponWork[] evolved = r.WeaponWork.Where(w => w.EvolvedAt >= 0f).ToArray();
                if (evolved.Length == 0)
                    continue;
                e.Evolved++;
                if (r.Cleared)
                    e.EvolvedClears++;
                first.Add(evolved.Min(w => w.EvolvedAt));
                foreach (DescentSimulation.FoundryWeaponWork w in evolved)
                {
                    e.Which[w.Id] = e.Which.TryGetValue(w.Id, out int n) ? n + 1 : 1;
                    e.ProcDealt += w.DealtByProcs;
                    e.DealtAfter += w.DealtAfterEvolving;
                    foreach (DescentSimulation.FoundryKill k in r.Kills)
                        if (k.By == w.Id && k.At > w.EvolvedAt)
                        {
                            e.KillsAfter++;
                            if (k.Proc)
                                e.ProcKills++;
                        }
                }
            }
            e.FirstP50 = Median(first);
            return e;
        }

        private static string TomeName(string id) =>
            DescentSimulation.Tomes().FirstOrDefault(t => t.Id == id)?.DisplayName ?? id;

        private static string Behaviour(UpgradeStat stat)
        {
            switch (stat)
            {
                case UpgradeStat.PulseDamage:
                    return "a pulse every 4th strike";
                case UpgradeStat.CritSplash:
                    return "crits splinter";
                case UpgradeStat.KillBurst:
                    return "kills burst";
                default:
                    return stat.ToString();
            }
        }

        // ---------------------------------------------------------------------------------------------------------
        // The section
        // ---------------------------------------------------------------------------------------------------------

        public static string Section(IReadOnlyList<FoundryBatch.Batch> heroes, IReadOnlyList<FoundryBatch.Batch> weapons,
            Evidence evidence)
        {
            evidence = evidence ?? new Evidence();
            weapons = weapons ?? new FoundryBatch.Batch[0];
            List<Row> rows = Rows(heroes, weapons, evidence);
            FoundryBatch.Batch v01 = heroes.FirstOrDefault(b => b.Hero.Id == V01HeroId) ?? heroes[0];
            var md = new StringBuilder();
            md.AppendLine("## V0.1 gate (docs/41 step 5, docs/40 section 17.5)").AppendLine();
            md.AppendLine("One row a criterion of docs/41 step 5's gate and of steps 1-4's acceptance lines. Simulation: DescentSimulation.RunFoundry, the development autopilot's policy, " +
                          $"{v01.Runs.Length} seeds a hero, P10 / P50 / P90 where three figures stand. Editor: HordePacingMeasurement's newest file a hero, its own seeds (below). " +
                          $"The status reads V0.1's hero, the {v01.Hero.Name} (docs/27 decision 12), in the simulation; the basis names a hero, or an Editor file of the simulated floor, that reads otherwise. " +
                          "Reported: a number the plan asks for without a target. Nothing here is estimated: what nothing measures is not measured, with what would measure it.").AppendLine();
            md.AppendLine(Summary(rows, v01.Hero.Name)).AppendLine();
            md.AppendLine(EditorLine(heroes, evidence)).AppendLine();
            md.AppendLine("| Criterion | Target | Simulation | Editor | Status | Basis |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach (Row row in rows)
                md.AppendLine($"| {Cell(row.Criterion)} | {Cell(row.Target)} | {Cell(row.Simulation)} | {Cell(row.Editor)} | " +
                              $"{(row.Status == NotMet ? "**" + row.Status + "**" : row.Status)} | {Cell(row.Basis)} |");
            md.AppendLine();
            WeaponTable(md, weapons);
            EvolutionTable(md, heroes.Concat(weapons.Where(w => !heroes.Contains(w))).ToArray());
            return md.ToString();
        }

        private static string Summary(List<Row> rows, string hero)
        {
            string Of(string status)
            {
                string[] named = rows.Where(r => r.Status == status).Select(r => r.Criterion).ToArray();
                return $"{status} {named.Length}" + (named.Length > 0 ? " (" + string.Join(", ", named) + ")" : string.Empty);
            }

            return $"**Where V0.1 stands, the {hero}'s floor:** {Of(Met)}; {Of(NotMet)}; {Of(NotMeasured)}; {Of(Reported)}.";
        }

        private static string EditorLine(IReadOnlyList<FoundryBatch.Batch> heroes, Evidence evidence)
        {
            var parts = new List<string>();
            foreach (FoundryBatch.Batch b in heroes)
            {
                EditorFile file = evidence.HeroFile(b.Hero.Id);
                parts.Add(file == null
                    ? $"{b.Hero.Name}: none"
                    : $"{b.Hero.Name}: {file.Source}, {Many(file.Seeds.Count, "seed")}" +
                      (file.OtherFloor != null ? " - **another floor**: " + file.OtherFloor : " - the simulated floor's levers"));
            }
            return "Editor files: " + string.Join("; ", parts) + ". A file names its floor's levers (surges, extra elites, the gate's time), not the Warden's version or the autopilot's.";
        }

        // docs/40 section 17.3's weapon_effectiveness for the gate's roles row: the Vanguard started with each weapon, and
        // that weapon's own work in those runs.
        private static void WeaponTable(StringBuilder md, IReadOnlyList<FoundryBatch.Batch> weapons)
        {
            if (weapons.Count == 0)
                return;
            md.AppendLine("### Weapons: the Vanguard started with each (weapon_effectiveness, the roles row)").AppendLine();
            md.AppendLine("The starting weapon's own work, the weapons the offers brought later apart. Share: of the weapons' damage. Horde, elites and Warden: its damage on the horde a second it was held, on the elite rooms' waves a second of those rooms' fights (the runs that met one), and on the Warden a second of the fight after the reveal (the runs that reached it), P50 (P10-P90). " +
                          "Hit a strike: the enemies its strikes landed on (target, splash or cleave), its evolution's procs apart. Blows: the kills it landed a run. Under fire: from an enemy's first damage to its fall, of the enemies it finished.").AppendLine();
            md.AppendLine("| Starting weapon | Runs (cleared) | Floor P50 | Warden fight P50 | Weapons at the gate P50 | Share | Its damage: horde / node waves / Warden | Horde a second | Elites a second | Warden a second | Hit a strike | Blows a run P50 (horde) | Under fire P50: node / horde | Evolved |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (FoundryBatch.Batch b in weapons)
            {
                string id = b.Hero.Weapon.Id;
                var work = b.Runs.Select(r => (Run: r, Work: r.WeaponWork.FirstOrDefault(w => w.Id == id))).Where(x => x.Work != null).ToArray();
                float dealt = work.Sum(x => x.Work.Dealt);
                float all = b.Runs.Sum(r => r.DealtByWeapons);
                DescentSimulation.FoundryKill[] blows = b.Runs.SelectMany(r => r.Kills).Where(k => k.By == id).ToArray();
                int strikes = work.Sum(x => x.Work.Strikes);
                int landings = work.Sum(x => x.Work.Landings);
                string Rate(IEnumerable<float> values)
                {
                    float[] list = values.ToArray();
                    return list.Length == 0 ? "-" : $"{F(Percentile(list, 50))} ({F(Percentile(list, 10))}-{F(Percentile(list, 90))})";
                }

                md.AppendLine($"| {b.Hero.Weapon.DisplayName} | {b.Runs.Length} ({Pct(b.ClearedCount, b.Runs.Length)}) | {F(Median(b.Runs.Select(r => r.FloorSeconds)))} s | " +
                              $"{F(Median(Felled(b).Select(r => r.BossSeconds)))} s | {F(Median(Reached(b).Select(r => (float)r.WeaponsAtGate)))} | {Pct(dealt, all)} | " +
                              $"{Pct(work.Sum(x => x.Work.DealtHorde), dealt)} / {Pct(work.Sum(x => x.Work.DealtNode), dealt)} / {Pct(work.Sum(x => x.Work.DealtWarden), dealt)} | " +
                              $"{Rate(work.Where(x => x.Run.FloorSeconds > x.Work.HeldFrom).Select(x => x.Work.DealtHorde / (x.Run.FloorSeconds - x.Work.HeldFrom)))} | " +
                              $"{Rate(work.Where(x => x.Run.EliteFought > 0f).Select(x => x.Work.DealtElite / x.Run.EliteFought))} | " +
                              $"{Rate(work.Where(x => x.Run.WardenFought > 0f).Select(x => x.Work.DealtWarden / x.Run.WardenFought))} | " +
                              $"{(strikes == 0 ? "-" : F((double)landings / strikes, "0.00"))} | " +
                              $"{F(Median(b.Runs.Select(r => (float)r.Kills.Count(k => k.By == id))))} ({Pct(blows.Count(k => k.Horde), blows.Length)}) | " +
                              $"{F(Median(blows.Where(k => !k.Horde).Select(k => k.UnderFire)), "0.0")} / {F(Median(blows.Where(k => k.Horde).Select(k => k.UnderFire)), "0.0")} s | " +
                              $"{Pct(work.Count(x => x.Work.EvolvedAt >= 0f), b.Runs.Length)} |");
            }
            md.AppendLine();
        }

        // The gate's synergy row in full: per batch, the runs that evolved, which weapon, when, and what the evolution's
        // procs dealt after it.
        private static void EvolutionTable(StringBuilder md, IReadOnlyList<FoundryBatch.Batch> batches)
        {
            md.AppendLine("### Evolutions (the synergy row)").AppendLine();
            md.AppendLine("Evolved: runs that took a weapon's evolution before the floor ended, and the clears among them. First: the active second of a run's first. After it: the evolved weapons' damage and kills from their evolution on, and the share of each its procs dealt.").AppendLine();
            md.AppendLine("| Batch | Evolved: runs / clears | Which (runs) | First P50 | After it, procs' share: damage / kills |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (FoundryBatch.Batch b in batches)
            {
                Evolving e = Evolutions(b);
                string which = string.Join(", ", e.Which.OrderByDescending(p => p.Value).Select(p => $"{WeaponName(p.Key)} {p.Value}"));
                md.AppendLine($"| {b.Hero.Name} | {e.Evolved} of {e.Runs} ({Pct(e.Evolved, e.Runs)}) / {e.EvolvedClears} of {e.Clears} ({Pct(e.EvolvedClears, e.Clears, "0.0")}) | " +
                              $"{(which.Length > 0 ? which : "-")} | {F(e.FirstP50)} s | {Pct(e.ProcDealt, e.DealtAfter)} / {Pct(e.ProcKills, e.KillsAfter)} |");
            }
            md.AppendLine();
        }

        private static string WeaponName(string id) =>
            DescentSimulation.WeaponCatalogue().FirstOrDefault(w => w.Id == id)?.DisplayName ?? id;

        // ---------------------------------------------------------------------------------------------------------
        // Numbers
        // ---------------------------------------------------------------------------------------------------------

        private static (string Text, bool? Met) Band(IEnumerable<float> values, float low, float high, string unit)
        {
            float[] list = values.ToArray();
            double p50 = Median(list);
            return ($"{Spread(list)} {unit}", double.IsNaN(p50) ? (bool?)null : p50 >= low && p50 <= high);
        }

        private static (string Text, bool? Met) BandEditor(IEnumerable<float> values, float low, float high, string unit)
        {
            double p50 = Median(values);
            return ($"{F(p50)} {unit}", double.IsNaN(p50) ? (bool?)null : p50 >= low && p50 <= high);
        }

        // Where a batch's clears read otherwise than its every run, which decides the status: deaths shorten floors and
        // fill them with fighting.
        private static string ClearsNote(FoundryBatch.Batch b, double all, double clears, Func<double, bool> meets, string format,
            string unit = "")
        {
            if (double.IsNaN(all) || double.IsNaN(clears) || meets(all) == meets(clears))
                return null;
            return $"the {b.Hero.Name}'s clears read {StatusOf(meets(clears))} (P50 {F(clears, format)}{unit}) where every run reads {StatusOf(meets(all))}";
        }

        // A share over every run (the status) with the clears' P50 beside it.
        private static (string Text, bool? Met) AtLeast(IEnumerable<float> values, IEnumerable<float> clears, float target)
        {
            float[] list = values.ToArray();
            double p50 = Median(list);
            return ($"{Spread(list, "0.00")}, clears' P50 {F(Median(clears), "0.00")}", double.IsNaN(p50) ? (bool?)null : p50 >= target);
        }

        private static (string Text, bool? Met) AtLeastEditor(IEnumerable<float> values, IEnumerable<float> clears, float target)
        {
            double p50 = Median(values);
            return ($"P50 {F(p50, "0.00")}, clears' {F(Median(clears), "0.00")}", double.IsNaN(p50) ? (bool?)null : p50 >= target);
        }

        public static string StatusOf(bool? met) => met == null ? NotMeasured : met.Value ? Met : NotMet;

        // The percentiles skip what was never measured (NaN, and the simulation's -1 for "never").
        private static double Percentile(IEnumerable<float> values, double p) =>
            FoundryBatch.Percentile(values.Where(v => !float.IsNaN(v) && v >= 0f), p);

        private static double Median(IEnumerable<float> values) => Percentile(values, 50);

        private static string Spread(IEnumerable<float> values, string format = "0")
        {
            float[] list = values.Where(v => !float.IsNaN(v) && v >= 0f).ToArray();
            return list.Length == 0
                ? "-"
                : $"{F(Percentile(list, 10), format)} / {F(Percentile(list, 50), format)} / {F(Percentile(list, 90), format)}";
        }

        private static string F(double value, string format = "0") =>
            double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);

        // A share in whole percent; one decimal where it is set against a threshold, so the figure never rounds across it.
        private static string Pct(double part, double whole, string format = "0") =>
            whole <= 0 ? "-" : (100.0 * part / whole).ToString(format, CultureInfo.InvariantCulture) + "%";

        private static string Count(int value) => value < 0 ? "-" : value.ToString(CultureInfo.InvariantCulture);

        private static string Many(int count, string noun) =>
            count.ToString(CultureInfo.InvariantCulture) + " " + noun + (count == 1 ? string.Empty : "s");

        private static string Clock(float seconds)
        {
            int whole = (int)Math.Round(seconds);
            return $"{whole / 60}:{whole % 60:00}";
        }

        private static string Sentence(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1) + (text.EndsWith(".") ? string.Empty : ".");

        private static string Cell(string text) => (text ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
