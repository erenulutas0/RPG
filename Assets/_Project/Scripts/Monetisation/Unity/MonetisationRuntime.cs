using System;
using System.Collections.Generic;
using Cryptforge.Analytics;
using Cryptforge.Core;
using Cryptforge.Save;
using UnityEngine;

namespace Cryptforge.Monetisation
{
    // The monetisation of one app launch (docs/58 section 7.4), beside TelemetryRuntime and in its shape: the gameplay scene
    // is rebuilt on every restart, but the offer's counters, the ledger, the entitlement and the play sessions are the
    // launch's. A deliberate exception to the no-static-state rule, like TelemetryRuntime and ProfileLocation: one runtime
    // per profile folder, built the first time the folder is asked for and replaced only when it changes, which in practice
    // means a scene test pointed the profile somewhere else.
    //
    // It holds, over the offline fakes until the SDKs come (docs/58 3.5; a Packages change at the owner's request):
    // - the skip's EntitlementService over a FakeStoreEntitlements, its cache (entitlements.json) beside the profile, asked
    //   at boot (Refresh). The fake's account starts as the cache says, so a skip "bought" in an earlier launch stays
    //   bought, as a real store's purchase does;
    // - the RewardLedgerStore (rewards.json) and its ledger;
    // - a SessionClock started with the telemetry session, told of the app leaving and returning by a hidden
    //   DontDestroyOnLoad PlaySessionForwarder (the bridge's OnApplicationPause lives only with a gameplay scene);
    // - the RewardOfferCoordinator with a FakeRewardedVideo and FakeAdConsent(true), the skip's ownership from the
    //   service, the ledger written by its store, and the telemetry through LocalFileAnalytics.
    // At boot the consent is gathered and a video asked for (consent.Gather(() => offer.Prefetch())).
    //
    // In the Editor and a development build development/ads.txt says how the fake video ends (DevelopmentStart.AdsOutcome:
    // completed, skipped, failed or noad), read again as each run begins, so every state of the offer can be captured.
    // Nothing here logs in normal play: only an unreadable rewards.json or entitlements.json is reported, as the profile's is.
    public static class MonetisationRuntime
    {
        // Written when the app returns from a trip that ended the play session: the new session's ordinal and the ended
        // one's length in seconds (SessionClock).
        public const string PlaySessionEvent = "play_session";

        private static string _directory;
        private static TelemetrySession _session;
        private static IAnalytics _analytics;
        private static FakeStoreEntitlements _store;
        private static EntitlementService _entitlements;
        private static RewardLedgerStore _ledgerStore;
        private static SessionClock _sessions;
        private static FakeRewardedVideo _video;
        private static FakeAdConsent _consent;
        private static RewardOfferCoordinator _offer;
        private static PlaySessionForwarder _forwarder;

        // What the skip's entitlement or its purchase changed: the offer's button, the result screen's link and the pause
        // overlay's entry draw again.
        public static event Action Changed;

        // The offer of this launch's result screens.
        public static RewardOfferCoordinator Offer
        {
            get
            {
                Ensure();
                return _offer;
            }
        }

        // The skip's entitlement: the cache, the store's answer and the purchase.
        public static EntitlementService Entitlements
        {
            get
            {
                Ensure();
                return _entitlements;
            }
        }

        public static bool OwnsSkip => Entitlements.OwnsSkip;

        // The fakes, for the tests, the captures and the development switch.
        public static FakeRewardedVideo Video
        {
            get
            {
                Ensure();
                return _video;
            }
        }

        public static FakeStoreEntitlements Store
        {
            get
            {
                Ensure();
                return _store;
            }
        }

        public static FakeAdConsent Consent
        {
            get
            {
                Ensure();
                return _consent;
            }
        }

        public static SessionClock Sessions
        {
            get
            {
                Ensure();
                return _sessions;
            }
        }

        public static RewardLedgerStore LedgerStore
        {
            get
            {
                Ensure();
                return _ledgerStore;
            }
        }

        // How the last purchase of the skip ended (null before one, and while one is open), for the plain words under it.
        public static StorePurchaseResult? LastPurchase { get; private set; }
        public static bool IsPurchasing => Entitlements.IsPurchasing;

        // The folder this launch's runtime keeps its files in.
        public static string Directory
        {
            get
            {
                Ensure();
                return _directory;
            }
        }

        // A run begins (CombatSetup, as the run's telemetry begins): the development switch is read again and the next
        // video asked for ahead of the result screen (RewardOfferCoordinator.Prefetch: never without consent, never for an
        // owner of the skip, not while one is ready, loading or showing).
        public static void BeginRun()
        {
            Ensure();
            ApplyDevelopmentOutcome(_video);
            _offer.Prefetch();
        }

        // "Skip reward videos" (docs/58 section 1, 7.7 question 1), from the result screen's link or the pause overlay's
        // entry: the store's purchase of StoreProducts.SkipRewardVideos. With the fake store a tap buys it at once. Nothing
        // for an owner, or while a purchase is open.
        public static void BuySkip()
        {
            Ensure();
            EntitlementService entitlements = _entitlements;
            if (entitlements.OwnsSkip || entitlements.IsPurchasing)
                return;
            LastPurchase = null;
            entitlements.Purchase(StoreProducts.SkipRewardVideos, result =>
            {
                // A purchase that ends after the folder changed (a test's) belongs to the runtime it was made with.
                if (!ReferenceEquals(entitlements, _entitlements))
                    return;
                LastPurchase = result;
                Changed?.Invoke();
            });
            if (entitlements.IsPurchasing)
                Changed?.Invoke();
        }

