using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Cryptforge.Analytics;
using Cryptforge.Monetisation;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/58's result-screen offer: x1.5 rounded up to whole gold, from the second completed run, only with a ready video
    // or the skip, never before the result is saved, credited once per run id with the ledger written first, and nothing
    // for a skipped or failed video. The counters of 5a leave the skip's owners out.
    public sealed class RewardOfferTests
    {
        // One launch's offer over the fakes: the profile's gold, the ledger's writes and the gold's credits in the order
        // they happened, and the telemetry as recorded lines.
        private sealed class Rig
        {
            public readonly FakeRewardedVideo Video = new FakeRewardedVideo();
            public readonly FakeAdConsent Consent = new FakeAdConsent(true);
            public readonly RecordingAnalytics Analytics = new RecordingAnalytics();
            public readonly SessionClock Sessions = new SessionClock();
            public readonly List<string> Order = new List<string>();
            public readonly RewardLedger Ledger;
            public readonly RewardOfferCoordinator Offer;
            public bool OwnsSkip;
            public bool Saved = true;
            public int SavedChecks;
            public int Gold;
            public Exception LedgerFailure;

            public Rig(RewardLedger ledger = null, Action<RewardLedger> save = null,
                int minimumRunGold = RewardOfferRule.MinimumRunGold)
            {
                Ledger = ledger ?? new RewardLedger();
                Offer = new RewardOfferCoordinator(new RewardOfferContext
                {
                    MinimumRunGold = minimumRunGold,
                    Video = Video,
                    Consent = Consent,
                    OwnsSkip = () => OwnsSkip,
                    Ledger = Ledger,
                    SaveLedger = l =>
                    {
                        if (LedgerFailure != null)
                            throw LedgerFailure;
                        save?.Invoke(l);
                        Order.Add("ledger " + l.BonusesCredited.ToString(CultureInfo.InvariantCulture));
                    },
                    Analytics = Analytics,
                    Sessions = Sessions
                });
            }

            public RewardOfferState Open(string runId, int gold) => Offer.Open(runId, gold, () =>
            {
                SavedChecks++;
                return Saved;
            }, Credit);

            private void Credit(int gold)
            {
                Gold += gold;
                Order.Add("gold " + gold.ToString(CultureInfo.InvariantCulture));
            }

            // The first completed run, which is never offered, then the second's screen opened.
            public RewardOfferState OpenSecond(int gold)
            {
                Assert.That(Open("s-1", 30), Is.EqualTo(RewardOfferState.TooEarly));
                Offer.Close();
                return Open("s-2", gold);
            }
        }

        [Test]
        public void TheBonusIsTheRunsGoldTimesOneAndAHalfRoundedUp()
        {
            int[] gold = { 0, 1, 2, 3, 10, 15, 99, 100, 101, 2333 };
            int[] bonus = { 0, 1, 1, 2, 5, 8, 50, 50, 51, 1167 };
            for (int i = 0; i < gold.Length; i++)
                Assert.That(RewardOfferRule.BonusGold(gold[i]), Is.EqualTo(bonus[i]), "Gold " + gold[i].ToString(CultureInfo.InvariantCulture));
            Assert.That(RewardOfferRule.BonusGold(int.MaxValue), Is.EqualTo(1073741824), "No overflow: 1073741823.5 rounded up.");
            Assert.That(RewardOfferRule.BonusGold(10, 125), Is.EqualTo(3), "x1.25 of 10 is 12.5, so +3.");
            Assert.That(RewardOfferRule.BonusGold(10, 100), Is.EqualTo(0));
            Assert.That(RewardOfferRule.BonusGold(7, 200), Is.EqualTo(7));
            Assert.That(RewardOfferRule.MultiplierPercent, Is.EqualTo(150));
            Assert.Throws<ArgumentOutOfRangeException>(() => RewardOfferRule.BonusGold(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => RewardOfferRule.BonusGold(10, 99));
        }

        [Test]
        public void TheRuleOffersFromTheSecondRunWithAReadyVideoOrTheSkipAndNeverUnsaved()
        {
            Assert.That(RewardOfferRule.Evaluate(1, true, 20, false, true, true), Is.EqualTo(RewardOfferState.TooEarly));
            Assert.That(RewardOfferRule.Evaluate(0, true, 20, false, false, true), Is.EqualTo(RewardOfferState.TooEarly));
            Assert.That(RewardOfferRule.Evaluate(2, false, 20, false, true, true), Is.EqualTo(RewardOfferState.NotSaved));
            Assert.That(RewardOfferRule.Evaluate(2, true, 0, false, true, true), Is.EqualTo(RewardOfferState.NoGold));
            Assert.That(RewardOfferRule.Evaluate(2, true, 20, true, true, true), Is.EqualTo(RewardOfferState.Resolved));
            Assert.That(RewardOfferRule.Evaluate(2, true, 20, false, true, true), Is.EqualTo(RewardOfferState.Skip), "The owner paid not to watch.");
            Assert.That(RewardOfferRule.Evaluate(2, true, 20, false, true, false), Is.EqualTo(RewardOfferState.Skip));
            Assert.That(RewardOfferRule.Evaluate(2, true, 20, false, false, true), Is.EqualTo(RewardOfferState.Video));
            Assert.That(RewardOfferRule.Evaluate(9, true, 10, false, false, false), Is.EqualTo(RewardOfferState.NoAd));

            Assert.That(RewardOfferRule.MinimumRunGold, Is.EqualTo(10));
            Assert.That(RewardOfferRule.Evaluate(2, true, 9, false, true, true), Is.EqualTo(RewardOfferState.NoGold),
                "Below 10 banked gold, not even the owner is offered: a +5 bonus is not worth it.");
            Assert.That(RewardOfferRule.Evaluate(2, true, 1, false, false, true, 150, 0), Is.EqualTo(RewardOfferState.Video),
                "With no minimum, 1 gold offers +1.");
            Assert.That(RewardOfferRule.Evaluate(2, true, 10, false, false, true, 150, 11), Is.EqualTo(RewardOfferState.NoGold));
            Assert.That(RewardOfferRule.Evaluate(2, true, 10, false, false, true, 100, 0), Is.EqualTo(RewardOfferState.NoGold),
                "x1.0 adds nothing.");

            Assert.That(RewardOfferRule.IsOffered(RewardOfferState.Video) && RewardOfferRule.IsOffered(RewardOfferState.Skip), Is.True);
            Assert.That(RewardOfferRule.IsOffered(RewardOfferState.NoAd), Is.False);
            Assert.That(RewardOfferRule.IsShown(RewardOfferState.NoAd), Is.True, "The button says plainly that no video is ready.");
            Assert.That(RewardOfferRule.IsShown(RewardOfferState.TooEarly) || RewardOfferRule.IsShown(RewardOfferState.Showing), Is.False);
        }

        [Test]
        public void TheFirstCompletedRunIsNeverOfferedAndTheSecondIs()
        {
            var rig = new Rig();
            Assert.That(rig.Open("s-1", 30), Is.EqualTo(RewardOfferState.TooEarly));
            Assert.That(rig.Video.LoadCalls, Is.EqualTo(0), "No video is asked for a screen that cannot offer.");
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered));
            rig.Offer.Close();
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.None));
            Assert.That(rig.Analytics.Lines, Is.Empty, "Nothing was offered, so nothing is written.");

            Assert.That(rig.Open("s-2", 15), Is.EqualTo(RewardOfferState.Video), "The screen asked for a video and the fake filled it.");
            Assert.That((rig.Ledger.CompletedRuns, rig.Video.LoadCalls, rig.Offer.BonusGold, rig.Offer.RunGold), Is.EqualTo((2, 1, 8, 15)));
            Assert.That(rig.Open("s-2", 15), Is.EqualTo(RewardOfferState.Video), "Opening the same screen again counts nothing.");
            Assert.That(rig.Ledger.CompletedRuns, Is.EqualTo(2));
            Assert.That(rig.Analytics.Lines, Is.EqualTo(new[]
            {
                "reward_offer @s-2 offer=\"video\" run_gold=15 bonus_gold=8 multiplier_pct=150 completed_runs=2"
            }));
        }

        [Test]
        public void NothingIsOfferedOrShownBeforeTheResultIsSaved()
        {
            var rig = new Rig { Saved = false };
            Assert.That(rig.Offer.Prefetch(), Is.True);
            Assert.That(rig.Video.IsReady, Is.True);
            Assert.That(rig.OpenSecond(20), Is.EqualTo(RewardOfferState.NotSaved));
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered));
            Assert.That((rig.Video.ShowCalls, rig.Gold, rig.Offer.Counters.Attempts), Is.EqualTo((0, 0, 0)));
            Assert.That(rig.Analytics.Named("reward_offer"), Is.Empty);

            rig.Saved = true;
            Assert.That(rig.Offer.Refresh(), Is.EqualTo(RewardOfferState.Video), "The save landed: the wiring refreshes the offer.");
            Assert.That(rig.Analytics.Named("reward_offer").Count, Is.EqualTo(1));
            int checks = rig.SavedChecks;
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.VideoShown));
            Assert.That(rig.SavedChecks, Is.GreaterThan(checks), "The tap checked the save again before the video.");
            Assert.That((rig.Video.ShowCalls, rig.Gold), Is.EqualTo((1, 10)));
        }

        [Test]
        public void ACompletedVideoCreditsTheBonusOnceWithTheLedgerWrittenFirst()
        {
            var rig = new Rig();
            rig.Video.ReportTwice = true;
            Assert.That(rig.OpenSecond(15), Is.EqualTo(RewardOfferState.Video));
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.VideoShown));

            Assert.That(rig.Gold, Is.EqualTo(8), "Credited once although the video reported twice.");
            Assert.That(rig.Order, Is.EqualTo(new[] { "ledger 0", "ledger 0", "ledger 1", "gold 8" }),
                "Each completed run is written; the credit's ledger before its gold.");
            Assert.That(rig.Ledger.CreditedRunIds, Is.EqualTo(new[] { "s-2" }));
            Assert.That((rig.Ledger.BonusesCredited, rig.Ledger.BonusGoldCredited), Is.EqualTo((1, 8L)));
            Assert.That((rig.Offer.LastResult, rig.Offer.LastCredited), Is.EqualTo(((RewardResultKind?)RewardResultKind.Watched, true)));
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.Resolved));
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered), "One offer a run.");
            rig.Offer.Close();
            Assert.That(rig.Gold, Is.EqualTo(8));

            AdCounters c = rig.Offer.Counters;
            Assert.That((c.Requests, c.Matches, c.Shows, c.Rewards, c.Attempts, c.AttemptsReady), Is.EqualTo((2, 2, 1, 1, 1, 1)),
                "The screen's request, and the next run's after the show.");
            Assert.That(rig.Analytics.Lines, Is.EqualTo(new[]
            {
                "reward_offer @s-2 offer=\"video\" run_gold=15 bonus_gold=8 multiplier_pct=150 completed_runs=2",
                "reward_result @s-2 result=\"watched\" credited=true bonus_gold=8",
                "ad_counters @- requests=1 matches=1 shows=1 show_failures=0 rewards=1 attempts=1 attempts_ready=1"
            }));
            Assert.That(rig.Analytics.Flushes, Is.EqualTo(1), "The offer's lines were flushed before the app left for the video.");
        }

        [Test]
        public void ASkippedVideoCreditsNothingAndEndsTheOfferAndASecondFailureEndsItToo()
        {
            var rig = new Rig();
            rig.Video.ScriptShows(RewardedVideoResult.Skipped, RewardedVideoResult.Failed, RewardedVideoResult.Failed);
            rig.OpenSecond(40);
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.VideoShown));
            Assert.That((rig.Gold, rig.Ledger.BonusesCredited), Is.EqualTo((0, 0)));
            Assert.That((rig.Offer.LastResult, rig.Offer.LastCredited), Is.EqualTo(((RewardResultKind?)RewardResultKind.Skipped, false)));
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.Resolved), "The player closed it: no second video.");
            rig.Offer.Close();

            Assert.That(rig.Open("s-3", 40), Is.EqualTo(RewardOfferState.Video));
            rig.Offer.Take();
            Assert.That((rig.Gold, rig.Offer.LastResult), Is.EqualTo((0, (RewardResultKind?)RewardResultKind.Failed)));
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.Video), "A failure is not the player's: one retry.");
            Assert.That(rig.Analytics.Named("reward_result").Count, Is.EqualTo(1), "The offer is not over yet.");
            rig.Offer.Take();
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.Resolved), "A second failure ends it.");
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered));
            rig.Offer.Close();

            Assert.That(rig.Analytics.Named("reward_result"), Is.EqualTo(new[]
            {
                "reward_result @s-2 result=\"skipped\" credited=false bonus_gold=0",
                "reward_result @s-3 result=\"failed\" credited=false bonus_gold=0"
            }), "One line a run; closing a resolved offer writes nothing more.");
            AdCounters c = rig.Offer.Counters;
            Assert.That((c.Shows, c.ShowFailures, c.Rewards, c.Attempts, c.AttemptsReady), Is.EqualTo((1, 2, 0, 3, 3)));
            Assert.That(rig.Gold, Is.EqualTo(0));
        }

        [Test]
        public void TheRetryAfterAFailureCanStillCreditAndAnUntakenRetryEndsAsFailed()
        {
            var rig = new Rig();
            rig.Video.ScriptShows(RewardedVideoResult.Failed, RewardedVideoResult.Completed);
            rig.OpenSecond(40);
            rig.Offer.Take();
            Assert.That((rig.Offer.State, rig.Offer.LastResult), Is.EqualTo((RewardOfferState.Video, (RewardResultKind?)RewardResultKind.Failed)),
                "The next video was asked for at once, and the fake filled it.");
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.VideoShown));
            Assert.That((rig.Gold, rig.Offer.LastResult, rig.Offer.LastCredited),
                Is.EqualTo((20, (RewardResultKind?)RewardResultKind.Watched, true)));
            Assert.That(rig.Analytics.Named("reward_result"), Is.EqualTo(new[] { "reward_result @s-2 result=\"watched\" credited=true bonus_gold=20" }));

            // With no video for the retry: the button says so, and leaving ends the offer as failed, not as declined.
            var left = new Rig();
            left.Video.ScriptShows(RewardedVideoResult.Failed).ScriptLoads(true, false);
            left.OpenSecond(40);
            left.Offer.Take();
            Assert.That(left.Offer.State, Is.EqualTo(RewardOfferState.NoAd));
            left.Offer.Close();
            Assert.That(left.Analytics.Named("reward_result"), Is.EqualTo(new[] { "reward_result @s-2 result=\"failed\" credited=false bonus_gold=0" }));
            Assert.That(left.Gold, Is.EqualTo(0));
        }

        [Test]
        public void ARunBelowTheMinimumIsNotOfferedUnlessTheContextLowersIt()
        {
            var rig = new Rig();
            Assert.That(rig.Offer.MinimumRunGold, Is.EqualTo(10));
            Assert.That(rig.OpenSecond(9), Is.EqualTo(RewardOfferState.NoGold));
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered));
            Assert.That(rig.Video.LoadCalls, Is.EqualTo(0), "No video is asked for a screen that cannot offer.");
            rig.Offer.Close();
            Assert.That(rig.Analytics.Lines, Is.Empty);
            Assert.That(rig.Open("s-3", 10), Is.EqualTo(RewardOfferState.Video), "10 banked gold is enough.");

            var none = new Rig(null, null, 0);
            Assert.That(none.OpenSecond(1), Is.EqualTo(RewardOfferState.Video));
            none.Offer.Take();
            Assert.That(none.Gold, Is.EqualTo(1));
        }

        [Test]
        public void TheSkipsOwnerIsCreditedWithoutAVideoAndCountsNowhere()
        {
            var rig = new Rig { OwnsSkip = true };
            Assert.That(rig.Offer.Prefetch(), Is.False, "No video is requested for an owner.");
            Assert.That(rig.OpenSecond(21), Is.EqualTo(RewardOfferState.Skip));
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.Credited));
            Assert.That(rig.Gold, Is.EqualTo(11), "21 x1.5 = 31.5, so +11.");
            Assert.That((rig.Video.LoadCalls, rig.Video.ShowCalls), Is.EqualTo((0, 0)));
            AdCounters c = rig.Offer.Counters;
            Assert.That((c.Requests, c.Matches, c.Shows, c.ShowFailures, c.Rewards, c.Attempts), Is.EqualTo((0, 0, 0, 0, 0, 0)));
            Assert.That(rig.Analytics.Lines, Is.EqualTo(new[]
            {
                "reward_offer @s-2 offer=\"skip\" run_gold=21 bonus_gold=11 multiplier_pct=150 completed_runs=2",
                "reward_result @s-2 result=\"skip_owned\" credited=true bonus_gold=11",
                "ad_counters @- requests=0 matches=0 shows=0 show_failures=0 rewards=0 attempts=0 attempts_ready=0"
            }));

            // Even with a video ready, the owner is offered the skip.
            var ready = new Rig();
            ready.Offer.Prefetch();
            ready.OwnsSkip = true;
            Assert.That(ready.OpenSecond(10), Is.EqualTo(RewardOfferState.Skip));
        }

        [Test]
        public void WithNoVideoReadyTheButtonSaysSoAndOneAskCountsUntilAVideoComes()
        {
            var rig = new Rig();
            rig.Video.ScriptLoads(false, false, false, true);
            Assert.That(rig.OpenSecond(10), Is.EqualTo(RewardOfferState.NoAd));
            Assert.That(rig.Offer.Counters.Requests, Is.EqualTo(1), "The screen asked for a video as it opened.");
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NoAd));
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NoAd));
            Assert.That((rig.Offer.Counters.Attempts, rig.Offer.Counters.AttemptsReady), Is.EqualTo((1, 0)),
                "Tapping again while nothing is ready is the same ask.");
            Assert.That(rig.Analytics.Named("reward_offer"), Is.Empty, "Nothing could be taken yet.");
            Assert.That(rig.Gold, Is.EqualTo(0));

            // The fourth request fills: the offer is now a video, and the player asks again.
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NoAd));
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.Video));
            Assert.That(rig.Analytics.Named("reward_offer").Count, Is.EqualTo(1), "Written when it could first be taken.");
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.VideoShown));
            AdCounters c = rig.Offer.Counters;
            Assert.That((c.Requests, c.Matches, c.Attempts, c.AttemptsReady), Is.EqualTo((5, 2, 2, 1)),
                "Four asks for this screen and the next run's after the show; availability 1 of 2.");
            Assert.That(rig.Gold, Is.EqualTo(5));

            // A screen left after an unanswered ask ends as no_ad.
            var none = new Rig();
            none.Video.FillByDefault = false;
            none.OpenSecond(10);
            none.Offer.Take();
            none.Offer.Close();
            Assert.That(none.Analytics.Named("reward_result"), Is.EqualTo(new[] { "reward_result @s-2 result=\"no_ad\" credited=false bonus_gold=0" }));

            // A screen left without asking, and with nothing to take, writes nothing.
            var quiet = new Rig();
            quiet.Video.FillByDefault = false;
            quiet.OpenSecond(10);
            quiet.Offer.Close();
            Assert.That(quiet.Analytics.Named("reward_result"), Is.Empty);
        }

        [Test]
        public void NoVideoIsRequestedWithoutConsent()
        {
            var rig = new Rig();
            rig.Consent.CanRequestAds = false;
            Assert.That(rig.Offer.Prefetch(), Is.False);
            Assert.That(rig.OpenSecond(10), Is.EqualTo(RewardOfferState.NoAd));
            rig.Offer.Take();
            Assert.That((rig.Video.LoadCalls, rig.Offer.Counters.Requests), Is.EqualTo((0, 0)));

            rig.Consent.Gather(() => rig.Offer.Prefetch());
            Assert.That((rig.Consent.CanRequestAds, rig.Consent.GatherCalls, rig.Video.LoadCalls), Is.EqualTo((true, 1, 1)));
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.Video));
        }

        [Test]
        public void LeavingAnOpenOfferDeclinesIt()
        {
            var rig = new Rig();
            rig.OpenSecond(10);
            rig.Offer.Close();
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.None));
            Assert.That(rig.Analytics.Named("reward_result"), Is.EqualTo(new[] { "reward_result @s-2 result=\"declined\" credited=false bonus_gold=0" }));
            Assert.That((rig.Offer.Counters.Attempts, rig.Offer.LastResult), Is.EqualTo((0, (RewardResultKind?)RewardResultKind.Declined)),
                "A No is not an attempt.");
            Assert.That(rig.Offer.Counters.Shows, Is.EqualTo(0));
            Assert.That(rig.Video.IsReady, Is.True, "The video stays for the next run.");
        }

        [Test]
        public void ABackgroundTripToTheVideoKeepsTheSessionAndAKilledAppCostsAtMostTheBonus()
        {
            var rig = new Rig();
            rig.Sessions.Start(0);
            rig.Video.Deferred = true;
            rig.OpenSecond(30);
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.NoAd), "The screen's request is still loading.");
            rig.Video.CompleteLoad();
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.Video));

            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.VideoShown));
            Assert.That((rig.Offer.State, rig.Offer.IsShowing, rig.Sessions.VideoOpen), Is.EqualTo((RewardOfferState.Showing, true, true)));
            Assert.That(rig.Sessions.Backgrounded(60), Is.False, "The video's activity takes the foreground.");
            Assert.That(rig.Sessions.Foregrounded(95).NewSession, Is.False);
            Assert.That(rig.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered), "Nothing while the video is up.");

            // The app is killed here: the result was saved before the video, the offer's lines flushed, and nothing credited.
            Assert.That(rig.Analytics.Flushes, Is.EqualTo(1));
            Assert.That((rig.Gold, rig.Ledger.BonusesCredited), Is.EqualTo((0, 0)));

            // It is not: the video completes.
            rig.Video.CompleteShow();
            Assert.That((rig.Gold, rig.Sessions.VideoOpen, rig.Sessions.Ordinal), Is.EqualTo((15, false, 1)));
            Assert.That(rig.Video.LoadPending, Is.True, "The next run's video is on its way.");
        }

        [Test]
        public void AVideoThatEndsAfterTheScreenClosedStillCreditsItsOwnRun()
        {
            var rig = new Rig();
            rig.Video.Deferred = true;
            rig.Offer.Prefetch();
            rig.Video.CompleteLoad();
            rig.OpenSecond(12);
            rig.Offer.Take();
            rig.Offer.Close();
            Assert.That(rig.Analytics.Named("reward_result"), Is.Empty, "Not declined: its video is on screen.");

            // Another screen opens, with a profile of its own, before the video ends: the bonus still goes to its own run's.
            int elsewhere = 0;
            Assert.That(rig.Offer.Open("s-3", 20, () => true, g => elsewhere += g), Is.EqualTo(RewardOfferState.NoAd));
            Assert.That(rig.Video.LoadCalls, Is.EqualTo(1), "No second video is asked for while one is on screen.");
            rig.Video.CompleteShow();
            Assert.That(elsewhere, Is.EqualTo(0));
            Assert.That(rig.Offer.State, Is.EqualTo(RewardOfferState.NoAd), "s-3's offer is untouched by s-2's result.");
            Assert.That((rig.Gold, rig.Ledger.CreditedRunIds.Count), Is.EqualTo((6, 1)));
            Assert.That(rig.Analytics.Named("reward_result"), Is.EqualTo(new[] { "reward_result @s-2 result=\"watched\" credited=true bonus_gold=6" }));
        }

        [Test]
        public void TheLedgerOnDiskKeepsARunFromBeingCreditedTwiceAfterARestart()
        {
            string folder = Path.Combine(Path.GetTempPath(), "cryptforge-offer-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new RewardLedgerStore(folder);
                var first = new Rig(store.Load(), l => store.Save(l));
                first.OpenSecond(50);
                first.Offer.Take();
                Assert.That(first.Gold, Is.EqualTo(25));

                // A new launch over the same files: the same run's screen (were it ever to open again) offers nothing.
                RewardLedger loaded = new RewardLedgerStore(folder).Load();
                Assert.That((loaded.CompletedRuns, loaded.LastCompletedRunId, loaded.BonusesCredited), Is.EqualTo((2, "s-2", 1)));
                var second = new Rig(loaded);
                Assert.That(second.Open("s-2", 50), Is.EqualTo(RewardOfferState.Resolved));
                Assert.That(second.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered));
                Assert.That((second.Gold, loaded.CompletedRuns), Is.EqualTo((0, 2)));
                Assert.That(second.Open("t-1", 50), Is.EqualTo(RewardOfferState.Video), "The next launch's runs are offered.");
                Assert.That(loaded.CompletedRuns, Is.EqualTo(3));
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        [Test]
        public void ALedgerThatCannotBeWrittenStillCreditsOnce()
        {
            var rig = new Rig { LedgerFailure = new IOException("disk full") };
            rig.Video.ReportTwice = true;
            rig.OpenSecond(10);
            Assert.That(rig.Offer.LastPersistError, Is.EqualTo("IOException: disk full"));
            rig.Offer.Take();
            Assert.That((rig.Gold, rig.Ledger.BonusesCredited), Is.EqualTo((5, 1)), "The player watched: the bonus stands, once.");
            rig.LedgerFailure = null;
            rig.Offer.Close();
            rig.Open("s-3", 10);
            Assert.That(rig.Offer.LastPersistError, Is.Null, "A later write clears it.");
        }

        [Test]
        public void TheContextIsChecked()
        {
            var video = new FakeRewardedVideo();
            var consent = new FakeAdConsent();
            Func<bool> owns = () => false;
            var ledger = new RewardLedger();
            Action<int> credit = g => { };
            Assert.Throws<ArgumentNullException>(() => new RewardOfferCoordinator(null));
            Assert.Throws<ArgumentException>(() => new RewardOfferCoordinator(new RewardOfferContext
                { Consent = consent, OwnsSkip = owns, Ledger = ledger }));
            Assert.Throws<ArgumentException>(() => new RewardOfferCoordinator(new RewardOfferContext
                { Video = video, Consent = consent, OwnsSkip = owns }));
            Assert.Throws<ArgumentException>(() => new RewardOfferCoordinator(new RewardOfferContext
                { Video = video, Consent = consent, OwnsSkip = owns, Ledger = ledger, MultiplierPercent = 90 }));
            Assert.Throws<ArgumentException>(() => new RewardOfferCoordinator(new RewardOfferContext
                { Video = video, Consent = consent, OwnsSkip = owns, Ledger = ledger, MinimumRunGold = -1 }));
            var offer = new RewardOfferCoordinator(new RewardOfferContext
                { Video = video, Consent = consent, OwnsSkip = owns, Ledger = ledger });
            Assert.Throws<ArgumentException>(() => offer.Open("", 10, () => true, credit));
            Assert.Throws<ArgumentOutOfRangeException>(() => offer.Open("r", -1, () => true, credit));
            Assert.Throws<ArgumentNullException>(() => offer.Open("r", 1, null, credit));
            Assert.Throws<ArgumentNullException>(() => offer.Open("r", 1, () => true, null));
            Assert.That(offer.Open("r", 1, () => true, credit), Is.EqualTo(RewardOfferState.TooEarly),
                "No analytics and no ledger file is fine.");
        }
    }
}
