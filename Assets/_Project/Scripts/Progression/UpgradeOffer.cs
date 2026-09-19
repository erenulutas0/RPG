using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // One presented choice. A new instance per offer lets stale taps be rejected by identity. Index counts the run's
    // offers from zero and names the random stream the offer was drawn from, so telemetry can say which draw a pick was.
    public sealed class UpgradeOffer
    {
        public IReadOnlyList<UpgradeOption> Choices { get; }
        public int Index { get; }
        // The tier each card is offered at, in the same order as Choices; a card is the same card at every tier, only
        // its magnitude differs, so this rides beside the list rather than inside it.
        private readonly UpgradeRarity[] _rarities;

        public UpgradeOffer(IReadOnlyList<UpgradeOption> choices, int index = 0, UpgradeRarity[] rarities = null)
        {
            Choices = choices;
            Index = index;
            _rarities = rarities;
        }

        public UpgradeRarity RarityAt(int slot)
        {
            if (slot < 0 || slot >= Choices.Count)
                throw new System.ArgumentOutOfRangeException(nameof(slot));
            return _rarities != null ? _rarities[slot] : UpgradeRarity.Common;
        }
    }
}
