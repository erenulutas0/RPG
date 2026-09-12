using System;

namespace Cryptforge.Combat
{
    // Floor scaling tier first, then the floor modifier; definitions stay untouched (05_ECONOMY_BALANCING).
    public static class FloorScaling
    {
        public static float Health(float baseHealth, float floorMultiplier, float modifierPercent)
        {
            Validate(floorMultiplier, modifierPercent);
            return baseHealth * floorMultiplier * (1f + modifierPercent);
        }

        // Expressed as a Percent weapon modifier so it composes with the runtime stat pipeline.
        public static float DamageBonus(float floorMultiplier, float modifierPercent)
        {
            Validate(floorMultiplier, modifierPercent);
            return floorMultiplier * (1f + modifierPercent) - 1f;
        }

        public static int Gold(int baseGold, float modifierPercent)
        {
            if (baseGold < 0)
                throw new ArgumentOutOfRangeException(nameof(baseGold));
            Validate(1f, modifierPercent);
            return (int)Math.Round(baseGold * (1.0 + modifierPercent), MidpointRounding.AwayFromZero);
        }

        private static void Validate(float floorMultiplier, float modifierPercent)
        {
            if (float.IsNaN(floorMultiplier) || float.IsInfinity(floorMultiplier) || floorMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(floorMultiplier));
            if (float.IsNaN(modifierPercent) || float.IsInfinity(modifierPercent) || modifierPercent <= -1f)
                throw new ArgumentOutOfRangeException(nameof(modifierPercent));
        }
    }
}
