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
            try
            {
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
                foreach (var view in views)
                {
                    Assert.That(view.CavernRenderer.sprite, Is.SameAs(sprite));
                    Assert.That(view.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(1));
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
            }
            finally
            {
                Object.Destroy(root); Object.Destroy(shared); Object.Destroy(sprite); Object.Destroy(source);
            }
            yield return null;
        }
    }
}
