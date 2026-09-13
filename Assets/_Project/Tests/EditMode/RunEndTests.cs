using System;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class RunEndTests
    {
        private static UpgradeOption DamageOption() =>
            new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0} damage", WeaponStat.Damage,
                new StatModifier(ModifierOperation.Flat, 5f), 5);

        private static UpgradeOption SpeedOption() =>
            new UpgradeOption("upgrade_attack_speed", "Quickened Grip", "+{0:0}% attack speed", WeaponStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.25f), 5);

        [Test]
        public void RunEndsOnceAndIgnoresLaterExperience()
        {
            var run = new RunState(10);
            int ends = 0;
            int experienceChanges = 0;
            run.Ended += () => ends++;
            run.ExperienceChanged += () => experienceChanges++;
            run.AddExperience(5);

            run.End(RunOutcome.Defeat);
            run.End(RunOutcome.Defeat);
            run.AddExperience(50);

            Assert.That(run.HasEnded, Is.True);
            Assert.That(ends, Is.EqualTo(1));
            Assert.That(run.Experience, Is.EqualTo(5));
            Assert.That(run.Level, Is.Zero);
            Assert.That(experienceChanges, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.AddExperience(-1));
        }

        [Test]
        public void EndingTheRunWithdrawsAnOpenOfferAndRejectsItsSelection()
        {
            var run = new RunState(10);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            var service = new UpgradeService(run, weapon, new[] { DamageOption(), SpeedOption() }, 2);
            run.AddExperience(10);
            UpgradeOffer offer = service.CurrentOffer;
            int offerChanges = 0;
            service.OfferChanged += () => offerChanges++;

            run.End(RunOutcome.Defeat);

            Assert.That(service.CurrentOffer, Is.Null);
            Assert.That(offerChanges, Is.EqualTo(1));
            Assert.That(service.TrySelect(offer, 0), Is.False);
            Assert.That(weapon.Damage, Is.EqualTo(10f));
            Assert.That(run.UpgradesApplied, Is.Zero);
        }

        [Test]
        public void KillsAfterTheRunEndsGrantNothing()
        {
            var run = new RunState(10);
            var rewards = new RewardService(run);
            var service = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f), new[] { DamageOption() }, 2);
            var victim = new HealthState(50f);
            run.End(RunOutcome.Defeat);

            victim.ApplyDamage(new DamageContext(50f));
            rewards.TryAwardKill(victim, 10, 0);

            Assert.That(run.Experience, Is.Zero);
            Assert.That(service.CurrentOffer, Is.Null);
        }

        [Test]
        public void PoolListsOptionsInOrderWithAppliedStacks()
        {
            var run = new RunState(10);
            UpgradeOption damage = DamageOption();
            UpgradeOption speed = SpeedOption();
            var service = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f), new[] { damage, speed }, 2);
            run.AddExperience(30);
            service.TrySelect(service.CurrentOffer, 0);
            service.TrySelect(service.CurrentOffer, 1);
            service.TrySelect(service.CurrentOffer, 0);

            Assert.That(service.Pool, Is.EqualTo(new[] { damage, speed }));
            Assert.That(service.StacksOf(damage), Is.EqualTo(2));
            Assert.That(service.StacksOf(speed), Is.EqualTo(1));
        }

        [Test]
        public void EncountersClearedCountsOnlyFinishedFights()
        {
            var progress = new EncounterProgress(1f);
            Assert.That(progress.EncountersCleared, Is.Zero);
            progress.Begin();
            Assert.That(progress.EncountersCleared, Is.Zero);
            progress.Clear();
            Assert.That(progress.EncountersCleared, Is.EqualTo(1));
            progress.Begin();
            Assert.That(progress.EncountersCleared, Is.EqualTo(1));
        }

        [Test]
        public void GruntStrikesWhileTheHeroFightsAndUpgradesReduceDamageTaken()
        {
            var hero = new HealthState(100f);
            var heroWeapon = new WeaponRuntime(10f, 0.8f, 3f);
            SimulateEncounter(hero, heroWeapon, out _);
            Assert.That(hero.Current, Is.EqualTo(76f), "Four 6-damage strikes land before the fifth Sword hit.");

            heroWeapon.AddModifier(WeaponStat.Damage, new StatModifier(ModifierOperation.Flat, 5f));
            SimulateEncounter(hero, heroWeapon, out _);
            Assert.That(hero.Current, Is.EqualTo(58f), "Three strikes land before the fourth upgraded hit.");
        }

        [Test]
        public void PrototypeRunEndsInDeathAfterSeveralEncounters()
        {
            var run = new RunState(10);
            var rewards = new RewardService(run);
            var heroWeapon = new WeaponRuntime(10f, 0.8f, 3f);
            var service = new UpgradeService(run, heroWeapon, new[] { DamageOption(), SpeedOption() }, 2);
            var hero = new HealthState(100f);
            hero.Died += () => run.End(RunOutcome.Defeat);
            var progress = new EncounterProgress(0f);

            while (!run.HasEnded && progress.EncounterNumber < 100)
            {
                progress.Begin();
                HealthState grunt = SimulateEncounter(hero, heroWeapon, out bool heroDied);
                if (heroDied)
                    break;

                progress.Clear();
                rewards.TryAwardKill(grunt, 10, 5);
                // Always take the first card: damage until it is maxed, then attack speed.
                while (service.CurrentOffer != null)
                    service.TrySelect(service.CurrentOffer, 0);
            }

            Assert.That(run.HasEnded, Is.True, "Enemy strikes must eventually end the run.");
            Assert.That(progress.EncountersCleared, Is.InRange(5, 20));
        }

        // Fixed 60 Hz steps; both combatants start ready and the hero acts first each frame.
        private static HealthState SimulateEncounter(HealthState hero, WeaponRuntime heroWeapon, out bool heroDied)
        {
            const float step = 1f / 60f;
            var grunt = new HealthState(50f);
            var gruntWeapon = new WeaponRuntime(6f, 1f, 3f);
            heroWeapon.Tick(10f);
            for (int frame = 0; frame < 6000 && grunt.IsAlive && hero.IsAlive; frame++)
            {
                if (frame > 0)
                {
                    heroWeapon.Tick(step);
                    gruntWeapon.Tick(step);
                }
                heroWeapon.TryAttack(grunt);
                if (grunt.IsAlive)
                    gruntWeapon.TryAttack(hero);
            }

            heroDied = !hero.IsAlive;
            return grunt;
        }
    }
}
