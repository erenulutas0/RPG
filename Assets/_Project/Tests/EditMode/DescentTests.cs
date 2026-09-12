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
            var rewards = new RewardService(run, 10);
            var victim = new HealthState(50f);
            victim.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(victim, 8), Is.True);
            Assert.That(rewards.TryAwardKill(victim, 8), Is.False);
            Assert.That(run.Gold, Is.EqualTo(8));
            Assert.That(run.Experience, Is.EqualTo(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => rewards.TryAwardKill(victim, -1));
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
            DescentResult damageMend = SimulateDescent(0, true);
            DescentResult speedMend = SimulateDescent(1, true);
            DescentResult damageTemper = SimulateDescent(0, false);
            DescentResult speedTemper = SimulateDescent(1, false);

            Assert.That(damageMend.ClearedFloors, Is.EqualTo(2), $"Damage first with Mend died on floor {damageMend.ClearedFloors + 1}.");
            Assert.That(speedMend.ClearedFloors, Is.EqualTo(2), $"Speed first with Mend died on floor {speedMend.ClearedFloors + 1}.");
            Assert.That(damageTemper.ClearedFloors, Is.EqualTo(1), "Tempering on floor 1 leaves too little health to descend.");
            Assert.That(speedTemper.ClearedFloors, Is.EqualTo(1));
            Assert.That(damageMend.HeroHealth, Is.InRange(5f, 40f), "Floor 2 must remain a real risk.");
            Assert.That(damageMend.Gold, Is.EqualTo(250), "Floor 1 pays 96 gold and floor 2 pays 154 with Cursed Gold.");
            Assert.That(damageTemper.GoldBanked, Is.EqualTo(damageTemper.Gold - damageTemper.UnsecuredAtDeath / 2));
        }

        private static CheckpointOption[] Options() => new[]
        {
            new CheckpointOption(CheckpointKind.Extract, "Extract", "Bank 96 gold"),
            new CheckpointOption(CheckpointKind.Descend, "Descend", "Secure 96 gold")
        };

        private struct DescentResult
        {
            public int ClearedFloors;
            public float HeroHealth;
            public int Gold;
            public int GoldBanked;
            public int UnsecuredAtDeath;
        }

        private sealed class EnemyStats
        {
            public float Health;
            public float Damage;
            public float Interval;
            public float InitialDelay;
            public float EnrageAt;
            public int Gold;
        }

        private sealed class FloorStats
        {
            public EnemyStats[][] Rooms;
            public float HealthMultiplier = 1f;
            public float DamageMultiplier = 1f;
            public float ModifierDamagePercent;
            public float ModifierGoldPercent;
        }

        // Mirrors Floor_EmberHalls, Floor_QuicksilverVaults, Modifier_CursedGold and the enemy/weapon assets.
        private static readonly EnemyStats Grunt = new EnemyStats { Health = 50f, Damage = 6f, Interval = 1f, Gold = 5 };
        private static readonly EnemyStats Runner = new EnemyStats { Health = 30f, Damage = 2f, Interval = 0.4f, Gold = 3 };
        private static readonly EnemyStats Tank = new EnemyStats { Health = 120f, Damage = 12f, Interval = 2.5f, InitialDelay = 1.5f, Gold = 10 };
        private static readonly EnemyStats Captain = new EnemyStats { Health = 140f, Damage = 9f, Interval = 1.2f, InitialDelay = 0.6f, Gold = 20 };
        private static readonly EnemyStats Warden = new EnemyStats { Health = 300f, Damage = 10f, Interval = 1.8f, InitialDelay = 1f, EnrageAt = 0.5f, Gold = 50 };
        private static readonly FloorStats EmberHalls = new FloorStats
        {
            Rooms = new[] { new[] { Grunt, Runner }, new[] { Runner, Grunt }, new[] { Tank }, new EnemyStats[0], new[] { Captain }, new[] { Warden } }
        };
        private static readonly FloorStats QuicksilverVaults = new FloorStats
        {
            Rooms = new[] { new[] { Grunt, Runner }, new[] { Tank, Runner }, new[] { Grunt, Grunt }, new EnemyStats[0], new[] { Captain }, new[] { Warden } },
            HealthMultiplier = 1.4f,
            DamageMultiplier = 1.25f,
            ModifierDamagePercent = 0.2f,
            ModifierGoldPercent = 0.5f
        };

        // Always descends; uses the real run, reward, upgrade, forge, choice and scaling code at 60 Hz.
        private static DescentResult SimulateDescent(int cardSlot, bool mendOnFloorOne)
        {
            var run = new RunState(10, 0.5f);
            var rewards = new RewardService(run, 10);
            var weapon = new WeaponRuntime(10f, 0.8f, 3f);
            var hero = new HealthState(100f);
            var upgrades = new UpgradeService(run, weapon, new[]
            {
                new UpgradeOption("upgrade_damage", "", "", WeaponStat.Damage, new StatModifier(ModifierOperation.Flat, 5f), 5),
                new UpgradeOption("upgrade_attack_speed", "", "", WeaponStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, 0.5f), 5)
            }, 2);
            var forge = new ForgeService(run, hero);
            var choices = new RunChoices(upgrades, forge);
            var mend = new ForgeOption("forge_mend", "", "", ForgeEffect.Heal, 0.4f);
            var temper = new ForgeOption("forge_temper", "", "", ForgeEffect.BonusUpgrade, 1f);

            var result = new DescentResult();
            FloorStats[] floors = { EmberHalls, QuicksilverVaults };
            for (int f = 0; f < floors.Length; f++)
            {
                bool useMend = f > 0 || mendOnFloorOne;
                if (!SimulateFloor(floors[f], run, rewards, weapon, hero, forge, choices, mend, temper, cardSlot, useMend))
                {
                    result.UnsecuredAtDeath = run.UnsecuredGold;
                    run.End(RunOutcome.Defeat);
                    break;
                }
                result.ClearedFloors++;
                run.SecureGold();
            }

            if (!run.HasEnded)
                run.End(RunOutcome.Victory);
            result.HeroHealth = hero.Current;
            result.Gold = run.Gold;
            result.GoldBanked = run.GoldBanked;
            return result;
        }

        private static bool SimulateFloor(FloorStats floor, RunState run, RewardService rewards, WeaponRuntime weapon, HealthState hero,
            ForgeService forge, RunChoices choices, ForgeOption mend, ForgeOption temper, int cardSlot, bool useMend)
        {
            const float step = 1f / 60f;
            var waves = new int[floor.Rooms.Length];
            for (int i = 0; i < waves.Length; i++)
                waves[i] = floor.Rooms[i].Length;
            var progress = new FloorProgress(waves);

            for (FloorStep next = progress.Advance(); next.Kind != FloorStepKind.Cleared; next = progress.Advance())
            {
                if (next.Kind == FloorStepKind.NonCombatRoom)
                {
                    forge.Open(new[] { mend, temper });
                    choices.TrySelect(choices.Current, useMend ? 0 : 1);
                    ChooseUpgrades(choices, cardSlot);
                    continue;
                }

                EnemyStats stats = floor.Rooms[next.RoomIndex][next.WaveIndex];
                var enemy = new HealthState(FloorScaling.Health(stats.Health, floor.HealthMultiplier, 0f));
                var enemyWeapon = new WeaponRuntime(stats.Damage, stats.Interval, 3f, stats.InitialDelay);
                float bonus = FloorScaling.DamageBonus(floor.DamageMultiplier, floor.ModifierDamagePercent);
                if (bonus != 0f)
                    enemyWeapon.AddModifier(WeaponStat.Damage, new StatModifier(ModifierOperation.Percent, bonus));
                EnrageRule enrage = stats.EnrageAt > 0f ? new EnrageRule(stats.EnrageAt) : null;
                weapon.Tick(10f);
                for (int frame = 0; frame < 100000 && enemy.IsAlive && hero.IsAlive; frame++)
                {
                    if (frame > 0)
                    {
                        weapon.Tick(step);
                        enemyWeapon.Tick(step);
                    }
                    weapon.TryAttack(enemy);
                    if (enrage != null && enrage.Evaluate(enemy.Current, enemy.Maximum))
                        enemyWeapon.AddModifier(WeaponStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, 1f));
                    if (enemy.IsAlive)
                        enemyWeapon.TryAttack(hero);
                }

                if (!hero.IsAlive)
                    return false;
                rewards.TryAwardKill(enemy, FloorScaling.Gold(stats.Gold, floor.ModifierGoldPercent));
                ChooseUpgrades(choices, cardSlot);
            }

            return true;
        }

        private static void ChooseUpgrades(RunChoices choices, int cardSlot)
        {
            while (choices.Current != null && choices.Current.Kind == ChoiceKind.Upgrade)
                choices.TrySelect(choices.Current, Math.Min(cardSlot, choices.Current.Cards.Count - 1));
        }
    }
}
