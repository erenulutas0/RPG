using System;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Combat
{
    // A boss behaviour module: once health first drops to the threshold, attack speed rises for the rest of the fight.
    [RequireComponent(typeof(Health), typeof(AttackController))]
    public sealed class EnrageBehaviour : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 0.95f)] private float _healthFraction = 0.5f;
        [SerializeField, Min(0f)] private float _attackSpeedBonus = 1f;
        private Health _health;
        private AttackController _attack;
        private EnrageRule _rule;

        public bool IsEnraged => _rule != null && _rule.IsEnraged;
        public event Action Enraged;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _attack = GetComponent<AttackController>();
            _rule = new EnrageRule(_healthFraction);
        }

        private void OnEnable() => _health.Changed += OnHealthChanged;

        private void OnDisable() => _health.Changed -= OnHealthChanged;

        private void OnHealthChanged()
        {
            if (_attack.Weapon == null || !_rule.Evaluate(_health.Current, _health.Maximum))
                return;

            _attack.Weapon.AddModifier(WeaponStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, _attackSpeedBonus));
            Enraged?.Invoke();
        }
    }
}
