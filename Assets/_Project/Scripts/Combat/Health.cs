using System;
using UnityEngine;

namespace Cryptforge.Combat
{
    public sealed class Health : MonoBehaviour, IDamageable, IHealable
    {
        private HealthState _state;
        private Action<DamageContext> _forwardDamaged;
        private Action _forwardChanged;
        private Action _forwardDied;

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

            Bind(new HealthState(maximum));
        }

        // A pooled body back on the floor (EncounterController's horde pool): a new state, full and alive, as Initialize
        // gives a new body, and no one listening - its views take it up again as it is enabled, the encounter as it spawns
        // it. Only while it is off the floor, after Initialize.
        internal void Revive(float maximum)
        {
            if (_state == null)
                throw new InvalidOperationException("Health needs Initialize before it is revived.");
            if (gameObject.activeInHierarchy)
                throw new InvalidOperationException("Only a body off the floor is revived.");

            Unbind();
            Damaged = null;
            Changed = null;
            Died = null;
            Bind(new HealthState(maximum));
        }

        // One set of forwarding delegates for every state this body ever holds, so a revival allocates only its state.
        private void Bind(HealthState state)
        {
            _state = state;
            _state.Damaged += _forwardDamaged ??= OnDamaged;
            _state.Changed += _forwardChanged ??= OnChanged;
            _state.Died += _forwardDied ??= OnDied;
        }

        private void Unbind()
        {
            _state.Damaged -= _forwardDamaged;
            _state.Changed -= _forwardChanged;
            _state.Died -= _forwardDied;
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

            Unbind();
        }
    }
}
