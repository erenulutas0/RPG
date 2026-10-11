using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryUndercroftStoneTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;

        [Test]
        public void JoinedCurtainKeepsEveryGapCornerAndExteriorSegment()
        {
            // Deliberately scrambled: the join must work across either end but
            // must not jump the missing bridge span or straighten a chamfer.
            var spans=new List<(Vector2 a,Vector2 b)>
            {
                (new Vector2(1,0),new Vector2(2,0)),
                (new Vector2(4,0),new Vector2(5,0)),
                (new Vector2(0,0),new Vector2(1,0)),
                (new Vector2(5,0),new Vector2(6,1)),
                (new Vector2(6,1),new Vector2(6,2)),
                (new Vector2(6,2),new Vector2(6,3))
            };
            var joined=(List<(Vector2 a,Vector2 b)>)typeof(FoundryRegionView)
                .GetMethod("JoinedFoundationSpans",Static).Invoke(null,new object[]{spans});
            Assert.That(joined.Count,Is.EqualTo(4));
            Assert.That(joined,Does.Contain((new Vector2(0,0),new Vector2(2,0))));
            Assert.That(joined,Does.Contain((new Vector2(4,0),new Vector2(5,0))));
            Assert.That(joined,Does.Contain((new Vector2(5,0),new Vector2(6,1))));
            Assert.That(joined,Does.Contain((new Vector2(6,1),new Vector2(6,3))));
            float sourceLength=0,resultLength=0;
            foreach(var span in spans)sourceLength+=Vector2.Distance(span.a,span.b);
            foreach(var span in joined)resultLength+=Vector2.Distance(span.a,span.b);
            Assert.That(resultLength,Is.EqualTo(sourceLength).Within(.00001f));
        }

        [Test]
        public void JoinedGateCurtainKeepsOriginalRampAndContractedFootBreaks()
        {
            // The actual exposed south edge of the gate landing crosses both
            // ramp grades. Its contracted foot reaches those grades at different
            // u coordinates, so even two straight deck segments may not be joined.
            var spans=new List<(Vector2 a,Vector2 b)>();
            for(int u=FoundryMap.GateLandingStart;u<FoundryMap.GateLandingEnd;u++)
                spans.Add((new Vector2(u,-2),new Vector2(u+1,-2)));
            var joined=(List<(Vector2 a,Vector2 b)>)typeof(FoundryRegionView)
                .GetMethod("JoinedFoundationSpans",Static).Invoke(null,new object[]{spans});
            MethodInfo project=typeof(FoundryRegionView).GetMethod("Project",Static);
            foreach(var original in spans)
            {
                bool covered=false;
                foreach(var merged in joined)
                {
                    if(original.a.x<merged.a.x||original.b.x>merged.b.x)continue;
                    covered=true;
                    foreach(float contraction in new[]{0f,.035f})
                    {
                        Vector2 centre=new Vector2(-8,2);
                        Vector2 a=Vector2.Lerp(merged.a,centre,contraction),b=Vector2.Lerp(merged.b,centre,contraction);
                        Vector3 pa=(Vector3)project.Invoke(null,new object[]{a.x,a.y}),pb=(Vector3)project.Invoke(null,new object[]{b.x,b.y});
                        foreach(Vector2 source in new[]{original.a,original.b})
                        {
                            Vector2 p=Vector2.Lerp(source,centre,contraction);
                            Vector3 pp=(Vector3)project.Invoke(null,new object[]{p.x,p.y});
                            double perpendicular=Math.Abs(Cross(pa,pb,pp))/Vector3.Distance(pa,pb);
                            Assert.That(perpendicular,Is.LessThan(.00002),
                                "A merged curtain must retain each former projected deck and contracted-foot vertex.");
                        }
                    }
                }
                Assert.That(covered,Is.True);
            }
            // Regression counterexample: 25..27 is straight at deck height, but
            // the contracted foot bends at its old26 station. Keep that joint.
            float middle=FoundryMap.GateLandingCentre;
            Assert.That(joined.Exists(span=>span.a.x<middle&&span.b.x>middle),Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DressedCoursesPreserveTaperedOrSlopingEnvelopeAndCoolBelowTheDeck(bool pier)
        {
            // The long face follows a sloping deck; the pier tapers toward its foot.
            var face=pier?new[]{new Vector3(-.18f,-7.9f,0),new Vector3(.18f,-7.9f,0),
                new Vector3(.33f,-2.5f,0),new Vector3(-.33f,-2.5f,0)}:
                new[]{new Vector3(-3,-7.25f,0),new Vector3(4,-5.35f,0),
                new Vector3(4.3f,-4.05f,0),new Vector3(-3.3f,-5.95f,0)};
            Type type=typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
            object builder=Activator.CreateInstance(type,true);
            typeof(FoundryRegionView).GetMethod("UndercroftCoursedFace",Static).Invoke(null,
                new object[]{builder,face[0],face[1],face[2],face[3],new Color(.43f,.46f,.52f),pier?7:2,pier?1:4});
            var vertices=(List<Vector3>)type.GetField("V",Members).GetValue(builder);
            var colors=(List<Color>)type.GetField("C",Members).GetValue(builder);
            var uv=(List<Vector2>)type.GetField("U",Members).GetValue(builder);
            var triangles=(List<int>)type.GetField("T",Members).GetValue(builder);
            Assert.That(vertices.Count,Is.EqualTo(colors.Count));
            Assert.That(vertices.Count,Is.EqualTo(uv.Count));
            var originalColors=colors.ToArray();
            Vector3 deckA=pier?Vector3.zero:new Vector3(-3.3f,0,0);
            Vector3 deckB=pier?Vector3.zero:new Vector3(4.3f,1.9f,0);
            typeof(FoundryRegionView).GetMethod("ShadeUndercroftDepth",Static)
                .Invoke(null,new object[]{builder,deckA,deckB});
            for(int i=0;i<vertices.Count;i++)
            {
                Vector3 p=vertices[i];
                Assert.That(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y),Is.False);
                Assert.That(p.z,Is.Zero);
                Assert.That(Inside(p,face),Is.True,"Dressed courses cannot expand the foundation or pier silhouette.");
                Assert.That(uv[i].x,Is.InRange(.2749f,.4151f));
                Assert.That(uv[i].y,Is.InRange(.6549f,.7471f));
                Assert.That(colors[i].a,Is.EqualTo(1));
                Assert.That(float.IsNaN(colors[i].r)||float.IsInfinity(colors[i].r),Is.False);
                Assert.That(float.IsNaN(colors[i].g)||float.IsInfinity(colors[i].g),Is.False);
                Assert.That(float.IsNaN(colors[i].b)||float.IsInfinity(colors[i].b),Is.False);
            }
            // The exact palette can evolve; lower masonry must remain dimmer and
            // cooler, including when the whole facade follows the sloping deck.
            Assert.That(colors[0].g/originalColors[0].g,Is.LessThan(colors[3].g/originalColors[3].g));
            Assert.That(colors[0].b/colors[0].r,Is.GreaterThan(colors[3].b/colors[3].r));
            foreach(Vector3 corner in face)
                Assert.That(vertices.Exists(p=>Vector3.Distance(p,corner)<.000001f),Is.True);
            for(int i=0;i<triangles.Count;i+=3)
            {
                for(int j=0;j<3;j++)Assert.That(triangles[i+j],Is.InRange(0,vertices.Count-1));
                Vector3 a=vertices[triangles[i]],b=vertices[triangles[i+1]],c=vertices[triangles[i+2]];
                Assert.That(Cross(a,b,c),Is.GreaterThan(.00000001),"No inverted or degenerate course triangles.");
                Assert.That(Inside((a+b+c)/3,face),Is.True);
            }
        }

        private static double Cross(Vector3 a,Vector3 b,Vector3 c)=>
            ((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
        private static bool Inside(Vector3 p,Vector3[] face)
        {
            for(int i=0;i<4;i++)if(Cross(face[i],face[(i+1)%4],p)<-.000005)return false;
            return true;
        }
    }
}
