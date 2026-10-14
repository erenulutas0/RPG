using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryServiceShaftTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private const float Epsilon=.0001f;

        [TestCase(1,12f,42f,3.7f,3.7f,-5.4f,-26f)]
        [TestCase(2,-5f,39f,4.2f,3.8f,-6f,-25f)]
        public void AuthoredServiceSkylineLowersCrownsAndRetainsFoundation(int variant,float u,float v,
            float width,float depth,float top,float bottom)
        {
            object stone=Builder(),fire=Builder();
            Call("DistantServiceTower",stone,fire,Color.white,variant);
            AssertMesh(stone);AssertMesh(fire);
            float u0=u-width*.5f,u1=u+width*.5f,v0=v-depth*.5f,v1=v+depth*.5f;
            // These independently authored limits test the actual production
            // configuration, not a test reconstruction of its top constants.
            // Foundation feet stay fixed; each unequal crown must reach its new
            // lower datum without merely disappearing or fading into the haze.
            AssertEnvelope(stone,u0-.24f,v0-.24f,u1+.16f,v1+.06f,bottom,top-(variant==2?.4f:0));
            AssertEnvelope(fire,u0-.24f,v0-.24f,u1+.16f,v1+.06f,bottom,top);
            var points=Field<List<Vector3>>(stone,"V");
            AssertPoint(points,Project(u0+.2f,v0+.2f)+Vector3.up*bottom);
            AssertPoint(points,Project(u1-.2f,v0+.2f)+Vector3.up*bottom);
            AssertPoint(points,Project(u0+.2f,v1-.2f)+Vector3.up*bottom);
            float neck=variant==1?.94f:1.02f,crown=top-(variant==2?.4f:0);
            AssertPoint(points,Project(u0+neck-.12f,v0+neck-.12f)+Vector3.up*crown);
            AssertPoint(points,Project(u1-neck+.12f,v1-neck+.12f)+Vector3.up*crown);
        }

        [TestCase(1,3.7f,3.7f)]
        [TestCase(2,4.2f,3.8f)]
        public void HeadIsOneConnectedAssemblySeatedOnLowerShoulder(int variant,float width,float depth)
        {
            const float u0=3.1f,v0=7.2f,top=-2f;
            float u1=u0+width,v1=v0+depth,shoulder=(float)Call("ServiceShoulder",top,variant);
            object head=Builder();Call("DistantFurnaceHead",head,u0,v0,u1,v1,top,Color.white,variant);
            AssertMesh(head,finalColors:false);
            AssertEnvelope(head,u0-.06f,v0-.06f,u1+.06f,v1+.06f,shoulder,top-(variant==2?.4f:0));
            var points=Field<List<Vector3>>(head,"V");
            AssertPoint(points,Project(u0-.06f,v0-.06f)+Vector3.up*shoulder);
            AssertPoint(points,Project(u1+.06f,v0-.06f)+Vector3.up*shoulder);
            AssertPoint(points,Project(u0-.06f,v1+.06f)+Vector3.up*shoulder);
            AssertConnected(head);
        }

        [TestCase(1,12f,42f,3.7f,3.7f,-5.4f,-26f)]
        [TestCase(2,-5f,39f,4.2f,3.8f,-6f,-25f)]
        public void CompressedServiceBayKeepsSixSeparatedFirePanesWithinRecess(int variant,float u,float v,
            float width,float depth,float top,float bottom)
        {
            object stone=Builder(),fire=Builder();
            Call("DistantServiceTower",stone,fire,Color.white,variant);
            var points=Field<List<Vector3>>(fire,"V");
            var uv=Field<List<Vector2>>(fire,"U");
            float u0=u-width*.5f,u1=u+width*.5f,v0=v-depth*.5f,v1=v+depth*.5f;
            float sill=(float)Call("ServiceSill",top,variant),head=(float)Call("ServiceShoulder",top,variant);
            // Fixed lower sills retain the original footing/bay contact; the
            // upper bay is deliberately shorter. Six panes retain real gaps and
            // remain below the new spring line rather than rising into the head.
            Assert.That(sill,Is.EqualTo(variant==1?-18.2f:-20.2f).Within(Epsilon));
            var expected=new List<Vector3>();
            foreach(bool west in new[]{true,false})
            {
                float span=west?depth:width,spring=head-.38f-.21f*span;
                float fireBottom=sill+1.05f,step=(spring-.55f-fireBottom)/3f;
                float paneHeight=Mathf.Min(1.65f,step*.57f);
                Assert.That(paneHeight,Is.GreaterThan(.8f));
                Assert.That(step-paneHeight,Is.GreaterThan(.6f),"Open gaps must survive compression.");
                Assert.That(fireBottom+step*2+paneHeight,Is.LessThan(spring-.55f));
                Vector3 At(float t,float z)=>(west?Project(u0+.452f,Mathf.Lerp(v1,v0,t)):
                    Project(Mathf.Lerp(u0,u1,t),v0+.452f))+Vector3.up*z;
                for(int pane=0;pane<3;pane++)
                {
                    float low=fireBottom+pane*step,high=low+paneHeight,mid=(low+high)*.5f;
                    foreach(var ends in new[]{new Vector2(low,mid),new Vector2(mid,high)})
                    {
                        expected.Add(At(.466f,ends.x));expected.Add(At(.534f,ends.x));
                        expected.Add(At(.534f,ends.y));expected.Add(At(.466f,ends.y));
                    }
                }
            }
            Assert.That(points.Count,Is.EqualTo(expected.Count));
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(Vector3.Distance(points[i],expected[i]),Is.LessThan(Epsilon),"Fire pane escaped its compressed bay contract.");
                Assert.That(uv[i].x,Is.InRange(.7815f-Epsilon,.7825f+Epsilon));
                Assert.That(uv[i].y,Is.InRange(.8375f-Epsilon,.8385f+Epsilon));
            }
        }

        [TestCase(1,true)]
        [TestCase(1,false)]
        [TestCase(2,true)]
        [TestCase(2,false)]
        public void AuthoredBayPiersReplaceFlatFieldsWithClosedRecessesAndContinuousMasonry(int variant,bool west)
        {
            float u=variant==1?12:-5,v=variant==1?42:39;
            float width=variant==1?3.7f:4.2f,depth=variant==1?3.7f:3.8f;
            float sill=variant==1?-18.2f:-20.2f,head=variant==1?-9.1f:-10.6f;
            float u0=u-width*.5f,u1=u+width*.5f,v0=v-depth*.5f,v1=v+depth*.5f;
            Vector3 At(float t,float z,float inset)=>(west?Project(u0+inset,Mathf.Lerp(v1,v0,t)):
                Project(Mathf.Lerp(u0,u1,t),v0+inset))+Vector3.up*z;
            object facade=Builder(),fire=Builder();
            Call("DistantRecessFace",facade,fire,u0,v0,u1,v1,west,sill,head,.46f,Color.white);
            AssertMesh(facade,finalColors:false);
            var points=Field<List<Vector3>>(facade,"V");var uv=Field<List<Vector2>>(facade,"U");
            var indices=Field<List<int>>(facade,"T");var colors=Field<List<Color>>(facade,"C");
            // Inspect the two actual pier owners emitted by the production bay,
            // rather than constructing an isolated sample of the new helper.
            for(int pier=0;pier<2;pier++)
            {
                float left=pier==0?0:.71f,right=pier==0?.29f:1,side=(right-left)*.22f;
                int first=pier*8,indexStart=pier*30;
                var outer=new[]{At(left,sill,0),At(right,sill,0),At(right,head,0),At(left,head,0)};
                var inner=new[]{At(left+side,sill+.85f,.12f),At(right-side,sill+.85f,.12f),
                    At(right-side,head-.85f,.12f),At(left+side,head-.85f,.12f)};
                var faces=indices.GetRange(indexStart,30);var used=new HashSet<int>(faces);
                Assert.That(used.Count,Is.EqualTo(8),"Four returns and one back must share their two indexed contours.");
                foreach(int index in used)Assert.That(index,Is.InRange(first,first+7));
                double total=0,expected=Area(outer[0],outer[1],outer[2])+Area(outer[0],outer[2],outer[3]);
                for(int edge=0;edge<4;edge++)
                {
                    int next=(edge+1)%4;
                    Assert.That(Vector3.Distance(points[first+edge],outer[edge]),Is.LessThan(Epsilon));
                    Assert.That(Vector3.Distance(points[first+4+edge],inner[edge]),Is.LessThan(Epsilon));
                    Assert.That(EdgeIncidence(points,faces,outer[edge],outer[next]),Is.EqualTo(1),
                        "The original rectangular footprint has one owner, with no duplicate flat wall.");
                    Assert.That(EdgeIncidence(points,faces,inner[edge],inner[next]),Is.EqualTo(2),
                        "Every inset back edge must join its bearing return.");
                }
                foreach(int index in used)
                {
                    Vector3 p=points[index];
                    for(int edge=0;edge<4;edge++)Assert.That(Area(outer[edge],outer[(edge+1)%4],p),
                        Is.GreaterThanOrEqualTo(-1e-8),"No new pier surface may escape the retained projected wall.");
                    Assert.That(uv[index].x,Is.EqualTo(p.x/2.1f).Within(.00001f));
                    Assert.That(uv[index].y,Is.EqualTo((p.y-(west?-.5f:.5f)*p.x)/1.8f).Within(.00001f),
                        "The inset and bearing must continue the existing facade field, not restart its atlas.");
                }
                for(int i=0;i<faces.Count;i+=3)
                {
                    double area=Area(points[faces[i]],points[faces[i+1]],points[faces[i+2]]);
                    Assert.That(area,Is.GreaterThan(1e-7),"A reversed return would overlap its recessed field in the fixed view.");
                    total+=area;
                }
                Assert.That(total,Is.EqualTo(expected).Within(1e-8),
                    "The closed pier partition must cover exactly its former wall area, without a second base layer.");
                for(int row=0;row<9;row++)for(int column=0;column<9;column++)
                {
                    Vector3 sample=At(Mathf.Lerp(left,right,(column+.317f)/9),Mathf.Lerp(sill,head,(row+.583f)/9),0);
                    int coverage=0;
                    for(int i=0;i<faces.Count;i+=3)
                    {
                        Vector3 a=points[faces[i]],b=points[faces[i+1]],c=points[faces[i+2]];
                        double area=Area(a,b,c),x=Area(sample,b,c)/area,y=Area(a,sample,c)/area,z=1-x-y;
                        if(x>1e-9&&y>1e-9&&z>1e-9)coverage++;
                    }
                    Assert.That(coverage,Is.EqualTo(1),"Actual wall interiors have one visible pier owner.");
                }
                for(int corner=0;corner<4;corner++)
                {
                    float height=corner<2?sill+.85f:head-.85f;
                    float original=Mathf.Lerp(.88f,1.03f,Mathf.InverseLerp(sill,head,height));
                    Assert.That(colors[first+4+corner].r/original,Is.EqualTo(.78f).Within(.00001f),
                        "A broad recessed field must remain visibly subordinate to its outer bearing.");
                    Assert.That(colors[first+4+corner].a,Is.EqualTo(1));
                }
            }
        }

        [TestCase(1,true)]
        [TestCase(1,false)]
        [TestCase(2,true)]
        [TestCase(2,false)]
        public void ServiceBayQuarryValueSeparatesExposedBearingsWithoutLightingShelteredReturns(int variant,bool west)
        {
            float width=variant==1?3.7f:4.2f,depth=variant==1?3.7f:3.8f;
            float sill=variant==1?-18.2f:-20.2f,head=variant==1?-9.1f:-10.6f;
            var tone=new Color(.82f,.67f,.53f,.6f);object stone=Builder(),fire=Builder();
            Call("DistantRecessFace",stone,fire,3.1f,7.2f,3.1f+width,7.2f+depth,west,sill,head,.46f,tone);
            var points=Field<List<Vector3>>(stone,"V");var uv=Field<List<Vector2>>(stone,"U");
            var colors=Field<List<Color>>(stone,"C");var indices=Field<List<int>>(stone,"T");
            Assert.That(points.Count,Is.EqualTo(260));Assert.That(indices.Count,Is.EqualTo(426),
                "Value hierarchy must reuse the complete existing bay, with no new geometry.");
            for(int pier=0;pier<2;pier++)for(int corner=0;corner<4;corner++)
            {
                bool upper=corner>=2;int first=pier*8;
                float baseLight=upper?1.03f:.88f;
                Color original=tone*baseLight;original.a=tone.a;
                float gain=west?(upper?1.18f:1.12f):(upper?1.12f:1.08f);
                AssertNeutralValue(colors[first+corner],original,gain);
                float z=upper?head-.85f:sill+.85f;
                float innerLight=Mathf.Lerp(.88f,1.03f,Mathf.InverseLerp(sill,head,z));
                Color sheltered=tone*(innerLight*.78f);sheltered.a=tone.a;
                AssertNeutralValue(colors[first+4+corner],sheltered,1);
            }
            // The three original deep return/sill quads follow the two piers.
            // Their values and alpha stay exact while the outer bearings change.
            var returns=new[]{.64f,.80f,.85f};
            for(int face=0;face<3;face++)for(int corner=0;corner<4;corner++)
            {
                Color original=tone*returns[face];float alpha=original.a;
                original*=face==2?1f:corner<2?.88f:1.03f;original.a=alpha;
                AssertNeutralValue(colors[16+face*4+corner],original,1);
            }
            // Each outer spandrel has one unchanged deep arch return after it.
            for(int segment=0;segment<8;segment++)for(int corner=0;corner<4;corner++)
            {
                float light=corner<2?.88f:1.03f;
                Color outer=tone*(segment%3==0?.92f:1f),inner=tone*.60f;
                float outerAlpha=outer.a,innerAlpha=inner.a;outer*=light;inner*=light;
                outer.a=outerAlpha;inner.a=innerAlpha;
                AssertNeutralValue(colors[28+segment*8+corner],outer,west?1.08f:1.05f);
                AssertNeutralValue(colors[32+segment*8+corner],inner,1);
            }
            // The projecting dressed arch keeps its old directional values; it
            // must not turn into a brighter outline along with the broad pier.
            for(int segment=0;segment<8;segment++)for(int face=0;face<3;face++)for(int corner=0;corner<4;corner++)
            {
                float baseValue=face==0?(segment%3==0?1.27f:1.38f):face==1?.87f:1.10f;
                Color original=tone*baseValue;float alpha=original.a;
                original*=corner<2?.88f:1.03f;original.a=alpha;
                AssertNeutralValue(colors[92+segment*12+face*4+corner],original,1);
            }
            for(int i=0;i<points.Count;i++)
            {
                Vector3 p=points[i];Assert.That(uv[i].x,Is.EqualTo(p.x/2.1f).Within(.00001f));
                Assert.That(uv[i].y,Is.EqualTo((p.y-(west?-.5f:.5f)*p.x)/1.8f).Within(.00001f));
            }
        }

        [TestCase(1,3.7f,3.7f)]
        [TestCase(2,4.2f,3.8f)]
        public void CompleteServiceShaftKeepsCornerBearingGeometryAndHeadOutsideItsLocalQuarryFinish(int variant,float width,float depth)
        {
            const float u0=3.1f,v0=7.2f,top=-2,bottom=-24;
            float u1=u0+width,v1=v0+depth,shoulder=(float)Call("ServiceShoulder",top,variant);
            float sill=(float)Call("ServiceSill",top,variant),neck=variant==1?.94f:1.02f;
            var tone=new Color(.82f,.67f,.53f,.6f);object shaft=Builder(),fire=Builder();
            Call("DistantFurnaceShaft",shaft,fire,u0,v0,u1,v1,top,bottom,tone,variant);
            var points=Field<List<Vector3>>(shaft,"V");var uv=Field<List<Vector2>>(shaft,"U");
            var colors=Field<List<Color>>(shaft,"C");var indices=Field<List<int>>(shaft,"T");
            Assert.That(points.Count,Is.EqualTo(1208));Assert.That(indices.Count,Is.EqualTo(1884),
                "The complete service shaft retains its pre-finish construction budget.");
            // Forty-four foundation/core vertices and two 260-vertex bays precede
            // the two supported corner bearings. Compare their actual production
            // output with the unchanged block/taper geometry at the old tone.
            int start=564;
            foreach(float x in new[]{u0-.16f,u1-.46f})
            {
                object old=Builder();
                Call("DistantBlock",old,x,v0-.20f,x+.62f,v0+.44f,sill,shoulder-.75f,tone*.85f,true);
                float inward=(x<(u0+u1)*.5f?1:-1)*(neck-.15f);
                Call("DistantTaper",old,x,v0-.20f,x+.62f,v0+.44f,shoulder-.75f,
                    shoulder+(variant==1?1.1f:1.3f),.16f,tone*.94f,inward,neck+.04f);
                var oldPoints=Field<List<Vector3>>(old,"V");var oldUv=Field<List<Vector2>>(old,"U");
                var oldColors=Field<List<Color>>(old,"C");
                Assert.That(oldPoints.Count,Is.EqualTo(24));
                for(int i=0;i<24;i++)
                {
                    Assert.That(points[start+i],Is.EqualTo(oldPoints[i]));Assert.That(uv[start+i],Is.EqualTo(oldUv[i]));
                    AssertNeutralValue(colors[start+i],oldColors[i],i<12?1.07f:1.085f);
                }
                start+=24;
            }
            object head=Builder();Call("DistantFurnaceHead",head,u0,v0,u1,v1,top,tone,variant);
            CollectionAssert.AreEqual(Field<List<Vector3>>(head,"V"),points.GetRange(start,points.Count-start));
            CollectionAssert.AreEqual(Field<List<Vector2>>(head,"U"),uv.GetRange(start,uv.Count-start));
            CollectionAssert.AreEqual(Field<List<Color>>(head,"C"),colors.GetRange(start,colors.Count-start));
        }

        [TestCase(2.06f,2.06f,.9f,.22f)]
        [TestCase(2.4f,2f,.8f,.26f)]
        public void HollowExhaustHasFourConnectedInnerWallsAndNoCapAcrossMouth(float width,float depth,float height,float wall)
        {
            const float u0=2.3f,v0=5.7f,bottom=-1.4f;
            float top=bottom+height;
            object mesh=Builder();
            Call("DistantHollowExhaust",mesh,new Vector4(u0,v0,u0+width,v0+depth),bottom,top,wall,Color.white);
            AssertMesh(mesh,finalColors:false);AssertConnected(mesh);
            AssertEnvelope(mesh,u0,v0,u0+width,v0+depth,bottom,top);
            var inside=new[]{Project(u0+wall,v0+wall),Project(u0+width-wall,v0+wall),
                Project(u0+width-wall,v0+depth-wall),Project(u0+wall,v0+depth-wall)};
            var points=Field<List<Vector3>>(mesh,"V");
            var indices=Field<List<int>>(mesh,"T");
            for(int edge=0;edge<4;edge++)
            {
                int next=(edge+1)%4;
                Vector3 lowA=inside[edge]+Vector3.up*bottom,lowB=inside[next]+Vector3.up*bottom;
                Vector3 highA=inside[edge]+Vector3.up*top,highB=inside[next]+Vector3.up*top;
                AssertPoint(points,lowA);AssertPoint(points,highA);
                // Each mouth edge joins exactly one inner wall and one annulus
                // strip. A full cap introduces a third incident face; a missing
                // return or disconnected crown leaves only one.
                Assert.That(EdgeIncidence(points,indices,highA,highB),Is.EqualTo(2));
                Assert.That(HasTriangle(points,indices,lowA,lowB,highB),Is.True);
                Assert.That(HasTriangle(points,indices,lowA,highB,highA),Is.True);
            }
        }

        [TestCase(2.06f,2.06f,.9f,.22f)]
        [TestCase(2.4f,2f,.8f,.26f)]
        public void CrownBearingHasAJoinedRecessedFasciaWithinItsOriginalRimAndMouth(float width,float depth,float height,float wall)
        {
            const float u0=2.3f,v0=5.7f,bottom=-1.4f;
            float top=bottom+height;object mesh=Builder();
            Call("DistantHollowExhaust",mesh,new Vector4(u0,v0,u0+width,v0+depth),bottom,top,wall,Color.white);
            AssertMesh(mesh,finalColors:false);AssertConnected(mesh);
            AssertEnvelope(mesh,u0,v0,u0+width,v0+depth,bottom,top);
            var points=Field<List<Vector3>>(mesh,"V");var indices=Field<List<int>>(mesh,"T");
            Assert.That(points.Count,Is.EqualTo(124),"The profile replaces two dressed planes; it must not append another wall.");
            Assert.That(indices.Count,Is.EqualTo(186));
            float low=bottom+height*.18f,high=bottom+height*.72f;
            Vector3 lowerCorner=Project(u0+.075f,v0+.075f)+Vector3.up*low;
            Vector3 upperCorner=Project(u0+.075f,v0+.075f)+Vector3.up*high;
            AssertPoint(points,lowerCorner);AssertPoint(points,upperCorner);
            Assert.That(EdgeIncidence(points,indices,lowerCorner,upperCorner),Is.EqualTo(2),
                "Both visible fascia planes meet at one shared recessed corner, not detached fins.");
            foreach(float z in new[]{bottom,top})
            {
                AssertPoint(points,Project(u0,v0)+Vector3.up*z);
                AssertPoint(points,Project(u0+width,v0)+Vector3.up*z);
                AssertPoint(points,Project(u0,v0+depth)+Vector3.up*z);
            }
            Assert.That(wall-.075f,Is.GreaterThan(.14f),"The recessed fascia retains real stone before the unchanged throat.");
            // The open mouth's actual inner edge is still borne by exactly one
            // return and one annulus strip despite the replacement outer profile.
            Vector3 a=Project(u0+wall,v0+wall)+Vector3.up*top;
            Vector3 b=Project(u0+width-wall,v0+wall)+Vector3.up*top;
            Assert.That(EdgeIncidence(points,indices,a,b),Is.EqualTo(2));
        }

        [TestCase(1,3.7f,3.7f)]
        [TestCase(2,4.2f,3.8f)]
        public void CompleteHeadSharesOnePhysicalMineralFieldAcrossItsBearingsReturnsAndCrown(int variant,float width,float depth)
        {
            object head=Builder();Call("DistantFurnaceHead",head,3.1f,7.2f,3.1f+width,7.2f+depth,-2f,Color.white,variant);
            var points=Field<List<Vector3>>(head,"V");var uv=Field<List<Vector2>>(head,"U");var indices=Field<List<int>>(head,"T");
            Assert.That(points.Count,Is.EqualTo(596),"Head grain may not allocate more geometric sampling.");
            var used=new HashSet<int>(indices);Assert.That(used.Count,Is.EqualTo(points.Count));
            float scale=0;int common=0,spans=0;
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(uv[i].x,Is.InRange(.035f,.39801f));Assert.That(uv[i].y,Is.InRange(.438f,.56701f));
                float length=Vector3.Distance(points[0],points[i]);
                if(length>.25f)
                {
                    float actual=Vector2.Distance(uv[0],uv[i])/length;
                    if(scale==0)scale=actual;
                    Assert.That(actual,Is.EqualTo(scale).Within(.00001f),
                        "A narrow return cannot restart or stretch the full mineral patch.");spans++;
                }
                for(int j=0;j<i;j++)if(points[i]==points[j])
                {
                    Assert.That(uv[i],Is.EqualTo(uv[j]),"Coincident bearing and crown samples must share their grain.");common++;
                }
            }
            Assert.That(common,Is.GreaterThan(40));Assert.That(spans,Is.GreaterThan(100));Assert.That(scale,Is.GreaterThan(.005f));
            for(int i=0;i<indices.Count;i+=3)
            {
                int a=indices[i],b=indices[i+1],c=indices[i+2];
                double worldArea=Area(points[a],points[b],points[c]);
                if(Math.Abs(worldArea)<.0001)continue;
                Assert.That(Math.Abs(Area(uv[a],uv[b],uv[c])),Is.GreaterThan(1e-10),
                    "Actually visible head faces retain a two-dimensional material sample.");
            }
        }

        [TestCase(1,3.7f,3.7f)]
        [TestCase(2,4.2f,3.8f)]
        public void ExposedHeadMaterialRetainsSourceAtmosphereWithABroadBearingResponse(int variant,float width,float depth)
        {
            const float u0=3.1f,v0=7.2f,top=-2,bottom=-20;
            float u=u0+width*.5f,v=v0+depth*.5f;object head=Builder();
            Call("DistantFurnaceHead",head,u0,v0,u0+width,v0+depth,top,Color.white,variant);
            var points=Field<List<Vector3>>(head,"V");var uv=Field<List<Vector2>>(head,"U");
            var indices=Field<List<int>>(head,"T");var colors=Field<List<Color>>(head,"C");
            var originalPoints=points.ToArray();var originalUV=uv.ToArray();var originalIndices=indices.ToArray();var before=colors.ToArray();
            Call("FinishDistantStone",head,0,u,v,top,bottom,variant);
            CollectionAssert.AreEqual(originalPoints,points);CollectionAssert.AreEqual(originalUV,uv);CollectionAssert.AreEqual(originalIndices,indices);
            Vector3 origin=Project(u,v);float low=float.MaxValue,high=float.MinValue;
            foreach(int i in new HashSet<int>(indices))
            {
                Vector3 p=points[i];float h=p.y-origin.y;
                float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(bottom+3,top,h));
                Color ambient=Color.Lerp(new Color(.64f,.70f,.82f),new Color(1.08f,1.05f,1.02f),rise);
                float stain=.96f+.04f*Mathf.Sin((p.x-origin.x)*1.1f+variant*1.7f);
                float heat=Mathf.Clamp01(1-Mathf.Abs(h-(top-10))/7.5f);
                // Remove the unchanged atmospheric/weather/fire response from
                // real head vertices to isolate the new supported-stone finish.
                float r=colors[i].r/(before[i].r*ambient.r*stain*(1+.10f*heat));
                float g=colors[i].g/(before[i].g*ambient.g*stain*(1+.025f*heat));
                float b=colors[i].b/(before[i].b*ambient.b*stain*(1-.025f*heat));
                Assert.That(r,Is.InRange(.99999f,1.23501f));Assert.That(g,Is.EqualTo(r).Within(.00001f));
                Assert.That(b,Is.EqualTo(r).Within(.00001f));Assert.That(colors[i].a,Is.EqualTo(before[i].a));
                low=Mathf.Min(low,r);high=Mathf.Max(high,r);
            }
            Assert.That(high,Is.GreaterThan(1.13f));Assert.That(high-low,Is.GreaterThan(.045f),
                "The actual head must have a broad bearing/neck hierarchy, not one global exposure lift.");
        }

        [Test]
        public void LowerBrokenServiceArchRemainsBorneByBothCoresWithOpenMiddle()
        {
            object span=Builder();Call("DistantServiceArch",span,Color.white);
            AssertMesh(span,finalColors:false);
            var points=Field<List<Vector3>>(span,"V");
            var endpoints=new[]{Project(-5,39)+Vector3.down*7.4f,Project(12,42)+Vector3.down*6.8f};
            for(int i=0;i<endpoints.Length;i++)
            {
                Vector3 end=endpoints[i];AssertPoint(points,end);
                object stone=Builder(),fire=Builder();Call("DistantServiceTower",stone,fire,Color.white,i==0?2:1);
                Assert.That(CoversPoint(stone,end),Is.True,"Arch deck must enter its supporting shaft.");
                Assert.That(CoversPoint(stone,end+Vector3.down*5.2f),Is.True,
                    "Arch springing must be borne by the retained furnace core.");
            }
            Vector3 middle=Vector3.Lerp(endpoints[0],endpoints[1],.535f)+Vector3.down*.70f;
            Assert.That(CoversPoint(span,middle),Is.False,"The broken span must not become a continuous implied route.");
        }

        [Test]
        public void CombinedDistantAssemblyRemainsWithinApprovedStaticVertexBudget()
        {
            object stone=Builder(),fire=Builder();
            Call("DistantServiceArch",stone,Color.white);
            // Match BuildDistantWorks: the raw arch's lighting multipliers affect
            // alpha too; production fixes the span opaque before adding towers.
            var archColors=Field<List<Color>>(stone,"C");
            for(int i=0;i<archColors.Count;i++){Color c=archColors[i];c.a=1;archColors[i]=c;}
            var towers=new[]{new[]{35f,28,3.2f,3.5f,-5,-25},new[]{12f,42,3.7f,3.7f,-5.4f,-26},
                new[]{-5f,39,4.2f,3.8f,-6f,-25},new[]{27f,-20,3.8f,4.1f,-5,-28},new[]{-40f,0,4.1f,3.7f,-3,-25}};
            for(int variant=0;variant<towers.Length;variant++)
            {
                float[] t=towers[variant];
                if(variant==1||variant==2)Call("DistantServiceTower",stone,fire,Color.white,variant);
                else Call("DistantTower",stone,fire,t[0],t[1],t[2],t[3],t[4],t[5],Color.white,variant);
            }
            AssertMesh(stone);AssertMesh(fire);
            Assert.That(Field<List<Vector3>>(stone,"V").Count+Field<List<Vector3>>(fire,"V").Count,
                Is.LessThanOrEqualTo(3600),"A pair of architectural heads must not cause unbounded background growth.");
        }

        private static void AssertNeutralValue(Color actual,Color original,float gain)
        {
            Assert.That(actual.r/original.r,Is.EqualTo(gain).Within(.00001f));
            Assert.That(actual.g/original.g,Is.EqualTo(gain).Within(.00001f));
            Assert.That(actual.b/original.b,Is.EqualTo(gain).Within(.00001f));
            Assert.That(actual.a,Is.EqualTo(original.a),"Quarry value must not alter the existing atmospheric alpha.");
        }

        private static bool CoversPoint(object mesh,Vector2 point)
        {
            var vertices=Field<List<Vector3>>(mesh,"V");var indices=Field<List<int>>(mesh,"T");
            float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
            for(int i=0;i<indices.Count;i+=3)
            {
                Vector2 a=vertices[indices[i]],b=vertices[indices[i+1]],c=vertices[indices[i+2]];
                float area=Cross(b-a,c-a);if(Mathf.Abs(area)<.0000001f)continue;
                float x=Cross(b-point,c-point)/area,y=Cross(c-point,a-point)/area,z=1-x-y;
                if(x>=-Epsilon&&y>=-Epsilon&&z>=-Epsilon)return true;
            }
            return false;
        }

        private static void AssertMesh(object mesh,bool finalColors=true)
        {
            var points=Field<List<Vector3>>(mesh,"V");var uv=Field<List<Vector2>>(mesh,"U");
            var colors=Field<List<Color>>(mesh,"C");var indices=Field<List<int>>(mesh,"T");
            Assert.That(points.Count,Is.GreaterThan(0));Assert.That(indices.Count,Is.GreaterThan(0));
            Assert.That(uv.Count,Is.EqualTo(points.Count));Assert.That(colors.Count,Is.EqualTo(points.Count));
            Assert.That(indices.Count%3,Is.Zero);
            foreach(int index in indices)Assert.That(index,Is.InRange(0,points.Count-1));
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(Finite(points[i].x)&&Finite(points[i].y)&&Finite(points[i].z),Is.True);
                Assert.That(Finite(uv[i].x)&&Finite(uv[i].y),Is.True,"Masonry UVs can repeat but must remain finite.");
                Assert.That(Finite(colors[i].r)&&Finite(colors[i].g)&&Finite(colors[i].b)&&Finite(colors[i].a),Is.True);
                // Color*lighting may temporarily exceed unit alpha in private
                // helpers. Every completed tower replaces it in FadeDistantFoot;
                // only those final batches have the rendered [0,1] contract.
                if(finalColors)Assert.That(colors[i].a,Is.InRange(0f,1f));
            }
        }

        private static void AssertEnvelope(object mesh,float u0,float v0,float u1,float v1,float bottom,float top)
        {
            var corners=new[]{Project(u0,v0),Project(u1,v0),Project(u1,v1),Project(u0,v1)};
            foreach(Vector3 point in Field<List<Vector3>>(mesh,"V"))
                for(int sample=0;sample<72;sample++)
                {
                    float angle=sample*Mathf.PI/36;
                    var axis=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    float limit=float.NegativeInfinity;
                    foreach(Vector3 corner in corners)limit=Mathf.Max(limit,Vector3.Dot(axis,corner));
                    limit+=axis.y*(axis.y>=0?top:bottom);
                    Assert.That(Vector3.Dot(axis,point),Is.LessThanOrEqualTo(limit+Epsilon),
                        "Generated architecture escaped its established projected scenic prism.");
                }
        }

        private static void AssertConnected(object mesh)
        {
            var points=Field<List<Vector3>>(mesh,"V");var indices=Field<List<int>>(mesh,"T");
            var parent=new int[points.Count];for(int i=0;i<parent.Length;i++)parent[i]=i;
            int Root(int i){while(parent[i]!=i)i=parent[i];return i;}
            void Join(int a,int b){parent[Root(a)]=Root(b);}
            for(int i=0;i<points.Count;i++)
                for(int j=0;j<i;j++)if(Near(points[i],points[j]))Join(i,j);
            for(int i=0;i<indices.Count;i+=3){Join(indices[i],indices[i+1]);Join(indices[i],indices[i+2]);}
            for(int i=1;i<parent.Length;i++)Assert.That(Root(i),Is.EqualTo(Root(0)),
                "The head must be connected through shared surface boundaries, not detached trim or a floating crown.");
        }

        private static int EdgeIncidence(List<Vector3> points,List<int> indices,Vector3 a,Vector3 b)
        {
            int count=0;
            for(int i=0;i<indices.Count;i+=3)
            {
                bool hasA=false,hasB=false;
                for(int j=0;j<3;j++){hasA|=Near(points[indices[i+j]],a);hasB|=Near(points[indices[i+j]],b);}
                if(hasA&&hasB)count++;
            }
            return count;
        }

        private static bool HasTriangle(List<Vector3> points,List<int> indices,Vector3 a,Vector3 b,Vector3 c)
        {
            for(int i=0;i<indices.Count;i+=3)
            {
                bool hasA=false,hasB=false,hasC=false;
                for(int j=0;j<3;j++)
                {
                    Vector3 p=points[indices[i+j]];hasA|=Near(p,a);hasB|=Near(p,b);hasC|=Near(p,c);
                }
                if(hasA&&hasB&&hasC)return true;
            }
            return false;
        }

        private static void AssertPoint(List<Vector3> points,Vector3 expected)=>Assert.That(points.Exists(p=>Near(p,expected)),Is.True,
            "The original attachment/datum point must remain present.");
        private static bool Near(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<Epsilon*Epsilon;
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        private static double Area(Vector2 a,Vector2 b,Vector2 c)=>((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
        private static Vector3 Project(float u,float v)=>(Vector3)Call("Project",u,v);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object builder,string name)=>(T)builder.GetType().GetField(name,Fields).GetValue(builder);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
