using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryCourtCompassTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        private static Type BuilderType=>typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
        private static object NewBuilder()=>Activator.CreateInstance(BuilderType,true);
        private static List<T> Data<T>(object builder,string field)=>(List<T>)BuilderType.GetField(field,Members).GetValue(builder);
        private static void Invoke(string method,params object[] arguments)=>
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,arguments);
        private static readonly Vector2 CourtCentre=new Vector2(-3,-5);

        [Test]
        public void CompassRemainsFlushBoundedAndReceivesOrdinaryFloorLighting()
        {
            object mesh=NewBuilder();Invoke("BuildCourtSealGeometry",mesh);
            CheckFlushMesh(mesh,CourtCentre,4.845f,600);
            var uv=Data<Vector2>(mesh,"U");
            Vector4 brass=(Vector4)typeof(FoundryRegionView).GetField("CopingRail",Static).GetValue(null);
            bool metal=false,stone=false;
            foreach(Vector2 point in uv)
            {
                bool inBrass=point.x>=brass.x-.00001f&&point.x<=brass.z+.00001f&&
                    point.y>=brass.y-.00001f&&point.y<=brass.w+.00001f;
                metal|=inBrass;stone|=!inBrass;
            }
            Assert.That(metal&&stone,Is.True,"Aged metal and its stone engraving remain separate material samples.");
            Invoke("LightFloorDecals",mesh);
            foreach(Color color in Data<Color>(mesh,"C"))
            {
                Assert.That(color.a,Is.EqualTo(1));
                Assert.That(color.r,Is.InRange(0f,1.90001f));Assert.That(color.g,Is.InRange(0f,1.90001f));
                Assert.That(color.b,Is.InRange(0f,1.90001f));
            }
        }

        [Test]
        public void FittedInnerFlagsPreserveCompleteMedallionCoverageAndWorldGrain()
        {
            var field=new Vector2[48];
            for(int i=0;i<field.Length;i++)field[i]=CourtCentre+new Vector2(Mathf.Cos(i*Mathf.PI/24),Mathf.Sin(i*Mathf.PI/24))*3.75f;
            object mesh=NewBuilder();Invoke("BuildCourtMedallionField",mesh,field);
            CheckFlushMesh(mesh,CourtCentre,3.75f,3200);
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");var uv=Data<Vector2>(mesh,"U");
            float span=(float)typeof(FoundryRegionView).GetField("PavingGrainSpan",Static|BindingFlags.Public).GetRawConstantValue();
            for(int i=0;i<vertices.Count;i++)
            {
                Vector2 design=ToDesign(vertices[i]);
                Vector2 expected=new Vector2(1+2*FoundryMap.Axis*design.x/span,1+2*FoundryMap.Axis*design.y/span);
                Assert.That(Vector2.Distance(uv[i],expected),Is.LessThan(.00001f),"The fitted flags retain continuous world-space mineral grain.");
            }
            // Sample the relocated inscribed polygon, including the eight fan
            // flags and their fitted joints. Each point has one stone owner.
            for(int x=-14;x<=14;x++)for(int y=-14;y<=14;y++)
            {
                Vector2 local=new Vector2(x*.25f,y*.25f);
                if(local.magnitude>3.70f)continue;
                Vector2 design=CourtCentre+local;
                Vector2 point=Project(design);bool covered=false;
                for(int t=0;t<triangles.Count&&!covered;t+=3)
                    covered=InTriangle(point,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]]);
                Assert.That(covered,Is.True,"No fitted flag or joint may cut a hole in the original medallion bed.");
            }
        }

        [Test]
        public void RelocatedMedallionHasOneCompleteStoneBedAndNoFormerOriginGhost()
        {
            object mesh=NewBuilder();Invoke("BuildCourtStoneRing",mesh);
            CheckFlushMesh(mesh,CourtCentre,5.05f,6500);
            CheckStoneGrainAndColour(mesh);
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");
            Assert.That(Covered(Vector2.zero,vertices,triangles),Is.False,
                "Moving the medallion must remove its old centre geometry rather than append a second circle.");
            for(int x=-19;x<=19;x++)for(int y=-19;y<=19;y++)
            {
                Vector2 local=new Vector2(x*.25f,y*.25f);
                if(local.magnitude>5f)continue;
                Assert.That(Covered(Project(CourtCentre+local),vertices,triangles),Is.True,
                    "The relocated field, radial bed and fitted ends retain continuous paving coverage.");
            }
            foreach(Vector3 p in vertices)
            {
                Vector2 design=ToDesign(p);
                Assert.That(design.y,Is.LessThanOrEqualTo(.05002f),
                    "The whole architectural setting stays below the fixed pit and its collar.");
            }
            for(int u=-4;u<4;u++)for(int v=-4;v<4;v++)
            {
                int zone=(int)typeof(FoundryRegionView).GetMethod("PavingZone",Static).Invoke(null,new object[]{new Vector2Int(u,v)});
                Assert.That(zone,Is.Not.EqualTo(1),"Do not leave a regular unit-flag square at the retired medallion origin.");
            }
        }

        [Test]
        public void ProcessionalStoneFitsTheWalkingUnionAndStopsOutsideTheSingleMedallion()
        {
            object mesh=NewBuilder();Invoke("BuildCourtProcessionalStone",mesh);
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");
            Assert.That(vertices.Count,Is.InRange(1,5000),"Two complete fitted paths must remain a bounded floor replacement.");
            CheckStoneGrainAndColour(mesh);
            foreach(Vector3 vertex in vertices)
            {
                Vector2 design=ToDesign(vertex);
                Assert.That(Vector2.Distance(design,CourtCentre),Is.GreaterThan(5.16f),
                    "Processional courses must not duplicate or cut through the medallion ring.");
                Assert.That(vertex.z,Is.Zero);
            }
            // A triangle can have valid corners and still cross a concave join.
            // Sample every edge, not just its endpoints, at the bridge transition.
            for(int t=0;t<triangles.Count;t+=3)
                for(int edge=0;edge<3;edge++)
                {
                    Vector2 a=ToDesign(vertices[triangles[t+edge]]),b=ToDesign(vertices[triangles[t+(edge+1)%3]]);
                    int samples=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.15f));
                    for(int k=0;k<=samples;k++)
                    {
                        Vector2 p=Vector2.Lerp(a,b,k/(float)samples);
                        FoundryMap.ToFloor(p.x,p.y,out float fx,out float fy);
                        Assert.That(FoundryMap.SurfaceContains(fx,fy,0),Is.True,
                            "No architectural setting may paint across the void or the furnace hole.");
                    }
                }
            Assert.That(Covered(Project(CourtCentre),vertices,triangles),Is.False);
            // Inspect both complete paired settings, including bed joints. These
            // positive samples would fail if any course were dropped or truncated.
            foreach(Vector2 end in new[]{new Vector2(-17.5f,-17.5f),new Vector2(15.9f,5)})
            {
                Vector2 direction=(end-CourtCentre).normalized,side=new Vector2(-direction.y,direction.x);
                float distance=Vector2.Distance(end,CourtCentre);
                for(int k=0;k<=12;k++)foreach(float lateral in new[]{-2.05f,-1.65f,0f,1.65f,2.05f})
                {
                    Vector2 point=CourtCentre+direction*Mathf.Lerp(6,distance-.1f,k/12f)+side*lateral;
                    Assert.That(Covered(Project(point),vertices,triangles),Is.True,
                        "Every course needs a continuous full-width stone bed, including its joints and fitted ends.");
                }
            }
            Assert.That(Covered(Project(new Vector2(-10,-11)),vertices,triangles),Is.True,
                "The complete approach replaces its former grid middle as well as the narrow shoulder courses.");
        }

        [Test]
        public void FittedCourtPartitionsTheExactFootprintsWithoutASecondJointBed()
        {
            object circle=NewBuilder(),paths=NewBuilder();
            Invoke("BuildCourtStoneRing",circle);Invoke("BuildCourtProcessionalStone",paths);
            double circleArea=24*5.05*5.05*Math.Sin(Math.PI/24);
            double pathArea=0;
            foreach(Vector2 end in new[]{new Vector2(-17.5f,-17.5f),new Vector2(15.9f,5)})
                pathArea+=4.18*(Vector2.Distance(end,CourtCentre)-5.21);
            Assert.That(DesignArea(circle),Is.EqualTo(circleArea).Within(.002),
                "Joint strips, radial shoulders and faces must partition the 48-gon, not cover a complete bed twice.");
            Assert.That(DesignArea(paths),Is.EqualTo(pathArea).Within(.003),
                "The whole approach width is fitted once, including mortar, with no layered middle or missing shoulders.");
            // A noncardinal phase also keeps i=0 off the half-unit light grid.
            // Golden-angle spacing alone starts on the v=-5 shared cell edge.
            for(int i=0;i<111;i++)
            {
                float angle=.173f+i*2.399963f,radius=.19f+4.77f*(i%31)/31;
                Vector2 point=Project(CourtCentre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
                AssertOneOwner(point,circle,"Every sampled medallion point has one opaque floor owner.");
            }
        }

        [Test]
        public void CourtReplacementCutsEveryCrossingBaseLayerAndPreservesInterpolatedSourceData()
        {
            object mesh=NewBuilder();
            // Two large original layers deliberately cross every new footprint.
            // A flag-top-only removal or centroid-only rejection fails this test.
            for(int layer=0;layer<2;layer++)
            {
                int start=Data<Vector3>(mesh,"V").Count;
                foreach(Vector2 point in new[]{new Vector2(-36,-36),new Vector2(19,-36),new Vector2(19,9),new Vector2(-36,9)})
                {
                    Data<Vector3>(mesh,"V").Add(Project(point));
                    Data<Vector2>(mesh,"U").Add(new Vector2(.3f+point.x*.013f,.6f+point.y*.009f));
                    Data<Color>(mesh,"C").Add(new Color(.5f+point.x*.006f,.6f+point.y*.007f,.2f+layer*.3f,1));
                }
                Data<int>(mesh,"T").AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            Invoke("ReplaceCourtPavingBase",mesh);
            var vertices=Data<Vector3>(mesh,"V");var uv=Data<Vector2>(mesh,"U");var colours=Data<Color>(mesh,"C");
            Assert.That(vertices.Count,Is.LessThan(1600),"Boundary clipping is bounded even for triangles spanning the whole court.");
            for(int i=0;i<vertices.Count;i++)
            {
                Vector2 p=ToDesign(vertices[i]);
                Assert.That(Vector2.Distance(uv[i],new Vector2(.3f+p.x*.013f,.6f+p.y*.009f)),Is.LessThan(.00001f));
                Assert.That(colours[i].r,Is.EqualTo(.5f+p.x*.006f).Within(.00001f));
                Assert.That(colours[i].g,Is.EqualTo(.6f+p.y*.007f).Within(.00001f));
                Assert.That(Mathf.Min(Mathf.Abs(colours[i].b-.2f),Mathf.Abs(colours[i].b-.5f)),Is.LessThan(.00001f));
                Assert.That(colours[i].a,Is.EqualTo(1));
            }
            Assert.That(Owners(Project(new Vector2(16.23f,-15.19f)),mesh),Is.EqualTo(2),"Unchanged outside stone retains both original source layers.");
            for(int i=0;i<55;i++)
            {
                float angle=i*2.399963f,radius=.27f+4.69f*(i%23)/23;
                Assert.That(Owners(Project(CourtCentre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius),mesh),Is.Zero,
                    "No old axis-grid bed, bevel or top may survive beneath the fitted medallion.");
            }
            foreach(Vector2 end in new[]{new Vector2(-17.5f,-17.5f),new Vector2(15.9f,5)})
            {
                Vector2 direction=(end-CourtCentre).normalized,side=new Vector2(-direction.y,direction.x);
                for(int k=0;k<13;k++)foreach(float lateral in new[]{-2.063f,-.719f,.153f,2.063f})
                {
                    Vector2 p=CourtCentre+direction*Mathf.Lerp(5.23f,Vector2.Distance(end,CourtCentre)-.021f,k/12f)+side*lateral;
                    Assert.That(Owners(Project(p),mesh),Is.Zero,"The entire approach, including its centre and fitted ends, replaces the old paving.");
                }
            }
            foreach(Vector2 p in BridgeContinuationSamples())
                Assert.That(Owners(Project(p),mesh),Is.Zero,"No previous bridge bed, bevel or flag survives beneath the continuation.");
            object replacement=NewBuilder();Invoke("BuildCourtStoneRing",replacement);Invoke("BuildCourtProcessionalStone",replacement);
            Invoke("BuildCourtBridgeStone",replacement);
            Assert.That(DesignArea(mesh)+2*DesignArea(replacement),Is.EqualTo(2*55*45).Within(.015),
                "Exact convex subtraction preserves all outside geometry and removes only the four agreed domains.");
        }

        [Test]
        public void ProductionCourtAndBridgeRemoveTheOldGridBeforeTheirReplacementIsFilled()
        {
            var go=new GameObject("Court paving ownership test");
            try
            {
                var view=go.AddComponent<FoundryRegionView>();
                var cells=(HashSet<Vector2Int>)typeof(FoundryRegionView).GetField("_cells",Members).GetValue(view);
                for(int r=0;r<FoundryMap.RectangleCount;r++)
                    for(int x=FoundryMap.Bound(r,0);x<FoundryMap.Bound(r,2);x++)
                        for(int y=FoundryMap.Bound(r,1);y<FoundryMap.Bound(r,3);y++)cells.Add(new Vector2Int(x,y));
                object mesh=NewBuilder();
                MethodInfo build=typeof(FoundryRegionView).GetMethod("BuildPavingCell",Members);
                foreach(Vector2Int cell in cells)build.Invoke(view,new object[]{mesh,cell});
                Invoke("ClipFloorCorners",mesh);Invoke("BuildBridgeDeck",mesh);
                int originalVertices=Data<Vector3>(mesh,"V").Count;
                Vector2 outside=Project(new Vector2(8.213f,-13.149f));
                int outsideOwners=Owners(outside,mesh);
                Assert.That(outsideOwners,Is.GreaterThan(0));
                Invoke("ReplaceCourtPavingBase",mesh);
                int clippedVertices=Data<Vector3>(mesh,"V").Count;
                Assert.That(Owners(outside,mesh),Is.EqualTo(outsideOwners),"Court carving must preserve ordinary paving outside the agreed fields.");
                var used=new HashSet<int>(Data<int>(mesh,"T"));
                Assert.That(used.Count,Is.EqualTo(Data<Vector3>(mesh,"V").Count),"Removed source vertices must not inflate the uploaded mesh.");
                var samples=new List<Vector2>();
                for(int i=0;i<23;i++)
                {
                    float angle=.173f+i*2.399963f;
                    samples.Add(CourtCentre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(.31f+4.53f*(i%7)/7));
                }
                foreach(Vector2 end in new[]{new Vector2(-17.5f,-17.5f),new Vector2(15.9f,5)})
                {
                    Vector2 direction=(end-CourtCentre).normalized,side=new Vector2(-direction.y,direction.x);
                    for(int i=0;i<11;i++)samples.Add(CourtCentre+direction*Mathf.Lerp(5.371f,Vector2.Distance(end,CourtCentre)-.131f,i/10f)+side*((i%3-1)*1.923f));
                }
                samples.AddRange(BridgeContinuationSamples());
                foreach(Vector2 p in samples)Assert.That(Owners(Project(p),mesh),Is.Zero,"Every real bed, bevel and flag top must be absent beneath the replacement.");
                Invoke("BuildCourtStoneRing",mesh);
                int ringVertices=Data<Vector3>(mesh,"V").Count-clippedVertices;
                Invoke("BuildCourtProcessionalStone",mesh);
                int pathVertices=Data<Vector3>(mesh,"V").Count-clippedVertices-ringVertices;
                Invoke("BuildCourtBridgeStone",mesh);
                int bridgeVertices=Data<Vector3>(mesh,"V").Count-clippedVertices-ringVertices-pathVertices;
                foreach(Vector2 p in samples)AssertOneOwner(Project(p),mesh,"The actual built floor must have exactly one replacement at the medallion and bridge approach.");
                TestContext.WriteLine($"Court floor vertices: raw base={originalVertices}; clipped base={clippedVertices}; ring={ringVertices}; paths={pathVertices}; bridge={bridgeVertices}; final={Data<Vector3>(mesh,"V").Count}.");
                Assert.That(clippedVertices,Is.LessThanOrEqualTo(originalVertices+2000),
                    "The four fixed footprint boundaries retain the same bounded intersection-vertex allowance.");
                Assert.That(ringVertices,Is.LessThanOrEqualTo(6500));
                Assert.That(pathVertices,Is.LessThanOrEqualTo(5000));
                Assert.That(bridgeVertices,Is.LessThanOrEqualTo(3000),"The newly fitted bridge domain has its own fixed allowance.");
                // Raw base excludes the previous medallion and paired courses;
                // it cannot serve as a before/after full-floor measurement. This
                // envelope retains both existing component budgets and bounds
                // the additional bridge continuation independently. The actual
                // region still has its independent unchanged 174,000 hard cap.
                Assert.That(Data<Vector3>(mesh,"V").Count,Is.LessThanOrEqualTo(originalVertices+6500+5000+3000),
                    "Complete floor stays within the raw base plus the independently bounded fitted assemblies.");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [Test]
        public void BridgeContinuationAndOriginalApproachShareTheirCapWithoutAGapOrOverlap()
        {
            object approach=NewBuilder(),bridge=NewBuilder();
            Invoke("BuildCourtProcessionalStone",approach);Invoke("BuildCourtBridgeStone",bridge);
            Vector2 end=new Vector2(-17.5f,-17.5f),direction=(end-CourtCentre).normalized;
            Vector2 side=new Vector2(-direction.y,direction.x);
            foreach(float lateral in new[]{-2.041f,-1.317f,-.419f,.263f,1.681f,2.041f})
            {
                foreach(float offset in new[]{-.0713f,.0713f})
                {
                    Vector2 point=Project(end+side*lateral+direction*offset);
                    int court=Owners(point,approach),continuation=Owners(point,bridge);
                    Assert.That(court,Is.EqualTo(offset<0?1:0),"The original approach retains its exact end edge.");
                    Assert.That(continuation,Is.EqualTo(offset>0?1:0),"The continuation starts on the other side of that same edge.");
                }
            }
        }

        private static IEnumerable<Vector2> BridgeContinuationSamples()
        {
            Vector2 end=new Vector2(-17.5f,-17.5f),direction=(end-CourtCentre).normalized;
            Vector2 side=new Vector2(-direction.y,direction.x);
            float threshold=FoundryMap.BridgeStart+.2f*1.41421356f;
            Vector2 far=new Vector2(threshold*.5f,threshold*.5f),across=new Vector2(FoundryMap.Axis,-FoundryMap.Axis);
            for(int i=0;i<17;i++)foreach(float lateral in new[]{-2.047f,-.713f,.163f,2.047f})
            {
                float t=.0131f+i*.06071f;
                yield return Vector2.Lerp(end+side*lateral,far+across*lateral,t);
            }
        }

        private static double DesignArea(object mesh)
        {
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");double area=0;
            for(int i=0;i<triangles.Count;i+=3)
                area+=Math.Abs(Cross(vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]]));
            // Project's determinant is one half, cancelling triangle's one half.
            return area;
        }

        private static int Owners(Vector2 point,object mesh)
        {
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");int count=0;
            for(int i=0;i<triangles.Count;i+=3)
                if(InTriangle(point,vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]]))count++;
            return count;
        }

        private static void AssertOneOwner(Vector2 point,object mesh,string message)
        {
            int owners=Owners(point,mesh);
            Assert.That(owners,Is.EqualTo(1),owners==1?message:message+" "+OwnerWitness(point,mesh));
        }

        private static string OwnerWitness(Vector2 point,object mesh)
        {
            Vector2 design=ToDesign(point);var text=new System.Text.StringBuilder();
            text.AppendFormat("Design({0:R},{1:R}) projected({2:R},{3:R}). ",design.x,design.y,point.x,point.y);
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");
            for(int i=0;i<triangles.Count;i+=3)
            {
                Vector2 a=vertices[triangles[i]],b=vertices[triangles[i+1]],c=vertices[triangles[i+2]];
                if(!InTriangle(point,a,b,c))continue;
                Vector2 da=ToDesign(a),db=ToDesign(b),dc=ToDesign(c);
                text.AppendFormat("Triangle{0} design[({1:R},{2:R}),({3:R},{4:R}),({5:R},{6:R})] edgeCross[{7:R},{8:R},{9:R}]. ",
                    i/3,da.x,da.y,db.x,db.y,dc.x,dc.y,Cross(a,b,point),Cross(b,c,point),Cross(c,a,point));
            }
            return text.ToString();
        }

        private static void CheckStoneGrainAndColour(object mesh)
        {
            var vertices=Data<Vector3>(mesh,"V");var uv=Data<Vector2>(mesh,"U");var colours=Data<Color>(mesh,"C");
            Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colours.Count,Is.EqualTo(vertices.Count));
            for(int k=0;k<vertices.Count;k++)
            {
                Vector2 p=ToDesign(vertices[k]);
                Vector2 expected=new Vector2(1+2*FoundryMap.Axis*p.x/FoundryRegionView.PavingGrainSpan,
                    1+2*FoundryMap.Axis*p.y/FoundryRegionView.PavingGrainSpan);
                Assert.That(Vector2.Distance(expected,uv[k]),Is.LessThan(.00001f));
                Assert.That(colours[k].a,Is.EqualTo(1));
                Assert.That(colours[k].r,Is.InRange(0f,1.9f));Assert.That(colours[k].g,Is.InRange(0f,1.9f));
                Assert.That(colours[k].b,Is.InRange(0f,1.9f));
            }
        }

        private static bool Covered(Vector2 point,List<Vector3> vertices,List<int> triangles)
        {
            for(int t=0;t<triangles.Count;t+=3)
                if(InTriangle(point,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]]))return true;
            return false;
        }

        private static void CheckFlushMesh(object mesh,Vector2 centre,float radius,int maximumVertices)
        {
            var vertices=Data<Vector3>(mesh,"V");var uv=Data<Vector2>(mesh,"U");
            var colors=Data<Color>(mesh,"C");var triangles=Data<int>(mesh,"T");
            Assert.That(vertices.Count,Is.InRange(1,maximumVertices));
            Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colors.Count,Is.EqualTo(vertices.Count));
            foreach(Vector3 p in vertices)
            {
                Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
                Assert.That(p.z,Is.Zero,"The medallion belongs to the flush floor decal, not a raised actor occluder.");
                Vector2 design=ToDesign(p);
                Assert.That((design-centre).magnitude,Is.LessThanOrEqualTo(radius+.00002f));
                Assert.That(Vector2.Distance(p,Project(design)),Is.LessThan(.00001f));
                FoundryMap.ToFloor(design.x,design.y,out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.True);
            }
            foreach(Vector2 p in uv)
                Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
            Assert.That(triangles.Count%3,Is.Zero);
            foreach(int index in triangles)Assert.That(index,Is.InRange(0,vertices.Count-1));
            for(int t=0;t<triangles.Count;t+=3)
                Assert.That(Math.Abs(Cross(vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]])),Is.GreaterThan(1e-10));
        }

        private static Vector2 ToDesign(Vector2 p)=>new Vector2((p.x+2*p.y)*FoundryMap.Axis,(-p.x+2*p.y)*FoundryMap.Axis);
        private static Vector2 Project(Vector2 p)=>(Vector3)typeof(FoundryRegionView).GetMethod("Project",Static).Invoke(null,new object[]{p.x,p.y});
        private static double Cross(Vector2 a,Vector2 b,Vector2 p)=>
            ((double)b.x-a.x)*((double)p.y-a.y)-((double)b.y-a.y)*((double)p.x-a.x);
        private static bool InTriangle(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {
            double x=Cross(a,b,p),y=Cross(b,c,p),z=Cross(c,a,p);
            return x>=-1e-7&&y>=-1e-7&&z>=-1e-7||x<=1e-7&&y<=1e-7&&z<=1e-7;
        }
    }
}
