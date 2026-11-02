using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
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
    // clock reaches the floor's time; then the gate boss's room begins. The proof floor's gate waits for 0:12. The hold
    // ends with the hero, and an overrun floor's clock says so over the gate's count.
    public sealed class GateHoldFlowTests
    {
        private const float GateSeconds = 12f;

        [UnitySetUp]
        public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
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

        // The proof floor on the foundry, its clock started at clockStart seconds when given.
        private static IEnumerator Load(float clockStart = 0f)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_gate_hold_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            if (clockStart > 0f)
                File.WriteAllText(DevelopmentStart.FloorClockPath, clockStart.ToString(CultureInfo.InvariantCulture));
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
        }

        // Offers answered, the hero kept standing, the hall's wave felled as it comes.
        private static void Keep(CombatSetup setup, Health hero)
        {
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            if (hero.IsAlive)
                hero.Heal(hero.Maximum);
        }

        // The hall felled until its cleared room holds for the gate.
        private static IEnumerator UntilHolding(CombatSetup setup, Health hero)
        {
            EncounterController encounters = setup.Encounters;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!encounters.IsHoldingForGate && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero);
                if (encounters.CurrentRoom != null && encounters.CurrentRoom.Kind == RoomKind.Combat)
                    PackTestUtility.KillWave(encounters);
                yield return null;
            }
            Assert.That(encounters.IsHoldingForGate, Is.True, "The cleared hall holds for the gate.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheClearedRoomBeforeTheGateHoldsForTheFloorsClock()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounters = setup.Encounters;
            Health hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            FloorClockView clock = Object.FindFirstObjectByType<RunHud>().FloorClock;
            Assert.That(encounters.Floor.FoundryGateNotBeforeSeconds, Is.EqualTo(GateSeconds));

            yield return UntilHolding(setup, hero);
            Assert.That(encounters.FloorClockSeconds, Is.LessThan(GateSeconds));
            Assert.That(encounters.RoomNumber, Is.EqualTo(1), "Still the hall.");
            Assert.That(encounters.GateBoss, Is.Null, "No gate boss yet.");
            Assert.That(encounters.GateOpensIn, Is.EqualTo(GateSeconds - encounters.FloorClockSeconds).Within(.001f));
            yield return null;
            Assert.That(clock.IsGateShown, Is.True, "The floor clock says when the gate opens.");
            Assert.That(clock.IsOverrunShown, Is.False);
            Assert.That(clock.ShownText, Does.StartWith("Gate opens in 0:"));

            float deadline = Time.realtimeSinceStartup + 30f;
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

        // A hero who falls while the room holds leaves no hold behind: nothing reads a gate about to open on a run that
        // has ended.
        [UnityTest, Timeout(90000)]
        public IEnumerator TheHoldEndsWhenTheHeroFalls()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounters = setup.Encounters;
            Health hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            FloorClockView clock = Object.FindFirstObjectByType<RunHud>().FloorClock;

            yield return UntilHolding(setup, hero);
            Assert.That(encounters.GateOpensIn, Is.GreaterThan(0f));
            hero.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(hero.IsAlive, Is.False);
            Assert.That(encounters.IsHoldingForGate, Is.False, "The hold ends with the hero, at once.");
            Assert.That(encounters.GateOpensIn, Is.Zero);
            yield return null;
            Assert.That(setup.Run.HasEnded, Is.True);
            Assert.That(encounters.IsHoldingForGate, Is.False);
            Assert.That(clock.IsGateShown, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        // The overrun's red count and beat win over the gate's: a floor can only hold before its gate time, which validation
        // keeps inside the clock, so the hold is set here by hand while the floor runs into its overrun.
        [UnityTest, Timeout(90000)]
        public IEnumerator AnOverrunFloorSaysSoOverTheGatesCount()
        {
            yield return Load(HordeClock.TargetSeconds - 3f);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounters = setup.Encounters;
            Health hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            FloorClockView clock = Object.FindFirstObjectByType<RunHud>().FloorClock;
            // The hall's wave stands, so the floor never moves on and ends the hold.
            hero.GetComponent<AttackController>().enabled = false;
            FieldInfo holding = typeof(EncounterController).GetField("_holdingForGate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(holding, Is.Not.Null);

            float deadline = Time.realtimeSinceStartup + 20f;
            while (!encounters.IsFloorOvertime && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero);
                holding.SetValue(encounters, true);
                yield return null;
            }
            Assert.That(encounters.IsFloorOvertime, Is.True, "The floor runs into its overrun.");
            holding.SetValue(encounters, true);
            yield return null;
            Assert.That(encounters.IsHoldingForGate, Is.True);
            Assert.That(clock.IsOverrunShown, Is.True, "The overrun is what the clock says.");
            Assert.That(clock.IsGateShown, Is.False);
            Assert.That(clock.ShownText, Does.StartWith("OVERRUN +"));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
