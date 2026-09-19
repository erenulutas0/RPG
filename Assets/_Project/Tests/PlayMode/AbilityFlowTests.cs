using System.Collections;
using System.Linq;
using Cryptforge.Combat;
using Cryptforge.Art;
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
            Assert.That(ring.transform.lossyScale.x * AbilityRingArt.ContourRadiusTexels / ring.sprite.pixelsPerUnit, Is.EqualTo(_ability.Ability.Radius).Within(.0001f), "The contour, not the transparent texture padding, defines reach.");
            Assert.That(ring.color.a, Is.GreaterThan(0.5f), "Bright while ready.");

            Tap(_button);
            yield return null;
            Assert.That(_ability.UseCount, Is.EqualTo(1), "A burst into empty floor is still spent.");
            Assert.That(ring.color.a, Is.LessThan(0.3f), "Faint while cooling down.");
        }

        [UnityTest]
        public IEnumerator TheRingFollowsTheHeroPausesAndSurvivesSceneReload()
        {
            _heroAttack.enabled = false;
            var ring = Object.FindFirstObjectByType<AbilityRingView>().Ring;
            Sprite sprite = ring.sprite;
            Texture2D texture = sprite.texture;
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(texture.isReadable, Is.False);
            Assert.That(texture.mipmapCount, Is.EqualTo(1));
            Assert.That(texture.width * texture.height * 4, Is.EqualTo(524288));
            _hero.transform.position = new Vector3(1f, .25f, 0);
            Assert.That(ring.transform.position, Is.EqualTo(_hero.transform.position));
            Time.timeScale = 0;
            yield return null;
            Color paused = ring.color;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(ring.color, Is.EqualTo(paused));
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            var next = Object.FindFirstObjectByType<AbilityRingView>().Ring;
            Assert.That(next.sprite, Is.SameAs(sprite));
            Assert.That(next.sprite.texture, Is.SameAs(texture));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator SessionResetReleasesTheRingAndCreatesFreshNativeArt()
        {
            Sprite old = AbilityRingArt.Get();
            Texture2D oldTexture = old.texture;
            AbilityRingArt.ResetSession();
            yield return null;
            Assert.That(old == null, Is.True);
            Assert.That(oldTexture == null, Is.True);
            Sprite fresh = AbilityRingArt.Get();
            Assert.That(fresh, Is.Not.Null);
            Assert.That(fresh, Is.Not.SameAs(old));
            Assert.That(AbilityRingArt.Get(), Is.SameAs(fresh));
        }
        [UnityTest]
        public IEnumerator TheCastPulseExpandsAtItsOriginPausesAndExpiresWithoutOwningArt()
        {
            _heroAttack.enabled = false;
            _encounters.enabled = false;
            Time.timeScale = 0;
            Vector3 origin = _hero.transform.position;
            Assert.That(_ability.TryUse(), Is.True);
            var pulse = GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>()
                .Single(r => r.enabled && r.sprite == AbilityRingArt.Get());
            Assert.That(pulse.sortingOrder, Is.Zero, "Ground effect below actor groups.");
            Assert.That(pulse.transform.localScale.x, Is.EqualTo(_ability.Ability.Radius * .6f).Within(.0001f));
            Vector3 initialScale = pulse.transform.localScale;
            Color initialColor = pulse.color;
            _hero.transform.position += Vector3.right;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(pulse.transform.position, Is.EqualTo(origin), "A cast stays at its cast origin.");
            Assert.That(pulse.transform.localScale, Is.EqualTo(initialScale));
            Assert.That(pulse.color, Is.EqualTo(initialColor));
            Time.timeScale = 1;
            yield return null;
            Assert.That(pulse.transform.localScale.x, Is.GreaterThan(initialScale.x));
            Assert.That(pulse.transform.localScale.x, Is.LessThanOrEqualTo(_ability.Ability.Radius));
            yield return new WaitForSeconds(.25f);
            Assert.That(pulse.enabled, Is.False);
            Assert.That(AbilityRingArt.Get(), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
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
