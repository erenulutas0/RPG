// Technical full-canvas sampling of an unchanged generated master; run as a temporary Editor script.
using System.IO;
using Cryptforge.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cryptforge.Editor
{
    public static class BuildExperienceRewardAsset
    {
        public static void Build()
        {
            const string folder = "Assets/_Project/Art/Props/GroundRewards";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project/Art/Props", "GroundRewards");
            const string path = folder + "/Reward_Experience.tga";
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes("ArtDirection/2026-09-22/xp-reward-proof-01/xp-v1.png")))
                throw new InvalidDataException("Cannot decode reward master.");
            source.filterMode = FilterMode.Trilinear;
            source.wrapMode = TextureWrapMode.Clamp;
            source.Apply(true);
            var target = RenderTexture.GetTemporary(96, 96, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            var pixels = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, 96, 96), 0, 0);
            pixels.Apply();
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write((byte)0); writer.Write((byte)0); writer.Write((byte)2); writer.Write(new byte[9]);
                writer.Write((ushort)96); writer.Write((ushort)96); writer.Write((byte)32); writer.Write((byte)8);
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
            settings.spritePivot = new Vector2(623f / 1254f, 111f / 1254f);
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
            { name = "Android", overridden = true, maxTextureSize = 128, format = TextureImporterFormat.ASTC_4x4 });
            importer.SaveAndReimport();
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var serialized = new SerializedObject(setup);
            serialized.FindProperty("_groundExperienceSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(setup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Ground reward imported: 96-square, 128 PPU, fixed full-canvas pivot, ASTC4; scene reference saved.");
        }
    }
}
