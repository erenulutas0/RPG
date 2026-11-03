using System;
using System.Collections.Generic;
using System.Linq;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Cryptforge.Tests
{
    // The autopilot's cohorts on the foundry floor (AutopilotCohort, as DescentSimulation.FoundryLevers.Cohort): with no
    // lever the careful pilot plays, and each cohort changes what it says it changes - on the same seeds, as docs/40
    // section 9.4 pairs its bots - and the batch report's cohort section (FoundryCohorts) has a row for each. Balance is
    // not pinned here: how often a cohort clears is the batch report's (Tools/CombatChecks/FoundryBatchReport.cs,
    // FOUNDRY_COHORTS=1).
    public sealed class FoundryCohortTests
    {
        // Two of the Editor measurement's seeds, as FoundrySimulationTests reads them; the cohorts' batches play the first.
        private static readonly int[] Seeds = { 11, 33 };
        private List<(AutopilotCohort Cohort, FoundryBatch.Batch[] Batches)> _cohorts;

        [OneTimeSetUp]
        public void RunTheCohorts() => _cohorts = FoundryCohorts.Run(Heroes(), new[] { Seeds[0] }, 2);

        private static DescentSimulation.FoundryHero[] Heroes() =>
            new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

        // The cohort's run of the first seed for the hero at index h of Heroes().
        private DescentSimulation.FoundryResult Played(AutopilotCohort cohort, int h) =>
            _cohorts.First(c => c.Cohort == cohort).Batches[h].Runs[0];

        private static DescentSimulation.FoundryResult Run(DescentSimulation.FoundryHero hero, int seed,
            AutopilotCohort cohort, Action<string> trace = null) =>
            DescentSimulation.RunFoundry(hero, seed, FoundryCohorts.Levers(cohort), trace: trace);

        private static AutopilotCohort CarefulLess(AutopilotCohort.Habit habit) =>
            new AutopilotCohort("careful less " + habit, AutopilotCohort.Habit.All & ~habit);

        // With no lever the careful pilot plays; a careful cohort built anew - the pilot reads the habits, not the name -
        // plays the very same floor. When the cohorts came in, the default played seeds 1-200 of both heroes exactly as
        // the careful pilot of 7946396 had, every column of the batch's CSV alike.
        [Test]
        public void WithNoLeverTheCarefulPilotPlays()
        {
            Assert.That(new DescentSimulation.FoundryLevers().Cohort, Is.SameAs(AutopilotCohort.Careful));
            Assert.That(FoundryBatch.Shipped().Cohort, Is.SameAs(AutopilotCohort.Careful));
            var anew = new AutopilotCohort("careful, anew", AutopilotCohort.Habit.All);
            DescentSimulation.FoundryHero[] heroes = Heroes();
            for (int h = 0; h < heroes.Length; h++)
            {
                DescentSimulation.FoundryResult plain = DescentSimulation.RunFoundry(heroes[h], Seeds[0]);
                foreach (DescentSimulation.FoundryResult careful in new[] { Played(AutopilotCohort.Careful, h), Run(heroes[h], Seeds[0], anew) })
                {
                    string where = $"{heroes[h].Name}, {careful.Levers}";
                    Assert.That(careful.Outcome, Is.EqualTo(plain.Outcome), where);
                    Assert.That(careful.FloorSeconds, Is.EqualTo(plain.FloorSeconds), where);
                    Assert.That(careful.RoomSeconds, Is.EqualTo(plain.RoomSeconds), where);
                    Assert.That(careful.Picks, Is.EqualTo(plain.Picks), where);
                    Assert.That(careful.HordeKills, Is.EqualTo(plain.HordeKills), where);
                    Assert.That(careful.WaveKills, Is.EqualTo(plain.WaveKills), where);
                    Assert.That(careful.HeroHealth, Is.EqualTo(plain.HeroHealth), where);
                    Assert.That(careful.BossSeconds, Is.EqualTo(plain.BossSeconds), where);
                    Assert.That(careful.DropsCollected, Is.EqualTo(plain.DropsCollected), where);
                    Assert.That((careful.KiteSeconds, careful.EscapeSeconds, careful.ShelterSeconds, careful.LureSeconds),
                        Is.EqualTo((plain.KiteSeconds, plain.EscapeSeconds, plain.ShelterSeconds, plain.LureSeconds)), where);
                    Assert.That((careful.AbilityUses, careful.SlamHits, careful.EruptionsOnHero),
                        Is.EqualTo((plain.AbilityUses, plain.SlamHits, plain.EruptionsOnHero)), where);
                }
            }
        }

        // docs/41 step 5's standing policy: not a step of kiting, and every other habit kept - on this seed it leaves each
        // of the Warden's marks (no slam lands), shelters from the spills and is burnt by none, draws the node fights to
        // the rim, fires its ability and fills its weapon slots.
        [Test]
        public void TheStandingCohortNeverKitesAndKeepsEveryOtherHabit()
        {
            DescentSimulation.FoundryHero[] heroes = Heroes();
            for (int h = 0; h < heroes.Length; h++)
            {
                DescentSimulation.FoundryResult careful = Played(AutopilotCohort.Careful, h);
                DescentSimulation.FoundryResult standing = Played(AutopilotCohort.Standing, h);
                string where = heroes[h].Name;
                Assert.That(careful.KiteSeconds, Is.GreaterThan(0f), where + ": the careful pilot kites");
                Assert.That(standing.KiteSeconds, Is.Zero, where);
                Assert.That(standing.LevelAtGate, Is.GreaterThan(0), where + ": it met the Warden");
                Assert.That(standing.EscapeSeconds, Is.GreaterThan(0f), where + ": it leaves the marks");
                Assert.That(standing.SlamHits, Is.Zero, where);
                Assert.That(standing.ShelterSeconds, Is.GreaterThan(0f), where + ": it shelters from the spills");
                Assert.That(standing.EruptionsOnHero, Is.LessThanOrEqualTo(1), where);
                Assert.That(standing.LureSeconds, Is.GreaterThan(0f), where);
                Assert.That(standing.AbilityUses, Is.GreaterThan(0), where);
                Assert.That(standing.WeaponsAtGate, Is.EqualTo(WeaponLoadout.MaxSlots), where);
            }
        }

        // docs/41 step 5's first-card picker: every offer's first card; and until an offer whose best scored card was not
        // its first, the careful pilot's very floor - the two traces part at a pick, alike frame for frame before it.
        [Test]
        public void TheFirstCardCohortTakesEachFirstCardAndPartsFromTheCarefulPilotOnlyAtAPick()
        {
            foreach (DescentSimulation.FoundryHero hero in Heroes())
            {
                var careful = new List<string>();
                DescentSimulation.RunFoundry(hero, Seeds[0], trace: careful.Add);
                var first = new List<string>();
                Run(hero, Seeds[0], AutopilotCohort.FirstCard, first.Add);
                AssertEveryPickIsTheFirstCard(first, hero.Name);
                int part = 0;
                while (part < careful.Count && part < first.Count && careful[part] == first[part])
                    part++;
                Assert.That(part, Is.LessThan(Math.Min(careful.Count, first.Count)), hero.Name + ": the two floors part");
                Assert.That(Event(first[part]), Is.EqualTo("upgrade_selected"), $"{hero.Name}: they part at '{first[part]}'");
                Assert.That(Event(careful[part]), Is.EqualTo("upgrade_selected"), $"{hero.Name}: they part at '{careful[part]}'");
            }
        }

        // The pilot before 2026-11-04's fixes: on these seeds the spills burn it again - the careful pilot at most once
        // (FoundrySimulationTests.TheAutopilotWaitsOutThePitsGlowingBand) - and it walks out to the Warden with two weapons,
        // the careful pilot with three. Each of those two fixes undone alone does the same; the three footwork fixes' two
        // ways are AutopilotCohortTests'. When the cohorts came in, the clumsy cohort played seeds 1-200 of both heroes
        // exactly as main d6664d2's pilot had, every column of the batch's CSV alike.
        [Test]
        public void TheClumsyCohortIsBurntByTheSpillsAndNeverTakesAThirdWeapon()
        {
            AutopilotCohort glow = CarefulLess(AutopilotCohort.Habit.KeepsOutOfTheGlow);
            AutopilotCohort weapons = CarefulLess(AutopilotCohort.Habit.FillsWeaponSlots);
            DescentSimulation.FoundryHero[] heroes = Heroes();
            for (int h = 0; h < heroes.Length; h++)
            foreach (int seed in Seeds)
            {
                string where = $"{heroes[h].Name} seed {seed}";
                DescentSimulation.FoundryResult clumsy = seed == Seeds[0]
                    ? Played(AutopilotCohort.Clumsy, h)
                    : Run(heroes[h], seed, AutopilotCohort.Clumsy);
                Assert.That(clumsy.EruptionsOnHero, Is.GreaterThan(1), where + ": " + clumsy.Eruptions + " spills");
                Assert.That(clumsy.WeaponsAtGate, Is.EqualTo(2), where + ": " + clumsy.Weapons);
                DescentSimulation.FoundryResult glowing = Run(heroes[h], seed, glow);
                Assert.That(glowing.EruptionsOnHero, Is.GreaterThan(1), where + ", " + glow.Name);
                DescentSimulation.FoundryResult two = Run(heroes[h], seed, weapons);
                Assert.That(two.WeaponsAtGate, Is.EqualTo(2), where + ", " + weapons.Name + ": " + two.Weapons);
            }
        }

        // docs/40 9.4's Auto Farmer walks the floor - to the halls, the altars and the drops beside its way - and does
        // nothing else: not a step of kiting, leaving a mark, sheltering or luring, never its ability, every offer's first
        // card. So each of the Warden's slams it meets lands.
        [Test]
        public void TheAutoFarmerWalksTheFloorAndDoesNothingElse()
        {
            DescentSimulation.FoundryHero[] heroes = Heroes();
            for (int h = 0; h < heroes.Length; h++)
            {
                var trace = new List<string>();
                DescentSimulation.FoundryResult farmer = Run(heroes[h], Seeds[0], AutopilotCohort.AutoFarmer, trace.Add);
                string where = heroes[h].Name;
                Assert.That(farmer.FloorSeconds, Is.EqualTo(Played(AutopilotCohort.AutoFarmer, h).FloorSeconds), where + ": traced or not");
                Assert.That((farmer.KiteSeconds, farmer.EscapeSeconds, farmer.ShelterSeconds, farmer.LureSeconds),
                    Is.EqualTo((0f, 0f, 0f, 0f)), where);
                Assert.That(farmer.AbilityUses, Is.Zero, where);
                AssertEveryPickIsTheFirstCard(trace, where);
                Assert.That(farmer.WalkSeconds[1], Is.GreaterThan(0f), where + ": it walks to the halls");
                Assert.That(farmer.AltarsTaken, Is.GreaterThan(0), where);
                Assert.That(farmer.DropsCollected, Is.GreaterThan(0), where);
                Assert.That(farmer.Slams, Is.GreaterThan(0), where + ": it met the Warden");
                Assert.That(farmer.SlamHits, Is.EqualTo(farmer.Slams), where);
            }
        }

        // The batch report's section: every cohort in AutopilotCohort's order, each with a batch a hero on the same seeds,
        // and a row a cohort and hero in its table, paired with the careful pilot's seed for seed.
        [Test]
        public void TheReportsSectionHasARowForEachCohortAndHero()
        {
            Assert.That(_cohorts.Select(c => c.Cohort), Is.EqualTo(AutopilotCohort.Cohorts));
            foreach ((AutopilotCohort cohort, FoundryBatch.Batch[] batches) in _cohorts)
            {
                Assert.That(batches.Select(b => b.Hero.Id), Is.EqualTo(Heroes().Select(hero => hero.Id)), cohort.Name);
                Assert.That(batches.All(b => b.Levers.Cohort == cohort && b.Runs.Select(r => r.Seed).SequenceEqual(new[] { Seeds[0] })),
                    Is.True, cohort.Name);
            }

            string section = FoundryCohorts.Section(_cohorts);
            Assert.That(section, Does.StartWith("## The autopilot's cohorts"));
            string[] rows = section.Split('\n').Where(line => line.StartsWith("| ", StringComparison.Ordinal)).ToArray();
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
            foreach (DescentSimulation.FoundryHero hero in Heroes())
            {
                string[] cells = rows.Select(line => line.Split('|').Select(c => c.Trim()).ToArray())
                    .Where(c => c.Length > 3 && c[1] == cohort.Name && c[2] == hero.Name).ToArray()[0];
                Assert.That(cells.Length, Is.EqualTo(13), $"{cohort.Name} {hero.Name}: eleven columns");
                Assert.That(cells[3], Does.Match(@"^[01] \(\d+%\)$"), $"{cohort.Name} {hero.Name}: cleared");
                IResolveConstraint paired = cohort == AutopilotCohort.Careful
                    ? (IResolveConstraint)Is.EqualTo("-")
                    : Does.Match(@"^\d+ / \d+ / \d+$");
                Assert.That(cells[11], paired, $"{cohort.Name} {hero.Name}: paired with careful");
            }
        }

        // The trace's event name: "12.345 upgrade_selected ..." is upgrade_selected.
        private static string Event(string line)
        {
            string[] words = line.Split(' ');
            return words.Length > 1 ? words[1] : string.Empty;
        }

        // Every upgrade_selected names the first card of the upgrade_offered before it.
        private static void AssertEveryPickIsTheFirstCard(List<string> trace, string where)
        {
            string firstOffered = null;
            int picks = 0;
            foreach (string line in trace)
            {
                string[] words = line.Split(' ');
                if (words.Length < 3)
                    continue;
                if (words[1] == "upgrade_offered")
                    firstOffered = words[2].Split(',')[0];
                else if (words[1] == "upgrade_selected")
                {
                    Assert.That(words[2], Is.EqualTo(firstOffered), where + ": " + line);
                    picks++;
                }
            }
            Assert.That(picks, Is.GreaterThan(0), where);
        }
    }
}
