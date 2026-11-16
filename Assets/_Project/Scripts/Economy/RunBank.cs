using System;
using System.Collections.Generic;
using Cryptforge.Core;
using Cryptforge.Progression;

namespace Cryptforge.Economy
{
    // Moves run gold into the profile: gold secured at a checkpoint right away, and the rest of the banked gold when
    // the run ends. Depositing at the checkpoint keeps a descent's secured gold even if the app closes mid-floor.
    // With a run's loot (docs/54 phase 3) it banks that the same way - materials and boosters secured at a checkpoint
    // right away, the rest as the run ends after a death's share is taken (RunLoot.Settle) - except items, which are
    // always kept (the lead, 2026-11-16) and go to the profile the moment they drop.
    public sealed class RunBank
    {
        private readonly RunState _run;
        private readonly PlayerProfile _profile;
        private readonly RunLoot _loot;
        private readonly int[] _materialsDeposited = new int[GearCurrency.MaterialCount];
        private readonly int[] _boostersDeposited = new int[GearCurrency.BoosterCount];
        private readonly List<ItemDrop> _oneItem = new List<ItemDrop>(1);

        public int Deposited { get; private set; }
        // What the profile did with the run's items: the ones a full bag melted, for the result screen.
        public LootDeposit ItemDeposits { get; } = new LootDeposit();

        public RunBank(RunState run, PlayerProfile profile, RunLoot loot = null)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _loot = loot;
            _run.GoldChanged += OnGoldChanged;
            _run.Ended += OnRunEnded;
            if (_loot != null)
            {
                _loot.ItemDropped += OnItemDropped;
                _loot.Secured += DepositSecuredLoot;
            }
        }

        private void OnGoldChanged()
        {
            if (!_run.HasEnded)
                DepositUpTo(_run.SecuredGold);
        }

        // Banked gold is never below secured gold, because a defeat only loses part of the unsecured gold.
        private void OnRunEnded()
        {
            DepositUpTo(_run.GoldBanked);
            if (_loot == null)
                return;
            DepositSecuredLoot();
            LootSettlement settlement = _loot.Settle(_run.Outcome);
            _profile.DepositLoot(settlement.MaterialsKept, settlement.BoostersKept, null);
        }

        private void DepositUpTo(int total)
        {
            int amount = total - Deposited;
            if (amount <= 0)
                return;

            Deposited = total;
            _profile.Deposit(amount);
        }

        private void OnItemDropped(ItemDrop item)
        {
            _oneItem.Clear();
            _oneItem.Add(item);
            LootDeposit deposit = _profile.DepositLoot(null, null, _oneItem);
            ItemDeposits.Stored.AddRange(deposit.Stored);
            ItemDeposits.Melted.AddRange(deposit.Melted);
            ItemDeposits.MeltGold += deposit.MeltGold;
            ItemDeposits.MeltEmber += deposit.MeltEmber;
        }

        // The secured loot not yet in the profile, as one change.
        private void DepositSecuredLoot()
        {
            var materials = new int[GearCurrency.MaterialCount];
            var boosters = new int[GearCurrency.BoosterCount];
            bool any = false;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = _loot.SecuredMaterial((MaterialKind)i) - _materialsDeposited[i];
                _materialsDeposited[i] += materials[i];
                any |= materials[i] > 0;
            }
            for (int i = 0; i < boosters.Length; i++)
            {
                boosters[i] = _loot.SecuredBooster((BoosterKind)i) - _boostersDeposited[i];
                _boostersDeposited[i] += boosters[i];
                any |= boosters[i] > 0;
            }
            if (any)
                _profile.DepositLoot(materials, boosters, null);
        }
    }
}
