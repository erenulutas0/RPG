using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryPitVesselTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(0)]
        [TestCase(1)]
        public void ActualHoodStaysInsideExistingProjectedCylinderOwnership(int index)
        {
            Vector2 centre=Centres()[index];
            object b=Builder();
            Call("FormedPitHood",b,centre);
            AssertMesh(b,centre,1.1f,0,1.3f);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ActualVesselAndHardwareStayBetweenDeckAndMoltenPlane(int index)
        {
            Vector2 centre=Centres()[index];
            object b=Builder();
            Call("PitDrum",b,centre);
            float molten=(float)typeof(FoundryRegionView).GetField("PitMolten",
                Static|BindingFlags.Public).GetRawConstantValue();
            AssertMesh(b,centre,1.1f,-molten,0);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void WorkedHoodPlatesPartitionOriginalProductionFacetsWithoutChangingTheirSilhouette(int index)
        {
            Vector2 centre=Centres()[index];
            var pitCentre=(Vector2)typeof(FoundryRegionView).GetField("PitCentre",Static).GetValue(null);
            bool pressure=centre.x<pitCentre.x;
            var profile=(Vector2[])Call("PitHoodProfile",pressure);
            int half=pressure?4:8;
            for(int row=0;row+1<profile.Length;row++)
            for(int facet=-half;facet<half;facet++)
            {
                float a=(float)Call("FrontAngle",facet,half),e=(float)Call("FrontAngle",facet+1,half);
                Vector2 low=profile[row],high=profile[row+1];
                var face=new[]{Ring(centre,low.x,a,low.y),Ring(centre,low.x,e,low.y),
                    Ring(centre,high.x,e,high.y),Ring(centre,high.x,a,high.y)};
                object built=Builder();
                Call("PitMetalPlate",built,centre,low.x,high.x,low.y,high.y,a,e,1f);
                var points=Field<List<Vector3>>(built,"V");var indices=Field<List<int>>(built,"T");
                double expected=Math.Abs(Cross(face[0],face[1],face[2]))+Math.Abs(Cross(face[0],face[2],face[3]));
                double actual=0;
                for(int t=0;t<indices.Count;t+=3)
                    actual+=Math.Abs(Cross(points[indices[t]],points[indices[t+1]],points[indices[t+2]]));
                Assert.That(actual,Is.EqualTo(expected).Within(.00002),"Seating and body replace the old face instead of covering it.");
                foreach(Vector3 corner in face)
                    Assert.That(points.Exists(p=>Vector3.Distance(p,corner)<.000001f),Is.True,"Every original silhouette corner remains.");
                foreach(Vector3 point in points)
                    Assert.That(InTriangle(point,face[0],face[1],face[2])||InTriangle(point,face[0],face[2],face[3]),Is.True);
                for(int x=0;x<5;x++)for(int y=0;y<7;y++)
                {
                    Vector3 point=Vector3.Lerp(Vector3.Lerp(face[0],face[1],(x+.371f)/5f),
                        Vector3.Lerp(face[3],face[2],(x+.371f)/5f),(y+.619f)/7f);
                    int owners=0;
                    for(int t=0;t<indices.Count;t+=3)
                        if(InTriangle(point,points[indices[t]],points[indices[t+1]],points[indices[t+2]],1e-9))owners++;
                    Assert.That(owners,Is.EqualTo(1),"A plate sample away from shared edges has exactly one surface owner.");
                }
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void WholeManufacturedHoodKeepsContinuousNoncollapsedGrainInsidePlainIron(int index)
        {
            object built=Builder();Call("FormedPitHood",built,Centres()[index]);
            var points=Field<List<Vector3>>(built,"V");var uv=Field<List<Vector2>>(built,"U");
            var indices=Field<List<int>>(built,"T");
            Assert.That(points.Count,Is.LessThan(1200),"The surface pass stays bounded rather than multiplying decorative geometry.");
            var shared=new Dictionary<Vector3,Vector2>();
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(uv[i].x,Is.InRange(.17f,.83f));Assert.That(uv[i].y,Is.InRange(.17f,.83f));
                if(shared.TryGetValue(points[i],out Vector2 other))
                    Assert.That(Vector2.Distance(other,uv[i]),Is.LessThan(.000001f),"Caps, collars and plate faces share their local grain.");
                else shared.Add(points[i],uv[i]);
            }
            for(int t=0;t<indices.Count;t+=3)
            {
                int a=indices[t],b=indices[t+1],c=indices[t+2];
                if(Math.Abs(Cross(points[a],points[b],points[c]))<1e-7)continue;
                Assert.That(Math.Abs(Cross(uv[a],uv[b],uv[c])),Is.GreaterThan(1e-12),"A visible metal face cannot collapse to one texture row.");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void AdjacentPressedFacetsShareTheirEdgeLightWithoutIndexDependentBands(int index)
        {
            Vector2 centre=Centres()[index];
            var pitCentre=(Vector2)typeof(FoundryRegionView).GetField("PitCentre",Static).GetValue(null);
            bool pressure=centre.x<pitCentre.x;int half=pressure?4:8;
            var profile=(Vector2[])Call("PitHoodProfile",pressure);
            for(int row=0;row+1<profile.Length;row++)
            for(int facet=-half;facet+1<half;facet++)
            {
                Vector2 low=profile[row],high=profile[row+1];
                object left=Builder(),right=Builder();
                float a=(float)Call("FrontAngle",facet,half),joint=(float)Call("FrontAngle",facet+1,half),
                    e=(float)Call("FrontAngle",facet+2,half);
                Call("PitMetalPlate",left,centre,low.x,high.x,low.y,high.y,a,joint,1f);
                Call("PitMetalPlate",right,centre,low.x,high.x,low.y,high.y,joint,e,1f);
                var lv=Field<List<Vector3>>(left,"V");var rv=Field<List<Vector3>>(right,"V");
                var lc=Field<List<Color>>(left,"C");var rc=Field<List<Color>>(right,"C");
                int matches=0;
                for(int x=0;x<lv.Count;x++)for(int y=0;y<rv.Count;y++)
                    if(Vector3.Distance(lv[x],rv[y])<.000001f)
                    {
                        matches++;
                        Assert.That(Mathf.Abs(lc[x].r-rc[y].r)+Mathf.Abs(lc[x].g-rc[y].g)+Mathf.Abs(lc[x].b-rc[y].b),
                            Is.LessThan(.00001f),"The same physical plate edge cannot change light when facet index changes.");
                    }
                Assert.That(matches,Is.GreaterThanOrEqualTo(2));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ExposedHeadSharesItsShoulderMaterialWhileTheActualMouthStaysDark(int index)
        {
            Vector2 centre=Centres()[index];
            var pitCentre=(Vector2)typeof(FoundryRegionView).GetField("PitCentre",Static).GetValue(null);
            bool pressure=centre.x<pitCentre.x;int half=pressure?4:8;
            var profile=(Vector2[])Call("PitHoodProfile",pressure);Vector2 cap=profile[profile.Length-1];
            object built=Builder();Call("FormedPitHood",built,centre);
            var points=Field<List<Vector3>>(built,"V");var colours=Field<List<Color>>(built,"C");
            for(int facet=-half;facet<half;facet++)
            {
                float angle=(float)Call("FrontAngle",facet,half);
                Vector3 perimeter=Ring(centre,cap.x,angle,cap.y);
                int matches=0;Color? first=null;
                for(int i=0;i<points.Count;i++)if(Vector3.Distance(points[i],perimeter)<.00001f)
                {
                    matches++;
                    if(first.HasValue)
                        Assert.That(Mathf.Abs(first.Value.r-colours[i].r)+Mathf.Abs(first.Value.g-colours[i].g)+
                            Mathf.Abs(first.Value.b-colours[i].b),Is.LessThan(.00002f),
                            "The exposed head and shoulder cannot have unrelated cast-iron colours at their shared rim.");
                    else first=colours[i];
                }
                Assert.That(matches,Is.GreaterThanOrEqualTo(2),"Inspect the actual adjoining surfaces, not a detached colour helper.");
            }
            Vector3 mouthCentre=Project(centre)+Vector3.up*1.14f;int darkCentre=0;
            for(int i=0;i<points.Count;i++)if(Vector3.Distance(points[i],mouthCentre)<.000001f)
            {
                darkCentre++;
                Assert.That(Mathf.Max(colours[i].r,Mathf.Max(colours[i].g,colours[i].b)),Is.LessThan(.08f),
                    "The actual recessed mouth retains its separate dark interior.");
            }
            Assert.That(darkCentre,Is.GreaterThan(0));
        }

        private static Vector3 Ring(Vector2 c,float r,float angle,float height)=>(Vector3)Call("OnRing",c,r,angle,height);
        private static double Cross(Vector3 a,Vector3 b,Vector3 c)=>((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
        private static bool InTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c,double epsilon=.00001)
        {
            double x=Cross(a,b,p),y=Cross(b,c,p),z=Cross(c,a,p);
            return x>=-epsilon&&y>=-epsilon&&z>=-epsilon||x<=epsilon&&y<=epsilon&&z<=epsilon;
        }

        // The test checks the actual emitted mesh against the projected convex
        // cylinder, not its vertex count or a copy of the decorative profile. This
        // catches seams, straps or collars escaping the existing depth envelope.
        private static void AssertMesh(object builder,Vector2 centre,float radius,float low,float high)
        {
            var v=Field<List<Vector3>>(builder,"V");
            var uv=Field<List<Vector2>>(builder,"U");
            var colors=Field<List<Color>>(builder,"C");
            var indices=Field<List<int>>(builder,"T");
            Assert.That(v.Count,Is.GreaterThan(0));
            Assert.That(uv.Count,Is.EqualTo(v.Count));
            Assert.That(colors.Count,Is.EqualTo(v.Count));
            Assert.That(indices.Count%3,Is.Zero);
            Vector3 origin=Project(centre),u=Project(centre+Vector2.right)-origin;
            Vector3 w=Project(centre+Vector2.up)-origin;
            foreach(int i in indices)Assert.That(i,Is.InRange(0,v.Count-1));
            for(int i=0;i<v.Count;i++)
            {
                Assert.That(float.IsNaN(v[i].x)||float.IsNaN(v[i].y)||float.IsInfinity(v[i].x)||
                    float.IsInfinity(v[i].y)||float.IsNaN(v[i].z)||float.IsInfinity(v[i].z),Is.False);
                Assert.That(uv[i].x,Is.InRange(0f,1f));
                Assert.That(uv[i].y,Is.InRange(0f,1f));
                Assert.That(colors[i].a,Is.EqualTo(1f).Within(.00001f));
                for(int angle=0;angle<72;angle++)
                {
                    float radians=angle*Mathf.PI/36;
                    var direction=new Vector3(Mathf.Cos(radians),Mathf.Sin(radians),0);
                    float du=Vector3.Dot(direction,u),dv=Vector3.Dot(direction,w);
                    float extent=radius*Mathf.Sqrt(du*du+dv*dv)+direction.y*(direction.y>=0?high:low);
                    Assert.That(Vector3.Dot(direction,v[i]-origin),Is.LessThanOrEqualTo(extent+.0001f),
                        "Generated vessel geometry escaped its existing projected depth ownership.");
                }
            }
        }

        private static Vector2[] Centres()=>(Vector2[])typeof(FoundryRegionView).GetField("PitBoilers",Static).GetValue(null);
        private static Vector3 Project(Vector2 p)=>(Vector3)Call("Project",p.x,p.y);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object b,string name)=>(T)b.GetType().GetField(name,Fields).GetValue(b);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
