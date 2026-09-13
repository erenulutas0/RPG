using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class PackTests
    {
        [Test]
        public void PackSlotsPutTheFirstEnemyInTheCentreAndSpreadTheRest()
        {
            Assert.That(PackLayout.OffsetX(0, 1, 1.7f), Is.Zero);
            Assert.That(PackLayout.OffsetX(0, 2, 1.7f), Is.EqualTo(-0.85f).Within(1e-5f));
            Assert.That(PackLayout.OffsetX(1, 2, 1.7f), Is.EqualTo(0.85f).Within(1e-5f));
            Assert.That(PackLayout.OffsetX(0, 3, 1.7f), Is.Zero, "The first enemy is the nearest, so the hero fights it first.");
            Assert.That(PackLayout.OffsetX(1, 3, 1.7f), Is.EqualTo(-1.7f));
            Assert.That(PackLayout.OffsetX(2, 3, 1.7f), Is.EqualTo(1.7f));

            // Three abreast stay inside the 3-unit weapon range from the hero 2.4 units below the pack.
            Assert.That(Math.Sqrt(1.7 * 1.7 + 2.4 * 2.4), Is.LessThan(3.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => PackLayout.OffsetX(0, 4, 1.7f));
            Assert.Throws<ArgumentOutOfRangeException>(() => PackLayout.OffsetX(2, 2, 1.7f));
            Assert.Throws<ArgumentOutOfRangeException>(() => PackLayout.OffsetX(0, 1, 0f));
        }

        [Test]
        public void ExperienceGrowthMakesEachLevelCostMoreThanTheLast()
        {
            var run = new RunState(10, 0.5f, 1);
            int levelChanges = 0;
            run.LevelChanged += () => levelChanges++;

            run.AddExperience(20);
            Assert.That(run.Level, Is.EqualTo(1));
            Assert.That(run.ExperienceForCurrentLevel, Is.EqualTo(10));
            Assert.That(run.ExperienceForNextLevel, Is.EqualTo(21), "Level 2 costs 11 more.");

            run.AddExperience(13);
            Assert.That(run.Level, Is.EqualTo(3), "33 experience reaches level 3 in one step.");
            Assert.That(run.PendingUpgrades, Is.EqualTo(3));
            Assert.That(run.ExperienceForNextLevel, Is.EqualTo(46));
            Assert.That(levelChanges, Is.EqualTo(2));

            var linear = new RunState(10);
            linear.AddExperience(35);
            Assert.That(linear.Level, Is.EqualTo(3));
            Assert.That(linear.ExperienceForNextLevel, Is.EqualTo(40));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunState(10, 0.5f, -1));
        }

        [Test]
        public void HitsCarryTheirSourceAndDamagedFiresBeforeChangedAndDied()
        {
            var attacker = new HealthState(50f);
            var target = new HealthState(10f);
            var events = new List<string>();
            IDamageable seenSource = null;
            target.Damaged += context =>
            {
                events.Add($"damaged {context.Amount:0}");
                seenSource = context.Source;
            };
            target.Changed += () => events.Add("changed");
            target.Died += () => events.Add("died");
            target.Heal(5f);

            var weapon = new WeaponRuntime(10f, 1f, 3f);
            Assert.That(weapon.TryAttack(target, attacker), Is.True);

            Assert.That(events, Is.EqualTo(new[] { "damaged 10", "changed", "died" }));
            Assert.That(seenSource, Is.SameAs(attacker));
            Assert.That(new DamageContext(3f).Source, Is.Null);
        }

        [Test]
        public void CleaveStrikesTheNearestOtherLivingEnemyForItsFraction()
        {
            var sword = new WeaponRuntime(10f, 0.8f, 3f, 0f, new AttackPattern(WeaponBehavior.Cleave, 2f, 0.6f));
            var target = new HealthState(50f);
            var dead = new HealthState(15f);
            var near = new HealthState(15f);
            var far = new HealthState(15f);
            dead.ApplyDamage(new DamageContext(15f));

            Assert.That(sword.IsReady, Is.True);
            Assert.That(sword.TryAttack(target, null, new IDamageable[] { target, dead, near, far }), Is.True);

            Assert.That(target.Current, Is.EqualTo(40f));
            Assert.That(near.Current, Is.EqualTo(9f).Within(1e-4f), "60% of 10 lands on the first living neighbour only.");
            Assert.That(far.Current, Is.EqualTo(15f));
            Assert.That(sword.IsReady, Is.False);

            var direct = new WeaponRuntime(10f, 0.8f, 3f);
            var bystander = new HealthState(15f);
            direct.TryAttack(new HealthState(50f), null, new IDamageable[] { bystander });
            Assert.That(bystander.Current, Is.EqualTo(15f), "A direct hit ignores nearby enemies.");
        }

        [Test]
        public void WeaponBehaviourSettingsAreValidated()
        {
            Assert.Throws<ArgumentException>(() => new AttackPattern(WeaponBehavior.DirectHit, 2f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.Cleave, 0f, 0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.Cleave, 2f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.Area, 2f, 1.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.Area, float.NaN, 0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern((WeaponBehavior)99));
            Assert.That(new WeaponRuntime(10f, 1f, 3f).Pattern.Behavior, Is.EqualTo(WeaponBehavior.DirectHit), "The default pattern is a direct hit.");
        }

        [Test]
        public void TheSwordCleaveClearsAMitePackInFewerSwingsThanADirectHit()
        {
            // The centre mite's cleave reaches a side mite; the side mites stand 3.4 apart, beyond the 2-unit cleave.
            Assert.That(SwingsToClearMites(DescentSimulation.Sword().CreateRuntime()), Is.EqualTo(5));
            Assert.That(SwingsToClearMites(new WeaponRuntime(10f, 0.8f, 3f)), Is.EqualTo(6));
        }

        // Three 15 HP Cinder Mites in PackLayout slots; the hero strikes the first living one each swing.
        private static int SwingsToClearMites(WeaponRuntime weapon)
        {
            var mites = new[] { new HealthState(15f), new HealthState(15f), new HealthState(15f) };
            int swings = 0;
            while (Array.Exists(mites, mite => mite.IsAlive) && swings < 50)
            {
                weapon.Tick(10f);
                int target = Array.FindIndex(mites, mite => mite.IsAlive);
                weapon.TryAttack(mites[target], null, DescentSimulation.Nearby(mites, target, weapon.Pattern.SplashRadius));
                swings++;
            }
            return swings;
        }
    }
}
