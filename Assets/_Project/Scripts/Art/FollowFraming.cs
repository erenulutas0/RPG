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

        // The free band's centre favours the room when the hero approaches an edge. The final clamp reserves space
        // around the hero for bodies, bars and nearby threats, even if that means showing some scenery outside the rim.
        // Inputs/outputs are projected world coordinates, not unprojected floor depth.
        public static void ConstrainToArena(ArenaGeometry arena, float heroX, float heroY, float visibleWidth,
            float bandHeight, out float centreX, out float centreY)
        {
            if (!(visibleWidth > 0f) || float.IsInfinity(visibleWidth) || !(bandHeight > 0f) || float.IsInfinity(bandHeight))
                throw new ArgumentOutOfRangeException(nameof(visibleWidth));
            if (float.IsNaN(heroX) || float.IsInfinity(heroX) || float.IsNaN(heroY) || float.IsInfinity(heroY))
                throw new ArgumentOutOfRangeException(nameof(heroX));
            float halfWidth = visibleWidth / 2f;
            float halfHeight = bandHeight / 2f;
            const float rimScenery = .65f;
            centreX = RoomCentre(heroX, -arena.HalfWidth, arena.HalfWidth, halfWidth, rimScenery);
            centreY = RoomCentre(heroY, arena.WorldBottom, arena.WorldTop, halfHeight, rimScenery);
            float sideRoom = Math.Min(2.6f, visibleWidth * .4f);
            float headRoom = Math.Min(3.1f, bandHeight * .42f);
            float footRoom = Math.Min(1.4f, bandHeight * .25f);
            centreX = Clamp(centreX, heroX + sideRoom - halfWidth, heroX - sideRoom + halfWidth);
            centreY = Clamp(centreY, heroY + headRoom - halfHeight, heroY - footRoom + halfHeight);
        }

        private static float RoomCentre(float target, float lower, float upper, float halfExtent, float scenery)
        {
            float minimum = lower + halfExtent - scenery;
            float maximum = upper - halfExtent + scenery;
            return minimum > maximum ? (lower + upper) / 2f : Clamp(target, minimum, maximum);
        }

        private static float Clamp(float value, float lower, float upper) => Math.Max(lower, Math.Min(upper, value));
    }
}
