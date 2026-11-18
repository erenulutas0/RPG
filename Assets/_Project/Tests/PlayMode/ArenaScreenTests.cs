using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cryptforge.Analytics;
using Cryptforge.Arena;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Save;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // P0's arena screen in the game (docs/55 section 11): opened over the map choice, as its button will open it - the build
    // chosen and kept, a training fight fought, saved and sent as arena_fight, played from its log at x1 and x2 and skipped
    // to its result; a changed build kept as a past hero; the open arena's three offers fought; and every label of every
    // page fitting in Turkish.
    public sealed class ArenaScreenTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private static readonly Regex Key = new Regex(@"^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+$", RegexOptions.CultureInvariant);
        private CombatSetup _setup;
        private Language? _language;

        [UnitySetUp]
        public IEnumerator Load()
        {
            _language = Loc.Override;
            TestProfile.Begin();
            // The arena opens over the map choice, which holds the run (its button's home, docs/55 section 11): the foundry
            // as a player's build loads it, no development file deciding the start.
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.Delete(DevelopmentStart.MapChoicePath);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(_setup.IsChoosingMap, Is.True);
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Loc.Override = _language;
            if (ArenaScreen.Current != null)
                ArenaScreen.Current.Close();
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Arena Screen Cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(ArenaScreen.InputDelay + .1f);
        }

        private static string TelemetryText()
        {
            var text = new StringBuilder();
            string folder = TelemetryLocation.Resolve();
            if (!Directory.Exists(folder))
                return string.Empty;
            foreach (string path in Directory.GetFiles(folder, "*.jsonl"))
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream))
                    text.Append(reader.ReadToEnd());
            }
            return text.ToString();
        }

        [UnityTest]
        public IEnumerator TheArenaOpensOnTheBuildFightsSavesAndSkipsToTheResult()
        {
            bool closed = false;
            ArenaScreen screen = ArenaScreen.Open(_setup, () => closed = true);
            yield return null;
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Build), "A fresh arena opens on the build.");
            Assert.That(screen.HeroId, Is.EqualTo(ArenaRules.Vanguard));
            Assert.That(screen.WeaponId, Is.EqualTo(ArenaRules.Sword));
            Assert.That(screen.Lines.Count, Is.EqualTo(ArenaRules.Current.RankedLines));
            Assert.That(screen.CanContinue, Is.True);

            // A fourth line replaces the earliest of the three; a line put back leaves two and Continue waits.
            screen.ToggleLine(ArenaStat.AttackSpeed);
            Assert.That(screen.Lines, Is.EqualTo(new[] { ArenaStat.MaxHealth, ArenaStat.Armour, ArenaStat.AttackSpeed }));
            screen.ToggleLine(ArenaStat.Armour);
            Assert.That(screen.Lines.Count, Is.EqualTo(2));
            Assert.That(screen.CanContinue, Is.False);
            screen.ToggleLine(ArenaStat.Damage);
            Assert.That(screen.CanContinue, Is.True);

            yield return Settle();
            screen.ContinueButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Ladder));
            Assert.That(screen.SaveData.rankedBuild, Is.Not.Null);
            Assert.That(File.Exists(Path.Combine(ProfileLocation.Resolve(), ArenaSaveStore.FileName)), Is.True, "The build is saved.");
            Assert.That(screen.Opponents.Count, Is.EqualTo(ArenaLadder.Offered));
            Assert.That(screen.CardText(0), Does.Contain(ArenaText.TrainingRival), "Generated rivals are labelled as training rivals.");
            Assert.That(screen.StatusText, Does.Contain("Rung 12 of 30").And.Contain("Training rating 1,000"));

            yield return Settle();
            screen.FightButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Playback));
            ArenaFightOutcome outcome = screen.LastOutcome;
            Assert.That(outcome, Is.Not.Null);
            Assert.That(outcome.TrainingRival, Is.True);
            Assert.That(screen.LastLog.Count, Is.EqualTo(outcome.Match.Events));
            Assert.That(screen.LastLog.Fingerprint(), Is.EqualTo(outcome.Match.Fingerprint), "The log played is the fight's own.");
            yield return null;
            Assert.That(screen.Playback.BannerText, Is.EqualTo("Round 1"));
            Assert.That(screen.Playback.HealthText(0), Does.EndWith(" / " + DuelNumbers.Points(outcome.Match.Rounds[0].MaxHealthA)));

            screen.Playback.SkipButton.onClick.Invoke();
            yield return null;
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Result));
            Assert.That(screen.ResultTitle, Is.EqualTo(ArenaText.Outcome(outcome.Match.Winner)));
            Assert.That(screen.RatingText, Does.StartWith("Training rating 1,000 -> "));
            if (outcome.Match.Winner >= 0)
                Assert.That(screen.WhyText.Split('\n').Length, Is.EqualTo(outcome.Match.Why.Length), "The summary's lines, localised.");

            ArenaSaveData saved = new ArenaSaveStore(ProfileLocation.Resolve()).Load(1);
            Assert.That(saved.fights, Is.EqualTo(1));
            Assert.That(saved.ranked.history.Count, Is.EqualTo(1));
            Assert.That(saved.ranked.history[0].fingerprint, Is.EqualTo(ArenaHash.Hex(outcome.Match.Fingerprint)));
            Assert.That(saved.ranked.rating, Is.EqualTo(outcome.RatingAfter));
            string telemetry = TelemetryText();
            Assert.That(telemetry, Does.Contain("\"event\":\"arena_fight\""));
            Assert.That(telemetry, Does.Contain("\"fingerprint\":\"" + ArenaHash.Hex(outcome.Match.Fingerprint) + "\""));

            yield return Settle();
            screen.NextButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Ladder));
            screen.Close();
            yield return null;
            Assert.That(closed, Is.True);
            Assert.That(ArenaScreen.Current == null, Is.True);
            Assert.That(_setup.IsChoosingMap, Is.True, "The map choice still holds the run.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ThePlaybackRunsAtX1AndX2AndDrawsTheDuel()
        {
            ArenaScreen screen = ArenaScreen.Open(_setup);
            yield return null;
            screen.ConfirmBuild();
            screen.FightSelected();
            DuelPlaybackView playback = screen.Playback;
            Assert.That(playback.Clock.Speed, Is.EqualTo(DuelPlaybackClock.Fast), "The arena plays at x2 first.");
            yield return new WaitForSecondsRealtime(.5f);
            double atX1 = playback.Clock.Ticks; // at x2, the default
            Assert.That(atX1, Is.GreaterThan(0.0), "The playback runs on unscaled time.");
            playback.SpeedButton.onClick.Invoke();
            Assert.That(playback.Clock.Speed, Is.EqualTo(DuelPlaybackClock.Normal));

            bool sawNumbers = false;
            bool sawFight = false;
            float until = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < until && screen.Shown == ArenaScreen.Page.Playback)
            {
                sawFight |= playback.Moment.Phase == DuelPhase.Fight;
                sawNumbers |= playback.NumbersShown > 0;
                if (sawNumbers && playback.HealthFill(0) < 1f && playback.HealthFill(1) < 1f)
                    break;
                yield return null;
            }
            Assert.That(sawFight, Is.True);
            Assert.That(sawNumbers, Is.True, "Blows show their numbers.");
            Assert.That(playback.HealthFill(0) < 1f || playback.HealthFill(1) < 1f, Is.True, "The bars follow the blows.");
            Assert.That(playback.Figure(0).BodySprite, Is.Not.Null, "The hero is drawn with the run's art.");
            Assert.That(playback.RoundText, Does.StartWith("Round "));

            screen.SkipPlayback();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Result));
            screen.WatchAgain();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Playback));
            Assert.That(playback.Clock.Ticks, Is.LessThan(atX1 + 1), "Watch again plays the same log from the start.");
            Assert.That(playback.Clock.Speed, Is.EqualTo(DuelPlaybackClock.Normal), "The speed chosen stays.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AChangedBuildIsKeptAsAPastHeroAndTheOpenArenaOffersThree()
        {
            ArenaScreen screen = ArenaScreen.Open(_setup);
            yield return null;
            screen.ConfirmBuild();
            screen.FightSelected();
            screen.SkipPlayback();
            screen.Show(ArenaScreen.Page.Build);
            screen.PickPolicy(ArenaPolicy.Bold);
            screen.ConfirmBuild();
            ArenaSaveData save = screen.SaveData;
            Assert.That(save.pastSnapshots.Count, Is.EqualTo(1), "The build that fought is kept as a past hero.");
            Assert.That(save.pastSnapshots[0].policy, Is.EqualTo("careful"));
            Assert.That(save.ranked.rivals.Exists(r => r.pastSelf), Is.True, "It stands on the ladder.");
            Assert.That(save.rankedBuild.policy, Is.EqualTo("bold"));
            screen.Show(ArenaScreen.Page.Build);
            screen.ConfirmBuild();
            Assert.That(save.pastSnapshots.Count, Is.EqualTo(1), "An unchanged build keeps nothing.");

            screen.ShowMode(ArenaMode.Open);
            Assert.That(screen.Offers.Count, Is.EqualTo(3));
            Assert.That(screen.Offers[0].Kind, Is.EqualTo(ArenaOfferKind.Below));
            Assert.That(screen.Offers[1].Kind, Is.EqualTo(ArenaOfferKind.Level));
            Assert.That(screen.Offers[2].Kind, Is.EqualTo(ArenaOfferKind.Above));
            Assert.That(screen.CardText(1), Does.Contain("Level with you").And.Contain("Chance to win"));
            Assert.That(screen.Selected, Is.EqualTo(1), "Level is offered first.");
            screen.FightSelected();
            Assert.That(screen.LastOutcome.Offer, Is.Not.Null);
            screen.SkipPlayback();
            Assert.That(screen.RatingText, Does.StartWith("Open rating"));
            ArenaSaveData saved = new ArenaSaveStore(ProfileLocation.Resolve()).Load(1);
            Assert.That(saved.open.history.Count, Is.EqualTo(1));
            Assert.That(saved.fights, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryPageFitsAndSpeaksTurkish()
        {
            Loc.Override = Language.Turkish;
            ArenaScreen screen = ArenaScreen.Open(_setup);
            yield return null;
            var problems = new List<string>();
            Check(screen.Panel.transform, "Build", problems);
            screen.PickHero(ArenaRules.Ilya);
            Check(screen.Panel.transform, "Build (Ilya)", problems);
            screen.ConfirmBuild();
            Check(screen.Panel.transform, "Training", problems);
            screen.ShowMode(ArenaMode.Open);
            Check(screen.Panel.transform, "Open", problems);
            screen.ShowMode(ArenaMode.Ranked);
            screen.FightSelected();
            screen.Playback.SpeedButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(2.5f);
            Check(screen.Panel.transform, "Playback", problems);
            screen.SkipPlayback();
            Check(screen.Panel.transform, "Result", problems);
            Assert.That(screen.RatingText, Does.StartWith("Antrenman puanı"));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }

        // Every active label under root fits its rect at its smallest size (TurkishScreensTests' check).
        private static void Check(Transform root, string where, List<string> problems)
        {
            Canvas.ForceUpdateCanvases();
            foreach (Text label in root.GetComponentsInChildren<Text>())
            {
                if (string.IsNullOrEmpty(label.text) || label.horizontalOverflow == HorizontalWrapMode.Overflow)
                    continue;
                Rect rect = label.rectTransform.rect;
                TextGenerationSettings settings = label.GetGenerationSettings(new Vector2(rect.width, 0f));
                settings.resizeTextForBestFit = false;
                settings.fontSize = label.resizeTextForBestFit ? label.resizeTextMinSize : label.fontSize;
                float height = label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
                if (height > rect.height + 1f)
                    problems.Add($"{where}/{label.name} needs {height:0} of {rect.height:0} units at size {settings.fontSize}: \"{label.text}\"");
            }
        }

        private static void AssertNoKeyShows()
        {
            var shown = new StringBuilder();
            foreach (Text label in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (Key.IsMatch(label.text ?? string.Empty))
                    shown.Append(label.name).Append(": ").Append(label.text).Append('\n');
            }
            Assert.That(shown.ToString(), Is.Empty, "A label shows a key: its row is missing from the table.");
        }
    }
}
