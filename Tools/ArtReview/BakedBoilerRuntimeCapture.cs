using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class BakedBoilerRuntimeCapture
    {
        const string Output="ArtDirection/2026-10-09/baked-boiler-runtime-02/";
        Scene _scene;
        [UnityTest] public IEnumerator CaptureProductionReplacement()
        {
            TestProfile.Begin();Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;_scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            var camera=Camera.main;var follow=camera.GetComponent<ArenaCameraFollow>();follow.enabled=false;
            Object.FindFirstObjectByType<CombatSetup>().Encounters.enabled=false;
            foreach(var a in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))a.enabled=false;
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            var movement=hero.GetComponent<HeroMovementInput>();movement.enabled=false;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.enabled=false;
            Vector3 centre=FoundrySurface.Project(18.39f,15.81f);
            camera.transform.position=new Vector3(centre.x,centre.y+3,-10);camera.orthographicSize=6;
            Capture(camera,arena,"furnace-foundation-pipes",1200,1200);
            // Matched legacy construction, in this same frozen scene with unchanged pipes/foundation.
            var bakedTexture=Resources.Load<Texture2D>("FoundryBoiler");
            var bakedRenderers=new System.Collections.Generic.List<MeshRenderer>();
            foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(r.sharedMaterial.mainTexture==bakedTexture){bakedRenderers.Add(r);r.enabled=false;}
            var oldRoot=new GameObject("Capture-only legacy boiler");
            var old=oldRoot.AddComponent<FoundryRegionView>();
            var type=typeof(FoundryRegionView);const BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic;
            var region=arena.Region.GetComponent<FoundryRegionView>();
            type.GetField("_visibility",fields).SetValue(old,type.GetField("_visibility",fields).GetValue(region));
            foreach(var c in new[]{new Vector2(18.39f,15.81f),new Vector2(26.95f,14.5f)})
                using((System.IDisposable)type.GetMethod("MassDepth",fields).Invoke(old,new object[]{new Vector4(c.x-2,c.y-2,c.x+2,c.y+2),7.2f}))
                    type.GetMethod("BuildTallBoiler",fields).Invoke(old,new object[]{arena,c});
            foreach(var batch in (IEnumerable)type.GetField("_batches",fields).GetValue(old))
            {
                object Field(string name)=>batch.GetType().GetField(name,fields|BindingFlags.Public).GetValue(batch);
                type.GetMethod("Emit",fields).Invoke(old,new[]{"Legacy boiler",Field("Builder"),Field("Material"),Field("Order"),Field("Owners")});
            }
            Capture(camera,arena,"furnace-before",1200,1200);
            Vector3 pair=(centre+FoundrySurface.Project(26.95f,14.5f))*.5f;
            camera.transform.position=new Vector3(pair.x,pair.y+3.5f,-10);camera.orthographicSize=7;
            Capture(camera,arena,"pair-before",1600,1000);
            oldRoot.SetActive(false);foreach(var r in bakedRenderers)r.enabled=true;
            Capture(camera,arena,"pair-after",1600,1000);
            camera.transform.position=new Vector3(0,4,-10);camera.orthographicSize=29;
            Capture(camera,arena,"overview",1600,1200);
            foreach(var p in new[]{new Vector2(12,12),new Vector2(16,9),new Vector2(22,9),new Vector2(28,9)})
            {
                FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                Assert.That(arena.Geometry.IsOnPlatform(x,y),Is.True);
                movement.Motion.Place(x,y,arena.Geometry);hero.transform.position=new Vector3(x,y*.5f,0);
                yield return null;
                follow.Frame(1080,2340,361,1913);
                Capture(camera,arena,"approach-"+(int)p.x,1080,2340);
            }
            var texture=Resources.Load<Texture2D>("FoundryBoiler");
            int renderers=0,vertices=0;foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {renderers++;vertices+=r.GetComponent<MeshFilter>().sharedMesh.vertexCount;}
            File.WriteAllText(Output+"runtime-budget.txt",$"Texture:{texture.width}x{texture.height} {texture.format}; mipmaps:{texture.mipmapCount}; readable:{texture.isReadable}\nRegion renderers:{renderers}; vertices:{vertices}\nActual production replacement, HUD hidden, hero staged at four validated walkable points. No synthetic prop placement.\n");
        }
        static void Capture(Camera c,ArenaView arena,string name,int width,int height)
        {
            c.aspect=(float)width/height;arena.Backdrop.FrameCavern();
            var rt=RenderTexture.GetTemporary(width,height,24);var previous=RenderTexture.active;c.targetTexture=rt;
            c.Render();RenderTexture.active=rt;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            using(var w=new BinaryWriter(File.Create(Output+name+".bmp")))
            {
                int bytes=width*height*3;w.Write((ushort)0x4d42);w.Write(bytes+54);w.Write(0);w.Write(54);
                w.Write(40);w.Write(width);w.Write(height);w.Write((ushort)1);w.Write((ushort)24);
                w.Write(0);w.Write(bytes);w.Write(0);w.Write(0);w.Write(0);w.Write(0);
                foreach(Color32 p in pixels.GetPixels32()){w.Write(p.b);w.Write(p.g);w.Write(p.r);}
            }
            Object.DestroyImmediate(pixels);c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
        }
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Baked boiler cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
