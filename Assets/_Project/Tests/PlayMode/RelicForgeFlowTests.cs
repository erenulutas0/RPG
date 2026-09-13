using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
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
    // Each test seeds its own profile, loads the gameplay scene and drives the result screen, Relic Forge and relics.
    public sealed class RelicForgeFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private Health _hero;
        private EncounterController _encounters;
        private RunResultView _result;
        private RelicForgeView _forge;

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Relic Forge Test Cleanup");
            SceneManager.SetActiveScene(empty);
            if (gameplay.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator ResultNamesTheNextRelicAndTheForgeLocksWhatTheProfileCannotAfford()
        {
            yield return LoadGameplay(null);
            Assert.That(_setup.Relic, Is.Null);
            Assert.That(Label("Relic Label"), Is.Empty);

            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;
            Assert.That(Label("Forge Hint Label"), Is.EqualTo("Forge gold 0: Second Wind costs 80"));
            Tap(FindButton("Forge Button"));
            Assert.That(_forge.IsOpen, Is.False, "The Forge button waits out the result input delay.");

            yield return new WaitForSecondsRealtime(0.6f);
            Tap(FindButton("Forge Button"));
            Assert.That(_forge.IsOpen, Is.True);
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(Label("Forge Title"), Is.EqualTo("Relic Forge"));
            Assert.That(Label("Forge Status Label"), Is.EqualTo("Gold 0  |  Deepest floor cleared: 0"));
            Assert.That(CardText(0, "Name Label"), Is.EqualTo("Second Wind"));
            Assert.That(CardText(0, "Description Label"), Is.EqualTo("Once per run, at 25% health or less, restore 25% of your health"));
            Assert.That(CardText(1, "Name Label"), Is.EqualTo("Counterweight"));
            Assert.That(CardText(0, "State Label"), Is.EqualTo("80 gold: need 80 more"));
            Assert.That(Card(0).interactable, Is.False, "An unaffordable relic reads as locked.");

            Tap(Card(0));
            Assert.That(_setup.Profile.OwnedRelicIds, Is.Empty);
            Assert.That(TestProfile.HasSavedFile, Is.False, "A run with no gold and no cleared floor writes nothing.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ForgingARelicSpendsGoldSavesItAndCarriesItIntoTheNextRun()
        {
            yield return LoadGameplay(new PlayerProfile(250, null, null, 1));
            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;
            Assert.That(Label("Forge Hint Label"), Is.EqualTo("Forge gold 250: Second Wind is ready to forge"));
            yield return new WaitForSecondsRealtime(0.6f);
            Tap(FindButton("Forge Button"));
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Label("Forge Status Label"), Is.EqualTo("Gold 250  |  Deepest floor cleared: 1"));
            Assert.That(CardText(0, "State Label"), Is.EqualTo("Forge for 80 gold"));
            Assert.That(CardText(1, "State Label"), Is.EqualTo("Forge for 150 gold"));

            Tap(Card(1));
            Tap(Card(1));
            Card(1).onClick.Invoke();
            Assert.That(_setup.Profile.Gold, Is.EqualTo(100), "Rapid taps forge Counterweight once.");
            Assert.That(_setup.Profile.EquippedRelicId, Is.EqualTo("relic_counterweight"));
            Assert.That(CardText(1, "State Label"), Is.EqualTo("Equipped"));
            Assert.That(Label("Forge Status Label"), Does.StartWith("Gold 100"));

            yield return new WaitForSecondsRealtime(0.4f);
            Tap(Card(0));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(20));
            Assert.That(_setup.Profile.EquippedRelicId, Is.EqualTo("relic_second_wind"));
            Assert.That(CardText(1, "State Label"), Is.EqualTo("Owned: tap to equip"));
            yield return new WaitForSecondsRealtime(0.4f);
            Tap(Card(1));
            Assert.That(_setup.Profile.EquippedRelicId, Is.EqualTo("relic_counterweight"), "An owned relic is re-equipped for free.");
            Assert.That(_setup.Profile.Gold, Is.EqualTo(20));

            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.Gold, Is.EqualTo(20));
            Assert.That(saved.OwnedRelicIds, Is.EquivalentTo(new[] { "relic_counterweight", "relic_second_wind" }));
            Assert.That(saved.EquippedRelicId, Is.EqualTo("relic_counterweight"));
            Assert.That(saved.DeepestFloorCleared, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(0.4f);
            int loads = 0;
            UnityAction<Scene, LoadSceneMode> countLoad = (scene, mode) => loads++;
            SceneManager.sceneLoaded += countLoad;
            try
            {
                Tap(FindButton("Start Run Button"));
                Tap(FindButton("Start Run Button"));
                float deadline = Time.realtimeSinceStartup + 5f;
                while (loads == 0 && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
                yield return new WaitForSecondsRealtime(0.3f);
            }
            finally
            {
                SceneManager.sceneLoaded -= countLoad;
            }

            Assert.That(loads, Is.EqualTo(1));
            FindSceneObjects();
            Assert.That(_setup.Profile.Gold, Is.EqualTo(20), "The new run reads the saved profile.");
            Assert.That(_setup.Relic, Is.Not.Null);
            Assert.That(_setup.Relic.Relic.Id, Is.EqualTo("relic_counterweight"));
            Assert.That(Label("Relic Label"), Does.StartWith("Relic: Counterweight"));
            Assert.That(_result.IsOpen, Is.False);
            Assert.That(_forge.IsOpen, Is.False);
            Assert.That(_setup.Run.HasEnded, Is.False);
        }

        [UnityTest]
        public IEnumerator CounterweightStrikesTheGruntEachTimeItHitsTheHero()
        {
            yield return LoadGameplay(new PlayerProfile(0, new[] { "relic_counterweight" }, "relic_counterweight", 0));
            Health grunt = _encounters.CurrentEnemy;
            AttackController gruntAttack = grunt.GetComponent<AttackController>();
            AttackController heroAttack = _hero.GetComponent<AttackController>();

            float deadline = Time.realtimeSinceStartup + 4f;
            while (gruntAttack.AttackCount < 2 && grunt.IsAlive && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(gruntAttack.AttackCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(_setup.Relic.Triggers, Is.EqualTo(gruntAttack.AttackCount), "Every Grunt Strike is answered.");
            Assert.That(grunt.Current, Is.EqualTo(50f - 10f * heroAttack.AttackCount - 6f * _setup.Relic.Triggers).Within(1e-3f),
                "Sword hits deal 10 and each counter deals 60% of 10.");
            Assert.That(Label("Relic Label"), Is.EqualTo($"Relic: Counterweight ({_setup.Relic.Triggers}x)"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SecondWindRestoresAQuarterOnceWhenTheHeroFallsToAQuarter()
        {
            yield return LoadGameplay(new PlayerProfile(0, new[] { "relic_second_wind" }, "relic_second_wind", 0));
            Assert.That(Label("Relic Label"), Is.EqualTo("Relic: Second Wind"));

            _hero.ApplyDamage(new DamageContext(_hero.Current - 20f));
            Assert.That(_hero.Current, Is.EqualTo(45f).Within(1e-3f), "20 HP is below 25%, so 25 HP returns.");
            Assert.That(_setup.Relic.Triggers, Is.EqualTo(1));
            Assert.That(Label("Relic Label"), Is.EqualTo("Relic: Second Wind (1x)"));

            _hero.ApplyDamage(new DamageContext(30f));
            Assert.That(_hero.Current, Is.EqualTo(15f).Within(1e-3f), "Once per run.");
            Assert.That(_setup.Relic.Triggers, Is.EqualTo(1));

            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;
            Assert.That(Label("Build Label"), Does.StartWith("Build: Second Wind (1x)"));
        }

        private IEnumerator LoadGameplay(PlayerProfile seed)
        {
            TestProfile.Begin(seed);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            // Views format their labels in Start, which runs before the first frame after the load.
            yield return null;
            FindSceneObjects();
        }

        private void FindSceneObjects()
        {
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _result = Object.FindFirstObjectByType<RunResultView>();
            _forge = Object.FindFirstObjectByType<RelicForgeView>();
        }

        private Button Card(int index) =>
            _forge.transform.Find($"Forge Panel/Safe Area/Relic Card {index + 1}").GetComponent<Button>();

        private string CardText(int index, string label) => Card(index).transform.Find(label).GetComponent<Text>().text;

        private static Button FindButton(string name) => GameObject.Find(name).GetComponent<Button>();

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
