using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class EncounterSequenceTests
    {
        private CombatSetup _setup;
        private Health _hero;
        private EncounterController _encounters;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Sequence Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
        }

        [UnityTest]
        public IEnumerator EncountersFollowTheAuthoredGruntRunnerGruntTankOrder()
        {
            string[] names = { "Grunt", "Runner", "Grunt", "Tank", "Grunt" };
            float[] health = { 50f, 30f, 50f, 120f, 50f };
            for (int i = 0; i < names.Length; i++)
            {
                yield return WaitForEncounter(i + 1);
                Health enemy = _encounters.CurrentEnemy;
                Assert.That(_encounters.CurrentDefinition.DisplayName, Is.EqualTo(names[i]));
                Assert.That(enemy.name, Is.EqualTo(names[i]), "Each archetype spawns from its own prefab.");
                Assert.That(enemy.Maximum, Is.EqualTo(health[i]));
                Assert.That(Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

                enemy.ApplyDamage(new DamageContext(10000f));
                yield return ChooseAllOffers();
            }

            Assert.That(_setup.Run.Experience, Is.EqualTo(50), "Each of the five kills rewards exactly once.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RunnerChipsTheHeroQuicklyAndDiesInTwoUpgradedHits()
        {
            yield return WaitForEncounter(1);
            _encounters.CurrentEnemy.ApplyDamage(new DamageContext(10000f));
            yield return ChooseAllOffers();
            yield return WaitForEncounter(2);

            AttackController runnerAttack = _encounters.CurrentEnemy.GetComponent<AttackController>();
            float heroBefore = _hero.Current;
            yield return WaitForClear();

            Assert.That(_encounters.CurrentDefinition.DisplayName, Is.EqualTo("Runner"));
            Assert.That(_encounters.HitsTaken, Is.EqualTo(2), "30 HP falls to two 15-damage hits.");
            Assert.That(runnerAttack.AttackCount, Is.InRange(2, 3), "Strikes every 0.4 s until the 0.8 s kill.");
            Assert.That(_hero.Current, Is.EqualTo(heroBefore - 2f * runnerAttack.AttackCount));
        }

        [UnityTest]
        public IEnumerator TankWindsUpBeforeItsFirstSlam()
        {
            for (int encounter = 1; encounter <= 3; encounter++)
            {
                yield return WaitForEncounter(encounter);
                _encounters.CurrentEnemy.ApplyDamage(new DamageContext(10000f));
                yield return ChooseAllOffers();
            }
            yield return WaitForEncounter(4);

            Assert.That(_encounters.CurrentDefinition.DisplayName, Is.EqualTo("Tank"));
            AttackController tankAttack = _encounters.CurrentEnemy.GetComponent<AttackController>();
            float heroBefore = _hero.Current;

            yield return new WaitForSeconds(1.2f);
            Assert.That(tankAttack.AttackCount, Is.Zero, "The 1.5 s windup holds the first slam.");
            Assert.That(_hero.Current, Is.EqualTo(heroBefore));

            float deadline = Time.realtimeSinceStartup + 3f;
            while (tankAttack.AttackCount == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(tankAttack.AttackCount, Is.EqualTo(1));
            Assert.That(_encounters.Elapsed, Is.GreaterThanOrEqualTo(1.4f));
            Assert.That(_hero.Current, Is.EqualTo(heroBefore - 12f));
        }

        private IEnumerator WaitForEncounter(int number)
        {
            float deadline = Time.realtimeSinceStartup + 6f;
            while (_encounters.EncounterNumber < number && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_encounters.EncounterNumber, Is.EqualTo(number));
        }

        private IEnumerator WaitForClear()
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!_encounters.IsCleared && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_encounters.IsCleared, Is.True);
        }

        // Takes the first card of every pending offer through the service; card UI is covered by UpgradeFlowTests.
        private IEnumerator ChooseAllOffers()
        {
            yield return null;
            while (_setup.Upgrades.CurrentOffer != null)
                _setup.Upgrades.TrySelect(_setup.Upgrades.CurrentOffer, 0);
        }
    }
}
