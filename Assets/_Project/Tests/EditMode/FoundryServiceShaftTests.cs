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

        [TestCase(1,12f,42f,3.7f,3.7f,-2f,-26f)]
        [TestCase(2,-5f,39f,4.2f,3.8f,-4f,-25f)]
        public void CompleteServiceShaftRetainsScenicEnvelopeAndFoundation(int variant,float u,float v,
            float width,float depth,float top,float bottom)
        {
            object stone=Builder(),fire=Builder();
            Call("DistantTower",stone,fire,u,v,width,depth,top,bottom,Color.white,variant);
            AssertMesh(stone);AssertMesh(fire);
            float u0=u-width*.5f,u1=u+width*.5f,v0=v-depth*.5f,v1=v+depth*.5f;
            // Include the unchanged projecting arch imposts and buttresses, not
            // just the narrower shaft. The cap remains at each original datum.
            AssertEnvelope(stone,u0-.24f,v0-.24f,u1+.16f,v1+.06f,bottom,top-(variant==2?.4f:0));
            AssertEnvelope(fire,u0-.24f,v0-.24f,u1+.16f,v1+.06f,bottom,top);
            var points=Field<List<Vector3>>(stone,"V");
            AssertPoint(points,Project(u0+.2f,v0+.2f)+Vector3.up*bottom);
            AssertPoint(points,Project(u1-.2f,v0+.2f)+Vector3.up*bottom);
            AssertPoint(points,Project(u0+.2f,v1-.2f)+Vector3.up*bottom);
        }

        [TestCase(1,3.7f,3.7f)]
        [TestCase(2,4.2f,3.8f)]
        public void HeadIsOneConnectedAssemblySeatedOnOriginalShoulder(int variant,float width,float depth)
        {
            const float u0=3.1f,v0=7.2f,top=-2f;
            float u1=u0+width,v1=v0+depth,shoulder=top-(variant==1?4.8f:6.2f);
            object head=Builder();Call("DistantFurnaceHead",head,u0,v0,u1,v1,top,Color.white,variant);
            AssertMesh(head,finalColors:false);
            AssertEnvelope(head,u0-.06f,v0-.06f,u1+.06f,v1+.06f,shoulder,top-(variant==2?.4f:0));
            var points=Field<List<Vector3>>(head,"V");
            AssertPoint(points,Project(u0-.06f,v0-.06f)+Vector3.up*shoulder);
            AssertPoint(points,Project(u1+.06f,v0-.06f)+Vector3.up*shoulder);
            AssertPoint(points,Project(u0-.06f,v1+.06f)+Vector3.up*shoulder);
            AssertConnected(head);
        }

        [TestCase(1,12f,42f,3.7f,3.7f,-2f,-26f)]
        [TestCase(2,-5f,39f,4.2f,3.8f,-4f,-25f)]
        public void HeadReplacementLeavesOriginalSixFurnacePanesAndGapsUnchanged(int variant,float u,float v,
            float width,float depth,float top,float bottom)
        {
            object stone=Builder(),fire=Builder();
            Call("DistantTower",stone,fire,u,v,width,depth,top,bottom,Color.white,variant);
            var points=Field<List<Vector3>>(fire,"V");
            var uv=Field<List<Vector2>>(fire,"U");
            float u0=u-width*.5f,u1=u+width*.5f,v0=v-depth*.5f,v1=v+depth*.5f;
            float sill=top-16.2f,head=top-(variant==1?4.8f:6.2f);
            // These lower-bay values are a retained visual contract, independent
            // of the new neck/crown helpers. In particular, no new fire face may
            // fill a gap or rise into the revised head.
            var expected=new List<Vector3>();
            foreach(bool west in new[]{true,false})
            {
                float span=west?depth:width,spring=head-.38f-.21f*span;
                float fireBottom=sill+1.05f,step=(spring-.55f-fireBottom)/3f;
                float paneHeight=Mathf.Min(1.65f,step*.57f);
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
                Assert.That(Vector3.Distance(points[i],expected[i]),Is.LessThan(Epsilon),"Retained fire pane changed.");
                Assert.That(uv[i].x,Is.InRange(.7815f-Epsilon,.7825f+Epsilon));
                Assert.That(uv[i].y,Is.InRange(.8375f-Epsilon,.8385f+Epsilon));
            }
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

        [Test]
        public void CombinedDistantAssemblyRemainsWithinApprovedStaticVertexBudget()
        {
            object stone=Builder(),fire=Builder();
            Call("DistantArchSpan",stone,Project(-5,39)+Vector3.down*5.4f,
                Project(12,42)+Vector3.down*3.4f,Color.white);
            // Match BuildDistantWorks: the raw arch's lighting multipliers affect
            // alpha too; production fixes the span opaque before adding towers.
            var archColors=Field<List<Color>>(stone,"C");
            for(int i=0;i<archColors.Count;i++){Color c=archColors[i];c.a=1;archColors[i]=c;}
            var towers=new[]{new[]{35f,28,3.2f,3.5f,-5,-25},new[]{12f,42,3.7f,3.7f,-2,-26},
                new[]{-5f,39,4.2f,3.8f,-4,-25},new[]{27f,-20,3.8f,4.1f,-5,-28},new[]{-40f,0,4.1f,3.7f,-3,-25}};
            for(int variant=0;variant<towers.Length;variant++)
            {
                float[] t=towers[variant];
                Call("DistantTower",stone,fire,t[0],t[1],t[2],t[3],t[4],t[5],Color.white,variant);
            }
            AssertMesh(stone);AssertMesh(fire);
            Assert.That(Field<List<Vector3>>(stone,"V").Count+Field<List<Vector3>>(fire,"V").Count,
                Is.LessThanOrEqualTo(3600),"A pair of architectural heads must not cause unbounded background growth.");
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
        private static Vector3 Project(float u,float v)=>(Vector3)Call("Project",u,v);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object builder,string name)=>(T)builder.GetType().GetField(name,Fields).GetValue(builder);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
