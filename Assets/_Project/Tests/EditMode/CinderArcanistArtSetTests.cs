using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // Ilya's imported sprites (docs/45; rendered by Tools/ArtReview/BuildBlenderArcanist.py, packed by
    // PackArcanistSprites.py): the art set is complete in every facing, every frame is cut from the CinderArcanist
    // atlases at 100 pixels a unit with its pivot inside its rect, the atlases import as the Vanguard's character
    // textures do (bilinear, no mipmaps, not readable, ASTC 4x4 on Android) inside the memory budget, and the Cinder
    // Arcanist - only the Arcanist - wears the set.
    public sealed class CinderArcanistArtSetTests
    {
        private const string Folder = "Assets/_Project/Art/Characters/CinderArcanist";
        private const string SetPath = Folder + "/CinderArcanistArtSet.asset";
        // docs/45 asks for well under 4 MB as ASTC 4x4 (8 bits a texel); the set was packed into about 2.6 MB.
        private const long BudgetBytes = 3L * 1024 * 1024;

        private static readonly Dictionary<RenderedMotion, int> Frames = new Dictionary<RenderedMotion, int>
        {
            { RenderedMotion.Idle, 8 }, { RenderedMotion.Walk, 8 }, { RenderedMotion.WalkStrike, 8 },
            { RenderedMotion.Strike, 5 }, { RenderedMotion.Cast, 5 }, { RenderedMotion.Hit, 3 }, { RenderedMotion.Death, 5 }
        };

        private static RenderedCharacterArtSet Set()
        {
            var set = AssetDatabase.LoadAssetAtPath<RenderedCharacterArtSet>(SetPath);
            Assert.That(set, Is.Not.Null, SetPath);
            return set;
        }

        private static HeroDefinition Hero(string name)
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>($"Assets/_Project/Data/Heroes/{name}.asset");
            Assert.That(hero, Is.Not.Null, name);
            return hero;
        }

        [Test]
        public void TheSetHasEveryMotionInEveryFacing()
        {
            RenderedCharacterArtSet set = Set();
            Assert.That(set.IsValid, Is.True);
            Assert.That(set.StrideLength, Is.EqualTo(1.2f).Within(1e-5f), "The render's stride.");
            var seen = new HashSet<Sprite>();
            foreach (KeyValuePair<RenderedMotion, int> clip in Frames)
            {
                Assert.That(set.FrameCount(clip.Key), Is.EqualTo(clip.Value), clip.Key.ToString());
                for (int f = 0; f < RenderedCharacterFrames.FacingCount; f++)
                    for (int k = 0; k < clip.Value; k++)
                    {
                        Sprite sprite = set.Frame(clip.Key, (RenderedFacing)f, k);
                        Assert.That(sprite, Is.Not.Null, $"{clip.Key} {(RenderedFacing)f} {k}");
                        Assert.That(seen.Add(sprite), Is.True, $"{clip.Key} {(RenderedFacing)f} {k} is its own frame.");
                    }
            }
            RenderedClipTiming[] timings = set.Timings();
            Assert.That(timings[(int)RenderedMotion.Idle].Loops, Is.True);
            Assert.That(timings[(int)RenderedMotion.Walk].Loops, Is.True);
            Assert.That(timings[(int)RenderedMotion.Death].Loops, Is.False, "Death holds its last frame.");
            foreach (RenderedMotion timed in new[] { RenderedMotion.Idle, RenderedMotion.Strike, RenderedMotion.Cast,
                         RenderedMotion.Hit, RenderedMotion.Death })
                Assert.That(timings[(int)timed].FramesPerSecond, Is.GreaterThan(0f), timed.ToString());
        }

        [Test]
        public void EveryFrameIsCutFromTheAtlasesAtTheGamesScaleWithItsFeetOnThePivot()
        {
            RenderedCharacterArtSet set = Set();
            foreach (KeyValuePair<RenderedMotion, int> clip in Frames)
                for (int f = 0; f < RenderedCharacterFrames.FacingCount; f++)
                    for (int k = 0; k < clip.Value; k++)
                    {
                        Sprite sprite = set.Frame(clip.Key, (RenderedFacing)f, k);
                        string at = $"{clip.Key} {(RenderedFacing)f} {k}";
                        Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Folder + "/CHR_CinderArcanist_"), at);
                        Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100f), at);
                        Assert.That(sprite.packed, Is.False, at);
                        // The pivot is the render's floor origin: inside the frame, in its lower part (the feet).
                        Rect rect = sprite.rect;
                        Assert.That(sprite.pivot.x, Is.InRange(0f, rect.width), at);
                        Assert.That(sprite.pivot.y, Is.InRange(0f, rect.height * 0.5f), at);
                        // A standing figure is about 1.4 units tall at most with its staff and hood; nothing is huge.
                        Assert.That(rect.height / sprite.pixelsPerUnit, Is.InRange(0.5f, 2.1f), at);
                    }
        }

        [Test]
        public void TheAtlasesImportLikeTheVanguardsTexturesInsideTheBudget()
        {
            long bytes = 0;
            string[] guids = AssetDatabase.FindAssets("CHR_CinderArcanist_ t:Texture2D", new[] { Folder });
            Assert.That(guids.Length, Is.GreaterThan(0));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.isReadable, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Assert.That(android.overridden, Is.True, path);
                Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture.width, Is.LessThanOrEqualTo(android.maxTextureSize), path + " is never downscaled.");
                Assert.That(texture.height, Is.LessThanOrEqualTo(android.maxTextureSize), path);
                // ASTC 4x4: one byte a texel, in whole 4x4 blocks.
                bytes += (long)((texture.width + 3) / 4 * 4) * ((texture.height + 3) / 4 * 4);
            }
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "Ilya's frames as ASTC 4x4.");
        }

        [Test]
        public void OnlyTheCinderArcanistWearsTheRenderedSet()
        {
            Assert.That(Hero("Hero_CinderArcanist").RenderedArt, Is.SameAs(Set()));
            Assert.That(Hero("Hero_Vanguard").RenderedArt, Is.Null, "The Vanguard keeps its painted frames.");
        }
    }
}
