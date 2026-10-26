using System;

namespace Cryptforge.Combat
{
    // The foundry's gate boss as a fight rather than a wave (docs/41 step 4, the owner's report docs/40 section 8.9): the
    // Forge Warden marks the floor where the hero stands every SlamEvery seconds, and after a Telegraph its hammer lands
    // on the mark - SlamRadius wide, HeroShare of the hero's maximum health - so a hero who reads the mark steps out and
    // one who stands and trades takes the blow. Below PhaseTwoAt of its health it is enraged: the marks come every
    // SlamEveryEnraged and the floor's horde thickens round it. Before this the Warden was a 300-health wave the owner
    // killed in 10-17 s for 6-11 health (docs/21, 2026-10-25). Pure, so the clock can be tested without a scene; the
    // scene's WardenSlamService applies it to the foundry's gate boss only.
    public static class WardenRule
    {
        // The gate boss's health against its authored 300, on the foundry only. With the horde's levels the autopilot
        // killed it in 20-28 s at 1, 15-27 s at 2.5 and 17 s at 5 (the report's own 1500 for a fifth-minute boss, docs/40
        // section 8.9) once it dodged the marks; the fight is to last 45-60 s. 10 held that with the legacy cards; with
        // the weapons-and-tomes offers (docs/42), whose steps are much smaller, the autopilot died at the Warden on every
        // seed with half its health left.
        public const float GateHealthShare = 5f;
        public const float SlamEvery = 6f;
        public const float SlamEveryEnraged = 4f;
        // Between the mark appearing and the hammer landing: the report's 0.8-1.2 s for a readable telegraph on a phone.
        public const float Telegraph = 1.1f;
        public const float SlamRadius = 2.2f;
        public const float HeroShare = 0.2f;
        public const float PhaseTwoAt = 0.5f;

        public static bool IsEnraged(float current, float maximum) => maximum > 0f && current <= maximum * PhaseTwoAt;
    }

    // The Warden's slam clock: idle until the next mark, then a telegraph, then the blow.
    public sealed class WardenSlamClock
    {
        public enum Step { None, Marked, Slammed }

        // Seconds since the last blow (or the fight's start), and whether a mark is down.
        public float Seconds { get; private set; }
        public bool IsMarked { get; private set; }
        // How far through the telegraph the clock is: 0 at the mark, 1 at the blow; 0 with no mark down.
        public float TelegraphProgress { get; private set; }

        public void Reset()
        {
            Seconds = 0f;
            IsMarked = false;
            TelegraphProgress = 0f;
        }

        // One frame of the fight; what happened on it.
        public Step Advance(float deltaTime, bool enraged)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            Seconds += deltaTime;
            float every = enraged ? WardenRule.SlamEveryEnraged : WardenRule.SlamEvery;
            if (!IsMarked)
            {
                if (Seconds < every - WardenRule.Telegraph)
                    return Step.None;
                IsMarked = true;
                Seconds = every - WardenRule.Telegraph;
                TelegraphProgress = 0f;
                return Step.Marked;
            }
            TelegraphProgress = Math.Min(1f, (Seconds - (every - WardenRule.Telegraph)) / WardenRule.Telegraph);
            if (Seconds < every)
                return Step.None;
            IsMarked = false;
            TelegraphProgress = 0f;
            Seconds = 0f;
            return Step.Slammed;
        }
    }
}
