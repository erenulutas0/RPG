using System;

namespace Cryptforge.Combat
{
    public sealed class HealthState : IDamageable, IHealable
    {
        public float Maximum { get; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        // Damaged carries the hit (amount and source) and fires before Changed; Changed also reports heals.
        public event Action<DamageContext> Damaged;
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
            Damaged?.Invoke(context);
            Changed?.Invoke();
            // The transition is captured before callbacks; lethal re-entry cannot emit twice.
            if (died)
                Died?.Invoke();
        }

        // Healing never revives: death is final for this state.
        public void Heal(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (!IsAlive || amount == 0f || Current >= Maximum)
                return;

            Current = Math.Min(Maximum, Current + amount);
            Changed?.Invoke();
        }
    }
}
