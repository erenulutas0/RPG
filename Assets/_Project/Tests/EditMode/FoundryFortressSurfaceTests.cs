using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryFortressSurfaceTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(false)]
        [TestCase(true)]
        public void FortressCoursesRetainSlopingOrTaperedFaceAndContinuousIsotropicGrain(bool pier)
        {
            var face=pier?new[]{new Vector3(-.18f,-7.9f),new Vector3(.18f,-7.9f),
                new Vector3(.33f,-2.5f),new Vector3(-.33f,-2.5f)}:
                new[]{new Vector3(-3,-7.25f),new Vector3(4,-5.35f),
                    new Vector3(4.3f,-4.05f),new Vector3(-3.3f,-5.95f)};
            object mesh=Builder();
            Call("FortressCoursedFace",mesh,face[0],face[1],face[2],face[3],new Color(.43f,.46f,.52f),pier?5:2,pier?1:4);
            var points=Field<List<Vector3>>(mesh,"V");var uv=Field<List<Vector2>>(mesh,"U");
            var colors=Field<List<Color>>(mesh,"C");var triangles=Field<List<int>>(mesh,"T");
            Assert.That(points.Count,Is.EqualTo(uv.Count));Assert.That(points.Count,Is.EqualTo(colors.Count));
            // An exact underlay still covers the whole original quad, including
            // the restrained joints between its broad fitted stones.
            for(int i=0;i<4;i++)Assert.That(points[i],Is.EqualTo(face[i]));
            for(int i=0;i<points.Count;i++)
            {
                Vector3 p=points[i];
                Assert.That(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y),Is.False);
                for(int edge=0;edge<4;edge++)Assert.That(Cross(face[edge],face[(edge+1)%4],p),Is.GreaterThanOrEqualTo(-.00001));
                Assert.That(uv[i].x,Is.InRange(.67999f,.97001f));Assert.That(uv[i].y,Is.InRange(.43999f,.56401f));
                Assert.That(colors[i].a,Is.EqualTo(1));
            }
            for(int i=0;i<triangles.Count;i+=3)
            {
                for(int j=0;j<3;j++)Assert.That(triangles[i+j],Is.InRange(0,points.Count-1));
                Assert.That(Cross(points[triangles[i]],points[triangles[i+1]],points[triangles[i+2]]),Is.GreaterThan(.00000001));
            }
            // Each dressed block owns twenty vertices (center and four rims).
            // Equal positions have equal UVs, and both texture directions retain
            // one physical scale even on tapered/sloping stones and thin bevels.
            for(int first=4;first<points.Count;first+=20)
            {
                float scale=Vector2.Distance(uv[first],uv[first+1])/Vector3.Distance(points[first],points[first+1]);
                Assert.That(scale,Is.GreaterThan(0));
                for(int a=first;a<first+20;a++)for(int b=a+1;b<first+20;b++)
                {
                    float distance=Vector3.Distance(points[a],points[b]);
                    Assert.That(Vector2.Distance(uv[a],uv[b]),Is.EqualTo(distance*scale).Within(.000002f));
                }
            }
        }

        [TestCase(-1f)]
        [TestCase(1f)]
        public void BridgeCapGrainCrossesOwnerSeamsWithoutRestarting(float side)
        {
            float start=FoundryMap.BridgeStart+6.75f,middle=start+.75f,end=middle+.75f;
            object left=Builder(),right=Builder();
            var first=(List<Vector2>)Call("BridgeParapetOutline",start,middle,side);
            var second=(List<Vector2>)Call("BridgeParapetOutline",middle,end,side);
            Call("BridgeParapetCap",left,first,.78f,Color.white);
            Call("BridgeParapetCap",right,second,.78f,Color.white);
            var a=Field<List<Vector3>>(left,"V");var b=Field<List<Vector3>>(right,"V");
            var ua=Field<List<Vector2>>(left,"U");var ub=Field<List<Vector2>>(right,"U");
            int matches=0;
            for(int i=0;i<a.Count;i++)for(int j=0;j<b.Count;j++)
                if(Vector3.Distance(a[i],b[j])<.000001f)
                {
                    matches++;
                    Assert.That(Vector2.Distance(ua[i],ub[j]),Is.LessThan(.000001f),
                        "The small depth-owner boundary is not a stone joint or UV restart.");
                }
            Assert.That(matches,Is.GreaterThanOrEqualTo(4),"Both sides must share the cap and bevel boundary.");
        }

        private static double Cross(Vector3 a,Vector3 b,Vector3 c)=>
            ((double)b.x-a.x)*(c.y-a.y)-((double)b.y-a.y)*(c.x-a.x);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object mesh,string name)=>(T)mesh.GetType().GetField(name,Fields).GetValue(mesh);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
