using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry floor's gate opens on its clock (FloorDefinition.FoundryGateNotBeforeSeconds; V0.1, the owner
    // 2026-11-02): the cleared room before the gate boss holds, the floor clock saying when the gate opens, until the
    // clock reaches the floor's time; then the gate boss's room begins. The proof floor's gate waits for 0:12.
    public sealed class GateHoldFlowTests
    {
        private const float GateSeconds = 12f;

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_gate_hold_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Gate hold cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // Offers answered, the hero kept standing, the hall's wave felled as it comes.
        private static void Keep(CombatSetup setup, Health hero)
        {
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            if (hero.IsAlive)
                hero.Heal(hero.Maximum);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheClearedRoomBeforeTheGateHoldsForTheFloorsClock()
        {
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounters = setup.Encounters;
            Health hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            FloorClockView clock = Object.FindFirstObjectByType<RunHud>().FloorClock;
            Assert.That(encounters.Floor.FoundryGateNotBeforeSeconds, Is.EqualTo(GateSeconds));

            float deadline = Time.realtimeSinceStartup + 20f;
            while (!encounters.IsHoldingForGate && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero);
                if (encounters.CurrentRoom != null && encounters.CurrentRoom.Kind == RoomKind.Combat)
                    PackTestUtility.KillWave(encounters);
                yield return null;
            }
            Assert.That(encounters.IsHoldingForGate, Is.True, "The cleared hall holds for the gate.");
            Assert.That(encounters.FloorClockSeconds, Is.LessThan(GateSeconds));
            Assert.That(encounters.RoomNumber, Is.EqualTo(1), "Still the hall.");
            Assert.That(encounters.GateBoss, Is.Null, "No gate boss yet.");
            Assert.That(encounters.GateOpensIn, Is.EqualTo(GateSeconds - encounters.FloorClockSeconds).Within(.001f));
            yield return null;
            Assert.That(clock.IsGateShown, Is.True, "The floor clock says when the gate opens.");
            Assert.That(clock.ShownText, Does.StartWith("Gate opens in 0:"));

            deadline = Time.realtimeSinceStartup + 30f;
            while (encounters.IsHoldingForGate && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero);
                yield return null;
            }
            Assert.That(encounters.IsHoldingForGate, Is.False);
            Assert.That(encounters.FloorClockSeconds, Is.GreaterThanOrEqualTo(GateSeconds), "Not before the floor's time.");
            Assert.That(encounters.RoomNumber, Is.EqualTo(2), "Then the gate boss's room begins.");
            Assert.That(clock.IsGateShown, Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
