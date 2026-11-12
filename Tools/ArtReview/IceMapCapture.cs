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
            // Floor 2's enemies wear the frosted palette (EnemyLookView.UseFrost, as EncounterController stands each up).
            arena.ApplyFloorTheme(second.Theme);Frost(true);
            Capture(camera,arena,"floor2/crowd-portrait",1080,2340);
            arena.ApplyFloorTheme(FloorTheme.Foundry);Frost(false);
            // Every painted kind side by side before the hero (the Warden too, whom the crowd never fields), each floor.
            // A lone Warden off its fight may log what it misses; this is a picture, not a test of it.
            LogAssert.ignoreFailingMessages=true;
            var lineup=Lineup(hero.transform.position);
            foreach(var floor in new[]{"floor1","floor2"})
            {
                bool ice=floor=="floor2";
                arena.ApplyFloorTheme(ice?second.Theme:FloorTheme.Foundry);Frost(ice);
                yield return null;
                follow.enabled=true;yield return null;follow.Frame(1080,2340,361,1913);follow.enabled=false;
                Capture(camera,arena,floor+"/enemy-lineup",1080,2340);
                int frosted=0;foreach(var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))if(ShowsFrost(look))frosted++;
                File.WriteAllText(Root+floor+"/enemy-palette.txt",$"frosted enemies:{frosted}; lineup:{lineup.Length}; theme:{arena.Theme}\n");
            }
            foreach(var body in lineup)Object.Destroy(body);
            arena.ApplyFloorTheme(FloorTheme.Foundry);Frost(false);
            yield return null;
            foreach(var floor in new[]{"floor1","floor2"})
            {
                if(floor=="floor2")arena.ApplyFloorTheme(second.Theme);
                {
                    // The region's shown budget on this floor: active mesh renderers and their vertices, distant works apart.
                    int renderers=0,vertices=0,distant=0;
                    foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
                    {
                        int count=r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                        renderers++;vertices+=count;
                        if(r.name.StartsWith("Distant foundry works"))distant+=count;
                    }
                    File.WriteAllText(Root+floor+"/runtime-budget.txt",$"Region renderers:{renderers}; vertices:{vertices}; distant vertices:{distant}; theme:{arena.Theme}\n");
                }
                Vector3 court=FoundrySurface.Project(-3,1);
                camera.transform.position=new Vector3(court.x,court.y,-10);camera.orthographicSize=10;
                Capture(camera,arena,floor+"/court",1200,1200);
                Vector3 gate=FoundrySurface.Project(28,6);
                camera.transform.position=new Vector3(gate.x,gate.y+2,-10);camera.orthographicSize=9;
                Capture(camera,arena,floor+"/gate",1200,1200);
                // Floor 2's mirror panels and drifts (FoundryRegionIceProps.cs): the north rail and the east rail.
                Vector3 north=FoundrySurface.Project(5.5f,16);
                camera.transform.position=new Vector3(north.x,north.y+1.5f,-10);camera.orthographicSize=6;
                Capture(camera,arena,floor+"/north-rail",1200,1200);
                Vector3 east=FoundrySurface.Project(12,4);
                camera.transform.position=new Vector3(east.x,east.y+1.5f,-10);camera.orthographicSize=9;
                Capture(camera,arena,floor+"/east-rail",1200,1200);
                Vector3 pit=FoundrySurface.Project(-10,8);
                camera.transform.position=new Vector3(pit.x,pit.y-1,-10);camera.orthographicSize=8;
                Capture(camera,arena,floor+"/pit",1200,1200);
                // The pit-eruption warning, its telegraph held mid-warning (the clock stepped by hand, time stopped): the
                // rim band must stay red-orange on either floor. The Warden's slam mark needs the boss fight: not shown.
                if(setup.Pit!=null&&setup.Pit.Clock!=null)
                {
                    var clock=setup.Pit.Clock;
                    float target=PitEruptionRule.Period-PitEruptionRule.Warning*.4f;
                    if(clock.Seconds<target)clock.Step(target-clock.Seconds);
                    yield return null;
                    camera.transform.position=new Vector3(pit.x,pit.y-1,-10);camera.orthographicSize=9;
                    Capture(camera,arena,floor+"/pit-warning",1200,1200);
                    File.WriteAllText(Root+floor+"/pit-warning.txt",$"warning:{clock.IsWarning}; seconds:{clock.Seconds}; running:{setup.Pit.IsRunning}\n");
                }
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
            // The frost the descent shows as it lands on floor 2 (ArenaView.PlayFrostTransition), near its peak, on the
            // portrait frame.
            camera.aspect=1080f/2340;
            Assert.That(arena.PlayFrostTransition(),Is.True,"The frost plays the first time it is asked.");
            typeof(FrostTransitionView).GetMethod("Advance",Internal).Invoke(arena.FrostTransition,new object[]{.15f});
            Capture(camera,arena,"floor2/frost-transition",1080,2340);
        }
        // EnemyLookView.UseFrost and ShowsFrost are internal: reached by reflection from the test assembly.
        const System.Reflection.BindingFlags Internal=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        static void Frost(bool on)
        {
            var use=typeof(EnemyLookView).GetMethod("UseFrost",Internal);
            foreach(var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))use.Invoke(look,new object[]{on});
        }
        static bool ShowsFrost(EnemyLookView look)=>(bool)typeof(EnemyLookView).GetProperty("ShowsFrost",Internal).GetValue(look);
        // The six painted kinds in a row a little below the hero, every body's own behaviour off so they stand.
        static GameObject[] Lineup(Vector3 hero)
        {
#if UNITY_EDITOR
            string[] kinds={"CinderMite","Runner","Grunt","Tank","GruntCaptain","ForgeWarden"};
            float[] x={-1.9f,-1.2f,-.45f,.45f,1.3f,2.2f};
            var bodies=new GameObject[kinds.Length];
            for(int i=0;i<kinds.Length;i++)
            {
                var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/"+kinds[i]+".prefab");
                bodies[i]=Object.Instantiate(prefab,hero+new Vector3(x[i],-1.1f,0f),Quaternion.identity);
                // The attacker and its targeting off: the body stands and breathes; its look and views stay on.
                if(bodies[i].TryGetComponent(out AttackController attack))attack.enabled=false;
                if(bodies[i].TryGetComponent(out Targeting targeting))targeting.enabled=false;
                if(bodies[i].TryGetComponent(out Health health))health.Initialize(100f);
            }
            return bodies;
#else
            return new GameObject[0];
#endif
        }
        static void Capture(Camera c,ArenaView arena,string name,int width,int height)
        {
            c.aspect=(float)width/height;arena.Backdrop.FrameCavern();
            var rt=RenderTexture.GetTemporary(width,height,24);var previous=RenderTexture.active;c.targetTexture=rt;
            c.Render();RenderTexture.active=rt;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            // The PlayMode tests do not reference ImageConversion (no EncodeToPNG): a BMP, as the other runners write,
            // which Run-IceMapProof.ps1 converts to PNG.
            using(var w=new BinaryWriter(File.Create(Root+name+".bmp")))
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
            if(File.Exists(DevelopmentStart.HordeProbePath))File.Delete(DevelopmentStart.HordeProbePath);
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Ice map capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
