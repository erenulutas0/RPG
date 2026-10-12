using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;
namespace Cryptforge.Tests
{
    public sealed class ImmortalCrowdDiagnostic
    {
        [TestCase(-10f,-12f)] [TestCase(0f,0f)] [TestCase(0f,-12f)] [TestCase(-10f,-15f)] public void ActualTenEnemyPackCrossesTheBridgeJoinIntoTheNewForecourt(float goalU,float goalV)
        {
            const float spacing=.9f,clearance=.6f;
            FoundryMap.ToFloor(-25,-25,out float startX,out float startY);
            FoundryMap.ToFloor(goalU,goalV,out float hx,out float hy); Assert.That(FoundryMap.Contains(hx,hy,.6f),Is.True,"Diagnostic target must be valid on this layout.");
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
            // Exercise the actual shared two-sided pack solver, including its
            // pursuit steering and congestion, rather than ten independent routes.
            for(int frame=0;frame<3600;frame++)
            {
                for(int i=0;i<pack.Count;i++){oldX[i]=pack.XOf(i);oldY[i]=pack.YOf(i);}
                pack.Step(1f/60,hx,hy);
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
            int arrived=0;
            for(int i=0;i<pack.Count;i++)
            {
                FoundryMap.ToDesign(pack.XOf(i),pack.YOf(i),out float u,out float v);
                TestContext.WriteLine($"Final actor {i}: design=({u:R},{v:R}), arrived={pack.HasArrived(i)}, stalled={pack.IsStalled(i)}, distanceSquared={ArenaFloor.FloorDistanceSquared(pack.XOf(i),pack.YOf(i),hx,hy):R}");
            }
            for(int i=0;i<pack.Count;i++)
            {
                FoundryMap.ToDesign(pack.XOf(i),pack.YOf(i),out _,out float v);
                Assert.That(v,Is.GreaterThan(FoundryMap.South+clearance),"No actor remains wedged at the bridge mouth.");
                Assert.That(ArenaFloor.FloorDistanceSquared(pack.XOf(i),pack.YOf(i),hx,hy),Is.LessThan(9),
                    "Every actor reaches the nearby crowd, even when an occupied attack point makes it wait.");
                if(pack.HasArrived(i))arrived++;
            }
            Assert.That(arrived,Is.GreaterThan(0),"The crowd must reach attack range, not merely collect beyond the seam.");
        }
    }
}
