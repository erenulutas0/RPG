using System;
using System.Collections.Generic;

namespace Cryptforge.Combat
{
    // The hero's active ability this run: a burst that strikes every enemy within its radius of the hero, then cools down.
    // Plain state like WeaponRuntime; the scene's AbilityController and the Descent simulation both drive it.
    public sealed class AbilityRuntime
    {
        public float Damage { get; }
        // Floor units from the hero.
        public float Radius { get; }
        public float Cooldown { get; }
        // Seconds until the ability is ready again; zero means ready.
        public float Remaining { get; private set; }
        public int UsesMade { get; private set; }

        public bool IsReady => Remaining <= 0f;

        public AbilityRuntime(float damage, float radius, float cooldown)
        {
            if (!(damage > 0f) || float.IsInfinity(damage))
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (!(radius > 0f) || float.IsInfinity(radius))
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (!(cooldown > 0f) || float.IsInfinity(cooldown))
                throw new ArgumentOutOfRangeException(nameof(cooldown));

            Damage = damage;
            Radius = radius;
            Cooldown = cooldown;
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Remaining > 0f)
                Remaining = Math.Max(0f, Remaining - deltaTime);
        }

        // Strikes every living target and starts the cooldown; a ready burst with nothing in reach is still spent, as the
        // player chose the moment. False while cooling down.
        public bool TryUse(IDamageable source, IReadOnlyList<IDamageable> targets)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));
            if (!IsReady)
                return false;

            Remaining = Cooldown;
            UsesMade++;
            for (int i = 0; i < targets.Count; i++)
            {
                IDamageable target = targets[i];
                if (target != null && target.IsAlive)
                    target.ApplyDamage(new DamageContext(Damage, source));
            }
            return true;
        }
    }
}
