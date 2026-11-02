using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;

namespace Cryptforge.Tests
{
    // Climb-scaled floors ignore their two authored multipliers at runtime. Keeping those fields equal to the formula at the
    // floor's height keeps the hand-mirrored Descent simulation (Tests/Support/DescentSimulation.cs) and any reader of the
    // asset honest. And the climb's floors, as the foundry plays them, walk its districts in the order the map sets.
    public sealed class ClimbFloorContentTests
    {
        private const string FirstFloor = "Assets/_Project/Data/Floors/Floor_EmberHalls.asset";

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
    }
}
