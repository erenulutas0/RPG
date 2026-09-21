using System;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // The swarm's way out: a place on the arena's far side that the hero leaves by standing still on (docs/27, S6c). Not
    // a touch and not merely a second inside it: the first measurement had a kiting hero leave the room twelve seconds in,
    // by running circles across the exit while fleeing. So the hold only counts while the hero is **not moving** - taking
    // what the finale paid means walking through the crowd and then holding ground in it, and a hero that is still
    // running from something has not decided to leave. Pure, so the scene and the simulation agree about all of it.
    public static class ExitRule
    {
        // How far from the arena's centre the exit stands, toward the far corner, in floor units: well beyond the chest, a
        // real walk. Measured in docs/35, where it turned out not to be the lever - three, five and seven units give almost
        // the same curve, because what leaving costs is being alive at all late in a hardening crowd, not the walk. The
        // time a hero decides to go is what decides whether it gets out.
        public const float Distance = 5f;
        // How close the hero's feet must come to count as standing on it.
        public const float Radius = 0.9f;
        // How long the hero must stand still on it, without stepping off or moving, to leave.
        public const float DwellSeconds = 1f;

        public static void SpotFor(ArenaGeometry geometry, out float floorX, out float floorY)
        {
            // Toward the far corner, never closer to the rim than the margin the hero itself keeps, so it can be reached.
            float reach = Math.Min(Distance, geometry.HalfDepth - HeroMotion.EdgeMargin - Radius);
            floorX = 0f;
            floorY = geometry.Middle + Math.Max(0f, reach);
        }

        public static bool IsWithinReach(float heroX, float heroY, float exitX, float exitY)
        {
            float dx = heroX - exitX;
            float dy = heroY - exitY;
            return dx * dx + dy * dy <= Radius * Radius;
        }
    }

    // How long the hero has stood still on the exit. Stepping off or moving starts it again; standing long enough leaves.
    public sealed class ExitDwell
    {
        public float Held { get; private set; }
        public bool Left { get; private set; }

        // holding is "on the exit and did not move this frame".
        public bool Step(float deltaTime, bool holding)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Left)
                return true;

            Held = holding ? Held + deltaTime : 0f;
            if (Held >= ExitRule.DwellSeconds)
                Left = true;
            return Left;
        }
    }
}
