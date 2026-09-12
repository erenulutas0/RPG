using System;

namespace Cryptforge.Core
{
    // Temporary per-run progression. Nothing here is persisted; a new Play session starts at zero.
    public sealed class RunState
    {
        public int Experience { get; private set; }
        public event Action ExperienceChanged;

        public void AddExperience(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0)
                return;

            Experience += amount;
            ExperienceChanged?.Invoke();
        }
    }
}
