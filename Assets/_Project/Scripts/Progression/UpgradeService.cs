using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // Opens an offer for each pending level and applies exactly one choice per offer to runtime weapon stats.
    public sealed class UpgradeService
    {
        private readonly RunState _run;
        private readonly WeaponRuntime _weapon;
        private readonly UpgradeOption[] _pool;
        private readonly int _choiceCount;
        private readonly Dictionary<UpgradeOption, int> _stacks = new Dictionary<UpgradeOption, int>();

        public UpgradeOffer CurrentOffer { get; private set; }
        public event Action OfferChanged;

        public UpgradeService(RunState run, WeaponRuntime weapon, IReadOnlyList<UpgradeOption> pool, int choiceCount)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            if (choiceCount < 1)
                throw new ArgumentOutOfRangeException(nameof(choiceCount));

            _pool = new UpgradeOption[pool.Count];
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] == null || _stacks.ContainsKey(pool[i]))
                    throw new ArgumentException("Upgrade pool entries must be unique and non-null.", nameof(pool));
                _pool[i] = pool[i];
                _stacks.Add(pool[i], 0);
            }

            _choiceCount = choiceCount;
            _run.LevelChanged += OnLevelChanged;
            OnLevelChanged();
        }

        public int StacksOf(UpgradeOption option) =>
            option != null && _stacks.TryGetValue(option, out int stacks) ? stacks : 0;

        public bool TrySelect(UpgradeOffer offer, int slot)
        {
            if (offer == null || offer != CurrentOffer || slot < 0 || slot >= offer.Choices.Count)
                return false;

            // Close the offer before side effects so re-entrant or repeated taps are rejected.
            UpgradeOption choice = offer.Choices[slot];
            CurrentOffer = null;
            _stacks[choice]++;
            _run.RecordUpgradeApplied();
            _weapon.AddModifier(choice.Stat, choice.Modifier);
            CurrentOffer = CreateOffer();
            OfferChanged?.Invoke();
            return true;
        }

        private void OnLevelChanged()
        {
            if (CurrentOffer != null)
                return;

            CurrentOffer = CreateOffer();
            if (CurrentOffer != null)
                OfferChanged?.Invoke();
        }

        private UpgradeOffer CreateOffer()
        {
            if (_run.PendingUpgrades <= 0)
                return null;

            // Deterministic pool order until encounter pacing justifies weighted random offers.
            var choices = new List<UpgradeOption>(_choiceCount);
            for (int i = 0; i < _pool.Length && choices.Count < _choiceCount; i++)
            {
                if (_stacks[_pool[i]] < _pool[i].MaxStacks)
                    choices.Add(_pool[i]);
            }

            return choices.Count > 0 ? new UpgradeOffer(choices) : null;
        }
    }
}
