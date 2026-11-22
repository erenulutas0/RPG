using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cryptforge.Analytics;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Monetisation;
using Cryptforge.Save;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // docs/58 7.4's wiring, in the game: the result screen offers the rewarded video from the second completed run, never
    // below 10 banked gold and never before the run's result is on disk; a watched video credits its bonus once and the
    // screen shows it; an owner of "Skip reward videos" (bought from the link under the offer) is credited without a
    // video; a failed video offers one retry; leaving an open offer records it declined; the pause overlay sells the skip
    // beside the language toggle; and the screen with its offer fits the 1760 and 2232 safe areas in English and Turkish.
    // Each test seeds its own profile folder, where the launch's MonetisationRuntime keeps rewards.json and
    // entitlements.json; a seeded ledger stands for the completed runs before the test's.
    public sealed class RewardOfferFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private static readonly float[] SafeHeights = { 1760f, 2232f };

        private CombatSetup _setup;
        private RunResultView _result;
        private Language? _language;

        [SetUp]
        public void RememberTheLanguage()
        {
            _language = Loc.Override;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            yield return Leave();
            Loc.Override = _language;
        }

        // The scene unloaded and its profile folder gone, the language left as it is.
        private static IEnumerator Leave()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Reward Offer Cleanup " + Guid.NewGuid().ToString("N")));
            if (scene.IsValid() && scene.isLoaded && scene.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheResultScreenOffersFromTheSecondCompletedRun()
        {
            yield return Load(0);
            yield return EndWithGold(40);
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.TooEarly), "No video before the player knows the game.");
            Assert.That(_result.OfferButtonShown || _result.SkipLinkShown || _result.OfferRowShown, Is.False);
            Assert.That(MonetisationRuntime.Offer.Ledger.CompletedRuns, Is.EqualTo(1));
            Assert.That(new RewardLedgerStore(TestProfile.Folder).Load().CompletedRuns, Is.EqualTo(1), "Counted in rewards.json.");

            yield return Restart();
            yield return EndWithGold(40);
            int bonus = RewardOfferRule.BonusGold(_setup.Run.GoldBanked);
            Assert.That(_setup.ResultSaved, Is.True);
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.Video));
            Assert.That(_result.OfferButtonShown, Is.True);
            Assert.That(_result.OfferButtonText, Is.EqualTo($"Watch a video: +{bonus} gold"));
            Assert.That(_result.SkipLinkShown, Is.True);
            Assert.That(_result.SkipLink.ShownText, Is.EqualTo("Skip reward videos"));
            Assert.That(_result.OfferButton.interactable, Is.False, "The result's input delay guards the offer too.");
            Assert.That(MonetisationRuntime.Offer.Ledger.CompletedRuns, Is.EqualTo(2));
            Assert.That(MonetisationRuntime.Video.ShowCalls, Is.Zero, "Nothing is shown before a tap.");
            AssertTheScreenFits("English, Video");
            MonetisationRuntime.Flush();
            Assert.That(Lines("reward_offer", _setup.RunId), Has.Count.EqualTo(1), "reward_offer once, under the run's id.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheOfferNeedsTenBankedGold()
        {
            yield return Load(1);
            yield return EndWithGold(9);
            Assert.That(_setup.Run.GoldBanked, Is.EqualTo(9));
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.NoGold), "+5 at most below 10: not worth a video.");
            Assert.That(_result.OfferButtonShown || _result.SkipLinkShown || _result.OfferRowShown, Is.False);

            yield return Restart();
            yield return EndWithGold(10);
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.Video));
            Assert.That(_result.OfferButtonText, Is.EqualTo("Watch a video: +5 gold"));
            LogAssert.NoUnexpectedReceived();
        }

        // docs/58 3.1: a result that could not be written offers no video. A folder where the profile's temp file goes makes
        // every save fail, the bank's and the result's alike; each says so in the log.
        [UnityTest, Timeout(120000)]
        public IEnumerator NoVideoIsOfferedWhileTheResultIsNotSaved()
        {
            yield return Load(1);
            Directory.CreateDirectory(Path.Combine(TestProfile.Folder, ProfileStore.TempFileName));
            var errors = new List<string>();
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception)
                    errors.Add(message);
            };
            Application.logMessageReceived += capture;
            // For the rest of this test only: the failed saves' errors are this test's subject, checked below.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                yield return EndWithGold(40);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
            Assert.That(errors, Is.Not.Empty);
            foreach (string error in errors)
                Assert.That(error, Does.StartWith("Saving the profile"), "Only the failed saves are reported.");
            Assert.That(_setup.ResultSaved, Is.False);
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.NotSaved));
            Assert.That(_result.OfferButtonShown || _result.SkipLinkShown || _result.OfferRowShown, Is.False);
            Assert.That(MonetisationRuntime.Offer.State, Is.EqualTo(RewardOfferState.NotSaved));
            Assert.That(MonetisationRuntime.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered), "Nothing to take.");
            Assert.That(MonetisationRuntime.Video.ShowCalls, Is.Zero);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AWatchedVideoCreditsTheBonusOnceAndTheScreenShowsIt()
        {
            yield return Load(1);
            yield return EndWithGold(40);
            int banked = _setup.Run.GoldBanked;
            int bonus = RewardOfferRule.BonusGold(banked);
            int before = _setup.Profile.Gold;
            string runId = _setup.RunId;
            MonetisationRuntime.Video.ReportTwice = true;
            yield return WaitOutTheDelay();
            Tap(_result.OfferButton);
            yield return null;

            Assert.That(MonetisationRuntime.Video.ShowCalls, Is.EqualTo(1));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(before + bonus), "Credited once though the video reported twice.");
            Assert.That(TestProfile.ReadSaved().Gold, Is.EqualTo(before + bonus), "The bonus is saved with the profile.");
            Assert.That(Label("Result Gold Label"), Is.EqualTo($"Gold banked: {banked + bonus}"));
            Assert.That(_result.OfferButtonShown, Is.False, "One offer a run.");
            Assert.That(_result.OfferNoteShown, Is.True);
            Assert.That(_result.OfferNoteText, Is.EqualTo($"+{bonus} gold added"));
            Assert.That(_result.SkipLinkShown, Is.False);
            Assert.That(Label("Forge Hint Label"), Does.Contain((before + bonus).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                "The forge's hint counts the bonus.");
            RewardLedger saved = new RewardLedgerStore(TestProfile.Folder).Load();
            Assert.That(saved.HasCredited(runId), Is.True, "The ledger on disk holds the run.");
            Assert.That((saved.BonusesCredited, saved.BonusGoldCredited), Is.EqualTo((1, (long)bonus)));

            // A second tap, or a second offer for the same run, credits nothing.
            _result.OfferButton.onClick.Invoke();
            Assert.That(MonetisationRuntime.Offer.Take(), Is.EqualTo(RewardTakeResult.NotOffered));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(before + bonus));
            MonetisationRuntime.Flush();
            List<string> results = Lines("reward_result", runId);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0], Does.Contain("\"result\":\"watched\"").And.Contain("\"credited\":true")
                .And.Contain($"\"bonus_gold\":{bonus}"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheSkipBoughtFromTheLinkCreditsWithoutAVideo()
        {
            yield return Load(1);
            yield return EndWithGold(40);
            int bonus = RewardOfferRule.BonusGold(_setup.Run.GoldBanked);
            int before = _setup.Profile.Gold;
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.Video));
            yield return WaitOutTheDelay();
            Tap(_result.SkipLink.Button);
            yield return null;

            Assert.That(MonetisationRuntime.OwnsSkip, Is.True, "The fake store sells it at once.");
            Assert.That(MonetisationRuntime.Store.PurchaseCalls, Is.EqualTo(1));
            Assert.That(MonetisationRuntime.Store.IsAcknowledged(StoreProducts.SkipRewardVideos), Is.True);
            Assert.That(new EntitlementCache(TestProfile.Folder).Load().Owns(StoreProducts.SkipRewardVideos), Is.True,
                "Cached for offline play, apart from the profile.");
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.Skip), "An owner is never shown a video.");
            Assert.That(_result.OfferButtonText, Is.EqualTo($"Skip owned: +{bonus} gold"));
            Assert.That(_result.SkipLinkShown, Is.False, "Not shown to an owner.");

            Tap(_result.OfferButton);
            yield return null;
            Assert.That(MonetisationRuntime.Video.ShowCalls, Is.Zero, "No video for the owner.");
            Assert.That(_setup.Profile.Gold, Is.EqualTo(before + bonus));
            Assert.That(_result.OfferNoteText, Is.EqualTo($"+{bonus} gold added"));
            MonetisationRuntime.Flush();
            Assert.That(Lines("store_purchase", null), Has.Count.EqualTo(1));
            Assert.That(Lines("store_purchase", null)[0], Does.Contain("\"result\":\"purchased\""));
            Assert.That(Lines("reward_result", _setup.RunId)[0], Does.Contain("\"result\":\"skip_owned\""));
            LogAssert.NoUnexpectedReceived();
        }

        // A skip bought in an earlier launch (the cache) is owned from the first frame; the pause overlay names it owned.
        [UnityTest, Timeout(120000)]
        public IEnumerator AnOwnerFromAnEarlierLaunchIsOfferedTheSkipAndThePauseSaysOwned()
        {
            yield return Load(1, ownsSkip: true);
            Assert.That(MonetisationRuntime.OwnsSkip, Is.True, "The fake's account starts as the cache says.");
            Assert.That(_setup.Pause.TryPause(), Is.True);
            yield return new WaitForSecondsRealtime(.5f);
            PauseView pause = Object.FindFirstObjectByType<PauseView>();
            Assert.That(pause.SkipVideos, Is.Not.Null);
            Assert.That(pause.SkipVideos.ShownText, Is.EqualTo("Skip reward videos: owned"));
            Assert.That(pause.SkipVideos.Button.interactable, Is.False);
            _setup.Pause.TryResume();
            yield return null;

            yield return EndWithGold(40);
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.Skip));
            Assert.That(MonetisationRuntime.Video.LoadCalls, Is.Zero, "No video is asked for an owner.");
            int before = _setup.Profile.Gold;
            yield return WaitOutTheDelay();
            Tap(_result.OfferButton);
            Assert.That(_setup.Profile.Gold, Is.EqualTo(before + RewardOfferRule.BonusGold(_setup.Run.GoldBanked)));
            Assert.That(MonetisationRuntime.Video.ShowCalls, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ThePauseOverlaySellsTheSkipBesideTheLanguageToggle()
        {
            yield return Load(1);
            Assert.That(_setup.Pause.TryPause(), Is.True);
            yield return null;
            PauseView pause = Object.FindFirstObjectByType<PauseView>();
            Assert.That(pause.SkipVideos.ShownText, Is.EqualTo("Skip reward videos"));
            Assert.That(pause.SkipVideos.Button.interactable, Is.False, "Not before the overlay's input delay.");
            Rect toggle = Box((RectTransform)pause.LanguageToggle.transform);
            Rect skip = Box((RectTransform)pause.SkipVideos.transform);
            Assert.That(skip.yMin, Is.GreaterThan(toggle.yMax), "Above the toggle.");
            Assert.That(skip.Overlaps(Box(GameObject.Find("Pause Hint Label").GetComponent<RectTransform>())), Is.False,
                "Clear of the hint.");
            yield return new WaitForSecondsRealtime(.5f);
            Tap(pause.SkipVideos.Button);
            Assert.That(MonetisationRuntime.OwnsSkip, Is.True);
            Assert.That(pause.SkipVideos.ShownText, Is.EqualTo("Skip reward videos: owned"));
            Assert.That(pause.SkipVideos.Button.interactable, Is.False);
            Tap(pause.SkipVideos.Button);
            Assert.That(MonetisationRuntime.Store.PurchaseCalls, Is.EqualTo(1), "Bought once.");
            LogAssert.NoUnexpectedReceived();
        }

        // A failure is not the player's: the same screen offers one retry, which can still credit.
        [UnityTest, Timeout(120000)]
        public IEnumerator AFailedVideoOffersOneRetry()
        {
            yield return Load(1);
            yield return EndWithGold(40);
            int bonus = RewardOfferRule.BonusGold(_setup.Run.GoldBanked);
            int before = _setup.Profile.Gold;
            MonetisationRuntime.Video.ScriptShows(RewardedVideoResult.Failed, RewardedVideoResult.Completed);
            yield return WaitOutTheDelay();
            Tap(_result.OfferButton);
            yield return null;
            Assert.That(_setup.Profile.Gold, Is.EqualTo(before), "A failed video credits nothing.");
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.Video), "The next video was asked for at once.");
            Assert.That(_result.OfferButtonShown, Is.True);
            Assert.That(_result.OfferNoteText, Is.EqualTo("The video could not play: try once more"));
            Assert.That(_result.SkipLinkShown, Is.False, "The words take the link's line.");
            AssertTheScreenFits("English, the retry");

            Tap(_result.OfferButton);
            yield return null;
            Assert.That(_setup.Profile.Gold, Is.EqualTo(before + bonus));
            Assert.That(_result.OfferNoteText, Is.EqualTo($"+{bonus} gold added"));
            Assert.That(MonetisationRuntime.Video.ShowCalls, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        // development/ads.txt "failed": every video fails, so the retry fails too and the offer ends plainly.
        [UnityTest, Timeout(120000)]
        public IEnumerator TheDevelopmentSwitchFailsEveryVideoAndASecondFailureEndsTheOffer()
        {
            yield return Load(1, ads: DevelopmentStart.AdsFailed);
            yield return EndWithGold(40);
            int before = _setup.Profile.Gold;
            yield return WaitOutTheDelay();
            Tap(_result.OfferButton);
            Assert.That(_result.OfferNoteText, Is.EqualTo("The video could not play: try once more"));
            Tap(_result.OfferButton);
            yield return null;
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.Resolved));
            Assert.That(_result.OfferButtonShown, Is.False);
            Assert.That(_result.OfferNoteText, Is.EqualTo("The video could not play: no bonus"));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(before));
            LogAssert.NoUnexpectedReceived();
        }

        // development/ads.txt "noad": the button says so plainly, a tap asks once, and the run's gold stands.
        [UnityTest, Timeout(120000)]
        public IEnumerator TheDevelopmentSwitchWithNoVideoShowsNoAdAndATapAsksOnce()
        {
            yield return Load(1, ads: DevelopmentStart.AdsNoAd);
            yield return EndWithGold(40);
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.NoAd));
            Assert.That(_result.OfferButtonText, Is.EqualTo("No video right now"));
            Assert.That(_result.SkipLinkShown, Is.True);
            yield return WaitOutTheDelay();
            Tap(_result.OfferButton);
            Tap(_result.OfferButton);
            Assert.That(MonetisationRuntime.Offer.Counters.Attempts, Is.EqualTo(1), "One unanswered ask a screen.");
            Assert.That(MonetisationRuntime.Offer.Counters.AttemptsReady, Is.Zero);
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.NoAd));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator LeavingAnOpenOfferRecordsItDeclined()
        {
            yield return Load(1);
            yield return EndWithGold(40);
            string runId = _setup.RunId;
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.Video));
            yield return Restart();

            Assert.That(MonetisationRuntime.Offer.LastResult, Is.EqualTo((RewardResultKind?)RewardResultKind.Declined));
            Assert.That(MonetisationRuntime.Offer.LastResultRunId, Is.EqualTo(runId));
            List<string> results = Lines("reward_result", runId);
            Assert.That(results, Has.Count.EqualTo(1), "Written as the screen closed, under the ended run.");
            Assert.That(results[0], Does.Contain("\"result\":\"declined\"").And.Contain("\"credited\":false"));
            Assert.That(Lines("ad_counters", null), Is.Not.Empty);
            Assert.That(MonetisationRuntime.Offer.Counters.Attempts, Is.Zero, "A No is not an attempt.");
            Assert.That(_result.OfferState, Is.EqualTo(RewardOfferState.None), "The new run's screen is not open.");
            LogAssert.NoUnexpectedReceived();
        }

        // The screen with its offer, in Turkish, at the short and the tall safe area: every label fits at its smallest size
        // and the row stands inside the ledger, clear of the gold above it and the divider below. Each state's words once:
        // the video, a failure's retry and a skipped video; the owner's skip and its credit; no video; two failures.
        [UnityTest, Timeout(300000)]
        public IEnumerator TheResultWithItsOfferFitsInTurkish()
        {
            var problems = new List<string>();
            Loc.Override = Language.Turkish;

            yield return Load(1);
            yield return EndWithGold(2000);
            MonetisationRuntime.Video.ScriptShows(RewardedVideoResult.Failed, RewardedVideoResult.Skipped);
            Assert.That(_result.OfferButtonText, Does.StartWith("Video izle"));
            Assert.That(_result.SkipLink.ShownText, Is.EqualTo("Ödüllü videoları atla"));
            Fit("Video", problems);
            yield return WaitOutTheDelay();
            Tap(_result.OfferButton);
            Assert.That(_result.OfferNoteText, Is.EqualTo("Video oynatılamadı: bir kez daha dene"));
            Fit("the retry", problems);
            Tap(_result.OfferButton);
            Assert.That(_result.OfferNoteText, Is.EqualTo("Video erken kapandı: bonus yok"));
            Fit("skipped", problems);
            yield return Leave();

            yield return Load(1, ownsSkip: true);
            yield return EndWithGold(2000);
            Assert.That(_result.OfferButtonText, Does.StartWith("Videosuz al"));
            Fit("Skip", problems);
            yield return WaitOutTheDelay();
            Tap(_result.OfferButton);
            Assert.That(_result.OfferNoteText, Does.EndWith("altın eklendi"));
            Fit("credited", problems);
            yield return Leave();

            yield return Load(1, ads: DevelopmentStart.AdsNoAd);
            yield return EndWithGold(2000);
            Assert.That(_result.OfferButtonText, Is.EqualTo("Şu an video yok"));
            Fit("NoAd", problems);
            yield return Leave();

            yield return Load(1, ads: DevelopmentStart.AdsFailed);
            yield return EndWithGold(2000);
            yield return WaitOutTheDelay();
            Tap(_result.OfferButton);
            Tap(_result.OfferButton);
            Assert.That(_result.OfferNoteText, Is.EqualTo("Video oynatılamadı: bonus yok"));
            Fit("failed twice", problems);

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            TurkishScreensTests.AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }

        private void Fit(string where, List<string> problems)
        {
            var safe = (RectTransform)_result.transform.Find("Result Panel/Safe Area");
            var fitter = safe.GetComponent<SafeAreaFitter>();
            bool fitted = fitter.enabled;
            Vector2 anchorMin = safe.anchorMin, anchorMax = safe.anchorMax, offsetMin = safe.offsetMin, offsetMax = safe.offsetMax;
            fitter.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(.5f, .5f);
            try
            {
                foreach (float height in SafeHeights)
                {
                    safe.sizeDelta = new Vector2(1080f, height);
                    Canvas.ForceUpdateCanvases();
                    TurkishScreensTests.Check(safe, "Result (" + where + ") at " + height, problems);
                    Layout(safe, where + " at " + height, problems);
                }
            }
            finally
            {
                safe.anchorMin = anchorMin;
                safe.anchorMax = anchorMax;
                safe.offsetMin = offsetMin;
                safe.offsetMax = offsetMax;
                fitter.enabled = fitted;
                Canvas.ForceUpdateCanvases();
            }
        }

        private void AssertTheScreenFits(string where)
        {
            var problems = new List<string>();
            Fit(where, problems);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // The ledger inside the safe area, every part of it inside the ledger, and the offer's row clear of its neighbours.
        private static void Layout(RectTransform safe, string where, List<string> problems)
        {
            Rect area = Box(safe);
            Rect surface = Box((RectTransform)safe.Find("Result Surface"));
            if (!Inside(surface, area))
                problems.Add($"{where}: the ledger {surface} leaves the safe area {area}");
            var parts = new List<RectTransform>();
            foreach (string name in new[]
                     {
                         "Result Title", "Cause Label", "Progress Label", "Result Gold Label", "Result Divider", "Build Label",
                         "Forge Hint Label", "Restart Button", "Forge Button", RunResultView.OfferButtonName,
                         RunResultView.OfferNoteName, RunResultView.SkipLinkName
                     })
            {
                Transform part = safe.Find(name);
                if (part == null || !part.gameObject.activeSelf)
                    continue;
                parts.Add((RectTransform)part);
                if (!Inside(Box((RectTransform)part), surface))
                    problems.Add($"{where}: {name} leaves the ledger");
            }
            foreach (RectTransform row in parts)
            {
                if (row.name != RunResultView.OfferButtonName && row.name != RunResultView.OfferNoteName &&
                    row.name != RunResultView.SkipLinkName)
                    continue;
                foreach (RectTransform other in parts)
                {
                    if (other == row)
                        continue;
                    bool rowPart = other.name == RunResultView.OfferButtonName || other.name == RunResultView.OfferNoteName ||
                                   other.name == RunResultView.SkipLinkName;
                    if (Box(row).Overlaps(Box(other)) && !rowPart)
                        problems.Add($"{where}: {row.name} overlaps {other.name}");
                }
            }
            // The row's own parts never lie on each other either.
            for (int i = 0; i < parts.Count; i++)
                for (int j = i + 1; j < parts.Count; j++)
                {
                    bool a = IsRow(parts[i]), b = IsRow(parts[j]);
                    if (a && b && Box(parts[i]).Overlaps(Box(parts[j])))
                        problems.Add($"{where}: {parts[i].name} overlaps {parts[j].name}");
                }
        }

        private static bool IsRow(RectTransform part) => part.name == RunResultView.OfferButtonName ||
                                                         part.name == RunResultView.OfferNoteName ||
                                                         part.name == RunResultView.SkipLinkName;

        private static bool Inside(Rect inner, Rect outer) =>
            inner.xMin >= outer.xMin - .5f && inner.xMax <= outer.xMax + .5f && inner.yMin >= outer.yMin - .5f &&
            inner.yMax <= outer.yMax + .5f;

        private static Rect Box(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // A fresh profile folder with the completed runs before this one in its ledger, the skip in its cache when owned,
        // and development/ads.txt when named; then the gameplay scene, its run started at once (TestProfile).
        private IEnumerator Load(int completedRuns, bool ownsSkip = false, string ads = null)
        {
            TestProfile.Begin();
            if (completedRuns > 0)
            {
                var ledger = new RewardLedger();
                for (int i = 0; i < completedRuns; i++)
                    ledger.RecordCompletedRun("earlier-" + i);
                new RewardLedgerStore(TestProfile.Folder).Save(ledger);
            }
            if (ownsSkip)
            {
                var owned = new EntitlementCacheData();
                owned.Grant(StoreProducts.SkipRewardVideos);
                new EntitlementCache(TestProfile.Folder).Save(owned);
            }
            if (ads != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.AdsPath));
                File.WriteAllText(DevelopmentStart.AdsPath, ads);
            }
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Find();
        }

        private void Find()
        {
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _result = Object.FindFirstObjectByType<RunResultView>();
            Assert.That(_setup, Is.Not.Null);
            Assert.That(_result, Is.Not.Null);
            // Nothing in the arena changes the run's gold while the test holds it.
            foreach (var attack in Object.FindObjectsByType<Cryptforge.Combat.AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
        }

        // The run ends extracted with this much gold, every coin banked.
        private IEnumerator EndWithGold(int gold)
        {
            _setup.Run.AddGold(gold - _setup.Run.Gold);
            _setup.Run.End(RunOutcome.Extracted);
            yield return null;
            Assert.That(_result.IsOpen, Is.True);
            Assert.That(_setup.Run.GoldBanked, Is.EqualTo(gold));
        }

        private static IEnumerator WaitOutTheDelay()
        {
            yield return new WaitForSecondsRealtime(.7f);
        }

        // The result's Restart, as a player taps it once its delay is over; the next run's scene found.
        private IEnumerator Restart()
        {
            yield return WaitOutTheDelay();
            int loads = 0;
            UnityAction<Scene, LoadSceneMode> countLoad = (scene, mode) => loads++;
            SceneManager.sceneLoaded += countLoad;
            CombatSetup previous = _setup;
            try
            {
                Tap(_result.transform.Find("Result Panel/Safe Area/Restart Button").GetComponent<Button>());
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((loads == 0 || previous != null) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= countLoad;
            }
            Assert.That(loads, Is.EqualTo(1));
            Find();
        }

        // The session's lines of one event, under a run id (null: outside any run), read while the file may be held.
        private static List<string> Lines(string name, string runId)
        {
            TelemetryRuntime.SessionFor(TelemetryLocation.Resolve()).Flush();
            var lines = new List<string>();
            string folder = TelemetryLocation.Resolve();
            if (!Directory.Exists(folder))
                return lines;
            string runKey = runId != null ? "\"run\":\"" + runId + "\"" : null;
            foreach (string file in Directory.GetFiles(folder, "*.jsonl"))
            {
                string text;
                using (var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read,
                           FileShare.ReadWrite | FileShare.Delete), Encoding.UTF8))
                    text = reader.ReadToEnd();
                foreach (string line in text.Split('\n'))
                {
                    if (!line.Contains("\"event\":\"" + name + "\""))
                        continue;
                    bool inRun = line.Contains("\"run\":");
                    if (runKey != null ? line.Contains(runKey) : !inRun)
                        lines.Add(line.Trim());
                }
            }
            return lines;
        }

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
