using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Combat;
using Cryptforge.Content;
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
    // The map choice before a run (MapChoiceView, docs/53) in the scene. It opens over the foundry with the three maps and
    // holds the first wave; the Foundry begins the run there as every run began before; the Quicksilver Vaults and the
    // Gloam Gardens reload the scene, which starts on that floor as a descent would have reached it - its FloorNumber and
    // climb scaling, its theme and dress, its enemies' palette, the halls above it, the floors after it - with no frost or
    // melt, and the hero at the simulation's arrival level, every level-up of it offered before the first wave. With
    // map-choice.txt "off" (every other scene test) the run starts at once on floor 1, as it always did. A slow loading
    // frame spends neither the panel's input delay nor that of a deeper start's first level-up.
    public sealed class MapChoiceFlowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Scene = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";

        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            RunStartRequest.Clear();
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Map choice cleanup " + Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The foundry as a player's build loads it: no development file decides the start, so the map choice opens.
        private static IEnumerator LoadWithTheMapChoice()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.Delete(DevelopmentStart.MapChoicePath);
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
        }

        // A card tapped once its input delay is over, as a finger would.
        private static IEnumerator Tap(MapChoiceView view, int slot)
        {
            float ready = Time.realtimeSinceStartup + MapChoiceView.InputDelay + .2f;
            while (Time.realtimeSinceStartup < ready)
                yield return null;
            Assert.That(view.CardButton(slot).interactable, Is.True, "The card can be tapped after its delay.");
            view.CardButton(slot).onClick.Invoke();
        }

        // A slow load: the loading frame held this long (a phone's load, or a batch run's), past every input delay here.
        private const int SlowLoadMilliseconds = 600;

        // The foundry loaded through a slow loading frame. loaded gets that frame's clock (Time.unscaledTime, the frame's
        // start), read once the scene has had its Awake - CombatSetup's, which builds the map choice or opens a deeper start's
        // first level-up - and before its first Update.
        private static IEnumerator LoadSlowly(Action<float> loaded)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> slow = (scene, mode) =>
            {
                loaded(Time.unscaledTime);
                System.Threading.Thread.Sleep(SlowLoadMilliseconds);
            };
            SceneManager.sceneLoaded += slow;
            try
            {
                yield return SceneManager.LoadSceneAsync(Scene);
            }
            finally
            {
                SceneManager.sceneLoaded -= slow;
            }
        }

        // The map choice's input delay counts from the panel's first frame after the load (the run flow's review,
        // 2026-11-26). CombatSetup.Awake builds the panel in the loading frame, whose clock is that frame's start, so a load
        // longer than the delay had spent it before the panel was seen, and the second tap of Restart on the result screen
        // landed on a map card. After a slow loading frame no card, toggle or arena button takes a tap inside the delay; then
        // the Foundry begins the run as ever.
        [UnityTest, Timeout(120000)]
        public IEnumerator ASlowLoadStillHoldsTheMapChoiceForItsDelay()
        {
            File.Delete(DevelopmentStart.MapChoicePath);
            float loadingFrame = -1f;
            yield return LoadSlowly(clock => loadingFrame = clock);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(view, Is.Not.Null, "The map choice opens before the run.");
            Assert.That(loadingFrame, Is.GreaterThanOrEqualTo(0f), "The scene came through the slow loading frame.");
            // The frame after the slow one, where the panel stamps its delay.
            int frames = 0;
            do
                yield return null;
            while (float.IsPositiveInfinity(view.InputEnabledAt) && ++frames < 10);
            Assert.That(Time.unscaledTime, Is.GreaterThanOrEqualTo(loadingFrame + .5f), "A frame after the slow one.");
            Assert.That(Time.unscaledTime, Is.LessThan(view.InputEnabledAt), "Still inside the delay: it was not spent loading.");
            Assert.That(view.InputEnabledAt, Is.GreaterThanOrEqualTo(loadingFrame + .5f + MapChoiceView.InputDelay));
            for (int i = 0; i < view.CardCount; i++)
                Assert.That(view.CardButton(i).interactable, Is.False, "Card " + i + " waits out the delay.");
            Assert.That(view.LanguageToggle.Button.interactable || view.ArenaButton.interactable, Is.False);
            view.CardButton(0).onClick.Invoke();
            view.ArenaButton.onClick.Invoke();
            Assert.That(view.IsOpen, Is.True, "A tap inside the delay chooses nothing.");
            Assert.That(setup.IsChoosingMap, Is.True);
            Assert.That(setup.Encounters.IsFirstEncounterPending, Is.True);
            Assert.That(ArenaScreen.Current == null, Is.True, "Nor opens the arena.");

            yield return Tap(view, 0);
            Assert.That(view.IsOpen, Is.False, "After the delay the Foundry begins the run.");
            Assert.That(setup.IsChoosingMap, Is.False);
        }

        // The same for a deeper start's first level-up, which opens in the loading frame (CombatSetup.Awake): its cards and
        // the Auto-pick beside them wait their delay from the first frame after the load, so the second tap of the map card
        // that started it picks nothing; after the delay a card takes the tap.
        [UnityTest, Timeout(180000)]
        public IEnumerator ASlowLoadStillHoldsADeeperStartsFirstLevelUpForItsDelay()
        {
            TestProfile.Begin(new PlayerProfile(0, null, null, 1));
            RunStartRequest.Set(2);
            float loadingFrame = -1f;
            yield return LoadSlowly(clock => loadingFrame = clock);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var choices = Object.FindFirstObjectByType<RunChoiceView>();
            // The level-up panel shows its first prompt on the frame after the load, as the other deeper starts read it.
            for (int wait = 0; wait < 10 && !choices.IsOpen; wait++)
                yield return null;
            Assert.That(loadingFrame, Is.GreaterThanOrEqualTo(0f), "The scene came through the slow loading frame.");
            Assert.That(setup.StartFloor, Is.EqualTo(2));
            Assert.That(setup.IsCatchingUp, Is.True, "The first catch-up level-up is offered.");
            Assert.That(choices.IsOpen, Is.True);
            int frames = 0;
            do
                yield return null;
            while (float.IsPositiveInfinity(choices.InputEnabledAt) && ++frames < 10);
            Assert.That(Time.unscaledTime, Is.GreaterThanOrEqualTo(loadingFrame + .5f), "A frame after the slow one.");
            Assert.That(Time.unscaledTime, Is.LessThan(choices.InputEnabledAt), "Still inside the delay: it was not spent loading.");
            var autoPick = Object.FindFirstObjectByType<CatchUpAutoPickView>();
            Assert.That(autoPick, Is.Not.Null);
            Assert.That(autoPick.IsShown, Is.True, "Auto-pick stands beside the cards.");
            Assert.That(choices.CardButton(0).interactable, Is.False, "The cards wait out the delay.");
            choices.CardButton(0).onClick.Invoke();
            autoPick.Button.onClick.Invoke();
            Assert.That(setup.Run.UpgradesApplied, Is.Zero, "Neither a card nor the Auto-pick takes a tap inside the delay.");
            Assert.That(setup.IsCatchingUp, Is.True);
            Assert.That(autoPick.Summary, Is.Empty);

            // A loaded machine can be a frame late to lift the delay: wait for it, up to 2 s.
            float lifted = Time.realtimeSinceStartup + 2f;
            while (!choices.CardButton(0).interactable && Time.realtimeSinceStartup < lifted)
                yield return null;
            Assert.That(choices.CardButton(0).interactable, Is.True, "The cards take taps once the delay is over.");
            choices.CardButton(0).onClick.Invoke();
            Assert.That(setup.Run.UpgradesApplied, Is.EqualTo(1), "After the delay a card is taken.");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheMapChoiceHoldsTheFirstWaveAndTheFoundryBeginsTheRunAsBefore()
        {
            yield return LoadWithTheMapChoice();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounters = setup.Encounters;
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            var movement = Object.FindFirstObjectByType<HeroMovementInput>();
            Assert.That(view, Is.Not.Null, "The map choice opens before the run.");
            Assert.That(view.IsOpen, Is.True);
            Assert.That(view.Panel.GetComponent<Canvas>().sortingOrder, Is.EqualTo(MapChoiceView.SortingOrder));
            Assert.That(view.CardCount, Is.EqualTo(3));
            Assert.That((view.FloorAt(0), view.FloorAt(1), view.FloorAt(2)), Is.EqualTo((1, 2, 3)));
            Assert.That((view.NameAt(0), view.NameAt(1), view.NameAt(2)),
                Is.EqualTo(("The Foundry", "Quicksilver Vaults", "Gloam Gardens")));
            for (int i = 0; i < 3; i++)
            {
                var preview = view.CardButton(i).transform.Find("Map Preview").GetComponent<UnityEngine.UI.Image>();
                Assert.That(preview.sprite, Is.Not.Null, "Each map shows a crop of its own backdrop plate: " + view.NameAt(i));
                Assert.That(((RectTransform)view.CardButton(i).transform).rect.height, Is.GreaterThanOrEqualTo(300f),
                    "Thumb-sized cards.");
            }
            Assert.That(setup.IsChoosingMap, Is.True);
            Assert.That(encounters.IsFirstEncounterPending, Is.True, "The first wave waits for a map.");
            Assert.That(movement.enabled, Is.False, "The hero stands while the player chooses.");
            Assert.That((encounters.FloorNumber, encounters.StartFloorNumber, setup.StartFloor), Is.EqualTo((1, 1, 1)));
            Assert.That((setup.Run.Level, setup.Choices.IsOpen), Is.EqualTo((0, false)));

            float still = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < still)
                yield return null;
            Assert.That(encounters.AllAliveCount, Is.EqualTo(0), "Nothing walks in while the map choice is open.");
            Assert.That(encounters.IsFirstEncounterPending, Is.True);

            yield return Tap(view, 0);
            Assert.That(view.IsOpen, Is.False, "The Foundry closes the choice.");
            Assert.That(RunStartRequest.IsPending, Is.False, "Floor 1 needs no reload.");
            Assert.That(setup.IsChoosingMap, Is.False);
            Assert.That(encounters.IsFirstEncounterPending, Is.False, "The run begins at once.");
            Assert.That(movement.enabled, Is.True, "The hero walks again.");
            Assert.That(encounters.Floor.Id, Is.EqualTo("floor_ember_halls"));
            Assert.That((encounters.FloorNumber, setup.StartFloor), Is.EqualTo((1, 1)));
            Assert.That(Object.FindFirstObjectByType<ArenaView>().Theme, Is.EqualTo(FloorTheme.Foundry));
            Assert.That((setup.Run.Level, setup.Run.PendingUpgrades, setup.Choices.IsOpen), Is.EqualTo((0, 0, false)),
                "A fresh hero, as every run began.");
            Assert.That(Object.FindFirstObjectByType<CatchUpAutoPickView>(), Is.Null, "A run's own level-ups never offer Auto-pick.");
            Assert.That(setup.IsCatchingUp, Is.False);

            float deadline = Time.realtimeSinceStartup + 30f;
            while (encounters.AllAliveCount == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(encounters.AllAliveCount, Is.GreaterThan(0), "Floor 1's first enemies walk in.");
            view.Choose(1);
            yield return null;
            Assert.That(RunStartRequest.IsPending, Is.False, "A second tap chooses nothing.");
            Assert.That(Object.FindFirstObjectByType<CombatSetup>(), Is.SameAs(setup));
        }

        // Nothing runs while the map is chosen, so nothing pauses: leaving the app there opened the pause overlay under the
        // map choice's canvas, and the run then began frozen (the run flow's review, 2026-11-26). And the phone's own screen
        // timeout stands on the map choice and the result screen, while the live run between them keeps the screen on.
        [UnityTest, Timeout(120000)]
        public IEnumerator NothingPausesOnTheMapChoiceAndOnlyTheLiveRunKeepsTheScreenOn()
        {
            yield return LoadWithTheMapChoice();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            var pause = Object.FindFirstObjectByType<PauseView>();
            Assert.That(setup.IsChoosingMap, Is.True);
            Assert.That(setup.Pause.CanPause, Is.False, "Nothing runs yet to pause.");
            Assert.That(GameObject.Find("Pause Button"), Is.Null, "No pause button under the map choice.");
            Assert.That(CombatSetup.KeepsScreenOn, Is.False, "The map choice keeps the phone's own screen timeout.");

            CombatSetup.FocusLossPausesOverride = true;
            try
            {
                setup.SendMessage("OnApplicationFocus", false);
                setup.SendMessage("OnApplicationPause", true);
                Assert.That(setup.Pause.IsPlayerPaused, Is.False, "Leaving the app on the map choice pauses nothing.");
                Assert.That(pause.IsOpen, Is.False, "No overlay hidden under the map choice.");
                setup.SendMessage("OnApplicationPause", false);
                setup.SendMessage("OnApplicationFocus", true);
            }
            finally
            {
                CombatSetup.FocusLossPausesOverride = null;
            }

            yield return Tap(view, 0);
            Assert.That(setup.IsChoosingMap, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "The run begins running, not frozen.");
            Assert.That(setup.Pause.CanPause, Is.True, "Begun, the run can be paused.");
            Assert.That(GameObject.Find("Pause Button"), Is.Not.Null, "Its pause button shows.");
            Assert.That(CombatSetup.KeepsScreenOn, Is.True, "The live run keeps the screen on.");

            setup.SendMessage("OnApplicationPause", true);
            Assert.That(pause.IsOpen, Is.True, "Leaving the app now pauses the run.");
            Assert.That(setup.Pause.TryResume(), Is.True);

            // The live run reloads the scene with floor 1 chosen, as a map tapped does: the new scene's run keeps the screen
            // on, whichever of the old scene's OnDestroy and the new one's Awake comes first.
            RunStartRequest.Set(DescentStart.FirstFloor, null, setup.Tier);
            CombatSetup previous = setup;
            previous.RestartRun();
            float deadline = Time.realtimeSinceStartup + 30f;
            setup = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                setup = previous == null ? Object.FindFirstObjectByType<CombatSetup>() : null;
                if (setup != null && setup.Run != null)
                    break;
                yield return null;
            }
            Assert.That(setup, Is.Not.Null, "The scene loaded again.");
            Assert.That(setup.IsChoosingMap, Is.False, "The chosen load opens no map choice.");
            Assert.That(CombatSetup.KeepsScreenOn, Is.True, "The reloaded run keeps the screen on.");

            setup.Run.End(RunOutcome.Defeat);
            Assert.That(CombatSetup.KeepsScreenOn, Is.False, "The result screen lets the screen sleep again.");
            LogAssert.NoUnexpectedReceived();
        }

        // Every other scene test: map-choice.txt "off" (TestProfile) starts the run at once on floor 1, as before.
        [UnityTest, Timeout(120000)]
        public IEnumerator WithTheMapChoiceOffTheRunStartsOnFloorOneAtOnce()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Assert.That(File.ReadAllText(DevelopmentStart.MapChoicePath), Is.EqualTo("off"));
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(Object.FindFirstObjectByType<MapChoiceView>(), Is.Null);
            Assert.That(setup.IsChoosingMap, Is.False);
            Assert.That(setup.Encounters.IsFirstEncounterPending, Is.False);
            Assert.That((setup.Encounters.FloorNumber, setup.Encounters.StartFloorNumber, setup.StartFloor), Is.EqualTo((1, 1, 1)));
            Assert.That(setup.Encounters.Floor.Id, Is.EqualTo("floor_ember_halls"));
            Assert.That(setup.Run.Level, Is.EqualTo(0));
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator TheQuicksilverVaultsStartOnFloorTwoCaughtUp() =>
            StartsOn(1, 2, "floor_quicksilver_vaults", FloorTheme.Ice, "floor_gloam_gardens");

        [UnityTest, Timeout(240000)]
        public IEnumerator TheGloamGardensStartOnFloorThreeCaughtUp() =>
            StartsOn(2, 3, "floor_gloam_gardens", FloorTheme.Gloam, "floor_final_swarm");

        private static IEnumerator StartsOn(int slot, int floorNumber, string floorId, FloorTheme theme, string nextId)
        {
            // A deeper map opens once the floor above it is cleared (MapUnlock): a profile that has reached it.
            TestProfile.Begin(new PlayerProfile(0, null, null, floorNumber - 1));
            yield return LoadWithTheMapChoice();
            var previous = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<MapChoiceView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.FloorAt(slot), Is.EqualTo(floorNumber));
            Assert.That(view.IsMapOpenAt(slot), Is.True, "Reached, so open.");
            yield return Tap(view, slot);

            // The deeper map reloads the scene, which begins there.
            float deadline = Time.realtimeSinceStartup + 30f;
            CombatSetup setup = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                setup = previous == null ? Object.FindFirstObjectByType<CombatSetup>() : null;
                if (setup != null && setup.Run != null)
                    break;
                yield return null;
            }
            Assert.That(previous == null, Is.True, "The floor-1 scene is gone.");
            Assert.That(setup, Is.Not.Null, "The scene loaded again.");
            yield return null;
            Assert.That(RunStartRequest.IsPending, Is.False, "The choice was taken by the load it started.");
            Assert.That(Object.FindFirstObjectByType<MapChoiceView>(), Is.Null, "The chosen load opens no map choice.");
            Assert.That(setup.IsChoosingMap, Is.False);
            Assert.That(CombatSetup.KeepsScreenOn, Is.True, "Its run is live from the load: the catch-up's cards are read with the screen on.");

            // The floor, as the stairs would have reached it.
            EncounterController encounters = setup.Encounters;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(encounters.Floor.Id, Is.EqualTo(floorId));
            Assert.That((encounters.FloorNumber, encounters.StartFloorNumber, setup.StartFloor),
                Is.EqualTo((floorNumber, floorNumber, floorNumber)), "Its FloorNumber is the floor's own.");
            Assert.That(encounters.Floor.Theme, Is.EqualTo(theme));
            Assert.That(arena.Theme, Is.EqualTo(theme), "The arena wears the floor's theme.");
            Assert.That(arena.Region.Theme, Is.EqualTo(theme), "The region is dressed for it.");
            Assert.That(arena.FrostTransition, Is.Null, "No frost: the run did not descend onto it.");
            Assert.That(arena.GloamTransition, Is.Null, "No melt either.");
            Assert.That(encounters.HasNextFloor, Is.True);
            Assert.That(encounters.Floor.NextFloor.Id, Is.EqualTo(nextId), "The chain goes on from it.");
            var authored = (FloorDefinition)typeof(EncounterController).GetField("_floor", Private).GetValue(encounters);
            EncounterController.DescentFloor(authored, floorNumber, out _, out int hallsAbove);
            Assert.That((int)typeof(EncounterController).GetField("_hallsOnEarlierFloors", Private).GetValue(encounters),
                Is.EqualTo(hallsAbove), "Its halls go on round from where the floors above would have left them.");
            MethodInfo health = typeof(EncounterController).GetMethod("FloorHealthMultiplier", Private);
            Assert.That((float)health.Invoke(encounters, new object[] { false }),
                Is.EqualTo(ClimbScaling.Health(floorNumber)).Within(1e-5f), "Climb-scaled at its height.");
            Assert.That((float)health.Invoke(encounters, new object[] { true }),
                Is.EqualTo(ClimbScaling.Health(floorNumber) * ClimbScaling.Boss(floorNumber)).Within(1e-5f), "And its boss with it.");
            if (floorNumber == 2)
                Assert.That(EliteLeaderOf(encounters.Floor), Is.EqualTo("enemy_mirror_sentry"), "Floor 2's elite is the Mirror Sentry.");

            // The catch-up: the arrival level, its level-ups offered one after another before the first wave.
            int level = DescentStart.CatchUpLevel(floorNumber);
            Assert.That(level, Is.GreaterThan(0));
            Assert.That((setup.Run.Level, setup.Run.PendingUpgrades), Is.EqualTo((level, level)));
            Assert.That(setup.Run.Gold, Is.EqualTo(0), "No forge gold comes with it.");
            Assert.That(setup.Choices.IsOpen, Is.True, "The first level-up is offered.");
            Assert.That(setup.Choices.Current.Kind, Is.EqualTo(Cryptforge.Progression.ChoiceKind.Upgrade));
            Assert.That(Object.FindFirstObjectByType<RunChoiceView>().IsOpen, Is.True, "On the level-up panel.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "The run waits while the player chooses.");

            // The panel offers Auto-pick beside the cards while the catch-up's level-ups are queued.
            Assert.That(setup.IsCatchingUp, Is.True);
            var autoPick = Object.FindFirstObjectByType<CatchUpAutoPickView>();
            Assert.That(autoPick, Is.Not.Null, "A deeper start has the Auto-pick.");
            Assert.That(autoPick.IsShown, Is.True, "Auto-pick stands on the level-up panel.");
            Assert.That(((RectTransform)autoPick.Button.transform).rect.height, Is.GreaterThanOrEqualTo(120f), "Thumb-sized.");
            Assert.That(autoPick.Button.GetComponentInChildren<UnityEngine.UI.Text>().text,
                Is.EqualTo(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Auto-pick {0} level-ups", level)));
            int picks = 0;
            if (autoPick && floorNumber == 3)
            {
                // One tap takes every level-up queued, as the careful autopilot would, and the run begins.
                float ready = Time.realtimeSinceStartup + CatchUpAutoPickView.InputDelay + .2f;
                while (Time.realtimeSinceStartup < ready)
                    yield return null;
                Assert.That(encounters.IsFirstEncounterPending, Is.True);
                autoPick.Button.onClick.Invoke();
                picks = setup.Run.UpgradesApplied;
                Assert.That(setup.Choices.IsOpen, Is.False, "Every queued level-up taken in one tap.");
                Assert.That(autoPick.Summary, Does.StartWith("Auto-picked "), "Its one-line summary.");
                Assert.That(autoPick.Summary, Does.Contain(picks.ToString(System.Globalization.CultureInfo.InvariantCulture) + " level-ups"));
                yield return null;
                Assert.That(autoPick.IsShown, Is.False);
                Assert.That(setup.IsCatchingUp, Is.False);
            }
            else
            {
                // By hand, as ever: the button stays beside the cards until the last pick.
                while (setup.Choices.IsOpen && picks < 200)
                {
                    Assert.That(encounters.IsFirstEncounterPending, Is.True, $"No wave before the last level-up (pick {picks}).");
                    Assert.That(encounters.AllAliveCount, Is.EqualTo(0));
                    Assert.That(setup.Choices.TrySelect(setup.Choices.Current, 0), Is.True);
                    picks++;
                    yield return null;
                    if (setup.Choices.IsOpen)
                        Assert.That(autoPick.IsShown, Is.True, $"Auto-pick still offered after pick {picks}.");
                }
                Assert.That(autoPick.IsShown, Is.False, "Gone with the last level-up.");
            }
            Assert.That(picks, Is.GreaterThan(0));
            Assert.That(picks, Is.LessThanOrEqualTo(level));
            Assert.That(setup.Run.UpgradesApplied, Is.EqualTo(picks));
            Assert.That(encounters.IsFirstEncounterPending, Is.False, "The last level-up chosen, the run begins.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            // Its enemies walk in wearing the floor's palette.
            deadline = Time.realtimeSinceStartup + 30f;
            Health themed = null;
            while (themed == null && Time.realtimeSinceStartup < deadline && !setup.Run.HasEnded)
            {
                themed = ThemedEnemy(encounters, theme);
                yield return null;
            }
            Assert.That(encounters.FloorNumber, Is.EqualTo(floorNumber));
            Assert.That(themed, Is.Not.Null, "An enemy of the floor wears its palette.");
        }

        // The first enemy of the floor's elite room's first wave.
        private static string EliteLeaderOf(FloorDefinition floor)
        {
            for (int i = 0; i < floor.RoomCount; i++)
                if (floor.RoomAt(i).Kind == RoomKind.Elite)
                    return floor.RoomAt(i).WaveAt(0).EnemyAt(0).Id;
            return null;
        }

        // A standing enemy with painted art on show in the floor's themed copy, or null.
        private static Health ThemedEnemy(EncounterController encounters, FloorTheme theme)
        {
            PropertyInfo shown = typeof(EnemyLookView).GetProperty("ShownTheme", Private);
            for (int i = 0; i < encounters.EnemyCount; i++)
            {
                Health enemy = encounters.EnemyAt(i);
                if (enemy == null || !enemy.IsAlive)
                    continue;
                var look = enemy.GetComponent<EnemyLookView>();
                if (look != null && look.PaintedArt != null && (FloorTheme)shown.GetValue(look) == theme)
                    return enemy;
            }
            return null;
        }
    }
}
