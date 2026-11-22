using System.Collections;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // docs/62 P0a's Outworks probe in the actual foundry scene, the portrait game camera (development/outworks.txt on): the
    // anvils with their edge arrows, an anvil charging, the Keeper on its loop with its lantern, a pad's arc over the pit
    // mid-flight, the Bell's swarm with the open stair, and the stair held - one Turkish frame of the Bell beside them.
    // Copied into Tests/PlayMode by Run-OutworksProof.ps1 for one batch run and removed after; the frames are untracked.
    public sealed class OutworksCapture
    {
        const string Output="ArtDirection/2026-11-22/outworks-p0a/";
        Scene _scene;
        Language? _pinned;

        [UnityTest] public IEnumerator CaptureOutworks()
        {
            // Every frame a fixed 1/60 s of game time: the long first frame after a batch-mode load cannot walk the hero past
            // a pad or through half an arc (OutworksFlowTests reads the same way).
            TestProfile.Begin();Time.timeScale=1;Time.captureDeltaTime=1f/60f;_pinned=Loc.Override;
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.OutworksPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            File.WriteAllText(DevelopmentStart.OutworksPath,"on");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            _scene=SceneManager.GetActiveScene();
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var input=Object.FindFirstObjectByType<HeroMovementInput>();input.enabled=false;var hero=input.transform;
            var health=input.GetComponent<Health>();
            var camera=Camera.main;var follow=camera.GetComponent<ArenaCameraFollow>();var arena=Object.FindFirstObjectByType<ArenaView>();
            Assert.That(encounter.IsInOutworks,Is.True,"The probe is on.");
            yield return Frames(setup,health,30);

            // The opening: the hero where the floor begins, the anvils off the screen marked by their arrows.
            Capture(camera,arena,follow,"01-opening-arrows");

            // An anvil charging: the hero in its ring, its arc half full.
            OutworksRule.AnvilSite(2,out float ax,out float ay);
            Put(hero,ax+.6f,ay+.4f);
            yield return Seconds(setup,health,OutworksRule.AnvilChargeSeconds*.5f);
            Capture(camera,arena,follow,"02-anvil-charging");
            yield return Seconds(setup,health,OutworksRule.AnvilChargeSeconds*.6f);
            yield return Frames(setup,health,3);
            Capture(camera,arena,follow,"03-anvil-captured-offer");
            Resolve(setup);

            // The Keeper on its loop, its lantern under it, the hero a few units off.
            encounter.SkipOutworksClock(OutworksRule.KeeperSpawnSeconds);
            yield return Frames(setup,health,10);
            Health keeper=encounter.KeeperLeader;
            Assert.That(keeper,Is.Not.Null,"The Keeper walks on.");
            encounter.TryFloorPositionOf(keeper,out float kx,out float ky);
            Put(hero,kx+3.5f,ky-2f);
            yield return Frames(setup,health,20);
            Capture(camera,arena,follow,"04-keeper-lantern");
            encounter.TryFloorPositionOf(keeper,out kx,out ky);
            Put(hero,kx+14f,ky);
            yield return Frames(setup,health,20);
            Capture(camera,arena,follow,"05-keeper-arrow");

            // A pad's arc over the pit, mid-flight: the body lifted off its shadow.
            input.enabled=true;
            OutworksRule.PadLaunch(0,out float lx,out float ly);
            FoundryMap.ToFloor(-19.4f,8f,out float sx,out float sy);
            input.Motion.Place(sx,sy,ArenaGeometry.Foundry);
            Put(hero,sx,sy);
            input.Hold(new Vector2(lx-sx,ly-sy).normalized);
            float deadline=Time.realtimeSinceStartup+10f;
            while(!encounter.Leap.IsAirborne&&Time.realtimeSinceStartup<deadline){Heal(health);yield return null;}
            input.Release();
            while(encounter.Leap.IsAirborne&&encounter.Leap.Progress<.45f&&Time.realtimeSinceStartup<deadline){Heal(health);yield return null;}
            // The coroutine resumes after the Updates and before the LateUpdates: the view lifts the body for this frame first.
            Object.FindFirstObjectByType<OutworksView>().Refresh();
            Capture(camera,arena,follow,"06-pad-arc");
            while(encounter.Leap.IsAirborne&&Time.realtimeSinceStartup<deadline){Heal(health);yield return null;}
            yield return Frames(setup,health,5);
            Capture(camera,arena,follow,"07-pad-landed");
            input.enabled=false;

            // The Bell: the swarm rises round the hero and the stair opens.
            encounter.SkipOutworksClock(OutworksRule.BellSeconds);
            yield return Seconds(setup,health,1.5f);
            Capture(camera,arena,follow,"08-bell-swarm");
            Loc.Override=Language.Turkish;
            yield return Frames(setup,health,3);
            Capture(camera,arena,follow,"09-bell-swarm-tr");
            Loc.Override=_pinned??Language.English;
            yield return Frames(setup,health,3);

            // The open stair, then held.
            OutworksRule.StairSpot(out float stx,out float sty);
            Put(hero,stx+4f,sty-3f);
            yield return Frames(setup,health,20);
            Capture(camera,arena,follow,"10-stair-open");
            Put(hero,stx+.3f,sty);
            yield return Seconds(setup,health,ExitRule.DwellSeconds*.55f);
            Capture(camera,arena,follow,"11-stair-hold");
            yield return Seconds(setup,health,ExitRule.DwellSeconds);
            yield return Frames(setup,health,10);
            Assert.That(encounter.IsInOutworks,Is.False,"The stair held.");
            Capture(camera,arena,follow,"12-first-hall");
        }

        static void Put(Transform hero,float x,float y)=>hero.position=new Vector3(x,ArenaFloor.WorldY(y),hero.position.z);
        static void Heal(Health hero){if(hero!=null&&hero.IsAlive)hero.Heal(hero.Maximum);}
        static IEnumerator Frames(CombatSetup setup,Health hero,int frames){for(int i=0;i<frames;i++){Heal(hero);Resolve(setup);yield return null;}}
        static IEnumerator Seconds(CombatSetup setup,Health hero,float seconds)
        {
            float end=Time.time+seconds,real=Time.realtimeSinceStartup+seconds+10f;
            while(Time.time<end&&Time.realtimeSinceStartup<real){Heal(hero);yield return null;}
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
            Time.captureDeltaTime=0f;Time.timeScale=1;Loc.Override=_pinned;
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Outworks capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
