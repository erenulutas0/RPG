using System;
using System.Collections.Generic;
using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // Whether a hero may be played (docs/45): open from the start, unlocked by play, or any hero on a development build.
    public static class HeroUnlock
    {
        public static bool IsPlayable(bool openFromStart, bool unlockedByPlay, bool developmentBuild) =>
            openFromStart || unlockedByPlay || developmentBuild;

        // Whether clearing The Foundry unlocks a hero whose unlock names a weapon carried through it (Ilya: "Clear The
        // Foundry with the Staff", docs/45): the run carried that weapon - or the run's cards could never have handed it
        // over, and then the clear alone unlocks (docs/63 section 6). Under the weapon roster a hero's own weapon is never
        // offered to another hero, so the Vanguard can never carry Ilya's Staff; the closest fair rule is the clear itself.
        // With today's catalogue the Staff is a card the Vanguard can take, and the rule is the one it always was.
        public static bool UnlocksOnFoundryClear(bool carriedTheWeapon, bool cardsCouldOfferIt) =>
            carriedTheWeapon || !cardsCouldOfferIt;
    }

    // Permanent progress between runs: banked gold, forged relics, what is equipped, the heroes unlocked and chosen, the
    // weapons the retired forge rack sold (kept, never read for a run: docs/63), the depth record, the gear (docs/54: items, basma, efsun, floor materials and boosters), the difficulty
    // tiers' records with the tier chosen for the next run (docs/54 section 21) and each hero's forged skills (docs/57).
    // Every successful change raises Changed exactly once, which is the save trigger.
    public sealed class PlayerProfile
    {
        private readonly List<string> _ownedRelicIds = new List<string>();
        private readonly List<string> _ownedWeaponIds = new List<string>();
        private readonly List<string> _unlockedHeroIds = new List<string>();

        public int Gold { get; private set; }
        public string EquippedRelicId { get; private set; }
        // The weapon the retired forge rack last equipped, null for the starting weapon (which was never stored as owned).
        // Every hero carries its own weapon since 2026-11-22 and the rack is gone since 2026-11-23 (docs/63): no run reads
        // this or OwnedWeaponIds, and nothing changes them any more; they are kept so no save loses what it recorded (and
        // the arena's build still reads them, ArenaProfileBuild).
        public string EquippedWeaponId { get; private set; }
        // The deepest floor any run has cleared, on any tier (the map choice's Forged record reads it through Tiers).
        public int DeepestFloorCleared { get; private set; }
        // The bag, each weapon's upgrades, the materials and boosters (docs/54). The Anvil and the run's bank change it;
        // each of their operations raises Changed once, through NotifyChanged.
        public ProfileGear Gear { get; }
        // Each difficulty tier's record and the tier chosen for the next run (docs/54 section 21). Forged's record is the
        // depth record above at the least: every run before the tiers was a Forged run.
        public ProfileTiers Tiers { get; }
        // Each hero's forged skills 2 and 3 (docs/57 phase 4); TryForgeSkill buys one.
        public ProfileSkills Skills { get; }
        public IReadOnlyList<string> OwnedRelicIds => _ownedRelicIds;
        public IReadOnlyList<string> OwnedWeaponIds => _ownedWeaponIds;
        // Heroes unlocked by play; the heroes open from the start are never stored.
        public IReadOnlyList<string> UnlockedHeroIds => _unlockedHeroIds;
        // The hero chosen for the next run; null for the default. Whether the choice may be played is the run's to judge
        // against what is unlocked (CombatSetup), so a development build can try a locked hero without unlocking it.
        public event Action Changed;

        public PlayerProfile()
        {
            Gear = new ProfileGear();
            Tiers = new ProfileTiers();
            Skills = new ProfileSkills();
        }

        // Restores saved progress. Damaged values are repaired instead of rejected so a hand-edited or partially
        // valid save still loads: negative numbers become zero, duplicates are dropped and unowned items are unequipped.
        public PlayerProfile(int gold, IEnumerable<string> ownedRelicIds, string equippedRelicId, int deepestFloorCleared,
            IEnumerable<string> ownedWeaponIds = null, string equippedWeaponId = null,
            IEnumerable<string> unlockedHeroIds = null, string selectedHeroId = null, ProfileGear gear = null,
            ProfileTiers tiers = null, ProfileSkills skills = null)
        {
            Gear = gear ?? new ProfileGear();
            Gold = Math.Max(0, gold);
            DeepestFloorCleared = Math.Max(0, deepestFloorCleared);
            Tiers = tiers ?? new ProfileTiers();
            Tiers.Reconcile(DeepestFloorCleared);
            // ProfileSkills.Restore has already repaired the skills against the depth record (ProfileStore).
            Skills = skills ?? new ProfileSkills();
            AddUnique(_ownedRelicIds, ownedRelicIds);
            AddUnique(_ownedWeaponIds, ownedWeaponIds);
            EquippedRelicId = Owns(equippedRelicId) ? equippedRelicId : null;
            EquippedWeaponId = OwnsWeapon(equippedWeaponId) ? equippedWeaponId : null;
            AddUnique(_unlockedHeroIds, unlockedHeroIds);
            SelectedHeroId = string.IsNullOrEmpty(selectedHeroId) ? null : selectedHeroId;
        }

        public string SelectedHeroId { get; private set; }

        public bool HasUnlockedHero(string heroId) => !string.IsNullOrEmpty(heroId) && _unlockedHeroIds.Contains(heroId);

        // Records a hero unlocked by play; false when it already was.
        public bool UnlockHero(string heroId)
        {
            if (string.IsNullOrEmpty(heroId))
                throw new ArgumentException("A hero id is required.", nameof(heroId));
            if (_unlockedHeroIds.Contains(heroId))
                return false;

            _unlockedHeroIds.Add(heroId);
            Changed?.Invoke();
            return true;
        }

        // Chooses the hero for the next run; null or empty chooses the default. False when nothing changed.
        public bool SelectHero(string heroId)
        {
            string chosen = string.IsNullOrEmpty(heroId) ? null : heroId;
            if (chosen == SelectedHeroId)
                return false;

            SelectedHeroId = chosen;
            Changed?.Invoke();
            return true;
        }

        public bool Owns(string relicId) => !string.IsNullOrEmpty(relicId) && _ownedRelicIds.Contains(relicId);

        public bool OwnsWeapon(string weaponId) => !string.IsNullOrEmpty(weaponId) && _ownedWeaponIds.Contains(weaponId);

        public void Deposit(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0)
                return;

            Gold += amount;
            Changed?.Invoke();
        }

        // A dropped item into the bag with the next uid (docs/54 section 3); null, and nothing changed, when the bag is full.
        public ItemInstance AddItem(ItemDrop drop)
        {
            ItemInstance item = AddItemQuietly(drop);
            if (item != null)
                Changed?.Invoke();
            return item;
        }

        public void AddMaterial(MaterialKind kind, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0)
                return;
            Gear.AddMaterial(kind, amount);
            Changed?.Invoke();
        }

        public void AddBooster(BoosterKind kind, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0)
                return;
            Gear.AddBooster(kind, amount);
            Changed?.Invoke();
        }

        // A run's banked loot as one change (docs/54 phase 3): materials and boosters by kind (null for none), and items into
        // the bag. An item that finds the bag full is melted at once for its gold and Ember Shards (docs/54 section 3.3);
        // the result says which. The first-boss armour marks its gift as given.
        public LootDeposit DepositLoot(int[] materials, int[] boosters, IReadOnlyList<ItemDrop> items)
        {
            var deposit = new LootDeposit();
            bool changed = false;
            for (int i = 0; materials != null && i < materials.Length && i < GearCurrency.MaterialCount; i++)
            {
                if (materials[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(materials));
                if (materials[i] == 0)
                    continue;
                Gear.AddMaterial((MaterialKind)i, materials[i]);
                changed = true;
            }
            for (int i = 0; boosters != null && i < boosters.Length && i < GearCurrency.BoosterCount; i++)
            {
                if (boosters[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(boosters));
                if (boosters[i] == 0)
                    continue;
                Gear.AddBooster((BoosterKind)i, boosters[i]);
                changed = true;
            }
            for (int i = 0; items != null && i < items.Count; i++)
            {
                ItemDrop drop = items[i];
                if (drop == null)
                    continue;
                if (drop.Source == LootSource.FirstBoss)
                    Gear.MarkFirstArmourGiven();
                ItemInstance stored = AddItemQuietly(drop);
                if (stored != null)
                {
                    deposit.Stored.Add(stored);
                }
                else
                {
                    int gold = AnvilTable.MeltGold(drop.Rarity);
                    int ember = AnvilTable.MeltEmber(drop.Rarity);
                    AddGoldQuietly(gold);
                    Gear.AddMaterial(MaterialKind.EmberShard, ember);
                    deposit.Melted.Add(drop);
                    deposit.MeltGold += gold;
                    deposit.MeltEmber += ember;
                }
                changed = true;
            }
            if (changed)
                Changed?.Invoke();
            return deposit;
        }

        internal ItemInstance AddItemQuietly(ItemDrop drop)
        {
            if (drop == null)
                throw new ArgumentNullException(nameof(drop));
            return Gear.IsBagFull ? null : Gear.AddItem(drop.ItemId, drop.Slot, drop.Rarity, drop.Lines);
        }

        // Spends gold for a gear operation without raising Changed: the operation raises it once with NotifyChanged when
        // everything it changes has changed (Anvil). False, and nothing spent, when the gold is short.
        internal bool TrySpendGold(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (Gold < amount)
                return false;
            Gold -= amount;
            return true;
        }

        // Adds gold for a gear operation (a melt) without raising Changed; see TrySpendGold.
        internal void AddGoldQuietly(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Gold = (int)Math.Min(int.MaxValue, (long)Gold + amount);
        }

        // One gear operation has finished changing the profile: the save trigger, once.
        internal void NotifyChanged() => Changed?.Invoke();

        // Spending, owning and equipping happen together so a purchase is saved as one change.
        public bool TryForgeRelic(string relicId, int price)
        {
            if (!TrySpendOn(_ownedRelicIds, relicId, price))
                return false;

            EquippedRelicId = relicId;
            Changed?.Invoke();
            return true;
        }

        public bool Equip(string relicId)
        {
            if (!Owns(relicId) || EquippedRelicId == relicId)
                return false;

            EquippedRelicId = relicId;
            Changed?.Invoke();
            return true;
        }

        // A floor cleared on Forged, as every run cleared one before the tiers.
        public void RecordFloorCleared(int floorNumber) => RecordFloorCleared(floorNumber, DifficultyTier.Forged);

        // A floor cleared on a tier: the depth record and the tier's own, as one change.
        public void RecordFloorCleared(int floorNumber, DifficultyTier tier)
        {
            if (floorNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(floorNumber));
            bool changed = Tiers.RecordFloorCleared(tier, floorNumber);
            if (floorNumber > DeepestFloorCleared)
            {
                DeepestFloorCleared = floorNumber;
                changed = true;
            }
            if (changed)
                Changed?.Invoke();
        }

        // A run walked out of the Descent's last floor on this tier: the tier is completed, which opens the next one.
        // False when it already was.
        public bool RecordDescentCompleted(DifficultyTier tier)
        {
            if (!Tiers.RecordCompleted(tier))
                return false;
            if (DescentStart.FinaleFloor > DeepestFloorCleared)
                DeepestFloorCleared = DescentStart.FinaleFloor;
            Changed?.Invoke();
            return true;
        }

        // Whether the hero carries the skill slot: skill 1 always, skills 2 and 3 once the Forge has forged them.
        public bool HasForgedSkill(string heroId, int slot) => Skills.IsForged(heroId, slot);

        // What the Forge shows for the hero's skill slot (HeroSkillUnlock.StatusOf): forged, locked until its floor's first
        // clear (any tier, any hero), waiting on skill 2, or affordable or not.
        public SkillForgeStatus SkillStatusOf(string heroId, int slot) =>
            HeroSkillUnlock.StatusOf(slot, DeepestFloorCleared, Skills.IsForged(heroId, slot),
                Skills.IsForged(heroId, HeroSkillUnlock.Requires(slot)), Gold);

        // The gold still needed to forge the hero's skill slot; 0 once it is forged or affordable.
        public int GoldNeededForSkill(string heroId, int slot) =>
            Skills.IsForged(heroId, slot) ? 0 : Math.Max(0, HeroSkillUnlock.Price(slot) - Gold);

        // The Forge forges the hero's skill 2 or 3 (docs/57 section 12): open (its floor cleared), skill 3 after skill 2,
        // not yet forged and affordable - the price spent and the skill recorded as one change. False, and nothing spent,
        // otherwise.
        public bool TryForgeSkill(string heroId, int slot)
        {
            if (string.IsNullOrEmpty(heroId))
                throw new ArgumentException("A hero id is required.", nameof(heroId));
            if (!HeroSkillUnlock.IsForgeable(slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (SkillStatusOf(heroId, slot) != SkillForgeStatus.Affordable)
                return false;
            Gold -= HeroSkillUnlock.Price(slot);
            Skills.Forge(heroId, slot);
            Changed?.Invoke();
            return true;
        }

        // Whether docs/57 5.2's one-line toast has introduced the hero's forged skill 2 or 3: once per skill and profile, on
        // the first run that carries it (SkillToastView).
        public bool HasShownSkillToast(string heroId, int slot) => Skills.ToastShown(heroId, slot);

        // Records the toast shown, as one change (the save's trigger); false, and nothing changed, for a skill not forged
        // or a toast already shown.
        public bool MarkSkillToastShown(string heroId, int slot)
        {
            if (!Skills.MarkToastShown(heroId, slot))
                return false;
            Changed?.Invoke();
            return true;
        }

        // Chooses the tier for the next run; only an open tier, and false when nothing changed.
        public bool SelectTier(DifficultyTier tier)
        {
            if (!Tiers.Select(tier))
                return false;
            Changed?.Invoke();
            return true;
        }

        // Spends the price and records ownership when the item is new and affordable; the caller equips and saves.
        private bool TrySpendOn(List<string> owned, string id, int price)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("An item id is required.", nameof(id));
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price));
            if (owned.Contains(id) || Gold < price)
                return false;

            Gold -= price;
            owned.Add(id);
            return true;
        }

        private static void AddUnique(List<string> owned, IEnumerable<string> ids)
        {
            if (ids == null)
                return;

            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id) && !owned.Contains(id))
                    owned.Add(id);
            }
        }
    }
}
