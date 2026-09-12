using System;

namespace Cryptforge.Progression
{
    // The single "is the player choosing?" source for pausing, encounter gating and the choice panel.
    // Upgrade offers take precedence; the forge never opens while one is pending because progression waits for it.
    public sealed class RunChoices
    {
        private readonly UpgradeService _upgrades;
        private readonly ForgeService _forge;
        private object _source;

        public ChoicePrompt Current { get; private set; }
        public bool IsOpen => Current != null;
        public event Action Changed;

        public RunChoices(UpgradeService upgrades, ForgeService forge)
        {
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _forge = forge ?? throw new ArgumentNullException(nameof(forge));
            _upgrades.OfferChanged += Refresh;
            _forge.OfferChanged += Refresh;
            Refresh();
        }

        public bool TrySelect(ChoicePrompt prompt, int slot)
        {
            if (prompt == null || prompt != Current)
                return false;

            // Capture the source first: a successful selection refreshes Current through OfferChanged.
            object source = _source;
            return source is UpgradeOffer upgrade
                ? _upgrades.TrySelect(upgrade, slot)
                : _forge.TrySelect((ForgeOffer)source, slot);
        }

        private void Refresh()
        {
            object source = (object)_upgrades.CurrentOffer ?? _forge.CurrentOffer;
            if (ReferenceEquals(source, _source))
                return;

            _source = source;
            Current = source switch
            {
                UpgradeOffer upgrade => BuildUpgradePrompt(upgrade),
                ForgeOffer forge => BuildForgePrompt(forge),
                _ => null
            };
            Changed?.Invoke();
        }

        private static ChoicePrompt BuildUpgradePrompt(UpgradeOffer offer)
        {
            var cards = new ChoiceCard[offer.Choices.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                UpgradeOption option = offer.Choices[i];
                cards[i] = new ChoiceCard(option.DisplayName, string.Format(option.DescriptionFormat, option.DescriptionValue));
            }
            return new ChoicePrompt(ChoiceKind.Upgrade, cards);
        }

        private static ChoicePrompt BuildForgePrompt(ForgeOffer offer)
        {
            var cards = new ChoiceCard[offer.Choices.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                ForgeOption option = offer.Choices[i];
                cards[i] = new ChoiceCard(option.DisplayName, string.Format(option.DescriptionFormat, option.DescriptionValue));
            }
            return new ChoicePrompt(ChoiceKind.Forge, cards);
        }
    }
}
