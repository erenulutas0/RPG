// Copy temporarily into Scripts/Editor and run with graphics via -executeMethod Cryptforge.Editor.BuildVanguardAssets.Build.
using System.IO;
using Cryptforge.Art;
using Cryptforge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cryptforge.Editor
{
    public static class BuildVanguardAssets
    {
        private const string Folder="Assets/_Project/Art/Characters/Vanguard";
        public static void Build()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            string[] files={"vanguard-body-v2.png","walk-a-v1.png","rear-passing-a-v2.png","walk-b-v2.png","rear-passing-b-v2.png","attack-windup-v1.png","strike-v1.png","recover-v1.png"};
            string[] names={"Idle","ContactA","PassingA","ContactB","PassingB","Windup","Strike","Recovery"};
            var body=new Sprite[8];var flash=new Sprite[8];
            for(int i=0;i<8;i++)
            {
                Texture2D pixels=Sample(files[i],240,89,816,1124,128,176);
                string path=Folder+"/CHR_Vanguard_"+names[i]+".tga";
                WriteTga(path,pixels,false);body[i]=Import(path,new Vector2(354f/816,0));
                path=Folder+"/CHR_Vanguard_"+names[i]+"_Flash.tga";
                WriteTga(path,pixels,true);flash[i]=Import(path,new Vector2(354f/816,0));
                Object.DestroyImmediate(pixels);
            }
            Texture2D sword=Sample("sword-v1.png",48,115,1080,1095,95,96);
            string swordPath=Folder+"/WPN_Vanguard_Sword.tga";WriteTga(swordPath,sword,false);Object.DestroyImmediate(sword);
            var swordSprite=Import(swordPath,new Vector2(192f/1080,215f/1095));
            Texture2D shield=Sample("shield-v1.png",323,259,635,716,51,58);
            string shieldPath=Folder+"/WPN_Vanguard_Shield.tga";WriteTga(shieldPath,shield,false);Object.DestroyImmediate(shield);
            var shieldSprite=Import(shieldPath,new Vector2(.59f,.51f));
            string setPath=Folder+"/VanguardArtSet.asset";
            var set=AssetDatabase.LoadAssetAtPath<VanguardArtSet>(setPath);
            if(set==null){set=ScriptableObject.CreateInstance<VanguardArtSet>();AssetDatabase.CreateAsset(set,setPath);}
            var serialized=new SerializedObject(set);var frames=serialized.FindProperty("_frames");frames.arraySize=8;
            Vector2[] right={new Vector2(848,794),new Vector2(858,790),new Vector2(862,790),new Vector2(860,790),new Vector2(858,790),new Vector2(844,273),new Vector2(982,624),new Vector2(865,724)};
            Vector2[] left={new Vector2(345,596),new Vector2(340,599),new Vector2(340,599),new Vector2(337,599),new Vector2(340,599),new Vector2(335,596),new Vector2(335,595),new Vector2(335,595)};
            for(int i=0;i<8;i++)
            {
                var frame=frames.GetArrayElementAtIndex(i);
                frame.FindPropertyRelative("Body").objectReferenceValue=body[i];frame.FindPropertyRelative("Flash").objectReferenceValue=flash[i];
                frame.FindPropertyRelative("RightHand").vector2Value=Anchor(right[i]);frame.FindPropertyRelative("LeftHand").vector2Value=Anchor(left[i]);
            }
            string[] frontFiles={"idle","contact-a","passing-a","contact-b","passing-b","windup","strike","recovery"};
            Vector2[] frontRight={new Vector2(365,790),new Vector2(365,790),new Vector2(365,790),new Vector2(365,790),new Vector2(365,790),new Vector2(402,211),new Vector2(328,780),new Vector2(469,720)};
            Vector2[] frontLeft={new Vector2(905,686),new Vector2(905,686),new Vector2(905,686),new Vector2(905,686),new Vector2(905,686),new Vector2(910,723),new Vector2(910,686),new Vector2(905,686)};
            var frontFrames=serialized.FindProperty("_frontFrames");frontFrames.arraySize=8;
            for(int i=0;i<8;i++)
            {
                // One fixed registration for all front poses; retain the extra sole pixels below the rear crop.
                Texture2D pixels=Sample("front-"+frontFiles[i]+"-v1.png",224,89,816,1152,128,180);
                string path=Folder+"/CHR_Vanguard_Front_"+names[i]+".tga";
                WriteTga(path,pixels,false);var sprite=Import(path,new Vector2(370f/816,16f/1152));
                path=Folder+"/CHR_Vanguard_Front_"+names[i]+"_Flash.tga";
                WriteTga(path,pixels,true);var silhouette=Import(path,new Vector2(370f/816,16f/1152));
                Object.DestroyImmediate(pixels);
                var frame=frontFrames.GetArrayElementAtIndex(i);
                frame.FindPropertyRelative("Body").objectReferenceValue=sprite;frame.FindPropertyRelative("Flash").objectReferenceValue=silhouette;
                frame.FindPropertyRelative("RightHand").vector2Value=FrontAnchor(frontRight[i]);frame.FindPropertyRelative("LeftHand").vector2Value=FrontAnchor(frontLeft[i]);
            }
            serialized.FindProperty("_sword").objectReferenceValue=swordSprite;serialized.FindProperty("_shield").objectReferenceValue=shieldSprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            // Opening a scene may unload the newly created, not-yet-referenced asset.
            set=AssetDatabase.LoadAssetAtPath<VanguardArtSet>(setPath);
            if(set==null || !set.IsValid) throw new System.InvalidOperationException("Imported Vanguard set is incomplete.");
            var look=Object.FindFirstObjectByType<HeroLookView>();
            var hero=new SerializedObject(look);hero.FindProperty("_paintedArt").objectReferenceValue=set;hero.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Vanguard imported: sixteen shared body/flash pairs, sword/shield, 128 PPU, one scene art-set reference.");
        }

        private static Vector2 Anchor(Vector2 point)=>new Vector2((point.x-594)/816f,(1213-point.y)/1124f*1.375f);
        private static Vector2 FrontAnchor(Vector2 point)=>new Vector2((point.x-594)/816f,(1225-point.y)/1152f*1.40625f);

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
