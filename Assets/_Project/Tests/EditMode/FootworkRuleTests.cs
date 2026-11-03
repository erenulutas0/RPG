using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The development autopilot's footwork in a fight (FootworkRule), one body for the scene's autopilot and the foundry
    // simulation's copy of it. Places are given in the foundry's design plane (FoundryMap.ToFloor turns them onto the
    // floor without changing a distance): the court's east wall stands at u = 14, the pit's hole spans u -16..-4, v 2..14.
    public sealed class FootworkRuleTests
    {
        private const float Turn = 35f * (3.14159274f * 2f / 360f);
        private const float Stride = 2f;
        private const float Radius = 3.2f;

        private static void Floor(float u, float v, out float x, out float y) => FoundryMap.ToFloor(u, v, out x, out y);

        private static void Design(float x, float y, out float u, out float v) => FoundryMap.ToDesign(x, y, out u, out v);

        [Test]
        public void InTheOpenTheKiteTurnsThePushByItsTurn()
        {
            Floor(4f, -8f, out float x, out float y);
            Floor(1f, 0f, out float px, out float py);
            Assert.That(FootworkRule.Kite(x, y, px, py, Turn, Stride, out float gx, out float gy), Is.True);
            float cos = (float)Math.Cos(Turn), sin = (float)Math.Sin(Turn);
            Assert.That(gx, Is.EqualTo(x + (px * cos - py * sin) * Stride).Within(1e-5f));
            Assert.That(gy, Is.EqualTo(y + (px * sin + py * cos) * Stride).Within(1e-5f));
        }

        // Pushed into the east wall the turned push runs off the floor, as the autopilot's kite did before it stood there;
        // the kite turns on until its stride has room.
        [Test]
        public void AgainstAWallTheKiteTurnsUntilItsStrideHasRoom()
        {
            Floor(13f, -8f, out float x, out float y);
            Floor(1f, 0f, out float px, out float py);
            float cos = (float)Math.Cos(Turn), sin = (float)Math.Sin(Turn);
            float straightX = x + (px * cos - py * sin) * Stride, straightY = y + (px * sin + py * cos) * Stride;
            Assert.That(FoundryMap.Contains(straightX, straightY, HeroMotion.EdgeMargin), Is.False, "the turned push leaves the floor");

            Assert.That(FootworkRule.Kite(x, y, px, py, Turn, Stride, out float gx, out float gy), Is.True);
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin), Is.True);
            Assert.That(FoundryMap.Segment(x, y, gx, gy, HeroMotion.EdgeMargin), Is.True);
            Assert.That(ArenaFloor.FloorDistanceSquared(gx, gy, x, y), Is.EqualTo(Stride * Stride).Within(1e-3f));
            Design(gx, gy, out float u, out _);
            Assert.That(u, Is.LessThan(13.4f), "along the wall or away from it");
        }

        // With the hero on the mark the escape leaves it on the far side from the Warden.
        [Test]
        public void TheEscapeLeavesTheMarkAwayFromTheWarden()
        {
            Floor(4f, -8f, out float markX, out float markY);
            Floor(1f, -8f, out float wardenX, out float wardenY);
            Assert.That(FootworkRule.EscapeMark(markX, markY, markX, markY, Radius, wardenX, wardenY, out float gx, out float gy), Is.True);
            Assert.That(ArenaFloor.FloorDistanceSquared(gx, gy, markX, markY), Is.EqualTo(Radius * Radius).Within(1e-3f));
            Design(gx, gy, out float u, out float v);
            Assert.That(u, Is.GreaterThan(4f + Radius * .9f), "straight away from the Warden, to the east");
            Assert.That(Math.Abs(v + 8f), Is.LessThan(Radius * .45f));

            // The Warden on the mark: nothing to get away from, so the nearest way out.
            Floor(5f, -8f, out float heroX, out float heroY);
            Assert.That(FootworkRule.EscapeMark(heroX, heroY, markX, markY, Radius, markX, markY, out gx, out gy), Is.True);
            Design(gx, gy, out u, out v);
            Assert.That(u, Is.EqualTo(4f + Radius).Within(1e-3f));
            Assert.That(v, Is.EqualTo(-8f).Within(1e-3f));
        }

        // More than half a radius out, the hero keeps to its side of the mark rather than crossing it toward the far side.
        [Test]
        public void HalfwayOutTheEscapeKeepsToItsSideOfTheMark()
        {
            Floor(4f, -8f, out float markX, out float markY);
            Floor(2.2f, -8f, out float heroX, out float heroY);
            Floor(-2f, -8f, out float wardenX, out float wardenY);
            Assert.That(FootworkRule.EscapeMark(heroX, heroY, markX, markY, Radius, wardenX, wardenY, out float gx, out float gy), Is.True);
            Design(gx, gy, out float u, out _);
            Assert.That(u, Is.LessThan(4f), "not across the mark to the east");
        }

        // A mark beside the east wall with the Warden to its west: the points away from the Warden lie off the floor or
        // against the wall, and the escape takes one with room round it.
        [Test]
        public void BesideAWallTheEscapeTakesAPointWithRoom()
        {
            Floor(12.4f, -8f, out float markX, out float markY);
            Floor(8f, -8f, out float wardenX, out float wardenY);
            Assert.That(FootworkRule.EscapeMark(markX, markY, markX, markY, Radius, wardenX, wardenY, out float gx, out float gy), Is.True);
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin + FootworkRule.Room), Is.True);
            Assert.That(FoundryMap.Segment(markX, markY, gx, gy, HeroMotion.EdgeMargin), Is.True);
        }

        // Nowhere to walk - a point the floor does not hold - and there is no escape.
        [Test]
        public void WithNoPointToWalkToThereIsNoEscape()
        {
            Floor(-10f, 8f, out float x, out float y);
            Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.False, "the pit's hole");
            Assert.That(FootworkRule.EscapeMark(x, y, x, y, .5f, x + 1f, y, out float gx, out float gy), Is.False);
            Assert.That((gx, gy), Is.EqualTo((x, y)));
        }
    }
}
