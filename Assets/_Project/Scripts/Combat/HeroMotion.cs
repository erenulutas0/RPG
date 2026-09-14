using System;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // The hero's position on the arena floor and how it walks: a steering direction from the player's drag, a fixed speed,
    // and the platform's edge as a wall. The floor origin is the arena's centre, where every run starts.
    public sealed class HeroMotion
    {
        // How far inside the platform's rim the hero stops, so the feet never hang over the void.
        public const float EdgeMargin = 0.6f;

        public float X { get; private set; }
        public float Y { get; private set; }
        // Floor units per second at full steer.
        public float Speed { get; }

        public HeroMotion(float speed)
        {
            if (!(speed > 0f) || float.IsInfinity(speed))
                throw new ArgumentOutOfRangeException(nameof(speed));
            Speed = speed;
        }

        // Steers by a direction whose length is the steer's strength (clamped to 1); a length under the dead band holds
        // still. A step that would leave the platform slides along its edge, or stops at it.
        public bool Move(float steerX, float steerY, float deltaTime, ArenaGeometry geometry)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (float.IsNaN(steerX) || float.IsInfinity(steerX) || float.IsNaN(steerY) || float.IsInfinity(steerY))
                throw new ArgumentOutOfRangeException(nameof(steerX));

            float length = (float)Math.Sqrt(steerX * steerX + steerY * steerY);
            if (length < 0.05f || deltaTime <= 0f)
                return false;
            float strength = Math.Min(1f, length);
            float stride = Speed * deltaTime * strength;
            float dx = steerX / length * stride;
            float dy = steerY / length * stride;
            if (TryPlace(X + dx, Y + dy, geometry))
                return true;

            // Against the rim, keep only the part of the step that runs along the edge the hero is pushing into.
            float normalX = Math.Sign(X) / geometry.HalfWidth;
            float normalY = Math.Sign(Y - geometry.Middle) / geometry.HalfDepth;
            float normalLength = (float)Math.Sqrt(normalX * normalX + normalY * normalY);
            if (normalLength <= 0f)
                return false;
            normalX /= normalLength;
            normalY /= normalLength;
            float outward = dx * normalX + dy * normalY;
            if (outward <= 0f)
                return false;
            float slideX = dx - outward * normalX;
            float slideY = dy - outward * normalY;
            if (slideX * slideX + slideY * slideY < 1e-8f)
                return false;
            return TryPlace(X + slideX, Y + slideY, geometry);
        }

        // Puts the hero down somewhere on the platform, for example at the start of a floor.
        public void Place(float x, float y, ArenaGeometry geometry)
        {
            if (!TryPlace(x, y, geometry))
                throw new ArgumentOutOfRangeException(nameof(x), "The point lies off the platform.");
        }

        private bool TryPlace(float x, float y, ArenaGeometry geometry)
        {
            if (!geometry.IsOnPlatform(x, y, EdgeMargin))
                return false;
            X = x;
            Y = y;
            return true;
        }
    }
}
