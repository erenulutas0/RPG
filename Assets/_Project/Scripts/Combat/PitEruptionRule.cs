using System;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // The foundry's furnace pit erupts (2026-10-24): every Period seconds of fighting molten metal spills over the
    // pit's rim, after a Warning in which the rim's danger band glows. Everything standing within Reach of the pit's edge
    // when it spills is burnt - the hero for HeroShare of its maximum health, each enemy for EnemyShare of its own - so a
    // hero who draws a pack to the rim and steps out of the band in time uses the pit as a weapon. Pure, so the band and
    // the clock can be tested without a scene.
    public static class PitEruptionRule
    {
        public const float Period = 7f;
        public const float Warning = 1.5f;
        // Design units beyond the hole's edge: the hero keeps .6 off it, so the band is 1.4 wide where the hero can stand.
        public const float Reach = 2f;
        public const float HeroShare = .1f;
        public const float EnemyShare = .4f;

        // How far a floor point lies outside the pit's square hole, in design units (0 inside it).
        public static float DistanceOutside(float floorX, float floorY)
        {
            FoundryMap.ToDesign(floorX, floorY, out float u, out float v);
            float du = Math.Max(0f, Math.Max(FoundryMap.PitWest - u, u - FoundryMap.PitEast));
            float dv = Math.Max(0f, Math.Max(FoundryMap.PitSouth - v, v - FoundryMap.PitNorth));
            return (float)Math.Sqrt(du * du + dv * dv);
        }

        public static bool InBand(float floorX, float floorY) => DistanceOutside(floorX, floorY) <= Reach;
    }

    // The eruption clock: it runs only while a fight is on.
    public sealed class PitEruptionClock
    {
        public float Seconds { get; private set; }
        public bool IsWarning => Seconds >= PitEruptionRule.Period - PitEruptionRule.Warning;
        // How far through the warning the clock is, 0 before it and 1 at the spill.
        public float WarningProgress =>
            Math.Max(0f, Math.Min(1f, (Seconds - (PitEruptionRule.Period - PitEruptionRule.Warning)) / PitEruptionRule.Warning));

        public void Reset() => Seconds = 0f;

        // One frame of a fight; true on the frame the pit spills.
        public bool Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            Seconds += deltaTime;
            if (Seconds < PitEruptionRule.Period)
                return false;
            Seconds -= PitEruptionRule.Period;
            return true;
        }
    }
}
