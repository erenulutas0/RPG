using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryBridgePavingTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [Test]
        public void ActualDeckCoversItsWalkingQuadWithFlushContinuousGrainAndExactCourtJoin()
        {
            Type view=typeof(FoundryRegionView),type=view.GetNestedType("Builder",BindingFlags.NonPublic);
            object builder=Activator.CreateInstance(type,true);
            view.GetMethod("BuildBridgeDeck",Static).Invoke(null,new[]{builder});
            object Read(string name)=>type.GetField(name,Fields).GetValue(builder);
            var vertices=(List<Vector3>)Read("V");var uv=(List<Vector2>)Read("U");
            var colours=(List<Color>)Read("C");var triangles=(List<int>)Read("T");
            Assert.That(vertices.Count,Is.GreaterThan(0));
            Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colours.Count,Is.EqualTo(vertices.Count));
            var design=new Vector2[vertices.Count];
            Type surface=view.Assembly.GetType("Cryptforge.UI.FoundrySurface");
            MethodInfo project=surface.GetMethod("Project",Static);
            for(int i=0;i<vertices.Count;i++)
            {
                design[i]=new Vector2((uv[i].x-1)*FoundryRegionView.PavingGrainSpan/(2*FoundryMap.Axis),
                    (uv[i].y-1)*FoundryRegionView.PavingGrainSpan/(2*FoundryMap.Axis));
                Vector2 p=design[i];
                Assert.That(p.x+p.y,Is.InRange(FoundryMap.BridgeStart-.0001f,FoundryMap.BridgeEnd+.0001f));
                Assert.That(Mathf.Abs(p.x-p.y),Is.LessThanOrEqualTo(FoundryMap.BridgeHalfWidth+.0001f));
                Assert.That(p.y,Is.LessThanOrEqualTo(FoundryMap.South+.0001f),"Court paving owns the shared overlap.");
                var expected=(Vector3)project.Invoke(null,new object[]{p.x,p.y});
                Assert.That(Vector3.Distance(vertices[i],expected),Is.LessThan(.0001f),"World UVs and projected walking plane agree.");
                Color c=colours[i];
                foreach(float value in new[]{c.r,c.g,c.b,c.a})
                    Assert.That(float.IsNaN(value)||float.IsInfinity(value),Is.False);
            }
            foreach(int index in triangles)Assert.That(index,Is.InRange(0,vertices.Count-1));
            // Mortar, faces and bevels must cover the entire traversable deck, including
            // the staggered courses, both lane boundaries and the two real court seams.
            for(int along=0;along<=48;along++)for(int across=0;across<=16;across++)
            {
                float s=Mathf.Lerp(FoundryMap.BridgeStart,FoundryMap.BridgeEnd,along/48f);
                float d=Mathf.Lerp(-FoundryMap.BridgeHalfWidth,FoundryMap.BridgeHalfWidth,across/16f);
                var point=new Vector2((s+d)*.5f,(s-d)*.5f);
                if(point.y>FoundryMap.South+.00001f)continue;
                bool covered=false;
                for(int t=0;t<triangles.Count&&!covered;t+=3)
                {
                    Vector2 a=design[triangles[t]],b=design[triangles[t+1]],c=design[triangles[t+2]];
                    double area=Cross(b-a,c-a);
                    if(Math.Abs(area)<1e-8)continue;
                    double sign=area>0?1:-1;
                    covered=sign*Cross(b-a,point-a)>=-.00005&&sign*Cross(c-b,point-b)>=-.00005&&
                        sign*Cross(a-c,point-c)>=-.00005;
                }
                Assert.That(covered,Is.True,$"No paving hole at deck {s},{d}.");
            }
        }
        private static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
    }
}
