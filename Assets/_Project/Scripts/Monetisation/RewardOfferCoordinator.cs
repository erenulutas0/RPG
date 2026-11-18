using System;
using System.IO;
using Cryptforge.Analytics;

namespace Cryptforge.Monetisation
{
    // What the coordinator works with for the launch, gathered by the composition root (RunTelemetryContext's pattern).
    // Plain fields; the coordinator copies and checks them once. Where a bonus goes is not here: the profile is read again
    // with every scene, so each result screen names its own (Open's creditGold).
    public sealed class RewardOfferContext
    {
        // Required: the placement's video (FakeRewardedVideo until the SDK), the consent gate, whether the skip is owned
        // (EntitlementService.OwnsSkip) and the ledger (RewardLedgerStore.Load).
        public IRewardedVideo Video;
        public IAdConsent Consent;
        public Func<bool> OwnsSkip;
        public RewardLedger Ledger;
        // Optional: where the ledger is written (RewardLedgerStore.Save; null keeps it in memory), the telemetry sink, and
        // the session clock told when a video opens and closes (a background trip to a video is not a new session).
        public Action<RewardLedger> SaveLedger;
        public IAnalytics Analytics;
        public SessionClock Sessions;
        public int MultiplierPercent = RewardOfferRule.MultiplierPercent;
        public int MinimumRunGold = RewardOfferRule.MinimumRunGold;
    }

    // What a tap on the offer's button did.
    public enum RewardTakeResult
    {
        // Nothing: the offer is hidden, over, or its video is on screen.
        NotOffered,
        // The skip's owner was credited at once.
        Credited,
        // A video was shown; its result arrives through Changed (at once with the fake).
        VideoShown,
        // No video was ready: nothing shown, a new one asked for, the run's normal reward stands.
        NoAd
    }

    // The result screen's one rewarded offer (docs/58 sections 1, 3 and 5a) as plain code. The view opens it for a completed
    // run with the run's id, its banked gold, an injected check that the run's result is saved and where the run's bonus
    // goes (its scene's PlayerProfile.Deposit, whose Changed saves the profile); the coordinator then
    // decides (RewardOfferRule), shows the video or credits the skip, credits the bonus once per run id through the ledger,
    // and writes reward_offer, reward_result and ad_counters. It owns docs/58 5a's counters for the launch, so one lives as
    // long as the launch (the wiring holds it beside the telemetry session), not as long as a scene.
    //
    // Save before the video (3.1): State is Video or Skip only while the injected check says the result is saved, and Take
    // reads State at the tap, the last moment before the app leaves for the video. The ledger is written before the gold is
    // credited: an app killed between the two loses the bonus, never doubles it, and the run's own reward is already saved.
    // A skipped or failed video credits nothing and LastResult says so. A skipped video ends the offer; a failed one (no
    // fault of the player's) leaves one retry on the same screen, and a second failure ends it (the lead, 2026-11-19).
    public sealed class RewardOfferCoordinator
    {
        // The one placement's name, for an SDK or backend that wants one (AdMob's ad unit, GameAnalytics' ad events).
        public const string Placement = "result_screen";
        // Failed videos a screen forgives before its offer ends: one, so the player gets one retry.
        public const int RetriesAfterAFailure = 1;

        private readonly IRewardedVideo _video;
        private readonly IAdConsent _consent;
        private readonly Func<bool> _ownsSkip;
        private readonly RewardLedger _ledger;
        private readonly Action<RewardLedger> _saveLedger;
        private readonly IAnalytics _analytics;
        private readonly SessionClock _sessions;
        private readonly int _multiplierPercent;
        private readonly int _minimumRunGold;

        private Func<bool> _resultSaved;
        private Action<int> _creditGold;
        private bool _open;
        private bool _resolved;
        private bool _offerLogged;
        private bool _askedWithoutAd;
        private int _failedShows;
        // The run whose video is on screen, its bonus and where it goes; null when none is.
        private string _showingRunId;
        private int _showingBonus;
        private Action<int> _showingCredit;

        // Fires when the offer's state may have changed: the view reads Refresh() again.
        public event Action Changed;

        public AdCounters Counters { get; } = new AdCounters();
        public RewardLedger Ledger => _ledger;
        public int MultiplierPercent => _multiplierPercent;
        public int MinimumRunGold => _minimumRunGold;
        public bool IsLoading { get; private set; }
        public bool IsShowing => _showingRunId != null;
        // The open result screen's run, its banked gold and the bonus a credit would add; null and 0 when none is open.
        public string RunId { get; private set; }
        public int RunGold { get; private set; }
        public int BonusGold { get; private set; }
        // How the last video or offer ended and whether it credited, for the view's plain words; null before one ends. A
        // first failure sets Failed while its screen still offers the retry (State Video or NoAd again).
        public RewardResultKind? LastResult { get; private set; }
        public bool LastCredited { get; private set; }
        // The type and message of the last ledger write that failed; null after a success.
        public string LastPersistError { get; private set; }

