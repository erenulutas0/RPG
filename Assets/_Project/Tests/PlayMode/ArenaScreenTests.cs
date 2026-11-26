using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cryptforge.Analytics;
using Cryptforge.Arena;
using Cryptforge.Art;
using Cryptforge.Content;
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
    // to its result; a double tap that fights nothing unchosen and skips nothing unseen; a changed build kept as a past hero;
    // the open arena's three offers fought; and every label of every page fitting in Turkish.
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

            // Skip waits out the page's input delay as every other page's button does.
            yield return Settle();
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

        // A double tap fights nothing unchosen and skips nothing unseen (the run flow's review, 2026-11-26): Continue (the
        // build) and Next fight (the result) stand where Fight stands, and Skip over Fight's corner. Every page holds its
        // buttons for the map choice's 0.35 s, Skip's too, so the second tap of Continue, Fight, Skip or Next fight lands on
        // nothing; at 0.2 s a second tap after Continue or Next fight fought the default rival and saved the rating change.
        [UnityTest]
        public IEnumerator ADoubleTapFightsNothingUnchosenAndSkipsNothingUnseen()
        {
            Assert.That(ArenaScreen.InputDelay, Is.EqualTo(MapChoiceView.InputDelay), "The map choice's delay.");
            ArenaScreen screen = ArenaScreen.Open(_setup);
            yield return null;
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Build));
            yield return Settle();
            screen.ContinueButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Ladder));
            screen.FightButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Ladder), "The second tap of Continue fights nothing.");
            Assert.That(screen.LastOutcome, Is.Null);
            Assert.That(screen.SaveData.fights, Is.Zero);

            yield return Settle();
            screen.FightButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Playback));
            Assert.That(screen.SaveData.fights, Is.EqualTo(1));
            screen.Playback.SkipButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Playback), "The second tap of Fight skips nothing.");
            Assert.That(screen.Playback.Finished, Is.False);
            yield return Settle();
            screen.Playback.SkipButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Result), "Skip goes to the result after its delay.");
            screen.NextButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Result), "The second tap of Skip moves on to nothing.");

            yield return Settle();
            screen.NextButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Ladder));
            screen.FightButton.onClick.Invoke();
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Ladder), "The second tap of Next fight fights nothing.");
            Assert.That(screen.SaveData.fights, Is.EqualTo(1), "One fight.");
            Assert.That(new ArenaSaveStore(ProfileLocation.Resolve()).Load(1).fights, Is.EqualTo(1), "One fight saved.");
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

        // The duel's figures and ground (the 2026-11-19 capture): the Vanguard is drawn from its own rendered set, the run's
        // Blender Vanguard (VanguardRenderedArtSet through its HeroDefinition), never the scene's painted frames; a blow
        // taken shows the white silhouette over the frame's own colours rather than tinting the navy plate; a Shield Rush
        // plays the set's dash. The lane stands on the Foundry (DuelLaneBackdrop): the lane's first child, behind the floor
        // marks, the figures and every effect, in the scene's Foundry textures, with two braziers, clear of the health bars
        // and taking no touch; the feet stand on its floor.
        [UnityTest]
        public IEnumerator TheDuelDrawsTheRenderedVanguardOnTheFoundrysGround()
        {
            ArenaScreen screen = ArenaScreen.Open(_setup);
            yield return null;
            Assert.That(screen.HeroId, Is.EqualTo(ArenaRules.Vanguard));
            screen.ConfirmBuild();
            screen.FightSelected();
            // The test moves the duel itself, as the arena's capture does: the screen's own clock stops.
            screen.enabled = false;
            DuelPlaybackView playback = screen.Playback;
            HeroDefinition vanguard = null;
            foreach (HeroDefinition hero in _setup.Heroes)
                if (hero != null && hero.Id == ArenaRules.Vanguard)
                    vanguard = hero;
            Assert.That(vanguard, Is.Not.Null);
            Assert.That(vanguard.RenderedArt != null && vanguard.RenderedArt.IsValid, Is.True, "The Vanguard carries its rendered set.");

            playback.Restart();
            DuelFigure figure = playback.Figure(0);
            Assert.That(figure.RenderedArt, Is.SameAs(vanguard.RenderedArt), "The duel draws the Vanguard's own rendered set.");
            Assert.That(figure.RenderedArt.name, Is.EqualTo("VanguardRenderedArtSet"));
            Assert.That(figure.PaintedArt, Is.Null, "Not the scene's painted frames.");
            Assert.That((figure.Motion, figure.Facing), Is.EqualTo((RenderedMotion.Idle, RenderedFacing.East)), "Standing, facing the rival.");
            Assert.That(figure.BodySprite, Is.SameAs(figure.RenderedArt.Frame(RenderedMotion.Idle, RenderedFacing.East, 0)));

            // A blow on the Vanguard: the silhouette over its frame, the frame's colours its own.
            DuelLog log = screen.LastLog;
            DuelTimeline timeline = playback.State.Timeline;
            DuelEvent? blow = null;
            for (int i = 0; i < log.Count && blow == null; i++)
                if (log[i].Round == 0 && log[i].Side == 1 && log[i].Value > 0 &&
                    (log[i].Kind == DuelEventKind.Strike || log[i].Kind == DuelEventKind.Hit))
                    blow = log[i];
            Assert.That(blow.HasValue, Is.True, "The rival lands a blow in round 1.");
            playback.Seek(timeline.RoundStart(0) + DuelTimeline.IntroTicks + blow.Value.Tick + 2);
            Assert.That(playback.State.Flashing(0), Is.True);
            if (playback.State.Side(0).Health > 0)
            {
                var flash = Resources.Load<Shader>("RenderedCharacterFlash");
                bool silhouette = flash != null && flash.isSupported;
                Assert.That(figure.FlashShown, Is.EqualTo(silhouette), "The run's flash: the frame's silhouette in white.");
                if (silhouette)
                    Assert.That(figure.BodyColour, Is.EqualTo(Color.white), "The navy plate is not tinted.");
            }

            // The Shield Rush: through the log where the Vanguard rushed (unless another of its skills or the round's end
            // falls in the same two ticks), and on a fresh figure either way.
            DuelEvent? rush = null;
            for (int i = 0; i < log.Count && rush == null; i++)
                if (log[i].Kind == DuelEventKind.Skill && log[i].Side == 0 && log[i].Effect == ArenaEffect.ShieldRush)
                    rush = log[i];
            if (rush.HasValue)
            {
                bool alone = true;
                for (int i = 0; i < log.Count; i++)
                {
                    DuelEvent e = log[i];
                    if (e.Round == rush.Value.Round && e.Tick >= rush.Value.Tick && e.Tick <= rush.Value.Tick + 2 &&
                        (e.Kind == DuelEventKind.RoundEnd || (e.Kind == DuelEventKind.Skill && e.Side == 0 && e.Effect != ArenaEffect.ShieldRush)))
                        alone = false;
                }
                playback.Seek(timeline.RoundStart(rush.Value.Round) + DuelTimeline.IntroTicks + rush.Value.Tick + 2);
                if (alone)
                    Assert.That(playback.Figure(0).Motion, Is.EqualTo(RenderedMotion.Dash), "The log's Shield Rush plays the dash.");
            }
            playback.Restart();
            playback.Figure(0).Rush();
            Assert.That(playback.Figure(0).Motion, Is.EqualTo(RenderedMotion.Dash), "A Shield Rush plays the set's dash.");

            // The ground.
            DuelLaneBackdrop backdrop = playback.Backdrop;
            Assert.That(backdrop, Is.Not.Null);
            Assert.That(backdrop.Root.parent, Is.SameAs(playback.Lane));
            Assert.That(backdrop.Root.GetSiblingIndex(), Is.EqualTo(0), "The lane's first child: behind everything in it.");
            Assert.That(backdrop.UsesFoundryArt, Is.True, "Drawn in the scene's Foundry textures.");
            Assert.That(backdrop.Braziers, Is.EqualTo(2));
            for (int side = 0; side < 2; side++)
            {
                Assert.That(playback.Figure(side).Root.parent, Is.SameAs(playback.Lane));
                Assert.That(playback.Figure(side).Root.GetSiblingIndex(), Is.GreaterThan(backdrop.Root.GetSiblingIndex()), "A figure stands in front of the ground.");
                Assert.That(-playback.MarkCentre(side).y, Is.InRange(DuelLaneBackdrop.FloorBack, DuelLaneBackdrop.FloorFront), "The feet on the floor.");
            }
            Graphic[] parts = backdrop.Root.GetComponentsInChildren<Graphic>(true);
            Assert.That(parts.Length, Is.GreaterThan(10));
            Canvas.ForceUpdateCanvases();
            var bars = new List<Rect>();
            foreach (Image image in screen.Panel.GetComponentsInChildren<Image>(true))
                if (image.name == "Arena Bar 0" || image.name == "Arena Bar 1")
                    bars.Add(WorldRect(image.rectTransform));
            Assert.That(bars.Count, Is.EqualTo(2));
            foreach (Graphic part in parts)
            {
                Assert.That(part.raycastTarget, Is.False, part.name);
                Rect area = WorldRect(part.rectTransform);
                foreach (Rect bar in bars)
                    Assert.That(area.Overlaps(bar), Is.False, part.name + " stays clear of the health bars.");
            }
            LogAssert.NoUnexpectedReceived();
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, Mathf.Min(corners[0].y, corners[1].y), corners[2].x, Mathf.Max(corners[0].y, corners[1].y));
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
