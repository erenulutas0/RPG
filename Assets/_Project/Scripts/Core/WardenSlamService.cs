using System;
using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // The foundry gate boss's slams in the scene (WardenRule): from the moment the Warden has walked out of the gate and
    // the camera has come back, the clock marks where the hero stands and the hammer lands there a telegraph later. From
    // the mark until the hammer is out of the floor again the Warden's own weapon is held (WardenSlamClock.HoldsWeapon).
    // Only the foundry's gate boss slams; the hall's Warden, the Descent simulation and every other enemy fight as
    // authored. It also hands the gate boss to the hero's attacker (AttackController.GateBoss), so a weapon's landings on
    // it take the weapon's share (WardenRule.SwordShare: the Sword is the Warden's weapon).
    //
    // It runs after EncounterController (-50), which brings the gate boss out, and before every script of the default
    // order, every AttackController among them: the Warden's weapon is held from the very frame its mark goes down, not
    // one frame later as Unity happens to order the two, and the foundry simulation steps the clock at the same point of
    // its frame (DescentSimulation.Foundry, FoundrySimulationSceneValueTests).
    [DefaultExecutionOrder(-40)]
    public sealed class WardenSlamService : MonoBehaviour
    {
        private CombatSetup _setup;
        private Health _hero;
        private AttackController _heroAttack;
        private Health _boss;
        private AttackController _bossAttack;
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

        internal void Initialize(CombatSetup setup, Health hero, AttackController heroAttack)
        {
            _setup = setup;
            _hero = hero;
            _heroAttack = heroAttack;
        }

        private void Update()
        {
            EncounterController encounter = _setup.Encounters;
            // The gate's boss is read, not waited for: on a floor that opens on its boss it walks out while the scene
            // is still being set up, before anything could listen.
            Health gate = encounter.GateBoss;
            if (gate != _boss)
            {
                Hold(false);
                _boss = gate;
                _bossAttack = gate != null ? gate.GetComponent<AttackController>() : null;
                if (_heroAttack != null)
                    _heroAttack.GateBoss = gate;
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
            Hold(_clock.HoldsWeapon);
        }

        // The Warden's weapon waits while its hammer is going up or is in the floor.
        private void Hold(bool held)
        {
            if (_bossAttack != null)
                _bossAttack.IsHeld = held;
        }
    }
}
