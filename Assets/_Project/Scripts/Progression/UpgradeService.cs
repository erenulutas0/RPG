using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // Opens an offer for each pending upgrade (level-ups and bonus grants) and applies exactly one choice per offer
    // to runtime weapon stats.
    public sealed class UpgradeService
    {
        private readonly RunState _run;
        private readonly WeaponRuntime _weapon;
        private readonly HeroStats _heroStats;
        private readonly UpgradeOption[] _pool;
        private readonly int _choiceCount;
        private readonly Dictionary<UpgradeOption, int> _stacks = new Dictionary<UpgradeOption, int>();
        private int _offersCreated;

        public UpgradeOffer CurrentOffer { get; private set; }
        // The offer the last accepted choice came from, for whoever reports the pick after CurrentOffer has moved on.
        public UpgradeOffer LastSelected { get; private set; }
        public IReadOnlyList<UpgradeOption> Pool { get; }
        public event Action OfferChanged;

        // (chosen option, slot) once per accepted choice, so observers such as telemetry learn what was picked without
        // diffing stacks. Raised after the modifier is applied and CurrentOffer holds the following offer (or null),
        // immediately before OfferChanged, so a selection is always reported before the offer that follows it.
        public event Action<UpgradeOption, int> Selected;

        // heroStats may be omitted by a caller whose pool holds only weapon cards; one is made so a hero card can
        // never find nothing to write to.
        public UpgradeService(RunState run, WeaponRuntime weapon, IReadOnlyList<UpgradeOption> pool, int choiceCount,
            HeroStats heroStats = null)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
            _heroStats = heroStats ?? new HeroStats(100f, 2.5f, HeroStamina.DefaultBar);
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

            Pool = new ReadOnlyCollection<UpgradeOption>(_pool);
            _choiceCount = choiceCount;
            _run.PendingUpgradesChanged += OnPendingUpgradesChanged;
            _run.Ended += OnRunEnded;
            OnPendingUpgradesChanged();
        }

        public int StacksOf(UpgradeOption option) =>
            option != null && _stacks.TryGetValue(option, out int stacks) ? stacks : 0;

        public bool TrySelect(UpgradeOffer offer, int slot)
        {
            if (_run.HasEnded || offer == null || offer != CurrentOffer || slot < 0 || slot >= offer.Choices.Count)
                return false;

            // Close the offer before side effects so re-entrant or repeated taps are rejected.
            UpgradeOption choice = offer.Choices[slot];
            UpgradeRarity rarity = offer.RarityAt(slot);
            CurrentOffer = null;
            _stacks[choice]++;
            _run.RecordUpgradeApplied();
            StatModifier modifier = choice.ModifierFor(rarity);
            if (HeroStats.Owns(choice.Stat))
                _heroStats.AddModifier(choice.Stat, modifier);
            else
                _weapon.AddModifier(choice.Stat, modifier);
            LastSelected = offer;
            CurrentOffer = CreateOffer();
            Selected?.Invoke(choice, slot);
            OfferChanged?.Invoke();
            return true;
        }

        private void OnPendingUpgradesChanged()
        {
            if (CurrentOffer != null || _run.HasEnded)
                return;

            CurrentOffer = CreateOffer();
            if (CurrentOffer != null)
                OfferChanged?.Invoke();
        }

        // A choice left open when the run ends is withdrawn so the result screen is never behind it.
        private void OnRunEnded()
        {
            if (CurrentOffer == null)
                return;

            CurrentOffer = null;
            OfferChanged?.Invoke();
        }

        // The eligible cards are those below their stack limit whose prerequisite, if any, holds a stack. When they are
        // no more than the choice count they are offered whole, in pool order, and no randomness is consumed - which is
        // the game as it was with two cards, so every number pinned before the engine existed still holds. Otherwise the
        // offer draws that many distinct cards from the stream (seed, offers, index), so a chest opened between two
        // level-ups never moves the second one.
        private UpgradeOffer CreateOffer()
        {
            if (_run.HasEnded || _run.PendingUpgrades <= 0)
                return null;

            var eligible = new List<UpgradeOption>(_pool.Length);
            for (int i = 0; i < _pool.Length; i++)
            {
                UpgradeOption option = _pool[i];
                if (_stacks[option] < option.MaxStacks && (option.RequiresId == null || StacksOfId(option.RequiresId) > 0))
                    eligible.Add(option);
            }
            if (eligible.Count == 0)
                return null;

            int index = _offersCreated++;
            // One stream per offer serves both draws: which cards, then at which tier. Cards first, so a change to the
            // tier weights cannot change which cards a seed shows.
            RunRandom stream = RunRandom.Stream(_run.Seed, RunRandom.Offers, index);
            List<UpgradeOption> choices;
            if (eligible.Count <= _choiceCount)
            {
                choices = eligible;
            }
            else
            {
                choices = new List<UpgradeOption>(_choiceCount);
                for (int i = 0; i < _choiceCount; i++)
                {
                    int pick = stream.NextBelow(eligible.Count);
                    choices.Add(eligible[pick]);
                    eligible.RemoveAt(pick);
                }
            }

            var rarities = new UpgradeRarity[choices.Count];
            for (int i = 0; i < choices.Count; i++)
                rarities[i] = RarityTable.Draw(stream, _heroStats.Luck);
            return new UpgradeOffer(choices, index, rarities);
        }

        private int StacksOfId(string id)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].Id == id)
                    return _stacks[_pool[i]];
            }
            return 0;
        }
    }
}
