using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateSurfaceTests
    {
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(-3)] [TestCase(11)] [TestCase(0)]
        public void FinishedGateKeepsConstructionLightAndCoverageWhileResolvingAtlasFolds(int part)
        {
            object b=Builder();
            if(part==0)Call("BuildGateHeaderFinish",b,6.5f);
            else Call("BuildGateInnerReturn",b,(float)part,6.5f);
            Vector3[] original=Data<Vector3>(b,"V").ToArray();
            Color[] light=Data<Color>(b,"C").ToArray();
            int[] oldTriangles=Data<int>(b,"T").ToArray();
            double area=Area(original,oldTriangles);
            Call("GateHouseGrain",b);
            var points=Data<Vector3>(b,"V");var colours=Data<Color>(b,"C");
            var uv=Data<Vector2>(b,"U");var triangles=Data<int>(b,"T");
            Assert.That(points.Count,Is.GreaterThan(original.Length),"Physical-scale folds must be tessellated, not stretched across long members.");
            Assert.That(points.Count-original.Length,Is.LessThan(1800),"Bound gate-local construction cost per tested assembly.");
            for(int i=0;i<original.Length;i++)
            {
                Assert.That(points[i],Is.EqualTo(original[i]),"No original contact, rebate or roof station may move.");
                Assert.That(colours[i],Is.EqualTo(Albedo(light[i])),"Source forge light and contact values survive the grain finish under one constant albedo balance.");
            }
            Assert.That(Area(points,triangles),Is.EqualTo(area).Within(.0006),"Grain subdivision cannot change coverage or create another stone layer.");
            Assert.That(uv.Count,Is.EqualTo(points.Count));Assert.That(colours.Count,Is.EqualTo(points.Count));
            FoundryMap.ToFloor(32+FoundryMap.GateOffset,-3,out float datumX,out float datumY);
            Vector2 datum=new Vector2(datumX,datumY*.5f+1.25f);
            Vector4 patch=Patch();
            var seen=new Dictionary<Vector3,Vector2>();
            foreach(int i in triangles)
            {
                Assert.That(uv[i].x,Is.InRange(patch.x-.00001f,patch.z+.00001f));
                Assert.That(uv[i].y,Is.InRange(patch.y-.00001f,patch.w+.00001f));
                if(seen.TryGetValue(points[i],out Vector2 previous))
                    Assert.That(Vector2.Distance(previous,uv[i]),Is.LessThan(.00001f),"Coincident course, mortar and arris points share one field.");
                else seen.Add(points[i],uv[i]);
            }
            // Every rendered triangle must lie on one affine branch of each
            // mirrored axis. Otherwise barycentric interpolation shortcuts the
            // repeated texture even if all endpoint UVs are in the right patch.
            for(int i=0;i<triangles.Count;i+=3)
            {
            Vector2 probe=(Vector2)(points[triangles[i]]*.231f+points[triangles[i+1]]*.317f+points[triangles[i+2]]*.452f);
            Vector2 sampled=uv[triangles[i]]*.231f+uv[triangles[i+1]]*.317f+uv[triangles[i+2]]*.452f;
            for(int axis=0;axis<2;axis++)
            {
                float size=axis==0?patch.z-patch.x:patch.w-patch.y,lo=float.MaxValue,hi=float.MinValue;
                for(int j=0;j<3;j++)
                {
                    float f=(points[triangles[i+j]][axis]-datum[axis])*.075f/size;
                    lo=Mathf.Min(lo,f);hi=Mathf.Max(hi,f);
                }
                Assert.That(Mathf.FloorToInt(lo+.00002f),Is.EqualTo(Mathf.FloorToInt(hi-.00002f)).Or.EqualTo(Mathf.FloorToInt(hi+.00002f)),
                    "A rendered triangle cannot bridge an uncut repeat fold.");
                double physical=(probe[axis]-datum[axis])*.075/size;
                double cycle=physical-Math.Floor(physical/2)*2;
                double expected=(axis==0?patch.x:patch.y)+(1-Math.Abs(cycle-1))*size;
                Assert.That(sampled[axis],Is.EqualTo(expected).Within(.00002),
                    "Asymmetric interior interpolation must match the independently translated physical quarry field.");
            }
            }
            TestContext.WriteLine($"Gate grain part {part}: {original.Length}->{points.Count} vertices; {oldTriangles.Length/3}->{triangles.Count/3} triangles.");
        }

        static Vector4 Patch()=>(Vector4)typeof(FoundryRegionView).GetField("GateHousePatch",Static).GetValue(null);
        static Color Albedo(Color c)=>(Color)Call("GateHouseAlbedo",c);

        static double Area(IList<Vector3> p,IList<int> t)
        {
            double total=0;
            for(int i=0;i<t.Count;i+=3)
            {
                Vector3 a=p[t[i]],b=p[t[i+1]],c=p[t[i+2]];
                total+=Math.Abs(((double)b.x-a.x)*(c.y-a.y)-((double)b.y-a.y)*(c.x-a.x))*.5;
            }
            return total;
        }
        static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic));
        static List<T> Data<T>(object b,string name)=>(List<T>)b.GetType().GetField(name,Fields).GetValue(b);
        static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
