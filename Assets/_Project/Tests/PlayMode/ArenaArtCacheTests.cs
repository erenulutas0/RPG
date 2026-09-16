using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class ArenaArtCacheTests
    {
        private GameObject _root;
        private Material _material;
        private static readonly ArenaGeometry Geometry = new ArenaGeometry(-9f, 9f, 9f);

        [SetUp]
        public void Create()
        {
            ArenaSpriteCache.ResetSession();
            _root = new GameObject("Arena cache test");
            _material = new Material(Shader.Find("Sprites/Default"));
        }

        [UnityTearDown]
        public IEnumerator Release()
        {
            Object.Destroy(_root);
            Object.Destroy(_material);
            yield return null;
            ArenaSpriteCache.ResetSession();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RecordColdAndRepeatedEnvironmentBuildCosts()
        {
            SpriteRenderer[] original = null;
            GameObject firstRoot = null;
            for (int repeat = 0; repeat < 2; repeat++)
            {
                var instance = new GameObject("Environment " + repeat);
                instance.transform.SetParent(_root.transform, false);
                var platformObject = new GameObject("Platform " + repeat);
                platformObject.transform.SetParent(instance.transform, false);
                var platform = platformObject.AddComponent<ForgePlatformView>();
                Measure("platform " + repeat, () => platform.Build(Geometry, _material, -15));
                var backdropObject = new GameObject("Backdrop " + repeat);
                backdropObject.transform.SetParent(instance.transform, false);
                var backdrop = backdropObject.AddComponent<VoidBackdropView>();
                Measure("backdrop " + repeat, () => backdrop.Build(Geometry, _material, -20));
                Assert.That(platform.PlatformRenderer.sprite != null && backdrop.SkyRenderer != null, Is.True);
                SpriteRenderer[] current = instance.GetComponentsInChildren<SpriteRenderer>();
                if (repeat == 0)
                {
                    original = current;
                    firstRoot = instance;
                }
                else
                {
                    Assert.That(current.Length, Is.EqualTo(original.Length));
                    for (int i = 0; i < current.Length; i++)
                    {
                        Assert.That(current[i].sprite, Is.SameAs(original[i].sprite), current[i].name);
                        Assert.That(current[i].transform.localPosition, Is.EqualTo(original[i].transform.localPosition));
                        Assert.That(current[i].sortingOrder, Is.EqualTo(original[i].sortingOrder));
                    }
                    Object.Destroy(firstRoot);
                    yield return null;
                    yield return Resources.UnloadUnusedAssets();
                    foreach (SpriteRenderer renderer in current)
                        Assert.That(renderer.sprite != null && renderer.sprite.texture != null, Is.True, renderer.name);
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator OtherGeometryUsesItsOwnDimensionsAndReleasesOnlyItsOwnArt()
        {
            PlatformSprites cached = ArenaSpriteCache.Platform(Geometry, out bool shared);
            Assert.That(shared, Is.True);
            Sprite surface = cached.Surface;
            foreach (ArenaGeometry geometry in new[]
                { new ArenaGeometry(-9f, 9f, 7f), new ArenaGeometry(-7f, 11f, 9f), new ArenaGeometry(-8f, 9f, 9f) })
            {
                var instance = new GameObject("Other geometry");
                instance.transform.SetParent(_root.transform, false);
                var view = instance.AddComponent<ForgePlatformView>();
                view.Build(geometry, _material, -15);
                Sprite privateSprite = view.PlatformRenderer.sprite;
                Texture2D privateTexture = privateSprite.texture;
                var layout = new PlatformLayout(geometry);
                Assert.That(privateSprite, Is.Not.SameAs(surface));
                Assert.That(privateSprite.rect.size, Is.EqualTo(new Vector2(layout.Width, layout.Height)));
                Assert.That(view.PlatformRenderer.transform.localPosition.y, Is.EqualTo(layout.WorldBottom));
                Object.Destroy(instance);
                yield return null;
                yield return null; // OnDestroy queues native resources for destruction.
                Assert.That(privateSprite == null && privateTexture == null, Is.True);
                Assert.That(ArenaSpriteCache.Platform(Geometry, out shared), Is.SameAs(cached));
                Assert.That(shared && surface != null && surface.texture != null, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator ResetReleasesEverySharedTextureIncludingHiddenAnimationFrames()
        {
            var all = new List<Sprite>();
            all.Add(ArenaSpriteCache.ContactShadow);
            PlatformSprites platform = ArenaSpriteCache.Platform(Geometry, out _);
            all.Add(platform.Surface);
            for (int i = 0; i < PlatformArt.FrameCount; i++)
                all.Add(platform.Light(i));
            VoidSprites backdrop = ArenaSpriteCache.Backdrop;
            for (int i = 0; i < backdrop.SpriteCount; i++)
                all.Add(backdrop.SpriteAt(i));
            var textures = new List<Texture2D>();
            long texelBytes = 0;
            foreach (Sprite sprite in all)
            {
                textures.Add(sprite.texture);
                texelBytes += sprite.texture.width * (long)sprite.texture.height * 4;
            }
            TestContext.WriteLine($"[ArenaArt] retained {all.Count} RGBA32 textures, {texelBytes} texel bytes (native overhead excluded)");
            ArenaSpriteCache.ResetSession();
            ArenaSpriteCache.ResetSession();
            yield return null;
            foreach (Sprite sprite in all)
                Assert.That(sprite == null, Is.True);
            foreach (Texture2D texture in textures)
                Assert.That(texture == null, Is.True);
            Assert.That(ArenaSpriteCache.Platform(Geometry, out _).Surface != null, Is.True);
            Assert.That(ArenaSpriteCache.Backdrop.Galaxy != null, Is.True);
            Assert.That(ArenaSpriteCache.ContactShadow != null, Is.True);
        }

        private static long Measure(string label, Action build)
        {
            // This Editor's Mono returns zero from GC.GetAllocatedBytesForCurrentThread. Count real GC.Alloc events
            // instead; the device probe remains the source of whole-frame allocation bytes.
            using (var allocations = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 65536,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                long started = Stopwatch.GetTimestamp();
                build();
                double milliseconds = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
                allocations.Stop();
                Assert.That(allocations.Valid, Is.True);
                Assert.That(allocations.Count, Is.LessThan(allocations.Capacity), "The recording must not truncate.");
                TestContext.WriteLine(FormattableString.Invariant($"[ArenaArt] {label}: {allocations.Count} allocation events, {milliseconds:0.00} ms (Editor, not device)"));
                return allocations.Count;
            }
        }
    }
}
