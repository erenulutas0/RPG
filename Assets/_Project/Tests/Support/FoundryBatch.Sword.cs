using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cryptforge.Core;

namespace Cryptforge.Tests
{
    // The Sword's role on the foundry (the owner's open decision 3; docs/cloud/2026-11-04-sword-role.md): candidate
    // foundry-only Sword patterns as research levers (FoundryLevers' Sword fields, read by the foundry simulation alone),
    // each run for both heroes in every cohort's hands and with the Vanguard started with each weapon, and read as the
    // V0.1 gate's roles row (FoundryGate.Efficiency, RoleMargin) beside the clears, the floor and the Warden. Pure: it only
    // runs DescentSimulation.RunFoundry and counts.
    internal static partial class FoundryBatch
    {
        // FOUNDRY_LEVERS=4 in the batch report, and the rows of the Sword's report. The first row is the shipped Sword.
        public static Lever[] SwordLevers()
        {
            Lever Make(string name, Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = Shipped().Copy(name);
                set(levers);
                return new Lever { Name = name, Estimate = "sword", Levers = levers };
            }

            return new[]
            {
                Make("Shipped Sword", l => { }),
                // Reach and arc.
                Make("Reach 2.1 -> 2.5", l => l.SwordReach = 2.5f),
                Make("Reach 2.1 -> 3.0", l => l.SwordReach = 3f),
                Make("Sweep 2.5 round the hero, 60%", l => { l.SwordReach = 2.5f; l.SwordSweepRadius = 2.5f; }),
                Make("Sweep 2.5 round the hero, 40%", l => { l.SwordReach = 2.5f; l.SwordSweepRadius = 2.5f; l.SwordSweepShare = .4f; }),
                Make("Arc 180 deg, 2.5, 60%", l => { l.SwordReach = 2.5f; l.SwordSweepRadius = 2.5f; l.SwordSweepArcDegrees = 180f; }),
                Make("Arc 120 deg, 2.5, 60%", l => { l.SwordReach = 2.5f; l.SwordSweepRadius = 2.5f; l.SwordSweepArcDegrees = 120f; }),
                Make("Arc 120 deg, 2.1, 60%", l => { l.SwordSweepRadius = 2.1f; l.SwordSweepArcDegrees = 120f; }),
                // At evolution.
                Make("Evolved: sweep 2.5, 60%", l => { l.SwordSweepRadius = 2.5f; l.SwordSweepAtEvolution = true; }),
                Make("Evolved: pulse x1.5", l => l.SwordEvolutionPulseScale = 1.5f),
                Make("Evolved: pulse x2", l => l.SwordEvolutionPulseScale = 2f),
                // On kill, knockback, rate.
                Make("Cleave on kill 40%", l => l.SwordKillCleaveShare = .4f),
                Make("Cleave on kill 60%", l => l.SwordKillCleaveShare = .6f),
                Make("Knockback 0.5 unit", l => l.SwordKnockbackUnits = .5f),
                Make("Knockback 1 unit", l => l.SwordKnockbackUnits = 1f),
                Make("Interval x0.8 (0.64 s)", l => l.SwordIntervalScale = .8f),
                Make("Interval x0.67 (0.53 s)", l => l.SwordIntervalScale = 2f / 3f),
                // Against the elites and the Warden, against the horde.
                Make("Warden x1.3", l => l.SwordWardenDamage = 1.3f / Cryptforge.Combat.WardenRule.SwordShare),
                Make("Warden x1.5", l => l.SwordWardenDamage = 1.5f / Cryptforge.Combat.WardenRule.SwordShare),
                Make("Elites and Warden x1.4", l => { l.SwordNodeDamage = 1.4f; l.SwordWardenDamage = 1.4f / Cryptforge.Combat.WardenRule.SwordShare; }),
                Make("Horde x0.8, Warden x1.5", l => { l.SwordHordeDamage = .8f; l.SwordWardenDamage = 1.5f / Cryptforge.Combat.WardenRule.SwordShare; }),
                // Small combinations.
                Make("Reach 2.5 + Warden x1.2", l => { l.SwordReach = 2.5f; l.SwordWardenDamage = 1.2f / Cryptforge.Combat.WardenRule.SwordShare; }),
                Make("Knockback 0.5 + Warden x1.3", l => { l.SwordKnockbackUnits = .5f; l.SwordWardenDamage = 1.3f / Cryptforge.Combat.WardenRule.SwordShare; }),
                Make("Evolved sweep 2.5 + Warden x1.3", l => { l.SwordSweepRadius = 2.5f; l.SwordSweepAtEvolution = true; l.SwordWardenDamage = 1.3f / Cryptforge.Combat.WardenRule.SwordShare; }),
                // What the Daggers would need (their values, so not candidates for the Sword's question).
                Make("Daggers: reach 1.8 -> 2.2", l => l.DaggersReach = 2.2f),
                Make("Daggers: reach 1.8 -> 2.5", l => l.DaggersReach = 2.5f),
                Make("Warden x1.4", l => l.SwordWardenDamage = 1.4f / Cryptforge.Combat.WardenRule.SwordShare),
                Make("Warden x1.5 + Daggers reach 2.5", l => { l.SwordWardenDamage = 1.5f / Cryptforge.Combat.WardenRule.SwordShare; l.DaggersReach = 2.5f; }),
                Make("Warden x1.5 + Daggers reach 2.5, Warden x2", l => { l.SwordWardenDamage = 1.5f / Cryptforge.Combat.WardenRule.SwordShare; l.DaggersReach = 2.5f; l.DaggersWardenDamage = 2f; })
            };
        }