        // Writes what the offer and the store buffered (a result screen closing): never builds a runtime, so a scene going
        // away after its test moved the profile on writes nothing anywhere new.
        public static void Flush() => _analytics?.Flush();

        // The runtime of the profile folder in use, built the first time that folder is asked for.
        private static void Ensure()
        {
            string directory = ProfileLocation.Resolve();
            if (_offer == null || !string.Equals(_directory, directory, StringComparison.Ordinal))
                Build(directory);
        }

        private static void Build(string directory)
        {
            if (_entitlements != null)
                _entitlements.Changed -= OnEntitlementsChanged;

            TelemetrySession session = TelemetryRuntime.SessionFor(TelemetryLocation.Resolve());
            var analytics = new LocalFileAnalytics(session);

            var store = new FakeStoreEntitlements();
            var cache = new EntitlementCache(directory);
            var entitlements = new EntitlementService(store, cache, analytics);
            Report(cache.LastLoadProblems, EntitlementCache.FileName, directory);
            // The fake's account is what the cache last confirmed, as a real store's account outlives the app: without it
            // the boot's query would answer "nothing owned" and take back a skip bought in an earlier launch.
            foreach (string owned in entitlements.Data.OwnedProductIds)
                store.SetOwned(owned, true);

            var ledgerStore = new RewardLedgerStore(directory);
            RewardLedger ledger = ledgerStore.Load();
            Report(ledgerStore.LastLoadProblems, RewardLedgerStore.FileName, directory);

            var sessions = new SessionClock();
            sessions.Start(session.Clock.Seconds);
            var video = new FakeRewardedVideo();
            var consent = new FakeAdConsent(true);
            var offer = new RewardOfferCoordinator(new RewardOfferContext
            {
                Video = video,
                Consent = consent,
                OwnsSkip = () => entitlements.OwnsSkip,
                Ledger = ledger,
                SaveLedger = ledgerStore.Save,
                Analytics = analytics,
                Sessions = sessions
            });

            _directory = directory;
            _session = session;
            _analytics = analytics;
            _store = store;
            _entitlements = entitlements;
            _ledgerStore = ledgerStore;
            _sessions = sessions;
            _video = video;
            _consent = consent;
            _offer = offer;
            LastPurchase = null;
            entitlements.Changed += OnEntitlementsChanged;
            EnsureForwarder();

            ApplyDevelopmentOutcome(video);
            entitlements.Refresh();
            consent.Gather(() => offer.Prefetch());
        }

        // development/ads.txt (DevelopmentStart.AdsOutcome): how the fake video ends. "noad" never fills and lets a ready
        // video go; anything else fills, and ends completed (the default), skipped or failed.
        private static void ApplyDevelopmentOutcome(FakeRewardedVideo video)
        {
            string outcome = DevelopmentStart.AdsOutcome();
            video.FillByDefault = !string.Equals(outcome, DevelopmentStart.AdsNoAd, StringComparison.Ordinal);
            video.ResultByDefault = string.Equals(outcome, DevelopmentStart.AdsSkipped, StringComparison.Ordinal)
                ? RewardedVideoResult.Skipped
                : string.Equals(outcome, DevelopmentStart.AdsFailed, StringComparison.Ordinal)
                    ? RewardedVideoResult.Failed
                    : RewardedVideoResult.Completed;
            if (!video.FillByDefault && video.IsReady)
                video.Expire();
        }

        private static void OnEntitlementsChanged() => Changed?.Invoke();

        private static void EnsureForwarder()
        {
            if (_forwarder != null)
                return;
            var host = new GameObject(PlaySessionForwarder.ObjectName) { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(host);
            _forwarder = host.AddComponent<PlaySessionForwarder>();
        }

        // The app left the foreground or came back (PlaySessionForwarder): the session clock decides whether a play session
        // ended (a background trip to a video keeps it, docs/59 3.3), and a return that starts a new one writes play_session
        // with the new session's ordinal and the ended one's length.
        internal static void ApplicationPaused(bool paused)
        {
            if (_sessions == null || _session == null)
                return;
            double now = _session.Clock.Seconds;
            if (paused)
            {
                _sessions.Backgrounded(now);
                return;
            }
            SessionResume resume = _sessions.Foregrounded(now);
            if (resume.NewSession)
                _analytics.Emit(PlaySessionEvent, new TelemetryFields()
                    .Add("ordinal", resume.Ordinal)
                    .Add("ended_sec", resume.EndedLength));
        }

        private static void Report(IReadOnlyList<string> problems, string file, string directory)
        {
            if (problems == null || problems.Count == 0)
                return;
            Debug.LogWarning($"{file} in {directory} had unreadable copies: {string.Join("; ", problems)}");
        }

        // With domain reload disabled in the Editor, statics survive into the next Play session; a new Play session is a
        // new launch, so it starts without the previous runtime (its forwarder went with the previous Play session).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewLaunch()
        {
            if (_entitlements != null)
                _entitlements.Changed -= OnEntitlementsChanged;
            Changed = null;
            _directory = null;
            _session = null;
            _analytics = null;
            _store = null;
            _entitlements = null;
            _ledgerStore = null;
            _sessions = null;
            _video = null;
            _consent = null;
            _offer = null;
            _forwarder = null;
            LastPurchase = null;
        }
    }
}
