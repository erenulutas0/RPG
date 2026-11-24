using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // Rootmother Myrr's imported sprites (docs/64 sections 7 and 14; rendered by Tools/ArtReview/BuildBlenderRootmother.py,
    // packed by PackRootmotherSprites.py) on the Forge Warden's and the Glass Regent's boss contract: six frames in each
    // facing (idle, contact A, contact B, the root strike, passing A, passing B - she walks on her root panels), the
    // telegraph's hold, the telegraph carried at a walk in the walk's four quarters (the hold its contact B), the second
    // phase's drawings of them all, and her own fall in each facing, whose sink and stump are the two remains every
    // painted enemy leaves; every body shares its flash's rect and a pivot at her root on its middle, the fall stands on
    // the floor origin; she is born on floor 3 in the garden's palette (no themed copy), nothing of hers is the snare's
    // red-orange or the garden's gloamlight, her orchid reads and her idle stands a step lighter than the mid ash loam;
    // the textures import as the Regent's (single sprites at 128 texels a unit, bilinear, no mipmaps, not readable, ASTC
    // 4x4 on Android - her white flashes ASTC 6x6) with her memory near the Regent's. Her prefab wears it since her scene
    // build (docs/64 section 16): a variant of the Forge Warden's, as the Regent's; the Warden and the Regent keep their own.
    public sealed class RootmotherMyrrArtSetTests
    {
        private const string Folder = "Assets/_Project/Art/Characters/RootmotherMyrr";
        private const string SetPath = Folder + "/RootmotherMyrrArtSet.asset";
        private const string RegentFolder = "Assets/_Project/Art/Characters/GlassRegent";
        private const string RegentPath = RegentFolder + "/GlassRegentArtSet.asset";
        private const string WardenPath = "Assets/_Project/Art/Characters/ForgeWarden/ForgeWardenArtSet.asset";
        private const string Enemies = "Assets/_Project/Prefabs/Enemies/";
        // Her textures as Android imports them (ASTC 4x4 a byte a texel, the flashes' 6x6 blocks 16 bytes each); the set
        // was packed into about 4.32 MiB (5.22 MiB were the flashes ASTC 4x4 too).
        private const long BudgetBytes = (long)(4.5 * 1024 * 1024);
        // Near the Regent's painted set (its 66 textures, 3.27 MiB): she is wider - the canopy, the hook beside it and
        // the root skirt: about 1.6 times its area a drawing - so a little more (packed at 1.32 times), never 1.4.
        private const float MostOfTheRegents = 1.4f;
        private const int Textures = 66;

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
                foreach (bool hot in new[] { false, true })
                {
                    for (int i = 0; i < 6; i++) yield return set.GetFrame(i, rear, hot);
                    yield return set.GetWindup(rear, hot);
                    for (int quarter = 0; quarter < 4; quarter++) yield return set.GetWalkingWindup(quarter, rear, hot);
                }
        }

        private static List<Sprite> FallDrawings(EnemyArtSet set)
        {
            var falls = new List<Sprite>(set.GetFall(false));
            falls.AddRange(set.GetFall(true));
            falls.AddRange(set.Death);
            return falls;
        }

        private static Object PaintedArtOf(string prefab)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Enemies + prefab + ".prefab");
            Assert.That(root, Is.Not.Null, prefab);
            var look = root.GetComponent<EnemyLookView>();
            Assert.That(look, Is.Not.Null, prefab);
            return new SerializedObject(look).FindProperty("_paintedArt").objectReferenceValue;
        }

        // The texels of a sprite's source PNG as the packer wrote them: the textures import non-readable, so the test
        // decodes the file the importer compresses.
        private static Color32[] Texels(Sprite sprite)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture))), Is.True, sprite.name);
                return texture.GetPixels32();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static long AndroidBytes(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int block = importer.GetPlatformTextureSettings("Android").format == TextureImporterFormat.ASTC_6x6 ? 6 : 4;
            return (long)((texture.width + block - 1) / block) * ((texture.height + block - 1) / block) * 16;
        }

        [Test]
        public void TheSetIsCompleteOnTheBossContract()
        {
            EnemyArtSet set = Set();
            Assert.That(set.IsValid, Is.True);
            Assert.That(set.HasPassingFrames, Is.True, "Idle, contact A, contact B, strike, passing A, passing B.");
            Assert.That(set.HasWindup, Is.True);
            Assert.That(set.HasEnragedFrames, Is.True);
            Assert.That(set.HasWalkingWindup(false), Is.True);
            Assert.That(set.HasWalkingWindup(true), Is.True, "The second phase carries its own drawings.");
            Assert.That(set.HasFall && set.HasRearFall, Is.True);
            // A boss's contract, not a horde enemy's: no strike windup, no own death.
            Assert.That(set.HasStrikeWindup, Is.False);
            Assert.That(set.HasOwnDeath, Is.False);
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
                    Assert.That(hot.Body.name, Is.EqualTo(cool.Body.name + "_hot"), at);
                }
                EnemyArtSet.Frame up = set.GetWindup(rear), hotUp = set.GetWindup(rear, true);
                Assert.That(seen.Add(up.Body) && seen.Add(hotUp.Body), Is.True, "The telegraph's hold is its own drawing.");
                Assert.That(set.FlashOf(up.Body), Is.SameAs(up.Flash));
                Assert.That(set.FlashOf(hotUp.Body), Is.SameAs(up.Flash));
            }
            Assert.That(set.Death.Length, Is.EqualTo(2));
            Assert.That(set.Death[0], Is.Not.SameAs(set.Death[1]));
            // Every texture in her folder is one of the set's drawings or flashes: nothing packed is left unreferenced.
            var inSet = new HashSet<Texture2D>();
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                inSet.Add(frame.Body.texture);
                inSet.Add(frame.Flash.texture);
            }
            foreach (Sprite drawing in FallDrawings(set)) inSet.Add(drawing.texture);
            Assert.That(inSet.Count, Is.EqualTo(Textures));
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Assert.That(inSet.Contains(AssetDatabase.LoadAssetAtPath<Texture2D>(path)), Is.True, path + " is in the set.");
            }
        }

        [Test]
        public void SheWalksOnHerRootsAndCarriesTheTelegraphAtAWalk()
        {
            EnemyArtSet set = Set();
            foreach (bool rear in new[] { false, true })
                foreach (bool hot in new[] { false, true })
                {
                    string facing = (rear ? "rear" : "front") + (hot ? ", phase two" : "");
                    // The walk's four drawings are their own, apart from the idle and the strike.
                    var walk = new HashSet<Sprite>();
                    foreach (int i in new[] { 1, 2, 4, 5 })
                    {
                        Sprite step = set.GetFrame(i, rear, hot).Body;
                        Assert.That(walk.Add(step), Is.True, facing + " walk frame " + i);
                        Assert.That(step, Is.Not.SameAs(set.GetFrame(0, rear, hot).Body), facing + ": a step is not the idle.");
                        Assert.That(step, Is.Not.SameAs(set.GetFrame(3, rear, hot).Body), facing);
                    }
                    var bodies = new HashSet<Sprite>();
                    for (int quarter = 0; quarter < 4; quarter++)
                    {
                        EnemyArtSet.Frame up = set.GetWalkingWindup(quarter, rear, hot);
                        string at = facing + " quarter " + quarter;
                        Assert.That(bodies.Add(up.Body), Is.True, at + " is its own drawing.");
                        Assert.That(up.Flash, Is.SameAs(set.GetWalkingWindup(quarter, rear).Flash), at);
                        Assert.That(set.FlashOf(up.Body), Is.SameAs(up.Flash), at);
                        for (int frame = 0; frame < 6; frame++)
                            Assert.That(up.Body, Is.Not.SameAs(set.GetFrame(frame, rear, hot).Body), at + " holds the hook up.");
                    }
                    // The standing hold is the stride's contact B: one figure, standing or stepping.
                    Assert.That(set.GetWalkingWindup(2, rear, hot).Body, Is.SameAs(set.GetWindup(rear, hot).Body), facing);
                }
            // Her cycle: 0.88 world units (BuildBlenderRootmother.py's STRIDE at her 0.88 scale) - at her 0.9 a second
            // about the Regent's cadence - stepped in quarters as the Warden's and the Regent's.
            Assert.That(set.WalkCycleDistance, Is.EqualTo(.88f).Within(1e-4f));
            float cycle = set.WalkCycleDistance;
            int[] walkFrames = { 1, 4, 2, 5 };
            for (int quarter = 0; quarter < 4; quarter++)
            {
                float distance = cycle * (quarter + .5f) / 4;
                Assert.That(set.WalkingQuarter(distance), Is.EqualTo(quarter));
                Assert.That(set.WalkingQuarter(distance + cycle * 3), Is.EqualTo(quarter), "Every cycle alike.");
                Assert.That(set.WalkingFrame(distance), Is.EqualTo(walkFrames[quarter]));
            }
        }

        [Test]
        public void SheFallsInHerOwnDrawingsInEachFacing()
        {
            EnemyArtSet set = Set();
            Sprite[] front = set.GetFall(false), rear = set.GetFall(true);
            Assert.That(front.Length, Is.EqualTo(4), "Struck, sinking into her roots, slumping, a stump.");
            Assert.That(rear.Length, Is.EqualTo(front.Length));
            Assert.That(new HashSet<Sprite>(front).Count, Is.EqualTo(front.Length), "Every drawing its own.");
            for (int i = 0; i < 2; i++)
                Assert.That(rear[i], Is.Not.SameAs(front[i]), "Beat " + i + " is drawn in each facing.");
            for (int i = 2; i < front.Length; i++)
                Assert.That(rear[i], Is.SameAs(front[i]), "Beat " + i + " is shared, as the Regent's.");
            // The two remains are the front's sink and stump: a set without the fall still falls in her drawings.
            Assert.That(set.Death[0], Is.SameAs(front[1]));
            Assert.That(set.Death[1], Is.SameAs(front[3]));
            foreach (Sprite drawing in front)
                Assert.That(set.FlashOf(drawing), Is.Null, "The remains never flash.");
            // The Warden's and the Regent's beat: the sink held longest before the stump.
            EnemyArtSet regent = Load(RegentPath);
            Assert.That(set.FallSeconds, Is.EqualTo(regent.FallSeconds).Within(1e-4f));
            Assert.That(set.FallHold(1), Is.GreaterThan(set.FallHold(0)).And.GreaterThan(set.FallHold(2)), "The sink is the held beat.");
            float held = 0f;
            for (int i = 0; i < front.Length; i++)
            {
                Assert.That(set.FallFrameAt(held + set.FallHold(i) * .5f, false), Is.SameAs(front[i]));
                Assert.That(set.FallFrameAt(held + set.FallHold(i) * .5f, true), Is.SameAs(rear[i]));
                held += set.FallHold(i);
            }
            Assert.That(set.FallFrameAt(set.FallSeconds + 1f, false), Is.SameAs(front[front.Length - 1]), "Then the stump stays.");
            foreach (bool facing in new[] { false, true })
                Assert.That(set.FallTimeline(facing).Length * EnemyArtSet.FallStep, Is.EqualTo(set.FallSeconds).Within(EnemyArtSet.FallStep));
        }

        [Test]
        public void EveryFrameStandsOnHerRootAtTheBossesScale()
        {
            EnemyArtSet set = Set();
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                Assert.That(frame.Flash.rect, Is.EqualTo(frame.Body.rect), frame.Body.name + ": the flash covers its body.");
                Assert.That(frame.Flash.pivot, Is.EqualTo(frame.Body.pivot), frame.Body.name);
                foreach (Sprite sprite in new[] { frame.Body, frame.Flash })
                {
                    string at = sprite.name;
                    Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Folder + "/CHR_Myrr_"), at);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f), at);
                    Assert.That(sprite.packed, Is.False, at);
                    Rect rect = sprite.rect;
                    // The pivot is the floor under her root, on the drawing's middle: its bounds centre is the root,
                    // where the hit sparks and the burn flames stand.
                    Assert.That(sprite.pivot.x, Is.EqualTo(rect.width / 2f).Within(1e-3f), at);
                    Assert.That(sprite.pivot.y, Is.InRange(0f, rect.height * 0.4f), at);
                    Assert.That(rect.height / sprite.pixelsPerUnit, Is.InRange(2f, 3.4f), at);
                    Assert.That(rect.width / sprite.pixelsPerUnit, Is.InRange(1.3f, 2.8f), at);
                }
            }
            foreach (Sprite sprite in FallDrawings(set))
            {
                string at = sprite.name;
                Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Folder + "/CHR_Myrr_"), at);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f), at);
                Rect rect = sprite.rect;
                Assert.That(sprite.pivot.x, Is.InRange(0f, rect.width), at);
                Assert.That(sprite.pivot.y, Is.InRange(0f, rect.height * 0.6f), at + ": she sinks where she stood.");
                Assert.That(rect.height / sprite.pixelsPerUnit, Is.InRange(.8f, 2.6f), at);
            }
            // The tallest boss (pass 1.1: drawn at 0.88 so she stands over the Regent without crowding the hero), her
            // root skirt's shadow wider than either's, her stride a little shorter than theirs.
            EnemyArtSet regent = Load(RegentPath), warden = Load(WardenPath);
            Assert.That(set.VisualTop, Is.InRange(regent.VisualTop, regent.VisualTop * 1.25f));
            Assert.That(set.ShadowWidth, Is.GreaterThan(regent.ShadowWidth).And.GreaterThan(warden.ShadowWidth));
            Assert.That(set.WalkCycleDistance, Is.LessThan(regent.WalkCycleDistance));
        }

        [Test]
        public void NothingOfHersIsTheSnaresRedOrangeOrTheGardensGloamlight()
        {
            // review_rootmother.py's checks (docs/52 section 2.1 rule 3, docs/51 rule 5), over every drawing she shows: no
            // warning red-orange, no gloamlight; her orchid (the buds, the blooms, the mantle, the heart) a share of her.
            EnemyArtSet set = Set();
            var drawings = new HashSet<Sprite>(FallDrawings(set));
            foreach (EnemyArtSet.Frame frame in Bodies(set)) drawings.Add(frame.Body);
            long opaque = 0, orchid = 0;
            foreach (Sprite drawing in drawings)
            {
                int warning = 0, gloam = 0;
                foreach (Color32 texel in Texels(drawing))
                {
                    if (texel.a < 128) continue;
                    opaque++;
                    Color.RGBToHSV(texel, out float h, out float s, out float v);
                    float hue = h * 360f;
                    if ((hue <= 25f || hue >= 350f) && s > .45f && v > .35f) warning++;
                    if (hue >= 65f && hue <= 95f && s > .35f && v > .5f) gloam++;
                    if (hue >= 280f && hue <= 330f && s > .25f && v > .35f) orchid++;
                }
                Assert.That(warning, Is.Zero, drawing.name + " has no red-orange: that is the snare's alone.");
                Assert.That(gloam, Is.Zero, drawing.name + " has no gloamlight: that is the garden's.");
            }
            Assert.That(orchid / (float)opaque, Is.InRange(.08f, .16f),
                "Her orchid reads (11.8 % packed; 10.6 % in pass 1.1's review).");
            // A step lighter than the mid ash loam (#3C3F36) in her idle, with the dark outline (docs/51 rule 3).
            double sum = 0;
            int count = 0;
            foreach (Color32 texel in Texels(set.GetFrame(0, false).Body))
            {
                if (texel.a < 128) continue;
                sum += .2126 * texel.r + .7152 * texel.g + .0722 * texel.b;
                count++;
            }
            double loam = (.2126 * 0x3C + .7152 * 0x3F + .0722 * 0x36) / 255.0;
            Assert.That(sum / count / 255.0, Is.InRange(loam, loam + .12), "The idle stands a step lighter than the loam.");
        }

        [Test]
        public void TheTexturesImportLikeTheRegentsNearItsMemory()
        {
            List<string> guids = FrostedArtCopies.BaseGuids("CHR_Myrr_ t:Texture2D", Folder);
            Assert.That(guids.Count, Is.EqualTo(Textures),
                "20 drawings in each phase, 20 flashes, and the fall's 6 (the two remains among them).");
            long bytes = 0;
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
                // A white flash carries no colour to keep: the coarser block costs it nothing a hit's tenth of a second shows.
                Assert.That(android.format, Is.EqualTo(path.EndsWith("_Flash.png") ? TextureImporterFormat.ASTC_6x6
                    : TextureImporterFormat.ASTC_4x4), path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(Mathf.Max(texture.width, texture.height), Is.LessThanOrEqualTo(android.maxTextureSize),
                    path + " is never downscaled.");
                Assert.That(texture.width % 4 == 0 && texture.height % 4 == 0, Is.True, path + ": whole 4x4 blocks.");
                bytes += AndroidBytes(path);
            }
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "Her frames as Android imports them.");
            // Beside the Glass Regent's painted set, as Android imports it (its copies apart).
            long regent = 0;
            foreach (string guid in FrostedArtCopies.BaseGuids("CHR_GlassRegent_ t:Texture2D", RegentFolder))
                regent += AndroidBytes(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(regent, Is.GreaterThan(0));
            Assert.That(bytes / (double)regent, Is.LessThanOrEqualTo(MostOfTheRegents), "Near the Regent's memory.");
        }

        [Test]
        public void SheIsBornOnFloorThreeWithNoThemedCopy()
        {
            EnemyArtSet set = Set();
            foreach (FloorTheme theme in new[] { FloorTheme.Foundry, FloorTheme.Ice, FloorTheme.Gloam })
                Assert.That(set.ForTheme(theme), Is.Null, theme + ": the set is her own look on every floor.");
            Assert.That(set.Frosted, Is.Null);
            Assert.That(AssetDatabase.IsValidFolder(Folder + "/Ice"), Is.False);
            Assert.That(AssetDatabase.IsValidFolder(Folder + "/Gloam"), Is.False);
        }

        [Test]
        public void TheWardenAndTheRegentKeepTheirOwnSets()
        {
            EnemyArtSet set = Set();
            Assert.That(PaintedArtOf("RootmotherMyrr"), Is.SameAs(set), "her prefab wears her set");
            Assert.That(PaintedArtOf("ForgeWarden"), Is.SameAs(Load(WardenPath)));
            Assert.That(PaintedArtOf("GlassRegent"), Is.SameAs(Load(RegentPath)));
            Assert.That(PaintedArtOf("ForgeWarden"), Is.Not.SameAs(set));
            Assert.That(PaintedArtOf("GlassRegent"), Is.Not.SameAs(set));
        }
    }
}
