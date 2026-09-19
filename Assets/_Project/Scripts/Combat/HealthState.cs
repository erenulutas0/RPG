using System;

namespace Cryptforge.Combat
{
    public sealed class HealthState : IDamageable, IHealable
    {
        public float Maximum { get; private set; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;
        // The share of every hit that never lands, from armor; 0 for everything that has none, which is every enemy.
        public float DamageReduction { get; private set; }

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

            // Armor is taken off before anything observes the hit, so a view, a relic and the movement report all read
            // the damage that actually landed.
            DamageContext landed = DamageReduction > 0f
                ? new DamageContext(context.Amount * (1f - DamageReduction), context.Source, context.IsCritical)
                : context;
            if (landed.Amount == 0f)
                return;

            Current = Math.Max(0f, Current - landed.Amount);
            bool died = !IsAlive;
            Damaged?.Invoke(landed);
            Changed?.Invoke();
            // The transition is captured before callbacks; lethal re-entry cannot emit twice.
            if (died)
                Died?.Invoke();
        }

        // Raises the ceiling and gives the new room filled, so a maximum-health card is worth taking mid-fight. A
        // lower maximum only trims the ceiling, never the health already held, and never below one.
        public void SetMaximum(float maximum)
        {
            if (float.IsNaN(maximum) || float.IsInfinity(maximum) || maximum <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximum));
            if (maximum == Maximum)
                return;

            float gained = Math.Max(0f, maximum - Maximum);
            Maximum = maximum;
            Current = Math.Min(maximum, Current + gained);
            Changed?.Invoke();
        }

        // The share of every hit armor takes away, in [0, 1).
        public void SetDamageReduction(float reduction)
        {
            if (float.IsNaN(reduction) || reduction < 0f || reduction >= 1f)
                throw new ArgumentOutOfRangeException(nameof(reduction));
            if (reduction == DamageReduction)
                return;

            DamageReduction = reduction;
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
