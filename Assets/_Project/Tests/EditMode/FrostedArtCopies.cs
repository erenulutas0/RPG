using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // An enemy's art folder holds its painted set and, under Ice/, the floor-2 frosted copies of it
    // (Tools/ArtReview/GenerateIceEnemyArt.py, EnemyArtSet.Frosted), and under Gloam/ the floor-3 bloomed copies
    // (GenerateGloamEnemyArt.py, EnemyArtSet.ForTheme). The budget tests count them apart: the painted drawings as
    // before, from the folder itself, and each theme's copies as a mirror of them.
    internal static class FrostedArtCopies
    {
        // The painted set's textures: in the folder itself, never under its Ice/ or Gloam/ copies.
        public static List<string> BaseGuids(string filter, string folder)
        {
            var guids = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path).Replace('\\', '/') == folder)
                    guids.Add(guid);
            }
            return guids;
        }

        // Every painted drawing of the folder but the white flashes (shared, not copied) has one themed copy under
        // `theme`/ (Ice, Gloam) of its name, its size and its import, and that folder holds nothing else. Returns the
        // copies' bytes as ASTC 4x4 among the painted drawings `counted` names (paths), for the caller's budget.
        public static long AssertMirror(string folder, ICollection<string> counted, string theme = "Ice")
        {
            string ice = folder + "/" + theme;
            Assert.That(AssetDatabase.IsValidFolder(ice), Is.True, ice);
            var copies = new Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ice }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                copies.Add(Path.GetFileNameWithoutExtension(path), path);
            }
            int drawings = 0;
            long bytes = 0;
            foreach (string guid in BaseGuids("t:Texture2D", folder))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.EndsWith("_Flash"))
                {
                    Assert.That(copies.ContainsKey(name), Is.False, name + ": a flash is shared, not copied.");
                    continue;
                }
                drawings++;
                Assert.That(copies.TryGetValue(name, out string copyPath), Is.True, path + " has its " + theme + " copy.");
                var source = (TextureImporter)AssetImporter.GetAtPath(path);
                var copy = (TextureImporter)AssetImporter.GetAtPath(copyPath);
                Assert.That(copy.textureType, Is.EqualTo(source.textureType), copyPath);
                Assert.That(copy.spriteImportMode, Is.EqualTo(source.spriteImportMode), copyPath);
                Assert.That(copy.spritePixelsPerUnit, Is.EqualTo(source.spritePixelsPerUnit), copyPath);
                Assert.That(copy.spritePivot, Is.EqualTo(source.spritePivot), copyPath);
                Assert.That(copy.mipmapEnabled, Is.EqualTo(source.mipmapEnabled), copyPath);
                Assert.That(copy.filterMode, Is.EqualTo(source.filterMode), copyPath);
                Assert.That(copy.wrapMode, Is.EqualTo(source.wrapMode), copyPath);
                Assert.That(copy.isReadable, Is.EqualTo(source.isReadable), copyPath);
                Assert.That(copy.alphaIsTransparency, Is.EqualTo(source.alphaIsTransparency), copyPath);
                Assert.That(copy.npotScale, Is.EqualTo(source.npotScale), copyPath);
                TextureImporterPlatformSettings sourceAndroid = source.GetPlatformTextureSettings("Android");
                TextureImporterPlatformSettings android = copy.GetPlatformTextureSettings("Android");
                Assert.That(android.overridden, Is.EqualTo(sourceAndroid.overridden), copyPath);
                Assert.That(android.format, Is.EqualTo(sourceAndroid.format), copyPath);
                Assert.That(android.maxTextureSize, Is.EqualTo(sourceAndroid.maxTextureSize), copyPath);
                var original = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(copyPath);
                Assert.That((texture.width, texture.height), Is.EqualTo((original.width, original.height)), copyPath);
                if (counted.Contains(path))
                    bytes += (long)((texture.width + 3) / 4) * ((texture.height + 3) / 4) * 16;
            }
            Assert.That(copies.Count, Is.EqualTo(drawings), ice + ": one copy per painted drawing, nothing else.");
            return bytes;
        }
    }
}
