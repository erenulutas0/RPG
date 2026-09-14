using System;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class FloorTests
    {
        private static UpgradeOption Damage() =>
            new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0} damage per hit", WeaponStat.Damage,
                new StatModifier(ModifierOperation.Flat, 5f), 5);

        private static UpgradeOption Speed() =>
            new UpgradeOption("upgrade_attack_speed", "Quickened Grip", "+{0:0}% attack speed", WeaponStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.5f), 5);

        private static ForgeOption Mend() => new ForgeOption("forge_mend", "Mend", "Restore {0:0}% health", ForgeEffect.Heal, 0.4f);

        private static ForgeOption Temper() =>
            new ForgeOption("forge_temper", "Temper", "Gain {0:0} extra upgrade choice", ForgeEffect.BonusUpgrade, 1f);

        [Test]
        public void FloorWalksWavesThenRoomsAndReportsTheFinalWave()
        {
            var floor = new FloorProgress(new[] { 2, 0, 1 });

            AssertStep(floor.Advance(), FloorStepKind.Wave, 0, 0);
            Assert.That(floor.IsFinalWave, Is.False);
            AssertStep(floor.Advance(), FloorStepKind.Wave, 0, 1);
            Assert.That(floor.RoomsCleared, Is.Zero);
            AssertStep(floor.Advance(), FloorStepKind.NonCombatRoom, 1, -1);
            Assert.That(floor.RoomsCleared, Is.EqualTo(1));
            AssertStep(floor.Advance(), FloorStepKind.Wave, 2, 0);
            Assert.That(floor.IsFinalWave, Is.True);
            Assert.That(floor.RoomsCleared, Is.EqualTo(2));

            AssertStep(floor.Advance(), FloorStepKind.Cleared, 2, -1);
            Assert.That(floor.IsComplete, Is.True);
            Assert.That(floor.RoomsCleared, Is.EqualTo(3));
            Assert.That(floor.IsFinalWave, Is.False);
            AssertStep(floor.Advance(), FloorStepKind.Cleared, 2, -1);
            Assert.That(floor.RoomsCleared, Is.EqualTo(3), "Advancing a cleared floor changes nothing.");
        }

        [Test]
        public void FloorRejectsEmptyOrNegativeLayouts()
        {
            Assert.Throws<ArgumentException>(() => new FloorProgress(new int[0]));
            Assert.Throws<ArgumentException>(() => new FloorProgress(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FloorProgress(new[] { 1, -1 }));
        }

        [Test]
        public void HealRestoresUpToMaximumAndNeverRevives()
        {
            var health = new HealthState(100f);
            int changes = 0;
            health.Changed += () => changes++;
            health.ApplyDamage(new DamageContext(70f));

            health.Heal(40f);
            Assert.That(health.Current, Is.EqualTo(70f));
            health.Heal(500f);
            Assert.That(health.Current, Is.EqualTo(100f));
            health.Heal(10f);
            Assert.That(changes, Is.EqualTo(3), "Damage and two effective heals; healing at full health is silent.");

            health.ApplyDamage(new DamageContext(1000f));
            health.Heal(50f);
            Assert.That(health.IsAlive, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => health.Heal(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.Heal(float.NaN));
        }

        [Test]
        public void RunOutcomeIsRecordedOnceAndNoneIsRejected()
        {
            var run = new RunState(10);
            run.End(RunOutcome.Victory);
            run.End(RunOutcome.Defeat);

            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(run.HasEnded, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunState(10).End(RunOutcome.None));
        }

        [Test]
        public void BonusUpgradesAddPendingChoicesWithoutLevelling()
        {
            var run = new RunState(10);
            int pendingChanges = 0;
            int levelChanges = 0;
            run.PendingUpgradesChanged += () => pendingChanges++;
            run.LevelChanged += () => levelChanges++;

            run.GrantBonusUpgrade();
            run.AddExperience(10);

            Assert.That(run.PendingUpgrades, Is.EqualTo(2));
            Assert.That(run.Level, Is.EqualTo(1));
            Assert.That(pendingChanges, Is.EqualTo(2));
            Assert.That(levelChanges, Is.EqualTo(1));
            run.End(RunOutcome.Defeat);
            run.GrantBonusUpgrade();
            Assert.That(run.BonusUpgrades, Is.EqualTo(1), "An ended run grants nothing.");
        }

        [Test]
        public void MendHealsAFractionOfMaximumExactlyOnce()
        {
            var run = new RunState(10);
            var hero = new HealthState(100f);
            var forge = new ForgeService(run, hero);
            hero.ApplyDamage(new DamageContext(70f));
            Assert.That(forge.Open(new[] { Mend(), Temper() }), Is.True);
            ForgeOffer offer = forge.CurrentOffer;

            Assert.That(forge.TrySelect(offer, 0), Is.True);
            Assert.That(forge.TrySelect(offer, 0), Is.False);
            Assert.That(forge.TrySelect(offer, 1), Is.False);

            Assert.That(hero.Current, Is.EqualTo(70f));
            Assert.That(run.PendingUpgrades, Is.Zero);
            Assert.That(forge.CurrentOffer, Is.Null);
        }

        [Test]
        public void TemperGrantsOneExtraUpgradeOffer()
        {
            var run = new RunState(10);
            var hero = new HealthState(100f);
            var upgrades = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f), new[] { Damage(), Speed() }, 2);
            var forge = new ForgeService(run, hero);
            forge.Open(new[] { Mend(), Temper() });

            forge.TrySelect(forge.CurrentOffer, 1);

            Assert.That(upgrades.CurrentOffer, Is.Not.Null);
            Assert.That(run.PendingUpgrades, Is.EqualTo(1));
            Assert.That(hero.Current, Is.EqualTo(100f));
        }

        [Test]
        public void ForgeRejectsSecondVisitsEndedRunsAndInvalidOptions()
        {
            var run = new RunState(10);
            var forge = new ForgeService(run, new HealthState(100f));
            Assert.That(forge.Open(new[] { Mend() }), Is.True);
            Assert.That(forge.Open(new[] { Temper() }), Is.False, "One visit at a time.");
            Assert.That(forge.TrySelect(forge.CurrentOffer, 5), Is.False);

            int changes = 0;
            forge.OfferChanged += () => changes++;
            run.End(RunOutcome.Defeat);
            Assert.That(forge.CurrentOffer, Is.Null);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(forge.Open(new[] { Mend() }), Is.False);

            Assert.Throws<ArgumentException>(() => forge.Open(new ForgeOption[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ForgeOption("x", "", "", ForgeEffect.Heal, 1.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ForgeOption("x", "", "", ForgeEffect.BonusUpgrade, 0.5f));
        }

        [Test]
        public void RunChoicesShowsUpgradeAndForgePromptsAndRoutesSelections()
        {
            var run = new RunState(10);
            var hero = new HealthState(100f);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            var upgrades = new UpgradeService(run, weapon, new[] { Damage(), Speed() }, 2);
            var forge = new ForgeService(run, hero);
            var choices = new RunChoices(upgrades, forge);
            int changes = 0;
            choices.Changed += () => changes++;
            Assert.That(choices.IsOpen, Is.False);

            run.AddExperience(10);
            ChoicePrompt upgradePrompt = choices.Current;
            Assert.That(upgradePrompt.Kind, Is.EqualTo(ChoiceKind.Upgrade));
            Assert.That(upgradePrompt.Cards[1].Name, Is.EqualTo("Quickened Grip"));
            Assert.That(upgradePrompt.Cards[1].Description, Is.EqualTo(string.Format("+{0:0}% attack speed", 50f)));
            Assert.That(choices.TrySelect(upgradePrompt, 1), Is.True);
            Assert.That(choices.TrySelect(upgradePrompt, 0), Is.False, "A stale prompt is rejected.");
            Assert.That(weapon.Interval, Is.EqualTo(0.8f / 1.5f).Within(1e-5f));
            Assert.That(choices.IsOpen, Is.False);

            forge.Open(new[] { Mend(), Temper() });
            ChoicePrompt forgePrompt = choices.Current;
            Assert.That(forgePrompt.Kind, Is.EqualTo(ChoiceKind.Forge));
            Assert.That(forgePrompt.Cards[0].Description, Is.EqualTo(string.Format("Restore {0:0}% health", 40f)));
            Assert.That(choices.TrySelect(forgePrompt, 1), Is.True);
            Assert.That(choices.Current.Kind, Is.EqualTo(ChoiceKind.Upgrade), "Temper hands straight over to the bonus upgrade.");
            Assert.That(changes, Is.EqualTo(4));
        }

        [Test]
        public void EnrageTriggersOnceAtTheThresholdButNotOnTheKillingBlow()
        {
            var rule = new EnrageRule(0.5f);
            Assert.That(rule.Evaluate(151f, 300f), Is.False);
            Assert.That(rule.Evaluate(150f, 300f), Is.True);
            Assert.That(rule.Evaluate(100f, 300f), Is.False);
            Assert.That(rule.IsEnraged, Is.True);

            var lethal = new EnrageRule(0.5f);
            Assert.That(lethal.Evaluate(0f, 300f), Is.False);
            Assert.That(lethal.IsEnraged, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnrageRule(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnrageRule(1f));
        }

        [Test]
        public void AuthoredFloorIsClearableWithEitherCardAfterMending()
        {
            DescentSimulation.Floor[] floor = { DescentSimulation.EmberHalls };
            DescentSimulation.Result damageFirst = DescentSimulation.Run(floor, 0, true);
            DescentSimulation.Result speedFirst = DescentSimulation.Run(floor, 1, true);

            Assert.That(damageFirst.ClearedFloors, Is.EqualTo(1), $"Damage first died in {damageFirst.DeathRoom}.");
            Assert.That(speedFirst.ClearedFloors, Is.EqualTo(1), $"Speed first died in {speedFirst.DeathRoom}.");
            Assert.That(damageFirst.HeroHealth, Is.InRange(20f, 75f), "The packs and the Warden should threaten a mended hero.");
            Assert.That(speedFirst.HeroHealth, Is.InRange(15f, 60f));
            Assert.That(damageFirst.Kills, Is.EqualTo(31), "Ember Halls fields ten named enemies and twenty-one Cinder Mites.");
            Assert.That(damageFirst.Gold, Is.EqualTo(116), "Only the named enemies pay gold.");
        }

        [Test]
        public void TemperTradesTheHealForPowerAndLeavesLessHealth()
        {
            DescentSimulation.Floor[] floor = { DescentSimulation.EmberHalls };
            DescentSimulation.Result mend = DescentSimulation.Run(floor, 0, true);
            DescentSimulation.Result temper = DescentSimulation.Run(floor, 0, false);

            Assert.That(temper.ClearedFloors == 1 ? temper.HeroHealth : 0f, Is.LessThan(mend.HeroHealth));
            Assert.That(temper.UpgradesApplied, Is.EqualTo(mend.UpgradesApplied + 1));
        }

        private static void AssertStep(FloorStep step, FloorStepKind kind, int room, int wave)
        {
            Assert.That(step.Kind, Is.EqualTo(kind));
            Assert.That(step.RoomIndex, Is.EqualTo(room));
            Assert.That(step.WaveIndex, Is.EqualTo(wave));
        }
    }
}