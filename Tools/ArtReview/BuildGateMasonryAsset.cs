using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Editor
{
    public static class BuildGateMasonryAsset
    {
        public static void Build()
        {
            const string path="Assets/_Project/Resources/FoundryGateMasonry.png";
            File.Copy("ArtDirection/2026-10-14/foundry-gate-depth-01/masonry-bay/gate-masonry.png",path,true);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;
            importer.mipmapEnabled=true;importer.isReadable=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
            importer.SaveAndReimport();AssetDatabase.SaveAssets();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Debug.Log($"Gate masonry imported: {texture.width}x{texture.height}; {texture.format}; mips:{texture.mipmapCount}; readable:{texture.isReadable}. Native source remains in ArtDirection.");
        }
    }
}
