using System;
using System.Runtime.CompilerServices;
using Cryptforge.Combat;

namespace Cryptforge.Tests
{
    // docs/46's Riftblade (Nix Vane), measured before anything is built (research 2026-11-11): its rules as the foundry
    // simulation plays them. Each is one pure body, written to move as it stands into Scripts/Combat once the owner says go
    // (docs/46 section 3, steps 1-3 and 6), so that the scene and the simulation keep one copy of the arithmetic: the float
    // math the two would share sits in one never-inlined method each (BleedRule.WoundOf and TickOf, MomentumRule.Stride,
    // PhaseDashRule.StepOf, SeverRule.DamageOn), nothing draws, and nothing raises a number to a power. Nothing in the game
    // reads them: the simulation's DescentSimulation.Riftblade() alone.

    // Bleed, Quicksilver Stride's status (docs/46 section 2), the game's second damage over time beside Burn. A wound is
    // WoundShare of a crit as it landed on its target, after the shares it took (EliteRule's x2 on an elite); its ticks take
    // no share again. Wounds pool on the enemy: each adds its worth and sets the bleed's DurationSeconds back to full, and
    // every TickSeconds a tick pays the pool spread over the ticks left, so every wound is paid in full unless the enemy
    // falls first. No cap.
    internal static class BleedRule
    {
        public const float WoundShare = .6f;
        public const float DurationSeconds = 3f;
        public const float TickSeconds = .5f;
        // DurationSeconds in ticks: a pool set back to full is paid over the next six ticks of its own clock, all within
        // the 3 s (the sixth falls at most 3 s on, whatever the clock's phase).
        public const int TicksPerWound = 6;

        // A wound of a crit that landed landing, at share of it (WoundShare as proposed; the research reads 0.4 and 0.8).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float WoundOf(float landing, float share) => landing * share;

        // What one tick of a pool pays with this many ticks left: the pool spread over them; the last pays what is left.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TickOf(float pool, int ticksLeft) => ticksLeft <= 1 ? pool : pool / ticksLeft;
    }

    // One enemy's bleed: its pool, the ticks left to pay it and how far the next tick is. Plain state, as BurnState; the
    // simulation ticks it on combat time and deals what it returns as the hero's damage.
    internal sealed class BleedState
    {
        // A hair of slack, so frames that sum to a tick in float - thirty of 1/60 s - still pay it.
        private const float TickSlack = 1e-4f;
        private float _tickClock;

        // What the pool has left to pay, and the ticks it has left to pay it in; none once the bleed is out.
        public float Pool { get; private set; }
        public int TicksLeft { get; private set; }
        public int Wounds { get; private set; }
        public bool IsBleeding => TicksLeft > 0;

        // A wound joins the pool and sets the bleed back to its whole TicksPerWound; a fresh bleed's tick clock starts now,
        // a pooled one keeps its own.
        public void Wound(float worth)
        {
            if (float.IsNaN(worth) || float.IsInfinity(worth) || worth < 0f)
                throw new ArgumentOutOfRangeException(nameof(worth));
            if (!IsBleeding)
            {
                _tickClock = 0f;
                Pool = 0f;
            }
            Pool += worth;
            TicksLeft = BleedRule.TicksPerWound;
            Wounds++;
        }

