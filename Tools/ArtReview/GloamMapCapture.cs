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
    // Floors 1 (Foundry), 2 (Ice) and 3 (Gloam, docs/51) side by side: the descent's floors read from the first floor's
    // NextFloor chain, each dress applied in order through ArenaView.ApplyFloorTheme as EncounterController does (so
    // floor 3's change also releases floor 2's variants), the same frame of the same crowd (the density probe), each
    // floor's enemies in its palette (EnemyLookView.UseTheme); then the garden's growth close up on the rim (rim-1..3, the
    // same frames on floors 2 and 1) and the melt onto floor 3. Writes BMPs; Run-GloamMapProof.ps1 converts them to PNG.
    public sealed class GloamMapCapture
    {
        const string Root="ArtDirection/2026-11-12/gloam-map-01/";
        static readonly string[] Floors={"floor1","floor2","floor3"};
        Scene _scene;
        [UnityTest] public IEnumerator CaptureGloamMap()
        {
            TestProfile.Begin();
            foreach(var floor in Floors)Directory.CreateDirectory(Root+floor);
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
            FloorDefinition third=second.NextFloor;
            Assert.That(third,Is.Not.Null,"The second floor descends to a third.");
            Assert.That(second.Theme,Is.EqualTo(FloorTheme.Ice),"Floor 2 is dressed in ice.");
            Assert.That(third.Theme,Is.EqualTo(FloorTheme.Gloam),"Floor 3 is dressed as the garden.");
            var themes=new[]{FloorTheme.Foundry,second.Theme,third.Theme};
            Assert.That(arena.Theme,Is.EqualTo(FloorTheme.Foundry));
            var camera=Camera.main;var follow=camera.GetComponent<ArenaCameraFollow>();
            setup.Encounters.enabled=false;
            foreach(var a in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))a.enabled=false;
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            var movement=hero.GetComponent<HeroMovementInput>();movement.enabled=false;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.enabled=false;
            // The crowd as it stands, every floor in the same frame.
            follow.enabled=true;yield return null;follow.Frame(1080,2340,361,1913);follow.enabled=false;
            for(int f=0;f<Floors.Length;f++)
            {
                Dress(arena,themes[f]);
                yield return null;
                Capture(camera,arena,Floors[f]+"/crowd-portrait",1080,2340);
            }
            Dress(arena,FloorTheme.Foundry);
            // Every painted kind side by side before the hero (the Warden too, whom the crowd never fields), each floor.
            // A lone Warden off its fight may log what it misses; this is a picture, not a test of it.
            LogAssert.ignoreFailingMessages=true;
            var lineup=Lineup(hero.transform.position);
            for(int f=0;f<Floors.Length;f++)
            {
                Dress(arena,themes[f]);
                yield return null;
                follow.enabled=true;yield return null;follow.Frame(1080,2340,361,1913);follow.enabled=false;
                Capture(camera,arena,Floors[f]+"/enemy-lineup",1080,2340);
                // A closer look at the lineup alone: the bodies' value against the floor, the bloom against the loam.
                Vector3 at=hero.transform.position;
                camera.transform.position=new Vector3(at.x,at.y-.6f,-10);camera.orthographicSize=2.4f;
                Capture(camera,arena,Floors[f]+"/enemy-lineup-close",1600,900);
                int themed=0;foreach(var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))if(Shown(look)==themes[f]&&themes[f]!=FloorTheme.Foundry)themed++;
                File.WriteAllText(Root+Floors[f]+"/enemy-palette.txt",$"themed enemies:{themed}; lineup:{lineup.Length}; theme:{arena.Theme}\n");
            }
            foreach(var body in lineup)Object.Destroy(body);
            Dress(arena,FloorTheme.Foundry);
            yield return null;
            for(int f=0;f<Floors.Length;f++)
            {
                string floor=Floors[f];
                Dress(arena,themes[f]);
                yield return null;
                // The left dress's release runs over the frames after the change.
                for(int wait=0;wait<120&&f>0&&arena.Region.HeldVariantCount(themes[f-1])>0;wait++)yield return null;
                {
                    // The region's shown budget on this floor: active mesh renderers and their vertices, distant works
                    // apart, the garden's growth apart; and the theme variants the region holds loaded (none of the floor
                    // before).
                    int renderers=0,vertices=0,distant=0,growth=0;
                    foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
                    {
                        int count=r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                        renderers++;vertices+=count;
                        if(r.name.StartsWith("Distant foundry works"))distant+=count;
                        if(r.name==GrowthName)growth+=count;
                    }
                    File.WriteAllText(Root+floor+"/runtime-budget.txt",$"Region renderers:{renderers}; vertices:{vertices}; distant vertices:{distant}; growth vertices:{growth}; theme:{arena.Theme}; "+
                        $"held variants ice:{arena.Region.HeldVariantCount(FloorTheme.Ice)} gloam:{arena.Region.HeldVariantCount(FloorTheme.Gloam)}\n");
                }
                Vector3 court=FoundrySurface.Project(-3,1);
                camera.transform.position=new Vector3(court.x,court.y,-10);camera.orthographicSize=10;
                Capture(camera,arena,floor+"/court",1200,1200);
                Vector3 gate=FoundrySurface.Project(28,6);
                camera.transform.position=new Vector3(gate.x,gate.y+2,-10);camera.orthographicSize=9;
                Capture(camera,arena,floor+"/gate",1200,1200);
                // The rails: floor 2's mirror-ice inlay, floor 3's sap channel.
                Vector3 north=FoundrySurface.Project(5.5f,16);
                camera.transform.position=new Vector3(north.x,north.y+1.5f,-10);camera.orthographicSize=6;
                Capture(camera,arena,floor+"/north-rail",1200,1200);
                Vector3 pit=FoundrySurface.Project(-10,8);
                camera.transform.position=new Vector3(pit.x,pit.y-1,-10);camera.orthographicSize=8;
                Capture(camera,arena,floor+"/pit",1200,1200);
                // The pit-eruption warning, its telegraph held mid-warning (the clock stepped by hand, time stopped): the
                // rim band must stay red-orange on every floor (docs/51 7.4: on GloamPavingV1 too).
                if(setup.Pit!=null&&setup.Pit.Clock!=null)
                {
                    var clock=setup.Pit.Clock;
                    float target=PitEruptionRule.Period-PitEruptionRule.Warning*.4f;
                    if(clock.Seconds<target)clock.Step(target-clock.Seconds);
                    yield return null;
                    camera.transform.position=new Vector3(pit.x,pit.y-1,-10);camera.orthographicSize=9;
                    Capture(camera,arena,floor+"/pit-warning",1200,1200);
                    File.WriteAllText(Root+floor+"/pit-warning.txt",$"warning:{clock.IsWarning}; seconds:{clock.Seconds}; running:{setup.Pit.IsRunning}; theme:{arena.Theme}\n");
                }
                camera.transform.position=new Vector3(0,4,-10);camera.orthographicSize=32;
                Capture(camera,arena,floor+"/overview",1600,1200);
                camera.transform.position=new Vector3(0,2,-10);camera.orthographicSize=37;
                Capture(camera,arena,floor+"/full-map",1600,1200);
                foreach(var p in new[]{new Vector2(0,0),new Vector2(-20,6),new Vector2(20,1)})
                {
                    FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                    if(!arena.Geometry.IsOnPlatform(x,y,HeroMotion.EdgeMargin))continue;
                    movement.Motion.Place(x,y,arena.Geometry);hero.transform.position=new Vector3(x,y*.5f,0);
                    yield return null;
                    follow.Frame(1080,2340,361,1913);
                    Capture(camera,arena,floor+"/portrait_"+(int)p.x+"_"+(int)p.y,1080,2340);
                }
            }
            Assert.That(arena.Theme,Is.EqualTo(FloorTheme.Gloam));
            Assert.That(arena.Region.HeldVariantCount(FloorTheme.Ice),Is.EqualTo(0),"Floor 3 holds none of floor 2's variants.");
            // The garden's growth close up (docs/51 step 5): three spots along it, floor 3 first, then the same frames on
            // floors 2 and 1 for the rim they replace.
            Transform growthObject=arena.Region.transform.Find(GrowthName);
            Assert.That(growthObject!=null&&growthObject.gameObject.activeInHierarchy,Is.True,"Floor 3 shows its growth.");
            Vector3[] grown=growthObject.GetComponent<MeshFilter>().sharedMesh.vertices;
            var spots=new Vector3[3];
            for(int i=0;i<spots.Length;i++)spots[i]=growthObject.TransformPoint(grown[Mathf.Clamp((i*2+1)*grown.Length/6,0,grown.Length-1)]);
            for(int f=Floors.Length-1;f>=0;f--)
            {
                Dress(arena,themes[f]);
                yield return null;
                for(int i=0;i<spots.Length;i++)
                {
                    camera.transform.position=new Vector3(spots[i].x,spots[i].y,-10);camera.orthographicSize=3.5f;
                    Capture(camera,arena,Floors[f]+"/rim-"+(i+1),1200,1200);
                }
            }
            // The melt the descent shows as it lands on floor 3 (ArenaView.PlayGloamTransition), early in it: the frost
            // receding, the ash and gloamlight drifting in.
            Dress(arena,third.Theme);
            yield return null;
            follow.enabled=true;yield return null;follow.Frame(1080,2340,361,1913);follow.enabled=false;
            camera.aspect=1080f/2340;
            Assert.That(arena.PlayGloamTransition(),Is.True,"The melt plays the first time it is asked.");
            var advance=typeof(GloamTransitionView).GetMethod("Advance",Internal);
            advance.Invoke(arena.GloamTransition,new object[]{.2f});
            Capture(camera,arena,"floor3/melt-transition",1080,2340);
            advance.Invoke(arena.GloamTransition,new object[]{.2f});
            Capture(camera,arena,"floor3/melt-transition-late",1080,2340);
        }
        const string GrowthName="Garden roots, fungi and moss";
        // The arena's dress and every enemy's palette for a floor, as the descent and EncounterController.CreateEnemy do.
        // EnemyLookView.UseTheme and ShownTheme are internal: reached by reflection from the test assembly.
        const System.Reflection.BindingFlags Internal=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        static void Dress(ArenaView arena,FloorTheme theme)
        {
            arena.ApplyFloorTheme(theme);
            var use=typeof(EnemyLookView).GetMethod("UseTheme",Internal);
            foreach(var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))use.Invoke(look,new object[]{theme});
        }
        static FloorTheme Shown(EnemyLookView look)=>(FloorTheme)typeof(EnemyLookView).GetProperty("ShownTheme",Internal).GetValue(look);
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
            // The PlayMode tests do not reference ImageConversion (no EncodeToPNG): a BMP, which Run-GloamMapProof.ps1
            // converts to PNG.
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
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Gloam map capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
