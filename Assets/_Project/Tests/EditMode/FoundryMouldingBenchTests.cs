using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryMouldingBenchTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(false)] [TestCase(true)]
        public void CastingOpeningHasPartitionedShouldersAndClosedLowerWalls(bool bearing)
        {
            Mould(bearing,out Vector4 box,out float height);
            object top=Build(bearing?"YardBearingSandTop":"YardIngotSandTop",box.x,box.y,box.z,box.w,height);
            var p=Data<Vector3>(top,"V");var t=Data<int>(top,"T");
            var opening=Opening(box,bearing);Vector2 centre=new Vector2((box.x+box.z)*.5f,(box.y+box.w)*.5f);
            var core=new List<Vector2>();if(bearing)foreach(Vector2 point in opening)core.Add(centre+(point-centre)*.48f);
            double expected=(box.z-box.x)*(box.w-box.y)-Area(opening)+(bearing?Area(core):0);
            double actual=0;
            for(int i=0;i<t.Count;i+=3)
            {
                Vector2 a=Design(p[t[i]],height),b=Design(p[t[i+1]],height),c=Design(p[t[i+2]],height);
                double area=Math.Abs(Cross(a,b,c))*.5;
                Assert.That(area,Is.GreaterThan(1e-9));actual+=area;
                foreach(Vector2 point in new[]{a,b,c})
                {
                    Assert.That(point.x,Is.InRange(box.x-.00002f,box.z+.00002f));
                    Assert.That(point.y,Is.InRange(box.y-.00002f,box.w+.00002f));
                }
            }
            Assert.That(actual,Is.EqualTo(expected).Within(.00002),
                "The real top contains the rectangular shoulders and optional core exactly once, without a hidden lid.");
            for(int u=0;u<23;u++)for(int v=0;v<19;v++)
            {
                Vector2 sample=new Vector2(Mathf.Lerp(box.x,box.z,(u+.37f)/23),Mathf.Lerp(box.y,box.w,(v+.41f)/19));
                bool solid=!Inside(opening,sample)||(bearing&&Inside(core,sample));
                Assert.That(Covered(top,Project(sample.x,sample.y)+Vector3.up*height),Is.EqualTo(solid),
                    "Inspect only the actual top plane; projected cavity back walls are not a closing lid.");
            }
            AssertClosedCavity(bearing);
        }

        private static void AssertClosedCavity(bool bearing)
        {
            Mould(bearing,out Vector4 box,out float height);
            object top=Build(bearing?"YardBearingSandTop":"YardIngotSandTop",box.x,box.y,box.z,box.w,height);
            object cavity=Build(bearing?"YardBearingSandCavity":"YardIngotSandCavity",box.x,box.y,box.z,box.w,height,1.215f);
            var p=Data<Vector3>(cavity,"V");var t=Data<int>(cavity,"T");var rim=Data<Vector3>(top,"V");
            var canonical=new List<Vector3>();var edges=new Dictionary<(int,int),int>();
            int Index(Vector3 point)
            {
                int found=Find(canonical,point);if(found>=0)return found;
                canonical.Add(point);return canonical.Count-1;
            }
            for(int i=0;i<t.Count;i+=3)for(int edge=0;edge<3;edge++)
            {
                int a=Index(p[t[i+edge]]),b=Index(p[t[i+(edge+1)%3]]);
                Assert.That(a,Is.Not.EqualTo(b));
                var key=a<b?(a,b):(b,a);
                edges.TryGetValue(key,out int count);edges[key]=count+1;
            }
            int boundary=0;
            foreach(var pair in edges)
            {
                Assert.That(pair.Value,Is.InRange(1,2),"A cavity must not duplicate a floor or wall.");
                if(pair.Value==2)continue;
                boundary++;
                Assert.That(Find(rim,canonical[pair.Key.Item1]),Is.GreaterThanOrEqualTo(0));
                Assert.That(Find(rim,canonical[pair.Key.Item2]),Is.GreaterThanOrEqualTo(0),
                    "Every free wall edge must meet the separately emitted top mouth; lower floors cannot leave a gap.");
            }
            Assert.That(boundary,Is.EqualTo(bearing?32:4),
                "Only the ingot mouth or the two annular mouth contours may remain open.");
        }

        [Test]
        public void CompleteStationEmitsRecessesAndSupportedLegsAndClampsWithinItsBudget()
        {
            float u0=FoundryMap.WorkIslandBound(1,0),v0=FoundryMap.WorkIslandBound(1,1);
            float u1=FoundryMap.WorkIslandBound(1,2),v1=FoundryMap.WorkIslandBound(1,3);
            object complete=Build("BuildYardMouldBench",u0,v0,u1,v1);
            var actual=Data<Vector3>(complete,"V");var referenced=new HashSet<int>(Data<int>(complete,"T"));
            var used=new List<Vector3>();foreach(int i in referenced)used.Add(actual[i]);
            Assert.That(actual.Count,Is.LessThanOrEqualTo(1784),"The replacement cannot exceed its accepted predecessor's station cost.");
            foreach(bool bearing in new[]{false,true})
            {
                Mould(bearing,out Vector4 box,out float height);
                foreach(object part in new[]{Build(bearing?"YardBearingSandTop":"YardIngotSandTop",box.x,box.y,box.z,box.w,height),
                    Build(bearing?"YardBearingSandCavity":"YardIngotSandCavity",box.x,box.y,box.z,box.w,height,1.215f)})
                    foreach(int i in new HashSet<int>(Data<int>(part,"T")))
                        Assert.That(Find(used,Data<Vector3>(part,"V")[i]),Is.GreaterThanOrEqualTo(0),
                            "The production station must actually emit each verified sand shoulder, wall and floor.");
            }
            object frame=Build("YardMouldingBenchFrame",u0,v0,u1,v1);
            foreach(Vector3 point in Data<Vector3>(frame,"V"))Assert.That(Find(used,point),Is.GreaterThanOrEqualTo(0));
            // Check the actual screw foot and actual flat oak cap, not projected
            // overlap with a bevel or a taller face behind the contact.
            foreach(float x in new[]{u0+.34f,u0+.42f})foreach(float y in new[]{v0+.45f,v1-.41f})
            {
                Vector3 point=Project(x,y)+Vector3.up*1.40f;
                Assert.That(Find(used,point),Is.GreaterThanOrEqualTo(0));
            }
            foreach(float v in new[]{v0+.55f,v1-.58f})
            {
                object clamp=Build("YardMouldClamp",u0+.24f,v);
                var clampPoints=Data<Vector3>(clamp,"V");
                foreach(Vector2 foot in new[]{new Vector2(u0+.35f,v+.035f),new Vector2(u0+.41f,v+.035f),new Vector2(u0+.35f,v+.105f)})
                {
                    Vector3 point=Project(foot.x,foot.y)+Vector3.up*1.40f;
                    Assert.That(Find(clampPoints,point),Is.GreaterThanOrEqualTo(0));
                    Assert.That(Find(used,point),Is.GreaterThanOrEqualTo(0));
                    Assert.That(foot.x,Is.InRange(u0+.34f,u0+.42f));
                    Assert.That(foot.y,Is.InRange(v0+.45f,v1-.41f),"The screw foot lies wholly inside the real flat rail cap.");
                }
            }
            foreach(float u in new[]{u0+.16f,u1-.36f})foreach(bool far in new[]{false,true})
            {
                float footV=far?v1-.38f:v0+.18f,headV=far?v1-.58f:v0+.34f;
                foreach(float x in new[]{u,u+.20f})foreach(float y in new[]{footV,footV+.20f})
                    Assert.That(Find(used,Project(x,y)+Vector3.up*.10f),Is.GreaterThanOrEqualTo(0),
                        "Every raked leg retains all four actual skid contacts.");
                float boardStart=far?v0+.10f+2*(v1-v0-.20f)/3:v0+.10f;
                float boardEnd=boardStart+(v1-v0-.20f)/3-.016f;
                foreach(float x in new[]{u-.025f,u+.225f})foreach(float y in new[]{headV,headV+.24f})
                {
                    Vector3 head=Project(x,y)+Vector3.up*.99f;
                    Assert.That(Find(used,head),Is.GreaterThanOrEqualTo(0));
                    Assert.That(Covered(frame,head),Is.True);
                    Assert.That(x,Is.InRange(u0+.10f,u1-.10f));
                    Assert.That(y,Is.InRange(boardStart,boardEnd),"The entire head bears below a real plank, clear of the plank gaps.");
                }
            }
        }

        private static void Mould(bool bearing,out Vector4 box,out float height)
        {
            float u0=FoundryMap.WorkIslandBound(1,0),v0=FoundryMap.WorkIslandBound(1,1);
            float u1=FoundryMap.WorkIslandBound(1,2),v1=FoundryMap.WorkIslandBound(1,3);
            box=bearing?new Vector4(u0+1.82f,v0+.52f,u1-.42f,v1-.59f):
                new Vector4(u0+.44f,v0+.43f,u0+1.30f,v1-.39f);
            height=bearing?1.28f:1.36f;
        }

        private static List<Vector2> Opening(Vector4 box,bool bearing)
        {
            Vector2 centre=new Vector2((box.x+box.z)*.5f,(box.y+box.w)*.5f);
            if(!bearing)return new List<Vector2>{new Vector2(centre.x-.22f,box.y+.17f),new Vector2(centre.x+.22f,box.y+.17f),
                new Vector2(centre.x+.17f,box.w-.17f),new Vector2(centre.x-.17f,box.w-.17f)};
            var result=new List<Vector2>();float radius=Mathf.Min(box.z-box.x,box.w-box.y)*.41f;
            // Independent polar boundary: corner rays and three equally spaced
            // rectangle-side stations on each side define the polygonal mouth.
            var corners=new[]{new Vector2(box.x,box.y),new Vector2(box.z,box.y),new Vector2(box.z,box.w),new Vector2(box.x,box.w)};
            for(int side=0;side<4;side++)for(int station=0;station<4;station++)
                result.Add(centre+(Vector2.Lerp(corners[side],corners[(side+1)%4],station*.25f)-centre).normalized*radius);
            return result;
        }

        private static double Area(List<Vector2> polygon)
        {
            double sum=0;for(int i=0;i<polygon.Count;i++)sum+=(double)polygon[i].x*polygon[(i+1)%polygon.Count].y-(double)polygon[i].y*polygon[(i+1)%polygon.Count].x;
            return Math.Abs(sum)*.5;
        }
        private static bool Inside(List<Vector2> polygon,Vector2 point)
        {
            for(int i=0;i<polygon.Count;i++)if(Cross(polygon[i],polygon[(i+1)%polygon.Count],point)<0)return false;
            return true;
        }
        private static bool Covered(object mesh,Vector2 point)
        {
            var p=Data<Vector3>(mesh,"V");var t=Data<int>(mesh,"T");
            for(int i=0;i<t.Count;i+=3)
            {
                double area=Cross(p[t[i]],p[t[i+1]],p[t[i+2]]);if(Math.Abs(area)<1e-10)continue;
                double a=Cross(point,p[t[i+1]],p[t[i+2]])/area,b=Cross(p[t[i]],point,p[t[i+2]])/area,c=1-a-b;
                if(a>=-1e-6&&b>=-1e-6&&c>=-1e-6)return true;
            }
            return false;
        }
        private static object Build(string method,params object[] values)
        {
            object mesh=Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
            var args=new object[values.Length+1];args[0]=mesh;Array.Copy(values,0,args,1,values.Length);
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,args);return mesh;
        }
        private static Vector3 Project(float u,float v)
        {
            FoundryMap.ToFloor(u,v,out float x,out float y);return new Vector3(x,y*.5f);
        }
        private static Vector2 Design(Vector3 p,float height)=>new Vector2((p.x+2*(p.y-height))/(2*FoundryMap.Axis),
            (2*(p.y-height)-p.x)/(2*FoundryMap.Axis));
        private static int Find(List<Vector3> points,Vector3 p)=>points.FindIndex(q=>Vector3.Distance(q,p)<.00002f);
        private static List<T> Data<T>(object mesh,string field)=>(List<T>)mesh.GetType().GetField(field,Fields).GetValue(mesh);
        private static double Cross(Vector2 a,Vector2 b,Vector2 c)=>((double)b.x-a.x)*(c.y-a.y)-((double)b.y-a.y)*(c.x-a.x);
    }
}
