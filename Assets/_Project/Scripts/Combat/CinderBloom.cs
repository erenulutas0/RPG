using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    // The Cinder Bloom's bark in the scene (BloomRule, BloomClock; docs/52 section 5, `bloom-m20-h70-r1.2`): on the Bloom's
    // prefab, ahead of its Health (Health.Guard), it lets BloomRule.BarkShare of every landing through while the Bloom is
    // closed and the whole landing while it is open - every landing that reaches its health, a burn's tick included (the
    // lead's decision 2026-11-15: the scene and the simulation equal). GloamService steps its clock each running frame at
    // the point of the frame the foundry simulation steps it (DescentSimulation.Gloam.cs), roots it while open and holds its
    // maul. Until the service has stepped it - the frame it walks in, off the foundry, in the hall arena - it guards nothing
    // and the Bloom fights as authored. Its open-state look is CinderBloomView's.
    [RequireComponent(typeof(Health))]
    public sealed class CinderBloom : MonoBehaviour, IDamageGuard
    {
        // Every Bloom standing on the floor, in the order they stood up: what the service steps and the view reads.
        private static readonly List<CinderBloom> Standing = new List<CinderBloom>(1);

        private Health _health;

        public BloomClock Clock { get; } = new BloomClock();
        public Health Body => _health != null ? _health : _health = GetComponent<Health>();
        // The frame it stood up on: the service leaves it unstepped through that frame.
        internal int StoodUpFrame { get; private set; } = -1;
        // Whether the service has stepped its clock since it stood up: the bark and the cycle run only then.
        public bool IsCycling { get; internal set; }
        public bool IsOpen => IsCycling && Clock.IsOpen;
        // The hero it fights, handed over by GloamService with its first step: the rooted, open Bloom faces it
        // (CinderBloomView).
        public Health Hero { get; internal set; }
        // The landings it judged while closed and while open, and what the bark took off them, since it stood up.
        public int ClosedLandings { get; private set; }
        public int OpenLandings { get; private set; }
        public float Barked { get; private set; }
        // A landing the closed bark blunted (for CinderBloomView's chip): the amount it took and what it let through.
        public event System.Action<float, float> BarkStruck;

        public static int StandingCount => Standing.Count;
        public static CinderBloom StandingAt(int index) => Standing[index];

        private void Awake()
        {
            Body.Guard = this;
        }

        private void OnEnable()
        {
            Clock.Reset();
            IsCycling = false;
            ClosedLandings = 0;
            OpenLandings = 0;
            Barked = 0f;
            StoodUpFrame = Time.frameCount;
            if (!Standing.Contains(this))
                Standing.Add(this);
        }

        private void OnDisable()
        {
            Standing.Remove(this);
        }

        private void OnDestroy()
        {
            Standing.Remove(this);
            if (_health != null && ReferenceEquals(_health.Guard, this))
                _health.Guard = null;
        }

        public DamageContext Guard(DamageContext context)
        {
            if (!IsCycling)
                return context;
            if (Clock.IsOpen)
            {
                OpenLandings++;
                return context;
            }
            float taken = Clock.Land(context.Amount);
            ClosedLandings++;
            Barked += context.Amount - taken;
            BarkStruck?.Invoke(context.Amount, taken);
            return new DamageContext(taken, context.Source, context.IsCritical, context.IsTick);
        }
    }
}
