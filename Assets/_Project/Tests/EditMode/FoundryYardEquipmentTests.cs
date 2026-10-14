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

        [Test]
        public void CompleteTrolleyAndItsSkidUseOneNoncollapsedCastField()
        {
            object builder=TrolleyWithSkid();
            var points=Field<List<Vector3>>(builder,"V");var uv=Field<List<Vector2>>(builder,"U");
            var indices=Field<List<int>>(builder,"T");var shared=new Dictionary<Vector3,Vector2>();
            int visible=0,joins=0;
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(uv[i].x,Is.InRange(0f,1f));Assert.That(uv[i].y,Is.InRange(0f,1f));
                if(shared.TryGetValue(points[i],out Vector2 other))
                {
                    joins++;
                    Assert.That(Vector2.Distance(other,uv[i]),Is.LessThan(.000001f),
                        "Frame, stock, bands and skid must not restart their cast grain at the same projected point.");
                }
                else shared.Add(points[i],uv[i]);
            }
            for(int t=0;t<indices.Count;t+=3)
            {
                int a=indices[t],b=indices[t+1],c=indices[t+2];
                if(Math.Abs(Cross(points[a],points[b],points[c]))<1e-7)continue;
                visible++;
                Assert.That(Math.Abs(Cross(uv[a],uv[b],uv[c])),Is.GreaterThan(1e-12),
                    "Actual trolley triangles must sample a two-dimensional field, not a clamped texture edge.");
                for(int side=0;side<3;side++)
                {
                    int i=indices[t+side],j=indices[t+(side+1)%3];
                    float distance=Vector2.Distance(points[i],points[j]);
                    if(distance>.01f)
                        Assert.That(Vector2.Distance(uv[i],uv[j])/distance,Is.EqualTo(.19f).Within(.00001f),
                            "The complete trolley uses the hood's grain density, including stock and thin hardware.");
                }
            }
            Assert.That(visible,Is.GreaterThan(0));Assert.That(joins,Is.GreaterThan(0));
        }

        [Test]
        public void TrolleyCastRemapChangesOnlyUvsAndPreservesAuthoredLightAndGeometry()
        {
            object builder=TrolleyWithSkid();
            var points=Field<List<Vector3>>(builder,"V");var uv=Field<List<Vector2>>(builder,"U");
            var colours=Field<List<Color>>(builder,"C");var indices=Field<List<int>>(builder,"T");
            var savedPoints=points.ToArray();var savedUvs=uv.ToArray();
            var savedColours=colours.ToArray();var savedIndices=indices.ToArray();
            float u0=FoundryMap.WorkIslandBound(0,0),v0=FoundryMap.WorkIslandBound(0,1);
            float u1=FoundryMap.WorkIslandBound(0,2),v1=FoundryMap.WorkIslandBound(0,3);
            Vector3 origin=Project((u0+u1)*.5f,(v0+v1)*.5f)+Vector3.up*.63f;
            // Shift only the field origin to exercise the actual final UV operation
            // with a different input; neither geometry nor forge colour may follow it.
            typeof(FoundryRegionView).GetMethod("YardTrolleyGrain",Static)
                .Invoke(null,new[]{builder,(object)(origin+new Vector3(.15f,-.12f,0))});
            Assert.That(points,Is.EqualTo(savedPoints));Assert.That(indices,Is.EqualTo(savedIndices));
            Assert.That(colours,Is.EqualTo(savedColours));Assert.That(uv.Count,Is.EqualTo(savedUvs.Length));
            for(int i=0;i<uv.Count;i++)
                Assert.That(Vector2.Distance(uv[i],savedUvs[i]),Is.GreaterThan(.03f),
                    "The remap was actually exercised on every owned vertex.");
        }

        private static object TrolleyWithSkid()
        {
            float u0=FoundryMap.WorkIslandBound(0,0),v0=FoundryMap.WorkIslandBound(0,1);
            float u1=FoundryMap.WorkIslandBound(0,2),v1=FoundryMap.WorkIslandBound(0,3);
            object builder=Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
            typeof(FoundryRegionView).GetMethod("WorkshopMetalHull",Static)
                .Invoke(null,new object[]{builder,u0,v0,u1,v1,.015f,.10f,.035f,true});
            typeof(FoundryRegionView).GetMethod("BuildYardTrolley",Static)
                .Invoke(null,new object[]{builder,u0,v0,u1,v1});
            return builder;
        }

        private static double Cross(Vector2 a,Vector2 b,Vector2 c)=>
            ((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);

        private static Vector3 Project(float u,float v)=>(Vector3)typeof(FoundryRegionView).GetMethod("Project",Static).Invoke(null,new object[]{u,v});
        private static T Field<T>(object builder,string name)=>(T)builder.GetType().GetField(name,Fields).GetValue(builder);
    }
}
