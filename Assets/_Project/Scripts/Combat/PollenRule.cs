using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The Pollen Wraith's pollen (docs/52 section 3; the lead's decisions 2026-11-12: Wraiths in the horde and the halls
    // only, the meter orchid under the hero, red-orange only in its last second): the hero carries pollen, 0 to 1. Each
    // running frame the hero is still when its smoothed speed (an average over about the last half second) is under
    // StillSpeed; while still, pollen rises by RisePerWraith a second for each living Wraith within Radius of the hero,
    // counting at most MostWraiths; while moving it falls by FallPerSecond. At 1 the pollen blooms: BloomShare of the hero's
    // maximum health, and the pollen returns to 0. Killing a Wraith removes its rate, not the pollen carried.
    //
    // Simulation research only today (docs/52 step 1): the foundry simulation (DescentSimulation.Gloam.cs) steps a
    // PollenState where a later scene's GloamService would. Pure, and every product below is one body that is never
    // inlined, so a later scene and the simulation round alike (the stamina divergence's lesson, docs/21: never copy
    // clock, speed or distance arithmetic across assemblies).
    public static class PollenRule
    {
        public const float Radius = 1.6f;
        public const float RisePerWraith = .10f;
        public const int MostWraiths = 3;
        public const float FallPerSecond = .5f;
        public const float BloomShare = .06f;
        public const float StillSpeed = 1f;
        // The smoothing of the hero's speed: an exponential average whose time constant is this many seconds.
        public const float SpeedWindow = .5f;
        // The pollen that blooms.
        public const float Full = 1f;

        // The hero's smoothed speed after a frame of deltaTime at speed: an exponential average with window seconds as its
        // time constant (one frame at most moves it all the way).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Smoothed(float smoothed, float speed, float deltaTime, float window)
        {
            float k = window > 0f ? deltaTime / window : 1f;
            if (k > 1f)
                k = 1f;
            return smoothed + (speed - smoothed) * k;
        }

        // The hero's walk in a frame, from its displacement.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Distance(float dx, float dy) => (float)Math.Sqrt(dx * dx + dy * dy);

        // Whether the hero is still: its smoothed speed under stillSpeed.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Still(float smoothedSpeed, float stillSpeed) => smoothedSpeed < stillSpeed;

        // Whether a Wraith at the squared floor distance from the hero counts toward the rise.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Near(float distanceSquared, float radius) => distanceSquared <= radius * radius;

        // The pollen after a frame: while still, rise a second for each Wraith near (at most most); while moving, fall a
        // second; never under 0. A bloom is read from the result (Blooms).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Step(float pollen, bool still, int wraithsNear, float rise, int most, float fall, float deltaTime)
        {
            if (still)
            {
                int counted = wraithsNear < most ? wraithsNear : most;
                return counted > 0 ? pollen + rise * counted * deltaTime : pollen;
            }
            float next = pollen - fall * deltaTime;
            return next > 0f ? next : 0f;
        }

        // Seconds the hero has stood still without a break, after a frame (research: the redesign screen's settle).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float StillFor(float stillFor, bool still, float deltaTime) => still ? stillFor + deltaTime : 0f;

        // The redesign screen's general step (research, docs/52's measured section): rise a second for each source (at most
        // most) while rising, fall a second while falling, else hold; never under 0. Step above is its shipped-design case
        // (rising: still with a Wraith near; falling: moving).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Advance(float pollen, bool rising, bool falling, int sources, float rise, int most, float fall, float deltaTime)
        {
            if (rising)
            {
                int counted = sources < most ? sources : most;
                return pollen + rise * counted * deltaTime;
            }
            if (!falling)
                return pollen;
            float next = pollen - fall * deltaTime;
            return next > 0f ? next : 0f;
        }

        // Whether a pollen cloud (research: a fallen Wraith's) that has stood age seconds still stands.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CloudStands(float age, float seconds) => age < seconds;

        // Whether the pollen blooms.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Blooms(float pollen) => pollen >= Full;

        // Seconds until a still hero's pollen blooms with wraithsNear Wraiths near (at most most); infinity with none. A
        // later view turns the orchid meter red-orange when this is a second or less (the lead's decision 5).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SecondsToBloom(float pollen, int wraithsNear, float rise, int most)
        {
            int counted = wraithsNear < most ? wraithsNear : most;
            if (counted <= 0 || !(rise > 0f))
                return float.PositiveInfinity;
            float left = Full - pollen;
            return left > 0f ? left / (rise * counted) : 0f;
        }

        // The bloom's damage: share of the hero's maximum health.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float BloomDamage(float maximumHealth, float share) => maximumHealth * share;
    }

    // The hero's pollen and smoothed speed, stepped once a running frame (held while the encounter holds: the reveal, a
    // panel), reset at each floor's checkpoint. The tuning is PollenRule's unless a measurement hands another.
    public sealed class PollenState
    {
        private readonly float _rise;
        private readonly int _most;
        private readonly float _fall;
        private readonly float _stillSpeed;
        private readonly float _window;

        public PollenState()
            : this(PollenRule.RisePerWraith, PollenRule.MostWraiths, PollenRule.FallPerSecond, PollenRule.StillSpeed,
                PollenRule.SpeedWindow)
        {
        }

        public PollenState(float risePerWraith, int mostWraiths, float fallPerSecond, float stillSpeed, float speedWindow)
        {
            RequireFinite(risePerWraith, nameof(risePerWraith));
            RequireFinite(fallPerSecond, nameof(fallPerSecond));
            RequireFinite(stillSpeed, nameof(stillSpeed));
            RequireFinite(speedWindow, nameof(speedWindow));
            if (mostWraiths < 0)
                throw new ArgumentOutOfRangeException(nameof(mostWraiths));
            _rise = risePerWraith;
            _most = mostWraiths;
            _fall = fallPerSecond;
            _stillSpeed = stillSpeed;
            _window = speedWindow;
        }

        public float Pollen { get; private set; }
        public float SmoothedSpeed { get; private set; }
        // Seconds still without a break (research: the redesign screen).
        public float StillFor { get; private set; }
        public bool IsStill => PollenRule.Still(SmoothedSpeed, _stillSpeed);

        // A new floor: no pollen, the hero standing.
        public void Reset()
        {
            Pollen = 0f;
            SmoothedSpeed = 0f;
            StillFor = 0f;
        }

        // One running frame: the hero walked distance this frame (its speed is distance over deltaTime) with wraithsNear
        // living Wraiths within the radius. True when the pollen bloomed this frame (it is back at 0).
        public bool Step(float deltaTime, float distance, int wraithsNear)
        {
            if (float.IsNaN(deltaTime) || deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            SmoothedSpeed = PollenRule.Smoothed(SmoothedSpeed, distance / deltaTime, deltaTime, _window);
            StillFor = PollenRule.StillFor(StillFor, IsStill, deltaTime);
            Pollen = PollenRule.Step(Pollen, IsStill, wraithsNear, _rise, _most, _fall, deltaTime);
            return Bloomed();
        }

        // The redesign screen's frame (research, docs/52's measured section): sources are what feeds the pollen (Wraiths near,
        // or pollen clouds the hero stands in); it rises only once the hero has stood still settleAfter seconds - or, with
        // whileMoving, whenever a source feeds it - and falls while the hero moves (with whileMoving: whenever nothing feeds
        // it). True when it bloomed.
        public bool Step(float deltaTime, float distance, int sources, bool whileMoving, float settleAfter)
        {
            if (float.IsNaN(deltaTime) || deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            SmoothedSpeed = PollenRule.Smoothed(SmoothedSpeed, distance / deltaTime, deltaTime, _window);
            StillFor = PollenRule.StillFor(StillFor, IsStill, deltaTime);
            bool rising = sources > 0 && (whileMoving || (IsStill && StillFor >= settleAfter));
            bool falling = whileMoving || !IsStill;
            Pollen = PollenRule.Advance(Pollen, rising, falling, sources, _rise, _most, _fall, deltaTime);
            return Bloomed();
        }

        private bool Bloomed()
        {
            if (!PollenRule.Blooms(Pollen))
                return false;
            Pollen = 0f;
            return true;
        }

        private static void RequireFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
