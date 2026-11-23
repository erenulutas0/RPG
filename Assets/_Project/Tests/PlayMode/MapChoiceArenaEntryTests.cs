using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Save;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The arena's entry on the map choice (docs/55 section 11.5): the Arena button at the title band's left, the language
    // toggle's mirror, colliding with nothing and taking taps only after the panel's input delay. A tap opens P0's arena
    // over the panel - on its build page, the arena's save a fresh one in the test's profile folder - and closing it
    // leaves the map choice open, holding the run, its input delay begun again; the Foundry then starts the run as before.
    // In Turkish the button's word comes from the table and fits at its best fit's smallest size, as does the title beside
    // it.
    public sealed class MapChoiceArenaEntryTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private static readonly float[] SafeHeights = { 1760f, 2232f };

        private Language? _language;

        [UnitySetUp]
        public IEnumerator Begin()
        {
            _language = Loc.Override;
            // The arena's save lives beside the profile (ArenaSaveStore(ProfileLocation.Resolve())), so this folder of the
            // test's own holds it, fresh, as in ArenaScreenTests.
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator End()
        {
            Loc.Override = _language;
            if (ArenaScreen.Current != null)
                ArenaScreen.Current.Close();
            Time.timeScale = 1f;
            RunStartRequest.Clear();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Map choice arena entry cleanup " + Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // The foundry as a player's build loads it: no development file decides the start, so the map choice opens.
        private static IEnumerator LoadWithTheMapChoice()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.Delete(DevelopmentStart.MapChoicePath);
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
        }

        private static IEnumerator AfterTheInputDelay()
        {
            float ready = Time.realtimeSinceStartup + MapChoiceView.InputDelay + .2f;
            while (Time.realtimeSinceStartup < ready)
                yield return null;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static Text Title(MapChoiceView view)
        {
            foreach (Text label in view.Panel.GetComponentsInChildren<Text>())
                if (label.name == "Map Choice Title")
                    return label;
            Assert.Fail("The map choice has no title.");
            return null;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheArenaButtonOpensTheArenaOverTheMapChoiceAndClosingItLeavesTheChoiceOpen()
        {
            yield return LoadWithTheMapChoice();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(view, Is.Not.Null, "The map choice opens before the run.");
            Assert.That(ProfileLocation.Resolve(), Is.EqualTo(TestProfile.Folder), "The arena's save is the test's own.");
            Assert.That(File.Exists(Path.Combine(TestProfile.Folder, ArenaSaveStore.FileName)), Is.False, "No arena save yet.");

            // The button: on the panel, in the title's band at its left, the toggle's size, colliding with nothing.
            Button arena = view.ArenaButton;
            Assert.That(arena, Is.Not.Null, "The map choice carries the arena's entry.");
            Assert.That(arena.name, Is.EqualTo(MapChoiceView.ArenaButtonName));
            Assert.That(arena.transform.IsChildOf(view.Panel.transform), Is.True);
            Assert.That(view.ArenaLabelText, Is.EqualTo("Arena"));
            var arenaRect = (RectTransform)arena.transform;
            var toggleRect = (RectTransform)view.LanguageToggle.transform;
            Assert.That(arenaRect.rect.size, Is.EqualTo(toggleRect.rect.size), "The toggle's mirror.");
            Rect arenaArea = WorldRect(arenaRect);
            Rect surface = WorldRect((RectTransform)arena.transform.parent);
            Assert.That(surface.Contains(arenaArea.min) && surface.Contains(arenaArea.max), Is.True, "On the surface.");
            Rect title = WorldRect(Title(view).rectTransform);
            Assert.That(arenaArea.Overlaps(title), Is.False, "Beside the title, not over it.");
            Assert.That(arenaArea.center.x, Is.LessThan(title.xMin), "At the band's left.");
            Assert.That(Mathf.Abs(arenaArea.center.y - title.center.y), Is.LessThan(1f), "In the title's band.");
            Assert.That(Mathf.Abs(title.center.x - surface.center.x), Is.LessThan(1f), "The title centred between the two.");
            foreach (Button other in view.Panel.GetComponentsInChildren<Button>())
                if (other != arena)
                    Assert.That(arenaArea.Overlaps(WorldRect((RectTransform)other.transform)), Is.False, "Clear of " + other.name);

            // Not before the panel's input delay: the scene's first frame can outlast it in a batch run, so it is judged only
            // while it lasts.
            if (Time.unscaledTime < view.InputEnabledAt)
            {
                Assert.That(arena.interactable, Is.False, "Not before the panel's input delay.");
                arena.onClick.Invoke();
                Assert.That(ArenaScreen.Current == null, Is.True, "A tap inside the delay opens nothing.");
            }
            yield return AfterTheInputDelay();
            Assert.That(arena.interactable, Is.True, "The button takes taps after the delay.");

            // A tap opens the arena over the panel: a fresh save opens on the build.
            arena.onClick.Invoke();
            ArenaScreen screen = ArenaScreen.Current;
            Assert.That(screen != null, Is.True, "The arena opens.");
            yield return null;
            Assert.That(screen.IsOpen, Is.True);
            Assert.That(screen.Shown, Is.EqualTo(ArenaScreen.Page.Build), "A fresh arena save opens on the build.");
            Assert.That(screen.SaveData.fights, Is.EqualTo(0));
            Assert.That(screen.SaveData.rankedBuild, Is.Null);
            Assert.That(screen.GetComponent<Canvas>().sortingOrder, Is.EqualTo(ArenaScreen.SortingOrder));
            Assert.That(ArenaScreen.SortingOrder, Is.GreaterThan(view.Panel.GetComponent<Canvas>().sortingOrder), "Above the map choice.");
            Assert.That(view.IsOpen, Is.True, "The map choice stays open underneath.");
            Assert.That(setup.IsChoosingMap, Is.True, "It still holds the run.");
            Assert.That(setup.Encounters.IsFirstEncounterPending, Is.True);

            // Its own Close, after its delay, as a finger would: the map choice stands as it was, its delay begun again.
            yield return new WaitForSecondsRealtime(ArenaScreen.InputDelay + .1f);
            Button close = null;
            foreach (Button button in screen.Panel.GetComponentsInChildren<Button>())
                if (button.name == "Arena Build Title Close")
                    close = button;
            Assert.That(close, Is.Not.Null, "The build page's Close.");
            close.onClick.Invoke();
            Assert.That(screen.IsOpen, Is.False, "Closed.");
            Assert.That(view.IsOpen, Is.True, "Closing the arena leaves the map choice open.");
            Assert.That(view.InputEnabledAt, Is.GreaterThan(Time.unscaledTime), "The panel's input delay begins again.");
            Assert.That(arena.interactable || view.CardButton(0).interactable, Is.False, "Nothing takes a tap meanwhile.");
            arena.onClick.Invoke();
            yield return null;
            Assert.That(ArenaScreen.Current == null, Is.True, "A second tap on the spot opens nothing; the arena's canvas is gone.");
            Assert.That(view.IsOpen, Is.True);
            Assert.That(setup.IsChoosingMap, Is.True, "The map choice still holds the run.");
            Assert.That(setup.Encounters.IsFirstEncounterPending, Is.True);

            // After the delay the panel takes taps again, and the Foundry begins the run as before.
            yield return AfterTheInputDelay();
            Assert.That(arena.interactable, Is.True);
            Assert.That(view.CardButton(0).interactable, Is.True);
            view.CardButton(0).onClick.Invoke();
            Assert.That(view.IsOpen, Is.False, "The Foundry closes the choice.");
            Assert.That(setup.IsChoosingMap, Is.False);
            Assert.That(setup.Encounters.IsFirstEncounterPending, Is.False, "The run begins at once.");
            Assert.That(arena.interactable, Is.False, "A chosen map ends the entry's turn.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator InTurkishTheArenaButtonIsNamedFromTheTableAndFitsBesideTheTitle()
        {
            Loc.Override = Language.Turkish;
            yield return LoadWithTheMapChoice();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(view, Is.Not.Null, "The map choice is open.");
            Assert.That(view.ArenaLabelText, Is.EqualTo(ArenaText.Title));
            Assert.That(view.ArenaLabelText, Is.EqualTo(Loc.Text("arena.title")).And.Not.EqualTo("arena.title"), "A row of the table.");
            Assert.That(view.LanguageToggle.ShownText, Is.EqualTo("Dil: Türkçe"));

            // At a phone's width and a short and a tall safe area: the button's word and the title it narrowed fit at their
            // best fit's smallest size.
            var problems = new List<string>();
            var safe = (RectTransform)view.Panel.transform.Find("Map Choice Safe Area");
            safe.GetComponent<SafeAreaFitter>().enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(.5f, .5f);
            Text title = Title(view);
            foreach (float height in SafeHeights)
            {
                safe.sizeDelta = new Vector2(1080f, height);
                Canvas.ForceUpdateCanvases();
                view.FitNow();
                TurkishScreensTests.Check(view.ArenaButton.transform, "Arena button at " + height, problems);
                TurkishScreensTests.Check(title.transform, "Map choice title at " + height, problems);
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));

            // "Arena" stands on one line at its full size, as the toggle's words do at theirs.
            Text label = view.ArenaButton.GetComponentInChildren<Text>();
            Assert.That(label.resizeTextForBestFit, Is.True);
            TextGenerationSettings settings = label.GetGenerationSettings(Vector2.zero);
            settings.resizeTextForBestFit = false;
            settings.fontSize = label.resizeTextMaxSize;
            float width = label.cachedTextGeneratorForLayout.GetPreferredWidth(label.text, settings) / label.pixelsPerUnit;
            Assert.That(width, Is.LessThanOrEqualTo(label.rectTransform.rect.width), "One line at size " + settings.fontSize);

            TurkishScreensTests.AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }

        // The title's painted plate holds the title's lines (ChoiceCardPainter.FitTitleToLines). "Kahramanını ve / haritanı
        // seç" wraps to two lines, and the plate's rules and crest, laid for one, crossed the second (the 2026-11-17
        // capture, 03-tempered-chosen). In each language, at a short and a tall phone: a wrapped title's crest stands wholly
        // under its lowest glyph, the upper rule over its highest, and the crest above the hero row; a title on one line
        // keeps the set's margins exactly, as before.
        [UnityTest, Timeout(180000)]
        public IEnumerator TheTitlesPlateHoldsItsLinesWithTheCrestClearInBothLanguages()
        {
            Loc.Override = Language.Turkish;
            yield return LoadWithTheMapChoice();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(view, Is.Not.Null, "The map choice is open.");
            Assert.That(view.PaintsCards, Is.True, "The scene's set paints the panel and its title's plate.");
            ChoiceCardArtSet art = Object.FindFirstObjectByType<RunChoiceView>(FindObjectsInactive.Include).CardArt;
            Text title = Title(view);
            RectTransform plate = (RectTransform)title.transform.parent.Find(ChoiceCardPainter.TitlePlateName);
            Assert.That(plate, Is.Not.Null, "The title's plate.");
            Assert.That(plate.GetSiblingIndex(), Is.EqualTo(title.transform.GetSiblingIndex() - 1), "Behind the title.");
            var crest = (RectTransform)plate.Find(ChoiceCardPainter.TitleCrestName);
            Assert.That(crest, Is.Not.Null, "The plate's crest.");
            var safe = (RectTransform)view.Panel.transform.Find("Map Choice Safe Area");
            safe.GetComponent<SafeAreaFitter>().enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(.5f, .5f);

            foreach (Language language in new[] { Language.Turkish, Language.English })
            {
                Loc.Override = language;
                yield return null;
                Assert.That(view.TitleText, Is.EqualTo(MapChoiceView.HeroAndMapTitle));
                foreach (float height in SafeHeights)
                {
                    string where = language + " at " + height + ": \"" + title.text + "\"";
                    safe.sizeDelta = new Vector2(1080f, height);
                    Canvas.ForceUpdateCanvases();
                    view.FitNow();
                    Canvas.ForceUpdateCanvases();
                    int lines = title.cachedTextGenerator.lineCount;
                    Assert.That(view.TitleLines, Is.EqualTo(lines), where + ": the plate was fitted to the lines drawn.");
                    if (language == Language.Turkish)
                        Assert.That(lines, Is.EqualTo(2), where + ": the Turkish title wraps, the case the plate failed.");
                    if (lines < 2)
                    {
                        Vector4 m = art.TitlePlateMargin;
                        RectTransform label = title.rectTransform;
                        Assert.That(plate.offsetMin, Is.EqualTo(label.offsetMin - new Vector2(m.x, m.y)), where + ": one line, the set's margins.");
                        Assert.That(plate.offsetMax, Is.EqualTo(label.offsetMax + new Vector2(m.z, m.w)), where + ": one line, the set's margins.");
                        continue;
                    }
                    GlyphRows(title, out float lowest, out float highest);
                    var corners = new Vector3[4];
                    crest.GetWorldCorners(corners);
                    float crestTop = title.rectTransform.InverseTransformPoint(corners[1]).y;
                    float crestFoot = corners[0].y;
                    plate.GetWorldCorners(corners);
                    float upperRule = title.rectTransform.InverseTransformPoint(corners[1]).y - art.TitleRuleHeight;
                    Assert.That(crestTop, Is.LessThanOrEqualTo(lowest + .5f), where + ": the crest stands wholly under the last line.");
                    Assert.That(upperRule, Is.GreaterThanOrEqualTo(highest - .5f), where + ": the upper rule over the first line.");
                    view.HeroCardButton(0).GetComponent<RectTransform>().GetWorldCorners(corners);
                    Assert.That(crestFoot, Is.GreaterThan(corners[1].y), where + ": the crest above the hero row.");
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        // The lowest and highest edge of the glyphs the title draws, in its own units (the generator's pixels at the
        // canvas's scale); empty quads (spaces, the end) ignored.
        private static void GlyphRows(Text title, out float lowest, out float highest)
        {
            IList<UIVertex> verts = title.cachedTextGenerator.verts;
            lowest = float.MaxValue;
            highest = float.MinValue;
            for (int i = 0; i + 3 < verts.Count; i += 4)
            {
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                for (int v = i; v < i + 4; v++)
                {
                    Vector3 p = verts[v].position;
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                }
                if (maxX - minX < .01f || maxY - minY < .01f)
                    continue;
                lowest = Mathf.Min(lowest, minY / title.pixelsPerUnit);
                highest = Mathf.Max(highest, maxY / title.pixelsPerUnit);
            }
            Assert.That(lowest < highest, Is.True, "The title draws glyphs.");
        }
    }
}
