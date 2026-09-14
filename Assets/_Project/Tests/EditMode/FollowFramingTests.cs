using System;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class FollowFramingTests
    {
        [Test]
        public void TheVisibleWidthSetsTheZoomAndTheHeroSitsInTheMiddleOfTheFreeBand()
        {
            // 1080 x 2340 like the Samsung S23; the HUD leaves rows 620 to 1778 free.
            FollowFraming.Fit(1080f, 2340f, 620f, 1778f, 4.6f, out float size, out float offset);

            Assert.That(size * 1080f / 2340f, Is.EqualTo(2.3f).Within(1e-4f), "Half the visible width either side of the centre.");
            float unitsPerRow = 2f * size / 2340f;
            float heroRow = 2340f / 2f + offset / unitsPerRow;
            Assert.That(heroRow, Is.EqualTo((620f + 1778f) / 2f).Within(0.01f), "The hero's row is the band's middle row.");
            Assert.That(offset, Is.GreaterThan(0f), "The band's middle lies above the screen's middle, so the camera sits below the hero.");
        }

        [Test]
        public void AShorterScreenKeepsTheWidthAndABandTooThinUsesTheWholeScreen()
        {
            FollowFraming.Fit(1080f, 1920f, 620f, 1458f, 4.6f, out float size, out float offset);
            Assert.That(size * 1080f / 1920f, Is.EqualTo(2.3f).Within(1e-4f));
            Assert.That(size, Is.LessThan(5f), "Less of the arena's depth shows on a 9:16 screen.");
            Assert.That(2f * size / 1920f * ((620f + 1458f) / 2f - 960f), Is.EqualTo(offset).Within(1e-4f));

            FollowFraming.Fit(1920f, 1080f, 620f, 380f, 4.6f, out _, out offset);
            Assert.That(offset, Is.EqualTo(0f).Within(1e-5f), "A landscape window centres the hero on the whole screen.");
        }

        [Test]
        public void FramingInputsAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FollowFraming.Fit(0f, 2340f, 620f, 1778f, 4.6f, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => FollowFraming.Fit(1080f, float.NaN, 620f, 1778f, 4.6f, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => FollowFraming.Fit(1080f, 2340f, 620f, 1778f, 0f, out _, out _));
        }
    }
}
