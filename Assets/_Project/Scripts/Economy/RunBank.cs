using System;
using Cryptforge.Core;

namespace Cryptforge.Economy
{
    // Moves run gold into the profile: gold secured at a checkpoint right away, and the rest of the banked gold when
    // the run ends. Depositing at the checkpoint keeps a descent's secured gold even if the app closes mid-floor.
    public sealed class RunBank
    {
        private readonly RunState _run;
        private readonly PlayerProfile _profile;

        public int Deposited { get; private set; }

        public RunBank(RunState run, PlayerProfile profile)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _run.GoldChanged += OnGoldChanged;
            _run.Ended += OnRunEnded;
        }

        private void OnGoldChanged()
        {
            if (!_run.HasEnded)
                DepositUpTo(_run.SecuredGold);
        }

        // Banked gold is never below secured gold, because a defeat only loses part of the unsecured gold.
        private void OnRunEnded() => DepositUpTo(_run.GoldBanked);

        private void DepositUpTo(int total)
        {
            int amount = total - Deposited;
            if (amount <= 0)
                return;

            Deposited = total;
            _profile.Deposit(amount);
        }
    }
}