        // One Sword lever's batches: each cohort for each hero (the careful Vanguard is the Vanguard started with the Sword),
        // and the Vanguard started with the Staff and with the Daggers in the careful hands, the Sword's card as the lever
        // shapes it among their offers.
        internal sealed class SwordRun
        {
            public Lever Lever;
            public List<(AutopilotCohort Cohort, Batch[] Batches)> Cohorts = new List<(AutopilotCohort, Batch[])>();
            public Batch[] Weapons;
        }

        public static SwordRun RunSword(Lever lever, int[] seeds, int parallelism)
        {
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };
            var run = new SwordRun { Lever = lever };
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
            {
                DescentSimulation.FoundryLevers levers = lever.Levers.Copy(lever.Levers.Name + ", cohort " + cohort.Name);
                levers.Cohort = cohort;
                run.Cohorts.Add((cohort, heroes.Select(h => Run(h, levers, seeds, parallelism)).ToArray()));
            }
            var weapons = new List<Batch>();
            foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
            {
                if (weapon.Id == DescentSimulation.SwordId)
                {
                    weapons.Add(run.Cohorts[0].Batches[0]);
                    continue;
                }
                DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
                hero.Weapon = weapon;
                hero.Name = "Vanguard + " + weapon.DisplayName;
                weapons.Add(Run(hero, lever.Levers.Copy(lever.Levers.Name + ", starting " + weapon.Id), seeds, parallelism));
            }
            run.Weapons = weapons.ToArray();
            return run;
        }

        // A starting weapon's three jobs as the Sword's research read them, P50s over the batch: its own damage on the horde
        // a second it was held and on the Warden a second of the fight (FoundryGate.Efficiency's), and on the node waves -
        // the halls' and the elite rooms' - a second of the node fights (this report's third job; the gate's third, since
        // 2026-11-10 night, is the elite rooms' Captains).
        public static (double Horde, double Warden, double Node) Jobs(Batch b)
        {
            (double Horde, double Elites, double Warden) gate = FoundryGate.Efficiency(b);
            double horde = gate.Horde, warden = gate.Warden;
            string id = b.Hero.Weapon.Id;
            var node = new List<float>();
            foreach (DescentSimulation.FoundryResult r in b.Runs)
            {
                DescentSimulation.FoundryWeaponWork w = r.WeaponWork.FirstOrDefault(x => x.Id == id);
                if (w != null && r.NodeSeconds > 0f)
                    node.Add(w.DealtNode / r.NodeSeconds);
            }
            return (horde, warden, Median(node));
        }

        // The gate's reading of 2026-11-04, which this report keeps: a weapon has a role when against every other it is ahead
        // by more than the margin on the horde or on the Warden - not necessarily the same job against each. Returns the
        // weapons without. The gate reads three jobs and Leaders' strict reading since 2026-11-10 night (FoundryGate.Roles).
        public static string[] WithoutRole(IReadOnlyList<(string Name, double Horde, double Warden)> rows)
        {
            double m = 1 + FoundryGate.RoleMargin;
            return rows.Where(a => rows.Any(b => b.Name != a.Name && !(a.Horde > b.Horde * m || a.Warden > b.Warden * m)))
                .Select(a => a.Name).ToArray();
        }

        // The stricter reading of the gate's words: the weapons that lead every other by more than the margin on one job,
        // the same job against all (at most one a job, so at most two of three weapons on two jobs).
        public static string[] Leaders(IReadOnlyList<(string Name, double Horde, double Warden)> rows)
        {
            double m = 1 + FoundryGate.RoleMargin;
            return rows.Where(a => rows.All(b => b.Name == a.Name || a.Horde > b.Horde * m) ||
                                   rows.All(b => b.Name == a.Name || a.Warden > b.Warden * m))
                .Select(a => a.Name).ToArray();
        }

