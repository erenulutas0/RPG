using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // One checkpoint decision. A new instance per checkpoint lets stale taps be rejected by identity.
    public sealed class CheckpointOffer
    {
        public IReadOnlyList<CheckpointOption> Choices { get; }

        public CheckpointOffer(IReadOnlyList<CheckpointOption> choices)
        {
            Choices = choices;
        }
    }
}
