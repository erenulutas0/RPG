using System;
using Cryptforge.Combat;

namespace Cryptforge.Art
{
    // The forge platform's top as a rhombus on the arena floor, in floor units with the hero at the origin: its near corner
    // behind the hero, its far corner beyond the deepest pack slot, and its half-width at the side corners. Floor depth
    // shows at half length on screen (ArenaFloor.WorldY), so the rhombus is taller than it is wide on the floor and looks
    // like a tall diamond on a portrait screen.
    public readonly struct ArenaGeometry
    {
        public readonly float NearCorner;
        public readonly float FarCorner;
        public readonly float HalfWidth;

        public ArenaGeometry(float nearCorner, float farCorner, float halfWidth)
        {
            if (!(farCorner > nearCorner) || float.IsInfinity(farCorner) || float.IsInfinity(nearCorner))
                throw new ArgumentOutOfRangeException(nameof(farCorner), "The far corner must lie beyond the near corner.");
            if (!(halfWidth > 0f) || float.IsInfinity(halfWidth))
                throw new ArgumentOutOfRangeException(nameof(halfWidth));

            NearCorner = nearCorner;
            FarCorner = farCorner;
            HalfWidth = halfWidth;
        }

        public float Middle => (NearCorner + FarCorner) / 2f;
        public float HalfDepth => (FarCorner - NearCorner) / 2f;

        // The top's extent in world units: x from -HalfWidth to +HalfWidth, y from the near to the far corner on screen.
        public float WorldBottom => ArenaFloor.WorldY(NearCorner);
        public float WorldTop => ArenaFloor.WorldY(FarCorner);
        public float WorldMiddle => ArenaFloor.WorldY(Middle);

        public bool IsOnPlatform(float floorX, float floorY) =>
            Math.Abs(floorX) / HalfWidth + Math.Abs(floorY - Middle) / HalfDepth <= 1f;

        // A point on the top by rhombus coordinates: s runs from the near corner toward the right corner, t toward the
        // left corner, both 0..1; (1, 1) is the far corner. Returned in world units.
        public void TopPoint(float s, float t, out float worldX, out float worldY)
        {
            worldX = (s - t) * HalfWidth;
            worldY = ArenaFloor.WorldY(NearCorner + (s + t) * (Middle - NearCorner));
        }

        // The rhombus coordinates of a floor point; outside the top they fall outside 0..1.
        public void RhombusCoordinates(float floorX, float floorY, out float s, out float t)
        {
            float along = (floorY - NearCorner) / (Middle - NearCorner);
            float across = floorX / HalfWidth;
            s = (along + across) / 2f;
            t = (along - across) / 2f;
        }
    }
}
