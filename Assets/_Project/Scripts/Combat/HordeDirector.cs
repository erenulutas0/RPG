using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cryptforge.Progression;

namespace Cryptforge.Combat
{
    // Where the floor's horde stands in its arc. The foundry keeps its six nodes - three combat halls, the forge, the
    // elite and the boss gate - as objectives, and the horde walks the whole map round them (the owner, 2026-10-26: the
    // Megabonk-style hybrid of docs/27, after the hall surges alone left the walk between nodes empty).
    public enum HordeState
    {
        // Nothing cleared yet: a thin crowd that teaches the walk without deciding it.
        Ramp = 0,
        // One node down: the crowd a Megabonk minute looks like.
        Horde = 1,
        // Two or more down: the floor leans on the hero all the way to the gate.
        Pressure = 2,
        // The boss gate is open: the boss room is the fight, and the horde thins so the boss can be read on a phone.
        Boss = 3
    }

    // How a floor's continuous horde paces itself. Pure, so the scene and the simulation agree on when an enemy walks in,
    // and on a phone the thing that must be controlled is how many stand at once, not how fast they arrive: a Samsung
    // S23 is the first prototype's floor, at 30-40 active enemies (the owner, 2026-10-26). Every number here is a
    // starting value, to be measured against the device log the way the swarm's were (docs/35, docs/36).
    public static class HordeRule
    {
        // How many seconds of kills and spawns the director remembers when it judges how fast the hero is killing. A
        // short window, so the boost follows the fight the hero is in now and not the one a minute ago. Whole seconds:
        // the memory is a per-second ring, so a kill is forgotten between WindowSeconds - 1 and WindowSeconds after it.
        public const float WindowSeconds = 5f;
        // How fast the horde may fill towards its target. Two a second is the rate the hall surges measured as a 5 s beat
        // of 10, and the crowd on a phone must grow in a way the eye can follow, not appear.
        public const float MaxSpawnsPerSecond = 2f;
        // Spawn credit never holds more than this many seconds' worth of spawns, so a frozen frame or a lag spike - a
        // level-up sitting, a scene load, the phone's thermal stall - never bursts a crowd onto the floor when it ends.
        public const float CreditCapSeconds = 1.5f;
        // Two spawns are never closer than this, even with credit to spare: the scene places each one, and a phone
        // instancing four in a frame is a hitch the device log would show.
        public const float MinSpawnGapSeconds = 0.25f;

        // The standing crowd each state aims for, indexed by (int)HordeState: how many horde enemies may stand at once
        // before the director stops filling. A target and not a rate, because the alive count is what the phone pays for.
        // Boss sits below Horde on purpose: the boss room must stay readable (docs/27).
        public static readonly int[] TargetByState = { 6, 12, 18, 8 };

        // How much of itself the target may grow when the hero is killing as fast as the horde arrives: at a kill ratio of
        // one the crowd aims half again higher, so a hero who clears the floor meets more of it, and a hero who is
        // struggling is not buried (docs/27, Megabonk's own pressure curve). Never past the cap.
        public const float ReactiveShare = 0.5f;

        // The most enemies the scene lets stand at once, horde and node waves together: the low end of the owner's first
        // range (2026-10-26: 30-40 on the S23, raised by measurement) until the density probe has its number.
        public const int SceneCap = 30;
        // A horde enemy pays this share of its authored experience and no gold. The horde is the floor's pressure, not its
        // purse: at full experience a kill a second opened a card offer every second or two at the floor's opening levels,
        // and its gold would have flooded the forge's economy, which the node waves and chests carry (docs/41).
        public const float ExperienceShare = 0.3f;
        // How long a fallen horde enemy's body stays on the floor before it is taken away.
        public const float RetireSeconds = 1.5f;

        // The RunRandom purpose the horde's role draws come from. RunRandom lists its own purposes one to four; this is
        // the next, kept here so the horde slice touches no existing file. It must stay unique among them.
        public const int RandomPurpose = 5;

        // The state a floor's horde is in from how far the hero has got: the first node cleared moves it from Ramp to
        // Horde, the second to Pressure, and the gate opening overrides everything.
        public static HordeState StateFor(int nodesCleared, bool bossOpen)
        {
            if (nodesCleared < 0)
                throw new ArgumentOutOfRangeException(nameof(nodesCleared));

            if (bossOpen)
                return HordeState.Boss;
            if (nodesCleared == 0)
                return HordeState.Ramp;
            return nodesCleared == 1 ? HordeState.Horde : HordeState.Pressure;
        }

        // A role's weight in a state: the lightest role leads the opening, and each state further on gives every heavier
        // role one more share. The boss room weights the roles as the opening does, so its minions stay light.
        public static int Weight(HordeState state, int role)
        {
            if (state < HordeState.Ramp || state > HordeState.Boss)
                throw new ArgumentOutOfRangeException(nameof(state));
            if (role < 0)
                throw new ArgumentOutOfRangeException(nameof(role));
            int pressure = state == HordeState.Boss ? 0 : (int)state;
            return Math.Max(0, 2 + pressure - role);
        }

