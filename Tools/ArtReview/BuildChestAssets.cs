using System.IO;
using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cryptforge.Editor
{
    public static class BuildChestAssets
    {
        private const string Folder = "Assets/_Project/Art/Props/FoundryChest";
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            string assetPath = Folder + "/FoundryChestArt.asset";
            var set = AssetDatabase.LoadAssetAtPath<ChestArtSet>(assetPath);
            if (set == null) { set = ScriptableObject.CreateInstance<ChestArtSet>(); AssetDatabase.CreateAsset(set, assetPath); }
            var data = new SerializedObject(set);
            string[] states = { "closed", "opening", "open" };
            string[] names = { "Chest Closed", "Chest Opening", "Chest Open" };
            for (int i = 0; i < states.Length; i++)
            {
                var source = new Texture2D(1254,1254,TextureFormat.RGBA32,true);
                using (var reader = new BinaryReader(File.OpenRead("TestResults/chest-source/chest-"+states[i]+"-v1.png.rgba")))
                {
                    if(reader.ReadInt32()!=1254 || reader.ReadInt32()!=1254) throw new System.InvalidOperationException("Unexpected chest source size.");
                    source.SetPixelData(reader.ReadBytes(1254*1254*4),0); source.Apply(true);
                }
                source.filterMode=FilterMode.Trilinear;source.wrapMode=TextureWrapMode.Clamp;
                var before=RenderTexture.active;
                var target=RenderTexture.GetTemporary(192,192,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                Graphics.Blit(source,target);RenderTexture.active=target;
                var sampled=new Texture2D(192,192,TextureFormat.RGBA32,false);
                sampled.ReadPixels(new Rect(0,0,192,192),0,0);sampled.Apply();
                RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);
                string path=Folder+"/"+names[i]+".tga";
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    writer.Write((byte)0);writer.Write((byte)0);writer.Write((byte)2);writer.Write(new byte[9]);
                    writer.Write((ushort)192);writer.Write((ushort)192);writer.Write((byte)32);writer.Write((byte)8);
                    foreach(var p in sampled.GetPixels32()){writer.Write(p.b);writer.Write(p.g);writer.Write(p.r);writer.Write(p.a);}
                }
                Object.DestroyImmediate(source);Object.DestroyImmediate(sampled);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=192f*788/(1254f*.75f);
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(620f/1254,174f/1254);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.mipmapEnabled=false;importer.isReadable=false;importer.alphaIsTransparency=true;
                importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=256,format=TextureImporterFormat.ASTC_4x4});
                importer.SaveAndReimport();
                data.FindProperty("_"+states[i]).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
            if(!set.IsValid)throw new System.InvalidOperationException("Chest art set incomplete.");
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            // Opening a scene may unload unused assets; resolve the saved set again in the new scene context.
            set=AssetDatabase.LoadAssetAtPath<ChestArtSet>(assetPath);
            if(set==null || !set.IsValid)throw new System.InvalidOperationException("Saved chest art missing.");
            var component=Object.FindFirstObjectByType<ChestSpawner>();
            var chest=new SerializedObject(component);
            chest.FindProperty("_art").objectReferenceValue=set;chest.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
