using System;
using System.Collections.Generic;

namespace Cryptforge.Core
{
    // Permanent progress between runs: banked gold, forged relics, the equipped relic and the depth record.
    // Every successful change raises Changed exactly once, which is the save trigger.
    public sealed class PlayerProfile
    {
        private readonly List<string> _ownedRelicIds = new List<string>();

        public int Gold { get; private set; }
        public string EquippedRelicId { get; private set; }
        public int DeepestFloorCleared { get; private set; }
        public IReadOnlyList<string> OwnedRelicIds => _ownedRelicIds;
        public event Action Changed;

        public PlayerProfile()
        {
        }

        // Restores saved progress. Damaged values are repaired instead of rejected so a hand-edited or partially
        // valid save still loads: negative numbers become zero, duplicates are dropped and an unowned relic is unequipped.
        public PlayerProfile(int gold, IEnumerable<string> ownedRelicIds, string equippedRelicId, int deepestFloorCleared)
        {
            Gold = Math.Max(0, gold);
            DeepestFloorCleared = Math.Max(0, deepestFloorCleared);
            if (ownedRelicIds != null)
            {
                foreach (string id in ownedRelicIds)
                {
                    if (!string.IsNullOrEmpty(id) && !_ownedRelicIds.Contains(id))
                        _ownedRelicIds.Add(id);
                }
            }

            EquippedRelicId = Owns(equippedRelicId) ? equippedRelicId : null;
        }

        public bool Owns(string relicId) => !string.IsNullOrEmpty(relicId) && _ownedRelicIds.Contains(relicId);

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
            if (string.IsNullOrEmpty(relicId))
                throw new ArgumentException("Relic id is required.", nameof(relicId));
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price));
            if (Owns(relicId) || Gold < price)
                return false;

            Gold -= price;
            _ownedRelicIds.Add(relicId);
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

        public void RecordFloorCleared(int floorNumber)
        {
            if (floorNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(floorNumber));
            if (floorNumber <= DeepestFloorCleared)
                return;

            DeepestFloorCleared = floorNumber;
            Changed?.Invoke();
        }
    }
}