        public static string SwordHeader() =>
            "| Lever | Clears V (careful / first-card / standing / clumsy / auto-farmer) | Clears I (same) | Roles (gate) | Leaders (strict) | " +
            "Horde dps Sword / Staff / Daggers | Warden dps Sword / Staff / Daggers | Node dps Sword / Staff / Daggers | " +
            "Floor P50 V / I | Warden P50 V / I | Health lost at the Warden V / I | Sword share of the Vanguard's weapon damage |" +
            Environment.NewLine + "|---|---|---|---|---|---|---|---|---|---|---|---|";

        public static string SwordRow(SwordRun run)
        {
            string Clears(int hero) => string.Join(" / ", new[]
            {
                AutopilotCohort.Careful, AutopilotCohort.FirstCard, AutopilotCohort.Standing, AutopilotCohort.Clumsy, AutopilotCohort.AutoFarmer
            }.Select(c => run.Cohorts.First(x => x.Cohort == c).Batches[hero].ClearedCount.ToString(CultureInfo.InvariantCulture)));
            var jobs = run.Weapons.Select(b => (Name: b.Hero.Weapon.DisplayName, Jobs: Jobs(b))).ToArray();
            var rows = jobs.Select(j => (j.Name, j.Jobs.Horde, j.Jobs.Warden)).ToArray();
            string[] without = WithoutRole(rows);
            string[] leaders = Leaders(rows);
            string roles = without.Length == 0 ? "met" : "not met: " + string.Join(", ", without) + " without";
            string Three(Func<(double Horde, double Warden, double Node), double> pick) =>
                string.Join(" / ", jobs.Select(j => N(pick(j.Jobs))));
            Batch[] careful = run.Cohorts[0].Batches;
            string Both(Func<Batch, string> f) => f(careful[0]) + " / " + f(careful[1]);
            string FloorP50(Batch b) => N(Median(b.Runs.Select(r => r.FloorSeconds)));
            string WardenP50(Batch b) => N(Median(b.Runs.Where(r => r.LevelAtGate >= 0 && r.BossSeconds >= 0f).Select(r => r.BossSeconds)));
            string Lost(Batch b)
            {
                DescentSimulation.FoundryResult[] reached = b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
                return reached.Length == 0 ? "-"
                    : N(reached.Average(r => r.TakenFromWarden + r.TakenFromSlams + r.FightTakenFromHorde + r.FightTakenFromPit));
            }
            Batch vanguard = careful[0];
            float sword = vanguard.Runs.Sum(r => r.WeaponWork.Where(w => w.Id == DescentSimulation.SwordId).Sum(w => w.Dealt));
            float all = vanguard.Runs.Sum(r => r.DealtByWeapons);
            return $"| {run.Lever.Name} | {Clears(0)} | {Clears(1)} | {roles} | {(leaders.Length == 0 ? "none" : string.Join(", ", leaders))} | " +
                   $"{Three(j => j.Horde)} | {Three(j => j.Warden)} | {Three(j => j.Node)} | {Both(FloorP50)} | {Both(WardenP50)} | {Both(Lost)} | " +
                   $"{(all > 0f ? N(100.0 * sword / all) + "%" : "-")} |";
        }

        // One line a lever for a sweep script: name, then the numbers the row shows, comma separated.
        public static string SwordCsvHeader() =>
            "lever,cohort,hero,cleared,runs,floor_p50,warden_p50,lost_at_warden,start_weapon_horde,start_weapon_warden,start_weapon_node";

        public static IEnumerable<string> SwordCsv(SwordRun run)
        {
            foreach ((AutopilotCohort cohort, Batch[] batches) in run.Cohorts)
                foreach (Batch b in batches)
                    yield return CsvLine(run.Lever.Name, cohort.Name, b);
            foreach (Batch b in run.Weapons.Where(w => w.Hero.Weapon.Id != DescentSimulation.SwordId))
                yield return CsvLine(run.Lever.Name, "careful", b);
        }

        private static string CsvLine(string lever, string cohort, Batch b)
        {
            (double horde, double warden, double node) = Jobs(b);
            DescentSimulation.FoundryResult[] reached = b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
            double lost = reached.Length == 0 ? double.NaN
                : reached.Average(r => r.TakenFromWarden + r.TakenFromSlams + r.FightTakenFromHorde + r.FightTakenFromPit);
            return string.Join(",", "\"" + lever + "\"", cohort, "\"" + b.Hero.Name + "\"", b.ClearedCount, b.Runs.Length,
                N(Median(b.Runs.Select(r => r.FloorSeconds))),
                N(Median(b.Runs.Where(r => r.LevelAtGate >= 0 && r.BossSeconds >= 0f).Select(r => r.BossSeconds))),
                N(lost), N(horde, "0.0"), N(warden, "0.0"), N(node, "0.0"));
        }

        private static string N(double value, string format = "0") =>
            double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);
    }
}
