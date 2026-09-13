using System;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Scene bridge for the equipped relic: reports every hit on the hero together with the enemy that dealt it.
    // Stays idle when no relic is equipped, because Initialize is never called.
    public sealed class RelicBehaviour : MonoBehaviour
    {
        [SerializeField] private Health _hero;
        [SerializeField] private AttackController _attack;
        private RelicRuntime _relic;

        public void Initialize(RelicRuntime relic)
        {
            if (_hero == null || _attack == null)
                throw new InvalidOperationException("RelicBehaviour needs the hero's health and attack.");
            if (_relic != null)
                throw new InvalidOperationException("RelicBehaviour has already been initialized.");

            _relic = relic ?? throw new ArgumentNullException(nameof(relic));
            _hero.Damaged += OnHeroDamaged;
        }

        private void OnHeroDamaged(DamageContext context)
        {
            if (_attack.Weapon != null)
                _relic.OnHeroDamaged(_hero, context.Source, _attack.Weapon.Damage);
        }

        private void OnDestroy()
        {
            if (_relic != null && _hero != null)
                _hero.Damaged -= OnHeroDamaged;
        }
    }
}
