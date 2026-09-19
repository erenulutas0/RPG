using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // One presented choice. A new instance per offer lets stale taps be rejected by identity. Index counts the run's
    // offers from zero and names the random stream the offer was drawn from, so telemetry can say which draw a pick was.
    public sealed class UpgradeOffer
    {
        public IReadOnlyList<UpgradeOption> Choices { get; }
        public int Index { get; }

        public UpgradeOffer(IReadOnlyList<UpgradeOption> choices, int index = 0)
        {
            Choices = choices;
            Index = index;
        }
    }
}
