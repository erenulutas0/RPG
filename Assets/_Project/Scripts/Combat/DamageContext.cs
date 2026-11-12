using System;

namespace Cryptforge.Combat
{
    public readonly struct DamageContext
    {
        public float Amount { get; }
        // Who dealt the damage, when known: relics strike back at it and the result screen names it.
        public IDamageable Source { get; }
        // Set for a critical attack and its splash, so views can show the crit.
        public bool IsCritical { get; }
        // Set for a burn's tick (BurnStatus): damage over time the hero's hit left behind, not a landing of the hero's, so a
        // guard that judges landings (the Mirror Sentry's, MirrorGuardRule) lets it through untouched.
        public bool IsTick { get; }

        public DamageContext(float amount, IDamageable source = null, bool isCritical = false, bool isTick = false)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
            Source = source;
            IsCritical = isCritical;
            IsTick = isTick;
        }
    }
}
