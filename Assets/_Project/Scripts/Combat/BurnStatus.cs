using System;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Burn on one enemy in the scene (BurnRule, BurnState; the Cinder Arcanist, docs/45). Added to an enemy the first
    // time it catches fire, it ticks the burn on combat time - a pause or an open choice holds it - and deals the burn's
    // damage as the hero's, so a burn kill drops and pays like any other. When the enemy falls holding a stack from the
    // hero's own hit, it passes one stack to its nearest living neighbours; spread stacks pass nothing on.
    public sealed class BurnStatus : MonoBehaviour
    {
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

        // The enemy's burn, added on its first stack. Null for a missing or fallen enemy.
        public static BurnStatus On(Health enemy)
        {
            if (enemy == null || !enemy.IsAlive)
                return null;
            BurnStatus status = enemy.GetComponent<BurnStatus>();
            if (status == null)
            {
                status = enemy.gameObject.AddComponent<BurnStatus>();
                status._health = enemy;
                enemy.Died += status.OnDied;
            }
            return status;
        }

        // Sets the enemy alight: stacks from the hero's hit (spread false) or a neighbour's fall (spread true). source is
        // whose damage the burn deals; neighbourhood finds who stands beside it when it falls.
        public void Ignite(int stacks, bool spread, IDamageable source, INeighbourhood neighbourhood)
        {
            if (_health == null || !_health.IsAlive)
                return;
            State.Apply(stacks, spread);
            _source = source;
            _neighbourhood = neighbourhood;
        }

        // Crucible Vent: every stack spent at once; how many there were.
        public int Consume() => State.Consume();

        private void Update()
        {
            if (!State.IsBurning || _health == null || !_health.IsAlive || Time.deltaTime <= 0f)
                return;
            float damage = State.Tick(Time.deltaTime);
            if (damage <= 0f)
                return;
            _health.ApplyDamage(new DamageContext(damage, _source));
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
                status.Ignite(BurnRule.SpreadStacks, true, _source, _neighbourhood);
                Spread?.Invoke(_health, neighbour);
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }
    }
}