        // The experience a horde enemy pays, from its authored reward: ExperienceShare of it, rounded, at least one.
        public static int Experience(int authored)
        {
            if (authored < 0)
                throw new ArgumentOutOfRangeException(nameof(authored));
            return authored == 0 ? 0 : Math.Max(1, (int)(authored * ExperienceShare + 0.5f));
        }

        // The crowd the director aims for now: the state's target, raised by ReactiveShare times the window's kill ratio
        // (kills over spawns, clamped to one), rounded to the nearest, and never above the cap. One body that is never
        // inlined, so the scene and the simulation round the same float the same way (docs/21, the targeting divergence).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int EffectiveTarget(HordeState state, int killsInWindow, int spawnsInWindow, int cap)
        {
            if (state < HordeState.Ramp || state > HordeState.Boss)
                throw new ArgumentOutOfRangeException(nameof(state));
            if (killsInWindow < 0)
                throw new ArgumentOutOfRangeException(nameof(killsInWindow));
            if (spawnsInWindow < 0)
                throw new ArgumentOutOfRangeException(nameof(spawnsInWindow));
            if (cap < 1)
                throw new ArgumentOutOfRangeException(nameof(cap));

            float ratio = killsInWindow / (float)Math.Max(1, spawnsInWindow);
            if (ratio > 1f)
                ratio = 1f;
            int target = (int)(TargetByState[(int)state] * (1f + ReactiveShare * ratio) + 0.5f);
            return target < cap ? target : cap;
        }
    }

    // Decides, frame by frame, when a horde enemy walks onto the floor and which role it has. Pure and shared, modelled
    // on SwarmDirector: the scene reports what stands and what fell, and reads back a list of spawns to place.
    //
    // The director fills towards a target crowd and no faster than its credit allows. Credit grows at MaxSpawnsPerSecond
    // while the crowd is below target, is capped so a stalled frame cannot burst, and is spent one whole spawn at a time
    // with MinSpawnGapSeconds between them. Node-wave enemies stand on the same floor, so they count towards the phone's
    // cap but not towards the horde's target: a hall fight is allowed to crowd the horde out, never the other way round.
    // Nothing is allocated after construction; the scene calls Tick every frame.
    public sealed class HordeDirector
    {
        public readonly struct Spawn
        {
            // Which role of the floor's horde table walks in.
            public int Role { get; }
            // Which spawn of the director's life this is, from zero.
            public int Ordinal { get; }

            public Spawn(int role, int ordinal)
            {
                Role = role;
                Ordinal = ordinal;
            }
        }

        private const int States = 4;
        private const int WindowBuckets = (int)HordeRule.WindowSeconds;

        private readonly int _cap;
        private readonly int _roleCount;
        private readonly RunRandom _random;
        // Role weights per state, flat: state * roleCount + role. Uniform until SetRoleWeights says otherwise.
        private readonly int[] _weights;
        private readonly int[] _weightTotals;
        // The window's per-second ring: bucket s % WindowBuckets holds second s. The sums are kept alongside.
        private readonly int[] _killBuckets = new int[WindowBuckets];
        private readonly int[] _spawnBuckets = new int[WindowBuckets];
        // The whole second the ring has been advanced to.
        private int _second;
        // Spawns owed, in whole and part spawns.
        private float _credit;
        // The clock time the next spawn is allowed; the gap is measured from it.
        private float _nextSpawnAt;
        private HordeState _state;

        // Seconds the horde has been running.
        public float Seconds { get; private set; }
        // Enemies spawned so far.
        public int Spawned { get; private set; }
        // The crowd the director aims for now, the reactive boost applied, never above the cap.
        public int TargetNow { get; private set; }
        // Horde deaths and spawns inside the last WindowSeconds.
        public int KillsInWindow { get; private set; }
        public int SpawnsInWindow { get; private set; }
        public HordeState State => _state;

        // cap: the most enemies the floor may hold at once, horde and node waves together (the scene passes 30-40).
        // roleCount: how many roles the floor's horde table has. seed: the run's, so the horde reproduces with the run.
        public HordeDirector(int cap, int roleCount, int seed)
        {
            if (cap < 1)
                throw new ArgumentOutOfRangeException(nameof(cap));
            if (roleCount < 1)
                throw new ArgumentOutOfRangeException(nameof(roleCount));

            _cap = cap;
            _roleCount = roleCount;
            // One stream, drawn in order. The run's other consumers derive a stream per draw (a chest, an attack), but
            // Stream allocates and the horde draws many times a second for a whole floor; one generator seeded from the
            // run is as reproducible, because every draw is this director's and they happen in Tick order.
            _random = RunRandom.Stream(seed, HordeRule.RandomPurpose, 0);
            _weights = new int[States * roleCount];
            _weightTotals = new int[States];
            for (int i = 0; i < _weights.Length; i++)
                _weights[i] = 1;
            for (int s = 0; s < States; s++)
                _weightTotals[s] = roleCount;
            _state = HordeState.Ramp;
            Retarget();
        }

