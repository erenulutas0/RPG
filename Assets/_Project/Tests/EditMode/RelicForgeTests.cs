using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class RelicForgeTests
    {
        [Test]
        public void ProfileSpendsOnlyOnAForgeThatItCanAfford()
        {
            var profile = new PlayerProfile();
            int changes = 0;
            profile.Changed += () => changes++;

            profile.Deposit(100);
            profile.Deposit(0);
            Assert.That(profile.TryForgeRelic("relic_counterweight", 150), Is.False);
            Assert.That(profile.TryForgeRelic("relic_second_wind", 80), Is.True);
            Assert.That(profile.TryForgeRelic("relic_second_wind", 80), Is.False, "A relic is forged once.");

            Assert.That(profile.Gold, Is.EqualTo(20));
            Assert.That(profile.OwnedRelicIds, Is.EqualTo(new[] { "relic_second_wind" }));
            Assert.That(profile.EquippedRelicId, Is.EqualTo("relic_second_wind"), "Forging equips the new relic.");
            Assert.That(changes, Is.EqualTo(2), "One change per deposit and per forge, so each is saved once.");
            Assert.That(profile.Equip("relic_second_wind"), Is.False);
            Assert.That(profile.Equip("relic_counterweight"), Is.False, "Only owned relics can be equipped.");
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.Deposit(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.TryForgeRelic("relic_x", -1));
        }

        [Test]
        public void RestoredProfilesRepairDamagedValues()
        {
            var damaged = new PlayerProfile(-5, new[] { "relic_a", "relic_a", null, "" }, "relic_b", -2);
            Assert.That(damaged.Gold, Is.Zero);
            Assert.That(damaged.OwnedRelicIds, Is.EqualTo(new[] { "relic_a" }));
            Assert.That(damaged.EquippedRelicId, Is.Null, "An unowned relic cannot stay equipped.");
            Assert.That(damaged.DeepestFloorCleared, Is.Zero);

            var valid = new PlayerProfile(40, new[] { "relic_a", "relic_b" }, "relic_b", 2);
            Assert.That(valid.EquippedRelicId, Is.EqualTo("relic_b"));
            Assert.That(new PlayerProfile(0, null, null, 0).OwnedRelicIds, Is.Empty);
        }

        [Test]
        public void DepthRecordOnlyRises()
        {
            var profile = new PlayerProfile();
            int changes = 0;
            profile.Changed += () => changes++;

            profile.RecordFloorCleared(2);
            profile.RecordFloorCleared(1);
            profile.RecordFloorCleared(2);

            Assert.That(profile.DeepestFloorCleared, Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.RecordFloorCleared(0));
        }

        [Test]
        public void ShopForgesAffordableRelicsEquipsOwnedOnesAndNamesTheNextUnlock()
        {
            var profile = new PlayerProfile(200, null, null, 0);
            RelicOption wind = DescentSimulation.SecondWind();
            RelicOption counter = DescentSimulation.Counterweight();
            var shop = new RelicShop(profile, new[] { wind, counter });
            Assert.That(shop.NextUnlock, Is.SameAs(wind), "The cheapest unowned relic is the next unlock.");
            Assert.That(shop.StatusOf(counter), Is.EqualTo(UnlockStatus.Affordable));

            Assert.That(shop.TrySelect(wind), Is.True);
            Assert.That(shop.TrySelect(wind), Is.False, "Tapping the equipped relic changes nothing.");
            Assert.That(profile.Gold, Is.EqualTo(120));
            Assert.That(shop.Equipped, Is.SameAs(wind));
            Assert.That(shop.StatusOf(counter), Is.EqualTo(UnlockStatus.TooExpensive));
            Assert.That(shop.GoldNeededFor(counter), Is.EqualTo(30));
            Assert.That(shop.TrySelect(counter), Is.False);
            Assert.That(shop.NextUnlock, Is.SameAs(counter));

            profile.Deposit(30);
            Assert.That(shop.TrySelect(counter), Is.True);
            Assert.That(profile.Gold, Is.Zero);
            Assert.That(shop.StatusOf(wind), Is.EqualTo(UnlockStatus.Owned));
            Assert.That(shop.GoldNeededFor(wind), Is.Zero);
            Assert.That(shop.TrySelect(wind), Is.True, "An owned relic is equipped on tap.");
            Assert.That(shop.Equipped, Is.SameAs(wind));
            Assert.That(shop.StatusOf(counter), Is.EqualTo(UnlockStatus.Owned));
            Assert.That(shop.NextUnlock, Is.Null);
        }

        [Test]
        public void ShopReportsForgedOnlyForPurchasesTheProfileAccepted()
        {
            var profile = new PlayerProfile(200, null, null, 0);
            RelicOption wind = DescentSimulation.SecondWind();
            RelicOption counter = DescentSimulation.Counterweight();
            var shop = new RelicShop(profile, new[] { wind, counter });
            var forged = new List<RelicOption>();
            var goldSeenByForged = new List<int>();
            bool ownedAndEquippedWhenForged = true;
            shop.Forged += relic =>
            {
                forged.Add(relic);
                goldSeenByForged.Add(profile.Gold);
                ownedAndEquippedWhenForged &= profile.Owns(relic.Id) && profile.EquippedRelicId == relic.Id;
            };

            Assert.That(shop.TrySelect(wind), Is.True);
            Assert.That(forged, Is.EqualTo(new[] { wind }));
            Assert.That(goldSeenByForged, Is.EqualTo(new[] { 120 }), "Forged follows the accepted spend.");

            Assert.That(shop.TrySelect(wind), Is.False, "Tapping the equipped relic is not a purchase.");
            Assert.That(shop.TrySelect(counter), Is.False, "An unaffordable relic is not a purchase.");
            Assert.That(forged, Has.Count.EqualTo(1));

            profile.Deposit(30);
            Assert.That(shop.TrySelect(counter), Is.True);
            Assert.That(shop.TrySelect(wind), Is.True, "Equipping an owned relic succeeds without a purchase.");
            Assert.That(shop.TrySelect(counter), Is.True);
            Assert.That(shop.TrySelect(counter), Is.False);

            Assert.That(forged, Is.EqualTo(new[] { wind, counter }));
            Assert.That(goldSeenByForged, Is.EqualTo(new[] { 120, 0 }));
            Assert.That(ownedAndEquippedWhenForged, Is.True);
        }

        [Test]
        public void ShopRejectsDuplicateAndUnlistedRelicsAndIgnoresUnknownSavedIds()
        {
            Assert.Throws<ArgumentException>(() => new RelicShop(new PlayerProfile(), new[] { DescentSimulation.SecondWind(), DescentSimulation.SecondWind() }));
            Assert.Throws<ArgumentException>(() => new RelicShop(new PlayerProfile(), new RelicOption[] { null }));
            var shop = new RelicShop(new PlayerProfile(500, new[] { "relic_removed" }, "relic_removed", 0), new[] { DescentSimulation.SecondWind() });

            Assert.That(shop.Equipped, Is.Null, "A saved relic that this build no longer sells is not applied.");
            Assert.Throws<ArgumentException>(() => shop.StatusOf(DescentSimulation.Counterweight()));
            Assert.Throws<ArgumentException>(() => shop.StatusOf(DescentSimulation.SecondWind()), "Only the shop's own relic instances are accepted.");
        }

        [TestCase(RunOutcome.Defeat, 100)]
        [TestCase(RunOutcome.Extracted, 104)]
        [TestCase(RunOutcome.Victory, 104)]
        public void RunBankDepositsSecuredGoldAtTheCheckpointAndTheRestAtTheEnd(RunOutcome outcome, int finalGold)
        {
            var run = new RunState(10, 0.5f);
            var profile = new PlayerProfile();
            var bank = new RunBank(run, profile);
            var deposits = new List<int>();
            profile.Changed += () => deposits.Add(profile.Gold);

            run.AddGold(96);
            Assert.That(profile.Gold, Is.Zero, "Unsecured gold stays in the run.");
            run.SecureGold();
            Assert.That(profile.Gold, Is.EqualTo(96), "Descending banks the secured gold at once.");
            run.AddGold(8);
            run.End(outcome);
            run.AddGold(50);

            Assert.That(profile.Gold, Is.EqualTo(finalGold));
            Assert.That(bank.Deposited, Is.EqualTo(finalGold));
            Assert.That(deposits, Is.EqualTo(new[] { 96, finalGold }));
        }

        [Test]
        public void ExtractingWithoutACheckpointBanksEverythingOnce()
        {
            var run = new RunState(10, 0.5f);
            var profile = new PlayerProfile(10, null, null, 0);
            new RunBank(run, profile);

            run.AddGold(96);
            run.End(RunOutcome.Extracted);
            run.End(RunOutcome.Defeat);

            Assert.That(profile.Gold, Is.EqualTo(106));
        }

        [Test]
        public void RelicOptionsValidateTheirEffectAndDescribeWholePercentages()
        {
            Assert.That(DescentSimulation.SecondWind().Description, Is.EqualTo("Once per run, at 25% health or less, restore 25% of your health"));
            Assert.That(DescentSimulation.Counterweight().Description, Is.EqualTo("When an enemy hits you, strike back for 60% of your weapon damage"));
            Assert.Throws<ArgumentException>(() => new RelicOption("", "", "", RelicEffect.SecondWind, 0.2f, 0.2f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RelicOption("r", "", "", RelicEffect.CounterStrike, 0f, 0f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RelicOption("r", "", "", RelicEffect.SecondWind, 1.5f, 0.2f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RelicOption("r", "", "", RelicEffect.SecondWind, 0.2f, 1f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RelicOption("r", "", "", RelicEffect.SecondWind, 0.2f, 0.2f, -1));
        }

        [Test]
        public void CounterweightStrikesBackOnlyWhileBothFightersLive()
        {
            var relic = new RelicRuntime(DescentSimulation.Counterweight());
            var hero = new HealthState(100f);
            var grunt = new HealthState(70f);
            int triggered = 0;
            relic.Triggered += () => triggered++;

            hero.ApplyDamage(new DamageContext(9f));
            Assert.That(relic.OnHeroDamaged(hero, grunt, 35f), Is.True);
            Assert.That(grunt.Current, Is.EqualTo(49f).Within(1e-4f), "60% of 35 damage.");
            Assert.That(relic.OnHeroDamaged(hero, grunt, 0f), Is.False);
            Assert.That(relic.OnHeroDamaged(hero, null, 35f), Is.False);

            grunt.ApplyDamage(new DamageContext(1000f));
            Assert.That(relic.OnHeroDamaged(hero, grunt, 35f), Is.False, "A dead attacker is not struck.");
            hero.ApplyDamage(new DamageContext(1000f));
            Assert.That(relic.OnHeroDamaged(hero, new HealthState(10f), 35f), Is.False, "A killing blow never triggers a relic.");
            Assert.That(relic.Triggers, Is.EqualTo(1));
            Assert.That(triggered, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => relic.OnHeroDamaged(hero, grunt, -1f));
        }

        [Test]
        public void SecondWindHealsOnceWhenHealthFirstFallsToTheThreshold()
        {
            var relic = new RelicRuntime(DescentSimulation.SecondWind());
            var hero = new HealthState(100f);

            hero.ApplyDamage(new DamageContext(74f));
            Assert.That(relic.OnHeroDamaged(hero, null, 10f), Is.False, "26 HP is above 25%.");
            hero.ApplyDamage(new DamageContext(1f));
            Assert.That(relic.OnHeroDamaged(hero, null, 10f), Is.True);
            Assert.That(hero.Current, Is.EqualTo(50f));

            hero.ApplyDamage(new DamageContext(30f));
            Assert.That(relic.OnHeroDamaged(hero, null, 10f), Is.False, "Once per run.");
            Assert.That(hero.Current, Is.EqualTo(20f));
            Assert.That(relic.Triggers, Is.EqualTo(1));

            var lethal = new RelicRuntime(DescentSimulation.SecondWind());
            var dead = new HealthState(100f);
            dead.ApplyDamage(new DamageContext(100f));
            Assert.That(lethal.OnHeroDamaged(dead, null, 10f), Is.False);
        }
    }
}
