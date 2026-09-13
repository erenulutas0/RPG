using System;

namespace Cryptforge.Combat
{
    public readonly struct DamageContext
    {
        public float Amount { get; }
        // Who dealt the damage, when known: relics strike back at it and the result screen names it.
        public IDamageable Source { get; }

        public DamageContext(float amount, IDamageable source = null)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
            Source = source;
        }
    }
}
