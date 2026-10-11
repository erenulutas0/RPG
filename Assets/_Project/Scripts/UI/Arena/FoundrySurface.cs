using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation height only. Combat, navigation and saves continue to use FoundryMap's
    // orthonormal floor coordinates. All ground contacts use this one projection.
    internal static class FoundrySurface
    {
        internal const float RampStart = FoundryMap.GateLandingStart + 1f, RampEnd = FoundryMap.GateLandingEnd - 1f, Rise = 1.25f;
        internal static float Height(float u) => Mathf.Clamp01((u - RampStart) / (RampEnd - RampStart)) * Rise;
        internal static Vector3 Project(float u, float v)
        {
            FoundryMap.ToFloor(u, v, out float x, out float y);
            return new Vector3(x, y * .5f + Height(u), 0);
        }
        internal static float Lift(Vector3 flatWorld, Vector3 region)
        {
            FoundryMap.ToDesign(flatWorld.x - region.x, (flatWorld.y - region.y) * 2, out float u, out _);
            return Height(u);
        }
        // Invert the continuous piecewise-linear surface; never infer floor coordinates
        // from an elevated point with the old flat inverse (lighting and clipping would drift).
        internal static void ToDesign(float x, float projectedY, out float u, out float v)
        {
            float q = (x + projectedY * 2) * FoundryMap.Axis;
            float gain = 2 * FoundryMap.Axis;
            float slope = Rise / (RampEnd - RampStart);
            u = q <= RampStart ? q : q >= RampEnd + gain * Rise ? q - gain * Rise
                : (q + gain * slope * RampStart) / (1 + gain * slope);
            v = u - x / FoundryMap.Axis;
        }
    }
}
