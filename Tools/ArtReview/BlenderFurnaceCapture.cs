using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // Isolated A/B tooling: unchanged production boiler vs an externally rendered study.
    // It does not replace a live map mesh or claim actor occlusion/mobile acceptance.
    public sealed class BlenderFurnaceCapture
    {
        private const string Output="ArtDirection/2026-10-09/blender-furnace-01/";
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private Scene _scene;
        private Texture2D _texture;
        private Sprite _sprite;

        [UnityTest]
        public IEnumerator CaptureMatchedFurnaceStudy()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            _scene=SceneManager.GetActiveScene();
            var arena=Object.FindFirstObjectByType<ArenaView>();
            var existing=Object.FindFirstObjectByType<FoundryRegionView>();
            Assert.That(existing,Is.Not.Null);
            Time.timeScale=0;
            var camera=Camera.main;
            camera.GetComponent<ArenaCameraFollow>().enabled=false;
            camera.cullingMask=1<<30;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.035f,.04f,.055f,1);
            camera.orthographicSize=5.4f;
            Vector2 centre=new Vector2(18.39f,15.81f);
            Vector3 projected=new Vector3((centre.x-centre.y)*.70710678f,(centre.x+centre.y)*.35355339f,0);
            camera.transform.position=new Vector3(projected.x,projected.y+3.6f,-10);
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                canvas.enabled=false;
            var root=new GameObject("Isolated unchanged production boiler");
            var isolated=root.AddComponent<FoundryRegionView>();
            var type=typeof(FoundryRegionView);
            type.GetField("_visibility",Private).SetValue(isolated,type.GetField("_visibility",Private).GetValue(existing));
            type.GetMethod("RegisterDepthLandmarks",Private).Invoke(isolated,null);
            type.GetMethod("BuildTallBoiler",Private).Invoke(isolated,new object[]{arena,centre});
            var batches=(IEnumerable)type.GetField("_batches",Private).GetValue(isolated);
            foreach(var batch in batches)
            {
                var bt=batch.GetType();
                object Field(string name)=>bt.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(batch);
                type.GetMethod("Emit",Private).Invoke(isolated,new[]{"Isolated boiler",Field("Builder"),Field("Material"),Field("Order"),Field("Owners")});
            }
            foreach(var child in root.GetComponentsInChildren<Transform>())child.gameObject.layer=30;
            Capture(camera,"unity-current",1200);
            Capture(camera,"unity-current-small",360);
            root.SetActive(false);
            _texture=new Texture2D(1200,1200,TextureFormat.BGRA32,false);
            _texture.LoadRawTextureData(File.ReadAllBytes("TestResults/furnace-study.bgra"));
            _texture.Apply();
            Assert.That(_texture.width,Is.EqualTo(1200));
            Assert.That(_texture.height,Is.EqualTo(1200));
            _texture.filterMode=FilterMode.Bilinear;
            _sprite=Sprite.Create(_texture,new Rect(0,0,1200,1200),Vector2.one*.5f,1200f/10.8f);
            var study=new GameObject("Blender baked appearance study");
            study.layer=30;
            study.transform.position=new Vector3(projected.x,projected.y+3.6f,0);
            study.AddComponent<SpriteRenderer>().sprite=_sprite;
            Capture(camera,"unity-blender-study",1200);
            Capture(camera,"unity-blender-small",360);
            File.WriteAllText(Output+"unity-contract.txt","Production BuildTallBoiler invoked unchanged; separate isolated layer.\nCamera: ortho half-height5.4, square1200/360, same background.\nCandidate: transparent Blender bake shown as ordinary sprite,1200/10.8PPU.\nLighting differs by design; this is a rendering-workflow comparison, not a controlled material-only comparison.\nNo map replacement, actor-depth/cutaway integration, mobile compression or device performance acceptance.\n");
            yield return null;
        }

        private static void Capture(Camera camera,string name,int size)
        {
            var prior=RenderTexture.active;var target=RenderTexture.GetTemporary(size,size,24);
            camera.aspect=1;camera.targetTexture=target;
            camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(size,size,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,size,size),0,0);pixels.Apply();
            // ImageConversion is intentionally absent from the project. Lossless BMP evidence.
            using(var writer=new BinaryWriter(File.Create(Output+name+".bmp")))
            {
                int bytes=size*size*3;writer.Write((ushort)0x4d42);writer.Write(bytes+54);writer.Write(0);writer.Write(54);
                writer.Write(40);writer.Write(size);writer.Write(size);writer.Write((ushort)1);writer.Write((ushort)24);
                writer.Write(0);writer.Write(bytes);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
                foreach(Color32 p in pixels.GetPixels32()){writer.Write(p.b);writer.Write(p.g);writer.Write(p.r);}
            }
            Object.DestroyImmediate(pixels);
            camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            if(_sprite!=null)Object.DestroyImmediate(_sprite);
            if(_texture!=null)Object.DestroyImmediate(_texture);
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Furnace study cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
