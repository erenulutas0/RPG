using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class GateArrivalTests
    {
        private Scene _scene;

        [SetUp] public void Begin() => TestProfile.Begin();

        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1f;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Gate arrival cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }

        private IEnumerator LoadArrival()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.GateArrivalPath, "enabled");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene = SceneManager.GetActiveScene();
            Time.timeScale = 0f; // Prevent combat while the unscaled presentation is under test.
            yield return null;
            yield return null;
            yield return null; // Allow Start's layout yield to complete before checking captured view state.
        }

        private static void AssertControlsRestored()
        {
            Assert.That(Object.FindFirstObjectByType<HeroMovementInput>().enabled, Is.True);
            Assert.That(Camera.main.GetComponent<ArenaCameraFollow>().enabled, Is.True);
            bool visibleHud = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.isRootCanvas && canvas.enabled) visibleHud = true;
            Assert.That(visibleHud, Is.True, "The gameplay HUD must be visible after handback.");
        }

        [UnityTest] public IEnumerator DeferredEntryAndSkipStartExactlyOneEncounter()
        {
            yield return LoadArrival();
            var arrival = Object.FindFirstObjectByType<GateArrivalView>();
            var encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            Assert.That(arrival, Is.Not.Null);
            Assert.That(arrival.IsPresenting, Is.True);
            Assert.That(Object.FindFirstObjectByType<HeroMovementInput>().enabled, Is.False);
            Assert.That(Camera.main.GetComponent<ArenaCameraFollow>().enabled, Is.False);
            for (int i = 0; i < 3; i++)
            {
                yield return null;
                Assert.That(encounters.EncounterNumber, Is.Zero);
                Assert.That(encounters.AliveEnemyCount, Is.Zero);
                Assert.That(encounters.RoomNumber, Is.Zero);
            }
            arrival.Skip();
            yield return null;
            yield return null;
            Assert.That(arrival.IsPresenting, Is.False);
            AssertControlsRestored();
            Assert.That(Time.timeScale, Is.Zero, "Arrival must not overwrite someone else's time scale.");
            Assert.That(encounters.EncounterNumber, Is.EqualTo(1));
            Assert.That(encounters.AliveEnemyCount, Is.GreaterThan(0));
            int count = encounters.AliveEnemyCount;
            Health first = encounters.WaveEnemyAt(0);
            encounters.BeginFirstEncounter();
            arrival.Skip();
            encounters.BeginFirstEncounter();
            yield return null;
            Assert.That(encounters.EncounterNumber, Is.EqualTo(1));
            Assert.That(encounters.AliveEnemyCount, Is.EqualTo(count));
            Assert.That(encounters.WaveEnemyAt(0), Is.SameAs(first));
        }

        [UnityTest] public IEnumerator NaturalCompletionUsesUnscaledTimeAndRestoresControl()
        {
            yield return LoadArrival();
            var arrival = Object.FindFirstObjectByType<GateArrivalView>();
            float began = Time.realtimeSinceStartup;
            while (arrival.IsPresenting && Time.realtimeSinceStartup - began < 6f) yield return null;
            Assert.That(arrival.IsPresenting, Is.False, "The arrival must finish without player input.");
            Assert.That(Time.realtimeSinceStartup - began, Is.InRange(3f, 6f));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(Object.FindFirstObjectByType<CombatSetup>().Encounters.EncounterNumber, Is.EqualTo(1));
            AssertControlsRestored();
        }

        [UnityTest] public IEnumerator PriorSceneFrameDoesNotConsumeVisibleArrivalDuration()
        {
            yield return LoadArrival();
            Object.FindFirstObjectByType<GateArrivalView>().Skip();
            yield return null;
            yield return null;
            AssertControlsRestored();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Health first = setup.Encounters.WaveEnemyAt(0);
            var host = new GameObject("Arrival after an expensive scene frame");
            host.SetActive(false); // Step the actual presentation without a second engine-owned Start.
            try
            {
                var arrival = host.AddComponent<GateArrivalView>();
                arrival.Initialize(setup, Object.FindFirstObjectByType<HeroMovementInput>());
                var reveal = (IEnumerator)typeof(GateArrivalView).GetMethod("Start",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(arrival, null);
                Assert.That(reveal.MoveNext(), Is.True, "Start first yields for scene layout.");
                // Reproduce a loading frame before capture, rather than changing or mocking Unity time.
                System.Threading.Thread.Sleep(1200);
                yield return null;
                Assert.That(Time.unscaledDeltaTime, Is.GreaterThanOrEqualTo(1f),
                    "The regression must actually exercise an expensive preceding frame.");
                double began = Time.realtimeSinceStartupAsDouble;
                while (reveal.MoveNext() && Time.realtimeSinceStartupAsDouble - began < 6d)
                    yield return null;
                Assert.That(arrival.IsPresenting, Is.False);
                Assert.That(Time.realtimeSinceStartupAsDouble - began, Is.InRange(4d, 6d),
                    "Four unscaled seconds start at capture, not at the preceding loading frame.");
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(setup.Encounters.EncounterNumber, Is.EqualTo(1));
                Assert.That(setup.Encounters.WaveEnemyAt(0), Is.SameAs(first));
                AssertControlsRestored();
            }
            finally { Object.DestroyImmediate(host); }
        }

        [UnityTest] public IEnumerator DepthProofTakesPrecedenceOverArrival()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.DepthProofPath));
            File.WriteAllText(DevelopmentStart.DepthProofPath, "enabled");
            yield return LoadArrival();
            Assert.That(DevelopmentStart.GateArrival(), Is.False);
            Assert.That(Object.FindFirstObjectByType<GateArrivalView>(), Is.Null);
            Assert.That(Object.FindFirstObjectByType<CombatSetup>().Encounters.EncounterNumber, Is.GreaterThan(0));
            AssertControlsRestored();
        }

        [UnityTest] public IEnumerator UnloadingDuringArrivalDoesNotLeakIntoNextEntry()
        {
            yield return LoadArrival();
            var arrival = Object.FindFirstObjectByType<GateArrivalView>();
            Assert.That(arrival.IsPresenting, Is.True);
            Time.timeScale = 1f;
            SceneManager.SetActiveScene(SceneManager.CreateScene("Gate arrival unload"));
            yield return SceneManager.UnloadSceneAsync(_scene);
            Assert.That(arrival == null, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "CombatSetup's normal unload cleanup must remain effective.");
            File.Delete(DevelopmentStart.GateArrivalPath);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene = SceneManager.GetActiveScene();
            Time.timeScale = 0f;
            yield return null;
            Assert.That(Object.FindFirstObjectByType<GateArrivalView>(), Is.Null);
            Assert.That(Object.FindFirstObjectByType<CombatSetup>().Encounters.EncounterNumber, Is.EqualTo(1));
            AssertControlsRestored();
        }

        [UnityTest] public IEnumerator DisableRestoresViewAndReenableCompletesOnlyOnce()
        {
            yield return LoadArrival();
            var arrival = Object.FindFirstObjectByType<GateArrivalView>();
            var encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            arrival.enabled = false;
            Assert.That(arrival.IsPresenting, Is.False);
            AssertControlsRestored();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(encounters.EncounterNumber, Is.Zero, "Disabling must stop the running coroutine, not spawn in its background.");
            Assert.That(encounters.AliveEnemyCount, Is.Zero);
            arrival.enabled = true;
            yield return null;
            yield return null;
            Assert.That(arrival.IsPresenting, Is.False);
            AssertControlsRestored();
            Assert.That(encounters.EncounterNumber, Is.EqualTo(1));
            Health first = encounters.WaveEnemyAt(0);
            arrival.enabled = false;
            arrival.enabled = true;
            yield return null;
            yield return null;
            Assert.That(encounters.EncounterNumber, Is.EqualTo(1));
            Assert.That(encounters.WaveEnemyAt(0), Is.SameAs(first));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest] public IEnumerator DisableBeforeStartDoesNotRecaptureRestoredControls()
        {
            yield return LoadArrival();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Object.FindFirstObjectByType<GateArrivalView>().Skip();
            yield return null;
            yield return null;
            AssertControlsRestored();
            Health first = setup.Encounters.WaveEnemyAt(0);
            var host = new GameObject("Arrival interrupted before Start");
            try
            {
                var arrival = host.AddComponent<GateArrivalView>();
                arrival.Initialize(setup, Object.FindFirstObjectByType<HeroMovementInput>());
                // These callbacks occur synchronously, before Unity can invoke this new component's Start.
                arrival.enabled = false;
                arrival.enabled = true;
                for (int i = 0; i < 4; i++) yield return null;
                Assert.That(arrival.IsPresenting, Is.False);
                AssertControlsRestored();
                Assert.That(setup.Encounters.EncounterNumber, Is.EqualTo(1));
                Assert.That(setup.Encounters.WaveEnemyAt(0), Is.SameAs(first));
                Assert.That(Time.timeScale, Is.Zero);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [UnityTest] public IEnumerator ApplicationPauseSkipsWithoutResumingExistingPause()
        {
            yield return LoadArrival();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var arrival = Object.FindFirstObjectByType<GateArrivalView>();
            Assert.That(setup.Pause.TryPause(), Is.True);
            arrival.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
            yield return null;
            yield return null;
            Assert.That(arrival.IsPresenting, Is.False);
            AssertControlsRestored();
            Assert.That(Object.FindFirstObjectByType<PauseView>().IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero, "Arrival handback must not dismiss a pause owned by the run.");
            Assert.That(setup.Encounters.EncounterNumber, Is.EqualTo(1));
            Assert.That(setup.Pause.TryResume(), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test] public void BeginBeforeInitializationIsRejected()
        {
            var host = new GameObject("Uninitialized encounter");
            try
            {
                var encounters = host.AddComponent<EncounterController>();
                Assert.Throws<InvalidOperationException>(() => encounters.BeginFirstEncounter());
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
