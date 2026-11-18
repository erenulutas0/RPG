using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Combat;
using Cryptforge.Core;
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
    // The difficulty tier chosen with the map (MapChoiceView's tier row, docs/54 section 21): four cards between the heroes
    // and the maps, the profile's tier lit, a locked tier saying how it opens and refusing a tap. A tier card chooses the
    // tier and the profile remembers it; the map cards follow it (each tier opens its own deeper starts); a tier other than
    // the scene's reloads the scene with the map, which then plays every enemy at the tier's health and damage, pays its
    // gold, rolls its loot on the tier, and names the tier on the HUD's floor line, the pause's title and the result's.
    public sealed class TierChoiceFlowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Scene = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";

        [UnitySetUp] public IEnumerator Begin() { Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            RunStartRequest.Clear();
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Tier choice cleanup " + Guid.NewGuid().ToString("N")));
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

        private static int SlotOf(MapChoiceView view, DifficultyTier tier)
        {
            for (int i = 0; i < view.TierCardCount; i++)
                if (view.TierAt(i) == tier)
                    return i;
            Assert.Fail("No tier card for " + tier);
            return -1;
        }

        // A profile that walked out of the Descent on Forged (Tempered open), and whose Tempered runs reached floor 2 when
        // temperedDeepest is 1, with the tier chosen.
        private static PlayerProfile Veteran(int temperedDeepest = 0, DifficultyTier selected = DifficultyTier.Forged) =>
            new PlayerProfile(0, null, null, DescentStart.FinaleFloor, tiers: ProfileTiers.Restore(new TierSnapshot
            {
                Records = new[] { new TierRecord { Tier = "tempered", DeepestFloorCleared = temperedDeepest } },
                SelectedTier = (int)selected
            }, DescentStart.FinaleFloor));

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
            Assert.That(Object.FindFirstObjectByType<MapChoiceView>(), Is.Null, "The chosen load opens no choice.");
            found(setup);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator AFreshProfileHasForgedAndTheOtherTiersLockedWithTheirHints()
        {
            yield return LoadWithTheChoice();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(setup.Tier, Is.EqualTo(DifficultyTier.Forged));
            Assert.That(setup.Encounters.Tier, Is.EqualTo(DifficultyTier.Forged));
            Assert.That(view.TierCardCount, Is.EqualTo(DifficultyTiers.Count), "A card per tier.");
            Assert.That(view.SelectedTier, Is.EqualTo(DifficultyTier.Forged));
            int forged = SlotOf(view, DifficultyTier.Forged);
            Assert.That(view.TierNameAt(forged), Is.EqualTo("Forged"));
            Assert.That(view.TierRewardAt(forged), Is.EqualTo("Rewards x1"));
            Assert.That(view.TierStateAt(forged), Is.EqualTo(MapChoiceView.SelectedText));
            Assert.That(view.TierDetailText, Does.StartWith("Forged:"));
            Assert.That(view.TierStateAt(SlotOf(view, DifficultyTier.Tempered)), Is.EqualTo("Locked: Complete the Descent on Forged"));
            Assert.That(view.TierStateAt(SlotOf(view, DifficultyTier.Inferno)), Is.EqualTo("Locked: Complete the Descent on Tempered"));
            Assert.That(view.TierStateAt(SlotOf(view, DifficultyTier.Abyssal)), Is.EqualTo("Locked: Complete the Descent on Inferno"));
            Assert.That(view.TierRewardAt(SlotOf(view, DifficultyTier.Abyssal)), Is.EqualTo("Rewards x2"));
            for (int i = 0; i < view.TierCardCount; i++)
            {
                var rect = (RectTransform)view.TierCardButton(i).transform;
                Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(150f), "Thumb-sized.");
                Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(150f));
            }
            // Every label of the row holds its text at its smallest size (the panel's best fit), as AnvilFlowTests asks.
            Canvas.ForceUpdateCanvases();
            foreach (Text label in view.Panel.GetComponentsInChildren<Text>())
            {
                if (!label.name.StartsWith("Tier", StringComparison.Ordinal) || label.text.Length == 0)
                    continue;
                TextGenerationSettings settings = label.GetGenerationSettings(new Vector2(label.rectTransform.rect.width, 0));
                settings.resizeTextForBestFit = false;
                settings.fontSize = label.resizeTextMinSize;
                float needed = label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
                Assert.That(needed, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    $"{label.name} (\"{label.text}\") truncates even at its minimum font size.");
            }

            yield return AfterTheInputDelay();
            Assert.That(view.TierCardButton(forged).interactable, Is.True);
            Assert.That(view.TierCardButton(SlotOf(view, DifficultyTier.Tempered)).interactable, Is.False, "A locked tier cannot be tapped.");
            view.ChooseTier(SlotOf(view, DifficultyTier.Tempered));
            Assert.That(view.SelectedTier, Is.EqualTo(DifficultyTier.Forged), "Nor chosen.");
            Assert.That(setup.Profile.Tiers.Selected, Is.EqualTo(DifficultyTier.Forged));
            setup.StartOnMap(1, null, DifficultyTier.Tempered);
            Assert.That(RunStartRequest.IsPending, Is.False, "StartOnMap refuses a locked tier too.");
            Assert.That(setup.IsChoosingMap, Is.True);

            // Forged on the Foundry with the scene's hero: no reload, the run begins here as it always did.
            view.CardButton(0).onClick.Invoke();
            Assert.That(RunStartRequest.IsPending, Is.False);
            Assert.That(view.IsOpen, Is.False);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<CombatSetup>(), Is.SameAs(setup));
            Assert.That(GameObject.Find("Floor Label").GetComponent<Text>().text, Does.StartWith("Floor 1"), "Forged's floor line as it was.");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator TemperedAndTheFoundryPlayTheTiersEnemiesAndRewards()
        {
            yield return LoadWithTheChoice(Veteran());
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            int tempered = SlotOf(view, DifficultyTier.Tempered);
            Assert.That(view.TierStateAt(tempered), Is.EqualTo(MapChoiceView.ChooseText), "Forged's Descent completed opens Tempered.");
            Assert.That(view.TierStateAt(SlotOf(view, DifficultyTier.Inferno)), Does.StartWith("Locked"));
            Assert.That((view.IsMapOpenAt(1), view.IsMapOpenAt(2)), Is.EqualTo((true, true)), "Forged reached every start.");

            yield return AfterTheInputDelay();
            view.TierCardButton(tempered).onClick.Invoke();
            Assert.That(view.SelectedTier, Is.EqualTo(DifficultyTier.Tempered));
            Assert.That(view.TierStateAt(tempered), Is.EqualTo(MapChoiceView.SelectedText));
            Assert.That(view.TierDetailText, Does.StartWith("Tempered: foes +").And.Contain("rewards x1.25").And.Contain("item luck +10"));
            Assert.That(setup.Profile.Tiers.Selected, Is.EqualTo(DifficultyTier.Tempered), "Remembered as the hero is.");
            Assert.That(TestProfile.ReadSaved().Tiers.Selected, Is.EqualTo(DifficultyTier.Tempered), "And saved.");
            Assert.That((view.IsMapOpenAt(1), view.IsMapOpenAt(2)), Is.EqualTo((false, false)), "Tempered has reached no deeper start yet.");
            Assert.That(view.MapStateAt(1), Is.EqualTo("Locked: Clear floor 1 to unlock"));
            Assert.That(view.CardButton(1).interactable, Is.False);
            Assert.That(view.IsOpen, Is.True, "Choosing a tier starts nothing.");

            view.CardButton(0).onClick.Invoke();
            Assert.That((RunStartRequest.IsPending, RunStartRequest.PendingTier), Is.EqualTo((true, (DifficultyTier?)DifficultyTier.Tempered)),
                "Another tier on the Foundry reloads the scene with it.");
            CombatSetup next = null;
            yield return Reloaded(setup, found => next = found);
            Assert.That(next.Tier, Is.EqualTo(DifficultyTier.Tempered));
            Assert.That(next.Encounters.Tier, Is.EqualTo(DifficultyTier.Tempered));
            Assert.That(next.Loot.Tier, Is.EqualTo(DifficultyTier.Tempered));
            Assert.That((next.StartFloor, next.Encounters.FloorNumber), Is.EqualTo((1, 1)));
            Assert.That(next.Encounters.IsFirstEncounterPending, Is.False, "The run begins at once.");
            MethodInfo health = typeof(EncounterController).GetMethod("FloorHealthMultiplier", Private);
            Assert.That((float)health.Invoke(next.Encounters, new object[] { false }),
                Is.EqualTo(ClimbScaling.Health(1) * DifficultyTiers.TemperedHealth).Within(1e-5f), "Every enemy at the tier's health.");
            PropertyInfo damage = typeof(EncounterController).GetProperty("FloorDamageMultiplier", Private);
            Assert.That((float)damage.GetValue(next.Encounters),
                Is.EqualTo(ClimbScaling.Damage(1) * DifficultyTiers.TemperedDamage).Within(1e-5f), "And its damage.");
            Assert.That(GameObject.Find("Floor Label").GetComponent<Text>().text, Does.Contain("Tempered").And.Contain("Floor 1"),
                "The HUD's floor line names the tier.");

            // The result names it too.
            yield return new WaitForSeconds(.5f);
            GameObject.Find("Vanguard").GetComponent<Health>().ApplyDamage(new DamageContext(100000f));
            yield return null;
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(Object.FindFirstObjectByType<RunResultView>().IsOpen, Is.True);
            Assert.That(GameObject.Find("Result Title").GetComponent<Text>().text, Does.Contain("Tempered"));
            Assert.That(next.Profile.Tiers.HasCompleted(DifficultyTier.Tempered), Is.False, "A death completes nothing.");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator ADeeperStartOnATierOpensOnceThatTierReachedIt()
        {
            // Tempered chosen, its runs having cleared floor 1: the Vaults open on Tempered, the Gardens not.
            yield return LoadWithTheChoice(Veteran(1, DifficultyTier.Tempered));
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(setup.Tier, Is.EqualTo(DifficultyTier.Tempered), "The profile's tier is the scene's.");
            Assert.That(view.SelectedTier, Is.EqualTo(DifficultyTier.Tempered));
            Assert.That((view.IsMapOpenAt(0), view.IsMapOpenAt(1), view.IsMapOpenAt(2)), Is.EqualTo((true, true, false)));
            Assert.That(view.MapStateAt(2), Is.EqualTo("Locked: Clear floor 2 to unlock"));

            yield return AfterTheInputDelay();
            view.TierCardButton(SlotOf(view, DifficultyTier.Forged)).onClick.Invoke();
            Assert.That((view.IsMapOpenAt(1), view.IsMapOpenAt(2)), Is.EqualTo((true, true)), "Forged's own record opens both.");
            view.TierCardButton(SlotOf(view, DifficultyTier.Tempered)).onClick.Invoke();
            Assert.That(view.IsMapOpenAt(2), Is.False, "Back on Tempered, its own record.");
            view.Choose(2);
            Assert.That(RunStartRequest.IsPending, Is.False, "The Gardens are not reached on Tempered.");

            view.CardButton(1).onClick.Invoke();
            Assert.That((RunStartRequest.IsPending, RunStartRequest.PendingTier), Is.EqualTo((true, (DifficultyTier?)DifficultyTier.Tempered)));
            CombatSetup next = null;
            yield return Reloaded(setup, found => next = found);
            Assert.That((next.Tier, next.StartFloor, next.Encounters.FloorNumber), Is.EqualTo((DifficultyTier.Tempered, 2, 2)),
                "The Vaults on Tempered.");
            MethodInfo health = typeof(EncounterController).GetMethod("FloorHealthMultiplier", Private);
            Assert.That((float)health.Invoke(next.Encounters, new object[] { true }),
                Is.EqualTo(ClimbScaling.Health(2) * ClimbScaling.Boss(2) * DifficultyTiers.TemperedHealth).Within(1e-5f),
                "The climb's tier and the difficulty tier on it, the boss's included.");
        }
    }
}
