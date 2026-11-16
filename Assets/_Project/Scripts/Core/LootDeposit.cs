using System.Collections.Generic;
using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // What PlayerProfile.DepositLoot did with a run's items: the ones the bag took, and the ones a full bag melted at once
    // with the gold and Ember Shards that gave.
    public sealed class LootDeposit
    {
        public readonly List<ItemInstance> Stored = new List<ItemInstance>();
        public readonly List<ItemDrop> Melted = new List<ItemDrop>();
        public int MeltGold;
        public int MeltEmber;
    }
}
