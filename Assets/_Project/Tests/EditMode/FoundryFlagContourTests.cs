using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryFlagContourTests
    {
        private const float RampStart=FoundryMap.GateLandingStart+1f,RampEnd=FoundryMap.GateLandingStart+3f;
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static object Call(string name,params object[] args)
        {
            var method=typeof(FoundryRegionView).GetMethod(name,Static);
            Assert.That(method,Is.Not.Null,name);
            return method.Invoke(null,args);
        }

        [Test]
        public void QuarryAbrasionStaysOnWorkedLipsAndLeavesBroadInteriorsUntouched()
        {
            const float u0=-10f,v0=-6f,u1=-7f,v1=-4f;
            float Rub(Vector2 p,int seed,float exposure)=>(float)Call("QuarryEdgeAbrasion",p,u0,v0,u1,v1,seed,seed*7,exposure);
            for(int seed=-20;seed<=20;seed++)
            {
                foreach(Vector2 inside in new[]{new Vector2(-9.5f,-5.5f),new Vector2(-8.5f,-5f),new Vector2(-7.5f,-4.5f)})
                    Assert.That(Rub(inside,seed,1),Is.Zero,"A rubbed edge must not become a cloudy wash across a broad slab.");
                float strongest=0;
                for(int side=0;side<4;side++)for(int sample=0;sample<=16;sample++)
                {
                    float t=sample/16f;
                    var point=side==0?new Vector2(Mathf.Lerp(u0,u1,t),v0):
                        side==1?new Vector2(u1,Mathf.Lerp(v0,v1,t)):
                        side==2?new Vector2(Mathf.Lerp(u0,u1,t),v1):new Vector2(u0,Mathf.Lerp(v0,v1,t));
                    Assert.That(Rub(point,seed,.16f),Is.Zero,"Unworked quarry edges stay dressed.");
                    float value=Rub(point,seed,1);
                    Assert.That(value,Is.InRange(0,1));strongest=Mathf.Max(strongest,value);
                }
                Assert.That(strongest,Is.GreaterThan(.85f),"A worked slab exposes one actual mineral lip.");
            }
        }

        [TestCase(.5f,2f)]
        [TestCase(1f,1f)]
        [TestCase(1.5f,2f)]
        [TestCase(2.5f,1f)]
        [TestCase(2.5f,2f)]
        [TestCase(3f,2f)]
        public void ProductionSizedContoursAndInsetsStayConvexAndInsideTheirPackedFlag(float width,float height)
        {
            Vector2[] first=null;
            bool varied=false;
            for(int seed=-48;seed<=48;seed++)
            {
                float u0=seed*.5f,v0=-7.5f,area=width*height;
                float joint=.011f+.0025f*Mathf.Sqrt(area);
                float bevel=Mathf.Min(.025f+.007f*Mathf.Sqrt(area),.12f*Mathf.Min(width,height));
                var outer=(Vector2[])Call("FlagContour",u0,v0,u0+width,v0+height,joint,seed,seed*7);
                var again=(Vector2[])Call("FlagContour",u0,v0,u0+width,v0+height,joint,seed,seed*7);
                CollectionAssert.AreEqual(outer,again,"Fixed packed coordinates and seeds reproduce the contour.");
                AssertConvex(outer);
                foreach(var p in outer)
                {
                    Assert.That(p.x,Is.InRange(u0+joint-.00001f,u0+width-joint+.00001f));
                    Assert.That(p.y,Is.InRange(v0+joint-.00001f,v0+height-joint+.00001f));
                }
                var inner=(Vector2[])Call("InsetFlagContour",outer,Mathf.Max(.008f,bevel-joint));
                AssertConvex(inner);
                Assert.That(Area(inner),Is.LessThan(Area(outer)));
                Assert.That(inner.Length,Is.EqualTo(outer.Length),"Bevel correspondence must survive tiny corner cuts.");
                for(int k=0;k<outer.Length;k++)
                {
                    Vector2 edge=outer[(k+1)%outer.Length]-outer[k];
                    Vector2 inside=inner[(k+1)%inner.Length]-inner[k];
                    Assert.That(Vector2.Dot(edge,inside),Is.GreaterThan(0),"A small chamfer must not reverse under the bevel offset.");
                    foreach(var p in inner)
                        Assert.That(Cross(edge,p-outer[k]),Is.GreaterThanOrEqualTo(-.00001),"Inset remains in every outer half-plane.");
                }
                var relative=new Vector2[outer.Length];
                for(int k=0;k<outer.Length;k++)relative[k]=outer[k]-new Vector2(u0,v0);
                if(first==null)first=relative;
                else for(int k=0;k<relative.Length;k++)if(Vector2.Distance(first[k],relative[k])>.001f)varied=true;
            }
            Assert.That(varied,Is.True,"Seeds should not return identical translated octagons.");
        }

        [Test]
        public void CellClippingHandlesKnownAreasNegativeCoordinatesAndNearBoundarySlivers()
        {
            Vector2 centre=new Vector2(-2.25f,3.5f);
            var diamond=new[]{centre+Vector2.down,centre+Vector2.right,centre+Vector2.up,centre+Vector2.left};
            var quarter=(List<Vector2>)Call("ClipFlagCell",diamond,centre.x,centre.y,centre.x+1,centre.y+1);
            Assert.That(Area(quarter),Is.EqualTo(.5).Within(.000001));
            Assert.That(((List<Vector2>)Call("ClipFlagCell",diamond,10f,10f,11f,11f)).Count,Is.Zero);
            var near=new[]{new Vector2(-.001f,-.001f),new Vector2(.999f,-.001f),new Vector2(.999f,.999f),new Vector2(-.001f,.999f)};
            var clipped=(List<Vector2>)Call("ClipFlagCell",near,0f,0f,1f,1f);
            Assert.That(Area(clipped),Is.EqualTo(.999*.999).Within(.000001));
        }

        [TestCase(.5f)]
        [TestCase(1f)]
        public void AdjacentLightCellsPartitionTheInnerContourWithoutLostOrDoubledArea(float step)
        {
            for(int seed=-12;seed<=12;seed++)
            {
                const float u0=-6.5f,v0=-3f,u1=-4f,v1=-2f;
                var outer=(Vector2[])Call("FlagContour",u0,v0,u1,v1,.016f,seed,seed*3);
                var inner=(Vector2[])Call("InsetFlagContour",outer,.025f);
                double total=0;
                for(float u=u0;u<u1;u+=step)for(float v=v0;v<v1;v+=step)
                {
                    float right=Mathf.Min(u+step,u1),top=Mathf.Min(v+step,v1);
                    var cell=(List<Vector2>)Call("ClipFlagCell",inner,u,v,right,top);
                    if(cell.Count<3)continue;
                    Assert.That(Area(cell),Is.GreaterThanOrEqualTo(0),"Cell clipping retains winding.");
                    foreach(var p in cell)
                    {
                        Assert.That(p.x,Is.InRange(u-.00001f,right+.00001f));
                        Assert.That(p.y,Is.InRange(v-.00001f,top+.00001f));
                        for(int k=0;k<inner.Length;k++)
                            Assert.That(Cross(inner[(k+1)%inner.Length]-inner[k],p-inner[k]),Is.GreaterThanOrEqualTo(-.00001));
                    }
                    total+=Area(cell);
                }
                Assert.That(total,Is.EqualTo(Area(inner)).Within(.00001),"Light-cell clipping must neither leave holes nor duplicate face area.");
            }
        }

        [Test]
        public void ProtectedFlagsAndCrossRampSeamsKeepTheirDressedPath()
        {
            bool Use(int zone,bool repair,float u0,float u1,float v1=2)=>(bool)Call("UseFlagContour",zone,repair,u0,0f,u1,v1);
            Assert.That(Use(0,false,4,5),Is.True);
            Assert.That(Use(3,false,-25,-24),Is.True);
            Assert.That(Use(1,false,0,1),Is.False,"Ceremonial flags remain regular.");
            Assert.That(Use(0,true,4,5),Is.False,"Service repairs retain their dressed contours.");
            Assert.That(Use(0,false,4,4.5f,1),Is.False,"Small infill remains dressed.");
            Assert.That(Use(2,false,RampStart-.5f,RampStart+.5f),Is.False,"A face must not bridge the ramp foot.");
            Assert.That(Use(2,false,RampEnd-.5f,RampEnd+.5f),Is.False,"A face must not bridge the ramp crest.");
            Assert.That(Use(2,false,RampStart,RampStart+1),Is.True,"A wholly planar ramp cell can be contoured.");
            Assert.That(Use(2,false,RampEnd,RampEnd+1),Is.True,"The raised landing can be contoured.");
        }

        [TestCase(-25f,6f,2f,1.5f,3,false)]
        [TestCase(RampStart-.5f,3f,1f,1f,2,false)]
        [TestCase(RampEnd-.5f,3f,1f,1f,2,false)]
        [TestCase(RampStart,3f,1f,1f,2,false)]
        [TestCase(0f,0f,1f,1f,1,false)]
        [TestCase(-24f,6f,1f,1f,3,true)]
        [TestCase(1f,-10f,3f,2f,0,false)]
        public void ProductionMeshRetainsWholeBedContinuousUvsAndFlushTriangles(float u0,float v0,float width,float height,int zone,bool repair)
        {
            Type view=typeof(FoundryRegionView),builderType=view.GetNestedType("Builder",BindingFlags.NonPublic);
            Type stoneType=view.GetNestedType("AshlarStone",BindingFlags.NonPublic);
            Assert.That(builderType,Is.Not.Null);Assert.That(stoneType,Is.Not.Null);
            object builder=Activator.CreateInstance(builderType,true),stone=Activator.CreateInstance(stoneType);
            void Set(string field,int value)=>stoneType.GetField(field,Fields).SetValue(stone,value);
            Set("I",Mathf.RoundToInt(u0*2));Set("J",Mathf.RoundToInt(v0*2));
            Set("W",Mathf.RoundToInt(width*2));Set("H",Mathf.RoundToInt(height*2));Set("Zone",zone);
            Call("AshlarFlagFace",builder,stone,repair);
            var vertices=(List<Vector3>)builderType.GetField("V",Fields).GetValue(builder);
            var uv=(List<Vector2>)builderType.GetField("U",Fields).GetValue(builder);
            var triangles=(List<int>)builderType.GetField("T",Fields).GetValue(builder);
            Assert.That(vertices.Count,Is.GreaterThan(0));Assert.That(uv.Count,Is.EqualTo(vertices.Count));
            Assert.That(triangles.Count%3,Is.Zero);
            var design=new Vector2[vertices.Count];
            for(int i=0;i<vertices.Count;i++)
            {
                Assert.That(float.IsNaN(vertices[i].x)||float.IsNaN(vertices[i].y)||float.IsInfinity(vertices[i].x)||float.IsInfinity(vertices[i].y),Is.False);
                design[i]=new Vector2((uv[i].x-1)*FoundryRegionView.PavingGrainSpan/(2*FoundryMap.Axis),(uv[i].y-1)*FoundryRegionView.PavingGrainSpan/(2*FoundryMap.Axis));
                Assert.That(design[i].x,Is.InRange(u0-.0001f,u0+width+.0001f));
                Assert.That(design[i].y,Is.InRange(v0-.0001f,v0+height+.0001f));
                Assert.That(Vector3.Distance(vertices[i],SurfacePoint(design[i].x,design[i].y)),Is.LessThan(.0001f));
            }
            bool contour=(bool)Call("UseFlagContour",zone,repair,u0,v0,u0+width,v0+height);
            for(int t=0;t<triangles.Count;t+=3)
            {
                int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                foreach(int index in new[]{a,b,c})Assert.That(index,Is.InRange(0,vertices.Count-1));
                double winding=Cross(design[b]-design[a],design[c]-design[a]);
                Assert.That(Math.Abs(winding),Is.GreaterThan(1e-9),"No degenerate triangles.");
                if(contour)Assert.That(winding,Is.GreaterThan(0),"Contoured bed, bevel and face share CCW winding.");
                Vector2 p=(design[a]+design[b]+design[c])/3;
                Vector3 centre=(vertices[a]+vertices[b]+vertices[c])/3;
                Assert.That(Vector3.Distance(centre,SurfacePoint(p.x,p.y)),Is.LessThan(.0001f),"Triangle interiors stay flush, including both ramp seams.");
            }
            for(int x=0;x<=12;x++)for(int y=0;y<=12;y++)
            {
                Vector2 p=new Vector2(u0+width*x/12,v0+height*y/12);
                bool covered=false;
                for(int t=0;t<triangles.Count&&!covered;t+=3)
                    covered=InTriangle(p,design[triangles[t]],design[triangles[t+1]],design[triangles[t+2]]);
                Assert.That(covered,Is.True,$"Complete mortar coverage at {p}, including removed corners and footprint boundaries.");
            }
        }

        private static void AssertConvex(IList<Vector2> points)
        {
            Assert.That(points.Count,Is.GreaterThanOrEqualTo(3));
            Assert.That(Area(points),Is.GreaterThan(0));
            for(int i=0;i<points.Count;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%points.Count],c=points[(i+2)%points.Count];
                Assert.That(float.IsNaN(a.x)||float.IsNaN(a.y)||float.IsInfinity(a.x)||float.IsInfinity(a.y),Is.False);
                Assert.That(Vector2.Distance(a,b),Is.GreaterThan(.00001f));
                Assert.That(Cross(b-a,c-b),Is.GreaterThan(0),"Strict convex CCW contour.");
            }
        }
        private static Vector3 SurfacePoint(float u,float v)=>
            (Vector3)typeof(FoundryRegionView).Assembly.GetType("Cryptforge.UI.FoundrySurface")
                .GetMethod("Project",Static).Invoke(null,new object[]{u,v});
        private static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        private static double Area(IList<Vector2> points)
        {
            if(points.Count<3)return 0;
            double area=0;Vector2 origin=points[0];
            for(int i=1;i+1<points.Count;i++)area+=Cross(points[i]-origin,points[i+1]-origin);
            return area*.5;
        }
        private static bool InTriangle(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {
            double x=Cross(b-a,p-a),y=Cross(c-b,p-b),z=Cross(a-c,p-c);
            const double tolerance=.00002;
            return x>=-tolerance&&y>=-tolerance&&z>=-tolerance||x<=tolerance&&y<=tolerance&&z<=tolerance;
        }
    }
}
