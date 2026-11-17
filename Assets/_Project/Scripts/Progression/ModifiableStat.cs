using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Progression
{
    // Value = (base + sum of flat) * (1 + sum of percent) * product of (1 + multiply), then clamped to the minimum.
    // Percents are summed rather than compounded so stacking stays linear (05_ECONOMY_BALANCING). A Multiply modifier is
    // the gear's own layer (docs/54 section 20): a factor apart from the cards' and tomes' percents. With none the factor
    // is 1 and is never multiplied in, so a stat without gear is the stat it always was, to the bit.
    public sealed class ModifiableStat
    {
        private float _flat;
        private float _percent;
        private float _factor = 1f;

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
            else if (modifier.Operation == ModifierOperation.Multiply)
                _factor = Compounded(_factor, modifier.Amount);
            else
                _percent += modifier.Amount;

            float value = (Base + _flat) * (1f + _percent);
            if (_factor != 1f)
                value = Factored(value, _factor);
            Value = Math.Max(Minimum, value);
        }

        // Every modifier gone: the stat as the constructor left it (WeaponRuntime.Restore, for a pooled enemy's weapon).
        internal void Restore()
        {
            _flat = 0f;
            _percent = 0f;
            _factor = 1f;
            Value = Base;
        }

        // The gear's layer, one body each and never inlined, so the scene's assembly and the simulation's multiply alike.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Compounded(float factor, float amount) => factor * (1f + amount);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Factored(float value, float factor) => value * factor;
    }
}
