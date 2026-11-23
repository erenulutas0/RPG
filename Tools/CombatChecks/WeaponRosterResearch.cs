using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/63's weapon roster measured (section 9): each hero's own weapon kept to that hero and the shared pool - the
    // Daggers, the Magma Mortar and the Forge Bow - for both (FoundryLevers.WeaponRoster, DescentSimulation.WeaponRoster.cs)
    // against the shipped catalogue, every playing cohort and both heroes down the whole climb (floors 1-3 and the finale),
    // with human hands, on the exploration seeds 1-2000 only (docs/56 rule 1: no confirmation block is spent here). Each
    // option plays the same seeds as `shipped`, so every row reads against today:
    //
    // - completion by cohort (the finale walked out of), the change in points against `shipped` (paired, docs/56's read:
    //   Tools/BalanceRule/paired_bootstrap.py reads weapon-roster.csv for the bootstrap CI);
    // - each weapon's pick rate (runs that took it; offers that showed it as a new weapon, and of those runs the share that
    //   took it) and damage share (of the weapons' damage, in the runs that held it and over all runs), and the climb's
    //   kills by weapon, so no weapon dominates unseen;
    // - the decomposition: `exclusive` (each hero's own kept to it, no new weapon: the Vanguard holds the Sword and the
    //   Daggers, Ilya the Staff and the Daggers) and `open` (all five weapons for both heroes, no exclusivity), and the A/A
    //   probe from floor 1 (`aa-f1`: the shipped game with one unread draw of the pilot's dice).
    // - phase 2 (docs/63 section 13): the lead's two candidates, `p2-c2` (a second shared crowd weapon, the Cinder Saw) and
    //   `p2-c1` (the crowd-weapon guarantee), and both (`p2-c1c2`), each beside c-mortar15's Mortar; each hero's own weapon's
    //   share of its damage (Ilya's Staff must stay her main damage) and the runs that held a crowd weapon.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~WeaponRosterResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1; the probe refuses a seed past 2000 unless
    // ROSTER_CONFIRM names the fresh block it reads, "<first>-<last>", for one candidate beside `shipped` and the A/A),
    // FOUNDRY_PARALLEL (the processors), FOUNDRY_OUT (the repository's TestResults), FOUNDRY_REALISM (typical; slow;
    // frame-perfect for none), ROSTER_OPTIONS (comma separated option keys; shipped,roster by default, "all" for every one;
    // a key may change a phase-2 candidate's numbers, "p2-c2~saw=5/0.2/2.5~mortar=30", see Named),
    // ROSTER_COHORTS (comma separated cohort names; careful, standing, first-card, clumsy, auto-farmer). Writes
    // weapon-roster.md and weapon-roster.csv (a row a run), rewritten after each option.
    [Explicit("docs/63's measurement: run it by name.")]
    public sealed class WeaponRosterResearch
    {
        // The six hero weapons in the scene's order (CombatSetup._weapons, then _rosterWeapons; phase 2 added the Cinder Saw).
        private static readonly string[] WeaponIds =
        {
            DescentSimulation.SwordId, DescentSimulation.StaffId, DescentSimulation.DaggersId, WeaponRoster.MagmaMortarId,
            WeaponRoster.ForgeBowId, WeaponRoster.CinderSawId
        };

        private static readonly string[] Short = { "sword", "staff", "daggers", "mortar", "bow", "saw" };

        private sealed class Option
        {
            public string Key;
            public string What;
            public Action<DescentSimulation.FoundryLevers> Set = _ => { };
        }

        // A copy of a shipped weapon with its numbers changed, for a candidate.
        private static DescentSimulation.HeroWeapon Tuned(DescentSimulation.HeroWeapon weapon, Action<DescentSimulation.HeroWeapon> set)
        {
            DescentSimulation.HeroWeapon copy = weapon.Copy();
            set(copy);
            return copy;
        }

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "today's catalogue: the Sword, the Staff and the Daggers for both heroes" };
            yield return new Option
            {
                Key = "aa-f1", What = "A/A: the shipped game, one unread draw of the pilot's dice on entering floor 1",
                Set = l => l.NudgeFloor = 1
            };
            // Phase 1's readings (docs/63 section 9) keep their pool - the five weapons, the Mortar at 2.5 every 1.8 s, no Cinder
            // Saw (PhaseOne) - so they read the same runs they read on 2026-11-23 whatever phase 2 adds to RosterWeapons.
            yield return new Option
            {
                Key = "roster", What = "docs/63's roster as built in phase 1: each hero's own weapon its alone (Vanguard Sword, Ilya Staff), the Daggers, the Magma Mortar (34 / 1.8 s, reach 4, blast 2.5 at full damage) and the Forge Bow (25 / 1.2 s, reach 5, a crit every 4th) shared",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = PhaseOne();
                }
            };
            yield return new Option
            {
                Key = "exclusive", What = "decomposition: each hero's own weapon its alone and no new weapon (Vanguard: Sword + Daggers; Ilya: Staff + Daggers)",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = DescentSimulation.WeaponCatalogue();
                }
            };
            yield return new Option
            {
                Key = "open", What = "decomposition: all five weapons for both heroes, no exclusivity (the new weapons added to today's catalogue)",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = PhaseOne();
                    l.RosterSignatureIds = new string[0];
                }
            };
            yield return new Option
            {
                Key = "roster-no-bow", What = "decomposition: the roster without the Forge Bow",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = PhaseOne().Where(w => w.Id != WeaponRoster.ForgeBowId).ToArray();
                }
            };
            yield return new Option
            {
                Key = "roster-no-mortar", What = "decomposition: the roster without the Magma Mortar",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = PhaseOne().Where(w => w.Id != WeaponRoster.MagmaMortarId).ToArray();
                }
            };
            // Candidates (exploration only), one design reason written before the read (docs/63 section 9): under the roster
            // the Staff is Ilya's alone, and in today's runs it deals about two thirds of the Vanguard's weapon damage (seeds
            // 1-20's probe: 65 %); the shared pool's area weapon has to carry that job for the Vanguard. The Staff covers
            // 24 / 1.1 s x 75 % over a 3.5 circle, about 200 (damage a second times the circle's squared radius); the Mortar
            // as built 34 / 1.8 s x 100 % over 2.5, about 118. `c-blast3` widens the blast to 3.0 (170); `c-mortar15` also
            // fires every 1.5 s (204, the Staff's output at a slower, heavier cadence).
            foreach ((string key, string what, float radius, float interval) in new[]
                     {
                         ("c-blast3", "the roster with the Mortar's blast at 3.0 (area output about 170 against the Staff's 200)", 3f, 1.8f),
                         ("c-mortar15", "the roster with the Mortar's blast at 3.0 every 1.5 s (area output about 204: the Staff's, heavier and slower)", 3f, 1.5f)
                     })
            {
                float r = radius, s = interval;
                yield return new Option
                {
                    Key = key, What = what,
                    Set = l =>
                    {
                        l.WeaponRoster = true;
                        l.RosterPool = MortarAt(r, s, PhaseOne());
                    }
                };
            }
            // Read after `roster` (seeds 1-2000): the Vanguard's runs that were offered the Mortar and passed it completed 2 %,
            // those that took it 53 % - its Sword, the Daggers and the Bow hold no crowd. Two readings of that, written before
            // their read: `pref-area` is the roster as built with a pilot that, when it takes a new weapon, takes the area weapon
            // first (the card order is otherwise the cohort's own; the first-card cohort, which scores nothing, is unchanged):
            // how much of the cost is the autopilot passing the only crowd weapon, which a player on a horde floor would not.
            // `c-sweep` gives the Vanguard's own Sword the crowd: the sweep FoundryBatch.SwordLevers measured in 2026-11-04's
            // research (every enemy within 2.5 of the hero takes 60 %, reach 2.5) - a signature that carries its hero, as Ilya's
            // Staff carries Ilya; simulation only (the scene's Sword is not reshaped). `c-sweep-m15` adds c-mortar15's Mortar.
            yield return new Option
            {
                Key = "pref-area", What = "the roster as built, the pilot taking the area weapon (the Magma Mortar) first among new weapons",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = PhaseOne();
                    AutopilotCohort cohort = l.Cohort;
                    l.Picker = (pick, upgrades) => AreaFirst(cohort, pick, upgrades);
                }
            };
            yield return new Option
            {
                Key = "c-sweep", What = "the roster as built with the Vanguard's Sword sweeping (every enemy within 2.5 of the hero takes 60 %, reach 2.5)",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = PhaseOne();
                    Sweep(l);
                }
            };
            yield return new Option
            {
                Key = "c-sweep-m15", What = "c-sweep with c-mortar15's Mortar (blast 3.0 every 1.5 s)",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = MortarAt(3f, 1.5f, PhaseOne());
                    Sweep(l);
                }
            };

            // Phase 2 (docs/63 section 13, 2026-11-23): the lead's two candidates of section 10, both on c-mortar15's Mortar
            // (blast 3.0 every 1.5 s), each with its design reason written before its read. The roster failed because the
            // Vanguard's runs that never hold the Mortar - 43 % of them - finish 2 %: its Sword, the Daggers and the Bow hold no
            // crowd, and the Staff that held it today is Ilya's alone.
            // - `p2-c2` removes that failure by the pool's shape: a second shared crowd weapon, the Cinder Saw (an aura round
            //   the hero, docs/40 section 5.2's 5 every 0.2 s within 2 units), so two of the Vanguard's four shared choices
            //   hold a crowd and a run that passes one is still likely to take the other.
            // - `p2-c1` removes it by a rule: UpgradeService's crowd-weapon guarantee (while a hero holds no crowd weapon and a
            //   slot is free, every new-weapon card is one), so the first new weapon the Vanguard takes holds a crowd; Ilya's
            //   Staff already does, so Ilya is untouched. The pool is phase 1's five, the Mortar at c-mortar15.
            // - `p2-c1c2` both: the guarantee picks between two crowd weapons.
            yield return new Option
            {
                Key = "p2-c2", What = "C2: the roster with c-mortar15's Mortar (blast 3.0 every 1.5 s) and the Cinder Saw (an aura: 5 every 0.2 s to every enemy within 2 of the hero) shared",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = MortarAt(3f, 1.5f, DescentSimulation.RosterWeapons());
                }
            };
            yield return new Option
            {
                Key = "p2-c1", What = "C1: phase 1's roster with c-mortar15's Mortar and the crowd-weapon guarantee (no Saw)",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = MortarAt(3f, 1.5f, PhaseOne());
                    l.CrowdWeaponGuarantee = true;
                }
            };
            yield return new Option
            {
                Key = "p2-c1c2", What = "C1 + C2: the roster with c-mortar15's Mortar and the Cinder Saw, and the crowd-weapon guarantee",
                Set = l =>
                {
                    l.WeaponRoster = true;
                    l.RosterPool = MortarAt(3f, 1.5f, DescentSimulation.RosterWeapons());
                    l.CrowdWeaponGuarantee = true;
                }
            };
        }

        // An option by its key: one of Options(), or - for exploring a candidate's numbers on the exploration seeds - a base
        // option with the Cinder Saw's or the Mortar's numbers changed, "<base>~saw=<damage>/<interval>/<radius>",
        // "<base>~blast=<radius>/<interval>" and "<base>~mortar=<damage>", applied in order (for example
        // "p2-c2~saw=5/0.2/2.5~mortar=30").
        private static Option Named(string key)
        {
            Option known = Options().FirstOrDefault(o => o.Key == key);
            if (known != null)
                return known;
            string[] parts = key.Split('~');
            Option baseOption = Options().FirstOrDefault(o => o.Key == parts[0]);
            if (baseOption == null || parts.Length < 2)
                throw new ArgumentException($"No option {key}.");
            var changes = new List<Action<DescentSimulation.FoundryLevers>>();
            var what = new StringBuilder(baseOption.What);
            for (int i = 1; i < parts.Length; i++)
            {
                string[] pair = parts[i].Split('=');
                float[] n = pair.Length == 2
                    ? pair[1].Split('/').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray()
                    : new float[0];
                if (pair[0] == "saw" && n.Length == 3)
                {
                    float d = n[0], s = n[1], r = n[2];
                    changes.Add(l => l.RosterPool = SawAt(d, s, r, l.RosterPool ?? DescentSimulation.RosterWeapons()));
                    what.Append(string.Format(CultureInfo.InvariantCulture, "; the Saw at {0} every {1} s within {2}", d, s, r));
                }
                else if (pair[0] == "blast" && n.Length == 2)
                {
                    float r = n[0], s = n[1];
                    changes.Add(l => l.RosterPool = MortarAt(r, s, l.RosterPool ?? DescentSimulation.RosterWeapons()));
                    what.Append(string.Format(CultureInfo.InvariantCulture, "; the Mortar's blast {0} every {1} s", r, s));
                }
                else if (pair[0] == "mortar" && n.Length == 1)
                {
                    float d = n[0];
                    changes.Add(l => l.RosterPool = (l.RosterPool ?? DescentSimulation.RosterWeapons())
                        .Select(w => w.Id == WeaponRoster.MagmaMortarId ? Tuned(w, m => m.Damage = d) : w).ToArray());
                    what.Append(string.Format(CultureInfo.InvariantCulture, "; the Mortar's damage {0}", d));
                }
                else
                {
                    throw new ArgumentException($"Option {key}: {parts[i]} is not saw=D/I/R, blast=R/I or mortar=D.");
                }
            }
            return new Option
            {
                Key = key, What = what.ToString(),
                Set = l =>
                {
                    baseOption.Set(l);
                    foreach (Action<DescentSimulation.FoundryLevers> change in changes)
                        change(l);
                }
            };
        }

        // Phase 1's pool (docs/63 section 7): the five hero weapons, the Magma Mortar at its phase-1 numbers (blast 2.5 every
        // 1.8 s), no Cinder Saw.
        private static DescentSimulation.HeroWeapon[] PhaseOne() =>
            MortarAt(2.5f, 1.8f, DescentSimulation.RosterWeapons().Where(w => w.Id != WeaponRoster.CinderSawId).ToArray());

        private static DescentSimulation.HeroWeapon[] MortarAt(float radius, float interval, DescentSimulation.HeroWeapon[] pool) =>
            pool.Select(w => w.Id == WeaponRoster.MagmaMortarId
                ? Tuned(w, m =>
                {
                    m.Pattern = new AttackPattern(WeaponBehavior.Area, radius, 1f);
                    m.Interval = interval;
                })
                : w).ToArray();

        // A pool with the Cinder Saw's numbers changed (exploration of its numbers only).
        private static DescentSimulation.HeroWeapon[] SawAt(float damage, float interval, float radius, DescentSimulation.HeroWeapon[] pool) =>
            pool.Select(w => w.Id == WeaponRoster.CinderSawId
                ? Tuned(w, s =>
                {
                    s.Damage = damage;
                    s.Interval = interval;
                    s.Range = radius;
                    s.Pattern = new AttackPattern(WeaponBehavior.Aura, radius, 1f);
                })
                : w).ToArray();

        // FoundryBatch.SwordLevers' "Sweep 2.5 round the hero, 60%".
        private static void Sweep(DescentSimulation.FoundryLevers levers)
        {
            levers.SwordReach = 2.5f;
            levers.SwordSweepRadius = 2.5f;
            levers.SwordSweepShare = .6f;
        }

        // The cohort's own pick (AutopilotCohort.Pick); when that is a new weapon and the offer also shows the Mortar as one,
        // the Mortar instead.
        private static int AreaFirst(AutopilotCohort cohort, DescentSimulation.FoundryPick pick, UpgradeService upgrades)
        {
            int own = cohort.Pick(pick.Offer, pick.Low, upgrades);
            if (pick.Offer.Choices[own].Category != OfferCategory.NewWeapon)
                return own;
            for (int i = 0; i < pick.Offer.Choices.Count; i++)
            {
                UpgradeOption card = pick.Offer.Choices[i];
                if (card.Category == OfferCategory.NewWeapon && card.Id == WeaponRoster.MagmaMortarId)
                    return i;
            }
            return own;
        }

        private sealed class RunRead
        {
            public string Option;
            public string Cohort;
            public string Hero;
            public int Seed;
            public string[] Outcomes = new string[4];
            public bool Completed;
            public int FloorsCleared;
            public int DiedOn;
            public float Seconds;
            // Per weapon (WeaponIds' order): held at any point of the climb, the floor it was first held on (1-4, 0 never),
            // the offers that showed it as a new weapon, and the damage it dealt over the climb.
            public bool[] Held = new bool[WeaponIds.Length];
            public int[] FirstFloor = new int[WeaponIds.Length];
            public int[] Offered = new int[WeaponIds.Length];
            public float[] Dealt = new float[WeaponIds.Length];
            public int[] Kills = new int[WeaponIds.Length];
            public int WeaponsAtEnd;
            public int WeaponsAfterFloorOne;
        }

        [Test]
        public void TheWeaponRosterProbe()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            // docs/56 rule 1: explore on seeds 1-2000; a confirmation reads one fresh block once, named in ROSTER_CONFIRM as
            // "<first>-<last>" (the block docs/56 section 10 records), with `shipped`, at most one candidate and the A/A.
            string confirm = Environment.GetEnvironmentVariable("ROSTER_CONFIRM");
            if (string.IsNullOrEmpty(confirm))
            {
                Assert.That(first >= 1 && first + count - 1 <= 2000, Is.True,
                    "docs/63's probe: the exploration seeds 1-2000 only (docs/56 rule 1); a confirmation names its block in ROSTER_CONFIRM.");
            }
            else
            {
                Assert.That(confirm, Is.EqualTo($"{first}-{first + count - 1}"), "The confirmation reads exactly the block it names.");
                Assert.That(first, Is.GreaterThan(2000), "A confirmation block is fresh: past the exploration seeds.");
                string[] read = List(Environment.GetEnvironmentVariable("ROSTER_OPTIONS") ?? string.Empty);
                Assert.That(read.Count(o => o != "shipped" && !o.StartsWith("aa-", StringComparison.Ordinal)), Is.EqualTo(1),
                    "One candidate a confirmation (docs/56 rule 4), beside `shipped` and the A/A.");
            }
            int parallel = Setting("FOUNDRY_PARALLEL", Environment.ProcessorCount);
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string optionWord = Environment.GetEnvironmentVariable("ROSTER_OPTIONS") ?? "shipped,roster";
            Option[] options = optionWord == "all" ? Options().ToArray() : List(optionWord).Select(Named).ToArray();
            Assert.That(options.Length, Is.GreaterThan(0), "an option named exists");
            string[] cohorts = List(Environment.GetEnvironmentVariable("ROSTER_COHORTS") ?? "careful,standing,first-card,clumsy,auto-farmer");
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var reads = new List<RunRead>();
            foreach (Option option in options)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts.Where(c => cohorts.Contains(c.Name)))
                foreach (DescentSimulation.FoundryHero hero in heroes)
                {
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                        .Copy($"cohort {cohort.Name}, {option.Key}, {hands}");
                    option.Set(levers);
                    levers.LogsPicks = true;
                    var cell = new RunRead[seeds.Length];
                    Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) }, i =>
                        cell[i] = Read(option.Key, cohort.Name, hero.Name, seeds[i],
                            DescentSimulation.RunFoundry(hero, seeds[i], levers, DescentSimulation.GloamChain(levers))));
                    reads.AddRange(cell);
                }
                TestContext.Progress.WriteLine($"{option.Key}: {watch.Elapsed.TotalSeconds:0} s");
                File.WriteAllText(Path.Combine(output, "weapon-roster.csv"), Csv(reads, hands));
                File.WriteAllText(Path.Combine(output, "weapon-roster.md"), Report(reads, options, cohorts, first, count, hands));
            }
            Assert.That(reads.Count, Is.EqualTo(options.Length * cohorts.Length * heroes.Length * count), "every seed ran");
        }

        private static int IndexOf(string weaponId) => Array.IndexOf(WeaponIds, weaponId);

        private static RunRead Read(string option, string cohort, string hero, int seed, DescentSimulation.FoundryResult run)
        {
            var read = new RunRead { Option = option, Cohort = cohort, Hero = hero, Seed = seed };
            int index = 0;
            for (DescentSimulation.FoundryResult floor = run; floor != null && index < 4; floor = floor.NextFloor, index++)
            {
                bool finale = FoundryBatch.IsFinale(floor);
                read.Outcomes[index] = finale && floor.WalkedOut ? "WalkedOut" : floor.Outcome.ToString();
                read.Seconds += floor.FloorSeconds;
                if (floor.Cleared)
                    read.FloorsCleared++;
                if (finale && floor.WalkedOut)
                    read.Completed = true;
                if (floor.Outcome == DescentSimulation.FoundryOutcome.Died)
                    read.DiedOn = index + 1;
                foreach (DescentSimulation.FoundryWeaponWork work in floor.WeaponWork)
                {
                    int w = IndexOf(work.Id);
                    if (w < 0)
                        continue;
                    if (!read.Held[w])
                        read.FirstFloor[w] = index + 1;
                    read.Held[w] = true;
                    read.Dealt[w] += work.Dealt;
                }
                if (floor.Kills != null)
                    foreach (DescentSimulation.FoundryKill kill in floor.Kills)
                    {
                        int w = IndexOf(kill.By);
                        if (w >= 0)
                            read.Kills[w]++;
                    }
                if (floor.PickLog != null)
                    foreach (DescentSimulation.FoundryPick pick in floor.PickLog)
                        for (int c = 0; c < pick.Offer.Choices.Count; c++)
                        {
                            UpgradeOption card = pick.Offer.Choices[c];
                            int w = card.Category == OfferCategory.NewWeapon ? IndexOf(card.Id) : -1;
                            if (w >= 0)
                                read.Offered[w]++;
                        }
                int held = floor.WeaponWork.Count;
                read.WeaponsAtEnd = held;
                if (index == 0)
                    read.WeaponsAfterFloorOne = held;
            }
            return read;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // The report
        // ---------------------------------------------------------------------------------------------------------------

        private static string Pct(double part, double whole) =>
            whole <= 0 ? "-" : (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture) + " %";

        private static string Pts(double value) => (value >= 0 ? "+" : string.Empty) + value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string Report(List<RunRead> reads, Option[] options, string[] cohorts, int first, int count, string hands)
        {
            var text = new StringBuilder();
            text.AppendLine("## docs/63: the weapon roster in the simulation");
            text.AppendLine();
            text.AppendLine($"Seeds {first}-{first + count - 1} (exploration; docs/56 rule 1), both heroes, {hands} hands, the whole climb (floors 1-3 and the finale). " +
                            "Every option plays the same seeds as `shipped`. Completion: the finale walked out of.");
            text.AppendLine();
            text.AppendLine("| Option | What |");
            text.AppendLine("|---|---|");
            foreach (Option option in options)
                text.AppendLine($"| {option.Key} | {option.What} |");
            text.AppendLine();

            text.AppendLine("### Completion by cohort (both heroes), and the change against `shipped` in points");
            text.AppendLine();
            text.AppendLine("| Option | " + string.Join(" | ", cohorts) + " |");
            text.AppendLine("|---|" + string.Concat(cohorts.Select(_ => "---|")));
            foreach (Option option in options)
            {
                var cells = new List<string>();
                foreach (string cohort in cohorts)
                {
                    RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Cohort == cohort).ToArray();
                    RunRead[] shipped = reads.Where(r => r.Option == "shipped" && r.Cohort == cohort).ToArray();
                    double rate = mine.Length == 0 ? double.NaN : 100.0 * mine.Count(r => r.Completed) / mine.Length;
                    string change = option.Key == "shipped" || shipped.Length != mine.Length || mine.Length == 0 ? string.Empty
                        : " (" + Pts(rate - 100.0 * shipped.Count(r => r.Completed) / shipped.Length) + ")";
                    cells.Add(mine.Length == 0 ? "-" : rate.ToString("0.0", CultureInfo.InvariantCulture) + " %" + change);
                }
                text.AppendLine($"| {option.Key} | {string.Join(" | ", cells)} |");
            }
            text.AppendLine();

            text.AppendLine("### By hero: completion, floors cleared (mean), weapons held after floor 1 and at the end (mean)");
            text.AppendLine();
            text.AppendLine("| Option | Hero | Cohort | Completed | Change (pts) | Floors cleared | Weapons after floor 1 | Weapons at the end |");
            text.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (Option option in options)
            foreach (string hero in new[] { "Vanguard", "Ilya" })
            foreach (string cohort in cohorts)
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Hero == hero && r.Cohort == cohort).ToArray();
                RunRead[] shipped = reads.Where(r => r.Option == "shipped" && r.Hero == hero && r.Cohort == cohort).ToArray();
                if (mine.Length == 0)
                    continue;
                double rate = 100.0 * mine.Count(r => r.Completed) / mine.Length;
                string change = option.Key == "shipped" || shipped.Length != mine.Length ? "-"
                    : Pts(rate - 100.0 * shipped.Count(r => r.Completed) / shipped.Length);
                text.AppendLine($"| {option.Key} | {hero} | {cohort} | {Pct(mine.Count(r => r.Completed), mine.Length)} | {change} | " +
                                $"{mine.Average(r => r.FloorsCleared).ToString("0.00", CultureInfo.InvariantCulture)} | " +
                                $"{mine.Average(r => r.WeaponsAfterFloorOne).ToString("0.00", CultureInfo.InvariantCulture)} | " +
                                $"{mine.Average(r => r.WeaponsAtEnd).ToString("0.00", CultureInfo.InvariantCulture)} |");
            }
            text.AppendLine();

            text.AppendLine("### Each weapon: pick rate and damage share");
            text.AppendLine();
            text.AppendLine("Held: runs that held it at any point (its own hero always). Offered: runs whose offers showed it as a new weapon at least once; taken: of those, the runs that took it. " +
                            "Share among holders: its damage over all its holders' weapon damage, summed over those runs. Share overall: its damage over all weapon damage. Kills: its share of the weapons' kills. " +
                            "An even share is a third once three are held.");
            text.AppendLine();
            text.AppendLine("| Option | Hero | Weapon | Held | Offered | Taken when offered | First floor held (mean) | Share among holders | Share overall | Kills share |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (Option option in options)
            foreach (string hero in new[] { "Vanguard", "Ilya" })
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Hero == hero).ToArray();
                if (mine.Length == 0)
                    continue;
                double all = mine.Sum(r => (double)r.Dealt.Sum());
                double kills = mine.Sum(r => (double)r.Kills.Sum());
                for (int w = 0; w < WeaponIds.Length; w++)
                {
                    RunRead[] holders = mine.Where(r => r.Held[w]).ToArray();
                    RunRead[] offered = mine.Where(r => r.Offered[w] > 0).ToArray();
                    if (holders.Length == 0 && offered.Length == 0)
                        continue;
                    double holdersAll = holders.Sum(r => (double)r.Dealt.Sum());
                    text.AppendLine($"| {option.Key} | {hero} | {Short[w]} | {Pct(holders.Length, mine.Length)} | {Pct(offered.Length, mine.Length)} | " +
                                    $"{Pct(offered.Count(r => r.Held[w]), offered.Length)} | " +
                                    $"{(holders.Length == 0 ? "-" : holders.Average(r => r.FirstFloor[w]).ToString("0.00", CultureInfo.InvariantCulture))} | " +
                                    $"{Pct(holders.Sum(r => (double)r.Dealt[w]), holdersAll)} | {Pct(mine.Sum(r => (double)r.Dealt[w]), all)} | " +
                                    $"{Pct(mine.Sum(r => (double)r.Kills[w]), kills)} |");
                }
            }
            text.AppendLine();

            // docs/63 phase 2's guard (section 13): Ilya's own Staff must stay her main damage, so a shared weapon that
            // out-damages it is read here, overall and in the runs that hold that weapon.
            text.AppendLine("### Each hero's own weapon: its share of the hero's weapon damage, and the runs it led");
            text.AppendLine();
            text.AppendLine("Own share: the signature's damage over all the hero's weapon damage (all cohorts but the auto-farmer). Top other: the shared weapon with the largest share overall. " +
                            "Led: the runs in which the signature dealt more than any other weapon. Against each shared weapon, in the runs that hold it: the signature's share and that weapon's.");
            text.AppendLine();
            text.AppendLine("| Option | Hero | Own share | Top other (share) | Led | " + string.Join(" | ", Short.Skip(2).Select(s => $"With {s}: own / {s}")) + " |");
            text.AppendLine("|---|---|---|---|---|" + string.Concat(Short.Skip(2).Select(_ => "---|")));
            foreach (Option option in options)
            foreach ((string hero, int own) in new[] { ("Vanguard", 0), ("Ilya", 1) })
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Hero == hero && r.Cohort != "auto-farmer").ToArray();
                if (mine.Length == 0)
                    continue;
                double all = mine.Sum(r => (double)r.Dealt.Sum());
                int top = -1;
                double topShare = -1;
                for (int w = 0; w < WeaponIds.Length; w++)
                {
                    if (w == own)
                        continue;
                    double share = mine.Sum(r => (double)r.Dealt[w]);
                    if (share > topShare)
                    {
                        topShare = share;
                        top = w;
                    }
                }
                int led = mine.Count(r => Enumerable.Range(0, WeaponIds.Length).All(w => w == own || r.Dealt[own] > r.Dealt[w]));
                var cells = new List<string>();
                for (int w = 2; w < WeaponIds.Length; w++)
                {
                    RunRead[] holders = mine.Where(r => r.Held[w]).ToArray();
                    double holdersAll = holders.Sum(r => (double)r.Dealt.Sum());
                    cells.Add(holders.Length == 0 || w == own ? "-"
                        : $"{Pct(holders.Sum(r => (double)r.Dealt[own]), holdersAll)} / {Pct(holders.Sum(r => (double)r.Dealt[w]), holdersAll)}");
                }
                text.AppendLine($"| {option.Key} | {hero} | {Pct(mine.Sum(r => (double)r.Dealt[own]), all)} | {Short[top]} ({Pct(topShare, all)}) | " +
                                $"{Pct(led, mine.Length)} | {string.Join(" | ", cells)} |");
            }
            text.AppendLine();

            // The failure phase 1 found (section 9.2): a hero that never holds a crowd weapon - the Staff, the Mortar or the
            // Saw - almost never finishes. Both candidates of phase 2 aim at that share of runs.
            int[] crowd = { Array.IndexOf(Short, "staff"), Array.IndexOf(Short, "mortar"), Array.IndexOf(Short, "saw") };
            text.AppendLine("### Runs that held a crowd weapon (the Staff, the Mortar or the Saw) and runs that never did, by cohort");
            text.AppendLine();
            text.AppendLine("| Option | Hero | Cohort | Held one | Held one: completed | Never: completed | First held on floor (mean) |");
            text.AppendLine("|---|---|---|---|---|---|---|");
            foreach (Option option in options)
            foreach (string hero in new[] { "Vanguard", "Ilya" })
            foreach (string cohort in cohorts)
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Hero == hero && r.Cohort == cohort).ToArray();
                if (mine.Length == 0)
                    continue;
                RunRead[] held = mine.Where(r => crowd.Any(w => r.Held[w])).ToArray();
                RunRead[] never = mine.Where(r => !crowd.Any(w => r.Held[w])).ToArray();
                string firstHeld = held.Length == 0 ? "-"
                    : held.Average(r => crowd.Where(w => r.Held[w]).Min(w => r.FirstFloor[w])).ToString("0.00", CultureInfo.InvariantCulture);
                text.AppendLine($"| {option.Key} | {hero} | {cohort} | {Pct(held.Length, mine.Length)} | {Pct(held.Count(r => r.Completed), held.Length)} | " +
                                $"{Pct(never.Count(r => r.Completed), never.Length)} of {never.Length} | {firstHeld} |");
            }
            text.AppendLine();

            text.AppendLine("### Completion with and without each shared weapon (all cohorts pooled)");
            text.AppendLine();
            text.AppendLine("A weapon that dominates shows as a large gap: runs that held it completing far more often than the runs offered it that did not take it. Not causal (a run that lives longer is offered more); read beside the share.");
            text.AppendLine();
            text.AppendLine("| Option | Hero | Weapon | Held: completed | Offered, not taken: completed |");
            text.AppendLine("|---|---|---|---|---|");
            foreach (Option option in options.Where(o => o.Key != "shipped" && o.Key != "aa-f1"))
            foreach (string hero in new[] { "Vanguard", "Ilya" })
            {
                RunRead[] mine = reads.Where(r => r.Option == option.Key && r.Hero == hero).ToArray();
                for (int w = 2; w < WeaponIds.Length; w++)
                {
                    RunRead[] held = mine.Where(r => r.Held[w]).ToArray();
                    RunRead[] passed = mine.Where(r => r.Offered[w] > 0 && !r.Held[w]).ToArray();
                    if (held.Length == 0 && passed.Length == 0)
                        continue;
                    text.AppendLine($"| {option.Key} | {hero} | {Short[w]} | {Pct(held.Count(r => r.Completed), held.Length)} of {held.Length} | " +
                                    $"{Pct(passed.Count(r => r.Completed), passed.Length)} of {passed.Length} |");
                }
            }
            text.AppendLine();
            return text.ToString();
        }

        private static string Csv(List<RunRead> reads, string hands)
        {
            var csv = new StringBuilder();
            csv.Append("realism,option,cohort,hero,seed,f1_outcome,f2_outcome,f3_outcome,finale_outcome,floors_cleared,died_on,climb_s,weapons_f1,weapons_end");
            foreach (string s in Short)
                csv.Append($",{s}_held,{s}_first_floor,{s}_offered,{s}_dealt,{s}_kills");
            csv.AppendLine();
            foreach (RunRead r in reads)
            {
                csv.Append(string.Join(",", hands, r.Option, r.Cohort, r.Hero, r.Seed.ToString(CultureInfo.InvariantCulture),
                    r.Outcomes[0] ?? string.Empty, r.Outcomes[1] ?? string.Empty, r.Outcomes[2] ?? string.Empty, r.Outcomes[3] ?? string.Empty,
                    r.FloorsCleared.ToString(CultureInfo.InvariantCulture), r.DiedOn.ToString(CultureInfo.InvariantCulture),
                    r.Seconds.ToString("0.0", CultureInfo.InvariantCulture), r.WeaponsAfterFloorOne.ToString(CultureInfo.InvariantCulture),
                    r.WeaponsAtEnd.ToString(CultureInfo.InvariantCulture)));
                for (int w = 0; w < WeaponIds.Length; w++)
                    csv.Append($",{(r.Held[w] ? 1 : 0)},{r.FirstFloor[w]},{r.Offered[w]},{r.Dealt[w].ToString("0", CultureInfo.InvariantCulture)},{r.Kills[w]}");
                csv.AppendLine();
            }
            return csv.ToString();
        }

        private static string[] List(string text) =>
            text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

        private static int Setting(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) && value > 0 ? value : fallback;

        private static string RepositoryRoot()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            return directory ?? TestContext.CurrentContext.TestDirectory;
        }
    }
}
