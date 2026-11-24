using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // How the game tells a player of a hero skill (docs/57 section 14), in English and Turkish: the result screen's line
    // when the run's floor clear opened skill 2 or 3 for the profile, over the forge's hint, both skills in one line when
    // one run opened both, nothing when it opened none - fitting the shortest and the tallest portrait safe areas.
    public sealed class SkillNoticeFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private Language? _before;

        [UnitySetUp]
        public IEnumerator PinTheLanguage()
        {
            _before = Loc.Override;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Loc.Override = _before;
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Skill notice cleanup " + System.Guid.NewGuid().ToString("N")));
            if (scene.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private IEnumerator Load(PlayerProfile seed, string heroId = null, string skills = null)
        {
            TestProfile.Begin(seed);
            if (heroId != null)
                File.WriteAllText(DevelopmentStart.HeroPath, heroId);
            if (skills != null)
                File.WriteAllText(DevelopmentStart.SkillsPath, skills);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            // Views bind in Start.
            yield return null;
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
        }

        private IEnumerator EndRun()
        {
            GameObject.Find("Vanguard").GetComponent<Health>().ApplyDamage(new DamageContext(100000f));
            yield return null;
            yield return new WaitForSecondsRealtime(.6f);
        }

        private static Text HintLabel => GameObject.Find("Forge Hint Label").GetComponent<Text>();

        // ---- The result screen's line (docs/57 section 14) ----

        [UnityTest]
        public IEnumerator TheResultSaysWhenTheRunsClearOpenedSkillTwo()
        {
            yield return Load(new PlayerProfile(0, null, null, 1));
            Assert.That(_setup.DeepestFloorClearedAtStart, Is.EqualTo(1));
            _setup.Profile.RecordFloorCleared(2);
            yield return EndRun();
            var result = Object.FindFirstObjectByType<RunResultView>();
            Assert.That(result.IsOpen, Is.True);
            Assert.That(result.SkillLine, Is.EqualTo("New skill opened: Iron Resolve. Forge it at the Relic Forge"));
            string[] lines = HintLabel.text.Split('\n');
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0], Does.StartWith("<color=#").And.EndWith(">" + result.SkillLine + "</color>"), "First, in gold.");
            Assert.That(lines[1], Does.StartWith("Forge gold "), "The forge's own hint stands under it.");
            AssertFits("Result Panel");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ARunThatOpensBothSkillsSaysBothInOneLine()
        {
            yield return Load(new PlayerProfile(0, null, null, 0));
            _setup.Profile.RecordFloorCleared(1);
            _setup.Profile.RecordFloorCleared(2);
            _setup.Profile.RecordFloorCleared(3);
            yield return EndRun();
            var result = Object.FindFirstObjectByType<RunResultView>();
            Assert.That(result.SkillLine, Is.EqualTo("New skills opened: Iron Resolve and Shield Rush. Forge them at the Relic Forge"));
            AssertFits("Result Panel");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ARunThatOpensNothingSaysNothing()
        {
            yield return Load(new PlayerProfile(0, null, null, 2));
            _setup.Profile.RecordFloorCleared(2);
            yield return EndRun();
            var result = Object.FindFirstObjectByType<RunResultView>();
            Assert.That(result.SkillLine, Is.Empty, "skill 2 was open before the run; floor 3 not cleared");
            Assert.That(HintLabel.text, Does.StartWith("Forge gold ").And.Not.Contain("\n"), "The hint alone, as before.");
            LogAssert.NoUnexpectedReceived();
        }

        // In Turkish, both heroes' longest line: one run opening both skills.
        [UnityTest]
        public IEnumerator TheLineFitsInTurkish()
        {
            Loc.Override = Language.Turkish;
            foreach (string hero in new[] { "hero_vanguard", "hero_cinder_arcanist" })
            {
                yield return Load(new PlayerProfile(1234, null, null, 1), hero);
                _setup.Profile.RecordFloorCleared(2);
                _setup.Profile.RecordFloorCleared(3);
                yield return EndRun();
                var result = Object.FindFirstObjectByType<RunResultView>();
                Assert.That(result.SkillLine, Does.StartWith("Yeni yetenekler açıldı: ").And.EndWith(". Kalıntı Ocağı'nda döv"));
                Assert.That(result.SkillLine, Does.Contain(hero == "hero_vanguard" ? "Demir İrade ve Kalkan Hücumu" : "Alev Mührü ve Kül Siperi"));
                AssertFits("Result Panel");
                TurkishScreensTests.AssertNoKeyShows();
            }
            LogAssert.NoUnexpectedReceived();
        }

        // Every label of the panel at its smallest size, on the shortest and the tallest portrait safe areas.
        private static void AssertFits(string panelName)
        {
            var problems = new List<string>();
            var safe = GameObject.Find(panelName).transform.Find("Safe Area").GetComponent<RectTransform>();
            safe.GetComponent<SafeAreaFitter>().enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(.5f, .5f);
            foreach (float height in new[] { 1760f, 2232f })
            {
                safe.sizeDelta = new Vector2(1080f, height);
                Canvas.ForceUpdateCanvases();
                TurkishScreensTests.Check(safe, panelName + " at " + height, problems);
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
