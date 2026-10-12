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

        [Test]
        public void PerimeterPostGeometryLightSourcesAndDepthOwnersShareTheCurrentBoundary()
        {
            var posts=(Vector2[])typeof(FoundryRegionView).GetField("CourtFinials",Static).GetValue(null);
            var lamps=(Vector2[])typeof(FoundryRegionView).GetField("CourtLanternPosts",Static).GetValue(null);
            Type builderType=typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
            var go=new GameObject("Perimeter ownership proof");
            try
            {
                var view=go.AddComponent<FoundryRegionView>();
                typeof(FoundryRegionView).GetMethod("RegisterDepthLandmarks",Members).Invoke(view,null);
                var owners=(System.Collections.IEnumerable)typeof(FoundryRegionView).GetField("_depthFootprints",Members).GetValue(view);
                foreach(Vector2 post in posts)
                {
                    AssertScenicBox(post,.4f);
                    bool hasOwner=false;
                    foreach(object owner in owners)
                    {
                        Type type=owner.GetType();
                        Vector3 left=(Vector3)type.GetField("Left",Members).GetValue(owner);
                        Vector3 right=(Vector3)type.GetField("Right",Members).GetValue(owner);
                        if(Vector3.Distance(left,Project(post.x-.4f,post.y+.4f))<.00001f&&
                            Vector3.Distance(right,Project(post.x+.4f,post.y-.4f))<.00001f)hasOwner=true;
                    }
                    Assert.That(hasOwner,Is.True,"The raised post must retain its own relocated depth footprint.");
                    object stone=Activator.CreateInstance(builderType,true);
                    typeof(FoundryRegionView).GetMethod("CourtFinialMasonry",Static).Invoke(null,new[]{stone,(object)post});
                    var vertices=(List<Vector3>)builderType.GetField("V",Members).GetValue(stone);
                    Assert.That(vertices.Count,Is.GreaterThan(0));
                    float leftX=Project(post.x-.4f,post.y+.4f).x,rightX=Project(post.x+.4f,post.y-.4f).x;
                    foreach(Vector3 vertex in vertices)Assert.That(vertex.x,Is.InRange(leftX,rightX));
                }
                var pools=(Array)typeof(FoundryRegionView).GetMethod("ForgeLamps",Static).Invoke(null,null);
                foreach(Vector2 lamp in lamps)
                {
                    Assert.That(Array.Exists(posts,p=>p==lamp),Is.True,"No baked light may remain without its moved post.");
                    Assert.That((bool)typeof(FoundryRegionView).GetMethod("CourtLantern",Static).Invoke(null,new object[]{lamp}),Is.True);
                    bool hasPool=false;
                    foreach(object pool in pools)
                    {
                        Vector4 box=(Vector4)pool.GetType().GetField("Box",Members).GetValue(pool);
                        if(box==new Vector4(lamp.x,lamp.y,lamp.x,lamp.y))hasPool=true;
                    }
                    Assert.That(hasPool,Is.True,"A moved physical lantern must move its baked illumination too.");
                }
                var bastions=(Vector4[])typeof(FoundryRegionView).GetField("Bastions",Static).GetValue(null);
                foreach(Vector4 box in bastions)
                    for(int x=0;x<=4;x++)for(int y=0;y<=4;y++)
                        AssertScenic(new Vector2(Mathf.Lerp(box.x+.0001f,box.z-.0001f,x*.25f),
                            Mathf.Lerp(box.y+.0001f,box.w-.0001f,y*.25f)));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [Test]
        public void ShortenedExposedBridgeKeepsSeparateLanternRowsAndBorneSupportRows()
        {
            float head=(float)typeof(FoundryRegionView).GetField("BridgeHeadS",Static).GetRawConstantValue();
            float foot=(float)typeof(FoundryRegionView).GetField("BridgeFootS",Static).GetRawConstantValue();
            var lanterns=(Vector2[])typeof(FoundryRegionView).GetField("BridgeLanterns",Static).GetValue(null);
            float previous=head;
            for(int i=0;i<lanterns.Length;i+=2)
            {
                Vector2 left=lanterns[i],right=lanterns[i+1];
                float station=left.x+left.y;
                Assert.That(right.x+right.y,Is.EqualTo(station).Within(.00001f));
                Assert.That(previous-station,Is.GreaterThan(5),"Avoid burying a cage in the keeper or preceding lantern silhouette.");
                Assert.That(station-foot,Is.GreaterThan(5),"The final lantern must clear the fixed mouth beacon.");
                AssertScenicBox(left,.32f);AssertScenicBox(right,.32f);
                previous=station;
            }
            var rows=(float[])typeof(FoundryRegionView).GetField("BridgeSupportStations",Static).GetValue(null);
            previous=head;
            foreach(float station in rows)
            {
                Assert.That(station,Is.LessThan(previous));
                Assert.That(station,Is.GreaterThanOrEqualTo(FoundryMap.BridgeStart));
                // Full rear return and outer arch shoulder stay below the exposed
                // bridge, never hidden inside the expanded court foundation.
                float furthestV=(station+1.3f+FoundryMap.BridgeHalfWidth+.18f)*.5f;
                Assert.That(furthestV,Is.LessThan(FoundryMap.South));
                previous=station;
            }
            var heads=(Vector2[])typeof(FoundryRegionView).GetField("BridgeHeadPosts",Static).GetValue(null);
            foreach(Vector2 post in heads)AssertScenicBox(post,.7f);
        }

        [Test]
        public void SouthHangingChainRequiresAnEmittedUnscreenedMajorPierSocket()
        {
            var choose=typeof(FoundryRegionView).GetMethod("SelectSouthChainPier",Static);
            Vector2 valid=new Vector2(0,FoundryMap.South),screened=new Vector2(2,FoundryMap.South);
            Assert.That((bool)typeof(FoundryRegionView).GetMethod("PierScreened",Static).Invoke(null,new object[]{screened}),Is.True);
            var piers=new List<(Vector2 at,bool major)>
            {
                (new Vector2(-4,FoundryMap.South),false),
                (new Vector2(-4,FoundryMap.South+4),true),(screened,true),(valid,true)
            };
            Assert.That(choose.Invoke(null,new object[]{piers}),Is.EqualTo(valid),
                "A nearby minor, old-edge or screened support must not inherit the chain.");
            piers.RemoveAt(piers.Count-1);
            Assert.That(choose.Invoke(null,new object[]{piers}),Is.Null,"No emitted iron socket means no floating fallback chain.");
            piers.Add((new Vector2(-8,FoundryMap.South),true));piers.Add((valid,true));
            Assert.That(choose.Invoke(null,new object[]{piers}),Is.EqualTo(valid));
            piers.Reverse();
            Assert.That(choose.Invoke(null,new object[]{piers}),Is.EqualTo(valid),"Tied candidates must not depend on HashSet traversal order.");
        }

        private static Vector3 Project(float u,float v)=>(Vector3)typeof(FoundryRegionView)
            .GetMethod("Project",Static).Invoke(null,new object[]{u,v});

        private static void AssertScenic(Vector2 point)
        {
            FoundryMap.ToFloor(point.x,point.y,out float x,out float y);
            Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.False,"Raised scenery and its depth owner must remain outside the walking union.");
        }

        private static void AssertScenicBox(Vector2 centre,float half)
        {
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)AssertScenic(centre+new Vector2(x*half,y*half));
        }

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
