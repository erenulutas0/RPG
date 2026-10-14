using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateCorniceTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private const float U0=32.08f+FoundryMap.GateOffset,U1=33.92f+FoundryMap.GateOffset;
        private const float V0=-.78f,V1=10.78f,Bottom=6.93f,Top=7.22f,Inset=.06f,Shade=1.02f;

        [Test]
        public void ActualHeaderUsesProfiledCorniceAndRetainsItsExactCapAndBearing()
        {
            MeshData old=Build("GateCraftTier"),cornice=Build("GateHeaderCornice");
            object shellBuilder=Builder();Call("LitFoundryTier",shellBuilder,U0,V0,U1,V1,Bottom,Top,Inset);
            MeshData shell=Read(shellBuilder);
            for(int i=0;i<shell.C.Count;i++)
            {
                Color c=shell.C[i];shell.C[i]=new Color(c.r*Shade,c.g*Shade,c.b*Shade,1);
            }
            object capBuilder=Builder();Call("GateCraftCap",capBuilder,shellBuilder,U0,V0,U1,V1,Top,Inset);
            MeshData cap=Read(capBuilder);
            foreach(MeshData mesh in new[]{old,cornice})
            {
                int offset=mesh.V.Count-cap.V.Count;
                for(int i=0;i<cap.V.Count;i++)
                {
                    Assert.That(mesh.V[offset+i],Is.EqualTo(cap.V[i]),"Existing bearing and coping cannot move.");
                    Assert.That(mesh.U[offset+i],Is.EqualTo(cap.U[i]));
                    Assert.That(mesh.C[offset+i],Is.EqualTo(cap.C[i]));
                }
            }
            object actualBuilder=Builder();Call("BuildGateHeaderFinish",actualBuilder,6.5f);
            MeshData actual=Read(actualBuilder);
            for(int i=0;i<cornice.V.Count;i++)
                Assert.That(Vector3.Distance(actual.V[i],cornice.V[i]),Is.LessThan(.00001f),
                    "The production header must emit this cornice, not leave it in a test-only helper.");
            Assert.That(cornice.V.Count-old.V.Count,Is.InRange(1,1200),
                "The three-part static fascia stays bounded; it adds no complete hidden wall.");
            TestContext.WriteLine($"Header cornice: old {old.V.Count}, new {cornice.V.Count}, delta {cornice.V.Count-old.V.Count} vertices.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ActualCarvedFacesHaveOneOwnerAndContinuousGrainAcrossDepthReturns(int face)
        {
            MeshData mesh=Build("GateHeaderCornice");
            int wallEnd=WallEnd(mesh);
            Vector3[][] strips=ExpectedStrips(face);
            var faceVertices=new HashSet<int>();
            foreach(Vector3[] strip in strips)
            {
                double expected=QuadArea(strip),covered=0;
                for(int t=0;t<mesh.T.Count;t+=3)
                {
                    int a=mesh.T[t],b=mesh.T[t+1],c=mesh.T[t+2];
                    if(a>=wallEnd||b>=wallEnd||c>=wallEnd)continue;
                    double area=TriangleArea(mesh.V[a],mesh.V[b],mesh.V[c]);
                    if(area<1e-9)continue;
                    if(!Inside(strip,mesh.V[a])||!Inside(strip,mesh.V[b])||!Inside(strip,mesh.V[c]))continue;
                    covered+=area;faceVertices.Add(a);faceVertices.Add(b);faceVertices.Add(c);
                }
                Assert.That(covered,Is.EqualTo(expected).Within(.00004),
                    "The carved fascia replaces each old surface; mortar and dressed faces cannot overlap a retained full wall.");
                for(int x=0;x<5;x++)for(int y=0;y<3;y++)
                {
                    Vector3 p=Vector3.Lerp(Vector3.Lerp(strip[0],strip[1],(x+.357f)/5),
                        Vector3.Lerp(strip[3],strip[2],(x+.357f)/5),(y+.623f)/3);
                    int owners=0;
                    for(int t=0;t<mesh.T.Count;t+=3)
                    {
                        int a=mesh.T[t],b=mesh.T[t+1],c=mesh.T[t+2];
                        if(a>=wallEnd||b>=wallEnd||c>=wallEnd)continue;
                        if(InTriangle(mesh.V[a],mesh.V[b],mesh.V[c],p,1e-7))owners++;
                    }
                    Assert.That(owners,Is.EqualTo(1),"Actual visible cornice samples must have exactly one surface owner.");
                }
            }
            foreach(Vector3[] strip in strips)foreach(Vector3 station in strip)
            {
                bool found=false;
                foreach(int i in faceVertices)if(Vector3.Distance(mesh.V[i],station)<.00002f){found=true;break;}
                Assert.That(found,Is.True,"Every shared bearing and 0.09-deep return station must occur in a rendered triangle.");
            }
            // A global affine field for each physical face must cross its three
            // carved bands; matching colours alone cannot reveal a UV restart.
            var indices=new List<int>(faceVertices);
            // The narrow closed corner deliberately has one quad per band:
            // three connected bands supply twelve rendered vertex indices.
            Assert.That(indices.Count,Is.GreaterThanOrEqualTo(12));
            int ia=indices[0],ib=-1,ic=-1;
            for(int j=1;j<indices.Count&&ic<0;j++)for(int k=j+1;k<indices.Count;k++)
                if(TriangleArea(mesh.V[ia],mesh.V[indices[j]],mesh.V[indices[k]])>.001)
                {ib=indices[j];ic=indices[k];break;}
            Assert.That(ic,Is.GreaterThanOrEqualTo(0));
            Vector2 e=mesh.V[ib]-mesh.V[ia],f=mesh.V[ic]-mesh.V[ia];
            double determinant=Cross(e,f);
            foreach(int i in faceVertices)
            {
                Vector2 p=mesh.V[i]-mesh.V[ia];
                float s=(float)(Cross(p,f)/determinant),t=(float)(Cross(e,p)/determinant);
                Vector2 expected=mesh.U[ia]+s*(mesh.U[ib]-mesh.U[ia])+t*(mesh.U[ic]-mesh.U[ia]);
                Assert.That(Vector2.Distance(mesh.U[i],expected),Is.LessThan(.00003f));
            }
        }

        [Test]
        public void ActualCorniceKeepsNoncollapsedMineralTrianglesAndDarkRecessedFascia()
        {
            MeshData mesh=Build("GateHeaderCornice");int wallEnd=WallEnd(mesh);
            foreach(Vector2 uv in mesh.U)
            {
                Assert.That(uv.x,Is.InRange(.03499f,.39801f));
                Assert.That(uv.y,Is.InRange(.43799f,.56701f));
            }
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                int a=mesh.T[t],b=mesh.T[t+1],c=mesh.T[t+2];
                if(TriangleArea(mesh.V[a],mesh.V[b],mesh.V[c])<1e-7)continue;
                Assert.That(Math.Abs(Cross(mesh.U[b]-mesh.U[a],mesh.U[c]-mesh.U[a])),Is.GreaterThan(1e-12));
            }
            Vector3[][] strips=ExpectedStrips(1);
            Color Sample(Vector3[] q)
            {
                Vector3 p=Vector3.Lerp(Vector3.Lerp(q[0],q[1],.513f),Vector3.Lerp(q[3],q[2],.513f),.531f);
                for(int t=0;t<mesh.T.Count;t+=3)
                {
                    int a=mesh.T[t],b=mesh.T[t+1],c=mesh.T[t+2];
                    if(a>=wallEnd||b>=wallEnd||c>=wallEnd||!InTriangle(mesh.V[a],mesh.V[b],mesh.V[c],p,1e-7))continue;
                    Vector2 e=mesh.V[b]-mesh.V[a],f=mesh.V[c]-mesh.V[a],d=p-mesh.V[a];
                    float u=(float)(Cross(d,f)/Cross(e,f)),v=(float)(Cross(e,d)/Cross(e,f));
                    return mesh.C[a]*(1-u-v)+mesh.C[b]*u+mesh.C[c]*v;
                }
                Assert.Fail("The actual production return sample is missing.");return Color.clear;
            }
            Color middle=Sample(strips[1]),upper=Sample(strips[2]);
            Assert.That(middle.grayscale,Is.LessThan(upper.grayscale*.88f),
                "In the unlit scenery, the real recessed face must read below its sloping dressed upper return.");
            foreach(Color c in mesh.C)
            {
                Assert.That(c.a,Is.EqualTo(1));Assert.That(float.IsNaN(c.r)||float.IsInfinity(c.r),Is.False);
            }
        }

        private static Vector3[][] ExpectedStrips(int face)
        {
            // Independent authored octagonal rings at the documented production
            // dimensions. The two internal rings remove 0.09 units of stone on
            // both design axes, while the first and last rings retain the bearing.
            const float corner=.18f,topCorner=.135f;
            Vector2[] Ring(float inset,float cut)=>new[]{new Vector2(U0+inset+cut,V0+inset),
                new Vector2(U1-inset-cut,V0+inset),new Vector2(U0+inset,V1-inset-cut),
                new Vector2(U0+inset,V0+inset+cut)};
            Vector2[] low=Ring(0,corner),high=Ring(Inset,topCorner);
            int a=face==0?0:face==1?2:3,b=face==0?1:face==1?3:0;
            var rises=new[]{0f,.28f,.65f,1f};var rows=new Vector3[4][];
            for(int row=0;row<4;row++)
            {
                rows[row]=new Vector3[2];
                for(int end=0;end<2;end++)
                {
                    Vector2 p=Vector2.Lerp(low[end==0?a:b],high[end==0?a:b],rises[row]);
                    if(row==1||row==2)
                        p+=new Vector2(p.x<(U0+U1)*.5f?.09f:-.09f,p.y<(V0+V1)*.5f?.09f:-.09f);
                    rows[row][end]=RaisedGatePoint(p)+Vector3.up*Mathf.Lerp(Bottom,Top,rises[row]);
                }
            }
            var result=new Vector3[3][];
            for(int i=0;i<3;i++)result[i]=new[]{rows[i][0],rows[i][1],rows[i+1][1],rows[i+1][0]};
            return result;
        }
        private static Vector3 RaisedGatePoint(Vector2 design)
        {
            // This production lintel is wholly past the ramp: its independent
            // expected floor plane is the authored 1.25-unit raised landing.
            // Do not reach across the runtime assembly into FoundrySurface.
            FoundryMap.ToFloor(design.x,design.y,out float x,out float y);
            return new Vector3(x,y*.5f+1.25f,0);
        }
        private static int WallEnd(MeshData mesh)
        {
            object shellBuilder=Builder();Call("LitFoundryTier",shellBuilder,U0,V0,U1,V1,Bottom,Top,Inset);
            object capBuilder=Builder();Call("GateCraftCap",capBuilder,shellBuilder,U0,V0,U1,V1,Top,Inset);
            return mesh.V.Count-Read(capBuilder).V.Count;
        }
        private static MeshData Build(string name)
        {
            object builder=Builder();Call(name,builder,U0,V0,U1,V1,Bottom,Top,Inset,Shade);return Read(builder);
        }
        private static bool Inside(Vector3[] q,Vector3 p)=>InTriangle(q[0],q[1],q[2],p,.00003)||InTriangle(q[0],q[2],q[3],p,.00003);
        private static bool InTriangle(Vector2 a,Vector2 b,Vector2 c,Vector2 p,double tolerance)
        {
            double area=Cross(b-a,c-a);if(Math.Abs(area)<1e-10)return false;
            double x=Cross(b-p,c-p)/area,y=Cross(c-p,a-p)/area,z=1-x-y;
            return x>=-tolerance&&y>=-tolerance&&z>=-tolerance;
        }
        private static double TriangleArea(Vector2 a,Vector2 b,Vector2 c)=>Math.Abs(Cross(b-a,c-a))*.5;
        private static double QuadArea(Vector3[] q)=>TriangleArea(q[0],q[1],q[2])+TriangleArea(q[0],q[2],q[3]);
        private static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
        private static MeshData Read(object builder)
        {
            Type type=builder.GetType();
            return new MeshData((List<Vector3>)type.GetField("V",Fields).GetValue(builder),
                (List<Vector2>)type.GetField("U",Fields).GetValue(builder),(List<Color>)type.GetField("C",Fields).GetValue(builder),
                (List<int>)type.GetField("T",Fields).GetValue(builder));
        }
        private sealed class MeshData
        {
            internal readonly List<Vector3> V;internal readonly List<Vector2> U;internal readonly List<Color> C;internal readonly List<int> T;
            internal MeshData(List<Vector3> v,List<Vector2> u,List<Color> c,List<int> t){V=v;U=u;C=c;T=t;}
        }
    }
}
