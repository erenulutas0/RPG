using System.Collections;
using Cryptforge.Combat;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // The placeholder arena: depth sorting, the platform under every pack slot, and the framing between the HUD blocks.
    public sealed class ArenaViewTests
    {
        private ArenaView _arena;
        private Camera _camera;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _arena = Object.FindFirstObjectByType<ArenaView>();
            _camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Arena Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator FartherSpritesDrawFirstAndTheArenaStaysBehindEveryCombatant()
        {
            yield return null;
            Assert.That(_camera.transparencySortMode, Is.EqualTo(TransparencySortMode.CustomAxis));
            Assert.That(_camera.transparencySortAxis, Is.EqualTo(Vector3.up), "Sprites higher on the screen stand farther away.");

            MeshRenderer arena = _arena.ArenaRenderer;
            Assert.That(arena, Is.Not.Null);
            Assert.That(arena.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0), "The sky was built.");
            Assert.That(_arena.Platform.PlatformRenderer.sprite, Is.Not.Null, "The platform was drawn.");
            Assert.That(_arena.Platform.PlatformRenderer.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point), "Pixel art stays crisp.");
            SpriteRenderer[] sprites = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(sprites.Length, Is.GreaterThan(2), "The hero and the first pack are in the scene.");
            var heroBody = GameObject.Find("Hero Body").GetComponent<SpriteRenderer>();
            Assert.That(heroBody.sprite.name, Does.StartWith("Hero Body"), "The hero wears its drawn look.");
            foreach (SpriteRenderer sprite in sprites)
            {
                Assert.That(sprite.sortingLayerID, Is.EqualTo(arena.sortingLayerID), sprite.name);
                Assert.That(sprite.sortingOrder, Is.GreaterThan(arena.sortingOrder), sprite.name);
            }
            // Backdrop props draw under the platform, the platform under every combatant.
            SpriteRenderer platform = _arena.Platform.PlatformRenderer;
            foreach (SpriteRenderer sprite in _arena.Backdrop.GetComponentsInChildren<SpriteRenderer>(true))
                Assert.That(sprite.sortingOrder, Is.LessThan(platform.sortingOrder), sprite.name);
            Assert.That(heroBody.sortingOrder, Is.GreaterThan(platform.sortingOrder));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryPackSlotEntersOnThePlatformInFrontOfTheHero()
        {
            Assert.That(_arena.IsOnPlatform(0f, 0f), Is.True, "The hero stands on the platform.");
            Assert.That(_arena.IsOnPlatform(0f, -1f), Is.True, "The platform continues behind the hero.");
            for (int count = 1; count <= PackLayout.MaxPackSize; count++)
            {
                for (int slot = 0; slot < count; slot++)
                {
                    PackLayout.Offset(slot, count, DescentSimulation.FormationSpacing, out float x, out float y);
                    Assert.That(_arena.IsOnPlatform(x, DescentSimulation.EntryDepth + y), Is.True, $"{count} enemies, slot {slot}");
                }
            }
            Assert.That(_arena.IsOnPlatform(3.5f, 5f), Is.False, "Beyond the side corner is the void.");
            yield break;
        }

        // Both HUD blocks hang from the safe area's edges. On a 1080 x 2340 phone with a 100-row cutout inset, the canvas scale
        // is 1, so the free band follows from their layout in canvas units.
        [UnityTest]
        public IEnumerator OnThePhoneTheHeroAndEveryPackSlotFitBetweenTheHudBlocks()
        {
            var framing = _camera.GetComponent<ArenaCameraFraming>();
            RectTransform enemyBar = GameObject.Find("Enemy Bar").GetComponent<RectTransform>();
            RectTransform heroLabel = GameObject.Find("Hero Label").GetComponent<RectTransform>();
            Assert.That(framing.TopHud, Is.SameAs(enemyBar), "The enemy bar is the lowest element of the top block.");
            Assert.That(framing.BottomHud, Is.SameAs(heroLabel), "The hero label is the highest element of the bottom block.");
            Assert.That(enemyBar.anchorMin.y, Is.EqualTo(1f));
            Assert.That(enemyBar.pivot.y, Is.EqualTo(1f));
            Assert.That(heroLabel.anchorMin.y, Is.Zero);
            Assert.That(heroLabel.pivot.y, Is.Zero);
            Assert.That(GameObject.Find("HUD Canvas").GetComponent<CanvasScaler>().referenceResolution.x, Is.EqualTo(1080f));

            const float width = 1080f;
            const float height = 2340f;
            float bandTop = height - 100f - (-enemyBar.anchoredPosition.y + enemyBar.sizeDelta.y);
            float bandBottom = heroLabel.anchoredPosition.y + heroLabel.sizeDelta.y;
            framing.Frame(width, height, bandBottom, bandTop);
            float size = _camera.orthographicSize;
            float cameraY = _camera.transform.position.y;

            Bounds hero = GameObject.Find("Hero Body").GetComponent<SpriteRenderer>().bounds;
            Assert.That(Row(hero.min.y, size, cameraY, height), Is.GreaterThanOrEqualTo(bandBottom), "The hero's feet clear the bottom block.");
            float visibleHalfWidth = size * width / height;
            for (int count = 1; count <= PackLayout.MaxPackSize; count++)
            {
                for (int slot = 0; slot < count; slot++)
                {
                    PackLayout.Offset(slot, count, DescentSimulation.FormationSpacing, out float x, out float y);
                    float worldY = ArenaFloor.WorldY(DescentSimulation.EntryDepth + y);
                    Assert.That(Row(worldY, size, cameraY, height), Is.LessThanOrEqualTo(bandTop), $"{count} enemies, slot {slot} row");
                    Assert.That(Mathf.Abs(x), Is.LessThanOrEqualTo(visibleHalfWidth), $"{count} enemies, slot {slot} column");
                }
            }
            yield break;
        }

        private static float Row(float worldY, float orthographicSize, float cameraY, float screenHeight) =>
            (worldY - (cameraY - orthographicSize)) / (2f * orthographicSize) * screenHeight;
    }
}
