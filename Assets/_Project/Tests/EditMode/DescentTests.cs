using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class DescentTests
    {
        [Test]
        public void GoldAccumulatesAndCheckpointsSecureIt()
        {
            var run = new RunState(10);
            int changes = 0;
            run.GoldChanged += () => changes++;

            run.AddGold(30);
            run.SecureGold();
            run.AddGold(12);
            run.AddGold(0);

            Assert.That(run.Gold, Is.EqualTo(42));
            Assert.That(run.SecuredGold, Is.EqualTo(30));
            Assert.That(run.UnsecuredGold, Is.EqualTo(12));
            Assert.That(changes, Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.AddGold(-1));
        }

        [TestCase(RunOutcome.Defeat, 20, 45, 5)]
        [TestCase(RunOutcome.Extracted, 20, 50, 0)]
        [TestCase(RunOutcome.Victory, 20, 50, 0)]
        public void EndingSettlesGoldByOutcome(RunOutcome outcome, int unsecured, int banked, int lost)
        {
            var run = new RunState(10, 0.25f);
            run.AddGold(30);
            run.SecureGold();
            run.AddGold(unsecured);

            run.End(outcome);
            run.AddGold(100);

            Assert.That(run.GoldBanked, Is.EqualTo(banked));
            Assert.That(run.GoldLost, Is.EqualTo(lost));
            Assert.That(run.Gold, Is.EqualTo(50), "An ended run earns nothing more.");
        }

        [Test]
        public void DefeatLossRoundsDownAndInvalidLossFractionsAreRejected()
        {
            var run = new RunState(10, 0.5f);
            run.AddGold(7);
            run.End(RunOutcome.Defeat);
            Assert.That(run.GoldLost, Is.EqualTo(3));
            Assert.That(run.GoldBanked, Is.EqualTo(4));

            Assert.Throws<ArgumentOutOfRangeException>(() => new RunState(10, -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunState(10, 1.1f));
        }

        [Test]
        public void KillRewardsGoldOnceWithTheExperience()
        {
            var run = new RunState(10);
            var rewards = new RewardService(run);
            var victim = new HealthState(50f);
            victim.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(victim, 10, 8), Is.True);
            Assert.That(rewards.TryAwardKill(victim, 10, 8), Is.False);
            Assert.That(run.Gold, Is.EqualTo(8));
            Assert.That(run.Experience, Is.EqualTo(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => rewards.TryAwardKill(victim, 10, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => rewards.TryAwardKill(victim, -1, 0));
        }

        [Test]
        public void FloorScalingAppliesTierThenModifier()
        {
            Assert.That(FloorScaling.Health(50f, 1.4f, 0f), Is.EqualTo(70f).Within(1e-3f));
            Assert.That(FloorScaling.DamageBonus(1.25f, 0.2f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(FloorScaling.DamageBonus(1f, 0f), Is.EqualTo(0f));
            Assert.That(FloorScaling.Gold(5, 0.5f), Is.EqualTo(8), "7.5 rounds away from zero.");
            Assert.That(FloorScaling.Gold(3, 0.5f), Is.EqualTo(5));
            Assert.That(FloorScaling.Gold(50, 0f), Is.EqualTo(50));
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorScaling.Health(50f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorScaling.DamageBonus(1f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorScaling.Gold(-1, 0f));
        }

        [Test]
        public void CheckpointRecordsExactlyOneDecision()
        {
            var run = new RunState(10);
            var checkpoint = new CheckpointService(run);
            var chosen = new List<CheckpointKind>();
            checkpoint.Chosen += chosen.Add;
            Assert.That(checkpoint.Open(Options()), Is.True);
            Assert.That(checkpoint.Open(Options()), Is.False, "One checkpoint at a time.");
            CheckpointOffer offer = checkpoint.CurrentOffer;

            Assert.That(checkpoint.TrySelect(offer, 1), Is.True);
            Assert.That(checkpoint.TrySelect(offer, 0), Is.False);

            Assert.That(chosen, Is.EqualTo(new[] { CheckpointKind.Descend }));
            Assert.That(checkpoint.CurrentOffer, Is.Null);
            Assert.Throws<ArgumentException>(() => checkpoint.Open(new CheckpointOption[0]));
        }

        [Test]
        public void EndingTheRunWithdrawsTheCheckpoint()
        {
            var run = new RunState(10);
            var checkpoint = new CheckpointService(run);
            checkpoint.Open(Options());
            CheckpointOffer offer = checkpoint.CurrentOffer;

            run.End(RunOutcome.Defeat);

            Assert.That(checkpoint.CurrentOffer, Is.Null);
            Assert.That(checkpoint.TrySelect(offer, 0), Is.False);
            Assert.That(checkpoint.Open(Options()), Is.False);
        }

        [Test]
        public void ChoicesResolveTheBossLevelUpBeforeTheCheckpoint()
        {
            var run = new RunState(10);
            var upgrades = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f), new[]
            {
                new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0}", WeaponStat.Damage, new StatModifier(ModifierOperation.Flat, 5f), 5)
            }, 2);
            var forge = new ForgeService(run, new HealthState(100f));
            var checkpoint = new CheckpointService(run);
            var choices = new RunChoices(upgrades, forge, checkpoint);

            run.AddExperience(10);
            checkpoint.Open(Options());
            Assert.That(choices.Current.Kind, Is.EqualTo(ChoiceKind.Upgrade));

            choices.TrySelect(choices.Current, 0);
            Assert.That(choices.Current.Kind, Is.EqualTo(ChoiceKind.Checkpoint));
            Assert.That(choices.Current.Cards[0].Name, Is.EqualTo("Extract"));
            Assert.That(choices.Current.Cards[1].Description, Is.EqualTo("Secure 96 gold"));

            var chosen = new List<CheckpointKind>();
            checkpoint.Chosen += chosen.Add;
            Assert.That(choices.TrySelect(choices.Current, 0), Is.True);
            Assert.That(chosen, Is.EqualTo(new[] { CheckpointKind.Extract }));
            Assert.That(choices.IsOpen, Is.False);
        }

        [Test]
        public void HealthyHeroesSurviveTheDescentAndWoundedOnesDoNot()
        {
            DescentSimulation.Floor[] floors = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };
            DescentSimulation.Result damageMend = DescentSimulation.Run(floors, 0, true);
            DescentSimulation.Result speedMend = DescentSimulation.Run(floors, 1, true);
            DescentSimulation.Result damageTemper = DescentSimulation.Run(floors, 0, false);
            DescentSimulation.Result speedTemper = DescentSimulation.Run(floors, 1, false);

            Assert.That(damageMend.ClearedFloors, Is.EqualTo(2), $"Damage first with Mend died in {damageMend.DeathRoom}.");
            Assert.That(speedMend.ClearedFloors, Is.EqualTo(2), $"Speed first with Mend died in {speedMend.DeathRoom}.");
            Assert.That(damageTemper.ClearedFloors, Is.EqualTo(1), "Tempering on floor 1 leaves too little health to descend.");
            Assert.That(speedTemper.ClearedFloors, Is.EqualTo(1));
            Assert.That(speedMend.HeroHealth, Is.InRange(5f, 50f), "Floor 2 must remain a real risk.");
            Assert.That(damageMend.Gold, Is.EqualTo(270), "Floor 1 pays 109 gold and floor 2 pays 161 with Cursed Gold.");
            Assert.That(damageTemper.GoldBanked, Is.EqualTo(damageTemper.Gold - damageTemper.UnsecuredAtDeath / 2));
            Assert.That(damageMend.FloorOneUpgrades, Is.InRange(6, 9), "Level-ups are spread over both floors.");
            Assert.That(damageMend.UpgradesApplied, Is.GreaterThan(damageMend.FloorOneUpgrades));
        }

        [Test]
        public void RelicsRescueTheDamageFirstTemperDescentInDifferentWays()
        {
            DescentSimulation.Floor[] floors = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };
            DescentSimulation.Result plainMend = DescentSimulation.Run(floors, 0, true);
            DescentSimulation.Result counterMend = DescentSimulation.Run(floors, 0, true, RelicForgeTests.Counterweight());
            DescentSimulation.Result counterTemper = DescentSimulation.Run(floors, 0, false, RelicForgeTests.Counterweight());
            DescentSimulation.Result windTemper = DescentSimulation.Run(floors, 0, false, RelicForgeTests.SecondWind());
            DescentSimulation.Result counterGreedy = DescentSimulation.Run(floors, 1, false, RelicForgeTests.Counterweight());
            DescentSimulation.Result windGreedy = DescentSimulation.Run(floors, 1, false, RelicForgeTests.SecondWind());

            Assert.That(counterTemper.ClearedFloors, Is.EqualTo(2), "Counterweight carries damage first with Temper through floor 2.");
            Assert.That(windTemper.ClearedFloors, Is.EqualTo(2), "Second Wind carries damage first with Temper through floor 2.");
            Assert.That(windTemper.RelicTriggers, Is.EqualTo(1));
            Assert.That(counterGreedy.ClearedFloors, Is.EqualTo(1), "Counterweight cannot save speed first with Temper.");
            Assert.That(windGreedy.ClearedFloors, Is.EqualTo(2), "Second Wind, the safety relic, can.");
            Assert.That(counterMend.HeroHealth, Is.GreaterThan(plainMend.HeroHealth + 10f), "Counterweight rewards damage upgrades.");
            Assert.That(counterMend.FightSeconds, Is.LessThan(plainMend.FightSeconds), "Counters shorten the fights against packs.");
        }

        private static CheckpointOption[] Options() => new[]
        {
            new CheckpointOption(CheckpointKind.Extract, "Extract", "Bank 96 gold"),
            new CheckpointOption(CheckpointKind.Descend, "Descend", "Secure 96 gold")
        };
    }
}