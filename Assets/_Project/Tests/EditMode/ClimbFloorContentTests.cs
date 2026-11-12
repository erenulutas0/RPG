using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // Climb-scaled floors ignore their two authored multipliers at runtime. Keeping those fields equal to the formula at the
    // floor's height keeps the hand-mirrored Descent simulation (Tests/Support/DescentSimulation.cs) and any reader of the
    // asset honest. And the climb's floors, as the foundry plays them, walk its districts in the order the map sets, and
    // every floor in the project passes the checks the Descent makes before it starts.
    public sealed class ClimbFloorContentTests
    {
        private const string FirstFloor = "Assets/_Project/Data/Floors/Floor_EmberHalls.asset";
        private const string ThirdFloor = "Assets/_Project/Data/Floors/Floor_GloamGardens.asset";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        // EncounterController.ValidateFloor, which Initialize runs over the whole chain before the first step.
        private static void Validate(FloorDefinition floor)
        {
            MethodInfo validate = typeof(EncounterController).GetMethod("ValidateFloor", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(validate, Is.Not.Null);
            try
            {
                validate.Invoke(null, new object[] { floor });
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }

        // A copy of a floor asset with one of its private fields set, so the asset itself is never touched.
        private static FloorDefinition CopyOf(string name, string field, object value)
        {
            var asset = AssetDatabase.LoadAssetAtPath<FloorDefinition>(name);
            Assert.That(asset, Is.Not.Null, name);
            FloorDefinition copy = Object.Instantiate(asset);
            copy.name = asset.name;
            if (field != null)
                Set(copy, field, value);
            return copy;
        }

        private static void Set(FloorDefinition floor, string field, object value)
        {
            FieldInfo info = typeof(FloorDefinition).GetField(field, Private);
            Assert.That(info, Is.Not.Null, field);
            info.SetValue(floor, value);
        }

        // The Daggers' elites (EliteRule; docs/41, status 2026-11-10 night): EncounterController.Elites holds the first enemy
        // of an elite room's wave, so every elite room of the climb's foundry floors opens each of its waves with its elite -
        // the Grunt Captain on floors 1 and 3, the Mirror Sentry on floor 2 since 2026-11-12 (docs/50, the lead's decisions) -
        // as the simulation's copies of the floors do (DescentSimulation.EmberHalls, QuicksilverVaults, GloamGardens).
        [Test]
        public void EveryEliteRoomOpensItsWavesWithItsElite()
        {
            var leaders = new[]
            {
                (FirstFloor, DescentSimulation.Captain.Name),
                ("Assets/_Project/Data/Floors/Floor_QuicksilverVaults.asset", DescentSimulation.MirrorSentry.Name),
                (ThirdFloor, DescentSimulation.Captain.Name)
            };
            foreach ((string path, string leader) in leaders)
            {
                var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>(path);
                Assert.That(floor, Is.Not.Null, path);
                int elites = 0;
                for (int r = 0; r < floor.RoomCount; r++)
                {
                    RoomDefinition room = floor.RoomAt(r);
                    if (room == null || room.Kind != RoomKind.Elite)
                        continue;
                    elites++;
                    for (int w = 0; w < room.WaveCount; w++)
                        Assert.That(room.WaveAt(w).EnemyAt(0).DisplayName, Is.EqualTo(leader), $"{path}: room {r + 1}, wave {w + 1}");
                }
                Assert.That(elites, Is.GreaterThan(0), path + " has an elite room");
            }
        }

        [Test]
        public void ClimbScaledFloorsMatchTheFormulaAtTheirHeight()
        {
            var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            Assert.That(floor, Is.Not.Null);
            int height = 1, climbing = 0;
            for (; floor != null; floor = floor.NextFloor, height++)
            {
                if (!floor.ClimbScaled)
                    continue;
                climbing++;
                Assert.That(floor.EnemyHealthMultiplier, Is.EqualTo(ClimbScaling.Health(height)).Within(1e-5f), floor.Id);
                Assert.That(floor.EnemyDamageMultiplier, Is.EqualTo(ClimbScaling.Damage(height)).Within(1e-5f), floor.Id);
            }
            Assert.That(climbing, Is.EqualTo(3), "The Foundry and the two floors above it climb; the finale keeps its own ramp.");
        }

        // The Forge Warden as the foundry's gate sends it out (EncounterController.SpawnWave): its authored 300 at the
        // floor's tier, the run's difficulty - 1.6 here - and the floor's gate share (WardenRule.GateHealthShareOn). Floor
        // 1's is WardenRule.GateHealthShare's, as before; floor 2's own 8.5 / 1.4 cancels the climb's x1.4 on its Warden,
        // which walks out with 4080 instead of 5712 - floor 1's Warden's at the same difficulty (docs/41, status
        // 2026-11-10 later).
        [Test]
        public void TheGateBossKeepsFloorOnesHealthAndFloorTwosHasItsClimbCancelled()
        {
            const float difficulty = 1.6f;
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            Assert.That(first, Is.Not.Null);
            FloorDefinition second = first.NextFloor;
            Assert.That(second, Is.Not.Null);
            Assert.That(second.Id, Is.EqualTo("floor_quicksilver_vaults"));

            float Warden(FloorDefinition floor, int height)
            {
                RoomDefinition[] rooms = EncounterController.FoundryRooms(floor);
                RoomDefinition gate = rooms[rooms.Length - 1];
                Assert.That(gate.Kind, Is.EqualTo(RoomKind.Boss), floor.Id);
                EnemyDefinition boss = gate.WaveAt(0).EnemyAt(0);
                Assert.That(boss.MaximumHealth, Is.EqualTo(300f), floor.Id + ": the Forge Warden");
                FloorModifierDefinition modifier = floor.Modifier;
                return FloorScaling.Health(boss.MaximumHealth,
                    ClimbScaling.Health(height) * ClimbScaling.Boss(height) * difficulty *
                    WardenRule.GateHealthShareOn(floor.FoundryGateHealthShare),
                    modifier != null ? modifier.EnemyHealthPercent : 0f);
            }

            Assert.That(first.FoundryGateHealthShare, Is.Zero, "floor 1 keeps the rule's share");
            Assert.That(Warden(first, 1), Is.EqualTo(300f * WardenRule.GateHealthShare * difficulty).Within(.01f), "floor 1's, unchanged");
            Assert.That(Warden(second, 2), Is.EqualTo(300f * (8.5f / 1.4f) * 1.4f * difficulty).Within(.01f));
            Assert.That(Warden(second, 2), Is.EqualTo(4080f).Within(.01f), "floor 1's Warden's at the same difficulty, not 5712");
        }

        // The climb's floor 3 (docs/48, Gloam Gardens) is the first floor whose gate boss ClimbScaling.Boss hardens (x1.15 at
        // floor 3, on top of the climb's x1.96): its Warden walks out with its authored 300 times both, the run's difficulty
        // and the floor's own share (WardenRule.GateHealthShareOn), measured for docs/40 section 9.6's 45-65 s fight. The
        // chain is floor 1, floor 2, floor 3, the finale.
        [Test]
        public void FloorThreesGateBossTakesTheBossGrowthAndItsOwnShare()
        {
            const float difficulty = 1.6f;
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            FloorDefinition third = first.NextFloor.NextFloor;
            Assert.That(third, Is.SameAs(AssetDatabase.LoadAssetAtPath<FloorDefinition>(ThirdFloor)));
            Assert.That(third.Id, Is.EqualTo("floor_gloam_gardens"));
            Assert.That(third.NextFloor, Is.Not.Null);
            Assert.That(third.NextFloor.Id, Is.EqualTo("floor_final_swarm"), "floor 3 descends to the finale");
            Assert.That(third.NextFloor.NextFloor, Is.Null);
            Assert.That(third.FoundryGateHealthShare, Is.GreaterThan(0f), "floor 3 sets a share of its own");
            Assert.That(ClimbScaling.Boss(3), Is.EqualTo(ClimbScaling.BossGrowth));

            RoomDefinition[] rooms = EncounterController.FoundryRooms(third);
            RoomDefinition gate = rooms[rooms.Length - 1];
            Assert.That(gate.Kind, Is.EqualTo(RoomKind.Boss));
            EnemyDefinition boss = gate.WaveAt(0).EnemyAt(0);
            Assert.That(boss.MaximumHealth, Is.EqualTo(300f), "the Forge Warden");
            Assert.That(third.Modifier, Is.Null);
            float warden = FloorScaling.Health(boss.MaximumHealth,
                ClimbScaling.Health(3) * ClimbScaling.Boss(3) * difficulty * WardenRule.GateHealthShareOn(third.FoundryGateHealthShare), 0f);
            Assert.That(warden, Is.EqualTo(300f * 1.96f * 1.15f * third.FoundryGateHealthShare * difficulty).Within(.05f));
            Assert.That(warden, Is.GreaterThan(4080f), "a harder Warden than floor 2's at the same difficulty");
        }

        // A floor's gate share (FloorDefinition.FoundryGateHealthShare) is 0 for the rule's, or a finite share above 0:
        // NaN, a negative or an infinite share fails the Descent's checks, as a gate held outside the floor's clock does.
        [Test]
        public void AGateBossShareThatIsNoShareIsRejected()
        {
            const string second = "Assets/_Project/Data/Floors/Floor_QuicksilverVaults.asset";
            const string share = "_foundryGateHealthShare";
            var copies = new List<FloorDefinition>();
            FloorDefinition Copy(object value)
            {
                FloorDefinition copy = CopyOf(second, share, value);
                copies.Add(copy);
                return copy;
            }

            try
            {
                Assert.DoesNotThrow(() => Validate(Copy(8.5f / 1.4f)), "Floor 2's own share.");
                Assert.DoesNotThrow(() => Validate(Copy(0f)), "The rule's share.");
                foreach (float value in new[] { float.NaN, -1f, float.PositiveInfinity })
                    Assert.Throws<System.InvalidOperationException>(() => Validate(Copy(value)), $"A gate share of {value}.");
            }
            finally
            {
                foreach (FloorDefinition copy in copies)
                    Object.DestroyImmediate(copy);
            }
        }

        // The climb's halls go round the foundry's districts (FoundryMap.RoomSite) where the controller opens them
        // (EncounterController.TryRoomSite, over each floor's rooms as the foundry plays them): floor 1 walks the workyard,
        // the pit's rim, the east court and, for its V0.1 elite before the gate, the north court; the floors above go on
        // from the authored floors' halls (EncounterController.LaterHalls), so floor 2 keeps the route it had before V0.1 -
        // the north court, the workyard, the pit's rim - floor 3 (docs/48) goes on to the east court, the north court and
        // the workyard, and the finale has no halls.
        [Test]
        public void TheClimbsHallsGoRoundTheDistrictsFromWhereTheAuthoredFloorsBelowLeftOff()
        {
            float[] workyard = { -25f, 7f }, pitRim = { -10f, -1.5f }, east = { 8f, -8f }, north = { 4f, 12f };
            float[][][] routes =
            {
                new[] { workyard, pitRim, east, north },
                new[] { north, workyard, pitRim },
                new[] { east, north, workyard },
                new float[0][]
            };
            var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            int height = 0, hallsBefore = 0;
            for (; floor != null; floor = floor.NextFloor, height++)
            {
                Assert.That(height, Is.LessThan(routes.Length), "Floor 1, floor 2, floor 3 and the finale.");
                RoomDefinition[] rooms = EncounterController.FoundryRooms(floor);
                var route = new List<(float X, float Y)>();
                for (int i = 0; i < rooms.Length; i++)
                    if (EncounterController.TryRoomSite(rooms, i, hallsBefore, out float x, out float y))
                        route.Add((x, y));
                float[][] expected = routes[height];
                Assert.That(route.Count, Is.EqualTo(expected.Length), $"Floor {height + 1} ({floor.Id}): its later halls.");
                for (int h = 0; h < expected.Length; h++)
                {
                    FoundryMap.ToFloor(expected[h][0], expected[h][1], out float ex, out float ey);
                    Assert.That(route[h], Is.EqualTo((ex, ey)), $"Floor {height + 1}'s later hall {h + 1}.");
                }
                hallsBefore += EncounterController.LaterHalls(floor);
            }
            Assert.That(height, Is.EqualTo(routes.Length), "Floor 1, floor 2, floor 3 and the finale.");
        }

        [Test]
        public void EveryFloorInTheProjectPassesTheDescentsChecks()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(FloorDefinition), new[] { "Assets/_Project" });
            Assert.That(guids.Length, Is.GreaterThanOrEqualTo(10), "The climb's floors and the development floors.");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Assert.DoesNotThrow(() => Validate(AssetDatabase.LoadAssetAtPath<FloorDefinition>(path)), path);
            }
        }

        // The gate's clock (FloorDefinition.FoundryGateNotBeforeSeconds) holds the cleared room before the gate boss, so a
        // floor that sets one needs a time inside the floor's clock, a horde to run that clock, a boss, and a combat or
        // elite room before the boss as the foundry plays the floor - a forge before it would let the gate open early.
        // RunHud's room pips grow one a room along the HUD's top row, which has room for nine before the pause button
        // (RunHud.RoomPipsThatFit): every floor in the project, as the foundry plays it (its extra elites added) and as the
        // hall arena plays it, keeps within them. The most any floor plays today is seven.
        [Test]
        public void EveryFloorsRoomsFitTheHudsRowOfPips()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(FloorDefinition), new[] { "Assets/_Project" });
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>(path);
                Assert.That(floor, Is.Not.Null, path);
                Assert.That(floor.RoomCount, Is.LessThanOrEqualTo(Cryptforge.UI.RunHud.RoomPipsThatFit), path + ", authored");
                Assert.That(EncounterController.FoundryRooms(floor).Length,
                    Is.LessThanOrEqualTo(Cryptforge.UI.RunHud.RoomPipsThatFit), path + ", as the foundry plays it");
            }
        }

        [Test]
        public void AGateHeldForTheClockIsRejectedWhereItCouldNotHold()
        {
            const string proof = "Assets/_Project/Resources/Development/Floor_GateHoldProof.asset";
            const string gate = "_foundryGateNotBeforeSeconds";
            var copies = new List<FloorDefinition>();
            FloorDefinition Copy(string name, string field, object value)
            {
                FloorDefinition copy = CopyOf(name, field, value);
                copies.Add(copy);
                return copy;
            }

            EnemyDefinition[] horde = { AssetDatabase.LoadAssetAtPath<FloorDefinition>(proof).HordeRoleAt(0) };

            try
            {
                Assert.DoesNotThrow(() => Validate(Copy(proof, null, null)), "The proof floor's 0:12.");
                foreach (float seconds in new[] { HordeClock.TargetSeconds, HordeClock.TargetSeconds + 60f, -1f, float.NaN })
                    Assert.Throws<System.InvalidOperationException>(() => Validate(Copy(proof, gate, seconds)),
                        $"A gate held until {seconds} s is outside the floor's clock.");
                Assert.Throws<System.InvalidOperationException>(() => Validate(Copy(proof, "_foundryHorde", new EnemyDefinition[0])),
                    "No horde runs the clock it waits for.");

                // Kite Test A ends elite, forge, boss: a forge before the gate, unless an extra elite stands between them.
                FloorDefinition forgeBefore = Copy("Assets/_Project/Resources/Development/Floor_KiteArmA.asset", gate, 12f);
                Set(forgeBefore, "_foundryHorde", horde);
                Assert.Throws<System.InvalidOperationException>(() => Validate(forgeBefore), "A forge before the boss never holds.");
                Set(forgeBefore, "_foundryExtraElites", 1);
                Assert.DoesNotThrow(() => Validate(forgeBefore), "The extra elite is the room before the boss as the foundry plays it.");

                // The halls proof has no boss at all.
                FloorDefinition bossless = Copy("Assets/_Project/Resources/Development/Floor_RoomSitesProof.asset", gate, 12f);
                Set(bossless, "_foundryHorde", horde);
                Assert.Throws<System.InvalidOperationException>(() => Validate(bossless), "No boss, no gate to hold.");
            }
            finally
            {
                foreach (FloorDefinition copy in copies)
                    Object.DestroyImmediate(copy);
            }
        }
    }
}
