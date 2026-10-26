using System;
using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // The foundry gate boss's slams in the scene (WardenRule): from the moment the Warden has walked out of the gate and
    // the camera has come back, the clock marks where the hero stands and the hammer lands there a telegraph later. Only
    // the foundry's gate boss slams; the hall's Warden, the Descent simulation and every other enemy fight as authored.
    public sealed class WardenSlamService : MonoBehaviour
    {
        private CombatSetup _setup;
        private Health _hero;
        private Health _boss;
        private readonly WardenSlamClock _clock = new WardenSlamClock();

        public WardenSlamClock Clock => _clock;
        public Health Boss => _boss != null && _boss.IsAlive ? _boss : null;
        public bool IsFighting => Boss != null;
        public bool IsEnraged { get; private set; }
        // Where the mark is down, on the floor.
        public float MarkX { get; private set; }
        public float MarkY { get; private set; }
        public event Action Marked;
        // A blow has landed; whether it caught the hero.
        public event Action<bool> Slammed;
        public bool LastSlamHit { get; private set; }
        // How far from the mark's centre the hero stood when the last blow landed, on the floor.
        public float LastSlamDistance { get; private set; }

        internal void Initialize(CombatSetup setup, Health hero)
        {
            _setup = setup;
            _hero = hero;
        }

        private void Update()
        {
            EncounterController encounter = _setup.Encounters;
            // The gate's boss is read, not waited for: on a floor that opens on its boss it walks out while the scene
            // is still being set up, before anything could listen.
            Health gate = encounter.GateBoss;
            if (gate != _boss)
            {
                _boss = gate;
                IsEnraged = false;
                encounter.SetBossEnraged(false);
                _clock.Reset();
            }
            Health boss = Boss;
            if (boss == null)
                return;
            if (_setup.Run.HasEnded || Time.deltaTime <= 0f || _setup.Choices.IsOpen || encounter.IsRevealHeld)
                return;

            if (!IsEnraged && WardenRule.IsEnraged(boss.Current, boss.Maximum))
            {
                IsEnraged = true;
                encounter.SetBossEnraged(true);
            }
            switch (_clock.Advance(Time.deltaTime, IsEnraged))
            {
                case WardenSlamClock.Step.Marked:
                    MarkX = _hero.transform.position.x;
                    MarkY = ArenaFloor.FloorY(_hero.transform.position.y);
                    Marked?.Invoke();
                    break;
                case WardenSlamClock.Step.Slammed:
                    float heroX = _hero.transform.position.x, heroY = ArenaFloor.FloorY(_hero.transform.position.y);
                    float distanceSquared = ArenaFloor.FloorDistanceSquared(heroX, heroY, MarkX, MarkY);
                    LastSlamDistance = Mathf.Sqrt(distanceSquared);
                    LastSlamHit = _hero.IsAlive && distanceSquared <= WardenRule.SlamRadius * WardenRule.SlamRadius;
                    // The Warden is the source, so the result screen names it if the blow is the last.
                    if (LastSlamHit)
                        _hero.ApplyDamage(new DamageContext(_hero.Maximum * WardenRule.HeroShare, boss));
                    Slammed?.Invoke(LastSlamHit);
                    break;
            }
        }
    }
}
