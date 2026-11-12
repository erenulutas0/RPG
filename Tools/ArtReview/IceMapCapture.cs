using System.Collections;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // Floor 1 (Foundry) and floor 2 (its theme read from the descent: the first floor's NextFloor) in the same scene, the
    // same frame of the same crowd (the density probe), applied through ArenaView.ApplyFloorTheme as EncounterController does.
    public sealed class IceMapCapture
    {
        const string Root="ArtDirection/2026-11-12/ice-map-01/";
        Scene _scene;
        [UnityTest] public IEnumerator CaptureIceMap()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Root+"floor1");Directory.CreateDirectory(Root+"floor2");
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            File.WriteAllText(DevelopmentStart.HordeProbePath,"24");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;_scene=SceneManager.GetActiveScene();
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            for(int frame=0;frame<600&&setup.Encounters.AllAliveCount<12;frame++)yield return null;
            Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            FloorDefinition second=setup.Encounters.Floor.NextFloor;
            Assert.That(second,Is.Not.Null,"The first floor descends to a second.");
            Assert.That(second.Theme,Is.EqualTo(FloorTheme.Ice),"Floor 2 is dressed in ice.");
            Assert.That(arena.Theme,Is.EqualTo(FloorTheme.Foundry));
            var camera=Camera.main;var follow=camera.GetComponent<ArenaCameraFollow>();
            setup.Encounters.enabled=false;
            foreach(var a in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))a.enabled=false;
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            var movement=hero.GetComponent<HeroMovementInput>();movement.enabled=false;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.enabled=false;
            // The crowd as it stands, both floors in the same frame.
            follow.enabled=true;yield return null;follow.Frame(1080,2340,361,1913);follow.enabled=false;
            Capture(camera,arena,"floor1/crowd-portrait",1080,2340);
            arena.ApplyFloorTheme(second.Theme);
            Capture(camera,arena,"floor2/crowd-portrait",1080,2340);
            arena.ApplyFloorTheme(FloorTheme.Foundry);
            foreach(var floor in new[]{"floor1","floor2"})
            {
                if(floor=="floor2")arena.ApplyFloorTheme(second.Theme);
                Vector3 court=FoundrySurface.Project(-3,1);
                camera.transform.position=new Vector3(court.x,court.y,-10);camera.orthographicSize=10;
                Capture(camera,arena,floor+"/court",1200,1200);
                Vector3 gate=FoundrySurface.Project(28,6);
                camera.transform.position=new Vector3(gate.x,gate.y+2,-10);camera.orthographicSize=9;
                Capture(camera,arena,floor+"/gate",1200,1200);
                Vector3 pit=FoundrySurface.Project(-10,8);
                camera.transform.position=new Vector3(pit.x,pit.y-1,-10);camera.orthographicSize=8;
                Capture(camera,arena,floor+"/pit",1200,1200);
                camera.transform.position=new Vector3(0,4,-10);camera.orthographicSize=32;
                Capture(camera,arena,floor+"/overview",1600,1200);
                camera.transform.position=new Vector3(0,2,-10);camera.orthographicSize=37;
                Capture(camera,arena,floor+"/full-map",1600,1200);
                foreach(var p in new[]{new Vector2(0,0),new Vector2(-20,6),new Vector2(-16,15),new Vector2(20,1),new Vector2(-31,-31)})
                {
                    FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                    if(!arena.Geometry.IsOnPlatform(x,y,HeroMotion.EdgeMargin))continue;
                    movement.Motion.Place(x,y,arena.Geometry);hero.transform.position=new Vector3(x,y*.5f,0);
                    yield return null;
                    follow.Frame(1080,2340,361,1913);
                    Capture(camera,arena,floor+"/portrait_"+(int)p.x+"_"+(int)p.y,1080,2340);
                }
            }
            Assert.That(arena.Theme,Is.EqualTo(FloorTheme.Ice));
        }
        static void Capture(Camera c,ArenaView arena,string name,int width,int height)
        {
            c.aspect=(float)width/height;arena.Backdrop.FrameCavern();
            var rt=RenderTexture.GetTemporary(width,height,24);var previous=RenderTexture.active;c.targetTexture=rt;
            c.Render();RenderTexture.active=rt;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            File.WriteAllBytes(Root+name+".png",pixels.EncodeToPNG());
            Object.DestroyImmediate(pixels);c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
        }
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;
            if(File.Exists(DevelopmentStart.HordeProbePath))File.Delete(DevelopmentStart.HordeProbePath);
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Ice map capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
