using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // docs/57 phase 3, the HUD's three buttons (section 5.2; section 14): the ability's button alone for a new player; a
    // skill's button locked (its icon dimmed under a padlock, no tap) once its floor's first clear has opened it; live once
    // the Forge has forged it for the hero - its own icon, a tap that fires it, its cooldown band and countdown; slots 2
    // and 3 at (-90, 341) and (-306, 160), 176 units, anchored bottom-right; development/skills.txt still forces them on.
    public sealed class SkillButtonsFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private AbilityController _ability;
        private AbilityButtonView _first;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Skill buttons cleanup"));
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
            // Views bind in Start; slot 1 makes the skills' buttons in its first Update, and they start a frame later.
            yield return null;
            yield return null;
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _ability = GameObject.Find("Vanguard").GetComponent<AbilityController>();
            _first = GameObject.Find("Ability Button").GetComponent<AbilityButtonView>();
        }

        private static PlayerProfile Cleared(int floor, int gold = 0)
        {
            var profile = new PlayerProfile(gold, null, null, floor);
            return profile;
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        [UnityTest]
        public IEnumerator ANewPlayerSeesTheAbilityButtonAlone()
        {
            yield return Load(null);
            Assert.That(_first.Slot, Is.EqualTo(1));
            Assert.That(_first.SkillButton(2), Is.Null);
            Assert.That(_first.SkillButton(3), Is.Null);
            Assert.That(GameObject.Find("Skill 2 Button"), Is.Null);
            Assert.That(GameObject.Find("Skill 3 Button"), Is.Null);
            Assert.That(Object.FindObjectsByType<AbilityButtonView>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AnOpenedSkillIsLockedUntilTheForgeForgesIt()
        {
            yield return Load(Cleared(2, 900));
            AbilityButtonView second = _first.SkillButton(2);
            Assert.That(second, Is.Not.Null, "floor 2 cleared: skill 2's button shows");
            Assert.That(second.gameObject.name, Is.EqualTo("Skill 2 Button"));
            Assert.That(second.State, Is.EqualTo(AbilityButtonView.SlotState.Locked));
            Assert.That(second.LockImage.enabled, Is.True, "a padlock over it");
            Assert.That(second.Button.interactable, Is.False);
            Assert.That(second.Icon.sprite.name, Is.EqualTo("UI_Icon_IronResolve_v1"), "its own icon, dimmed");
            Assert.That(second.Icon.color.a, Is.LessThan(1f));
            Assert.That(_first.SkillButton(3), Is.Null, "floor 3 not cleared: nothing for skill 3");
            Assert.That(_ability.SkillAt(2), Is.Null, "the run does not carry it");
            Tap(second.Button);
            Assert.That(_ability.SkillAt(2), Is.Null);
            Assert.That(_first.State, Is.EqualTo(AbilityButtonView.SlotState.Live));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AForgedSkillIsLiveAndCoolsDownAtItsAnchor()
        {
            PlayerProfile seed = Cleared(3, 900);
            Assert.That(seed.TryForgeSkill("hero_vanguard", 2), Is.True);
            yield return Load(seed);
            AbilityButtonView second = _first.SkillButton(2);
            AbilityButtonView third = _first.SkillButton(3);
            Assert.That(second.State, Is.EqualTo(AbilityButtonView.SlotState.Live));
            Assert.That(third.State, Is.EqualTo(AbilityButtonView.SlotState.Locked), "floor 3 cleared, not forged");
            Assert.That(second.LockImage.enabled, Is.False);
            Assert.That(second.Icon.sprite.name, Is.EqualTo("UI_Icon_IronResolve_v1"));
            Assert.That(third.Icon.sprite.name, Is.EqualTo("UI_Icon_ShieldRush_v1"));

            // docs/57 section 5.2's anchors: 176 units on an arc from slot 1's corner, anchored bottom-right, pivot (1, 0).
            var firstRect = (RectTransform)_first.transform;
            var secondRect = (RectTransform)second.transform;
            var thirdRect = (RectTransform)third.transform;
            Assert.That(secondRect.anchoredPosition, Is.EqualTo(new Vector2(-90f, 341f)));
            Assert.That(thirdRect.anchoredPosition, Is.EqualTo(new Vector2(-306f, 160f)));
            Assert.That(secondRect.pivot, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(secondRect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(secondRect.sizeDelta.x * secondRect.localScale.x, Is.EqualTo(AbilityButtonView.SkillSize).Within(.01f));
            Assert.That(firstRect.anchoredPosition, Is.EqualTo(new Vector2(-40f, 68f)), "slot 1 where it always stood");
            Assert.That(firstRect.localScale.x, Is.EqualTo(1f));

            Assert.That(second.Button.interactable, Is.True);
            Assert.That(second.CooldownFill.fillAmount, Is.Zero);
            Tap(second.Button);
            Assert.That(_ability.SkillAt(2).IsReady, Is.False, "Iron Resolve fired");
            Assert.That(_ability.SkillAt(2).IsActive, Is.True);
            Assert.That(_first.State, Is.EqualTo(AbilityButtonView.SlotState.Live));
            Assert.That(_ability.Ability.IsReady, Is.True, "slot 1 untouched");
            yield return null;
            Assert.That(second.CooldownFill.fillAmount, Is.GreaterThan(.9f), "the band drains from full");
            Assert.That(second.Button.interactable, Is.False, "cooling");
            Assert.That(second.transform.Find("Skill 2 Countdown").GetComponent<Text>().text, Is.Not.Empty);
            Tap(third.Button);
            Assert.That(_ability.SkillAt(3), Is.Null, "a locked slot fires nothing");
            LogAssert.NoUnexpectedReceived();
        }

        // A floor cleared mid-run shows the new skill's button locked at once (the unlock is the tutorial, docs/57 5.2).
        [UnityTest]
        public IEnumerator AFloorClearedMidRunShowsItsSkillLocked()
        {
            yield return Load(Cleared(1));
            Assert.That(_first.SkillButton(2), Is.Null);
            _setup.Profile.RecordFloorCleared(2);
            yield return null;
            Assert.That(_first.SkillButton(2), Is.Not.Null);
            yield return null;
            Assert.That(_first.SkillButton(2).State, Is.EqualTo(AbilityButtonView.SlotState.Locked));
            Assert.That(_first.SkillButton(2).gameObject.activeSelf, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        // The Editor's development file still arms both skills on a profile that has forged nothing (phases 1-2's reads).
        [UnityTest]
        public IEnumerator TheDevelopmentFileStillArmsBothSkills()
        {
            yield return Load(null, "hero_cinder_arcanist", "3");
            Assert.That(_first.SkillButton(2).State, Is.EqualTo(AbilityButtonView.SlotState.Live));
            Assert.That(_first.SkillButton(3).State, Is.EqualTo(AbilityButtonView.SlotState.Live));
            Assert.That(_first.SkillButton(2).Icon.sprite.name, Is.EqualTo("UI_Icon_FlameSigil_v1"));
            Assert.That(_first.SkillButton(3).Icon.sprite.name, Is.EqualTo("UI_Icon_CinderWard_v1"));
            Tap(_first.SkillButton(3).Button);
            Assert.That(_ability.SkillAt(3).IsReady, Is.False, "Cinder Ward fired");
        }

        // The pure rule the buttons follow.
        [Test]
        public void TheSlotStatesFollowTheUnlock()
        {
            Assert.That(AbilityButtonView.StateOf(true, true, 2, 0), Is.EqualTo(AbilityButtonView.SlotState.Live), "forced or forged");
            Assert.That(AbilityButtonView.StateOf(false, true, 2, 1), Is.EqualTo(AbilityButtonView.SlotState.Hidden));
            Assert.That(AbilityButtonView.StateOf(false, true, 2, 2), Is.EqualTo(AbilityButtonView.SlotState.Locked));
            Assert.That(AbilityButtonView.StateOf(false, true, 3, 2), Is.EqualTo(AbilityButtonView.SlotState.Hidden));
            Assert.That(AbilityButtonView.StateOf(false, true, 3, 4), Is.EqualTo(AbilityButtonView.SlotState.Locked));
            Assert.That(AbilityButtonView.StateOf(false, false, 2, 4), Is.EqualTo(AbilityButtonView.SlotState.Hidden), "a hero without it");
            Assert.That(AbilityButtonView.PositionOf(2), Is.EqualTo(new Vector2(-90f, 341f)));
            Assert.That(AbilityButtonView.PositionOf(3), Is.EqualTo(new Vector2(-306f, 160f)));
        }
    }
}
