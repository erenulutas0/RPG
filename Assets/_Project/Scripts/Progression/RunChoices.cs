using System;

namespace Cryptforge.Progression
{
    // The single "is the player choosing?" source for pausing, encounter gating and the choice panel.
    // Priority is upgrade, then forge, then checkpoint: a level-up from the boss kill is resolved before the
    // Extract/Descend decision, and progression never opens the forge while another choice is pending.
    public sealed class RunChoices
    {
        private readonly UpgradeService _upgrades;
        private readonly ForgeService _forge;
        private readonly CheckpointService _checkpoint;
        private object _source;

        public ChoicePrompt Current { get; private set; }
        public bool IsOpen => Current != null;
        public event Action Changed;

        public RunChoices(UpgradeService upgrades, ForgeService forge, CheckpointService checkpoint = null)
        {
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _forge = forge ?? throw new ArgumentNullException(nameof(forge));
            _checkpoint = checkpoint;
            _upgrades.OfferChanged += Refresh;
            _forge.OfferChanged += Refresh;
            if (_checkpoint != null)
                _checkpoint.OfferChanged += Refresh;
            Refresh();
        }

        public bool TrySelect(ChoicePrompt prompt, int slot)
        {
            if (prompt == null || prompt != Current)
                return false;

            // Capture the source first: a successful selection refreshes Current through OfferChanged.
            return _source switch
            {
                UpgradeOffer upgrade => _upgrades.TrySelect(upgrade, slot),
                ForgeOffer forge => _forge.TrySelect(forge, slot),
                CheckpointOffer checkpoint => _checkpoint.TrySelect(checkpoint, slot),
                _ => false
            };
        }

        private void Refresh()
        {
            object source = (object)_upgrades.CurrentOffer ?? (object)_forge.CurrentOffer ?? _checkpoint?.CurrentOffer;
            if (ReferenceEquals(source, _source))
                return;

            _source = source;
            Current = source switch
            {
                UpgradeOffer upgrade => BuildUpgradePrompt(upgrade),
                ForgeOffer forge => BuildForgePrompt(forge),
                CheckpointOffer checkpoint => BuildCheckpointPrompt(checkpoint),
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
                UpgradeRarity rarity = offer.RarityAt(i);
                cards[i] = new ChoiceCard(option.DisplayName,
                    string.Format(option.DescriptionFormat, option.DescriptionValueFor(rarity)), rarity);
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

        private static ChoicePrompt BuildCheckpointPrompt(CheckpointOffer offer)
        {
            var cards = new ChoiceCard[offer.Choices.Count];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = new ChoiceCard(offer.Choices[i].Name, offer.Choices[i].Description);
            return new ChoicePrompt(ChoiceKind.Checkpoint, cards);
        }
    }
}
