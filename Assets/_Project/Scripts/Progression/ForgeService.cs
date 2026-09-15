using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // Resolves a forge visit: exactly one option per offer, applied to the hero or the run.
    public sealed class ForgeService
    {
        private readonly RunState _run;
        private readonly IHealable _hero;

        public ForgeOffer CurrentOffer { get; private set; }
        public event Action OfferChanged;

        // (chosen option, slot) once per accepted choice, so observers such as telemetry learn what was picked; the
        // closing OfferChanged alone cannot tell a choice from a visit withdrawn at run end. Raised after the effect is
        // applied, immediately before OfferChanged.
        public event Action<ForgeOption, int> Selected;

        public ForgeService(RunState run, IHealable hero)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _run.Ended += OnRunEnded;
        }

        public bool Open(IReadOnlyList<ForgeOption> options)
        {
            if (options == null || options.Count == 0)
                throw new ArgumentException("A forge visit needs at least one option.", nameof(options));
            if (_run.HasEnded || CurrentOffer != null)
                return false;

            var choices = new ForgeOption[options.Count];
            for (int i = 0; i < choices.Length; i++)
                choices[i] = options[i] ?? throw new ArgumentException("Forge options must be non-null.", nameof(options));

            CurrentOffer = new ForgeOffer(choices);
            OfferChanged?.Invoke();
            return true;
        }

        public bool TrySelect(ForgeOffer offer, int slot)
        {
            if (_run.HasEnded || offer == null || offer != CurrentOffer || slot < 0 || slot >= offer.Choices.Count)
                return false;

            // Close the visit before side effects so repeated or re-entrant taps are rejected.
            ForgeOption choice = offer.Choices[slot];
            CurrentOffer = null;
            switch (choice.Effect)
            {
                case ForgeEffect.Heal:
                    _hero.Heal(_hero.Maximum * choice.Amount);
                    break;
                case ForgeEffect.BonusUpgrade:
                    for (int i = 0; i < (int)choice.Amount; i++)
                        _run.GrantBonusUpgrade();
                    break;
            }

            Selected?.Invoke(choice, slot);
            OfferChanged?.Invoke();
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
