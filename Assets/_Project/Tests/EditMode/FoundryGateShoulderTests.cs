using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateShoulderTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private const float U0=32.18f+FoundryMap.GateOffset,U1=33.82f+FoundryMap.GateOffset;
        private const float Height=6.5f,Bottom=Height+.72f,Top=Height+1.16f,Inset=.065f,Shade=.99f;

        [TestCase(-.68f,3.35f)]
        [TestCase(6.65f,10.68f)]
        public void ProductionHeaderUsesClosedWeatheredShouldersInsideItsOriginalBearing(float v0,float v1)
        {
            object shoulder=Build("GateHeaderShoulder",v0,v1),old=Build("GateCraftTier",v0,v1);
            var points=Data<Vector3>(shoulder,"V");var oldPoints=Data<Vector3>(old,"V");
            object shell=Builder();Call("LitFoundryTier",shell,U0,v0,U1,v1,Bottom,Top,Inset);
            var original=Data<Vector3>(shell,"V");
            for(int face=0;face<3;face++)for(int end=0;end<2;end++)
                AssertPoint(points,original[face*4+end]);
            // Support-function containment tests the retained complete projected
            // envelope; no new roof or gable may push outside the old solid tier.
            foreach(Vector3 point in points)for(int step=0;step<96;step++)
            {
                var axis=new Vector3(Mathf.Cos(step*Mathf.PI/48),Mathf.Sin(step*Mathf.PI/48),0);
                float limit=float.NegativeInfinity;
                foreach(Vector3 p in original)limit=Mathf.Max(limit,Vector3.Dot(p,axis));
                Assert.That(Vector3.Dot(point,axis),Is.LessThanOrEqualTo(limit+.00002f));
            }
            float a=U0+Inset,c=U1-Inset,b=v0+Inset,d=v1-Inset,middle=(a+c)*.5f;
            AssertPoint(points,RoofPoint(new Vector2(middle,b)));
            AssertPoint(points,RoofPoint(new Vector2(middle,d)));
            AssertPoint(points,RoofPoint(new Vector2(a,b+.135f)));
            AssertPoint(points,RoofPoint(new Vector2(c,d-.135f)));
            Assert.That(points.Count-oldPoints.Count,Is.LessThanOrEqualTo(500),
                "Two shoulder replacements together may not consume more than 1000 extra vertices.");
            object cornice=Builder();Call("GateHeaderCornice",cornice,32.08f+FoundryMap.GateOffset,-.78f,
                33.92f+FoundryMap.GateOffset,10.78f,Height+.43f,Bottom,.06f,1.02f);
            object actual=Builder();Call("BuildGateHeaderFinish",actual,Height);
            int prefix=Data<Vector3>(cornice,"V").Count;
            AssertRange(cornice,actual,0);
            if(v0>0)prefix+=Data<Vector3>(Build("GateHeaderShoulder",-.68f,3.35f),"V").Count;
            AssertRange(shoulder,actual,prefix);
            // The shoulder pair remains outside the retained recessed lock.
            Vector3 lockPoint=(Vector3)Call("GateCartouchePoint",new Vector2(0,.995f),32.58f+FoundryMap.GateOffset,6.5f);
            Assert.That(Owners(shoulder,lockPoint),Is.Zero);
            TestContext.WriteLine($"Shoulder {v0}: old {oldPoints.Count}, new {points.Count}, delta {points.Count-oldPoints.Count} vertices.");
        }

        [TestCase(-.68f,3.35f)]
        [TestCase(6.65f,10.68f)]
        public void ActualPitchedRoofHasOneOwnerAcrossItsRidgeAndBroadMortarBeds(float v0,float v1)
        {
            object mesh=Build("GateHeaderShoulder",v0,v1);
            var points=Data<Vector3>(mesh,"V");var indices=Data<int>(mesh,"T");
            var halves=RoofHalves(v0,v1);
            foreach(var half in halves)
            {
                double expected=PolygonArea(half),covered=0;
                for(int i=0;i<indices.Count;i+=3)
                {
                    Vector3 a=points[indices[i]],b=points[indices[i+1]],c=points[indices[i+2]];
                    if(!Inside(half,a)||!Inside(half,b)||!Inside(half,c))continue;
                    covered+=Math.Abs(Area(a,b,c))*.5;
                }
                Assert.That(covered,Is.EqualTo(expected).Within(.00005),
                    "Stone and mortar partition the complete roof; no retained flat cap is allowed underneath.");
            }
            float left=U0+Inset,right=U1-Inset,low=v0+Inset,high=v1-Inset;
            for(int row=0;row<17;row++)for(int column=0;column<13;column++)
            {
                float u=Mathf.Lerp(left+.14f,right-.14f,(column+.317f)/13);
                float v=Mathf.Lerp(low+.14f,high-.14f,(row+.583f)/17);
                Assert.That(Owners(mesh,RoofPoint(new Vector2(u,v))),Is.EqualTo(1),
                    "Actual pitched interiors must not have gaps or duplicate owners.");
            }
            float mid=(left+right)*.5f,uProbe=(left+mid)*.5f;
            for(int bed=1;bed<3;bed++)
            {
                float v=Mathf.Lerp(low,high,bed/3f);
                Color joint=Sample(mesh,RoofPoint(new Vector2(uProbe,v)));
                Color stone=Sample(mesh,RoofPoint(new Vector2(uProbe,v+.04f)));
                Assert.That(joint.grayscale,Is.LessThan(stone.grayscale*.80f),
                    "Broad cap stones meet at real separately owned mortar, not an overlay seam.");
            }
        }

        [TestCase(-.68f,3.35f)]
        [TestCase(6.65f,10.68f)]
        public void WholeShoulderSharesMineralGrainAndKeepsItsRoofBelowExistingStoneValue(float v0,float v1)
        {
            object mesh=Build("GateHeaderShoulder",v0,v1),shell=Builder();
            Call("LitFoundryTier",shell,U0,v0,U1,v1,Bottom,Top,Inset);
            var points=Data<Vector3>(mesh,"V");var uv=Data<Vector2>(mesh,"U");var colors=Data<Color>(mesh,"C");
            var indices=Data<int>(mesh,"T");var halves=RoofHalves(v0,v1);
            Color cap=Data<Color>(shell,"C")[12]*Shade;float scale=0;int visibleRoof=0;
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(colors[i].a,Is.EqualTo(1));
                Assert.That(uv[i].x,Is.InRange(.03499f,.39001f));Assert.That(uv[i].y,Is.InRange(.43799f,.56301f));
                float distance=Vector3.Distance(points[0],points[i]);
                if(distance>.1f)
                {
                    float actual=Vector2.Distance(uv[0],uv[i])/distance;
                    if(scale==0)scale=actual;
                    Assert.That(actual,Is.EqualTo(scale).Within(.00001f));
                }
            }
            for(int i=0;i<indices.Count;i+=3)
            {
                int a=indices[i],b=indices[i+1],c=indices[i+2];
                if(Math.Abs(Area(points[a],points[b],points[c]))<1e-8)continue;
                Assert.That(Math.Abs(Area(uv[a],uv[b],uv[c])),Is.GreaterThan(1e-12));
                bool roof=false;foreach(var half in halves)roof|=Inside(half,points[a])&&Inside(half,points[b])&&Inside(half,points[c]);
                if(!roof)continue;visibleRoof++;
                foreach(int vertex in new[]{a,b,c})
                {
                    Assert.That(colors[vertex].r,Is.LessThanOrEqualTo(cap.r*.99f));
                    Assert.That(colors[vertex].g,Is.LessThanOrEqualTo(cap.g*.99f));
                    Assert.That(colors[vertex].b,Is.LessThanOrEqualTo(cap.b*.99f));
                }
            }
            Assert.That(visibleRoof,Is.GreaterThan(12));
        }

        private static List<Vector2>[] RoofHalves(float v0,float v1)
        {
            float a=U0+Inset,c=U1-Inset,b=v0+Inset,d=v1-Inset,m=(a+c)*.5f;const float cut=.135f;
            var left=new[]{new Vector2(a+cut,b),new Vector2(m,b),new Vector2(m,d),new Vector2(a+cut,d),new Vector2(a,d-cut),new Vector2(a,b+cut)};
            var right=new[]{new Vector2(m,b),new Vector2(c-cut,b),new Vector2(c,b+cut),new Vector2(c,d-cut),new Vector2(c-cut,d),new Vector2(m,d)};
            var result=new[]{new List<Vector2>(),new List<Vector2>()};
            foreach(Vector2 p in left)result[0].Add(RoofPoint(p));foreach(Vector2 p in right)result[1].Add(RoofPoint(p));return result;
        }
        private static Vector3 RoofPoint(Vector2 p)
        {
            float left=U0+Inset,right=U1-Inset,middle=(left+right)*.5f;
            float rise=p.x<=middle?.12f*(p.x-left)/(middle-left):.12f*(right-p.x)/(right-middle);
            FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
            return new Vector3(x,y*.5f+1.25f+Top-.12f+rise,0);
        }
        private static bool Inside(List<Vector2> polygon,Vector2 p)
        {
            for(int i=0;i<polygon.Count;i++)if(Area(polygon[i],polygon[(i+1)%polygon.Count],p)<-.00001)return false;return true;
        }
        private static double PolygonArea(List<Vector2> polygon)
        {double area=0;for(int i=1;i+1<polygon.Count;i++)area+=Math.Abs(Area(polygon[0],polygon[i],polygon[i+1]))*.5;return area;}
        private static int Owners(object mesh,Vector2 p)
        {
            var points=Data<Vector3>(mesh,"V");var indices=Data<int>(mesh,"T");int count=0;
            for(int i=0;i<indices.Count;i+=3)if(Weights(points[indices[i]],points[indices[i+1]],points[indices[i+2]],p,out _))count++;return count;
        }
        private static Color Sample(object mesh,Vector2 p)
        {
            var points=Data<Vector3>(mesh,"V");var indices=Data<int>(mesh,"T");var colors=Data<Color>(mesh,"C");
            for(int i=0;i<indices.Count;i+=3)
            {int a=indices[i],b=indices[i+1],c=indices[i+2];if(Weights(points[a],points[b],points[c],p,out Vector3 w))return colors[a]*w.x+colors[b]*w.y+colors[c]*w.z;}
            Assert.Fail("Expected roof sample has no actual owner.");return Color.clear;
        }
        private static bool Weights(Vector2 a,Vector2 b,Vector2 c,Vector2 p,out Vector3 weights)
        {
            weights=Vector3.zero;double area=Area(a,b,c);if(Math.Abs(area)<1e-10)return false;
            double x=Area(p,b,c)/area,y=Area(a,p,c)/area,z=1-x-y;
            weights=new Vector3((float)x,(float)y,(float)z);return x>1e-8&&y>1e-8&&z>1e-8;
        }
        private static void AssertRange(object expected,object actual,int offset)
        {
            int count=Data<Vector3>(expected,"V").Count;
            CollectionAssert.AreEqual(Data<Vector3>(expected,"V"),Data<Vector3>(actual,"V").GetRange(offset,count));
            CollectionAssert.AreEqual(Data<Vector2>(expected,"U"),Data<Vector2>(actual,"U").GetRange(offset,count));
            CollectionAssert.AreEqual(Data<Color>(expected,"C"),Data<Color>(actual,"C").GetRange(offset,count));
        }
        private static void AssertPoint(List<Vector3> points,Vector3 target)=>Assert.That(points.Exists(p=>Vector3.Distance(p,target)<.00002f),Is.True);
        private static double Area(Vector2 a,Vector2 b,Vector2 c)=>((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
        private static object Build(string method,float v0,float v1)
        {object b=Builder();Call(method,b,U0,v0,U1,v1,Bottom,Top,Inset,Shade);return b;}
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Call<T>(string method,params object[] args)=>(T)typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,args);
        private static object Call(string method,params object[] args)=>Call<object>(method,args);
        private static List<T> Data<T>(object mesh,string name)=>(List<T>)mesh.GetType().GetField(name,Fields).GetValue(mesh);
    }
}
