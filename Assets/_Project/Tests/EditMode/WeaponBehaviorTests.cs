using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class WeaponBehaviorTests
    {
        private static readonly DescentSimulation.Floor[] Descent = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };

        [Test]
        public void AreaStrikesEveryOtherLivingEnemyNearTheTargetForItsFraction()
        {
            WeaponRuntime staff = DescentSimulation.Staff().CreateRuntime();
            var hero = new HealthState(100f);
            var target = new HealthState(50f);
            var dead = new HealthState(15f);
            var left = new HealthState(15f);
            var right = new HealthState(40f);
            dead.ApplyDamage(new DamageContext(15f));
            var sources = new List<IDamageable>();
            right.Damaged += context => sources.Add(context.Source);

            Assert.That(staff.TryAttack(target, hero, new IDamageable[] { target, dead, left, right }), Is.True);

            Assert.That(target.Current, Is.EqualTo(40f));
            Assert.That(left.Current, Is.EqualTo(5f).Within(1e-4f), "The full 10 lands on every living neighbour.");
            Assert.That(right.Current, Is.EqualTo(30f).Within(1e-4f));
            Assert.That(sources, Is.EqualTo(new[] { hero }), "Splash hits carry the attacker too.");
        }

        [Test]
        public void EveryThirdDaggerStrikeCritsForDoubleDamage()
        {
            WeaponRuntime daggers = DescentSimulation.Daggers().CreateRuntime();
            var grunt = new HealthState(100f);
            var hits = new List<string>();
            grunt.Damaged += context => hits.Add(context.IsCritical ? $"crit {context.Amount:0}" : $"{context.Amount:0}");

            for (int i = 0; i < 6; i++)
            {
                daggers.Tick(10f);
                daggers.TryAttack(grunt);
            }

            Assert.That(hits, Is.EqualTo(new[] { "6", "6", "crit 12", "6", "6", "crit 12" }));
            Assert.That(daggers.AttacksMade, Is.EqualTo(6));
            Assert.That(grunt.Current, Is.EqualTo(52f));

            daggers.Tick(10f);
            Assert.That(daggers.TryAttack(null), Is.False);
            Assert.That(daggers.AttacksMade, Is.EqualTo(6), "An attack without a target does not advance the rhythm.");
        }

        [Test]
        public void ACriticalAttackMultipliesItsSplashToo()
        {
            var weapon = new WeaponRuntime(10f, 1f, 3f, 0f, new AttackPattern(WeaponBehavior.Area, 2f, 0.5f, 1, 3f));
            var target = new HealthState(100f);
            var near = new HealthState(100f);
            bool nearWasCritical = false;
            near.Damaged += context => nearWasCritical = context.IsCritical;

            weapon.TryAttack(target, null, new IDamageable[] { near });

            Assert.That(target.Current, Is.EqualTo(70f));
            Assert.That(near.Current, Is.EqualTo(85f), "Half of the 30 critical damage.");
            Assert.That(nearWasCritical, Is.True);
        }

        [Test]
        public void CritSettingsAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.DirectHit, critEvery: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.DirectHit, critEvery: 3, critMultiplier: 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.DirectHit, critEvery: 3, critMultiplier: float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.DirectHit, critEvery: 0, critMultiplier: 2f),
                "Without crits the multiplier stays 0.");
            Assert.That(new AttackPattern(WeaponBehavior.DirectHit).IsCritical(3), Is.False);
            Assert.That(new AttackPattern(WeaponBehavior.DirectHit, critEvery: 3, critMultiplier: 2f).IsCritical(6), Is.True);
            Assert.That(new AttackPattern(WeaponBehavior.DirectHit, critEvery: 3, critMultiplier: 2f).IsCritical(4), Is.False);
        }

        [Test]
        public void StaffClearsMitePacksFastestAndDaggersFellTheWardensFastest()
        {
            DescentSimulation.Result sword = DescentSimulation.Run(Descent, 0, true, heroWeapon: DescentSimulation.Sword());
            DescentSimulation.Result staff = DescentSimulation.Run(Descent, 0, true, heroWeapon: DescentSimulation.Staff());
            DescentSimulation.Result daggers = DescentSimulation.Run(Descent, 0, true, heroWeapon: DescentSimulation.Daggers());

            Assert.That(staff.MitePackFightSeconds, Is.LessThan(sword.MitePackFightSeconds));
            Assert.That(sword.MitePackFightSeconds, Is.LessThan(daggers.MitePackFightSeconds));
            Assert.That(daggers.BossFightSeconds, Is.LessThan(sword.BossFightSeconds));
            Assert.That(sword.BossFightSeconds, Is.LessThan(staff.BossFightSeconds));
        }

        // Unlocked weapons are sidegrades: each survives the mending descents near the Sword's health, none carries a greedy
        // Temper descent alone, and both relics still rescue damage-first Temper.
        [Test]
        public void EveryWeaponSurvivesTheMendingDescentButNeedsARelicToCarryGreed()
        {
            DescentSimulation.Result swordDamage = DescentSimulation.Run(Descent, 0, true);
            DescentSimulation.Result swordSpeed = DescentSimulation.Run(Descent, 1, true);
            var weapons = new[] { ("Sword", DescentSimulation.Sword()), ("Staff", DescentSimulation.Staff()), ("Daggers", DescentSimulation.Daggers()) };
            foreach (var (name, weapon) in weapons)
            {
                DescentSimulation.Result damageMend = DescentSimulation.Run(Descent, 0, true, heroWeapon: weapon);
                DescentSimulation.Result speedMend = DescentSimulation.Run(Descent, 1, true, heroWeapon: weapon);
                Assert.That(damageMend.ClearedFloors, Is.EqualTo(2), $"{name}: damage first with Mend");
                Assert.That(speedMend.ClearedFloors, Is.EqualTo(2), $"{name}: speed first with Mend");
                Assert.That(damageMend.HeroHealth, Is.EqualTo(swordDamage.HeroHealth).Within(15f), $"{name}: damage first health");
                Assert.That(speedMend.HeroHealth, Is.EqualTo(swordSpeed.HeroHealth).Within(15f), $"{name}: speed first health");

                Assert.That(DescentSimulation.Run(Descent, 0, false, heroWeapon: weapon).ClearedFloors, Is.EqualTo(1), $"{name}: damage first with Temper");
                Assert.That(DescentSimulation.Run(Descent, 1, false, heroWeapon: weapon).ClearedFloors, Is.EqualTo(1), $"{name}: speed first with Temper");
                Assert.That(DescentSimulation.Run(Descent, 0, false, RelicForgeTests.Counterweight(), weapon).ClearedFloors, Is.EqualTo(2),
                    $"{name}: damage first with Temper and Counterweight");
                Assert.That(DescentSimulation.Run(Descent, 0, false, RelicForgeTests.SecondWind(), weapon).ClearedFloors, Is.EqualTo(2),
                    $"{name}: damage first with Temper and Second Wind");
            }
        }
    }
}
