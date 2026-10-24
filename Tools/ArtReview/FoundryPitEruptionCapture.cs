using System.Collections;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // Actual foundry scene, portrait game camera: the second hall's district marked (on and off screen), then its fight.
    public sealed class FoundryPitEruptionCapture
    {
        const string Output="ArtDirection/2026-10-24/foundry-pit-eruption-01/";
        Scene _scene;
        [UnityTest] public IEnumerator CapturePitEruption()
        {
            TestProfile.Begin();Time.timeScale=1;
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath,"floor_room_sites_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            _scene=SceneManager.GetActiveScene();
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var input=Object.FindFirstObjectByType<HeroMovementInput>();input.enabled=false;var hero=input.transform;
            var camera=Camera.main;var follow=camera.GetComponent<ArenaCameraFollow>();var arena=Object.FindFirstObjectByType<ArenaView>();
            float deadline=Time.realtimeSinceStartup+30;
            while(encounter.AliveEnemyCount==0&&Time.realtimeSinceStartup<deadline)yield return null;
            FoundryMap.ToFloor(FoundryMap.PitWest-1.2f,6,out float x,out float y);
            hero.position=new Vector3(x,ArenaFloor.WorldY(y),0);
            while(setup.Pit.Clock.WarningProgress<.75f&&Time.realtimeSinceStartup<deadline)yield return null;
            Capture(camera,arena,follow,"pit-warning");
            bool erupted=false;setup.Pit.Erupted+=()=>erupted=true;
            while(!erupted&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;
            Capture(camera,arena,follow,"pit-spill");
            Assert.That(erupted,Is.True);
        }
        static void Resolve(CombatSetup setup){for(int i=0;i<8&&setup.Choices.Current!=null&&setup.Choices.Current.Kind==ChoiceKind.Upgrade;i++)setup.Choices.TrySelect(setup.Choices.Current,0);}
        static void Capture(Camera c,ArenaView arena,ArenaCameraFollow follow,string name)
        {
            const int width=1080,height=2340;
            follow.Frame(width,height,361,1913);
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
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Pit eruption capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
