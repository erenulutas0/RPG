using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateJambTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private const float Bottom=1.15f,Height=6.5f,Top=Height-.38f;
        private const int FaceVertices=44;

        [TestCase(true)]
        [TestCase(false)]
        public void ActualJambCoursesAndBedsPartitionEveryOriginalFaceExactlyOnce(bool near)
        {
            MeshData actual=Build(near);Vector2[] section=Section(Box(near),near);
            var order=new int[6];for(int i=0;i<6;i++)order[i]=i;
            Array.Sort(order,(i,j)=>((section[j]+section[(j+1)%6]).x+(section[j]+section[(j+1)%6]).y)
                .CompareTo((section[i]+section[(i+1)%6]).x+(section[i]+section[(i+1)%6]).y));
            Assert.That(actual.V.Count,Is.EqualTo(272));
            Assert.That(actual.T.Count/3,Is.EqualTo(280));
            for(int ordinal=0;ordinal<6;ordinal++)
            {
                int edge=order[ordinal];Vector3 lowA=Floor(section[edge])+Vector3.up*Bottom;
                Vector3 lowB=Floor(section[(edge+1)%6])+Vector3.up*Bottom;
                Vector3 highA=lowA+Vector3.up*(Top-Bottom),highB=lowB+Vector3.up*(Top-Bottom);
                var face=new[]{lowA,lowB,highB,highA};MeshData fitted=Slice(actual,ordinal*FaceVertices,FaceVertices);
                double expected=Math.Abs(Area(face[0],face[1],face[2]))*.5+Math.Abs(Area(face[0],face[2],face[3]))*.5;
                Assert.That(TriangleArea(fitted),Is.EqualTo(expected).Within(.00003),
                    "Actual stones and beds replace the whole old face; no complete duplicate backing remains.");
                foreach(Vector3 p in fitted.V)foreach(var axis in new[]{Vector2.right,Vector2.up,
                    (Vector2)(face[1]-face[0]).normalized,new Vector2(-(face[1]-face[0]).y,(face[1]-face[0]).x).normalized})
                {
                    float min=float.PositiveInfinity,max=float.NegativeInfinity;
                    foreach(Vector3 corner in face){float d=Vector2.Dot(corner,axis);min=Mathf.Min(min,d);max=Mathf.Max(max,d);}
                    Assert.That(Vector2.Dot(p,axis),Is.InRange(min-.00002f,max+.00002f));
                }
                for(int row=0;row<17;row++)for(int column=0;column<13;column++)
                {
                    Vector3 p=Vector3.Lerp(Vector3.Lerp(lowA,lowB,(column+.371f)/13),
                        Vector3.Lerp(highA,highB,(column+.371f)/13),(row+.619f)/17);
                    Assert.That(Cover(fitted,p),Is.EqualTo(1),"Every strict face interior keeps one owner.");
                }
            }
            // The old implementation emitted six 64-vertex/32-triangle faces and
            // two cap quads. Record the real index trade, not just the vertex saving.
            TestContext.WriteLine($"Jamb near={near}: old 392 vertices/196 triangles; actual {actual.V.Count} vertices/{actual.T.Count/3} triangles; delta {actual.V.Count-392} vertices/{actual.T.Count/3-196} triangles.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FittedJambKeepsProductionBearingsAndTheExactUnfilledLShapedCap(bool near)
        {
            float v=near?-3f:11f,a=near?v+1.42f:v+.04f,b=near?v+1.96f:v+.58f;
            Vector4 box=Box(near);MeshData actual=Build(near);var section=Section(box,near);
            var authored=(Vector2[])Call("GateJambSection",box,near);
            CollectionAssert.AreEqual(section,authored,"No jamb footprint or aperture station moves.");
            object production=Builder();Call("BuildGateInnerReturn",production,v,Height);
            object baseBuilder=Builder();Call("GateCraftTier",baseBuilder,32.02f+FoundryMap.GateOffset,a,
                33.04f+FoundryMap.GateOffset,b,.45f,Bottom,.03f,.94f);
            MeshData baseMesh=Read(baseBuilder),whole=Read(production);
            AssertRange(baseMesh,whole,0);
            AssertRange(actual,whole,baseMesh.V.Count);
            object upperBuilder=Builder();Call("GateCraftTier",upperBuilder,32.02f+FoundryMap.GateOffset,a-.02f,
                33.04f+FoundryMap.GateOffset,b+.02f,Top,Height+.10f,.065f,.96f);
            MeshData upper=Read(upperBuilder);
            AssertRange(upper,whole,baseMesh.V.Count+actual.V.Count);
            Assert.That(whole.V.Count,Is.EqualTo(baseMesh.V.Count+actual.V.Count+upper.V.Count));
            float split=section[4].y,back=near?section[3].x:section[0].x;
            var capPoints=new List<Vector3>();
            void Quad(float u0,float v0,float u1,float v1)
            {
                foreach(var p in new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)})
                    capPoints.Add(Floor(p)+Vector3.up*Top);
            }
            if(near){Quad(box.x,box.y,box.z,split);Quad(back,split,box.z,box.w);}
            else{Quad(back,box.y,box.z,split);Quad(box.x,split,box.z,box.w);}
            CollectionAssert.AreEqual(capPoints,actual.V.GetRange(actual.V.Count-8,8),"Both original cap rectangles remain exact.");
            MeshData cap=Slice(actual,actual.V.Count-8,8);
            float notch=near?Mathf.Lerp(box.y,box.w,.90f):Mathf.Lerp(box.y,box.w,.10f);
            Vector3 missing=Floor(new Vector2(Mathf.Lerp(box.x,box.z,.15f),notch))+Vector3.up*Top;
            Assert.That(Cover(cap,missing),Is.Zero,"No cap triangle may fill the missing reveal.");
            foreach(Vector2 p in section)
            {
                Assert.That(p.y< -1||p.y>11,Is.True,"The complete return stays outside the painted facade.");
                Assert.That(actual.V.Exists(q=>Vector3.Distance(q,Floor(p)+Vector3.up*Bottom)<.00001f),Is.True);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WholeJambFacesShareGrainAcrossDressedStonesAndRealMortar(bool near)
        {
            MeshData actual=Build(near);
            for(int ordinal=0;ordinal<6;ordinal++)
            {
                MeshData face=Slice(actual,ordinal*FaceVertices,FaceVertices);
                float scale=0;
                for(int i=0;i<face.V.Count;i++)
                {
                    Assert.That(face.C[i].a,Is.EqualTo(1));
                    Assert.That(float.IsNaN(face.U[i].x)||float.IsInfinity(face.U[i].x)||
                        float.IsNaN(face.U[i].y)||float.IsInfinity(face.U[i].y),Is.False);
                    float distance=Vector3.Distance(face.V[0],face.V[i]);
                    if(distance>.02f)
                    {
                        float measured=Vector2.Distance(face.U[0],face.U[i])/distance;
                        if(scale==0)scale=measured;
                        Assert.That(measured,Is.EqualTo(scale).Within(.00001f),"Grain scale does not restart at a new course or arris.");
                    }
                    for(int j=0;j<i;j++)if(Vector3.Distance(face.V[i],face.V[j])<.000001f)
                        Assert.That(Vector2.Distance(face.U[i],face.U[j]),Is.LessThan(.000001f));
                }
                for(int i=0;i<face.T.Count;i+=3)
                {
                    int a=face.T[i],b=face.T[i+1],c=face.T[i+2];
                    Assert.That(Math.Abs(Area(face.V[a],face.V[b],face.V[c])),Is.GreaterThan(1e-10));
                    Assert.That(Math.Abs(Area(face.U[a],face.U[b],face.U[c])),Is.GreaterThan(1e-12));
                }
                // Each indexed stone is eight vertices; the following four
                // vertices are its independently owned bed, before the next stone.
                for(int course=0;course<3;course++)
                {
                    int first=course*12;
                    var bed=Slice(face,first+8,4);
                    Vector3 p=(bed.V[0]+bed.V[1]+bed.V[2]+bed.V[3])*.25f;
                    // An asymmetric interior avoids the quad's shared diagonal.
                    p=Vector3.Lerp(p,bed.V[1],.117f);
                    Assert.That(Cover(face,p),Is.EqualTo(1));
                    Assert.That(bed.C[0].grayscale,Is.LessThan(face.C[first+4].grayscale*.80f));
                }
            }
        }

        private static Vector4 Box(bool near)
        {float v=near?-3f:11f,a=near?v+1.42f:v+.04f,b=near?v+1.96f:v+.58f;return new Vector4(32.10f+FoundryMap.GateOffset,a+.075f,32.96f+FoundryMap.GateOffset,b-.075f);}
        private static Vector2[] Section(Vector4 b,bool near)
        {
            float u=b.x+(b.z-b.x)*.53f,cut=(b.w-b.y)*.36f;
            return near?new[]{new Vector2(b.x,b.y),new Vector2(b.z,b.y),new Vector2(b.z,b.w),new Vector2(u,b.w),new Vector2(u,b.w-cut),new Vector2(b.x,b.w-cut)}
                :new[]{new Vector2(u,b.y),new Vector2(b.z,b.y),new Vector2(b.z,b.w),new Vector2(b.x,b.w),new Vector2(b.x,b.y+cut),new Vector2(u,b.y+cut)};
        }
        private static Vector3 Floor(Vector2 p)
        {FoundryMap.ToFloor(p.x,p.y,out float x,out float y);return new Vector3(x,y*.5f+1.25f,0);}
        private static MeshData Build(bool near)
        {object b=Builder();Call("BuildGateJambRebate",b,Box(near),Bottom,Top,near);return Read(b);}
        private static double Area(Vector2 a,Vector2 b,Vector2 c)=>((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
        private static double TriangleArea(MeshData m)
        {double area=0;for(int i=0;i<m.T.Count;i+=3)area+=Math.Abs(Area(m.V[m.T[i]],m.V[m.T[i+1]],m.V[m.T[i+2]]))*.5;return area;}
        private static int Cover(MeshData m,Vector2 p)
        {
            int count=0;for(int i=0;i<m.T.Count;i+=3)
            {
                Vector2 a=m.V[m.T[i]],b=m.V[m.T[i+1]],c=m.V[m.T[i+2]];double area=Area(a,b,c);
                if(Math.Abs(area)<1e-10)continue;
                double x=Area(p,b,c)/area,y=Area(a,p,c)/area,z=1-x-y;
                if(x>1e-8&&y>1e-8&&z>1e-8)count++;
            }
            return count;
        }
        private static MeshData Slice(MeshData m,int start,int count)
        {
            var indices=new List<int>();for(int i=0;i<m.T.Count;i+=3)
                if(m.T[i]>=start&&m.T[i]<start+count)
                {for(int k=0;k<3;k++){Assert.That(m.T[i+k],Is.InRange(start,start+count-1));indices.Add(m.T[i+k]-start);}}
            return new MeshData(m.V.GetRange(start,count),m.U.GetRange(start,count),m.C.GetRange(start,count),indices);
        }
        private static void AssertRange(MeshData expected,MeshData actual,int start)
        {CollectionAssert.AreEqual(expected.V,actual.V.GetRange(start,expected.V.Count));CollectionAssert.AreEqual(expected.U,actual.U.GetRange(start,expected.V.Count));CollectionAssert.AreEqual(expected.C,actual.C.GetRange(start,expected.V.Count));}
        private sealed class MeshData
        {
            internal readonly List<Vector3> V;internal readonly List<Vector2> U;internal readonly List<Color> C;internal readonly List<int> T;
            internal MeshData(List<Vector3> v,List<Vector2> u,List<Color> c,List<int> t){V=v;U=u;C=c;T=t;}
        }
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static object Call(string method,params object[] args)=>typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,args);
        private static List<T> Data<T>(object b,string field)=>(List<T>)b.GetType().GetField(field,Fields).GetValue(b);
        private static MeshData Read(object b)=>new MeshData(Data<Vector3>(b,"V"),Data<Vector2>(b,"U"),Data<Color>(b,"C"),Data<int>(b,"T"));
    }
}
