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
    public sealed class FoundryKeepersCapture
    {
        static string Output;
        // Identical camera/actor setup is used for fresh baseline and changed-runtime captures.
        static readonly System.Collections.Generic.HashSet<string> SelectedViews=new System.Collections.Generic.HashSet<string>
        {"trolley-detail","trolley-route","gate-header-close","court-floor-close","gate-open-detail","gate-crown-detail","stock-detail","casting-detail","bridge-foot","approach--29--29","forecourt-detail","approach--8--12","bridge-detail","yard-detail","crane-detail","service-shaft-detail","skyline-detail","full-map","court-detail","shaft-detail","gate-detail","overview","approach-0-0","approach--20-6","approach--16-15","approach-20-1","pit-close","pit-near-corner","pit-far-corner","approach--10-0","approach--2-8","seal-close","way-close","underside-south","underside-west","bridge-head"};
        Scene _scene;
        [UnityTest] public IEnumerator CaptureKeepers()
        {
            string phase=File.ReadAllText("TestResults/foundry-keepers-phase.txt").Trim();
            // Both phases use this exact scene setup. The launcher pins Before to baseline HEAD.
            Assert.That(phase=="before"||phase=="after",Is.True);
            Output="ArtDirection/2026-10-22/foundry-keepers-01/"+phase+"/";
            TestProfile.Begin();Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;_scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            if(phase=="after")CheckCourtFloorOwnership(arena);
            var camera=Camera.main;var follow=camera.GetComponent<ArenaCameraFollow>();follow.enabled=false;
            Object.FindFirstObjectByType<CombatSetup>().Encounters.enabled=false;
            foreach(var a in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))a.enabled=false;
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            var movement=hero.GetComponent<HeroMovementInput>();movement.enabled=false;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.enabled=false;
            Vector3 pit=FoundrySurface.Project(-24,14);
            camera.transform.position=new Vector3(pit.x,pit.y-1,-10);camera.orthographicSize=7;
            Capture(camera,arena,"workshop-detail",1200,1200);
            Vector3 crane=FoundrySurface.Project(-25.5f-12,13)+Vector3.up*1.1f;
            camera.transform.position=new Vector3(crane.x-2,crane.y+5,-10);camera.orthographicSize=6.3f;
            Capture(camera,arena,"crane-detail",1200,1200);
            Vector3 stock=FoundrySurface.Project(4.5f-20-12,20.45f);
            camera.transform.position=new Vector3(stock.x,stock.y+.5f,-10);camera.orthographicSize=4.2f;
            Capture(camera,arena,"stock-detail",1200,1200);
            {
                // Matched baseline cameras retain the unchanged production camera width.
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
            // The same camera with the gate's leaves swung fully open (the state after the floor's boss falls). Before the
            // door existed this frame is the closed painting again.
            var doors=new System.Collections.Generic.List<MonoBehaviour>();
            foreach(var behaviour in arena.Region.GetComponentsInChildren<MonoBehaviour>())
                if(behaviour.GetType().Name=="FoundryGateDoorMotion")doors.Add(behaviour);
            foreach(var door in doors){door.enabled=false;door.GetType().GetMethod("Pose",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(door,new object[]{1f});}
            Capture(camera,arena,"gate-open-detail",1200,1200);
            foreach(var door in doors){door.GetType().GetMethod("Pose",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(door,new object[]{0f});door.enabled=true;}
            camera.transform.position=new Vector3(portal.x,portal.y+6.5f,-10);camera.orthographicSize=7;
            Capture(camera,arena,"gate-crown-detail",1200,1200);
            // Close crown/header field beside the baked outer bay for the stone-finish study.
            camera.transform.position=new Vector3(portal.x+1.17f,portal.y+7.43f,-10);camera.orthographicSize=3;
            Capture(camera,arena,"gate-header-close",1200,1200);
            // Open court floor beside the seal at a close phone-like scale for the flagstone study.
            Vector3 court=FoundrySurface.Project(3,-8);
            camera.transform.position=new Vector3(court.x,court.y,-10);camera.orthographicSize=4;
            Capture(camera,arena,"court-floor-close",1200,1200);
            // The seal and the bridge way at close scale, for the ceremonial stone study.
            Vector3 seal=FoundrySurface.Project(-3,-5);
            camera.transform.position=new Vector3(seal.x,seal.y,-10);camera.orthographicSize=4.5f;
            Capture(camera,arena,"seal-close",1200,1200);
            Vector3 way=FoundrySurface.Project(-10,-11);
            camera.transform.position=new Vector3(way.x,way.y,-10);camera.orthographicSize=4;
            Capture(camera,arena,"way-close",1200,1200);
            // Below the deck's camera-facing edges, for the hanging masonry study.
            Vector3 south=FoundrySurface.Project(-8,FoundryMap.South);
            camera.transform.position=new Vector3(south.x,south.y-5.5f,-10);camera.orthographicSize=7.5f;
            Capture(camera,arena,"underside-south",1200,1200);
            Vector3 west=FoundrySurface.Project(FoundryMap.West,-2);
            camera.transform.position=new Vector3(west.x,west.y-5.5f,-10);camera.orthographicSize=7.5f;
            Capture(camera,arena,"underside-west",1200,1200);
            // The bridge head and its keepers at the portrait camera's scale.
            Vector3 head=FoundrySurface.Project(-22.5f,-22.5f);
            camera.transform.position=new Vector3(head.x,head.y+1.5f,-10);camera.orthographicSize=6;
            Capture(camera,arena,"bridge-head",1200,1200);
            Vector3 gate=FoundrySurface.Project(-3,1);
            camera.transform.position=new Vector3(gate.x,gate.y,-10);camera.orthographicSize=10;
            Capture(camera,arena,"court-detail",1200,1200);
            Vector3 shaft=FoundrySurface.Project(-10,8);
            camera.transform.position=new Vector3(shaft.x,shaft.y-1,-10);camera.orthographicSize=8;
            Capture(camera,arena,"shaft-detail",1200,1200);
            // The pit at a close phone-like scale: the rail ring, its corners and the molten pool past the near rail.
            camera.transform.position=new Vector3(shaft.x,shaft.y-.6f,-10);camera.orthographicSize=4.6f;
            Capture(camera,arena,"pit-close",1200,1200);
            Vector3 nearCorner=FoundrySurface.Project(-16,2);
            camera.transform.position=new Vector3(nearCorner.x,nearCorner.y+.9f,-10);camera.orthographicSize=2.6f;
            Capture(camera,arena,"pit-near-corner",1200,1200);
            Vector3 farCorner=FoundrySurface.Project(-4,14);
            camera.transform.position=new Vector3(farCorner.x,farCorner.y-.2f,-10);camera.orthographicSize=2.6f;
            Capture(camera,arena,"pit-far-corner",1200,1200);
            camera.transform.position=new Vector3(0,4,-10);camera.orthographicSize=32;
            Capture(camera,arena,"overview",1600,1200);
            foreach(var p in new[]{new Vector2(0,0),new Vector2(-20,6),new Vector2(-16,15),new Vector2(20,1),new Vector2(-8,-12),new Vector2(-29,-29),new Vector2(-10,0),new Vector2(-2,8)})
            {
                FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                Assert.That(arena.Geometry.IsOnPlatform(x,y,HeroMotion.EdgeMargin),Is.True);
                movement.Motion.Place(x,y,arena.Geometry);hero.transform.position=new Vector3(x,y*.5f,0);
                yield return null;
                follow.Frame(1080,2340,361,1913);
                Capture(camera,arena,"approach-"+(int)p.x+"-"+(int)p.y,1080,2340);
            }
            // Fixed baseline cameras preserve scale across both phases.
            {
                camera.transform.position=new Vector3(0,2,-10);camera.orthographicSize=37;
                Capture(camera,arena,"full-map",1600,1200);
                Vector3 bridgeFoot=FoundrySurface.Project(-33,-33);
                camera.transform.position=new Vector3(bridgeFoot.x,bridgeFoot.y-3,-10);camera.orthographicSize=7;
                Capture(camera,arena,"bridge-foot",1200,1200);
                Vector3 yard=FoundrySurface.Project(-24,7);
                camera.transform.position=new Vector3(yard.x,yard.y,-10);camera.orthographicSize=6;
                Capture(camera,arena,"yard-detail",1200,1200);
                // The active native trolley (work island 0) at a close native camera and with its rails and route.
                Vector3 trolley=FoundrySurface.Project(-25,-2.9f);
                camera.transform.position=new Vector3(trolley.x,trolley.y+.55f,-10);camera.orthographicSize=2.4f;
                Capture(camera,arena,"trolley-detail",1200,1200);
                camera.transform.position=new Vector3(trolley.x,trolley.y+1.2f,-10);camera.orthographicSize=4.6f;
                Capture(camera,arena,"trolley-route",1200,1200);
            }
            Vector3 forecourt=FoundrySurface.Project(-7,-13);
            camera.transform.position=new Vector3(forecourt.x,forecourt.y-1,-10);camera.orthographicSize=11;
            Capture(camera,arena,"forecourt-detail",1200,1200);
            var hoist=Resources.Load<Texture2D>("FoundryStockHoist");
            if(hoist!=null)
                File.WriteAllText(Output+"hoist-texture.txt",$"FoundryStockHoist:{hoist.width}x{hoist.height} {hoist.format}; mipmaps:{hoist.mipmapCount}; readable:{hoist.isReadable}\n");
            foreach(string resource in new[]{"FoundryStockStation","FoundryCrane","FoundryCastingBench"})
            {
                var workshopTexture=Resources.Load<Texture2D>(resource);
                Assert.That(workshopTexture,Is.Not.Null);
                int expectedSize=resource=="FoundryCrane"?1024:512;
                Assert.That(workshopTexture.width,Is.EqualTo(expectedSize));
                Assert.That(workshopTexture.height,Is.EqualTo(expectedSize));
                Assert.That(workshopTexture.format,Is.EqualTo(TextureFormat.ASTC_6x6));
                Assert.That(workshopTexture.isReadable,Is.False);
                Assert.That(workshopTexture.mipmapCount,Is.EqualTo(resource=="FoundryCrane"?11:10));
                if(workshopTexture!=null)
                    File.WriteAllText(Output+resource+"-texture.txt",$"{resource}:{workshopTexture.width}x{workshopTexture.height} {workshopTexture.format}; mipmaps:{workshopTexture.mipmapCount}; readable:{workshopTexture.isReadable}\n");
            }
            var paving=(Texture2D)arena.PlatformMaterials.FoundryPaving.mainTexture;
            if(phase=="after")
            {
                var masonry=Resources.Load<Texture2D>("FoundryGateMasonry");
                Assert.That(masonry,Is.Not.Null);
                Assert.That(masonry.width,Is.EqualTo(512));Assert.That(masonry.height,Is.EqualTo(1024));
                Assert.That(masonry.format,Is.EqualTo(TextureFormat.ASTC_6x6));
                Assert.That(masonry.mipmapCount,Is.EqualTo(11));Assert.That(masonry.isReadable,Is.False);
                int batches=0;var variants=new System.Collections.Generic.HashSet<Material>();
                foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterial.mainTexture==masonry){batches++;variants.Add(renderer.sharedMaterial);}
                Assert.That(batches,Is.EqualTo(2));Assert.That(variants.Count,Is.EqualTo(1));
                File.WriteAllText(Output+"gate-masonry-texture.txt",
                    $"{masonry.name}:{masonry.width}x{masonry.height}; {masonry.format}; mipmaps:{masonry.mipmapCount}; readable:{masonry.isReadable}; Editor texture memory:{UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(masonry)} bytes\n"+
                    $"Inherited gate-card batches:{batches}; shared cutaway variants:{variants.Count}; existing focus-uniform update retained. The native Blender gate-bay render is unchanged by the bridge keepers package. No device frame-time claim.\n");
            }
            File.WriteAllText(Output+"paving-texture.txt",$"{paving.name}: {paving.width}x{paving.height} {paving.format}; mipmaps:{paving.mipmapCount}; readable:{paving.isReadable}; grain span:{FoundryRegionView.PavingGrainSpan}\n");
            if(phase=="after")
            {
                var cast=arena.PlatformMaterials.FoundryCastIron;
                var castTexture=(Texture2D)cast.mainTexture;
                int castBatches=0;var variants=new System.Collections.Generic.HashSet<Material>();
                foreach(var item in arena.Region.GetComponentsInChildren<MeshRenderer>())
                    if(item.sharedMaterial.mainTexture==castTexture){castBatches++;variants.Add(item.sharedMaterial);}
                File.WriteAllText(Output+"cast-iron-texture.txt",
                    $"{castTexture.name}: {castTexture.width}x{castTexture.height} {castTexture.format}; mipmaps:{castTexture.mipmapCount}; readable:{castTexture.isReadable}; tint:{cast.color}\n"+
                    $"Inherited cast-iron batches:{castBatches}; shared cutaway variants:{variants.Count}; existing focus-uniform update retained.\n");
            }
            var cavernRenderer=arena.Backdrop.CavernRenderer;
            Assert.That(cavernRenderer,Is.Not.Null,"The foundry cavern plate must be present.");
            var cavernSprite=cavernRenderer.sprite;
            Assert.That(cavernSprite,Is.Not.Null);
            var cavernTexture=cavernSprite.texture;
            File.WriteAllText(Output+"cavern-texture.txt",
                $"Sprite:{cavernSprite.name}; rect:{cavernSprite.rect.width}x{cavernSprite.rect.height}; pixels per unit:{cavernSprite.pixelsPerUnit}; bounds:{cavernSprite.bounds.size}\n"+
                $"Texture:{cavernTexture.name}; dimensions:{cavernTexture.width}x{cavernTexture.height}; format:{cavernTexture.format}; graphics format:{cavernTexture.graphicsFormat}; mipmaps:{cavernTexture.mipmapCount}; readable:{cavernTexture.isReadable}\n"+
                $"Renderer:{cavernRenderer.name}; sorting order:{cavernRenderer.sortingOrder}; shader:{cavernRenderer.sharedMaterial.shader.name}; color:{cavernRenderer.color}\n"+
                "Texture format is the actual graphics-capture import; it is not a physical Android texture observation.\n");
#if UNITY_EDITOR
            var cavernPath=UnityEditor.AssetDatabase.GetAssetPath(cavernTexture);
            var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(cavernPath);
            var android=importer.GetPlatformTextureSettings("Android");
            var automatic=importer.GetAutomaticFormat("Android");
            var direct=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(cavernPath);
            File.AppendAllText(Output+"cavern-texture.txt",
                $"Editor target:{UnityEditor.EditorUserBuildSettings.activeBuildTarget}; asset:{cavernPath}\n"+
                $"Android override:{android.overridden}; format:{android.format}; max:{android.maxTextureSize}; automatic:{automatic}\n"+
                $"Direct asset format:{direct.format}; sprite texture memory:{UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(cavernTexture)} bytes\n");
#endif
            var texture=Resources.Load<Texture2D>("FoundryShortBoiler");
            foreach(string resource in new[]{"FoundryBoiler","FoundryShortBoiler"})
            {
                var imported=Resources.Load<Texture2D>(resource);
                Assert.That(imported.width,Is.EqualTo(2048));Assert.That(imported.height,Is.EqualTo(1024));
                Assert.That(imported.mipmapCount,Is.EqualTo(12));Assert.That(imported.isReadable,Is.False);
                Assert.That(imported.format,Is.EqualTo(TextureFormat.ASTC_6x6));
                File.WriteAllText(Output+resource+"-texture.txt",
                    $"{resource}:{imported.width}x{imported.height}; {imported.format}; mipmaps:{imported.mipmapCount}; readable:{imported.isReadable}; Editor texture memory:{UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(imported)} bytes\n");
            }
            Vector3 northBoilers=FoundrySurface.Project(20.67f,15.15f);
            camera.transform.position=new Vector3(northBoilers.x,northBoilers.y+2.5f,-10);camera.orthographicSize=7.8f;
            Capture(camera,arena,"north-furnaces",1400,1200);
            Vector3 southBoilers=FoundrySurface.Project(20.4f,-4.08f);
            camera.transform.position=new Vector3(southBoilers.x,southBoilers.y-.4f,-10);camera.orthographicSize=5;
            Capture(camera,arena,"south-retorts",1400,1200);
            // The service shaft is a matched stability view in both phases.
            Vector3 serviceShaft=FoundrySurface.Project(-5,39);
            camera.transform.position=new Vector3(serviceShaft.x,serviceShaft.y-14.5f,-10);camera.orthographicSize=12.5f;
            Capture(camera,arena,"service-shaft-detail",1200,1200);
            int renderers=0,vertices=0;foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {renderers++;vertices+=r.GetComponent<MeshFilter>().sharedMesh.vertexCount;}
            var distantBudget=new System.Collections.Generic.List<string>();
            foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(r.name.StartsWith("Distant foundry works"))
                    distantBudget.Add($"{r.name}: {r.GetComponent<MeshFilter>().sharedMesh.vertexCount} vertices; rank {r.sortingOrder}; shader {r.sharedMaterial.shader.name}");
            File.WriteAllLines(Output+"distant-budget.txt",distantBudget);
            WriteDistantMaterialReport(arena);
            if(phase=="after")
            {
                typeof(FoundryRegionTests).GetMethod("AssertGateThresholdPresentation",BindingFlags.Static|BindingFlags.NonPublic)
                    .Invoke(null,new object[]{arena});
                File.WriteAllText(Output+"gate-threshold-presentation.txt",
                    "Actual merged scene: closed portal covers three lower-face witnesses, no later masonry covers them, threshold paving coverage remains, central posts absent.\n");
            }
            File.WriteAllText(Output+"runtime-budget.txt",$"FoundryShortBoiler texture:{texture.width}x{texture.height} {texture.format}; mipmaps:{texture.mipmapCount}; readable:{texture.isReadable}\nRegion renderers:{renderers}; vertices:{vertices}\nActual production scene, HUD hidden; thirty-six matched camera views; fresh Before at baseline b466288, After from changed runtime. No synthetic architecture placement or phone acceptance.\n");
        }
        static void CheckCourtFloorOwnership(ArenaView arena)
        {
            var centre=new Vector2(-3,-5);
            var end=new Vector2(15.9f,5);
            Vector2 direction=(end-centre).normalized,side=new Vector2(-direction.y,direction.x);
            var points=new[]{new Vector2(-2.719f,-4.833f),new Vector2(-4.237f,-7.149f),
                end-direction*.191f-side*1.413f,end-direction*.347f-side*1.823f,
                // Actual merged-floor ownership along the new bridge continuation,
                // including its oblique junction with the existing court route.
                new Vector2(-32.41f,-32.23f),new Vector2(-29.37f,-29.19f),
                new Vector2(-25.73f,-25.49f),new Vector2(-21.27f,-21.43f),
                new Vector2(-18.31f,-18.07f),new Vector2(-17.65f,-17.41f),
                new Vector2(-16.81f,-16.67f)};
            var report=new System.Collections.Generic.List<string>();
            foreach(Vector2 design in points)
            {
                Vector2 p=FoundrySurface.Project(design.x,design.y);int owners=0;
                foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                {
                    // Fitted stone and the laid cut stone (the seal's field and the processional ways) are both floor owners.
                    if(renderer.sharedMaterial!=arena.PlatformMaterials.FoundryPaving&&renderer.sharedMaterial!=arena.PlatformMaterials.FoundryFlagstone)continue;
                    var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                    var vertices=mesh.vertices;var triangles=mesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3)
                    {
                        Vector2 a=renderer.transform.TransformPoint(vertices[triangles[i]]);
                        Vector2 b=renderer.transform.TransformPoint(vertices[triangles[i+1]]);
                        Vector2 c=renderer.transform.TransformPoint(vertices[triangles[i+2]]);
                        double area=Cross(b-a,c-a);if(System.Math.Abs(area)<1e-10)continue;
                        double x=Cross(b-p,c-p)/area,y=Cross(c-p,a-p)/area,z=1-x-y;
                        if(x>1e-7&&y>1e-7&&z>1e-7)owners++;
                    }
                }
                report.Add($"Design {design}: {owners} paving triangle owner(s)");
                Assert.That(owners,Is.EqualTo(1),$"The actual merged floor and gate batches must not duplicate court stone at {design}.");
            }
            File.WriteAllLines(Output+"court-floor-ownership.txt",report);
        }
        static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        static void WriteDistantMaterialReport(ArenaView arena)
        {
            // Inspect the native mesh batches, not CavernRenderer's borrowed painted sprite.
            // sharedMaterial avoids creating instances merely by gathering evidence.
            var owned=typeof(FoundryRegionView).GetField("_distantMasonryMaterial",BindingFlags.Instance|BindingFlags.NonPublic)
                ?.GetValue(arena.Region) as Material;
            var lines=new System.Collections.Generic.List<string>
            {
                "Native distant foundry mesh/material observations. Painted backdrop metadata is separate in cavern-texture.txt.",
                "Observed resource ranges are not a physical-device performance measurement.",
                "Owned masonry material: "+(owned!=null?owned.name:"none (shared fallback)")+
                    "; atmosphere: "+(owned!=null&&owned.HasProperty("_Atmosphere")?Scalar(owned.GetFloat("_Atmosphere")):"not present")
            };
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if(!renderer.name.StartsWith("Distant foundry works"))continue;
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                var material=renderer.sharedMaterial;
                Vector4 colourMin=new Vector4(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
                Vector4 colourMax=new Vector4(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
                Vector2 uvMin=new Vector2(float.PositiveInfinity,float.PositiveInfinity),uvMax=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
                int finiteColours=0,invalidColours=0,finiteUvs=0,invalidUvs=0;
                var colours=mesh.colors;var uvs=mesh.uv;
                foreach(var colour in colours)
                {
                    if(!Finite(colour.r)||!Finite(colour.g)||!Finite(colour.b)||!Finite(colour.a)){invalidColours++;continue;}
                    var value=new Vector4(colour.r,colour.g,colour.b,colour.a);
                    colourMin=Vector4.Min(colourMin,value);colourMax=Vector4.Max(colourMax,value);finiteColours++;
                }
                foreach(var uv in uvs)
                {
                    if(!Finite(uv.x)||!Finite(uv.y)){invalidUvs++;continue;}
                    uvMin=Vector2.Min(uvMin,uv);uvMax=Vector2.Max(uvMax,uv);finiteUvs++;
                }
                lines.Add(renderer.name+": vertices="+mesh.vertexCount+"; rank="+renderer.sortingOrder+
                    "; material="+material.name+"; owned masonry="+(material==owned)+"; shader="+material.shader.name+
                    "; atmosphere="+(material.HasProperty("_Atmosphere")?Scalar(material.GetFloat("_Atmosphere")):"not present"));
                lines.Add("  RGBA entries="+colours.Length+"; finite="+finiteColours+"; nonfinite="+invalidColours+
                    "; min="+(finiteColours>0?Components(colourMin):"none")+"; max="+(finiteColours>0?Components(colourMax):"none"));
                lines.Add("  UV entries="+uvs.Length+"; finite="+finiteUvs+"; nonfinite="+invalidUvs+
                    "; min="+(finiteUvs>0?Scalar(uvMin.x)+","+Scalar(uvMin.y):"none")+
                    "; max="+(finiteUvs>0?Scalar(uvMax.x)+","+Scalar(uvMax.y):"none"));
            }
            File.WriteAllLines(Output+"distant-material.txt",lines);
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static string Scalar(float value)=>value.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        static string Components(Vector4 value)=>Scalar(value.x)+","+Scalar(value.y)+","+Scalar(value.z)+","+Scalar(value.w);
        static void Capture(Camera c,ArenaView arena,string name,int width,int height)
        {
            c.aspect=(float)width/height;arena.Backdrop.FrameCavern();
            if(!SelectedViews.Contains(name))return;
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
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Ceremonial architecture capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}
