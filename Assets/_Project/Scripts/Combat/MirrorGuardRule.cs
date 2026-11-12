using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The Mirror Sentry's guard (docs/50 section 5; the lead's decisions 2026-11-12, `sentry-t45-h125-c2-f50`): floor 2's
    // elite carries a mirror on its front and turns toward the hero at TurnDegrees a second. A landing of the hero's - its
    // weapons' strikes, splashes and procs, its ability - made while the HERO stands inside the front arc (ArcDegrees in
    // all, centred on the facing; the hero's position, not the strike's, so a Staff splash that reaches the Sentry is
    // blunted when the hero stands before it) deals FrontShare of itself to the Sentry and reflects ReflectShare of the
    // landing's full amount back to the hero, the reflection capped at ReflectCap of the hero's maximum health a second
    // for each Sentry (a budget that refills at that rate up to one second's worth). From the flanks and the rear the
    // Sentry takes the landing whole and reflects nothing. A burn's tick (DamageContext.IsTick), the pit's spill and a
    // relic's counter are not landings of the hero's and pass untouched.
    //
    // Measured, not chosen (docs/50 Round 2, VaultsEnemiesResearch, seeds 1-200, five cohorts, typical hands): careful
    // clears +1 / -2 against the Captain, clumsy completion 40.8 %, the Daggers lead the Sentry by 83 %, the careful-minus-
    // standing gap 28 / 24 clears. Change it only with a re-measurement. Pure: the scene's MirrorGuard and MirrorGuardService
    // and the foundry simulation (DescentSimulation.Vaults.cs) step the same MirrorGuardState, and every angle and product
    // below is one body that is never inlined, so the scene and the simulation round alike (the stamina divergence's lesson,
    // docs/21: never copy distance or angle arithmetic across assemblies).
    public static class MirrorGuardRule
    {
        public const float TurnDegrees = 45f;
        public const float ArcDegrees = 120f;
        public const float FrontShare = .5f;
        public const float ReflectShare = .2f;
        public const float ReflectCap = .02f;

        // The facing of a body at (x, y) that looks straight at (tx, ty), in radians.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float FacingToward(float x, float y, float tx, float ty) => (float)Math.Atan2(ty - y, tx - x);

        // How far a body turning turnDegrees a second turns in deltaTime, in radians.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TurnStep(float turnDegrees, float deltaTime) => turnDegrees * (float)Math.PI / 180f * deltaTime;

        // A facing turned from (x, y) toward (tx, ty) by at most maxTurn radians, in (-pi, pi].
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Turn(float facing, float x, float y, float tx, float ty, float maxTurn)
        {
            float want = (float)Math.Atan2(ty - y, tx - x);
            float delta = Wrap(want - facing);
            if (delta > maxTurn)
                delta = maxTurn;
            else if (delta < -maxTurn)
                delta = -maxTurn;
            return Wrap(facing + delta);
        }

        // Whether (px, py) stands inside the front arc (arcDegrees in all, centred on facing) of a body at (x, y).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool InFront(float facing, float x, float y, float px, float py, float arcDegrees)
        {
            float toward = (float)Math.Atan2(py - y, px - x);
            return Math.Abs(Wrap(toward - facing)) <= arcDegrees * (float)Math.PI / 360f;
        }

        // One landing of amount on the guard: frontShare of it from the front, the amount itself from anywhere else.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float DamageOn(float amount, bool front, float frontShare) => front ? amount * frontShare : amount;

        // The reflection a front landing of amount asks for, and what a budget pays of it (never below nothing).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Reflection(float amount, float reflectShare, float budget) =>
            Math.Min(amount * reflectShare, Math.Max(0f, budget));

        // The reflection budget a second later: refilled at cap a second, never above one second's cap; full the first time.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Refill(float budget, float cap, float deltaTime) =>
            float.IsNaN(budget) ? cap : Math.Min(cap, budget + cap * deltaTime);

        // An angle in (-pi, pi].
        public static float Wrap(float angle)
        {
            const float tau = 2f * (float)Math.PI;
            while (angle > (float)Math.PI)
                angle -= tau;
            while (angle <= -(float)Math.PI)
                angle += tau;
            return angle;
        }
    }

    // One Sentry's guard: its facing and its reflection budget, both unset until its first step (no guard on the frame it
    // walks in, as the simulation's foe is not stepped on the frame it spawns). Step turns it and refills the budget while
    // the fight runs (its caller holds it whenever the encounter holds: the boss reveal, a level-up panel); Land judges one
    // landing of the hero's. The tuning is MirrorGuardRule's unless a measurement hands another (the simulation's levers).
    public sealed class MirrorGuardState
    {
        private readonly float _turnDegrees;
        private readonly float _arcDegrees;
        private readonly float _frontShare;
        private readonly float _reflectShare;
        private readonly float _reflectCap;

        public MirrorGuardState()
            : this(MirrorGuardRule.TurnDegrees, MirrorGuardRule.ArcDegrees, MirrorGuardRule.FrontShare,
                MirrorGuardRule.ReflectShare, MirrorGuardRule.ReflectCap)
        {
        }

        public MirrorGuardState(float turnDegrees, float arcDegrees, float frontShare, float reflectShare, float reflectCap)
        {
            RequireFinite(turnDegrees, nameof(turnDegrees), 0f);
            RequireFinite(arcDegrees, nameof(arcDegrees), 0f);
            RequireFinite(frontShare, nameof(frontShare), 0f);
            RequireFinite(reflectShare, nameof(reflectShare), 0f);
            RequireFinite(reflectCap, nameof(reflectCap), 0f);
            _turnDegrees = turnDegrees;
            _arcDegrees = arcDegrees;
            _frontShare = frontShare;
            _reflectShare = reflectShare;
            _reflectCap = reflectCap;
        }

        public float ArcDegrees => _arcDegrees;
        // Radians, NaN until the first step.
        public float Facing { get; private set; } = float.NaN;
        // What the reflection may still take, in health; NaN until the first step.
        public float ReflectBudget { get; private set; } = float.NaN;
        // Whether it has been stepped and guards its front.
        public bool IsGuarding => !float.IsNaN(Facing);

        // A body back on the floor: unturned and unbudgeted again.
        public void Reset()
        {
            Facing = float.NaN;
            ReflectBudget = float.NaN;
        }

        // One running frame: the budget refills (heroMaximum's ReflectCap a second), then the mirror turns toward the hero -
        // at once the first time, at TurnDegrees a second after.
        public void Step(float deltaTime, float x, float y, float heroX, float heroY, float heroMaximum)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            ReflectBudget = MirrorGuardRule.Refill(ReflectBudget, heroMaximum * _reflectCap, deltaTime);
            Facing = float.IsNaN(Facing)
                ? MirrorGuardRule.FacingToward(x, y, heroX, heroY)
                : MirrorGuardRule.Turn(Facing, x, y, heroX, heroY, MirrorGuardRule.TurnStep(_turnDegrees, deltaTime));
        }

        // One landing of amount on the Sentry at (x, y) while the hero stands at (heroX, heroY): what the Sentry takes, whether
        // the hero stood in front, and the reflection the hero is owed for it (taken from the budget; 0 from a flank).
        public float Land(float amount, float x, float y, float heroX, float heroY, out bool front, out float reflected)
        {
            reflected = 0f;
            front = IsGuarding && MirrorGuardRule.InFront(Facing, x, y, heroX, heroY, _arcDegrees);
            if (!front)
                return amount;
            float paid = MirrorGuardRule.Reflection(amount, _reflectShare, ReflectBudget);
            if (paid > 0f)
            {
                ReflectBudget -= paid;
                reflected = paid;
            }
            return MirrorGuardRule.DamageOn(amount, true, _frontShare);
        }

        private static void RequireFinite(float value, string name, float minimum)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < minimum)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    // The reflection the hero is owed (MirrorGuardState.Land's reflected), banked as the landings make it and paid as one
    // hit - from the last Sentry that reflected - on the next frame the fight runs: never inside the hero's own strike, and
    // never while the encounter holds (an open choice, the boss's reveal; the lead's decision 2026-11-12), so the hero loses
    // no health while it chooses; what was banked meanwhile is paid as the clock resumes. One body for the scene's
    // MirrorGuardService and the foundry simulation. TSource is whatever names the Sentry there.
    public sealed class MirrorReflectionDue<TSource> where TSource : class
    {
        public float Owed { get; private set; }
        public TSource From { get; private set; }

        public void Owe(float amount, TSource from)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount <= 0f)
                return;
            Owed += amount;
            From = from;
        }

        // What to pay now, and from whom: all that is banked when the fight runs and the hero can take it, else nothing
        // (it stays banked).
        public float Take(bool runs, out TSource from)
        {
            from = From;
            if (!runs || Owed <= 0f)
                return 0f;
            float owed = Owed;
            Owed = 0f;
            From = null;
            return owed;
        }
    }
}
