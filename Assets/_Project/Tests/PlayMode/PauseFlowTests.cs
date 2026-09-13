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
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Pause Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator PauseButtonFreezesCombatUntilResume()
        {
            Health grunt = _encounters.CurrentEnemy;
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(_pause.IsOpen, Is.False);

            Tap(_pauseButton);
            int heroHits = _heroAttack.AttackCount;
            float gruntHealth = grunt.Current;
            float heroHealth = _hero.Current;
            Assert.That(_pause.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(Label("Pause Title"), Is.EqualTo("Paused"));

            Tap(_resumeButton);
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_pause.IsOpen, Is.True, "A tap as the overlay appears does not resume.");
            Assert.That(_heroAttack.AttackCount, Is.EqualTo(heroHits), "Nothing attacks while paused.");
            Assert.That(grunt.Current, Is.EqualTo(gruntHealth));
            Assert.That(_hero.Current, Is.EqualTo(heroHealth));

            Tap(_resumeButton);
            Assert.That(_pause.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.True);
            yield return new WaitForSeconds(1f);
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

            float deadline = Time.realtimeSinceStartup + 8f;
            while (_setup.Choices.Current == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_setup.Choices.Current, Is.Not.Null, "The first kill opens an upgrade choice.");
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.False, "The choice already holds the run.");

            _setup.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(_pause.IsOpen, Is.False, "No pause overlay on top of a choice.");
            Assert.That(Time.timeScale, Is.Zero);

            _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_pauseButton.gameObject.activeInHierarchy, Is.True);
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
