using System;
using UnityEngine;

namespace Cryptforge.Combat
{
    public sealed class Health : MonoBehaviour, IDamageable, IHealable
    {
        private HealthState _state;

        // The pure state behind this component, for the composition root that binds the run's stat sheet to it.
        public HealthState State => _state;

        public float Current => _state?.Current ?? 0f;
        public float Maximum => _state?.Maximum ?? 0f;
        public bool IsAlive => _state != null && _state.IsAlive;
        public event Action<DamageContext> Damaged;
        public event Action Changed;
        public event Action Died;

        public void Initialize(float maximum)
        {
            if (_state != null)
                throw new InvalidOperationException("Health has already been initialized.");

            _state = new HealthState(maximum);
            _state.Damaged += OnDamaged;
            _state.Changed += OnChanged;
            _state.Died += OnDied;
        }

        public void ApplyDamage(DamageContext context)
        {
            if (isActiveAndEnabled)
                _state?.ApplyDamage(context);
        }

        public void Heal(float amount)
        {
            if (isActiveAndEnabled)
                _state?.Heal(amount);
        }

        private void OnDamaged(DamageContext context) => Damaged?.Invoke(context);
        private void OnChanged() => Changed?.Invoke();
        private void OnDied() => Died?.Invoke();

        private void OnDestroy()
        {
            if (_state == null)
                return;

            _state.Damaged -= OnDamaged;
            _state.Changed -= OnChanged;
            _state.Died -= OnDied;
        }
    }
}
