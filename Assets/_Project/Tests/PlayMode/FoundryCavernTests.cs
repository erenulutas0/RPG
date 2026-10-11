using System.Collections;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class FoundryCavernTests
    {
        [UnityTest] public IEnumerator ReloadReleasesOnlyOwnedAtmosphereAndKeepsBorrowedArt()
        {
            var root = new GameObject("Cavern ownership test");
            var source = new Texture2D(4, 4);
            var sprite = Sprite.Create(source, new Rect(0, 0, 4, 4), Vector2.one * .5f, 4);
            var shared = new Material(Shader.Find("Sprites/Default"));
            var foundry = Resources.Load<Sprite>("FoundryCavernV2");
            try
            {
                Assert.That(foundry, Is.Not.Null, "The shipped foundry composition must import as a Resources sprite.");
                Assert.That(foundry.texture.isReadable, Is.False, "The optional plate retains no CPU pixel copy.");
                Assert.That(foundry.texture.mipmapCount, Is.EqualTo(1), "NPOT sprite mipmaps would disable compressed import in the pinned Editor.");
#if UNITY_ANDROID
                Assert.That(foundry.texture.format, Is.EqualTo(TextureFormat.ASTC_6x6), "Check loaded Android format, not just the importer request.");
#endif
                var camera = root.AddComponent<Camera>();
                camera.orthographic = true;
                var views = new VoidBackdropView[3];
                for (int i = 0; i < views.Length; i++)
                {
                    var child = new GameObject("Cavern " + i);
                    child.transform.SetParent(root.transform, false);
                    views[i] = child.AddComponent<VoidBackdropView>();
                    views[i].Build(i < 2 ? ArenaGeometry.Foundry : new ArenaGeometry(-9, 9, 9), shared, -20, sprite, camera);
                }
                var owned = views[0].CavernRenderer.sharedMaterial;
                var second = views[1].CavernRenderer.sharedMaterial;
                Assert.That(owned.shader.name, Is.EqualTo("Cryptforge/Foundry Cavern"));
                Assert.That(second, Is.Not.SameAs(owned));
                Assert.That(views[2].CavernRenderer.sharedMaterial.shader.name, Is.EqualTo("Sprites/Default"));
                Assert.That(views[2].CavernRenderer.color, Is.EqualTo(Color.white));
                for (int i = 0; i < 2; i++)
                {
                    Assert.That(views[i].SkyRenderer.sortingOrder, Is.EqualTo(-22));
                    Assert.That(views[i].CavernRenderer.sortingOrder, Is.EqualTo(-21));
                }
                Assert.That(views[2].SkyRenderer.sortingOrder, Is.EqualTo(-20), "Non-foundry backdrops keep their existing ranks.");
                Assert.That(views[2].CavernRenderer.sortingOrder, Is.EqualTo(-19));
                for (int i = 0; i < views.Length; i++)
                {
                    Assert.That(views[i].CavernRenderer.sprite, Is.SameAs(i < 2 ? foundry : sprite),
                        "Only foundry geometry selects the versioned composition.");
                    Assert.That(views[i].GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(1));
                }
                Object.Destroy(views[0].gameObject);
                yield return null;
                yield return null;
                Assert.That(owned == null, Is.True, "Reload must release the scene-owned atmosphere material.");
                Assert.That(second != null && source != null && sprite != null && shared != null, Is.True);
                Object.Destroy(views[1].gameObject);
                yield return null;
                yield return null;
                Assert.That(second == null, Is.True);
                Assert.That(views[2].CavernRenderer.sprite, Is.SameAs(sprite));
                Assert.That(foundry != null && foundry.texture != null, Is.True, "Destroying every foundry view preserves its borrowed replacement sprite and texture.");
            }
            finally
            {
                Object.Destroy(root); Object.Destroy(shared); Object.Destroy(sprite); Object.Destroy(source);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator FoundryPlateAndMissingResourceFallbackCoverRoamingCameras()
        {
            var root = new GameObject("Cavern cover test");
            var texture = new Texture2D(8, 4);
            var original = Sprite.Create(texture, new Rect(0, 0, 8, 4), Vector2.one * .5f, 4, 0, SpriteMeshType.FullRect);
            var shared = new Material(Shader.Find("Sprites/Default"));
            try
            {
                var camera = root.AddComponent<Camera>();camera.orthographic = true;
                var replacement = Resources.Load<Sprite>("FoundryCavernV2");
                Assert.That(replacement, Is.Not.Null);
                var views = new VoidBackdropView[3];
                for (int i = 0; i < views.Length; i++)
                {
                    var child = new GameObject("Cover case " + i);child.transform.SetParent(root.transform, false);
                    views[i] = child.AddComponent<VoidBackdropView>();
                    if (i == 0) views[i].Build(ArenaGeometry.Foundry, shared, -20, original, camera);
                    else views[i].Build(i == 1 ? ArenaGeometry.Foundry : new ArenaGeometry(-9, 9, 9),
                        shared, -20, original, camera, i == 1 ? null : replacement);
                    Assert.That(views[i].CavernRenderer.sprite, Is.SameAs(i == 0 ? replacement : original),
                        "A missing optional plate falls back to supplied art, and ordinary arenas ignore foundry art.");
                }
                foreach (float aspect in new[] { 1080f / 2340, 1080f / 1920, 4f / 3, 21f / 9 })
                foreach (float halfHeight in new[] { 4.5f, 25f, 40f })
                foreach (Vector2 position in new[] { Vector2.zero, new Vector2(-40, -40), new Vector2(40, 40), new Vector2(-40, 40) })
                {
                    camera.aspect = aspect;camera.orthographicSize = halfHeight;
                    camera.transform.position = new Vector3(position.x, position.y, -10);
                    foreach (var view in views)
                    {
                        view.FrameCavern();var renderer = view.CavernRenderer;Bounds bounds = renderer.bounds;
                        float halfWidth = halfHeight * aspect;
                        Assert.That(bounds.min.x, Is.LessThan(position.x - halfWidth));
                        Assert.That(bounds.max.x, Is.GreaterThan(position.x + halfWidth));
                        Assert.That(bounds.min.y, Is.LessThan(position.y - halfHeight));
                        Assert.That(bounds.max.y, Is.GreaterThan(position.y + halfHeight));
                        Assert.That(renderer.transform.localScale.x, Is.EqualTo(renderer.transform.localScale.y), "Cover fitting never stretches the plate.");
                        Assert.That(Mathf.Abs(renderer.transform.position.x - position.x), Is.LessThanOrEqualTo(halfWidth * .05f + .0001f));
                        Assert.That(Mathf.Abs(renderer.transform.position.y - position.y), Is.LessThanOrEqualTo(halfHeight * .05f + .0001f), "Parallax remains bounded across the explorable foundry.");
                    }
                }
            }
            finally { Object.Destroy(root);Object.Destroy(shared);Object.Destroy(original);Object.Destroy(texture); }
            yield return null;
        }

        [UnityTest] public IEnumerator MissingOriginalPlateOrCameraRetainsProceduralVoid()
        {
            var root = new GameObject("Optional cavern prerequisites test");
            var texture = new Texture2D(4, 4);
            var original = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * .5f, 4);
            var shared = new Material(Shader.Find("Sprites/Default"));
            try
            {
                var camera = root.AddComponent<Camera>();camera.orthographic = true;
                for (int i = 0; i < 2; i++)
                {
                    var child = new GameObject("Missing prerequisite " + i);child.transform.SetParent(root.transform, false);
                    var view = child.AddComponent<VoidBackdropView>();
                    view.Build(ArenaGeometry.Foundry, shared, -20, i == 0 ? null : original, i == 0 ? camera : null);
                    Assert.That(view.CavernRenderer, Is.Null, "An available foundry resource cannot opt a caller into cavern rendering.");
                    Assert.That(view.SkyRenderer.sortingOrder, Is.EqualTo(-20));
                    Assert.That(view.GetComponentsInChildren<SpriteRenderer>().Length, Is.GreaterThan(1), "The complete procedural fallback remains available.");
                }
            }
            finally { Object.Destroy(root);Object.Destroy(shared);Object.Destroy(original);Object.Destroy(texture); }
            yield return null;
        }
    }
}
