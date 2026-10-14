using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateCurtainDepthTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(false)]
        [TestCase(true)]
        public void ActiveCurtainUsesCutBayAndPreservesAllOriginalCapAndBearingStations(bool far)
        {
            Bounds(far,out float low,out float high,out Vector4 box,out Vector4 roof);
            object upper=Builder();Call("GateCurtainUpperTier",upper,box,far);
            object shell=Shell(box);var source=Data<Vector3>(shell,"V");
            var points=Data<Vector3>(upper,"V");int tail=points.Count-36;
            CollectionAssert.AreEqual(source.GetRange(8,36),points.GetRange(tail,36),
                "The existing closed chamfer and eight cap triangles stay exact.");
            CollectionAssert.AreEqual(Data<Vector2>(shell,"U").GetRange(8,36),Data<Vector2>(upper,"U").GetRange(tail,36));
            for(int i=0;i<8;i++)AssertPoint(points,source[i]);
            object prefix=Builder();Prefix(prefix,low,high);
            object actual=Builder();Call("GateCurtainWall",actual,far);
            int offset=Data<Vector3>(prefix,"V").Count;
            AssertRange(prefix,actual,0);AssertRange(upper,actual,offset);
            object old=Builder();Prefix(old,low,high);
            Call("GateEnclosureTier",old,box.x,box.y,box.z,box.w,3.13f,6.55f,.05f,.85f,false);
            Call("GateEnclosureTier",old,G(32.02f),low+.02f,G(36),high-.02f,6.55f,6.82f,.045f,.98f,true);
            Call("GateEnclosureTier",old,roof.x,roof.y,roof.z,roof.w,6.82f,7.02f,.11f,.91f,true);
            Call("GateEnclosureRelief",old,box,3.13f,6.55f,.05f,true,far?.155f:.845f,.085f,3.53f,6.12f);
            int delta=Data<Vector3>(actual,"V").Count-Data<Vector3>(old,"V").Count;
            Assert.That(delta,Is.LessThanOrEqualTo(600),"The pair's construction scope adds at most 1200 vertices before root grain subdivision.");
            TestContext.WriteLine($"Curtain {far}: old {Data<Vector3>(old,"V").Count}, new {Data<Vector3>(actual,"V").Count}, delta {delta} vertices.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActualBayKeepsItsThroatAndExposesARealBackWithoutAFrontOverlay(bool far)
        {
            Bounds(far,out _,out _,out Vector4 box,out _);
            object shell=Shell(box),front=Builder();Call("GateCurtainFront",front,shell,box,far);
            var points=Data<Vector3>(front,"V");var indices=Data<int>(front,"T");
            var inner=Opening(box,far,true);var outer=Opening(box,far,false);
            Vector3 inward=Project(box.x+.18f,box.y)-Project(box.x,box.y);
            foreach(Vector3 point in outer)AssertPoint(points,point);
            foreach(Vector3 point in inner){AssertPoint(points,point);AssertPoint(points,point+inward);}
            var throat=Polygon(inner);var rear=new List<(double x,double y)>();
            foreach(Vector3 point in inner)
            {
                // Match the physical float storage boundary of emitted vertices.
                // Scalar sums widened directly into the double tuple can retain
                // intermediate precision that a stored Vector3 cannot represent.
                Vector3 shifted=point+inward;rear.Add((shifted.x,shifted.y));
            }
            var visible=Clip(rear,throat);double expected=Math.Abs(PolygonArea(visible));
            Assert.That(expected,Is.GreaterThan(.15),"A narrow decorative line is not a useful blind bay.");
            double backing=0,overpaint=0,frontArea=0;
            for(int t=0;t<indices.Count;t+=3)
            {
                var triangle=new List<(double x,double y)>();
                for(int k=0;k<3;k++){Vector3 p=points[indices[t+k]];triangle.Add((p.x,p.y));}
                double area=Math.Abs(PolygonArea(triangle));if(area<=1e-12)continue;
                double overlap=Math.Abs(PolygonArea(Clip(visible,triangle)));
                if(indices[t]<20)backing+=overlap;else overpaint+=overlap;
                if(indices[t]>=40)frontArea+=area;
            }
            Assert.That(backing,Is.EqualTo(expected).Within(1e-8));
            Assert.That(overpaint,Is.LessThan(1e-8),"No later return/body/frame may cover the visible backing.");
            var original=Data<Vector3>(shell,"V");
            var parent=new[]{original[5],original[4],original[7],original[6]};
            double wall=Math.Abs(PolygonArea(Polygon(parent))),opening=Math.Abs(PolygonArea(throat));
            Assert.That(frontArea,Is.EqualTo(wall-opening).Within(.00008),
                "Front stones partition only the old wall outside its original opening; no solid bed is retained.");
            var uv=Data<Vector2>(front,"U");float scale=0;
            for(int i=1;i<points.Count;i++)
            {
                float distance=Vector3.Distance(points[0],points[i]);if(distance<.05f)continue;
                float actual=Vector2.Distance(uv[0],uv[i])/distance;
                if(scale==0)scale=actual;
                Assert.That(actual,Is.EqualTo(scale).Within(.00002f),"Body, frame and returns retain one parent mineral field.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PitchedCurtainCopingHasSupportedCornersClosedGablesAndOneRoofOwner(bool far)
        {
            Bounds(far,out float low,out float high,out _,out Vector4 box);
            object mesh=Builder();Call("GateCurtainRoof",mesh,box);
            object original=Builder();Call("LitFoundryTier",original,box.x,box.y,box.z,box.w,6.82f,7.02f,.11f);
            var points=Data<Vector3>(mesh,"V");var shell=Data<Vector3>(original,"V");
            for(int face=0;face<3;face++)for(int endpoint=0;endpoint<2;endpoint++)AssertPoint(points,shell[face*4+endpoint]);
            foreach(Vector3 p in points)for(int step=0;step<64;step++)
            {
                var direction=new Vector3(Mathf.Cos(step*Mathf.PI/32),Mathf.Sin(step*Mathf.PI/32),0);
                float bound=float.NegativeInfinity;foreach(Vector3 s in shell)bound=Mathf.Max(bound,Vector3.Dot(s,direction));
                Assert.That(Vector3.Dot(p,direction),Is.LessThanOrEqualTo(bound+.00002f));
            }
            float left=box.x+.11f,right=box.z-.11f,bottom=box.y+.11f,top=box.w-.11f,middle=(left+right)*.5f;
            AssertPoint(points,RoofPoint(middle,bottom,left,right));AssertPoint(points,RoofPoint(middle,top,left,right));
            AssertPoint(points,RoofPoint(left,bottom+.135f,left,right));AssertPoint(points,RoofPoint(right,top-.135f,left,right));
            // Original upper-tier foot stations must rest on the actual lower cap.
            object seat=Builder();Call("LitFoundryTier",seat,G(32.02f),low+.02f,G(36),high-.02f,6.55f,6.82f,.045f);
            var seatPoints=Data<Vector3>(seat,"V");
            for(int face=0;face<3;face++)for(int endpoint=0;endpoint<2;endpoint++)
            {
                Vector3 p=shell[face*4+endpoint];bool supported=false;
                for(int i=12;i<seatPoints.Count;i+=4)supported|=InsideTriangle(p,seatPoints[i],seatPoints[i+1],seatPoints[i+2],-1e-5);
                Assert.That(supported,Is.True,"Every retained bearing corner sits on real lower coping.");
            }
            // Both pitched planes tile the roof. Samples avoid triangle boundaries;
            // the original flat cap would create duplicate coverage here.
            for(int row=0;row<19;row++)for(int column=0;column<17;column++)
            {
                float u=Mathf.Lerp(left+.15f,right-.15f,(column+.317f)/17);
                float v=Mathf.Lerp(bottom+.15f,top-.15f,(row+.619f)/19);
                Assert.That(Owners(mesh,RoofPoint(u,v,left,right)),Is.EqualTo(1));
            }
            // The front gable must close the vertical band below both slopes.
            for(int x=0;x<9;x++)
            {
                float t=(x+.39f)/9;
                Vector3 a=Vector3.Lerp(shell[0],shell[1],t),b=Vector3.Lerp(shell[3],shell[2],t);
                Vector3 floor=b-Vector3.up*7.02f;
                float u=((floor.x+(floor.y-1.25f)*2)*FoundryMap.Axis);
                b+=Vector3.up*(-.08f*Mathf.Abs((u-middle)/((right-left)*.5f)));
                Assert.That(Owners(mesh,Vector3.Lerp(a,b,.43f)),Is.EqualTo(1));
            }
        }

        private static void Bounds(bool far,out float low,out float high,out Vector4 box,out Vector4 roof)
        {low=far?13f:-6.6f;high=far?16.6f:-3f;box=new Vector4(G(32.2f),low+.16f,G(35.84f),high-.16f);roof=new Vector4(G(32.15f),low+.14f,G(35.87f),high-.14f);}
        private static object Shell(Vector4 box)
        {object b=Builder();Call("LitFoundryTier",b,box.x,box.y,box.z,box.w,3.13f,6.55f,.05f);var colors=Data<Color>(b,"C");for(int i=0;i<colors.Count;i++){Color c=colors[i]*.85f;c.a=1;colors[i]=c;}return b;}
        private static void Prefix(object b,float low,float high)
        {
            Call("GateEnclosureTier",b,G(32),low,G(36),high,0f,.46f,.06f,.91f,true);
            Call("GateEnclosureTier",b,G(32.16f),low+.12f,G(35.88f),high-.12f,.46f,2.9f,.035f,.82f,false);
            Call("GateEnclosureTier",b,G(32.05f),low+.05f,G(35.98f),high-.05f,2.9f,3.13f,.025f,.94f,true);
        }
        private static Vector3[] Opening(Vector4 box,bool far,bool inner)
        {
            float centre=far?.155f:.845f,h=6.12f-3.53f;
            var p=new[]{new Vector2(centre-.085f,3.53f),new Vector2(centre+.085f,3.53f),
                new Vector2(centre+.085f,6.12f-h*.27f),new Vector2(centre,6.12f),new Vector2(centre-.085f,6.12f-h*.27f)};
            var result=new Vector3[5];
            for(int i=0;i<5;i++)
            {
                Vector2 at=p[i];if(inner)at=new Vector2(Mathf.Lerp(at.x,centre,.25f),Mathf.Lerp(at.y,3.53f+h*.46f,.13f));
                float taper=.05f*Mathf.InverseLerp(3.13f,6.55f,at.y);
                result[i]=Project(box.x+taper,Mathf.Lerp(box.y+taper,box.w-taper,at.x))+Vector3.up*at.y;
            }
            return result;
        }
        private static Vector3 RoofPoint(float u,float v,float left,float right)
        {float z=7.02f-.08f*Mathf.Abs((u-(left+right)*.5f)/((right-left)*.5f));return Project(u,v)+Vector3.up*z;}
        private static Vector3 Project(float u,float v)
        {FoundryMap.ToFloor(u,v,out float x,out float y);return new Vector3(x,y*.5f+1.25f,0);}
        private static float G(float u)=>u+FoundryMap.GateOffset;
        private static int Owners(object mesh,Vector2 p)
        {var v=Data<Vector3>(mesh,"V");var t=Data<int>(mesh,"T");int n=0;for(int i=0;i<t.Count;i+=3)if(InsideTriangle(p,v[t[i]],v[t[i+1]],v[t[i+2]],1e-8))n++;return n;}
        private static bool InsideTriangle(Vector2 p,Vector2 a,Vector2 b,Vector2 c,double margin)
        {double area=Cross(a,b,c);if(Math.Abs(area)<1e-10)return false;double x=Cross(p,b,c)/area,y=Cross(a,p,c)/area;return x>=margin&&y>=margin&&1-x-y>=margin;}
        private static double Cross(Vector2 a,Vector2 b,Vector2 c)=>((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
        private static List<(double x,double y)> Polygon(Vector3[] p)
        {var result=new List<(double x,double y)>();foreach(Vector3 v in p)result.Add((v.x,v.y));return result;}
        private static double PolygonArea(List<(double x,double y)> p)
        {double sum=0;for(int i=0;i<p.Count;i++){var a=p[i];var b=p[(i+1)%p.Count];sum+=a.x*b.y-a.y*b.x;}return sum*.5;}
        private static List<(double x,double y)> Clip(List<(double x,double y)> source,List<(double x,double y)> clip)
        {
            var result=new List<(double x,double y)>(source);double sign=Math.Sign(PolygonArea(clip));
            for(int edge=0;edge<clip.Count&&result.Count>0;edge++)
            {
                var a=clip[edge];var b=clip[(edge+1)%clip.Count];var input=result;result=new List<(double x,double y)>();
                double Side((double x,double y) p)=>sign*((b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x));
                var previous=input[input.Count-1];double old=Side(previous);
                foreach(var p in input)
                {
                    double side=Side(p);
                    if((side>=0)!=(old>=0)){double t=old/(old-side);result.Add((previous.x+(p.x-previous.x)*t,previous.y+(p.y-previous.y)*t));}
                    if(side>=0)result.Add(p);previous=p;old=side;
                }
            }
            return result;
        }
        private static void AssertPoint(List<Vector3> points,Vector3 target)=>Assert.That(points.Exists(p=>Vector3.Distance(p,target)<.000002f),Is.True,$"Missing actual station {target}.");
        private static void AssertRange(object expected,object actual,int offset)
        {int count=Data<Vector3>(expected,"V").Count;CollectionAssert.AreEqual(Data<Vector3>(expected,"V"),Data<Vector3>(actual,"V").GetRange(offset,count));}
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
        private static List<T> Data<T>(object mesh,string name)=>(List<T>)mesh.GetType().GetField(name,Fields).GetValue(mesh);
    }
}
