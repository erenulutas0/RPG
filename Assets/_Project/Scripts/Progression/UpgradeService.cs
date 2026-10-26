using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // How many of the authored cards a run may be offered, by the level it has reached. S2b measured a nine-card pool
    // handed to today's Descent and it collapsed the game - a standing hero cleared one run in ten - because a level-up
    // is the only place a hero grows and every card past a point thins the chance of the one it needs. The answer is
    // not a narrower set of cards but a pool that follows the run: a Descent grants about eleven level-ups and sees the
    // five that ship, a swarm room grants seventy and sees all nine, with somewhere to put them (docs/27, S2c).
    //
    // The cards are authored in one order and the width takes them from the front, so a run below the first threshold
    // is offered exactly what it was offered before this rule existed, seed for seed.
    public static class UpgradePool
    {
        // The five cards a Descent has always been offered: Tempered Edge, Quickened Grip, Long Reach, Keen Edge and
        // Vital Surge.
        public const int BaseWidth = 5;
        // The sixth card arrives one level past what a whole Descent reaches, so nothing about the Descent moves.
        public const int FirstExtraAtLevel = 12;
        public const int LevelsPerExtraCard = 6;

        public static int WidthAt(int level)
        {
            if (level < FirstExtraAtLevel)
                return BaseWidth;
            return BaseWidth + 1 + (level - FirstExtraAtLevel) / LevelsPerExtraCard;
        }
    }

    // Opens an offer for each pending upgrade (level-ups and bonus grants) and applies exactly one choice per offer
    // to runtime weapon stats.
    //
    // Given a loadout and a catalogue of weapons and tomes, it runs in category mode (docs/42, PR 4): each card of an
    // offer is first a kind of pick - a new weapon, a level of a carried one, a new tome, a level of a held one, or a
    // general card - drawn from OfferWeights, and then a card of that kind. Without them it is the engine every run so
    // far has played, untouched: nothing below that reads the loadout runs unless one was handed in.
    public sealed class UpgradeService
    {
        // At most three distinct tomes (the report's 40 section 6.2), each to the level a weapon reaches.
        public const int MaxTomes = 3;
        public const int MaxTomeLevel = WeaponLoadout.MaxLevel;

        private readonly RunState _run;
        private readonly WeaponRuntime _weapon;
        private readonly HeroStats _heroStats;
        private readonly UpgradeOption[] _pool;
        private readonly int _choiceCount;
        private readonly bool _movementEnergyEnabled;
        private readonly Dictionary<UpgradeOption, int> _stacks = new Dictionary<UpgradeOption, int>();
        private int _offersCreated;

        // Category mode; all null in the legacy engine.
        private readonly WeaponLoadout _loadout;
        private readonly WeaponCard[] _weaponCards;
        private readonly UpgradeOption[] _tomes;
        // Each tome as the two kinds of card it can be, so an offer's card says which it is without the view or the
        // telemetry asking the service (UpgradeOption.Category).
        private readonly UpgradeOption[] _newTomes;
        private readonly UpgradeOption[] _tomeUpgrades;
        private readonly int[] _tomeLevels;
        // Every weapon card's Acquire and lines to their weapon, and every tome card (authored or copy) to its index.
        private readonly Dictionary<UpgradeOption, WeaponCard> _weaponOf = new Dictionary<UpgradeOption, WeaponCard>();
        private readonly Dictionary<UpgradeOption, int> _tomeOf = new Dictionary<UpgradeOption, int>();
        // The weapons whose evolution this run has taken: once each (docs/42, PR 5).
        private readonly HashSet<string> _evolved = new HashSet<string>();
        // A swarm room holds level-ups back and offers them together (docs/27 decision 7). While held, a pending level-up
        // opens no offer; a sitting offers every pending one in a row, and the hold resumes once they are answered.
        private bool _held;
        private bool _sitting;

        public UpgradeOffer CurrentOffer { get; private set; }
        // The offer the last accepted choice came from, for whoever reports the pick after CurrentOffer has moved on.
        public UpgradeOffer LastSelected { get; private set; }
        public IReadOnlyList<UpgradeOption> Pool { get; }
        public event Action OfferChanged;

        // (chosen option, slot) once per accepted choice, so observers such as telemetry learn what was picked without
        // diffing stacks. Raised after the modifier is applied and CurrentOffer holds the following offer (or null),
        // immediately before OfferChanged, so a selection is always reported before the offer that follows it.
        public event Action<UpgradeOption, int> Selected;

        // (book, tier) once per granted book, raised after the modifier is applied, for telemetry and for whatever
        // shows it later.
        public event Action<UpgradeOption, UpgradeRarity> BookGranted;

        // (card, runtime) once per weapon picked up, after the loadout has seeded it and replayed the tomes onto it and
        // before the next offer is made, so the scene can hand the runtime to its AttackController (docs/42, PR 7).
        public event Action<WeaponCard, WeaponRuntime> WeaponAcquired;

        // How many books this run has taken, so a report can tell a stack that was chosen from one that was found.
        public int BooksGranted { get; private set; }
        // Evolution stacks applied, each of which raised the world's difficulty.
        public int EvolutionsTaken { get; private set; }

        // heroStats may be omitted by a caller whose pool holds only weapon cards; one is made so a hero card can
        // never find nothing to write to.
        //
        // A loadout switches on category mode: weapon must then be its primary, weaponCards the weapons a run can pick
        // up or level (the one it started with included), and tomes the tomes it can hold. pool stays the authored
        // cards in their authored order; category mode offers only those that write to the hero (Eligible).
        public UpgradeService(RunState run, WeaponRuntime weapon, IReadOnlyList<UpgradeOption> pool, int choiceCount,
            HeroStats heroStats = null, bool movementEnergyEnabled = true, WeaponLoadout loadout = null,
            IReadOnlyList<WeaponCard> weaponCards = null, IReadOnlyList<UpgradeOption> tomes = null)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _movementEnergyEnabled = movementEnergyEnabled;
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

            if (loadout != null)
            {
                if (!ReferenceEquals(loadout.Primary, _weapon))
                    throw new ArgumentException("In category mode the weapon is the loadout's primary.", nameof(weapon));
                _loadout = loadout;
                _weaponCards = Copy(weaponCards, nameof(weaponCards));
                _tomes = Copy(tomes, nameof(tomes));
                _newTomes = new UpgradeOption[_tomes.Length];
                _tomeUpgrades = new UpgradeOption[_tomes.Length];
                _tomeLevels = new int[_tomes.Length];
                IndexCatalogue();
                WeaponCards = new ReadOnlyCollection<WeaponCard>(_weaponCards);
                Tomes = new ReadOnlyCollection<UpgradeOption>(_tomes);
            }
            else if (weaponCards != null || tomes != null)
            {
                throw new ArgumentException("A weapon and tome catalogue needs a loadout to fill.", nameof(loadout));
            }

            _run.PendingUpgradesChanged += OnPendingUpgradesChanged;
            _run.Ended += OnRunEnded;
            OnPendingUpgradesChanged();
        }

        public bool IsHeld => _held;

        // Whether offers are drawn by category, which is whether a loadout was handed in.
        public bool IsCategoryMode => _loadout != null;
        // The loadout category mode fills and levels; null in the legacy engine.
        public WeaponLoadout Loadout => _loadout;
        // The catalogue category mode draws from; null in the legacy engine.
        public IReadOnlyList<WeaponCard> WeaponCards { get; }
        public IReadOnlyList<UpgradeOption> Tomes { get; }

        // Distinct tomes held, at most MaxTomes.
        public int TomesHeld
        {
            get
            {
                int held = 0;
                if (_tomeLevels != null)
                    for (int i = 0; i < _tomeLevels.Length; i++)
                        if (_tomeLevels[i] > 0)
                            held++;
                return held;
            }
        }

        // A tome's level, 0 while it is not held or not in the catalogue.
        public int TomeLevel(string tomeId)
        {
            if (_tomes == null)
                return 0;
            for (int i = 0; i < _tomes.Length; i++)
                if (_tomes[i].Id == tomeId)
                    return _tomeLevels[i];
            return 0;
        }

        // Whether the weapon with this id has taken its evolution this run; false in the legacy engine.
        public bool HasEvolved(string weaponId) => weaponId != null && _evolved.Contains(weaponId);

        // From now on pending level-ups wait for a sitting. An offer already open stays open.
        public void Hold()
        {
            _held = true;
            _sitting = false;
        }

        // Offers every pending level-up, one after the other, then holds again. Does nothing unless held.
        public void OpenSitting()
        {
            if (!_held || _run.HasEnded)
                return;

            _sitting = true;
            if (CurrentOffer != null)
                return;
            CurrentOffer = CreateOffer();
            if (CurrentOffer != null)
                OfferChanged?.Invoke();
        }

        // Back to offering each level-up as it comes, starting with whatever the hold kept back.
        public void Release()
        {
            _held = false;
            _sitting = false;
            OnPendingUpgradesChanged();
        }

        // A general card's stacks; in category mode also a tome card's level, a weapon card's (its Acquire or one of its
        // lines) carried weapon's level, 0 while it is not carried, and a weapon's evolution's one stack once taken.
        public int StacksOf(UpgradeOption option)
        {
            if (option == null)
                return 0;
            if (_stacks.TryGetValue(option, out int stacks))
                return stacks;
            if (_loadout != null && _tomeOf.TryGetValue(option, out int tome))
                return _tomeLevels[tome];
            if (_loadout != null && _weaponOf.TryGetValue(option, out WeaponCard card))
            {
                if (option == card.Evolution)
                    return HasEvolved(card.Id) ? 1 : 0;
                int slot = _loadout.IndexOf(card.Id);
                return slot >= 0 ? _loadout.At(slot).Level : 0;
            }
            return 0;
        }

        public bool TrySelect(UpgradeOffer offer, int slot)
        {
            if (_run.HasEnded || offer == null || offer != CurrentOffer || slot < 0 || slot >= offer.Choices.Count)
                return false;

            // Close the offer before side effects so re-entrant or repeated taps are rejected.
            UpgradeOption choice = offer.Choices[slot];
            UpgradeRarity rarity = offer.RarityAt(slot);
            CurrentOffer = null;
            if (choice.Category != OfferCategory.General)
            {
                _run.RecordUpgradeApplied();
                ApplyCategoryPick(choice, rarity);
            }
            else
            {
                _stacks[choice]++;
                _run.RecordUpgradeApplied();
                StatModifier modifier = choice.ModifierFor(rarity);
                if (HeroStats.Owns(choice.Stat))
                    _heroStats.AddModifier(choice.Stat, modifier);
                else
                    _weapon.AddModifier(choice.Stat, modifier);
                ApplyEvolutionCost(choice);
            }
            LastSelected = offer;
            CurrentOffer = CreateOffer();
            Selected?.Invoke(choice, slot);
            OfferChanged?.Invoke();
            return true;
        }

        // A book: one card and one tier, granted rather than offered, drawn from the books stream at this chest's run
        // ordinal so the same chest of the same run always holds the same book. It draws from the same eligibility a
        // level-up does, and grants nothing when every card is full or blocked. A card the open offer happens to show
        // is not excluded: whether an offer is open is true in the scene and never true in the simulation, and a draw
        // that depended on it would diverge between them (docs/27 S3). In category mode a book is a tome level or a
        // general card instead (TryGrantCategoryBook).
        public bool TryGrantBook(int chestIndex, out UpgradeOption book, out UpgradeRarity rarity)
        {
            book = null;
            rarity = UpgradeRarity.Common;
            if (_run.HasEnded)
                return false;

            if (_loadout != null)
                return TryGrantCategoryBook(chestIndex, out book, out rarity);

            List<UpgradeOption> eligible = Eligible();
            if (eligible.Count == 0)
                return false;

            RunRandom stream = RunRandom.Stream(_run.Seed, RunRandom.Books, chestIndex);
            book = eligible[stream.NextBelow(eligible.Count)];
            rarity = RarityTable.Draw(stream, _heroStats.Luck);
            // Not RecordUpgradeApplied: that answers a pending level-up, and a book answers nothing. The run's
            // UpgradesApplied stays what it has always meant - cards chosen at a level-up - and BooksGranted counts the
            // stacks the hero found instead of chose.
            _stacks[book]++;
            BooksGranted++;
            StatModifier modifier = book.ModifierFor(rarity);
            if (HeroStats.Owns(book.Stat))
                _heroStats.AddModifier(book.Stat, modifier);
            else
                _weapon.AddModifier(book.Stat, modifier);
            ApplyEvolutionCost(book);
            BookGranted?.Invoke(book, rarity);
            return true;
        }

        // A book in category mode (docs/42, PR 6; owner decision): a level of a tome the hero already holds, or a general
        // card, and nothing else. Never a weapon - a chest is not where a build's weapons come from - and never a new tome
        // either: the three tome slots are as much the build's choice as the three weapon slots, and a chest that filled
        // one would make that choice for the player, for the rest of the run. So a hero holding no tome finds a general
        // card, and a hero whose tomes are all full finds a general card, or nothing once those are full too.
        //
        // Which of the two is drawn by OfferWeights over just those two kinds, so a book leans the way the level-ups at the
        // same run level lean (level early, five to two for the tome late); then a card of that kind and its tier. Every
        // draw is on the books stream at this chest's ordinal, three words in that order, so the same chest of the same
        // run always holds the same book, and an offer's draws never see a chest.
        private bool TryGrantCategoryBook(int chestIndex, out UpgradeOption book, out UpgradeRarity rarity)
        {
            book = null;
            rarity = UpgradeRarity.Common;
            List<UpgradeOption> general = Eligible();
            var tomes = new List<UpgradeOption>(_tomes.Length);
            for (int i = 0; i < _tomes.Length; i++)
                if (_tomeLevels[i] > 0 && _tomeLevels[i] < TomeCap(i))
                    tomes.Add(_tomeUpgrades[i]);

            var open = new bool[OfferWeights.CategoryCount];
            open[(int)OfferCategory.General] = general.Count > 0;
            open[(int)OfferCategory.TomeUpgrade] = tomes.Count > 0;
            if (!OfferWeights.AnyOpen(_run.Level, open))
                return false;

            RunRandom stream = RunRandom.Stream(_run.Seed, RunRandom.Books, chestIndex);
            OfferCategory category = OfferWeights.Draw(stream, _run.Level, open);
            List<UpgradeOption> cards = category == OfferCategory.TomeUpgrade ? tomes : general;
            book = cards[stream.NextBelow(cards.Count)];
            rarity = RarityTable.Draw(stream, _heroStats.Luck);
            // As a legacy book: no RecordUpgradeApplied, since a book answers no level-up.
            BooksGranted++;
            StatModifier modifier = book.ModifierFor(rarity);
            if (category == OfferCategory.TomeUpgrade)
            {
                _tomeLevels[_tomeOf[book]]++;
                ApplyTome(book.Stat, modifier);
            }
            else
            {
                // A general card in category mode is always the hero's (Eligible).
                _stacks[book]++;
                _heroStats.AddModifier(book.Stat, modifier);
                ApplyEvolutionCost(book);
            }
            BookGranted?.Invoke(book, rarity);
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
            if (_held && !_sitting)
                return null;
            if (_run.HasEnded || _run.PendingUpgrades <= 0)
                return EndSitting();

            if (_loadout != null)
                return CreateCategoryOffer();

            List<UpgradeOption> eligible = Eligible();
            if (eligible.Count == 0)
                return EndSitting();

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

        // Nothing left to offer: a sitting, if one is open, is over.
        private UpgradeOffer EndSitting()
        {
            _sitting = false;
            return null;
        }

        // The cards below their stack limit whose prerequisite, if any, holds a stack, in pool order, and only as far
        // into the pool as the run's level has opened it. One list serves both reward moments, so a book can never
        // reach a card a level-up would refuse to offer.
        //
        // An evolution is the exception to the width, and deliberately so: its gate is its base card being full, which is
        // a harder thing to reach than a level, and an evolution sitting behind the width would never be offered at all -
        // the run that prompted this slice reached level fifty-one, which opens twelve of fourteen cards (docs/27, S5b).
        //
        // In category mode the general cards are only those that write to the hero, and the width does not apply. A card
        // on a weapon stat - Tempered Edge, Quickened Grip, Long Reach, Keen Edge, Forge Pulse, Splintering Crit and the
        // two weapon evolutions - would write to slot 0 alone and double-dip with the tomes, so it leaves (docs/42,
        // risks); what a weapon gains comes from its own lines and the tomes instead. And the width answered a pool where
        // every card past a point thinned the chance of the one a run needed: in category mode the general cards are a
        // tenth of the offer whatever their number, so the few hero cards left are all open from the start. A category
        // book draws its general cards from this same list (TryGrantCategoryBook).
        private List<UpgradeOption> Eligible()
        {
            int width = _loadout != null ? _pool.Length : Math.Min(_pool.Length, UpgradePool.WidthAt(_run.Level));
            var eligible = new List<UpgradeOption>(_pool.Length);
            for (int i = 0; i < _pool.Length; i++)
            {
                UpgradeOption option = _pool[i];
                if (!_movementEnergyEnabled && option.Stat == UpgradeStat.StaminaBar)
                    continue;
                if (_loadout != null && WeaponRuntime.Owns(option.Stat))
                    continue;
                if (_stacks[option] >= option.MaxStacks)
                    continue;
                if (option.IsEvolution)
                {
                    if (EvolutionRule.Unlocked(_run.Level, IsFull(option.EvolvesFromId)))
                        eligible.Add(option);
                    continue;
                }
                if (i < width && (option.RequiresId == null || StacksOfId(option.RequiresId) > 0))
                    eligible.Add(option);
            }
            return eligible;
        }

        // Whether the card with this id is at its stack limit. An id the pool does not hold is not full, so a
        // mis-authored evolution is never offered rather than breaking a run.
        private bool IsFull(string id)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].Id == id)
                    return _stacks[_pool[i]] >= _pool[i].MaxStacks;
            }
            return false;
        }

        // Every evolution makes the world harder. Applied here rather than carried on the card, because a card holds one
        // stat and an evolution needs two things to happen at once (EvolutionRule).
        private void ApplyEvolutionCost(UpgradeOption option)
        {
            if (!option.IsEvolution)
                return;

            EvolutionsTaken++;
            _heroStats.AddModifier(UpgradeStat.Difficulty, EvolutionRule.DifficultyCost());
        }

        // A category-mode offer: per card, a kind of pick drawn from OfferWeights over the kinds that still hold a card not
        // already in this offer, then a card of that kind - for a carried weapon, the weapon and then one line of its
        // table, drawn now so the card can show what it does. Both draws come from the categories stream at this offer's
        // index; the tiers come afterwards from the offers stream at the same index, so which card and which tier never
        // share a word. Fewer cards than the choice count when fewer remain; no offer at all when none do.
        private UpgradeOffer CreateCategoryOffer()
        {
            List<UpgradeOption> general = Eligible();
            var choices = new List<UpgradeOption>(_choiceCount);
            var open = new bool[OfferWeights.CategoryCount];
            var candidates = new List<UpgradeOption>();
            if (!FindOpen(general, choices, open))
                return EndSitting();

            int index = _offersCreated++;
            RunRandom draws = RunRandom.Stream(_run.Seed, RunRandom.Categories, index);
            for (int i = 0; i < _choiceCount; i++)
            {
                if (i > 0 && !FindOpen(general, choices, open))
                    break;

                OfferCategory category = OfferWeights.Draw(draws, _run.Level, open);
                Candidates(category, general, choices, candidates);
                UpgradeOption pick = candidates[draws.NextBelow(candidates.Count)];
                if (category == OfferCategory.WeaponUpgrade)
                    pick = DrawWeaponUpgrade(_weaponOf[pick], draws);
                choices.Add(pick);
            }

            // One word per card, whatever its kind, so a tier never depends on which kinds came before it. A weapon picked
            // up is the same weapon at every tier, so its card is shown green whatever the word said.
            RunRandom tiers = RunRandom.Stream(_run.Seed, RunRandom.Offers, index);
            var rarities = new UpgradeRarity[choices.Count];
            for (int i = 0; i < choices.Count; i++)
            {
                UpgradeRarity rarity = RarityTable.Draw(tiers, _heroStats.Luck);
                rarities[i] = choices[i].Category == OfferCategory.NewWeapon ? UpgradeRarity.Common : rarity;
            }
            return new UpgradeOffer(choices, index, rarities);
        }

        // A carried weapon's card for this offer: one entry of its table, which is its lines while it is below its last
        // level and, once its evolution is unlocked and not yet taken, its evolution after them. The evolution joins the
        // table rather than replacing it, so a player who would rather not pay its cost can still level the weapon; at the
        // last level it is the table's only entry and is shown whenever the weapon's card comes up, and NextBelow(1)
        // takes no word. With no evolution waiting the draw is the one PR 4 made, word for word.
        private UpgradeOption DrawWeaponUpgrade(WeaponCard card, RunRandom draws)
        {
            int lines = LinesOpen(card) ? card.Lines.Count : 0;
            int entries = lines + (EvolutionPending(card) ? 1 : 0);
            int entry = draws.NextBelow(entries);
            return entry < lines ? card.Lines[entry] : card.Evolution;
        }

        private bool LinesOpen(WeaponCard card)
        {
            int slot = _loadout.IndexOf(card.Id);
            return slot >= 0 && _loadout.At(slot).Level < WeaponLoadout.MaxLevel && card.Lines.Count > 0;
        }

        // A carried weapon's evolution is waiting once the weapon and its tome have reached EvolutionRule's levels, until
        // the run takes it; never for a weapon with none, and never in a run whose catalogue lacks the tome.
        private bool EvolutionPending(WeaponCard card)
        {
            if (card.Evolution == null || _evolved.Contains(card.Id))
                return false;
            int slot = _loadout.IndexOf(card.Id);
            return slot >= 0 && EvolutionRule.WeaponUnlocked(_loadout.At(slot).Level, TomeLevel(card.EvolvesWithTomeId));
        }

        // Which kinds of pick still hold a card this offer does not show yet; false when none does.
        private bool FindOpen(List<UpgradeOption> general, List<UpgradeOption> choices, bool[] open)
        {
            for (int c = 0; c < OfferWeights.CategoryCount; c++)
                open[c] = HasCandidate((OfferCategory)c, general, choices);
            return OfferWeights.AnyOpen(_run.Level, open);
        }

        private bool HasCandidate(OfferCategory category, List<UpgradeOption> general, List<UpgradeOption> choices)
        {
            switch (category)
            {
                case OfferCategory.General:
                    for (int i = 0; i < general.Count; i++)
                        if (!choices.Contains(general[i]))
                            return true;
                    return false;
                case OfferCategory.NewWeapon:
                case OfferCategory.WeaponUpgrade:
                    for (int i = 0; i < _weaponCards.Length; i++)
                        if (WeaponOpen(category, _weaponCards[i], choices))
                            return true;
                    return false;
                default:
                    for (int i = 0; i < _tomes.Length; i++)
                        if (TomeOpen(category, i, choices))
                            return true;
                    return false;
            }
        }

        // The cards of one kind still open to this offer, in catalogue order. For a carried weapon the card listed is the
        // weapon's Acquire, standing for the weapon until a line is drawn for it.
        private void Candidates(OfferCategory category, List<UpgradeOption> general, List<UpgradeOption> choices,
            List<UpgradeOption> into)
        {
            into.Clear();
            switch (category)
            {
                case OfferCategory.General:
                    for (int i = 0; i < general.Count; i++)
                        if (!choices.Contains(general[i]))
                            into.Add(general[i]);
                    break;
                case OfferCategory.NewWeapon:
                case OfferCategory.WeaponUpgrade:
                    for (int i = 0; i < _weaponCards.Length; i++)
                        if (WeaponOpen(category, _weaponCards[i], choices))
                            into.Add(_weaponCards[i].Acquire);
                    break;
                case OfferCategory.NewTome:
                    for (int i = 0; i < _tomes.Length; i++)
                        if (TomeOpen(category, i, choices))
                            into.Add(_newTomes[i]);
                    break;
                default:
                    for (int i = 0; i < _tomes.Length; i++)
                        if (TomeOpen(category, i, choices))
                            into.Add(_tomeUpgrades[i]);
                    break;
            }
        }

        // A new weapon while a slot is free and the hero does not carry it; a carried one's upgrade while it is below
        // its last level and has lines to draw, or while its evolution waits. Either way at most one card per weapon in an
        // offer.
        private bool WeaponOpen(OfferCategory category, WeaponCard card, List<UpgradeOption> choices)
        {
            for (int i = 0; i < choices.Count; i++)
                if (_weaponOf.TryGetValue(choices[i], out WeaponCard shown) && shown == card)
                    return false;

            if (category == OfferCategory.NewWeapon)
                return !_loadout.Owns(card.Id) && !_loadout.IsFull;
            return LinesOpen(card) || EvolutionPending(card);
        }

        // A new tome while fewer than MaxTomes are held; a held one's next level while it is below its last.
        private bool TomeOpen(OfferCategory category, int tome, List<UpgradeOption> choices)
        {
            for (int i = 0; i < choices.Count; i++)
                if (_tomeOf.TryGetValue(choices[i], out int shown) && shown == tome)
                    return false;

            int level = _tomeLevels[tome];
            if (category == OfferCategory.NewTome)
                return level == 0 && TomesHeld < MaxTomes;
            return level > 0 && level < TomeCap(tome);
        }

        private int TomeCap(int tome) => Math.Min(_tomes[tome].MaxStacks, MaxTomeLevel);

        // What a weapon or tome card does once taken. A new weapon takes the next slot, seeded and with every tome
        // replayed onto it by the loadout, and is announced; a weapon's line goes to that weapon's runtime alone and
        // raises its level; its evolution goes to that weapon's runtime alone too, raises no level, is taken once and
        // makes the world harder (EvolutionRule); a tome goes to every weapon, now and later, through the loadout's global
        // record.
        private void ApplyCategoryPick(UpgradeOption choice, UpgradeRarity rarity)
        {
            switch (choice.Category)
            {
                case OfferCategory.NewWeapon:
                {
                    WeaponCard card = _weaponOf[choice];
                    WeaponRuntime runtime = card.CreateRuntime();
                    if (!_loadout.TryAdd(card.Id, runtime))
                        throw new InvalidOperationException($"Weapon {card.Id} was offered but has no slot to take.");
                    WeaponAcquired?.Invoke(card, runtime);
                    break;
                }
                case OfferCategory.WeaponUpgrade:
                {
                    WeaponCard card = _weaponOf[choice];
                    int slot = _loadout.IndexOf(card.Id);
                    if (choice == card.Evolution)
                    {
                        if (!EvolutionPending(card))
                            throw new InvalidOperationException($"Weapon {card.Id} was offered an evolution it cannot take.");
                        _evolved.Add(card.Id);
                        _loadout.At(slot).Runtime.AddModifier(choice.Stat, choice.ModifierFor(rarity));
                        ApplyEvolutionCost(choice);
                        break;
                    }
                    if (slot < 0 || !_loadout.TryLevelUp(card.Id))
                        throw new InvalidOperationException($"Weapon {card.Id} was offered a level it cannot take.");
                    _loadout.At(slot).Runtime.AddModifier(choice.Stat, choice.ModifierFor(rarity));
                    break;
                }
                default:
                {
                    int tome = _tomeOf[choice];
                    _tomeLevels[tome]++;
                    ApplyTome(choice.Stat, choice.ModifierFor(rarity));
                    break;
                }
            }
        }

        // Builds the lookups category mode draws through, and refuses a catalogue that could put one id on two cards of
        // one offer or a tome with nothing to write to.
        private void IndexCatalogue()
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < _pool.Length; i++)
                ids.Add(_pool[i].Id);

            for (int i = 0; i < _weaponCards.Length; i++)
            {
                WeaponCard card = _weaponCards[i];
                RequireNewId(ids, card.Id);
                _weaponOf.Add(card.Acquire, card);
                for (int j = 0; j < card.Lines.Count; j++)
                {
                    RequireNewId(ids, card.Lines[j].Id);
                    _weaponOf.Add(card.Lines[j], card);
                }
                if (card.Evolution != null)
                {
                    RequireNewId(ids, card.Evolution.Id);
                    _weaponOf.Add(card.Evolution, card);
                }
            }

            for (int i = 0; i < _tomes.Length; i++)
            {
                UpgradeOption tome = _tomes[i];
                if (!WeaponRuntime.Owns(tome.Stat) && !HeroStats.Owns(tome.Stat))
                    throw new ArgumentException($"Tome {tome.Id} is on {tome.Stat}, which neither a weapon nor the hero owns.", "tomes");
                if (tome.IsEvolution || tome.RequiresId != null)
                    throw new ArgumentException($"Tome {tome.Id} has a gate; a tome's only gates are its level and the count.", "tomes");
                RequireNewId(ids, tome.Id);
                _newTomes[i] = tome.WithCategory(OfferCategory.NewTome);
                _tomeUpgrades[i] = tome.WithCategory(OfferCategory.TomeUpgrade);
                _tomeOf.Add(tome, i);
                _tomeOf.Add(_newTomes[i], i);
                _tomeOf.Add(_tomeUpgrades[i], i);
            }
        }

        // A tome on a weapon stat levels every weapon at once (and any picked up later); one on a hero stat - Vitality's
        // health - levels the hero. Without a hero tome a category-mode run had no health beyond the general cards' 10 %
        // and the autopilot met the Warden at its starting 100 on every seed (docs/42, 2026-10-26).
        private void ApplyTome(UpgradeStat stat, StatModifier modifier)
        {
            if (HeroStats.Owns(stat))
                _heroStats.AddModifier(stat, modifier);
            else
                _loadout.AddGlobal(stat, modifier);
        }

        private static void RequireNewId(HashSet<string> ids, string id)
        {
            if (!ids.Add(id))
                throw new ArgumentException($"Id {id} names two cards; an offer could show it twice.");
        }

        private static T[] Copy<T>(IReadOnlyList<T> list, string name) where T : class
        {
            if (list == null)
                return new T[0];
            var copy = new T[list.Count];
            for (int i = 0; i < list.Count; i++)
                copy[i] = list[i] ?? throw new ArgumentException("Catalogue entries must be non-null.", name);
            return copy;
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
