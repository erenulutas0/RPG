using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class FoundryMapTests
    {
        [TestCase(-23f,-13f,-20f,-9f)]
        [TestCase(13f,-13f,9f,-9f)]
        [TestCase(-23f,17f,-21f,15f)]
        [TestCase(13f,17f,11f,15f)]
        public void ChamferRejectsRemovedCornerAndStopsLongMovement(float cutU,float cutV,float safeU,float safeV)
        {
            FoundryMap.ToFloor(cutU,cutV,out float tx,out float ty);
            FoundryMap.ToFloor(safeU,safeV,out float x,out float y);
            Assert.That(FoundryMap.Contains(tx,ty,0),Is.False);
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,0),Is.False);
            FoundryMap.Move(ref x,ref y,tx-x,ty-y,.6f);
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
            Assert.That(ArenaFloor.FloorDistanceSquared(x,y,tx,ty),Is.GreaterThan(1));
        }
        [Test] public void ChamferKeepsFullBridgeAndItsCourtSeamOpen()
        {
            for(float s=-47;s<=-23;s+=.25f)
                foreach(float d in new[]{-2.7f,0,2.7f})
                {
                    FoundryMap.ToFloor((s+d)*.5f,(s-d)*.5f,out float x,out float y);
                    Assert.That(FoundryMap.Contains(x,y,.6f),Is.True,$"Bridge s={s},d={d}");
                }
        }
        [Test] public void StraightBridgeBlocksSidewaysMotionAndConnectsToCourt()
        {
            FoundryMap.ToFloor(-23,-23,out float x,out float y);
            FoundryMap.ToFloor(8,-8,out float dx,out float dy);
            FoundryMap.Move(ref x,ref y,dx,dy,.6f);
            FoundryMap.ToDesign(x,y,out float u,out float v);
            Assert.That(u-v,Is.LessThanOrEqualTo(2.81f));
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
            FoundryMap.ToFloor(-23,-23,out x,out y);
            FoundryMap.ToFloor(0,0,out float tx,out float ty);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.True);
        }
        [Test] public void ActualPackMotionPursuesAroundSideVoid()
        {
            FoundryMap.ToFloor(-20,6,out float x,out float y);
            FoundryMap.ToFloor(0,6,out float hx,out float hy);
            var pack=new PackMotion(.9f,2f,ArenaGeometry.Foundry,hx,hy);
            pack.Add(new HealthState(50),new EntryPlacement(EntrySide.Left,0,x,y),2.5f,1.2f);
            for(int frame=0;frame<3600&&!pack.HasArrived(0);frame++)
            {
                pack.Step(1f/60,hx,hy);
                Assert.That(FoundryMap.Segment(x,y,pack.XOf(0),pack.YOf(0),.6f),Is.True);
                x=pack.XOf(0);y=pack.YOf(0);
            }
            Assert.That(pack.HasArrived(0),Is.True,$"Enemy {x},{y}; hero {hx},{hy}; distance {ArenaFloor.FloorDistanceSquared(x,y,hx,hy)}");
        }
        [TestCase(.5f)]
        [TestCase(.61f)]
        [TestCase(.99f)]
        [TestCase(.01f)]
        public void EveryInteriorCellCanSpawnTenWithoutOverlapOrVoid(float offset)
        {
            var points=new EntryPlacement[10];
            for(int u=-28;u<32;u++)for(int v=-28;v<FoundryMap.North;v++)
            {
                FoundryMap.ToFloor(u+offset,v+offset,out float x,out float y);
                if(!FoundryMap.Contains(x,y,.6f))continue;
                FoundryMap.PlacePack(0,10,x,y,.9f,points);
                for(int i=0;i<10;i++)
                {
                    Assert.That(FoundryMap.Contains(points[i].X,points[i].Y,.6f),Is.True);
                    Assert.That(FoundryMap.Segment(x,y,points[i].X,points[i].Y,.6f),Is.True);
                    for(int j=0;j<i;j++)Assert.That(ArenaFloor.FloorDistanceSquared(points[i].X,points[i].Y,points[j].X,points[j].Y),Is.GreaterThanOrEqualTo(.9f*.9f));
                }
            }
        }
        [Test] public void LongHeroStepCannotCrossTheSideHole()
        {
            FoundryMap.ToFloor(-20,6,out float x,out float y);
            FoundryMap.ToFloor(17,0,out float dx,out float dy);
            FoundryMap.Move(ref x,ref y,dx,dy,.6f);
            FoundryMap.ToDesign(x,y,out float u,out _);
            Assert.That(u,Is.LessThan(FoundryMap.PitWest));
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
        }
        [Test] public void PursuitRoutesAroundHoleAndThroughPassage()
        {
            foreach(var goal in new[]{new[]{0f,6f},new[]{28f,6f},new[]{-23f,-23f},
                new[]{-21f,15f},new[]{11f,15f},new[]{30f,-1f},new[]{30f,11f}})
            {
                FoundryMap.ToFloor(-20,6,out float x,out float y);
                FoundryMap.ToFloor(goal[0],goal[1],out float hx,out float hy);
                var route=new FoundryPursuit();
                for(int tick=0;tick<6000&&ArenaFloor.FloorDistanceSquared(x,y,hx,hy)>.04f;tick++)
                {
                    float tx=hx,ty=hy;route.Target(x,y,hx,hy,ref tx,ref ty);
                    float dx=tx-x,dy=ty-y;float length=(float)System.Math.Sqrt(dx*dx+dy*dy);
                    if(length>0)FoundryMap.Move(ref x,ref y,dx/length*System.Math.Min(.08f,length),dy/length*System.Math.Min(.08f,length),.6f);
                    Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
                }
                Assert.That(ArenaFloor.FloorDistanceSquared(x,y,hx,hy),Is.LessThanOrEqualTo(.04f),"A routed enemy must reach the hero.");
            }
        }
    }
}
