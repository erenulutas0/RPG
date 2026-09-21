using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The first wave of Floor_EmberHalls is a Grunt with a Cinder Mite beside it; both walk in from the far end of the arena.
    public sealed class CombatSceneTests
    {
        private Health _hero;
        private Health _enemy;
        private Health _mite;
        private AttackController _attack;
        private Targeting _targeting;
        private EncounterController _encounters;
        private CombatSetup _setup;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _enemy = _encounters.WaveEnemyAt(0);
            _mite = _encounters.WaveEnemyAt(1);
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
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator AuthoredSceneAutomaticallyClearsTheFirstPackKillingEachEnemyOnce()
        {
            int gruntDeaths = 0;
            int miteDeaths = 0;
            _enemy.Died += () => gruntDeaths++;
            _mite.Died += () => miteDeaths++;
            Assert.That(_encounters.DefinitionOf(_enemy).DisplayName, Is.EqualTo("Grunt"));
            Assert.That(_encounters.DefinitionOf(_mite).DisplayName, Is.EqualTo("Cinder Mite"));
            Assert.That(_setup.Run.Experience, Is.Zero, "A fresh session starts with no experience.");
            float deadline = Time.realtimeSinceStartup + 15f;
            while (_enemy.IsAlive && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(_enemy.IsAlive, Is.False, "The pack must fall without input within fifteen seconds.");
            Assert.That(_mite.IsAlive, Is.False, "The faster mite walks in first and falls first.");
            Assert.That(gruntDeaths, Is.EqualTo(1));
            Assert.That(miteDeaths, Is.EqualTo(1));
            // Two swings fell the mite from the right corner; the Grunt walks in from the far corner, beyond the cleave's
            // reach of the mite, so five more fell it.
            Assert.That(_attack.AttackCount, Is.EqualTo(7));
            // The pack strikes back (exact counts are covered by the Descent simulation).
            Assert.That(_hero.IsAlive, Is.True);
            Assert.That(_hero.Current, Is.LessThan(100f));
            Assert.That(_enemy.GetComponentInChildren<SpriteRenderer>(true).enabled, Is.False);
            Assert.That(_targeting.Acquire(3f), Is.Null);
            // The pack's rewards lie where it fell, one drop each - Enemy_Grunt.asset's 10 experience and 5 gold and
            // Enemy_CinderMite.asset's 1 - and a hero who stands picks up neither (docs/27 S6d, docs/37).
            Assert.That(_setup.Run.Experience, Is.Zero, "A standing hero picks nothing up.");
            Assert.That(_setup.Drops.Count, Is.EqualTo(2));
            Assert.That(_setup.Drops.At(0).Experience + _setup.Drops.At(1).Experience, Is.EqualTo(11));
            Assert.That(_setup.Drops.At(0).Gold + _setup.Drops.At(1).Gold, Is.EqualTo(5));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ADeathDropsItsRewardOnceAndPickingItUpPaysItOnce()
        {
            int awards = 0;
            _setup.Run.ExperienceChanged += () => awards++;
            yield return new WaitForSeconds(1f);
            Assert.That(_enemy.IsAlive, Is.True, "The pack is still walking in.");
            Assert.That(_attack.AttackCount, Is.Zero, "Nothing is in reach yet.");
            Assert.That(_setup.Run.Experience, Is.Zero);

            _enemy.ApplyDamage(new DamageContext(1000f));
            _enemy.ApplyDamage(new DamageContext(1000f));
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_enemy.IsAlive, Is.False);
            Assert.That(_setup.Run.Experience, Is.Zero, "The reward lies where the Grunt fell (docs/37).");
            Assert.That(_setup.Drops.Count, Is.EqualTo(1), "One drop, however many blows.");
            GroundDrops.Drop drop = _setup.Drops.At(0);
            Assert.That((drop.Experience, drop.Gold), Is.EqualTo((10, 5)), "Enemy_Grunt.asset's 10 experience and 5 gold.");

            Assert.That(_setup.Drops.CollectNear(drop.X, drop.Y), Is.EqualTo(1), "Reaching it picks it up.");
            Assert.That(_setup.Drops.CollectNear(drop.X, drop.Y), Is.Zero, "Once.");
            Assert.That(_setup.Run.Experience, Is.EqualTo(10));
            Assert.That(awards, Is.EqualTo(1));
            Assert.That(_setup.Run.Gold, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator OutOfRangeAndDisabledTargetsAreIgnoredThenReacquired()
        {
            // The encounter stops walking the pack, so the test can place it.
            _encounters.enabled = false;
            _enemy.transform.position = new Vector3(20f, 20f, 0f);
            _mite.transform.position = new Vector3(-20f, 20f, 0f);
            float health = _enemy.Current;
            yield return new WaitForSeconds(1f);
            Assert.That(_targeting.Acquire(3f), Is.Null);
            Assert.That(_enemy.Current, Is.EqualTo(health));

            // 1.2 floor units in front of the hero, inside the Sword's 1.8 reach; depth shows at half length on screen.
            _enemy.transform.position = new Vector3(0f, ArenaFloor.WorldY(1.2f), 0f);
            _enemy.gameObject.SetActive(false);
            yield return new WaitForSeconds(1f);
            Assert.That(_targeting.Acquire(3f), Is.Null);
            Assert.That(_enemy.Current, Is.EqualTo(health));

            _enemy.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            Assert.That(_enemy.Current, Is.LessThan(health));
        }

        [UnityTest]
        public IEnumerator DestroyedEnemyDoesNotCauseMissingReferenceErrors()
        {
            Object.Destroy(_enemy.gameObject);
            yield return null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_mite.IsAlive && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_mite.IsAlive, Is.False, "The hero still fights the rest of the pack.");

            int hits = _attack.AttackCount;
            yield return new WaitForSeconds(1f);
            Assert.That(_targeting.Acquire(3f), Is.Null);
            Assert.That(_attack.AttackCount, Is.EqualTo(hits));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PauseFreezesAttacksAndWalkingAndResumeContinues()
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (_attack.AttackCount == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_attack.AttackCount, Is.GreaterThan(0), "The mite walks into reach.");

            Time.timeScale = 0f;
            int hits = _attack.AttackCount;
            Vector3 gruntPosition = _enemy.transform.position;
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_attack.AttackCount, Is.EqualTo(hits));
            Assert.That(_enemy.transform.position, Is.EqualTo(gruntPosition), "Nothing walks while time is frozen.");

            Time.timeScale = 1f;
            yield return new WaitForSeconds(1f);
            Assert.That(_attack.AttackCount, Is.GreaterThan(hits));
            Assert.That(_enemy.transform.position.y, Is.LessThan(gruntPosition.y), "The Grunt walks on.");
        }

        [UnityTest]
        public IEnumerator DeadHeroStopsAttackingAndThePackStopsWalking()
        {
            _hero.ApplyDamage(new DamageContext(1000f));
            int hits = _attack.AttackCount;
            yield return null;
            Vector3 gruntPosition = _enemy.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.That(_attack.AttackCount, Is.EqualTo(hits));
            Assert.That(_enemy.transform.position, Is.EqualTo(gruntPosition));
        }
    }
}
