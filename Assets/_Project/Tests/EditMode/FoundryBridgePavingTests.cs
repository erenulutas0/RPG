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

        [Test]
        public void FittedContinuationPartitionsTheActualThresholdAndApproachWithNoncollapsedWorldGrain()
        {
            Type view=typeof(FoundryRegionView),type=view.GetNestedType("Builder",BindingFlags.NonPublic);
            object mesh=Activator.CreateInstance(type,true);
            view.GetMethod("BuildCourtBridgeStone",Static).Invoke(null,new[]{mesh});
            var vertices=(List<Vector3>)type.GetField("V",Fields).GetValue(mesh);
            var uv=(List<Vector2>)type.GetField("U",Fields).GetValue(mesh);
            var triangles=(List<int>)type.GetField("T",Fields).GetValue(mesh);
            Assert.That(vertices.Count,Is.InRange(1,3000));
            Assert.That(uv.Count,Is.EqualTo(vertices.Count));
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
                Vector2 d=design[i];
                Assert.That(p.z,Is.Zero);
                Assert.That(d.x+d.y,Is.GreaterThanOrEqualTo(threshold-.00002f),"The fitted floor stops at the actual entrance kerb.");
                for(int edge=0;edge<4;edge++)
                    Assert.That(Cross(contour[(edge+1)%4]-contour[edge],d-contour[edge]),Is.GreaterThanOrEqualTo(-.00008),
                        "No inset or tapered course can project outside the exact bridge field.");
                FoundryMap.ToFloor(d.x,d.y,out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.True);
                Vector2 expected=new Vector2(1+2*FoundryMap.Axis*d.x/FoundryRegionView.PavingGrainSpan,
                    1+2*FoundryMap.Axis*d.y/FoundryRegionView.PavingGrainSpan);
                Assert.That(Vector2.Distance(uv[i],expected),Is.LessThan(.00001f));
            }
            double expectedArea=0,actualArea=0;
            for(int i=0;i<4;i++)expectedArea+=Cross(contour[i],contour[(i+1)%4]);
            for(int i=0;i<triangles.Count;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                double area=Math.Abs(Cross(design[b]-design[a],design[c]-design[a]));
                actualArea+=area;
                if(area>1e-7)
                    Assert.That(Math.Abs(Cross(uv[b]-uv[a],uv[c]-uv[a])),Is.GreaterThan(1e-12),
                        "Every visible fitted face retains a two-dimensional mineral sample.");
            }
            Assert.That(actualArea,Is.EqualTo(Math.Abs(expectedArea)).Within(.003),
                "Faces, dressed shoulders and joints partition the new quadrilateral exactly once.");
            // Non-lattice probes cover the full width, both end courses and each
            // side of the oblique bridge/court seam, avoiding shared triangle edges.
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
                Assert.That(owners,Is.EqualTo(1),$"Expected one complete fitted stone field at {point}.");
            }
        }

        private static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
    }
}
