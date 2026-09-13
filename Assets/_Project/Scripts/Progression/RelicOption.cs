using System;

namespace Cryptforge.Progression
{
    // CounterStrike: Amount is the fraction of the hero's weapon damage dealt back for every hit the hero survives.
    // SecondWind: Amount is the fraction of maximum health restored once per run when health falls to Threshold or less.
    // The description format receives Amount and Threshold as whole percentages.
    public sealed class RelicOption
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string DescriptionFormat { get; }
        public RelicEffect Effect { get; }
        public float Amount { get; }
        public float Threshold { get; }
        public int Price { get; }

        public string Description => string.Format(DescriptionFormat, Amount * 100f, Threshold * 100f);

        public RelicOption(string id, string displayName, string descriptionFormat, RelicEffect effect, float amount,
            float threshold, int price)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Relic id is required.", nameof(id));
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price));
            switch (effect)
            {
                case RelicEffect.CounterStrike:
                    if (!(amount > 0f) || float.IsInfinity(amount))
                        throw new ArgumentOutOfRangeException(nameof(amount), "Counter damage must be a positive fraction.");
                    break;
                case RelicEffect.SecondWind:
                    if (!(amount > 0f && amount <= 1f))
                        throw new ArgumentOutOfRangeException(nameof(amount), "Second Wind heals a fraction in (0, 1].");
                    if (!(threshold > 0f && threshold < 1f))
                        throw new ArgumentOutOfRangeException(nameof(threshold), "Second Wind triggers below a fraction in (0, 1).");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect));
            }

            Id = id;
            DisplayName = displayName ?? string.Empty;
            DescriptionFormat = descriptionFormat ?? string.Empty;
            Effect = effect;
            Amount = amount;
            Threshold = threshold;
            Price = price;
        }
    }
}
