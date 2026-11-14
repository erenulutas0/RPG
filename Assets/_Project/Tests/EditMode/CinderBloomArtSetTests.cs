using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Cinder Bloom's imported sprites (docs/52 section 5; rendered by Tools/ArtReview/BuildBlenderCinderBloom.py,
    // packed by PackCinderBloomSprites.py): its set keeps the Grunt Captain's elite contract - six bodies a facing, the
    // strike windup, the two remains and its own death, every body sharing its flash's rect - so it can stand at Thorn
    // Post where the Captain stands; it is born on floor 3 in the garden's palette, so it has no frosted and no bloomed
    // copy (docs/52 section 2 rule 6) and its folder holds no Ice/ or Gloam/; its open state (unfold-0, unfold-1 and open
    // in each facing, each with its flash; closing plays the unfold back) lies beside the set, outside it, for the
    // runtime to hold; and the
    // textures import as the Captain's do (single sprites at 128 texels a unit, bilinear, no mipmaps, not readable, ASTC
    // 4x4 on Android, whole blocks) inside a budget near the Mirror Sentry's.
    public sealed class CinderBloomArtSetTests
    {
        private const string Folder = "Assets/_Project/Art/Characters/CinderBloom";
        private const string SetPath = Folder + "/CinderBloomArtSet.asset";
        private const string CaptainPath = "Assets/_Project/Art/Characters/GruntCaptain/GruntCaptainArtSet.asset";
        private static readonly string[] OpenPoses = { "unfold-0", "unfold-1", "open" };
        // The textures as ASTC 4x4 (a byte a texel); packed into about 1.25 MiB with the open state and the bark chip.
        private const long BudgetBytes = (long)(1.4 * 1024 * 1024);

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

        private static IEnumerable<EnemyArtSet.Frame> OpenFrames()
        {
            foreach (string facing in new[] { "front", "rear" })
                foreach (string pose in OpenPoses)
                {
                    string stem = Folder + "/CHR_CinderBloom_" + facing + "-" + pose;
                    yield return new EnemyArtSet.Frame
                    {
                        Body = AssetDatabase.LoadAssetAtPath<Sprite>(stem + ".png"),
                        Flash = AssetDatabase.LoadAssetAtPath<Sprite>(stem + "_Flash.png"),
                    };
                }
        }

        [Test]
        public void TheSetKeepsTheCaptainsEliteContract()
        {
            EnemyArtSet set = Load(SetPath), captain = Load(CaptainPath);
            Assert.That(set.IsValid, Is.True);
            Assert.That(set.HasPassingFrames, Is.EqualTo(captain.HasPassingFrames));
            Assert.That(set.HasStrikeWindup, Is.EqualTo(captain.HasStrikeWindup));
            Assert.That(set.StrikeWindupSeconds, Is.EqualTo(captain.StrikeWindupSeconds));
            Assert.That(set.HasOwnDeath, Is.EqualTo(captain.HasOwnDeath));
            Assert.That(set.HasOwnRearDeath, Is.EqualTo(captain.HasOwnRearDeath));
            Assert.That(set.OwnDeathStep, Is.EqualTo(captain.OwnDeathStep));
            Assert.That(set.GetOwnDeath(false).Length, Is.EqualTo(captain.GetOwnDeath(false).Length));
            Assert.That(set.GetOwnDeath(true).Length, Is.EqualTo(captain.GetOwnDeath(true).Length));
            Assert.That(set.HasWindup, Is.False, "the maul's beat is the strike windup, not a boss's telegraph");
            Assert.That(set.HasEnragedFrames, Is.False);
            Assert.That(set.HasFall, Is.False);
            Assert.That(set.Death.Length, Is.EqualTo(2));
            // A head and branches over the Captain's; a golem's shadow and short, heavy stride.
            Assert.That(set.VisualTop, Is.InRange(captain.VisualTop, captain.VisualTop * 1.35f));
            Assert.That(set.ShadowWidth, Is.InRange(0.8f, 1.0f));
            Assert.That(set.WalkCycleDistance, Is.InRange(0.6f, 0.9f));
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                Assert.That(frame.Body, Is.Not.Null);
                Assert.That(frame.Flash, Is.Not.Null, frame.Body.name);
                Assert.That(frame.Flash.rect, Is.EqualTo(frame.Body.rect), frame.Body.name + " shares its flash's rect.");
                Assert.That(frame.Flash.pivot, Is.EqualTo(frame.Body.pivot), frame.Body.name);
                Assert.That(AssetDatabase.GetAssetPath(frame.Body), Does.StartWith(Folder + "/CHR_CinderBloom_"));
                Assert.That(frame.Body.name, Does.Not.Contain("_Flash"));
            }
            foreach (bool rear in new[] { false, true })
                foreach (Sprite drawing in set.GetOwnDeath(rear))
                    Assert.That(AssetDatabase.GetAssetPath(drawing), Does.StartWith(Folder + "/CHR_CinderBloom_dying-"));
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
        public void TheOpenStateLiesBesideTheSetEachDrawingWithItsFlash()
        {
            EnemyArtSet set = Load(SetPath);
            var inSet = new HashSet<Sprite>();
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                inSet.Add(frame.Body);
                inSet.Add(frame.Flash);
            }
            int count = 0;
            foreach (EnemyArtSet.Frame frame in OpenFrames())
            {
                Assert.That(frame.Body, Is.Not.Null, "open drawing " + count);
                Assert.That(frame.Flash, Is.Not.Null, frame.Body.name);
                Assert.That(frame.Flash.rect, Is.EqualTo(frame.Body.rect), frame.Body.name);
                Assert.That(frame.Flash.pivot, Is.EqualTo(frame.Body.pivot), frame.Body.name);
                Assert.That(frame.Body.pixelsPerUnit, Is.EqualTo(128f), frame.Body.name);
                Assert.That(inSet.Contains(frame.Body), Is.False, frame.Body.name + " is not one of the set's.");
                count++;
            }
            Assert.That(count, Is.EqualTo(6), "three drawings in each facing");
            for (int k = 0; k < 3; k++)
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/CHR_CinderBloom_bark-chip-" + k + ".png"),
                    Is.Not.Null, "bark chip " + k);
        }

        [Test]
        public void TheTexturesImportLikeTheCaptainsInsideTheBudget()
        {
            long bytes = 0;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
            Assert.That(guids.Length, Is.EqualTo(59),
                "the contract's 44 (12 bodies, 2 windups, their 14 flashes, 14 deaths, 2 remains), the open state's 12 "
                + "and the bark chip's 3");
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
                Assert.That(texture.width % 4 == 0 && texture.height % 4 == 0, Is.True, path + ": whole 4x4 blocks.");
                bytes += (long)texture.width * texture.height;
            }
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "The Bloom's frames as ASTC 4x4.");
        }
    }
}