        // How the roles are weighted in one state. weights has one entry per role, none negative, at least one above zero.
        public void SetRoleWeights(HordeState state, int[] weights)
        {
            if (state < HordeState.Ramp || state > HordeState.Boss)
                throw new ArgumentOutOfRangeException(nameof(state));
            if (weights == null)
                throw new ArgumentNullException(nameof(weights));
            if (weights.Length != _roleCount)
                throw new ArgumentException("One weight per role of the horde table.", nameof(weights));

            int total = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(weights));
                total += weights[i];
            }
            if (total < 1)
                throw new ArgumentException("At least one role must have weight.", nameof(weights));

            int offset = (int)state * _roleCount;
            for (int i = 0; i < weights.Length; i++)
                _weights[offset + i] = weights[i];
            _weightTotals[(int)state] = total;
        }

        // The floor has moved on; the horde aims for its new state's crowd from the next Tick.
        public void SetState(HordeState state)
        {
            if (state < HordeState.Ramp || state > HordeState.Boss)
                throw new ArgumentOutOfRangeException(nameof(state));

            _state = state;
            Retarget();
        }

        // The scene reports a horde enemy's death: it counts in the window and raises the target while it is remembered.
        public void Fell()
        {
            _killBuckets[_second % WindowBuckets]++;
            KillsInWindow++;
            Retarget();
        }

        // One frame of the floor. alive is how many horde enemies stand, otherAlive how many node-wave enemies stand
        // beside them; whatever is due walks into spawns.
        public void Tick(float deltaTime, int alive, int otherAlive, List<Spawn> spawns)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (alive < 0)
                throw new ArgumentOutOfRangeException(nameof(alive));
            if (otherAlive < 0)
                throw new ArgumentOutOfRangeException(nameof(otherAlive));
            if (spawns == null)
                throw new ArgumentNullException(nameof(spawns));

            spawns.Clear();
            float before = Seconds;
            Seconds += deltaTime;
            Advance((int)Seconds);
            Retarget();

            // Credit grows only while the crowd is short, and never past the cap: a frame that lasted ten seconds owes
            // the floor a second and a half of spawns, not ten seconds of them.
            if (alive < TargetNow)
                _credit += deltaTime * HordeRule.MaxSpawnsPerSecond;
            float creditCap = HordeRule.CreditCapSeconds * HordeRule.MaxSpawnsPerSecond;
            if (_credit > creditCap)
                _credit = creditCap;

            // Spawns are spaced on the clock, so a long frame emits the ones its span allowed and no more, and the next
            // frame picks up the gap where this one left it.
            float at = _nextSpawnAt > before ? _nextSpawnAt : before;
            while (_credit >= 1f && at <= Seconds
                   && alive + spawns.Count < TargetNow
                   && alive + otherAlive + spawns.Count < _cap)
            {
                _credit -= 1f;
                at += HordeRule.MinSpawnGapSeconds;
                spawns.Add(Take());
            }
            _nextSpawnAt = at;
        }

        // Drops the window's buckets the clock has left behind.
        private void Advance(int second)
        {
            if (second - _second >= WindowBuckets)
            {
                for (int i = 0; i < WindowBuckets; i++)
                {
                    _killBuckets[i] = 0;
                    _spawnBuckets[i] = 0;
                }
                KillsInWindow = 0;
                SpawnsInWindow = 0;
                _second = second;
                return;
            }
            while (_second < second)
            {
                _second++;
                int bucket = _second % WindowBuckets;
                KillsInWindow -= _killBuckets[bucket];
                SpawnsInWindow -= _spawnBuckets[bucket];
                _killBuckets[bucket] = 0;
                _spawnBuckets[bucket] = 0;
            }
        }

        private void Retarget()
        {
            TargetNow = HordeRule.EffectiveTarget(_state, KillsInWindow, SpawnsInWindow, _cap);
        }

        // One spawn: a role drawn by the state's weights, counted in the window and in the director's life.
        private Spawn Take()
        {
            int offset = (int)_state * _roleCount;
            int draw = _random.NextBelow(_weightTotals[(int)_state]);
            int role = 0;
            while (role < _roleCount - 1 && draw >= _weights[offset + role])
            {
                draw -= _weights[offset + role];
                role++;
            }
            _spawnBuckets[_second % WindowBuckets]++;
            SpawnsInWindow++;
            return new Spawn(role, Spawned++);
        }
    }
}
