using System.Collections;
using System.IO;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class RegionSceneryTests
    {
        private Scene _scene;
        [SetUp] public void Begin() => TestProfile.Begin();
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene = SceneManager.GetActiveScene();
            Time.timeScale = 0;
        }
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Region cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
        // The foundry is the game's first floor: only an explicit "hall" (the scene tests' default, see TestProfile) or
        // "wide" selects another arena; a missing or unknown flag plays the foundry.
        [UnityTest] public IEnumerator HallIsExplicitAndAMissingOrUnknownFlagPlaysTheFoundry()
        {
            yield return Load();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.Geometry.HalfWidth, Is.EqualTo(9));
            Assert.That(arena.Scenery, Is.Null);
            Assert.That(arena.IsFoundryRegion, Is.False);
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            foreach (string flag in new[] { "unknown", null })
            {
                if (flag == null) File.Delete(DevelopmentStart.RegionPreviewPath);
                else File.WriteAllText(DevelopmentStart.RegionPreviewPath, flag);
                yield return Load();
                arena = Object.FindFirstObjectByType<ArenaView>();
                Assert.That(arena.IsWideRegionPreview, Is.False);
                Assert.That(arena.IsFoundryRegion, Is.True, $"Flag '{flag ?? "missing"}' plays the foundry.");
                Assert.That(arena.Region, Is.Not.Null);
            }
        }
        [UnityTest] public IEnumerator WidePreviewBorrowsArtAndReturnsToHallOnReload()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "wide");
            yield return Load();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.Geometry.HalfWidth, Is.EqualTo(18));
            Assert.That(arena.Geometry.HalfDepth, Is.EqualTo(27));
            Assert.That(arena.Scenery, Is.Not.Null);
            var renderer = arena.Scenery.GetComponentInChildren<SpriteRenderer>();
            Sprite sprite = renderer.sprite;
            Material material = arena.Scenery.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            Mesh mesh = arena.Scenery.GetComponentInChildren<MeshFilter>().sharedMesh;
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.isReadable, Is.False);
            Assert.That(arena.IsOnPlatform(renderer.transform.position.x, renderer.transform.position.y * 2), Is.False);
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"hall");
            Assert.That(arena.Geometry.HalfWidth, Is.EqualTo(18), "Geometry is fixed for the loaded scene.");
            yield return Load();
            yield return null;
            arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.Geometry.HalfWidth, Is.EqualTo(9));
            Assert.That(arena.Scenery, Is.Null);
            Assert.That(mesh == null, Is.True);
            Assert.That(sprite != null && material != null, Is.True, "Borrowed imports survive scene teardown.");
        }
    }
}
