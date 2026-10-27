using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Forge Warden's imported sprites (rendered by Tools/ArtReview/BuildBlenderWarden.py, packed by
    // PackWardenSprites.py): the set is complete - six frames in each facing, the windup pair, the raised hammer carried
    // at a walk in the walk's four quarters (the standing windup its contact B), the second phase's frames of them all,
    // and its own fall in each facing, whose kneel and lying drawings are the two remains every painted enemy leaves -
    // every body shares its flash's rect and a pivot at the feet on its middle, the fall stands on the floor origin
    // wherever it lies, the textures import as the painted Grunt's do (single sprites at 128 texels a unit, bilinear, no
    // mipmaps, not readable, ASTC 4x4 on Android) inside the 3.3 MB budget, and the Forge Warden - only the Warden -
    // wears the set.
    public sealed class ForgeWardenArtSetTests
    {
        private const string Folder = "Assets/_Project/Art/Characters/ForgeWarden";
        private const string SetPath = Folder + "/ForgeWardenArtSet.asset";
        private const string Enemies = "Assets/_Project/Prefabs/Enemies/";
        private const string Characters = "Assets/_Project/Art/Characters/";
        // The boss's textures as ASTC 4x4 (a byte a texel); the set was packed into about 3.27 MB.
        private const long BudgetBytes = (long)(3.3 * 1024 * 1024);

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
                for (int quarter = 0; quarter < 4; quarter++)
                {
                    yield return set.GetWalkingWindup(quarter, rear);
                    yield return set.GetWalkingWindup(quarter, rear, true);
                }
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
        public void TheRaisedHammerIsCarriedAtAWalkInBothPhases()
        {
            EnemyArtSet set = Set();
            Assert.That(set.HasWalkingWindup(false), Is.True);
            Assert.That(set.HasWalkingWindup(true), Is.True, "The second phase carries its own hotter drawings.");
            foreach (bool rear in new[] { false, true })
            {
                var bodies = new HashSet<Sprite>();
                for (int quarter = 0; quarter < 4; quarter++)
                {
                    EnemyArtSet.Frame cool = set.GetWalkingWindup(quarter, rear), hot = set.GetWalkingWindup(quarter, rear, true);
                    string at = (rear ? "rear " : "front ") + quarter;
                    Assert.That(bodies.Add(cool.Body) && bodies.Add(hot.Body), Is.True, at + " is its own drawing in each phase.");
                    Assert.That(hot.Flash, Is.SameAs(cool.Flash), at + ": the second phase borrows the first's silhouette.");
                    Assert.That(set.FlashOf(cool.Body), Is.SameAs(cool.Flash), at);
                    Assert.That(set.FlashOf(hot.Body), Is.SameAs(cool.Flash), at);
                    for (int walk = 0; walk < 6; walk++)
                        Assert.That(cool.Body, Is.Not.SameAs(set.GetFrame(walk, rear).Body), at + " holds the hammer up.");
                }
                // The standing windup is the stride's contact B: one figure, standing or stepping.
                Assert.That(set.GetWalkingWindup(2, rear).Body, Is.SameAs(set.GetWindup(rear).Body));
                Assert.That(set.GetWalkingWindup(2, rear, true).Body, Is.SameAs(set.GetWindup(rear, true).Body));
            }
            // The walk's own quarters: contact A, passing A, contact B, passing B, as WalkingFrame steps the walk.
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
        public void TheBossFallsInItsOwnDrawingsInEachFacing()
        {
            EnemyArtSet set = Set();
            Assert.That(set.HasFall, Is.True);
            Assert.That(set.HasRearFall, Is.True, "A fall in each facing.");
            Sprite[] front = set.GetFall(false), rear = set.GetFall(true);
            Assert.That(front.Length, Is.EqualTo(4), "Struck, down on a knee, toppling, down.");
            Assert.That(rear.Length, Is.EqualTo(front.Length));
            Assert.That(new HashSet<Sprite>(front).Count, Is.EqualTo(front.Length), "Every drawing its own.");
            Assert.That(new HashSet<Sprite>(rear).Count, Is.EqualTo(rear.Length));
            // Each facing is struck and kneels in its own drawing; both turn toward the screen's left as they go down, so
            // from the topple on they share theirs.
            for (int i = 0; i < 2; i++)
                Assert.That(rear[i], Is.Not.SameAs(front[i]), "Beat " + i + " is drawn in each facing.");
            for (int i = 2; i < front.Length; i++)
                Assert.That(rear[i], Is.SameAs(front[i]), "Beat " + i + " is shared.");
            // The two remains every painted enemy leaves are the front kneel and the body down: a set without the fall
            // still falls in drawings of the same boss.
            Assert.That(set.Death[0], Is.SameAs(front[1]));
            Assert.That(set.Death[1], Is.SameAs(front[3]));
            foreach (Sprite drawing in front)
                Assert.That(set.FlashOf(drawing), Is.Null, "The remains never flash.");

            // A boss's beat: about a second and a third, the kneel held longest before the body comes down.
            Assert.That(set.FallSeconds, Is.InRange(1f, 1.5f));
            float held = 0f;
            for (int i = 0; i < front.Length; i++)
            {
                Assert.That(set.FallHold(i), Is.GreaterThan(.1f), "Beat " + i + " reads.");
                Assert.That(set.FallFrameAt(held + set.FallHold(i) * .5f, false), Is.SameAs(front[i]));
                Assert.That(set.FallFrameAt(held + set.FallHold(i) * .5f, true), Is.SameAs(rear[i]));
                held += set.FallHold(i);
            }
            Assert.That(set.FallHold(1), Is.GreaterThan(set.FallHold(0)).And.GreaterThan(set.FallHold(2)), "The kneel is the held beat.");
            Assert.That(set.FallFrameAt(set.FallSeconds + 1f, false), Is.SameAs(front[front.Length - 1]), "Then it lies still.");

            // The timeline the pooled remains step evenly: every drawing in order for its hold, the last to the end.
            foreach (bool facing in new[] { false, true })
            {
                Sprite[] timeline = set.FallTimeline(facing);
                Assert.That(timeline.Length * EnemyArtSet.FallStep, Is.EqualTo(set.FallSeconds).Within(EnemyArtSet.FallStep));
                Assert.That(set.FallTimeline(facing), Is.SameAs(timeline), "Built once.");
                Sprite[] drawings = set.GetFall(facing);
                int drawing = 0, run = 0;
                for (int i = 0; i < timeline.Length; i++)
                {
                    if (timeline[i] != drawings[drawing])
                    {
                        Assert.That(run * EnemyArtSet.FallStep, Is.EqualTo(set.FallHold(drawing)).Within(EnemyArtSet.FallStep));
                        drawing++;
                        run = 0;
                        Assert.That(timeline[i], Is.SameAs(drawings[drawing]), "In order.");
                    }
                    run++;
                }
                Assert.That(drawing, Is.EqualTo(drawings.Length - 1), "Down to the last drawing.");
            }
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
            foreach (Sprite sprite in sprites)
            {
                string at = sprite.name;
                Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Folder + "/CHR_Warden_"), at);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f), at);
                Assert.That(sprite.packed, Is.False, at);
                Rect rect = sprite.rect;
                // The pivot is the floor under the feet, on the drawing's middle: its bounds centre is the root, where
                // the hit sparks and the burn flames stand.
                Assert.That(sprite.pivot.x, Is.EqualTo(rect.width / 2f).Within(1e-3f), at);
                Assert.That(sprite.pivot.y, Is.InRange(0f, rect.height * 0.4f), at);
                Assert.That(rect.height / sprite.pixelsPerUnit, Is.InRange(0.8f, 3.1f), at);
            }
            // The fall (and the two remains among it) is drawn by the pooled remains, not the body: trimmed to wherever
            // the body lies, its pivot is still the floor origin where the boss stood, at the same scale.
            var falls = new List<Sprite>(set.GetFall(false));
            falls.AddRange(set.GetFall(true));
            falls.AddRange(set.Death);
            foreach (Sprite sprite in falls)
            {
                string at = sprite.name;
                Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Folder + "/CHR_Warden_"), at);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f), at);
                Assert.That(sprite.packed, Is.False, at);
                Rect rect = sprite.rect;
                Assert.That(sprite.pivot.x, Is.InRange(0f, rect.width), at);
                Assert.That(sprite.pivot.y, Is.InRange(0f, rect.height * 0.5f), at + ": the body stands or lies on the floor.");
                Assert.That(rect.height / sprite.pixelsPerUnit, Is.InRange(1f, 2.2f), at);
                Assert.That(rect.width / sprite.pixelsPerUnit, Is.InRange(1.2f, 2.9f), at);
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
            Assert.That(guids.Length, Is.EqualTo(66),
                "20 drawings in each phase, 20 flashes, and the fall's 6 (the two remains among them).");
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
        public void ASetWithoutTheNewFramesIsTheSetItWasBefore()
        {
            // The walking windup and the fall torn out of the Warden's own set: still a valid set with its windup and
            // second phase, now standing to raise its hammer and falling in the two remains.
            EnemyArtSet bare = Object.Instantiate(Set());
            try
            {
                foreach (string field in new[] { "_windupWalk", "_enragedWindupWalk", "_fallFront", "_fallRear", "_fallSeconds" })
                    typeof(EnemyArtSet).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(bare, null);
                Assert.That(bare.IsValid && bare.HasWindup && bare.HasEnragedFrames, Is.True);
                Assert.That(bare.HasWalkingWindup(false), Is.False);
                Assert.That(bare.HasWalkingWindup(true), Is.False);
                Assert.That(bare.HasFall, Is.False);
                Assert.That(bare.HasRearFall, Is.False);
                // A partly drawn part counts as absent: a second phase without its walking drawings stands to raise its
                // hammer in that phase, and a fall without its timing is no fall.
                EnemyArtSet set = Set();
                var walk = (EnemyArtSet.Frame[])typeof(EnemyArtSet).GetField("_windupWalk", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(set);
                typeof(EnemyArtSet).GetField("_windupWalk", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(bare, walk);
                Assert.That(bare.HasWalkingWindup(false), Is.True);
                Assert.That(bare.HasWalkingWindup(true), Is.False);
                typeof(EnemyArtSet).GetField("_fallFront", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(bare, set.GetFall(false));
                Assert.That(bare.HasFall, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(bare);
            }
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
            // Every other painted enemy's set carries none of the boss's parts, so it walks, strikes and falls as before.
            foreach (string path in new[] { "SlagBrute/SlagBruteArtSet", "CinderMite/CinderMiteArtSet", "Runner/RunnerArtSet",
                         "Tank/TankArtSet", "GruntCaptain/GruntCaptainArtSet" })
            {
                EnemyArtSet other = Load(Characters + path + ".asset");
                Assert.That(other.IsValid, Is.True, path);
                Assert.That(other.HasWindup || other.HasWalkingWindup(false) || other.HasFall, Is.False, path);
            }
        }
    }
}
