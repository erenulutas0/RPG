using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryPitVesselTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(0)]
        [TestCase(1)]
        public void ActualHoodStaysInsideExistingProjectedCylinderOwnership(int index)
        {
            Vector2 centre=Centres()[index];
            object b=Builder();
            Call("FormedPitHood",b,centre);
            AssertMesh(b,centre,1.1f,0,1.3f);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ActualVesselAndHardwareStayBetweenDeckAndMoltenPlane(int index)
        {
            Vector2 centre=Centres()[index];
            object b=Builder();
            Call("PitDrum",b,centre);
            float molten=(float)typeof(FoundryRegionView).GetField("PitMolten",
                Static|BindingFlags.Public).GetRawConstantValue();
            AssertMesh(b,centre,1.1f,-molten,0);
        }

        // The test checks the actual emitted mesh against the projected convex
        // cylinder, not its vertex count or a copy of the decorative profile. This
        // catches seams, straps or collars escaping the existing depth envelope.
        private static void AssertMesh(object builder,Vector2 centre,float radius,float low,float high)
        {
            var v=Field<List<Vector3>>(builder,"V");
            var uv=Field<List<Vector2>>(builder,"U");
            var colors=Field<List<Color>>(builder,"C");
            var indices=Field<List<int>>(builder,"T");
            Assert.That(v.Count,Is.GreaterThan(0));
            Assert.That(uv.Count,Is.EqualTo(v.Count));
            Assert.That(colors.Count,Is.EqualTo(v.Count));
            Assert.That(indices.Count%3,Is.Zero);
            Vector3 origin=Project(centre),u=Project(centre+Vector2.right)-origin;
            Vector3 w=Project(centre+Vector2.up)-origin;
            foreach(int i in indices)Assert.That(i,Is.InRange(0,v.Count-1));
            for(int i=0;i<v.Count;i++)
            {
                Assert.That(float.IsNaN(v[i].x)||float.IsNaN(v[i].y)||float.IsInfinity(v[i].x)||
                    float.IsInfinity(v[i].y)||float.IsNaN(v[i].z)||float.IsInfinity(v[i].z),Is.False);
                Assert.That(uv[i].x,Is.InRange(0f,1f));
                Assert.That(uv[i].y,Is.InRange(0f,1f));
                Assert.That(colors[i].a,Is.EqualTo(1f).Within(.00001f));
                for(int angle=0;angle<72;angle++)
                {
                    float radians=angle*Mathf.PI/36;
                    var direction=new Vector3(Mathf.Cos(radians),Mathf.Sin(radians),0);
                    float du=Vector3.Dot(direction,u),dv=Vector3.Dot(direction,w);
                    float extent=radius*Mathf.Sqrt(du*du+dv*dv)+direction.y*(direction.y>=0?high:low);
                    Assert.That(Vector3.Dot(direction,v[i]-origin),Is.LessThanOrEqualTo(extent+.0001f),
                        "Generated vessel geometry escaped its existing projected depth ownership.");
                }
            }
        }

        private static Vector2[] Centres()=>(Vector2[])typeof(FoundryRegionView).GetField("PitBoilers",Static).GetValue(null);
        private static Vector3 Project(Vector2 p)=>(Vector3)Call("Project",p.x,p.y);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object b,string name)=>(T)b.GetType().GetField(name,Fields).GetValue(b);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
