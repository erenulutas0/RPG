using System.Collections;
using System.IO;
using Cryptforge.Core;
using Cryptforge.UI;
using Cryptforge.Combat;
using Cryptforge.Art;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class FoundryRegionTests
    {
        private Scene _scene;
        private static bool FloorCovers(Mesh mesh,Vector2 point)
        {
            var vertices=mesh.vertices;var triangles=mesh.triangles;
            // Clipping can leave sub-pixel slivers. Float cross products of a far
            // point can cancel to barycentrics (0,1,0) on such a triangle. Bounds
            // rejection and double orientations keep this geometry oracle local.
            double Cross(Vector2 a,Vector2 b,Vector2 origin)=>((double)a.x-origin.x)*((double)b.y-origin.y)-((double)a.y-origin.y)*((double)b.x-origin.x);
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector2 a=vertices[triangles[i]],b=vertices[triangles[i+1]],c=vertices[triangles[i+2]];
                if(point.x<Mathf.Min(a.x,Mathf.Min(b.x,c.x))-1e-5f || point.x>Mathf.Max(a.x,Mathf.Max(b.x,c.x))+1e-5f ||
                    point.y<Mathf.Min(a.y,Mathf.Min(b.y,c.y))-1e-5f || point.y>Mathf.Max(a.y,Mathf.Max(b.y,c.y))+1e-5f)continue;
                double area=Cross(b,c,a);
                if(System.Math.Abs(area)<1e-8)continue;
                double u=Cross(b,c,point)/area,v=Cross(c,a,point)/area,w=1-u-v;
                if(u>=-1e-5&&v>=-1e-5&&w>=-1e-5)return true;
            }
            return false;
        }
        [Test] public void FloorCoverageRejectsRemoteClippedSliverAndRetainsRealFloor()
        {
            var mesh=new Mesh();
            try
            {
                mesh.vertices=new[]{new Vector3(-35.00179f,-7.247844f),new Vector3(-35.35534f,-7.071067f),
                    new Vector3(-35.35534f,-7.071068f),Vector3.zero,Vector3.right,Vector3.up};
                mesh.triangles=new[]{0,1,2,3,4,5};
                Assert.That(FloorCovers(mesh,new Vector2(-15.55635f,-16.97056f)),Is.False,
                    "A remote clipped sliver cannot cover a removed court corner.");
                Assert.That(FloorCovers(mesh,new Vector2(-199.7f,75.1f)),Is.False,
                    "Exact float32 counterexample: old barycentrics round to (0,0,1) for this remote point.");
                Assert.That(FloorCovers(mesh,new Vector2(.2f,.2f)),Is.True);
                Assert.That(FloorCovers(mesh,new Vector2(.8f,.8f)),Is.False);
            }
            finally {Object.DestroyImmediate(mesh);}
        }
        private static void AssertWorkIslandContacts(ArenaView arena,Mesh paving)
        {
            var iron=new System.Collections.Generic.List<(Vector3[] points,Vector2[] flags)>();
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.sharedMaterial.mainTexture!=arena.PlatformMaterials.FoundryIron.mainTexture)continue;
                if(renderer.sharedMaterial.shader.name!="Cryptforge/Foundry Cutaway")continue;
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                var flags=new System.Collections.Generic.List<Vector2>();mesh.GetUVs(1,flags);
                Assert.That(flags.Count,Is.EqualTo(mesh.vertexCount));
                iron.Add((mesh.vertices,flags.ToArray()));
            }
            Assert.That(iron,Is.Not.Empty);
            for(int island=0;island<FoundryMap.WorkIslandCount;island++)
            {
                float u0=FoundryMap.WorkIslandBound(island,0),v0=FoundryMap.WorkIslandBound(island,1);
                float u1=FoundryMap.WorkIslandBound(island,2),v1=FoundryMap.WorkIslandBound(island,3);
                FoundryMap.ToFloor((u0+u1)*.5f,(v0+v1)*.5f,out float cx,out float cy);
                Assert.That(FoundryMap.Contains(cx,cy,0),Is.False);
                Assert.That(FloorCovers(paving,new Vector2(cx,cy*.5f)),Is.True,"Raised work stands above real continuous floor triangles.");
                FoundryMap.ToFloor(u0,v1,out float lx,out float ly);
                FoundryMap.ToFloor(u1,v0,out float rx,out float ry);
                foreach(var corner in new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)})
                {
                    FoundryMap.ToFloor(corner.x,corner.y,out float x,out float y);
                    float depth=50+Mathf.Lerp(ly,ry,Mathf.InverseLerp(lx,rx,x))*.5f;
                    bool grounded=false;
                    foreach(var batch in iron)
                        for(int i=0;i<batch.points.Length;i++)
                        {
                            Vector3 p=batch.points[i];
                            // The authored skid is shallow; require real low geometry
                            // at each blocked corner, not a floating image or a distant vertex.
                            if(Mathf.Abs(p.x-x)>.005f||p.y<y*.5f||p.y>y*.5f+.12f)continue;
                            if(Mathf.Abs(p.z-depth)>.001f)continue;
                            Assert.That(batch.flags[i],Is.EqualTo(Vector2.one),"Each low corner owns its island depth and hero cutaway.");
                            grounded=true;break;
                        }
                    Assert.That(grounded,Is.True,$"Work island {island} has grounded iron at shared corner {corner}.");
                    Vector2 inset=Vector2.Lerp(corner,new Vector2((u0+u1)*.5f,(v0+v1)*.5f),.15f);
                    FoundryMap.ToFloor(inset.x,inset.y,out x,out y);
                    Assert.That(FloorCovers(paving,new Vector2(x,y*.5f)),Is.True,"The new obstacle cannot punch a hole in the rendered paving.");
                }
            }
        }
        private static int VerifyBoilerBand(Mesh mesh,int rank,bool squat)
        {
            Assert.That(rank==-10||rank==-7,Is.True,"Boilers retain the lower drum and upper shell ranks.");
            bool upper=rank==-7;
            var centres=squat?new[]{new Vector2(17.9f,-4.08f),new Vector2(26.9f,-4.08f)}:
                new[]{new Vector2(18.39f,15.81f),new Vector2(26.95f,14.5f)};
            var points=mesh.vertices;var uv=mesh.uv;var colours=mesh.colors;var triangles=mesh.triangles;
            var flags=new System.Collections.Generic.List<Vector2>();mesh.GetUVs(1,flags);
            Assert.That(points.Length,Is.GreaterThanOrEqualTo(8));
            Assert.That(uv.Length,Is.EqualTo(points.Length));Assert.That(colours.Length,Is.EqualTo(points.Length));
            Assert.That(flags.Count,Is.EqualTo(points.Length));
            foreach(var flag in flags)Assert.That(flag,Is.EqualTo(Vector2.one));
            float axis=Cryptforge.Art.FoundryMap.Axis,imageBottom=squat?-1.86f:-1.8f,size=squat?6f:10.8f;
            float split=squat?1.2f:2.8f,bottom=upper?split:imageBottom,top=upper?imageBottom+size:split;
            var expectedUv=new[]{new Vector2(0,(bottom-imageBottom)/size),new Vector2(1,(bottom-imageBottom)/size),
                new Vector2(1,(top-imageBottom)/size),new Vector2(0,(top-imageBottom)/size)};
            int first=0,triangle=0,hardware=0;
            for(int side=0;side<2;side++)
            {
                Vector2 c=centres[side];float originX=(c.x-c.y)*axis,originY=(c.x+c.y)*axis*.5f;
                float depth=50+originY;int end=first;
                while(end<points.Length&&Mathf.Abs(points[end].z-depth)<.001f)end++;
                Assert.That(end-first,Is.GreaterThanOrEqualTo(4),"Each contiguous owner group begins with its original card.");
                bool decorated=upper&&squat&&side==0;
                if(decorated)Assert.That(end-first,Is.GreaterThan(4),"The selected upper shell carries its fitted service assembly.");
                else Assert.That(end-first,Is.EqualTo(4),"Other shells and both lower bands retain only the original card.");
                var expectedPoints=new[]{new Vector2(originX-size*.5f,originY+bottom),new Vector2(originX+size*.5f,originY+bottom),
                    new Vector2(originX+size*.5f,originY+top),new Vector2(originX-size*.5f,originY+top)};
                for(int corner=0;corner<4;corner++)
                {
                    int at=first+corner;
                    Assert.That(Vector2.Distance(points[at],expectedPoints[corner]),Is.LessThan(.001f),"Hardware must not reshape or move either baked card.");
                    Assert.That(Vector2.Distance(uv[at],expectedUv[corner]),Is.LessThan(.00001f));
                    Assert.That(colours[at],Is.EqualTo(Color.white));
                }
                int[] cardTriangles={first,first+1,first+2,first,first+2,first+3};
                Assert.That(triangles.Length-triangle,Is.GreaterThanOrEqualTo(6));
                foreach(int index in cardTriangles)Assert.That(triangles[triangle++],Is.EqualTo(index),"The image quad draws before its fitted parts.");
                Vector2 sample=squat?new Vector2(.52f,.53f):new Vector2(.60f,.68f);
                for(int i=first+4;i<end;i++)
                {
                    Assert.That(uv[i],Is.EqualTo(sample),"Hardware samples only the verified opaque steel patch.");
                    Assert.That(points[i].z,Is.EqualTo(depth).Within(.001f),"Service parts inherit their own boiler's depth.");
                    float dx=Mathf.Abs(points[i].x-originX),height=squat?2.28f:7.2f;
                    Assert.That(dx,Is.LessThanOrEqualTo(4*axis+.001f));
                    Assert.That(points[i].y,Is.InRange(originY-2*axis+dx*.5f-.001f,originY+2*axis-dx*.5f+height+.001f),
                        "Fitted parts stay inside the original projected footprint and height extrusion.");
                }
                while(triangle<triangles.Length&&triangles[triangle]<end)
                {
                    Assert.That(triangles.Length-triangle,Is.GreaterThanOrEqualTo(3));
                    for(int corner=0;corner<3;corner++)
                        Assert.That(triangles[triangle++],Is.InRange(first+4,end-1),"Hardware triangles cannot connect to image cards or another boiler.");
                }
                hardware+=end-first-4;first=end;
            }
            Assert.That(first,Is.EqualTo(points.Length),"Only the two original boiler depth owners may occur in a shared band.");
            Assert.That(triangle,Is.EqualTo(triangles.Length));
            if(!upper)Assert.That(points.Length,Is.EqualTo(8));
            return hardware;
        }
        [Test] public void ArchitectureLightFollowsFireHeightAndRecessesBelowLedges()
        {
            Color near=FoundryRegionView.ArchitectureLight(-8,2,0,2);
            Color above=FoundryRegionView.ArchitectureLight(-8,2,14,2);
            Assert.That(near.r/near.b,Is.GreaterThan(above.r/above.b+.1f),"Pit heat must fall away above the fire, not climb the entire structure.");
            Color recessed=FoundryRegionView.ArchitectureLight(-8,2,0,0);
            Assert.That(recessed.r,Is.LessThan(near.r*.9f),"The ledge underside needs localized contact shade.");
        }
        [Test] public void MetalReflectionFallsOffInHeightAndStaysContinuousAcrossTheRamp()
        {
            Color near=FoundryRegionView.MetalLight(26.9f,-4.08f,1.8f);
            Color high=FoundryRegionView.MetalLight(26.9f,-4.08f,12f);
            Color away=FoundryRegionView.MetalLight(-40,-40,1.8f);
            Assert.That(near.r/near.b,Is.GreaterThan(high.r/high.b+.2f),"Heat must remain near the fire, not color the whole stack.");
            Assert.That(high,Is.EqualTo(away),"Far above all lamps only the cool metal ambient remains.");
            foreach(float seam in new[]{29f,31f})
                for(float v=-5;v<=16;v+=1)
                {
                    Color a=FoundryRegionView.MetalLight(seam-.001f,v,1.8f);
                    Color b=FoundryRegionView.MetalLight(seam+.001f,v,1.8f);
                    Assert.That(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b),Is.LessThan(.005f));
                }
            for(float u=-35;u<=38;u+=2)for(float v=-30;v<=22;v+=2)
                foreach(float h in new[]{0f,1.8f,4f,8f})
                {
                    Color light=FoundryRegionView.MetalLight(u,v,h);
                    Assert.That(light.r,Is.InRange(0f,1.9f));Assert.That(light.g,Is.InRange(0f,1.9f));
                    Assert.That(light.b,Is.InRange(0f,1.9f));Assert.That(light.a,Is.EqualTo(1));
                }
        }

        [SetUp] public void Begin()=>TestProfile.Begin();
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
        [UnityTest] public IEnumerator FlagCreatesPlayableMapAndReloadReleasesMeshes()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            var movement=Object.FindFirstObjectByType<HeroMovementInput>();
            Assert.That(arena.Region,Is.Not.Null);Assert.That(arena.Platform,Is.Null);
            // Test the actual generated triangles, not just the map predicates: clipped ashlar
            // must leave the removed tips empty and keep the lanes/bridge covered.
            Mesh paving=null;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial==arena.PlatformMaterials.FoundryPaving)paving=renderer.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(paving,Is.Not.Null);
            AssertWorkIslandContacts(arena,paving);
            foreach(var sample in new[]{new Vector3(FoundryMap.West+1,-13,0),new Vector3(13,-13,0),
                new Vector3(-26.17f,-9.77f,1),new Vector3(9.83f,-9.77f,1),new Vector3(-23,-23,1),
                new Vector3(FoundryMap.West+1,17,0),new Vector3(13,17,0),new Vector3(FoundryMap.West+2.83f,14.77f,1),new Vector3(10.83f,14.77f,1),
                new Vector3(-29,-13,1),new Vector3(-29,17,1),new Vector3(-34.17f,.23f,1),new Vector3(-32.17f,12.23f,1),
                new Vector3(-23.17f,-12.77f,1),new Vector3(-23.17f,16.77f,1),new Vector3(-27.17f,6.23f,1),
                new Vector3(-32.17f,-31.77f,1),new Vector3(-29.83f,-34.17f,1),new Vector3(-29.35f,-34.65f,0),
                new Vector3(-15.17f,-14.23f,1),new Vector3(-15.17f,-13.77f,1),new Vector3(-10.17f,-13.77f,1)})
            {
                Cryptforge.Art.FoundryMap.ToFloor(sample.x,sample.y,out float px,out float py);
                Assert.That(FloorCovers(paving,new Vector2(px,py*.5f)),Is.EqualTo(sample.z>0),$"Rendered floor at {sample}");
            }
            var gate=arena.PlatformMaterials.GateFacade;
            Assert.That(gate,Is.Not.Null,"Foundry facade must be an imported shared material.");
            Assert.That(((Texture2D)gate.mainTexture).isReadable,Is.False);
            Assert.That(gate.mainTexture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            bool usesGate=false;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial.mainTexture==gate.mainTexture)usesGate=true;
            Assert.That(usesGate,Is.True);
            Assert.That(arena.PlatformMaterials.FoundryPitLining,Is.Not.SameAs(arena.PlatformMaterials.FurnaceFacade),"The pit lining must stay independent of the boiler wrap.");
            var surfaces=new[]{arena.PlatformMaterials.FoundryWall,arena.PlatformMaterials.FoundryPitLining,arena.PlatformMaterials.FoundryIron,arena.PlatformMaterials.FoundryEmbers,arena.PlatformMaterials.FoundryPaving};
            foreach(var shared in surfaces)
            {
                Assert.That(shared,Is.Not.Null);
                Assert.That(((Texture2D)shared.mainTexture).isReadable,Is.False);
                Assert.That(Mathf.IsPowerOfTwo(shared.mainTexture.width)&&Mathf.IsPowerOfTwo(shared.mainTexture.height),Is.True,"Mipmapped foundry surfaces must be POT to preserve Android compression.");
                bool used=false;
                foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterial.mainTexture==shared.mainTexture)used=true;
                Assert.That(used,Is.True,"Imported foundry surface must be used by the scene.");
            }
            foreach(var r in arena.Region.GetComponentsInChildren<MeshRenderer>())
                Assert.That(r.sharedMaterial.mainTexture,Is.Not.SameAs(arena.PlatformMaterials.FurnaceFacade.mainTexture),"The four baked boilers retire the old furnace-wrap batch.");
            var keeper=arena.PlatformMaterials.ForgeKeeper;
            Assert.That(keeper,Is.Not.Null,"The paired bridge guardians use one imported material.");
            var keeperTexture=keeper.mainTexture as Texture2D;
            Assert.That(keeperTexture,Is.Not.Null);
            Assert.That(keeperTexture.isReadable,Is.False);
            Assert.That(keeperTexture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(Mathf.IsPowerOfTwo(keeperTexture.width)&&Mathf.IsPowerOfTwo(keeperTexture.height),Is.True);
            Mesh keeperMesh=null;
            Material keeperVariant=null;
            var baked=Resources.Load<Texture2D>("FoundryBoiler");
            Assert.That(baked,Is.Not.Null);
            Assert.That(baked.isReadable,Is.False);
            Assert.That(baked.mipmapCount,Is.GreaterThan(1));
            Assert.That(baked.width,Is.EqualTo(1024));
            Assert.That(baked.height,Is.EqualTo(1024));
            var shortBake=Resources.Load<Texture2D>("FoundryShortBoiler");
            Assert.That(shortBake,Is.Not.Null);
            Assert.That(shortBake.isReadable,Is.False);
            Assert.That(shortBake.width,Is.EqualTo(1024));
            Assert.That(shortBake.mipmapCount,Is.GreaterThan(1));
            var boilerMeshes=new System.Collections.Generic.List<Mesh>();
            int boilerHardware=0;
            var reservoir=Resources.Load<Texture2D>("FoundryReservoir");
            Assert.That(reservoir,Is.Not.Null);
            Assert.That(reservoir.width,Is.EqualTo(512));
            Assert.That(reservoir.isReadable,Is.False);
            Assert.That(reservoir.mipmapCount,Is.GreaterThan(1));
            var salvage=Resources.Load<Texture2D>("FoundrySalvage");
            Assert.That(salvage,Is.Not.Null);
            Assert.That(salvage.width,Is.EqualTo(512));
            Assert.That(salvage.isReadable,Is.False);
            Assert.That(salvage.mipmapCount,Is.GreaterThan(1));
            var stockHoist=Resources.Load<Texture2D>("FoundryStockHoist");
            Assert.That(stockHoist,Is.Not.Null);
            Assert.That(stockHoist.width,Is.EqualTo(512));
            Assert.That(stockHoist.isReadable,Is.False);
            Assert.That(stockHoist.mipmapCount,Is.GreaterThan(1));
            var stockStation=Resources.Load<Texture2D>("FoundryStockStation");
            var craneTexture=Resources.Load<Texture2D>("FoundryCrane");
            Assert.That(stockStation,Is.Not.Null);Assert.That(craneTexture,Is.Not.Null);
            Assert.That(stockStation.width,Is.EqualTo(512));
            Assert.That(craneTexture.width,Is.EqualTo(1024));
            foreach(var texture in new[]{stockStation,craneTexture})
            {
                Assert.That(texture.isReadable,Is.False);
                Assert.That(texture.mipmapCount,Is.GreaterThan(1));
            }
            int stockBatches=0,craneBatches=0;
            var workshopMeshes=new System.Collections.Generic.List<Mesh>();
            var workshopVariants=new System.Collections.Generic.List<Material>();
            int hoistBatches=0;
            Material hoistVariant=null;
            Mesh hoistMesh=null;
            int salvageBatches=0;
            int reservoirBatches=0;
            var reservoirRanks=new System.Collections.Generic.HashSet<int>();
            int shortBatches=0;
            int keeperBatches=0,otherBatches=0,bakedBatches=0,bakedDepthBands=0;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.sharedMaterial.mainTexture==stockStation||renderer.sharedMaterial.mainTexture==craneTexture)
                {
                    bool isCrane=renderer.sharedMaterial.mainTexture==craneTexture;
                    if(isCrane)craneBatches++;else stockBatches++;
                    Assert.That(renderer.sortingOrder,Is.EqualTo(isCrane?-9:-10));
                    var card=renderer.GetComponent<MeshFilter>().sharedMesh;
                    workshopMeshes.Add(card);workshopVariants.Add(renderer.sharedMaterial);
                    Assert.That(card.vertexCount,Is.EqualTo(4));
                    var flags=new System.Collections.Generic.List<Vector2>();card.GetUVs(1,flags);
                    Assert.That(flags.Count,Is.EqualTo(4));
                    foreach(var flag in flags)Assert.That(flag,Is.EqualTo(Vector2.one));
                    float minimum=(isCrane?-16:.5f)+FoundryMap.WestAssemblyOffset;
                    float maximum=(isCrane?-9:9.4f)+FoundryMap.WestAssemblyOffset;
                    foreach(var p in card.vertices)
                        Assert.That(p.z,Is.InRange(50+minimum*Cryptforge.Art.FoundryMap.Axis*.5f-.001f,
                            50+maximum*Cryptforge.Art.FoundryMap.Axis*.5f+.001f),
                            "Explicit structure depth stays within its original footprint, including tall transparent margins.");
                    continue;
                }
                if(renderer.sharedMaterial.mainTexture==stockHoist)
                {
                    hoistBatches++;hoistVariant=renderer.sharedMaterial;
                    Assert.That(renderer.sortingOrder,Is.EqualTo(-11),"Hoist stays behind stock -10, crane -9 and live wood -8.");
                    hoistMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(hoistMesh.vertexCount,Is.EqualTo(4));
                    var flags=new System.Collections.Generic.List<Vector2>();hoistMesh.GetUVs(1,flags);
                    Assert.That(flags.Count,Is.EqualTo(4));
                    foreach(var flag in flags)Assert.That(flag,Is.EqualTo(Vector2.one),"Tall crosshead retains explicit stock-shelf depth and cutaway ownership.");
                    float shift=Cryptforge.Art.FoundryMap.WestAssemblyOffset;
                    float leftDepth=50+(-18+shift+22.4f)*Cryptforge.Art.FoundryMap.Axis*.5f;
                    float rightDepth=50+(-13+shift+18.5f)*Cryptforge.Art.FoundryMap.Axis*.5f;
                    foreach(var p in hoistMesh.vertices)Assert.That(p.z,Is.InRange(leftDepth-.001f,rightDepth+.001f));
                    continue;
                }
                if(renderer.sharedMaterial.mainTexture==salvage)
                {
                    salvageBatches++;
                    Assert.That(renderer.sortingOrder,Is.EqualTo(-7));
                    var station=renderer.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(station.vertexCount,Is.EqualTo(4));
                    var flags=new System.Collections.Generic.List<Vector2>();station.GetUVs(1,flags);
                    Assert.That(flags.Count,Is.EqualTo(4));
                    foreach(var flag in flags)Assert.That(flag,Is.EqualTo(Vector2.one));
                    // Whole-shelf depth spans its unequal design-plane side lengths.
                    float westDepthShift=Cryptforge.Art.FoundryMap.WestAssemblyOffset*Cryptforge.Art.FoundryMap.Axis*.5f;
                    foreach(var p in station.vertices)Assert.That(p.z,Is.InRange(42.70f+westDepthShift,43.13f+westDepthShift));
                    continue;
                }
                if(renderer.sharedMaterial.mainTexture==reservoir)
                {
                    reservoirBatches++;reservoirRanks.Add(renderer.sortingOrder);
                    bool far=renderer.sortingOrder==-9;
                    Assert.That(renderer.sortingOrder,Is.EqualTo(far?-9:-7),"Tank ranks must straddle the service mains at -8.");
                    var tank=renderer.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(tank.vertexCount,Is.EqualTo(4));
                    foreach(var p in tank.vertices)
                        Assert.That(p.z,Is.EqualTo(50+(22.3f+(far?15.8f:-5.8f))*.35355339f).Within(.001f));
                    var flags=new System.Collections.Generic.List<Vector2>();tank.GetUVs(1,flags);
                    foreach(var flag in flags)Assert.That(flag,Is.EqualTo(Vector2.one));
                    continue;
                }
                if(renderer.sharedMaterial.mainTexture==shortBake)
                {
                    shortBatches++;
                    var shortMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                    boilerMeshes.Add(shortMesh);
                    boilerHardware+=VerifyBoilerBand(shortMesh,renderer.sortingOrder,true);
                    continue;
                }
                if(renderer.sharedMaterial.mainTexture==baked)
                {
                    bakedBatches++;
                    if(renderer.sortingOrder>-15)
                    {
                        bakedDepthBands++;
                        var card=renderer.GetComponent<MeshFilter>().sharedMesh;
                        boilerMeshes.Add(card);
                        boilerHardware+=VerifyBoilerBand(card,renderer.sortingOrder,false);
                    }
                    continue;
                }
                if(renderer.sharedMaterial.mainTexture!=keeperTexture){otherBatches++;continue;}
                keeperBatches++;
                keeperVariant=renderer.sharedMaterial;
                keeperMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(renderer.sortingOrder,Is.EqualTo(-6),"The shared guardian rank remains below actors.");
                Assert.That(keeperVariant.shader.name,Is.EqualTo("Cryptforge/Foundry Cutaway"));
                Assert.That(keeperVariant,Is.Not.SameAs(keeper),"The scene owns its cutaway variant, not the imported material.");
                Assert.That(keeperMesh.vertexCount,Is.EqualTo(8),"Two guardian cards share one batch.");
                Assert.That(keeperMesh.triangles.Length,Is.EqualTo(12));
                var keeperFlags=new System.Collections.Generic.List<Vector2>();
                keeperMesh.GetUVs(1,keeperFlags);
                Assert.That(keeperFlags.Count,Is.EqualTo(keeperMesh.vertexCount));
                foreach(var flag in keeperFlags)
                    Assert.That(flag,Is.EqualTo(Vector2.one),"Every guardian vertex writes depth and supports hero cutaway.");
                var keeperPoints=keeperMesh.vertices;
                var keeperUvs=keeperMesh.uv;
                for(int side=0;side<2;side++)
                {
                    int first=side*4;
                    // The complete card is outboard of the bridge's walking width, including its transparent margin.
                    float sign=Mathf.Sign(keeperPoints[first].x);
                    for(int corner=0;corner<4;corner++)
                    {
                        var point=keeperPoints[first+corner];
                        Assert.That(Mathf.Sign(point.x),Is.EqualTo(sign));
                        Assert.That(Mathf.Abs(point.x),Is.GreaterThan(Cryptforge.Art.FoundryMap.BridgeHalfWidth*Cryptforge.Art.FoundryMap.Axis));
                        Assert.That(point.z,Is.GreaterThan(0f));
                    }
                    Assert.That(keeperUvs[first],Is.EqualTo(Vector2.zero));
                    Assert.That(keeperUvs[first+1],Is.EqualTo(Vector2.right));
                    Assert.That(keeperUvs[first+2],Is.EqualTo(Vector2.one));
                    Assert.That(keeperUvs[first+3],Is.EqualTo(Vector2.up),"Both cards preserve the source's baked lighting orientation.");
                }
            }
            Assert.That(keeperBatches,Is.EqualTo(1),"The guardian pair adds exactly one shared batch.");
            Assert.That(bakedBatches,Is.EqualTo(2),"Two replacements: two shared depth bands; shade shares existing floor decals.");
            Assert.That(bakedDepthBands,Is.EqualTo(2));
            Assert.That(salvageBatches,Is.EqualTo(1));
            Assert.That(hoistBatches,Is.EqualTo(1),"One hoist card replaces the procedural assembly.");
            Assert.That(stockBatches,Is.EqualTo(1));Assert.That(craneBatches,Is.EqualTo(1));
            Assert.That(reservoirBatches,Is.EqualTo(2));
            Assert.That(reservoirRanks.Count,Is.EqualTo(2));
            Assert.That(shortBatches,Is.EqualTo(2),"Distinct short texture uses two bounded bands.");
            Assert.That(boilerHardware,Is.InRange(1,600),"Fitted service hardware has a small shared geometry budget, with no extra material batch.");
            Assert.That(otherBatches,Is.LessThanOrEqualTo(22),"The casting bench adds one card; its supports reuse the existing masonry batches.");
            Assert.That(arena.Region.GetComponentsInChildren<MeshRenderer>().Length,Is.LessThanOrEqualTo(32),
                "One casting bench card adds one renderer to the measured 31-renderer region.");


            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(renderer.name.StartsWith("Distant foundry works"))
                    Assert.That(renderer.sortingOrder,Is.InRange(-20,-19),"Distant works occupy explicit ranks below the island.");
                else
                    Assert.That(renderer.sortingOrder,Is.InRange(-18,-1),"Existing scenery retains its ranks above distant works and below combatants.");

            foreach(var post in FoundryRegionView.BridgeHeadPosts)
                for(int u=0;u<=14;u++)for(int v=0;v<=14;v++)
                {
                    Cryptforge.Art.FoundryMap.ToFloor(post.x-.7f+u*.1f,post.y-.7f+v*.1f,out float x,out float y);
                    Assert.That(Cryptforge.Art.FoundryMap.SurfaceContains(x,y,0),Is.False,"Guardian plinths stay outside the complete physical deck.");
                }
            foreach(var posts in new[]{FoundryRegionView.BridgeFootPosts,FoundryRegionView.BridgeLanterns})
                foreach(var post in posts)
                    for(int u=0;u<=8;u++)for(int v=0;v<=8;v++)
                    {
                        Cryptforge.Art.FoundryMap.ToFloor(post.x-.32f+u*.08f,post.y-.32f+v*.08f,out float x,out float y);
                        Assert.That(Cryptforge.Art.FoundryMap.SurfaceContains(x,y,0),Is.False,"Bridge lantern and end-pier feet remain outside the wider physical deck.");
                    }
            var visibility=arena.Region.GetComponent<FoundryVisibilityView>();
            // The gate's fire baskets hang on posts the balustrade places for itself: a rail change must not leave one
            // floating over a span or bare void.
            var regionView=arena.Region.GetComponent<FoundryRegionView>();
            var boilerSources=new System.Collections.Generic.List<Material>();
            foreach(var field in new[]{"_bakedBoilerMaterial","_bakedShortBoilerMaterial"})
            {
                var source=typeof(FoundryRegionView).GetField(field,
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(regionView) as Material;
                Assert.That(source,Is.Not.Null);boilerSources.Add(source);
            }
            var hoistSource=typeof(FoundryRegionView).GetField("_stockHoistMaterial",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(regionView) as Material;
            Assert.That(hoistSource,Is.Not.Null);
            var workshopSources=new System.Collections.Generic.List<Material>();
            foreach(var field in new[]{"_stockStationMaterial","_craneMaterial"})
            {
                var source=typeof(FoundryRegionView).GetField(field,
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(regionView) as Material;
                Assert.That(source,Is.Not.Null);workshopSources.Add(source);
            }
            foreach(float v in FoundryRegionView.GateBrazierPosts)
            {
                bool held=false;
                foreach(var post in regionView.RailPostBoxes)
                    if(FoundryRegionView.GateBrazierU>=post.x&&FoundryRegionView.GateBrazierU<=post.z&&v>=post.y&&v<=post.w)held=true;
                Assert.That(held,Is.True,"A balustrade post holds the gate brazier at v "+v+".");
            }
            Assert.That(visibility,Is.Not.Null);
            var ownedMaterials=new System.Collections.Generic.HashSet<Material>();
            int floorDecals=0;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                // Flush floor decals are the walking surface: the cutaway never takes them, and they sort at the paving's
                // rank, drawn after it, so raised structures at -14 (bridge coping, block sides) paint over them.
                if(renderer.name==FoundryRegionView.FloorDecalLayer)
                {
                    floorDecals++;
                    Assert.That(renderer.sharedMaterial.shader.name,Is.Not.EqualTo("Cryptforge/Foundry Cutaway"),
                        "Brass inlays must not vanish around the hero's feet.");
                    Assert.That(renderer.sortingOrder,Is.EqualTo(-15),"Decals sort at the paving's rank, under every raised structure.");
                    foreach(var other in arena.Region.GetComponentsInChildren<MeshRenderer>())
                        if(other!=renderer&&other.sortingOrder==renderer.sortingOrder)
                            Assert.That(renderer.bounds.center.y,Is.LessThan(other.bounds.center.y),"Decals draw after the paving under them.");
                    continue;
                }
                if(renderer.sortingOrder < -14) continue;
                var variant=renderer.sharedMaterial;
                Assert.That(variant.shader.name,Is.EqualTo("Cryptforge/Foundry Cutaway"));
                ownedMaterials.Add(variant);
            }
            Assert.That(floorDecals,Is.EqualTo(1),"One batch of flush floor decals.");
            yield return null; // Start and LateUpdate bind the actual hero, including while paused.
            foreach(var variant in ownedMaterials)
            {
                Vector4 focus=variant.GetVector("_HeroFocus");
                Assert.That(focus.w,Is.EqualTo(1));
                Assert.That(focus.x,Is.EqualTo(movement.transform.position.x).Within(.001f));
                Assert.That(focus.y,Is.EqualTo(movement.transform.position.y+.35f).Within(.001f));
            }
            // Structures carry their ground-contact depth; while a camera renders, actor sprites follow their feet.
            // Roots never move, and gameplay reading positions between renders sees the original z.
            float heroDepth=FoundryDepth.ActorZ(arena.Region.transform.position,movement.transform.position.y);
            foreach(var variant in ownedMaterials)Assert.That(variant.GetVector("_HeroFocus").z,Is.EqualTo(heroDepth).Within(.001f));
            Vector3 heroRoot=movement.transform.position;
            visibility.ApplyActorDepth();
            Assert.That(movement.transform.childCount,Is.GreaterThan(0));
            foreach(Transform child in movement.transform)Assert.That(child.position.z,Is.EqualTo(heroDepth).Within(.001f));
            Assert.That(movement.transform.position,Is.EqualTo(heroRoot),"Depth must never move the gameplay root.");
            visibility.RestoreActorDepth();
            foreach(Transform child in movement.transform)Assert.That(child.localPosition.z,Is.Zero,"Between renders actors keep their authored z.");
            visibility.ApplyActorDepth();
            bool writesDepth=false;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.sharedMaterial.shader.name!="Cryptforge/Foundry Cutaway")continue;
                var structure=renderer.GetComponent<MeshFilter>().sharedMesh;
                var flags=new System.Collections.Generic.List<Vector2>();structure.GetUVs(1,flags);
                Assert.That(flags.Count,Is.EqualTo(structure.vertexCount),"Every structure vertex states whether it writes depth.");
                var points=structure.vertices;
                for(int i=0;i<points.Length;i++)if(flags[i].x>0){writesDepth=true;Assert.That(points[i].z,Is.GreaterThan(0f));}
            }
            Assert.That(writesDepth,Is.True,"Tall structures must occlude actors behind them.");
            // Floor light, decal light and tints multiply vertex colours above 1; the sprite shaders carry them in a
            // fixed4 interpolator, whose guaranteed range on every mobile precision is +/-2 (after the material tint).
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                var lit=renderer.GetComponent<MeshFilter>().sharedMesh;
                var colours=new System.Collections.Generic.List<Color>();lit.GetColors(colours);
                Color tint=renderer.sharedMaterial.HasProperty("_Color")?renderer.sharedMaterial.color:Color.white;
                float peak=0,low=0;
                foreach(var c in colours)
                {
                    peak=Mathf.Max(peak,Mathf.Max(c.r*tint.r,Mathf.Max(c.g*tint.g,c.b*tint.b)));
                    low=Mathf.Min(low,Mathf.Min(c.r,Mathf.Min(c.g,c.b)));
                }
                Assert.That(peak,Is.LessThan(2f),renderer.name+": vertex light beyond the shader's guaranteed colour range.");
                Assert.That(low,Is.GreaterThanOrEqualTo(0f),renderer.name+": negative vertex colour.");
            }
            visibility.enabled=false;
            foreach(var variant in ownedMaterials)Assert.That(variant.GetVector("_HeroFocus").w,Is.Zero);
            foreach(Transform child in movement.transform)Assert.That(child.localPosition.z,Is.Zero,"Without the view actors return to the flat layer and draw over every structure.");
            visibility.enabled=true;
            var ring=Object.FindFirstObjectByType<AbilityRingView>().Ring;
            Assert.That(ring.sortingOrder,Is.EqualTo(arena.GroundEffectSortingOrder));
            Assert.That(ring.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder,Is.EqualTo(ring.sortingOrder));
            Assert.That(Object.FindFirstObjectByType<AbilityController>().TryUse(),Is.True);
            SpriteRenderer groundPulse=null;
            foreach(var effect in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
            {
                if(!effect.enabled || effect.sprite!=Cryptforge.Art.AbilityRingArt.Get())continue;
                groundPulse=effect;
                Assert.That(effect.sortingOrder,Is.EqualTo(arena.GroundEffectSortingOrder));
            }
            Assert.That(groundPulse,Is.Not.Null,"The real ability event must produce a grounded pulse.");
            // A floor pulse already sorts under the structures: while a camera renders it keeps the flat layer.
            visibility.RestoreActorDepth();
            float pulseZ=groundPulse.transform.position.z;
            visibility.ApplyActorDepth();
            Assert.That(groundPulse.transform.position.z,Is.EqualTo(pulseZ),"Ground effects are left out of the foundry's depth.");
            visibility.RestoreActorDepth();

            var atmosphere=Object.FindFirstObjectByType<FoundryAtmosphereView>();
            Assert.That(atmosphere,Is.Not.Null);
            var atmosphereMesh=atmosphere.GetComponent<MeshFilter>().sharedMesh;
            var atmosphereRenderer=atmosphere.GetComponent<MeshRenderer>();
            Assert.That(atmosphereRenderer.sharedMaterial,Is.SameAs(arena.Material));
            Assert.That(atmosphereRenderer.sortingOrder,Is.LessThan(1),"Atmosphere cannot cover combatant silhouettes.");
            var initialAtmosphere=atmosphereMesh.vertices;
            Time.timeScale=1;
            yield return new WaitForSeconds(.12f);
            Assert.That(atmosphereMesh.vertices,Is.Not.EqualTo(initialAtmosphere),"Active atmosphere should move.");
            // The atmosphere is a sibling of the region, so the region's colour check above never covered it.
            var tones=new System.Collections.Generic.List<Color>();atmosphereMesh.GetColors(tones);
            foreach(var c in tones)
            {
                Assert.That(Mathf.Max(c.r,Mathf.Max(c.g,c.b)),Is.LessThan(2f),"Atmosphere colour beyond the shader's range.");
                Assert.That(Mathf.Min(c.r,Mathf.Min(c.g,c.b)),Is.GreaterThanOrEqualTo(0f),"Negative atmosphere colour.");
            }
            // The pit's sparks and haze are depth-tested against the collar (behind the near pieces and the domes, in front
            // of the far collar), and are born only inside the collar's opening, never over walkable court.
            int pitVertices=0;
            foreach(var p in atmosphereMesh.vertices)
                if(p.z!=0){pitVertices++;Assert.That(p.z,Is.InRange(FoundryDepth.Z(-.6f),FoundryDepth.Z(.09f)));}
            Assert.That(pitVertices,Is.GreaterThan(0),"The furnace pit has sparks and haze.");
            for(int spark=0;spark<FoundryAtmosphereView.SparkCount;spark++)
                for(int life=0;life<200;life++)
                {
                    Vector2 at=FoundryAtmosphereView.SparkSpawn(spark,life);
                    // Distance to the rounded-square opening, not its inscribed circle:
                    // the enlarged pit has long straight sides around the corner arcs.
                    Vector2 corner=new Vector2(Mathf.Max(0,Mathf.Abs(at.x+10)-2.49f),Mathf.Max(0,Mathf.Abs(at.y-8)-2.49f));
                    Assert.That(corner.magnitude,Is.LessThan(2.96f),"Sparks are born inside the collar's opening.");
                    Assert.That(Cryptforge.Art.FoundryMap.Cell(Mathf.FloorToInt(at.x),Mathf.FloorToInt(at.y)),Is.False,"Never over walkable court.");
                }


            Assert.That(setup.Encounters.AliveEnemyCount,Is.GreaterThan(0));
            for(int i=0;i<setup.Encounters.WaveEnemyCount;i++)
            {
                Vector3 p=setup.Encounters.WaveEnemyAt(i).transform.position;
                Assert.That(arena.Geometry.IsOnPlatform(p.x,p.y*2,.6f),Is.True);
            }
            // Combat effects above the floor stand at the depth of their ground contact while a camera renders, as the actors
            // do: a hit spark takes the struck enemy's feet, not its own height on the body, so the structure hiding the
            // enemy hides its hit too. Numbers and floor pulses keep the flat layer; between renders, and without the view,
            // every effect is where its animation put it.
            var effects=Object.FindFirstObjectByType<CombatEffectsView>();
            Health struck=null;
            for(int i=0;i<setup.Encounters.WaveEnemyCount&&struck==null;i++)
                if(setup.Encounters.WaveEnemyAt(i).IsAlive)struck=setup.Encounters.WaveEnemyAt(i);
            Assert.That(struck,Is.Not.Null);
            visibility.RestoreActorDepth();
            struck.ApplyDamage(new DamageContext(.001f)); // A spark and no number: the amount rounds to nothing.
            var flatZ=new System.Collections.Generic.Dictionary<Transform,float>();
            foreach(var shown in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if(shown.enabled)flatZ.Add(shown.transform,shown.transform.position.z);
            visibility.ApplyActorDepth();
            Vector3 regionOrigin=arena.Region.transform.position;
            var aboveFloor=new System.Collections.Generic.HashSet<Transform>();
            int onStruckFeet=0;
            for(int i=0;i<effects.EffectSlotCount;i++)
            {
                Transform raised=effects.AboveFloorEffect(i,out float groundY);
                if(raised==null)continue;
                aboveFloor.Add(raised);
                Assert.That(flatZ.ContainsKey(raised),Is.True,"Only effects on show are placed in depth.");
                Assert.That(raised.position.z,Is.EqualTo(FoundryDepth.ActorZ(regionOrigin,groundY)).Within(.001f),
                    "An effect above the floor stands at the depth of its ground contact.");
                if(groundY==struck.transform.position.y)onStruckFeet++;
            }
            Assert.That(onStruckFeet,Is.GreaterThan(0),"The hit spark stands on the struck enemy's feet.");
            int placed=visibility.PlacedCount;
            visibility.ApplyActorDepth();
            Assert.That(visibility.PlacedCount,Is.EqualTo(placed),"Every apply asks the pool afresh: no effect is remembered twice.");
            foreach(var shown in flatZ)
                if(!aboveFloor.Contains(shown.Key))
                    Assert.That(shown.Key.position.z,Is.EqualTo(shown.Value),"Numbers and floor pulses keep the flat layer, over every structure.");
            visibility.RestoreActorDepth();
            foreach(var shown in flatZ)
                Assert.That(shown.Key.position.z,Is.EqualTo(shown.Value).Within(.0001f),"Between renders effects keep the z their animation gave them.");
            visibility.ApplyActorDepth();
            visibility.enabled=false;
            foreach(var shown in flatZ)
                Assert.That(shown.Key.position.z,Is.EqualTo(shown.Value).Within(.0001f),"Without the view effects return to the flat layer.");
            visibility.enabled=true;
            // Enemies hidden by a structure keep a silhouette: every live enemy body shares the scene's x-ray
            // material while the view runs, and gets its own material back when it stops.
            visibility.ApplyActorDepth();
            Material xray=null;
            for(int i=0;i<setup.Encounters.WaveEnemyCount;i++)
            {
                var body=setup.Encounters.WaveEnemyAt(i).GetComponent<EnemyLookView>().Body;
                Assert.That(body.sharedMaterial.shader.name,Is.EqualTo(FoundryVisibilityView.XrayShaderName));
                if(xray==null)xray=body.sharedMaterial;
                Assert.That(body.sharedMaterial,Is.SameAs(xray),"One shared silhouette material, not one per enemy.");
                // The health bar is a readout: it keeps its authored depth while the body goes to its feet depth.
                var enemy=setup.Encounters.WaveEnemyAt(i).GetComponent<EnemyLookView>();
                Assert.That(enemy.Bar,Is.Not.Null);
                Assert.That(enemy.Bar.localPosition.z,Is.Zero,"Health bars stay on top of scenery, like the damage numbers.");
            }
            Assert.That(xray.passCount,Is.EqualTo(2));
            visibility.enabled=false;
            for(int i=0;i<setup.Encounters.WaveEnemyCount;i++)
                Assert.That(setup.Encounters.WaveEnemyAt(i).GetComponent<EnemyLookView>().Body.sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.enabled=true;
            // Drops behind a structure keep a silhouette too, in loot gold rather than the enemies' ember: only the reward
            // body, one shared material, kept by a pooled drop when it is shown again, and handed back when the view stops.
            var drops=setup.GetComponent<GroundDropView>();
            setup.Drops.Add(1.5f,1f,0,1); // Gold only: if the hero reaches it later it cannot bring a level choice.
            Assert.That(drops.ShownCount,Is.GreaterThan(0));
            for(int i=0;i<drops.ShownCount;i++)Assert.That(drops.ShownBody(i).sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.ApplyActorDepth();
            Material loot=drops.ShownBody(0).sharedMaterial;
            Assert.That(loot.shader.name,Is.EqualTo(FoundryVisibilityView.XrayShaderName));
            Assert.That(loot,Is.Not.SameAs(xray),"Loot must not read as a threat.");
            Assert.That(loot.GetColor("_XrayColor"),Is.EqualTo(FoundryVisibilityView.LootXrayColor));
            for(int i=0;i<drops.ShownCount;i++)Assert.That(drops.ShownBody(i).sharedMaterial,Is.SameAs(loot),"One shared loot material, not one per drop.");
            foreach(var sprite in drops.ShownBody(0).transform.parent.GetComponentsInChildren<SpriteRenderer>())
                if(sprite!=drops.ShownBody(0))Assert.That(sprite.sharedMaterial,Is.Not.SameAs(loot),"Only the reward body, never its shadow.");
            foreach(var sprite in movement.GetComponentsInChildren<SpriteRenderer>())
                Assert.That(sprite.sharedMaterial==xray||sprite.sharedMaterial==loot,Is.False,"The hero never gets a silhouette.");
            int registered=visibility.XrayedCount;
            drops.enabled=false;drops.enabled=true; // Parks every drop in the pool and shows it again from there.
            visibility.ApplyActorDepth();
            Assert.That(visibility.XrayedCount,Is.EqualTo(registered),"A re-used pooled drop is not registered twice.");
            for(int i=0;i<drops.ShownCount;i++)Assert.That(drops.ShownBody(i).sharedMaterial,Is.SameAs(loot));
            visibility.enabled=false;
            for(int i=0;i<drops.ShownCount;i++)Assert.That(drops.ShownBody(i).sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.enabled=true;
            // The chest's root is its sprite, with no children: while a camera renders the root itself stands at the depth of
            // its feet, so a boiler in front hides it, and a hidden chest shows in the drops' loot gold. Between renders it
            // stands where gameplay put it, and without the view it has its own material back.
            var chests=Object.FindFirstObjectByType<ChestSpawner>();
            Assert.That(chests.HasChest,Is.True,"The foundry's first room has a chest.");
            var chest=chests.Body;
            Assert.That(chest.transform.childCount,Is.Zero);
            Assert.That(GroundedSorting.Actors,Does.Contain(chest.transform));
            Assert.That(chest.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder,Is.EqualTo(GroundedSorting.ActorOrder));
            visibility.RestoreActorDepth();
            Vector3 chestSpot=chest.transform.position;
            Assert.That(chestSpot.x,Is.EqualTo(chests.ChestFloorX).Within(.0001f));
            Assert.That(chestSpot.y,Is.EqualTo(ArenaFloor.WorldY(chests.ChestFloorY)).Within(.0001f));
            Assert.That(chestSpot.z,Is.Zero);
            Assert.That(chest.sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.ApplyActorDepth();
            float chestDepth=FoundryDepth.ActorZ(arena.Region.transform.position,chestSpot.y);
            Assert.That(chest.transform.position.z,Is.EqualTo(chestDepth).Within(.001f),"Structures in front must hide the chest.");
            Assert.That(chest.transform.position.x,Is.EqualTo(chestSpot.x).Within(.0001f),"Depth moves the chest in z only.");
            Assert.That(chest.transform.position.y,Is.EqualTo(chestSpot.y).Within(.0001f),"Depth moves the chest in z only.");
            Assert.That(chest.sharedMaterial,Is.SameAs(loot),"A hidden chest reads as loot, sharing the drops' material.");
            int withChest=visibility.XrayedCount;
            visibility.ApplyActorDepth();
            Assert.That(visibility.XrayedCount,Is.EqualTo(withChest),"The chest is registered once.");
            visibility.RestoreActorDepth();
            Assert.That(chest.transform.localPosition.z,Is.Zero,"Between renders the chest keeps its authored z.");
            Assert.That(chest.transform.position.x,Is.EqualTo(chestSpot.x).Within(.0001f));
            Assert.That(chest.transform.position.y,Is.EqualTo(chestSpot.y).Within(.0001f));
            Assert.That(chest.sharedMaterial,Is.SameAs(loot),"The chest keeps the loot material between renders, like the drops.");
            visibility.ApplyActorDepth();
            visibility.enabled=false;
            Assert.That(chest.transform.localPosition.z,Is.Zero,"Without the view the chest returns to the flat layer.");
            Assert.That(chest.sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.enabled=true;
            // Select the borrowed floor explicitly: the first child may instead be
            // a scene-owned distant atmosphere layer, covered by its lifecycle test.
            MeshRenderer floorRenderer=null;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial==arena.PlatformMaterials.FoundryPaving){floorRenderer=renderer;break;}
            Assert.That(floorRenderer,Is.Not.Null);
            var mesh=floorRenderer.GetComponent<MeshFilter>().sharedMesh;
            var material=floorRenderer.sharedMaterial;
            Assert.That(setup.Pause.TryPause(),Is.True);
            yield return null; // Finish the frame whose delta was calculated before the pause.
            var pausedAtmosphere=atmosphereMesh.vertices;
            Vector3 before=movement.transform.position;movement.Hold(Vector2.right);yield return null;
            Assert.That(movement.transform.position,Is.EqualTo(before),"Paused map must not move.");
            Assert.That(atmosphereMesh.vertices,Is.EqualTo(pausedAtmosphere),"Atmosphere must freeze with combat.");
            File.Delete(DevelopmentStart.RegionPreviewPath);
            Assert.That(arena.IsFoundryRegion,Is.True,"Mode is frozen for a loaded scene.");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene=SceneManager.GetActiveScene();yield return null;
            Assert.That(Object.FindFirstObjectByType<ArenaView>().Region,Is.Null);
            Assert.That(Object.FindFirstObjectByType<FoundryVisibilityView>(),Is.Null,"Ordinary halls place neither actors nor effects in depth.");
            Assert.That(mesh==null,Is.True);
            Assert.That(material!=null,Is.True,"Reload preserves the explicitly selected borrowed floor material.");
            Assert.That(keeperMesh==null,Is.True,"Reload releases the guardian mesh.");
            Assert.That(keeperVariant==null,Is.True,"Reload releases the guardian cutaway material.");
            foreach(var boilerMesh in boilerMeshes)Assert.That(boilerMesh==null,Is.True,"Reload releases both boiler cards and their fitted geometry.");
            foreach(var source in boilerSources)Assert.That(source==null,Is.True,"Reload releases the owned boiler source materials.");
            Assert.That(baked!=null&&shortBake!=null,Is.True,"Reload preserves borrowed boiler textures.");
            Assert.That(hoistMesh==null&&hoistVariant==null&&hoistSource==null,Is.True,
                "Reload releases the hoist mesh, cutaway variant and owned source material.");
            Assert.That(stockHoist!=null,Is.True,"Reload preserves the shared hoist texture resource.");
            foreach(var workshopMesh in workshopMeshes)Assert.That(workshopMesh==null,Is.True);
            foreach(var variant in workshopVariants)Assert.That(variant==null,Is.True);
            foreach(var source in workshopSources)Assert.That(source==null,Is.True);
            Assert.That(stockStation!=null&&craneTexture!=null,Is.True,"Reload retains shared workshop textures.");
            Assert.That(keeper!=null&&keeperTexture!=null,Is.True,"Shared keeper assets survive reload.");
            Assert.That(gate!=null,Is.True,"Reload must not destroy shared facade assets.");
            foreach(var variant in ownedMaterials)Assert.That(variant==null,Is.True,"Scene cutaway variants must be released on reload.");
            Assert.That(xray==null,Is.True,"The enemy silhouette material must be released on reload.");
            Assert.That(loot==null,Is.True,"The loot silhouette material must be released on reload.");
            Assert.That(atmosphereMesh==null,Is.True,"Reload must release dynamic atmosphere geometry.");
            Assert.That(Object.FindFirstObjectByType<FoundryAtmosphereView>(),Is.Null,"Ordinary hall does not acquire foundry atmosphere.");
            foreach(var shared in surfaces)Assert.That(shared!=null,Is.True,"Reload must preserve shared dressing materials.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
