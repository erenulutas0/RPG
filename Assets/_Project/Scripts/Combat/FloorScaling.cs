using System;
using System.Runtime.CompilerServices;
using Cryptforge.Progression;

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

        // The difficulty tier's own layer (docs/54 sections 8 and 21) on a floor's tier: the floor's multiplier (the climb's,
        // a boss room's growth included) times the tier's enemy health or damage, before the run's difficulty and the
        // horde's shares. Forged returns the floor's multiplier itself.
        public static float TierHealth(float floorMultiplier, DifficultyTier tier) =>
            Tiered(floorMultiplier, DifficultyTiers.EnemyHealth(tier));

        public static float TierDamage(float floorMultiplier, DifficultyTier tier) =>
            Tiered(floorMultiplier, DifficultyTiers.EnemyDamage(tier));

        // The one non-inlined body the scene (EncounterController, through the two above) and the Descent simulation both
        // call at the same point, so the two round the product alike (the stamina lesson, docs/21). A multiplier of exactly
        // 1 returns the floor's own. The simulation passes the tier's multiplier itself, so a research candidate can be
        // played through the same body.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Tiered(float floorMultiplier, float tierMultiplier) =>
            tierMultiplier == 1f ? floorMultiplier : floorMultiplier * tierMultiplier;

        // An enemy's or a chest's gold on a difficulty tier: the floor's gold times the tier's reward, rounded once. Forged
        // is Gold above, to the coin.
        public static int Gold(int baseGold, float modifierPercent, DifficultyTier tier)
        {
            if (tier == DifficultyTier.Forged)
                return Gold(baseGold, modifierPercent);
            if (baseGold < 0)
                throw new ArgumentOutOfRangeException(nameof(baseGold));
            Validate(1f, modifierPercent);
            return (int)Math.Round(baseGold * (1.0 + modifierPercent) * DifficultyTiers.RewardPercent(tier) / 100.0,
                MidpointRounding.AwayFromZero);
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
