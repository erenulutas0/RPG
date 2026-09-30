using System.IO;
using Cryptforge.Art;
using UnityEditor;
using UnityEngine;
namespace Cryptforge.Editor
{
    public static class BuildGateFacadeAsset
    {
        public static void Build()
        {
            const string folder="Assets/_Project/Art/Props/Foundry";
            const string texturePath=folder+"/GateFacade.png";
            const string materialPath=folder+"/MAT_GateFacade.mat";
            File.Copy("ArtDirection/2026-09-23/gate-facade-01/gate-facade-v1.png",texturePath,true);
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;
            importer.mipmapEnabled=true;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=1024;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
            importer.SaveAndReimport();
            var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Platform/MAT_Platform_Wall.mat");
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(template){name="MAT_GateFacade"};AssetDatabase.CreateAsset(material,materialPath);}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);material.color=Color.white;
            EditorUtility.SetDirty(material);
            var set=AssetDatabase.LoadAssetAtPath<PlatformMaterialSet>("Assets/_Project/Art/Environment/Platform/PlatformMaterials.asset");
            var serialized=new SerializedObject(set);serialized.FindProperty("_gateFacade").objectReferenceValue=material;serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
            Debug.Log("Gate facade imported unchanged: opaque RGB master, clamped UV, mipmaps, ASTC6, max1024; shared material bound.");
        }
    }
}
