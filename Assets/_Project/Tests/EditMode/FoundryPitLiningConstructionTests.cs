using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryPitLiningConstructionTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private const float Epsilon=.0001f;

        [TestCase("PitLiningBearingCourse")]
        [TestCase("PitLiningBearings")]
        public void NewConstructionStaysInsideOriginalBelowDeckLiningEnvelope(string method)
        {
            object mesh=Builder();Call(method,mesh);AssertMesh(mesh);
            var oldEdge=new List<Vector3>();
            for(int i=0;i<=Constant<int>("PitFacets");i++)oldEdge.Add(Project(Station(i)));
            foreach(Vector3 p in Field<List<Vector3>>(mesh,"V"))
                for(int sample=0;sample<72;sample++)
                {
                    float angle=sample*Mathf.PI/36;
                    var axis=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    float limit=float.NegativeInfinity;
                    foreach(Vector3 corner in oldEdge)limit=Mathf.Max(limit,Vector3.Dot(axis,corner));
                    if(axis.y<0)limit-=axis.y*Constant<float>("PitMolten");
                    Assert.That(Vector3.Dot(axis,p),Is.LessThanOrEqualTo(limit+Epsilon),
                        "Pit construction must not escape the original far lining/deck-to-pool prism.");
                }
        }

        [Test]
        public void BearingCourseJoinsEveryOriginalDeckAndLowerWallStation()
        {
            object mesh=Builder();Call("PitLiningBearingCourse",mesh);
            var points=Field<List<Vector3>>(mesh,"V");
            // The new course replaces the old first two rows. Its outer boundaries
            // must share the actual rail/deck edge and the retained wall below.
            float lower=FieldStatic<float[]>("PitLiningRows")[2];
            for(int i=0;i<=Constant<int>("PitFacets");i++)
            {
                Vector3 edge=Project(Station(i));
                Assert.That(points.Exists(p=>(p-edge).sqrMagnitude<Epsilon*Epsilon),Is.True);
                Assert.That(points.Exists(p=>(p-(edge+Vector3.down*lower)).sqrMagnitude<Epsilon*Epsilon),Is.True);
            }
        }

        [Test]
        public void DeepBearingsLeaveExistingFurnaceApertureColumnsUncovered()
        {
            object mesh=Builder();Call("PitLiningBearings",mesh);
            foreach(int bay in new[]{5,10,15})
            {
                Vector2 a=Station(bay),b=Station(bay+1);
                Vector3 left=Project(Vector2.Lerp(a,b,.12f)),right=Project(Vector2.Lerp(a,b,.88f));
                float min=Mathf.Min(left.x,right.x),max=Mathf.Max(left.x,right.x);
                foreach(Vector3 point in Field<List<Vector3>>(mesh,"V"))
                    Assert.That(point.x<min-Epsilon||point.x>max+Epsilon,Is.True,
                        "A new bearing must not paint across an existing furnace opening or its frame.");
            }
        }

        [Test]
        public void CompleteLiningHasBoundedGrowthAndRetainsOriginalMoltenSeam()
        {
            object mesh=Builder();Call("PitLiningFronts",mesh);AssertMesh(mesh);
            var points=Field<List<Vector3>>(mesh,"V");
            Assert.That(points.Count,Is.LessThanOrEqualTo(924+1200),
                "The original 924-vertex lining plus this package's approved 1,200-vertex allowance is a hard cap.");
            float molten=Constant<float>("PitMolten");
            for(int i=0;i<=Constant<int>("PitFacets");i++)
            {
                Vector3 foot=Project(Station(i))+Vector3.down*molten;
                Assert.That(points.Exists(p=>(p-foot).sqrMagnitude<Epsilon*Epsilon),Is.True,
                    "The lining still meets the unchanged molten waterline at every original station.");
            }
        }

        private static void AssertMesh(object mesh)
        {
            var points=Field<List<Vector3>>(mesh,"V");var uv=Field<List<Vector2>>(mesh,"U");
            var colors=Field<List<Color>>(mesh,"C");var indices=Field<List<int>>(mesh,"T");
            Assert.That(points.Count,Is.GreaterThan(0));Assert.That(indices.Count%3,Is.Zero);
            Assert.That(uv.Count,Is.EqualTo(points.Count));Assert.That(colors.Count,Is.EqualTo(points.Count));
            foreach(int index in indices)Assert.That(index,Is.InRange(0,points.Count-1));
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(Finite(points[i].x)&&Finite(points[i].y)&&Finite(points[i].z),Is.True);
                Assert.That(uv[i].x,Is.InRange(0f,1f));Assert.That(uv[i].y,Is.InRange(0f,1f));
                Assert.That(Finite(colors[i].r)&&Finite(colors[i].g)&&Finite(colors[i].b),Is.True);
                Assert.That(colors[i].a,Is.InRange(0f,1f));
            }
        }

        private static Vector2 Station(int i)
        {
            object[] args={i,Vector2.zero};
            return (Vector2)typeof(FoundryRegionView).GetMethod("PitLiningStation",Static).Invoke(null,args);
        }
        private static T Constant<T>(string name)=>(T)typeof(FoundryRegionView).GetField(name,Static).GetRawConstantValue();
        private static T FieldStatic<T>(string name)=>(T)typeof(FoundryRegionView).GetField(name,Static).GetValue(null);
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        private static Vector3 Project(Vector2 p)=>(Vector3)Call("Project",p.x,p.y);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object builder,string name)=>(T)builder.GetType().GetField(name,Fields).GetValue(builder);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
