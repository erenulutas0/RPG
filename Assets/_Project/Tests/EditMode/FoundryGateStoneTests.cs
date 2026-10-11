using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateStoneTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private const float RampStart=FoundryMap.GateLandingStart+1f;
        private const float RampEnd=FoundryMap.GateLandingEnd-1f;

        // Include a production-sized clipped panel and two off-grid panels straddling
        // each change of grade. Correct vertices alone do not prove a flush triangle.
        [TestCase(16.48f,25.92f,1.98f,8.02f)]
        [TestCase(RampStart-.37f,RampStart+.37f,2.13f,7.87f)]
        [TestCase(RampEnd-.37f,RampEnd+.37f,2.13f,7.87f)]
        public void ProcessionalFlagsCoverTheirContourWithFlushInteriorAndContinuousGrain(
            float u0,float u1,float v0,float v1)
        {
            var contour=new[]{new Vector2(u0,v0+.12f),new Vector2(u0+.12f,v0),
                new Vector2(u1-.12f,v0),new Vector2(u1,v0+.12f),
                new Vector2(u1,v1-.12f),new Vector2(u1-.12f,v1),
                new Vector2(u0+.12f,v1),new Vector2(u0,v1-.12f)};
            object builder=Builder();
            Call("BuildGateProcessionalFlags",builder,contour);
            MeshData mesh=Read(builder);
            Vector2[] design=AssertFlushFloor(mesh);
            foreach(Vector2 point in design)
                Assert.That(InConvex(point,contour),Is.True,"Every bevel and face stays within the clipped field.");
            for(int x=0;x<=20;x++)for(int y=0;y<=12;y++)
            {
                var point=new Vector2(Mathf.Lerp(u0,u1,x/20f),Mathf.Lerp(v0,v1,y/12f));
                if(InConvex(point,contour))AssertCovered(mesh,design,point);
            }
            foreach(Vector2 point in contour)AssertCovered(mesh,design,point);
        }

        [Test]
        public void ApronCoursesStayInTheirTwoWalkableEdgeBandsAndMeetTheThreshold()
        {
            object builder=Builder();
            Call("BuildGateApronCourses",builder);
            MeshData mesh=Read(builder);
            Vector2[] design=AssertFlushFloor(mesh);
            const float start=16.2f,width=.65f;
            float end=30.5f+FoundryMap.GateOffset;
            foreach(Vector2 point in design)
            {
                Assert.That(point.x,Is.InRange(start-.0001f,end+.0001f));
                Assert.That(InBand(point.y,.7f,width)||InBand(point.y,8.65f,width),Is.True,
                    "Edge courses must not fill the central insignia or spill into scenery.");
                FoundryMap.ToFloor(point.x,point.y,out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.True,"Dressed paving stays on the shared walking surface.");
            }
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                Vector2 a=design[mesh.T[t]],b=design[mesh.T[t+1]],c=design[mesh.T[t+2]];
                bool lower=InBand(a.y,.7f,width)&&InBand(b.y,.7f,width)&&InBand(c.y,.7f,width);
                bool upper=InBand(a.y,8.65f,width)&&InBand(b.y,8.65f,width)&&InBand(c.y,8.65f,width);
                Assert.That(lower||upper,Is.True,"No triangle may bridge the two separated courses.");
            }
            foreach(float bottom in new[]{.7f,8.65f})
                for(int x=0;x<=30;x++)for(int y=0;y<=4;y++)
                    AssertCovered(mesh,design,new Vector2(Mathf.Lerp(start,end,x/30f),bottom+width*y/4f));
        }

        [Test]
        public void CompressedLockRemainsWithinItsRecessedStoneBacking()
        {
            const float height=6.5f;
            object builder=Builder();
            Call("BuildGateCartoucheLock",builder,height);
            MeshData mesh=Read(builder);
            AssertFiniteMesh(mesh);
            var outline=(Vector2[])Call("GateCartoucheOutline");
            var backing=new Vector2[outline.Length];
            for(int i=0;i<outline.Length;i++)
            {
                Vector2 face=new Vector2(outline[i].x*.63f,Mathf.Lerp(.99f,outline[i].y,.52f));
                backing[i]=(Vector3)Call("GateCartouchePoint",face,32.60f+FoundryMap.GateOffset,height);
            }
            foreach(Vector3 point in mesh.V)
                Assert.That(InConvex(point,backing),Is.True,
                    "The lock, pins and key slot must stay visually inside the actual recessed backing after compression.");
        }

        private static Vector2[] AssertFlushFloor(MeshData mesh)
        {
            AssertFiniteMesh(mesh);
            var design=new Vector2[mesh.V.Count];
            for(int i=0;i<design.Length;i++)
            {
                // Recover unprojected coordinates from the material's world grain,
                // then compare against the shared surface, including elevation.
                design[i]=new Vector2((mesh.U[i].x-1)*FoundryRegionView.PavingGrainSpan/(2*FoundryMap.Axis),
                    (mesh.U[i].y-1)*FoundryRegionView.PavingGrainSpan/(2*FoundryMap.Axis));
                Assert.That(Vector3.Distance(mesh.V[i],SurfacePoint(design[i])),Is.LessThan(.0001f),
                    "The new face must retain world-continuous stone UVs and touch the floor.");
            }
            var weights=new[]{new Vector3(1f/3,1f/3,1f/3),new Vector3(.6f,.2f,.2f),
                new Vector3(.2f,.6f,.2f),new Vector3(.2f,.2f,.6f)};
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                int a=mesh.T[t],b=mesh.T[t+1],c=mesh.T[t+2];
                Assert.That(Cross(design[b]-design[a],design[c]-design[a]),Is.GreaterThan(1e-8),
                    "Clipped stone triangles keep a positive, nondegenerate winding.");
                foreach(float seam in new[]{RampStart,RampEnd})
                {
                    float min=Mathf.Min(design[a].x,design[b].x,design[c].x);
                    float max=Mathf.Max(design[a].x,design[b].x,design[c].x);
                    Assert.That(min<seam-.0001f&&max>seam+.0001f,Is.False,
                        "A face may touch a grade break but may not span it.");
                }
                foreach(Vector3 w in weights)
                {
                    Vector2 p=design[a]*w.x+design[b]*w.y+design[c]*w.z;
                    Vector3 actual=mesh.V[a]*w.x+mesh.V[b]*w.y+mesh.V[c]*w.z;
                    Assert.That(Vector3.Distance(actual,SurfacePoint(p)),Is.LessThan(.0001f),
                        "Triangle interiors cannot bridge above or below either ramp seam.");
                }
            }
            return design;
        }

        private static void AssertFiniteMesh(MeshData mesh)
        {
            Assert.That(mesh.V.Count,Is.GreaterThan(0));
            Assert.That(mesh.U.Count,Is.EqualTo(mesh.V.Count));
            Assert.That(mesh.C.Count,Is.EqualTo(mesh.V.Count));
            Assert.That(mesh.T.Count,Is.GreaterThan(0));Assert.That(mesh.T.Count%3,Is.Zero);
            for(int i=0;i<mesh.V.Count;i++)
            {
                foreach(float value in new[]{mesh.V[i].x,mesh.V[i].y,mesh.V[i].z,mesh.U[i].x,mesh.U[i].y,
                    mesh.C[i].r,mesh.C[i].g,mesh.C[i].b,mesh.C[i].a})
                    Assert.That(float.IsNaN(value)||float.IsInfinity(value),Is.False,"Geometry and vertex attributes remain finite.");
            }
            foreach(int index in mesh.T)Assert.That(index,Is.InRange(0,mesh.V.Count-1));
        }

        private static void AssertCovered(MeshData mesh,Vector2[] design,Vector2 point)
        {
            bool covered=false;
            for(int t=0;t<mesh.T.Count&&!covered;t+=3)
            {
                Vector2 a=design[mesh.T[t]],b=design[mesh.T[t+1]],c=design[mesh.T[t+2]];
                if(point.x<Mathf.Min(a.x,b.x,c.x)-.0001f||point.x>Mathf.Max(a.x,b.x,c.x)+.0001f||
                    point.y<Mathf.Min(a.y,b.y,c.y)-.0001f||point.y>Mathf.Max(a.y,b.y,c.y)+.0001f)continue;
                double area=Cross(b-a,c-a);
                if(area<=1e-8)continue;
                covered=Cross(b-a,point-a)>=-.00002&&Cross(c-b,point-b)>=-.00002&&Cross(a-c,point-c)>=-.00002;
            }
            Assert.That(covered,Is.True,$"The mortar bed covers {point}, including clipped corners and boundary seams.");
        }

        private static bool InConvex(Vector2 point,IList<Vector2> polygon)
        {
            bool positive=false,negative=false;
            for(int i=0;i<polygon.Count;i++)
            {
                double side=Cross(polygon[(i+1)%polygon.Count]-polygon[i],point-polygon[i]);
                positive|=side>.00003;negative|=side<-.00003;
            }
            return !(positive&&negative);
        }
        private static bool InBand(float y,float bottom,float width)=>y>=bottom-.0001f&&y<=bottom+width+.0001f;
        private static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        private static Vector3 SurfacePoint(Vector2 p)=>(Vector3)typeof(FoundryRegionView).Assembly
            .GetType("Cryptforge.UI.FoundrySurface").GetMethod("Project",Static).Invoke(null,new object[]{p.x,p.y});
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static object Call(string name,params object[] args)
        {
            MethodInfo method=typeof(FoundryRegionView).GetMethod(name,Static);
            Assert.That(method,Is.Not.Null,name);
            return method.Invoke(null,args);
        }
        private static MeshData Read(object builder)
        {
            object Field(string name)=>builder.GetType().GetField(name,Fields).GetValue(builder);
            return new MeshData{V=(List<Vector3>)Field("V"),U=(List<Vector2>)Field("U"),
                C=(List<Color>)Field("C"),T=(List<int>)Field("T")};
        }
        private sealed class MeshData
        {
            internal List<Vector3> V;internal List<Vector2> U;internal List<Color> C;internal List<int> T;
        }
    }
}
