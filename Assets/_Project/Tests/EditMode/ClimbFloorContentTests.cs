using Cryptforge.Combat;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;

namespace Cryptforge.Tests
{
    // Climb-scaled floors ignore their two authored multipliers at runtime. Keeping those fields equal to the formula at the
    // floor's height keeps the hand-mirrored Descent simulation (Tests/Support/DescentSimulation.cs) and any reader of the
    // asset honest.
    public sealed class ClimbFloorContentTests
    {
        [Test]
        public void ClimbScaledFloorsMatchTheFormulaAtTheirHeight()
        {
            var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>("Assets/_Project/Data/Floors/Floor_EmberHalls.asset");
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
    }
}
