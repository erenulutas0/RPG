using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The stat sheet of docs/27 slice S2: what a card can change about the hero and its weapon beyond damage and
    // cadence. Each case proves the card reaches the thing that reads it, because a stat nothing reads is a card that
    // lies. Nothing here changes an authored number: the game's pool still holds two cards.
    public sealed class HeroStatsTests
    {
        private static readonly ArenaGeometry Platform = new ArenaGeometry(-9f, 9f, 9f);

        private static HeroStats Sheet() => new HeroStats(100f, 2.5f, HeroStamina.DefaultBar);

        private static UpgradeOption Card(UpgradeStat stat, ModifierOperation operation, float amount) =>
            new UpgradeOption("card_" + stat, stat.ToString(), "+{0:0}", stat, new StatModifier(operation, amount), 5);

        // A run whose next level-up offers exactly the card under test.
        private static UpgradeService Offering(UpgradeOption card, WeaponRuntime weapon, HeroStats stats)
        {
            var run = new RunState(10);
            var service = new UpgradeService(run, weapon, new[] { card }, 1, stats);
            run.AddExperience(10);
            return service;
        }

        private static void Take(UpgradeService service)
        {
            Assert.That(service.CurrentOffer, Is.Not.Null);
            Assert.That(service.TrySelect(service.CurrentOffer, 0), Is.True);
        }

        [Test]
        public void ARangeCardLengthensTheWeaponsReach()
        {
            var weapon = new WeaponRuntime(10f, 0.8f, 2.1f);
            Take(Offering(Card(UpgradeStat.Range, ModifierOperation.Flat, 0.4f), weapon, Sheet()));
            Assert.That(weapon.Range, Is.EqualTo(2.5f).Within(1e-5f));
        }

        [Test]
        public void AMaximumHealthCardRaisesTheCeilingAndArrivesFilled()
        {
            var health = new HealthState(100f);
            HeroStats stats = Sheet();
            health.ApplyDamage(new DamageContext(30f));
            Assert.That(health.Current, Is.EqualTo(70f));

            Take(Offering(Card(UpgradeStat.MaxHealth, ModifierOperation.Flat, 25f), new WeaponRuntime(10f, 1f, 2f), stats));
            HeroStatsBinding.Apply(stats, health, null, null);

            Assert.That(health.Maximum, Is.EqualTo(125f));
            Assert.That(health.Current, Is.EqualTo(95f), "The room the card added arrives filled.");
        }

        [Test]
        public void ArmorTakesAShareOfEveryHitAndNeverAllOfIt()
        {
            var health = new HealthState(100f);
            HeroStats stats = Sheet();
            Assert.That(stats.DamageReduction, Is.Zero, "A hero starts with no armor.");

            Take(Offering(Card(UpgradeStat.Armor, ModifierOperation.Flat, 100f), new WeaponRuntime(10f, 1f, 2f), stats));
            HeroStatsBinding.Apply(stats, health, null, null);

            // 100 armor against a softness of 100 is half of every hit.
            Assert.That(stats.DamageReduction, Is.EqualTo(0.5f).Within(1e-5f));
            health.ApplyDamage(new DamageContext(40f));
            Assert.That(health.Current, Is.EqualTo(80f));

            var mountain = new HeroStats(100f, 2.5f, 2f, 1_000_000f);
            Assert.That(mountain.DamageReduction, Is.LessThan(1f), "No pile of armor stops a hit entirely.");
        }

        [Test]
        public void TheDamageEveryoneObservesIsTheDamageThatLanded()
        {
            var health = new HealthState(100f);
            health.SetDamageReduction(0.25f);
            float observed = 0f;
            health.Damaged += context => observed = context.Amount;

            health.ApplyDamage(new DamageContext(40f));
            Assert.That(observed, Is.EqualTo(30f), "A view or a relic must not be told about damage armor stopped.");
            Assert.That(health.Current, Is.EqualTo(70f));
        }

        [Test]
        public void AMoveSpeedCardWalksTheHeroFurtherInTheSameSecond()
        {
            var motion = new HeroMotion(2.5f);
            var reference = new HeroMotion(2.5f);
            motion.Place(0f, 0f, Platform);
            reference.Place(0f, 0f, Platform);
            HeroStats stats = Sheet();

            Take(Offering(Card(UpgradeStat.MoveSpeed, ModifierOperation.Percent, 0.2f), new WeaponRuntime(10f, 1f, 2f), stats));
            HeroStatsBinding.Apply(stats, null, motion, null);
            Assert.That(motion.Speed, Is.EqualTo(3f).Within(1e-5f));

            for (int frame = 0; frame < 60; frame++)
            {
                motion.Move(1f, 0f, 1f / 60f, Platform);
                reference.Move(1f, 0f, 1f / 60f, Platform);
            }
            Assert.That(motion.X, Is.EqualTo(reference.X * 1.2f).Within(1e-4f));
        }

        [Test]
        public void AStaminaCardLengthensTheBarAndWhatItAddsArrivesFilled()
        {
            var stamina = new HeroStamina();
            HeroStats stats = Sheet();
            stamina.Step(1f, true);
            Assert.That(stamina.Current, Is.EqualTo(1f).Within(1e-5f));

            Take(Offering(Card(UpgradeStat.StaminaBar, ModifierOperation.Flat, 1f), new WeaponRuntime(10f, 1f, 2f), stats));
            HeroStatsBinding.Apply(stats, null, null, stamina);

            Assert.That(stamina.Bar, Is.EqualTo(3f).Within(1e-5f));
            Assert.That(stamina.Current, Is.EqualTo(2f).Within(1e-5f));
        }

        [Test]
        public void ACritChanceCardCritsOnTheSameSwingsForTheSameSeed()
        {
            int CritsOf(int seed)
            {
                var weapon = new WeaponRuntime(10f, 0.01f, 2f) { Seed = seed };
                weapon.AddModifier(UpgradeStat.CritChance, new StatModifier(ModifierOperation.Flat, 0.5f));
                var target = new HealthState(1_000_000f);
                int crits = 0;
                for (int i = 0; i < 400; i++)
                {
                    float before = target.Current;
                    weapon.TryAttack(target);
                    if (before - target.Current > 10f)
                        crits++;
                    weapon.Tick(1f);
                }
                return crits;
            }

            Assert.That(CritsOf(11), Is.EqualTo(CritsOf(11)), "The same seed crits on the same swings.");
            Assert.That(CritsOf(11), Is.Not.EqualTo(CritsOf(12)));
            // Half of four hundred, give or take what four hundred draws can wander.
            Assert.That(CritsOf(11), Is.InRange(160, 240));
        }

        [Test]
        public void WithoutAChanceNothingIsRolledAndTheAuthoredRhythmIsUntouched()
        {
            // The Daggers' pattern: every third strike crits for double, and no draw is ever taken.
            var weapon = new WeaponRuntime(6f, 0.01f, 1.8f, 0f, new AttackPattern(WeaponBehavior.DirectHit, 0f, 0f, 3, 2f));
            var target = new HealthState(1_000_000f);
            for (int i = 1; i <= 9; i++)
            {
                float before = target.Current;
                weapon.TryAttack(target);
                float dealt = before - target.Current;
                Assert.That(dealt, Is.EqualTo(i % 3 == 0 ? 12f : 6f).Within(1e-4f), $"strike {i}");
                weapon.Tick(1f);
            }
        }

        [Test]
        public void AHeroCardNeverWritesToTheWeaponAndAWeaponCardNeverWritesToTheHero()
        {
            HeroStats stats = Sheet();
            Assert.Throws<ArgumentOutOfRangeException>(() => stats.AddModifier(UpgradeStat.Damage, new StatModifier(ModifierOperation.Flat, 1f)));
            var weapon = new WeaponRuntime(10f, 1f, 2f);
            Assert.Throws<ArgumentOutOfRangeException>(() => weapon.AddModifier(UpgradeStat.MaxHealth, new StatModifier(ModifierOperation.Flat, 1f)));
            Assert.That(HeroStats.Owns(UpgradeStat.MaxHealth), Is.True);
            Assert.That(HeroStats.Owns(UpgradeStat.Range), Is.False);
        }

        [Test]
        public void ApplyingTheSameSheetTwiceChangesNothingTheSecondTime()
        {
            var health = new HealthState(100f);
            var motion = new HeroMotion(2.5f);
            var stamina = new HeroStamina();
            motion.Place(0f, 0f, Platform);
            HeroStats stats = Sheet();
            Take(Offering(Card(UpgradeStat.MaxHealth, ModifierOperation.Flat, 40f), new WeaponRuntime(10f, 1f, 2f), stats));

            HeroStatsBinding.Apply(stats, health, motion, stamina);
            float current = health.Current;
            HeroStatsBinding.Apply(stats, health, motion, stamina);
            HeroStatsBinding.Apply(stats, health, motion, stamina);

            Assert.That(health.Maximum, Is.EqualTo(140f));
            Assert.That(health.Current, Is.EqualTo(current), "Absolute values, so a second push adds nothing.");
        }

        [Test]
        public void SafetyFloorsHoldWhateverACardSubtracts()
        {
            HeroStats stats = Sheet();
            stats.AddModifier(UpgradeStat.MaxHealth, new StatModifier(ModifierOperation.Percent, -10f));
            stats.AddModifier(UpgradeStat.MoveSpeed, new StatModifier(ModifierOperation.Percent, -10f));
            stats.AddModifier(UpgradeStat.StaminaBar, new StatModifier(ModifierOperation.Percent, -10f));
            stats.AddModifier(UpgradeStat.Armor, new StatModifier(ModifierOperation.Flat, -500f));

            Assert.That(stats.MaxHealth, Is.EqualTo(1f));
            Assert.That(stats.MoveSpeed, Is.EqualTo(0.1f));
            Assert.That(stats.StaminaBar, Is.EqualTo(0.1f));
            Assert.That(stats.DamageReduction, Is.Zero);
        }
    }
}
