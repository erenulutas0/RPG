using System;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class EncounterTests
    {
        [Test]
        public void BeginStartsAFreshNumberedEncounter()
        {
            var progress = new EncounterProgress(1f);
            progress.Begin();
            progress.RecordHit();
            progress.Tick(0.5f, true);
            progress.Clear();

            progress.Begin();

            Assert.That(progress.EncounterNumber, Is.EqualTo(2));
            Assert.That(progress.HitsTaken, Is.Zero);
            Assert.That(progress.Elapsed, Is.Zero);
            Assert.That(progress.IsCleared, Is.False);
        }

        [Test]
        public void HitsAndElapsedCountOnlyWhileFightingAndClearFreezesTheClearTime()
        {
            var progress = new EncounterProgress(1f);
            progress.RecordHit();
            Assert.That(progress.Tick(1f, true), Is.False);
            Assert.That(progress.HitsTaken, Is.Zero, "Nothing counts before the first encounter begins.");

            progress.Begin();
            progress.RecordHit();
            progress.RecordHit();
            progress.Tick(0.75f, false);
            Assert.That(progress.Clear(), Is.True);
            progress.RecordHit();
            progress.Tick(5f, false);

            Assert.That(progress.HitsTaken, Is.EqualTo(2));
            Assert.That(progress.Elapsed, Is.EqualTo(0.75f));
        }

        [Test]
        public void ClearHappensOncePerEncounter()
        {
            var progress = new EncounterProgress(1f);
            Assert.That(progress.Clear(), Is.False);
            progress.Begin();
            Assert.That(progress.Clear(), Is.True);
            Assert.That(progress.Clear(), Is.False);
            progress.Begin();
            Assert.That(progress.Clear(), Is.True);
        }

        [Test]
        public void NextEncounterWaitsForDelayAndOnlyCountsTimeWhenAllowed()
        {
            var progress = new EncounterProgress(1f);
            progress.Begin();
            Assert.That(progress.Tick(10f, true), Is.False, "An uncleared encounter never advances.");
            progress.Clear();

            Assert.That(progress.Tick(5f, false), Is.False, "An open choice holds progression.");
            Assert.That(progress.Tick(0.6f, true), Is.False);
            Assert.That(progress.Tick(0.4f, true), Is.True);
        }

        [Test]
        public void ZeroDelayAdvancesOnTheFirstAllowedTick()
        {
            var progress = new EncounterProgress(0f);
            progress.Begin();
            progress.Clear();
            Assert.That(progress.Tick(0f, false), Is.False);
            Assert.That(progress.Tick(0f, true), Is.True);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDelayAndDeltaTimeAreRejected(float value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterProgress(value));
            var progress = new EncounterProgress(1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.Tick(value, true));
        }

        [Test]
        public void EarlierVictimCannotBeRewardedAgainInALaterEncounter()
        {
            var run = new RunState(1000);
            var rewards = new RewardService(run);
            var progress = new EncounterProgress(1f);
            var first = new HealthState(50f);
            var second = new HealthState(50f);
            first.Died += () => rewards.TryAwardKill(first, 10, 0);
            second.Died += () => rewards.TryAwardKill(second, 10, 0);

            progress.Begin();
            first.ApplyDamage(new DamageContext(50f));
            progress.Clear();
            progress.Begin();
            first.ApplyDamage(new DamageContext(50f));
            Assert.That(rewards.TryAwardKill(first, 10, 0), Is.False);
            second.ApplyDamage(new DamageContext(50f));

            Assert.That(run.Experience, Is.EqualTo(20));
        }

        [Test]
        public void DamageUpgradeKillsTheNextGruntInFourHits()
        {
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            Assert.That(SimulateFight(weapon, out _), Is.EqualTo(5));

            weapon.AddModifier(UpgradeStat.Damage, new StatModifier(ModifierOperation.Flat, 5f));
            Assert.That(SimulateFight(weapon, out float clearTime), Is.EqualTo(4));
            // Frame quantization can add up to one step per hit.
            Assert.That(clearTime, Is.EqualTo(2.4f).Within(0.1f));
        }

        [Test]
        public void AttackSpeedUpgradeClearsTheNextGruntSooner()
        {
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            SimulateFight(weapon, out float baseline);

            weapon.AddModifier(UpgradeStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, 0.25f));
            Assert.That(SimulateFight(weapon, out float upgraded), Is.EqualTo(5));
            Assert.That(baseline, Is.EqualTo(3.2f).Within(0.1f));
            Assert.That(upgraded, Is.EqualTo(2.56f).Within(0.1f));
            Assert.That(baseline - upgraded, Is.GreaterThan(0.5f));
        }

        // Fixed 60 Hz steps against a fresh 50 HP Grunt, mirroring AttackController's tick-then-attack order.
        private static int SimulateFight(WeaponRuntime weapon, out float clearTime)
        {
            const float step = 1f / 60f;
            var progress = new EncounterProgress(1f);
            var grunt = new HealthState(50f);
            grunt.Changed += progress.RecordHit;
            grunt.Died += () => progress.Clear();
            progress.Begin();
            weapon.Tick(10f);
            for (int frame = 0; frame < 600 && !progress.IsCleared; frame++)
            {
                if (frame > 0)
                {
                    weapon.Tick(step);
                    progress.Tick(step, true);
                }
                weapon.TryAttack(grunt);
            }

            clearTime = progress.Elapsed;
            return progress.HitsTaken;
        }
    }
}
