using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Economy
{
    // Awards the configured kill experience, plus the enemy's gold, once per victim. Death callbacks can repeat or
    // re-enter, so the rewarded set is the guarantee rather than caller discipline.
    public sealed class RewardService
    {
        private readonly RunState _run;
        private readonly int _experiencePerKill;
        private readonly HashSet<IDamageable> _rewarded = new HashSet<IDamageable>();

        public RewardService(RunState run, int experiencePerKill)
        {
            if (experiencePerKill < 0)
                throw new ArgumentOutOfRangeException(nameof(experiencePerKill));

            _run = run ?? throw new ArgumentNullException(nameof(run));
            _experiencePerKill = experiencePerKill;
        }

        public bool TryAwardKill(IDamageable victim) => TryAwardKill(victim, 0);

        public bool TryAwardKill(IDamageable victim, int gold)
        {
            if (gold < 0)
                throw new ArgumentOutOfRangeException(nameof(gold));
            if (victim == null || victim.IsAlive || !_rewarded.Add(victim))
                return false;

            _run.AddExperience(_experiencePerKill);
            _run.AddGold(gold);
            return true;
        }
    }
}
