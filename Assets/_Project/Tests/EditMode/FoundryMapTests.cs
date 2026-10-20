using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class FoundryMapTests
    {
        [TestCase(0)]
        [TestCase(1)]
        public void WorkIslandsBlockActorsButRetainTheirContinuousFloor(int island)
        {
            float u=(FoundryMap.WorkIslandBound(island,0)+FoundryMap.WorkIslandBound(island,2))*.5f;
            float v=(FoundryMap.WorkIslandBound(island,1)+FoundryMap.WorkIslandBound(island,3))*.5f;
            FoundryMap.ToFloor(u,v,out float x,out float y);
            Assert.That(FoundryMap.SurfaceContains(x,y,.6f),Is.True,"Equipment stands on continuous paving.");
            Assert.That(FoundryMap.Cell((int)System.Math.Floor(u),(int)System.Math.Floor(v)),Is.True);
            Assert.That(FoundryMap.UncutCell((int)System.Math.Floor(u),(int)System.Math.Floor(v)),Is.True);
            Assert.That(FoundryMap.Contains(x,y,0),Is.False,"Actors cannot enter the solid footprint.");
            Assert.That(FoundryMap.Segment(x,y,x,y,0),Is.False,"A zero-length query agrees with occupancy.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void WorkIslandSweepsRejectCrossingsAndClearanceGrazes(int island)
        {
            float left=FoundryMap.WorkIslandBound(island,0),right=FoundryMap.WorkIslandBound(island,2);
            float bottom=FoundryMap.WorkIslandBound(island,1),top=FoundryMap.WorkIslandBound(island,3);
            FoundryMap.ToFloor(left-1,(bottom+top)*.5f,out float x,out float y);
            FoundryMap.ToFloor(right+1,(bottom+top)*.5f,out float tx,out float ty);
            Assert.That(FoundryMap.Contains(x,y,.6f)&&FoundryMap.Contains(tx,ty,.6f),Is.True);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.False,"Free endpoints cannot tunnel across a work island.");
            Assert.That(FoundryMap.Segment(tx,ty,x,y,.6f),Is.False);
            // A line misses the footprint but clips its actor clearance.
            FoundryMap.ToFloor(left-1,bottom-.59f,out x,out y);
            FoundryMap.ToFloor(right+1,bottom-.59f,out tx,out ty);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,0),Is.True);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.False);
            FoundryMap.ToFloor(left-1,bottom-.61f,out x,out y);
            FoundryMap.ToFloor(right+1,bottom-.61f,out tx,out ty);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.True);
            FoundryMap.ToFloor(left-1,bottom-.6f,out x,out y);
            FoundryMap.ToFloor(right+1,bottom-.6f,out tx,out ty);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.False,"Nominal grazing remains blocked after float projection round trips.");
            Assert.That(FoundryMap.Segment(tx,ty,x,y,.6f),Is.False);
            FoundryMap.ToFloor((left+right)*.5f,bottom-.6f,out x,out y);
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.False,"Point clearance uses the same boundary relief as swept clearance.");
            // Both endpoints are outside the box, but a diagonal clips only its corner.
            FoundryMap.ToFloor(left-.9f,bottom-.5f,out x,out y);
            FoundryMap.ToFloor(left-.5f,bottom-.9f,out tx,out ty);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.True);
            FoundryMap.ToFloor(left-.9f,bottom-.1f,out x,out y);
            FoundryMap.ToFloor(left-.1f,bottom-.9f,out tx,out ty);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.False,"Short corner crossings are checked analytically.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LongMovementStopsAtWorkIslandAndDiagonalInputSlidesAlongIt(int island)
        {
            float left=FoundryMap.WorkIslandBound(island,0),right=FoundryMap.WorkIslandBound(island,2);
            float middle=(FoundryMap.WorkIslandBound(island,1)+FoundryMap.WorkIslandBound(island,3))*.5f;
            FoundryMap.ToFloor(left-1,middle,out float x,out float y);
            FoundryMap.ToFloor(right+1,middle,out float tx,out float ty);
            FoundryMap.Move(ref x,ref y,tx-x,ty-y,.6f);
            FoundryMap.ToDesign(x,y,out float stoppedU,out float stoppedV);
            Assert.That(stoppedU,Is.LessThan(left-.59f));
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
            FoundryMap.ToFloor(.5f,-.4f,out float dx,out float dy);
            Assert.That(FoundryMap.Move(ref x,ref y,dx,dy,.6f),Is.True,"Blocked forward input retains its clear lateral component.");
            FoundryMap.ToDesign(x,y,out float slidU,out float slidV);
            Assert.That(slidU,Is.LessThan(left-.59f));
            Assert.That(slidV,Is.LessThan(stoppedV-.3f));
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void PursuitAndTenEnemyEntriesRouteAroundEachWorkIsland(int island)
        {
            float left=FoundryMap.WorkIslandBound(island,0),right=FoundryMap.WorkIslandBound(island,2);
            float bottom=FoundryMap.WorkIslandBound(island,1),top=FoundryMap.WorkIslandBound(island,3);
            float middleU=(left+right)*.5f,middleV=(bottom+top)*.5f;
            AssertRoute(left-1,middleV,right+1,middleV);
            AssertRoute(right+1,middleV,left-1,middleV);
            AssertRoute(middleU,bottom-1,middleU,top+1);
            AssertRoute(middleU,top+1,middleU,bottom-1);
            var entries=new EntryPlacement[10];
            foreach(var design in new[]{new[]{left-1,middleV},new[]{right+1,middleV},new[]{middleU,bottom-1},new[]{middleU,top+1}})
            {
                FoundryMap.ToFloor(design[0],design[1],out float hx,out float hy);
                for(int wave=0;wave<4;wave++)
                {
                    FoundryMap.PlacePack(wave,10,hx,hy,.9f,entries);
                    for(int i=0;i<entries.Length;i++)
                    {
                        Assert.That(FoundryMap.Contains(entries[i].X,entries[i].Y,.6f),Is.True);
                        Assert.That(FoundryMap.Segment(hx,hy,entries[i].X,entries[i].Y,.6f),Is.True);
                        for(int j=0;j<i;j++)Assert.That(ArenaFloor.FloorDistanceSquared(entries[i].X,entries[i].Y,entries[j].X,entries[j].Y),Is.GreaterThanOrEqualTo(.9f*.9f));
                    }
                }
            }
        }

        [TestCase(FoundryMap.West+1f,FoundryMap.South+1f,FoundryMap.West+4f,FoundryMap.South+5f)]
        [TestCase(13f,FoundryMap.South+1f,9f,FoundryMap.South+5f)]
        [TestCase(FoundryMap.West+1f,17f,FoundryMap.West+3f,15f)]
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
            for(float s=FoundryMap.BridgeStart+1.3f;s<=FoundryMap.BridgeEnd+1;s+=.25f)
                foreach(float d in new[]{-FoundryMap.BridgeHalfWidth+1.3f,0,FoundryMap.BridgeHalfWidth-1.3f})
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
            Assert.That(u-v,Is.LessThanOrEqualTo(FoundryMap.BridgeHalfWidth-1.19f));
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
            FoundryMap.ToFloor(-23,-23,out x,out y);
            FoundryMap.ToFloor(0,0,out float tx,out float ty);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.True);
        }
        [Test] public void ExtendedBridgeStopsAtItsRealFarEnd()
        {
            FoundryMap.ToFloor(-31,-31,out float x,out float y);
            FoundryMap.ToFloor(-40,-40,out float tx,out float ty);
            Assert.That(FoundryMap.Contains(tx,ty,0),Is.False);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.False);
            FoundryMap.Move(ref x,ref y,tx-x,ty-y,.6f);
            FoundryMap.ToDesign(x,y,out float u,out float v);
            Assert.That(u+v,Is.GreaterThanOrEqualTo(FoundryMap.BridgeStart+1.19f));
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
        }
        [TestCase(-31f,-31f,0f,6f)]
        [TestCase(-31f,-31f,-27f,6f)]
        [TestCase(-31f,-31f,FoundryMap.GateLandingCentre,6f)]
        public void ExtendedBridgePursuitWorksInBothDirections(float u,float v,float goalU,float goalV)
        {
            AssertRoute(u,v,goalU,goalV);
            AssertRoute(goalU,goalV,u,v);
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
        [Test] public void ActualTenEnemyPackFollowsAWalkingHeroAcrossTheBridgeJoinAndForecourt()
        {
            const float spacing=.9f,clearance=.6f;
            FoundryMap.ToFloor(-25,-25,out float startX,out float startY);
            var hero=new HeroMotion(2.5f);hero.Place(startX,startY,ArenaGeometry.Foundry);
            var route=new[]{new[]{-18f,-18f},new[]{-10f,-15f},new[]{0f,-15f}};
            int waypoint=0;
            var entries=new EntryPlacement[10];
            FoundryMap.PlacePack(0,10,startX,startY,spacing,entries);
            var pack=new PackMotion(spacing,2f,ArenaGeometry.Foundry,startX,startY);
            var oldX=new float[10];var oldY=new float[10];
            for(int i=0;i<entries.Length;i++)
            {
                FoundryMap.ToDesign(entries[i].X,entries[i].Y,out _,out float v);
                Assert.That(v,Is.LessThan(FoundryMap.South-clearance),"Every actor starts on the exposed bridge.");
                pack.Add(new HealthState(50),entries[i],i<2?1.6f:2.2f,1.5f);
            }
            // Exercise both production movement classes on a real walking route.
            // PackMotion permits immortal actors to wait permanently behind an
            // occupied stopping point; this does not require every body to arrive.
            for(int frame=0;frame<3600;frame++)
            {
                for(int i=0;i<pack.Count;i++){oldX[i]=pack.XOf(i);oldY[i]=pack.YOf(i);}
                float oldHeroX=hero.X,oldHeroY=hero.Y;
                if(waypoint<route.Length)
                {
                    FoundryMap.ToFloor(route[waypoint][0],route[waypoint][1],out float tx,out float ty);
                    float dx=tx-hero.X,dy=ty-hero.Y,length=(float)System.Math.Sqrt(dx*dx+dy*dy);
                    if(length<.08f)waypoint++;
                    else hero.Move(dx/length,dy/length,1f/60,ArenaGeometry.Foundry);
                }
                Assert.That(FoundryMap.Segment(oldHeroX,oldHeroY,hero.X,hero.Y,clearance),Is.True);
                pack.Step(1f/60,hero.X,hero.Y);
                for(int i=0;i<pack.Count;i++)
                {
                    float x=pack.XOf(i),y=pack.YOf(i);
                    Assert.That(FoundryMap.Contains(x,y,clearance),Is.True,$"Actor {i}, frame {frame}.");
                    Assert.That(FoundryMap.Segment(oldX[i],oldY[i],x,y,clearance),Is.True,
                        "No actor may cut across the outside corner while the pack turns off the bridge.");
                    for(int j=0;j<i;j++)Assert.That(ArenaFloor.FloorDistanceSquared(x,y,pack.XOf(j),pack.YOf(j)),
                        Is.GreaterThanOrEqualTo(spacing*spacing),"The physical join cannot defeat two-sided spacing.");
                }
            }
            Assert.That(waypoint,Is.EqualTo(route.Length),"The hero really walked both joins and the new southern strip.");
            int arrived=0;
            for(int i=0;i<pack.Count;i++)
            {
                FoundryMap.ToDesign(pack.XOf(i),pack.YOf(i),out _,out float v);
                Assert.That(v,Is.GreaterThan(FoundryMap.South+clearance),"No actor remains wedged at the bridge mouth.");
                Assert.That(ArenaFloor.FloorDistanceSquared(pack.XOf(i),pack.YOf(i),hero.X,hero.Y),Is.LessThan(9),
                    "Every actor reaches the nearby crowd, even when an occupied attack point makes it wait.");
                if(pack.HasArrived(i))arrived++;
            }
            Assert.That(arrived,Is.GreaterThan(0),"The crowd must reach attack range, not merely collect beyond the seam.");
        }
        [TestCase(.5f)]
        [TestCase(.61f)]
        [TestCase(.99f)]
        [TestCase(.01f)]
        public void EveryInteriorCellCanSpawnTenWithoutOverlapOrVoid(float offset)
        {
            var points=new EntryPlacement[10];
            // Include the diagonal bridge and every authored room, including the widened workyard.
            int bridgeMin=(int)System.Math.Floor((FoundryMap.BridgeStart-FoundryMap.BridgeHalfWidth)*.5f);
            int bridgeMax=(int)System.Math.Ceiling((FoundryMap.BridgeEnd+FoundryMap.BridgeHalfWidth)*.5f);
            int minU=bridgeMin,minV=bridgeMin,maxU=bridgeMax,maxV=bridgeMax;
            for(int room=0;room<FoundryMap.RectangleCount;room++)
            {
                minU=System.Math.Min(minU,FoundryMap.Bound(room,0));
                minV=System.Math.Min(minV,FoundryMap.Bound(room,1));
                maxU=System.Math.Max(maxU,FoundryMap.Bound(room,2));
                maxV=System.Math.Max(maxV,FoundryMap.Bound(room,3));
            }
            for(int u=minU;u<maxU;u++)for(int v=minV;v<maxV;v++)
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
        [TestCase(-27f,0f)]
        [TestCase(-27f,6f)]
        [TestCase(-26f,12f)]
        [TestCase(-23f,-13f)]
        [TestCase(-23f,17f)]
        public void ExpandedWestWorkyardKeepsFullHeroClearance(float u,float v)
        {
            FoundryMap.ToFloor(u,v,out float x,out float y);
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
        }
        [TestCase(-27f,6f,0f,6f)]
        [TestCase(-27f,6f,FoundryMap.GateLandingCentre,6f)]
        [TestCase(-27f,6f,-23f,-23f)]
        [TestCase(-26f,16f,-2f,16f)]
        [TestCase(-27f,-9f,11f,15f)]
        public void ExpandedWestPursuitReachesBothEnds(float u,float v,float goalU,float goalV)
        {
            AssertRoute(u,v,goalU,goalV);
            AssertRoute(goalU,goalV,u,v);
        }
        [Test] public void RebalancedCourtGivesTheForecourtDepthAndPreservesFixedLandmarks()
        {
            Assert.That(FoundryMap.East-FoundryMap.West,Is.EqualTo(46));
            Assert.That(FoundryMap.North-FoundryMap.South,Is.EqualTo(36));
            Assert.That(FoundryMap.PitWest-FoundryMap.West,Is.EqualTo(16));
            Assert.That(FoundryMap.PitSouth-FoundryMap.South,Is.EqualTo(20),"Space is reassigned to the forecourt, not simulated by camera zoom.");
            foreach(var design in new[]{new[]{-30f,0f},new[]{-30f,12f},new[]{-30f,-9f},
                new[]{-24f,-16f},new[]{-10f,-16f},new[]{0f,-16f},new[]{8f,-15f}})
            {
                FoundryMap.ToFloor(design[0],design[1],out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,.6f),Is.True,"Retained yard and new forecourt have actor clearance.");
                Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
                FoundryMap.ToFloor(design[0]+.3f,design[1]+.2f,out float tx,out float ty);
                Assert.That(FoundryMap.Move(ref x,ref y,tx-x,ty-y,.6f),Is.True);
                Assert.That(ArenaFloor.FloorDistanceSquared(x,y,tx,ty),Is.LessThan(.00001f));
            }
            Assert.That(new[]{FoundryMap.PitWest,FoundryMap.PitSouth,FoundryMap.PitEast,FoundryMap.PitNorth},Is.EqualTo(new[]{-16,2,-4,14}));
            Assert.That(new[]{FoundryMap.BridgeStart,FoundryMap.BridgeEnd,FoundryMap.BridgeHalfWidth},Is.EqualTo(new[]{-66,-22,5}));
            Assert.That(new[]{FoundryMap.Bound(5,0),FoundryMap.Bound(5,1),FoundryMap.Bound(5,2),FoundryMap.Bound(5,3)},Is.EqualTo(new[]{24,-2,28,12}));
        }
        [TestCase(-30f,0f)]
        [TestCase(-30f,12f)]
        [TestCase(-30f,-9f)]
        [TestCase(-24f,-16f)]
        [TestCase(-10f,-16f)]
        [TestCase(0f,-16f)]
        [TestCase(8f,-15f)]
        public void RebalancedYardAndForecourtSupportFullPacksAndBothWayPursuit(float u,float v)
        {
            var entries=new EntryPlacement[10];
            FoundryMap.ToFloor(u,v,out float hx,out float hy);
            for(int wave=0;wave<4;wave++)
            {
                FoundryMap.PlacePack(wave,10,hx,hy,.9f,entries);
                for(int i=0;i<entries.Length;i++)
                {
                    Assert.That(FoundryMap.Contains(entries[i].X,entries[i].Y,.6f),Is.True);
                    Assert.That(FoundryMap.Segment(hx,hy,entries[i].X,entries[i].Y,.6f),Is.True);
                    for(int j=0;j<i;j++)Assert.That(ArenaFloor.FloorDistanceSquared(entries[i].X,entries[i].Y,entries[j].X,entries[j].Y),Is.GreaterThanOrEqualTo(.9f*.9f));
                }
            }
            foreach(var goal in new[]{new[]{0f,6f},new[]{FoundryMap.GateLandingCentre,6f},new[]{-23f,-23f},new[]{-18f,16f}})
            {
                AssertRoute(u,v,goal[0],goal[1]);
                AssertRoute(goal[0],goal[1],u,v);
            }
        }
        [TestCase(0f)]
        [TestCase(12f)]
        [TestCase(-9f)]
        public void ReassignedWesternStripIsVoidAndLongMovementStopsAtTheNewRim(float v)
        {
            // These points were floor before the four-unit redistribution.
            FoundryMap.ToFloor(-34,v,out float tx,out float ty);
            Assert.That(FoundryMap.Cell(-34,(int)v),Is.False);
            Assert.That(FoundryMap.SurfaceContains(tx,ty,0),Is.False);
            Assert.That(FoundryMap.Contains(tx,ty,0),Is.False);
            FoundryMap.ToFloor(-30,v,out float x,out float y);
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.False);
            FoundryMap.Move(ref x,ref y,tx-x,ty-y,.6f);
            FoundryMap.ToDesign(x,y,out float stoppedU,out float stoppedV);
            Assert.That(stoppedU,Is.GreaterThanOrEqualTo(FoundryMap.West+.599f));
            Assert.That(stoppedV,Is.EqualTo(v).Within(.0001f));
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
        }
        [TestCase(-10f)]
        [TestCase(0f)]
        [TestCase(6f)]
        public void LongMovementUsesTheNewForecourtRimWithoutLeakingIntoTheVoid(float u)
        {
            FoundryMap.ToFloor(u,-16,out float x,out float y);
            FoundryMap.ToFloor(u,-22,out float tx,out float ty);
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
            Assert.That(FoundryMap.SurfaceContains(tx,ty,0),Is.False);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.False);
            FoundryMap.Move(ref x,ref y,tx-x,ty-y,.6f);
            FoundryMap.ToDesign(x,y,out float stoppedU,out float stoppedV);
            Assert.That(stoppedU,Is.EqualTo(u).Within(.0001f));
            Assert.That(stoppedV,Is.InRange(-17.401f,-17.2f),"Movement reaches the new rim instead of the old -14 boundary.");
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
        }
        [TestCase(-24f,-16f)]
        [TestCase(-10f,-16f)]
        public void BridgeHeadJoinsBothNewForecourtShouldersWithoutAHiddenBarrier(float u,float v)
        {
            FoundryMap.ToFloor(-18,-18,out float x,out float y);
            FoundryMap.ToFloor(u,v,out float tx,out float ty);
            Assert.That(FoundryMap.Contains(x,y,.6f)&&FoundryMap.Contains(tx,ty,.6f),Is.True);
            Assert.That(FoundryMap.Segment(x,y,tx,ty,.6f),Is.True);
            Assert.That(FoundryMap.Segment(tx,ty,x,y,.6f),Is.True);
            FoundryMap.Move(ref x,ref y,tx-x,ty-y,.6f);
            Assert.That(ArenaFloor.FloorDistanceSquared(x,y,tx,ty),Is.LessThan(.00001f));
            FoundryMap.ToFloor(-18,-18,out tx,out ty);
            FoundryMap.Move(ref x,ref y,tx-x,ty-y,.6f);
            Assert.That(ArenaFloor.FloorDistanceSquared(x,y,tx,ty),Is.LessThan(.00001f));
        }
        [Test] public void CompactGateRetainsItsLandingBesideTheRebalancedCourt()
        {
            Assert.That(new[]{FoundryMap.West,FoundryMap.East,FoundryMap.South,FoundryMap.North},Is.EqualTo(new[]{-32,14,-18,18}));
            Assert.That(new[]{FoundryMap.Bound(4,0),FoundryMap.Bound(4,2)},Is.EqualTo(new[]{14,24}));
            // The landing retains its complete transverse width but now ends at28.
            foreach(var p in new[]{new[]{26f,-1f},new[]{26f,11f},new[]{27f,5f}})
            {
                FoundryMap.ToFloor(p[0],p[1],out float x,out float y);
                Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
                AssertRoute(-20,6,p[0],p[1]);AssertRoute(p[0],p[1],-20,6);
            }
            foreach(var p in new[]{new[]{29f,5f},new[]{31f,-1f},new[]{31f,11f},new[]{23f,11f}})
            {
                FoundryMap.ToFloor(p[0],p[1],out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.False,"Old eastern floor is truly gone; only the shifted landing is wider.");
            }
            FoundryMap.ToFloor(27,5,out float hx,out float hy);
            FoundryMap.ToFloor(31,5,out float tx,out float ty);
            Assert.That(FoundryMap.Segment(hx,hy,tx,ty,.6f),Is.False);
            FoundryMap.Move(ref hx,ref hy,tx-hx,ty-hy,.6f);
            Assert.That(FoundryMap.Contains(hx,hy,.6f),Is.True,"Moving toward the old gate cannot walk into the void.");
        }
        [TestCase(26f,-1f)]
        [TestCase(26f,11f)]
        [TestCase(27f,5f)]
        public void CompactLandingRetainsSeparatedTenEnemySpawns(float u,float v)
        {
            var entries=new EntryPlacement[10];FoundryMap.ToFloor(u,v,out float hx,out float hy);
            for(int wave=0;wave<4;wave++)
            {
                FoundryMap.PlacePack(wave,10,hx,hy,.9f,entries);
                for(int i=0;i<entries.Length;i++)
                {
                    Assert.That(FoundryMap.Contains(entries[i].X,entries[i].Y,.6f),Is.True);
                    Assert.That(FoundryMap.Segment(hx,hy,entries[i].X,entries[i].Y,.6f),Is.True);
                    for(int j=0;j<i;j++)Assert.That(ArenaFloor.FloorDistanceSquared(entries[i].X,entries[i].Y,entries[j].X,entries[j].Y),Is.GreaterThanOrEqualTo(.9f*.9f));
                }
            }
        }
        private static void AssertRoute(float u,float v,float goalU,float goalV)
        {
            FoundryMap.ToFloor(u,v,out float x,out float y);
            FoundryMap.ToFloor(goalU,goalV,out float hx,out float hy);
            Assert.That(FoundryMap.Contains(x,y,.6f),Is.True);
            Assert.That(FoundryMap.Contains(hx,hy,.6f),Is.True);
            var route=new FoundryPursuit();
            for(int tick=0;tick<6000&&ArenaFloor.FloorDistanceSquared(x,y,hx,hy)>.04f;tick++)
            {
                float tx=hx,ty=hy;route.Target(x,y,hx,hy,ref tx,ref ty);
                float dx=tx-x,dy=ty-y;float length=(float)System.Math.Sqrt(dx*dx+dy*dy);
                float oldX=x,oldY=y;
                if(length>0)FoundryMap.Move(ref x,ref y,dx/length*System.Math.Min(.08f,length),dy/length*System.Math.Min(.08f,length),.6f);
                Assert.That(FoundryMap.Segment(oldX,oldY,x,y,.6f),Is.True);
            }
            Assert.That(ArenaFloor.FloorDistanceSquared(x,y,hx,hy),Is.LessThanOrEqualTo(.04f),$"Route {u},{v} -> {goalU},{goalV}");
        }
        [Test] public void PursuitRoutesAroundHoleAndThroughPassage()
        {
            foreach(var goal in new[]{new[]{0f,6f},new[]{FoundryMap.GateLandingCentre,6f},new[]{-23f,-23f},
                new[]{-21f,15f},new[]{11f,15f},new[]{FoundryMap.GateLandingCentre,-1f},new[]{FoundryMap.GateLandingCentre,11f}})
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
    
        // A boss that walks out of the gate reaches a hero anywhere on the court by the same pursuit as every enemy.
        [Test] public void GateBossRoutesFromTheLandingToTheCourt()
        {
            var placements=new EntryPlacement[1];FoundryMap.PlaceAtGate(1,placements);
            foreach(var goal in new[]{new[]{0f,0f},new[]{-3f,-5f},new[]{-20f,6f},new[]{-23f,-23f}})
            {
                float x=placements[0].X,y=placements[0].Y;
                FoundryMap.ToFloor(goal[0],goal[1],out float hx,out float hy);
                var route=new FoundryPursuit();
                for(int tick=0;tick<8000&&ArenaFloor.FloorDistanceSquared(x,y,hx,hy)>.04f;tick++)
                {
                    float tx=hx,ty=hy;route.Target(x,y,hx,hy,ref tx,ref ty);
                    float dx=tx-x,dy=ty-y;float length=(float)System.Math.Sqrt(dx*dx+dy*dy);
                    if(length>0)FoundryMap.Move(ref x,ref y,dx/length*System.Math.Min(.08f,length),dy/length*System.Math.Min(.08f,length),.6f);
                }
                Assert.That(ArenaFloor.FloorDistanceSquared(x,y,hx,hy),Is.LessThanOrEqualTo(.04f),$"The gate boss must reach a hero at ({goal[0]},{goal[1]}).");
            }
        }

        // The boss walks out of the gate onto its landing and the opened gate is held from the landing's centre line: every
        // spot is open walking floor the hero and the pack can reach, in front of the door, with the boss nearer the door.
        [Test] public void GateBossAndOpenedGateExitStandOnTheLanding()
        {
            FoundryMap.GateExitSpot(out float ex,out float ey);
            Assert.That(FoundryMap.Contains(ex,ey,HeroMotion.EdgeMargin+.02f),Is.True,"The hero can stand on the gate exit.");
            FoundryMap.ToDesign(ex,ey,out float eu,out float ev);
            Assert.That(eu,Is.InRange((float)FoundryMap.GateLandingStart,(float)FoundryMap.GateLandingEnd));
            Assert.That(ev,Is.EqualTo(FoundryMap.GateExitV).Within(.001f),"The exit stands on the door's axis.");
            var placements=new EntryPlacement[5];
            FoundryMap.PlaceAtGate(placements.Length,placements);
            for(int i=0;i<placements.Length;i++)
            {
                Assert.That(FoundryMap.Contains(placements[i].X,placements[i].Y,HeroMotion.EdgeMargin+.02f),Is.True,$"Gate arrival {i} is open floor.");
                FoundryMap.ToDesign(placements[i].X,placements[i].Y,out float u,out _);
                Assert.That(u,Is.GreaterThan(eu).And.LessThan(FoundryMap.GateLandingEnd),"The pack stands between the exit and the door.");
                Assert.That(FoundryMap.Segment(placements[i].X,placements[i].Y,ex,ey,HeroMotion.EdgeMargin),Is.True,"It can walk off the landing.");
                for(int j=0;j<i;j++)
                    Assert.That(ArenaFloor.FloorDistanceSquared(placements[i].X,placements[i].Y,placements[j].X,placements[j].Y),
                        Is.GreaterThanOrEqualTo(.9f*.9f),"Arrivals keep body spacing.");
            }
            FoundryMap.ToDesign(placements[0].X,placements[0].Y,out _,out float bv);
            Assert.That(bv,Is.EqualTo(ev).Within(.001f),"The boss walks out on the door's axis.");
        }
    }
}
