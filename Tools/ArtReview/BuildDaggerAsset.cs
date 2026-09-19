// Offline technical sampling of the preserved master; no repainting or independent alpha fitting.
using System.IO;
using Cryptforge.Art;
using UnityEditor;
using UnityEngine;
namespace Cryptforge.Editor
{
    public static class BuildDaggerAsset
    {
        public static void Build()
        {
            // Fixed crop: source top-left (438,10), 380x1220. Grip source (628,975).
            // Near-uniform sampling -> 20x64 at 128 PPU; visible length about .49 world units.
            var pixels=Sample("dagger-v1.png",438,10,380,1220,20,64);
            string path="Assets/_Project/Art/Characters/Vanguard/WPN_Vanguard_Dagger.tga";
            WriteTga(path,pixels,false); Object.DestroyImmediate(pixels);
            var sprite=Import(path,new Vector2(.5f,255f/1220));
            var set=AssetDatabase.LoadAssetAtPath<VanguardArtSet>("Assets/_Project/Art/Characters/Vanguard/VanguardArtSet.asset");
            var serialized=new SerializedObject(set);
            serialized.FindProperty("_dagger").objectReferenceValue=sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
            Debug.Log("Daggers imported: shared 20x64 sprite, fixed grip, 128 PPU, ASTC4 Android.");
        }
        private static Texture2D Sample(string file,int x,int top,int cropWidth,int cropHeight,int width,int height)
        {
            Texture2D source;
            using(var reader=new BinaryReader(File.OpenRead("TestResults/daggers-weapon-source/"+file+".rgba")))
            {
                int sw=reader.ReadInt32(),sh=reader.ReadInt32();source=new Texture2D(sw,sh,TextureFormat.RGBA32,true);
                source.SetPixelData(reader.ReadBytes(sw*sh*4),0);source.Apply(true);
            }
            source.filterMode=FilterMode.Trilinear;source.wrapMode=TextureWrapMode.Clamp;
            var rt=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previous=RenderTexture.active;
            Graphics.Blit(source,rt,new Vector2((float)cropWidth/source.width,(float)cropHeight/source.height),new Vector2((float)x/source.width,(float)(source.height-top-cropHeight)/source.height));
            RenderTexture.active=rt;var pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(source);return pixels;
        }

        private static void WriteTga(string path,Texture2D texture,bool silhouette)
        {
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write((byte)0);writer.Write((byte)0);writer.Write((byte)2);writer.Write(new byte[9]);
                writer.Write((ushort)texture.width);writer.Write((ushort)texture.height);writer.Write((byte)32);writer.Write((byte)8);
                foreach(var p in texture.GetPixels32())
                {writer.Write(silhouette?(byte)255:p.b);writer.Write(silhouette?(byte)255:p.g);writer.Write(silhouette?(byte)255:p.r);writer.Write(p.a);}
            }
        }

        private static Sprite Import(string path,Vector2 pivot)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=128;importer.spritePivot=pivot;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false;importer.isReadable=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=256,format=TextureImporterFormat.ASTC_4x4});
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
