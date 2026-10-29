using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The horde's anticipation and deaths (art round 3, Tools/ArtReview/build_horde_motion.py): each of the Foundry's five
    // sets carries a strike windup in both facings on its walk's own canvas and pivot, with its own white silhouette,
    // and a death of its own in both facings, about 0.5-0.7 s, that starts standing where the body stood and ends on the
    // floor; every new texture imports as the walk's do, and together they stay inside 1.5 MiB as ASTC 4x4. The Forge
    // Warden carries neither: its telegraph and its fall are its own.
    public sealed class HordeMotionArtSetTests
    {
        private const string Art = "Assets/_Project/Art/Characters/";
        private const long BudgetBytes = 1536L * 1024;

        private static readonly (string folder, string set, string idle)[] Sets =
        {
            ("SlagBrute", "SlagBruteArtSet", "CHR_Grunt_front-idle"),
            ("CinderMite", "CinderMiteArtSet", "CHR_Mite_front_idle"),
            ("Runner", "RunnerArtSet", "CHR_Runner_front-idle"),
            ("Tank", "TankArtSet", "CHR_Tank_front-idle"),
            ("GruntCaptain", "GruntCaptainArtSet", "CHR_Captain_front-idle"),
        };

        private static EnemyArtSet Load(string folder, string set)
        {
            var asset = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(Art + folder + "/" + set + ".asset");
            Assert.That(asset, Is.Not.Null, set);
            return asset;
        }

        // The texels of an imported TGA as the packer wrote it (bottom row first): the textures import non-readable.
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

        // The opaque figure's left, right, bottom and top in world units from the sprite's pivot.
        private static Rect Figure(Sprite sprite)
        {
            byte[] texels = Texels(sprite, out int width, out int height);
            int left = width, right = -1, bottom = height, top = -1;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (texels[(y * width + x) * 4 + 3] > 128)
                    {
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                        bottom = Mathf.Min(bottom, y);
                        top = Mathf.Max(top, y);
                    }
            Assert.That(right, Is.GreaterThanOrEqualTo(left), sprite.name + " shows nothing.");
            float ppu = sprite.pixelsPerUnit;
            return Rect.MinMaxRect((left - sprite.pivot.x) / ppu, (bottom - sprite.pivot.y) / ppu,
                (right + 1 - sprite.pivot.x) / ppu, (top + 1 - sprite.pivot.y) / ppu);
        }

        // How much of a's opaque figure b still covers, each placed by its pivot on the same floor root.
        private static float Overlap(Sprite a, Sprite b)
        {
            byte[] ta = Texels(a, out int aw, out int ah);
            byte[] tb = Texels(b, out int bw, out int bh);
            int shown = 0, covered = 0;
            int dx = Mathf.RoundToInt(b.pivot.x - a.pivot.x), dy = Mathf.RoundToInt(b.pivot.y - a.pivot.y);
            for (int y = 0; y < ah; y++)
                for (int x = 0; x < aw; x++)
                {
                    if (ta[(y * aw + x) * 4 + 3] <= 128) continue;
                    shown++;
                    int bx = x + dx, by = y + dy;
                    if (bx >= 0 && by >= 0 && bx < bw && by < bh && tb[(by * bw + bx) * 4 + 3] > 128) covered++;
                }
            return shown > 0 ? (float)covered / shown : 0f;
        }

        private static IEnumerable<Sprite> NewSprites(EnemyArtSet set)
        {
            foreach (bool rear in new[] { false, true })
            {
                yield return set.GetStrikeWindup(rear).Body;
                yield return set.GetStrikeWindup(rear).Flash;
                foreach (Sprite drawing in set.GetOwnDeath(rear))
                    yield return drawing;
            }
        }

        [Test]
        public void EachSetCarriesAStrikeWindupAndADeathOfItsOwnBesideItsWalk()
        {
            foreach (var (folder, name, _) in Sets)
            {
                EnemyArtSet set = Load(folder, name);
                Assert.That(set.IsValid, Is.True, name);
                Assert.That(set.HasStrikeWindup, Is.True, name);
                Assert.That(set.StrikeWindupSeconds, Is.InRange(.12f, .25f), name + ": a short beat, read but not waited on.");
                Assert.That(set.HasOwnDeath && set.HasOwnRearDeath, Is.True, name);
                Assert.That(set.OwnDeathSeconds, Is.InRange(.5f, .7f), name);
                Assert.That(set.OwnDeathStep, Is.InRange(.04f, .1f), name);
                Assert.That(set.GetOwnDeath(true).Length, Is.EqualTo(set.GetOwnDeath(false).Length), name);
                // The windup is its own drawing in each facing, flashing in its own silhouette.
                var walk = new HashSet<Sprite>();
                foreach (bool rear in new[] { false, true })
                    for (int i = 0; i < (set.HasPassingFrames ? 6 : 4); i++)
                        walk.Add(set.GetFrame(i, rear).Body);
                foreach (bool rear in new[] { false, true })
                {
                    EnemyArtSet.Frame windup = set.GetStrikeWindup(rear);
                    Assert.That(walk.Contains(windup.Body), Is.False, name + ": the windup is not a walking pose.");
                    Assert.That(set.FlashOf(windup.Body), Is.SameAs(windup.Flash), name);
                    Assert.That(windup.Flash, Is.Not.SameAs(windup.Body), name);
                }
                Assert.That(set.GetStrikeWindup(false).Body, Is.Not.SameAs(set.GetStrikeWindup(true).Body), name);
                // The death begins in the facing it fell in and holds its last drawing while it fades.
                Sprite[] front = set.GetOwnDeath(false), rearDeath = set.GetOwnDeath(true);
                Assert.That(front[0], Is.Not.SameAs(rearDeath[0]), name + ": each facing falls from its own pose.");
                Assert.That(front[front.Length - 1], Is.SameAs(front[front.Length - 2]), name + ": the last drawing holds.");
                Assert.That(new HashSet<Sprite>(front).Count, Is.GreaterThanOrEqualTo(4), name + ": a death, not a swap.");
                foreach (Sprite drawing in front) Assert.That(set.Death, Has.No.Member(drawing), name);
            }
        }

        [Test]
        public void TheForgeWardenKeepsItsOwnTelegraphAndFall()
        {
            var warden = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(Art + "ForgeWarden/ForgeWardenArtSet.asset");
            Assert.That(warden, Is.Not.Null);
            Assert.That(warden.HasStrikeWindup, Is.False, "The boss's blow is telegraphed by its slam, not a horde's beat.");
            Assert.That(warden.HasOwnDeath, Is.False, "It falls in its own slot, as before.");
            Assert.That(warden.HasWindup && warden.HasFall, Is.True);
        }

        [Test]
        public void TheWindupsShareTheirWalksCanvasAndPivotAndFlashTheirOwnSilhouette()
        {
            foreach (var (folder, name, _) in Sets)
            {
                EnemyArtSet set = Load(folder, name);
                Sprite idle = set.GetFrame(0, false).Body;
                foreach (bool rear in new[] { false, true })
                {
                    EnemyArtSet.Frame windup = set.GetStrikeWindup(rear);
                    foreach (Sprite sprite in new[] { windup.Body, windup.Flash })
                    {
                        string at = name + " " + sprite.name;
                        Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Art + folder + "/CHR_"), at);
                        Assert.That(sprite.pixelsPerUnit, Is.EqualTo(idle.pixelsPerUnit), at);
                        Assert.That(sprite.rect.size, Is.EqualTo(idle.rect.size), at + ": the walk's canvas, so nothing jumps.");
                        Assert.That(Vector2.Distance(sprite.pivot, idle.pivot), Is.LessThan(.001f), at + ": the walk's pivot.");
                    }
                    byte[] body = Texels(windup.Body, out int bw, out int bh);
                    byte[] flash = Texels(windup.Flash, out int fw, out int fh);
                    Assert.That((fw, fh), Is.EqualTo((bw, bh)));
                    int wrong = 0, shown = 0;
                    for (int i = 0; i < body.Length; i += 4)
                    {
                        if (flash[i + 3] != body[i + 3]) wrong++;
                        if (body[i + 3] > 0 && (flash[i] != 255 || flash[i + 1] != 255 || flash[i + 2] != 255)) wrong++;
                        if (body[i + 3] > 0) shown++;
                    }
                    Assert.That(wrong, Is.Zero, windup.Flash.name + " is " + windup.Body.name + "'s silhouette in white.");
                    Assert.That(shown, Is.GreaterThan(bw * bh / 12), windup.Body.name);
                    // It stands on the walk's feet, about as large as the idle, under the health bar.
                    Rect pose = Figure(windup.Body), still = Figure(set.GetFrame(0, rear).Body);
                    Assert.That(pose.yMin, Is.EqualTo(still.yMin).Within(.08f), windup.Body.name + " stands on the same floor.");
                    Assert.That(pose.height / still.height, Is.InRange(.75f, 1.35f), windup.Body.name);
                    Assert.That(pose.yMax, Is.LessThan(set.VisualTop + .1f), windup.Body.name + " stays under the bar.");
                }
            }
        }

        [Test]
        public void EachDeathStartsWhereTheBodyStoodAndEndsOnTheFloor()
        {
            foreach (var (folder, name, _) in Sets)
            {
                EnemyArtSet set = Load(folder, name);
                foreach (bool rear in new[] { false, true })
                {
                    Sprite[] death = set.GetOwnDeath(rear);
                    Rect body = Figure(set.GetFrame(0, rear).Body);
                    Rect first = Figure(death[0]), last = Figure(death[death.Length - 1]);
                    string at = name + (rear ? " rear" : " front");
                    Assert.That(first.yMin, Is.EqualTo(body.yMin).Within(.1f), at + ": the death starts on the body's feet.");
                    Assert.That(first.center.x, Is.EqualTo(body.center.x).Within(.15f), at + ": where it stood.");
                    Assert.That(first.height, Is.GreaterThan(body.height * .6f), at + ": standing as it is struck.");
                    // Down by the end: little of the standing figure is still covered (a body fallen away from the
                    // camera lies up the screen, so its top alone says nothing).
                    Assert.That(Overlap(set.GetFrame(0, rear).Body, death[death.Length - 1]), Is.LessThan(.65f),
                        at + ": down by the end.");
                    Assert.That(last.yMin, Is.GreaterThan(-.7f), at + ": on the floor, not under it.");
                    foreach (Sprite drawing in death)
                    {
                        Assert.That(drawing.pixelsPerUnit, Is.EqualTo(128f), drawing.name);
                        Assert.That(drawing.rect.width % 4 + drawing.rect.height % 4, Is.Zero, drawing.name + ": whole ASTC blocks.");
                        Assert.That(drawing.rect.Contains(drawing.pivot), Is.True, drawing.name + ": its floor root inside it.");
                        Assert.That(AssetDatabase.GetAssetPath(drawing.texture), Does.StartWith(Art + folder + "/CHR_"), drawing.name);
                    }
                }
            }
        }

        [Test]
        public void TheNewTexturesImportAsTheWalksDoInsideTheBudget()
        {
            long bytes = 0;
            var counted = new HashSet<Texture2D>();
            foreach (var (folder, name, idleName) in Sets)
            {
                EnemyArtSet set = Load(folder, name);
                var idleImporter = (TextureImporter)AssetImporter.GetAtPath(Art + folder + "/" + idleName + ".tga");
                foreach (Sprite sprite in NewSprites(set))
                {
                    if (!counted.Add(sprite.texture)) continue;
                    string path = AssetDatabase.GetAssetPath(sprite.texture);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(idleImporter.spritePixelsPerUnit), path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.filterMode, Is.EqualTo(idleImporter.filterMode), path);
                    Assert.That(importer.wrapMode, Is.EqualTo(idleImporter.wrapMode), path);
                    Assert.That(importer.isReadable, Is.False, path);
                    Assert.That(importer.alphaIsTransparency, Is.True, path);
                    Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                    TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                    Assert.That(android.overridden, Is.True, path);
                    Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
                    Assert.That(Mathf.Max(sprite.texture.width, sprite.texture.height),
                        Is.LessThanOrEqualTo(android.maxTextureSize), path + " is never downscaled.");
                    bytes += (long)((sprite.texture.width + 3) / 4) * ((sprite.texture.height + 3) / 4) * 16;
                }
            }
            TestContext.WriteLine($"Horde motion: {counted.Count} textures, {bytes} bytes as ASTC 4x4.");
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "The five enemies' windups and deaths as ASTC 4x4.");
        }
    }
}
