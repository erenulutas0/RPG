using System;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Mirrors the authored data: Grunt 50 HP 6/1.0s, Runner 30 HP 2/0.4s, Tank 120 HP 12/2.5s with a 1.5s windup,
    // in the repeating order Grunt, Runner, Grunt, Tank; upgrades Tempered Edge +5 damage and Quickened Grip +50%
    // attack speed, five stacks each. Update these fixtures when the assets change.
    public sealed class EnemyArchetypeTests
    {
        private sealed class EnemyStats
        {
            public string Name;
            public float Health;
            public float Damage;
            public float Interval;
            public float InitialDelay;
        }

        private static readonly EnemyStats Grunt = new EnemyStats { Name = "Grunt", Health = 50f, Damage = 6f, Interval = 1f };
        private static readonly EnemyStats Runner = new EnemyStats { Name = "Runner", Health = 30f, Damage = 2f, Interval = 0.4f };
        private static readonly EnemyStats Tank = new EnemyStats { Name = "Tank", Health = 120f, Damage = 12f, Interval = 2.5f, InitialDelay = 1.5f };
        private static readonly EnemyStats[] Sequence = { Grunt, Runner, Grunt, Tank };

        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(4, 3)]
        [TestCase(5, 0)]
        [TestCase(8, 3)]
        public void EncounterScheduleRepeatsTheAuthoredOrder(int encounterNumber, int expectedIndex)
        {
            Assert.That(EncounterSchedule.IndexFor(encounterNumber, 4), Is.EqualTo(expectedIndex));
        }

        [Test]
        public void EncounterScheduleRejectsInvalidInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EncounterSchedule.IndexFor(0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => EncounterSchedule.IndexFor(1, 0));
        }

        [Test]
        public void InitialDelayHoldsOnlyTheFirstAttack()
        {
            var weapon = new WeaponRuntime(12f, 2.5f, 3f, 1.5f);
            var target = new HealthState(100f);

            Assert.That(weapon.TryAttack(target), Is.False);
            weapon.Tick(1.4f);
            Assert.That(weapon.TryAttack(target), Is.False);
            // Slightly past the windup; an exact 0.1 step leaves float residue above zero.
            weapon.Tick(0.11f);
            Assert.That(weapon.TryAttack(target), Is.True);
            weapon.Tick(1.5f);
            Assert.That(weapon.TryAttack(target), Is.False, "Later attacks use the full interval.");
            Assert.That(target.Current, Is.EqualTo(88f));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidInitialDelayIsRejected(float value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponRuntime(1f, 1f, 1f, value));
        }

        [Test]
        public void RunnerDiesQuicklyButChipsTheHeroSeveralTimes()
        {
            var hero = new HealthState(100f);
            FightResult result = Fight(hero, new WeaponRuntime(10f, 0.8f, 3f), Runner);

            Assert.That(result.HeroHits, Is.EqualTo(3));
            Assert.That(result.Duration, Is.EqualTo(1.6f).Within(0.1f));
            // Strikes every 0.4 s land until the third Sword hit at about 1.6 s; frame quantization decides
            // whether the strike due at 1.6 s lands first.
            Assert.That(result.EnemyHits, Is.InRange(4, 5));
            Assert.That(hero.Current, Is.EqualTo(100f - 2f * result.EnemyHits));
        }

        [Test]
        public void TankWindupLetsTheHeroStrikeFirstButTheFightIsLongAndHeavy()
        {
            var hero = new HealthState(100f);
            FightResult result = Fight(hero, new WeaponRuntime(10f, 0.8f, 3f), Tank);

            Assert.That(result.HeroHits, Is.EqualTo(12));
            Assert.That(result.Duration, Is.EqualTo(8.8f).Within(0.25f));
            Assert.That(result.FirstEnemyHitTime, Is.EqualTo(1.5f).Within(0.05f));
            Assert.That(result.EnemyHits, Is.EqualTo(3), "Slams land at 1.5, 4.0 and 6.5 s; the next would come at 9.0 s, after the twelfth Sword hit.");
            Assert.That(hero.Current, Is.EqualTo(64f));
        }

        [Test]
        public void MixedSequenceRunEndsInDeathAndLastsLongerThanGruntsAlone()
        {
            int mixed = SimulateRun(Sequence, 0);
            int gruntsOnly = SimulateRun(new[] { Grunt }, 0);

            Assert.That(mixed, Is.InRange(8, 30));
            Assert.That(mixed, Is.GreaterThan(gruntsOnly));
        }

        [Test]
        public void EitherFirstPickSurvivesAComparableNumberOfEncounters()
        {
            int damageFirst = SimulateRun(Sequence, 0);
            int speedFirst = SimulateRun(Sequence, 1);

            // At +25% the speed-first run died in encounter 7 against 15 for damage first; neither card may be a trap.
            Assert.That(speedFirst, Is.GreaterThanOrEqualTo(10));
            Assert.That(speedFirst, Is.GreaterThanOrEqualTo(damageFirst * 0.7f),
                $"Damage first died in encounter {damageFirst}, speed first in {speedFirst}.");
        }

        private struct FightResult
        {
            public int HeroHits;
            public int EnemyHits;
            public float Duration;
            public float FirstEnemyHitTime;
        }

        // Fixed 60 Hz steps; the hero acts first each frame, matching the other simulations in this suite.
        private static FightResult Fight(HealthState hero, WeaponRuntime heroWeapon, EnemyStats stats)
        {
            const float step = 1f / 60f;
            var enemy = new HealthState(stats.Health);
            var enemyWeapon = new WeaponRuntime(stats.Damage, stats.Interval, 3f, stats.InitialDelay);
            var result = new FightResult { FirstEnemyHitTime = -1f };
            heroWeapon.Tick(10f);
            int frame = 0;
            for (; frame < 100000 && enemy.IsAlive && hero.IsAlive; frame++)
            {
                if (frame > 0)
                {
                    heroWeapon.Tick(step);
                    enemyWeapon.Tick(step);
                }
                if (heroWeapon.TryAttack(enemy))
                    result.HeroHits++;
                if (enemy.IsAlive && enemyWeapon.TryAttack(hero))
                {
                    if (result.EnemyHits == 0)
                        result.FirstEnemyHitTime = frame * step;
                    result.EnemyHits++;
                }
            }

            result.Duration = (frame - 1) * step;
            return result;
        }

        // Takes the given card slot at every choice (falling back to the only card left); returns the encounter the
        // hero died in.
        private static int SimulateRun(EnemyStats[] sequence, int pick)
        {
            var run = new RunState(10);
            var rewards = new RewardService(run, 10);
            var heroWeapon = new WeaponRuntime(10f, 0.8f, 3f);
            var damage = new UpgradeOption("upgrade_damage", "Tempered Edge", "", WeaponStat.Damage,
                new StatModifier(ModifierOperation.Flat, 5f), 5);
            var speed = new UpgradeOption("upgrade_attack_speed", "Quickened Grip", "", WeaponStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.5f), 5);
            var upgrades = new UpgradeService(run, heroWeapon, new[] { damage, speed }, 2);
            var hero = new HealthState(100f);

            for (int encounter = 1; encounter <= 200; encounter++)
            {
                EnemyStats stats = sequence[EncounterSchedule.IndexFor(encounter, sequence.Length)];
                var enemy = new HealthState(stats.Health);
                Fight(hero, heroWeapon, stats);
                if (!hero.IsAlive)
                    return encounter;

                enemy.ApplyDamage(new DamageContext(stats.Health));
                rewards.TryAwardKill(enemy);
                while (upgrades.CurrentOffer != null)
                    upgrades.TrySelect(upgrades.CurrentOffer, Math.Min(pick, upgrades.CurrentOffer.Choices.Count - 1));
            }

            return int.MaxValue;
        }
    }
}
