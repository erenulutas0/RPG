using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Mirrors the authored data: Runner 30 HP 2/0.4s, Tank 120 HP 12/2.5s with a 1.5s windup. Floor-level balance,
    // including the Captain and the Warden, is covered by FloorTests. Update these fixtures when the assets change.
    public sealed class EnemyArchetypeTests
    {
        private sealed class EnemyStats
        {
            public float Health;
            public float Damage;
            public float Interval;
            public float InitialDelay;
        }

        private static readonly EnemyStats Runner = new EnemyStats { Health = 30f, Damage = 2f, Interval = 0.4f };
        private static readonly EnemyStats Tank = new EnemyStats { Health = 120f, Damage = 12f, Interval = 2.5f, InitialDelay = 1.5f };

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
    }
}
