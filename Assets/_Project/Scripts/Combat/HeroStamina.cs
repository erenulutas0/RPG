using System;

namespace Cryptforge.Combat
{
    // The hero's movement budget. Walking spends the bar, standing still refills it, and a hero on an empty bar still
    // walks - at a fraction of its speed - rather than being frozen where it stands. Measured in
    // 26_MOVEMENT_CADENCE_EXPERIMENT Part 5: it is the one lever that changes the cheapest way to play from "run the
    // whole fight" to "stop and go", and it needs no balance re-tune, because a hero that stands still never spends any
    // and so fights exactly the fight it fought before.
    //
    // Spending is measured in seconds of movement: a full bar buys Bar seconds of walking, and standing refills it at
    // Refill bar-seconds for every second the hero really stands. "Really" is displacement, not the steer that was
    // asked for: a steer under the dead band, or one into the rim, moves nothing and so costs nothing.
    public sealed class HeroStamina
    {
        // Part 5's recommended setting. The empty speed matters more than the refill: at 0.7 the hero still outwalks a
        // Grunt on an empty bar and the budget does nothing, at 0 it is pinned in place and reads as a punishment.
        public const float DefaultBar = 2f;
        public const float DefaultRefill = 1.5f;
        public const float DefaultEmptySpeed = 0.4f;

        // Seconds of walking a full bar buys.
        public float Bar { get; private set; }
        // Bar-seconds restored for every second the hero stands still.
        public float Refill { get; }
        // What the hero's speed is multiplied by once the bar is empty.
        public float EmptySpeed { get; }

        public float Current { get; private set; }
        public float Fraction => Current / Bar;
        public bool IsEmpty => Current <= 0f;
        // What to multiply this frame's step by before walking. Read it before moving; spend afterwards.
        public float SpeedFactor => IsEmpty ? EmptySpeed : 1f;

        // Raised only when the bar actually changed, so a view can bind to it without polling.
        public event Action Changed;

        public HeroStamina() : this(DefaultBar, DefaultRefill, DefaultEmptySpeed)
        {
        }

        public HeroStamina(float bar, float refill, float emptySpeed)
        {
            RequirePositiveFinite(bar, nameof(bar));
            RequirePositiveFinite(refill, nameof(refill));
            if (float.IsNaN(emptySpeed) || emptySpeed < 0f || emptySpeed > 1f)
                throw new ArgumentOutOfRangeException(nameof(emptySpeed));

            Bar = bar;
            Refill = refill;
            EmptySpeed = emptySpeed;
            Current = bar;
        }

        // Spends or restores the bar for one frame. `moved` is whether the hero really changed position on it.
        public void Step(float deltaTime, bool moved)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (deltaTime == 0f)
                return;

            float was = Current;
            Current = moved
                ? Math.Max(0f, Current - deltaTime)
                : Math.Min(Bar, Current + deltaTime * Refill);
            if (Current != was)
                Changed?.Invoke();
        }

        // A card may lengthen the bar; what it adds arrives filled, as a longer bar with nothing in it would be a card
        // that does nothing until the hero next stands still. Shortening it trims what is held to the new length.
        public void SetBar(float bar)
        {
            if (!(bar > 0f) || float.IsInfinity(bar))
                throw new ArgumentOutOfRangeException(nameof(bar));
            if (bar == Bar)
                return;

            float gained = Math.Max(0f, bar - Bar);
            Bar = bar;
            float was = Current;
            Current = Math.Min(bar, Current + gained);
            if (Current != was)
                Changed?.Invoke();
        }

        // Puts the bar back to full. Every wave starts this way: the pause while the next pack forms up is the hero's
        // breather, and both the scene and the Descent simulation fill it at that one moment, which is what keeps the
        // budget from drifting between them.
        public void Fill()
        {
            if (Current == Bar)
                return;
            Current = Bar;
            Changed?.Invoke();
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            if (!(value > 0f) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
