using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Economy
{
    // Awards a kill's experience and gold once per victim. Death callbacks can repeat or re-enter, so the rewarded set
    // is the guarantee rather than caller discipline.
    public sealed class RewardService
    {
        private readonly RunState _run;
        private readonly HashSet<IDamageable> _rewarded = new HashSet<IDamageable>();

        public RewardService(RunState run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        public bool TryAwardKill(IDamageable victim, int experience, int gold)
        {
            if (experience < 0)
                throw new ArgumentOutOfRangeException(nameof(experience));
            if (gold < 0)
                throw new ArgumentOutOfRangeException(nameof(gold));
            if (victim == null || victim.IsAlive || !_rewarded.Add(victim))
                return false;

            _run.AddExperience(experience);
            _run.AddGold(gold);
            return true;
        }
    }
}
