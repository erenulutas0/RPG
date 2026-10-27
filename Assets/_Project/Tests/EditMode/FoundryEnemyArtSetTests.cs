using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Runner's, the Tank's and the Grunt Captain's imported sprites (docs/44, The Foundry; rendered by
    // Tools/ArtReview/BuildBlenderFoundryEnemies.py, the Captain kitbashed from the painted Grunt's masters, all packed by
    // build_foundry_enemies.py): each set is complete with the Grunt's six poses in both facings, its frames share one
    // canvas and one pivot at the feet, every flash is its body's exact silhouette in white, the figures stand about as
    // large as the pixel looks they replace, the textures import as the Grunt's do inside the memory budget, and each
    // prefab - only its own prefab - wears its set.
    public sealed class FoundryEnemyArtSetTests
    {
        private const string Art = "Assets/_Project/Art/Characters/";
        // The three sets together, as ASTC 4x4 (one byte a texel in whole 4x4 blocks); they were packed into about 2 MB.
        private const long BudgetBytes = 3L * 1024 * 1024;

        private sealed class Enemy
        {
            public string Folder, Set, Prefab;
            public EnemyLook Look;
        }

        private static readonly Enemy[] Enemies =
        {
            new Enemy { Folder = "Runner", Set = "RunnerArtSet", Prefab = "Runner", Look = EnemyLook.Runner },
            new Enemy { Folder = "Tank", Set = "TankArtSet", Prefab = "Tank", Look = EnemyLook.Tank },
            new Enemy { Folder = "GruntCaptain", Set = "GruntCaptainArtSet", Prefab = "GruntCaptain", Look = EnemyLook.Captain },
        };

        private static EnemyArtSet Set(Enemy enemy)
        {
            string path = Art + enemy.Folder + "/" + enemy.Set + ".asset";
            var set = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(path);
            Assert.That(set, Is.Not.Null, path);
            return set;
        }

        private static IEnumerable<EnemyArtSet.Frame> Frames(EnemyArtSet set)
        {
            foreach (bool rear in new[] { false, true })
                for (int i = 0; i < 6; i++)
                    yield return set.GetFrame(i, rear);
        }

        // The texels of one imported TGA as the packer wrote it (uncompressed, 32 bits, bottom row first): the textures
        // import non-readable, so the test reads the source file, which is what the importer compresses.
        private static byte[] Texels(Sprite sprite, out int width, out int height)
        {
            byte[] file = File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture));
            Assert.That(file[2], Is.EqualTo(2), "An uncompressed true-colour TGA.");
            Assert.That(file[16], Is.EqualTo(32), "Thirty-two bits a texel.");
            width = file[12] | file[13] << 8;
            height = file[14] | file[15] << 8;
            var texels = new byte[width * height * 4];
            System.Buffer.BlockCopy(file, 18 + file[0], texels, 0, texels.Length);
            return texels;
        }

        // The opaque figure's width and its top above the pivot, in world units.
        private static (float width, float top) Figure(Sprite sprite)
        {
            byte[] texels = Texels(sprite, out int width, out int height);
            int left = width, right = -1, top = -1;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (texels[(y * width + x) * 4 + 3] > 128)
                    {
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                        top = Mathf.Max(top, y);
                    }
            Assert.That(right, Is.GreaterThanOrEqualTo(left), sprite.name + " shows nothing.");
            return ((right + 1 - left) / sprite.pixelsPerUnit, (top + 1 - sprite.pivot.y) / sprite.pixelsPerUnit);
        }

        // The pixel look's figure (its outlined texels, not the translucent ground shadow) at 32 texels a unit.
        private static (float width, float top) PixelFigure(EnemyLook look)
        {
            PixelCanvas canvas = EnemyArt.Draw(look, EnemyPose.IdleA);
            int left = canvas.Width, right = -1, top = -1;
            for (int y = 0; y < canvas.Height; y++)
                for (int x = 0; x < canvas.Width; x++)
                    if (canvas.Get(x, y).IsOpaque)
                    {
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                        top = Mathf.Max(top, y);
                    }
            return ((right + 1 - left) / 32f, (top + 1) / 32f);
        }

        [Test]
        public void EachSetIsCompleteWithTheGruntsSixPosesInBothFacings()
        {
            foreach (Enemy enemy in Enemies)
            {
                EnemyArtSet set = Set(enemy);
                Assert.That(set.IsValid, Is.True, enemy.Set);
                Assert.That(set.HasPassingFrames, Is.True, enemy.Set + ": idle, two contacts, the strike and two passings.");
                var seen = new HashSet<Sprite>();
                foreach (EnemyArtSet.Frame frame in Frames(set))
                {
                    Assert.That(seen.Add(frame.Body), Is.True, enemy.Set + ": every pose is its own drawing.");
                    Assert.That(seen.Add(frame.Flash), Is.True, enemy.Set + ": every flash is its own silhouette.");
                    Assert.That(set.FlashOf(frame.Body), Is.SameAs(frame.Flash), enemy.Set);
                }
                Assert.That(set.Death[0], Is.Not.SameAs(set.Death[1]), enemy.Set + ": the collapse, then the cooled remains.");
                // The bar clears the head, the shadow sits under the feet, and one walk cycle is a real stride.
                Assert.That(set.VisualTop, Is.InRange(0.8f, 1.4f), enemy.Set);
                Assert.That(set.ShadowWidth, Is.InRange(0.35f, 1.1f), enemy.Set);
                Assert.That(set.WalkCycleDistance, Is.InRange(0.5f, 1.2f), enemy.Set);
            }
        }

        [Test]
        public void EveryFrameSharesOneCanvasAndOnePivotAtTheFeet()
        {
            foreach (Enemy enemy in Enemies)
            {
                EnemyArtSet set = Set(enemy);
                Sprite first = set.GetFrame(0, false).Body;
                var sprites = new List<Sprite>();
                foreach (EnemyArtSet.Frame frame in Frames(set))
                {
                    sprites.Add(frame.Body);
                    sprites.Add(frame.Flash);
                }
                sprites.AddRange(set.Death);
                foreach (Sprite sprite in sprites)
                {
                    string at = enemy.Set + " " + sprite.name;
                    Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Art + enemy.Folder + "/CHR_"), at);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f), at);
                    Assert.That(sprite.rect.size, Is.EqualTo(first.rect.size), at + ": one canvas, so poses never jump.");
                    Assert.That(sprite.pivot, Is.EqualTo(first.pivot), at);
                }
                // The pivot is the floor root: centred, so the body's bounds centre on the feet, and low in the canvas.
                Assert.That(first.pivot.x, Is.EqualTo(first.rect.width / 2f), enemy.Set);
                Assert.That(first.pivot.y, Is.InRange(4f, first.rect.height * 0.25f), enemy.Set);
                Assert.That(first.rect.width % 4, Is.Zero, enemy.Set + ": whole ASTC blocks.");
                Assert.That(first.rect.height % 4, Is.Zero, enemy.Set);
            }
        }

        [Test]
        public void EveryFlashIsItsBodysSilhouetteInWhite()
        {
            foreach (Enemy enemy in Enemies)
                foreach (EnemyArtSet.Frame frame in Frames(Set(enemy)))
                {
                    byte[] body = Texels(frame.Body, out int bw, out int bh);
                    byte[] flash = Texels(frame.Flash, out int fw, out int fh);
                    Assert.That((fw, fh), Is.EqualTo((bw, bh)), frame.Flash.name);
                    int shown = 0, wrong = 0;
                    for (int i = 0; i < body.Length; i += 4)
                    {
                        if (flash[i + 3] != body[i + 3])
                            wrong++;
                        if (body[i + 3] > 0)
                        {
                            shown++;
                            if (flash[i] != 255 || flash[i + 1] != 255 || flash[i + 2] != 255)
                                wrong++;
                        }
                    }
                    Assert.That(wrong, Is.Zero, frame.Flash.name + " matches " + frame.Body.name + " texel for texel.");
                    Assert.That(shown, Is.GreaterThan(bw * bh / 12), frame.Body.name + " is mostly empty.");
                }
        }

        [Test]
        public void EachFigureStandsAboutAsLargeAsThePixelLookItReplaces()
        {
            foreach (Enemy enemy in Enemies)
            {
                EnemyArtSet set = Set(enemy);
                (float pixelWidth, float pixelTop) = PixelFigure(enemy.Look);
                foreach (bool rear in new[] { false, true })
                {
                    (float width, float top) = Figure(set.GetFrame(0, rear).Body);
                    string at = $"{enemy.Set} {(rear ? "rear" : "front")} idle: {width:0.00} x {top:0.00} against {pixelWidth:0.00} x {pixelTop:0.00}";
                    Assert.That(width / pixelWidth, Is.InRange(0.75f, 1.25f), at);
                    Assert.That(top / pixelTop, Is.InRange(0.75f, 1.25f), at);
                    Assert.That(set.VisualTop, Is.GreaterThan(top), at + ": the bar clears the figure.");
                }
            }
            // The three still read in the pixel looks' order of size: the Runner narrowest, the Tank widest.
            float runner = Figure(Set(Enemies[0]).GetFrame(0, false).Body).width;
            float tank = Figure(Set(Enemies[1]).GetFrame(0, false).Body).width;
            float captain = Figure(Set(Enemies[2]).GetFrame(0, false).Body).width;
            Assert.That(runner, Is.LessThan(captain));
            Assert.That(captain, Is.LessThan(tank));
        }

        [Test]
        public void TheTexturesImportAsTheGruntsDoInsideTheBudget()
        {
            long bytes = 0;
            foreach (Enemy enemy in Enemies)
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Art + enemy.Folder });
                Assert.That(guids.Length, Is.EqualTo(26), enemy.Folder + ": twelve bodies, twelve flashes, two remains.");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(128f), path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                    Assert.That(importer.isReadable, Is.False, path);
                    Assert.That(importer.alphaIsTransparency, Is.True, path);
                    Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                    TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                    Assert.That(android.overridden, Is.True, path);
                    Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    Assert.That(Mathf.Max(texture.width, texture.height), Is.LessThanOrEqualTo(android.maxTextureSize),
                        path + " is never downscaled.");
                    bytes += (long)((texture.width + 3) / 4) * ((texture.height + 3) / 4) * 16;
                }
            }
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "The three enemies' frames as ASTC 4x4.");
        }

        [Test]
        public void EachPrefabWearsItsOwnSetAndTheGruntAndMiteKeepTheirs()
        {
            foreach (Enemy enemy in Enemies)
                Assert.That(PaintedArtOf(enemy.Prefab), Is.SameAs(Set(enemy)), enemy.Prefab);
            Assert.That(PaintedArtOf("Grunt"),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<EnemyArtSet>(Art + "SlagBrute/SlagBruteArtSet.asset")));
            Assert.That(PaintedArtOf("CinderMite"),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<EnemyArtSet>(Art + "CinderMite/CinderMiteArtSet.asset")));
        }

        private static Object PaintedArtOf(string prefab)
        {
            string path = "Assets/_Project/Prefabs/Enemies/" + prefab + ".prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(asset, Is.Not.Null, path);
            var view = asset.GetComponent<EnemyLookView>();
            Assert.That(view, Is.Not.Null, path);
            return new SerializedObject(view).FindProperty("_paintedArt").objectReferenceValue;
        }
    }
}
