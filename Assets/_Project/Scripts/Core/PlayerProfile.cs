using System;
using System.Collections.Generic;

namespace Cryptforge.Core
{
    // Permanent progress between runs: banked gold, forged relics and weapons, what is equipped and the depth record.
    // Every successful change raises Changed exactly once, which is the save trigger.
    public sealed class PlayerProfile
    {
        private readonly List<string> _ownedRelicIds = new List<string>();
        private readonly List<string> _ownedWeaponIds = new List<string>();

        public int Gold { get; private set; }
        public string EquippedRelicId { get; private set; }
        // Null while the hero carries the starting weapon, which is never stored as owned.
        public string EquippedWeaponId { get; private set; }
        public int DeepestFloorCleared { get; private set; }
        public IReadOnlyList<string> OwnedRelicIds => _ownedRelicIds;
        public IReadOnlyList<string> OwnedWeaponIds => _ownedWeaponIds;
        public event Action Changed;

        public PlayerProfile()
        {
        }

        // Restores saved progress. Damaged values are repaired instead of rejected so a hand-edited or partially
        // valid save still loads: negative numbers become zero, duplicates are dropped and unowned items are unequipped.
        public PlayerProfile(int gold, IEnumerable<string> ownedRelicIds, string equippedRelicId, int deepestFloorCleared,
            IEnumerable<string> ownedWeaponIds = null, string equippedWeaponId = null)
        {
            Gold = Math.Max(0, gold);
            DeepestFloorCleared = Math.Max(0, deepestFloorCleared);
            AddUnique(_ownedRelicIds, ownedRelicIds);
            AddUnique(_ownedWeaponIds, ownedWeaponIds);
            EquippedRelicId = Owns(equippedRelicId) ? equippedRelicId : null;
            EquippedWeaponId = OwnsWeapon(equippedWeaponId) ? equippedWeaponId : null;
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

        public bool TryForgeWeapon(string weaponId, int price)
        {
            if (!TrySpendOn(_ownedWeaponIds, weaponId, price))
                return false;

            EquippedWeaponId = weaponId;
            Changed?.Invoke();
            return true;
        }

        public bool EquipWeapon(string weaponId)
        {
            if (!OwnsWeapon(weaponId) || EquippedWeaponId == weaponId)
                return false;

            EquippedWeaponId = weaponId;
            Changed?.Invoke();
            return true;
        }

        // Returns the hero to the starting weapon.
        public bool UnequipWeapon()
        {
            if (EquippedWeaponId == null)
                return false;

            EquippedWeaponId = null;
            Changed?.Invoke();
            return true;
        }

        public void RecordFloorCleared(int floorNumber)
        {
            if (floorNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(floorNumber));
            if (floorNumber <= DeepestFloorCleared)
                return;

            DeepestFloorCleared = floorNumber;
            Changed?.Invoke();
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
