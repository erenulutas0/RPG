using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cryptforge.Core;

namespace Cryptforge.Tests
{
    // The Daggers' role (docs/41, status 2026-11-10 night): with the Staff the horde's weapon and the Sword the Warden's, the
    // V0.1 gate's roles row needs a third job for the third weapon - the elites. Candidate Daggers patterns as research
    // levers (FoundryLevers' Daggers fields, read by the foundry simulation alone), each run with the Vanguard started with
    // each weapon and for both heroes in every cohort's hands, down the floors asked for, frame-perfect or with a human's
    // hands (PilotRealism), read as three jobs - the horde, the elites, the Warden - beside the clears and the Warden's
    // fight. Two readings of the elites: the gate's, the elite rooms' Grunt Captains (FoundryGate.Efficiency,
    // FoundryWeaponWork.DealtEliteLeader), and their whole packs (an elite room's node wave, DealtElite), each a second of
    // FoundryResult.EliteFought. The levers are measured on the shipped floor, the Daggers' EliteRule share among it: a
    // Captains row names the share it plays in all, a pack row its share on the pack on top. Pure: it only runs
    // DescentSimulation.RunFoundry and counts.
    internal static partial class FoundryBatch
    {
        // The rows of the Daggers' report (FoundryBatchReport.TheDaggersRoleOverASeedBatch, DAGGERS_LEVERS by index). The
        // first row is the shipped floor; every other sets a Daggers lever on it. docs/41's status 2026-11-10 night read
        // them before EliteRule shipped (be1f9ceb on 4791f86f), where the shipped Daggers dealt the Captains their own.
        public static Lever[] DaggersLevers()
        {
            Lever Make(string name, Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = Shipped().Copy(name);
                set(levers);
                return new Lever { Name = name, Estimate = "daggers", Levers = levers };
            }

            // The Daggers dealing the Captains alone share.
            Action<DescentSimulation.FoundryLevers> Captains(float share) => l =>
            {
                l.DaggersEliteDamage = share;
                l.DaggersEliteLeaderOnly = true;
            };

            return new[]
            {
                Make("Shipped", l => { }),
                // A share on the elites, as the Sword's on the Warden.
                Make("Daggers on the elites x3", l => l.DaggersEliteDamage = 3f),
                Make("Daggers on the elites x4", l => l.DaggersEliteDamage = 4f),
                Make("Daggers on the elites x4.5", l => l.DaggersEliteDamage = 4.5f),
                Make("Daggers on the elites x5", l => l.DaggersEliteDamage = 5f),
                Make("Daggers on the elites x6", l => l.DaggersEliteDamage = 6f),
                // Their crits on the elites only (a third of their strikes crit, for double).
                Make("Daggers' crits on the elites x3", l => l.DaggersEliteCritDamage = 3f),
                // Their reach (Weapon_Daggers.asset's, shared with the hall and the Descent unless made foundry-only).
                Make("Daggers reach 1.8 -> 2.2", l => l.DaggersReach = 2.2f),
                Make("Daggers reach 2.0 + on the elites x3", l => { l.DaggersReach = 2f; l.DaggersEliteDamage = 3f; }),
                Make("Daggers reach 2.2 + on the elites x3", l => { l.DaggersReach = 2.2f; l.DaggersEliteDamage = 3f; }),
                Make("Daggers reach 2.2 + on the elites x5", l => { l.DaggersReach = 2.2f; l.DaggersEliteDamage = 5f; }),
                Make("Daggers reach 2.5 + on the elites x3", l => { l.DaggersReach = 2.5f; l.DaggersEliteDamage = 3f; }),
                Make("Daggers on the elites x10", l => l.DaggersEliteDamage = 10f),
                // The share on the elite rooms' leaders alone, their Grunt Captains (the gate's elites; the shipped 12).
                Make("Daggers on the Captains x3", Captains(3f)),
                Make("Daggers on the Captains x5", Captains(5f)),
                Make("Daggers on the Captains x8", Captains(8f)),
                Make("Daggers on the Captains x15", Captains(15f)),
                Make("Daggers on the Captains x10", Captains(10f)),
                Make("Daggers on the Captains x12", Captains(12f)),
                Make("Daggers on the Captains x1", Captains(1f))
            };
        }

