using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryYardEquipmentTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(0,"BuildYardTrolley")]
        [TestCase(1,"BuildYardMouldBench")]
        public void ActualEquipmentRetainsSharedProjectedFootprintAndDepthEnvelope(int island,string method)
        {
            float u0=FoundryMap.WorkIslandBound(island,0),v0=FoundryMap.WorkIslandBound(island,1);
            float u1=FoundryMap.WorkIslandBound(island,2),v1=FoundryMap.WorkIslandBound(island,3);
            object builder=Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,new[]{builder,(object)u0,v0,u1,v1});
            var points=Field<List<Vector3>>(builder,"V");
            var uv=Field<List<Vector2>>(builder,"U");
            var colors=Field<List<Color>>(builder,"C");
            var indices=Field<List<int>>(builder,"T");
            Assert.That(points.Count,Is.GreaterThan(0));
            Assert.That(uv.Count,Is.EqualTo(points.Count));
            Assert.That(colors.Count,Is.EqualTo(points.Count));
            Assert.That(indices.Count%3,Is.Zero);
            var corners=new[]{Project(u0,v0),Project(u1,v0),Project(u1,v1),Project(u0,v1)};
            foreach(int index in indices)Assert.That(index,Is.InRange(0,points.Count-1));
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(float.IsNaN(points[i].x)||float.IsInfinity(points[i].x)||
                    float.IsNaN(points[i].y)||float.IsInfinity(points[i].y),Is.False);
                Assert.That(uv[i].x,Is.InRange(0f,1f));
                Assert.That(uv[i].y,Is.InRange(0f,1f));
                Assert.That(colors[i].a,Is.EqualTo(1f).Within(.00001f));
                // Support lines of the projected shared box plus the exact existing
                // vertical owner envelope. A higher clamp or projecting handle must
                // not silently escape the obstacle's cutaway/depth coverage.
                for(int direction=0;direction<72;direction++)
                {
                    float a=direction*Mathf.PI/36;
                    var axis=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                    float limit=float.NegativeInfinity;
                    foreach(Vector3 corner in corners)limit=Mathf.Max(limit,Vector3.Dot(axis,corner));
                    limit+=axis.y*(axis.y>0?1.55f:.10f);
                    Assert.That(Vector3.Dot(axis,points[i]),Is.LessThanOrEqualTo(limit+.0001f),
                        "Equipment must stay in its established projected island/depth envelope.");
                }
            }
        }

        private static Vector3 Project(float u,float v)=>(Vector3)typeof(FoundryRegionView).GetMethod("Project",Static).Invoke(null,new object[]{u,v});
        private static T Field<T>(object builder,string name)=>(T)builder.GetType().GetField(name,Fields).GetValue(builder);
    }
}
