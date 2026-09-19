using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class CombatStateTests
    {
        [Test]
        public void NonlethalDamageChangesHealthWithoutDeath()
        {
            var health = new HealthState(50f);
            int changes = 0;
            int deaths = 0;
            health.Changed += () => changes++;
            health.Died += () => deaths++;

            health.ApplyDamage(new DamageContext(10f));

            Assert.That(health.Current, Is.EqualTo(40f));
            Assert.That(health.Maximum, Is.EqualTo(50f));
            Assert.That(health.IsAlive, Is.True);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(deaths, Is.Zero);
        }

        [Test]
        public void OverkillClampsAndEmitsDeathOnlyOnceEvenWithReentrantDamage()
        {
            var health = new HealthState(50f);
            int changes = 0;
            int deaths = 0;
            health.Changed += () => { changes++; health.ApplyDamage(new DamageContext(1f)); };
            health.Died += () => { deaths++; health.ApplyDamage(new DamageContext(1f)); };

            health.ApplyDamage(new DamageContext(500f));
            health.ApplyDamage(new DamageContext(500f));

            Assert.That(health.Current, Is.Zero);
            Assert.That(health.IsAlive, Is.False);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void ZeroDamageDoesNotEmitChange()
        {
            var health = new HealthState(50f);
            int changes = 0;
            health.Changed += () => changes++;
            health.ApplyDamage(new DamageContext(0f));
            Assert.That(changes, Is.Zero);
            Assert.That(health.Current, Is.EqualTo(50f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidMaximumHealthIsRejected(float value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HealthState(value));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDamageIsRejected(float value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DamageContext(value));
        }

        [TestCase(0f, 1f, 1f)]
        [TestCase(1f, 0f, 1f)]
        [TestCase(1f, 1f, 0f)]
        [TestCase(-1f, 1f, 1f)]
        [TestCase(1f, float.NaN, 1f)]
        [TestCase(1f, 1f, float.PositiveInfinity)]
        public void InvalidUpgradeStatsAreRejected(float damage, float interval, float range)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponRuntime(damage, interval, range));
        }

        [Test]
        public void AttackIsImmediateThenWaitsForFullInterval()
        {
            var weapon = new WeaponRuntime(10f, 1f, 3f);
            var target = new HealthState(50f);
            Assert.That(weapon.TryAttack(target), Is.True);
            Assert.That(weapon.TryAttack(target), Is.False);
            weapon.Tick(0.75f);
            Assert.That(weapon.TryAttack(target), Is.False);
            weapon.Tick(0.25f);
            Assert.That(weapon.TryAttack(target), Is.True);
            Assert.That(target.Current, Is.EqualTo(30f));
        }

        [Test]
        public void NoTargetDoesNotConsumeAttackAndDeadTargetIsIgnored()
        {
            var weapon = new WeaponRuntime(50f, 1f, 3f);
            var target = new HealthState(50f);
            Assert.That(weapon.TryAttack(null), Is.False);
            Assert.That(weapon.TryAttack(target), Is.True);
            weapon.Tick(1f);
            Assert.That(weapon.TryAttack(target), Is.False);
            Assert.That(weapon.TryAttack(new HealthState(50f)), Is.True);
        }

        [Test]
        public void LongFrameAllowsOneAttackWithoutStoredBurst()
        {
            var weapon = new WeaponRuntime(10f, 1f, 3f);
            var target = new HealthState(100f);
            weapon.TryAttack(target);
            weapon.Tick(60f);
            Assert.That(weapon.TryAttack(target), Is.True);
            Assert.That(weapon.TryAttack(target), Is.False);
            Assert.That(target.Current, Is.EqualTo(80f));
        }

        [Test]
        public void HitCallbackCannotAttackRecursively()
        {
            var weapon = new WeaponRuntime(10f, 1f, 3f);
            var target = new HealthState(50f);
            target.Changed += () => Assert.That(weapon.TryAttack(target), Is.False);
            weapon.TryAttack(target);
            Assert.That(target.Current, Is.EqualTo(40f));
        }

        [Test]
        public void SeparateWeaponsDoNotShareCooldown()
        {
            var first = new WeaponRuntime(10f, 1f, 3f);
            var second = new WeaponRuntime(10f, 1f, 3f);
            var target = new HealthState(50f);
            first.TryAttack(target);
            Assert.That(second.TryAttack(target), Is.True);
        }

        [Test]
        public void PausedClockDoesNotAdvanceCooldown()
        {
            var weapon = new WeaponRuntime(10f, 1f, 3f);
            var target = new HealthState(50f);
            weapon.TryAttack(target);
            weapon.Tick(0f);
            Assert.That(weapon.TryAttack(target), Is.False);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDeltaTimeIsRejected(float value)
        {
            var weapon = new WeaponRuntime(10f, 1f, 3f);
            Assert.Throws<ArgumentOutOfRangeException>(() => weapon.Tick(value));
        }

        [Test]
        public void PrototypeBalanceKillsOnFifthHitAndThenStops()
        {
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            var target = new HealthState(50f);
            int hits = 0;
            int deaths = 0;
            target.Died += () => deaths++;
            for (int i = 0; i < 20; i++)
            {
                if (weapon.TryAttack(target))
                    hits++;
                weapon.Tick(0.8f);
            }
            Assert.That(hits, Is.EqualTo(5));
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(target.Current, Is.Zero);
        }
    }
}
