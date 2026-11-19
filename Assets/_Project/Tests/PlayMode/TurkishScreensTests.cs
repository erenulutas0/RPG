using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // The game in Turkish (docs/60): the scene loaded with the language set, its screens speak Turkish, no label shows a
    // key, every label of the result, forge, pause, level-up and Anvil screens fits at its smallest best-fit size on a
    // short and a tall phone (Turkish runs 20-30 % longer than English), and the pause overlay's toggle switches the
    // language at once and stores the choice. The difficulty tiers' screens - the map choice with its tier row and its own
    // toggle, the HUD, pause and result of a tier run - are TurkishTierScreensTests', with these checks.
    public sealed class TurkishScreensTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // A key as the table writes one: a letter first, so a Turkish number ("12.345") is not taken for one.
        private static readonly Regex Key = new Regex(@"^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+$", RegexOptions.CultureInvariant);
        private CombatSetup _setup;
        private Language? _before;

        [UnitySetUp]
        public IEnumerator Load()
        {
            _before = Loc.Override;
            Loc.Override = Language.Turkish;
            TestProfile.Begin(AnvilFixtures.Profile());
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Loc.Override = _before;
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Turkish Screens Cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator TheGameSpeaksTurkishAndNoLabelShowsAKey()
        {
            yield return null;
            Assert.That(Loc.Table.Count, Is.GreaterThan(300), "The table loaded at boot.");
            Assert.That(_setup.Text.PauseTitle, Is.EqualTo("Duraklatıldı"));
            Assert.That(_setup.Encounters.Floor.DisplayName, Is.Not.EqualTo("The Foundry").And.Not.Empty);
            _setup.Run.GrantBonusUpgrade();
            yield return null;
            Assert.That(_setup.Choices.IsOpen, Is.True);
            Assert.That(_setup.Choices.Current.Cards[0].Name, Is.Not.Empty);
            var view = Object.FindFirstObjectByType<RunChoiceView>();
            Assert.That(view.TitleLabel.text, Is.EqualTo(_setup.Text.UpgradeChoiceTitle));
            Assert.That(view.TitleLabel.text, Does.StartWith("Seviye"));
            AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheLevelUpPauseResultAndForgeFitInTurkish()
        {
            var problems = new List<string>();
            _setup.Run.GrantBonusUpgrade();
            yield return null;
            Check(Object.FindFirstObjectByType<RunChoiceView>().Panel.transform, "Level-up", problems);
            _setup.Upgrades.TrySelect(_setup.Upgrades.CurrentOffer, 0);
            yield return null;

            Assert.That(_setup.Pause.TryPause(), Is.True);
            yield return null;
            var pause = Object.FindFirstObjectByType<PauseView>();
            Assert.That(pause.LanguageToggle, Is.Not.Null);
            Assert.That(pause.LanguageToggle.ShownText, Is.EqualTo("Dil: Türkçe"));
            Check(pause.transform, "Pause", problems);
            _setup.Pause.TryResume();
            yield return null;

            _setup.Run.End(RunOutcome.Defeat);
            Object.FindFirstObjectByType<RelicForgeView>().Open();
            yield return null;
            foreach (string panelName in new[] { "Result Panel", "Forge Panel" })
            {
                var safe = GameObject.Find(panelName).transform.Find("Safe Area").GetComponent<RectTransform>();
                safe.GetComponent<SafeAreaFitter>().enabled = false;
                safe.anchorMin = safe.anchorMax = new Vector2(.5f, .5f);
                foreach (float height in new[] { 1760f, 2232f })
                {
                    safe.sizeDelta = new Vector2(1080f, height);
                    Canvas.ForceUpdateCanvases();
                    Check(safe, panelName + " at " + height, problems);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheAnvilsTabsFitInTurkish()
        {
            _setup.Run.End(RunOutcome.Defeat);
            var forge = Object.FindFirstObjectByType<RelicForgeView>();
            forge.Open();
            yield return null;
            var problems = new List<string>();
            foreach (AnvilView.Tab tab in new[] { AnvilView.Tab.Anvil, AnvilView.Tab.Efsun, AnvilView.Tab.Bag, AnvilView.Tab.Skills })
            {
                forge.Anvil.Open(tab);
                yield return new WaitForSecondsRealtime(AnvilView.InputDelay + .1f);
                Canvas.ForceUpdateCanvases();
                Check(forge.Anvil.Panel.transform, "Anvil " + tab, problems);
            }
            Assert.That(forge.Anvil.StrikeText, Does.Contain("bas"), "The strike button speaks of basma.");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }

        // The toggle on the pause overlay: English at once, the choice stored, Turkish again on the next tap. The stored
        // choice the developer had is put back.
        [UnityTest]
        public IEnumerator ThePauseTogglesTheLanguageAndStoresTheChoice()
        {
            bool had = PlayerPrefs.HasKey(LocalisationRuntime.LanguageKey);
            string stored = PlayerPrefs.GetString(LocalisationRuntime.LanguageKey, string.Empty);
            Language chosen = Loc.Chosen;
            try
            {
                Loc.Override = null;
                Loc.SetLanguage(Language.Turkish);
                Assert.That(_setup.Pause.TryPause(), Is.True);
                yield return new WaitForSecondsRealtime(.5f);
                var pause = Object.FindFirstObjectByType<PauseView>();
                Text title = GameObject.Find("Pause Title").GetComponent<Text>();
                Assert.That(title.text, Is.EqualTo("Duraklatıldı"));

                pause.LanguageToggle.Button.onClick.Invoke();
                Assert.That(Loc.Language, Is.EqualTo(Language.English));
                Assert.That(PlayerPrefs.GetString(LocalisationRuntime.LanguageKey), Is.EqualTo("en"));
                Assert.That(title.text, Is.EqualTo("Paused"), "The open overlay speaks English at once.");
                Assert.That(pause.LanguageToggle.ShownText, Is.EqualTo("Language: English"));

                pause.LanguageToggle.Button.onClick.Invoke();
                Assert.That(Loc.Language, Is.EqualTo(Language.Turkish));
                Assert.That(PlayerPrefs.GetString(LocalisationRuntime.LanguageKey), Is.EqualTo("tr"));
                Assert.That(title.text, Is.EqualTo("Duraklatıldı"));
            }
            finally
            {
                if (had)
                    PlayerPrefs.SetString(LocalisationRuntime.LanguageKey, stored);
                else
                    PlayerPrefs.DeleteKey(LocalisationRuntime.LanguageKey);
                PlayerPrefs.Save();
                Loc.SetLanguage(chosen);
                Loc.Override = Language.Turkish;
            }
            LogAssert.NoUnexpectedReceived();
        }

        // Every active label under root fits its rect at its smallest size: its best fit's minimum, or its size without one.
        internal static void Check(Transform root, string where, List<string> problems)
        {
            Canvas.ForceUpdateCanvases();
            foreach (Text label in root.GetComponentsInChildren<Text>())
            {
                if (string.IsNullOrEmpty(label.text))
                    continue;
                Rect rect = label.rectTransform.rect;
                TextGenerationSettings settings = label.GetGenerationSettings(new Vector2(rect.width, 0f));
                settings.resizeTextForBestFit = false;
                settings.fontSize = label.resizeTextForBestFit ? label.resizeTextMinSize : label.fontSize;
                float height = label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
                if (height > rect.height + 1f)
                    problems.Add($"{where}/{label.name} needs {height:0} of {rect.height:0} units at size {settings.fontSize}: \"{label.text}\"");
                if (label.horizontalOverflow == HorizontalWrapMode.Overflow)
                {
                    float width = label.cachedTextGeneratorForLayout.GetPreferredWidth(label.text, settings) / label.pixelsPerUnit;
                    if (width > rect.width + 1f)
                        problems.Add($"{where}/{label.name} needs {width:0} of {rect.width:0} units across: \"{label.text}\"");
                }
            }
        }

        internal static void AssertNoKeyShows()
        {
            var shown = new StringBuilder();
            foreach (Text label in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (Key.IsMatch(label.text ?? string.Empty))
                    shown.Append(label.name).Append(": ").Append(label.text).Append('\n');
            foreach (TextMesh mesh in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (Key.IsMatch(mesh.text ?? string.Empty))
                    shown.Append(mesh.name).Append(": ").Append(mesh.text).Append('\n');
            Assert.That(shown.ToString(), Is.Empty, "A label shows a key: its row is missing from the table.");
        }
    }
}
