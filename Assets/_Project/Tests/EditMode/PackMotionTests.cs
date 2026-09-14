using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class PackMotionTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void AnEnemyWalksAtItsSpeedAndStopsJustInsideItsReach()
        {
            var motion = new PackMotion(0.9f, 2f);
            var grunt = new HealthState(50f);
            motion.Add(grunt, 0f, 6f, 2f, 1.2f);

            motion.Step(1f);
            Assert.That(motion.XOf(0), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(motion.YOf(0), Is.EqualTo(4f).Within(1e-4f), "Two floor units in one second.");
            Assert.That(motion.HasArrived(0), Is.False);

            for (int frame = 0; frame < 300; frame++)
                motion.Step(Frame);
            Assert.That(motion.HasArrived(0), Is.True);
            Assert.That(motion.YOf(0), Is.EqualTo(1.2f - PackMotion.ReachMargin).Within(1e-4f));
            float arrivedAt = motion.YOf(0);
            motion.Step(1f);
            Assert.That(motion.YOf(0), Is.EqualTo(arrivedAt), "An arrived enemy stays where it stopped.");
            Assert.That(1.2f * 1.2f, Is.GreaterThan(motion.YOf(0) * motion.YOf(0)), "It stands inside its reach.");
        }

        [Test]
        public void TheEntrySideDecidesTheApproachAngle()
        {
            var motion = new PackMotion(0.9f, 2f);
            motion.Add(new HealthState(10f), -2f, 6f, 3f, 2.1f);
            motion.Add(new HealthState(10f), 0f, 7f, 3f, 2.1f);
            motion.Add(new HealthState(10f), 2f, 6f, 3f, 2.1f);
            for (int frame = 0; frame < 600; frame++)
                motion.Step(Frame);

            // The widest entries approach from 60 degrees off the centre line, the centre one straight on.
            Assert.That(motion.XOf(0), Is.EqualTo(-2f * (float)Math.Sin(Math.PI / 3)).Within(1e-4f));
            Assert.That(motion.YOf(0), Is.EqualTo(2f * (float)Math.Cos(Math.PI / 3)).Within(1e-4f));
            Assert.That(motion.XOf(1), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(motion.YOf(1), Is.EqualTo(2f).Within(1e-4f));
            Assert.That(motion.XOf(2), Is.EqualTo(-motion.XOf(0)).Within(1e-4f));
        }

        [Test]
        public void AnEnemyWaitsBehindACloserOneAndWalksInWhenItFalls()
        {
            var motion = new PackMotion(0.9f, 2f);
            var front = new HealthState(10f);
            var rear = new HealthState(10f);
            motion.Add(rear, 0f, 7f, 2f, 1.2f);
            motion.Add(front, 0f, 6f, 2f, 1.2f);
            for (int frame = 0; frame < 600; frame++)
                motion.Step(Frame);

            Assert.That(motion.HasArrived(1), Is.True, "The closer enemy walks in first, whatever its slot.");
            Assert.That(motion.HasArrived(0), Is.False, "The rear enemy waits in line.");
            Assert.That(motion.YOf(0) - motion.YOf(1), Is.GreaterThanOrEqualTo(0.9f - 1e-4f), "It keeps its spacing.");

            front.ApplyDamage(new DamageContext(10f));
            for (int frame = 0; frame < 600; frame++)
                motion.Step(Frame);
            Assert.That(motion.HasArrived(0), Is.True, "A fallen enemy no longer blocks the way.");
            Assert.That(motion.YOf(1), Is.EqualTo(1.1f).Within(1e-4f), "A dead enemy does not move.");
        }

        [Test]
        public void MotionSettingsAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PackMotion(-1f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PackMotion(0.9f, 0f));
            var motion = new PackMotion(0.9f, 2f);
            Assert.Throws<ArgumentNullException>(() => motion.Add(null, 0f, 6f, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Add(new HealthState(1f), 0f, 6f, -1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Add(new HealthState(1f), 0f, 6f, 1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Step(-Frame));
            for (int i = 0; i < PackLayout.MaxPackSize; i++)
                motion.Add(new HealthState(1f), i, 6f, 1f, 1f);
            Assert.Throws<InvalidOperationException>(() => motion.Add(new HealthState(1f), 0f, 6f, 1f, 1f));
        }

        // Halving and doubling are exact, so the scene's world positions give the simulation's floor distances bit for bit.
        [Test]
        public void FloorDistancesFromWorldPositionsMatchFloorCoordinatesExactly()
        {
            float[] values = { 0f, 0.1f, -1.3f, 2.71828f, 5.9999f, -0.00037f, 6.123456f, 1.1f };
            foreach (float x in values)
            {
                foreach (float y in values)
                {
                    float expected = x * x + y * y;
                    Assert.That(ArenaFloor.DistanceSquared(x - 0f, ArenaFloor.WorldY(y) - 0f), Is.EqualTo(expected), $"({x}, {y})");
                    foreach (float otherY in values)
                    {
                        float floorDy = y - otherY;
                        Assert.That(ArenaFloor.DistanceSquared(x, ArenaFloor.WorldY(y) - ArenaFloor.WorldY(otherY)),
                            Is.EqualTo(x * x + floorDy * floorDy), $"({x}, {y} - {otherY})");
                    }
                }
            }
            Assert.That(ArenaFloor.WorldY(4f), Is.EqualTo(2f), "Depth shows at half length on screen.");
        }
    }
}
