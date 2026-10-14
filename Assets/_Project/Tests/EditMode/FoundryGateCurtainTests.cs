using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateCurtainTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(28.16f,-6.48f,31.88f,-3.12f,.46f,2.9f,.035f,.82f)]
        [TestCase(28.2f,13.16f,31.84f,16.44f,3.13f,6.55f,.05f,.85f)]
        [TestCase(28.12f,-2.9f,30.88f,-1.1f,.45f,1.45f,.14f,.91f)]
        public void ActualCurtainAndJambTiersPartitionTheOldWallsAndRetainTheirBearing(
            float u0,float v0,float u1,float v1,float bottom,float top,float inset,float shade)
        {
            MeshData shell=Build(false,u0,v0,u1,v1,bottom,top,inset,shade);
            MeshData actual=Build(true,u0,v0,u1,v1,bottom,top,inset,shade);
            Assert.That(actual.V.Count,Is.LessThan(1100),"Curtain stock should stay broad, not become fine brick decoration.");
            Assert.That(Area(actual),Is.EqualTo(Area(shell)).Within(.00012),
                "Stone interiors, arrises and joints replace the full wall; no complete duplicate bed may remain.");
            int capStart=actual.V.Count-32;
            for(int i=0;i<32;i++)
            {
                Assert.That(actual.V[capStart+i],Is.EqualTo(shell.V[12+i]),"The original cap/bearing does not move.");
                Assert.That(actual.U[capStart+i],Is.EqualTo(shell.U[12+i]));
                Assert.That(actual.C[capStart+i],Is.EqualTo(shell.C[12+i]));
            }
            foreach(Vector3 p in actual.V)
                Assert.That(ContainedAtFloatPrecision(shell,p),Is.True,"Only the original shell may own the new vertices.");
            for(int face=0;face<3;face++)
            {
                int a=face*4;
                for(int x=0;x<17;x++)for(int y=0;y<13;y++)
                {
                    Vector3 p=Vector3.Lerp(Vector3.Lerp(shell.V[a],shell.V[a+1],(x+.371f)/17),
                        Vector3.Lerp(shell.V[a+3],shell.V[a+2],(x+.371f)/17),(y+.619f)/13);
                    Assert.That(Cover(actual,p),Is.EqualTo(Cover(shell,p)),
                        "Actual original-plane samples must keep exactly one owner across stones and mortar.");
                }
            }
            // The small boundary representation guard cannot accept actual growth.
            Vector2 edgeA=shell.V[20],edgeB=shell.V[21],mid=(edgeA+edgeB)*.5f,along=edgeB-edgeA;
            Vector2 outward=new Vector2(along.y,-along.x).normalized;
            float step=8*Mathf.Max(Neighbour(mid.x,true)-mid.x,Neighbour(mid.y,true)-mid.y);
            Assert.That(ContainedAtFloatPrecision(shell,mid+outward*step),Is.False);
        }

        [TestCase(.46f,2.9f,.035f,.82f)]
        [TestCase(3.13f,6.55f,.05f,.85f)]
        public void ActualCurtainFacesSharePhysicalGrainAndRetainTheInheritedLightFamily(
            float bottom,float top,float inset,float shade)
        {
            MeshData shell=Build(false,28.2f,-6.44f,31.84f,-3.16f,bottom,top,inset,shade);
            MeshData actual=Build(true,28.2f,-6.44f,31.84f,-3.16f,bottom,top,inset,shade);
            int end=actual.V.Count-32;
            for(int face=0;face<3;face++)
            {
                int a=face*4;
                var quad=new[]{shell.V[a],shell.V[a+1],shell.V[a+2],shell.V[a+3]};
                var points=new HashSet<int>();
                for(int t=0;t<actual.T.Count;t+=3)
                {
                    int i=actual.T[t],j=actual.T[t+1],k=actual.T[t+2];
                    if(i>=end||j>=end||k>=end||Math.Abs(Cross(actual.V[i],actual.V[j],actual.V[k]))<1e-8)continue;
                    if(!InQuad(quad,actual.V[i])||!InQuad(quad,actual.V[j])||!InQuad(quad,actual.V[k]))continue;
                    points.Add(i);points.Add(j);points.Add(k);
                    Assert.That(Math.Abs(Cross(actual.U[i],actual.U[j],actual.U[k])),Is.GreaterThan(1e-12));
                }
                Assert.That(points.Count,Is.GreaterThanOrEqualTo(4));
                var indices=new List<int>(points);int ia=indices[0],ib=-1,ic=-1;
                for(int j=1;j<indices.Count&&ic<0;j++)for(int k=j+1;k<indices.Count;k++)
                    if(Math.Abs(Cross(actual.V[ia],actual.V[indices[j]],actual.V[indices[k]]))>.001)
                    {ib=indices[j];ic=indices[k];break;}
                Assert.That(ic,Is.GreaterThanOrEqualTo(0));
                foreach(int i in points)
                {
                    Barycentric(actual.V[i],actual.V[ia],actual.V[ib],actual.V[ic],out Vector3 weights);
                    Vector2 expected=actual.U[ia]*weights.x+actual.U[ib]*weights.y+actual.U[ic]*weights.z;
                    Assert.That(Vector2.Distance(actual.U[i],expected),Is.LessThan(.00003f),
                        "The face-wide physical mineral field cannot restart on a stone, joint or arris.");
                    Assert.That(actual.U[i].x,Is.InRange(.03499f,.39801f));
                    Assert.That(actual.U[i].y,Is.InRange(.43799f,.56701f));
                    Vector2 at=Parameter(actual.V[i],quad);
                    Color light=Color.Lerp(Color.Lerp(shell.C[a],shell.C[a+1],at.x),
                        Color.Lerp(shell.C[a+3],shell.C[a+2],at.x),at.y),c=actual.C[i];
                    float red=c.r/light.r,green=c.g/light.g,blue=c.b/light.b;
                    Assert.That(red/green,Is.InRange(.97f,1.03f));Assert.That(blue/green,Is.InRange(.97f,1.03f),
                        "Existing cool damp/mineral tint stays restrained and preserves local forge-light hue.");
                    Assert.That(c.a,Is.EqualTo(1));
                }
            }
        }

        private static MeshData Build(bool finished,float u0,float v0,float u1,float v1,
            float bottom,float top,float inset,float shade)
        {
            object b=Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
            if(finished)typeof(FoundryRegionView).GetMethod("GateEnclosureTier",Static)
                .Invoke(null,new object[]{b,u0,v0,u1,v1,bottom,top,inset,shade,false});
            else typeof(FoundryRegionView).GetMethod("LitFoundryTier",Static)
                .Invoke(null,new object[]{b,u0,v0,u1,v1,bottom,top,inset});
            Type t=b.GetType();var c=(List<Color>)t.GetField("C",Fields).GetValue(b);
            if(!finished)for(int i=0;i<c.Count;i++){Color v=c[i]*shade;v.a=1;c[i]=v;}
            return new MeshData((List<Vector3>)t.GetField("V",Fields).GetValue(b),
                (List<Vector2>)t.GetField("U",Fields).GetValue(b),c,(List<int>)t.GetField("T",Fields).GetValue(b));
        }
        private static bool InQuad(Vector3[] q,Vector2 p)=>Inside(p,q[0],q[1],q[2],.00002)||Inside(p,q[0],q[2],q[3],.00002);
        private static Vector2 Parameter(Vector3 p,Vector3[] q)
        {
            Vector2 r=new Vector2(.5f,.5f);
            for(int n=0;n<10;n++)
            {
                Vector3 at=Vector3.Lerp(Vector3.Lerp(q[0],q[1],r.x),Vector3.Lerp(q[3],q[2],r.x),r.y);
                Vector3 dx=Vector3.Lerp(q[1]-q[0],q[2]-q[3],r.y),dy=Vector3.Lerp(q[3]-q[0],q[2]-q[1],r.x),e=at-p;
                float det=dx.x*dy.y-dx.y*dy.x;
                r-=new Vector2((e.x*dy.y-e.y*dy.x)/det,(dx.x*e.y-dx.y*e.x)/det);
            }
            return r;
        }
        private static bool ContainedAtFloatPrecision(MeshData m,Vector2 p)
        {
            if(Cover(m,p)>0)return true;
            for(int t=0;t<m.T.Count;t+=3)for(int e=0;e<3;e++)
            {
                Vector2 a=m.V[m.T[t+e]],b=m.V[m.T[t+(e+1)%3]];double enter=0,leave=1;
                if(Clip(a.x,b.x,p.x,ref enter,ref leave)&&Clip(a.y,b.y,p.y,ref enter,ref leave)&&enter<=leave)return true;
            }
            return false;
        }
        private static bool Clip(float a,float b,float p,ref double enter,ref double leave)
        {
            double low=Neighbour(p,false),high=Neighbour(p,true),delta=(double)b-a;
            if(delta==0)return a>=low&&a<=high;
            double t0=(low-a)/delta,t1=(high-a)/delta;
            enter=Math.Max(enter,Math.Min(t0,t1));leave=Math.Min(leave,Math.Max(t0,t1));return enter<=leave;
        }
        private static float Neighbour(float value,bool higher)
        {
            if(value==0)return higher?float.Epsilon:-float.Epsilon;
            return BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value)+((value>0)==higher?1:-1));
        }
        private static int Cover(MeshData m,Vector2 p)
        {
            int count=0;for(int t=0;t<m.T.Count;t+=3)
                if(Inside(p,m.V[m.T[t]],m.V[m.T[t+1]],m.V[m.T[t+2]],1e-7))count++;
            return count;
        }
        private static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c,double tolerance)
        {
            if(!Barycentric(p,a,b,c,out Vector3 w))return false;
            return w.x>=-tolerance&&w.y>=-tolerance&&w.z>=-tolerance;
        }
        private static bool Barycentric(Vector2 p,Vector2 a,Vector2 b,Vector2 c,out Vector3 w)
        {
            double area=Cross(a,b,c);w=Vector3.zero;if(Math.Abs(area)<1e-9)return false;
            double x=Cross(p,b,c)/area,y=Cross(p,c,a)/area;w=new Vector3((float)x,(float)y,(float)(1-x-y));return true;
        }
        private static double Area(MeshData m)
        {
            double sum=0;for(int t=0;t<m.T.Count;t+=3)sum+=Math.Abs(Cross(m.V[m.T[t]],m.V[m.T[t+1]],m.V[m.T[t+2]]))*.5;return sum;
        }
        private static double Cross(Vector2 a,Vector2 b,Vector2 c)=>
            ((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
        private sealed class MeshData
        {
            internal readonly List<Vector3> V;internal readonly List<Vector2> U;internal readonly List<Color> C;internal readonly List<int> T;
            internal MeshData(List<Vector3> v,List<Vector2> u,List<Color> c,List<int> t){V=v;U=u;C=c;T=t;}
        }
    }
}
