using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The Thornling's dive (docs/52 section 4; the lead's decisions 2026-11-12: slow hands are its read, the ring 0.8 s,
    // lengthened to 1.0 s if careful slow hands lose more than 8 points of floor-3 clears; red-orange only on what will
    // hurt): a Thornling that has walked WalkSeconds (since it walked in or last erupted) and stands DiveMin-DiveMax from
    // the hero dives - under the ash it cannot be targeted or struck, and a ring of Radius goes down at the hero's spot,
    // filling for Telegraph seconds. When it fills the Thornling erupts at the ring's centre: Share of the hero's maximum
    // health if the hero stands inside. Then it is stuck StuckSeconds (it neither walks nor strikes, and takes damage as
    // usual). Floor-wide, at most one ring is down at a time, and a new one waits Gap seconds after the last eruption.
    // Between dives it is the Runner it replaces.
    //
    // Simulation research only today (docs/52 step 1): the foundry simulation (DescentSimulation.Gloam.cs) steps a
    // ThornlingClock on each Thornling and one ThornlingField for the floor where a later scene's GloamService would. Pure,
    // and every product below is one body that is never inlined, so a later scene and the simulation round alike (the
    // stamina divergence's lesson, docs/21).
    public static class ThornlingRule
    {
        public const float WalkSeconds = 3f;
        public const float DiveMin = 2.5f;
        public const float DiveMax = 5f;
        public const float Radius = 1.2f;
        public const float Telegraph = .8f;
        public const float Share = .05f;
        public const float StuckSeconds = 1f;
        public const float Gap = 2.5f;

        // Whether a Thornling that has walked walked seconds, at the squared floor distance from the hero, dives now.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Dives(float walked, float walkSeconds, float distanceSquared, float min, float max) =>
            walked >= walkSeconds && distanceSquared >= min * min && distanceSquared <= max * max;

        // Whether the floor may put a new ring down: none is down and gap seconds have passed since the last eruption.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool MayRing(bool ringDown, float sinceEruption, float gap) => !ringDown && sinceEruption >= gap;

        // Whether the Thornlings hold their dives: quietAtGate (research: a fight whose escapes are the gate boss's alone)
        // while the gate boss stands.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Quiet(bool quietAtGate, bool gateBossStands) => quietAtGate && gateBossStands;

        // Whether a ring down age seconds has filled.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Lands(float age, float telegraph) => age >= telegraph;

        // The ring's fill, 0 to 1 (a later view's red-orange fill, as WardenSlamClock.TelegraphProgress).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Progress(float age, float telegraph)
        {
            if (!(telegraph > 0f))
                return 1f;
            float p = age / telegraph;
            return p < 0f ? 0f : p > 1f ? 1f : p;
        }

        // Whether the hero at (x, y) stands inside the ring of radius at (ringX, ringY); the square cast to float as
        // PollenRule.Near's is, so Mono and .NET agree on the boundary.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool InRing(float x, float y, float ringX, float ringY, float radius) =>
            ArenaFloor.FloorDistanceSquared(x, y, ringX, ringY) <= (float)(radius * radius);

        // The eruption's damage: share of the hero's maximum health.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float EruptionDamage(float maximumHealth, float share) => maximumHealth * share;

        // The Thornling's walking speed: none under the ash or stuck, its own otherwise.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SpeedOf(float speed, bool burrowed, bool stuck) => burrowed || stuck ? 0f : speed;
    }

    // One Thornling's state: the seconds it has walked since it walked in or last erupted, under the ash, or stuck for how
    // long more. Its caller steps it only while the fight runs.
    public sealed class ThornlingClock
    {
        private readonly float _walkSeconds;
        private readonly float _stuckSeconds;

        public ThornlingClock()
            : this(ThornlingRule.WalkSeconds, ThornlingRule.StuckSeconds)
        {
        }

        public ThornlingClock(float walkSeconds, float stuckSeconds)
        {
            if (float.IsNaN(walkSeconds) || walkSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(walkSeconds));
            if (float.IsNaN(stuckSeconds) || stuckSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(stuckSeconds));
            _walkSeconds = walkSeconds;
            _stuckSeconds = stuckSeconds;
        }

        public float Walked { get; private set; }
        public bool IsBurrowed { get; private set; }
        public float StuckLeft { get; private set; }
        public bool IsStuck => StuckLeft > 0f;
        // Neither under the ash nor stuck: it walks and strikes as the Runner.
        public bool IsFree => !IsBurrowed && !IsStuck;

        // One running frame: a free Thornling walks, a stuck one waits out its thorns.
        public void Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (IsBurrowed)
                return;
            if (IsStuck)
            {
                StuckLeft -= deltaTime;
                if (StuckLeft < 0f)
                    StuckLeft = 0f;
                return;
            }
            Walked += deltaTime;
        }

        // Whether it dives now at the squared distance from the hero, between min and max.
        public bool WouldDive(float distanceSquared, float min, float max) =>
            IsFree && ThornlingRule.Dives(Walked, _walkSeconds, distanceSquared, min, max);

        // Into the ash.
        public void Dive() => IsBurrowed = true;

        // Up at the ring's centre: stuck, its walk counted again from 0.
        public void Erupt()
        {
            IsBurrowed = false;
            StuckLeft = _stuckSeconds;
            Walked = 0f;
        }

        // Its walking speed now, from its own.
        public float Speed(float speed) => ThornlingRule.SpeedOf(speed, IsBurrowed, IsStuck);
    }

    // The floor's one ring: down at a spot for its telegraph, then landed; a new one waits the gap after the last
    // eruption. Its caller steps it only while the fight runs; Reset on a new floor.
    public sealed class ThornlingField
    {
        private readonly float _telegraph;
        private readonly float _gap;

        public ThornlingField()
            : this(ThornlingRule.Telegraph, ThornlingRule.Gap)
        {
        }

        public ThornlingField(float telegraph, float gap)
        {
            if (float.IsNaN(telegraph) || telegraph < 0f)
                throw new ArgumentOutOfRangeException(nameof(telegraph));
            if (float.IsNaN(gap) || gap < 0f)
                throw new ArgumentOutOfRangeException(nameof(gap));
            _telegraph = telegraph;
            _gap = gap;
            Reset();
        }

        public bool IsDown { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Age { get; private set; }
        public float SinceEruption { get; private set; }
        public float Telegraph => _telegraph;
        public float Progress => ThornlingRule.Progress(Age, _telegraph);
        public bool MayRing => ThornlingRule.MayRing(IsDown, SinceEruption, _gap);

        public void Reset()
        {
            IsDown = false;
            Age = 0f;
            SinceEruption = float.PositiveInfinity;
        }

        // A ring down at the hero's spot.
        public void Down(float x, float y)
        {
            if (IsDown)
                throw new InvalidOperationException("One ring at a time.");
            IsDown = true;
            X = x;
            Y = y;
            Age = 0f;
        }

        // The ring gone without erupting (its Thornling left the floor): the gap is not restarted.
        public void Cancel()
        {
            IsDown = false;
            Age = 0f;
        }

        // One running frame. True on the frame the ring fills: it is gone, the eruption due, the gap restarted.
        public bool Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsDown)
            {
                SinceEruption += deltaTime;
                return false;
            }
            Age += deltaTime;
            if (!ThornlingRule.Lands(Age, _telegraph))
                return false;
            IsDown = false;
            SinceEruption = 0f;
            return true;
        }

        // Whether the hero at (x, y) stands inside the ring of radius.
        public bool Holds(float x, float y, float radius) => ThornlingRule.InRing(x, y, X, Y, radius);
    }
}
