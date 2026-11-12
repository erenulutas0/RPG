using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    // The Mirror Sentry's mirror in the scene (MirrorGuardRule; docs/50 section 5): on the Sentry's prefab, ahead of its
    // Health (Health.Guard), it judges each landing of the hero's - a context whose source is the hero, not a burn's tick -
    // by where the hero stands against its facing: from the front the Sentry takes MirrorGuardRule.FrontShare of it and the
    // hero is owed a reflection, from the flanks and the rear the landing is whole. MirrorGuardService turns it and refills
    // its budget each running frame, at the Warden's point of the frame (after EncounterController, before every attacker),
    // and pays the reflection it owes the next frame, as the foundry simulation does (DescentSimulation.Vaults.cs). Until
    // the service has stepped it - the frame it walks in, off the foundry, in the hall arena - it guards nothing and the
    // Sentry fights as authored. Its mirror's look is MirrorGlintView's.
    [RequireComponent(typeof(Health))]
    public sealed class MirrorGuard : MonoBehaviour, IDamageGuard
    {
        // Every mirror standing on the floor, in the order they stood up: what the service steps and the view draws.
        private static readonly List<MirrorGuard> Standing = new List<MirrorGuard>(2);

        private Health _health;
        private EncounterController _encounter;
        private Health _hero;
        private Action<MirrorGuard, float> _owe;

        public MirrorGuardState State { get; } = new MirrorGuardState();
        public Health Body => _health != null ? _health : _health = GetComponent<Health>();
        // The frame it stood up on: the service leaves it unstepped through that frame.
        internal int StoodUpFrame { get; private set; } = -1;
        // The landings of the hero's it judged, and those made from its front, since it stood up.
        public int Landings { get; private set; }
        public int FrontLandings { get; private set; }

        public static int StandingCount => Standing.Count;
        public static MirrorGuard StandingAt(int index) => Standing[index];

        private void Awake()
        {
            Body.Guard = this;
        }

        private void OnEnable()
        {
            State.Reset();
            Landings = 0;
            FrontLandings = 0;
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

        // The fight it guards in: where it and the hero stand, and who it owes a reflection to (MirrorGuardService.Owe).
        internal void Arm(EncounterController encounter, Health hero, Action<MirrorGuard, float> owe)
        {
            _encounter = encounter;
            _hero = hero;
            _owe = owe;
        }

        public DamageContext Guard(DamageContext context)
        {
            if (!State.IsGuarding || _hero == null || _encounter == null || context.IsTick ||
                !ReferenceEquals(context.Source, _hero) || !_encounter.TryFloorPositionOf(Body, out float x, out float y))
                return context;
            float dealt = State.Land(context.Amount, x, y, _encounter.HeroFloorX, _encounter.HeroFloorY, out bool front,
                out float reflected);
            Landings++;
            if (!front)
                return context;
            FrontLandings++;
            if (reflected > 0f)
                _owe?.Invoke(this, reflected);
            return new DamageContext(dealt, context.Source, context.IsCritical);
        }
    }
}
