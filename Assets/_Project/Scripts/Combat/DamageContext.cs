using System;

namespace Cryptforge.Combat
{
    public readonly struct DamageContext
    {
        public float Amount { get; }

        public DamageContext(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
        }
    }
}