        public RewardOfferCoordinator(RewardOfferContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            _video = context.Video ?? throw new ArgumentException("A rewarded video is required.", nameof(context));
            _consent = context.Consent ?? throw new ArgumentException("A consent gate is required.", nameof(context));
            _ownsSkip = context.OwnsSkip ?? throw new ArgumentException("The skip's ownership is required.", nameof(context));
            _ledger = context.Ledger ?? throw new ArgumentException("A reward ledger is required.", nameof(context));
            if (context.MultiplierPercent < 100)
                throw new ArgumentException("The bonus never takes gold away.", nameof(context));
            if (context.MinimumRunGold < 0)
                throw new ArgumentException("The minimum run gold is 0 or more.", nameof(context));
            _saveLedger = context.SaveLedger;
            _analytics = context.Analytics;
            _sessions = context.Sessions;
            _multiplierPercent = context.MultiplierPercent;
            _minimumRunGold = context.MinimumRunGold;
        }

        // The offer as it stands now, without writing anything.
        public RewardOfferState State
        {
            get
            {
                if (!_open)
                    return RewardOfferState.None;
                if (_showingRunId != null && string.Equals(_showingRunId, RunId, StringComparison.Ordinal))
                    return RewardOfferState.Showing;
                bool resolved = _resolved || _ledger.HasCredited(RunId);
                return RewardOfferRule.Evaluate(_ledger.CompletedRuns, _resultSaved(), RunGold, resolved, _ownsSkip(),
                    _video.IsReady, _multiplierPercent, _minimumRunGold);
            }
        }

        // The offer as it stands now; the first time it can be taken, reward_offer is written. The view calls it whenever
        // it draws the button (and on Changed); the wiring calls it once the result's save lands.
        public RewardOfferState Refresh()
        {
            RewardOfferState state = State;
            if (!_offerLogged && RewardOfferRule.IsOffered(state))
            {
                _offerLogged = true;
                _analytics?.EmitForRun(RunId, MonetisationTelemetry.RewardOfferEvent,
                    MonetisationTelemetry.RewardOffer(state, RunGold, BonusGold, _multiplierPercent, _ledger.CompletedRuns));
            }
            return state;
        }

        // Asks for a video ahead of need (the wiring: once consent is gathered, and as each run starts): never without
        // consent, never for an owner of the skip, and not while one is ready, loading or on screen. True when it asked.
        public bool Prefetch()
        {
            if (IsLoading || _showingRunId != null || _video.IsReady || !_consent.CanRequestAds || _ownsSkip())
                return false;
            IsLoading = true;
            Counters.RecordRequest();
            _video.Load(OnLoaded);
            return true;
        }

        // The result screen opens for a completed run: counts it (once per run id), and offers if the rule allows. runGold
        // is what the run banked (RunState.GoldBanked); resultSaved is true once the run's result is on disk; creditGold
        // takes a credited bonus (the scene's PlayerProfile.Deposit).
        public RewardOfferState Open(string runId, int runGold, Func<bool> resultSaved, Action<int> creditGold)
        {
            if (string.IsNullOrEmpty(runId))
                throw new ArgumentException("A run id is required.", nameof(runId));
            if (runGold < 0)
                throw new ArgumentOutOfRangeException(nameof(runGold));
            if (resultSaved == null)
                throw new ArgumentNullException(nameof(resultSaved));
            if (creditGold == null)
                throw new ArgumentNullException(nameof(creditGold));
            if (_open && string.Equals(runId, RunId, StringComparison.Ordinal))
                return Refresh();

            Close();
            RunId = runId;
            RunGold = runGold;
            BonusGold = RewardOfferRule.BonusGold(runGold, _multiplierPercent);
            _resultSaved = resultSaved;
            _creditGold = creditGold;
            _open = true;
            _resolved = false;
            _offerLogged = false;
            _askedWithoutAd = false;
            _failedShows = 0;
            if (_ledger.RecordCompletedRun(runId))
                SaveLedger();
            // A screen that could offer but has no video asks for one now.
            if (State == RewardOfferState.NoAd)
                Prefetch();
            RewardOfferState state = Refresh();
            Changed?.Invoke();
            return state;
        }

