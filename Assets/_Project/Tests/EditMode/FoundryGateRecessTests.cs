using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateRecessTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(-6.6f)]
        [TestCase(14.15f)]
        public void OuterGateTowerKeepsItsFootprintAndExposesTheTallBayAcrossTheFormerWaist(float v)
        {
            float u=31.05f+FoundryMap.GateOffset;
            var foot=new Vector4(u,v,u+3.85f,v+2.45f);
            object built=Builder();Call("BuildGateOuterButtress",built,foot);
            MeshData mesh=Read(built);AssertFinite(mesh);
            object envelope=Builder();Call("LitFoundryTier",envelope,foot.x,foot.y,foot.z,foot.w,0f,7.35f,0f);
            MeshData hull=Read(envelope);
            foreach(Vector3 point in mesh.V)Assert.That(LastCoveringTriangle(hull,point),Is.GreaterThanOrEqualTo(0),
                "The stronger tower body must stay inside its established footing and crown envelope.");
            Assert.That(mesh.V.Count,Is.LessThan(2400),"A longer bay must replace the waist rather than accumulate tiny trim.");
            var box=new Vector4(foot.x+.48f,foot.y+.46f,foot.z-.48f,foot.w-.46f);
            object isolated=Builder();Call("GateRecessedShaft",isolated,box,1.84f,6.55f,.065f,.92f,.36f);
            MeshData bay=Read(isolated);
            object shell=Builder();Call("LitFoundryTier",shell,box.x,box.y,box.z,box.w,1.84f,6.55f,.065f);
            MeshData plane=Read(shell);
            foreach(int side in new[]{0,1})
            {
                int first=side*4;
                Vector3 inward=side==0?Project(box.x,box.y+.36f)-Project(box.x,box.y):
                    Project(box.x+.36f,box.y)-Project(box.x,box.y);
                // Mid-height probes cross the old storey belt. The complete tower's
                // final visible triangle must still be the real recessed bay.
                foreach(float rise in new[]{.30f,.37f,.44f,.51f,.58f})
                {
                    Vector3 low=Vector3.Lerp(plane.V[first],plane.V[first+1],.5f);
                    Vector3 high=Vector3.Lerp(plane.V[first+3],plane.V[first+2],.5f);
                    Vector3 point=Vector3.Lerp(low,high,rise)+inward;
                    int expected=LastCoveringTriangle(bay,point),actual=LastCoveringTriangle(mesh,point);
                    Assert.That(expected,Is.GreaterThanOrEqualTo(0));Assert.That(actual,Is.GreaterThanOrEqualTo(0));
                    for(int k=0;k<3;k++)Assert.That(Vector3.Distance(mesh.V[mesh.T[actual+k]],bay.V[bay.T[expected+k]]),
                        Is.LessThan(.00001f),"No surviving full storey or corner applique may cover the new opening.");
                }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void JambRebateHasAClosedBearingWithoutFillingItsConcaveCutOrEnteringTheFacade(bool near)
        {
            float a=near?-1.58f:11.04f,b=near?-1.04f:11.58f;
            var box=new Vector4(32.10f+FoundryMap.GateOffset,a+.075f,32.96f+FoundryMap.GateOffset,b-.075f);
            const float bottom=1.15f,top=6.12f;
            object built=Builder();Call("BuildGateJambRebate",built,box,bottom,top,near);
            MeshData mesh=Read(built);AssertFinite(mesh);
            Assert.That(mesh.V.Count,Is.LessThanOrEqualTo(400));
            // Independently reconstruct the original body, including its inset
            // and real bevel. A new square box is not a valid bearing oracle.
            object envelope=Builder();Call("LitFoundryTier",envelope,32.04f+FoundryMap.GateOffset,a+.025f,
                33.04f+FoundryMap.GateOffset,b-.025f,bottom,top,.025f);
            MeshData hull=Read(envelope);
            foreach(Vector3 point in mesh.V)Assert.That(LastCoveringTriangle(hull,point),Is.GreaterThanOrEqualTo(0));
            // The existing base's TOP is the support, not its larger lower box
            // or projected walls. Check every section corner and edge midpoint.
            object footing=Builder();Call("LitFoundryTier",footing,32.02f+FoundryMap.GateOffset,a,
                33.04f+FoundryMap.GateOffset,b,.45f,bottom,.03f);
            MeshData baseMesh=Read(footing);
            var baseCap=new MeshData(baseMesh.V,baseMesh.U,baseMesh.C,new List<int>());
            for(int t=0;t<baseMesh.T.Count;t+=3)
                if(baseMesh.T[t]>=12&&baseMesh.T[t+1]>=12&&baseMesh.T[t+2]>=12)
                    for(int k=0;k<3;k++)baseCap.T.Add(baseMesh.T[t+k]);
            var section=(Vector2[])Call("GateJambSection",box,near);
            for(int i=0;i<section.Length;i++)
                foreach(Vector2 foot in new[]{section[i],(section[i]+section[(i+1)%section.Length])*.5f})
                    Assert.That(LastCoveringTriangle(baseCap,Project(foot.x,foot.y)+Vector3.up*bottom),
                        Is.GreaterThanOrEqualTo(0),"Every rebate corner must bear on the actual chamfered base cap.");
            // The final two cap quads are the actual L-shaped bearing. Invert their
            // projected plane to test coverage, including the absent front corner.
            var cap=new MeshData(new List<Vector3>(),new List<Vector2>(),new List<Color>(),new List<int>());
            int start=mesh.V.Count-8;
            for(int i=start;i<mesh.V.Count;i++)
            {
                Vector3 floor=mesh.V[i]-Vector3.up*top;
                object[] coordinates={floor.x,floor.y,0f,0f};
                typeof(FoundryRegionView).Assembly.GetType("Cryptforge.UI.FoundrySurface")
                    .GetMethod("ToDesign",Static).Invoke(null,coordinates);
                float u=(float)coordinates[2],v=(float)coordinates[3];
                cap.V.Add(new Vector3(u,v,0));
                Assert.That(u,Is.InRange(box.x-.00001f,box.z+.00001f));
                Assert.That(v,Is.InRange(box.y-.00001f,box.w+.00001f));
                Assert.That(v< -1||v>11,Is.True,"Stone stays outside all painted banner/door pixels.");
            }
            foreach(int index in mesh.T)if(index>=start)cap.T.Add(index-start);
            float front=box.x+(box.z-box.x)*.15f,rear=box.x+(box.z-box.x)*.85f;
            float cut=near?Mathf.Lerp(box.y,box.w,.90f):Mathf.Lerp(box.y,box.w,.10f);
            float spine=near?Mathf.Lerp(box.y,box.w,.20f):Mathf.Lerp(box.y,box.w,.80f);
            Assert.That(LastCoveringTriangle(cap,new Vector2(front,cut)),Is.EqualTo(-1),"A cap fan must not bridge across the real reveal.");
            Assert.That(LastCoveringTriangle(cap,new Vector2(rear,cut)),Is.GreaterThanOrEqualTo(0));
            Assert.That(LastCoveringTriangle(cap,new Vector2(front,spine)),Is.GreaterThanOrEqualTo(0));
            Assert.That(LastCoveringTriangle(cap,new Vector2(rear,spine)),Is.GreaterThanOrEqualTo(0));
        }

        private static Vector3 Project(float u,float v)=>(Vector3)Call("Project",u,v);

        [TestCase(.25f,.125f)]
        [TestCase(-.25f,.125f)]
        public void RecessInteriorRemainsExposedAfterAllFrontStonesArePainted(float dx,float dy)
        {
            object builder=Builder();
            var face=new[]{new Vector3(0,0,0),new Vector3(4,0,0),new Vector3(3.8f,5,0),new Vector3(.2f,5,0)};
            Call("GateRecessedFace",builder,face,new Vector3(dx,dy,0),new Color(.6f,.64f,.72f));
            MeshData mesh=Read(builder);
            AssertFinite(mesh);
            // Probe the visible throat, not just vertices or an authored marker.
            // The last covering triangle must be backing or a connected return;
            // later bearing/frame triangles must not close the intended opening.
            for(int x=0;x<=8;x++)for(int y=0;y<=10;y++)
            {
                var point=new Vector2(Mathf.Lerp(1.53f,2.47f,x/8f),Mathf.Lerp(1.4f,3.1f,y/10f));
                int last=LastCoveringTriangle(mesh,point);
                Assert.That(last,Is.GreaterThanOrEqualTo(0),"A blind recess cannot contain a missing face.");
                Assert.That(mesh.T[last],Is.LessThan(40),"Front masonry must not overpaint the throat.");
            }
            // The complete original wall still has a continuous projected cover,
            // including every frame return and both upper shoulder junctions.
            for(int x=1;x<25;x++)for(int y=1;y<25;y++)
            {
                Vector3 low=Vector3.Lerp(face[0],face[1],x/25f),high=Vector3.Lerp(face[3],face[2],x/25f);
                Assert.That(LastCoveringTriangle(mesh,Vector3.Lerp(low,high,y/25f)),Is.GreaterThanOrEqualTo(0));
            }
        }

        [TestCase(28.26f,-2.76f,30.74f,-1.24f,1.70f,5.45f,.04f,.28f)]
        [TestCase(28.26f,11.24f,30.74f,12.76f,1.70f,5.45f,.04f,.28f)]
        [TestCase(27.53f,-6.14f,30.42f,-4.61f,1.84f,6.55f,.065f,.36f)]
        [TestCase(27.53f,14.61f,30.42f,16.14f,1.84f,6.55f,.065f,.36f)]
        public void RecessedShaftKeepsOriginalTaperedPrismAndClosedCornerAndCap(
            float u0,float v0,float u1,float v1,float bottom,float top,float inset,float depth)
        {
            object baseline=Builder();Call("LitFoundryTier",baseline,u0,v0,u1,v1,bottom,top,inset);
            MeshData old=Read(baseline);
            object builder=Builder();Call("GateRecessedShaft",builder,new Vector4(u0,v0,u1,v1),bottom,top,inset,.96f,depth);
            MeshData mesh=Read(builder);
            AssertFinite(mesh);
            Assert.That(mesh.V.Count,Is.LessThanOrEqualTo(550),"The rebate must remain a bounded static structure.");
            // The existing prism is convex in projection. Every new return and
            // backing point must remain inside its original visible silhouette.
            foreach(Vector3 point in mesh.V)
                Assert.That(LastCoveringTriangle(old,point),Is.GreaterThanOrEqualTo(0),
                    "New geometry must not spill outside the existing prism's projection.");
            int kept=old.V.Count-8;
            for(int i=0;i<kept;i++)
                Assert.That(Vector3.Distance(mesh.V[mesh.V.Count-kept+i],old.V[8+i]),Is.LessThan(.00001f),
                    "The chamfer and cap contacts are retained exactly.");
        }

        private static int LastCoveringTriangle(MeshData mesh,Vector2 p)
        {
            int last=-1;
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                Vector2 a=mesh.V[mesh.T[t]],b=mesh.V[mesh.T[t+1]],c=mesh.V[mesh.T[t+2]];
                float area=Cross(b-a,c-a);if(Mathf.Abs(area)<1e-8f)continue;
                float x=Cross(b-p,c-p)/area,y=Cross(c-p,a-p)/area,z=1-x-y;
                if(x>=-.0001f&&y>=-.0001f&&z>=-.0001f)last=t;
            }
            return last;
        }
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
        private static MeshData Read(object builder)
        {
            Type type=builder.GetType();
            return new MeshData((List<Vector3>)type.GetField("V",Fields).GetValue(builder),
                (List<Vector2>)type.GetField("U",Fields).GetValue(builder),
                (List<Color>)type.GetField("C",Fields).GetValue(builder),
                (List<int>)type.GetField("T",Fields).GetValue(builder));
        }
        private static void AssertFinite(MeshData mesh)
        {
            Assert.That(mesh.V.Count,Is.EqualTo(mesh.U.Count));Assert.That(mesh.V.Count,Is.EqualTo(mesh.C.Count));
            foreach(Vector3 p in mesh.V)Assert.That(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||
                float.IsNaN(p.z)||float.IsInfinity(p.z),Is.False);
            foreach(Vector2 p in mesh.U)Assert.That(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y),Is.False);
            foreach(Color c in mesh.C)
                Assert.That(float.IsNaN(c.r)||float.IsInfinity(c.r)||float.IsNaN(c.g)||float.IsInfinity(c.g)||
                    float.IsNaN(c.b)||float.IsInfinity(c.b)||float.IsNaN(c.a)||float.IsInfinity(c.a),Is.False);
            foreach(int index in mesh.T)Assert.That(index,Is.InRange(0,mesh.V.Count-1));
        }
        private sealed class MeshData
        {
            internal readonly List<Vector3> V;internal readonly List<Vector2> U;
            internal readonly List<Color> C;internal readonly List<int> T;
            internal MeshData(List<Vector3> v,List<Vector2> u,List<Color> c,List<int> t){V=v;U=u;C=c;T=t;}
        }
    }
}
