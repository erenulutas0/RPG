using System;
using System.Collections.Generic;

namespace Cryptforge.Monetisation
{
    // How a purchase ended. Pending is Play's "pending" state (a payment still to clear): nothing is granted until a later
    // query reports the product owned. AlreadyOwned is the store refusing a second purchase of a non-consumable: the
    // product is owned, and is granted (it doubles as a restore).
    public enum StorePurchaseResult
    {
        Purchased,
        Pending,
        Cancelled,
        Failed,
        AlreadyOwned
    }

    // One product the store reports as bought and completed, and whether that purchase was acknowledged. Google refunds a
    // purchase left unacknowledged for three days (docs/58 3.3).
    public readonly struct OwnedProduct
    {
        public string ProductId { get; }
        public bool Acknowledged { get; }

        public OwnedProduct(string productId, bool acknowledged)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("An owned product needs its id.", nameof(productId));
            ProductId = productId;
            Acknowledged = acknowledged;
        }
    }

    // The answer to a query of the owned products. A failed query (offline, the store unavailable) says nothing about what
    // is owned: the cache stands. A successful one is the store's whole truth: a product missing from it is not owned
    // (refunded, revoked, or another account).
    public sealed class StoreQueryResult
    {
        private static readonly OwnedProduct[] None = new OwnedProduct[0];

        public bool Succeeded { get; }
        public IReadOnlyList<OwnedProduct> Owned { get; }

        private StoreQueryResult(bool succeeded, IReadOnlyList<OwnedProduct> owned)
        {
            Succeeded = succeeded;
            Owned = owned;
        }

        public static StoreQueryResult Failed() => new StoreQueryResult(false, None);

        public static StoreQueryResult Of(IEnumerable<OwnedProduct> owned)
        {
            var list = owned == null ? new List<OwnedProduct>() : new List<OwnedProduct>(owned);
            return new StoreQueryResult(true, list);
        }
    }

    // The store's side of the one product (docs/58 sections 1 and 3.3): the owned products queried on start, a purchase,
    // and its acknowledgement. Unity IAP's adapter implements it at integration (Play Billing Library 8 or later, docs/59
    // section 2); FakeStoreEntitlements stands in until then. Callbacks arrive on the main thread, once per call.
    public interface IStoreEntitlements
    {
        // The completed purchases of this account's non-consumables (Play's queryPurchases).
        void QueryOwned(Action<StoreQueryResult> onResult);

        // Opens the store's purchase flow for one product.
        void Purchase(string productId, Action<StorePurchaseResult> onResult);

        // Acknowledges a completed purchase; onAcknowledged(false) when it could not be done now (the next start retries).
        void Acknowledge(string productId, Action<bool> onAcknowledged);
    }

    // The store's product ids. The first release sells exactly one (docs/58 section 1).
    public static class StoreProducts
    {
        // "Skip reward videos", a non-consumable of about 99.99 TL. A PLACEHOLDER id: the real one is created in the Play
        // Console when the owner's account exists (docs/59 section 4), and can never change after the first upload.
        public const string SkipRewardVideos = "skip_reward_videos";
    }
}
