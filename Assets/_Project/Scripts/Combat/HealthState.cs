using System;

namespace Cryptforge.Combat
{
    public sealed class HealthState : IDamageable
    {
        public float Maximum { get; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        public event Action Changed;
        public event Action Died;

        public HealthState(float maximum)
        {
            if (float.IsNaN(maximum) || float.IsInfinity(maximum) || maximum <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximum));

            Maximum = maximum;
            Current = maximum;
        }

        public void ApplyDamage(DamageContext context)
        {
            if (!IsAlive || context.Amount == 0f)
                return;

            Current = Math.Max(0f, Current - context.Amount);
            bool died = !IsAlive;
            Changed?.Invoke();
            // The transition is captured before callbacks; lethal re-entry cannot emit twice.
            if (died)
                Died?.Invoke();
        }
    }
}
