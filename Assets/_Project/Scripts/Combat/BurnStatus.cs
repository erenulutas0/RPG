using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Burn on one enemy in the scene (BurnRule, BurnState; the Cinder Arcanist, docs/45). Added to an enemy the first
    // time it catches fire, it ticks the burn on combat time - a pause or an open choice holds it - and deals the burn's
    // damage as the hero's, so a burn kill drops and pays like any other. When the enemy falls holding a stack from the
    // hero's own hit, it passes one stack to its nearest living neighbours; spread stacks pass nothing on.
    public sealed class BurnStatus : MonoBehaviour
    {
        // Every burn in the scene, so a run's end can put them all out at once.
        private static readonly List<BurnStatus> Live = new List<BurnStatus>();
        private Health _health;
        private IDamageable _source;
        private INeighbourhood _neighbourhood;

        public BurnState State { get; } = new BurnState();
        public bool IsBurning => State.IsBurning;
        public int Stacks => State.Stacks;

        // Every burn tick that dealt damage, every stack passed on a fall, and every enemy a burn tick felled - for the
        // run's telemetry and the tests.
        public static event Action<Health, float> Burned;
        public static event Action<Health, Health> Spread;
        public static event Action<Health> BurnKill;

        // The enemy's burn, added on its first stack. Null for a missing or fallen enemy, and for one off the floor: a
        // strike's StruckWith lands after its TryAttack returns, when the strike's own kill may have released the body (the
        // gate opening on the Warden's fall), and a burn put on it then would ride the horde's pool into its next life with
        // no one to put it out (the pool's review, 2026-11-11).
        public static BurnStatus On(Health enemy)
        {
            if (enemy == null || !enemy.IsAlive || !enemy.isActiveAndEnabled)
                return null;
            BurnStatus status = enemy.GetComponent<BurnStatus>();
            if (status == null)
            {
                status = enemy.gameObject.AddComponent<BurnStatus>();
                status._health = enemy;
                enemy.Died += status.OnDied;
                Live.Add(status);
            }
            return status;
        }

        // Sets the enemy alight: stacks from the hero's hit (spread false) or a neighbour's fall (spread true). source is
        // whose damage the burn deals; neighbourhood finds who stands beside it when it falls.
        // hold: the encounter's fight hold (EncounterController.FightHold) - while it reads true, the burn waits.
        public void Ignite(int stacks, bool spread, IDamageable source, INeighbourhood neighbourhood, Func<bool> hold = null)
        {
            if (_health == null || !_health.IsAlive)
                return;
            State.Apply(stacks, spread);
            _source = source;
            _neighbourhood = neighbourhood;
            if (hold != null)
                _hold = hold;
        }

        private Func<bool> _hold;

        // Crucible Vent: every stack spent at once; how many there were.
        public int Consume() => State.Consume();

        // The run has ended (CombatSetup, on Run.Ended): every burn goes out, so nothing dies behind the result screen
        // and no burn kill advances a wave, opens a gate or clears a floor after a defeat or an extract.
        public static void ExtinguishAll()
        {
            for (int i = 0; i < Live.Count; i++)
                Live[i].State.Clear();
        }

        private void Update()
        {
            if (!State.IsBurning || _health == null || !_health.IsAlive || Time.deltaTime <= 0f)
                return;
            // A burn is the hero's damage: once the hero has fallen, it deals no more.
            if (_source is Health hero && !hero.IsAlive)
            {
                State.Clear();
                return;
            }
            // The boss's reveal holds the fight (AttackController.FightHold), and the burn with it: no tick, no kill, no
            // spread while the camera is at the gate, and the burn keeps the time it had left (the pre-push review,
            // 2026-11-12: burns ticked through the ~2.6 s reveal while every attacker waited).
            float damage = State.Tick(Time.deltaTime, _hold != null && _hold());
            if (damage <= 0f)
                return;
            // A tick, not a landing: the Mirror Sentry's guard lets it through (MirrorGuardRule), as the simulation's does.
            _health.ApplyDamage(new DamageContext(damage, _source, isTick: true));
            Burned?.Invoke(_health, damage);
            if (!_health.IsAlive)
                BurnKill?.Invoke(_health);
        }

        private void OnDied()
        {
            bool spreads = State.Spreads;
            State.Clear();
            if (!spreads || _neighbourhood == null)
                return;
            var near = _neighbourhood.Near(_health, BurnRule.SpreadRadius);
            // Gather first: igniting a neighbour never asks the neighbourhood again, but the list is the caller's to reuse.
            int count = Math.Min(BurnRule.SpreadTargets, near.Count);
            for (int i = 0; i < count; i++)
            {
                if (!(near[i] is Health neighbour))
                    continue;
                BurnStatus status = On(neighbour);
                if (status == null)
                    continue;
                status.Ignite(BurnRule.SpreadStacks, true, _source, _neighbourhood, _hold);
                Spread?.Invoke(_health, neighbour);
            }
        }

        private void OnDestroy()
        {
            Live.Remove(this);
            if (_health != null)
                _health.Died -= OnDied;
        }
    }

    // Who stands beside a burning enemy that falls, gathered into a list of the burn's own. A burn must never borrow the
    // AttackController's neighbourhood: its list is the one a Cinder Nova burst is still walking when the burst fells a
    // burning enemy, and refilling it from that enemy's fall would send the rest of the burst to the wrong enemies
    // (review, 2026-10-27).
    public sealed class TargetingNeighbourhood : INeighbourhood
    {
        private readonly Targeting _targeting;
        private readonly List<IDamageable> _near = new List<IDamageable>();

        public TargetingNeighbourhood(Targeting targeting)
        {
            _targeting = targeting != null ? targeting : throw new ArgumentNullException(nameof(targeting));
        }

        public IReadOnlyList<IDamageable> Near(IDamageable centre, float radius)
        {
            _targeting.CollectNear(centre as Health, radius, _near);
            return _near;
        }
    }
}
