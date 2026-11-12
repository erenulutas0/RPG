using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The Cinder Bloom's bark-and-bloom cycle (docs/52 section 5; the lead's decisions 2026-11-12: the research first,
    // Thorn Post keeps its name, the closed bark's 25 % accepted): floor 3's elite repeats one cycle from the moment it
    // walks in - closed for ClosedSeconds, its bark letting BarkShare of every landing through while it walks and mauls;
    // then open for OpenSeconds, rooted (it neither walks nor strikes; its weapon is held as the Warden's is through its
    // slam) and taking every landing whole. Every landing: a weapon's strike, splash or proc, the ability, a burn's tick -
    // the bark blunts every weapon alike. The bark is judged where the landing meets the body (the scene's Health.Guard,
    // the simulation's FoeDamaged), after the attacker's own shares (EliteRule's Daggers x2), so the Daggers' x2
    // multiplies what the bark lets through.
    //
    // Simulation research only (docs/52 section 8, step 1; DescentSimulation.Gloam.cs, GloamEnemiesResearch): no scene,
    // asset or prefab reads it yet. Pure, and every product below is one body that is never inlined, so a later scene and
    // the simulation round alike (the stamina divergence's lesson, docs/21: never copy clock or damage arithmetic across
    // assemblies).
    public static class BloomRule
    {
        public const float ClosedSeconds = 3.5f;
        public const float OpenSeconds = 2.5f;
        public const float BarkShare = .25f;

        // Whether a Bloom that has stood age seconds on the clock (held while the encounter holds) is open: closed for the
        // first closed seconds of every cycle of closed + open.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Open(float age, float closed, float open)
        {
            float cycle = closed + open;
            if (!(cycle > 0f) || age < closed)
                return false;
            return age % cycle >= closed;
        }

        // How many open windows have begun by age: 0 while the first closed phase stands.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int WindowsBegun(float age, float closed, float open)
        {
            float cycle = closed + open;
            if (!(cycle > 0f) || age < closed)
                return 0;
            return (int)((age - closed) / cycle) + 1;
        }

        // One landing of amount on the Bloom: barkShare of it while closed, the amount itself while open.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float DamageOn(float amount, bool open, float barkShare) => open ? amount : amount * barkShare;

        // The Bloom's walking speed: its own while closed, rooted (none) while open.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SpeedOf(float speed, bool open) => open ? 0f : speed;
    }

    // One Bloom's clock: the seconds it has stood on the floor's running clock, unset until its first step (no cycle on
    // the frame it walks in, as the simulation's foe is not stepped on the frame it spawns). Its caller steps it only while
    // the fight runs (the boss reveal and a level-up panel hold it, as they hold the Warden's slams and the burns). The
    // tuning is BloomRule's unless a measurement hands another (the simulation's levers).
    public sealed class BloomClock
    {
        private readonly float _closed;
        private readonly float _open;
        private readonly float _bark;

        public BloomClock()
            : this(BloomRule.ClosedSeconds, BloomRule.OpenSeconds, BloomRule.BarkShare)
        {
        }

        public BloomClock(float closedSeconds, float openSeconds, float barkShare)
        {
            RequireFinite(closedSeconds, nameof(closedSeconds));
            RequireFinite(openSeconds, nameof(openSeconds));
            RequireFinite(barkShare, nameof(barkShare));
            if (closedSeconds + openSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(openSeconds));
            _closed = closedSeconds;
            _open = openSeconds;
            _bark = barkShare;
        }

        // Seconds on the running clock since its first step.
        public float Age { get; private set; }
        public bool IsOpen => BloomRule.Open(Age, _closed, _open);
        public int WindowsBegun => BloomRule.WindowsBegun(Age, _closed, _open);

        // A body back on the floor: closed, its clock at the start again.
        public void Reset() => Age = 0f;

        // One running frame.
        public void Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            Age += deltaTime;
        }

        // One landing of amount: what the Bloom takes of it now.
        public float Land(float amount) => BloomRule.DamageOn(amount, IsOpen, _bark);

        // Its walking speed now, from its own.
        public float Speed(float speed) => BloomRule.SpeedOf(speed, IsOpen);

        private static void RequireFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
