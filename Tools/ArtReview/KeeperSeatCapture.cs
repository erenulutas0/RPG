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
    public sealed class KeeperSeatCapture
    {
        static string Output;
        Scene _scene;
        [UnityTest] public IEnumerator CaptureProductionReplacement()
        {
            string phase=File.ReadAllText("TestResults/keeper-seat-phase.txt").Trim();
            Assert.That(phase=="before"||phase=="after");
            Output="ArtDirection/2026-10-09/keeper-seat-01/"+phase+"/";
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
            Vector3 seat=FoundrySurface.Project(-17.65f,-17.65f);
            camera.transform.position=new Vector3(seat.x,seat.y+1.6f,-10);camera.orthographicSize=4;
            Capture(camera,arena,"bridge-detail",1200,1200);
            foreach(var p in new[]{new Vector2(-18,-18),new Vector2(-17.3f,-12.1f),new Vector2(-15.85f,-18.55f)})
            {
                FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                Assert.That(arena.Geometry.IsOnPlatform(x,y,HeroMotion.EdgeMargin),Is.True);
                movement.Motion.Place(x,y,arena.Geometry);hero.transform.position=new Vector3(x,y*.5f,0);
                yield return null;
                follow.Frame(1080,2340,361,1913);
                Capture(camera,arena,"approach-"+(int)p.x+"-"+(int)p.y,1080,2340);
            }
            var texture=Resources.Load<Texture2D>("FoundryShortBoiler");
            int renderers=0,vertices=0;foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {renderers++;vertices+=r.GetComponent<MeshFilter>().sharedMesh.vertexCount;}
            File.WriteAllText(Output+"runtime-budget.txt",$"Texture:{texture.width}x{texture.height} {texture.format}; mipmaps:{texture.mipmapCount}; readable:{texture.isReadable}\nRegion renderers:{renderers}; vertices:{vertices}\nActual production replacement, HUD hidden, hero staged at three validated walkable points. No synthetic prop placement.\n");
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
