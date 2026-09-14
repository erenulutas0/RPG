using System;

namespace Cryptforge.UI
{
    // Fits the arena into the rows of a portrait screen that the HUD leaves free. The camera zooms until the arena's world
    // bounds fit both the free band and the screen width, then shifts so the bounds sit centred in the band. Rows count up
    // from the bottom of the screen.
    public static class ArenaFraming
    {
        // A free band thinner than this share of the screen cannot show a portrait arena (a landscape window, say); the whole
        // screen is used instead.
        public const float MinimumBandShare = 0.25f;

        public static void Fit(float screenWidth, float screenHeight, float bandBottom, float bandTop,
            float bottom, float top, float halfWidth, out float orthographicSize, out float cameraY)
        {
            if (!(screenWidth > 0f) || !(screenHeight > 0f) || float.IsInfinity(screenWidth) || float.IsInfinity(screenHeight))
                throw new ArgumentOutOfRangeException(nameof(screenWidth), "The screen needs a positive size.");
            if (!(top > bottom) || float.IsInfinity(top) || float.IsInfinity(bottom))
                throw new ArgumentOutOfRangeException(nameof(top), "The arena's top must be above its bottom.");
            if (!(halfWidth > 0f) || float.IsInfinity(halfWidth))
                throw new ArgumentOutOfRangeException(nameof(halfWidth));

            if (!(bandTop - bandBottom >= screenHeight * MinimumBandShare))
            {
                bandBottom = 0f;
                bandTop = screenHeight;
            }

            float unitsPerRow = Math.Max((top - bottom) / (bandTop - bandBottom), 2f * halfWidth / screenWidth);
            orthographicSize = unitsPerRow * screenHeight / 2f;
            cameraY = (bottom + top) / 2f - ((bandBottom + bandTop) / 2f - screenHeight / 2f) * unitsPerRow;
        }
    }
}
