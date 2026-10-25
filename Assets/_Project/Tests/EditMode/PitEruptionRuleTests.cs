using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class PitEruptionRuleTests
    {
        private static bool Band(float u,float v){FoundryMap.ToFloor(u,v,out float x,out float y);return PitEruptionRule.InBand(x,y);}

        [Test] public void TheBandRunsRoundThePitsEdgeAndNoFurther()
        {
            Assert.That(Band(FoundryMap.PitWest-1f,8),Is.True,"A unit off the west edge burns.");
            Assert.That(Band(-10,FoundryMap.PitSouth-2.9f),Is.True);
            Assert.That(Band(-10,FoundryMap.PitSouth-3.1f),Is.False,"Past the reach is safe.");
            Assert.That(Band(FoundryMap.PitEast+2f,FoundryMap.PitNorth+2f),Is.True,"The corners round off: 2.83 from the corner.");
            Assert.That(Band(FoundryMap.PitEast+2.2f,FoundryMap.PitNorth+2.2f),Is.False,"3.11 from the corner.");
            // A pack standing round a hero on the rim stands in the band too: a hero 1.3 out, an enemy its reach beyond.
            Assert.That(Band(FoundryMap.PitWest-1.3f-1.5f,8),Is.True);
            FoundryMap.RoomSite(0,2,out float hx,out float hy);
            Assert.That(PitEruptionRule.InBand(hx,hy),Is.False,"The pit-rim hall opens just outside it, so its fight starts safe.");
            Assert.That(Band(0,0),Is.False,"The court's middle is safe.");
            // The hero can stand in the band: walkable floor exists between the edge margin and the reach.
            FoundryMap.ToFloor(FoundryMap.PitWest-1.3f,8,out float x,out float y);
            Assert.That(FoundryMap.Contains(x,y,HeroMotion.EdgeMargin),Is.True);
        }

        [Test] public void TheClockWarnsThenSpillsEveryPeriod()
        {
            var clock=new PitEruptionClock();
            Assert.That(clock.Step(PitEruptionRule.Period-PitEruptionRule.Warning-.01f),Is.False);
            Assert.That(clock.IsWarning,Is.False);Assert.That(clock.WarningProgress,Is.Zero);
            Assert.That(clock.Step(.02f),Is.False);Assert.That(clock.IsWarning,Is.True,"The warning comes first.");
            Assert.That(clock.Step(PitEruptionRule.Warning),Is.True,"Then the spill.");
            Assert.That(clock.IsWarning,Is.False,"And the clock goes round again.");
            clock.Step(5f);clock.Reset();Assert.That(clock.Seconds,Is.Zero);
            Assert.That(PitEruptionRule.EnemyShare,Is.GreaterThan(PitEruptionRule.HeroShare*3),"The pit is a weapon against packs, a sting to the hero.");
        }
    }
}
