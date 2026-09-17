using System.IO;
using Cryptforge.Art;
using Cryptforge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cryptforge.Editor
{
    // Unchanged masters; Unity importer performs mobile sampling/compression. Materials stay asset-owned.
    public static class BuildPlatformMaterials
    {
        private const string Folder = "Assets/_Project/Art/Environment/Platform";
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            string[] sources = { "floor-v2.png", "coping-v1.png", "wall-v1.png" };
            string[] names = { "Floor", "Coping", "Wall" };
            var materials = new Material[3];
            for (int i = 0; i < sources.Length; i++)
            {
                string path = Folder + "/ENV_Platform_" + names[i] + ".png";
                File.Copy("ArtDirection/2026-09-17/platform-material-01/" + sources[i], path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.isReadable = false;
                importer.mipmapEnabled = true;
                importer.streamingMipmaps = false;
                // A 341-pixel imported height silently falls back to RGBA32. Power-of-two sampling keeps ASTC active.
                importer.npotScale = i == 1 ? TextureImporterNPOTScale.ToNearest : TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 2;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true,
                    maxTextureSize = 1024, format = i == 1 ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6,
                    compressionQuality = 100, resizeAlgorithm = TextureResizeAlgorithm.Mitchell });
                importer.SaveAndReimport();
                string materialPath = Folder + "/MAT_Platform_" + names[i] + ".mat";
                materials[i] = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (materials[i] == null)
                {
                    materials[i] = new Material(Shader.Find("Sprites/Default"));
                    AssetDatabase.CreateAsset(materials[i], materialPath);
                }
                materials[i].mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                EditorUtility.SetDirty(materials[i]);
            }
            string setPath = Folder + "/PlatformMaterials.asset";
            var set = AssetDatabase.LoadAssetAtPath<PlatformMaterialSet>(setPath);
            if (set == null) { set = ScriptableObject.CreateInstance<PlatformMaterialSet>(); AssetDatabase.CreateAsset(set, setPath); }
            var data = new SerializedObject(set);
            for (int i = 0; i < names.Length; i++) data.FindProperty("_" + names[i].ToLowerInvariant()).objectReferenceValue = materials[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            // Opening a scene may unload assets that had only managed local references.
            set = AssetDatabase.LoadAssetAtPath<PlatformMaterialSet>(setPath);
            if (set == null || !set.IsValid) throw new System.InvalidOperationException("Platform material set did not reload.");
            var arena = new SerializedObject(Object.FindFirstObjectByType<ArenaView>());
            arena.FindProperty("_platformMaterials").objectReferenceValue = set;
            arena.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Platform materials imported: 1024 cap, mipmaps, ASTC6 floor/wall and ASTC4 coping; only ArenaView material reference assigned.");
        }
    }
}
