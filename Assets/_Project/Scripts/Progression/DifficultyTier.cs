using System;
using System.Globalization;

namespace Cryptforge.Progression
{
    // The difficulty a run is played on (docs/54 sections 8 and 21): Forged is the Descent as it shipped; Tempered, Inferno
    // and Abyssal each harden its enemies - node waves, the horde, elites and the bosses alike, through FloorScaling, the
    // path the floors' own tiers already take - and pay more, each matched to a gear power, so that a player wearing that
    // power meets the Descent the lead measured. The value is what the save and telemetry hold: append, never reorder.
    public enum DifficultyTier
    {
        Forged = 0,
        Tempered = 1,
        Inferno = 2,
        Abyssal = 3
    }

    // Each tier's numbers and words. Forged's are exactly 1, 100 % and 0 everywhere, and every caller treats Forged as no
    // change at all, so a Forged run is today's run to the bit (the default batch is byte-identical).
    //
    // The enemy multipliers were tuned on the exploration seeds 1-2000 (both heroes, typical hands) with each tier's matched
    // geared cohort against the ungeared Forged one, and confirmed once on seeds 5201-8400 (docs/54 section 21, docs/56).
    public static class DifficultyTiers
    {
        public const int Count = 4;
        public const DifficultyTier Highest = DifficultyTier.Abyssal;

        // Enemy health and damage (docs/54 section 8 started at 1.08 / 1.06, 1.15 / 1.13 and 1.20 / 1.18).
        public const float TemperedHealth = 1.08f;
        public const float TemperedDamage = 1.06f;
        public const float InfernoHealth = 1.15f;
        public const float InfernoDamage = 1.13f;
        public const float AbyssalHealth = 1.20f;
        public const float AbyssalDamage = 1.18f;

        public static bool IsDefined(int value) => value >= (int)DifficultyTier.Forged && value <= (int)Highest;

        // What every enemy's health is multiplied by on this tier, on top of its floor's own tier.
        public static float EnemyHealth(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Forged: return 1f;
                case DifficultyTier.Tempered: return TemperedHealth;
                case DifficultyTier.Inferno: return InfernoHealth;
                case DifficultyTier.Abyssal: return AbyssalHealth;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        // What every enemy's weapon damage is multiplied by on this tier, on top of its floor's own tier. The Warden's slam
        // and the Regent's lanes take a share of the hero's health, as on every floor, and are not scaled.
        public static float EnemyDamage(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Forged: return 1f;
                case DifficultyTier.Tempered: return TemperedDamage;
                case DifficultyTier.Inferno: return InfernoDamage;
                case DifficultyTier.Abyssal: return AbyssalDamage;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        // Gold, floor materials and boosters, in percent: x1, x1.25, x1.5, x2 (the lead, 2026-11-16).
        public static int RewardPercent(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Forged: return 100;
                case DifficultyTier.Tempered: return 125;
                case DifficultyTier.Inferno: return 150;
                case DifficultyTier.Abyssal: return 200;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        // The luck a dropped item's rarity is drawn at (RarityTable): 0, 10, 20, 30.
        public static float ItemLuck(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Forged: return 0f;
                case DifficultyTier.Tempered: return 10f;
                case DifficultyTier.Inferno: return 20f;
                case DifficultyTier.Abyssal: return 30f;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        // The gear power the tier is built for, in hundredths of a percent as GearLoadout.Power reads it: a +5 set, a +9
        // set, a +9 set with good efsun (docs/54 section 7.2; the simulation's careful+5, careful+9 and careful+9g).
        public static int MatchedGearPower(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Forged: return 0;
                case DifficultyTier.Tempered: return 1470;
                case DifficultyTier.Inferno: return 3090;
                case DifficultyTier.Abyssal: return 4190;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        public static string Name(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Forged: return "Forged";
                case DifficultyTier.Tempered: return "Tempered";
                case DifficultyTier.Inferno: return "Inferno";
                case DifficultyTier.Abyssal: return "Abyssal";
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        // The stable id the save and telemetry write ("tempered").
        public static string Id(DifficultyTier tier) => Name(tier).ToLowerInvariant();

        public static bool TryParse(string id, out DifficultyTier tier)
        {
            for (int i = 0; i < Count; i++)
            {
                if (string.Equals(id, Id((DifficultyTier)i), StringComparison.Ordinal))
                {
                    tier = (DifficultyTier)i;
                    return true;
                }
            }
            tier = DifficultyTier.Forged;
            return false;
        }

        // The tier below, whose completed Descent opens this one; Forged has none and is always open.
        public static bool TryBelow(DifficultyTier tier, out DifficultyTier below)
        {
            below = tier == DifficultyTier.Forged ? DifficultyTier.Forged : tier - 1;
            return tier != DifficultyTier.Forged;
        }

        public const string UnlockHintFormat = "Complete the Descent on {0}";

        // How a locked tier is opened ("Complete the Descent on Forged"); empty for Forged.
        public static string UnlockHint(DifficultyTier tier) =>
            TryBelow(tier, out DifficultyTier below)
                ? string.Format(CultureInfo.InvariantCulture, UnlockHintFormat, Name(below))
                : string.Empty;

        // "x1.25" in the invariant culture; "x1" for Forged.
        public static string RewardText(DifficultyTier tier) =>
            "x" + (RewardPercent(tier) / 100.0).ToString("0.##", CultureInfo.InvariantCulture);

        // A booster's (or any loot chance's) parts of 10,000 on this tier: the chance times the reward, never above certain.
        public static int Chance(int parts, DifficultyTier tier)
        {
            if (parts < 0)
                throw new ArgumentOutOfRangeException(nameof(parts));
            if (tier == DifficultyTier.Forged)
                return parts;
            return (int)Math.Min(LootRule.Parts, (long)parts * RewardPercent(tier) / 100);
        }

        // A counted reward (a floor material) on this tier: the amount times the reward, its fraction made whole by one coin
        // from the given stream (drawn only when there is a fraction, so Forged and whole products draw nothing).
        public static int Amount(int amount, DifficultyTier tier, RunRandom coin)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (tier == DifficultyTier.Forged || amount == 0)
                return amount;
            long hundredths = (long)amount * RewardPercent(tier);
            int whole = (int)(hundredths / 100);
            int fraction = (int)(hundredths % 100);
            if (fraction == 0 || coin == null)
                return whole;
            return coin.NextBelow(100) < fraction ? whole + 1 : whole;
        }
    }
}
