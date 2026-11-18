using System;
using System.Collections.Generic;

namespace Cryptforge.Monetisation
{
    // The offline stand-in for the store (docs/58 3.5). It keeps the account's completed purchases and whether each was
    // acknowledged; a purchase's outcome is scriptable (ScriptPurchases, else PurchaseByDefault), Online false makes every
    // call fail as a phone without a network would, SetOwned and Revoke stand for a purchase made elsewhere and a refund.
    // Immediate by default; Deferred holds every callback until CompletePending.
    public sealed class FakeStoreEntitlements : IStoreEntitlements
    {
        private readonly List<string> _owned = new List<string>();
        private readonly List<bool> _acknowledged = new List<bool>();
        private readonly Queue<StorePurchaseResult> _purchases = new Queue<StorePurchaseResult>();
        private readonly Queue<Action> _pending = new Queue<Action>();

        public bool Online { get; set; } = true;
        public StorePurchaseResult PurchaseByDefault { get; set; } = StorePurchaseResult.Purchased;
        public bool Deferred { get; set; }

        public int QueryCalls { get; private set; }
        public int PurchaseCalls { get; private set; }
        public int AcknowledgeCalls { get; private set; }
        public int Pending => _pending.Count;

        public FakeStoreEntitlements ScriptPurchases(params StorePurchaseResult[] results)
        {
            if (results != null)
            {
                foreach (StorePurchaseResult result in results)
                    _purchases.Enqueue(result);
            }
            return this;
        }

        // The account owns the product already: bought on another phone, or before a reinstall.
        public void SetOwned(string productId, bool acknowledged)
        {
            int index = IndexOf(productId);
            if (index < 0)
            {
                _owned.Add(productId);
                _acknowledged.Add(acknowledged);
            }
            else
            {
                _acknowledged[index] = acknowledged;
            }
        }

        // A refund or a revocation: the product leaves the account.
        public void Revoke(string productId)
        {
            int index = IndexOf(productId);
            if (index < 0)
                return;
            _owned.RemoveAt(index);
            _acknowledged.RemoveAt(index);
        }

        public bool IsOwned(string productId) => IndexOf(productId) >= 0;

        public bool IsAcknowledged(string productId)
        {
            int index = IndexOf(productId);
            return index >= 0 && _acknowledged[index];
        }

        public void QueryOwned(Action<StoreQueryResult> onResult)
        {
            if (onResult == null)
                throw new ArgumentNullException(nameof(onResult));
            QueryCalls++;
            if (!Online)
            {
                Deliver(() => onResult(StoreQueryResult.Failed()));
                return;
            }
            var owned = new List<OwnedProduct>(_owned.Count);
            for (int i = 0; i < _owned.Count; i++)
                owned.Add(new OwnedProduct(_owned[i], _acknowledged[i]));
            StoreQueryResult result = StoreQueryResult.Of(owned);
            Deliver(() => onResult(result));
        }

        public void Purchase(string productId, Action<StorePurchaseResult> onResult)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("A purchase needs a product id.", nameof(productId));
            if (onResult == null)
                throw new ArgumentNullException(nameof(onResult));
            PurchaseCalls++;
            StorePurchaseResult result = _purchases.Count > 0 ? _purchases.Dequeue() : PurchaseByDefault;
            if (!Online)
                result = StorePurchaseResult.Failed;
            else if (IsOwned(productId))
                result = StorePurchaseResult.AlreadyOwned;
            else if (result == StorePurchaseResult.Purchased)
                SetOwned(productId, false);
            Deliver(() => onResult(result));
        }

        public void Acknowledge(string productId, Action<bool> onAcknowledged)
        {
            if (onAcknowledged == null)
                throw new ArgumentNullException(nameof(onAcknowledged));
            AcknowledgeCalls++;
            int index = IndexOf(productId);
            bool done = Online && index >= 0;
            if (done)
                _acknowledged[index] = true;
            Deliver(() => onAcknowledged(done));
        }

        // Runs every held callback in order, including any queued while they run; how many ran.
        public int CompletePending()
        {
            int ran = 0;
            while (_pending.Count > 0)
            {
                _pending.Dequeue()();
                ran++;
            }
            return ran;
        }

        private void Deliver(Action callback)
        {
            if (Deferred)
                _pending.Enqueue(callback);
            else
                callback();
        }

        private int IndexOf(string productId)
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (string.Equals(_owned[i], productId, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }
    }
}
