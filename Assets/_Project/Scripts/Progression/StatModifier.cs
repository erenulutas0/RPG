using System;

namespace Cryptforge.Progression
{
    // Flat amounts are in stat units; Percent amounts are fractions (0.25 = +25%), summed with every other percent; Multiply
    // amounts are fractions too (0.14 = x1.14), each a factor of its own on the final value.
    public readonly struct StatModifier
    {
        public ModifierOperation Operation { get; }
        public float Amount { get; }

        public StatModifier(ModifierOperation operation, float amount)
        {
            if (operation != ModifierOperation.Flat && operation != ModifierOperation.Percent && operation != ModifierOperation.Multiply)
                throw new ArgumentOutOfRangeException(nameof(operation));
            if (float.IsNaN(amount) || float.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));

            Operation = operation;
            Amount = amount;
        }
    }
}
