using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Economy
{
    // Awards a kill's experience and gold once per victim. Death callbacks can repeat or re-enter, so the rewarded set
    // is the guarantee rather than caller discipline. A victim stays in it until its owner says nothing can report its
    // death again (Forget): kept for the whole run, the set held every kill of a horde floor - thousands of enemies long
    // destroyed - and rehashed as it grew (the review of 2026-11-11).
    public sealed class RewardService
    {
        private readonly RunState _run;
        private readonly HashSet<IDamageable> _rewarded = new HashSet<IDamageable>();

        public RewardService(RunState run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        // How many paid victims the guarantee still holds.
        public int RememberedCount => _rewarded.Count;

        // The victim's death can no longer be reported - the encounter has let go of its body and unsubscribed from its
        // death (EncounterController.EnemyRemoved) - so the guarantee need not hold it. A victim forgotten while its death
        // could still be reported would be paid again.
        public void Forget(IDamageable victim)
        {
            if (victim != null)
                _rewarded.Remove(victim);
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

        // The Descent's version of a kill (docs/27, S6d): the same once-per-victim guarantee, but the reward falls where
        // the enemy fell and is paid when the hero picks it up.
        public bool TryDropKill(IDamageable victim, int experience, int gold, float floorX, float floorY, GroundDrops drops)
        {
            if (experience < 0)
                throw new ArgumentOutOfRangeException(nameof(experience));
            if (gold < 0)
                throw new ArgumentOutOfRangeException(nameof(gold));
            if (drops == null)
                throw new ArgumentNullException(nameof(drops));
            if (victim == null || victim.IsAlive || !_rewarded.Add(victim))
                return false;

            drops.Add(floorX, floorY, experience, gold);
            return true;
        }
    }
}
