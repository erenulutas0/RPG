using System;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class RewardTests
    {
        [Test]
        public void KillAwardsConfiguredExperienceExactlyOnce()
        {
            var run = new RunState();
            var rewards = new RewardService(run, 10);
            var victim = new HealthState(50f);
            int changes = 0;
            run.ExperienceChanged += () => changes++;
            victim.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(victim), Is.True);
            Assert.That(rewards.TryAwardKill(victim), Is.False);
            Assert.That(run.Experience, Is.EqualTo(10));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void LivingAndNullVictimsAreNotRewarded()
        {
            var run = new RunState();
            var rewards = new RewardService(run, 10);
            var victim = new HealthState(50f);

            Assert.That(rewards.TryAwardKill(null), Is.False);
            Assert.That(rewards.TryAwardKill(victim), Is.False);
            victim.ApplyDamage(new DamageContext(10f));
            Assert.That(rewards.TryAwardKill(victim), Is.False);
            Assert.That(run.Experience, Is.Zero);
        }

        [Test]
        public void RepeatedAndReentrantDeathEventsAwardOnce()
        {
            var run = new RunState();
            var rewards = new RewardService(run, 10);
            var victim = new HealthState(50f);
            victim.Died += () =>
            {
                rewards.TryAwardKill(victim);
                rewards.TryAwardKill(victim);
            };

            victim.ApplyDamage(new DamageContext(500f));
            victim.ApplyDamage(new DamageContext(500f));
            rewards.TryAwardKill(victim);

            Assert.That(run.Experience, Is.EqualTo(10));
        }

        [Test]
        public void EachVictimIsRewardedIndependently()
        {
            var run = new RunState();
            var rewards = new RewardService(run, 10);
            var first = new HealthState(50f);
            var second = new HealthState(50f);
            first.ApplyDamage(new DamageContext(50f));
            second.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(first), Is.True);
            Assert.That(rewards.TryAwardKill(second), Is.True);
            Assert.That(rewards.TryAwardKill(first), Is.False);
            Assert.That(run.Experience, Is.EqualTo(20));
        }

        [Test]
        public void ZeroRewardMarksVictimWithoutChangingExperience()
        {
            var run = new RunState();
            var rewards = new RewardService(run, 0);
            var victim = new HealthState(50f);
            int changes = 0;
            run.ExperienceChanged += () => changes++;
            victim.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(victim), Is.True);
            Assert.That(rewards.TryAwardKill(victim), Is.False);
            Assert.That(run.Experience, Is.Zero);
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void InvalidRewardConfigurationIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new RewardService(null, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RewardService(new RunState(), -1));
        }

        [Test]
        public void RunStateAccumulatesAndRejectsNegativeExperience()
        {
            var run = new RunState();
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
            var run = new RunState();
            var rewards = new RewardService(run, 10);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            var target = new HealthState(50f);
            target.Died += () => rewards.TryAwardKill(target);
            for (int i = 0; i < 20; i++)
            {
                weapon.TryAttack(target);
                weapon.Tick(0.8f);
            }

            Assert.That(run.Experience, Is.EqualTo(10));
        }
    }
}
