using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryBridgeArchitectureTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;

        [TestCase(-1f)]
        [TestCase(1f)]
        public void ParapetPanelsAndOwnersStayOutsidePassageThroughBothCourtSeams(float side)
        {
            MethodInfo outlineMethod=typeof(FoundryRegionView).GetMethod("BridgeParapetOutline",Static);
            MethodInfo ownerMethod=typeof(FoundryRegionView).GetMethod("BridgeParapetOwner",Static);
            float width=(float)typeof(FoundryRegionView).GetField("BridgeParapetWidth",Static).GetValue(null);
            float step=(float)typeof(FoundryRegionView).GetField("BridgeParapetStep",Static).GetValue(null);
            float innerEnd=side<0?FoundryMap.BridgeWestSeam:FoundryMap.BridgeEastSeam;
            float end=Mathf.Max(innerEnd,2*FoundryMap.South+side*(FoundryMap.BridgeHalfWidth+width));
            bool reachesInnerSeam=false,reachesOuterSeam=false;
            for(float s=FoundryMap.BridgeStart;s<end;s+=step)
            {
                var outline=(List<Vector2>)outlineMethod.Invoke(null,new object[]{s,Mathf.Min(s+step,end),side});
                if(outline.Count<3)continue;
                foreach(Vector2 point in outline)
                {
                    Assert.That(side*point.y,Is.InRange((float)FoundryMap.BridgeHalfWidth,FoundryMap.BridgeHalfWidth+width+.00001f));
                    Assert.That((point.x-point.y)*.5f,Is.LessThanOrEqualTo(FoundryMap.South+.00001f),
                        "The outboard west lip reaches the court before its inner edge.");
                    Assert.That(point.x,Is.GreaterThanOrEqualTo(FoundryMap.BridgeStart));
                    if(Mathf.Abs(point.y-side*FoundryMap.BridgeHalfWidth)<.00001f&&Mathf.Abs(point.x-innerEnd)<.00001f)
                        reachesInnerSeam=true;
                    if(Mathf.Abs(point.y-side*(FoundryMap.BridgeHalfWidth+width))<.00001f&&
                        Mathf.Abs(point.x-(2*FoundryMap.South+point.y))<.00001f)reachesOuterSeam=true;
                }
                Vector4 box=(Vector4)ownerMethod.Invoke(null,new object[]{outline});
                Assert.That(box.z,Is.GreaterThan(box.x));Assert.That(box.w,Is.GreaterThan(box.y));
                foreach(Vector2 corner in new[]{new Vector2(box.x,box.y),new Vector2(box.z,box.y),
                    new Vector2(box.z,box.w),new Vector2(box.x,box.w)})
                {
                    FoundryMap.ToFloor(corner.x,corner.y,out float x,out float y);
                    Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.False,
                        "A diagonal parapet owner must never claim the walking lane or court.");
                    Assert.That(corner.x+corner.y,Is.InRange(s-.00001f,Mathf.Min(s+step,end)+.00001f));
                }
                CheckEmittedParapet(outline,side);
            }
            Assert.That(reachesInnerSeam,Is.True,"Keep the asymmetric physical passage join exact.");
            Assert.That(reachesOuterSeam,Is.True,"The cap must seat against the court instead of overhanging it.");
        }

        private static void CheckEmittedParapet(List<Vector2> outline,float side)
        {
            Type type=typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
            object builder=Activator.CreateInstance(type,true);
            typeof(FoundryRegionView).GetMethod("BridgeParapetCourses",Static).Invoke(null,new[]{builder,outline});
            var vertices=(List<Vector3>)type.GetField("V",Members).GetValue(builder);
            foreach(Vector3 p in vertices)
                Assert.That(side*p.x/FoundryMap.Axis,Is.GreaterThanOrEqualTo(FoundryMap.BridgeHalfWidth-.00001f),
                    "Actual raised geometry must keep the full passage width.");
            // Inspect the actual cap helper separately from its wall. Invert its
            // monotone projection numerically instead of assuming a flat top; the
            // reconstructed footprint must still respect both asymmetric seams.
            object cap=Activator.CreateInstance(type,true);
            const float height=.78f;
            typeof(FoundryRegionView).GetMethod("BridgeParapetCap",Static)
                .Invoke(null,new object[]{cap,outline,height,Color.white});
            var topVertices=(List<Vector3>)type.GetField("V",Members).GetValue(cap);
            var triangles=(List<int>)type.GetField("T",Members).GetValue(cap);
            Assert.That(topVertices.Count,Is.GreaterThan(0));
            MethodInfo lift=typeof(FoundryRegionView).GetMethod("BridgeParapetCapHeight",Static);
            MethodInfo deck=typeof(FoundryRegionView).GetMethod("Deck",Static);
            float minS=float.MaxValue,maxS=float.MinValue;
            foreach(Vector2 p in outline){minS=Mathf.Min(minS,p.x);maxS=Mathf.Max(maxS,p.x);}
            foreach(Vector3 p in topVertices)
            {
                float d=p.x/FoundryMap.Axis;
                float ProjectY(float station)
                {
                    Vector3 ground=(Vector3)deck.Invoke(null,new object[]{station,d});
                    return ground.y+(float)lift.Invoke(null,new object[]{new Vector2(station,d),height});
                }
                // At a shallow joint several adjacent float stations map to the
                // same screen y. An unconstrained inverse selects an arbitrary
                // plateau edge, even for an exactly legal source vertex (-66).
                // Ask whether the ACTUAL vertex has a valid forward image inside
                // the original panel and court plane instead of moving that edge.
                float lo=minS,hi=Mathf.Min(maxS,2*FoundryMap.South+d);
                float lowerY=ProjectY(lo),upperY=ProjectY(hi);
                // One output ULP covers the x/Axis round trip through the bevel;
                // a second covers the independently rounded projected-height sum.
                // This is screen precision, not an expanded design footprint.
                float allowance=2*Mathf.Max(ScreenUlp(p.y),Mathf.Max(ScreenUlp(lowerY),ScreenUlp(upperY)));
                if(hi<lo)
                {
                    Assert.That(Mathf.Abs(lowerY-upperY),Is.LessThanOrEqualTo(allowance),
                        "A clipped corner can collapse only by output-rounding precision.");
                    hi=lo;upperY=lowerY;
                }
                Assert.That(p.y,Is.InRange(lowerY-allowance,upperY+allowance),
                    "An emitted cap point must have a forward projection on the legal footprint.");
                for(int iteration=0;iteration<24;iteration++)
                {
                    float middle=(lo+hi)*.5f;
                    if(ProjectY(middle)<p.y)lo=middle;else hi=middle;
                }
                // At float precision the final interval may straddle adjacent
                // representable stations; averaging can select the farther image.
                float station=Mathf.Abs(ProjectY(lo)-p.y)<=Mathf.Abs(ProjectY(hi)-p.y)?lo:hi;
                Assert.That(Mathf.Abs(ProjectY(station)-p.y),Is.LessThanOrEqualTo(allowance),
                    $"Clamping needs a forward match: point={p}, d={d:R}, lo={lo:R}, hi={hi:R}.");
                float u=(station+d)*.5f,v=(station-d)*.5f;
                Assert.That(v,Is.LessThanOrEqualTo(FoundryMap.South+.00002f));
                Assert.That(u+v,Is.GreaterThanOrEqualTo(FoundryMap.BridgeStart-.00002f));
                Assert.That(station,Is.InRange(minS-.00002f,maxS+.00002f));
            }
            for(int i=0;i<triangles.Count;i+=3)
            {
                Vector3 a=topVertices[triangles[i]],b=topVertices[triangles[i+1]],c=topVertices[triangles[i+2]];
                double area=Math.Abs(((double)b.x-a.x)*(c.y-a.y)-((double)b.y-a.y)*(c.x-a.x));
                Assert.That(area,Is.GreaterThan(.00000001),"The cap fan must contain real triangles, not repeated centre indices.");
            }
        }

        private static float ScreenUlp(float value)
        {
            float magnitude=Mathf.Abs(value);
            int bits=BitConverter.SingleToInt32Bits(magnitude);
            return BitConverter.Int32BitsToSingle(bits+1)-magnitude;
        }

        [Test]
        public void KeeperConsoleParapetLeavesExistingSeatAndStatueSocketsUnburied()
        {
            float station=(float)typeof(FoundryRegionView).GetField("BridgeHeadS",Static).GetRawConstantValue();
            Type type=typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
            foreach(float side in new[]{-1f,1f})
            {
                var outline=(List<Vector2>)typeof(FoundryRegionView).GetMethod("BridgeParapetOutline",Static)
                    .Invoke(null,new object[]{station-.4f,station+.3f,side});
                object builder=Activator.CreateInstance(type,true);
                typeof(FoundryRegionView).GetMethod("BridgeParapetCourses",Static).Invoke(null,new[]{builder,outline});
                var vertices=(List<Vector3>)type.GetField("V",Members).GetValue(builder);
                var triangles=(List<int>)type.GetField("T",Members).GetValue(builder);
                var uv=(List<Vector2>)type.GetField("U",Members).GetValue(builder);
                float top=float.MinValue;
                foreach(Vector3 p in vertices)
                {
                    Assert.That(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y),Is.False);
                    Assert.That(Mathf.Abs(p.x),Is.GreaterThanOrEqualTo(FoundryMap.BridgeHalfWidth*FoundryMap.Axis-.00001f));
                    top=Mathf.Max(top,p.y);
                }
                float upperDeck=(station+.3f)*FoundryMap.Axis*.5f;
                Assert.That(top-upperDeck,Is.InRange(.169f,.18001f),"The shallow joint stays at/below the .18 seating slab and .38 statue foot.");
                Assert.That(uv.Count,Is.EqualTo(vertices.Count));
                foreach(int index in triangles)Assert.That(index,Is.InRange(0,vertices.Count-1));
            }
        }
    }
}
