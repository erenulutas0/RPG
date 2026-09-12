using System;
using UnityEngine;

namespace Cryptforge.Combat
{
    public sealed class AttackController : MonoBehaviour
    {
        [SerializeField] private Health _owner;
        [SerializeField] private Targeting _targeting;
        private WeaponRuntime _weapon;

        public int AttackCount { get; private set; }
        // Null until Initialize; behaviours such as enrage modify this runtime, never the definition.
        public WeaponRuntime Weapon => _weapon;
        public event Action Attacked;

        public void Initialize(WeaponRuntime weapon)
        {
            if (_owner == null || _targeting == null)
                throw new InvalidOperationException("AttackController needs owner and targeting references.");
            if (_weapon != null)
                throw new InvalidOperationException("AttackController has already been initialized.");

            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
        }

        private void Update()
        {
            if (_weapon == null || !_owner.isActiveAndEnabled || !_owner.IsAlive || Time.deltaTime <= 0f)
                return;

            _weapon.Tick(Time.deltaTime);
            Health target = _targeting.Acquire(_weapon.Range);
            if (target != null && _weapon.TryAttack(target))
            {
                AttackCount++;
                Attacked?.Invoke();
            }
        }
    }
}
