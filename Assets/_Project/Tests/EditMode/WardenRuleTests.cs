using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class WardenRuleTests
    {
        [Test] public void TheMarkComesATelegraphBeforeEachBlow()
        {
            var clock=new WardenSlamClock();
            Assert.That(clock.Advance(WardenRule.SlamEvery-WardenRule.Telegraph-.01f,false),Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.IsMarked,Is.False);
            Assert.That(clock.Advance(.02f,false),Is.EqualTo(WardenSlamClock.Step.Marked),"Then the mark.");
            Assert.That(clock.IsMarked,Is.True);Assert.That(clock.TelegraphProgress,Is.Zero);
            Assert.That(clock.Advance(WardenRule.Telegraph*.5f,false),Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.TelegraphProgress,Is.EqualTo(.5f).Within(.01f),"Halfway through the telegraph.");
            Assert.That(clock.Advance(WardenRule.Telegraph*.6f,false),Is.EqualTo(WardenSlamClock.Step.Slammed),"Then the blow.");
            Assert.That(clock.IsMarked,Is.False);Assert.That(clock.Seconds,Is.Zero,"And round again from the blow.");
        }

        [Test] public void EnragedTheBlowsComeFasterBelowHalfHealth()
        {
            var calm=new WardenSlamClock();var enraged=new WardenSlamClock();
            int calmBlows=0,enragedBlows=0;
            for(int f=0;f<60*60;f++)
            {
                if(calm.Advance(1f/60f,false)==WardenSlamClock.Step.Slammed)calmBlows++;
                if(enraged.Advance(1f/60f,true)==WardenSlamClock.Step.Slammed)enragedBlows++;
            }
            Assert.That(calmBlows,Is.EqualTo((int)(60f/WardenRule.SlamEvery)).Within(1));
            Assert.That(enragedBlows,Is.EqualTo((int)(60f/WardenRule.SlamEveryEnraged)).Within(1));
            Assert.That(WardenRule.IsEnraged(150,300),Is.True);Assert.That(WardenRule.IsEnraged(151,300),Is.False);
            Assert.That(WardenRule.Telegraph,Is.InRange(.8f,1.2f),"The report's readable telegraph on a phone.");
            Assert.That(()=>calm.Advance(-1f,false),Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }
    }
}
