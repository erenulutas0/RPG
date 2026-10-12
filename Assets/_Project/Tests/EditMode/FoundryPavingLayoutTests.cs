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
            int work=0,varied=0;
            foreach(var stone in stones)
            {
                Type type=stone.GetType();
                int Read(string name)=>(int)type.GetField(name,Fields).GetValue(stone);
                int i=Read("I"),j=Read("J"),w=Read("W"),h=Read("H"),z=Read("Zone");
                Assert.That(w*h,Is.LessThanOrEqualTo(12));
                for(int x=i;x<i+w;x++)for(int y=j;y<j+h;y++)
                {
                    Assert.That(x,Is.InRange(0,width-1));Assert.That(y,Is.InRange(0,height-1));
                    Assert.That(zone[x,y],Is.EqualTo(z),"A slab must never cover void or cross a material-zone boundary.");
                    coverage[x,y]++;
                }
                if(z==1){Assert.That(w,Is.EqualTo(2));Assert.That(h,Is.EqualTo(2));}
                if(z==3){work++;if(w!=2||h!=2)varied++;workSizes.Add(w+"x"+h);}
            }
            for(int i=0;i<width;i++)for(int j=0;j<height;j++)
                Assert.That(coverage[i,j],Is.EqualTo(zone[i,j]<0?0:1),$"Exact coverage at {i},{j}.");
            Assert.That(workSizes.Count,Is.GreaterThanOrEqualTo(3),"The workyard must not return to a single square grid.");
            Assert.That(varied,Is.GreaterThan(work/2));
        }
    }
}
