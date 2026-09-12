using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class CombatSceneTests
    {
        private Health _hero;
        private Health _enemy;
        private AttackController _attack;
        private Targeting _targeting;
        private CombatSetup _setup;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _enemy = GameObject.Find("Grunt").GetComponent<Health>();
            _attack = _hero.GetComponent<AttackController>();
            _targeting = _hero.GetComponent<Targeting>();
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Combat Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
        }

        [UnityTest]
        public IEnumerator AuthoredSceneAutomaticallyKillsGruntExactlyOnce()
        {
            int deaths = 0;
            _enemy.Died += () => deaths++;
            Assert.That(_setup.Run.Experience, Is.Zero, "A fresh session starts with no experience.");
            float deadline = Time.realtimeSinceStartup + 8f;
            while (_enemy.IsAlive && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(_enemy.IsAlive, Is.False, "Grunt must die without input within eight seconds.");
            Assert.That(_enemy.Current, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(_attack.AttackCount, Is.EqualTo(5));
            Assert.That(_hero.Current, Is.EqualTo(100f));
            Assert.That(_enemy.GetComponentInChildren<SpriteRenderer>(true).enabled, Is.False);
            Assert.That(_targeting.Acquire(3f), Is.Null);
            // PrototypeEconomy.asset configures 10 XP per kill, matching the five-hit balance fixture.
            Assert.That(_setup.Run.Experience, Is.EqualTo(10));
            // The kill opens an upgrade choice that pauses scaled time, so wait in real time.
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_attack.AttackCount, Is.EqualTo(5));
            Assert.That(_setup.Run.Experience, Is.EqualTo(10));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ExperienceIsAwardedOnlyOnDeathAndNeverTwice()
        {
            int awards = 0;
            _setup.Run.ExperienceChanged += () => awards++;
            yield return new WaitForSeconds(1f);
            Assert.That(_enemy.IsAlive, Is.True, "Two hits cannot kill a 50 HP Grunt.");
            Assert.That(_setup.Run.Experience, Is.Zero);

            _enemy.ApplyDamage(new DamageContext(1000f));
            _enemy.ApplyDamage(new DamageContext(1000f));
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_enemy.IsAlive, Is.False);
            Assert.That(_setup.Run.Experience, Is.EqualTo(10));
            Assert.That(awards, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OutOfRangeAndDisabledTargetsAreIgnoredThenReacquired()
        {
            _enemy.transform.position = new Vector3(20f, 20f, 0f);
            float health = _enemy.Current;
            yield return new WaitForSeconds(1f);
            Assert.That(_targeting.Acquire(3f), Is.Null);
            Assert.That(_enemy.Current, Is.EqualTo(health));

            _enemy.transform.position = new Vector3(0f, 1.2f, 0f);
            _enemy.gameObject.SetActive(false);
            yield return new WaitForSeconds(1f);
            Assert.That(_targeting.Acquire(3f), Is.Null);
            Assert.That(_enemy.Current, Is.EqualTo(health));

            _enemy.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            Assert.That(_enemy.Current, Is.LessThan(health));
        }

        [UnityTest]
        public IEnumerator DestroyedTargetDoesNotCauseMissingReferenceErrors()
        {
            Object.Destroy(_enemy.gameObject);
            yield return null;
            int hits = _attack.AttackCount;
            yield return new WaitForSeconds(1f);
            Assert.That(_targeting.Acquire(3f), Is.Null);
            Assert.That(_attack.AttackCount, Is.EqualTo(hits));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PauseFreezesAttacksAndResumeContinues()
        {
            Time.timeScale = 0f;
            int hits = _attack.AttackCount;
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_attack.AttackCount, Is.EqualTo(hits));
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1f);
            Assert.That(_attack.AttackCount, Is.GreaterThan(hits));
        }

        [UnityTest]
        public IEnumerator DeadHeroStopsAttacking()
        {
            _hero.ApplyDamage(new DamageContext(1000f));
            int hits = _attack.AttackCount;
            yield return new WaitForSeconds(1f);
            Assert.That(_attack.AttackCount, Is.EqualTo(hits));
        }
    }
}
