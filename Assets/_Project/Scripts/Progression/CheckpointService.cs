using System;
using System.Collections.Generic;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // The Extract/Descend decision after a floor boss. It only records the choice; the composition root banks gold,
    // ends the run or descends in response to Chosen.
    public sealed class CheckpointService
    {
        private readonly RunState _run;

        public CheckpointOffer CurrentOffer { get; private set; }
        public event Action OfferChanged;
        public event Action<CheckpointKind> Chosen;

        public CheckpointService(RunState run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _run.Ended += OnRunEnded;
        }

        public bool Open(IReadOnlyList<CheckpointOption> options)
        {
            if (options == null || options.Count == 0)
                throw new ArgumentException("A checkpoint needs at least one option.", nameof(options));
            if (_run.HasEnded || CurrentOffer != null)
                return false;

            var choices = new CheckpointOption[options.Count];
            for (int i = 0; i < choices.Length; i++)
                choices[i] = options[i] ?? throw new ArgumentException("Checkpoint options must be non-null.", nameof(options));

            CurrentOffer = new CheckpointOffer(choices);
            OfferChanged?.Invoke();
            return true;
        }

        public bool TrySelect(CheckpointOffer offer, int slot)
        {
            if (_run.HasEnded || offer == null || offer != CurrentOffer || slot < 0 || slot >= offer.Choices.Count)
                return false;

            // Close before notifying so the decision cannot be taken twice, even from a re-entrant listener.
            CheckpointKind kind = offer.Choices[slot].Kind;
            CurrentOffer = null;
            OfferChanged?.Invoke();
            Chosen?.Invoke(kind);
            return true;
        }

        private void OnRunEnded()
        {
            if (CurrentOffer == null)
                return;

            CurrentOffer = null;
            OfferChanged?.Invoke();
        }
    }
}
