using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryPitGuardTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [Test]
        public void DressedCopingPartitionsEveryOriginalFrontAndTopWithoutAnOverlay()
        {
            for(int run=0;run<8;run++)
            {
                var lines=Lines(run,out _);var along=new float[lines[1].Length];
                for(int j=1;j<along.Length;j++)along[j]=along[j-1]+Vector2.Distance(lines[1][j-1],lines[1][j]);
                for(int j=0;j+1<along.Length;j++)
                {
                    Vector2 f0=lines[0][j],f1=lines[0][j+1],r0=lines[2][j],r1=lines[2][j+1];
                    object dressed=Builder();
                    Call("PitGuardCoping",dressed,f0,f1,r0,r1,along[j],along[j+1],along[along.Length-1],run*19,true);
                    object original=Builder();
                    Vector3 a=Project(f0),b=Project(f1),c=Project(r1),d=Project(r0);
                    Call("FullFace",original,a+Vector3.up*.59f,b+Vector3.up*.59f,b+Vector3.up*.72f,a+Vector3.up*.72f,Color.white);
                    Call("FullFace",original,a+Vector3.up*.72f,b+Vector3.up*.72f,c+Vector3.up*.72f,d+Vector3.up*.72f,Color.white);
                    Assert.That(Area(dressed),Is.EqualTo(Area(original)).Within(.00004),
                        "Joint, body and arris faces must partition the actual original coping planes.");
                    var originalV=Field<List<Vector3>>(original,"V");
                    for(int face=0;face<2;face++)for(int x=0;x<7;x++)for(int y=0;y<5;y++)
                    {
                        int first=face*4;float u=(x+.371f)/7,v=(y+.613f)/5;
                        Vector3 point=Vector3.Lerp(Vector3.Lerp(originalV[first],originalV[first+1],u),
                            Vector3.Lerp(originalV[first+3],originalV[first+2],u),v);
                        Assert.That(Covers(dressed,point),Is.EqualTo(Covers(original,point)),
                            "Keep the original projected cover, including real front/top occlusion, without an extra backing face.");
                    }
                    var vertices=Field<List<Vector3>>(dressed,"V");var uv=Field<List<Vector2>>(dressed,"U");
                    var triangles=Field<List<int>>(dressed,"T");
                    foreach(Vector2 sample in uv)
                    {
                        Assert.That(sample.x,Is.InRange(.043f,.22f));
                        Assert.That(sample.y,Is.InRange(.4479f,.50f));
                    }
                    for(int t=0;t<triangles.Count;t+=3)
                    {
                        int p=triangles[t],q=triangles[t+1],r=triangles[t+2];
                        double area=Math.Abs(CrossDouble((Vector2)(vertices[q]-vertices[p]),(Vector2)(vertices[r]-vertices[p])));
                        if(area<1e-7)continue;
                        Assert.That(Math.Abs(CrossDouble(uv[q]-uv[p],uv[r]-uv[p])),Is.GreaterThan(1e-12),
                            $"Coping grain cannot collapse across curve chord {run}/{j}, triangle {t/3}.");
                    }
                }
            }
        }

        [Test]
        public void CopingGrainAndSelectedWearDoNotRestartAtHiddenBodyCuts()
        {
            // Sampling belongs to run distance and unfolded stone depth. The face
            // and cap keep their actual interpolated texture after an arbitrary
            // hidden body cut splits the same chord into two separate calls.
            var f0=new Vector2(0,0);var f1=new Vector2(0,.7f);
            var r0=new Vector2(.24f,0);var r1=new Vector2(.24f,.7f);
            object full=Builder(),split=Builder();
            Call("PitGuardCoping",full,f0,f1,r0,r1,0f,.7f,3.6f,1,false);
            var fm=new Vector2(0,.31f);var rm=new Vector2(.24f,.31f);
            Call("PitGuardCoping",split,f0,fm,r0,rm,0f,.31f,3.6f,1,false);
            Call("PitGuardCoping",split,fm,f1,rm,r1,.31f,.7f,3.6f,1,false);
            foreach(float along in new[]{.1275f,.5132f,.6527f})
            {
                foreach(Vector3 point in new[]{Project(new Vector2(0,along))+Vector3.up*.63f,
                    Project(new Vector2(0,along))+Vector3.up*.705f,
                    Project(new Vector2(.073f,along))+Vector3.up*.72f})
                    Assert.That(Vector2.Distance(GrainAt(full,point),GrainAt(split,point)),Is.LessThan(.000001f));
            }
            // Test emitted colour on a whole selected stone with no pre-existing
            // panel or curve cut at its shoulder. The mesh must actually sample
            // the wear peak; testing the helper alone would miss invisible wear.
            object wornStone=Builder();
            Call("PitGuardCoping",wornStone,f0,new Vector2(0,1.2f),r0,new Vector2(.24f,1.2f),
                0f,1.2f,2.4f,1,false);
            const float peak=.528f;
            Color shoulder=ToneAt(wornStone,Project(new Vector2(.01f,peak))+Vector3.up*.72f);
            Color inward=ToneAt(wornStone,Project(new Vector2(.075f,peak))+Vector3.up*.72f);
            Color end=ToneAt(wornStone,Project(new Vector2(.01f,.015f))+Vector3.up*.72f);
            Assert.That(shoulder.r/inward.r,Is.GreaterThan(1.055f),"A selected shoulder must exist in emitted vertex colours.");
            Assert.That(end.r/inward.r,Is.LessThan(1.01f),"Wear must fade before a stone end, not outline its entire length.");
            int worn=0;
            for(int cut=0;cut<40;cut++)
            {
                float maximum=0;
                for(int i=0;i<=30;i++)
                {
                    float wear=(float)Call("PitCopingWear",i/30f,cut);
                    Assert.That(wear,Is.InRange(0,.09f));maximum=Mathf.Max(maximum,wear);
                }
                if(maximum>0)worn++;
                Assert.That((float)Call("PitCopingWear",0f,cut),Is.LessThan(.002f));
                Assert.That((float)Call("PitCopingWear",1f,cut),Is.Zero);
            }
            Assert.That(worn,Is.EqualTo(10),"Only one quarter of stone cuts has selected shoulder wear.");
        }

        private static Color ToneAt(object mesh,Vector2 p)
        {
            var v=Field<List<Vector3>>(mesh,"V");var t=Field<List<int>>(mesh,"T");var colors=Field<List<Color>>(mesh,"C");
            Color value=Color.clear;bool found=false;
            for(int i=0;i<t.Count;i+=3)
            {
                Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
                double area=CrossDouble(b-a,c-a);if(Math.Abs(area)<1e-9)continue;
                double x=CrossDouble(b-p,c-p)/area,y=CrossDouble(c-p,a-p)/area,z=1-x-y;
                if(x<-.000001||y<-.000001||z<-.000001)continue;
                value=colors[t[i]]*(float)x+colors[t[i+1]]*(float)y+colors[t[i+2]]*(float)z;found=true;
            }
            Assert.That(found,Is.True,"The actual coping must contain each colour probe.");
            return value;
        }

        private static Vector2 GrainAt(object mesh,Vector2 p)
        {
            var v=Field<List<Vector3>>(mesh,"V");var t=Field<List<int>>(mesh,"T");var uv=Field<List<Vector2>>(mesh,"U");
            Vector2 value=Vector2.zero;bool found=false;
            for(int i=0;i<t.Count;i+=3)
            {
                Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
                double area=CrossDouble(b-a,c-a);if(Math.Abs(area)<1e-9)continue;
                double x=CrossDouble(b-p,c-p)/area,y=CrossDouble(c-p,a-p)/area,z=1-x-y;
                if(x<-.000001||y<-.000001||z<-.000001)continue;
                value=uv[t[i]]*(float)x+uv[t[i+1]]*(float)y+uv[t[i+2]]*(float)z;found=true;
            }
            Assert.That(found,Is.True,"The actual coping must contain each grain probe.");
            return value;
        }

        private static double CrossDouble(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        private static double Area(object mesh)
        {
            var v=Field<List<Vector3>>(mesh,"V");var t=Field<List<int>>(mesh,"T");double area=0;
            for(int i=0;i<t.Count;i+=3)
                area+=Math.Abs(CrossDouble((Vector2)(v[t[i+1]]-v[t[i]]),(Vector2)(v[t[i+2]]-v[t[i]])))*.5;
            return area;
        }
        private static int Covers(object mesh,Vector2 p)
        {
            var v=Field<List<Vector3>>(mesh,"V");var t=Field<List<int>>(mesh,"T");int count=0;
            for(int i=0;i<t.Count;i+=3)
            {
                Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
                double area=CrossDouble(b-a,c-a);if(Math.Abs(area)<1e-9)continue;
                double x=CrossDouble(b-p,c-p)/area,y=CrossDouble(c-p,a-p)/area,z=1-x-y;
                if(x>1e-7&&y>1e-7&&z>1e-7)count++;
            }
            return count;
        }

        [Test]
        public void AllGuardRunsKeepTheirOriginalCurvedBandAndHeightEnvelope()
        {
            for(int run=0;run<8;run++)
            {
                var lines=Lines(run,out bool near);object mesh=Builder();Call("PitGuardRun",mesh,lines,near);
                var points=Field<List<Vector3>>(mesh,"V");
                for(int direction=0;direction<144;direction++)
                {
                    float angle=direction*Mathf.PI/72;
                    var axis=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    float limit=float.NegativeInfinity;
                    foreach(int line in new[]{0,2})foreach(Vector2 p in lines[line])
                    {
                        Vector3 projected=Project(p);
                        limit=Mathf.Max(limit,Vector3.Dot(axis,projected),
                            Vector3.Dot(axis,projected+Vector3.up*.72f));
                    }
                    foreach(Vector3 p in points)Assert.That(Vector3.Dot(axis,p),Is.LessThanOrEqualTo(limit+.0001f),
                        "Guard masonry must remain inside the original rail's projected band/height prism.");
                }
            }
        }

        [Test]
        public void EveryBayRetainsBroadActualOpeningsAboveItsBearingSill()
        {
            for(int run=0;run<8;run++)
            {
                var lines=Lines(run,out bool near);object mesh=Builder();Call("PitGuardRun",mesh,lines,near);
                // The same run as a bare frame - its bearing sill and coping only, no responds, panel or bars.
                object frame=Builder();var axis=lines[1];var along=new float[axis.Length];
                for(int j=1;j<axis.Length;j++)along[j]=along[j-1]+Vector2.Distance(axis[j-1],axis[j]);
                for(int j=0;j+1<axis.Length;j++)
                {
                    Call("PitGuardBlock",frame,lines[0][j],lines[0][j+1],lines[2][j],lines[2][j+1],near?.15f:0f,.26f,.96f,false,false);
                    Call("PitGuardCoping",frame,lines[0][j],lines[0][j+1],lines[2][j],lines[2][j+1],along[j],along[j+1],
                        along[along.Length-1],1,false);
                }
                int clear=0,open=0,samples=0;
                for(int j=0;j+1<axis.Length;j++)for(int sample=0;sample<9;sample++)
                {
                    Vector2 p=Vector2.Lerp(axis[j],axis[j+1],(sample+.371f)/9);
                    // A point on the actual projected clear band. Coping, returns
                    // and recessed panels all participate in the triangle query.
                    Vector3 screen=Project(p)+Vector3.up*.433f;
                    if(!Covered(mesh,screen))clear++;
                    if(!Covered(frame,screen))open++;
                    samples++;
                }
                // Where the round collar turns toward the camera (beside the side corners) the run is seen nearly
                // edge-on and its own coping hides part of every bay; the infill must still leave more than half of
                // what the bare frame shows, and a run seen square-on keeps more than 40% of its band open.
                Assert.That(clear,Is.GreaterThan(open*.5f),$"Run {run}: the infill must not close the bays the frame leaves open.");
                if(open>=samples*.95f)Assert.That(clear/(float)samples,Is.GreaterThan(.40f),
                    "More than 40% of each real bay must stay visibly open to the shaft, not an opaque parapet.");
            }
        }

        [Test]
        public void GuardsMeetOriginalPostsAndHaveFiniteBoundedMeshData()
        {
            int total=0;
            for(int run=0;run<8;run++)
            {
                var lines=Lines(run,out bool near);object mesh=Builder();Call("PitGuardRun",mesh,lines,near);
                var points=Field<List<Vector3>>(mesh,"V");var uv=Field<List<Vector2>>(mesh,"U");
                var colors=Field<List<Color>>(mesh,"C");var indices=Field<List<int>>(mesh,"T");total+=points.Count;
                Assert.That(points.Count,Is.GreaterThan(0));Assert.That(uv.Count,Is.EqualTo(points.Count));
                Assert.That(colors.Count,Is.EqualTo(points.Count));Assert.That(indices.Count%3,Is.Zero);
                foreach(int index in indices)Assert.That(index,Is.InRange(0,points.Count-1));
                for(int i=0;i<points.Count;i++)
                {
                    Assert.That(Finite(points[i].x)&&Finite(points[i].y)&&Finite(points[i].z),Is.True);
                    Assert.That(Finite(uv[i].x)&&Finite(uv[i].y),Is.True);
                    Assert.That(Finite(colors[i].r)&&Finite(colors[i].g)&&Finite(colors[i].b),Is.True);
                    Assert.That(colors[i].a,Is.EqualTo(1).Within(.00001f));
                }
                foreach(int end in new[]{0,lines[0].Length-1})
                {
                    Vector3 p=Project(lines[0][end]);
                    foreach(float h in new[]{near?.15f:0,.26f,.59f,.72f})
                        Assert.That(points.Exists(v=>(v-(p+Vector3.up*h)).sqrMagnitude<.00000001f),Is.True,
                            "Sill, end respond and coping must join the unchanged original post boundary.");
                }
            }
            Assert.That(total,Is.LessThanOrEqualTo(6000),"All eight curved guard bays have a fixed combined mesh budget.");
        }

        private static bool Covered(object mesh,Vector3 p)
        {
            var v=Field<List<Vector3>>(mesh,"V");var t=Field<List<int>>(mesh,"T");
            for(int i=0;i<t.Count;i+=3)
            {
                Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
                float area=Cross(b-a,c-a);if(Mathf.Abs(area)<.0000001f)continue;
                float s=Cross(b-(Vector2)p,c-(Vector2)p)/area;
                float u=Cross(c-(Vector2)p,a-(Vector2)p)/area;
                if(s>=.00001f&&u>=.00001f&&1-s-u>=.00001f)return true;
            }
            return false;
        }
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        private static Vector2[][] Lines(int run,out bool near)
        {
            var posts=(Vector4[])Call("PitPosts");object[] args={posts,run,false};
            var result=(Vector2[][])typeof(FoundryRegionView).GetMethod("PitRunLines",Static).Invoke(null,args);
            near=(bool)args[2];return result;
        }
        private static Vector3 Project(Vector2 p)=>(Vector3)Call("Project",p.x,p.y);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object b,string field)=>(T)b.GetType().GetField(field,Fields).GetValue(b);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
