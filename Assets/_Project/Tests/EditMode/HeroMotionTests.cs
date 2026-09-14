using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class HeroMotionTests
    {
        private static readonly ArenaGeometry Arena = new ArenaGeometry(-9f, 9f, 9f);

        [Test]
        public void TheHeroWalksAtItsSpeedInTheSteeredDirectionAndHoldsStillInTheDeadBand()
        {
            var hero = new HeroMotion(2.5f);
            Assert.That(hero.Move(1f, 0f, 1f, Arena), Is.True);
            Assert.That(hero.X, Is.EqualTo(2.5f).Within(1e-5f));
            Assert.That(hero.Y, Is.Zero);

            Assert.That(hero.Move(0f, 0.5f, 1f, Arena), Is.True, "A half steer walks at half speed.");
            Assert.That(hero.Y, Is.EqualTo(1.25f).Within(1e-5f));
            Assert.That(hero.Move(3f, 4f, 1f, Arena), Is.True, "A long steer is clamped to full speed along its direction.");
            Assert.That(hero.X, Is.EqualTo(2.5f + 1.5f).Within(1e-5f));
            Assert.That(hero.Y, Is.EqualTo(1.25f + 2f).Within(1e-5f));

            float x = hero.X;
            Assert.That(hero.Move(0.01f, 0.02f, 1f, Arena), Is.False, "Inside the dead band nothing moves.");
            Assert.That(hero.Move(1f, 0f, 0f, Arena), Is.False, "No time, no step.");
            Assert.That(hero.X, Is.EqualTo(x));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeroMotion(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => hero.Move(1f, 0f, -1f, Arena));
            Assert.Throws<ArgumentOutOfRangeException>(() => hero.Move(float.NaN, 0f, 1f, Arena));
        }

        [Test]
        public void ThePlatformsRimStopsTheHeroAndLetsItSlideAlongTheEdge()
        {
            var hero = new HeroMotion(4f);
            for (int frame = 0; frame < 300; frame++)
                hero.Move(1f, 0f, 1f / 60f, Arena);
            Assert.That(hero.X, Is.EqualTo(9f - HeroMotion.EdgeMargin).Within(0.1f), "Walking right ends at the right corner's margin.");
            Assert.That(Arena.IsOnPlatform(hero.X, hero.Y, HeroMotion.EdgeMargin), Is.True);

            Assert.That(hero.Move(1f, 0f, 0.5f, Arena), Is.False, "Straight into the corner there is nowhere to go.");

            // On the upper-right edge, pushing mostly right slides the hero down along the edge toward the right corner.
            var edge = new HeroMotion(4f);
            edge.Place(4f, 4f, Arena);
            Assert.That(edge.Move(1f, 0.2f, 0.25f, Arena), Is.True);
            Assert.That(edge.X, Is.GreaterThan(4f), "The step along the edge survives.");
            Assert.That(edge.Y, Is.LessThan(4f));
            Assert.That(Arena.IsOnPlatform(edge.X, edge.Y, HeroMotion.EdgeMargin), Is.True);
            Assert.That(edge.Move(1f, 1f, 0.25f, Arena), Is.False, "Straight into the edge nothing moves.");
            Assert.That(edge.Move(-1f, -1f, 0.25f, Arena), Is.True, "Back toward the centre is free.");
            Assert.Throws<ArgumentOutOfRangeException>(() => edge.Place(9f, 9f, Arena));
        }
    }
}
