using System;

namespace Cryptforge.Core
{
    // Temporary per-run progression. Nothing here is persisted; a new Play session starts at zero.
    public sealed class RunState
    {
        private readonly int _experiencePerLevel;

        public int Experience { get; private set; }
        public int Level { get; private set; }
        public int UpgradesApplied { get; private set; }
        public int PendingUpgrades => Level - UpgradesApplied;
        public int ExperienceForCurrentLevel => Level * _experiencePerLevel;
        public int ExperienceForNextLevel => (Level + 1) * _experiencePerLevel;

        public event Action ExperienceChanged;
        public event Action LevelChanged;

        // Linear thresholds until sequential encounters exist to tune a curve against.
        public RunState(int experiencePerLevel)
        {
            if (experiencePerLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(experiencePerLevel));

            _experiencePerLevel = experiencePerLevel;
        }

        public void AddExperience(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0)
                return;

            Experience += amount;
            int level = Experience / _experiencePerLevel;
            bool levelled = level != Level;
            // Level is updated before callbacks so listeners always read a consistent threshold.
            Level = level;
            ExperienceChanged?.Invoke();
            if (levelled)
                LevelChanged?.Invoke();
        }

        public void RecordUpgradeApplied()
        {
            if (PendingUpgrades <= 0)
                throw new InvalidOperationException("No pending upgrade to apply.");

            UpgradesApplied++;
        }
    }
}
