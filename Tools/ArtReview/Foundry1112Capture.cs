using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    // 2026-11-12 trolley (V1) and gate header/crown (V2) package. Copied in temporarily by
    // Tools/ArtReview/Run-Foundry1112Proof.ps1; both phases use exactly this scene setup and
    // these cameras. It only captures and reports; it asserts nothing about the art itself.
    public sealed class Foundry1112Capture
    {
        static string Output;
        Scene _scene;
        [UnityTest] public IEnumerator CaptureFoundry1112()
        {
            string phase=File.ReadAllText("TestResults/foundry-1112-phase.txt").Trim();
            Assert.That(phase=="before"||phase=="after",Is.True);
            Output="ArtDirection/2026-11-12/foundry-trolley-crown-01/"+phase+"/";
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
            var notes=new List<string>();

            // V1: the active trolley (work island 0, design centre -25,-2.9), close, with its
            // rails/route, and the yard it sits in. The same cameras as foundry-trolley-01.
            Vector3 trolley=FoundrySurface.Project(-25,-2.9f);
            Fixed(camera,arena,"trolley-detail",trolley.x,trolley.y+.55f,2.4f,1200,1200);
            Fixed(camera,arena,"trolley-route",trolley.x,trolley.y+1.2f,4.6f,1200,1200);
            Vector3 yard=FoundrySurface.Project(-24,7);
            Fixed(camera,arena,"yard-detail",yard.x,yard.y,6,1200,1200);
            // Other consumers of the shared yard finishes: the moulding bench (island 1) and the stock.
            Vector3 mould=FoundrySurface.Project(-21.2f,12.9f);
            Fixed(camera,arena,"mould-bench-detail",mould.x,mould.y+.6f,3,1200,1200);
            Vector3 stock=FoundrySurface.Project(4.5f-20-12,20.45f);
            Fixed(camera,arena,"stock-detail",stock.x,stock.y+.5f,4.2f,1200,1200);
            Vector3 casting=FoundrySurface.Project(-15.5f,20.75f);
            Fixed(camera,arena,"casting-detail",casting.x,casting.y+1.2f,5,1200,1200);

            // V2: gate detail, crown, and a close header field beside the baked outer bay.
            Vector3 portal=FoundrySurface.Project(28,6);
            Fixed(camera,arena,"gate-detail",portal.x,portal.y+2,9,1200,1200);
            Fixed(camera,arena,"gate-crown-detail",portal.x,portal.y+6.5f,7,1200,1200);
            Fixed(camera,arena,"gate-header-close",portal.x+1.17f,portal.y+7.43f,3,1200,1200);

            // Whole-scene and stability views.
            Fixed(camera,arena,"full-map",0,2,37,1600,1200);
            Fixed(camera,arena,"overview",0,4,32,1600,1200);
            Vector3 court=FoundrySurface.Project(-3,1);
            Fixed(camera,arena,"court-detail",court.x,court.y,10,1200,1200);
            Vector3 forecourt=FoundrySurface.Project(-7,-13);
            Fixed(camera,arena,"forecourt-detail",forecourt.x,forecourt.y-1,11,1200,1200);

            // Native portrait approach cameras: the hero beside the trolley, in the yard, at the
            // gate causeway, plus the usual matched set. A position off the platform is skipped
            // and noted rather than failing, so both phases record the same views.
            foreach(var p in new[]{new Vector2(-21,-3),new Vector2(-24,1),new Vector2(-20,6),new Vector2(23,5),
                new Vector2(20,1),new Vector2(0,0),new Vector2(-16,15),new Vector2(-8,-12)})
            {
                FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                string name="approach-"+(int)p.x+"-"+(int)p.y;
                if(!arena.Geometry.IsOnPlatform(x,y,HeroMotion.EdgeMargin))
                {notes.Add(name+": skipped, not walkable");continue;}
                movement.Motion.Place(x,y,arena.Geometry);hero.transform.position=new Vector3(x,y*.5f,0);
                yield return null;
                follow.Frame(1080,2340,361,1913);
                Capture(camera,arena,name,1080,2340);
            }

            int renderers=0,vertices=0,distant=0;
            var lines=new List<string>();
            foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                int count=r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                renderers++;vertices+=count;
                if(r.name.StartsWith("Distant foundry works"))distant+=count;
                lines.Add($"{r.name}: {count} vertices; material {r.sharedMaterial.name}; order {r.sortingOrder}");
            }
            lines.Insert(0,$"Region renderers:{renderers}; vertices:{vertices}; distant vertices:{distant}");
            lines.Insert(1,$"Phase:{phase}. Actual production scene, HUD hidden, time stopped. Not a phone measurement.");
            lines.AddRange(notes);
            File.WriteAllLines(Output+"runtime-budget.txt",lines);
        }
        static void Fixed(Camera c,ArenaView arena,string name,float x,float y,float size,int width,int height)
        {
            c.transform.position=new Vector3(x,y,-10);c.orthographicSize=size;
            Capture(c,arena,name,width,height);
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
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry 1112 capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
