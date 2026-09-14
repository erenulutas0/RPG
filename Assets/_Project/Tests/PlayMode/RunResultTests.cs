using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    public sealed class RunResultTests
    {
        private CombatSetup _setup;
        private Health _hero;
        private AttackController _heroAttack;
        private EncounterController _encounters;
        private RunResultView _result;
        private Button _restart;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            FindSceneObjects();
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Result Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator ThePackStrikesTheHeroOnlyOnceInReach()
        {
            // The hero holds its swings, so both enemies live and every point of damage comes from their strikes.
            _heroAttack.enabled = false;
            AttackController gruntAttack = _encounters.WaveEnemyAt(0).GetComponent<AttackController>();
            AttackController miteAttack = _encounters.WaveEnemyAt(1).GetComponent<AttackController>();
            yield return new WaitForSeconds(1f);
            Assert.That(gruntAttack.AttackCount + miteAttack.AttackCount, Is.Zero, "Nothing strikes while walking in.");
            Assert.That(_hero.Current, Is.EqualTo(100f));

            float deadline = Time.realtimeSinceStartup + 10f;
            while (gruntAttack.AttackCount < 2 && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(gruntAttack.AttackCount, Is.EqualTo(2));
            Assert.That(miteAttack.AttackCount, Is.GreaterThanOrEqualTo(1), "The faster mite strikes first.");
            Assert.That(_hero.Current, Is.EqualTo(100f - 3.6f * gruntAttack.AttackCount - 0.6f * miteAttack.AttackCount).Within(1e-3f),
                "Each Grunt Strike deals the 3.6 authored in Weapon_GruntStrike.asset and each Mite Bite 0.6.");
            Assert.That(_setup.Run.HasEnded, Is.False);
            Assert.That(_result.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator GruntKillsAWoundedHeroAndTheResultExplainsWhy()
        {
            Health grunt = _encounters.WaveEnemyAt(0);
            AttackController gruntAttack = grunt.GetComponent<AttackController>();
            _hero.ApplyDamage(new DamageContext(95f));

            float deadline = Time.realtimeSinceStartup + 10f;
            while (_hero.IsAlive && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_hero.IsAlive, Is.False, "The pack must finish a hero left at 5 HP; the mite's bite cannot, the Grunt's second strike does.");
            Assert.That(grunt.IsAlive, Is.True, "The hero died to the Grunt, not after clearing it.");
            yield return null;

            Assert.That(_setup.Run.HasEnded, Is.True);
            Assert.That(_result.IsOpen, Is.True);
            Assert.That(Label("Result Title"), Is.EqualTo("Defeated"));
            Assert.That(Label("Cause Label"), Does.Contain("Grunt").And.Contain("Ember Hall"));
            Assert.That(Label("Progress Label"), Does.Contain("Floor 1").And.Contain("0 rooms"));
            Assert.That(Label("Result Gold Label"), Is.EqualTo("Gold banked: 0"));
            Assert.That(Label("Build Label"), Does.Contain("no upgrades"));
            Assert.That(Label("Forge Hint Label"), Is.EqualTo("Forge gold 0: Second Wind costs 80"));

            int gruntHits = gruntAttack.AttackCount;
            int heroHits = _heroAttack.AttackCount;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(gruntAttack.AttackCount, Is.EqualTo(gruntHits), "Nothing attacks a dead hero.");
            Assert.That(_heroAttack.AttackCount, Is.EqualTo(heroHits));
            Assert.That(_encounters.EncounterNumber, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ResultListsClearedEncountersAndTheBuild()
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (_setup.Upgrades.CurrentOffer == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            UpgradeOffer offer = _setup.Upgrades.CurrentOffer;
            Assert.That(offer, Is.Not.Null);
            for (int i = 0; i < offer.Choices.Count; i++)
            {
                if (offer.Choices[i].Stat == WeaponStat.Damage)
                    _setup.Upgrades.TrySelect(offer, i);
            }

            // The hero stops swinging so the mites of wave 2 cannot die and change the experience under test.
            _heroAttack.enabled = false;
            deadline = Time.realtimeSinceStartup + 6f;
            while (_encounters.EncounterNumber < 2 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_encounters.EncounterNumber, Is.EqualTo(2));
            // The first pack's Grunt struck last, after its mite fell; the new mites must walk in before one of them bites.
            deadline = Time.realtimeSinceStartup + 6f;
            while ((_setup.LastAttacker == null || _setup.LastAttacker.DisplayName != "Cinder Mite") && Time.realtimeSinceStartup < deadline)
                yield return null;

            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;

            Assert.That(_result.IsOpen, Is.True);
            Assert.That(Label("Cause Label"), Does.Contain("Cinder Mite").And.Contain("Ember Hall"), "The last enemy to hit the hero is named.");
            Assert.That(Label("Progress Label"), Does.Contain("0 rooms").And.Contain("Level 1").And.Contain("XP 11"));
            // The Grunt's 5 gold was never secured: half, rounded down, is lost.
            Assert.That(Label("Result Gold Label"), Is.EqualTo("Gold banked: 3  |  lost: 2"));
            Assert.That(Label("Build Label"), Does.Contain("Tempered Edge x1"));
        }

        [UnityTest]
        public IEnumerator TryAgainReloadsAFreshRunExactlyOnce()
        {
            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;
            Assert.That(_result.IsOpen, Is.True);

            int loads = 0;
            UnityAction<Scene, LoadSceneMode> countLoad = (scene, mode) => loads++;
            SceneManager.sceneLoaded += countLoad;
            try
            {
                Tap(_restart);
                yield return null;
                Assert.That(loads, Is.Zero, "Taps during the input delay are ignored.");

                yield return new WaitForSecondsRealtime(0.7f);
                CombatSetup previous = _setup;
                Tap(_restart);
                Tap(_restart);
                _restart.onClick.Invoke();

                float deadline = Time.realtimeSinceStartup + 5f;
                while (loads == 0 && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
                yield return new WaitForSecondsRealtime(0.5f);

                Assert.That(loads, Is.EqualTo(1));
                FindSceneObjects();
                Assert.That(previous == null, Is.True, "The ended run's objects are destroyed.");
                Assert.That(_setup.Run.HasEnded, Is.False);
                Assert.That(_setup.Run.Experience, Is.Zero);
                Assert.That(_setup.Run.Level, Is.Zero);
                Assert.That(_setup.Weapon.Damage, Is.EqualTo(10f));
                Assert.That(_hero.IsAlive, Is.True);
                Assert.That(_encounters.EncounterNumber, Is.EqualTo(1));
                Assert.That(_result.IsOpen, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                SceneManager.sceneLoaded -= countLoad;
            }
        }

        private void FindSceneObjects()
        {
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _heroAttack = _hero.GetComponent<AttackController>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _result = Object.FindFirstObjectByType<RunResultView>();
            _restart = _result.transform.Find("Result Panel/Safe Area/Restart Button").GetComponent<Button>();
        }

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
