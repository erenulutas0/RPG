using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateInnerPierTests
    {
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(-3f)] [TestCase(11f)]
        public void CompletePierKeepsFootAndHeadAndSeatsItsNewMassesOnActualCaps(float v)
        {
            object built=Builder();Call("BuildGateInnerPier",built,v);
            Mesh mesh=Read(built);
            object lower=Builder();Lower(lower,v);Mesh foot=Read(lower);
            object head=Builder();Head(head,v);Mesh roof=Read(head);
            AssertPart(mesh,foot,0);
            AssertPart(mesh,roof,mesh.V.Count-roof.V.Count);
            object envelope=Builder();Call("LitFoundryTier",envelope,G(32),v,G(35),v+2,0f,7.25f,0f);
            foreach(Vector3 p in mesh.V)
                Assert.That(LastCover(Read(envelope),p),Is.GreaterThanOrEqualTo(0),
                    "A complete inner pier must remain within its original footing and head envelope.");

            var lowerBox=new Vector4(G(32.26f),v+.24f,G(34.74f),v+1.76f);
            var neck=new Vector4(G(32.48f),v+.46f,G(34.52f),v+1.54f);
            var crown=new Vector4(G(32.25f),v+.20f,G(34.75f),v+1.80f);
            object haunch=Builder();Call("GateInnerPierMass",haunch,lowerBox,neck,1.70f,2.65f,.96f);
            object shaft=Builder();Call("GateRecessedShaft",shaft,neck,2.65f,5.45f,-.18f,.96f,.28f);
            object capital=Builder();Call("GateInnerPierMass",capital,
                new Vector4(G(32.08f),v+.08f,G(34.92f),v+1.92f),crown,5.45f,6.05f,.96f);
            Mesh shoulder=Read(haunch),bay=Read(shaft),cap=Read(capital);
            AssertPart(mesh,shoulder,foot.V.Count);
            AssertPart(mesh,bay,foot.V.Count+shoulder.V.Count);
            AssertPart(mesh,cap,foot.V.Count+shoulder.V.Count+bay.V.Count);
            AssertBearing(shoulder,shoulder.V.Count-9,neck,2.65f);
            AssertBearing(cap,cap.V.Count-9,crown,6.05f);

            // Check the actual tapered face at the fixed collar height. Its
            // plane can change slightly while the existing iron still overlaps
            // it; equality to the former shaft's chamfers would be a false oracle.
            object iron=Builder();
            Call("ArchitectureBar",iron,Project(G(32.25f),v+.3f)+Vector3.up*5.43f,
                Project(G(32.25f),v+1.7f)+Vector3.up*5.43f,.075f,new Color(.47f,.43f,.38f));
            float faceU=Mathf.Lerp(neck.x,neck.x-.18f,(5.43f-2.65f)/(5.45f-2.65f));
            foreach(float along in new[]{.6f,.9f,1.3f})
            {
                Vector3 contact=Project(faceU,v+along)+Vector3.up*5.43f;
                Assert.That(LastCover(bay,contact),Is.GreaterThanOrEqualTo(0),"The collar must touch an emitted stone face.");
                Assert.That(LastCover(Read(iron),contact),Is.GreaterThanOrEqualTo(0),"The unchanged collar must reach that face.");
            }
        }

        [TestCase(-3f)] [TestCase(11f)]
        public void RaisedHaunchAndConsolidatedCapitalLeaveTheShorterRealBayExposed(float v)
        {
            var box=new Vector4(G(32.48f),v+.46f,G(34.52f),v+1.54f);
            object built=Builder();Call("BuildGateInnerPier",built,v);Mesh complete=Read(built);
            object isolated=Builder();Call("GateRecessedShaft",isolated,box,2.65f,5.45f,-.18f,.96f,.28f);
            Mesh bay=Read(isolated);
            object shell=Builder();Call("LitFoundryTier",shell,box.x,box.y,box.z,box.w,2.65f,5.45f,-.18f);
            Mesh plane=Read(shell);
            for(int side=0;side<2;side++)
            {
                int first=side*4;
                Vector3 inward=side==0?Project(box.x,box.y+.28f)-Project(box.x,box.y):
                    Project(box.x+.28f,box.y)-Project(box.x,box.y);
                foreach(float rise in new[]{.30f,.37f,.44f,.51f,.58f})
                {
                    Vector3 point=Vector3.Lerp(Vector3.Lerp(plane.V[first],plane.V[first+1],.5f),
                        Vector3.Lerp(plane.V[first+3],plane.V[first+2],.5f),rise)+inward;
                    int expected=LastCover(bay,point),actual=LastCover(complete,point);
                    Assert.That(expected,Is.GreaterThanOrEqualTo(0));Assert.That(actual,Is.GreaterThanOrEqualTo(0));
                    for(int k=0;k<3;k++)
                        Assert.That(Vector3.Distance(complete.V[complete.T[actual+k]],bay.V[bay.T[expected+k]]),
                            Is.LessThan(.00001f),"The complete assembly must expose the actual recessed shaft, without a retained storey over it.");
                }
            }
        }

        [TestCase(-3f)] [TestCase(11f)]
        public void FamilyFinishKeepsConstructionLightAndCoverageAndDoesNotRefinishItsNeighbours(float v)
        {
            object pier=Builder();Call("BuildGateInnerPier",pier,v);Mesh raw=Read(pier);
            Vector3[] points=raw.V.ToArray();Color[] light=raw.C.ToArray();double area=Area(raw);
            object former=Builder();LegacyPier(former,v);int formerSource=Read(former).V.Count;
            Assert.That(points.Length,Is.LessThanOrEqualTo(formerSource),"Broader masses replace existing geometry.");
            Call("GateInnerPierGrain",pier);Mesh finished=Read(pier);
            for(int i=0;i<points.Length;i++)
            {
                Assert.That(finished.V[i],Is.EqualTo(points[i]),"Family grain cannot move a contact or a reveal.");
                Assert.That(finished.C[i],Is.EqualTo(light[i]),"Family grain must retain source forge and contact illumination.");
            }
            Assert.That(Area(finished),Is.EqualTo(area).Within(.0006),"The finish partitions the surface without adding another layer.");
            AssertField(finished,.11f,new Vector4(.04f,.44f,.21f,.564f));

            object neighbour=Builder();Call("BuildGateInnerReturn",neighbour,v,6.5f);Call("GateHouseGrain",neighbour);
            Mesh returned=Read(neighbour);
            AssertField(returned,.075f,(Vector4)typeof(FoundryRegionView).GetField("GateHousePatch",Static).GetValue(null));
            object merged=Builder();Call("AppendGateStone",merged,pier);Call("AppendGateStone",merged,neighbour);
            Mesh combined=Read(merged);AssertPart(combined,finished,0,true);
            AssertPart(combined,returned,finished.V.Count,true);
            Assert.That(combined.V.Count,Is.EqualTo(finished.V.Count+returned.V.Count));
            Assert.That(combined.T.Count,Is.EqualTo(finished.T.Count+returned.T.Count));
            for(int i=0;i<finished.T.Count;i++)Assert.That(combined.T[i],Is.EqualTo(finished.T[i]));
            for(int i=0;i<returned.T.Count;i++)Assert.That(combined.T[finished.T.Count+i],Is.EqualTo(returned.T[i]+finished.V.Count));
            Call("GateHouseGrain",former);
            TestContext.WriteLine($"Inner pier {v}: source {formerSource}->{points.Length}; finished {Read(former).V.Count}->{finished.V.Count} vertices; " +
                $"including unchanged inner return {Read(former).V.Count+returned.V.Count}->{combined.V.Count} vertices.");
        }

        static void AssertBearing(Mesh support,int capStart,Vector4 box,float height)
        {
            var cap=new Mesh(support.V,support.U,support.C,new List<int>());
            for(int t=0;t<support.T.Count;t+=3)
                if(support.T[t]>=capStart&&support.T[t+1]>=capStart&&support.T[t+2]>=capStart)
                    cap.T.AddRange(new[]{support.T[t],support.T[t+1],support.T[t+2]});
            Assert.That(cap.T.Count,Is.GreaterThan(0));
            float c=Mathf.Min(.18f,Mathf.Min(box.z-box.x,box.w-box.y)*.15f);
            var outline=new[]{Project(box.x+c,box.y),Project(box.z-c,box.y),Project(box.z,box.y+c),Project(box.z,box.w-c),
                Project(box.z-c,box.w),Project(box.x+c,box.w),Project(box.x,box.w-c),Project(box.x,box.y+c)};
            for(int i=0;i<outline.Length;i++)
                foreach(Vector3 p in new[]{outline[i],(outline[i]+outline[(i+1)%outline.Length])*.5f})
                    Assert.That(LastCover(cap,p+Vector3.up*height),Is.GreaterThanOrEqualTo(0),
                        "Every supported foot corner and edge midpoint must bear on the actual chamfered cap.");
        }

        static void AssertField(Mesh mesh,float density,Vector4 patch)
        {
            Vector3 datum=Project(G(32),-3);
            Assert.That(mesh.U.Count,Is.EqualTo(mesh.V.Count));Assert.That(mesh.C.Count,Is.EqualTo(mesh.V.Count));
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                Vector3 p=mesh.V[mesh.T[t]]*.231f+mesh.V[mesh.T[t+1]]*.317f+mesh.V[mesh.T[t+2]]*.452f;
                Vector2 uv=mesh.U[mesh.T[t]]*.231f+mesh.U[mesh.T[t+1]]*.317f+mesh.U[mesh.T[t+2]]*.452f;
                for(int axis=0;axis<2;axis++)
                {
                    double size=axis==0?patch.z-patch.x:patch.w-patch.y,physical=((double)p[axis]-datum[axis])*density/size;
                    double cycle=physical-Math.Floor(physical/2)*2;
                    double expected=(axis==0?patch.x:patch.y)+(1-Math.Abs(cycle-1))*size;
                    Assert.That(uv[axis],Is.EqualTo(expected).Within(.00002),
                        "The rendered triangle must sample the family field through its interior, including resolved mirror folds.");
                }
            }
        }

        static void AssertPart(Mesh whole,Mesh part,int offset,bool uv=false)
        {
            for(int i=0;i<part.V.Count;i++)
            {
                Assert.That(whole.V[offset+i],Is.EqualTo(part.V[i]));
                Assert.That(whole.C[offset+i],Is.EqualTo(part.C[i]));
                if(uv)Assert.That(whole.U[offset+i],Is.EqualTo(part.U[i]));
            }
        }
        static void Lower(object b,float v)
        {
            Call("LitFoundryTier",b,G(32),v,G(35),v+2,0f,.45f,.08f);
            Call("GateEnclosureTier",b,G(32.12f),v+.10f,G(34.88f),v+1.90f,.45f,1.45f,.14f,.91f,false);
            Call("GateEnclosureTier",b,G(32.16f),v+.14f,G(34.84f),v+1.86f,1.45f,1.70f,.08f,.96f,true);
        }
        static void Head(object b,float v)
        {
            Call("GateCraftTier",b,G(32.25f),v+.2f,G(34.75f),v+1.8f,6.05f,7.05f,.48f,1f);
            Call("GateCraftTier",b,G(32.62f),v+.57f,G(34.38f),v+1.43f,7.05f,7.25f,.05f,1f);
        }
        static void LegacyPier(object b,float v)
        {
            Lower(b,v);
            Call("GateRecessedShaft",b,new Vector4(G(32.26f),v+.24f,G(34.74f),v+1.76f),1.70f,5.45f,.04f,.96f,.28f);
            Call("GateEnclosureTier",b,G(32.08f),v+.08f,G(34.92f),v+1.92f,5.45f,5.70f,.08f,.96f,true);
            Call("GateCraftTier",b,G(32.02f),v+.02f,G(34.98f),v+1.98f,5.70f,6.05f,.12f,1f);
            Head(b,v);
        }
        static float G(float u)=>u+FoundryMap.GateOffset;
        static Vector3 Project(float u,float v)
        {
            FoundryMap.ToFloor(u,v,out float x,out float y);return new Vector3(x,y*.5f+1.25f,0);
        }
        static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        static double Area(Mesh mesh)
        {
            double area=0;
            for(int t=0;t<mesh.T.Count;t+=3)
                area+=Math.Abs(Cross(mesh.V[mesh.T[t+1]]-mesh.V[mesh.T[t]],mesh.V[mesh.T[t+2]]-mesh.V[mesh.T[t]]))*.5;
            return area;
        }
        static int LastCover(Mesh mesh,Vector2 p)
        {
            int last=-1;
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                Vector2 a=mesh.V[mesh.T[t]],b=mesh.V[mesh.T[t+1]],c=mesh.V[mesh.T[t+2]];
                double area=Cross(b-a,c-a);if(Math.Abs(area)<1e-9)continue;
                double x=Cross(b-p,c-p)/area,y=Cross(c-p,a-p)/area,z=1-x-y;
                if(x>=-.0001&&y>=-.0001&&z>=-.0001)last=t;
            }
            return last;
        }
        static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
        static Mesh Read(object b)=>new Mesh((List<Vector3>)b.GetType().GetField("V",Fields).GetValue(b),
            (List<Vector2>)b.GetType().GetField("U",Fields).GetValue(b),(List<Color>)b.GetType().GetField("C",Fields).GetValue(b),
            (List<int>)b.GetType().GetField("T",Fields).GetValue(b));
        sealed class Mesh
        {
            internal readonly List<Vector3> V;internal readonly List<Vector2> U;
            internal readonly List<Color> C;internal readonly List<int> T;
            internal Mesh(List<Vector3> v,List<Vector2> u,List<Color> c,List<int> t){V=v;U=u;C=c;T=t;}
        }
    }
}
