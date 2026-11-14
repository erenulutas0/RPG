using System;
using System.Collections;
using System.IO;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The hero chosen with the map (MapChoiceView's hero row, docs/53): one card per hero of the roster above the three
    // maps, the scene's hero lit. A hero card chooses the hero and the profile remembers it as the forge's chooser does;
    // a map with another hero reloads the scene once, which starts with that hero on that map and opens no choice; a
    // locked hero says how to unlock it and cannot be chosen.
    public sealed class HeroAndMapChoiceFlowTests
    {
        private const string Scene = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string VanguardId = "hero_vanguard";
        private const string ArcanistId = "hero_cinder_arcanist";

        [UnitySetUp] public IEnumerator Begin() { Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            RunStartRequest.Clear();
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Hero choice cleanup " + Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The foundry as a player's build loads it, from this profile: no development file decides the start.
        private static IEnumerator LoadWithTheChoice(PlayerProfile profile = null)
        {
            TestProfile.Begin(profile);
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.Delete(DevelopmentStart.MapChoicePath);
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
        }

        private static IEnumerator AfterTheInputDelay()
        {
            float ready = Time.realtimeSinceStartup + MapChoiceView.InputDelay + .2f;
            while (Time.realtimeSinceStartup < ready)
                yield return null;
        }

        private static int SlotOf(MapChoiceView view, string heroId)
        {
            for (int i = 0; i < view.HeroCardCount; i++)
                if (view.HeroAt(i).Id == heroId)
                    return i;
            Assert.Fail("No hero card for " + heroId);
            return -1;
        }

        // The scene loaded again after a tap: the new root once the old one is gone.
        private static IEnumerator Reloaded(CombatSetup previous, Action<CombatSetup> found)
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            CombatSetup setup = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                setup = previous == null ? Object.FindFirstObjectByType<CombatSetup>() : null;
                if (setup != null && setup.Run != null)
                    break;
                yield return null;
            }
            Assert.That(previous == null, Is.True, "The first scene is gone.");
            Assert.That(setup, Is.Not.Null, "The scene loaded again.");
            yield return null;
            Assert.That(RunStartRequest.IsPending, Is.False, "The choice was taken by the load it started.");
            Assert.That(Object.FindFirstObjectByType<MapChoiceView>(), Is.Null, "The chosen load opens no choice: the panel opens once.");
            Assert.That(setup.IsChoosingMap, Is.False);
            found(setup);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator IlyaAndTheFoundryStartTheRunWithIlya()
        {
            yield return LoadWithTheChoice();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.TitleText, Is.EqualTo(MapChoiceView.HeroAndMapTitle));
            Assert.That(view.CardCount, Is.EqualTo(3), "The three maps stay.");
            Assert.That(view.HeroCardCount, Is.EqualTo(setup.Heroes.Count), "A card per hero of the roster.");
            Assert.That(setup.Hero.Id, Is.EqualTo(VanguardId));
            Assert.That(view.SelectedHero, Is.SameAs(setup.Hero), "The scene's own hero is lit.");
            int vanguard = SlotOf(view, VanguardId), ilya = SlotOf(view, ArcanistId);
            Assert.That(view.HeroStateAt(vanguard), Is.EqualTo(MapChoiceView.SelectedText));
            Assert.That(view.HeroStateAt(ilya), Is.EqualTo(MapChoiceView.ChooseText));
            Assert.That(view.HeroNameAt(ilya), Does.StartWith("Cinder Arcanist").And.Contain("Ilya Kest"));
            Assert.That(view.HeroRoleAt(ilya), Is.Not.Empty, "A one-line role.");
            Assert.That(view.HeroRoleAt(vanguard), Is.Not.Empty);
            Assert.That(view.HeroWeaponAt(vanguard), Is.EqualTo("Weapon: " + setup.HeroWeapon.DisplayName));
            Assert.That(view.HeroWeaponAt(ilya), Is.EqualTo("Weapon: Staff"));
            for (int i = 0; i < view.HeroCardCount; i++)
            {
                Assert.That(view.HeroPortraitAt(i), Is.SameAs(view.HeroAt(i).Portrait).And.Not.Null, "Each hero stands in its card.");
                Assert.That(((RectTransform)view.HeroCardButton(i).transform).rect.height, Is.GreaterThanOrEqualTo(300f), "Thumb-sized.");
            }

            yield return AfterTheInputDelay();
            Assert.That(view.HeroCardButton(ilya).interactable, Is.True);
            view.HeroCardButton(ilya).onClick.Invoke();
            Assert.That(view.SelectedHero.Id, Is.EqualTo(ArcanistId));
            Assert.That(view.HeroStateAt(ilya), Is.EqualTo(MapChoiceView.SelectedText));
            Assert.That(view.HeroStateAt(vanguard), Is.EqualTo(MapChoiceView.ChooseText));
            Assert.That(view.IsOpen, Is.True, "Choosing a hero starts nothing.");
            Assert.That(setup.Profile.SelectedHeroId, Is.EqualTo(ArcanistId), "Remembered as the forge remembers it.");
            Assert.That(TestProfile.ReadSaved().SelectedHeroId, Is.EqualTo(ArcanistId), "And saved.");

            Assert.That(view.CardButton(0).interactable, Is.True);
            view.CardButton(0).onClick.Invoke();
            Assert.That((RunStartRequest.IsPending, RunStartRequest.PendingHeroId), Is.EqualTo((true, ArcanistId)),
                "Another hero on the Foundry reloads the scene with both.");
            CombatSetup next = null;
            yield return Reloaded(setup, found => next = found);
            Assert.That(next.Hero.Id, Is.EqualTo(ArcanistId), "The run starts with Ilya.");
            Assert.That(next.HeroWeapon.Id, Is.EqualTo(next.Hero.StartingWeapon.Id), "With its own weapon.");
            Assert.That((next.StartFloor, next.Encounters.FloorNumber), Is.EqualTo((1, 1)), "On the Foundry.");
            Assert.That((next.Run.Level, next.Run.PendingUpgrades, next.Choices.IsOpen), Is.EqualTo((0, 0, false)), "A fresh hero.");
            Assert.That(next.Encounters.IsFirstEncounterPending, Is.False, "The run begins at once.");
            Assert.That(Object.FindFirstObjectByType<CatchUpAutoPickView>(), Is.Null);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator TheVanguardAndTheGloamGardensStartOnFloorThreeWithTheQueuedLevelUps()
        {
            // The forge chose Ilya for the next run: the scene is built with Ilya, and the panel lights it.
            yield return LoadWithTheChoice(new PlayerProfile(0, null, null, 0, selectedHeroId: ArcanistId));
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(setup.Hero.Id, Is.EqualTo(ArcanistId));
            Assert.That(view.SelectedHero.Id, Is.EqualTo(ArcanistId));
            int vanguard = SlotOf(view, VanguardId);
            Assert.That(view.HeroStateAt(SlotOf(view, ArcanistId)), Is.EqualTo(MapChoiceView.SelectedText));

            yield return AfterTheInputDelay();
            view.HeroCardButton(vanguard).onClick.Invoke();
            Assert.That(view.SelectedHero.Id, Is.EqualTo(VanguardId));
            Assert.That(setup.Profile.SelectedHeroId, Is.Null, "The default hero is stored as no choice, as the forge stores it.");
            Assert.That(view.FloorAt(2), Is.EqualTo(3));
            view.CardButton(2).onClick.Invoke();
            Assert.That((RunStartRequest.IsPending, RunStartRequest.PendingHeroId), Is.EqualTo((true, VanguardId)));

            CombatSetup next = null;
            yield return Reloaded(setup, found => next = found);
            Assert.That(next.Hero.Id, Is.EqualTo(VanguardId), "The run starts with the Vanguard.");
            Assert.That(TestProfile.ReadSaved().SelectedHeroId, Is.Null);
            Assert.That((next.StartFloor, next.Encounters.FloorNumber), Is.EqualTo((3, 3)), "On the Gloam Gardens.");
            Assert.That(next.Encounters.Floor.Id, Is.EqualTo("floor_gloam_gardens"));
            int level = DescentStart.CatchUpLevel(3);
            Assert.That((next.Run.Level, next.Run.PendingUpgrades), Is.EqualTo((level, level)), "Its catch-up, queued.");
            Assert.That(next.Choices.IsOpen, Is.True, "The first level-up is offered.");
            Assert.That(next.IsCatchingUp, Is.True);
            var autoPick = Object.FindFirstObjectByType<CatchUpAutoPickView>();
            Assert.That(autoPick, Is.Not.Null);
            Assert.That(autoPick.IsShown, Is.True, "Auto-pick stands beside the queued level-ups.");
            Assert.That(next.Encounters.IsFirstEncounterPending, Is.True, "The first wave waits for the last pick.");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator ALockedHeroSaysHowToUnlockItAndCannotBeChosen()
        {
            yield return LoadWithTheChoice();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            // As a release build judges it: the Editor plays every hero, so the test stands in for the lock.
            view.TreatHeroesAs(hero => hero.UnlockedFromStart || setup.Profile.HasUnlockedHero(hero.Id));
            int ilya = SlotOf(view, ArcanistId);
            Assert.That(view.HeroStateAt(ilya), Is.EqualTo("Locked: " + view.HeroAt(ilya).UnlockHint));
            Assert.That(view.HeroStateAt(ilya), Does.Contain("Clear The Foundry with the Staff"));

            yield return AfterTheInputDelay();
            Assert.That(view.HeroCardButton(ilya).interactable, Is.False, "A locked hero cannot be tapped.");
            Assert.That(view.HeroCardButton(SlotOf(view, VanguardId)).interactable, Is.True);
            view.ChooseHero(ilya);
            Assert.That(view.SelectedHero.Id, Is.EqualTo(VanguardId), "Nor chosen.");
            Assert.That(setup.Profile.SelectedHeroId, Is.Null);
            Assert.That(TestProfile.ReadSaved().SelectedHeroId, Is.Null, "Nothing remembered.");

            // The scene's own hero on the Foundry: no reload, the run begins here, the panel gone for good.
            view.CardButton(0).onClick.Invoke();
            Assert.That(RunStartRequest.IsPending, Is.False);
            Assert.That(view.IsOpen, Is.False);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<CombatSetup>(), Is.SameAs(setup));
            Assert.That(setup.Hero.Id, Is.EqualTo(VanguardId));
            Assert.That(setup.Encounters.IsFirstEncounterPending, Is.False);
            view.ChooseHero(ilya);
            view.Choose(1);
            Assert.That(RunStartRequest.IsPending, Is.False, "A tap after the start chooses nothing.");
        }
    }
}
