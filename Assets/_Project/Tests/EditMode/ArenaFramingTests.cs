using System;
using Cryptforge.UI;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class ArenaFramingTests
    {
        private const float Bottom = -0.7f;
        private const float Top = 4.5f;
        private const float HalfWidth = 2.6f;

        [Test]
        public void OnATallPhoneThePackWidthSetsTheZoomAndTheArenaSitsCentredInTheFreeBand()
        {
            // 1080 x 2340 like the Samsung S23; the HUD leaves rows 620 to 1778 free.
            ArenaFraming.Fit(1080f, 2340f, 620f, 1778f, Bottom, Top, HalfWidth, out float size, out float cameraY);

            Assert.That(size * 1080f / 2340f, Is.EqualTo(HalfWidth).Within(1e-4f), "The widest slots touch the screen edges.");
            float bottomRow = Row(Bottom, size, cameraY, 2340f);
            float topRow = Row(Top, size, cameraY, 2340f);
            Assert.That(bottomRow, Is.GreaterThanOrEqualTo(620f));
            Assert.That(topRow, Is.LessThanOrEqualTo(1778f));
            Assert.That((bottomRow + topRow) / 2f, Is.EqualTo((620f + 1778f) / 2f).Within(0.01f));
        }

        [Test]
        public void OnAShorterScreenTheArenaZoomsOutToFillTheBand()
        {
            // 1080 x 1920 (9:16): the same HUD leaves only rows 620 to 1458 free.
            ArenaFraming.Fit(1080f, 1920f, 620f, 1458f, Bottom, Top, HalfWidth, out float size, out float cameraY);

            Assert.That(Row(Bottom, size, cameraY, 1920f), Is.EqualTo(620f).Within(0.01f));
            Assert.That(Row(Top, size, cameraY, 1920f), Is.EqualTo(1458f).Within(0.01f));
            Assert.That(size * 1080f / 1920f, Is.GreaterThan(HalfWidth), "The screen is wider than the pack.");
        }

        [Test]
        public void ABandTooThinForAPortraitArenaFallsBackToTheWholeScreen()
        {
            // A landscape window where the two HUD blocks overlap.
            ArenaFraming.Fit(1920f, 1080f, 620f, 380f, Bottom, Top, HalfWidth, out float size, out float cameraY);

            Assert.That(Row(Bottom, size, cameraY, 1080f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Row(Top, size, cameraY, 1080f), Is.EqualTo(1080f).Within(0.01f));
        }

        [Test]
        public void FramingInputsAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ArenaFraming.Fit(0f, 2340f, 620f, 1778f, Bottom, Top, HalfWidth, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => ArenaFraming.Fit(1080f, float.NaN, 620f, 1778f, Bottom, Top, HalfWidth, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => ArenaFraming.Fit(1080f, 2340f, 620f, 1778f, Top, Bottom, HalfWidth, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => ArenaFraming.Fit(1080f, 2340f, 620f, 1778f, Bottom, Top, 0f, out _, out _));
        }

        // The screen row, counted from the bottom, that an orthographic camera shows a world height on.
        private static float Row(float worldY, float orthographicSize, float cameraY, float screenHeight) =>
            (worldY - (cameraY - orthographicSize)) / (2f * orthographicSize) * screenHeight;
    }
}
