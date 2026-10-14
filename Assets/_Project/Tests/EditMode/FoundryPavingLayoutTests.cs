using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryPavingLayoutTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(24)]
        [TestCase(37)]
        [TestCase(91)]
        public void VariedCourtPackingCoversEachHalfCellOnceWithoutRetiredGuideSeams(int salt)
        {
            // The room bounds are real; zone 1 is an intentional synthetic caller
            // exercising the packer's reusable unit-bond support. Actual PavingZone
            // no longer assigns it beneath either the old or new medallion centre.
            int u0=FoundryMap.West,v0=FoundryMap.South,maxU=u0,maxV=v0;
            for(int room=0;room<FoundryMap.RectangleCount;room++)
            {
                maxU=Math.Max(maxU,FoundryMap.Bound(room,2));
                maxV=Math.Max(maxV,FoundryMap.Bound(room,3));
            }
            int width=(maxU-u0)*2,height=(maxV-v0)*2;
            var zone=new sbyte[width,height];
            for(int i=0;i<width;i++)for(int j=0;j<height;j++)zone[i,j]=-1;
            for(int room=0;room<FoundryMap.RectangleCount;room++)
                for(int u=FoundryMap.Bound(room,0);u<FoundryMap.Bound(room,2);u++)
                    for(int v=FoundryMap.Bound(room,1);v<FoundryMap.Bound(room,3);v++)
                    {
                        sbyte z=(sbyte)(u>=-4&&u<4&&v>=-4&&v<4?1:u>=14?2:u<-16&&v>=2&&v<12?3:0);
                        for(int a=0;a<2;a++)for(int b=0;b<2;b++)zone[(u-u0)*2+a,(v-v0)*2+b]=z;
                    }
            var method=typeof(FoundryRegionView).GetMethod("LayAshlar",Static);
            Assert.That(method,Is.Not.Null);
            var stones=(IEnumerable)method.Invoke(null,new object[]{zone,u0,v0,salt});
            var joints=(Vector4[])typeof(FoundryRegionView).GetField("CourtBrassJoints",Static).GetValue(null);
            Assert.That(joints,Is.Empty,"Retiring the large brass box must retire its artificial paving seams too.");
            var coverage=new int[width,height];
            var workSizes=new HashSet<string>();
            int work=0,varied=0,courtArea=0,broadCourtArea=0;
            var broadCourtSizes=new HashSet<string>();
            foreach(var stone in stones)
            {
                Type type=stone.GetType();
                int Read(string name)=>(int)type.GetField(name,Fields).GetValue(stone);
                int i=Read("I"),j=Read("J"),w=Read("W"),h=Read("H"),z=Read("Zone");
                // Ordinary quarry stock now intentionally permits 3x2 design-unit
                // slabs. Gate crosswise stock retains its previous area3 contract.
                Assert.That(w*h,Is.LessThanOrEqualTo(z==2?12:24));
                Assert.That(w,Is.LessThanOrEqualTo(z==2?5:6));
                Assert.That(h,Is.LessThanOrEqualTo(z==2?5:6));
                for(int x=i;x<i+w;x++)for(int y=j;y<j+h;y++)
                {
                    Assert.That(x,Is.InRange(0,width-1));Assert.That(y,Is.InRange(0,height-1));
                    Assert.That(zone[x,y],Is.EqualTo(z),"A slab must never cover void or cross a material-zone boundary.");
                    coverage[x,y]++;
                }
                if(z==1){Assert.That(w,Is.EqualTo(2));Assert.That(h,Is.EqualTo(2));}
                if(z==3){work++;if(w!=2||h!=2)varied++;workSizes.Add(w+"x"+h);}
                if(z==0)
                {
                    courtArea+=w*h;
                    if(w*h>12){broadCourtArea+=w*h;broadCourtSizes.Add(w+"x"+h);}
                }
            }
            for(int i=0;i<width;i++)for(int j=0;j<height;j++)
                Assert.That(coverage[i,j],Is.EqualTo(zone[i,j]<0?0:1),$"Exact coverage at {i},{j}.");
            Assert.That(workSizes.Count,Is.GreaterThanOrEqualTo(3),"The workyard must not return to a single square grid.");
            Assert.That(varied,Is.GreaterThan(work/2));
            Assert.That(broadCourtArea,Is.GreaterThan(courtArea*.30f),
                "Broader quarry slabs must own a meaningful share of the ordinary court, not just exist in the size catalogue.");
            Assert.That(broadCourtSizes.Count,Is.GreaterThanOrEqualTo(3),
                "Broad stock retains unequal proportions instead of becoming another square grid.");
        }

        [Test]
        public void LaidCourtStoneKeepsOneMineralBodyUnderContinuousWorldLight()
        {
            float low=float.MaxValue,high=float.MinValue;
            foreach(Vector2 origin in new[]{new Vector2(-30,8),new Vector2(-28,6),new Vector2(-23,-10),
                new Vector2(-15,-12),new Vector2(-8,-6),new Vector2(1,2),new Vector2(7,8)})
            {
                var flag=BuildFlag(origin.x,origin.y,3,2,0);
                Color body=InteriorMaterial(flag,origin,3,2);
                low=Mathf.Min(low,(body.r+body.g+body.b)/3);
                high=Mathf.Max(high,(body.r+body.g+body.b)/3);
                foreach(float channel in new[]{body.r,body.g,body.b})Assert.That(channel,Is.InRange(.80f,1.15f));
                int interior=0;
                // Face vertices follow their bed and bevel. Last occurrences at
                // interior coordinates are the visible face, not hidden mortar.
                var seen=new HashSet<Vector2Int>();
                for(int i=flag.U.Count-1;i>=0;i--)
                {
                    Vector2 p=Design(flag.U[i]);
                    var key=new Vector2Int(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.y*10000));
                    if(!seen.Add(key)||Depth(p,origin,3,2)<.5f-.0001f)continue;
                    Color material=StoneMineral(flag.C[i],p);
                    Assert.That(material.r,Is.EqualTo(body.r).Within(.0001f));
                    Assert.That(material.g,Is.EqualTo(body.g).Within(.0001f));
                    Assert.That(material.b,Is.EqualTo(body.b).Within(.0001f));
                    Assert.That(flag.C[i].a,Is.EqualTo(1));interior++;
                }
                Assert.That(interior,Is.GreaterThanOrEqualTo(2),"Verify several actual face samples under different world light.");
            }
            Assert.That(high-low,Is.InRange(.055f,.18f),
                "Whole stones expose restrained mineral differences without becoming pale/dark checkerboard tiles.");
        }

        [TestCase(-30f,8f,2f,1.5f)]
        [TestCase(-28f,6f,3f,2f)]
        public void WorkedShoulderHasVisibleFaceSamplesAndLeavesItsBroadInteriorUntouched(float u,float v,float width,float height)
        {
            Vector2 origin=new Vector2(u,v);
            var flag=BuildFlag(u,v,width,height,3);
            Color body=InteriorMaterial(flag,origin,width,height);
            var visible=new HashSet<int>();
            for(int t=0;t<flag.T.Count;t+=3)
            {
                int a=flag.T[t],b=flag.T[t+1],c=flag.T[t+2];
                Vector2 ab=Design(flag.U[b])-Design(flag.U[a]),ac=Design(flag.U[c])-Design(flag.U[a]);
                if(Math.Abs((double)ab.x*ac.y-(double)ab.y*ac.x)<=1e-8)continue;
                visible.Add(a);visible.Add(b);visible.Add(c);
            }
            int shoulder=0;float strongest=0;
            for(int i=0;i<flag.U.Count;i++)
            {
                Vector2 p=Design(flag.U[i]);
                Assert.That(p.x,Is.InRange(u-.0001f,u+width+.0001f));
                Assert.That(p.y,Is.InRange(v-.0001f,v+height+.0001f));
                Assert.That(flag.C[i].a,Is.EqualTo(1));
                float depth=Depth(p,origin,width,height);
                if(!visible.Contains(i)||depth<.15f||depth>.30f)continue;
                Color material=StoneMineral(flag.C[i],p);
                float r=material.r/body.r,g=material.g/body.g,b=material.b/body.b;
                if(r<=1.01f)continue;
                // Mortar and narrow geometric bevels cannot satisfy this: the
                // mineral shoulder must actually be sampled inside the face.
                Assert.That(g,Is.EqualTo(r).Within(.0001f));
                Assert.That(b,Is.EqualTo(r).Within(.0001f));
                strongest=Mathf.Max(strongest,r);shoulder++;
            }
            Assert.That(shoulder,Is.GreaterThanOrEqualTo(1));
            Assert.That(strongest,Is.InRange(1.015f,1.0451f));
            Assert.That(flag.U.Count,Is.LessThanOrEqualTo(240),"Selected wear remains a bounded subdivision of one original face.");
        }

        [TestCase(-23f,-10f,0)]
        [TestCase(-30f,8f,3)]
        public void OpenCourtMortarSharesItsStoneMineralsWithoutDarkOutlineContrast(float u,float v,int zone)
        {
            Vector2 origin=new Vector2(u,v);
            var flag=BuildFlag(u,v,3,2,zone);
            Color body=InteriorMaterial(flag,origin,3,2);
            Color mortar=StoneMineral(flag.C[0],Design(flag.U[0]));
            float r=mortar.r/body.r,g=mortar.g/body.g,b=mortar.b/body.b;
            Assert.That(r,Is.InRange(.88f,.93f));
            Assert.That(g,Is.EqualTo(r).Within(.0001f));
            Assert.That(b,Is.EqualTo(r).Within(.0001f),"Joint dust belongs to the same mineral family while the existing light still crosses it.");
        }

        private sealed class FlagMesh
        {
            internal List<Vector2> U;
            internal List<Color> C;
            internal List<int> T;
        }

        private static FlagMesh BuildFlag(float u,float v,float width,float height,int zone)
        {
            Type view=typeof(FoundryRegionView),builderType=view.GetNestedType("Builder",BindingFlags.NonPublic);
            Type stoneType=view.GetNestedType("AshlarStone",BindingFlags.NonPublic);
            object builder=Activator.CreateInstance(builderType,true),stone=Activator.CreateInstance(stoneType);
            void Set(string name,int value)=>stoneType.GetField(name,Fields).SetValue(stone,value);
            Set("I",Mathf.RoundToInt(u*2));Set("J",Mathf.RoundToInt(v*2));
            Set("W",Mathf.RoundToInt(width*2));Set("H",Mathf.RoundToInt(height*2));Set("Zone",zone);
            view.GetMethod("AshlarFlagFace",Static).Invoke(null,new[]{builder,stone,(object)false});
            return new FlagMesh{U=(List<Vector2>)builderType.GetField("U",Fields).GetValue(builder),
                C=(List<Color>)builderType.GetField("C",Fields).GetValue(builder),
                T=(List<int>)builderType.GetField("T",Fields).GetValue(builder)};
        }

        private static Color InteriorMaterial(FlagMesh flag,Vector2 origin,float width,float height)
        {
            for(int i=flag.U.Count-1;i>=0;i--)
            {
                Vector2 p=Design(flag.U[i]);
                if(Depth(p,origin,width,height)>=.5f-.0001f)return StoneMineral(flag.C[i],p);
            }
            Assert.Fail("Production flag has no sampled broad interior.");return Color.clear;
        }

        private static float Depth(Vector2 p,Vector2 origin,float width,float height)=>
            Mathf.Min(p.x-origin.x,origin.x+width-p.x,p.y-origin.y,origin.y+height-p.y);

        private static Vector2 Design(Vector2 uv)=>
            (uv-Vector2.one)*(FoundryRegionView.PavingGrainSpan/(2*FoundryMap.Axis));

        private static Color StoneMineral(Color color,Vector2 p)
        {
            var light=(Color)typeof(FoundryRegionView).GetMethod("FloorLight",Static).Invoke(null,new object[]{p.x,p.y});
            // A laid stone retains one quarry body beneath world illumination
            // and the broad entry/apron use field. Remove both spatial fields
            // before testing that body and its local worn shoulder. The separate
            // court-material tests verify the emitted use field exactly once,
            // its world continuity, bounded gains and protected combat centre.
            float use=(float)typeof(FoundryRegionView).GetMethod("PavingCourtUse",Static).Invoke(null,new object[]{p.x,p.y});
            return new Color(color.r/(light.r*use),color.g/(light.g*use),color.b/(light.b*use),1);
        }
    }
}
