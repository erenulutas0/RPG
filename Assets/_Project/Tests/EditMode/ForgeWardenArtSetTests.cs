using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Forge Warden's imported sprites (rendered by Tools/ArtReview/BuildBlenderWarden.py, packed by
    // PackWardenSprites.py): the set is complete - six frames in each facing, the windup pair, the second phase's
    // frames and the two remains - every body shares its flash's rect and a pivot at the feet on its middle, the
    // textures import as the painted Grunt's do (single sprites at 128 texels a unit, bilinear, no mipmaps, not
    // readable, ASTC 4x4 on Android) inside the 3 MB budget, and the Forge Warden - only the Warden - wears the set.
    public sealed class ForgeWardenArtSetTests
    {
        private const string Folder = "Assets/_Project/Art/Characters/ForgeWarden";
        private const string SetPath = Folder + "/ForgeWardenArtSet.asset";
        private const string Enemies = "Assets/_Project/Prefabs/Enemies/";
        // The boss's textures as ASTC 4x4 (a byte a texel); the set was packed into about 2.0 MB.
        private const long BudgetBytes = 3L * 1024 * 1024;

        private static EnemyArtSet Load(string path)
        {
            var set = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(path);
            Assert.That(set, Is.Not.Null, path);
            return set;
        }

        private static EnemyArtSet Set() => Load(SetPath);

        private static IEnumerable<EnemyArtSet.Frame> Bodies(EnemyArtSet set)
        {
            foreach (bool rear in new[] { false, true })
            {
                for (int i = 0; i < 6; i++)
                {
                    yield return set.GetFrame(i, rear);
                    yield return set.GetFrame(i, rear, true);
                }
                yield return set.GetWindup(rear);
                yield return set.GetWindup(rear, true);
            }
        }

        private static Object PaintedArtOf(string prefab)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Enemies + prefab + ".prefab");
            Assert.That(root, Is.Not.Null, prefab);
            var look = root.GetComponent<EnemyLookView>();
            Assert.That(look, Is.Not.Null, prefab);
            return new SerializedObject(look).FindProperty("_paintedArt").objectReferenceValue;
        }

        [Test]
        public void TheSetIsCompleteWithItsWindupSecondPhaseAndRemains()
        {
            EnemyArtSet set = Set();
            Assert.That(set.IsValid, Is.True);
            Assert.That(set.HasPassingFrames, Is.True, "Idle, contact A, contact B, strike, passing A, passing B.");
            Assert.That(set.HasWindup, Is.True);
            Assert.That(set.HasEnragedFrames, Is.True);
            var seen = new HashSet<Sprite>();
            foreach (bool rear in new[] { false, true })
            {
                for (int i = 0; i < 6; i++)
                {
                    EnemyArtSet.Frame cool = set.GetFrame(i, rear), hot = set.GetFrame(i, rear, true);
                    string at = (rear ? "rear " : "front ") + i;
                    Assert.That(seen.Add(cool.Body) && seen.Add(hot.Body), Is.True, at + " is its own drawing in each phase.");
                    Assert.That(hot.Flash, Is.SameAs(cool.Flash), at + ": the second phase borrows the first's silhouette.");
                    Assert.That(set.FlashOf(cool.Body), Is.SameAs(cool.Flash), at);
                    Assert.That(set.FlashOf(hot.Body), Is.SameAs(cool.Flash), at);
                }
                EnemyArtSet.Frame up = set.GetWindup(rear), hotUp = set.GetWindup(rear, true);
                Assert.That(seen.Add(up.Body) && seen.Add(hotUp.Body), Is.True, "The windup is its own drawing.");
                Assert.That(set.FlashOf(up.Body), Is.SameAs(up.Flash));
                Assert.That(set.FlashOf(hotUp.Body), Is.SameAs(up.Flash));
            }
            Assert.That(set.Death.Length, Is.EqualTo(2));
            Assert.That(set.Death[0], Is.Not.SameAs(set.Death[1]));
        }

        [Test]
        public void EveryFrameStandsOnItsFeetAtThePaintedGruntsScale()
        {
            EnemyArtSet set = Set();
            var sprites = new List<Sprite>();
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                Assert.That(frame.Flash.rect, Is.EqualTo(frame.Body.rect), frame.Body.name + ": the flash covers its body.");
                Assert.That(frame.Flash.pivot, Is.EqualTo(frame.Body.pivot), frame.Body.name);
                sprites.Add(frame.Body);
                sprites.Add(frame.Flash);
            }
            sprites.AddRange(set.Death);
            foreach (Sprite sprite in sprites)
            {
                string at = sprite.name;
                Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Folder + "/CHR_Warden_"), at);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f), at);
                Assert.That(sprite.packed, Is.False, at);
                Rect rect = sprite.rect;
                // The pivot is the floor under the feet, on the drawing's middle: its bounds centre is the root, where
                // the hit sparks and the burn flames stand. (The remains lie forward, toward the camera, so their feet
                // sit a little higher in the drawing.)
                Assert.That(sprite.pivot.x, Is.EqualTo(rect.width / 2f).Within(1e-3f), at);
                Assert.That(sprite.pivot.y, Is.InRange(0f, rect.height * 0.4f), at);
                Assert.That(rect.height / sprite.pixelsPerUnit, Is.InRange(0.8f, 3.1f), at);
            }
            // About one and a half to one and three-quarters the painted Grunt's height, its shadow and stride heavier.
            EnemyArtSet grunt = Load("Assets/_Project/Art/Characters/SlagBrute/SlagBruteArtSet.asset");
            Assert.That(set.VisualTop / grunt.VisualTop, Is.InRange(1.4f, 1.8f));
            Assert.That(set.ShadowWidth, Is.GreaterThan(grunt.ShadowWidth));
            Assert.That(set.WalkCycleDistance, Is.GreaterThan(grunt.WalkCycleDistance));
            Assert.That(set.WalkingFrame(0f), Is.EqualTo(1));
            Assert.That(set.WalkingFrame(set.WalkCycleDistance * .3f), Is.EqualTo(4));
        }

        [Test]
        public void TheTexturesImportLikeTheGruntsInsideTheBudget()
        {
            long bytes = 0;
            string[] guids = AssetDatabase.FindAssets("CHR_Warden_ t:Texture2D", new[] { Folder });
            Assert.That(guids.Length, Is.EqualTo(44), "14 drawings in each phase, 14 flashes, 2 remains.");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(128f), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.isReadable, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Assert.That(android.overridden, Is.True, path);
                Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(Mathf.Max(texture.width, texture.height), Is.LessThanOrEqualTo(android.maxTextureSize),
                    path + " is never downscaled.");
                Assert.That(texture.width % 4 == 0 && texture.height % 4 == 0, Is.True, path + ": whole 4x4 blocks.");
                bytes += (long)texture.width * texture.height;
            }
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "The Warden's frames as ASTC 4x4.");
        }

        [Test]
        public void OnlyTheForgeWardenWearsTheSet()
        {
            EnemyArtSet set = Set();
            Assert.That(PaintedArtOf("ForgeWarden"), Is.SameAs(set));
            Assert.That(PaintedArtOf("Grunt"), Is.SameAs(Load("Assets/_Project/Art/Characters/SlagBrute/SlagBruteArtSet.asset")));
            Assert.That(PaintedArtOf("CinderMite"), Is.SameAs(Load("Assets/_Project/Art/Characters/CinderMite/CinderMiteArtSet.asset")));
            // The Warden is a variant of the Tank: the override is the Warden's alone.
            foreach (string other in new[] { "Tank", "Runner", "GruntCaptain" })
                Assert.That(PaintedArtOf(other), Is.Not.SameAs(set), other);
        }
    }
}
