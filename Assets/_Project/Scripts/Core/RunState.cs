using System;

namespace Cryptforge.Core
{
    // Temporary per-run progression. Nothing here is persisted; a new Play session starts at zero.
    public sealed class RunState
    {
        private readonly int _experiencePerLevel;
        private readonly float _atRiskGoldLoss;

        public int Experience { get; private set; }
        public int Level { get; private set; }
        public int BonusUpgrades { get; private set; }
        public int UpgradesApplied { get; private set; }
        public int PendingUpgrades => Level + BonusUpgrades - UpgradesApplied;
        public int ExperienceForCurrentLevel => Level * _experiencePerLevel;
        public int ExperienceForNextLevel => (Level + 1) * _experiencePerLevel;
        public RunOutcome Outcome { get; private set; }
        public bool HasEnded => Outcome != RunOutcome.None;

        // Gold earned since the last floor checkpoint is unsecured; a defeat loses a fraction of it.
        public int Gold { get; private set; }
        public int SecuredGold { get; private set; }
        public int UnsecuredGold => Gold - SecuredGold;
        public int GoldBanked { get; private set; }
        public int GoldLost { get; private set; }

        public event Action ExperienceChanged;
        public event Action LevelChanged;
        public event Action PendingUpgradesChanged;
        public event Action GoldChanged;
        public event Action Ended;

        // Linear thresholds; the floor structure currently paces choices at one per kill.
        public RunState(int experiencePerLevel, float atRiskGoldLoss = 0.5f)
        {
            if (experiencePerLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(experiencePerLevel));
            if (float.IsNaN(atRiskGoldLoss) || atRiskGoldLoss < 0f || atRiskGoldLoss > 1f)
                throw new ArgumentOutOfRangeException(nameof(atRiskGoldLoss));

            _experiencePerLevel = experiencePerLevel;
            _atRiskGoldLoss = atRiskGoldLoss;
        }

        public void AddGold(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0 || HasEnded)
                return;

            Gold += amount;
            GoldChanged?.Invoke();
        }

        // Called when the player descends past a checkpoint: everything earned so far becomes safe.
        public void SecureGold()
        {
            if (HasEnded || UnsecuredGold == 0)
                return;

            SecuredGold = Gold;
            GoldChanged?.Invoke();
        }

        public void AddExperience(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            // An ended run is final: late rewards cannot reopen progression behind the result screen.
            if (amount == 0 || HasEnded)
                return;

            Experience += amount;
            int level = Experience / _experiencePerLevel;
            bool levelled = level != Level;
            // Level is updated before callbacks so listeners always read a consistent threshold.
            Level = level;
            ExperienceChanged?.Invoke();
            if (!levelled)
                return;

            LevelChanged?.Invoke();
            PendingUpgradesChanged?.Invoke();
        }

        // An upgrade choice earned outside levelling, such as the forge's Temper.
        public void GrantBonusUpgrade()
        {
            if (HasEnded)
                return;

            BonusUpgrades++;
            PendingUpgradesChanged?.Invoke();
        }

        public void RecordUpgradeApplied()
        {
            if (PendingUpgrades <= 0)
                throw new InvalidOperationException("No pending upgrade to apply.");

            UpgradesApplied++;
        }

        public void End(RunOutcome outcome)
        {
            if (outcome == RunOutcome.None)
                throw new ArgumentOutOfRangeException(nameof(outcome));
            if (HasEnded)
                return;

            // Rounded down so a defeat never loses more than the configured fraction.
            GoldLost = outcome == RunOutcome.Defeat ? (int)Math.Floor(UnsecuredGold * (double)_atRiskGoldLoss) : 0;
            GoldBanked = Gold - GoldLost;
            Outcome = outcome;
            Ended?.Invoke();
        }
    }
}
