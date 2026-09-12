using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // One forge visit. A new instance per visit lets stale taps be rejected by identity.
    public sealed class ForgeOffer
    {
        public IReadOnlyList<ForgeOption> Choices { get; }

        public ForgeOffer(IReadOnlyList<ForgeOption> choices)
        {
            Choices = choices;
        }
    }
}
