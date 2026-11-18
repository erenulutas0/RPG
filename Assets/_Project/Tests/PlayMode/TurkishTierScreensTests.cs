using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The difficulty tiers' screens in Turkish (docs/60 pass 2), beside TurkishScreensTests and with its checks: the map
    // choice with its hero row (a locked hero among them), its tier row (all four tiers, Abyssal locked, each open one
    // chosen in turn) and its maps (locked ones on a tier that has not reached them); the HUD, the pause readout and the
    // result of a run on a tier above Forged. No label shows a key or the English the tiers added, and every label fits at
    // its smallest best-fit size on a short (1760) and a tall (2232) safe area. The map choice's own language toggle
    // switches the open panel at once and stores the choice, and the run then starts in a scene loaded in that language.
    public sealed class TurkishTierScreensTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private static readonly float[] SafeHeights = { 1760f, 2232f };
        // The English the tiers' views wrote before pass 2; none may show in Turkish.
        private static readonly string[] English =
        {
            "Forged", "Tempered", "Inferno", "Abyssal", "Descent", "Rewards", "Locked", "Selected", "Tap to choose", "Weapon:",
            "Built for", "Your gear", "Choose your", "Floor ", "Room "
        };

        private Language? _before;

        [UnitySetUp]
        public IEnumerator Speak()
        {
            _before = Loc.Override;
            Loc.Override = Language.Turkish;
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Loc.Override = _before;
            Time.timeScale = 1f;
            RunStartRequest.Clear();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Turkish tier screens cleanup " + Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // The Anvil's geared profile that walked out of the Descent on Forged and on Tempered: Forged, Tempered and Inferno
        // open, Abyssal locked; Inferno has reached no deeper start yet.
        private static PlayerProfile TwoTiersDone()
        {
            PlayerProfile profile = AnvilFixtures.Profile();
            profile.RecordFloorCleared(DescentStart.FinaleFloor);
            profile.RecordDescentCompleted(DifficultyTier.Forged);
            profile.RecordDescentCompleted(DifficultyTier.Tempered);
            return profile;
        }

        // The foundry as a player's build loads it, with the map choice or, choice false, with the run begun at once.
        private static IEnumerator LoadTheFoundry(PlayerProfile profile, bool choice = true)
        {
            TestProfile.Begin(profile);
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            if (choice)
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

        private static int SlotOf(MapChoiceView view, DifficultyTier tier)
        {
            for (int i = 0; i < view.TierCardCount; i++)
                if (view.TierAt(i) == tier)
                    return i;
            Assert.Fail("No tier card for " + tier);
            return -1;
        }

        // The safe area under root (its SafeAreaFitter off) at a phone's width and each height, every label checked.
        private static void CheckAtEachHeight(RectTransform safe, Transform root, string where, List<string> problems,
            MapChoiceView fit = null)
        {
            safe.GetComponent<SafeAreaFitter>().enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(.5f, .5f);
            foreach (float height in SafeHeights)
            {
                safe.sizeDelta = new Vector2(1080f, height);
                Canvas.ForceUpdateCanvases();
                if (fit != null)
                    fit.FitNow();
                TurkishScreensTests.Check(root, where + " at " + height, problems);
            }
        }

        private static void NoEnglish(Transform root, string where, List<string> problems)
        {
            foreach (Text label in root.GetComponentsInChildren<Text>())
                foreach (string word in English)
                    if (label.text.Contains(word))
                        problems.Add($"{where}/{label.name} shows the English \"{word}\": \"{label.text}\"");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator TheMapChoiceWithItsTierRowSpeaksTurkishAndFits()
        {
            yield return LoadTheFoundry(TwoTiersDone());
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(view, Is.Not.Null, "The map choice is open.");
            // As a release build judges it: the Editor plays every hero, so the test stands in for the lock.
            view.TreatHeroesAs(hero => hero.UnlockedFromStart || setup.Profile.HasUnlockedHero(hero.Id));
            var problems = new List<string>();

            Assert.That(view.TitleText, Is.EqualTo(Loc.Text("map.title_with_heroes")).And.Not.EqualTo("Choose your hero and map"));
            Assert.That(view.LanguageToggle, Is.Not.Null, "The toggle stands on the map choice too.");
            Assert.That(view.LanguageToggle.ShownText, Is.EqualTo("Dil: Türkçe"));
            Assert.That(view.TierCardCount, Is.EqualTo(DifficultyTiers.Count));
            for (int i = 0; i < view.TierCardCount; i++)
            {
                DifficultyTier tier = view.TierAt(i);
                Assert.That(view.TierNameAt(i), Is.EqualTo(TierText.Name(tier)).And.Not.EqualTo(DifficultyTiers.Name(tier)));
                Assert.That(view.TierRewardAt(i), Does.EndWith(TierText.Reward(tier)));
            }
            Assert.That(view.TierRewardAt(SlotOf(view, DifficultyTier.Tempered)), Does.EndWith("x1,25"), "The decimal comma.");
            int abyssal = SlotOf(view, DifficultyTier.Abyssal);
            Assert.That(view.IsTierOpenAt(abyssal), Is.False, "Abyssal waits for Inferno's Descent.");
            Assert.That(view.TierStateAt(abyssal), Is.EqualTo(string.Format(Loc.Numbers, setup.Text.HeroLockedFormat,
                MapChoiceView.LockedWord, TierText.UnlockHint(DifficultyTier.Abyssal))));
            Assert.That(view.TierStateAt(abyssal), Does.Contain(TierText.Name(DifficultyTier.Inferno)));
            int ilya = -1;
            for (int i = 0; i < view.HeroCardCount; i++)
                if (!view.HeroAt(i).UnlockedFromStart)
                    ilya = i;
            Assert.That(ilya, Is.GreaterThanOrEqualTo(0), "A hero locked at the start.");
            Assert.That(view.HeroStateAt(ilya), Does.StartWith(MapChoiceView.LockedWord).And.Contain(HeroText.UnlockHint(view.HeroAt(ilya))));
            Assert.That(view.HeroRoleAt(ilya), Is.EqualTo(HeroText.Role(view.HeroAt(ilya))));

            // Each open tier chosen in turn - Inferno last, whose deeper maps are locked - every label checked on both phones.
            yield return AfterTheInputDelay();
            var safe = (RectTransform)view.Panel.transform.Find("Map Choice Safe Area");
            foreach (DifficultyTier tier in new[] { DifficultyTier.Forged, DifficultyTier.Tempered, DifficultyTier.Inferno })
            {
                view.TierCardButton(SlotOf(view, tier)).onClick.Invoke();
                Assert.That(view.SelectedTier, Is.EqualTo(tier));
                Assert.That(view.TierDetailText, Does.StartWith(TierText.Name(tier) + ":").And.Contain("+%"));
                Assert.That(view.TierDetailText, Does.Not.Contain("%+"), "The percent sign before the number.");
                CheckAtEachHeight(safe, view.Panel.transform, "Map choice on " + tier, problems, view);
                NoEnglish(view.Panel.transform, "Map choice on " + tier, problems);
            }
            Assert.That((view.IsMapOpenAt(1), view.IsMapOpenAt(2)), Is.EqualTo((false, false)), "Inferno has reached no deeper start.");
            Assert.That(view.MapStateAt(1), Is.EqualTo(string.Format(Loc.Numbers, setup.Text.HeroLockedFormat, MapChoiceView.LockedWord,
                MapUnlock.Hint(2))));
            Assert.That(view.NameAt(1), Is.EqualTo(setup.Encounters.Floor.NextFloor.DisplayName));

            // Abyssal cannot be chosen, so its line is set in the label to be measured: the longest of the four.
            Text detail = null;
            foreach (Text label in view.Panel.GetComponentsInChildren<Text>())
                if (label.name == "Tier Detail")
                    detail = label;
            Assert.That(detail, Is.Not.Null);
            detail.text = MapChoiceView.DescribeTier(DifficultyTier.Abyssal, setup.GearPowerFor(view.SelectedHero));
            CheckAtEachHeight(safe, detail.transform, "Abyssal's line", problems, view);

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            TurkishScreensTests.AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheHudPauseAndResultOfATierRunNameItInTurkishAndFit()
        {
            PlayerProfile profile = TwoTiersDone();
            Assert.That(profile.SelectTier(DifficultyTier.Inferno), Is.True);
            yield return LoadTheFoundry(profile, choice: false);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(setup.Tier, Is.EqualTo(DifficultyTier.Inferno), "The profile's tier, with no choice to make.");
            yield return null;
            var problems = new List<string>();
            string inferno = TierText.Name(DifficultyTier.Inferno);

            // The HUD: the tier before the floor line, the compact gold grouped the Turkish way.
            setup.Run.AddGold(1250);
            yield return null;
            Text floor = GameObject.Find("Floor Label").GetComponent<Text>();
            Assert.That(floor.text, Does.StartWith(TierText.Named(DifficultyTier.Inferno) + TierText.Separator));
            Assert.That(GameObject.Find("Gold Count").GetComponent<Text>().text, Is.EqualTo("1.250"));
            Transform hud = GameObject.Find("HUD Canvas").transform;
            CheckAtEachHeight((RectTransform)hud.Find("Safe Area"), hud, "HUD", problems);
            NoEnglish(hud, "HUD", problems);

            // The pause: its readout's title names the tier.
            Assert.That(setup.Pause.TryPause(), Is.True);
            yield return null;
            var pause = Object.FindFirstObjectByType<PauseView>();
            Text title = GameObject.Find("Title Label").GetComponent<Text>();
            Assert.That(title.text, Is.EqualTo(TierText.AfterTitle(setup.Text.Title, DifficultyTier.Inferno)));
            Assert.That(title.text, Does.EndWith(inferno + "</color>"));
            CheckAtEachHeight((RectTransform)pause.transform.Find("Pause Panel/Safe Area"), pause.transform, "Pause", problems);
            NoEnglish(pause.transform, "Pause", problems);
            setup.Pause.TryResume();
            yield return null;

            // The result: the title, then the tier.
            setup.Run.End(RunOutcome.Defeat);
            yield return null;
            var result = Object.FindFirstObjectByType<RunResultView>();
            Assert.That(result.IsOpen, Is.True);
            Transform panel = GameObject.Find("Result Panel").transform;
            Text resultTitle = GameObject.Find("Result Title").GetComponent<Text>();
            Assert.That(resultTitle.text, Is.EqualTo(TierText.AfterTitle(setup.Text.ResultDefeatTitle, DifficultyTier.Inferno)));
            Assert.That(resultTitle.text, Does.StartWith(setup.Text.ResultDefeatTitle).And.Not.StartWith("Defeated"));
            Assert.That(GameObject.Find("Restart Label").GetComponent<Text>().text, Is.EqualTo(setup.Text.RestartLabel).And.Not.EqualTo("Try again"));
            CheckAtEachHeight((RectTransform)panel.Find("Safe Area"), panel, "Result", problems);
            NoEnglish(panel, "Result", problems);

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            TurkishScreensTests.AssertNoKeyShows();
            LogAssert.NoUnexpectedReceived();
        }

        // The toggle on the map choice: English at once on the open panel and stored, Turkish again on the next tap; a run
        // started after a switch loads the scene again in the language chosen. The stored choice the developer had is put
        // back.
        [UnityTest, Timeout(240000)]
        public IEnumerator TheMapChoicesToggleSwitchesThePanelAndTheRunLoadsInTheLanguageChosen()
        {
            bool had = PlayerPrefs.HasKey(LocalisationRuntime.LanguageKey);
            string stored = PlayerPrefs.GetString(LocalisationRuntime.LanguageKey, string.Empty);
            Language chosen = Loc.Chosen;
            try
            {
                Loc.Override = null;
                Loc.SetLanguage(Language.Turkish);
                yield return LoadTheFoundry(TwoTiersDone());
                var setup = Object.FindFirstObjectByType<CombatSetup>();
                var view = Object.FindFirstObjectByType<MapChoiceView>();
                Assert.That(setup.SceneLanguage, Is.EqualTo(Language.Turkish));
                int tempered = SlotOf(view, DifficultyTier.Tempered);
                int abyssal = SlotOf(view, DifficultyTier.Abyssal);
                // The scene's first frame can outlast the delay in a batch run, so it is judged only while the delay lasts.
                if (Time.unscaledTime < view.InputEnabledAt)
                    Assert.That(view.LanguageToggle.Button.interactable, Is.False, "Not before the panel's input delay.");
                yield return AfterTheInputDelay();
                Assert.That(view.LanguageToggle.Button.interactable, Is.True);

                view.LanguageToggle.Button.onClick.Invoke();
                Assert.That(Loc.Language, Is.EqualTo(Language.English));
                Assert.That(PlayerPrefs.GetString(LocalisationRuntime.LanguageKey), Is.EqualTo("en"));
                Assert.That(view.LanguageToggle.ShownText, Is.EqualTo("Language: English"));
                Assert.That(view.TitleText, Is.EqualTo("Choose your hero and map"), "The open panel speaks English at once.");
                Assert.That(view.TierNameAt(tempered), Is.EqualTo("Tempered"));
                Assert.That(view.TierRewardAt(tempered), Is.EqualTo("Rewards x1.25"));
                Assert.That(view.TierStateAt(abyssal), Is.EqualTo("Locked: Complete the Descent on Inferno"));
                Assert.That(view.TierDetailText, Does.StartWith("Forged: the Descent as it was first forged"));
                Assert.That(view.NameAt(0), Is.EqualTo("The Foundry"));
                Assert.That(view.HeroRoleAt(0), Is.EqualTo(view.HeroAt(0).Role));
                Assert.That(setup.LanguageChangedSinceLoad, Is.True);

                view.LanguageToggle.Button.onClick.Invoke();
                Assert.That(Loc.Language, Is.EqualTo(Language.Turkish));
                Assert.That(PlayerPrefs.GetString(LocalisationRuntime.LanguageKey), Is.EqualTo("tr"));
                Assert.That(view.LanguageToggle.ShownText, Is.EqualTo("Dil: Türkçe"));
                Assert.That(view.TierNameAt(tempered), Is.EqualTo(TierText.Name(DifficultyTier.Tempered)).And.Not.EqualTo("Tempered"));
                Assert.That(setup.LanguageChangedSinceLoad, Is.False, "Back in the scene's language.");

                // English again, then the Foundry with the scene's hero and tier: the scene loads again, in English.
                view.LanguageToggle.Button.onClick.Invoke();
                view.CardButton(0).onClick.Invoke();
                Assert.That(RunStartRequest.IsPending, Is.True, "A switched language starts the run in a scene of its own.");
                Assert.That(view.IsOpen, Is.True, "The panel stands until the scene goes.");
                Assert.That(view.LanguageToggle.Button.interactable, Is.False, "Nor can it switch again meanwhile.");
                float deadline = Time.realtimeSinceStartup + 30f;
                CombatSetup next = null;
                while (Time.realtimeSinceStartup < deadline)
                {
                    next = setup == null ? Object.FindFirstObjectByType<CombatSetup>() : null;
                    if (next != null && next.Run != null)
                        break;
                    yield return null;
                }
                Assert.That(next, Is.Not.Null, "The scene loaded again.");
                yield return null;
                Assert.That(Object.FindFirstObjectByType<MapChoiceView>(), Is.Null, "The chosen load opens no choice.");
                Assert.That(next.SceneLanguage, Is.EqualTo(Language.English));
                Assert.That((next.StartFloor, next.Tier), Is.EqualTo((1, DifficultyTier.Forged)));
                Assert.That(next.Encounters.IsFirstEncounterPending, Is.False, "The run begins at once.");
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
    }
}
