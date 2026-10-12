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
            // Sample the relocated inscribed polygon, including all four fitted
            // courses and their clipped ends. The common stone bed closes them.
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
            Assert.That(vertices.Count,Is.InRange(1,5000),"Four short stone courses must remain a bounded floor addition.");
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
                for(int k=0;k<=12;k++)foreach(float lateral in new[]{-2.05f,-1.65f,1.65f,2.05f})
                {
                    Vector2 point=CourtCentre+direction*Mathf.Lerp(6,distance-.1f,k/12f)+side*lateral;
                    Assert.That(Covered(Project(point),vertices,triangles),Is.True,
                        "Every course needs a continuous full-width stone bed, including its joints and fitted ends.");
                }
            }
            Assert.That(Covered(Project(new Vector2(-10,-11)),vertices,triangles),Is.False,
                "The entrance setting leaves its central battle paving open rather than adding a solid carpet.");
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
