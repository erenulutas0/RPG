using System;

namespace Cryptforge.Combat
{
    // A per-attacker snapshot. Definitions are never mutated by combat.
    public sealed class WeaponRuntime
    {
        private float _cooldown;
        public float Damage { get; }
        public float Interval { get; }
        public float Range { get; }

        public WeaponRuntime(float damage, float interval, float range)
        {
            RequirePositiveFinite(damage, nameof(damage));
            RequirePositiveFinite(interval, nameof(interval));
            RequirePositiveFinite(range, nameof(range));
            Damage = damage;
            Interval = interval;
            Range = range;
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
