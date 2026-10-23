using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryRimPipeTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;

        [Test]
        public void RimMainRunsOffTheWalkingFloorBehindTheNorthRail()
        {
            var run=(Vector2[])typeof(FoundryRegionView).GetField("RimPipeRun",Static).GetValue(null);
            float width=(float)typeof(FoundryRegionView).GetField("RimPipeWidth",Static).GetRawConstantValue();
            float v=(float)typeof(FoundryRegionView).GetField("RimPipeV",Static).GetRawConstantValue();
            Assert.That(run.Length,Is.GreaterThanOrEqualTo(3));
            Assert.That(v-width*.5f,Is.GreaterThan(FoundryMap.North+.72f),"Clear of the north rail's lantern posts.");
            for(int i=0;i+1<run.Length;i++)
                for(int k=0;k<=40;k++)
                {
                    Vector2 p=Vector2.Lerp(run[i],run[i+1],k/40f);
                    foreach(Vector2 side in new[]{new Vector2(width*.5f,0),new Vector2(-width*.5f,0),new Vector2(0,width*.5f),new Vector2(0,-width*.5f)})
                    {
                        FoundryMap.ToFloor(p.x+side.x,p.y+side.y,out float x,out float y);
                        Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.False,$"The rim main at {p} stands off the walking floor.");
                    }
                }
        }
    }
}