        // The player tapped the offer's button.
        public RewardTakeResult Take()
        {
            RewardOfferState state = Refresh();
            switch (state)
            {
                case RewardOfferState.Skip:
                    CreditAndResolve(RunId, BonusGold, _creditGold, RewardResultKind.SkipOwned);
                    return RewardTakeResult.Credited;
                case RewardOfferState.Video:
                    Counters.RecordAttempt(true);
                    _showingRunId = RunId;
                    _showingBonus = BonusGold;
                    _showingCredit = _creditGold;
                    // The offer's lines reach the file before the app leaves for the video.
                    _analytics?.Flush();
                    _sessions?.VideoOpened();
                    Changed?.Invoke();
                    _video.Show(OnShown);
                    return RewardTakeResult.VideoShown;
                case RewardOfferState.NoAd:
                    // One unanswered ask per result screen: tapping again while nothing is ready is not more demand.
                    if (!_askedWithoutAd)
                    {
                        _askedWithoutAd = true;
                        Counters.RecordAttempt(false);
                    }
                    Prefetch();
                    return RewardTakeResult.NoAd;
                default:
                    return RewardTakeResult.NotOffered;
            }
        }

        // The player left the result screen. An offer still open ends as failed when its video failed once and the retry
        // was not taken, as no_ad when the player asked and no video came, else as declined. A video still on screen keeps
        // its run: its result still credits that run.
        public void Close()
        {
            if (!_open)
                return;
            bool showing = _showingRunId != null && string.Equals(_showingRunId, RunId, StringComparison.Ordinal);
            if (!_resolved && !showing && !_ledger.HasCredited(RunId))
            {
                if (_failedShows > 0)
                    Resolve(RunId, RewardResultKind.Failed, false, 0);
                else if (_askedWithoutAd)
                    Resolve(RunId, RewardResultKind.NoAd, false, 0);
                else if (_offerLogged)
                    Resolve(RunId, RewardResultKind.Declined, false, 0);
            }
            _open = false;
            _resultSaved = null;
            _creditGold = null;
            RunId = null;
            RunGold = 0;
            BonusGold = 0;
            Changed?.Invoke();
        }

        private void OnLoaded(bool matched)
        {
            // A second report of the same load.
            if (!IsLoading)
                return;
            IsLoading = false;
            if (matched)
                Counters.RecordMatch();
            if (!_open)
                return;
            Refresh();
            Changed?.Invoke();
        }

        private void OnShown(RewardedVideoResult result)
        {
            // A second report of the same video.
            string runId = _showingRunId;
            if (runId == null)
                return;
            int bonus = _showingBonus;
            Action<int> credit = _showingCredit;
            _showingRunId = null;
            _showingBonus = 0;
            _showingCredit = null;
            _sessions?.VideoClosed();
            Counters.RecordShow(result);
            if (result == RewardedVideoResult.Completed)
            {
                CreditAndResolve(runId, bonus, credit, RewardResultKind.Watched);
            }
            else if (result == RewardedVideoResult.Failed && _failedShows < RetriesAfterAFailure &&
                     _open && string.Equals(runId, RunId, StringComparison.Ordinal))
            {
                // No fault of the player's: the screen keeps its offer for one retry, with the next video asked for below.
                _failedShows++;
                LastResult = RewardResultKind.Failed;
                LastCredited = false;
                Changed?.Invoke();
            }
            else
            {
                Resolve(runId, result == RewardedVideoResult.Skipped ? RewardResultKind.Skipped : RewardResultKind.Failed, false, 0);
            }
            // The next video: the retry's, or the next run's.
            Prefetch();
        }

        private void CreditAndResolve(string runId, int bonus, Action<int> credit, RewardResultKind kind)
        {
            // State never offers a credited run; this keeps a credit from ever doubling all the same.
            if (!_ledger.TryCredit(runId, bonus))
            {
                Resolve(runId, kind, false, 0);
                return;
            }
            SaveLedger();
            credit(bonus);
            Resolve(runId, kind, true, bonus);
        }

        private void Resolve(string runId, RewardResultKind kind, bool credited, int creditedGold)
        {
            if (_open && string.Equals(runId, RunId, StringComparison.Ordinal))
                _resolved = true;
            LastResult = kind;
            LastCredited = credited;
            if (_analytics != null)
            {
                _analytics.EmitForRun(runId, MonetisationTelemetry.RewardResultEvent,
                    MonetisationTelemetry.RewardResult(kind, credited, creditedGold));
                _analytics.EmitForRun(null, MonetisationTelemetry.AdCountersEvent, MonetisationTelemetry.AdCounters(Counters));
            }
            Changed?.Invoke();
        }

        private void SaveLedger()
        {
            if (_saveLedger == null)
                return;
            try
            {
                _saveLedger(_ledger);
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
