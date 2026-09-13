using System;
using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // Plays the Descent in the real scene the way DescentSimulation plays it, with game time stepping exactly 1/60 s per
    // frame: the same upgrade card every time, Mend or Temper at the first forge, Mend later, Descend at the checkpoint.
    // Every balance claim rests on the simulation, so the scene must reach the same result; a gap means the simulation
    // misses scene behaviour, as it once missed the advance delay between waves. A whole Descent takes about a second.
    public sealed class SimulationParityTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // Both sides reach identical health today; the margin only absorbs a reordering of hits inside one frame.
        private const float HealthTolerance = 1f;
        private int _previousFrameRate;

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            Application.targetFrameRate = _previousFrameRate;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Parity Test Cleanup");
            SceneManager.SetActiveScene(empty);
            if (gameplay.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator SwordDamageFirstMendingDescentMatchesTheSimulation() =>
            PlayDescent(null, DescentSimulation.Sword(), 0, true, null);

        [UnityTest]
        public IEnumerator StaffDamageFirstMendingDescentMatchesTheSimulation() =>
            PlayDescent("weapon_staff", DescentSimulation.Staff(), 0, true, null);

        [UnityTest]
        public IEnumerator DaggersDamageFirstMendingDescentMatchesTheSimulation() =>
            PlayDescent("weapon_daggers", DescentSimulation.Daggers(), 0, true, null);

        [UnityTest]
        public IEnumerator SwordSpeedFirstTemperDeathMatchesTheSimulation() =>
            PlayDescent(null, DescentSimulation.Sword(), 1, false, null);

        [UnityTest]
        public IEnumerator SwordTemperWithCounterweightMatchesTheSimulation() =>
            PlayDescent(null, DescentSimulation.Sword(), 0, false, DescentSimulation.Counterweight());

        [UnityTest]
        public IEnumerator DaggersTemperWithSecondWindMatchesTheSimulation() =>
            PlayDescent("weapon_daggers", DescentSimulation.Daggers(), 0, false, DescentSimulation.SecondWind());

        [UnityTest]
        public IEnumerator StaffSpeedFirstTemperWithCounterweightDeathMatchesTheSimulation() =>
            PlayDescent("weapon_staff", DescentSimulation.Staff(), 1, false, DescentSimulation.Counterweight());

        private IEnumerator PlayDescent(string weaponId, DescentSimulation.HeroWeapon weapon, int cardSlot, bool mendOnFloorOne,
            RelicOption relic)
        {
            DescentSimulation.Result expected = DescentSimulation.Run(
                new[] { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults }, cardSlot, mendOnFloorOne, relic, weapon);

            TestProfile.Begin(new PlayerProfile(0, relic != null ? new[] { relic.Id } : null, relic?.Id, 0,
                weaponId != null ? new[] { weaponId } : null, weaponId));
            _previousFrameRate = Application.targetFrameRate;
            Time.timeScale = 1f;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            // Game time keeps its fixed step, so frames may run as fast as the Editor renders them.
            Application.targetFrameRate = -1;

            var setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            var hero = GameObject.Find("Vanguard").GetComponent<Health>();
            var encounters = Object.FindFirstObjectByType<EncounterController>();
            int kills = 0;
            Action<Health> countKill = enemy => kills++;
            encounters.EnemyDefeated += countKill;
            float healthAfterFloorOne = -1f;
            float deadline = Time.realtimeSinceStartup + 180f;
            try
            {
                while (!setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
                {
                    ChoicePrompt prompt = setup.Choices.Current;
                    if (prompt != null)
                        setup.Choices.TrySelect(prompt, SlotFor(prompt, cardSlot, mendOnFloorOne, encounters.FloorNumber, hero, ref healthAfterFloorOne));
                    yield return null;
                }
            }
            finally
            {
                encounters.EnemyDefeated -= countKill;
            }

            string sceneDeath = setup.Run.Outcome == RunOutcome.Defeat ? $"{encounters.RoomNumber}.{encounters.WaveNumber}" : null;
            string report = $"scene: {setup.Run.Outcome}, floor {encounters.FloorNumber}, death {sceneDeath ?? "-"}, " +
                $"{healthAfterFloorOne:0.#} HP after floor 1, {hero.Current:0.#} HP at the end, {kills} kills, gold {setup.Run.Gold} " +
                $"({setup.Run.GoldBanked} banked), {setup.Run.UpgradesApplied} upgrades, relic x{setup.Relic?.Triggers ?? 0}; " +
                $"simulation: {expected.ClearedFloors} floors cleared, death {expected.DeathRoom ?? "-"}, {expected.HealthAfterFloorOne:0.#}, " +
                $"{expected.HeroHealth:0.#}, {expected.Kills}, {expected.Gold} ({expected.GoldBanked}), {expected.UpgradesApplied}, " +
                $"x{expected.RelicTriggers}";
            TestContext.WriteLine(report);

            Assert.That(setup.Run.HasEnded, Is.True, "The Descent did not finish. " + report);
            bool expectedVictory = expected.ClearedFloors == 2;
            Assert.That(setup.Run.Outcome, Is.EqualTo(expectedVictory ? RunOutcome.Victory : RunOutcome.Defeat), report);
            if (!expectedVictory)
            {
                Assert.That(encounters.FloorNumber, Is.EqualTo(expected.ClearedFloors + 1), report);
                Assert.That(sceneDeath, Is.EqualTo(expected.DeathRoom), report);
            }
            Assert.That(kills, Is.EqualTo(expected.Kills), report);
            Assert.That(setup.Run.Gold, Is.EqualTo(expected.Gold), report);
            Assert.That(setup.Run.GoldBanked, Is.EqualTo(expected.GoldBanked), report);
            Assert.That(setup.Run.UpgradesApplied, Is.EqualTo(expected.UpgradesApplied), report);
            Assert.That(setup.Relic?.Triggers ?? 0, Is.EqualTo(expected.RelicTriggers), report);
            Assert.That(healthAfterFloorOne, Is.EqualTo(expected.HealthAfterFloorOne).Within(HealthTolerance), report);
            Assert.That(hero.Current, Is.EqualTo(expected.HeroHealth).Within(HealthTolerance), report);
            LogAssert.NoUnexpectedReceived();
        }

        // The simulation's choices: its upgrade card (or the last card left), Mend or Temper at the first forge, Mend on later
        // floors, and Descend at the checkpoint, where the health after floor 1 is recorded.
        private static int SlotFor(ChoicePrompt prompt, int cardSlot, bool mendOnFloorOne, int floorNumber, Health hero,
            ref float healthAfterFloorOne)
        {
            switch (prompt.Kind)
            {
                case ChoiceKind.Upgrade:
                    return Math.Min(cardSlot, prompt.Cards.Count - 1);
                case ChoiceKind.Forge:
                    return floorNumber > 1 || mendOnFloorOne ? 0 : 1;
                default:
                    healthAfterFloorOne = hero.Current;
                    return 1;
            }
        }
    }
}
