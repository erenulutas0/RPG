using System;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Analytics;

namespace Cryptforge.Monetisation
{
    // The skip's entitlement (docs/58 3.3) over the store and the cache. It starts from the cache, so the skip works offline
    // from the first frame; Refresh (on start) asks the store, and a store that answers is the truth: the cache becomes its
    // list (a refund or another account takes the skip away) and every completed purchase not yet acknowledged is
    // acknowledged. A store that cannot answer leaves the cache as it was. Purchase grants on Purchased and AlreadyOwned,
    // acknowledges a new purchase, and writes store_purchase. A Pending purchase grants nothing: the next Refresh that sees
    // it completed grants it. Restoring a purchase (Refresh) is apart from restoring a save: neither touches the other.
    public sealed class EntitlementService
    {
        private readonly IStoreEntitlements _store;
        private readonly EntitlementCache _cache;
        private readonly IAnalytics _analytics;
        private readonly Func<DateTime> _utcNow;
        private readonly EntitlementCacheData _data;
        private bool _purchasing;
        // Products a purchase granted while a query was out: the query's list was taken before them, so it must not take
        // them away (a skip bought while Play Billing was still connecting).
        private readonly List<string> _grantedDuringQuery = new List<string>();

        // Fires when what is owned changes.
        public event Action Changed;

        public EntitlementCacheData Data => _data;
        public bool IsRefreshing { get; private set; }
        public bool IsPurchasing => _purchasing;
        // Whether the last finished query had an answer; null before one finishes.
        public bool? LastRefreshSucceeded { get; private set; }
        public int AcknowledgeFailures { get; private set; }
        // The type and message of the last cache write that failed; null after a success.
        public string LastPersistError { get; private set; }

        // cache null: nothing is written (a test, or a capture). analytics null: no telemetry. utcNow: DateTime.UtcNow.
        public EntitlementService(IStoreEntitlements store, EntitlementCache cache, IAnalytics analytics = null,
            Func<DateTime> utcNow = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _cache = cache;
            _analytics = analytics;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _data = cache != null ? cache.Load() : new EntitlementCacheData();
        }

        public bool Owns(string productId) => _data.Owns(productId);

        public bool OwnsSkip => _data.Owns(StoreProducts.SkipRewardVideos);

        public void Refresh(Action onDone = null)
        {
            if (IsRefreshing)
            {
                onDone?.Invoke();
                return;
            }
            IsRefreshing = true;
            _grantedDuringQuery.Clear();
            _store.QueryOwned(result => OnQueried(result, onDone));
        }

        // Opens the store's purchase for productId; onDone with the store's answer. A second call while one is open is
        // refused as Failed without reaching the store.
        public void Purchase(string productId, Action<StorePurchaseResult> onDone = null)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("A product id is required.", nameof(productId));
            if (_purchasing)
            {
                onDone?.Invoke(StorePurchaseResult.Failed);
                return;
            }
            _purchasing = true;
            _store.Purchase(productId, result => OnPurchased(productId, result, onDone));
        }

        private void OnQueried(StoreQueryResult result, Action onDone)
        {
            if (!IsRefreshing)
                return;
            IsRefreshing = false;
            LastRefreshSucceeded = result != null && result.Succeeded;
            var grantedDuringQuery = new List<string>(_grantedDuringQuery);
            _grantedDuringQuery.Clear();
            if (LastRefreshSucceeded == true)
            {
                var ids = new List<string>(result.Owned.Count);
                foreach (OwnedProduct product in result.Owned)
                    ids.Add(product.ProductId);
                foreach (string granted in grantedDuringQuery)
                {
                    if (!ids.Contains(granted))
                        ids.Add(granted);
                }
                bool changed = _data.ReplaceWith(ids);
                _data.ConfirmedUtc = Now();
                Persist();
                foreach (OwnedProduct product in result.Owned)
                {
                    if (!product.Acknowledged)
                        Acknowledge(product.ProductId);
                }
                if (changed)
                    Changed?.Invoke();
            }
            onDone?.Invoke();
        }

        private void OnPurchased(string productId, StorePurchaseResult result, Action<StorePurchaseResult> onDone)
        {
            if (!_purchasing)
                return;
            _purchasing = false;
            bool granted = false;
            if (result == StorePurchaseResult.Purchased || result == StorePurchaseResult.AlreadyOwned)
            {
                granted = _data.Grant(productId);
                if (IsRefreshing && !_grantedDuringQuery.Contains(productId))
                    _grantedDuringQuery.Add(productId);
                _data.ConfirmedUtc = Now();
                Persist();
            }
            _analytics?.Emit(MonetisationTelemetry.StorePurchaseEvent, MonetisationTelemetry.StorePurchase(productId, result));
            // Acknowledged once granted and written: a crash before it is mended by the next start's Refresh.
            if (result == StorePurchaseResult.Purchased)
                Acknowledge(productId);
            if (granted)
                Changed?.Invoke();
            onDone?.Invoke(result);
        }

        private void Acknowledge(string productId)
        {
            _store.Acknowledge(productId, done =>
            {
                if (!done)
                    AcknowledgeFailures++;
            });
        }

        private DateTime Now()
        {
            DateTime now = _utcNow();
            DateTime utc = now.Kind == DateTimeKind.Local ? now.ToUniversalTime() : now;
            // To the second, as the file keeps it.
            return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        }

        private void Persist()
        {
            if (_cache == null)
                return;
            try
            {
                _cache.Save(_data);
                LastPersistError = null;
            }
            catch (IOException exception)
            {
                LastPersistError = exception.GetType().Name + ": " + exception.Message;
            }
            catch (UnauthorizedAccessException exception)
            {
                LastPersistError = exception.GetType().Name + ": " + exception.Message;
            }
        }
    }
}
