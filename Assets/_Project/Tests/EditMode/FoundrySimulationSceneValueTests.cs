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

        // Who the scene's autopilot plays as comes from development/autopilot.txt's words (AutopilotCohort.FromSwitch, in
        // DevelopmentAutopilot.Initialize), who the simulation's plays as from its levers; the habits are one body, so what
        // must agree is the reading: the careful pilot for a line naming no cohort ("on", as PerfProbe and
        // HordePacingMeasurement write it) and for the simulation's default levers, and each cohort for its name after a
        // mode ("<mode> <cohort>", as HordePacingMeasurement writes TestResults/horde-cohort.txt).
        [Test]
        public void TheAutopilotsCohortsAreTheSimulations()
        {
            Assert.That(AutopilotCohort.FromSwitch("on"), Is.SameAs(new DescentSimulation.FoundryLevers().Cohort));
            Assert.That(AutopilotCohort.FromSwitch("on"), Is.SameAs(AutopilotCohort.Careful));
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
            {
                Assert.That(AutopilotCohort.FromSwitch("on " + cohort.Name), Is.SameAs(cohort), cohort.Name);
                Assert.That(AutopilotCohort.FromSwitch("linger " + cohort.Name), Is.SameAs(cohort), cohort.Name);
            }
        }

        private static T Autopilot<T>(string name)
        {
            FieldInfo field = typeof(DevelopmentAutopilot).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"DevelopmentAutopilot.{name}");
            return (T)field.GetValue(null);
        }

        // The scene's script order, as the simulation steps a frame (DescentSimulation.Foundry's header): the hero walks,
        // the encounter moves the packs and brings the gate boss out, the Warden's slam clock marks and holds the gate
        // boss's weapon, and only then the default-order scripts - every AttackController, the hero's ability, the burns,
        // the altars, the pit, the chest, the autopilot. While WardenSlamService had no order of its own Unity ran it before
        // or after the attackers as it pleased, and the Warden could swing on the frame its mark went down.
        [Test]
        public void TheScenesScriptOrderIsTheSimulations()
        {
            int walk = ScriptOrder<HeroMovementInput>("UI/HeroMovementInput.cs");
            int encounter = ScriptOrder<EncounterController>("Combat/EncounterController.cs");
            int slams = ScriptOrder<WardenSlamService>("Core/WardenSlamService.cs");
            Assert.That(walk, Is.LessThan(encounter), "the hero walks before the packs move");
            Assert.That(encounter, Is.LessThan(slams), "the gate boss is out before its clock reads it");
            var after = new (string Name, int Order)[]
            {
                (nameof(AttackController), ScriptOrder<AttackController>("Combat/AttackController.cs")),
                (nameof(AbilityController), ScriptOrder<AbilityController>("Combat/AbilityController.cs")),
                (nameof(BurnStatus), ScriptOrder<BurnStatus>("Combat/BurnStatus.cs")),
                (nameof(AltarService), ScriptOrder<AltarService>("Core/AltarService.cs")),
                (nameof(PitEruptionService), ScriptOrder<PitEruptionService>("Core/PitEruptionService.cs")),
                (nameof(ChestSpawner), ScriptOrder<ChestSpawner>("Combat/ChestSpawner.cs")),
                (nameof(DevelopmentAutopilot), ScriptOrder<DevelopmentAutopilot>("Core/DevelopmentAutopilot.cs"))
            };
            foreach ((string name, int order) in after)
                Assert.That(order, Is.GreaterThan(slams), name + " runs after the Warden's slam clock");
        }

        // The order Unity runs a script in: its Script Execution Order setting where one is set, else its
        // DefaultExecutionOrder, else 0.
        private static int ScriptOrder<T>(string path) where T : UnityEngine.MonoBehaviour
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/" + path);
            Assert.That(script, Is.Not.Null, path);
            Assert.That(script.GetClass(), Is.EqualTo(typeof(T)), path);
            int set = MonoImporter.GetExecutionOrder(script);
            return set != 0 ? set : typeof(T).GetCustomAttribute<UnityEngine.DefaultExecutionOrder>()?.order ?? 0;
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
            Assert.That(asset.FoundryGateHealthShare, Is.Zero, "floor 1's Warden keeps WardenRule.GateHealthShare");
            Assert.That(WardenRule.GateHealthShareOn(asset.FoundryGateHealthShare), Is.EqualTo(FoundryBatch.Shipped().WardenHealthShare));
        }

        // The climb's floor 2, which a run goes down to after a floor-1 clear (FoundryLevers.Floors = 2): Floor_EmberHalls'
        // next floor as the foundry plays it - its rooms, horde, climb scaling, Cursed Gold and its own foundry knobs (no
        // V0.1 surges or elite, floor 1's gate time; its Warden's own share) - its gate boss the Forge Warden with two Cinder Mites,
        // the districts the round goes on from, and below it floor 3, Gloam Gardens, which the simulation plays after a
        // floor-2 clear with FoundryLevers.Floors = 3 (TheFoundrysThirdFloorIsTheAsset; the finale before docs/48).
        [Test]
        public void TheFoundrysSecondFloorIsTheAsset()
        {
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            FloorDefinition asset = first.NextFloor;
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next;
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.FloorNumber, Is.EqualTo(2));
            AssertFloor(asset, floor);
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(210f), "floor 2's gate holds for floor 1's 3:30 (docs/47)");

            // Its gate boss: the boss room's first enemy walks out of the gate and WardenRule's share and hold are its; the
            // others walk out beside it.
            int boss = System.Array.IndexOf(floor.Kinds, RoomKind.Boss);
            int firstBoss = System.Array.IndexOf(DescentSimulation.FoundryFloorOne().Kinds, RoomKind.Boss);
            Assert.That(asset.RoomAt(boss).WaveAt(0).EnemyAt(0).Id, Is.EqualTo(first.RoomAt(firstBoss).WaveAt(0).EnemyAt(0).Id),
                "floor 1's gate boss, the Forge Warden");
            Assert.That(floor.Rooms[boss][0][0], Is.SameAs(DescentSimulation.Warden));
            Assert.That(floor.Rooms[boss][0].Length, Is.EqualTo(3), "two Cinder Mites beside it");

            // Its Warden's share is the floor's own, 8.5 / 1.4 (docs/41, status 2026-11-10 later), and the simulation's floor
            // 2 plays the asset's (LaterFloorLevers, through the scene's WardenRule.GateHealthShareOn).
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(8.5f / 1.4f));
            Assert.That(DescentSimulation.LaterFloorLevers(FoundryBatch.Shipped(), floor).WardenHealthShare,
                Is.EqualTo(WardenRule.GateHealthShareOn(asset.FoundryGateHealthShare)));

            // The districts go on from floor 1's authored halls.
            Assert.That(DescentSimulation.LaterHalls(DescentSimulation.FoundryFloorOne()), Is.EqualTo(EncounterController.LaterHalls(first)));
            Assert.That(DescentSimulation.LaterHalls(floor), Is.EqualTo(EncounterController.LaterHalls(asset)));

            Assert.That(asset.NextFloor, Is.Not.Null);
            Assert.That(asset.NextFloor.Id, Is.EqualTo("floor_gloam_gardens"), "floor 3 (docs/48)");
            Assert.That(floor.Next, Is.Not.Null, "the simulation's floor 2 leads to floor 3 too");
            Assert.That(floor.Next.Id, Is.EqualTo(asset.NextFloor.Id));
        }

        // The climb's floor 3 (docs/48 phase 1), which a run goes down to after a floor-2 clear (FoundryLevers.Floors = 3):
        // Floor_QuicksilverVaults' next floor as the foundry plays it - floor 2's six rooms under the garden's names, climb-
        // scaled at floor 3 (the first floor where ClimbScaling.Boss's x1.15 reaches the gate boss), no modifier, floor 1's
        // three horde roles, no surges or extra elite, the gate held for 3:00 and its Warden's own share, which the
        // simulation reads through the scene's WardenRule.GateHealthShareOn - its gate boss the Forge Warden with two Cinder
        // Mites, the Foundry's look (its own theme comes later), the districts the round goes on from, and below it the
        // finale's swarm room, which the simulation plays after a floor-3 clear (FoundryLevers.Floors = 4,
        // TheFoundrysFinaleIsTheAsset).
        [Test]
        public void TheFoundrysThirdFloorIsTheAsset()
        {
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            FloorDefinition second = first.NextFloor;
            FloorDefinition asset = second.NextFloor;
            Assert.That(asset, Is.SameAs(AssetDatabase.LoadAssetAtPath<FloorDefinition>("Assets/_Project/Data/Floors/Floor_GloamGardens.asset")));
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next.Next;
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.FloorNumber, Is.EqualTo(3));
            AssertFloor(asset, floor);
            Assert.That(asset.DisplayName, Is.EqualTo("Gloam Gardens"));
            Assert.That(asset.Modifier, Is.Null, "no modifier on floor 3's first build");
            Assert.That(asset.Theme, Is.EqualTo(FloorTheme.Gloam), "floor 3 wears its own overgrown look (docs/51)");
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(DescentSimulation.FloorThreeGateSeconds), "floor 3's gate");
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(180f), "floor 3's gate holds for 3:00, half a minute before floors 1 and 2's");
            Assert.That(asset.EnemyHealthMultiplier, Is.EqualTo(ClimbScaling.Health(3)).Within(1e-5f));
            Assert.That(asset.EnemyDamageMultiplier, Is.EqualTo(ClimbScaling.Damage(3)).Within(1e-5f));

            // Its gate boss: the Forge Warden, two Cinder Mites beside it, at the floor's own share.
            int boss = System.Array.IndexOf(floor.Kinds, RoomKind.Boss);
            Assert.That(asset.RoomAt(boss).WaveAt(0).EnemyAt(0).Id, Is.EqualTo(second.RoomAt(boss).WaveAt(0).EnemyAt(0).Id),
                "floor 2's gate boss, the Forge Warden");
            Assert.That(floor.Rooms[boss][0][0], Is.SameAs(DescentSimulation.Warden));
            Assert.That(floor.Rooms[boss][0].Length, Is.EqualTo(3), "two Cinder Mites beside it");
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(DescentSimulation.FloorThreeGateShare));
            Assert.That(DescentSimulation.LaterFloorLevers(FoundryBatch.Shipped(), floor).WardenHealthShare,
                Is.EqualTo(WardenRule.GateHealthShareOn(asset.FoundryGateHealthShare)));
            Assert.That(ClimbScaling.Boss(floor.FloorNumber), Is.EqualTo(ClimbScaling.BossGrowth), "floor 3's gate is the first the boss growth reaches");

            // The districts go on from floors 1 and 2's authored halls.
            Assert.That(DescentSimulation.LaterHalls(floor), Is.EqualTo(EncounterController.LaterHalls(asset)));

            Assert.That(asset.NextFloor, Is.Not.Null);
            Assert.That(asset.NextFloor.RoomAt(0).Kind, Is.EqualTo(RoomKind.Swarm), "the finale");
            Assert.That(floor.Next, Is.Not.Null, "the simulation's floor 3 leads to the finale too");
            Assert.That(floor.Next.Id, Is.EqualTo(asset.NextFloor.Id));
        }

        // The climb's last floor, which a run goes down to after a floor-3 clear (FoundryLevers.Floors = 4,
        // DescentSimulation.FinaleFloor, FoundryFinale): Floor_GloamGardens' next floor as the foundry plays it - as authored, no
        // extra elite before a boss it does not have - one swarm room whose ladder's waves and enemies are the asset's, at
        // its own multipliers (it is not climb-scaled), with no modifier, no horde, none of the foundry's knobs and nothing
        // below it; and the development autopilot's stay in it before it walks to the exit.
        [Test]
        public void TheFoundrysFinaleIsTheAsset()
        {
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            FloorDefinition asset = first.NextFloor.NextFloor.NextFloor;
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next.Next.Next;
            Assert.That(asset, Is.Not.Null);
            Assert.That(floor, Is.Not.Null);
            Assert.That(asset.Id, Is.EqualTo(floor.Id));
            Assert.That(floor.FloorNumber, Is.EqualTo(DescentSimulation.FinaleFloor));
            Assert.That(DescentSimulation.FinaleFloor, Is.EqualTo(4), "floor 1, floor 2, floor 3, the finale");
            Assert.That(asset.ClimbScaled, Is.EqualTo(floor.ClimbScaled), "the finale keeps its own multipliers");
            Assert.That(asset.ClimbScaled, Is.False);
            Assert.That(asset.EnemyHealthMultiplier, Is.EqualTo(floor.EnemyHealthMultiplier));
            Assert.That(asset.EnemyDamageMultiplier, Is.EqualTo(floor.EnemyDamageMultiplier));
            Assert.That(asset.Modifier, Is.Null);
            Assert.That((floor.ModifierHealthPercent, floor.ModifierDamagePercent, floor.ModifierGoldPercent), Is.EqualTo((0f, 0f, 0f)));
            Assert.That(asset.FoundryHallSurges, Is.EqualTo(floor.HallSurges));
            Assert.That(asset.FoundryExtraElites, Is.EqualTo(floor.ExtraElites));
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(floor.GateNotBeforeSeconds));
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(floor.GateHealthShare));
            Assert.That(asset.HordeRoleCount, Is.EqualTo(floor.HordeRoles.Length), "no horde in the finale");
            Assert.That(asset.NextFloor, Is.Null);
            Assert.That(floor.Next, Is.Null);

            RoomDefinition[] played = EncounterController.FoundryRooms(asset);
            Assert.That(played.Length, Is.EqualTo(floor.Kinds.Length), "the foundry plays it as authored");
            for (int room = 0; room < played.Length; room++)
            {
                RoomDefinition definition = played[room];
                string where = $"{asset.Id} room {room}";
                Assert.That(definition.Kind, Is.EqualTo(floor.Kinds[room]), where);
                Assert.That(definition.WaveCount, Is.EqualTo(floor.Rooms[room].Length), where);
                for (int wave = 0; wave < definition.WaveCount; wave++)
                {
                    WaveDefinition waveDefinition = definition.WaveAt(wave);
                    Assert.That(waveDefinition.EnemyCount, Is.EqualTo(floor.Rooms[room][wave].Length), $"{where} wave {wave}");
                    for (int e = 0; e < waveDefinition.EnemyCount; e++)
                        AssertEnemy(waveDefinition.EnemyAt(e), floor.Rooms[room][wave][e], $"{where} wave {wave} enemy {e}");
                }
            }

            Assert.That(Autopilot<float>("SwarmStay"), Is.EqualTo(DescentSimulation.PilotSwarmStay));
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
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(floor.GateHealthShare), where);
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
