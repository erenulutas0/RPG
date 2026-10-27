using System;

namespace Cryptforge.Combat
{
    // Burn, the Cinder Arcanist's status (docs/45): the game's first damage over time. Pure, so whatever later simulates
    // a run burns the same way the scene does.
    //
    // The contract. A burning enemy carries up to MaxStacks stacks; each deals DamagePerStackPerSecond, paid in whole
    // ticks of TickSeconds of burning, and a new stack refreshes every stack's DurationSeconds. A stack comes either from
    // the hero's own hit (direct) or from a burning enemy that fell beside it (spread). An enemy that falls holding a
    // direct stack passes SpreadStacks to up to SpreadTargets living enemies within SpreadRadius, nearest first; an enemy
    // that only ever caught spread stacks passes nothing, so a chain is one generation deep - the bible's "recursive
    // limit". Crucible Vent spends a burning enemy's stacks at once: VentDamagePerStack for each, to it and to every
    // enemy within VentSplashRadius of it. First numbers, to be measured with the autopilot against the Vanguard.
    public static class BurnRule
    {
        public const int MaxStacks = 5;
        public const float DamagePerStackPerSecond = 3f;
        public const float TickSeconds = .5f;
        public const float DurationSeconds = 3f;
        public const int SpreadStacks = 1;
        public const int SpreadTargets = 4;
        // Floor units, the Staff's Cinder Nova burst's reach (KillBurstRule.Radius): the same idea of "beside it".
        public const float SpreadRadius = 1.6f;
        public const float VentSplashRadius = 1.2f;

        // What one tick of a burn with this many stacks deals.
        public static float TickDamage(int stacks)
        {
            if (stacks < 0 || stacks > MaxStacks)
                throw new ArgumentOutOfRangeException(nameof(stacks));
            return stacks * DamagePerStackPerSecond * TickSeconds;
        }

        // What Crucible Vent deals for this many spent stacks, at the ability's damage per stack.
        public static float VentDamage(int stacks, float damagePerStack)
        {
            if (stacks < 0 || stacks > MaxStacks)
                throw new ArgumentOutOfRangeException(nameof(stacks));
            if (float.IsNaN(damagePerStack) || float.IsInfinity(damagePerStack) || damagePerStack < 0f)
                throw new ArgumentOutOfRangeException(nameof(damagePerStack));
            return stacks * damagePerStack;
        }
    }

    // One enemy's burn: its stacks, how long they last and how far the next tick is. Plain state; the scene's BurnStatus
    // ticks it on combat time and deals what it returns.
    public sealed class BurnState
    {
        private const float TickSlack = 1e-4f;
        private float _tickClock;

        public int Stacks { get; private set; }
        // Seconds the stacks have left; zero once the burn is out.
        public float Remaining { get; private set; }
        // Whether any stack came from the hero's own hit, which is what lets a fall pass the burn on.
        public bool HasDirectStack { get; private set; }
        public bool IsBurning => Stacks > 0;
        // Whether this enemy, falling now, passes its burn to its neighbours.
        public bool Spreads => IsBurning && HasDirectStack;

        // Adds stacks up to the cap and refreshes the duration. spread: the stacks came from a neighbour's fall.
        public void Apply(int stacks, bool spread)
        {
            if (stacks < 1)
                throw new ArgumentOutOfRangeException(nameof(stacks));

            if (!IsBurning)
                _tickClock = 0f;
            Stacks = Math.Min(BurnRule.MaxStacks, Stacks + stacks);
            Remaining = BurnRule.DurationSeconds;
            if (!spread)
                HasDirectStack = true;
        }

        // Advances the burn by deltaTime of combat time and returns the damage its whole ticks deal. A burn that runs
        // out mid-tick pays nothing for the part tick.
        public float Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsBurning)
                return 0f;

            float burning = deltaTime < Remaining ? deltaTime : Remaining;
            Remaining -= deltaTime;
            _tickClock += burning;
            float damage = 0f;
            // A hair of slack, so frames that sum to a tick in float - sixty of 1/60 s - still pay it at the burn's end.
            while (_tickClock >= BurnRule.TickSeconds - TickSlack)
            {
                _tickClock -= BurnRule.TickSeconds;
                damage += BurnRule.TickDamage(Stacks);
            }
            if (Remaining <= 0f)
                Clear();
            return damage;
        }

        // Spends every stack at once (Crucible Vent) and returns how many there were.
        public int Consume()
        {
            int stacks = Stacks;
            Clear();
            return stacks;
        }

        public void Clear()
        {
            Stacks = 0;
            Remaining = 0f;
            HasDirectStack = false;
            _tickClock = 0f;
        }
    }
}
