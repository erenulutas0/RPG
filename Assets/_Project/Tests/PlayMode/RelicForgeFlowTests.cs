using System.Collections;
using System.Collections.Generic;
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
            Assert.That(Label("Weapons Header"), Is.EqualTo("Weapons: carry one"));
            Assert.That(Label("Relics Header"), Is.EqualTo("Relics: equip one"));
            Assert.That(WeaponCardText(0, "Name Label"), Is.EqualTo("Sword"));
            Assert.That(WeaponCardText(0, "State Label"), Is.EqualTo("Equipped"), "The starting weapon never needs forging.");
            Assert.That(WeaponCardText(1, "Name Label"), Is.EqualTo("Staff"));
            Assert.That(WeaponCardText(1, "State Label"), Is.EqualTo("120 gold: need 120 more"));
            Assert.That(WeaponCardText(2, "Name Label"), Is.EqualTo("Daggers"));
            Assert.That(WeaponCard(2).interactable, Is.False);

            Tap(Card(0));
            Tap(WeaponCard(1));
            Assert.That(_setup.Profile.OwnedRelicIds, Is.Empty);
            Assert.That(_setup.Profile.OwnedWeaponIds, Is.Empty);
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
            Assert.That(Label("Build Label"), Does.StartWith("Build: Sword  |  Second Wind (1x)"));
        }

        [UnityTest]
        public IEnumerator ForgingTheStaffCarriesItIntoTheNextRunAndTheSwordStaysFree()
        {
            // Decimals follow the current culture, as on the device.
            yield return LoadGameplay(new PlayerProfile(150, null, null, 1));
            Assert.That(Label("Weapon Label"), Is.EqualTo($"Sword  |  10 damage every {0.8f:0.00}s"));
            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
            Tap(FindButton("Forge Button"));
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(WeaponCardText(0, "Description Label"), Is.EqualTo($"10 damage every {0.8f:0.0#}s. Cleaves a second enemy for 60%"));
            Assert.That(WeaponCardText(1, "Description Label"), Is.EqualTo($"18 damage every {1.2f:0.0#}s. Blasts every enemy near the target for 75%"));
            Assert.That(WeaponCardText(1, "State Label"), Is.EqualTo("Forge for 120 gold"));
            Assert.That(WeaponCardText(2, "Description Label"), Is.EqualTo($"6 damage every {0.55f:0.0#}s. Crits for 200% once every 3 strikes"));
            Assert.That(WeaponCardText(2, "State Label"), Is.EqualTo("180 gold: need 30 more"));

            Tap(WeaponCard(1));
            Tap(WeaponCard(1));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(30), "Rapid taps forge the Staff once.");
            Assert.That(_setup.Profile.EquippedWeaponId, Is.EqualTo("weapon_staff"));
            Assert.That(WeaponCardText(1, "State Label"), Is.EqualTo("Equipped"));
            Assert.That(WeaponCardText(0, "State Label"), Is.EqualTo("Owned: tap to equip"));

            yield return new WaitForSecondsRealtime(0.4f);
            Tap(WeaponCard(0));
            Assert.That(_setup.Profile.EquippedWeaponId, Is.Null, "The Sword is equipped again for free.");
            Assert.That(_setup.Profile.Gold, Is.EqualTo(30));
            yield return new WaitForSecondsRealtime(0.4f);
            Tap(WeaponCard(1));
            Assert.That(_setup.Profile.EquippedWeaponId, Is.EqualTo("weapon_staff"));

            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }));
            Assert.That(saved.EquippedWeaponId, Is.EqualTo("weapon_staff"));

            yield return new WaitForSecondsRealtime(0.4f);
            yield return StartRun();
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_staff"));
            Assert.That(_setup.Weapon.Pattern.Behavior, Is.EqualTo(WeaponBehavior.Area));
            Assert.That(Label("Weapon Label"), Is.EqualTo($"Staff  |  18 damage every {1.2f:0.00}s"));
            Assert.That(Object.FindFirstObjectByType<HeroWeaponView>().ShownLoadout.name, Is.EqualTo("Staff Loadout"));
            Assert.That(GameObject.Find("Sword Placeholder"), Is.Null, "The Sword loadout is hidden.");

            Health grunt = _encounters.CurrentEnemy;
            AttackController heroAttack = _hero.GetComponent<AttackController>();
            float deadline = Time.realtimeSinceStartup + 3f;
            while (heroAttack.AttackCount < 2 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(heroAttack.AttackCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(grunt.Current, Is.EqualTo(50f - 18f * heroAttack.AttackCount).Within(1e-3f), "Each Staff blast deals 18.");
        }

        [UnityTest]
        public IEnumerator DaggersCritEveryThirdStrikeAndTheCritFlashesGold()
        {
            yield return LoadGameplay(new PlayerProfile(0, null, null, 0, new[] { "weapon_daggers" }, "weapon_daggers"));
            Assert.That(Label("Weapon Label"), Is.EqualTo($"Daggers  |  6 damage every {0.55f:0.00}s"));
            Assert.That(Object.FindFirstObjectByType<HeroWeaponView>().ShownLoadout.name, Is.EqualTo("Daggers Loadout"));

            // The Daggers may already have struck while the scene loaded, so each hit is checked against its attack number.
            Health grunt = _encounters.CurrentEnemy;
            WeaponRuntime daggers = _setup.Weapon;
            SpriteRenderer body = grunt.transform.Find("Enemy Body").GetComponent<SpriteRenderer>();
            var hits = new List<(int Attack, float Amount, bool Critical, bool FlashedGold)>();
            System.Action<DamageContext> record = context =>
                hits.Add((daggers.AttacksMade, context.Amount, context.IsCritical, body.color == new Color(1f, 0.82f, 0.2f)));
            grunt.Damaged += record;
            try
            {
                float deadline = Time.realtimeSinceStartup + 4f;
                while ((hits.Count < 3 || !hits.Exists(hit => hit.Critical)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
            }
            finally
            {
                grunt.Damaged -= record;
            }

            Assert.That(hits.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(hits.Exists(hit => hit.Critical), Is.True);
            foreach (var hit in hits)
            {
                bool third = hit.Attack % 3 == 0;
                Assert.That(hit.Critical, Is.EqualTo(third), $"Strike {hit.Attack}");
                Assert.That(hit.Amount, Is.EqualTo(third ? 12f : 6f), $"Strike {hit.Attack}");
                Assert.That(hit.FlashedGold, Is.EqualTo(third), $"Strike {hit.Attack} flash");
            }
            int strikes = daggers.AttacksMade;
            Assert.That(grunt.Current, Is.EqualTo(50f - 6f * strikes - 6f * (strikes / 3)).Within(1e-3f));
            LogAssert.NoUnexpectedReceived();
        }

        // Every Forge panel element is anchored to the bottom of the safe area; together they must fit the shortest
        // 9:16 portrait screen (1920 canvas units) without overlapping.
        [UnityTest]
        public IEnumerator ForgePanelFitsTheShortestPortraitScreenWithoutOverlaps()
        {
            yield return LoadGameplay(null);
            Transform safeArea = _forge.transform.Find("Forge Panel/Safe Area");
            var spans = new List<(string Name, float Bottom, float Top)>();
            foreach (RectTransform child in safeArea)
            {
                Assert.That(child.anchorMin.y, Is.Zero, child.name);
                Assert.That(child.anchorMax.y, Is.Zero, child.name);
                Assert.That(child.pivot.y, Is.Zero, child.name);
                spans.Add((child.name, child.anchoredPosition.y, child.anchoredPosition.y + child.sizeDelta.y));
            }

            spans.Sort((a, b) => a.Bottom.CompareTo(b.Bottom));
            Assert.That(spans.Count, Is.EqualTo(10));
            Assert.That(spans[0].Bottom, Is.GreaterThanOrEqualTo(0f));
            Assert.That(spans[spans.Count - 1].Top, Is.LessThanOrEqualTo(1920f), spans[spans.Count - 1].Name);
            for (int i = 1; i < spans.Count; i++)
                Assert.That(spans[i].Bottom, Is.GreaterThanOrEqualTo(spans[i - 1].Top), $"{spans[i].Name} overlaps {spans[i - 1].Name}.");
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

        private IEnumerator StartRun()
        {
            int loads = 0;
            UnityAction<Scene, LoadSceneMode> countLoad = (scene, mode) => loads++;
            SceneManager.sceneLoaded += countLoad;
            try
            {
                Tap(FindButton("Start Run Button"));
                float deadline = Time.realtimeSinceStartup + 5f;
                while (loads == 0 && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= countLoad;
            }

            Assert.That(loads, Is.EqualTo(1));
            // Views read the new run in Start, on the first frame after the load.
            yield return null;
            FindSceneObjects();
        }

        private Button Card(int index) =>
            _forge.transform.Find($"Forge Panel/Safe Area/Relic Card {index + 1}").GetComponent<Button>();

        private string CardText(int index, string label) => Card(index).transform.Find(label).GetComponent<Text>().text;

        private Button WeaponCard(int index) =>
            _forge.transform.Find($"Forge Panel/Safe Area/Weapon Card {index + 1}").GetComponent<Button>();

        private string WeaponCardText(int index, string label) => WeaponCard(index).transform.Find(label).GetComponent<Text>().text;

        private static Button FindButton(string name) => GameObject.Find(name).GetComponent<Button>();

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
