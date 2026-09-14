using System;

namespace Cryptforge.Art
{
    // The camera's zoom and offset when it follows the hero: a fixed width of the arena is visible across the screen, and
    // the hero sits in the middle of the rows the HUD leaves free. Rows count up from the bottom of the screen.
    public static class FollowFraming
    {
        // A free band thinner than this share of the screen (a landscape window, say) centres the hero on the whole screen.
        public const float MinimumBandShare = 0.25f;

        // orthographicSize shows visibleWidth world units across the screen; cameraOffsetY is how far below the hero's
        // world height the camera's centre sits, so the hero lands on the band's middle row.
        public static void Fit(float screenWidth, float screenHeight, float bandBottom, float bandTop, float visibleWidth,
            out float orthographicSize, out float cameraOffsetY)
        {
            if (!(screenWidth > 0f) || !(screenHeight > 0f) || float.IsInfinity(screenWidth) || float.IsInfinity(screenHeight))
                throw new ArgumentOutOfRangeException(nameof(screenWidth), "The screen needs a positive size.");
            if (!(visibleWidth > 0f) || float.IsInfinity(visibleWidth))
                throw new ArgumentOutOfRangeException(nameof(visibleWidth));

            if (!(bandTop - bandBottom >= screenHeight * MinimumBandShare))
            {
                bandBottom = 0f;
                bandTop = screenHeight;
            }

            float unitsPerRow = visibleWidth / screenWidth;
            orthographicSize = unitsPerRow * screenHeight / 2f;
            cameraOffsetY = ((bandBottom + bandTop) / 2f - screenHeight / 2f) * unitsPerRow;
        }
    }
}
