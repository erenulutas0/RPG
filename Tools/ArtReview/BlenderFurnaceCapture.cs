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
    // Includes a staged cutaway probe; does not replace a live map mesh or claim mobile acceptance.
    public sealed class BlenderFurnaceCapture
    {
        private const string Output="ArtDirection/2026-10-09/blender-furnace-02/";
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private Scene _scene;
        private Texture2D _texture;
        private Sprite _sprite;
        private Mesh _studyMesh;
        private Material _studyMaterial;

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
            int productionMask=camera.cullingMask;
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
            study.SetActive(false);
            // Explicit staged integration probe on a spare court location, not a live boiler replacement.
            // Existing structures, gameplay footprint and saves remain unchanged.
            var ground=FoundrySurface.Project(8,4)+existing.transform.position;
            var candidate=new GameObject("Temporary staged furnace depth probe");
            _studyMesh=new Mesh{name="Baked furnace depth plane"};
            float z=50+ground.y-existing.transform.position.y+existing.transform.position.z;
            _studyMesh.vertices=new[]{new Vector3(ground.x-5.4f,ground.y-1.8f,z),new Vector3(ground.x+5.4f,ground.y-1.8f,z),
                new Vector3(ground.x+5.4f,ground.y+9,z),new Vector3(ground.x-5.4f,ground.y+9,z)};
            _studyMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            _studyMesh.uv2=new[]{Vector2.one,Vector2.one,Vector2.one,Vector2.one};
            _studyMesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            _studyMesh.triangles=new[]{0,1,2,0,2,3};_studyMesh.RecalculateBounds();
            candidate.AddComponent<MeshFilter>().sharedMesh=_studyMesh;
            _studyMaterial=new Material(Shader.Find("Sprites/Default")){mainTexture=_texture};
            var visibility=(FoundryVisibilityView)type.GetField("_visibility",Private).GetValue(existing);
            var material=(Material)typeof(FoundryVisibilityView).GetMethod("ForStructure",Private).Invoke(visibility,new object[]{_studyMaterial});
            var renderer=candidate.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=-7;
            camera.cullingMask=productionMask;camera.orthographicSize=6.8f;
            camera.transform.position=new Vector3(ground.x,ground.y+3,-10);
            arena.Backdrop.FrameCavern();
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            hero.GetComponent<HeroMovementInput>().enabled=false;
            Object.FindFirstObjectByType<CombatSetup>().Encounters.enabled=false;
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
            hero.transform.position=new Vector3(ground.x,ground.y-2,0);
            typeof(FoundryVisibilityView).GetMethod("LateUpdate",Private).Invoke(visibility,null);
            var front=Capture(camera,"map-front",900);
            material.SetVector("_HeroFocus",Vector4.zero);
            var frontControl=Capture(camera,"map-front-control",900);
            Assert.That(ChangedPixels(front,frontControl),Is.EqualTo(0),"Furnace must not dissolve when hero is in front");
            hero.transform.position=new Vector3(ground.x,ground.y+2,0);
            material.SetVector("_HeroFocus",Vector4.zero);
            var covered=Capture(camera,"map-behind-no-cutaway",900);
            typeof(FoundryVisibilityView).GetMethod("LateUpdate",Private).Invoke(visibility,null);
            var revealed=Capture(camera,"map-behind-cutaway",900);
            int changed=ChangedPixels(covered,revealed);
            Assert.That(changed,Is.GreaterThan(100),"Hero behind candidate must trigger visible cutaway");
            hero.transform.position=new Vector3(ground.x,ground.y-2,0);
            typeof(FoundryVisibilityView).GetMethod("LateUpdate",Private).Invoke(visibility,null);
            camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            camera.transform.position=new Vector3(ground.x,ground.y+3,-10);
            camera.aspect=1080f/2340f;
            arena.Backdrop.FrameCavern();
            Capture(camera,"map-portrait",1080,2340);
            File.WriteAllText(Output+"unity-contract.txt","Production BuildTallBoiler unchanged; isolated1200/360px comparison.\nCandidate: Blender bake1200/10.8PPU, lighting differs by design.\nStaged map probe at design(8,4), new temporary depth plane; no production boiler replaced.\nReal hero transform staged front/back; not a traversal/collision or phone proof.\nFront cutaway changed pixels:0. Behind cutaway changed pixels:"+changed+".\nNo mobile compression, shadow/contact tuning or device performance acceptance.\n");
            yield return null;
        }

        private static int ChangedPixels(Color32[] a,Color32[] b)
        {
            int changed=0;for(int i=0;i<a.Length;i++)if(a[i].r!=b[i].r||a[i].g!=b[i].g||a[i].b!=b[i].b)changed++;
            return changed;
        }

        private static Color32[] Capture(Camera camera,string name,int size,int height=0)
        {
            if(height==0)height=size;
            var prior=RenderTexture.active;var target=RenderTexture.GetTemporary(size,height,24);
            camera.aspect=(float)size/height;camera.targetTexture=target;
            camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(size,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,size,height),0,0);pixels.Apply();
            // ImageConversion is intentionally absent from the project. Lossless BMP evidence.
            using(var writer=new BinaryWriter(File.Create(Output+name+".bmp")))
            {
                int bytes=size*height*3;writer.Write((ushort)0x4d42);writer.Write(bytes+54);writer.Write(0);writer.Write(54);
                writer.Write(40);writer.Write(size);writer.Write(height);writer.Write((ushort)1);writer.Write((ushort)24);
                writer.Write(0);writer.Write(bytes);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
                foreach(Color32 p in pixels.GetPixels32()){writer.Write(p.b);writer.Write(p.g);writer.Write(p.r);}
            }
            var result=pixels.GetPixels32();Object.DestroyImmediate(pixels);
            camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);
            return result;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            if(_sprite!=null)Object.DestroyImmediate(_sprite);
            if(_texture!=null)Object.DestroyImmediate(_texture);
            if(_studyMesh!=null)Object.DestroyImmediate(_studyMesh);
            if(_studyMaterial!=null)Object.DestroyImmediate(_studyMaterial);
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Furnace study cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
