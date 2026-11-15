using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Thornling's imported sprites (docs/52 section 4; rendered by Tools/ArtReview/BuildBlenderGloamHorde.py, packed
    // by PackGloamHordeSprites.py): its set keeps the Runner's horde contract - six bodies a facing, the strike windup,
    // the two remains and its own death, every body sharing its flash's rect - so it can take the Runner's slot on floor
    // 3; it is born on floor 3 in the garden's palette, so it has no frosted and no bloomed copy (docs/52 section 2 rule
    // 6) and its folder holds no Ice/ or Gloam/; beside the set, outside it, for the runtime to hold: the dive (dive-0..2),
    // the eruption (erupt-0..2) and the stuck pose (stuck-0..1) in each facing, each drawn above the ash with its flash
    // (dive-1, dive-2 and erupt-0 lie mostly under it, where it cannot be struck: no flash), and its pooled effects - the
    // ash puff, the thorn burst and the ring's edge and fill in orchid and in warning red-orange; and the textures import
    // as the Runner's do (single sprites at 128 texels a unit - the ring's soft fills at 64 - bilinear, no mipmaps, not
    // readable, ASTC 4x4 on Android, whole blocks) inside a budget.
    public sealed class ThornlingArtSetTests
    {
        private const string Folder = "Assets/_Project/Art/Characters/Thornling";
        private const string SetPath = Folder + "/ThornlingArtSet.asset";
        private const string RunnerPath = "Assets/_Project/Art/Characters/Runner/RunnerArtSet.asset";
        private static readonly string[] Extras =
            { "dive-0", "dive-1", "dive-2", "erupt-0", "erupt-1", "erupt-2", "stuck-0", "stuck-1" };
        private static readonly HashSet<string> UnderTheAsh = new HashSet<string> { "dive-1", "dive-2", "erupt-0" };
        private static readonly string[] Effects =
        {
            "ash-puff-0", "ash-puff-1", "ash-puff-2", "thorn-burst-0", "thorn-burst-1", "thorn-burst-2",
            "ring-edge-orchid", "ring-edge-warning", "ring-fill-orchid", "ring-fill-warning",
        };
        // Every texture as ASTC 4x4 (a byte a texel): packed into about 0.87 MiB - the set 0.45 (the Runner's 0.54),
        // the drawings beside it 0.28, the effects 0.14.
        private const long BudgetBytes = (long)(1.1 * 1024 * 1024);

        private static EnemyArtSet Load(string path)
        {
            var set = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(path);
            Assert.That(set, Is.Not.Null, path);
            return set;
        }

        private static IEnumerable<EnemyArtSet.Frame> Bodies(EnemyArtSet set)
        {
            foreach (bool rear in new[] { false, true })
            {
                for (int i = 0; i < 6; i++) yield return set.GetFrame(i, rear);
                yield return set.GetStrikeWindup(rear);
            }
        }

        private static Sprite Drawing(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/CHR_Thornling_" + name + ".png");

        [Test]
        public void TheSetKeepsTheRunnersHordeContract()
        {
            EnemyArtSet set = Load(SetPath), runner = Load(RunnerPath);
            Assert.That(set.IsValid, Is.True);
            Assert.That(set.HasPassingFrames, Is.EqualTo(runner.HasPassingFrames));
            Assert.That(set.HasStrikeWindup, Is.EqualTo(runner.HasStrikeWindup));
            Assert.That(set.StrikeWindupSeconds, Is.EqualTo(runner.StrikeWindupSeconds));
            Assert.That(set.HasOwnDeath, Is.EqualTo(runner.HasOwnDeath));
            Assert.That(set.HasOwnRearDeath, Is.EqualTo(runner.HasOwnRearDeath));
            Assert.That(set.OwnDeathStep, Is.EqualTo(runner.OwnDeathStep));
            Assert.That(set.GetOwnDeath(false).Length, Is.EqualTo(runner.GetOwnDeath(false).Length));
            Assert.That(set.GetOwnDeath(true).Length, Is.EqualTo(runner.GetOwnDeath(true).Length));
            Assert.That(set.HasWindup, Is.False, "the rake's beat is the strike windup, not a boss's telegraph");
            Assert.That(set.HasEnragedFrames, Is.False);
            Assert.That(set.HasFall, Is.False);
            Assert.That(set.Death.Length, Is.EqualTo(2));
            // Low and lean: its bar no higher than the Runner's; a runner's shadow and stride.
            Assert.That(set.VisualTop, Is.InRange(0.75f, runner.VisualTop));
            Assert.That(set.ShadowWidth, Is.InRange(0.4f, 0.6f));
            Assert.That(set.WalkCycleDistance, Is.EqualTo(runner.WalkCycleDistance).Within(0.25f));
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                Assert.That(frame.Body, Is.Not.Null);
                Assert.That(frame.Flash, Is.Not.Null, frame.Body.name);
                Assert.That(frame.Flash.rect, Is.EqualTo(frame.Body.rect), frame.Body.name + " shares its flash's rect.");
                Assert.That(frame.Flash.pivot, Is.EqualTo(frame.Body.pivot), frame.Body.name);
                Assert.That(AssetDatabase.GetAssetPath(frame.Body), Does.StartWith(Folder + "/CHR_Thornling_"));
                Assert.That(frame.Body.name, Does.Not.Contain("_Flash"));
            }
            foreach (bool rear in new[] { false, true })
                foreach (Sprite drawing in set.GetOwnDeath(rear))
                    Assert.That(AssetDatabase.GetAssetPath(drawing), Does.StartWith(Folder + "/CHR_Thornling_dying-"));
        }

        [Test]
        public void ItIsBornOnFloorThreeWithNoThemedCopy()
        {
            EnemyArtSet set = Load(SetPath);
            foreach (FloorTheme theme in new[] { FloorTheme.Foundry, FloorTheme.Ice, FloorTheme.Gloam })
                Assert.That(set.ForTheme(theme), Is.Null, theme + ": the set is its own look on every floor.");
            Assert.That(set.Frosted, Is.Null);
            Assert.That(AssetDatabase.IsValidFolder(Folder + "/Ice"), Is.False);
            Assert.That(AssetDatabase.IsValidFolder(Folder + "/Gloam"), Is.False);
        }

        [Test]
        public void TheDiveTheEruptionAndStuckLieBesideTheSetWithTheirFlashes()
        {
            EnemyArtSet set = Load(SetPath);
            var inSet = new HashSet<Sprite>(set.Death);
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                inSet.Add(frame.Body);
                inSet.Add(frame.Flash);
            }
            int count = 0;
            foreach (string facing in new[] { "front", "rear" })
                foreach (string pose in Extras)
                {
                    string name = facing + "-" + pose;
                    Sprite body = Drawing(name), flash = Drawing(name + "_Flash");
                    Assert.That(body, Is.Not.Null, name);
                    Assert.That(body.pixelsPerUnit, Is.EqualTo(128f), name);
                    Assert.That(inSet.Contains(body), Is.False, name + " is not one of the set's.");
                    if (UnderTheAsh.Contains(pose))
                        Assert.That(flash, Is.Null, name + " lies under the ash, where it cannot be struck.");
                    else
                    {
                        Assert.That(flash, Is.Not.Null, name);
                        Assert.That(flash.rect, Is.EqualTo(body.rect), name);
                        Assert.That(flash.pivot, Is.EqualTo(body.pivot), name);
                    }
                    count++;
                }
            Assert.That(count, Is.EqualTo(16), "eight drawings in each facing");
            foreach (string effect in Effects)
            {
                Sprite sprite = Drawing(effect);
                Assert.That(sprite, Is.Not.Null, effect);
                Assert.That(inSet.Contains(sprite), Is.False, effect);
            }
            // The ring's pair is drawn at a radius of one world unit on the 2:1 floor: the edge 2 units across, 1 deep.
            foreach (string colouring in new[] { "orchid", "warning" })
                foreach (string part in new[] { "edge", "fill" })
                {
                    Sprite ring = Drawing("ring-" + part + "-" + colouring);
                    Assert.That(ring.bounds.size.x, Is.EqualTo(2f).Within(0.05f), ring.name);
                    Assert.That(ring.bounds.size.y, Is.EqualTo(1f).Within(0.05f), ring.name);
                    Assert.That(ring.bounds.center.x, Is.EqualTo(0f).Within(0.01f), ring.name + " is centred on its pivot.");
                    Assert.That(ring.bounds.center.y, Is.EqualTo(0f).Within(0.01f), ring.name);
                }
        }

        [Test]
        public void TheTexturesImportLikeTheRunnersInsideTheBudget()
        {
            long bytes = 0;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
            Assert.That(guids.Length, Is.EqualTo(76),
                "the contract's 40 (12 bodies, 2 windups, their 14 flashes, 10 deaths, 2 remains), the 16 drawings "
                + "beside it and the 10 flashes of those above the ash, and 10 effects");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(path.Contains("_ring-fill-") ? 64f : 128f), path);
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
                Assert.That(texture.width % 4 == 0 && texture.height % 4 == 0, Is.True, path + ": whole 4x4 blocks.");
                bytes += (long)texture.width * texture.height;
            }
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "The Thornling's frames as ASTC 4x4.");
        }
    }
}
