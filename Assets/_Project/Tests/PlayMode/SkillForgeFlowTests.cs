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
    // docs/57 phase 4, the Forge's Skills tab in the scene (section 14): its entry, the Relic Forge's Skills card in the band
    // above the start button (docs/63 section 12 answer 7) - the hero's next skill and its state, the header counting the
    // skills it carries, the card in the call's gold frame while a skill is affordable; the hero's three cards (skill 1 its
    // own, skill 2 forged for 300 once floor 2 is cleared, skill 3 for 500 after floor 3 and skill 2); a tap forges, the
    // profile saves it, and the next run's HUD shows the skill live.
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

            // Back to the Relic Forge: its card moved on to Shield Rush, locked; then into the next run, whose HUD carries Iron
            // Resolve live.
            Tap(_anvil.TabButton(AnvilView.Tab.Forge));
            Assert.That(_anvil.IsOpen, Is.False);
            Assert.That(_forge.SkillsHeader.text, Is.EqualTo("Skills: 2 of 3 ready"));
            Assert.That(_forge.SkillsCardSlot, Is.EqualTo(3));
            Assert.That(_forge.SkillsCardText, Does.StartWith("Shield Rush | ").And.EndWith(" | Locked: clear floor 3"));
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

        // The band's card (docs/63 section 12 answer 7) is the next run's hero's next skill in its state, the header the
        // skills it carries: a new player's Iron Resolve locked; the card follows the hero chooser; every skill forged shows
        // the last one forged, and a tap on the card still opens the tab.
        [UnityTest]
        public IEnumerator TheForgesCardShowsTheHerosNextSkill()
        {
            yield return OpenForge(new PlayerProfile(1000, null, null, 1));
            Assert.That(_forge.SkillsHeader.text, Is.EqualTo("Skills: 1 of 3 ready"));
            Assert.That(_forge.SkillsCardSlot, Is.EqualTo(2));
            Assert.That(_forge.SkillsCardText, Does.StartWith("Iron Resolve | +100 armour for 4s").And.EndWith(" | Locked: clear floor 2"));
            Assert.That(_forge.SkillsPicture.sprite.name, Is.EqualTo("UI_Icon_IronResolve_v1"));
            Assert.That(_forge.SkillsCard.interactable, Is.True, "an entry: a tap's whatever its skill's state");
            Assert.That(_anvil.SkillsEntryCalls, Is.False);

            // Ilya chosen: her next skill, the card and the header following the chooser.
            _forge.HeroChoice.Cycle();
            yield return null;
            Assert.That(_forge.SkillsCardText, Does.StartWith("Flame Sigil | ").And.EndWith(" | Locked: clear floor 2"));
            Assert.That(_forge.SkillsHeader.text, Is.EqualTo("Skills: 1 of 3 ready"));
            _forge.HeroChoice.Cycle();
            yield return null;

            // Every skill forged: the header full, the card the last one forged; a tap opens the tab.
            _setup.Profile.RecordFloorCleared(3);
            Assert.That(_setup.Profile.TryForgeSkill("hero_vanguard", 2) && _setup.Profile.TryForgeSkill("hero_vanguard", 3), Is.True);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(_forge.SkillsHeader.text, Is.EqualTo("Skills: 3 of 3 ready"));
            Assert.That(_forge.SkillsCardText, Does.StartWith("Shield Rush | ").And.EndWith(" | Forged: yours in every run"));
            Tap(_forge.SkillsCard);
            yield return Settle();
            Assert.That(_anvil.IsOpen, Is.True);
            Assert.That(_anvil.Current, Is.EqualTo(AnvilView.Tab.Skills));
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

        // Ends the run and opens the result's Forge, past its input delay.
        private IEnumerator OpenForge(PlayerProfile seed, string heroId = null)
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
        }

        // Opens the Forge, checks the skills' entry - the band's card, in the call's gold frame while a skill is affordable -
        // and opens the Skills tab from it.
        private IEnumerator OpenSkills(PlayerProfile seed, string heroId = null, bool callsFirst = true)
        {
            yield return OpenForge(seed, heroId);
            Assert.That(_anvil.SkillsEntry, Is.Not.Null, "the skills' entry, the band's card");
            Assert.That(_anvil.SkillsEntry, Is.SameAs(_forge.SkillsCard));
            Assert.That(_anvil.SkillsEntry.name, Is.EqualTo(AnvilView.SkillsEntryName));
            Assert.That(_anvil.SkillsEntryCalls, Is.EqualTo(callsFirst), "the entry calls while a skill is affordable");
            Assert.That(_forge.SkillsCardText.EndsWith(" | " + string.Format(_setup.Text.UnlockForgeFormat, 300)), Is.EqualTo(callsFirst),
                "the card shows the affordable skill's call");
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
