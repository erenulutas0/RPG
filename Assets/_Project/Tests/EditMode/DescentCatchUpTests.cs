using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // A run started on a deeper map (DescentStart, MapChoiceView): the levels it starts at are the Descent simulation's
    // arrival levels (docs/53), each of them a level-up left to choose, offered by the run's own seeded streams; floor 1
    // starts a fresh hero exactly as before.
    public sealed class DescentCatchUpTests
    {
        [Test]
        public void TheCatchUpLevelsAreTheSimulationsArrivalLevels()
        {
            Assert.That(DescentStart.CatchUpLevel(1), Is.EqualTo(0), "Floor 1 starts a fresh hero.");
            Assert.That(DescentStart.CatchUpLevel(2), Is.EqualTo(31), "The careful cohort's P50 going down to floor 2.");
            Assert.That(DescentStart.CatchUpLevel(3), Is.EqualTo(44), "The careful cohort's P50 going down to floor 3.");
            Assert.That(DescentStart.CatchUpLevel(0), Is.EqualTo(0));
            Assert.That(DescentStart.CatchUpLevel(4), Is.EqualTo(0), "The finale is never a start.");
            Assert.That(DescentStart.CatchUpLevel(-1), Is.EqualTo(0));
        }

        [Test]
        public void OnlyTheThreeMapsAreStarts()
        {
            Assert.That(DescentStart.IsStart(0), Is.False);
            Assert.That(DescentStart.IsStart(1), Is.True);
            Assert.That(DescentStart.IsStart(2), Is.True);
            Assert.That(DescentStart.IsStart(3), Is.True);
            Assert.That(DescentStart.IsStart(4), Is.False);
        }

        // The economy's own curve (PrototypeEconomy: 10 a level, 2 more each level) and a linear one: the run stands at the
        // level exactly, at its threshold, with every level-up of it pending.
        [TestCase(2, 10, 2)]
        [TestCase(3, 10, 2)]
        [TestCase(2, 7, 0)]
        [TestCase(3, 25, 5)]
        public void ACatchUpRaisesAFreshRunToTheLevelWithEveryLevelUpToChoose(int floor, int perLevel, int growth)
        {
            var run = new RunState(perLevel, 0.5f, growth, 1234);
            int pendingChanges = 0, levelChanges = 0;
            run.PendingUpgradesChanged += () => pendingChanges++;
            run.LevelChanged += () => levelChanges++;
            int level = DescentStart.CatchUpLevel(floor);

            Assert.That(DescentStart.CatchUp(run, floor), Is.EqualTo(level));
            Assert.That(run.Level, Is.EqualTo(level));
            Assert.That(run.PendingUpgrades, Is.EqualTo(level), "Each level a level-up left to choose.");
            Assert.That(run.Experience, Is.EqualTo(run.ExperienceToReach(level)), "Exactly the level's threshold, none of the next.");
            Assert.That(run.Experience, Is.LessThan(run.ExperienceToReach(level + 1)));
            Assert.That(run.ExperienceForCurrentLevel, Is.EqualTo(run.Experience));
            Assert.That((pendingChanges, levelChanges), Is.EqualTo((1, 1)), "One change, as one large reward makes.");
            Assert.That((run.Gold, run.SecuredGold), Is.EqualTo((0, 0)), "No gold comes with it: run gold only banks into the forge.");
            Assert.That(DescentStart.CatchUp(run, floor), Is.EqualTo(0), "Asked again, nothing more.");
        }

        [Test]
        public void TheFoundryAndNonStartsLeaveTheRunAsItWas()
        {
            foreach (int floor in new[] { 1, 0, 4, -3 })
            {
                var run = new RunState(10, 0.5f, 2);
                bool changed = false;
                run.ExperienceChanged += () => changed = true;
                Assert.That(DescentStart.CatchUp(run, floor), Is.EqualTo(0), "floor " + floor);
                Assert.That((run.Level, run.Experience, run.PendingUpgrades, changed), Is.EqualTo((0, 0, 0, false)), "floor " + floor);
            }
        }

        [Test]
        public void AnEndedRunOrOneAlreadyThatHighGainsNothing()
        {
            var ended = new RunState(10, 0.5f, 2);
            ended.End(RunOutcome.Defeat);
            Assert.That(DescentStart.CatchUp(ended, 3), Is.EqualTo(0));
            Assert.That(ended.Level, Is.EqualTo(0));

            var high = new RunState(10, 0.5f, 2);
            high.AddExperience(high.ExperienceToReach(50));
            Assert.That(DescentStart.CatchUp(high, 3), Is.EqualTo(0));
            Assert.That(high.Level, Is.EqualTo(50));

            var partway = new RunState(10, 0.5f, 2);
            partway.AddExperience(partway.ExperienceToReach(5) + 3);
            Assert.That(DescentStart.CatchUp(partway, 2), Is.EqualTo(26), "From level 5 to 31.");
            Assert.That(partway.Level, Is.EqualTo(31));
        }

        [Test]
        public void ExperienceToReachIsTheLevelThreshold()
        {
            var run = new RunState(10, 0.5f, 2);
            Assert.That(run.ExperienceToReach(0), Is.EqualTo(0));
            Assert.That(run.ExperienceToReach(-2), Is.EqualTo(0));
            Assert.That(run.ExperienceToReach(1), Is.EqualTo(10));
            Assert.That(run.ExperienceToReach(2), Is.EqualTo(22), "10 + 12");
            Assert.That(run.ExperienceToReach(3), Is.EqualTo(36), "10 + 12 + 14");
            Assert.That(run.ExperienceForNextLevel, Is.EqualTo(run.ExperienceToReach(1)));
        }

        // The catch-up's level-ups are the run's ordinary offers: the same seed offers the same cards, one after another,
        // until every level-up is chosen or no card is left; then nothing is open.
        [Test]
        public void TheCatchUpsOffersAreSeededAndComeOneAfterAnother()
        {
            List<string> first = PlayCatchUp(77, out int picksA, out int pendingA);
            List<string> again = PlayCatchUp(77, out int picksB, out _);
            List<string> other = PlayCatchUp(78, out _, out _);
            Assert.That(again, Is.EqualTo(first), "The same seed, the same offers.");
            Assert.That(other, Is.Not.EqualTo(first), "Another seed, other offers.");
            Assert.That(picksA, Is.EqualTo(picksB));
            Assert.That(picksA, Is.GreaterThan(0));
            Assert.That(picksA + pendingA, Is.EqualTo(DescentStart.CatchUpLevel(2)),
                "Every level-up is chosen, or left over once no card remains.");
        }

        private static List<string> PlayCatchUp(int seed, out int picks, out int pending)
        {
            var run = new RunState(10, 0.5f, 2, seed);
            var options = new List<UpgradeOption>();
            for (int i = 0; i < 6; i++)
                options.Add(new UpgradeOption("upgrade_" + i, "Card " + i, "+{0:0}", UpgradeStat.Damage,
                    new StatModifier(ModifierOperation.Flat, 1f), 4));
            var upgrades = new UpgradeService(run, new WeaponRuntime(10f, 1f, 1f), options, 3);
            var shown = new List<string>();
            picks = 0;
            Assert.That(upgrades.CurrentOffer, Is.Null, "Nothing is offered before the catch-up.");
            DescentStart.CatchUp(run, 2);
            while (upgrades.CurrentOffer != null && picks < 100)
            {
                UpgradeOffer offer = upgrades.CurrentOffer;
                for (int i = 0; i < offer.Choices.Count; i++)
                    shown.Add(offer.Choices[i].Id);
                shown.Add("|");
                Assert.That(upgrades.TrySelect(offer, 0), Is.True);
                picks++;
            }
            pending = run.PendingUpgrades;
            return shown;
        }
    }
}
