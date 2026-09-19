using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class UpgradeTests
    {
        private static UpgradeOption DamageOption(int maxStacks = 5) =>
            new UpgradeOption("upgrade_damage", "Damage", "+{0:0} damage", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Flat, 5f), maxStacks);

        private static UpgradeOption SpeedOption(int maxStacks = 5) =>
            new UpgradeOption("upgrade_attack_speed", "Speed", "+{0:0}% attack speed", UpgradeStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.25f), maxStacks);

        [Test]
        public void FlatModifiersApplyBeforePercentRegardlessOfInsertionOrder()
        {
            var percentFirst = new ModifiableStat(10f, 0f);
            percentFirst.AddModifier(new StatModifier(ModifierOperation.Percent, 1f));
            percentFirst.AddModifier(new StatModifier(ModifierOperation.Flat, 5f));

            var flatFirst = new ModifiableStat(10f, 0f);
            flatFirst.AddModifier(new StatModifier(ModifierOperation.Flat, 5f));
            flatFirst.AddModifier(new StatModifier(ModifierOperation.Percent, 1f));

            Assert.That(percentFirst.Value, Is.EqualTo(30f));
            Assert.That(flatFirst.Value, Is.EqualTo(30f));
            Assert.That(flatFirst.Base, Is.EqualTo(10f));
        }

        [Test]
        public void PercentModifiersSumInsteadOfCompounding()
        {
            var stat = new ModifiableStat(1f, 0.1f);
            stat.AddModifier(new StatModifier(ModifierOperation.Percent, 0.25f));
            stat.AddModifier(new StatModifier(ModifierOperation.Percent, 0.25f));
            Assert.That(stat.Value, Is.EqualTo(1.5f).Within(1e-5f));
        }

        [Test]
        public void NegativeModifiersClampToMinimum()
        {
            var stat = new ModifiableStat(10f, 0f);
            stat.AddModifier(new StatModifier(ModifierOperation.Flat, -50f));
            Assert.That(stat.Value, Is.Zero);

            var speed = new ModifiableStat(1f, 0.1f);
            speed.AddModifier(new StatModifier(ModifierOperation.Percent, -5f));
            Assert.That(speed.Value, Is.EqualTo(0.1f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidModifierAmountsAreRejected(float amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatModifier(ModifierOperation.Flat, amount));
        }

        [Test]
        public void InvalidStatAndOptionConfigurationIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatModifier((ModifierOperation)99, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ModifiableStat(1f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => DamageOption(0));
            Assert.Throws<ArgumentException>(() => new UpgradeOption("", "Damage", "", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Flat, 1f), 1));
        }

        [Test]
        public void WeaponModifiersChangeRuntimeDamageAndInterval()
        {
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            int changes = 0;
            weapon.StatsChanged += () => changes++;

            weapon.AddModifier(UpgradeStat.Damage, new StatModifier(ModifierOperation.Flat, 5f));
            weapon.AddModifier(UpgradeStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, 0.25f));

            Assert.That(weapon.Damage, Is.EqualTo(15f));
            Assert.That(weapon.Interval, Is.EqualTo(0.64f).Within(1e-5f));
            Assert.That(weapon.Range, Is.EqualTo(3f));
            Assert.That(changes, Is.EqualTo(2));
            var target = new HealthState(50f);
            weapon.TryAttack(target);
            Assert.That(target.Current, Is.EqualTo(35f));
        }

        [Test]
        public void AttackSpeedChangeKeepsCurrentCooldownAndAppliesToNextAttack()
        {
            var weapon = new WeaponRuntime(10f, 1f, 3f);
            var target = new HealthState(100f);
            weapon.TryAttack(target);
            weapon.AddModifier(UpgradeStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, 1f));

            weapon.Tick(0.5f);
            Assert.That(weapon.TryAttack(target), Is.False, "The in-progress one-second cooldown is not shortened.");
            weapon.Tick(0.5f);
            Assert.That(weapon.TryAttack(target), Is.True);
            weapon.Tick(0.5f);
            Assert.That(weapon.TryAttack(target), Is.True, "The next cooldown uses the doubled attack speed.");
        }

        [Test]
        public void LevelsFollowLinearThresholdsAndCountPendingUpgrades()
        {
            var run = new RunState(10);
            int levelChanges = 0;
            run.LevelChanged += () => levelChanges++;

            run.AddExperience(9);
            Assert.That(run.Level, Is.Zero);
            Assert.That(run.ExperienceForNextLevel, Is.EqualTo(10));

            run.AddExperience(1);
            Assert.That(run.Level, Is.EqualTo(1));
            Assert.That(run.PendingUpgrades, Is.EqualTo(1));
            Assert.That(run.ExperienceForNextLevel, Is.EqualTo(20));

            run.AddExperience(25);
            Assert.That(run.Level, Is.EqualTo(3));
            Assert.That(run.PendingUpgrades, Is.EqualTo(3));
            Assert.That(levelChanges, Is.EqualTo(2));
        }

        [Test]
        public void RecordingAnUpgradeWithoutPendingLevelIsRejected()
        {
            var run = new RunState(10);
            Assert.Throws<InvalidOperationException>(() => run.RecordUpgradeApplied());
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunState(0));
        }

        [Test]
        public void NoOfferUntilThresholdThenOfferContainsConfiguredChoiceCount()
        {
            var run = new RunState(10);
            var service = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f),
                new[] { DamageOption(), SpeedOption() }, 2);
            int offers = 0;
            service.OfferChanged += () => offers++;

            run.AddExperience(5);
            Assert.That(service.CurrentOffer, Is.Null);

            run.AddExperience(5);
            Assert.That(service.CurrentOffer, Is.Not.Null);
            Assert.That(service.CurrentOffer.Choices.Count, Is.EqualTo(2));
            Assert.That(offers, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedSelectionOfTheSameOfferAppliesExactlyOnce()
        {
            var run = new RunState(10);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            UpgradeOption damage = DamageOption();
            var service = new UpgradeService(run, weapon, new[] { damage, SpeedOption() }, 2);
            run.AddExperience(10);
            UpgradeOffer offer = service.CurrentOffer;

            Assert.That(service.TrySelect(offer, 0), Is.True);
            Assert.That(service.TrySelect(offer, 0), Is.False);
            Assert.That(service.TrySelect(offer, 1), Is.False);

            Assert.That(weapon.Damage, Is.EqualTo(15f));
            Assert.That(weapon.Interval, Is.EqualTo(0.8f));
            Assert.That(service.StacksOf(damage), Is.EqualTo(1));
            Assert.That(run.UpgradesApplied, Is.EqualTo(1));
            Assert.That(service.CurrentOffer, Is.Null);
        }

        [Test]
        public void ReentrantSelectionFromOfferCallbackCannotDuplicate()
        {
            var run = new RunState(10);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            var service = new UpgradeService(run, weapon, new[] { DamageOption(), SpeedOption() }, 2);
            run.AddExperience(10);
            UpgradeOffer offer = service.CurrentOffer;
            service.OfferChanged += () => service.TrySelect(offer, 0);

            service.TrySelect(offer, 0);

            Assert.That(weapon.Damage, Is.EqualTo(15f));
            Assert.That(run.UpgradesApplied, Is.EqualTo(1));
        }

        [Test]
        public void StaleOfferIsRejectedWhenAnotherLevelIsPending()
        {
            var run = new RunState(10);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            var service = new UpgradeService(run, weapon, new[] { DamageOption(), SpeedOption() }, 2);
            run.AddExperience(20);
            UpgradeOffer first = service.CurrentOffer;

            Assert.That(service.TrySelect(first, 0), Is.True);
            UpgradeOffer second = service.CurrentOffer;
            Assert.That(second, Is.Not.Null.And.Not.SameAs(first));
            Assert.That(service.TrySelect(first, 1), Is.False);
            Assert.That(service.TrySelect(second, 1), Is.True);

            Assert.That(weapon.Damage, Is.EqualTo(15f));
            Assert.That(weapon.Interval, Is.EqualTo(0.64f).Within(1e-5f));
            Assert.That(service.CurrentOffer, Is.Null);
        }

        [Test]
        public void InvalidSlotsAndNullOffersAreRejected()
        {
            var run = new RunState(10);
            var service = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f),
                new[] { DamageOption(), SpeedOption() }, 2);
            run.AddExperience(10);

            Assert.That(service.TrySelect(null, 0), Is.False);
            Assert.That(service.TrySelect(service.CurrentOffer, -1), Is.False);
            Assert.That(service.TrySelect(service.CurrentOffer, 2), Is.False);
            Assert.That(run.UpgradesApplied, Is.Zero);
        }

        [Test]
        public void MaxedUpgradesLeaveTheOfferAndExhaustedPoolOpensNone()
        {
            var run = new RunState(10);
            UpgradeOption damage = DamageOption(1);
            UpgradeOption speed = SpeedOption(1);
            var service = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f), new[] { damage, speed }, 2);
            run.AddExperience(30);

            service.TrySelect(service.CurrentOffer, 0);
            Assert.That(service.CurrentOffer.Choices, Is.EqualTo(new[] { speed }));
            service.TrySelect(service.CurrentOffer, 0);

            Assert.That(service.CurrentOffer, Is.Null);
            Assert.That(run.PendingUpgrades, Is.EqualTo(1));
        }

        [Test]
        public void ChoiceCountLimitsOfferSizeAndPoolIsValidated()
        {
            var run = new RunState(10);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            UpgradeOption damage = DamageOption();
            var service = new UpgradeService(run, weapon, new[] { damage, SpeedOption() }, 1);
            run.AddExperience(10);

            // Two eligible cards for one slot is a draw from the run's seed (OfferEngineTests): one card, from the pool,
            // and the same card for the same seed.
            Assert.That(service.CurrentOffer.Choices, Has.Count.EqualTo(1));
            Assert.That(new[] { damage.Id, "upgrade_attack_speed" }, Does.Contain(service.CurrentOffer.Choices[0].Id));
            var sameSeed = new RunState(10);
            var again = new UpgradeService(sameSeed, new WeaponRuntime(10f, 0.8f, 3f), new[] { DamageOption(), SpeedOption() }, 1);
            sameSeed.AddExperience(10);
            Assert.That(again.CurrentOffer.Choices[0].Id, Is.EqualTo(service.CurrentOffer.Choices[0].Id));
            Assert.Throws<ArgumentException>(() => new UpgradeService(run, weapon, new[] { damage, damage }, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeService(run, weapon, new[] { damage }, 0));
        }

        [Test]
        public void SelectedReportsEachAcceptedChoiceOnceAfterItAppliesAndBeforeTheFollowingOffer()
        {
            var run = new RunState(10);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            UpgradeOption damage = DamageOption();
            UpgradeOption speed = SpeedOption();
            var service = new UpgradeService(run, weapon, new[] { damage, speed }, 2);
            run.AddExperience(20);
            UpgradeOffer first = service.CurrentOffer;

            var log = new List<string>();
            var slots = new List<int>();
            var offersSeenBySelected = new List<UpgradeOffer>();
            float intervalSeenBySelected = 0f;
            int stacksSeenBySelected = 0;
            int appliedSeenBySelected = 0;
            service.Selected += (option, slot) =>
            {
                log.Add("selected " + option.Id);
                slots.Add(slot);
                offersSeenBySelected.Add(service.CurrentOffer);
                if (option == speed)
                {
                    intervalSeenBySelected = weapon.Interval;
                    stacksSeenBySelected = service.StacksOf(speed);
                    appliedSeenBySelected = run.UpgradesApplied;
                }
            };
            service.OfferChanged += () => log.Add("offer");

            Assert.That(service.TrySelect(first, 1), Is.True);
            UpgradeOffer second = service.CurrentOffer;
            Assert.That(second, Is.Not.Null.And.Not.SameAs(first));
            Assert.That(intervalSeenBySelected, Is.EqualTo(0.64f).Within(1e-5f), "The modifier is applied before Selected.");
            Assert.That(stacksSeenBySelected, Is.EqualTo(1));
            Assert.That(appliedSeenBySelected, Is.EqualTo(1));
            Assert.That(offersSeenBySelected[0], Is.SameAs(second), "Selected already sees the following offer.");

            Assert.That(service.TrySelect(first, 1), Is.False, "A repeated tap on the old offer is not reported.");
            Assert.That(service.TrySelect(first, 0), Is.False, "A stale offer is not reported.");
            Assert.That(service.TrySelect(null, 0), Is.False);
            Assert.That(service.TrySelect(second, -1), Is.False);
            Assert.That(service.TrySelect(second, 2), Is.False);
            Assert.That(log, Is.EqualTo(new[] { "selected upgrade_attack_speed", "offer" }));

            Assert.That(service.TrySelect(second, 0), Is.True);
            Assert.That(offersSeenBySelected[1], Is.Null, "No level is pending, so no offer follows the last choice.");
            Assert.That(service.TrySelect(second, 0), Is.False);

            Assert.That(log, Is.EqualTo(new[] { "selected upgrade_attack_speed", "offer", "selected upgrade_damage", "offer" }));
            Assert.That(slots, Is.EqualTo(new[] { 1, 0 }));
        }

        [Test]
        public void SelectedIsNotReportedForAnOfferWithdrawnAtRunEndOrAReentrantTap()
        {
            var run = new RunState(10);
            var service = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f),
                new[] { DamageOption(), SpeedOption() }, 2);
            run.AddExperience(10);
            UpgradeOffer offer = service.CurrentOffer;
            int selections = 0;
            int offerChanges = 0;
            service.Selected += (option, slot) => selections++;
            service.OfferChanged += () => offerChanges++;

            run.End(RunOutcome.Defeat);
            Assert.That(service.CurrentOffer, Is.Null);
            Assert.That(offerChanges, Is.EqualTo(1));
            Assert.That(service.TrySelect(offer, 0), Is.False, "A choice after the run ended is rejected.");
            run.AddExperience(50);
            Assert.That(selections, Is.Zero);

            var reentrantRun = new RunState(10);
            var reentrant = new UpgradeService(reentrantRun, new WeaponRuntime(10f, 0.8f, 3f),
                new[] { DamageOption(), SpeedOption() }, 2);
            reentrantRun.AddExperience(10);
            UpgradeOffer only = reentrant.CurrentOffer;
            int reentrantSelections = 0;
            reentrant.Selected += (option, slot) =>
            {
                reentrantSelections++;
                reentrant.TrySelect(only, slot);
            };
            reentrant.OfferChanged += () => reentrant.TrySelect(only, 0);

            Assert.That(reentrant.TrySelect(only, 0), Is.True);
            Assert.That(reentrantSelections, Is.EqualTo(1));
            Assert.That(reentrantRun.UpgradesApplied, Is.EqualTo(1));
        }

        [Test]
        public void ABookGrantsOneStackOfAnEligibleCardAndRespectsEveryLimitTheOfferDoes()
        {
            var run = new RunState(10, 0.5f, 0, seed: 21);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            UpgradeOption damage = DamageOption(2);
            UpgradeOption speed = SpeedOption(2);
            var service = new UpgradeService(run, weapon, new[] { damage, speed }, 2);

            Assert.That(service.TryGrantBook(0, out UpgradeOption book, out UpgradeRarity rarity), Is.True);
            Assert.That(book, Is.Not.Null);
            Assert.That(service.StacksOf(book), Is.EqualTo(1));
            Assert.That(service.BooksGranted, Is.EqualTo(1));
            Assert.That(run.UpgradesApplied, Is.Zero,
                "A book answers no level-up, so the run's applied-upgrade count is still what the player chose.");
            float damageAfter = weapon.Damage;
            Assert.That(book.Stat == UpgradeStat.Damage ? damageAfter > 10f : damageAfter == 10f, Is.True,
                "The book that was granted is the one that moved a stat.");

            // The same chest of the same run always holds the same book at the same tier.
            var replay = new UpgradeService(new RunState(10, 0.5f, 0, seed: 21), new WeaponRuntime(10f, 0.8f, 3f),
                new[] { DamageOption(2), SpeedOption(2) }, 2);
            Assert.That(replay.TryGrantBook(0, out UpgradeOption again, out UpgradeRarity againRarity), Is.True);
            Assert.That(again.Id, Is.EqualTo(book.Id));
            Assert.That(againRarity, Is.EqualTo(rarity));

            // Fill the pool: four stacks over two cards of two, and the fifth book finds nothing to give.
            for (int chest = 1; chest < 4; chest++)
                Assert.That(service.TryGrantBook(chest, out _, out _), Is.True, $"Chest {chest} still has room.");
            Assert.That(service.StacksOf(damage), Is.EqualTo(2));
            Assert.That(service.StacksOf(speed), Is.EqualTo(2));
            Assert.That(service.TryGrantBook(4, out UpgradeOption none, out _), Is.False, "A full build takes no books.");
            Assert.That(none, Is.Null);
        }

        [Test]
        public void ABookNeverReachesACardWhoseRequirementIsUnmet()
        {
            UpgradeOption damage = DamageOption(3);
            var evolved = new UpgradeOption("upgrade_evolved", "Evolved", "+{0:0} damage", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Flat, 50f), 1, requiresId: "upgrade_damage");
            var run = new RunState(10, 0.5f, 0, seed: 5);
            var service = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f), new[] { evolved, damage }, 1);

            Assert.That(service.TryGrantBook(0, out UpgradeOption first, out _), Is.True);
            Assert.That(first.Id, Is.EqualTo("upgrade_damage"),
                "The locked card stands first in the pool and is still not in the book's reach.");
            Assert.That(service.StacksOf(evolved), Is.Zero);

            // And once its requirement holds a stack it is reachable, like any card a level-up would now offer.
            bool found = false;
            for (int chest = 1; chest < 12 && !found; chest++)
                found = service.TryGrantBook(chest, out UpgradeOption later, out _) && later.Id == evolved.Id;
            Assert.That(found, Is.True, "A book reaches a card whose requirement the run has since met.");
        }

        [Test]
        public void ABookDrawnBetweenTwoLevelUpsLeavesBothOffersWhereTheyWere()
        {
            // The two services see the same run and the same seed; only one of them opens a chest in between.
            UpgradeOffer[] Offers(bool withBook)
            {
                var run = new RunState(10, 0.5f, 0, seed: 33);
                var service = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f),
                    new[] { DamageOption(9), SpeedOption(9), RangeOption(9) }, 2);
                run.AddExperience(10);
                UpgradeOffer first = service.CurrentOffer;
                Assert.That(service.TrySelect(first, 0), Is.True);
                if (withBook)
                    service.TryGrantBook(0, out _, out _);
                run.AddExperience(10);
                return new[] { first, service.CurrentOffer };
            }

            UpgradeOffer[] plain = Offers(false);
            UpgradeOffer[] withBook = Offers(true);
            for (int i = 0; i < plain.Length; i++)
            {
                Assert.That(withBook[i].Index, Is.EqualTo(plain[i].Index));
                for (int slot = 0; slot < plain[i].Choices.Count; slot++)
                {
                    Assert.That(withBook[i].Choices[slot].Id, Is.EqualTo(plain[i].Choices[slot].Id),
                        $"Offer {i} slot {slot} moved because a chest was opened.");
                    Assert.That(withBook[i].RarityAt(slot), Is.EqualTo(plain[i].RarityAt(slot)));
                }
            }
        }

        private static UpgradeOption RangeOption(int maxStacks = 5) =>
            new UpgradeOption("upgrade_range", "Range", "+{0:0} range", UpgradeStat.Range,
                new StatModifier(ModifierOperation.Flat, 0.3f), maxStacks);

        [Test]
        public void PercentDescriptionValueReadsAsWholePercent()
        {
            Assert.That(SpeedOption().DescriptionValue, Is.EqualTo(25f));
            Assert.That(DamageOption().DescriptionValue, Is.EqualTo(5f));
        }
    }
}
