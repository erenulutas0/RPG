using System;
using System.Collections.Generic;
using Cryptforge.Core;
using Cryptforge.Progression;

namespace Cryptforge.Economy
{
    // What a run's death took from its loot, and what it kept (docs/54 section 5, the lead's decisions of 2026-11-16).
    public sealed class LootSettlement
    {
        public readonly int[] MaterialsKept = new int[GearCurrency.MaterialCount];
        public readonly int[] MaterialsLost = new int[GearCurrency.MaterialCount];
        public readonly int[] BoostersKept = new int[GearCurrency.BoosterCount];
        public readonly int[] BoostersLost = new int[GearCurrency.BoosterCount];

        public bool LostAnything
        {
            get
            {
                for (int i = 0; i < MaterialsLost.Length; i++)
                    if (MaterialsLost[i] > 0)
                        return true;
                for (int i = 0; i < BoostersLost.Length; i++)
                    if (BoostersLost[i] > 0)
                        return true;
                return false;
            }
        }
    }

    // A run's satchel (docs/54 phase 3): the floor materials, boosters and items its bosses, elites, floor clears and
    // walk-out drop (LootRule). Materials and boosters follow the gold's rule - secured at each Descend, and a defeat loses
    // a share of what the floor it ended on dropped (half: floor of the materials, a seeded coin per booster). Items are
    // always kept (the lead, 2026-11-16): each goes to the profile the moment it drops (RunBank). Nothing here touches the
    // fight: loot is never on the ground, and its draws are its own stream (RunRandom.Loot).
    public sealed class RunLoot
    {
        private readonly RunState _run;
        private readonly ItemTemplate[] _items;
        private readonly float _atRiskShare;
        private readonly int[] _securedMaterials = new int[GearCurrency.MaterialCount];
        private readonly int[] _unsecuredMaterials = new int[GearCurrency.MaterialCount];
        private readonly int[] _securedBoosters = new int[GearCurrency.BoosterCount];
        // The boosters dropped since the last Descend, in order: a death throws one coin each.
        private readonly List<BoosterKind> _unsecuredBoosters = new List<BoosterKind>();
        private readonly List<ItemDrop> _itemDrops = new List<ItemDrop>();
        private bool _giftOwed;
        private int _moments;

        public IReadOnlyList<ItemDrop> Items => _itemDrops;
        public LootSettlement Settlement { get; private set; }

        // Every drop moment with anything in it, as it drops.
        public event Action<LootDrop> Dropped;
        // An item, as it drops: the bank puts it in the profile at once.
        public event Action<ItemDrop> ItemDropped;
        // A Descend made the floor's loot safe: the bank deposits what became secured.
        public event Action Secured;

        public RunLoot(RunState run, IReadOnlyList<ItemTemplate> items, bool firstArmourOwed, float atRiskShare = 0.5f)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            if (float.IsNaN(atRiskShare) || atRiskShare < 0f || atRiskShare > 1f)
                throw new ArgumentOutOfRangeException(nameof(atRiskShare));
            _items = items != null ? new List<ItemTemplate>(items).ToArray() : new ItemTemplate[0];
            _giftOwed = firstArmourOwed;
            _atRiskShare = atRiskShare;
        }

        public int SecuredMaterial(MaterialKind kind) => _securedMaterials[(int)kind];
        public int UnsecuredMaterial(MaterialKind kind) => _unsecuredMaterials[(int)kind];
        public int SecuredBooster(BoosterKind kind) => _securedBoosters[(int)kind];

        public int UnsecuredBooster(BoosterKind kind)
        {
            int count = 0;
            for (int i = 0; i < _unsecuredBoosters.Count; i++)
                if (_unsecuredBoosters[i] == kind)
                    count++;
            return count;
        }

        // A floor's gate boss fell; the first floor-1 boss a profile fells drops its armour.
        public void BossFelled(int floor)
        {
            bool gift = _giftOwed && floor == 1;
            if (gift)
                _giftOwed = false;
            Take(LootSource.Boss, floor, gift);
        }

        public void EliteFelled(int floor) => Take(LootSource.Elite, floor, false);
        public void FloorCleared(int floor) => Take(LootSource.FloorClear, floor, false);
        public void WalkedOut(int floor) => Take(LootSource.WalkOut, floor, false);

        // The checkpoint's Descend: everything dropped so far is safe.
        public void Secure()
        {
            if (_run.HasEnded)
                return;
            bool any = false;
            for (int i = 0; i < _unsecuredMaterials.Length; i++)
            {
                any |= _unsecuredMaterials[i] > 0;
                _securedMaterials[i] += _unsecuredMaterials[i];
                _unsecuredMaterials[i] = 0;
            }
            foreach (BoosterKind kind in _unsecuredBoosters)
                _securedBoosters[(int)kind]++;
            any |= _unsecuredBoosters.Count > 0;
            _unsecuredBoosters.Clear();
            if (any)
                Secured?.Invoke();
        }

        // The run's end: what of the unsecured loot it keeps. A defeat loses the at-risk share (materials rounded down, as
        // gold is; a coin per booster); any other end keeps it all. Settled once; later calls return the same settlement.
        public LootSettlement Settle(RunOutcome outcome)
        {
            if (Settlement != null)
                return Settlement;
            var settlement = new LootSettlement();
            bool defeat = outcome == RunOutcome.Defeat;
            for (int i = 0; i < _unsecuredMaterials.Length; i++)
            {
                int lost = defeat ? (int)Math.Floor(_unsecuredMaterials[i] * (double)_atRiskShare) : 0;
                settlement.MaterialsLost[i] = lost;
                settlement.MaterialsKept[i] = _unsecuredMaterials[i] - lost;
            }
            int lossParts = (int)Math.Round(_atRiskShare * LootRule.Parts);
            for (int coin = 0; coin < _unsecuredBoosters.Count; coin++)
            {
                int kind = (int)_unsecuredBoosters[coin];
                if (!defeat || LootRule.KeepsOnDeath(_run.Seed, coin, lossParts))
                    settlement.BoostersKept[kind]++;
                else
                    settlement.BoostersLost[kind]++;
            }
            Settlement = settlement;
            return settlement;
        }

        private void Take(LootSource source, int floor, bool gift)
        {
            if (_run.HasEnded)
                return;
            LootDrop drop = LootRule.Roll(source, floor, _run.Seed, _moments++, _items, gift);
            if (drop.HasMaterial && drop.MaterialAmount > 0)
                _unsecuredMaterials[(int)drop.Material] += drop.MaterialAmount;
            for (int kind = 0; kind < GearCurrency.BoosterCount; kind++)
                for (int n = 0; n < drop.Booster((BoosterKind)kind); n++)
                    _unsecuredBoosters.Add((BoosterKind)kind);
            if (drop.IsEmpty)
                return;
            Dropped?.Invoke(drop);
            if (drop.Item != null)
            {
                _itemDrops.Add(drop.Item);
                ItemDropped?.Invoke(drop.Item);
            }
        }
    }
}
