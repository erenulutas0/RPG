using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // One presented choice. A new instance per offer lets stale taps be rejected by identity.
    public sealed class UpgradeOffer
    {
        public IReadOnlyList<UpgradeOption> Choices { get; }

        public UpgradeOffer(IReadOnlyList<UpgradeOption> choices)
        {
            Choices = choices;
        }
    }
}
