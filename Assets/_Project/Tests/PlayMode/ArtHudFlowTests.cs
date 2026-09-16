using System.Collections;
using System.Collections.Generic;
using Cryptforge.Art;
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
    public sealed class ArtHudFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Art HUD Cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator SharedEnvironmentSurvivesReloadWhileMeshesAndAnimationStayLocal()
        {
            Time.timeScale = 0f;
            ArenaView previous = Object.FindFirstObjectByType<ArenaView>();
            Sprite platform = previous.Platform.PlatformRenderer.sprite;
            Sprite stars = previous.Backdrop.transform.Find("Stars 0").GetComponent<SpriteRenderer>().sprite;
            Mesh oldSky = previous.Backdrop.SkyRenderer.GetComponent<MeshFilter>().sharedMesh;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return Resources.UnloadUnusedAssets();
            Time.timeScale = 0f;
            ArenaView current = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(current.Platform.PlatformRenderer.sprite, Is.SameAs(platform));
            Assert.That(current.Backdrop.transform.Find("Stars 0").GetComponent<SpriteRenderer>().sprite, Is.SameAs(stars));
            Assert.That(oldSky == null, Is.True, "Small view-owned meshes must still be freed on scene unload.");
            Transform island = current.Backdrop.transform.Find("Island 0");
            Vector3 paused = island.localPosition;
            Sprite light = current.Platform.LightsRenderer.sprite;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(island.localPosition, Is.EqualTo(paused));
            Assert.That(current.Platform.LightsRenderer.sprite, Is.SameAs(light));
            Time.timeScale = 1f;
            bool flickered = false;
            bool drifted = false;
            float stop = Time.time + .6f;
            while (Time.time < stop)
            {
                yield return null;
                flickered |= current.Platform.LightsRenderer.sprite != light;
                drifted |= island.localPosition != paused;
            }
            Assert.That(flickered && drifted, Is.True, "Shared textures must not freeze per-view animation.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SharedEnemyArtSurvivesRestartAndUnusedAssetCleanup()
        {
            EnemyLookView first = Object.FindFirstObjectByType<EnemyLookView>();
            EnemyLook look = first.Look;
            Sprite frame = EnemySpriteCache.Get(look).Frame(EnemyPose.IdleA);
            Sprite flash = first.SilhouetteOf(frame);
            Sprite bar = first.transform.Find("Health Bar/Fill").GetComponent<SpriteRenderer>().sprite;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return Resources.UnloadUnusedAssets();
            EnemyLookView next = Object.FindFirstObjectByType<EnemyLookView>();
            Assert.That(next.Look, Is.EqualTo(look));
            Assert.That(frame != null && flash != null && bar != null, Is.True);
            Assert.That(EnemySpriteCache.Get(look).Frame(EnemyPose.IdleA), Is.SameAs(frame));
            Assert.That(next.SilhouetteOf(frame), Is.SameAs(flash));
            Assert.That(next.transform.Find("Health Bar/Fill").GetComponent<SpriteRenderer>().sprite, Is.SameAs(bar));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SharedEnemySpritesKeepHealthAndHitFeedbackIndependentWhenOneEnemyDies()
        {
            Time.timeScale = 0f;
            EnemyLookView source = Object.FindFirstObjectByType<EnemyLookView>();
            EnemyLookView first = Object.Instantiate(source);
            EnemyLookView second = Object.Instantiate(source);
            first.GetComponent<Health>().Initialize(100f);
            second.GetComponent<Health>().Initialize(100f);
            // Cloning a live view also copies its generated bar; find the newly built bar via the last child.
            var firstFill = first.transform.GetChild(first.transform.childCount - 1).Find("Fill").GetComponent<SpriteRenderer>();
            var secondFill = second.transform.GetChild(second.transform.childCount - 1).Find("Fill").GetComponent<SpriteRenderer>();
            Assert.That(firstFill.sprite, Is.SameAs(secondFill.sprite));
            first.GetComponent<Health>().ApplyDamage(new DamageContext(25f));
            Assert.That(firstFill.transform.localScale.x, Is.EqualTo(.75f));
            Assert.That(secondFill.transform.localScale.x, Is.EqualTo(1f));
            Assert.That(first.GetComponent<CombatantView>().IsFlashing, Is.True);
            Assert.That(second.GetComponent<CombatantView>().IsFlashing, Is.False);
            second.GetComponent<Health>().ApplyDamage(new DamageContext(10f, isCritical: true));
            Assert.That(second.GetComponent<CombatantView>().FlashColor,
                Is.Not.EqualTo(first.GetComponent<CombatantView>().FlashColor));
            Sprite frame = EnemySpriteCache.Get(source.Look).Frame(EnemyPose.IdleA);
            Sprite flash = second.SilhouetteOf(frame);
            first.GetComponent<Health>().ApplyDamage(new DamageContext(100f));
            Object.Destroy(first.gameObject);
            yield return null;
            Assert.That(second.HasSprites, Is.True);
            Assert.That(frame != null && frame.texture != null && flash != null && flash.texture != null, Is.True);
            Assert.That(second.SilhouetteOf(frame), Is.SameAs(flash));
            Assert.That(secondFill.sprite != null && secondFill.sprite.texture != null, Is.True);
            Assert.That(second.GetComponent<Health>().Current, Is.EqualTo(90f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CompactReadoutKeepsDetailsAccessibleThroughPause()
        {
            CanvasGroup details = GameObject.Find("Run Details").GetComponent<CanvasGroup>();
            Assert.That(details.alpha, Is.Zero);
            Assert.That(details.blocksRaycasts, Is.False, "Hidden statistics must never intercept steering.");
            Assert.That(GameObject.Find("Boss Readout").GetComponent<CanvasGroup>().alpha, Is.Zero,
                "A normal pack does not display a boss bar.");
            _setup.Run.AddGold(999999);
            Text gold = GameObject.Find("Gold Count").GetComponent<Text>();
            Assert.That(gold.text, Is.EqualTo("999999"));
            Assert.That(GameObject.Find("Gold Label").GetComponent<Text>().text, Does.Contain("999999 at risk"));
            Assert.That(_setup.Pause.TryPause(), Is.True);
            Assert.That(details.alpha, Is.EqualTo(1f));
            Assert.That(GameObject.Find("Weapon Label").GetComponent<Text>().text, Does.Contain("damage every"));
            Assert.That(_setup.Pause.TryResume(), Is.True);
            Assert.That(details.alpha, Is.Zero);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CardArtworkFollowsContentAfterTheFirstUpgradeHitsItsCap()
        {
            UpgradeOption damage = _setup.Upgrades.Pool[0];
            for (int i = 0; i < damage.MaxStacks; i++)
            {
                _setup.Run.GrantBonusUpgrade();
                Assert.That(_setup.Upgrades.TrySelect(_setup.Upgrades.CurrentOffer, 0), Is.True);
            }
            _setup.Run.GrantBonusUpgrade();
            Assert.That(_setup.Upgrades.CurrentOffer.Choices[0].Stat, Is.EqualTo(WeaponStat.AttackSpeed));
            GameObject first = GameObject.Find("Choice Button 1");
            Image icon = first.transform.Find("Choice Icon").GetComponent<Image>();
            Assert.That(icon.enabled, Is.True);
            Assert.That(icon.sprite.name, Is.EqualTo("UI_Icon_QuickenedGrip_v1"), "Slot zero now holds the other upgrade.");
            Assert.That(GameObject.Find("Damage Badge").transform.Find("Stack Count").GetComponent<Text>().text,
                Is.EqualTo(damage.MaxStacks.ToString()));
            yield return new WaitForSecondsRealtime(.3f);

            // Real raycast through the decorative icon must still reach the whole card's button.
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) { position = icon.rectTransform.position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(first.GetComponent<Button>()));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(_setup.Choices.IsOpen, Is.False);
            Assert.That(_setup.Upgrades.StacksOf(_setup.Upgrades.Pool[1]), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ImportedAbilityGlyphSurvivesSceneRestart()
        {
            Sprite original = GameObject.Find("Ability Icon").GetComponent<Image>().sprite;
            Assert.That(original.name, Is.EqualTo("UI_Icon_ForgeBurst_v1"));
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Assert.That(original != null, Is.True, "A view must not destroy a shared imported sprite on unload.");
            Assert.That(GameObject.Find("Ability Icon").GetComponent<Image>().sprite, Is.SameAs(original));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ResultAndForgeTextFitsShortAndTallPortraitSafeAreas()
        {
            _setup.Run.End(RunOutcome.Defeat);
            Object.FindFirstObjectByType<RelicForgeView>().Open();
            yield return null;
            foreach (string panelName in new[] { "Result Panel", "Forge Panel" })
            {
                var safe = GameObject.Find(panelName).transform.Find("Safe Area").GetComponent<RectTransform>();
                safe.GetComponent<SafeAreaFitter>().enabled = false;
                safe.anchorMin = safe.anchorMax = new Vector2(.5f, .5f);
                foreach (float height in new[] { 1760f, 2232f })
                {
                    safe.sizeDelta = new Vector2(1080f, height);
                    Canvas.ForceUpdateCanvases();
                    foreach (Text label in safe.GetComponentsInChildren<Text>())
                    {
                        var settings = label.GetGenerationSettings(new Vector2(label.rectTransform.rect.width, 0));
                        settings.resizeTextForBestFit = false;
                        settings.fontSize = label.resizeTextMinSize;
                        float needed = label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
                        Assert.That(needed, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                            $"{panelName}/{label.name} truncates even at its minimum font size on a {height}-unit safe area.");
                    }
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ThreeCameraCandidatesContainTheGuardianAtCentreAndRim()
        {
            var camera = Camera.main;
            var follow = camera.GetComponent<ArenaCameraFollow>();
            Assert.That(follow.VisibleWidth, Is.EqualTo(9f), "The wider portrait framing is selected for movement review.");
            // Full Warden rectangle, including attack pose and health bar, at the largest stopping radius. Camera
            // remains hero-relative at the rim. A further 0.35 units budgets the follow lag while walking.
            float halfBody = EnemyArt.WardenWidth / 64f;
            float top = (EnemyArt.WardenHeight + EnemyArt.HealthBarHeight + 3f) / 32f;
            foreach (float width in new[] { 6f, 7.5f, 9f })
                foreach (float height in new[] { 1920f, 2340f })
                    foreach (float heroX in new[] { 0f, -8.4f, 8.4f })
                    {
                        const float bottom = 285f;
                        float bandTop = height - 100f - 327f;
                        FollowFraming.Fit(1080f, height, bottom, bandTop, width, out float size, out float offset);
                        float left = heroX - width / 2f;
                        float right = heroX + width / 2f;
                        Assert.That(heroX - 1.6f - halfBody - .35f, Is.GreaterThan(left));
                        Assert.That(heroX + 1.6f + halfBody + .35f, Is.LessThan(right));
                        float guardianTopRow = (top + .8f + offset + size) / (2 * size) * height;
                        Assert.That(guardianTopRow, Is.LessThan(bandTop), "The guardian's bar stays below the top HUD.");
                    }
            yield return null;
        }
    }
}
