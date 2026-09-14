using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class AbilityTests
    {
        private static readonly DescentSimulation.Floor[] Descent = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };

        [Test]
        public void TheBurstStrikesEveryLivingTargetOnceThenCoolsDown()
        {
            var burst = new AbilityRuntime(20f, 2.5f, 8f);
            var hero = new HealthState(100f);
            var grunt = new HealthState(50f);
            var mite = new HealthState(15f);
            var fallen = new HealthState(15f);
            fallen.ApplyDamage(new DamageContext(15f));
            IDamageable seenSource = null;
            grunt.Damaged += context => seenSource = context.Source;

            Assert.That(burst.IsReady, Is.True, "The burst starts ready.");
            Assert.That(burst.TryUse(hero, new IDamageable[] { grunt, mite, fallen }), Is.True);

            Assert.That(grunt.Current, Is.EqualTo(30f));
            Assert.That(mite.IsAlive, Is.False, "20 damage fells a 15 HP mite.");
            Assert.That(fallen.Current, Is.Zero);
            Assert.That(seenSource, Is.SameAs(hero), "The hit carries the hero as its source, for relics and the defeat cause.");
            Assert.That(burst.Remaining, Is.EqualTo(8f));
            Assert.That(burst.UsesMade, Is.EqualTo(1));
            Assert.That(burst.TryUse(hero, new IDamageable[] { grunt }), Is.False, "Nothing fires while cooling down.");
            Assert.That(grunt.Current, Is.EqualTo(30f));

            burst.Tick(7.9f);
            Assert.That(burst.IsReady, Is.False);
            burst.Tick(0.2f);
            Assert.That(burst.IsReady, Is.True);
            Assert.That(burst.TryUse(hero, Array.Empty<IDamageable>()), Is.True, "A burst into empty floor is still spent.");
            Assert.That(burst.UsesMade, Is.EqualTo(2));
        }

        [Test]
        public void AbilitySettingsAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AbilityRuntime(0f, 2.5f, 8f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AbilityRuntime(20f, float.NaN, 8f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AbilityRuntime(20f, 2.5f, 0f));
            var burst = new AbilityRuntime(20f, 2.5f, 8f);
            Assert.Throws<ArgumentOutOfRangeException>(() => burst.Tick(-1f));
            Assert.Throws<ArgumentNullException>(() => burst.TryUse(null, null));
        }

        // The burst is the player's edge, not the balance: fired whenever ready with an enemy in reach it spares health on
        // every weapon's mending Descent, yet a greedy speed-first Temper Descent still falls on floor 2.
        [Test]
        public void TheForgeBurstSparesHealthOnEveryWeaponWithoutCarryingGreed()
        {
            var weapons = new[] { ("Sword", DescentSimulation.Sword()), ("Staff", DescentSimulation.Staff()), ("Daggers", DescentSimulation.Daggers()) };
            foreach (var (name, weapon) in weapons)
            {
                DescentSimulation.Result plain = DescentSimulation.Run(Descent, 0, true, heroWeapon: weapon);
                DescentSimulation.Result burst = DescentSimulation.Run(Descent, 0, true, heroWeapon: weapon, ability: DescentSimulation.ForgeBurst());
                Assert.That(burst.AbilityUses, Is.GreaterThanOrEqualTo(6), $"{name}: the burst fires through the Descent.");
                Assert.That(burst.ClearedFloors, Is.EqualTo(2), name);
                Assert.That(burst.HeroHealth, Is.GreaterThan(plain.HeroHealth), $"{name}: the burst spares health.");
                Assert.That(burst.FightSeconds, Is.LessThan(plain.FightSeconds), $"{name}: the burst shortens the fights.");
                Assert.That(plain.AbilityUses, Is.Zero);

                DescentSimulation.Result greedy = DescentSimulation.Run(Descent, 1, false, heroWeapon: weapon, ability: DescentSimulation.ForgeBurst());
                Assert.That(greedy.ClearedFloors, Is.EqualTo(1), $"{name}: speed first with Temper still falls on floor 2, burst or not.");
            }
        }
    }
}
