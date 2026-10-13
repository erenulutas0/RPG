using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryPitGuardTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [Test]
        public void AllGuardRunsKeepTheirOriginalCurvedBandAndHeightEnvelope()
        {
            for(int run=0;run<8;run++)
            {
                var lines=Lines(run,out bool near);object mesh=Builder();Call("PitGuardRun",mesh,lines,near);
                var points=Field<List<Vector3>>(mesh,"V");
                for(int direction=0;direction<144;direction++)
                {
                    float angle=direction*Mathf.PI/72;
                    var axis=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    float limit=float.NegativeInfinity;
                    foreach(int line in new[]{0,2})foreach(Vector2 p in lines[line])
                    {
                        Vector3 projected=Project(p);
                        limit=Mathf.Max(limit,Vector3.Dot(axis,projected),
                            Vector3.Dot(axis,projected+Vector3.up*.72f));
                    }
                    foreach(Vector3 p in points)Assert.That(Vector3.Dot(axis,p),Is.LessThanOrEqualTo(limit+.0001f),
                        "Guard masonry must remain inside the original rail's projected band/height prism.");
                }
            }
        }

        [Test]
        public void EveryBayRetainsBroadActualOpeningsAboveItsBearingSill()
        {
            for(int run=0;run<8;run++)
            {
                var lines=Lines(run,out bool near);object mesh=Builder();Call("PitGuardRun",mesh,lines,near);
                int clear=0,samples=0;var axis=lines[1];
                for(int j=0;j+1<axis.Length;j++)for(int sample=0;sample<9;sample++)
                {
                    Vector2 p=Vector2.Lerp(axis[j],axis[j+1],(sample+.371f)/9);
                    // A point on the actual projected clear band. Coping, returns
                    // and recessed panels all participate in the triangle query.
                    Vector3 screen=Project(p)+Vector3.up*.433f;
                    if(!Covered(mesh,screen))clear++;
                    samples++;
                }
                Assert.That(clear/(float)samples,Is.GreaterThan(.40f),
                    "More than 40% of each real bay must stay visibly open to the shaft, not an opaque parapet.");
            }
        }

        [Test]
        public void GuardsMeetOriginalPostsAndHaveFiniteBoundedMeshData()
        {
            int total=0;
            for(int run=0;run<8;run++)
            {
                var lines=Lines(run,out bool near);object mesh=Builder();Call("PitGuardRun",mesh,lines,near);
                var points=Field<List<Vector3>>(mesh,"V");var uv=Field<List<Vector2>>(mesh,"U");
                var colors=Field<List<Color>>(mesh,"C");var indices=Field<List<int>>(mesh,"T");total+=points.Count;
                Assert.That(points.Count,Is.GreaterThan(0));Assert.That(uv.Count,Is.EqualTo(points.Count));
                Assert.That(colors.Count,Is.EqualTo(points.Count));Assert.That(indices.Count%3,Is.Zero);
                foreach(int index in indices)Assert.That(index,Is.InRange(0,points.Count-1));
                for(int i=0;i<points.Count;i++)
                {
                    Assert.That(Finite(points[i].x)&&Finite(points[i].y)&&Finite(points[i].z),Is.True);
                    Assert.That(Finite(uv[i].x)&&Finite(uv[i].y),Is.True);
                    Assert.That(Finite(colors[i].r)&&Finite(colors[i].g)&&Finite(colors[i].b),Is.True);
                    Assert.That(colors[i].a,Is.EqualTo(1).Within(.00001f));
                }
                foreach(int end in new[]{0,lines[0].Length-1})
                {
                    Vector3 p=Project(lines[0][end]);
                    foreach(float h in new[]{near?.15f:0,.26f,.59f,.72f})
                        Assert.That(points.Exists(v=>(v-(p+Vector3.up*h)).sqrMagnitude<.00000001f),Is.True,
                            "Sill, end respond and coping must join the unchanged original post boundary.");
                }
            }
            Assert.That(total,Is.LessThanOrEqualTo(6000),"All eight curved guard bays have a fixed combined mesh budget.");
        }

        private static bool Covered(object mesh,Vector3 p)
        {
            var v=Field<List<Vector3>>(mesh,"V");var t=Field<List<int>>(mesh,"T");
            for(int i=0;i<t.Count;i+=3)
            {
                Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
                float area=Cross(b-a,c-a);if(Mathf.Abs(area)<.0000001f)continue;
                float s=Cross(b-(Vector2)p,c-(Vector2)p)/area;
                float u=Cross(c-(Vector2)p,a-(Vector2)p)/area;
                if(s>=.00001f&&u>=.00001f&&1-s-u>=.00001f)return true;
            }
            return false;
        }
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        private static Vector2[][] Lines(int run,out bool near)
        {
            var posts=(Vector4[])Call("PitPosts");object[] args={posts,run,false};
            var result=(Vector2[][])typeof(FoundryRegionView).GetMethod("PitRunLines",Static).Invoke(null,args);
            near=(bool)args[2];return result;
        }
        private static Vector3 Project(Vector2 p)=>(Vector3)Call("Project",p.x,p.y);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object b,string field)=>(T)b.GetType().GetField(field,Fields).GetValue(b);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
