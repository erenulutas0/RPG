using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Cryptforge.Monetisation;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The skip's entitlement (docs/58 3.3): cached in its own file for offline play, replaced by the store's answer when it
    // comes, acknowledged after the purchase, and never granted for a pending or failed one.
    public sealed class EntitlementTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 19, 3, 4, 5, 678, DateTimeKind.Utc);

        private string _root;

        [SetUp]
        public void MakeFolder()
        {
            _root = Path.Combine(Path.GetTempPath(), "cryptforge-entitlements-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void TheCacheIsFixedTextInTheInvariantCulture()
        {
            var data = new EntitlementCacheData { ConfirmedUtc = new DateTime(2026, 11, 19, 3, 4, 5, DateTimeKind.Utc) };
            Assert.That(data.Grant(StoreProducts.SkipRewardVideos), Is.True);
            Assert.That(data.Grant(StoreProducts.SkipRewardVideos), Is.False);
            string text = EntitlementCache.Write(data);
            Assert.That(text, Is.EqualTo(
                "{\n" +
                "  \"saveVersion\": 1,\n" +
                "  \"revision\": 0,\n" +
                "  \"confirmedUtc\": \"2026-11-19T03:04:05Z\",\n" +
                "  \"ownedProductIds\": [\"skip_reward_videos\"]\n" +
                "}\n"));

            EntitlementCacheData back = EntitlementCache.Read(text);
            Assert.That(back.Owns(StoreProducts.SkipRewardVideos), Is.True);
            Assert.That(back.ConfirmedUtc, Is.EqualTo(data.ConfirmedUtc));
            Assert.That(back.ConfirmedUtc.Value.Kind, Is.EqualTo(DateTimeKind.Utc));

            Assert.That(EntitlementCache.Write(new EntitlementCacheData()), Does.Contain("\"confirmedUtc\": null").And.Contain("\"ownedProductIds\": []"));
            EntitlementCacheData badDate = EntitlementCache.Read(
                "{\"saveVersion\": 1, \"confirmedUtc\": \"19.11.2026 03:04\", \"ownedProductIds\": [\"skip_reward_videos\"]}");
            Assert.That((badDate.ConfirmedUtc.HasValue, badDate.Owns(StoreProducts.SkipRewardVideos)), Is.EqualTo((false, true)),
                "A date that does not parse costs the date, never the product.");
            Assert.That(EntitlementCache.Read("{\"saveVersion\": 2, \"ownedProductIds\": [\"skip_reward_videos\"]}"), Is.Null);
            Assert.That(EntitlementCache.Read("not json"), Is.Null);
            Assert.That(EntitlementCache.FormatTime(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified)), Is.EqualTo("2026-01-02T03:04:05Z"));

            // Every culture writes the same bytes and reads them back.
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                Assert.That(EntitlementCache.Write(data), Is.EqualTo(text));
                Assert.That(EntitlementCache.Read(text).ConfirmedUtc, Is.EqualTo(data.ConfirmedUtc));
                Thread.CurrentThread.CurrentCulture = new CultureInfo("ar-SA");
                Assert.That(EntitlementCache.Write(data), Is.EqualTo(text), "Not even another calendar changes the date.");
                Assert.That(EntitlementCache.Read(text).ConfirmedUtc, Is.EqualTo(data.ConfirmedUtc));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
        }

        [Test]
        public void TheSkipIsReadableOfflineFromTheCache()
        {
            var store = new FakeStoreEntitlements();
            var online = new EntitlementService(store, new EntitlementCache(_root), null, () => Now);
            Assert.That(online.OwnsSkip, Is.False);
            online.Purchase(StoreProducts.SkipRewardVideos);
            Assert.That(online.OwnsSkip, Is.True);

            // The next launch has no network: the cache answers, and a failed query leaves it alone.
            store.Online = false;
            var offline = new EntitlementService(store, new EntitlementCache(_root), null, () => Now.AddDays(3));
            Assert.That(offline.OwnsSkip, Is.True, "Owned from the first frame, before any query.");
            offline.Refresh();
            Assert.That((offline.OwnsSkip, offline.LastRefreshSucceeded), Is.EqualTo((true, (bool?)false)));
            Assert.That(offline.Data.ConfirmedUtc, Is.EqualTo(new DateTime(2026, 11, 19, 3, 4, 5, DateTimeKind.Utc)),
                "Confirmed when the store last answered, to the second.");
            Assert.That(File.ReadAllText(Path.Combine(_root, EntitlementCache.FileName)), Does.Contain("\"skip_reward_videos\""));
        }

        [Test]
        public void APurchaseIsGrantedWrittenAcknowledgedAndRecorded()
        {
            var store = new FakeStoreEntitlements();
            var analytics = new RecordingAnalytics();
            var cache = new EntitlementCache(_root);
            var service = new EntitlementService(store, cache, analytics, () => Now);
            int changes = 0;
            service.Changed += () => changes++;
            StorePurchaseResult? answer = null;
            service.Purchase(StoreProducts.SkipRewardVideos, r => answer = r);

            Assert.That(answer, Is.EqualTo((StorePurchaseResult?)StorePurchaseResult.Purchased));
            Assert.That((service.OwnsSkip, changes, service.IsPurchasing), Is.EqualTo((true, 1, false)));
            Assert.That(store.IsAcknowledged(StoreProducts.SkipRewardVideos), Is.True, "Acknowledged, or Google refunds it in three days.");
            Assert.That(new EntitlementCache(_root).Load().Owns(StoreProducts.SkipRewardVideos), Is.True, "Written before anything else.");
            Assert.That(analytics.Lines, Is.EqualTo(new[] { "store_purchase @current product=\"skip_reward_videos\" result=\"purchased\"" }));

            // Buying it again is the store's "already owned": still owned, nothing new to acknowledge.
            int acknowledgements = store.AcknowledgeCalls;
            service.Purchase(StoreProducts.SkipRewardVideos);
            Assert.That((service.OwnsSkip, store.AcknowledgeCalls, changes), Is.EqualTo((true, acknowledgements, 1)));
            Assert.That(analytics.Lines[1], Is.EqualTo("store_purchase @current product=\"skip_reward_videos\" result=\"already_owned\""));
        }

        [Test]
        public void CancelledFailedAndPendingPurchasesGrantNothingUntilTheStoreSaysSo()
        {
            var store = new FakeStoreEntitlements().ScriptPurchases(StorePurchaseResult.Cancelled, StorePurchaseResult.Pending);
            var analytics = new RecordingAnalytics();
            var service = new EntitlementService(store, new EntitlementCache(_root), analytics, () => Now);
            service.Purchase(StoreProducts.SkipRewardVideos);
            service.Purchase(StoreProducts.SkipRewardVideos);
            store.Online = false;
            service.Purchase(StoreProducts.SkipRewardVideos);
            Assert.That(service.OwnsSkip, Is.False);
            Assert.That(store.AcknowledgeCalls, Is.EqualTo(0));
            Assert.That(analytics.Named("store_purchase"), Is.EqualTo(new[]
            {
                "store_purchase @current product=\"skip_reward_videos\" result=\"cancelled\"",
                "store_purchase @current product=\"skip_reward_videos\" result=\"pending\"",
                "store_purchase @current product=\"skip_reward_videos\" result=\"failed\""
            }));
            Assert.That(File.Exists(Path.Combine(_root, EntitlementCache.FileName)), Is.False, "Nothing to write.");

            // The pending payment clears: the next start's query grants it and acknowledges it.
            store.Online = true;
            store.SetOwned(StoreProducts.SkipRewardVideos, false);
            service.Refresh();
            Assert.That((service.OwnsSkip, service.LastRefreshSucceeded), Is.EqualTo((true, (bool?)true)));
            Assert.That(store.IsAcknowledged(StoreProducts.SkipRewardVideos), Is.True);
            Assert.That(new EntitlementCache(_root).Load().Owns(StoreProducts.SkipRewardVideos), Is.True);
        }

        [Test]
        public void TheStoresAnswerReplacesTheCacheARefundIncluded()
        {
            var store = new FakeStoreEntitlements();
            store.SetOwned(StoreProducts.SkipRewardVideos, true);
            var service = new EntitlementService(store, new EntitlementCache(_root), null, () => Now);
            Assert.That(service.OwnsSkip, Is.False, "A fresh install knows nothing until the store answers.");
            int changes = 0;
            service.Changed += () => changes++;
            bool done = false;
            service.Refresh(() => done = true);
            Assert.That((service.OwnsSkip, done, changes), Is.EqualTo((true, true, 1)), "Restored from the store, apart from any save.");
            Assert.That(store.AcknowledgeCalls, Is.EqualTo(0), "Already acknowledged.");

            store.Revoke(StoreProducts.SkipRewardVideos);
            service.Refresh();
            Assert.That((service.OwnsSkip, changes), Is.EqualTo((false, 2)), "A refund takes the skip away.");
            Assert.That(new EntitlementCache(_root).Load().OwnedProductIds, Is.Empty);
        }

        [Test]
        public void ACallbackThatComesLaterIsHandledOnceAndASecondPurchaseWaits()
        {
            var store = new FakeStoreEntitlements { Deferred = true };
            var service = new EntitlementService(store, null);
            var answers = new System.Collections.Generic.List<StorePurchaseResult>();
            service.Purchase(StoreProducts.SkipRewardVideos, answers.Add);
            service.Purchase(StoreProducts.SkipRewardVideos, answers.Add);
            Assert.That(answers, Is.EqualTo(new[] { StorePurchaseResult.Failed }), "The second is refused while the first is open.");
            Assert.That((service.IsPurchasing, store.PurchaseCalls), Is.EqualTo((true, 1)));
            service.Refresh();
            service.Refresh();
            Assert.That((service.IsRefreshing, store.QueryCalls), Is.EqualTo((true, 1)));

            store.CompletePending();
            Assert.That(answers, Is.EqualTo(new[] { StorePurchaseResult.Failed, StorePurchaseResult.Purchased }));
            Assert.That((service.OwnsSkip, service.IsPurchasing, service.IsRefreshing), Is.EqualTo((true, false, false)));
            Assert.That(store.IsAcknowledged(StoreProducts.SkipRewardVideos), Is.True);
            Assert.That(service.LastPersistError, Is.Null, "No cache: nothing written, nothing failed.");
            Assert.Throws<ArgumentNullException>(() => new EntitlementService(null, null));
            Assert.Throws<ArgumentException>(() => service.Purchase(""));
        }

        // The review of 2026-11-19: a query sent before a purchase, answered after it, must not take the skip away.
        [Test]
        public void AQueryAnsweredAfterAPurchaseKeepsWhatThePurchaseGranted()
        {
            var store = new HeldStore();
            var service = new EntitlementService(store, new EntitlementCache(_root), null, () => Now);
            service.Refresh();
            service.Purchase(StoreProducts.SkipRewardVideos);
            store.PurchaseAnswer(StorePurchaseResult.Purchased);
            Assert.That(service.OwnsSkip, Is.True);
            store.QueryAnswer(StoreQueryResult.Of(new OwnedProduct[0]));
            Assert.That(service.OwnsSkip, Is.True, "The query's list was taken before the purchase.");
            Assert.That(new EntitlementCache(_root).Load().Owns(StoreProducts.SkipRewardVideos), Is.True);

            service.Refresh();
            store.QueryAnswer(StoreQueryResult.Of(new OwnedProduct[0]));
            Assert.That(service.OwnsSkip, Is.False, "A later query without it is the store's truth again (a refund).");
        }

        // A store whose answers the test hands over in any order.
        private sealed class HeldStore : IStoreEntitlements
        {
            private Action<StoreQueryResult> _query;
            private Action<StorePurchaseResult> _purchase;

            public void QueryOwned(Action<StoreQueryResult> onResult) => _query = onResult;
            public void Purchase(string productId, Action<StorePurchaseResult> onResult) => _purchase = onResult;
            public void Acknowledge(string productId, Action<bool> onAcknowledged) => onAcknowledged(true);

            public void QueryAnswer(StoreQueryResult result)
            {
                Action<StoreQueryResult> answer = _query;
                _query = null;
                answer(result);
            }

            public void PurchaseAnswer(StorePurchaseResult result)
            {
                Action<StorePurchaseResult> answer = _purchase;
                _purchase = null;
                answer(result);
            }
        }
    }
}
