using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Editor
{
    public static class BuildPaintedSurfaceAsset
    {
        public static void Build()
        {
            string stage=Environment.GetEnvironmentVariable("CRYPTFORGE_SURFACE");
            string name,materialName;int max=1024;
            switch(stage)
            {
                case "P1b":name="FoundryPavingV3";materialName="MAT_FoundryPaving";break;
                case "P3":name="FoundryMasonryV2";materialName="MAT_FoundryMasonry";break;
                case "P3b":name="FoundryMasonryV3";materialName="MAT_FoundryMasonry";break;
                case "P4":name="FoundryPitLining";materialName="MAT_FoundryPitLining";max=512;break;
                case "P5":name="FoundryBoilerWrap";materialName="MAT_FoundryBoilerWrap";max=512;break;
                default:throw new InvalidOperationException("Choose P1b, P3, P3b, P4 or P5.");
            }
            string root="Assets/_Project/Art/Props/Foundry/",path=root+name+".png";
            string source=stage=="P3b"?"ArtDirection/2026-10-05/masonry-variety-01/P3b-raw.png":
                "ArtDirection/2026-10-04/painted-surfaces-02-masters/"+stage+"-raw.png";
            File.Copy(source,path,true);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.wrapMode=stage=="P1b"||stage=="P3"||stage=="P3b"?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;
            importer.mipmapEnabled=true;importer.isReadable=false;
            // Raw masters stay untouched. The importer makes generated NPOT dimensions POT for Android mip compression.
            importer.npotScale=TextureImporterNPOTScale.ToNearest;
            importer.maxTextureSize=max;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=max,format=TextureImporterFormat.ASTC_6x6});
            importer.SaveAndReimport();
            var material=AssetDatabase.LoadAssetAtPath<Material>(root+materialName+".mat");
            if(material==null)
            {
                material=new Material(AssetDatabase.LoadAssetAtPath<Material>(root+"MAT_FurnaceFacade.mat")){name=materialName};
                AssetDatabase.CreateAsset(material,root+materialName+".mat");
            }
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            // P1b keeps geometric bevels and flag tints: retain their calibrated .8 headroom, unlike painted-joint P1.
            EditorUtility.SetDirty(material);
            if(stage=="P4"||stage=="P5")
            {
                var set=AssetDatabase.LoadMainAssetAtPath("Assets/_Project/Art/Environment/Platform/PlatformMaterials.asset");
                var serialized=new SerializedObject(set);
                serialized.FindProperty(stage=="P4"?"_foundryPitLining":"_furnaceFacade").objectReferenceValue=material;
                serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(set);
            }
            AssetDatabase.SaveAssets();
            var imported=(Texture2D)material.mainTexture;
            Debug.Log($"Painted surface actual target {EditorUserBuildSettings.activeBuildTarget}: {name} {imported.width}x{imported.height} {imported.format}, {imported.mipmapCount} mips");
            Debug.Log("Painted surface "+stage+" imported, source untouched, ASTC_6x6 configured: "+path);
        }
    }
}
