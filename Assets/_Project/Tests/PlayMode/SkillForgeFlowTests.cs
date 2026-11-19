using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // docs/57 phase 4, the Forge's Skills tab in the scene (section 14): its entry beside the weapons' header, on the
    // primary plate while a skill is affordable; the hero's three cards (skill 1 its own, skill 2 forged for 300 once floor
    // 2 is cleared, skill 3 for 500 after floor 3 and skill 2); a tap forges, the profile saves it, and the next run's HUD
    // shows the skill live.
    public sealed class SkillForgeFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private RelicForgeView _forge;
        private AnvilView _anvil;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Skill Forge Test Cleanup " + System.Guid.NewGuid().ToString("N")));
            if (gameplay.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator ForgingSkillTwoMakesItLiveInTheNextRun()
        {
            yield return OpenSkills(new PlayerProfile(450, null, null, 2));
            Assert.That(_anvil.SkillsHeroText, Does.StartWith("Vanguard"));
            Assert.That(_anvil.SkillCardStatus(1), Is.EqualTo(SkillForgeStatus.Forged));
            Assert.That(_anvil.SkillCardText(1), Does.StartWith("Forge Burst").And.EndWith("Ready from the first run"));
            Assert.That(_anvil.SkillCardStatus(2), Is.EqualTo(SkillForgeStatus.Affordable));
            Assert.That(_anvil.SkillCardText(2), Does.StartWith("Iron Resolve | +100 armour for 4s").And.EndWith("Forge for 300 gold"));
            Assert.That(_anvil.SkillCardIcon(2).name, Is.EqualTo("UI_Icon_IronResolve_v1"));
            Assert.That(_anvil.SkillCardStatus(3), Is.EqualTo(SkillForgeStatus.Locked));
            Assert.That(_anvil.SkillCardText(3), Does.EndWith("Locked: clear floor 3"));
            Assert.That(_anvil.SkillCardButton(3).interactable, Is.False);

            Tap(_anvil.SkillCardButton(3));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(450), "a locked card forges nothing");
            Tap(_anvil.SkillCardButton(2));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(150), "300 spent");
            Assert.That(_setup.Profile.HasForgedSkill("hero_vanguard", 2), Is.True);
            Assert.That(_anvil.SkillCardStatus(2), Is.EqualTo(SkillForgeStatus.Forged));
            Assert.That(_anvil.SkillCardText(2), Does.EndWith("Forged: yours in every run"));
            Assert.That(_anvil.SkillsEntryCalls, Is.False, "nothing affordable is left");
            Assert.That(TestProfile.ReadSaved().HasForgedSkill("hero_vanguard", 2), Is.True, "saved at once");
            yield return Settle();
            Tap(_anvil.SkillCardButton(2));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(150), "never forged twice");

            // Back to the Relic Forge and into the next run: the HUD carries Iron Resolve live.
            Tap(_anvil.TabButton(AnvilView.Tab.Forge));
            Assert.That(_anvil.IsOpen, Is.False);
            yield return new WaitForSecondsRealtime(.4f);
            int loads = 0;
            UnityAction<Scene, LoadSceneMode> countLoad = (scene, mode) => loads++;
            SceneManager.sceneLoaded += countLoad;
            try
            {
                Tap(GameObject.Find("Start Run Button").GetComponent<Button>());
                float deadline = Time.realtimeSinceStartup + 5f;
                while (loads == 0 && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
                yield return null;
                yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= countLoad;
            }
            Assert.That(loads, Is.EqualTo(1));
            var controller = GameObject.Find("Vanguard").GetComponent<AbilityController>();
            Assert.That(controller.SkillAt(2), Is.Not.Null, "the next run carries the forged skill");
            Assert.That(controller.SkillAt(2).Kind, Is.EqualTo(AbilityKind.Guard));
            Assert.That(controller.SkillAt(3), Is.Null);
            var first = GameObject.Find("Ability Button").GetComponent<AbilityButtonView>();
            Assert.That(first.SkillButton(2), Is.Not.Null);
            Assert.That(first.SkillButton(2).State, Is.EqualTo(AbilityButtonView.SlotState.Live));
            Assert.That(first.SkillButton(3), Is.Null, "floor 3 not cleared: no third button");
            LogAssert.NoUnexpectedReceived();
        }

        // Skill 3 waits on skill 2, then on its gold; the entry calls while something is affordable.
        [UnityTest]
        public IEnumerator SkillThreeWaitsOnSkillTwoThenOnItsGold()
        {
            yield return OpenSkills(new PlayerProfile(350, null, null, 4));
            Assert.That(_anvil.SkillCardStatus(3), Is.EqualTo(SkillForgeStatus.NeedsEarlierSkill));
            Assert.That(_anvil.SkillCardText(3), Does.EndWith("Forge Iron Resolve first"));
            Tap(_anvil.SkillCardButton(3));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(350));
            Tap(_anvil.SkillCardButton(2));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(50));
            Assert.That(_anvil.SkillCardStatus(3), Is.EqualTo(SkillForgeStatus.TooExpensive));
            Assert.That(_anvil.SkillCardText(3), Does.EndWith("500 gold: need 450 more"));
            Assert.That(_anvil.SkillsEntryCalls, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        // A new profile sees the tab, every skill but the first locked, and an entry that does not call.
        [UnityTest]
        public IEnumerator ANewProfileSeesItsSkillsLocked()
        {
            yield return OpenSkills(new PlayerProfile(1000, null, null, 1), callsFirst: false);
            Assert.That(_anvil.SkillCardStatus(2), Is.EqualTo(SkillForgeStatus.Locked));
            Assert.That(_anvil.SkillCardText(2), Does.EndWith("Locked: clear floor 2"));
            Assert.That(_anvil.SkillCardStatus(3), Is.EqualTo(SkillForgeStatus.Locked));
            Tap(_anvil.SkillCardButton(2));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(1000));
            LogAssert.NoUnexpectedReceived();
        }

        // The tab follows the forge's hero: Ilya's three, and in Turkish (docs/60) the table's words.
        [UnityTest]
        public IEnumerator IlyasSkillsInTurkish()
        {
            Language? before = Loc.Override;
            Loc.Override = Language.Turkish;
            try
            {
                yield return OpenSkills(new PlayerProfile(400, null, null, 3), heroId: "hero_cinder_arcanist");
                Assert.That(_anvil.SkillCardText(1), Does.StartWith("Pota Ağzı"));
                Assert.That(_anvil.SkillCardText(2), Does.StartWith("Alev Mührü").And.EndWith("300 altına döv"));
                Assert.That(_anvil.SkillCardText(3), Does.StartWith("Kül Siperi").And.EndWith("Önce Alev Mührü dövülmeli"));
                Assert.That(_anvil.SkillCardIcon(3).name, Is.EqualTo("UI_Icon_CinderWard_v1"));
                var problems = new System.Collections.Generic.List<string>();
                TurkishScreensTests.Check(_anvil.Panel.transform, "Skills", problems);
                Assert.That(problems, Is.Empty, string.Join("\n", problems));
                TurkishScreensTests.AssertNoKeyShows();
            }
            finally
            {
                Loc.Override = before;
            }
        }

        // Ends the run, opens the result's Forge, checks the skills' entry and opens the Skills tab from it.
        private IEnumerator OpenSkills(PlayerProfile seed, string heroId = null, bool callsFirst = true)
        {
            TestProfile.Begin(seed);
            if (heroId != null)
                File.WriteAllText(DevelopmentStart.HeroPath, heroId);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _forge = Object.FindFirstObjectByType<RelicForgeView>();
            GameObject.Find("Vanguard").GetComponent<Health>().ApplyDamage(new DamageContext(100000f));
            yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            Tap(GameObject.Find("Forge Button").GetComponent<Button>());
            Assert.That(_forge.IsOpen, Is.True);
            yield return new WaitForSecondsRealtime(.4f);
            _anvil = _forge.Anvil;
            Assert.That(_anvil.SkillsEntry, Is.Not.Null, "the skills' entry beside the weapons' header");
            Assert.That(_anvil.SkillsEntry.name, Is.EqualTo(AnvilView.SkillsEntryName));
            Assert.That(_anvil.SkillsEntryCalls, Is.EqualTo(callsFirst), "the entry calls while a skill is affordable");
            Tap(_anvil.SkillsEntry);
            yield return Settle();
            Assert.That(_anvil.IsOpen, Is.True);
            Assert.That(_anvil.Current, Is.EqualTo(AnvilView.Tab.Skills));
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(AnvilView.InputDelay + .05f);
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
