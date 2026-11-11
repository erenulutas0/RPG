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
    // fight. Two readings of the elites: their whole packs (an elite room's node wave, FoundryWeaponWork.DealtElite) and
    // the elite rooms' Grunt Captains alone (DealtEliteLeader), each a second of FoundryResult.EliteFought. A lever can
    // also give the careful-footwork cohorts a habit (Lever.CarefulHabits: the pilot's step-in, AutopilotCohort.Habit.StepsIn).
    // Pure: it only runs DescentSimulation.RunFoundry and counts.
    internal static partial class FoundryBatch
    {
        // The rows of the Daggers' report (FoundryBatchReport.TheDaggersRoleOverASeedBatch, DAGGERS_LEVERS by index). The
        // first row is the shipped floor; every other sets a Daggers lever or a pilot's habit on it (docs/41, status
        // 2026-11-10 night).
        public static Lever[] DaggersLevers()
        {
            Lever Make(string name, Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = Shipped().Copy(name);
                set(levers);
                return new Lever { Name = name, Estimate = "daggers", Levers = levers };
            }

            // The Daggers dealing the Captains share in all, the shipped EliteRule.DaggersShare divided out.
            Action<DescentSimulation.FoundryLevers> Captains(float share) => l =>
            {
                l.DaggersEliteDamage = share / Cryptforge.Combat.EliteRule.DaggersShare;
                l.DaggersEliteLeaderOnly = true;
            };

            Lever Before()
            {
                Lever lever = Make("Before: the Daggers x1 on the Captains, the careful pilots kiting them", Captains(1f));
                lever.CarefulHabitsDropped = AutopilotCohort.Habit.StepsIn;
                return lever;
            }

            // The careful-footwork cohorts stepping in on a single elite room's Captain and the gate boss (FootworkRule.StepIn),
            // or on the Captain alone.
            Lever SteppingIn(string name, Action<DescentSimulation.FoundryLevers> set)
            {
                Lever lever = Make(name, set);
                lever.CarefulHabits = AutopilotCohort.Habit.StepsIn | AutopilotCohort.Habit.StepsInOnTheGateBoss;
                return lever;
            }

            Lever SteppingInOnTheElites(string name, Action<DescentSimulation.FoundryLevers> set)
            {
                Lever lever = Make(name, set);
                lever.CarefulHabits = AutopilotCohort.Habit.StepsIn;
                return lever;
            }

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
                // The share on the elite rooms' leaders alone, their Grunt Captains, in all (x12 was shipped and reverted on
                // 2026-11-10; x2 ships on 2026-11-11 with the careful pilots stepping in).
                Make("Daggers on the Captains x3", Captains(3f)),
                Make("Daggers on the Captains x5", Captains(5f)),
                Make("Daggers on the Captains x8", Captains(8f)),
                Make("Daggers on the Captains x15", Captains(15f)),
                Make("Daggers on the Captains x10", Captains(10f)),
                Make("Daggers on the Captains x12", Captains(12f)),
                // The pilot's spacing: the careful and first-card cohorts step in (AutopilotCohort.Habit.StepsIn), alone and
                // with a readable Daggers mechanic of x2 at most.
                SteppingIn("The careful pilot steps in", l => { }),
                SteppingIn("Steps in + the Daggers x2 on the elites", l => l.DaggersEliteDamage = 2f),
                SteppingIn("Steps in + the Daggers x2 on the Captains", Captains(2f)),
                Make("The Daggers x2 on the elites", l => l.DaggersEliteDamage = 2f),
                SteppingIn("Steps in + the Daggers' crits x2 on the elites", l => l.DaggersEliteCritDamage = 2f),
                // The step-in on the elite rooms' Captains alone, the gate boss kited as before.
                SteppingInOnTheElites("Steps in on the elites", l => { }),
                SteppingInOnTheElites("Steps in on the elites + the Daggers x2 on the elites", l => l.DaggersEliteDamage = 2f),
                SteppingInOnTheElites("Steps in on the elites + the Daggers x2 on the Captains", Captains(2f)),
                SteppingInOnTheElites("Steps in on the elites + the Daggers' crits x2 on the elites", l => l.DaggersEliteCritDamage = 2f),
                // The Daggers' design options (docs/41, status 2026-11-11): shapes a player can see, on every enemy alike, none
                // above x2 and none a one-shot. The throw: with nothing within 1.8, one blade at the nearest within 3.2, at most
                // once a second, or at every strike (the reach alone).
                Make("Throw at 3.2, a blade a second", Throw(1f)),
                Make("Throw at 3.2, every strike", Throw(0f)),
                // The bleed: each Daggers landing's wound deals as much again over 3 s (x2 at most, over time).
                Make("Bleed, 100 % over 3 s", Bleed(1f)),
                Make("Throw a second + bleed", l => { Throw(1f)(l); Bleed(1f)(l); }),
                // The lunge: with nothing within 1.8, the hero dashes to 1.4 from an enemy within 3.0 and strikes, every 2 s at
                // most - at the nearest, or at the heaviest (the elite room's Captain, a hall's Tank, the Warden).
                Make("Lunge at the nearest, 3.0, every 2 s", Lunge(false)),
                Make("Lunge at the heaviest, 3.0, every 2 s", Lunge(true)),
                // The mark: a Daggers landing exposes its target for 3 s, +30 % from every weapon, the extra the Daggers'.
                Make("Mark, +30 % for 3 s", Mark(.3f)),
                Make("Throw a second + mark", l => { Throw(1f)(l); Mark(.3f)(l); }),
                // The execute: x2 under 30 % of the target's health.
                Make("Execute, x2 under 30 %", Execute),
                Make("Throw a second + execute", l => { Throw(1f)(l); Execute(l); }),
                // With the pilot's step-in on the elite rooms' Captains (off for every cohort in the game).
                SteppingInOnTheElites("Steps in on the elites + bleed", Bleed(1f)),
                SteppingInOnTheElites("Steps in on the elites + mark", Mark(.3f)),
                Make("Lunge at the heaviest + bleed", l => { Lunge(true)(l); Bleed(1f)(l); }),
                Make("Throw every strike + bleed", l => { Throw(0f)(l); Bleed(1f)(l); }),
                Make("Lunge at the heaviest + throw a second + bleed", l => { Lunge(true)(l); Throw(1f)(l); Bleed(1f)(l); }),
                // The throw at the heaviest enemy within 3.2 (the elite room's Captain), and the throw's cadence and reach.
                Make("Throw at the heaviest, a blade a second", Throw(1f, true)),
                Make("Throw at the heaviest a second + bleed", l => { Throw(1f, true)(l); Bleed(1f)(l); }),
                Make("Throw every 0.75 s", Throw(.75f)),
                Make("Throw at the heaviest every 0.75 s + bleed", l => { Throw(.75f, true)(l); Bleed(1f)(l); }),
                Make("Throw at 2.6, every strike", l => { Throw(0f)(l); l.DaggersThrowReach = 2.6f; }),
                Make("Lunge at the heaviest every 1 s + bleed", l => { Lunge(true)(l); l.DaggersLungeCooldown = 1f; Bleed(1f)(l); }),
                // A fan: the throw is three (or two) blades, at the nearest enemies within 3.2 - one blade on a lone Warden.
                Make("Fan of 3 at 3.2, a second", Fan(3, 1f)),
                Make("Fan of 3 at 3.2, every 0.75 s", Fan(3, .75f)),
                Make("Fan of 2 at 3.2, a second", Fan(2, 1f)),
                Make("Fan of 3 a second + bleed", l => { Fan(3, 1f)(l); Bleed(1f)(l); }),
                // The throw at every strike with the gate boss deflecting thrown blades (its armour; the Daggers' strikes
                // within 1.8 land on it as ever), so the throw's reach does not take the Warden from the Sword.
                Make("Throw every strike, the Warden deflects throws", l => { Throw(0f)(l); l.DaggersThrowSparesGateBoss = true; }),
                Make("Throw every strike + bleed, the Warden deflects throws", l =>
                {
                    Throw(0f)(l);
                    Bleed(1f)(l);
                    l.DaggersThrowSparesGateBoss = true;
                }),
                Make("Throw every 0.75 s + bleed, the Warden deflects throws", l =>
                {
                    Throw(.75f)(l);
                    Bleed(1f)(l);
                    l.DaggersThrowSparesGateBoss = true;
                }),
                // How much of the thrown Daggers the three roles need: half the bleed, and a throw of 2.6 (just past the
                // careful pilot's kite) rather than 3.2.
                Make("Throw every strike + bleed 50 %, the Warden deflects throws", l =>
                {
                    Throw(0f)(l);
                    Bleed(.5f)(l);
                    l.DaggersThrowSparesGateBoss = true;
                }),
                Make("Throw at 2.6 every strike + bleed, the Warden deflects throws", l =>
                {
                    Throw(0f)(l);
                    l.DaggersThrowReach = 2.6f;
                    Bleed(1f)(l);
                    l.DaggersThrowSparesGateBoss = true;
                }),
                // The thrown Daggers with the gate boss turning both: it deflects thrown blades and does not bleed.
                Make("Throw every strike + bleed, the Warden deflects throws and does not bleed", l =>
                {
                    Throw(0f)(l);
                    Bleed(1f)(l);
                    l.DaggersThrowSparesGateBoss = true;
                    l.DaggersBleedSparesGateBoss = true;
                }),
                // The floor before 2026-11-11 (main 9b5ef052), bit for bit: the Daggers' share divided out on the Captains
                // (x2 then x0.5, both exact in floating point) and the careful pilots kiting a Captain rather than stepping in.
                Before()
            };
        }

        private static Action<DescentSimulation.FoundryLevers> Fan(int blades, float every) => l =>
        {
            Throw(every)(l);
            l.DaggersThrowBlades = blades;
        };

        // The Daggers' design levers (docs/41, status 2026-11-11; FoundryLevers' Daggers design fields).
        private static Action<DescentSimulation.FoundryLevers> Throw(float every, bool heaviest = false) => l =>
        {
            l.DaggersThrowReach = 3.2f;
            l.DaggersThrowEvery = every;
            l.DaggersThrowHeaviest = heaviest;
        };

        private static Action<DescentSimulation.FoundryLevers> Bleed(float share) => l => l.DaggersBleedShare = share;

        private static Action<DescentSimulation.FoundryLevers> Lunge(bool heaviest) => l =>
        {
            l.DaggersLungeReach = 3f;
            l.DaggersLungeHeaviest = heaviest;
        };

        private static Action<DescentSimulation.FoundryLevers> Mark(float share) => l => l.DaggersMarkShare = share;

        private static void Execute(DescentSimulation.FoundryLevers l) => l.DaggersExecuteBelow = .3f;

        // One lever's batches in one pair of hands: each cohort for each hero (the careful Vanguard is the Vanguard started
        // with the Sword), and the Vanguard started with the Staff and with the Daggers in the careful hands. cohorts, when
        // given, plays those alone (SweepCohorts: the careful one always first) - a screen of the jobs before every cohort.
        internal sealed class RoleRun
        {
            public Lever Lever;
            public PilotRealism Realism;
            public List<(AutopilotCohort Cohort, Batch[] Batches)> Cohorts = new List<(AutopilotCohort, Batch[])>();
            public Batch[] Weapons;
        }

        public static RoleRun RunRoles(Lever lever, int[] seeds, int parallelism, int floors, PilotRealism realism,
            IReadOnlyList<AutopilotCohort> cohorts = null)
        {
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };
            string hands = realism != null ? ", " + realism.Name : string.Empty;
            var run = new RoleRun { Lever = lever, Realism = realism };
            foreach (AutopilotCohort cohort in cohorts ?? AutopilotCohort.Cohorts)
            {
                DescentSimulation.FoundryLevers levers = lever.Levers.Copy(lever.Levers.Name + ", cohort " + cohort.Name + hands);
                levers.Cohort = Played(lever, cohort);
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
                levers.Cohort = Played(lever, AutopilotCohort.Careful);
                levers.Floors = floors;
                levers.Realism = realism;
                weapons.Add(Run(hero, levers, seeds, parallelism));
            }
            run.Weapons = weapons.ToArray();
            return run;
        }

        // A starting weapon's three jobs, P50s over the batch: its own damage on the horde a second it was held and on the
        // Warden a second of the fight after the reveal (FoundryGate.Efficiency's), and on the elites a second of the elite
        // rooms' fights over the runs that met them - their whole packs (the gate's), or with captains their Grunt Captains.
        public static double[] RoleJobsOf(Batch b, bool captains = false)
        {
            (double Horde, double Elites, double Warden) gate = FoundryGate.Efficiency(b);
            double horde = gate.Horde, warden = gate.Warden;
            string id = b.Hero.Weapon.Id;
            double elites = Median(b.Runs.Where(r => r.EliteFought > 0f).Select(r =>
                r.WeaponWork.Where(w => w.Id == id).Sum(w => captains ? w.DealtEliteLeader : w.DealtElite) / r.EliteFought));
            return new[] { horde, elites, warden };
        }

        // The cohort a lever plays: the careful-footwork cohorts (careful, first-card) with the lever's CarefulHabits and
        // without its CarefulHabitsDropped, every other as it is.
        private static AutopilotCohort Played(Lever lever, AutopilotCohort cohort) =>
            (lever.CarefulHabits != AutopilotCohort.Habit.None || lever.CarefulHabitsDropped != AutopilotCohort.Habit.None) &&
            (cohort == AutopilotCohort.Careful || cohort == AutopilotCohort.FirstCard)
                ? new AutopilotCohort(cohort.Name, (cohort.Habits | lever.CarefulHabits) & ~lever.CarefulHabitsDropped)
                : cohort;

        public static string RoleHeader() =>
            "| Lever | Hands | Horde a second: Sword / Staff / Daggers | Elite rooms' waves a second | Warden a second | Roles, the waves as the elites | Elite enemies (Captains) a second | Roles, the elite enemies (the gate's) | " +
            "Floor 1 cleared V (careful / standing / first-card / clumsy / auto-farmer) | Floor 1 cleared I | Both floors V | Both floors I | " +
            "Warden P50 floor 1 V / I | Warden P50 floor 2 V / I | Elite fights P50 s, started with each | Every weapon on the elites a second, the same | " +
            "A Captain under fire P50 s, the same | A Captain the Daggers reached, under fire P10 / P50 s (felled; within a frame), careful | The Sword's lead on the Warden |" +
            Environment.NewLine + "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|";

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
                   $"{Warden(b => FloorTwo(b).Runs.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds))} | " +
                   $"{string.Join(" / ", run.Weapons.Select(b => N(Median(b.Runs.Where(r => r.EliteFought > 0f).Select(r => r.EliteFought)))))} | " +
                   $"{string.Join(" / ", run.Weapons.Select(b => N(Median(b.Runs.Where(r => r.EliteFought > 0f).Select(r => r.WeaponWork.Sum(w => w.DealtElite) / r.EliteFought)))))} | " +
                   $"{string.Join(" / ", run.Weapons.Select(b => N(CaptainUnderFire(b), "0.0")))} | " +
                   $"{DaggersCaptains(run)} | {SwordWardenLead(captains)} |";
        }

        // The Captains a Daggers landing reached before they fell (FoundryKill.DaggersLanded), in the careful hands - both
        // heroes and the Vanguard started with each weapon - their time under fire P10 / P50, how many, and how many fell
        // within a frame of their first damage (none: no one-shot).
        public static string DaggersCaptains(RoleRun run)
        {
            float[] underFire = run.Cohorts[0].Batches.Concat(run.Weapons.Where(w => w.Hero.Weapon.Id != DescentSimulation.SwordId))
                .SelectMany(b => b.Runs).SelectMany(r => r.Kills)
                .Where(k => !k.Horde && k.Kind == "Grunt Captain" && k.DaggersLanded).Select(k => k.UnderFire).ToArray();
            return $"{N(Percentile(underFire, 10), "0.0")} / {N(Percentile(underFire, 50), "0.0")} ({underFire.Length}; {underFire.Count(u => u <= 1f / 60f)})";
        }

        // The Sword's Warden figure against the best other weapon's, as a lead in percent.
        private static string SwordWardenLead(IReadOnlyList<(string Name, double[] Jobs)> jobs)
        {
            double sword = jobs.First(j => j.Name == "Sword").Jobs[2];
            double best = jobs.Where(j => j.Name != "Sword").Max(j => j.Jobs[2]);
            return best > 0 ? N(100.0 * (sword / best - 1.0)) + " %" : "-";
        }

        // docs/40 section 8.6's elite TTK, the elite's time under fire: P50 over the batch's felled elite-room Captains of the
        // seconds from the first damage each took to its fall (FoundryKill.UnderFire).
        public static double CaptainUnderFire(Batch b) =>
            Median(b.Runs.SelectMany(r => r.Kills).Where(k => !k.Horde && k.Kind == "Grunt Captain").Select(k => k.UnderFire));

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
            "elite_fights_p50,weapons_on_elites_p50,start_weapon_share_of_elites,start_weapon_captains,step_in_s_p50,daggers_throws_p50,daggers_lunges_p50," +
            "captains_daggers_reached,captains_daggers_under_fire_p10,captains_daggers_under_fire_p50";

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
            float[] reached = b.Runs.SelectMany(r => r.Kills).Where(k => !k.Horde && k.Kind == "Grunt Captain" && k.DaggersLanded)
                .Select(k => k.UnderFire).ToArray();
            return string.Join(",", "\"" + lever + "\"", hands, cohort, "\"" + b.Hero.Name + "\"", b.Runs.Length,
                b.ClearedCount, b.Runs.Count(BothCleared),
                N(Median(b.Runs.Where(r => r.LevelAtGate >= 0 && r.BossSeconds >= 0f).Select(r => r.BossSeconds))),
                N(Median(FloorTwo(b).Runs.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds))),
                N(jobs[0], "0.0"), N(jobs[1], "0.0"), N(jobs[2], "0.0"),
                N(Median(met.Select(r => r.EliteFought)), "0.0"),
                N(Median(met.Select(r => r.WeaponWork.Sum(w => w.DealtElite) / r.EliteFought)), "0.0"),
                all > 0 ? N(100.0 * own / all) : "-",
                N(RoleJobsOf(b, true)[1], "0.0"), N(Median(b.Runs.Select(r => r.StepInSeconds)), "0.0"),
                N(Median(b.Runs.Select(r => (float)r.DaggersThrows)), "0"), N(Median(b.Runs.Select(r => (float)r.DaggersLunges)), "0"),
                reached.Length, N(Percentile(reached, 10), "0.00"), N(Percentile(reached, 50), "0.00"));
        }
    }
}
