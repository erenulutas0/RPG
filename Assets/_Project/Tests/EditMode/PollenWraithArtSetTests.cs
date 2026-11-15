using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Pollen Wraith's imported sprites (docs/52 section 3; rendered by Tools/ArtReview/BuildBlenderGloamHorde.py,
    // packed by PackGloamHordeSprites.py): its set keeps the Cinder Mite's horde contract - idle, step A, step B and
    // strike a facing, the strike windup, the two remains and its own death, every body sharing its flash's rect - so it
    // can take the Mite's slot in floor 3's horde and halls; it is born on floor 3 in the garden's palette, so it has no
    // frosted and no bloomed copy (docs/52 section 2 rule 6) and its folder holds no Ice/ or Gloam/; its effects (the
    // pollen motes and the pollen bloom) lie beside the set, outside it, for the runtime to pool; and the textures
    // import as the Mite's do (single sprites at 128 texels a unit, bilinear, no mipmaps, not readable, ASTC 4x4 on
    // Android, whole blocks), the set at or under the Mite's own memory: it is the most numerous body on the floor.
    public sealed class PollenWraithArtSetTests
    {
        private const string Folder = "Assets/_Project/Art/Characters/PollenWraith";
        private const string SetPath = Folder + "/PollenWraithArtSet.asset";
        private const string MiteFolder = "Assets/_Project/Art/Characters/CinderMite";
        private const string MitePath = MiteFolder + "/CinderMiteArtSet.asset";
        private static readonly string[] Effects =
        {
            "pollen-mote-0", "pollen-mote-1", "pollen-mote-2",
            "pollen-bloom-0", "pollen-bloom-1", "pollen-bloom-2", "pollen-bloom-3",
        };

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
                for (int i = 0; i < 4; i++) yield return set.GetFrame(i, rear);
                yield return set.GetStrikeWindup(rear);
            }
        }

        // The texels of a folder's own textures (not its subfolders'), each a byte as ASTC 4x4 with whole blocks.
        private static long TexelsIn(string folder, System.Func<string, bool> include)
        {
            long texels = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path).Replace('\\', '/') != folder || !include(path)) continue;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                texels += (long)texture.width * texture.height;
            }
            return texels;
        }

        private static bool IsEffect(string path)
        {
            foreach (string effect in Effects)
                if (path.EndsWith("_" + effect + ".png")) return true;
            return false;
        }

        [Test]
        public void TheSetKeepsTheMitesHordeContract()
        {
            EnemyArtSet set = Load(SetPath), mite = Load(MitePath);
            Assert.That(set.IsValid, Is.True);
            Assert.That(set.HasPassingFrames, Is.EqualTo(mite.HasPassingFrames));
            Assert.That(set.HasStrikeWindup, Is.EqualTo(mite.HasStrikeWindup));
            Assert.That(set.StrikeWindupSeconds, Is.EqualTo(mite.StrikeWindupSeconds));
            Assert.That(set.HasOwnDeath, Is.EqualTo(mite.HasOwnDeath));
            Assert.That(set.HasOwnRearDeath, Is.EqualTo(mite.HasOwnRearDeath));
            Assert.That(set.OwnDeathStep, Is.EqualTo(mite.OwnDeathStep));
            Assert.That(set.GetOwnDeath(false).Length, Is.EqualTo(mite.GetOwnDeath(false).Length));
            Assert.That(set.GetOwnDeath(true).Length, Is.EqualTo(mite.GetOwnDeath(true).Length));
            Assert.That(set.HasWindup, Is.False, "the snap's beat is the strike windup, not a boss's telegraph");
            Assert.That(set.HasEnragedFrames, Is.False);
            Assert.That(set.HasFall, Is.False);
            Assert.That(set.Death.Length, Is.EqualTo(2));
            // A small mote a hand over the loam: its bar near the Mite's, a narrow shadow under it, a slow drift.
            Assert.That(set.VisualTop, Is.InRange(0.4f, 0.7f));
            Assert.That(set.ShadowWidth, Is.InRange(0.25f, 0.45f));
            Assert.That(set.WalkCycleDistance, Is.InRange(0.8f, 1.4f));
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                Assert.That(frame.Body, Is.Not.Null);
                Assert.That(frame.Flash, Is.Not.Null, frame.Body.name);
                Assert.That(frame.Flash.rect, Is.EqualTo(frame.Body.rect), frame.Body.name + " shares its flash's rect.");
                Assert.That(frame.Flash.pivot, Is.EqualTo(frame.Body.pivot), frame.Body.name);
                Assert.That(AssetDatabase.GetAssetPath(frame.Body), Does.StartWith(Folder + "/CHR_PollenWraith_"));
                Assert.That(frame.Body.name, Does.Not.Contain("_Flash"));
            }
            foreach (bool rear in new[] { false, true })
                foreach (Sprite drawing in set.GetOwnDeath(rear))
                    Assert.That(AssetDatabase.GetAssetPath(drawing), Does.StartWith(Folder + "/CHR_PollenWraith_dying-"));
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
        public void TheEffectsLieBesideTheSet()
        {
            EnemyArtSet set = Load(SetPath);
            var inSet = new HashSet<Sprite>(set.Death);
            foreach (EnemyArtSet.Frame frame in Bodies(set))
            {
                inSet.Add(frame.Body);
                inSet.Add(frame.Flash);
            }
            foreach (string effect in Effects)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/CHR_PollenWraith_" + effect + ".png");
                Assert.That(sprite, Is.Not.Null, effect);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f), effect);
                Assert.That(inSet.Contains(sprite), Is.False, effect + " is not one of the set's.");
            }
        }

        [Test]
        public void TheTexturesImportLikeTheMitesAndTheSetCostsNoMoreThanTheMites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
            Assert.That(guids.Length, Is.EqualTo(43),
                "the contract's 36 (8 bodies, 2 windups, their 10 flashes, 14 deaths, 2 remains) and 7 effects");
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
            }
            long set = TexelsIn(Folder, path => !IsEffect(path));
            long mite = TexelsIn(MiteFolder, path => true);
            Assert.That(mite, Is.GreaterThan(0));
            Assert.That(set, Is.LessThanOrEqualTo(mite), "The Wraith's set as ASTC 4x4 at or under the Mite's painted set.");
            Assert.That(TexelsIn(Folder, IsEffect), Is.LessThanOrEqualTo(64 * 1024), "The pooled effects stay small.");
        }
    }
}
