using System;
using Cryptforge.Progression;

namespace Cryptforge.Combat
{
    // A per-attacker snapshot. Definitions are never mutated by combat; upgrades modify these runtime stats.
    public sealed class WeaponRuntime
    {
        // Safety floors, not balance values: damage cannot go negative and cadence cannot stall.
        private const float MinimumDamage = 0f;
        private const float MinimumAttackSpeed = 0.1f;

        private readonly ModifiableStat _damage;
        private readonly ModifiableStat _attackSpeed;
        private readonly float _baseInterval;
        private float _cooldown;

        public float Damage => _damage.Value;
        public float Interval => _baseInterval / _attackSpeed.Value;
        public float Range { get; }
        public event Action StatsChanged;

        public WeaponRuntime(float damage, float interval, float range)
        {
            RequirePositiveFinite(damage, nameof(damage));
            RequirePositiveFinite(interval, nameof(interval));
            RequirePositiveFinite(range, nameof(range));
            _damage = new ModifiableStat(damage, MinimumDamage);
            _attackSpeed = new ModifiableStat(1f, MinimumAttackSpeed);
            _baseInterval = interval;
            Range = range;
        }

        public void AddModifier(WeaponStat stat, StatModifier modifier)
        {
            switch (stat)
            {
                case WeaponStat.Damage:
                    _damage.AddModifier(modifier);
                    break;
                case WeaponStat.AttackSpeed:
                    // An in-progress cooldown keeps its remaining time; the new cadence starts with the next attack.
                    _attackSpeed.AddModifier(modifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat));
            }

            StatsChanged?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            _cooldown = Math.Max(0f, _cooldown - deltaTime);
        }

        public bool TryAttack(IDamageable target)
        {
            if (_cooldown > 0f || target == null || !target.IsAlive)
                return false;

            // Consume cadence before callbacks so an on-hit callback cannot attack recursively.
            _cooldown = Interval;
            target.ApplyDamage(new DamageContext(Damage));
            return true;
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
