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
    // 2026-11-12 court processional ways package (foundry-court-ways-02). Copied in temporarily by
    // Tools/ArtReview/Run-FoundryCourtWaysProof.ps1; both phases use exactly this scene setup and these
    // cameras. Enemy views are hidden so both phases frame the same floor. It captures and reports only.
    public sealed class FoundryCourtWaysCapture
    {
        static string Output;
        Scene _scene;
        [UnityTest] public IEnumerator CaptureFoundryCourtWays()
        {
            string phase=File.ReadAllText("TestResults/foundry-court-ways-phase.txt").Trim();
            Assert.That(phase=="before"||phase=="after",Is.True);
            Output="ArtDirection/2026-11-12/foundry-court-ways-02/"+phase+"/";
            TestProfile.Begin();Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;_scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            var camera=Camera.main;var follow=camera.GetComponent<ArenaCameraFollow>();follow.enabled=false;
            Object.FindFirstObjectByType<CombatSetup>().Encounters.enabled=false;
            foreach(var a in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))a.enabled=false;
            yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            var movement=hero.GetComponent<HeroMovementInput>();movement.enabled=false;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.enabled=false;
            var notes=new List<string>();

            // The seal and both processional ways: inside the octagon setting, along each way, and at the
            // bridge threshold and the gate landing where each way's brass lane lines end.
            Vector3 seal=FoundrySurface.Project(-3,-5);
            Fixed(camera,arena,"seal-detail",seal.x,seal.y,6.5f,1200,1200);
            Vector3 bridgeWay=FoundrySurface.Project(-11,-11.5f);
            Fixed(camera,arena,"bridge-way",bridgeWay.x,bridgeWay.y,6,1200,1200);
            Vector3 threshold=FoundrySurface.Project(-17.5f,-17.5f);
            Fixed(camera,arena,"bridge-threshold",threshold.x,threshold.y,5,1200,1200);
            Vector3 gateWay=FoundrySurface.Project(6,0);
            Fixed(camera,arena,"gate-way",gateWay.x,gateWay.y,6,1200,1200);
            Vector3 gateEnd=FoundrySurface.Project(14,4.2f);
            Fixed(camera,arena,"gate-way-end",gateEnd.x,gateEnd.y+.6f,5,1200,1200);
            Vector3 court=FoundrySurface.Project(-3,1);
            Fixed(camera,arena,"court-detail",court.x,court.y,10,1200,1200);
            Vector3 forecourt=FoundrySurface.Project(-7,-13);
            Fixed(camera,arena,"forecourt-detail",forecourt.x,forecourt.y-1,11,1200,1200);
            Fixed(camera,arena,"full-map",0,2,37,1600,1200);
            Fixed(camera,arena,"overview",0,4,32,1600,1200);

            // Native portrait gameplay cameras on and beside both ways. A position off the platform is
            // skipped and noted, so both phases record the same views.
            foreach(var p in new[]{new Vector2(0,0),new Vector2(-3,-1),new Vector2(-8,-12),new Vector2(-12,-12),
                new Vector2(4,-1),new Vector2(10,2),new Vector2(-16,-16)})
            {
                FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                string name="approach-"+(int)p.x+"-"+(int)p.y;
                if(!arena.Geometry.IsOnPlatform(x,y,HeroMotion.EdgeMargin)){notes.Add(name+": skipped, not walkable");continue;}
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
            lines.Insert(1,$"Phase:{phase}. Actual production scene, HUD and enemies hidden, time stopped. Not a phone measurement.");
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
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry court ways capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
