using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateRecessTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

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
        [TestCase(27.53f,-6.14f,30.42f,-4.61f,3.63f,6.55f,.065f,.36f)]
        [TestCase(27.53f,14.61f,30.42f,16.14f,3.63f,6.55f,.065f,.36f)]
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
