using System;
using System.Runtime.CompilerServices;

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
            // A ward (docs/57's Cinder Ward) takes what armour let through, until it is spent: what it took never lands, and
            // the hit is reported to WardAbsorbed - the striker catches fire there. No ward, nothing changes.
            if (Ward > 0f && landed.Amount > 0f)
            {
                float absorbed = Math.Min(Ward, landed.Amount);
                Ward -= absorbed;
                landed = new DamageContext(landed.Amount - absorbed, landed.Source, landed.IsCritical, landed.IsTick);
                WardAbsorbed?.Invoke(context, absorbed);
            }
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

        // docs/57's Cinder Ward: a pool laid over the health that every landing after armour spends first (ApplyDamage), for
        // as long as whoever raised it keeps it (the skill's Duration; ClearWard ends it). 0 without one: every enemy's, and
        // the hero's between wards. Its size is read off the hero's own maximum health (SkillRule.WardOf).
        public float Ward { get; private set; }

        // A landing the ward took (some or all of it): the hit as it came, and how much the ward absorbed.
        public event Action<DamageContext, float> WardAbsorbed;

        // A ward of amount: the larger of it and what is left of the last.
        public void RaiseWard(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > Ward)
                Ward = amount;
        }

        public void ClearWard() => Ward = 0f;

        // The share of every hit armor takes away, in [0, 1).
        public void SetDamageReduction(float reduction)
        {
            if (float.IsNaN(reduction) || reduction < 0f || reduction >= 1f)
                throw new ArgumentOutOfRangeException(nameof(reduction));
            if (reduction == DamageReduction)
                return;

            DamageReduction = reduction;
        }

        // What every heal is multiplied by: the gear's healing-received line on the hero (docs/54 section 20), set once as
        // the run begins (GearStats.Apply); 1 for everything else. Every heal the game has - the chest's mend, the forge's
        // heal, Second Wind - comes through Heal, so this is the one place it applies. Raising the maximum (SetMaximum) is
        // not a heal and is not scaled.
        public float HealingReceived { get; private set; } = 1f;

        public void SetHealingReceived(float factor)
        {
            if (float.IsNaN(factor) || float.IsInfinity(factor) || factor <= 0f)
                throw new ArgumentOutOfRangeException(nameof(factor));
            HealingReceived = factor;
        }

        // Healing never revives: death is final for this state.
        public void Heal(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (!IsAlive || amount == 0f || Current >= Maximum)
                return;

            if (HealingReceived != 1f)
                amount = Received(amount, HealingReceived);
            Current = Math.Min(Maximum, Current + amount);
            Changed?.Invoke();
        }

        // One body, never inlined, so the scene's assembly and the simulation's scale a heal alike.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Received(float amount, float factor) => amount * factor;
    }
}
