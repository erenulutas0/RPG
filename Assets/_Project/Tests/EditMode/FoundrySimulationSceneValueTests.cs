using System.Reflection;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;

namespace Cryptforge.Tests
{
    // The foundry floor of the Descent simulation (DescentSimulation.RunFoundry) reads values that live on the scene's
    // MonoBehaviours and assets, which the pure .NET runner cannot compile or load. Each is copied there as a constant;
    // these hold every copy to its source in the Editor, so a change to the game that the simulation has not followed
    // fails here rather than drifting the regression harness silently (docs/21, the targeting divergence).
    public sealed class FoundrySimulationSceneValueTests
    {
        [Test]
        public void TheSceneConstantsAreTheSimulations()
        {
            Assert.That(DescentSimulation.FoundryPace, Is.EqualTo(HeroMovementInput.FoundryPace));
            Assert.That(DescentSimulation.GateMarchFactor, Is.EqualTo(EncounterController.GateMarchFactor));
            Assert.That(DescentSimulation.GateMarchRange, Is.EqualTo(EncounterController.GateMarchRange));
            Assert.That(DescentSimulation.RevealTurnSeconds, Is.EqualTo(BossRevealView.TurnSeconds));
            Assert.That(DescentSimulation.RevealHoldSeconds, Is.EqualTo(BossRevealView.HoldSeconds));
        }

        // DevelopmentAutopilot keeps its policy private; read it the way the runtime holds it.
        [Test]
        public void TheAutopilotsPolicyIsTheSimulations()
        {
            Assert.That(Autopilot<float>("LureDepth"), Is.EqualTo(DescentSimulation.PilotLureDepth));
            Assert.That(Autopilot<float>("ShelterDepth"), Is.EqualTo(DescentSimulation.PilotShelterDepth));
            Assert.That(Autopilot<float>("Arrived"), Is.EqualTo(DescentSimulation.PilotArrived));
            Assert.That(Autopilot<float>("DropDetour"), Is.EqualTo(DescentSimulation.PilotDropDetour));
            Assert.That(Autopilot<float>("DetourSlack"), Is.EqualTo(DescentSimulation.PilotDetourSlack));
            Assert.That(Autopilot<float>("DetourBudget"), Is.EqualTo(DescentSimulation.PilotDetourBudget));
            Assert.That(Autopilot<float>("KiteReach"), Is.EqualTo(DescentSimulation.PilotKiteReach));
            Assert.That(Autopilot<float>("KiteTurn"), Is.EqualTo(DescentSimulation.PilotKiteTurn));
            Assert.That(Autopilot<float>("KiteStride"), Is.EqualTo(DescentSimulation.PilotKiteStride));
            Assert.That(Autopilot<int>("AbilityCrowd"), Is.EqualTo(DescentSimulation.PilotAbilityCrowd));
        }

        private static T Autopilot<T>(string name)
        {
            FieldInfo field = typeof(DevelopmentAutopilot).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"DevelopmentAutopilot.{name}");
            return (T)field.GetValue(null);
        }

        private const string FirstFloor = "Assets/_Project/Data/Floors/Floor_EmberHalls.asset";

