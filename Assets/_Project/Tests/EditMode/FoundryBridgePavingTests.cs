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
        public void LaidDeckCoversItsWalkingQuadInTheBridgesFieldWithAnExactCourtJoin()
        {
            Type view=typeof(FoundryRegionView),type=view.GetNestedType("Builder",BindingFlags.NonPublic);
            object builder=Activator.CreateInstance(type,true);
            view.GetMethod("BuildBridgeDeck",Static).Invoke(null,new[]{builder});
            object Read(string name)=>type.GetField(name,Fields).GetValue(builder);
            var vertices=(List<Vector3>)Read("V");var uv=(List<Vector2>)Read("U");
            var colours=(List<Color>)Read("C");var triangles=(List<int>)Read("T");
            Assert.That(vertices.Count,Is.InRange(1,3000),"The laid deck is its light lattice only.");
            Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colours.Count,Is.EqualTo(vertices.Count));
            float span=(float)view.GetField("FlagstoneSpan",Static|BindingFlags.Public).GetValue(null);
            float scale=(float)view.GetField("CourtWayScale",Static).GetRawConstantValue();
            var design=new Vector2[vertices.Count];
            for(int i=0;i<vertices.Count;i++)
            {
                Vector3 v=vertices[i];
                Assert.That(v.z,Is.Zero);
                design[i]=new Vector2((v.x+2*v.y)*FoundryMap.Axis,(-v.x+2*v.y)*FoundryMap.Axis);
                Vector2 p=design[i];
                Assert.That(Mathf.Abs(p.x-p.y),Is.LessThanOrEqualTo(FoundryMap.BridgeHalfWidth+.001f),"The deck stays within the bridge's width.");
                Assert.That(p.x+p.y,Is.InRange(FoundryMap.BridgeStart-.001f,FoundryMap.BridgeEnd+.001f));
                // One field for the whole bridge: courses across it, at the processional ways' density.
                Vector2 expected=new Vector2(p.x-p.y,p.x+p.y)*FoundryMap.Axis*scale/span;
                Assert.That(Vector2.Distance(uv[i],expected),Is.LessThan(2e-4f),"The bridge's laid stone runs in its own field.");
                Color c=colours[i];
                foreach(float value in new[]{c.r,c.g,c.b,c.a})
                    Assert.That(float.IsNaN(value)||float.IsInfinity(value),Is.False);
                Assert.That(c.a,Is.EqualTo(1));
            }
            foreach(int index in triangles)Assert.That(index,Is.InRange(0,vertices.Count-1));
            // The processional way's end, which dips onto the deck, is the way's own: count it with the deck.
            object way=Activator.CreateInstance(type,true);
            view.GetMethod("BuildCourtProcessionalWay",Static).Invoke(null,new[]{way});
            var wayVertices=(List<Vector3>)type.GetField("V",Fields).GetValue(way);
            var wayTriangles=(List<int>)type.GetField("T",Fields).GetValue(way);
            int baseIndex=design.Length;
            var all=new List<Vector2>(design);
            foreach(Vector3 v in wayVertices)all.Add(new Vector2((v.x+2*v.y)*FoundryMap.Axis,(-v.x+2*v.y)*FoundryMap.Axis));
            var allTriangles=new List<int>(triangles);
            foreach(int index in wayTriangles)allTriangles.Add(baseIndex+index);
            design=all.ToArray();triangles=allTriangles;
            // The laid stone covers the entire traversable deck.
            for(int along=0;along<=48;along++)for(int across=0;across<=16;across++)
            {
                float s=Mathf.Lerp(FoundryMap.BridgeStart,FoundryMap.BridgeEnd,along/48f)+.0137f;
                float d=Mathf.Lerp(-FoundryMap.BridgeHalfWidth,FoundryMap.BridgeHalfWidth,across/16f)*.998f+.0031f;
                var point=new Vector2((s+d)*.5f,(s-d)*.5f);
                if(point.y>FoundryMap.South+.00001f||s>FoundryMap.BridgeEnd)continue;
                int owners=0;
                for(int t=0;t<triangles.Count;t+=3)
                {
                    Vector2 a=design[triangles[t]],b=design[triangles[t+1]],c=design[triangles[t+2]];
                    double area=Cross(b-a,c-a);
                    if(Math.Abs(area)<1e-8)continue;
                    double sign=area>0?1:-1;
                    if(sign*Cross(b-a,point-a)>=-1e-7&&sign*Cross(c-b,point-b)>=-1e-7&&sign*Cross(a-c,point-c)>=-1e-7)owners++;
                }
                Assert.That(owners,Is.GreaterThanOrEqualTo(1),$"No paving hole at deck {s},{d}.");
            }
        }

        [Test]
        public void ContinuationIsLaidOnceFromTheWayToTheEntranceThreshold()
        {
            Type view=typeof(FoundryRegionView),type=view.GetNestedType("Builder",BindingFlags.NonPublic);
            object mesh=Activator.CreateInstance(type,true);
            view.GetMethod("BuildBridgeDeck",Static).Invoke(null,new[]{mesh});
            var vertices=(List<Vector3>)type.GetField("V",Fields).GetValue(mesh);
            var triangles=(List<int>)type.GetField("T",Fields).GetValue(mesh);
            Vector2 end=new Vector2(-17.5f,-17.5f),centre=new Vector2(-3,-5);
            Vector2 direction=(end-centre).normalized,side=new Vector2(-direction.y,direction.x);
            float threshold=FoundryMap.BridgeStart+.2f*1.41421356f;
            Vector2 far=new Vector2(threshold*.5f,threshold*.5f),across=new Vector2(FoundryMap.Axis,-FoundryMap.Axis);
            var contour=new[]{end-side*2.09f,far-across*2.09f,far+across*2.09f,end+side*2.09f};
            var design=new Vector2[vertices.Count];
            for(int i=0;i<vertices.Count;i++)
            {
                Vector3 p=vertices[i];
                design[i]=new Vector2((p.x+2*p.y)*FoundryMap.Axis,(-p.x+2*p.y)*FoundryMap.Axis);
            }
            // Non-lattice probes cover the full width, both end courses and each side of the oblique bridge/court seam,
            // avoiding shared triangle edges: exactly one laid face owns each.
            for(int row=0;row<19;row++)foreach(float width in new[]{.0067f,.1733f,.5417f,.8123f,.9931f})
            {
                float along=.0083f+row*.05417f;
                Vector2 left=Vector2.Lerp(contour[0],contour[1],along),right=Vector2.Lerp(contour[3],contour[2],along);
                Vector2 point=Vector2.Lerp(left,right,width);int owners=0;
                for(int i=0;i<triangles.Count;i+=3)
                {
                    Vector2 a=design[triangles[i]],b=design[triangles[i+1]],c=design[triangles[i+2]];
                    double area=Cross(b-a,c-a);
                    if(Math.Abs(area)<1e-10)continue;
                    double sign=area>0?1:-1;
                    if(sign*Cross(b-a,point-a)>=-1e-7&&sign*Cross(c-b,point-b)>=-1e-7&&sign*Cross(a-c,point-c)>=-1e-7)owners++;
                }
                Assert.That(owners,Is.EqualTo(1),$"Expected the continuation laid once at {point}.");
            }
        }

        private static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
    }
}
