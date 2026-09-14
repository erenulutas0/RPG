using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // The Forge Burst: the round button at the bottom right fires it around the hero, the ring on the floor shows its reach.
    public sealed class AbilityFlowTests
    {
        private CombatSetup _setup;
        private Health _hero;
        private AttackController _heroAttack;
        private AbilityController _ability;
        private EncounterController _encounters;
        private CombatEffectsView _effects;
        private Button _button;
        private Image _fill;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            // Views bind in Start, before the first frame after the load.
            yield return null;
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _heroAttack = _hero.GetComponent<AttackController>();
            _ability = _hero.GetComponent<AbilityController>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _effects = Object.FindFirstObjectByType<CombatEffectsView>();
            _button = GameObject.Find("Ability Button").GetComponent<Button>();
            _fill = _button.transform.Find("Cooldown Fill").GetComponent<Image>();
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Ability Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator TheButtonBurstsEveryEnemyInReachThenCoolsDown()
        {
            // The hero holds its swings, so every point of damage on the pack comes from the burst.
            _heroAttack.enabled = false;
            Assert.That(_setup.Ability, Is.SameAs(_ability.Ability));
            Assert.That(_ability.Ability.IsReady, Is.True, "The burst starts ready.");
            Assert.That(_button.interactable, Is.True);
            Assert.That(_fill.fillAmount, Is.Zero);
            Health grunt = _encounters.WaveEnemyAt(0);
            Health mite = _encounters.WaveEnemyAt(1);

            float radius = _ability.Ability.Radius;
            float deadline = Time.realtimeSinceStartup + 12f;
            while ((!WithinReach(grunt, radius) || !WithinReach(mite, radius)) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(WithinReach(grunt, radius) && WithinReach(mite, radius), Is.True, "Both enemies walk into the burst's reach.");
            float gruntHealth = grunt.Current;

            Tap(_button);
            Assert.That(_ability.UseCount, Is.EqualTo(1));
            Assert.That(grunt.Current, Is.EqualTo(gruntHealth - 20f).Within(1e-3f), "Ability_ForgeBurst.asset deals 20.");
            Assert.That(mite.IsAlive, Is.False, "20 damage fells the 15 HP mite.");
            Assert.That(_ability.Ability.Remaining, Is.EqualTo(8f).Within(1e-3f));
            yield return null;
            Assert.That(_button.interactable, Is.False, "The button waits for the cooldown.");
            Assert.That(_fill.fillAmount, Is.GreaterThan(0.9f));
            Assert.That(_effects.ActiveEffectCount, Is.GreaterThan(0), "The burst rings the hero and sparks the struck enemies.");

            float health = grunt.Current;
            Tap(_button);
            Assert.That(_ability.UseCount, Is.EqualTo(1), "A tap while cooling down does nothing.");
            Assert.That(grunt.Current, Is.EqualTo(health));

            yield return new WaitForSeconds(8.2f);
            Assert.That(_ability.Ability.IsReady, Is.True);
            Assert.That(_fill.fillAmount, Is.Zero);
            Assert.That(_button.interactable, Is.EqualTo(!_setup.Choices.IsOpen && !_setup.Run.HasEnded));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheBurstWaitsWhileAChoiceIsOpen()
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (_setup.Choices.Current == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_setup.Choices.Current, Is.Not.Null, "Clearing the first pack opens an upgrade choice.");
            yield return null;

            Assert.That(_button.interactable, Is.False, "No burst behind a choice.");
            Tap(_button);
            Assert.That(_ability.UseCount, Is.Zero);

            _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            yield return null;
            Assert.That(_button.interactable, Is.True, "Ready again once the run resumes.");
        }

        [UnityTest]
        public IEnumerator TheRangeRingLiesOnTheFloorAndFadesWhileCoolingDown()
        {
            var ring = GameObject.Find("Ability Ring").GetComponent<SpriteRenderer>();
            var body = GameObject.Find("Hero Body").GetComponent<SpriteRenderer>();
            Assert.That(ring.transform.parent, Is.SameAs(_hero.transform));
            Assert.That(ring.sprite, Is.Not.Null);
            Assert.That(ring.sortingOrder, Is.LessThan(body.sortingOrder), "The ring lies under the hero.");
            Assert.That(ring.sortingOrder, Is.GreaterThan(Object.FindFirstObjectByType<ArenaView>().Platform.PlatformRenderer.sortingOrder), "And over the platform.");
            Assert.That(ring.bounds.size.x, Is.EqualTo(2f * _ability.Ability.Radius).Within(0.2f), "Its width spans the burst's radius either side.");
            Assert.That(ring.color.a, Is.GreaterThan(0.5f), "Bright while ready.");

            Tap(_button);
            yield return null;
            Assert.That(_ability.UseCount, Is.EqualTo(1), "A burst into empty floor is still spent.");
            Assert.That(ring.color.a, Is.LessThan(0.3f), "Faint while cooling down.");
        }

        private static bool WithinReach(Health enemy, float radius)
        {
            Vector3 offset = enemy.transform.position;
            return ArenaFloor.DistanceSquared(offset.x, offset.y) <= radius * radius;
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
