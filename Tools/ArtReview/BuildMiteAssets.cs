// Temporary Editor importer; retained source PNGs stay unchanged. All character poses use one registration.
using System.IO;
using Cryptforge.Art;
using Cryptforge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cryptforge.Editor
{
    public static class BuildMiteAssets
    {
        private const string Folder = "Assets/_Project/Art/Characters/CinderMite";
        private const string Effects = "Assets/_Project/Art/Effects/FoundryStrikes";
        public static void Build()
        {
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(Effects); AssetDatabase.Refresh();
            string setPath = Folder + "/CinderMiteArtSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(setPath);
            if (set == null) { set = ScriptableObject.CreateInstance<EnemyArtSet>(); AssetDatabase.CreateAsset(set, setPath); }
            var serialized = new SerializedObject(set);
            var pivot = new Vector2(508f / 1056, 32f / 896);
            string[] names = { "idle", "step-a", "step-b", "strike" };
            foreach (string side in new[] { "front", "rear" })
            {
                var frames = serialized.FindProperty("_" + side); frames.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    string source = side == "front" && i == 1 ? "mite-front-step-a-v2.png" : "mite-" + side + "-" + names[i] + ".png";
                    Texture2D pixels = Sample(source, 80, 240, 1056, 896, 80, 68);
                    string path = Folder + "/CHR_Mite_" + side + "_" + names[i] + ".tga";
                    WriteTga(path, pixels, false);
                    var frame = frames.GetArrayElementAtIndex(i);
                    frame.FindPropertyRelative("Body").objectReferenceValue = Import(path, pivot);
                    path = Folder + "/CHR_Mite_" + side + "_" + names[i] + "_Flash.tga";
                    WriteTga(path, pixels, true); frame.FindPropertyRelative("Flash").objectReferenceValue = Import(path, pivot);
                    Object.DestroyImmediate(pixels);
                }
            }
            var death = serialized.FindProperty("_death"); death.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                string name = i == 0 ? "break" : "embers";
                var pixels = Sample("mite-death-" + name + ".png", 80, 240, 1056, 896, 80, 68);
                string path = Folder + "/CHR_Mite_death_" + name + ".tga";
                WriteTga(path, pixels, false); death.GetArrayElementAtIndex(i).objectReferenceValue = Import(path, pivot);
                Object.DestroyImmediate(pixels);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            string strikePath = Effects + "/FoundryStrikeArtSet.asset";
            var strikes = AssetDatabase.LoadAssetAtPath<StrikeArtSet>(strikePath);
            if (strikes == null) { strikes = ScriptableObject.CreateInstance<StrikeArtSet>(); AssetDatabase.CreateAsset(strikes, strikePath); }
            var strikeData = new SerializedObject(strikes);
            foreach (string type in new[] { "slash", "spark" })
            {
                int width, height;
                using (var reader = new BinaryReader(File.OpenRead("TestResults/character-proof-source/" + type + "-sheet.png.rgba")))
                { width = reader.ReadInt32(); height = reader.ReadInt32(); }
                if (width % 2 != 0 || height % 2 != 0) throw new System.InvalidOperationException("Effect sheet must have even dimensions.");
                var frames = strikeData.FindProperty("_" + type); frames.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    var pixels = Sample(type + "-sheet.png", i % 2 * width / 2, i / 2 * height / 2, width / 2, height / 2, 128, 128);
                    string path = Effects + "/VFX_" + type + "_" + i + ".tga";
                    WriteTga(path, pixels, false); frames.GetArrayElementAtIndex(i).objectReferenceValue = Import(path, new Vector2(.5f, .5f));
                    Object.DestroyImmediate(pixels);
                }
            }
            strikeData.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(strikes); AssetDatabase.SaveAssets();
            string prefabPath = "Assets/_Project/Prefabs/Enemies/CinderMite.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var look = new SerializedObject(prefab.GetComponent<EnemyLookView>());
                look.FindProperty("_paintedArt").objectReferenceValue = set; look.ApplyModifiedPropertiesWithoutUndo();
                var feedback = new SerializedObject(prefab.GetComponent<CombatantView>());
                feedback.FindProperty("_attackNudge").floatValue = .04f; feedback.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            strikes = AssetDatabase.LoadAssetAtPath<StrikeArtSet>(strikePath);
            if (strikes == null || !strikes.IsValid) throw new System.InvalidOperationException("Strike set incomplete.");
            var effects = new SerializedObject(Object.FindFirstObjectByType<CombatEffectsView>());
            effects.FindProperty("_paintedArt").objectReferenceValue = strikes; effects.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Mite imported: eight body/flash pairs, two deaths; four slash and spark frames. Shared art only; combat data unchanged.");
        }
        private static Texture2D Sample(string file,int x,int top,int cropWidth,int cropHeight,int width,int height)
        {
            Texture2D source;
            using(var reader=new BinaryReader(File.OpenRead("TestResults/character-proof-source/"+file+".rgba")))
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
