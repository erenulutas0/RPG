using System;

namespace Cryptforge.Combat
{
    // Triggers once when health first falls to or below a fraction of maximum; a killing blow never triggers it.
    public sealed class EnrageRule
    {
        private readonly float _healthFraction;

        public bool IsEnraged { get; private set; }

        public EnrageRule(float healthFraction)
        {
            if (float.IsNaN(healthFraction) || healthFraction <= 0f || healthFraction >= 1f)
                throw new ArgumentOutOfRangeException(nameof(healthFraction));

            _healthFraction = healthFraction;
        }

        public bool Evaluate(float current, float maximum)
        {
            if (IsEnraged || current <= 0f || current > maximum * _healthFraction)
                return false;

            IsEnraged = true;
            return true;
        }
    }
}
