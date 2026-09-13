using System;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class RewardTests
    {
        // High threshold keeps these reward cases independent of levelling.
        private const int ExperiencePerLevel = 1000;

        [Test]
        public void KillAwardsItsExperienceExactlyOnce()
        {
            var run = new RunState(ExperiencePerLevel);
            var rewards = new RewardService(run);
            var victim = new HealthState(50f);
            int changes = 0;
            run.ExperienceChanged += () => changes++;
            victim.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(victim, 10, 0), Is.True);
            Assert.That(rewards.TryAwardKill(victim, 10, 0), Is.False);
            Assert.That(run.Experience, Is.EqualTo(10));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void LivingAndNullVictimsAreNotRewarded()
        {
            var run = new RunState(ExperiencePerLevel);
            var rewards = new RewardService(run);
            var victim = new HealthState(50f);

            Assert.That(rewards.TryAwardKill(null, 10, 0), Is.False);
            Assert.That(rewards.TryAwardKill(victim, 10, 0), Is.False);
            victim.ApplyDamage(new DamageContext(10f));
            Assert.That(rewards.TryAwardKill(victim, 10, 0), Is.False);
            Assert.That(run.Experience, Is.Zero);
        }

        [Test]
        public void RepeatedAndReentrantDeathEventsAwardOnce()
        {
            var run = new RunState(ExperiencePerLevel);
            var rewards = new RewardService(run);
            var victim = new HealthState(50f);
            victim.Died += () =>
            {
                rewards.TryAwardKill(victim, 10, 0);
                rewards.TryAwardKill(victim, 10, 0);
            };

            victim.ApplyDamage(new DamageContext(500f));
            victim.ApplyDamage(new DamageContext(500f));
            rewards.TryAwardKill(victim, 10, 0);

            Assert.That(run.Experience, Is.EqualTo(10));
        }

        [Test]
        public void EachVictimIsRewardedIndependently()
        {
            var run = new RunState(ExperiencePerLevel);
            var rewards = new RewardService(run);
            var first = new HealthState(50f);
            var second = new HealthState(50f);
            first.ApplyDamage(new DamageContext(50f));
            second.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(first, 10, 0), Is.True);
            Assert.That(rewards.TryAwardKill(second, 10, 0), Is.True);
            Assert.That(rewards.TryAwardKill(first, 10, 0), Is.False);
            Assert.That(run.Experience, Is.EqualTo(20));
        }

        [Test]
        public void ZeroRewardMarksVictimWithoutChangingExperience()
        {
            var run = new RunState(ExperiencePerLevel);
            var rewards = new RewardService(run);
            var victim = new HealthState(50f);
            int changes = 0;
            run.ExperienceChanged += () => changes++;
            victim.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(victim, 0, 0), Is.True);
            Assert.That(rewards.TryAwardKill(victim, 0, 0), Is.False);
            Assert.That(run.Experience, Is.Zero);
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void MissingRunAndNegativeRewardsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new RewardService(null));
            var rewards = new RewardService(new RunState(ExperiencePerLevel));
            var victim = new HealthState(10f);
            victim.ApplyDamage(new DamageContext(10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => rewards.TryAwardKill(victim, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rewards.TryAwardKill(victim, 0, -1));
        }

        [Test]
        public void RunStateAccumulatesAndRejectsNegativeExperience()
        {
            var run = new RunState(ExperiencePerLevel);
            int changes = 0;
            run.ExperienceChanged += () => changes++;

            run.AddExperience(3);
            run.AddExperience(0);
            run.AddExperience(4);

            Assert.That(run.Experience, Is.EqualTo(7));
            Assert.That(changes, Is.EqualTo(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.AddExperience(-1));
        }

        [Test]
        public void PrototypeBalanceAwardsTenExperienceForOneGrunt()
        {
            var run = new RunState(ExperiencePerLevel);
            var rewards = new RewardService(run);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            var target = new HealthState(50f);
            target.Died += () => rewards.TryAwardKill(target, 10, 5);
            for (int i = 0; i < 20; i++)
            {
                weapon.TryAttack(target);
                weapon.Tick(0.8f);
            }

            Assert.That(run.Experience, Is.EqualTo(10), "Enemy_Grunt.asset gives 10 experience.");
            Assert.That(run.Gold, Is.EqualTo(5));
        }
    }
}
