using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    public sealed class AttackController : MonoBehaviour
    {
        [SerializeField] private Health _owner;
        [SerializeField] private Targeting _targeting;
        private readonly List<IDamageable> _nearby = new List<IDamageable>();
        private WeaponRuntime _weapon;

        public int AttackCount { get; private set; }
        // Null until Initialize; behaviours such as enrage modify this runtime, never the definition.
        public WeaponRuntime Weapon => _weapon;
        public event Action Attacked;
        // The target of each successful attack, right after Attacked, so views can place a strike effect at it.
        public event Action<Health> Struck;

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
            if (!_weapon.IsReady)
                return;

            Health target = _targeting.Acquire(_weapon.Range);
            if (target == null)
                return;

            // Splash candidates are gathered only for a ready attack, into a reused list.
            _nearby.Clear();
            float splashRadius = _weapon.Pattern.SplashRadius;
            if (splashRadius > 0f)
                _targeting.CollectNear(target, splashRadius, _nearby);
            if (_weapon.TryAttack(target, _owner, _nearby))
            {
                AttackCount++;
                Attacked?.Invoke();
                Struck?.Invoke(target);
            }
        }
    }
}
