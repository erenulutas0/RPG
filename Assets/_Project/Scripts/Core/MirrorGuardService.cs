using System;
using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // The Mirror Sentries' guards in the scene (MirrorGuardRule, MirrorGuard; docs/50 section 5): each running frame every
    // standing Sentry's mirror refills its reflection budget and turns toward the hero, and the reflection the hero's
    // landings were owed since the last frame is paid as one hit on the hero with the Sentry as its source - the next frame,
    // never inside the hero's own strike, as the foundry simulation pays it (DescentSimulation.Vaults.cs). The clock holds
    // whenever the encounter holds - an open choice, the boss's reveal, the player's pause - as the Warden's slams and the
    // burns do, and so does the payment (MirrorReflectionDue; the lead's decision 2026-11-12): the hero loses no health
    // while it chooses, and what was owed is paid as the clock resumes. Only on the foundry: off it no mirror is ever
    // stepped, so the hall arena's Sentry fights as authored, as its elites and its Warden do.
    //
    // It runs after WardenSlamService (-40) and so after EncounterController (-50), which moves the packs and brings each
    // wave in, and before every script of the default order, every AttackController among them: a mirror has turned before
    // the strikes of the frame land on it, at the point of the frame where the simulation steps it
    // (FoundrySimulationSceneValueTests.TheScenesScriptOrderIsTheSimulations).
    [DefaultExecutionOrder(-39)]
    public sealed class MirrorGuardService : MonoBehaviour
    {
        private CombatSetup _setup;
        private Health _hero;
        private Action<MirrorGuard, float> _owe;
        private readonly MirrorReflectionDue<MirrorGuard> _due = new MirrorReflectionDue<MirrorGuard>();

        public bool IsActive { get; private set; }
        // A reflection has landed on the hero: the mirror it came from and the health it asked for (before the hero's armour).
        public event Action<MirrorGuard, float> Reflected;
        // Every reflection paid this run, in health asked.
        public float ReflectedTotal { get; private set; }

        internal void Initialize(CombatSetup setup, Health hero)
        {
            _setup = setup;
            _hero = hero;
            var arena = FindFirstObjectByType<ArenaView>();
            IsActive = arena != null && arena.IsFoundryRegion;
        }

        private void Update()
        {
            if (!IsActive || _setup == null || _setup.Run == null || _setup.Run.HasEnded)
                return;
            EncounterController encounter = _setup.Encounters;
            bool held = Time.deltaTime <= 0f || _setup.Choices.IsOpen || encounter.IsRevealHeld;
            // What the landings banked since the last running frame (MirrorReflectionDue): nothing while the fight holds.
            float owed = _due.Take(!held && _hero.IsAlive, out MirrorGuard from);
            if (owed > 0f)
            {
                ReflectedTotal += owed;
                // The Sentry is the source, so the result screen names it if the reflection is the last hit.
                _hero.ApplyDamage(new DamageContext(owed, from != null ? from.Body : null));
                Reflected?.Invoke(from, owed);
            }
            if (held)
                return;
            float heroX = encounter.HeroFloorX, heroY = encounter.HeroFloorY;
            int frame = Time.frameCount;
            for (int i = 0; i < MirrorGuard.StandingCount; i++)
            {
                MirrorGuard guard = MirrorGuard.StandingAt(i);
                Health body = guard.Body;
                if (guard.StoodUpFrame == frame || body == null || !body.IsAlive ||
                    !encounter.TryFloorPositionOf(body, out float x, out float y))
                    continue;
                guard.Arm(encounter, _hero, _owe ??= Owe);
                guard.State.Step(Time.deltaTime, x, y, heroX, heroY, _hero.Maximum);
            }
        }

        // A landing of the hero's on a mirror's front owes the hero this much, paid on the next running frame.
        private void Owe(MirrorGuard guard, float amount) => _due.Owe(amount, guard);

        // What the hero is owed and not yet paid (banked while the fight holds).
        public float Owed => _due.Owed;
    }
}