        // One lever's batches in one pair of hands: each cohort for each hero (the careful Vanguard is the Vanguard started
        // with the Sword), and the Vanguard started with the Staff and with the Daggers in the careful hands.
        internal sealed class RoleRun
        {
            public Lever Lever;
            public PilotRealism Realism;
            public List<(AutopilotCohort Cohort, Batch[] Batches)> Cohorts = new List<(AutopilotCohort, Batch[])>();
            public Batch[] Weapons;
        }

        public static RoleRun RunRoles(Lever lever, int[] seeds, int parallelism, int floors, PilotRealism realism)
        {
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };
            string hands = realism != null ? ", " + realism.Name : string.Empty;
            var run = new RoleRun { Lever = lever, Realism = realism };
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
            {
                DescentSimulation.FoundryLevers levers = lever.Levers.Copy(lever.Levers.Name + ", cohort " + cohort.Name + hands);
                levers.Cohort = cohort;
                levers.Floors = floors;
                levers.Realism = realism;
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
                DescentSimulation.FoundryLevers levers = lever.Levers.Copy(lever.Levers.Name + ", starting " + weapon.Id + hands);
                levers.Floors = floors;
                levers.Realism = realism;
                weapons.Add(Run(hero, levers, seeds, parallelism));
            }
            run.Weapons = weapons.ToArray();
            return run;
        }

        // A starting weapon's three jobs, P50s over the batch: its own damage on the horde a second it was held and on the
        // Warden a second of the fight after the reveal (FoundryGate.Efficiency's), and on the elites a second of the elite
        // rooms' fights over the runs that met them - their whole packs, or with captains the gate's, their Grunt Captains.
        public static double[] RoleJobsOf(Batch b, bool captains = false)
        {
            (double Horde, double Elites, double Warden) gate = FoundryGate.Efficiency(b);
            double horde = gate.Horde, warden = gate.Warden;
            string id = b.Hero.Weapon.Id;
            double elites = Median(b.Runs.Where(r => r.EliteFought > 0f).Select(r =>
                r.WeaponWork.Where(w => w.Id == id).Sum(w => captains ? w.DealtEliteLeader : w.DealtElite) / r.EliteFought));
            return new[] { horde, elites, warden };
        }

        public static string RoleHeader() =>
            "| Lever | Hands | Horde a second: Sword / Staff / Daggers | Elite packs a second | Warden a second | Roles, the packs as the elites | Captains (the gate's elites) a second | Roles (the gate's) | " +
            "Floor 1 cleared V (careful / standing / first-card / clumsy / auto-farmer) | Floor 1 cleared I | Both floors V | Both floors I | " +
            "Warden P50 floor 1 V / I | Warden P50 floor 2 V / I |" +
            Environment.NewLine + "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|";

        public static string RoleRow(RoleRun run)
        {
            var jobs = run.Weapons.Select(b => (Name: b.Hero.Weapon.DisplayName, Jobs: RoleJobsOf(b))).ToArray();
            var captains = run.Weapons.Select(b => (Name: b.Hero.Weapon.DisplayName, Jobs: RoleJobsOf(b, true))).ToArray();
            string Three(int job) => string.Join(" / ", jobs.Select(j => N(j.Jobs[job])));
            string Cohorts(int hero, Func<DescentSimulation.FoundryResult, bool> won) =>
                string.Join(" / ", run.Cohorts.Select(c => c.Batches[hero].Runs.Count(won).ToString(CultureInfo.InvariantCulture)));
            Batch[] careful = run.Cohorts[0].Batches;
            string Warden(Func<Batch, IEnumerable<float>> fights) =>
                N(Median(fights(careful[0]))) + " / " + N(Median(fights(careful[1])));
            return $"| {run.Lever.Name} | {(run.Realism != null ? run.Realism.Name : "frame-perfect")} | {Three(0)} | {Three(1)} | {Three(2)} | {Roles(jobs)} | " +
                   $"{string.Join(" / ", captains.Select(c => N(c.Jobs[1])))} | {Roles(captains)} | " +
                   $"{Cohorts(0, r => r.Cleared)} | {Cohorts(1, r => r.Cleared)} | {Cohorts(0, BothCleared)} | {Cohorts(1, BothCleared)} | " +
                   $"{Warden(b => b.Runs.Where(r => r.LevelAtGate >= 0 && r.BossSeconds >= 0f).Select(r => r.BossSeconds))} | " +
                   $"{Warden(b => FloorTwo(b).Runs.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds))} |";
        }

