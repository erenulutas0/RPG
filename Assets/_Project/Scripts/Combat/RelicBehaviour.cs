using System;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Scene bridge for the equipped relic: reports every hit the hero survives, with the current enemy as the attacker.
    // Stays idle when no relic is equipped, because Initialize is never called.
    public sealed class RelicBehaviour : MonoBehaviour
    {
        [SerializeField] private Health _hero;
        [SerializeField] private AttackController _attack;
        [SerializeField] private EncounterController _encounters;
        private RelicRuntime _relic;
        private float _lastHealth;

        public void Initialize(RelicRuntime relic)
        {
            if (_hero == null || _attack == null || _encounters == null)
                throw new InvalidOperationException("RelicBehaviour needs the hero's health, attack and the encounter controller.");
            if (_relic != null)
                throw new InvalidOperationException("RelicBehaviour has already been initialized.");

            _relic = relic ?? throw new ArgumentNullException(nameof(relic));
            _lastHealth = _hero.Current;
            _hero.Changed += OnHeroChanged;
        }

        // Health.Changed also reports heals, so only a drop since the last report counts as a hit. A Second Wind heal
        // re-enters here with a rise and just updates the stored value.
        private void OnHeroChanged()
        {
            float previous = _lastHealth;
            _lastHealth = _hero.Current;
            if (_hero.Current >= previous || _attack.Weapon == null)
                return;

            _relic.OnHeroDamaged(_hero, _encounters.CurrentEnemy, _attack.Weapon.Damage);
            _lastHealth = _hero.Current;
        }

        private void OnDestroy()
        {
            if (_relic != null && _hero != null)
                _hero.Changed -= OnHeroChanged;
        }
    }
}