        // Advances the bleed by deltaTime of combat time and returns what its whole ticks pay.
        public float Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsBleeding)
                return 0f;
            _tickClock += deltaTime;
            float paid = 0f;
            while (TicksLeft > 0 && _tickClock >= BleedRule.TickSeconds - TickSlack)
            {
                _tickClock -= BleedRule.TickSeconds;
                float pay = BleedRule.TickOf(Pool, TicksLeft);
                Pool -= pay;
                TicksLeft--;
                paid += pay;
            }
            if (TicksLeft == 0)
                Clear();
            return paid;
        }

        public void Clear()
        {
            Pool = 0f;
            TicksLeft = 0;
            _tickClock = 0f;
        }
    }

    // Quicksilver Stride's primed crit (docs/46 section 2): once StrideUnits are walked - the hero's real displacement on
    // the floor's metric, a steer into a wall walking nothing, the dash's travel walking - and CooldownSeconds have passed
    // since the last, the Daggers' next strike crits unless its rhythm already does (rhythm, primed, rolled: a rhythm crit
    // never spends it). Spending it starts the walk from 0 and the clock again. At the foundry's walk (3.5 units a second)
    // 2.5 units take 0.7 s, so the 1.2 s binds.
    internal static class MomentumRule
    {
        public const float StrideUnits = 2.5f;
        public const float CooldownSeconds = 1.2f;
        // The primed crit's multiplier: the Daggers' pattern's own 2, a rolled crit's 2 on a weapon that authors none.
        public const float CritMultiplier = 2f;

        // The floor distance between two positions of the hero, before and after its walk step: the square root of
        // ArenaFloor.FloorDistanceSquared, in one body that is never inlined (the stamina lesson, docs/21: one copy of the
        // arithmetic for the scene's HeroMovementInput and the simulation).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Stride(float fromX, float fromY, float toX, float toY) =>
            (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(toX, toY, fromX, fromY));

        // The primed crit's multiplier on a weapon of this pattern: its own where it authors one, else CritMultiplier.
        public static float MultiplierOf(AttackPattern pattern) =>
            pattern.CritMultiplier > 1f ? pattern.CritMultiplier : CritMultiplier;
    }

    // The stride tracker: the units walked toward the next primed crit and the seconds since the last was spent, both
    // capped at what priming needs, so that nothing is banked.
    internal sealed class MomentumState
    {
        private const float Slack = 1e-4f;

        public float Walked { get; private set; }
        // The hero starts with the clock run out: the first crit waits for the walk alone.
        public float SinceSpent { get; private set; } = MomentumRule.CooldownSeconds;
        public int Spent { get; private set; }
        public bool IsPrimed => Walked >= MomentumRule.StrideUnits - Slack && SinceSpent >= MomentumRule.CooldownSeconds - Slack;

        public void Walk(float units)
        {
            if (float.IsNaN(units) || float.IsInfinity(units) || units < 0f)
                throw new ArgumentOutOfRangeException(nameof(units));
            Walked = Math.Min(MomentumRule.StrideUnits, Walked + units);
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            SinceSpent = Math.Min(MomentumRule.CooldownSeconds, SinceSpent + deltaTime);
        }

        // The primed crit landed: the walk and the clock start again. False, and nothing changed, when nothing was primed.
        public bool Spend()
        {
            if (!IsPrimed)
                return false;
            Walked = 0f;
            SinceSpent = 0f;
            Spent++;
            return true;
        }
    }

    // Phase Dash (docs/46 section 2): Length floor units in TravelSeconds along the heading - the stick's, or the last
    // heading walked when no finger is down - walls sliding or stopping it as they do the walk (what is left is lost); for
    // GuardSeconds from its first step every hit on the hero is turned away (an enemy's strike, the Warden's slam, the
    // pit's spill). It deals nothing, and runs on its cooldown alone. Its travel is walking.
    internal static class PhaseDashRule
    {
        public const float Length = 3f;
        public const float TravelSeconds = .2f;
        public const float GuardSeconds = .3f;
        public const float CooldownSeconds = 9f;

        // The dash's walk on a step of deltaTime with travelLeft seconds of its travel to go: its length over its travel a
        // second, for the part of the step it still travels. One body, never inlined.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float StepOf(float deltaTime, float travelLeft) =>
            Length / TravelSeconds * Math.Min(deltaTime, travelLeft);
    }

    // One dash's clocks: the travel left, the guard left and its heading, stepped at the walk step (HeroMovementInput's
    // order in the scene, the simulation's frame start). Guarded is the walk step's reading for its frame: every hit on the
    // hero until the next walk step reads it.
    internal sealed class PhaseDashState
    {
        private const float Slack = 1e-4f;

        public float TravelLeft { get; private set; }
        public float GuardLeft { get; private set; }
        public float HeadingX { get; private set; }
        public float HeadingY { get; private set; }
        public bool Guarded { get; private set; }
        public int Dashes { get; private set; }
        public bool IsDashing => TravelLeft > Slack;

        // The press, taken at the walk step, along a heading (normalised here; straight down the floor for none).
        public void Begin(float headingX, float headingY)
        {
            float length = (float)Math.Sqrt(headingX * headingX + headingY * headingY);
            if (float.IsNaN(length) || float.IsInfinity(length))
                throw new ArgumentOutOfRangeException(nameof(headingX));
            if (length > 1e-5f)
            {
                HeadingX = headingX / length;
                HeadingY = headingY / length;
            }
            else
            {
                HeadingX = 0f;
                HeadingY = -1f;
            }
            TravelLeft = PhaseDashRule.TravelSeconds;
            GuardLeft = PhaseDashRule.GuardSeconds;
            Dashes++;
        }

        // One walk step of deltaTime: the units the dash moves the hero along its heading on it (0 once its travel is
        // spent), and the frame's guard - from the dash's first step for GuardSeconds.
        public float Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            Guarded = GuardLeft > Slack;
            float units = IsDashing ? PhaseDashRule.StepOf(deltaTime, TravelLeft) : 0f;
            TravelLeft = Math.Max(0f, TravelLeft - deltaTime);
            GuardLeft = Math.Max(0f, GuardLeft - deltaTime);
            return units;
        }
    }

    // Sever, docs/46's "later" active, measured as the variant (docs/46 section 2, "What Sever would need later"): a
    // marked execute on the heaviest enemy within Reach, every CooldownSeconds. The mark is the Daggers' design options' D
    // (+30 % from every weapon for 3 s), the execute their E (x2 under 30 % of the target's maximum health), in the one
    // damage path every weapon's landing takes; a wound's tick takes neither. The gate boss is marked and executed as any
    // enemy (docs/40's "Elite/boss strong"). It deals nothing of its own, so nothing falls to it outside the execute's x2.
    internal static class SeverRule
    {
        public const float CooldownSeconds = 18f;
        public const float Reach = 3f;
        public const float MarkSeconds = 3f;
        public const float MarkShare = .3f;
        public const float ExecuteBelow = .3f;
        public const float ExecuteDamage = 2f;

        // What a landing of amount deals on a marked enemy at health of maximum: the mark's share more, and the execute's
        // x2 on that below ExecuteBelow. One body, never inlined.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float DamageOn(float amount, float health, float maximum)
        {
            float marked = amount * (1f + MarkShare);
            return health < maximum * ExecuteBelow ? marked * ExecuteDamage : marked;
        }
    }

    // The Riftblade's active's cooldown: AbilityRuntime's clock (TrySpend, Tick) for an active that deals no damage of its
    // own, which AbilityRuntime does not take yet (docs/46 section 3 step 3: AbilityKind.Dash).
    internal sealed class ActiveClock
    {
        public float Cooldown { get; }
        public float Remaining { get; private set; }
        public int Uses { get; private set; }
        public bool IsReady => Remaining <= 0f;

        public ActiveClock(float cooldown)
        {
            if (!(cooldown > 0f) || float.IsInfinity(cooldown))
                throw new ArgumentOutOfRangeException(nameof(cooldown));
            Cooldown = cooldown;
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Remaining > 0f)
                Remaining = Math.Max(0f, Remaining - deltaTime);
        }

        public bool TrySpend()
        {
            if (!IsReady)
                return false;
            Remaining = Cooldown;
            Uses++;
            return true;
        }
    }

    // The development autopilot's dash (docs/46 section 3 step 6), proposed to join FootworkRule as one body for
    // DevelopmentAutopilot and the simulation. It rides on AutopilotCohort.Habit.UsesTheAbility, so the auto-farmer never
    // dashes. The late escape: while the dash is ready, a pilot that escapes marks holds the hero at the Daggers' reach of
    // the gate boss through the telegraph (FootworkRule.StepIn read with the mark left to the dash) and dashes out along
    // its escape's heading as the blow comes due within the guard; with the dash cooling it walks out early, as before. Out
    // of a surround: SurroundCount or more enemies within their own reach of the hero and the hero under SurroundHealth of
    // its maximum, a dash along the kite's heading.
    internal static class DashFootwork
    {
        public const int SurroundCount = 4;
        public const float SurroundHealth = .5f;

        // Whether the Warden's blow, telegraphProgress through its telegraph, lands within the guard of a dash pressed now:
        // the press is taken at the next walk step, a frame later, and its guard lasts GuardSeconds from there.
        public static bool BlowDueWithinGuard(float telegraphProgress, float frame) =>
            (1f - telegraphProgress) * WardenRule.Telegraph <= PhaseDashRule.GuardSeconds - frame;

        public static bool Surrounded(int withinTheirReach, float health, float maximum) =>
            withinTheirReach >= SurroundCount && health < maximum * SurroundHealth;
    }
}
