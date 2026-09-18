using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The two development floors behind the enemy-speed question of 26_MOVEMENT_CADENCE_EXPERIMENT. Arm A is the game as
    // it is authored today; arm B carries the measured candidate, the graded 2.3 speed set with enemy damage at x0.78.
    // A comparison is only worth playing if the arms differ in nothing else, so this pins that: same rooms, same waves,
    // same enemy health, rewards, weapons and prefabs, and the same experience total, so both arms offer the same
    // upgrade cards at the same moments. It also pins arm A against today's authored numbers, which is how the
    // experiment states that it changed no shipped data.
    public sealed class ComparisonArmTests
    {
        private const string ArmAId = "floor_kite_arm_a";
        private const string ArmBId = "floor_kite_arm_b";
        private const float EmberHallsDamageRate = 0.92f;
        private const float Compensation = 0.78f;

        // Today's authored walking speeds, and the candidate beside them. Ordered as the waves meet them.
        private static readonly Dictionary<string, float> TodaySpeeds = new Dictionary<string, float>
        {
            { "enemy_grunt", 1.6f }, { "enemy_cinder_mite", 2.2f }, { "enemy_runner", 3f },
            { "enemy_tank", 0.9f }, { "enemy_grunt_captain", 1.4f }, { "enemy_forge_warden", 1f }
        };

        private static readonly Dictionary<string, float> CandidateSpeeds = new Dictionary<string, float>
        {
            { "enemy_grunt_fast", 2.5f }, { "enemy_cinder_mite_fast", 2.6f }, { "enemy_runner_fast", 3.2f },
            { "enemy_tank_fast", 2.3f }, { "enemy_grunt_captain_fast", 2.4f }, { "enemy_forge_warden_fast", 2.3f }
        };

        private static FloorDefinition Load(string id)
        {
            FloorDefinition[] floors = Resources.LoadAll<FloorDefinition>(DevelopmentStart.ResourcesFolder);
            foreach (FloorDefinition floor in floors)
            {
                if (floor != null && floor.Id == id)
                    return floor;
            }
            Assert.Fail($"No floor with id {id} in Resources/{DevelopmentStart.ResourcesFolder}.");
            return null;
        }

        [Test]
        public void BothArmsAreReachableThroughTheDevelopmentHookAndNothingElseLivesBesideThem()
        {
            Assert.That(Load(ArmAId).DisplayName, Is.EqualTo("Kite Test A"));
            Assert.That(Load(ArmBId).DisplayName, Is.EqualTo("Kite Test B"));
            Assert.That(Load("floor_density_proof"), Is.Not.Null, "The proof floor stays reachable.");

            // The folder ships with every build, so only floors may live in it (DevelopmentStart.ResourcesFolder).
            foreach (Object asset in Resources.LoadAll(DevelopmentStart.ResourcesFolder))
                Assert.That(asset, Is.InstanceOf<FloorDefinition>(), $"{asset.name} is not a floor.");
        }

        [Test]
        public void TheArmsRunTheSameWavesAndDifferOnlyInWalkingSpeed()
        {
            FloorDefinition a = Load(ArmAId);
            FloorDefinition b = Load(ArmBId);

            Assert.That(b.RoomCount, Is.EqualTo(a.RoomCount));
            // Ember Halls' own shape without its first combat room: fight, fight, elite, a single heal, boss.
            var shape = new[] { RoomKind.Combat, RoomKind.Combat, RoomKind.Elite, RoomKind.Forge, RoomKind.Boss };
            Assert.That(a.RoomCount, Is.EqualTo(shape.Length));
            int slots = 0;
            int experienceA = 0;
            int experienceB = 0;
            for (int room = 0; room < a.RoomCount; room++)
            {
                RoomDefinition roomA = a.RoomAt(room);
                RoomDefinition roomB = b.RoomAt(room);
                Assert.That(roomA.Kind, Is.EqualTo(shape[room]), $"Room {room} kind.");
                Assert.That(roomB.Kind, Is.EqualTo(roomA.Kind), $"Room {room} kind.");
                Assert.That(roomB.DisplayName, Is.EqualTo(roomA.DisplayName), $"Room {room} name.");
                Assert.That(roomB.WaveCount, Is.EqualTo(roomA.WaveCount), $"Room {room} waves.");
                Assert.That(roomB.ForgeOptionCount, Is.EqualTo(roomA.ForgeOptionCount), $"Room {room} forge options.");
                for (int option = 0; option < roomA.ForgeOptionCount; option++)
                {
                    Assert.That(roomB.ForgeOptionAt(option), Is.SameAs(roomA.ForgeOptionAt(option)),
                        $"Room {room} option {option}: both arms open the same forge.");
                    Assert.That(roomA.ForgeOptionAt(option).Id, Is.EqualTo("forge_mend"),
                        "The only forge option is the heal, so the visit is not a decision that can differ between arms.");
                }

                for (int wave = 0; wave < roomA.WaveCount; wave++)
                {
                    WaveDefinition waveA = roomA.WaveAt(wave);
                    WaveDefinition waveB = roomB.WaveAt(wave);
                    Assert.That(waveB.EnemyCount, Is.EqualTo(waveA.EnemyCount), $"Room {room} wave {wave} size.");

                    for (int slot = 0; slot < waveA.EnemyCount; slot++)
                    {
                        EnemyDefinition enemyA = waveA.EnemyAt(slot);
                        EnemyDefinition enemyB = waveB.EnemyAt(slot);
                        string where = $"Room {room} wave {wave} slot {slot}";
                        AssertSameExceptSpeed(enemyA, enemyB, where);
                        Assert.That(enemyA.MoveSpeed, Is.EqualTo(TodaySpeeds[enemyA.Id]).Within(1e-4f),
                            $"{where}: arm A must walk at today's authored speed.");
                        Assert.That(enemyB.MoveSpeed, Is.EqualTo(CandidateSpeeds[enemyB.Id]).Within(1e-4f),
                            $"{where}: arm B must walk at the candidate speed.");
                        experienceA += enemyA.ExperienceReward;
                        experienceB += enemyB.ExperienceReward;
                        slots++;
                    }
                }
            }

            Assert.That(slots, Is.EqualTo(18), "Both arms send the same eighteen enemies.");
            Assert.That(experienceB, Is.EqualTo(experienceA),
                "Equal experience means equal level-ups, so both arms offer the same upgrade cards at the same moments.");
        }

        [Test]
        public void ArmBCarriesTheMeasuredDamageCompensationAndNothingElseScales()
        {
            FloorDefinition a = Load(ArmAId);
            FloorDefinition b = Load(ArmBId);

            Assert.That(a.EnemyDamageMultiplier, Is.EqualTo(EmberHallsDamageRate).Within(1e-4f),
                "Arm A hits as hard as the authored first floor.");
            Assert.That(b.EnemyDamageMultiplier, Is.EqualTo(EmberHallsDamageRate * Compensation).Within(1e-4f),
                "Arm B applies the measured x0.78 to the same rate; enemy assets keep their authored damage.");
            Assert.That(b.EnemyHealthMultiplier, Is.EqualTo(a.EnemyHealthMultiplier));
            Assert.That(a.Modifier, Is.Null);
            Assert.That(b.Modifier, Is.Null);
            Assert.That(a.NextFloor, Is.Null, "One floor per arm: clearing it ends the run.");
            Assert.That(b.NextFloor, Is.Null);
        }

        private static void AssertSameExceptSpeed(EnemyDefinition a, EnemyDefinition b, string where)
        {
            Assert.That(b.MaximumHealth, Is.EqualTo(a.MaximumHealth), $"{where}: health.");
            Assert.That(b.GoldReward, Is.EqualTo(a.GoldReward), $"{where}: gold.");
            Assert.That(b.ExperienceReward, Is.EqualTo(a.ExperienceReward), $"{where}: experience.");
            Assert.That(b.Prefab, Is.SameAs(a.Prefab), $"{where}: both arms use the same body, so the look is identical.");
            Assert.That(b.Weapon, Is.SameAs(a.Weapon),
                $"{where}: both arms share the weapon asset, so damage, reach and rhythm can only differ by the floor rate.");
        }
    }
}
