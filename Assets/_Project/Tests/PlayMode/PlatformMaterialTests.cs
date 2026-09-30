using System.Collections;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class PlatformMaterialTests
    {
        private Scene _scene;
        private ArenaView _arena;
        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene = SceneManager.GetActiveScene();
            _arena = Object.FindFirstObjectByType<ArenaView>();
            Time.timeScale = 0;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Platform material cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            ArenaSpriteCache.ResetSession();
            TestProfile.End();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealSceneUsesBoundedImportedTexturesBelowEffectsAndKeepsThePlayableBoundary()
        {
            Assert.That(_arena.Platform.UsesPaintedMaterials, Is.True);
            var set = _arena.PlatformMaterials;
            Assert.That(set.IsValid, Is.True);
            foreach (Material material in new[] { set.Floor, set.Coping, set.Wall })
            {
                var texture = (Texture2D)material.mainTexture;
                Assert.That(texture.isReadable, Is.False);
                Assert.That(Mathf.Max(texture.width, texture.height), Is.LessThanOrEqualTo(1024));
                Assert.That(texture.mipmapCount, Is.GreaterThan(1));
                Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
#if UNITY_ANDROID
                Assert.That(texture.format, Is.EqualTo(material == set.Coping ? TextureFormat.ASTC_4x4 : TextureFormat.ASTC_6x6),
                    "Non-power-of-two imports must not silently fall back to RGBA32 on Android.");
#endif
                TestContext.WriteLine($"[Platform] {texture.name}: {texture.width}x{texture.height} {texture.format}, mips {texture.mipmapCount}");
            }
            var meshes = _arena.Platform.GetComponentsInChildren<MeshFilter>();
            Assert.That(meshes.Length, Is.EqualTo(9));
            foreach (var mesh in meshes)
                Assert.That(mesh.GetComponent<MeshRenderer>().sortingOrder, Is.LessThan(_arena.GroundEffectSortingOrder));
            Vector3[] floor = meshes[0].sharedMesh.vertices;
            Assert.That(floor[0].y, Is.EqualTo(_arena.Geometry.WorldBottom * .95f).Within(.0001f));
            Assert.That(_arena.IsOnPlatform(8.9f, 0), Is.True);
            Assert.That(_arena.IsOnPlatform(9.1f, 0), Is.False);
            // All coping wedges meet at exactly the same outer and inner corner vertices.
            for (int i = 0; i < 4; i++)
            {
                Vector3[] a = meshes[1 + i].sharedMesh.vertices, b = meshes[1 + (i + 1) % 4].sharedMesh.vertices;
                Assert.That(a[1], Is.EqualTo(b[0]));
                Assert.That(a[2], Is.EqualTo(b[3]));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneReloadReusesMeshesMaterialsAndFoundationWithoutDestroyingBorrowedAssets()
        {
            var first = _arena.Platform.GetComponentsInChildren<MeshFilter>();
            var meshes = new Mesh[first.Length];
            for (int i = 0; i < first.Length; i++) meshes[i] = first[i].sharedMesh;
            Material floor = _arena.PlatformMaterials.Floor;
            Texture texture = floor.mainTexture;
            Sprite foundation = _arena.Platform.PlatformRenderer.sprite;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene = SceneManager.GetActiveScene();
            _arena = Object.FindFirstObjectByType<ArenaView>();
            yield return Resources.UnloadUnusedAssets();
            var current = _arena.Platform.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < current.Length; i++) Assert.That(current[i].sharedMesh, Is.SameAs(meshes[i]));
            Assert.That(_arena.Platform.PlatformRenderer.sprite, Is.SameAs(foundation));
            Assert.That(_arena.PlatformMaterials.Floor, Is.SameAs(floor));
            Assert.That(floor.mainTexture, Is.SameAs(texture));
            Assert.That(texture != null && foundation != null, Is.True);
        }

        [UnityTest]
        public IEnumerator DifferentGeometryOwnsAndReleasesItsMeshesWhileCachedSceneStaysAlive()
        {
            Mesh cached = _arena.Platform.GetComponentsInChildren<MeshFilter>()[0].sharedMesh;
            var other = new GameObject("Different painted geometry");
            var view = other.AddComponent<ForgePlatformView>();
            view.Build(new ArenaGeometry(-7, 11, 6), _arena.Material, -15, _arena.PlatformMaterials);
            Mesh privateMesh = view.GetComponentsInChildren<MeshFilter>()[0].sharedMesh;
            Sprite privateFoundation = view.PlatformRenderer.sprite;
            Assert.That(privateMesh, Is.Not.SameAs(cached));
            Assert.That(privateMesh.bounds.size.x, Is.InRange(11f, 12f));
            Object.Destroy(other);
            yield return null;
            yield return null;
            Assert.That(privateMesh == null && privateFoundation == null, Is.True);
            Assert.That(cached != null && _arena.PlatformMaterials.Floor.mainTexture != null, Is.True);
        }

        [UnityTest]
        public IEnumerator WideRegionKeepsHallPavingScaleAndOrthogonalFloorAxes()
        {
            var other = new GameObject("Wide paving check");
            var view = other.AddComponent<ForgePlatformView>();
            view.Build(new ArenaGeometry(-27, 27, 18), _arena.Material, -15, _arena.PlatformMaterials);
            Mesh hall = _arena.Platform.GetComponentsInChildren<MeshFilter>()[0].sharedMesh;
            Mesh wide = view.GetComponentsInChildren<MeshFilter>()[0].sharedMesh;
            foreach (Mesh mesh in new[] { hall, wide })
            {
                Vector3[] points = mesh.vertices;
                Vector2[] uv = mesh.uv;
                // Opposite corners isolate each floor axis, independent of the diamond's aspect ratio.
                Vector2 across = (uv[1] - uv[3]) / (points[1].x - points[3].x);
                Vector2 along = (uv[2] - uv[0]) / ((points[2].y - points[0].y) * 2f);
                Assert.That(across.x, Is.EqualTo(1f / 8.55f).Within(.00001f));
                Assert.That(across.y, Is.EqualTo(-1f / 8.55f).Within(.00001f));
                Assert.That(along.x, Is.EqualTo(across.x).Within(.00001f));
                Assert.That(along.y, Is.EqualTo(-across.y).Within(.00001f));
            }
            Assert.That(hall.uv[0], Is.EqualTo(Vector2.zero));
            Assert.That(hall.uv[2], Is.EqualTo(new Vector2(2, 2)));
            var wideParts = view.GetComponentsInChildren<MeshFilter>();
            var hallParts = _arena.Platform.GetComponentsInChildren<MeshFilter>();
            foreach (var parts in new[] { hallParts, wideParts })
            {
                for (int i = 1; i <= 4; i++)
                {
                    Vector3[] p = parts[i].sharedMesh.vertices;
                    Vector2 edge = new Vector2(p[1].x - p[0].x, (p[1].y - p[0].y) * 2);
                    Vector2 inset = new Vector2(p[3].x - p[0].x, (p[3].y - p[0].y) * 2);
                    float width = Mathf.Abs(edge.x * inset.y - edge.y * inset.x) / edge.magnitude;
                    Assert.That(width, Is.EqualTo(.45f / Mathf.Sqrt(2)).Within(.00001f));
                    Assert.That(parts[i].sharedMesh.uv[1].x / edge.magnitude,
                        Is.EqualTo(2f / Mathf.Sqrt(162)).Within(.00001f));
                    Vector3[] next = parts[1 + i % 4].sharedMesh.vertices;
                    Assert.That(p[1], Is.EqualTo(next[0]));
                    Assert.That(p[2], Is.EqualTo(next[3]));
                }
            }
            Object.Destroy(other);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SessionResetReleasesGeneratedMeshesButPreservesImportedMaterialAndTexture()
        {
            Mesh mesh = _arena.Platform.GetComponentsInChildren<MeshFilter>()[0].sharedMesh;
            Material material = _arena.PlatformMaterials.Floor;
            Texture texture = material.mainTexture;
            Object.Destroy(_arena.Platform.gameObject);
            yield return null;
            Assert.That(mesh != null, Is.True, "View borrows the session mesh.");
            ArenaSpriteCache.ResetSession();
            ArenaSpriteCache.ResetSession();
            yield return null;
            Assert.That(mesh == null, Is.True);
            Assert.That(material != null && texture != null && material.mainTexture == texture, Is.True);
        }
    }
}
