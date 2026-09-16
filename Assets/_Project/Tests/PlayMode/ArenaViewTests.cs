using System.Collections;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // The placeholder arena: depth sorting, the platform under every corner's pack slots, and the camera that follows the
    // hero between the HUD blocks.
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
        }

        // Packs of every size enter round the hero at the centre from all four corners, and every enemy starts on the
        // platform inside the margin the hero keeps from the rim. The Descent simulation walks its packs on this platform.
        [UnityTest]
        public IEnumerator EveryPackSlotAtEveryCornerEntersOnThePlatform()
        {
            ArenaGeometry geometry = _arena.Geometry;
            ArenaGeometry simulated = DescentSimulation.Platform;
            Assert.That((geometry.NearCorner, geometry.FarCorner, geometry.HalfWidth),
                Is.EqualTo((simulated.NearCorner, simulated.FarCorner, simulated.HalfWidth)), "The simulation's platform is the scene's.");
            Assert.That(_arena.IsOnPlatform(0f, 0f), Is.True, "The hero starts at the centre.");
            Assert.That(_arena.IsOnPlatform(0f, -8f), Is.True, "The platform reaches behind the hero.");
            var placements = new EntryPlacement[PackLayout.MaxPackSize];
            for (int wave = 0; wave < EntrySides.Count; wave++)
            {
                for (int count = 1; count <= PackLayout.MaxPackSize; count++)
                {
                    EntrySides.Place(wave, count, 0f, 0f, geometry, DescentSimulation.EntryDepth, DescentSimulation.FormationSpacing, placements);
                    for (int slot = 0; slot < count; slot++)
                    {
                        EntrySides.Formation(wave, slot, count, out EntrySide side, out _, out _);
                        string where = $"wave {wave}, {count} enemies, slot {slot}";
                        Assert.That(placements[slot].Side, Is.EqualTo(side), where + " keeps its corner");
                        Assert.That(geometry.IsOnPlatform(placements[slot].X, placements[slot].Y, HeroMotion.EdgeMargin), Is.True, where);
                    }
                }
            }
            Assert.That(_arena.IsOnPlatform(9.5f, 0f), Is.False, "Beyond the right corner is the void.");
            Assert.That(_arena.IsOnPlatform(5f, 5f), Is.False, "Beyond the edge between two corners too.");
            yield break;
        }

        // On a 1080 x 2340 phone the HUD leaves rows 620 to 1778 free: 4.6 world units show across the screen, the hero
        // stands on the band's middle row, and the camera glides after the hero when it walks.
        [UnityTest]
        public IEnumerator TheCameraShowsAFixedWidthAndFollowsTheHeroInTheFreeBand()
        {
            var follow = _camera.GetComponent<ArenaCameraFollow>();
            RectTransform enemyBar = GameObject.Find("Enemy Bar").GetComponent<RectTransform>();
            RectTransform heroLabel = GameObject.Find("Hero Label").GetComponent<RectTransform>();
            Assert.That(follow.TopHud, Is.SameAs(enemyBar), "The enemy bar is the lowest element of the top block.");
            Assert.That(follow.BottomHud, Is.SameAs(heroLabel), "The hero label is the highest element of the bottom block.");
            Assert.That(GameObject.Find("HUD Canvas").GetComponent<CanvasScaler>().referenceResolution.x, Is.EqualTo(1080f));
            var hero = GameObject.Find("Vanguard");
            Assert.That(follow.Target, Is.SameAs(hero.transform));

            const float width = 1080f;
            const float height = 2340f;
            float bandTop = height - 100f - (-enemyBar.anchoredPosition.y + enemyBar.sizeDelta.y);
            float bandBottom = heroLabel.anchoredPosition.y + heroLabel.sizeDelta.y;
            follow.Frame(width, height, bandBottom, bandTop);
            float size = _camera.orthographicSize;
            Assert.That(size * width / height, Is.EqualTo(follow.VisibleWidth / 2f).Within(1e-3f), "Half the visible width either side.");
            Assert.That(Row(hero.transform.position.y, size, _camera.transform.position.y, height), Is.EqualTo((bandBottom + bandTop) / 2f).Within(1f),
                "The hero's feet stand on the band's middle row.");

            // The hero walks right for half a second; the camera has closed most of the way after another half.
            var movement = hero.GetComponent<HeroMovementInput>();
            movement.Hold(new Vector2(1f, 0f));
            yield return new WaitForSeconds(0.5f);
            movement.Release();
            Assert.That(hero.transform.position.x, Is.GreaterThan(1f), "The hero walked.");
            yield return new WaitForSeconds(0.5f);
            Assert.That(_camera.transform.position.x, Is.EqualTo(hero.transform.position.x).Within(0.2f), "The camera followed.");
            Assert.That(_camera.transform.position.y, Is.EqualTo(hero.transform.position.y - follow.OffsetY).Within(0.2f));
        }

        [UnityTest]
        public IEnumerator BoundedCameraAndCavernCoverAllRimsAtBothPortraitHeights()
        {
            Time.timeScale = 0f;
            var follow = _camera.GetComponent<ArenaCameraFollow>();
            follow.enabled = false;
            SpriteRenderer cavern = _arena.Backdrop.CavernRenderer;
            Assert.That(cavern, Is.Not.Null, "The authored scene uses the imported cavern layer.");
            Assert.That(cavern.sprite.texture.isReadable, Is.False, "No retained CPU copy of the imported background.");
            foreach (int height in new[] { 1920, 2340 })
            foreach (Vector2 point in new[] { Vector2.zero, new Vector2(-8.5f, 0), new Vector2(8.5f, 0),
                         new Vector2(0, -4.25f), new Vector2(0, 4.25f) })
            {
                _camera.aspect = 1080f / height;
                follow.Target.position = new Vector3(point.x, point.y, 0);
                follow.Frame(1080, height, 361, height - 427);
                _arena.Backdrop.FrameCavern();
                var body = GameObject.Find("Hero Body").GetComponent<SpriteRenderer>().bounds;
                Vector3 lower = _camera.WorldToViewportPoint(body.min);
                Vector3 upper = _camera.WorldToViewportPoint(body.max);
                Assert.That(lower.x, Is.GreaterThan(0f));
                Assert.That(upper.x, Is.LessThan(1f));
                Assert.That(lower.y, Is.GreaterThan(361f / height));
                Assert.That(upper.y, Is.LessThan((height - 427f) / height));
                Bounds background = cavern.bounds;
                float halfHeight = _camera.orthographicSize;
                float halfWidth = halfHeight * _camera.aspect;
                Vector3 centre = _camera.transform.position;
                Assert.That(background.min.x, Is.LessThan(centre.x - halfWidth));
                Assert.That(background.max.x, Is.GreaterThan(centre.x + halfWidth));
                Assert.That(background.min.y, Is.LessThan(centre.y - halfHeight));
                Assert.That(background.max.y, Is.GreaterThan(centre.y + halfHeight));
                Assert.That(cavern.transform.localScale.x, Is.EqualTo(cavern.transform.localScale.y), "Preserve source proportions.");
                if (point.y > 4f)
                    Assert.That(centre.y + follow.OffsetY, Is.LessThan(3f), "The actual scene must bind the arena bounds.");
            }
            yield break;
        }

        private static float Row(float worldY, float orthographicSize, float cameraY, float screenHeight) =>
            (worldY - (cameraY - orthographicSize)) / (2f * orthographicSize) * screenHeight;
    }
}
