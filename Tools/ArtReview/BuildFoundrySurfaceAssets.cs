using System.IO;
using Cryptforge.Art;
using UnityEditor;
using UnityEngine;
namespace Cryptforge.Editor
{
    public static class BuildFoundrySurfaceAssets
    {
        public static void Build()
        {
            var set=AssetDatabase.LoadAssetAtPath<PlatformMaterialSet>("Assets/_Project/Art/Environment/Platform/PlatformMaterials.asset");
            var serialized=new SerializedObject(set);
            serialized.FindProperty("_foundryWall").objectReferenceValue=Import("FoundryMasonry","masonry-v1.png",TextureWrapMode.Mirror);
            serialized.FindProperty("_furnaceFacade").objectReferenceValue=Import("FurnaceFacade","furnace-v1.png",TextureWrapMode.Clamp);
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
            Debug.Log("Foundry masonry/furnace imported unchanged, shared material refs bound.");
        }
        private static Material Import(string name,string source,TextureWrapMode wrap)
        {
            const string folder="Assets/_Project/Art/Props/Foundry/";
            string texturePath=folder+name+".png",materialPath=folder+"MAT_"+name+".mat";
            File.Copy("ArtDirection/2026-09-23/foundry-surfaces-01/"+source,texturePath,true);
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.wrapMode=wrap;importer.filterMode=FilterMode.Trilinear;
            importer.mipmapEnabled=true;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=1024;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
            importer.SaveAndReimport();
            var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Platform/MAT_Platform_Wall.mat");
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(template){name="MAT_"+name};AssetDatabase.CreateAsset(material,materialPath);}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);material.color=Color.white;EditorUtility.SetDirty(material);return material;
        }
    }
}

