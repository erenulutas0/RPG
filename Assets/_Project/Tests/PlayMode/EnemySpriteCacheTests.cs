using System;
using System.Collections;
using System.Collections.Generic;
using Cryptforge.Art;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class EnemySpriteCacheTests
    {
        [UnityTearDown]
        public IEnumerator Release()
        {
            EnemySpriteCache.ResetSession();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryLookSharesOnlyItsOwnFramesAndKeepsTheArtContract()
        {
            EnemySpriteCache.ResetSession();
            var sprites = new HashSet<Sprite>();
            foreach (EnemyLook look in Enum.GetValues(typeof(EnemyLook)))
            {
                EnemySprites set = EnemySpriteCache.Get(look);
                foreach (EnemyPose pose in Enum.GetValues(typeof(EnemyPose)))
                {
                    Sprite frame = set.Frame(pose);
                    Sprite flash = set.SilhouetteOf(frame);
                    Assert.That(sprites.Add(frame), Is.True, "Different looks and poses remain distinct.");
                    Assert.That(sprites.Add(flash), Is.True);
                    Assert.That(frame.name, Is.EqualTo(look + " " + pose));
                    Assert.That(flash.name, Is.EqualTo(frame.name + " Flash"));
                    Assert.That(frame.rect.height, Is.EqualTo(EnemyArt.HeightOf(look)));
                    Assert.That(frame.pivot, Is.EqualTo(new Vector2(frame.rect.width / 2f, 0f)));
                    Assert.That(flash.pivot, Is.EqualTo(frame.pivot));
                    Assert.That(frame.pixelsPerUnit, Is.EqualTo(32f));
                    Assert.That(frame.texture.filterMode, Is.EqualTo(FilterMode.Point));
                    Assert.That(frame.texture.mipmapCount, Is.EqualTo(1));
                    for (int enemy = 0; enemy < 10; enemy++)
                    {
                        EnemySprites borrowed = EnemySpriteCache.Get(look);
                        Assert.That(borrowed.Frame(pose), Is.SameAs(frame));
                        Assert.That(borrowed.SilhouetteOf(frame), Is.SameAs(flash));
                    }
                }
                Assert.That(set.SilhouetteOf(EnemySpriteCache.BarBack), Is.Null);
                Assert.That(set.SilhouetteOf(null), Is.Null);
            }
            sprites.Add(EnemySpriteCache.BarBack);
            sprites.Add(EnemySpriteCache.BarFill);
            Assert.That(sprites.Count, Is.EqualTo(38), "All six looks and both bars have a bounded native resource cost.");
            Assert.That(EnemySpriteCache.BarFill.pivot, Is.EqualTo(new Vector2(0f, 1.5f)));
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemySpriteCache.Get((EnemyLook)99));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SessionResetReleasesEveryNativeResourceAndAllowsFreshArt()
        {
            var sprites = new List<Sprite>();
            var textures = new List<Texture2D>();
            foreach (EnemyLook look in Enum.GetValues(typeof(EnemyLook)))
            {
                EnemySprites set = EnemySpriteCache.Get(look);
                foreach (EnemyPose pose in Enum.GetValues(typeof(EnemyPose)))
                {
                    sprites.Add(set.Frame(pose));
                    sprites.Add(set.SilhouetteOf(set.Frame(pose)));
                }
            }
            sprites.Add(EnemySpriteCache.BarBack);
            sprites.Add(EnemySpriteCache.BarFill);
            foreach (Sprite sprite in sprites)
                textures.Add(sprite.texture);
            EnemySpriteCache.ResetSession();
            EnemySpriteCache.ResetSession(); // Safe across repeated subsystem registration / cleanup.
            yield return null;
            foreach (Sprite sprite in sprites)
                Assert.That(sprite == null, Is.True, "The session owner releases sprites.");
            foreach (Texture2D texture in textures)
                Assert.That(texture == null, Is.True, "Releasing a sprite alone must not leak its native texture.");
            Assert.That(EnemySpriteCache.Get(EnemyLook.Grunt).Frame(EnemyPose.IdleA) != null, Is.True);
            Assert.That(EnemySpriteCache.BarBack != null && EnemySpriteCache.BarFill != null, Is.True);
        }
    }
}
