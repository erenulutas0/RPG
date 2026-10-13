using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateRecessTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(2f,12f,.30f)]
        [TestCase(2.5f,1.5f,1f)]
        [TestCase(1.02f,.54f,.70f)]
        public void HeaderCoursesPartitionActualTaperedFacesAndRetainOneMineralFieldPerStone(
            float width,float length,float height)
        {
            object original=Builder();Call("LitFoundryTier",original,28f,0f,28f+width,length,6f,6f+height,.04f);
            MeshData shell=Read(original);
            for(int side=0;side<3;side++)
            {
                int first=side*4;
                var face=new[]{shell.V[first],shell.V[first+1],shell.V[first+2],shell.V[first+3]};
                var light=new[]{shell.C[first],shell.C[first+1],shell.C[first+2],shell.C[first+3]};
                var source=new MeshData(new List<Vector3>(face),new List<Vector2>(),new List<Color>(),
                    new List<int>{0,1,2,0,2,3});
                object built=Builder();Call("GateCraftCourses",built,face,light,height,side,2307);
                MeshData mesh=Read(built);AssertFinite(mesh);
                Assert.That(TriangleArea(mesh),Is.EqualTo(TriangleArea(source)).Within(.0001),
                    "Header mortar, arrises and faces partition the old wall; no hidden full-wall overlay remains.");
                foreach(Vector3 point in mesh.V)
                    Assert.That(LastCoveringTriangle(source,point),Is.GreaterThanOrEqualTo(0));
                for(int x=0;x<19;x++)for(int y=0;y<11;y++)
                {
                    float u=(x+.371f)/19f,v=(y+.619f)/11f;
                    Vector3 point=Vector3.Lerp(Vector3.Lerp(face[0],face[1],u),Vector3.Lerp(face[3],face[2],u),v);
                    Assert.That(CoverCount(mesh,point),Is.EqualTo(1),"The wall has exactly one owner away from joints.");
                }
                AssertMineralGrain(mesh);
            }
        }

        [Test]
        public void HeaderCopingAndRetainedBayCapUseBoundedNoncollapsedGrainAtProductionDimensions()
        {
            object header=Builder();Call("GateCraftTier",header,28.08f,-.78f,29.92f,10.78f,6.93f,7.22f,.06f,1.02f);
            MeshData cap=Read(header);AssertFinite(cap);AssertMineralGrain(cap);
            object bay=Builder();Call("GateRecessedShaft",bay,new Vector4(27.53f,-6.14f,30.42f,-4.61f),
                2.85f,6.55f,.065f,.92f,.36f);
            MeshData recess=Read(bay);AssertFinite(recess);AssertMineralGrain(recess);
        }

        [Test]
        public void StoneContactPreservesLocalLightRatiosAndStaysAtCrownAndBearing()
        {
            var sampled=new Color(.93f,.57f,.29f);
            Color middle=(Color)Call("GateStoneContact",sampled,new Vector2(.8f,.5f),4f);
            Color crown=(Color)Call("GateStoneContact",sampled,new Vector2(.8f,1f),4f);
            Color foot=(Color)Call("GateStoneContact",sampled,new Vector2(.8f,0f),4f);
            Assert.That(middle.r/sampled.r,Is.GreaterThan(.995f));
            Assert.That(crown.r,Is.LessThan(middle.r*.93f));
            Assert.That(foot.r,Is.LessThan(middle.r*.97f));
            foreach(float height in new[]{.24f,.48f,1f,3.7f})
            for(int x=0;x<=20;x++)for(int y=0;y<=20;y++)
            {
                Color actual=(Color)Call("GateStoneContact",sampled,new Vector2(x/20f,y/20f),height);
                float gain=actual.r/sampled.r;
                Assert.That(gain,Is.InRange(.82f,1f),"Surface finish cannot globally lift or black out its original illumination.");
                Assert.That(actual.g/sampled.g,Is.EqualTo(gain).Within(.000001f));
                Assert.That(actual.b/sampled.b,Is.EqualTo(gain).Within(.000001f));
                Assert.That(actual.a,Is.EqualTo(1));
            }
        }

        private static void AssertMineralGrain(MeshData mesh)
        {
            foreach(Vector2 uv in mesh.U)
            {
                Assert.That(uv.x,Is.InRange(.03499f,.39801f));
                Assert.That(uv.y,Is.InRange(.43799f,.56701f));
            }
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                int a=mesh.T[t],b=mesh.T[t+1],c=mesh.T[t+2];
                double area=Math.Abs(CrossDouble((Vector2)(mesh.V[b]-mesh.V[a]),(Vector2)(mesh.V[c]-mesh.V[a])));
                if(area<1e-7)continue;
                double uvArea=Math.Abs(CrossDouble(mesh.U[b]-mesh.U[a],mesh.U[c]-mesh.U[a]));
                Assert.That(uvArea,Is.GreaterThan(1e-12),
                    $"Visible caps, bevels and returns must not collapse onto a single texture row. Triangle {t/3}, indices {a}/{b}/{c}, world area {area:R}, UV area {uvArea:R}. " +
                    $"Positions {mesh.V[a].ToString("R")} / {mesh.V[b].ToString("R")} / {mesh.V[c].ToString("R")}; " +
                    $"UV {mesh.U[a].ToString("R")} / {mesh.U[b].ToString("R")} / {mesh.U[c].ToString("R")}");
            }
        }

        [TestCase(0f,.48f,.08f)]
        [TestCase(.48f,2.05f,.09f)]
        [TestCase(2.05f,2.85f,.28f)]
        public void CoursedButtressTierPartitionsEachOriginalFaceWithoutAHiddenSecondBed(
            float bottom,float top,float inset)
        {
            var box=new Vector4(27.16f,-6.51f,30.79f,-4.24f);
            object original=Builder();Call("LitFoundryTier",original,box.x,box.y,box.z,box.w,bottom,top,inset);
            MeshData shell=Read(original);
            object built=Builder();Call("GateButtressTier",built,box,bottom,top,inset,.94f);
            MeshData mesh=Read(built);AssertFinite(mesh);
            Assert.That(mesh.V.Count,Is.LessThan(700),"Broad structural stone cannot become dense decorative strips.");
            Assert.That(TriangleArea(mesh),Is.EqualTo(TriangleArea(shell)).Within(.0001),
                "Mortar, dressed edges and interiors must partition the source walls rather than layer over them.");
            foreach(Vector3 point in mesh.V)
                Assert.That(LastCoveringTriangle(shell,point),Is.GreaterThanOrEqualTo(0));
            // Offset samples avoid authored course/bevel boundaries. Test every
            // source facet, including the narrow closed corner, in its actual plane.
            for(int face=0;face<3;face++)for(int x=0;x<9;x++)for(int y=0;y<7;y++)
            {
                int first=face*4;
                float u=(x+.371f)/9f,v=(y+.619f)/7f;
                Vector3 lo=Vector3.Lerp(shell.V[first],shell.V[first+1],u);
                Vector3 hi=Vector3.Lerp(shell.V[first+3],shell.V[first+2],u);
                Assert.That(CoverCount(mesh,Vector3.Lerp(lo,hi,v)),Is.EqualTo(1),
                    "Every source face point has one owner, including joints and battered shoulders.");
            }
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                int a=mesh.T[t],b=mesh.T[t+1],c=mesh.T[t+2];
                double area=Math.Abs(CrossDouble((Vector2)(mesh.V[b]-mesh.V[a]),(Vector2)(mesh.V[c]-mesh.V[a])));
                if(area<1e-7)continue;
                Assert.That(Math.Abs(CrossDouble(mesh.U[b]-mesh.U[a],mesh.U[c]-mesh.U[a])),Is.GreaterThan(1e-12),
                    "The physical mineral field must not collapse a stone or shoulder onto one texture row.");
            }
        }

        [TestCase(-6.6f)]
        [TestCase(14.15f)]
        public void JoinedButtressLowerMassSeatsEveryFollowingCourseOnItsActualChamferedCap(float v)
        {
            var foot=new Vector4(31.05f+FoundryMap.GateOffset,v,34.9f+FoundryMap.GateOffset,v+2.45f);
            object built=Builder();Call("BuildGateButtressLower",built,foot);
            MeshData actual=Read(built);AssertFinite(actual);
            var boxes=new[]{foot,new Vector4(foot.x+.11f,foot.y+.09f,foot.z-.11f,foot.w-.09f),
                new Vector4(foot.x+.20f,foot.y+.18f,foot.z-.20f,foot.w-.18f),
                new Vector4(foot.x+.48f,foot.y+.46f,foot.z-.48f,foot.w-.46f)};
            var levels=new[]{0f,.48f,2.05f,2.85f,6.55f};
            var insets=new[]{.08f,.09f,.28f,.065f};
            int offset=0;
            for(int tier=0;tier<3;tier++)
            {
                object part=Builder();Call("GateButtressTier",part,boxes[tier],levels[tier],levels[tier+1],insets[tier],tier==1?.90f:.94f);
                MeshData stone=Read(part);
                foreach(Vector3 point in stone.V)
                    Assert.That(Vector3.Distance(actual.V[offset++],point),Is.LessThan(.00001f),
                        "The supported tier under review must actually be emitted by the production lower mass.");
                // GateButtressTier retains the original final eight cap patches.
                int capStart=stone.V.Count-32;
                var cap=new MeshData(stone.V,stone.U,stone.C,new List<int>());
                for(int t=0;t<stone.T.Count;t+=3)
                    if(stone.T[t]>=capStart&&stone.T[t+1]>=capStart&&stone.T[t+2]>=capStart)
                        for(int k=0;k<3;k++)cap.T.Add(stone.T[t+k]);
                Vector4 upper=boxes[tier+1];
                float corner=Mathf.Min(.18f,Mathf.Min(upper.z-upper.x,upper.w-upper.y)*.15f);
                var outline=(Vector3[])Call("Octagon",upper.x,upper.y,upper.z,upper.w,corner);
                for(int i=0;i<outline.Length;i++)
                    foreach(Vector3 point in new[]{outline[i],(outline[i]+outline[(i+1)%outline.Length])*.5f})
                        Assert.That(LastCoveringTriangle(cap,point+Vector3.up*levels[tier+1]),Is.GreaterThanOrEqualTo(0),
                            "Every upper section corner and midpoint must bear on real stone, including chamfered corners.");
            }
            Assert.That(offset,Is.EqualTo(actual.V.Count));
        }

        private static double CrossDouble(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        private static double TriangleArea(MeshData mesh)
        {
            double area=0;
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                Vector2 a=mesh.V[mesh.T[t]],b=mesh.V[mesh.T[t+1]],c=mesh.V[mesh.T[t+2]];
                area+=Math.Abs(CrossDouble(b-a,c-a))*.5;
            }
            return area;
        }
        private static int CoverCount(MeshData mesh,Vector2 p)
        {
            int count=0;
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                Vector2 a=mesh.V[mesh.T[t]],b=mesh.V[mesh.T[t+1]],c=mesh.V[mesh.T[t+2]];
                double area=CrossDouble(b-a,c-a);if(Math.Abs(area)<1e-9)continue;
                double x=CrossDouble(b-p,c-p)/area,y=CrossDouble(c-p,a-p)/area,z=1-x-y;
                if(x>=-1e-7&&y>=-1e-7&&z>=-1e-7)count++;
            }
            return count;
        }

        [TestCase(-6.6f)]
        [TestCase(14.15f)]
        public void OuterGateTowerKeepsItsFootprintAndExposesTheBayAboveTheSeatedShoulder(float v)
        {
            float u=31.05f+FoundryMap.GateOffset;
            var foot=new Vector4(u,v,u+3.85f,v+2.45f);
            object built=Builder();Call("BuildGateOuterButtress",built,foot);
            MeshData mesh=Read(built);AssertFinite(mesh);
            object envelope=Builder();Call("LitFoundryTier",envelope,foot.x,foot.y,foot.z,foot.w,0f,7.35f,0f);
            MeshData hull=Read(envelope);
            foreach(Vector3 point in mesh.V)Assert.That(LastCoveringTriangle(hull,point),Is.GreaterThanOrEqualTo(0),
                "The stronger tower body must stay inside its established footing and crown envelope.");
            Assert.That(mesh.V.Count,Is.LessThan(2400),"A longer bay must replace the waist rather than accumulate tiny trim.");
            var box=new Vector4(foot.x+.48f,foot.y+.46f,foot.z-.48f,foot.w-.46f);
            object isolated=Builder();Call("GateRecessedShaft",isolated,box,2.85f,6.55f,.065f,.92f,.36f);
            MeshData bay=Read(isolated);
            object shell=Builder();Call("LitFoundryTier",shell,box.x,box.y,box.z,box.w,2.85f,6.55f,.065f);
            MeshData plane=Read(shell);
            foreach(int side in new[]{0,1})
            {
                int first=side*4;
                Vector3 inward=side==0?Project(box.x,box.y+.36f)-Project(box.x,box.y):
                    Project(box.x+.36f,box.y)-Project(box.x,box.y);
                // Probe the shortened upper bay above its new broad shoulder. The
                // complete tower's last visible triangle must remain its real recess.
                foreach(float rise in new[]{.30f,.37f,.44f,.51f,.58f})
                {
                    Vector3 low=Vector3.Lerp(plane.V[first],plane.V[first+1],.5f);
                    Vector3 high=Vector3.Lerp(plane.V[first+3],plane.V[first+2],.5f);
                    Vector3 point=Vector3.Lerp(low,high,rise)+inward;
                    int expected=LastCoveringTriangle(bay,point),actual=LastCoveringTriangle(mesh,point);
                    Assert.That(expected,Is.GreaterThanOrEqualTo(0));Assert.That(actual,Is.GreaterThanOrEqualTo(0));
                    for(int k=0;k<3;k++)Assert.That(Vector3.Distance(mesh.V[mesh.T[actual+k]],bay.V[bay.T[expected+k]]),
                        Is.LessThan(.00001f),"No surviving full storey or corner applique may cover the new opening.");
                }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void JambRebateHasAClosedBearingWithoutFillingItsConcaveCutOrEnteringTheFacade(bool near)
        {
            float a=near?-1.58f:11.04f,b=near?-1.04f:11.58f;
            var box=new Vector4(32.10f+FoundryMap.GateOffset,a+.075f,32.96f+FoundryMap.GateOffset,b-.075f);
            const float bottom=1.15f,top=6.12f;
            object built=Builder();Call("BuildGateJambRebate",built,box,bottom,top,near);
            MeshData mesh=Read(built);AssertFinite(mesh);
            Assert.That(mesh.V.Count,Is.LessThanOrEqualTo(400));
            // Independently reconstruct the original body, including its inset
            // and real bevel. A new square box is not a valid bearing oracle.
            object envelope=Builder();Call("LitFoundryTier",envelope,32.04f+FoundryMap.GateOffset,a+.025f,
                33.04f+FoundryMap.GateOffset,b-.025f,bottom,top,.025f);
            MeshData hull=Read(envelope);
            foreach(Vector3 point in mesh.V)Assert.That(LastCoveringTriangle(hull,point),Is.GreaterThanOrEqualTo(0));
            // The existing base's TOP is the support, not its larger lower box
            // or projected walls. Check every section corner and edge midpoint.
            object footing=Builder();Call("LitFoundryTier",footing,32.02f+FoundryMap.GateOffset,a,
                33.04f+FoundryMap.GateOffset,b,.45f,bottom,.03f);
            MeshData baseMesh=Read(footing);
            var baseCap=new MeshData(baseMesh.V,baseMesh.U,baseMesh.C,new List<int>());
            for(int t=0;t<baseMesh.T.Count;t+=3)
                if(baseMesh.T[t]>=12&&baseMesh.T[t+1]>=12&&baseMesh.T[t+2]>=12)
                    for(int k=0;k<3;k++)baseCap.T.Add(baseMesh.T[t+k]);
            var section=(Vector2[])Call("GateJambSection",box,near);
            for(int i=0;i<section.Length;i++)
                foreach(Vector2 foot in new[]{section[i],(section[i]+section[(i+1)%section.Length])*.5f})
                    Assert.That(LastCoveringTriangle(baseCap,Project(foot.x,foot.y)+Vector3.up*bottom),
                        Is.GreaterThanOrEqualTo(0),"Every rebate corner must bear on the actual chamfered base cap.");
            // The final two cap quads are the actual L-shaped bearing. Invert their
            // projected plane to test coverage, including the absent front corner.
            var cap=new MeshData(new List<Vector3>(),new List<Vector2>(),new List<Color>(),new List<int>());
            int start=mesh.V.Count-8;
            for(int i=start;i<mesh.V.Count;i++)
            {
                Vector3 floor=mesh.V[i]-Vector3.up*top;
                object[] coordinates={floor.x,floor.y,0f,0f};
                typeof(FoundryRegionView).Assembly.GetType("Cryptforge.UI.FoundrySurface")
                    .GetMethod("ToDesign",Static).Invoke(null,coordinates);
                float u=(float)coordinates[2],v=(float)coordinates[3];
                cap.V.Add(new Vector3(u,v,0));
                Assert.That(u,Is.InRange(box.x-.00001f,box.z+.00001f));
                Assert.That(v,Is.InRange(box.y-.00001f,box.w+.00001f));
                Assert.That(v< -1||v>11,Is.True,"Stone stays outside all painted banner/door pixels.");
            }
            foreach(int index in mesh.T)if(index>=start)cap.T.Add(index-start);
            float front=box.x+(box.z-box.x)*.15f,rear=box.x+(box.z-box.x)*.85f;
            float cut=near?Mathf.Lerp(box.y,box.w,.90f):Mathf.Lerp(box.y,box.w,.10f);
            float spine=near?Mathf.Lerp(box.y,box.w,.20f):Mathf.Lerp(box.y,box.w,.80f);
            Assert.That(LastCoveringTriangle(cap,new Vector2(front,cut)),Is.EqualTo(-1),"A cap fan must not bridge across the real reveal.");
            Assert.That(LastCoveringTriangle(cap,new Vector2(rear,cut)),Is.GreaterThanOrEqualTo(0));
            Assert.That(LastCoveringTriangle(cap,new Vector2(front,spine)),Is.GreaterThanOrEqualTo(0));
            Assert.That(LastCoveringTriangle(cap,new Vector2(rear,spine)),Is.GreaterThanOrEqualTo(0));
        }

        private static Vector3 Project(float u,float v)=>(Vector3)Call("Project",u,v);

        [TestCase(.25f,.125f)]
        [TestCase(-.25f,.125f)]
        public void RecessInteriorRemainsExposedAfterAllFrontStonesArePainted(float dx,float dy)
        {
            object builder=Builder();
            var face=new[]{new Vector3(0,0,0),new Vector3(4,0,0),new Vector3(3.8f,5,0),new Vector3(.2f,5,0)};
            Call("GateRecessedFace",builder,face,new Vector3(dx,dy,0),new Color(.6f,.64f,.72f));
            MeshData mesh=Read(builder);
            AssertFinite(mesh);
            // Probe the visible throat, not just vertices or an authored marker.
            // The last covering triangle must be backing or a connected return;
            // later bearing/frame triangles must not close the intended opening.
            for(int x=0;x<=8;x++)for(int y=0;y<=10;y++)
            {
                var point=new Vector2(Mathf.Lerp(1.53f,2.47f,x/8f),Mathf.Lerp(1.4f,3.1f,y/10f));
                int last=LastCoveringTriangle(mesh,point);
                Assert.That(last,Is.GreaterThanOrEqualTo(0),"A blind recess cannot contain a missing face.");
                Assert.That(mesh.T[last],Is.LessThan(40),"Front masonry must not overpaint the throat.");
            }
            // The complete original wall still has a continuous projected cover,
            // including every frame return and both upper shoulder junctions.
            for(int x=1;x<25;x++)for(int y=1;y<25;y++)
            {
                Vector3 low=Vector3.Lerp(face[0],face[1],x/25f),high=Vector3.Lerp(face[3],face[2],x/25f);
                Assert.That(LastCoveringTriangle(mesh,Vector3.Lerp(low,high,y/25f)),Is.GreaterThanOrEqualTo(0));
            }
        }

        [TestCase(28.26f,-2.76f,30.74f,-1.24f,1.70f,5.45f,.04f,.28f)]
        [TestCase(27.53f,-6.14f,30.42f,-4.61f,2.85f,6.55f,.065f,.36f)]
        public void ProductionSplayedFramesConsumeExistingDepthAndMeetTheirUnmovedBacking(
            float u0,float v0,float u1,float v1,float bottom,float top,float inset,float depth)
        {
            object original=Builder();Call("LitFoundryTier",original,u0,v0,u1,v1,bottom,top,inset);
            MeshData shell=Read(original);
            var opening=new[]{new Vector2(.28f,.17f),new Vector2(.72f,.17f),new Vector2(.72f,.69f),
                new Vector2(.5f,.84f),new Vector2(.28f,.69f)};
            var frame=new[]{new Vector2(.14f,.08f),new Vector2(.86f,.08f),new Vector2(.86f,.75f),
                new Vector2(.5f,.94f),new Vector2(.14f,.75f)};
            for(int side=0;side<2;side++)
            {
                int first=side*4;
                var face=new[]{shell.V[first],shell.V[first+1],shell.V[first+2],shell.V[first+3]};
                Vector3 At(Vector2 p)=>Vector3.Lerp(Vector3.Lerp(face[0],face[1],p.x),Vector3.Lerp(face[3],face[2],p.x),p.y);
                Vector3 inward=side==0?Project(u0,v0+depth)-Project(u0,v0):Project(u0+depth,v0)-Project(u0,v0);
                object built=Builder();Call("GateRecessedFace",built,face,inward,new Color(.6f,.64f,.72f));
                MeshData mesh=Read(built);AssertFinite(mesh);AssertMineralGrain(mesh);
                Assert.That(mesh.V.Count,Is.EqualTo(240),"Splay replaces the flat frame; it does not append another architectural layer.");
                // Backing fan and connected return slots retain their existing
                // ownership order. Only each return's front edge moves inward.
                for(int edge=0;edge<5;edge++)
                {
                    int next=(edge+1)%5,back=edge*4,returnStart=20+edge*4;
                    Vector3 a=At(opening[edge]),b=At(opening[next]);
                    Assert.That(Vector3.Distance(mesh.V[back],a+inward),Is.LessThan(.000001f));
                    Assert.That(Vector3.Distance(mesh.V[back+1],b+inward),Is.LessThan(.000001f));
                    Assert.That(Vector3.Distance(mesh.V[returnStart],a+inward*.25f),Is.LessThan(.000001f));
                    Assert.That(Vector3.Distance(mesh.V[returnStart+1],b+inward*.25f),Is.LessThan(.000001f));
                    Assert.That(Vector3.Distance(mesh.V[returnStart+2],mesh.V[back+1]),Is.LessThan(.000001f));
                    Assert.That(Vector3.Distance(mesh.V[returnStart+3],mesh.V[back]),Is.LessThan(.000001f));
                    Assert.That(Vector3.Distance(mesh.V[returnStart],a),Is.GreaterThan(.045f),
                        "The production arris must actually recede from the old flat wall.");
                    foreach(Vector3 point in new[]{mesh.V[returnStart],mesh.V[returnStart+1],At(frame[edge]),At(frame[next])})
                    {
                        bool shared=false;
                        for(int i=140;i<mesh.V.Count;i++)shared|=Vector3.Distance(mesh.V[i],point)<.000001f;
                        Assert.That(shared,Is.True,"Every splayed frame meets the unchanged exterior and shortened return at exact shared stations.");
                    }
                }
                // The five visible frame stones partition their sloping faces.
                // Their original exterior edges are fixed; no old flat polygon
                // survives beneath the new splay or covers the visible throat.
                var dressed=new MeshData(mesh.V,mesh.U,mesh.C,new List<int>());
                for(int t=0;t<mesh.T.Count;t+=3)
                    if(mesh.T[t]>=140&&mesh.T[t+1]>=140&&mesh.T[t+2]>=140)
                        dressed.T.AddRange(new[]{mesh.T[t],mesh.T[t+1],mesh.T[t+2]});
                double expectedArea=0;
                for(int edge=0;edge<5;edge++)
                {
                    int next=(edge+1)%5;
                    Vector3 a=At(frame[edge]),b=At(frame[next]),c=At(opening[next])+inward*.25f,d=At(opening[edge])+inward*.25f;
                    expectedArea+=(Math.Abs(CrossDouble(b-a,c-a))+Math.Abs(CrossDouble(c-a,d-a)))*.5;
                    foreach(float s in new[]{.217f,.573f,.839f})foreach(float h in new[]{.173f,.493f,.811f})
                    {
                        Vector3 point=Vector3.Lerp(Vector3.Lerp(a,b,s),Vector3.Lerp(d,c,s),h);
                        Assert.That(CoverCount(dressed,point),Is.EqualTo(1),"Each physical frame face has one owner, including its inset arrises.");
                    }
                    // Compare with the same ordinary dressed stone emitter, so
                    // relief light must live on the rendered splay, preserving
                    // both its geometry and the original forge-tint ratios.
                    Color tint=new Color(.6f,.64f,.72f)*(edge==0?1.12f:edge==2||edge==3?1.07f:1.04f);tint.a=1;
                    object plainBuilder=Builder();Call("GateRecessDressedFace",plainBuilder,a,b,c,d,tint,.055f,edge+5);
                    MeshData plain=Read(plainBuilder);int offset=140+edge*20;
                    var referenced=new HashSet<int>();
                    for(int t=0;t<plain.T.Count;t+=3)
                    {
                        int i=plain.T[t],j=plain.T[t+1],k=plain.T[t+2];
                        if(Math.Abs(CrossDouble(plain.V[j]-plain.V[i],plain.V[k]-plain.V[i]))<=1e-8)continue;
                        referenced.Add(i);referenced.Add(j);referenced.Add(k);
                    }
                    int outerSamples=0,innerSamples=0;float innerTotal=0;
                    foreach(int index in referenced)
                    {
                        Assert.That(Vector3.Distance(mesh.V[offset+index],plain.V[index]),Is.LessThan(.000001f));
                        Color actual=mesh.C[offset+index],source=plain.C[index];
                        float gain=actual.r/source.r;
                        Assert.That(gain,Is.InRange(.75f,1.00001f),"Splay shading cannot globally brighten or blacken the frame.");
                        Assert.That(actual.g/source.g,Is.EqualTo(gain).Within(.00001f));
                        Assert.That(actual.b/source.b,Is.EqualTo(gain).Within(.00001f));
                        Vector3 p=plain.V[index];
                        if(Vector3.Distance(p,a)<.000001f||Vector3.Distance(p,b)<.000001f)
                        {Assert.That(gain,Is.EqualTo(1).Within(.00001f));outerSamples++;}
                        if(Vector3.Distance(p,c)<.000001f||Vector3.Distance(p,d)<.000001f)
                        {innerTotal+=gain;innerSamples++;}
                    }
                    Assert.That(outerSamples,Is.GreaterThanOrEqualTo(2));
                    Assert.That(innerSamples,Is.GreaterThanOrEqualTo(2));
                    float inner=innerTotal/innerSamples;
                    if(edge==0)Assert.That(inner,Is.EqualTo(1).Within(.00001f),"The sill keeps its original reflected light.");
                    else if(edge==1||edge==2)Assert.That(inner,Is.LessThan(.83f),"Far returns visibly recede instead of retaining the old flat frame tint.");
                    else Assert.That(inner,Is.GreaterThan(.87f).And.LessThan(1),"Near planes remain distinguishable from the darker far reveal.");
                }
                Assert.That(TriangleArea(dressed),Is.EqualTo(expectedArea).Within(.0001));
                // A translated backing's physical centre need not be visible:
                // the narrow production face already hid it behind the far jamb
                // before the splay. The legitimate visible backing is the convex
                // intersection of its rear contour and the front throat. Test
                // its entire projected area against actual emitted triangles.
                var rear=new List<(double x,double y)>();
                var flat=new List<(double x,double y)>();
                var throat=new List<(double x,double y)>();
                foreach(Vector2 station in opening)
                {
                    Vector3 p=At(station),back=p+inward,front=p+inward*.25f;
                    flat.Add((p.x,p.y));rear.Add((back.x,back.y));throat.Add((front.x,front.y));
                }
                var visible=ClipProjectedPolygon(rear,throat);
                var oldVisible=ClipProjectedPolygon(rear,flat);
                double visibleArea=Math.Abs(ProjectedPolygonArea(visible));
                double oldArea=Math.Abs(ProjectedPolygonArea(oldVisible));
                Assert.That(oldArea,Is.GreaterThan(.01),$"Side {side}: the baseline has a real visible backing.");
                Assert.That(visibleArea,Is.GreaterThan(oldArea),$"Side {side}: the splay must expose more of the unchanged backing.");
                Assert.That(Math.Abs(ProjectedPolygonArea(ClipProjectedPolygon(oldVisible,visible))),
                    Is.EqualTo(oldArea).Within(1e-8),$"Side {side}: no part of the old visible backing may be lost.");
                double covered=0,overpaint=0;
                for(int t=0;t<mesh.T.Count;t+=3)
                {
                    var triangle=new List<(double x,double y)>();
                    for(int k=0;k<3;k++)
                    {
                        Vector3 p=mesh.V[mesh.T[t+k]];triangle.Add((p.x,p.y));
                    }
                    if(Math.Abs(ProjectedPolygonArea(triangle))<=1e-12)continue;
                    double area=Math.Abs(ProjectedPolygonArea(ClipProjectedPolygon(visible,triangle)));
                    if(mesh.T[t]<20)covered+=area;else overpaint+=area;
                }
                Assert.That(covered,Is.EqualTo(visibleArea).Within(1e-8),
                    $"Side {side}: actual backing triangles must cover the complete visible domain without a hole.");
                Assert.That(overpaint,Is.LessThan(1e-8),
                    $"Side {side}: no later return, body or frame triangle may overpaint any visible backing area.");
            }
        }

        // Independent double-precision convex clipping keeps the area oracle
        // separate from production emitters and point-in-triangle tolerances.
        private static List<(double x,double y)> ClipProjectedPolygon(
            IReadOnlyList<(double x,double y)> subject,IReadOnlyList<(double x,double y)> window)
        {
            var result=new List<(double x,double y)>(subject);
            double winding=Math.Sign(ProjectedPolygonArea(window));
            for(int edge=0;edge<window.Count&&result.Count>0;edge++)
            {
                var a=window[edge];var b=window[(edge+1)%window.Count];
                var source=result;result=new List<(double x,double y)>();
                double Distance((double x,double y) p)=>winding*((b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x));
                for(int i=0;i<source.Count;i++)
                {
                    var p=source[i];var q=source[(i+1)%source.Count];
                    double dp=Distance(p),dq=Distance(q);
                    if(dp>=0)result.Add(p);
                    if((dp>=0)!=(dq>=0))
                    {
                        double t=dp/(dp-dq);
                        result.Add((p.x+(q.x-p.x)*t,p.y+(q.y-p.y)*t));
                    }
                }
            }
            return result;
        }

        private static double ProjectedPolygonArea(IReadOnlyList<(double x,double y)> polygon)
        {
            if(polygon.Count<3)return 0;
            var origin=polygon[0];double area=0;
            for(int i=1;i<polygon.Count-1;i++)
            {
                var a=polygon[i];var b=polygon[i+1];
                area+=(a.x-origin.x)*(b.y-origin.y)-(a.y-origin.y)*(b.x-origin.x);
            }
            return area*.5;
        }

        [TestCase(28.26f,-2.76f,30.74f,-1.24f,1.70f,5.45f,.04f,.28f)]
        [TestCase(28.26f,11.24f,30.74f,12.76f,1.70f,5.45f,.04f,.28f)]
        [TestCase(27.53f,-6.14f,30.42f,-4.61f,1.84f,6.55f,.065f,.36f)]
        [TestCase(27.53f,14.61f,30.42f,16.14f,1.84f,6.55f,.065f,.36f)]
        public void RecessedShaftKeepsOriginalTaperedPrismAndClosedCornerAndCap(
            float u0,float v0,float u1,float v1,float bottom,float top,float inset,float depth)
        {
            object baseline=Builder();Call("LitFoundryTier",baseline,u0,v0,u1,v1,bottom,top,inset);
            MeshData old=Read(baseline);
            object builder=Builder();Call("GateRecessedShaft",builder,new Vector4(u0,v0,u1,v1),bottom,top,inset,.96f,depth);
            MeshData mesh=Read(builder);
            AssertFinite(mesh);
            Assert.That(mesh.V.Count,Is.LessThanOrEqualTo(550),"The rebate must remain a bounded static structure.");
            // The existing prism is convex in projection. Every new return and
            // backing point must remain inside its original visible silhouette.
            foreach(Vector3 point in mesh.V)
                Assert.That(LastCoveringTriangle(old,point),Is.GreaterThanOrEqualTo(0),
                    "New geometry must not spill outside the existing prism's projection.");
            int kept=old.V.Count-8;
            for(int i=0;i<kept;i++)
                Assert.That(Vector3.Distance(mesh.V[mesh.V.Count-kept+i],old.V[8+i]),Is.LessThan(.00001f),
                    "The chamfer and cap contacts are retained exactly.");
        }

        private static int LastCoveringTriangle(MeshData mesh,Vector2 p)
        {
            int last=-1;
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                Vector2 a=mesh.V[mesh.T[t]],b=mesh.V[mesh.T[t+1]],c=mesh.V[mesh.T[t+2]];
                float area=Cross(b-a,c-a);if(Mathf.Abs(area)<1e-8f)continue;
                float x=Cross(b-p,c-p)/area,y=Cross(c-p,a-p)/area,z=1-x-y;
                if(x>=-.0001f&&y>=-.0001f&&z>=-.0001f)last=t;
            }
            return last;
        }
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
        private static MeshData Read(object builder)
        {
            Type type=builder.GetType();
            return new MeshData((List<Vector3>)type.GetField("V",Fields).GetValue(builder),
                (List<Vector2>)type.GetField("U",Fields).GetValue(builder),
                (List<Color>)type.GetField("C",Fields).GetValue(builder),
                (List<int>)type.GetField("T",Fields).GetValue(builder));
        }
        private static void AssertFinite(MeshData mesh)
        {
            Assert.That(mesh.V.Count,Is.EqualTo(mesh.U.Count));Assert.That(mesh.V.Count,Is.EqualTo(mesh.C.Count));
            foreach(Vector3 p in mesh.V)Assert.That(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||
                float.IsNaN(p.z)||float.IsInfinity(p.z),Is.False);
            foreach(Vector2 p in mesh.U)Assert.That(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y),Is.False);
            foreach(Color c in mesh.C)
                Assert.That(float.IsNaN(c.r)||float.IsInfinity(c.r)||float.IsNaN(c.g)||float.IsInfinity(c.g)||
                    float.IsNaN(c.b)||float.IsInfinity(c.b)||float.IsNaN(c.a)||float.IsInfinity(c.a),Is.False);
            foreach(int index in mesh.T)Assert.That(index,Is.InRange(0,mesh.V.Count-1));
        }
        private sealed class MeshData
        {
            internal readonly List<Vector3> V;internal readonly List<Vector2> U;
            internal readonly List<Color> C;internal readonly List<int> T;
            internal MeshData(List<Vector3> v,List<Vector2> u,List<Color> c,List<int> t){V=v;U=u;C=c;T=t;}
        }
    }
}
