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
        public void PackSlotsFillTheFrontFirstAndStayInsideTheWidestSpread()
        {
            for (int count = 1; count <= PackLayout.MaxPackSize; count++)
            {
                float previousDepth = 0f;
                float sumX = 0f;
                for (int index = 0; index < count; index++)
                {
                    PackLayout.Offset(index, count, 1f, out float x, out float y);
                    Assert.That(Math.Abs(x), Is.LessThanOrEqualTo(PackLayout.HalfWidth), $"{count} enemies, slot {index}");
                    Assert.That(y, Is.GreaterThanOrEqualTo(previousDepth), $"{count} enemies: slot {index} is not in front of an earlier one");
                    previousDepth = y;
                    sumX += x;
                }
                PackLayout.Offset(0, count, 1f, out _, out float frontDepth);
                Assert.That(frontDepth, Is.Zero, "The first slot is at the front.");
                Assert.That(sumX, Is.EqualTo(0f).Within(1e-4f), $"{count} enemies stay centred.");
            }

            PackLayout.Offset(1, 3, 1.5f, out float scaledX, out float scaledY);
            Assert.That(scaledX, Is.EqualTo(-2.1f).Within(1e-5f), "Offsets scale with the spacing.");
            Assert.That(scaledY, Is.EqualTo(1.2f).Within(1e-5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => PackLayout.Offset(0, PackLayout.MaxPackSize + 1, 1f, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => PackLayout.Offset(2, 2, 1f, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => PackLayout.Offset(0, 1, 0f, out _, out _));
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
            int cleave = SwingsToClearMites(DescentSimulation.Sword().CreateRuntime());
            int direct = SwingsToClearMites(new WeaponRuntime(10f, 0.8f, 1.8f));
            Assert.That(direct, Is.EqualTo(6), "Two 10-damage hits per 15 HP mite.");
            Assert.That(cleave, Is.LessThan(direct), "Mites standing side by side at the hero share the cleave.");
        }

        // Three 15 HP Cinder Mites walked in to the hero; each swing strikes the nearest living one in reach.
        private static int SwingsToClearMites(WeaponRuntime weapon)
        {
            var mites = new[] { new HealthState(15f), new HealthState(15f), new HealthState(15f) };
            var motion = new PackMotion(DescentSimulation.BodySpacing, PackLayout.HalfWidth);
            for (int i = 0; i < mites.Length; i++)
            {
                PackLayout.Offset(i, mites.Length, 1f, out float x, out float y);
                motion.Add(mites[i], x, DescentSimulation.EntryDepth + y, DescentSimulation.Mite.Speed, DescentSimulation.Mite.Reach);
            }
            for (int frame = 0; frame < 600; frame++)
                motion.Step(1f / 60f);

            int swings = 0;
            while (Array.Exists(mites, mite => mite.IsAlive) && swings < 50)
            {
                weapon.Tick(10f);
                int target = DescentSimulation.Acquire(motion, mites, weapon.Range);
                Assert.That(target, Is.GreaterThanOrEqualTo(0), "Every mite waits within the hero's reach.");
                weapon.TryAttack(mites[target], null, DescentSimulation.Nearby(motion, mites, target, weapon.Pattern.SplashRadius));
                swings++;
                motion.Step(1f / 60f);
            }
            return swings;
        }
    }
}
