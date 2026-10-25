using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class AltarFieldTests
    {
        private static AltarField Field()=>new AltarField(new[]{0f,10f},new[]{0f,0f});

        [Test] public void StayingOnItForTheDwellTakesTheAltarOnceEvenWhileWalking()
        {
            var field=Field();
            float half=AltarRule.DwellSeconds*.5f;
            Assert.That(field.Step(0,0,half),Is.EqualTo(-1));
            Assert.That(field.Standing,Is.EqualTo(0));Assert.That(field.Held,Is.EqualTo(half).Within(1e-6f));
            Assert.That(field.Step(.3f,.2f,half),Is.EqualTo(0),"Walking on it still counts: the dwell takes it.");
            Assert.That(field.IsUsed(0),Is.True);
            for(int i=0;i<10;i++)Assert.That(field.Step(0,0,half),Is.EqualTo(-1),"A taken altar answers no more.");
            Assert.That(field.Standing,Is.EqualTo(-1));
        }

        [Test] public void SteppingOffStartsTheHoldAgain()
        {
            var field=Field();
            field.Step(0,0,AltarRule.DwellSeconds*.9f);
            field.Step(5,0,.05f);Assert.That(field.Standing,Is.EqualTo(-1));Assert.That(field.Held,Is.Zero);
            field.Step(0,0,AltarRule.DwellSeconds*.5f);Assert.That(field.Held,Is.EqualTo(AltarRule.DwellSeconds*.5f).Within(1e-6f),"Stepping off and back starts over.");
            Assert.That(new AltarField(new[]{0f},new[]{0f}).Step(AltarRule.Radius+.05f,0,2f),Is.EqualTo(-1),"Beyond the radius nothing counts.");
        }

        [Test] public void ANewFloorRestoresEveryAltar()
        {
            var field=Field();
            field.Step(0,0,1f);field.Step(10,0,1f);
            Assert.That(field.IsUsed(0)&&field.IsUsed(1),Is.True);
            field.Reset();
            Assert.That(field.IsUsed(0)||field.IsUsed(1),Is.False);
            Assert.That(field.Step(10,0,1f),Is.EqualTo(1));
        }

        [Test] public void TheBargainIsHalfAnEvolutionAndTheAltarsStandOnOpenFloor()
        {
            Assert.That(AltarRule.DifficultyCost,Is.EqualTo(Cryptforge.Progression.EvolutionRule.DifficultyPerStack*.5f).Within(1e-6f));
            Assert.That(AltarRule.DwellSeconds,Is.LessThan(.5f),"Stepping in is enough; no long stand.");
            Assert.That(FoundryMap.AltarCount,Is.EqualTo(2));
            for(int i=0;i<FoundryMap.AltarCount;i++)
            {
                FoundryMap.AltarSite(i,out float x,out float y);
                Assert.That(FoundryMap.Contains(x,y,HeroMotion.EdgeMargin),Is.True,$"Altar {i} stands on walkable floor.");
                for(int r=1;r<=FoundryMap.RoomSiteCount;r++)
                {
                    FoundryMap.RoomSite(0,r,out float rx,out float ry);
                    Assert.That(ArenaFloor.FloorDistanceSquared(x,y,rx,ry),Is.GreaterThan(4f*4f),"Clear of every hall's district.");
                }
            }
        }
    }
}
