using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // A swarm room holds level-ups back and offers them together (docs/27 decision 7). The hold decides only when an
    // offer opens: the cards, the seed's draws and what a choice applies are the same as a level-up offered at once.
    public sealed class UpgradeSittingTests
    {
        private static (RunState Run, UpgradeService Upgrades) Make()
        {
            var run = new RunState(1, 0.5f, 0, 7);
            var weapon = new WeaponRuntime(10f, 1f, 1.5f);
            IReadOnlyList<UpgradeOption> pool = DescentSimulation.Pool();
            return (run, new UpgradeService(run, weapon, pool, 2));
        }

        [Test]
        public void AHeldLevelUpWaitsAndASittingOffersEveryOneInARow()
        {
            (RunState run, UpgradeService upgrades) = Make();
            upgrades.Hold();
            run.AddExperience(1);
            run.AddExperience(2);
            run.AddExperience(3);
            Assert.That(run.PendingUpgrades, Is.GreaterThanOrEqualTo(2), "Several level-ups are waiting.");
            Assert.That(upgrades.CurrentOffer, Is.Null, "While held, a level-up opens nothing.");

            int pending = run.PendingUpgrades;
            upgrades.OpenSitting();
            int answered = 0;
            while (upgrades.CurrentOffer != null)
            {
                Assert.That(upgrades.TrySelect(upgrades.CurrentOffer, 0), Is.True);
                answered++;
            }
            Assert.That(answered, Is.EqualTo(pending), "One sitting answers every level-up it kept.");
            Assert.That(run.PendingUpgrades, Is.Zero);

            run.AddExperience(10);
            Assert.That(run.PendingUpgrades, Is.GreaterThan(0));
            Assert.That(upgrades.CurrentOffer, Is.Null, "After the sitting the hold is back.");
            Assert.That(upgrades.IsHeld, Is.True);
        }

        [Test]
        public void ReleasingOffersWhatTheHoldKeptAndThenEachLevelUpAsItComes()
        {
            (RunState run, UpgradeService upgrades) = Make();
            upgrades.Hold();
            run.AddExperience(1);
            Assert.That(upgrades.CurrentOffer, Is.Null);

            upgrades.Release();
            Assert.That(upgrades.CurrentOffer, Is.Not.Null, "Released, the kept level-up is offered at once.");
            Assert.That(upgrades.TrySelect(upgrades.CurrentOffer, 0), Is.True);
            run.AddExperience(10);
            Assert.That(upgrades.CurrentOffer, Is.Not.Null, "And the next is offered as it comes.");
        }

        [Test]
        public void ASittingWithNothingWaitingOpensNothingAndAnUnheldServiceIgnoresIt()
        {
            (RunState run, UpgradeService upgrades) = Make();
            upgrades.OpenSitting();
            Assert.That(upgrades.CurrentOffer, Is.Null);

            upgrades.Hold();
            upgrades.OpenSitting();
            Assert.That(upgrades.CurrentOffer, Is.Null, "Nothing waiting, nothing offered.");
            run.AddExperience(1);
            Assert.That(upgrades.CurrentOffer, Is.Null, "And the empty sitting did not leave the door open.");
        }

        [Test]
        public void TheCardsASittingShowsAreTheOnesTheSeedWouldHaveShownAnyway()
        {
            (RunState heldRun, UpgradeService held) = Make();
            (RunState plainRun, UpgradeService plain) = Make();
            held.Hold();
            heldRun.AddExperience(1);
            plainRun.AddExperience(1);
            held.OpenSitting();
            Assert.That(Ids(held.CurrentOffer), Is.EqualTo(Ids(plain.CurrentOffer)),
                "Offers are drawn from the seed and the offer's index, not from when it opened.");
        }

        private static List<string> Ids(UpgradeOffer offer)
        {
            var ids = new List<string>();
            foreach (UpgradeOption option in offer.Choices)
                ids.Add(option.Id);
            return ids;
        }
    }
}
