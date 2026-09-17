// Temporary Editor importer: unchanged generated sources -> registered shared Grunt sprites.
using System;
using System.IO;
using Cryptforge.Art;
using Cryptforge.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Cryptforge.Editor
{
    public static class BuildGruntAssets
    {
        private const string Folder="Assets/_Project/Art/Characters/SlagBrute";
        [Serializable] private sealed class Frame { public string name="";public string file="";public int[] bounds=Array.Empty<int>();public int[] root=Array.Empty<int>();public float sourceScale=1; }
        [Serializable] private sealed class Registration { public Frame[] frames=Array.Empty<Frame>(); }
        public static void Build()
        {
            var data=JsonUtility.FromJson<Registration>(File.ReadAllText("ArtDirection/2026-09-17/grunt-polish-01/registration.json"));
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            string path=Folder+"/SlagBruteArtSet.asset";
            var set=AssetDatabase.LoadAssetAtPath<EnemyArtSet>(path);
            if(set==null){set=ScriptableObject.CreateInstance<EnemyArtSet>();AssetDatabase.CreateAsset(set,path);}
            var serialized=new SerializedObject(set);
            string[] names={"idle","step-a","step-b","strike","pass-a","pass-b"};
            foreach(string side in new[]{"front","rear"})
            {
                var frames=serialized.FindProperty("_"+side);frames.arraySize=6;
                for(int i=0;i<6;i++)
                {
                    string name=side+"-"+names[i];var spec=Array.Find(data.frames,f=>f.name==name);
                    var texture=Sample(spec);
                    string spritePath=Folder+"/CHR_Grunt_"+name+".tga";
                    WriteTga(spritePath,texture,false);frames.GetArrayElementAtIndex(i).FindPropertyRelative("Body").objectReferenceValue=Import(spritePath);
                    spritePath=Folder+"/CHR_Grunt_"+name+"_Flash.tga";
                    WriteTga(spritePath,texture,true);frames.GetArrayElementAtIndex(i).FindPropertyRelative("Flash").objectReferenceValue=Import(spritePath);
                    Object.DestroyImmediate(texture);
                }
            }
            var deaths=serialized.FindProperty("_death");deaths.arraySize=2;
            for(int i=0;i<2;i++)
            {
                string name=i==0?"death-collapse":"death-cool";var texture=Sample(Array.Find(data.frames,f=>f.name==name));
                string spritePath=Folder+"/CHR_Grunt_"+name+".tga";
                WriteTga(spritePath,texture,false);deaths.GetArrayElementAtIndex(i).objectReferenceValue=Import(spritePath);Object.DestroyImmediate(texture);
            }
            serialized.FindProperty("_visualTop").floatValue=1.05f;
            serialized.FindProperty("_shadowWidth").floatValue=.78f;
            serialized.FindProperty("_walkCycleDistance").floatValue=.7f;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
            if(!set.IsValid)throw new InvalidOperationException("Incomplete Grunt set.");
            const string prefabPath="Assets/_Project/Prefabs/Enemies/Grunt.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var view=new SerializedObject(prefab.GetComponent<EnemyLookView>());view.FindProperty("_paintedArt").objectReferenceValue=set;view.ApplyModifiedPropertiesWithoutUndo();
                var feedback=new SerializedObject(prefab.GetComponent<CombatantView>());feedback.FindProperty("_attackNudge").floatValue=0;feedback.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            Debug.Log("Grunt imported: twelve body/flash pairs, two deaths, fixed visible-top bar and four-pose stride. Scene and combat untouched.");
        }
        private static Texture2D Sample(Frame frame)
        {
            if(frame==null)throw new InvalidOperationException("Missing registration.");
            Texture2D source;int sw,sh;
            using(var reader=new BinaryReader(File.OpenRead("TestResults/grunt-polish-source/"+Path.GetFileName(frame.file)+".rgba")))
            {
                sw=reader.ReadInt32();sh=reader.ReadInt32();source=new Texture2D(sw,sh,TextureFormat.RGBA32,true);
                source.SetPixelData(reader.ReadBytes(sw*sh*4),0);source.Apply();
            }
            var pixels=source.GetPixels32();var isolated=new Color32[pixels.Length];
            for(int sy=frame.bounds[1];sy<frame.bounds[3];sy++)
                Array.Copy(pixels,(sh-1-sy)*sw+frame.bounds[0],isolated,(sh-1-sy)*sw+frame.bounds[0],frame.bounds[2]-frame.bounds[0]);
            source.SetPixels32(isolated);source.Apply(true);source.filterMode=FilterMode.Trilinear;source.wrapMode=TextureWrapMode.Clamp;
            float extent=640/frame.sourceScale;
            var rt=RenderTexture.GetTemporary(180,180,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;
            Graphics.Blit(source,rt,new Vector2(extent/sw,extent/sh),new Vector2((frame.root[0]-320/frame.sourceScale)/sw,(sh-1-frame.root[1]-64/frame.sourceScale)/sh));
            RenderTexture.active=rt;var result=new Texture2D(180,180,TextureFormat.RGBA32,false);
            result.ReadPixels(new Rect(0,0,180,180),0,0);result.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(source);return result;
        }
        private static void WriteTga(string path,Texture2D texture,bool flash)
        {
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write((byte)0);writer.Write((byte)0);writer.Write((byte)2);writer.Write(new byte[9]);writer.Write((ushort)180);writer.Write((ushort)180);writer.Write((byte)32);writer.Write((byte)8);
                foreach(var p in texture.GetPixels32()){writer.Write(flash?(byte)255:p.b);writer.Write(flash?(byte)255:p.g);writer.Write(flash?(byte)255:p.r);writer.Write(p.a);}
            }
        }
        private static Sprite Import(string path)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=128;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.1f);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.isReadable=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=256,format=TextureImporterFormat.ASTC_4x4});importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
