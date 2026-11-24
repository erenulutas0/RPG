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
    // How the game tells a player of a hero skill (docs/57 sections 5.2 and 14), in English and Turkish:
    //   - the result screen's line when the run's floor clear opened skill 2 or 3 for the profile, over the forge's hint,
    //     both skills in one line when one run opened both, nothing when it opened none - fitting the shortest and the
    //     tallest portrait safe areas;
    //   - the one-line toast on the first run that carries a newly forged skill: its name and when to tap it, once the run
    //     is live, over the skill buttons' arc, catching no tap, once per skill and profile (saved at once), one skill after
    //     the other, held while a level-up is open, never for development/skills.txt's forced skills.
    public sealed class SkillNoticeFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private SkillToastView _toast;
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
            // Views bind in Start; slot 1 makes the skills' buttons in its first Update.
            yield return null;
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _toast = Object.FindFirstObjectByType<SkillToastView>();
        }

        // The hero cannot fall while a test waits on the toast's clock, and holds its swings so no kill brings a level-up
        // whose panel would hold the toast back.
        private void Sturdy()
        {
            _setup.HeroStats.AddModifier(UpgradeStat.MaxHealth, new StatModifier(ModifierOperation.Flat, 1000000f));
            GameObject hero = GameObject.Find("Vanguard");
            hero.GetComponent<Health>().Heal(1000000f);
            hero.GetComponent<AttackController>().enabled = false;
        }

        private static PlayerProfile Forged(int deepest, string heroId, params int[] slots)
        {
            var profile = new PlayerProfile(2000, null, null, deepest);
            foreach (int slot in slots)
                Assert.That(profile.TryForgeSkill(heroId, slot), Is.True);
            return profile;
        }

        private IEnumerator Until(System.Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
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

        // ---- docs/57 5.2's toast ----

        [UnityTest, Timeout(60000)]
        public IEnumerator TheFirstRunWithAForgedSkillNamesItOnce()
        {
            yield return Load(Forged(2, "hero_vanguard", 2));
            Sturdy();
            Assert.That(_toast, Is.Not.Null);
            Assert.That(_toast.PendingCount, Is.EqualTo(1), "Iron Resolve forged, never introduced.");
            Assert.That(_toast.IsShowing, Is.False, "Not in the run's first moment.");
            yield return Until(() => _toast.IsShowing, SkillToastView.LeadSeconds + 3f);
            Assert.That(_toast.IsShowing, Is.True);
            Assert.That(_toast.ShownSlot, Is.EqualTo(2));
            Assert.That(_toast.ShownLine, Does.Contain("Iron Resolve</color>").And.EndWith(": tap when the blows come"));
            Assert.That(_toast.Label.text, Is.EqualTo(_toast.ShownLine));
            Assert.That(TestProfile.ReadSaved().HasShownSkillToast("hero_vanguard", 2), Is.True, "Recorded and saved at once.");

            // Over the skill buttons' arc, in the HUD's safe area, catching no tap.
            var first = GameObject.Find("Ability Button").GetComponent<AbilityButtonView>();
            RectTransform plate = _toast.Plate;
            Assert.That(plate.parent, Is.SameAs(first.transform.parent));
            var plateCorners = new Vector3[4];
            var buttonCorners = new Vector3[4];
            plate.GetWorldCorners(plateCorners);
            ((RectTransform)first.SkillButton(2).transform).GetWorldCorners(buttonCorners);
            Assert.That(plateCorners[0].y, Is.GreaterThanOrEqualTo(buttonCorners[2].y - .5f), "Clear above skill 2's button.");
            Assert.That(plate.rect.width, Is.LessThanOrEqualTo(SkillToastView.MaxWidth + .5f));
            Assert.That(plate.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(plate.GetComponent<Image>().raycastTarget || _toast.Label.raycastTarget, Is.False);
            Assert.That(first.SkillButton(2).Button.interactable, Is.True, "The new button answers under it.");

            yield return Until(() => !_toast.IsShowing, SkillToastView.ShowSeconds + 3f);
            Assert.That(_toast.IsShowing, Is.False);
            Assert.That(_toast.enabled, Is.False, "Nothing left to introduce.");
            Assert.That(_toast.ShownCount, Is.EqualTo(1));

            // The next run carries it too: no toast again.
            int loads = 0;
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> countLoad = (scene, mode) => loads++;
            SceneManager.sceneLoaded += countLoad;
            try
            {
                _setup.RestartRun();
                yield return Until(() => loads > 0, 5f);
                yield return null;
                yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= countLoad;
            }
            Assert.That(loads, Is.EqualTo(1));
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _toast = Object.FindFirstObjectByType<SkillToastView>();
            Assert.That(_setup.Profile.HasForgedSkill("hero_vanguard", 2), Is.True);
            Assert.That(_toast.PendingCount, Is.Zero, "Once per skill and profile.");
            Assert.That(_toast.enabled, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TwoNewSkillsAreIntroducedOneAfterTheOther()
        {
            yield return Load(Forged(3, "hero_vanguard", 2, 3));
            Sturdy();
            Assert.That(_toast.PendingCount, Is.EqualTo(2));
            yield return Until(() => _toast.IsShowing, SkillToastView.LeadSeconds + 3f);
            Assert.That(_toast.ShownSlot, Is.EqualTo(2), "Skill 2 first.");
            yield return Until(() => _toast.IsShowing && _toast.ShownSlot == 3, SkillToastView.ShowSeconds + 4f);
            Assert.That(_toast.ShownSlot, Is.EqualTo(3));
            Assert.That(_toast.ShownLine, Does.Contain("Shield Rush</color>").And.EndWith(": tap when surrounded: charge along the stick"));
            Assert.That(_toast.ShownCount, Is.EqualTo(2));
            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.HasShownSkillToast("hero_vanguard", 2) && saved.HasShownSkillToast("hero_vanguard", 3), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        // A level-up open holds the toast back; answered, the run is live and the toast comes.
        [UnityTest, Timeout(60000)]
        public IEnumerator ALevelUpHoldsTheToastBack()
        {
            yield return Load(Forged(2, "hero_vanguard", 2));
            Sturdy();
            _setup.Run.GrantBonusUpgrade();
            yield return null;
            Assert.That(_setup.Choices.IsOpen, Is.True);
            yield return new WaitForSecondsRealtime(SkillToastView.LeadSeconds + 1f);
            Assert.That(_toast.IsShowing, Is.False, "Not under the level-up's panel.");
            Assert.That(_setup.Profile.HasShownSkillToast("hero_vanguard", 2), Is.False);
            _setup.Upgrades.TrySelect(_setup.Upgrades.CurrentOffer, 0);
            yield return Until(() => _toast.IsShowing, SkillToastView.LeadSeconds + 3f);
            Assert.That(_toast.IsShowing, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        // development/skills.txt forces the skills on (the Editor's measurements): nothing forged, nothing introduced.
        [UnityTest]
        public IEnumerator AForcedSkillIsNeverIntroduced()
        {
            yield return Load(new PlayerProfile(0, null, null, 3), skills: "3");
            var controller = GameObject.Find("Vanguard").GetComponent<AbilityController>();
            Assert.That(controller.SkillAt(2), Is.Not.Null, "The file forces the skills on.");
            Assert.That(_toast.PendingCount, Is.Zero);
            Assert.That(_toast.enabled, Is.False);
            yield return new WaitForSecondsRealtime(SkillToastView.LeadSeconds + .5f);
            Assert.That(_toast.ShownCount, Is.Zero);
            Assert.That(_setup.Profile.HasShownSkillToast("hero_vanguard", 2), Is.False);
        }

        // In Turkish: Ilya's two, the longest lines, each fitting its plate at its smallest size within the widest.
        [UnityTest, Timeout(60000)]
        public IEnumerator TheToastFitsInTurkish()
        {
            Loc.Override = Language.Turkish;
            yield return Load(Forged(3, "hero_cinder_arcanist", 2, 3), "hero_cinder_arcanist");
            Sturdy();
            var expected = new Dictionary<int, string>
            {
                { 2, "Alev Mührü</color>: kalabalık yakınken dokun, sonra onu ateşten geçir" },
                { 3, "Kül Siperi</color>: bossun darbesi inmeden hemen önce dokun" }
            };
            foreach (int slot in new[] { 2, 3 })
            {
                yield return Until(() => _toast.IsShowing && _toast.ShownSlot == slot, SkillToastView.ShowSeconds + 4f);
                Assert.That(_toast.ShownSlot, Is.EqualTo(slot));
                Assert.That(_toast.ShownLine, Does.EndWith(expected[slot]));
                Canvas.ForceUpdateCanvases();
                var problems = new List<string>();
                TurkishScreensTests.Check(_toast.Plate, "Skill toast " + slot, problems);
                Assert.That(problems, Is.Empty, string.Join("\n", problems));
                Assert.That(_toast.Plate.rect.width, Is.LessThanOrEqualTo(SkillToastView.MaxWidth + .5f));
            }
            TurkishScreensTests.AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
