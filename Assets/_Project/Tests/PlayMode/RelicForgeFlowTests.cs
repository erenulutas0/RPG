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
            Assert.That(Label("Forge Status Label"), Is.EqualTo("Gold 0  |  Highest floor cleared: 0"));
            Assert.That(CardText(0, "Name Label"), Is.EqualTo("Second Wind"));
            Assert.That(CardText(0, "Description Label"), Is.EqualTo("Once per run, at 25% health or less, restore 25% of your health"));
            Assert.That(CardText(1, "Name Label"), Is.EqualTo("Counterweight"));
            Assert.That(CardText(0, "State Label"), Is.EqualTo("80 gold: need 80 more"));
            Assert.That(Card(0).interactable, Is.False, "An unaffordable relic reads as locked.");
            Assert.That(Label("Weapons Header"), Is.EqualTo("Weapon: the hero's own"));
            Assert.That(Label("Relics Header"), Is.EqualTo("Relics: equip one"));
            Assert.That(WeaponCardText(0, "Name Label"), Is.EqualTo("Sword"));
            Assert.That(WeaponCardText(0, "State Label"), Is.EqualTo("Signature weapon"), "The hero's own weapon, never sold.");
            // The forge's weapon rack is gone (docs/63, 2026-11-23): every hero starts with its own weapon, so the forge
            // shows that one card and sells no weapon.
            Assert.That(NoMoreWeaponCards(), Is.True, "No Staff or Daggers for sale.");

            Tap(Card(0));
            Tap(WeaponCard(0));
            Assert.That(_setup.Profile.OwnedRelicIds, Is.Empty);
            Assert.That(_setup.Profile.OwnedWeaponIds, Is.Empty);
            // The run's end writes the profile once (docs/58 3.1: the result on disk before any video), even with nothing
            // banked; the taps on the locked cards write nothing into it.
            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That((saved.Gold, saved.OwnedRelicIds.Count, saved.OwnedWeaponIds.Count), Is.EqualTo((0, 0, 0)),
                "A run with no gold and no cleared floor saves an empty profile.");
            Assert.That(_setup.ResultSaved, Is.True);
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
            Assert.That(Label("Forge Status Label"), Is.EqualTo("Gold 250  |  Highest floor cleared: 1"));
            Assert.That(CardText(0, "State Label"), Is.EqualTo("Forge for 80 gold"));
            Assert.That(CardText(1, "State Label"), Is.EqualTo("Forge for 150 gold"));
            Assert.That(Card(0).transform.Find("Relic Artwork").GetComponent<Image>().sprite.name,
                Is.EqualTo("UI_Icon_SecondWind_v1"));
            Assert.That(Card(1).transform.Find("Relic Artwork").GetComponent<Image>().sprite.name,
                Is.EqualTo("UI_Icon_Counterweight_v1"));
            Color affordableColor = Card(1).transform.Find("State Accent").GetComponent<Image>().color;

            Tap(Card(1));
            Tap(Card(1));
            Card(1).onClick.Invoke();
            Assert.That(_setup.Profile.Gold, Is.EqualTo(100), "Rapid taps forge Counterweight once.");
            Assert.That(_setup.Profile.EquippedRelicId, Is.EqualTo("relic_counterweight"));
            Assert.That(CardText(1, "State Label"), Is.EqualTo("Equipped"));
            Assert.That(Card(1).transform.Find("State Accent").GetComponent<Image>().color,
                Is.Not.EqualTo(affordableColor), "Equipping changes the visual state as well as the written label.");
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

        // Ilya carries its own Staff (docs/45) and the Vanguard its Sword (since 2026-11-22), and the forge sells no weapon
        // (docs/63): once every relic is forged the hint says so, and the forge shows the next hero's own weapon alone.
        [UnityTest]
        public IEnumerator IlyasResultSaysEveryRelicIsForgedAndTheForgeShowsTheVanguardsSwordAlone()
        {
            const string arcanist = "hero_cinder_arcanist";
            yield return LoadGameplay(new PlayerProfile(500, new[] { "relic_second_wind", "relic_counterweight" },
                null, 0, null, null, new[] { arcanist }, arcanist));
            Assert.That(_setup.Hero.Id, Is.EqualTo(arcanist));
            Assert.That(_setup.NextWeapon.Id, Is.EqualTo("weapon_staff"), "Its own Staff.");
            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;
            Assert.That(Label("Forge Hint Label"), Is.EqualTo("Forge gold 500: every relic is forged"));

            yield return new WaitForSecondsRealtime(0.6f);
            Tap(FindButton("Forge Button"));
            Assert.That(_forge.IsOpen, Is.True);
            _forge.HeroChoice.Cycle();
            Assert.That(_setup.Profile.SelectedHeroId, Is.Null, "The Vanguard for the next run.");
            Assert.That(_setup.NextWeapon.Id, Is.EqualTo("weapon_sword"), "The Vanguard's Sword, always its own.");
            Assert.That(WeaponCard(0).gameObject.activeSelf, Is.True);
            Assert.That(WeaponCardText(0, "Name Label"), Is.EqualTo("Sword"));
            Assert.That(WeaponCardText(0, "State Label"), Is.EqualTo("Signature weapon"));
            Assert.That(NoMoreWeaponCards(), Is.True, "The Staff is not the Vanguard's, and nothing is for sale.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CounterweightStrikesBackAtWhicheverEnemyHitsTheHero()
        {
            yield return LoadGameplay(new PlayerProfile(0, new[] { "relic_counterweight" }, "relic_counterweight", 0));
            // The hero holds its swings, so every point of damage on the pack comes from a counter.
            _hero.GetComponent<AttackController>().enabled = false;
            Health grunt = _encounters.WaveEnemyAt(0);
            Health mite = _encounters.WaveEnemyAt(1);
            AttackController gruntAttack = grunt.GetComponent<AttackController>();
            AttackController miteAttack = mite.GetComponent<AttackController>();

            float deadline = Time.realtimeSinceStartup + 10f;
            while (gruntAttack.AttackCount < 2 && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(gruntAttack.AttackCount, Is.EqualTo(2));
            Assert.That(miteAttack.AttackCount, Is.GreaterThanOrEqualTo(1), "The faster mite bites first.");
            Assert.That(_setup.Relic.Triggers, Is.EqualTo(gruntAttack.AttackCount + miteAttack.AttackCount), "Every strike is answered.");
            Assert.That(grunt.Current, Is.EqualTo(50f - 6f * gruntAttack.AttackCount).Within(1e-3f),
                "Each counter deals 60% of the Sword's 10 to the enemy that struck.");
            Assert.That(mite.Current, Is.EqualTo(Mathf.Max(0f, 15f - 6f * miteAttack.AttackCount)).Within(1e-3f));
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

        // Before 2026-11-22 the Vanguard's rack sold the Staff and the Daggers and the next run carried what it forged.
        // The Vanguard is bound to its Sword since (the owner), and the rack is gone since 2026-11-23 (docs/63): the forge
        // shows the Sword, read only, a tap on it changes nothing, and the next run carries the Sword.
        [UnityTest]
        public IEnumerator TheForgeShowsTheVanguardsSwordSellsNoWeaponAndTheNextRunCarriesIt()
        {
            // Decimals follow the current culture, as on the device.
            yield return LoadGameplay(new PlayerProfile(150, null, null, 1));
            Assert.That(Label("Weapon Label"), Is.EqualTo("Sword  |  10 damage every 0.80s"));
            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
            Tap(FindButton("Forge Button"));
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(WeaponCardText(0, "Description Label"), Is.EqualTo("10 damage every 0.8s. Cleaves a second enemy for 60%"));
            Assert.That(WeaponCardText(0, "State Label"), Is.EqualTo("Signature weapon"));
            Assert.That(NoMoreWeaponCards(), Is.True);
            Tap(WeaponCard(0));
            Tap(WeaponCard(0));
            Assert.That(_setup.Profile.Gold, Is.EqualTo(150), "Nothing forged.");
            Assert.That(_setup.Profile.OwnedWeaponIds, Is.Empty);
            Assert.That(_setup.Profile.EquippedWeaponId, Is.Null);

            yield return new WaitForSecondsRealtime(0.4f);
            yield return StartRun();
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_sword"));
            Assert.That(Label("Weapon Label"), Is.EqualTo("Sword  |  10 damage every 0.80s"));
            LogAssert.NoUnexpectedReceived();
        }

        // The Staff's blast as the forged Staff's run measured it, the run now started with the Staff by the development
        // file (the Vanguard's own is the Sword alone since 2026-11-22; a player's run gets the Staff from a card).
        [UnityTest]
        public IEnumerator TheStaffBlastsTheMiteThenTheGruntAndTheSwordLoadoutHides()
        {
            yield return LoadGameplay(new PlayerProfile(150, null, null, 1), "weapon_staff");
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_staff"));
            Assert.That(_setup.Weapon.Pattern.Behavior, Is.EqualTo(WeaponBehavior.Area));
            Assert.That(Label("Weapon Label"), Is.EqualTo("Staff  |  11 damage every 1.10s"));
            Assert.That(Object.FindFirstObjectByType<HeroWeaponView>().ShownLoadout.name, Is.EqualTo("Staff Loadout"));
            Assert.That(GameObject.Find("Sword Placeholder"), Is.Null, "The Sword loadout is hidden.");

            Health grunt = _encounters.WaveEnemyAt(0);
            Health mite = _encounters.WaveEnemyAt(1);
            AttackController heroAttack = _hero.GetComponent<AttackController>();
            float deadline = Time.realtimeSinceStartup + 8f;
            while (heroAttack.AttackCount < 2 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(heroAttack.AttackCount, Is.EqualTo(2));
            Assert.That(mite.IsAlive, Is.False, "The first blast wounds the mite from the right corner; the second, aimed at the Grunt, finishes it with its 75% splash.");
            Assert.That(grunt.Current, Is.EqualTo(50f - 11f).Within(1e-3f), "The Grunt from the far corner takes the second blast head on.");
        }

        [UnityTest]
        public IEnumerator DaggersCritEveryThirdStrikeAndTheCritFlashesGold()
        {
            yield return LoadGameplay(new PlayerProfile(0, null, null, 0, new[] { "weapon_daggers" }, "weapon_daggers"),
                "weapon_daggers");
            Assert.That(Label("Weapon Label"), Is.EqualTo("Daggers  |  6 damage every 0.50s"));
            Assert.That(Object.FindFirstObjectByType<HeroWeaponView>().ShownLoadout.name, Is.EqualTo("Daggers Loadout"));

            // Every hit on the pack is checked against its attack number: the mite from the right corner walks in first and
            // takes the first strikes, then the Grunt from the far corner takes the rest until the hero turns back.
            Health grunt = _encounters.WaveEnemyAt(0);
            Health mite = _encounters.WaveEnemyAt(1);
            WeaponRuntime daggers = _setup.Weapon;
            var hits = new List<(Health Enemy, int Attack, float Amount, bool Critical, bool FlashedGold)>();
            System.Action<DamageContext> recordGrunt = Recorder(grunt, daggers, hits);
            System.Action<DamageContext> recordMite = Recorder(mite, daggers, hits);
            grunt.Damaged += recordGrunt;
            mite.Damaged += recordMite;
            try
            {
                float deadline = Time.realtimeSinceStartup + 10f;
                while (!hits.Exists(hit => hit.Enemy == grunt && hit.Critical) && Time.realtimeSinceStartup < deadline)
                    yield return null;
            }
            finally
            {
                grunt.Damaged -= recordGrunt;
                mite.Damaged -= recordMite;
            }

            Assert.That(hits.Exists(hit => hit.Enemy == grunt && hit.Critical), Is.True);
            float gruntDamage = 0f;
            foreach (var hit in hits)
            {
                bool third = hit.Attack % 3 == 0;
                Assert.That(hit.Critical, Is.EqualTo(third), $"Strike {hit.Attack}");
                Assert.That(hit.Amount, Is.EqualTo(third ? 12f : 6f), $"Strike {hit.Attack}");
                Assert.That(hit.FlashedGold, Is.EqualTo(third), $"Strike {hit.Attack} flash");
                if (hit.Enemy == grunt)
                    gruntDamage += hit.Amount;
            }
            Assert.That(hits[0].Enemy, Is.SameAs(mite), "The mite arrives first and takes the first strike.");
            Assert.That(hits.Count, Is.EqualTo(daggers.AttacksMade), "Each strike lands once.");
            Assert.That(mite.Current, Is.LessThan(15f));
            Assert.That(grunt.Current, Is.EqualTo(50f - gruntDamage).Within(1e-3f));
            LogAssert.NoUnexpectedReceived();
        }

        // Records each hit with the weapon's attack number and whether the enemy flashed crit gold as it landed. The flash is
        // a silhouette overlay over the drawn body, so CombatantView reports its colour.
        private static System.Action<DamageContext> Recorder(Health enemy, WeaponRuntime weapon,
            List<(Health Enemy, int Attack, float Amount, bool Critical, bool FlashedGold)> hits)
        {
            var view = enemy.GetComponent<CombatantView>();
            return context => hits.Add((enemy, weapon.AttacksMade, context.Amount, context.IsCritical,
                view.IsFlashing && view.FlashColor == new Color(1f, 0.82f, 0.2f)));
        }

        // Test the rendered bounds, not a particular anchoring strategy: the foundry rows now expand with safe height.
        // The result screen once showed through between cards, so the Forge backdrop must remain fully opaque.
        [UnityTest]
        public IEnumerator ForgePanelIsOpaqueAndFitsTheShortestPortraitScreenWithoutOverlaps()
        {
            yield return LoadGameplay(null);
            Assert.That(_forge.transform.Find("Forge Panel").GetComponent<Image>().color.a, Is.EqualTo(1f),
                "The result screen must not show through the Forge.");
            _forge.Open();
            var safeArea = _forge.transform.Find("Forge Panel/Safe Area").GetComponent<RectTransform>();
            safeArea.GetComponent<SafeAreaFitter>().enabled = false;
            safeArea.anchorMin = safeArea.anchorMax = new Vector2(.5f, .5f);
            var corners = new Vector3[4];
            foreach (float height in new[] { 1760f, 1920f, 2232f })
            {
                safeArea.sizeDelta = new Vector2(1080f, height);
                Canvas.ForceUpdateCanvases();
                var spans = new List<(string Name, float Bottom, float Top)>();
                RectTransform chooser = null;
                RectTransform skills = null;
                var entries = new List<RectTransform>();
                foreach (RectTransform child in safeArea)
                {
                    // The decorative background intentionally lies behind every row; only functional rows may not overlap.
                    if (child.GetComponent<Button>() == null && child.GetComponent<Text>() == null)
                        continue;
                    // The hero chooser (docs/45) shares the start button's row, side by side: checked against it below.
                    if (child.name == "Hero Choice")
                    {
                        chooser = child;
                        continue;
                    }
                    // The Bag and Anvil buttons (docs/54 phase 4) share the title's row at its ends: checked against it below.
                    if (child.name == AnvilView.BagEntryName || child.name == AnvilView.AnvilEntryName)
                    {
                        entries.Add(child);
                        continue;
                    }
                    // The Skills tab's entry (docs/57 phase 4) shares the weapons' header row at its right end: below.
                    if (child.name == AnvilView.SkillsEntryName)
                    {
                        skills = child;
                        continue;
                    }
                    child.GetWorldCorners(corners);
                    Vector3 bottom = safeArea.InverseTransformPoint(corners[0]);
                    Vector3 top = safeArea.InverseTransformPoint(corners[2]);
                    Assert.That(bottom.x, Is.GreaterThanOrEqualTo(safeArea.rect.xMin - .5f), child.name);
                    Assert.That(top.x, Is.LessThanOrEqualTo(safeArea.rect.xMax + .5f), child.name);
                    spans.Add((child.name, bottom.y - safeArea.rect.yMin, top.y - safeArea.rect.yMin));
                }

                spans.Sort((a, b) => a.Bottom.CompareTo(b.Bottom));
                // The title, the status, the weapon's header and its one card (the rack's other two places went with it,
                // docs/63), the relics' header and two cards, and the start button.
                Assert.That(spans.Count, Is.EqualTo(8));
                Assert.That(chooser, Is.Not.Null, "Two heroes: the start row carries the hero chooser.");
                var start = safeArea.Find("Start Run Button").GetComponent<RectTransform>();
                var chooserCorners = new Vector3[4];
                chooser.GetWorldCorners(chooserCorners);
                start.GetWorldCorners(corners);
                Vector3 chooserMin = safeArea.InverseTransformPoint(chooserCorners[0]);
                Vector3 chooserMax = safeArea.InverseTransformPoint(chooserCorners[2]);
                Vector3 startMin = safeArea.InverseTransformPoint(corners[0]);
                Vector3 startMax = safeArea.InverseTransformPoint(corners[2]);
                Assert.That(chooserMin.x, Is.GreaterThanOrEqualTo(safeArea.rect.xMin - .5f));
                Assert.That(chooserMax.x, Is.LessThanOrEqualTo(startMin.x + .5f), "The chooser sits left of the start button.");
                Assert.That(chooserMin.y, Is.EqualTo(startMin.y).Within(.5f), "On the start button's row.");
                Assert.That(chooserMax.y, Is.EqualTo(startMax.y).Within(.5f), "On the start button's row.");
                Assert.That(startMax.x - startMin.x, Is.GreaterThan(chooserMax.x - chooserMin.x),
                    "The start button keeps the larger share of the row.");
                // The two entries: inside the safe area, on the title's row, and clear of the title on either side.
                Assert.That(entries.Count, Is.EqualTo(2), "Bag and Anvil beside the nameplate.");
                var title = safeArea.Find("Forge Title").GetComponent<RectTransform>();
                var titleCorners = new Vector3[4];
                title.GetWorldCorners(titleCorners);
                Vector3 titleMin = safeArea.InverseTransformPoint(titleCorners[0]);
                Vector3 titleMax = safeArea.InverseTransformPoint(titleCorners[2]);
                foreach (RectTransform entry in entries)
                {
                    entry.GetWorldCorners(corners);
                    Vector3 entryMin = safeArea.InverseTransformPoint(corners[0]);
                    Vector3 entryMax = safeArea.InverseTransformPoint(corners[2]);
                    Assert.That(entryMin.x, Is.GreaterThanOrEqualTo(safeArea.rect.xMin - .5f), entry.name);
                    Assert.That(entryMax.x, Is.LessThanOrEqualTo(safeArea.rect.xMax + .5f), entry.name);
                    Assert.That(entryMin.y, Is.GreaterThanOrEqualTo(titleMin.y - .5f), entry.name + " stays in the title's row.");
                    Assert.That(entryMax.y, Is.LessThanOrEqualTo(titleMax.y + .5f), entry.name + " stays in the title's row.");
                    bool clear = entryMax.x <= titleMin.x + .5f || entryMin.x >= titleMax.x - .5f;
                    Assert.That(clear, Is.True, entry.name + " overlaps the title.");
                    Assert.That(entryMax.y - entryMin.y, Is.GreaterThanOrEqualTo(44f), entry.name + " is a thumb's target.");
                }
                // The skills' entry: inside the safe area, on the weapons' header row, clear of its words, a thumb's target.
                Assert.That(skills, Is.Not.Null, "The Skills tab's entry beside the weapons' header.");
                var header = safeArea.Find("Weapons Header").GetComponent<Text>();
                var headerCorners = new Vector3[4];
                header.rectTransform.GetWorldCorners(headerCorners);
                Vector3 headerMin = safeArea.InverseTransformPoint(headerCorners[0]);
                Vector3 headerMax = safeArea.InverseTransformPoint(headerCorners[2]);
                skills.GetWorldCorners(corners);
                Vector3 skillsMin = safeArea.InverseTransformPoint(corners[0]);
                Vector3 skillsMax = safeArea.InverseTransformPoint(corners[2]);
                Assert.That(skillsMax.x, Is.LessThanOrEqualTo(safeArea.rect.xMax + .5f));
                Assert.That(skillsMin.y, Is.GreaterThanOrEqualTo(headerMin.y - .5f), "On the weapons' header row.");
                Assert.That(skillsMax.y, Is.LessThanOrEqualTo(headerMax.y + .5f), "On the weapons' header row.");
                Assert.That(skillsMin.x, Is.GreaterThanOrEqualTo(headerMin.x + header.preferredWidth - .5f), "Clear of the header's words.");
                Assert.That(skillsMax.y - skillsMin.y, Is.GreaterThanOrEqualTo(44f), "A thumb's target.");
                Assert.That(spans[0].Bottom, Is.GreaterThanOrEqualTo(-.5f));
                Assert.That(spans[spans.Count - 1].Top, Is.LessThanOrEqualTo(height + .5f), spans[spans.Count - 1].Name);
                for (int i = 1; i < spans.Count; i++)
                    Assert.That(spans[i].Bottom, Is.GreaterThanOrEqualTo(spans[i - 1].Top - .5f),
                        $"{spans[i].Name} overlaps {spans[i - 1].Name} at safe height {height}.");
            }
        }

        // startWeapon: development/start-weapon.txt's weapon (every hero starts with its own alone since 2026-11-22).
        private IEnumerator LoadGameplay(PlayerProfile seed, string startWeapon = null)
        {
            TestProfile.Begin(seed);
            TestProfile.StartWith(startWeapon);
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

        // The rack's other places are gone from the scene with it (docs/63): one weapon card, the hero's own.
        private bool NoMoreWeaponCards() =>
            _forge.transform.Find("Forge Panel/Safe Area/Weapon Card 2") == null &&
            _forge.transform.Find("Forge Panel/Safe Area/Weapon Card 3") == null;

        private string WeaponCardText(int index, string label) => WeaponCard(index).transform.Find(label).GetComponent<Text>().text;

        private static Button FindButton(string name) => GameObject.Find(name).GetComponent<Button>();

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
