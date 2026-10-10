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
    public sealed class FoundrySkylineCapture
    {
        static string Output;
        Scene _scene;
        [UnityTest] public IEnumerator CaptureDistantArchitecture()
        {
            string phase=File.ReadAllText("TestResults/foundry-skyline-phase.txt").Trim();
            Assert.That(phase=="before"||phase=="after");
            Output="ArtDirection/2026-10-10/foundry-skyline-01/"+phase+"/";
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
            Vector3 pit=FoundrySurface.Project(-24,14);
            camera.transform.position=new Vector3(pit.x,pit.y-1,-10);camera.orthographicSize=7;
            Capture(camera,arena,"workshop-detail",1200,1200);
            Vector3 crane=FoundrySurface.Project(-31.5f,13);
            camera.transform.position=new Vector3(crane.x,crane.y+2,-10);camera.orthographicSize=7;
            Capture(camera,arena,"crane-detail",1200,1200);
            Vector3 stock=FoundrySurface.Project(-22,20);
            camera.transform.position=new Vector3(stock.x,stock.y+1,-10);camera.orthographicSize=5;
            Capture(camera,arena,"stock-detail",1200,1200);
            {
                // Both phases share all local views and the unchanged production camera width.
                Vector3 casting=FoundrySurface.Project(-15.5f,20.75f);
                camera.transform.position=new Vector3(casting.x,casting.y+1.2f,-10);camera.orthographicSize=5;
                Capture(camera,arena,"casting-detail",1200,1200);
            }
            Vector3 skyline=FoundrySurface.Project(-12,30);
            camera.transform.position=new Vector3(skyline.x,skyline.y+1,-10);camera.orthographicSize=14;
            Capture(camera,arena,"skyline-detail",1200,1200);
            Vector3 entrance=FoundrySurface.Project(-23,-23);
            camera.transform.position=new Vector3(entrance.x,entrance.y,-10);camera.orthographicSize=11;
            Capture(camera,arena,"bridge-detail",1200,1200);
            Vector3 portal=FoundrySurface.Project(28,6);
            camera.transform.position=new Vector3(portal.x,portal.y+2,-10);camera.orthographicSize=9;
            Capture(camera,arena,"gate-detail",1200,1200);
            Vector3 gate=FoundrySurface.Project(-3,1);
            camera.transform.position=new Vector3(gate.x,gate.y,-10);camera.orthographicSize=10;
            Capture(camera,arena,"court-detail",1200,1200);
            Vector3 shaft=FoundrySurface.Project(-10,8);
            camera.transform.position=new Vector3(shaft.x,shaft.y-1,-10);camera.orthographicSize=8;
            Capture(camera,arena,"shaft-detail",1200,1200);
            camera.transform.position=new Vector3(0,4,-10);camera.orthographicSize=32;
            Capture(camera,arena,"overview",1600,1200);
            foreach(var p in new[]{new Vector2(0,0),new Vector2(-20,6),new Vector2(-16,15),new Vector2(20,1)})
            {
                FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                Assert.That(arena.Geometry.IsOnPlatform(x,y,HeroMotion.EdgeMargin),Is.True);
                movement.Motion.Place(x,y,arena.Geometry);hero.transform.position=new Vector3(x,y*.5f,0);
                yield return null;
                follow.Frame(1080,2340,361,1913);
                Capture(camera,arena,"approach-"+(int)p.x+"-"+(int)p.y,1080,2340);
            }
            // Both phases share the already-expanded walking layout.
            {
                camera.transform.position=new Vector3(0,2,-10);camera.orthographicSize=37;
                Capture(camera,arena,"full-map",1600,1200);
                Vector3 bridgeFoot=FoundrySurface.Project(-33,-33);
                camera.transform.position=new Vector3(bridgeFoot.x,bridgeFoot.y-3,-10);camera.orthographicSize=7;
                Capture(camera,arena,"bridge-foot",1200,1200);
                Vector3 yard=FoundrySurface.Project(-24,7);
                camera.transform.position=new Vector3(yard.x,yard.y,-10);camera.orthographicSize=6;
                Capture(camera,arena,"yard-detail",1200,1200);
                // Added floor must be shown at the unchanged production follow width too.
                foreach(var p in new[]{new Vector2(-27,6),new Vector2(-26,15),new Vector2(-31,-31),new Vector2(30,5)})
                {
                    FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                    Assert.That(arena.Geometry.IsOnPlatform(x,y,HeroMotion.EdgeMargin),Is.True);
                    movement.Motion.Place(x,y,arena.Geometry);hero.transform.position=new Vector3(x,y*.5f,0);
                    yield return null;follow.Frame(1080,2340,361,1913);
                    Capture(camera,arena,"expanded-"+(int)p.x+"-"+(int)p.y,1080,2340);
                }
            }
            var hoist=Resources.Load<Texture2D>("FoundryStockHoist");
            {
                FoundryMap.ToFloor(-20,16,out float cx,out float cy);
                Assert.That(arena.Geometry.IsOnPlatform(cx,cy,HeroMotion.EdgeMargin),Is.True);
                movement.Motion.Place(cx,cy,arena.Geometry);hero.transform.position=new Vector3(cx,cy*.5f,0);
                yield return null;follow.Frame(1080,2340,361,1913);
                Capture(camera,arena,"casting-portrait",1080,2340);
            }
            if(hoist!=null)
                File.WriteAllText(Output+"hoist-texture.txt",$"FoundryStockHoist:{hoist.width}x{hoist.height} {hoist.format}; mipmaps:{hoist.mipmapCount}; readable:{hoist.isReadable}\n");
            foreach(string resource in new[]{"FoundryStockStation","FoundryCrane","FoundryCastingBench"})
            {
                var workshopTexture=Resources.Load<Texture2D>(resource);
                if(workshopTexture!=null)
                    File.WriteAllText(Output+resource+"-texture.txt",$"{resource}:{workshopTexture.width}x{workshopTexture.height} {workshopTexture.format}; mipmaps:{workshopTexture.mipmapCount}; readable:{workshopTexture.isReadable}\n");
            }
            var texture=Resources.Load<Texture2D>("FoundryShortBoiler");
            int renderers=0,vertices=0;foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {renderers++;vertices+=r.GetComponent<MeshFilter>().sharedMesh.vertexCount;}
            var distantBudget=new System.Collections.Generic.List<string>();
            foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(r.name.StartsWith("Distant foundry works"))
                    distantBudget.Add($"{r.name}: {r.GetComponent<MeshFilter>().sharedMesh.vertexCount} vertices; rank {r.sortingOrder}; shader {r.sharedMaterial.shader.name}");
            File.WriteAllLines(Output+"distant-budget.txt",distantBudget);
            File.WriteAllText(Output+"runtime-budget.txt",$"FoundryShortBoiler texture:{texture.width}x{texture.height} {texture.format}; mipmaps:{texture.mipmapCount}; readable:{texture.isReadable}\nRegion renderers:{renderers}; vertices:{vertices}\nActual production replacement, HUD hidden, matched walkable court, expanded-area, bridge and gate points in both phases. No synthetic prop placement.\n");
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
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Skyline capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
