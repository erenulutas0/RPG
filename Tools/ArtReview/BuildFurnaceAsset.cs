// Technical full-canvas sampling of an unchanged generated master; run as a temporary Editor script.
using System.IO;
using Cryptforge.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cryptforge.Editor
{
    public static class BuildFurnaceAsset
    {
        public static void Build()
        {
            const string folder = "Assets/_Project/Art/Props/Foundry";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project/Art/Props", "Foundry");
            const string path = folder + "/Furnace.tga";
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes("ArtDirection/2026-09-23/furnace-prop-01/furnace-v1.png")))
                throw new InvalidDataException("Cannot decode reward master.");
            source.filterMode = FilterMode.Trilinear;
            source.wrapMode = TextureWrapMode.Clamp;
            source.Apply(true);
            var target = RenderTexture.GetTemporary(384, 328, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            var pixels = new Texture2D(384, 328, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, 384, 328), 0, 0);
            pixels.Apply();
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write((byte)0); writer.Write((byte)0); writer.Write((byte)2); writer.Write(new byte[9]);
                writer.Write((ushort)384); writer.Write((ushort)328); writer.Write((byte)32); writer.Write((byte)8);
                foreach (Color32 p in pixels.GetPixels32()) { writer.Write(p.b); writer.Write(p.g); writer.Write(p.r); writer.Write(p.a); }
            }
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(source); Object.DestroyImmediate(pixels);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(726f / 1358f, 114f / 1159f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            { name = "Android", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.ASTC_4x4 });
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            Debug.Log("Furnace imported: 384x328, 128 PPU, fixed full-canvas pivot, ASTC4; no scene reference.");
        }
    }
}

