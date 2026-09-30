using System.IO;
using Cryptforge.Art;
using UnityEditor;
using UnityEngine;
namespace Cryptforge.Editor
{
    public static class BuildFoundryPavingAsset
    {
        public static void Build()
        {
            const string path="Assets/_Project/Art/Props/Foundry/FoundryPavingV2.png";
            File.Copy("ArtDirection/2026-09-29/foundry-integrated-01/basalt-v2.png",path,true);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.wrapMode=TextureWrapMode.Mirror;importer.filterMode=FilterMode.Trilinear;
            importer.mipmapEnabled=true;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=1024;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
            importer.SaveAndReimport();
            const string materialPath="Assets/_Project/Art/Props/Foundry/MAT_FoundryPaving.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null)
            {
                material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Platform/MAT_Platform_Floor.mat")){name="MAT_FoundryPaving"};
                AssetDatabase.CreateAsset(material,materialPath);
            }
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            material.color=new Color(.8f,.8f,.8f,1);EditorUtility.SetDirty(material);
            var set=AssetDatabase.LoadAssetAtPath<PlatformMaterialSet>("Assets/_Project/Art/Environment/Platform/PlatformMaterials.asset");
            var serialized=new SerializedObject(set);serialized.FindProperty("_foundryPaving").objectReferenceValue=material;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
            Debug.Log("Foundry paving source imported unchanged, optional shared material bound.");
        }
    }
}
