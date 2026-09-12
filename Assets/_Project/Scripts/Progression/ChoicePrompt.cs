using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // What the choice panel shows. A new instance per underlying offer, so stale prompts are rejected by identity.
    public sealed class ChoicePrompt
    {
        public ChoiceKind Kind { get; }
        public IReadOnlyList<ChoiceCard> Cards { get; }

        public ChoicePrompt(ChoiceKind kind, IReadOnlyList<ChoiceCard> cards)
        {
            Kind = kind;
            Cards = cards;
        }
    }
}
