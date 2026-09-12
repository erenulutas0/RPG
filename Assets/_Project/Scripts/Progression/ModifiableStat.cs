using System;

namespace Cryptforge.Progression
{
    // Value = (base + sum of flat) * (1 + sum of percent), then clamped to the minimum.
    // Percents are summed rather than compounded so stacking stays linear (05_ECONOMY_BALANCING).
    public sealed class ModifiableStat
    {
        private float _flat;
        private float _percent;

        public float Base { get; }
        public float Minimum { get; }
        public float Value { get; private set; }

        public ModifiableStat(float baseValue, float minimum)
        {
            if (float.IsNaN(baseValue) || float.IsInfinity(baseValue))
                throw new ArgumentOutOfRangeException(nameof(baseValue));
            if (float.IsNaN(minimum) || float.IsInfinity(minimum) || minimum > baseValue)
                throw new ArgumentOutOfRangeException(nameof(minimum));

            Base = baseValue;
            Minimum = minimum;
            Value = baseValue;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier.Operation == ModifierOperation.Flat)
                _flat += modifier.Amount;
            else
                _percent += modifier.Amount;

            Value = Math.Max(Minimum, (Base + _flat) * (1f + _percent));
        }
    }
}
