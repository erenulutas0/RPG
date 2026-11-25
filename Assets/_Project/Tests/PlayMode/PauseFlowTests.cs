using System.Collections;
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
    public sealed class PauseFlowTests
    {
        private CombatSetup _setup;
        private Health _hero;
        private AttackController _heroAttack;
        private EncounterController _encounters;
        private PauseView _pause;
        private Button _pauseButton;
        private Button _resumeButton;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            // Views bind their buttons in Start, which runs before the first frame after the load.
            yield return null;
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            // The first pack has to level the hero on its own here, as it did before drops (docs/37).
            DropVacuum.Attach(_setup);
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _heroAttack = _hero.GetComponent<AttackController>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _pause = Object.FindFirstObjectByType<PauseView>();
            _pauseButton = GameObject.Find("Pause Button").GetComponent<Button>();
            _resumeButton = _pause.transform.Find("Pause Panel/Safe Area/Resume Button").GetComponent<Button>();
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            CombatSetup.FocusLossPausesOverride = null;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Pause Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator PauseButtonFreezesCombatUntilResume()
        {
            Health grunt = _encounters.WaveEnemyAt(0);
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(_pause.IsOpen, Is.False);

            Tap(_pauseButton);
            int heroHits = _heroAttack.AttackCount;
            float gruntHealth = grunt.Current;
            float heroHealth = _hero.Current;
            Vector3 gruntPosition = grunt.transform.position;
            Assert.That(_pause.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(Label("Pause Title"), Is.EqualTo("Paused"));

            Tap(_resumeButton);
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_pause.IsOpen, Is.True, "A tap as the overlay appears does not resume.");
            Assert.That(_heroAttack.AttackCount, Is.EqualTo(heroHits), "Nothing attacks while paused.");
            Assert.That(grunt.Current, Is.EqualTo(gruntHealth));
            Assert.That(grunt.transform.position, Is.EqualTo(gruntPosition), "Nothing walks while paused.");
            Assert.That(_hero.Current, Is.EqualTo(heroHealth));

            Tap(_resumeButton);
            Assert.That(_pause.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.True);
            yield return new WaitForSeconds(0.5f);
            Assert.That(grunt.transform.position.y, Is.LessThan(gruntPosition.y), "The pack walks on after resuming.");
            float deadline = Time.realtimeSinceStartup + 8f;
            while (_heroAttack.AttackCount == heroHits && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_heroAttack.AttackCount, Is.GreaterThan(heroHits), "Combat continues after resuming.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LeavingTheAppPausesTheRunButAnOpenChoiceStaysInCharge()
        {
            _setup.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(_pause.IsOpen, Is.True, "Returning to the app finds the run paused.");
            Assert.That(Time.timeScale, Is.Zero);
            _setup.gameObject.SendMessage("OnApplicationPause", false);
            Assert.That(_pause.IsOpen, Is.True, "Coming back does not resume by itself.");

            yield return new WaitForSecondsRealtime(0.4f);
            Tap(_resumeButton);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            float deadline = Time.realtimeSinceStartup + 15f;
            while (_setup.Choices.Current == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_setup.Choices.Current, Is.Not.Null, "Clearing the first pack opens an upgrade choice.");
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.False, "The choice already holds the run.");

            _setup.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(_pause.IsOpen, Is.False, "No pause overlay on top of a choice.");
            Assert.That(Time.timeScale, Is.Zero);

            _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.True);
        }

        // On a phone the notification shade, the power or volume dialog, an assistant overlay or split screen take the focus
        // without pausing the app: the run went on at full speed with the stick's touch cancelled (the run flow's review,
        // 2026-11-26). Losing focus pauses it there, and regaining it resumes nothing. The Editor and the batch tests lose
        // focus for other reasons, so only a mobile platform does (CombatSetup.FocusLossPausesOverride stands in for it).
        [UnityTest]
        public IEnumerator LosingFocusOnAPhonePausesTheRunAndRegainingItResumesNothing()
        {
            CombatSetup.FocusLossPausesOverride = false;
            _setup.gameObject.SendMessage("OnApplicationFocus", false);
            Assert.That(_pause.IsOpen, Is.False, "Off a phone, losing focus pauses nothing.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            CombatSetup.FocusLossPausesOverride = true;
            _setup.gameObject.SendMessage("OnApplicationFocus", false);
            Assert.That(_pause.IsOpen, Is.True, "On a phone, the shade pulled down finds the run paused.");
            Assert.That(_setup.Pause.IsPlayerPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            _setup.gameObject.SendMessage("OnApplicationFocus", true);
            Assert.That(_pause.IsOpen, Is.True, "Regaining focus does not resume by itself.");
            Assert.That(Time.timeScale, Is.Zero);

            yield return new WaitForSecondsRealtime(0.6f);
            Tap(_resumeButton);
            Assert.That(_pause.IsOpen, Is.False, "The player resumes.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            LogAssert.NoUnexpectedReceived();
        }

        // The overlay's input delay was started as the app left and ran out in the background, so the tap that brings the
        // player back (a call's end button) resumed at once (the run flow's review, 2026-11-26). Back in the app with the
        // overlay open, the delay runs again, from OnApplicationPause(false) and from OnApplicationFocus(true).
        [UnityTest]
        public IEnumerator ComingBackToTheAppHoldsTheResumeButtonForItsDelayAgain()
        {
            _setup.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(_pause.IsOpen, Is.True);
            // The time away: the delay the overlay started as the app left has run out.
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(_resumeButton.interactable, Is.True);

            _setup.gameObject.SendMessage("OnApplicationPause", false);
            _pause.SendMessage("OnApplicationPause", false);
            Assert.That(_resumeButton.interactable, Is.False, "Back in the app, Resume waits its delay again.");
            Tap(_resumeButton);
            yield return null;
            Tap(_resumeButton);
            Assert.That(_pause.IsOpen, Is.True, "The tap that brings the player back does not resume.");
            Assert.That(Time.timeScale, Is.Zero);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(_resumeButton.interactable, Is.True, "Then it answers.");

            _pause.SendMessage("OnApplicationFocus", true);
            Assert.That(_resumeButton.interactable, Is.False, "Focus regained holds it too.");
            Tap(_resumeButton);
            Assert.That(_pause.IsOpen, Is.True);
            yield return new WaitForSecondsRealtime(0.6f);
            Tap(_resumeButton);
            Assert.That(_pause.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            _pause.SendMessage("OnApplicationFocus", true);
            Assert.That(_pause.IsOpen, Is.False, "With the overlay shut, coming back changes nothing.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            LogAssert.NoUnexpectedReceived();
        }

        // Flow tests click buttons directly, which bypasses raycasting. On a device a touch only reaches a button whose
        // root canvas has a GraphicRaycaster; the HUD canvas once lacked one, so the pause button ignored real touches.
        [UnityTest]
        public IEnumerator EveryButtonSitsOnACanvasThatReceivesTouches()
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(buttons, Is.Not.Empty);
            foreach (Button button in buttons)
            {
                Canvas root = button.GetComponentInParent<Canvas>(true).rootCanvas;
                Assert.That(root.GetComponent<GraphicRaycaster>(), Is.Not.Null,
                    $"{button.name} is on {root.name}, which has no GraphicRaycaster.");
            }
            yield break;
        }

        // The pause button's label was copied from the taller Try again button and floated above the smaller button.
        [UnityTest]
        public IEnumerator EveryButtonLabelSitsInsideItsButton()
        {
            const float tolerance = 0.5f;
            var buttonCorners = new Vector3[4];
            var labelCorners = new Vector3[4];
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ((RectTransform)button.transform).GetWorldCorners(buttonCorners);
                foreach (Text label in button.GetComponentsInChildren<Text>(true))
                {
                    label.rectTransform.GetWorldCorners(labelCorners);
                    bool inside = labelCorners[0].x >= buttonCorners[0].x - tolerance && labelCorners[0].y >= buttonCorners[0].y - tolerance &&
                                  labelCorners[2].x <= buttonCorners[2].x + tolerance && labelCorners[2].y <= buttonCorners[2].y + tolerance;
                    Assert.That(inside, Is.True, $"{label.name} extends outside {button.name}.");
                }
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator PauseButtonHidesWhenTheRunEnds()
        {
            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;

            Assert.That(_setup.Run.HasEnded, Is.True);
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(_setup.Pause.TryPause(), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
