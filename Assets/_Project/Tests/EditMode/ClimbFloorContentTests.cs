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
            Assert.That(climbing, Is.EqualTo(2), "The Foundry and the floor above it climb; the finale keeps its own ramp.");
        }

        // The climb's halls go round the foundry's districts (FoundryMap.RoomSite) where the controller opens them
        // (EncounterController.TryRoomSite, over each floor's rooms as the foundry plays them): floor 1 walks the workyard,
        // the pit's rim, the east court and, for its V0.1 elite before the gate, the north court; the floors above go on
        // from the authored floors' halls (EncounterController.LaterHalls), so floor 2 keeps the route it had before V0.1 -
        // the north court, the workyard, the pit's rim - and the finale has no halls.
        [Test]
        public void TheClimbsHallsGoRoundTheDistrictsFromWhereTheAuthoredFloorsBelowLeftOff()
        {
            float[] workyard = { -25f, 7f }, pitRim = { -10f, -1.5f }, east = { 8f, -8f }, north = { 4f, 12f };
            float[][][] routes =
            {
                new[] { workyard, pitRim, east, north },
                new[] { north, workyard, pitRim },
                new float[0][]
            };
            var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            int height = 0, hallsBefore = 0;
            for (; floor != null; floor = floor.NextFloor, height++)
            {
                Assert.That(height, Is.LessThan(routes.Length), "Floor 1, floor 2 and the finale.");
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
            Assert.That(height, Is.EqualTo(routes.Length), "Floor 1, floor 2 and the finale.");
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