        private static bool BothCleared(DescentSimulation.FoundryResult run) => run.Cleared && run.NextFloor != null && run.NextFloor.Cleared;

        // Three jobs read for three weapons: each weapon's own (FoundryGate.LeadsJob), "met" when each has one.
        public static string Roles(IReadOnlyList<(string Name, double[] Jobs)> jobs)
        {
            var parts = new List<string>();
            bool met = true;
            foreach ((string Name, double[] Jobs) a in jobs)
            {
                int job = FoundryGate.LeadsJob(a.Jobs, jobs.Where(b => b.Name != a.Name).Select(b => b.Jobs).ToArray());
                met &= job >= 0;
                parts.Add(a.Name + " " + (job >= 0 ? FoundryGate.JobNames[job] : "none"));
            }
            return (met ? "met: " : "not met: ") + string.Join(", ", parts);
        }

        // A line a batch: the clears, the Warden's fights, the starting weapon's three jobs, and what binds the elites' job -
        // the elites' fights' length, every weapon's damage on them a second, the starting weapon's share of it, and its
        // damage on the Captains alone a second.
        public static string RoleCsvHeader() =>
            "lever,hands,cohort,hero,runs,floor1_cleared,both_cleared,warden_p50_f1,warden_p50_f2,start_weapon_horde,start_weapon_elites,start_weapon_warden," +
            "elite_fights_p50,weapons_on_elites_p50,start_weapon_share_of_elites,start_weapon_captains";

        public static IEnumerable<string> RoleCsv(RoleRun run)
        {
            string hands = run.Realism != null ? run.Realism.Name : "frame-perfect";
            foreach ((AutopilotCohort cohort, Batch[] batches) in run.Cohorts)
                foreach (Batch b in batches)
                    yield return RoleCsvLine(run.Lever.Name, hands, cohort.Name, b);
            foreach (Batch b in run.Weapons.Where(w => w.Hero.Weapon.Id != DescentSimulation.SwordId))
                yield return RoleCsvLine(run.Lever.Name, hands, "careful", b);
        }

        private static string RoleCsvLine(string lever, string hands, string cohort, Batch b)
        {
            double[] jobs = RoleJobsOf(b);
            DescentSimulation.FoundryResult[] met = b.Runs.Where(r => r.EliteFought > 0f).ToArray();
            double all = b.Runs.Sum(r => r.WeaponWork.Sum(w => w.DealtElite));
            double own = b.Runs.Sum(r => r.WeaponWork.Where(w => w.Id == b.Hero.Weapon.Id).Sum(w => w.DealtElite));
            return string.Join(",", "\"" + lever + "\"", hands, cohort, "\"" + b.Hero.Name + "\"", b.Runs.Length,
                b.ClearedCount, b.Runs.Count(BothCleared),
                N(Median(b.Runs.Where(r => r.LevelAtGate >= 0 && r.BossSeconds >= 0f).Select(r => r.BossSeconds))),
                N(Median(FloorTwo(b).Runs.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds))),
                N(jobs[0], "0.0"), N(jobs[1], "0.0"), N(jobs[2], "0.0"),
                N(Median(met.Select(r => r.EliteFought)), "0.0"),
                N(Median(met.Select(r => r.WeaponWork.Sum(w => w.DealtElite) / r.EliteFought)), "0.0"),
                all > 0 ? N(100.0 * own / all) : "-",
                N(RoleJobsOf(b, true)[1], "0.0"));
        }
    }
}