        // Floor_EmberHalls as the foundry plays it: the rooms' kinds and waves, the horde's roles, climb-scaled, and its V0.1
        // knobs (surges, extra elites, the gate's clock) as the simulation's shipped levers.
        [Test]
        public void TheFoundryFloorIsTheAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            AssertFloor(asset, DescentSimulation.FoundryFloorOne());
            Assert.That(asset.FoundryHallSurges, Is.EqualTo(FoundryBatch.Shipped().HallSurges));
            Assert.That(asset.FoundryExtraElites, Is.EqualTo(FoundryBatch.Shipped().ExtraElites));
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(FoundryBatch.Shipped().GateNotBeforeSeconds));
        }

        // The climb's floor 2, which a run goes down to after a floor-1 clear (FoundryLevers.Floors = 2): Floor_EmberHalls'
        // next floor as the foundry plays it - its rooms, horde, climb scaling, Cursed Gold and its own V0.1 knobs (none) -
        // its gate boss the Forge Warden with two Cinder Mites, the districts the round goes on from, and below it the
        // finale's swarm room, which the simulation does not play.
        [Test]
        public void TheFoundrysSecondFloorIsTheAsset()
        {
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            FloorDefinition asset = first.NextFloor;
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next;
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.FloorNumber, Is.EqualTo(2));
            AssertFloor(asset, floor);
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.Zero, "floor 2's gate opens as its room clears");

            // Its gate boss: the boss room's first enemy walks out of the gate and WardenRule's share and hold are its; the
            // others walk out beside it.
            int boss = System.Array.IndexOf(floor.Kinds, RoomKind.Boss);
            int firstBoss = System.Array.IndexOf(DescentSimulation.FoundryFloorOne().Kinds, RoomKind.Boss);
            Assert.That(asset.RoomAt(boss).WaveAt(0).EnemyAt(0).Id, Is.EqualTo(first.RoomAt(firstBoss).WaveAt(0).EnemyAt(0).Id),
                "floor 1's gate boss, the Forge Warden");
            Assert.That(floor.Rooms[boss][0][0], Is.SameAs(DescentSimulation.Warden));
            Assert.That(floor.Rooms[boss][0].Length, Is.EqualTo(3), "two Cinder Mites beside it");

            // The districts go on from floor 1's authored halls.
            Assert.That(DescentSimulation.LaterHalls(DescentSimulation.FoundryFloorOne()), Is.EqualTo(EncounterController.LaterHalls(first)));
            Assert.That(DescentSimulation.LaterHalls(floor), Is.EqualTo(EncounterController.LaterHalls(asset)));

            Assert.That(asset.NextFloor, Is.Not.Null);
            Assert.That(asset.NextFloor.RoomAt(0).Kind, Is.EqualTo(RoomKind.Swarm), "the finale");
            Assert.That(floor.Next, Is.Null, "the simulation does not play the finale's swarm room");
        }

        // A foundry floor of the simulation against its asset: id, climb scaling, modifier, its own foundry knobs, the rooms'
        // kinds, waves and enemies (a forge's options in the order the autopilot takes them) and the horde's roles.
        private static void AssertFloor(FloorDefinition asset, DescentSimulation.FoundryFloor floor)
        {
            Assert.That(asset, Is.Not.Null);
            string where = asset.Id;
            Assert.That(asset.Id, Is.EqualTo(floor.Id));
            Assert.That(asset.ClimbScaled, Is.True, where + ": the simulation scales the floor by ClimbScaling");
            FloorModifierDefinition modifier = asset.Modifier;
            Assert.That(modifier != null ? modifier.EnemyHealthPercent : 0f, Is.EqualTo(floor.ModifierHealthPercent), where);
            Assert.That(modifier != null ? modifier.EnemyDamagePercent : 0f, Is.EqualTo(floor.ModifierDamagePercent), where);
            Assert.That(modifier != null ? modifier.GoldPercent : 0f, Is.EqualTo(floor.ModifierGoldPercent), where);
            Assert.That(asset.FoundryHallSurges, Is.EqualTo(floor.HallSurges), where);
            Assert.That(asset.FoundryExtraElites, Is.EqualTo(floor.ExtraElites), where);
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(floor.GateNotBeforeSeconds), where);
            Assert.That(asset.RoomCount, Is.EqualTo(floor.Kinds.Length), where);
            for (int room = 0; room < asset.RoomCount; room++)
            {
                RoomDefinition definition = asset.RoomAt(room);
                Assert.That(definition.Kind, Is.EqualTo(floor.Kinds[room]), $"{where} room {room}");
                if (definition.Kind == RoomKind.Forge)
                {
                    Assert.That(floor.Rooms[room], Is.Null, $"{where} room {room}");
                    Assert.That(definition.ForgeOptionAt(0).Id, Is.EqualTo(DescentSimulation.Mend().Id), "the autopilot takes the first");
                    Assert.That(definition.ForgeOptionAt(1).Id, Is.EqualTo(DescentSimulation.Temper().Id));
                    continue;
                }
                Assert.That(definition.WaveCount, Is.EqualTo(floor.Rooms[room].Length), $"{where} room {room}");
                for (int wave = 0; wave < definition.WaveCount; wave++)
                {
                    WaveDefinition waveDefinition = definition.WaveAt(wave);
                    Assert.That(waveDefinition.EnemyCount, Is.EqualTo(floor.Rooms[room][wave].Length), $"{where} room {room} wave {wave}");
                    for (int e = 0; e < waveDefinition.EnemyCount; e++)
                        AssertEnemy(waveDefinition.EnemyAt(e), floor.Rooms[room][wave][e], $"{where} room {room} wave {wave} enemy {e}");
                }
            }
            Assert.That(asset.HordeRoleCount, Is.EqualTo(floor.HordeRoles.Length), where);
            for (int role = 0; role < asset.HordeRoleCount; role++)
                AssertEnemy(asset.HordeRoleAt(role), floor.HordeRoles[role], $"{where} horde role {role}");
        }

        private static void AssertEnemy(EnemyDefinition definition, DescentSimulation.Enemy enemy, string where)
        {
            Assert.That(definition.DisplayName, Is.EqualTo(enemy.Name), where);
            Assert.That(definition.MaximumHealth, Is.EqualTo(enemy.Health), where);
            Assert.That(definition.MoveSpeed, Is.EqualTo(enemy.Speed), where);
            Assert.That(definition.ExperienceReward, Is.EqualTo(enemy.Experience), where);
            Assert.That(definition.GoldReward, Is.EqualTo(enemy.Gold), where);
            Assert.That(definition.Weapon.Damage, Is.EqualTo(enemy.Damage), where);
            Assert.That(definition.Weapon.Interval, Is.EqualTo(enemy.Interval), where);
            Assert.That(definition.Weapon.Range, Is.EqualTo(enemy.Reach), where);
            Assert.That(definition.Weapon.InitialDelay, Is.EqualTo(enemy.InitialDelay), where);
        }

        [Test]
        public void TheFoundryHeroesAreTheAssets()
        {
            AssertHero("Assets/_Project/Data/Heroes/Hero_Vanguard.asset", DescentSimulation.Vanguard(), HeroPassive.None);
            AssertHero("Assets/_Project/Data/Heroes/Hero_CinderArcanist.asset", DescentSimulation.CinderArcanist(), HeroPassive.EmberMemory);
            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/_Project/Data/Economy/PrototypeEconomy.asset");
            Assert.That(economy.CategoryChoiceCount, Is.EqualTo(DescentSimulation.FoundryChoiceCount));
            Assert.That(economy.ExperiencePerLevel, Is.EqualTo(DescentSimulation.ExperiencePerLevel));
            Assert.That(economy.ExperienceGrowth, Is.EqualTo(DescentSimulation.ExperienceGrowth));
        }

        private static void AssertHero(string path, DescentSimulation.FoundryHero hero, HeroPassive passive)
        {
            var asset = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
            Assert.That(asset.Id, Is.EqualTo(hero.Id), path);
            Assert.That(asset.MaximumHealth, Is.EqualTo(hero.MaximumHealth), path);
            Assert.That(asset.StartingWeapon.Id, Is.EqualTo(hero.Weapon.Id), path);
            Assert.That(asset.StartingWeapon.CategoryDamage, Is.EqualTo(hero.Weapon.CategoryDamage), path);
            Assert.That(asset.StartingWeapon.CategoryInterval, Is.EqualTo(hero.Weapon.CategoryInterval), path);
            Assert.That(asset.StartingWeapon.Range, Is.EqualTo(hero.Weapon.Range), path);
            Assert.That(asset.Ability.Damage, Is.EqualTo(hero.Ability.Damage), path);
            Assert.That(asset.Ability.Radius, Is.EqualTo(hero.Ability.Radius), path);
            Assert.That(asset.Ability.Cooldown, Is.EqualTo(hero.Ability.Cooldown), path);
            Assert.That(asset.Ability.Kind, Is.EqualTo(hero.Ability.Kind), path);
            Assert.That(asset.Passive, Is.EqualTo(passive), path);
            Assert.That(hero.EmberMemory, Is.EqualTo(passive == HeroPassive.EmberMemory), path);
        }
    }
}
