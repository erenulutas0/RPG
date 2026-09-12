using System;

namespace Cryptforge.Progression
{
    // Heal amounts are fractions of maximum health (0.4 = 40%); BonusUpgrade amounts are whole upgrade counts.
    public sealed class ForgeOption
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string DescriptionFormat { get; }
        public ForgeEffect Effect { get; }
        public float Amount { get; }

        public float DescriptionValue => Effect == ForgeEffect.Heal ? Amount * 100f : Amount;

        public ForgeOption(string id, string displayName, string descriptionFormat, ForgeEffect effect, float amount)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Forge option id is required.", nameof(id));
            if (effect == ForgeEffect.Heal && !(amount > 0f && amount <= 1f))
                throw new ArgumentOutOfRangeException(nameof(amount), "Heal amounts are fractions in (0, 1].");
            if (effect == ForgeEffect.BonusUpgrade && !(amount >= 1f && amount == (float)Math.Floor(amount)))
                throw new ArgumentOutOfRangeException(nameof(amount), "Bonus upgrades are whole counts of at least 1.");
            if (effect != ForgeEffect.Heal && effect != ForgeEffect.BonusUpgrade)
                throw new ArgumentOutOfRangeException(nameof(effect));

            Id = id;
            DisplayName = displayName ?? string.Empty;
            DescriptionFormat = descriptionFormat ?? string.Empty;
            Effect = effect;
            Amount = amount;
        }
    }
}
