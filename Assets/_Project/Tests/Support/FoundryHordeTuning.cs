using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cryptforge.Combat;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // The foundry horde's director with two of its rules as parameters, for the regression harness's levers (docs/41
    // step 5): the crowd each state aims for (HordeRule.TargetByState) and how fast it fills (HordeRule.MaxSpawnsPerSecond,
    // which HordeClock grows with the floor's clock). The game's HordeDirector reads both as constants, so a lever on
    // either needs this copy; everything else - the window, the credit, the gap, the cap, the role draw - is HordeDirector's
    // line for line, and every number it does not tune is read from HordeRule and HordeClock themselves. With no targets
    // and a rate scale of one it calls the game's own HordeRule.EffectiveTarget, HordeClock.SpawnRate and SpawnGap, and
    // FoundryHordeTuningTests holds it equal to HordeDirector spawn for spawn, so a lever is measured against a baseline
    // that is the game's. The foundry simulation uses HordeDirector itself whenever neither lever is set.
    internal sealed class TunedHordeDirector
    {
        private const int States = 4;
        private const int WindowBuckets = (int)HordeRule.WindowSeconds;

        private readonly int _cap;
        private readonly int _roleCount;
        private readonly RunRandom _random;
        private readonly int[] _weights;
        private readonly int[] _weightTotals;
        private readonly int[] _killBuckets = new int[WindowBuckets];
        private readonly int[] _spawnBuckets = new int[WindowBuckets];
        // The crowd per state, indexed by (int)HordeState; null keeps HordeRule.TargetByState.
        private readonly int[] _targets;
        // What the fill rate before the overrun is multiplied by; one keeps HordeClock.SpawnRate. The overrun's own rate
        // (HordeClock.OvertimeRate) is never scaled: the lever is the floor's pace, not its punishment.
        private readonly float _rateScale;
        // How much the crowd grows by the overrun (HordeClock.TargetGrowth unless a lever sets it, docs/41's open
        // decision 2).
        private readonly float _targetGrowth;
        private int _second;
        private float _credit;
        private float _nextSpawnAt;
        private HordeState _state;

        public float Seconds { get; private set; }
        public bool IsOvertime => HordeClock.IsOvertime(Seconds);
        public float HealthMultiplier => HordeClock.HealthMultiplier(Seconds);
        public float DamageMultiplier => HordeClock.DamageMultiplier(Seconds);
        public int Spawned { get; private set; }
        public int TargetNow { get; private set; }
        public int KillsInWindow { get; private set; }
        public int SpawnsInWindow { get; private set; }
        public HordeState State => _state;

        // Always clocked, as the scene's foundry horde is. targets: four crowds or null; rateScale: above zero.
        public TunedHordeDirector(int cap, int roleCount, int seed, int[] targets, float rateScale,
            float targetGrowth = HordeClock.TargetGrowth)
        {
            if (!(targetGrowth >= 0f) || float.IsInfinity(targetGrowth))
                throw new ArgumentOutOfRangeException(nameof(targetGrowth));
            if (cap < 1)
                throw new ArgumentOutOfRangeException(nameof(cap));
            if (roleCount < 1)
                throw new ArgumentOutOfRangeException(nameof(roleCount));
            if (targets != null && targets.Length != States)
                throw new ArgumentException("One crowd per state.", nameof(targets));
            if (!(rateScale > 0f) || float.IsInfinity(rateScale))
                throw new ArgumentOutOfRangeException(nameof(rateScale));

            _cap = cap;
            _roleCount = roleCount;
            _targets = targets != null ? (int[])targets.Clone() : null;
            _rateScale = rateScale;
            _targetGrowth = targetGrowth;
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

        public void SetState(HordeState state)
        {
            if (state < HordeState.Ramp || state > HordeState.Boss)
                throw new ArgumentOutOfRangeException(nameof(state));

            _state = state;
            Retarget();
        }

        public void Fell()
        {
            _killBuckets[_second % WindowBuckets]++;
            KillsInWindow++;
            Retarget();
        }

        public void Tick(float deltaTime, int alive, int otherAlive, List<HordeDirector.Spawn> spawns)
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

            float rate = Rate(Seconds);
            float gap = Gap(Seconds, rate);
            if (alive < TargetNow)
                _credit += deltaTime * rate;
            float creditCap = HordeRule.CreditCapSeconds * rate;
            if (_credit > creditCap)
                _credit = creditCap;

            float at = _nextSpawnAt > before ? _nextSpawnAt : before;
            while (_credit >= 1f && at <= Seconds
                   && alive + spawns.Count < TargetNow
                   && alive + otherAlive + spawns.Count < _cap)
            {
                _credit -= 1f;
                at += gap;
                spawns.Add(Take());
            }
            _nextSpawnAt = at;
        }

        // HordeClock.SpawnRate, scaled before the overrun.
        private float Rate(float seconds)
        {
            float rate = HordeClock.SpawnRate(seconds);
            return _rateScale == 1f || HordeClock.IsOvertime(seconds) ? rate : rate * _rateScale;
        }

        // HordeClock.SpawnGap at the shipped rate; its body on the scaled one.
        private float Gap(float seconds, float rate)
        {
            if (_rateScale == 1f || HordeClock.IsOvertime(seconds))
                return HordeClock.SpawnGap(seconds);
            float gap = 1f / rate;
            return gap < HordeRule.MinSpawnGapSeconds ? gap : HordeRule.MinSpawnGapSeconds;
        }

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
            TargetNow = _targets == null && _targetGrowth == HordeClock.TargetGrowth
                ? HordeRule.EffectiveTarget(_state, KillsInWindow, SpawnsInWindow, _cap, Seconds)
                : EffectiveTarget(_targets ?? HordeRule.TargetByState, _state, KillsInWindow, SpawnsInWindow, _cap, Seconds,
                    _targetGrowth);
        }

        // HordeRule.EffectiveTarget's clocked body over given crowds. Never inlined, as the original is not; the tests hold
        // it to the original at HordeRule.TargetByState over every state, window and second they try.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int EffectiveTarget(int[] targets, HordeState state, int killsInWindow, int spawnsInWindow, int cap,
            float clockSeconds, float targetGrowth = HordeClock.TargetGrowth)
        {
            float ratio = killsInWindow / (float)Math.Max(1, spawnsInWindow);
            if (ratio > 1f)
                ratio = 1f;
            int plain = (int)(targets[(int)state] * (1f + HordeRule.ReactiveShare * ratio) + 0.5f);
            plain = plain < cap ? plain : cap;
            if (state == HordeState.Boss)
                return plain;
            if (HordeClock.IsOvertime(clockSeconds))
                return cap;
            float grown = targets[(int)state] * TargetMultiplier(clockSeconds, targetGrowth);
            int clocked = (int)(grown * (1f + HordeRule.ReactiveShare * ratio) + 0.5f);
            return clocked < cap ? clocked : cap;
        }

        // HordeClock.TargetMultiplier with its growth as a parameter: the game's own at the shipped growth, else its body.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static float TargetMultiplier(float seconds, float growth)
        {
            if (growth == HordeClock.TargetGrowth)
                return HordeClock.TargetMultiplier(seconds);
            if (float.IsNaN(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            float progress = seconds < HordeClock.TargetSeconds ? seconds / HordeClock.TargetSeconds : 1f;
            return 1f + growth * progress;
        }

        private HordeDirector.Spawn Take()
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
            return new HordeDirector.Spawn(role, Spawned++);
        }
    }

    // The horde's experience share as a lever: HordeRule.Experience's body with the share as a parameter. The game's
    // own HordeRule.Experience is called whenever the share is the shipped one; the tests hold this to it there.
    internal static class TunedHordeExperience
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Experience(int authored, float share)
        {
            if (share == HordeRule.ExperienceShare)
                return HordeRule.Experience(authored);
            if (authored < 0)
                throw new ArgumentOutOfRangeException(nameof(authored));
            return authored == 0 ? 0 : Math.Max(1, (int)(authored * share + 0.5f));
        }

        // The same body at any share, for the test that holds it to HordeRule.Experience at the shipped one.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int Copy(int authored, float share) =>
            authored == 0 ? 0 : Math.Max(1, (int)(authored * share + 0.5f));
    }
}
