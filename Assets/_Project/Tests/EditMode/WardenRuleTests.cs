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

        // The Warden swings nothing from the mark until its hammer is out of the floor again (docs/41 step 4's window).
        [Test] public void TheWeaponIsHeldFromTheMarkUntilTheHammerIsOutOfTheFloor()
        {
            var clock=new WardenSlamClock();
            Assert.That(clock.HoldsWeapon,Is.False,"Free before the first mark.");
            clock.Advance(WardenRule.SlamEvery-WardenRule.Telegraph-.01f,false);
            Assert.That(clock.HoldsWeapon,Is.False);
            Assert.That(clock.Advance(.02f,false),Is.EqualTo(WardenSlamClock.Step.Marked));
            Assert.That(clock.HoldsWeapon,Is.True,"Winding the hammer up.");
            Assert.That(clock.Advance(WardenRule.Telegraph+.01f,false),Is.EqualTo(WardenSlamClock.Step.Slammed));
            Assert.That(clock.HoldsWeapon,Is.True,"The hammer is in the floor.");
            clock.Advance(WardenRule.RecoverySeconds-.02f,false);
            Assert.That(clock.HoldsWeapon,Is.True);
            clock.Advance(.04f,false);
            Assert.That(clock.HoldsWeapon,Is.False,"Then it swings again until the next mark.");
            Assert.That(clock.HoldsWeaponFor(WardenRule.RecoverySeconds+1f),Is.True,"A longer recovery holds it longer.");
            Assert.That(WardenRule.RecoverySeconds,Is.LessThan(WardenRule.SlamEveryEnraged-WardenRule.Telegraph),
                "Even enraged it swings between one blow and the next mark.");
            clock.Reset();
            Assert.That(clock.HoldsWeapon,Is.False,"A new fight starts free.");
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

        // An enrage while a mark is down speeds only the marks after it: the blow still lands a whole telegraph after its
        // own mark. Read from the enraged pace, a calm mark (down at 4.9 s) was already past the enraged blow's 4 s, and
        // the hammer came down the frame after the enrage.
        [Test] public void AnEnrageTheFrameAfterACalmMarkLeavesTheWholeTelegraph()
        {
            var clock=new WardenSlamClock();
            Assert.That(clock.Advance(WardenRule.SlamEvery-WardenRule.Telegraph+.01f,false),Is.EqualTo(WardenSlamClock.Step.Marked),"A calm mark.");
            Assert.That(clock.Advance(1f/60f,true),Is.EqualTo(WardenSlamClock.Step.None),"Enraged a frame later, the hammer is still going up.");
            Assert.That(clock.IsMarked,Is.True);Assert.That(clock.HoldsWeapon,Is.True);
            Assert.That(clock.TelegraphProgress,Is.EqualTo(1f/60f/WardenRule.Telegraph).Within(1e-3f),"A frame into the telegraph.");
            Assert.That(1f/60f+SecondsToTheBlow(clock,true),Is.EqualTo(WardenRule.Telegraph).Within(1.01f/60f),
                "The blow lands a telegraph after its mark.");
            // Enraged from that blow on, the next mark comes sooner, and it too keeps its telegraph.
            Assert.That(clock.Advance(WardenRule.SlamEveryEnraged-WardenRule.Telegraph-.01f,true),Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.Advance(.02f,true),Is.EqualTo(WardenSlamClock.Step.Marked),"The next mark, SlamEveryEnraged after the blow.");
            Assert.That(SecondsToTheBlow(clock,true),Is.EqualTo(WardenRule.Telegraph).Within(1.01f/60f));
        }

        [Test] public void AnEnrageHalfwayThroughTheTelegraphDoesNotBringTheBlowEarly()
        {
            var clock=new WardenSlamClock();
            Assert.That(clock.Advance(WardenRule.SlamEvery-WardenRule.Telegraph+.01f,false),Is.EqualTo(WardenSlamClock.Step.Marked));
            Assert.That(clock.Advance(.5f,false),Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.Advance(1f/60f,true),Is.EqualTo(WardenSlamClock.Step.None),"Enraged 0.5 s into the telegraph, the hammer is still going up.");
            Assert.That(clock.TelegraphProgress,Is.EqualTo((.5f+1f/60f)/WardenRule.Telegraph).Within(1e-3f));
            Assert.That(.5f+1f/60f+SecondsToTheBlow(clock,true),Is.EqualTo(WardenRule.Telegraph).Within(1.01f/60f),
                "The blow lands a telegraph after its mark.");
        }

        // An enrage while the hammer is in the floor neither cuts the recovery short nor stretches it, and the first enraged
        // mark still comes SlamEveryEnraged less the telegraph after the blow (review 2026-11-04).
        [Test] public void AnEnrageDuringTheRecoveryKeepsItAndBringsOnlyTheNextMarkSooner()
        {
            var clock=new WardenSlamClock();
            Assert.That(clock.Advance(WardenRule.SlamEvery-WardenRule.Telegraph+.01f,false),Is.EqualTo(WardenSlamClock.Step.Marked));
            Assert.That(clock.Advance(WardenRule.Telegraph+.01f,false),Is.EqualTo(WardenSlamClock.Step.Slammed));
            Assert.That(clock.Advance(.5f,false),Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.Advance(.5f,true),Is.EqualTo(WardenSlamClock.Step.None),"Enraged a second after the blow.");
            Assert.That(clock.HoldsWeapon,Is.True,"Still in the floor.");
            Assert.That(clock.Advance(WardenRule.RecoverySeconds-1f-.02f,true),Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.HoldsWeapon,Is.True,"The enrage does not cut the recovery short.");
            Assert.That(clock.Advance(.03f,true),Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.HoldsWeapon,Is.False,"Out of the floor on time.");
            Assert.That(clock.Advance(WardenRule.SlamEveryEnraged-WardenRule.Telegraph-WardenRule.RecoverySeconds-.02f,true),
                Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.Advance(.02f,true),Is.EqualTo(WardenSlamClock.Step.Marked),"The enraged mark, on the enraged pace from the blow.");
        }

        // A Warden felled under its mark: WardenSlamService resets the clock as the gate boss goes, so no mark stays down,
        // nothing reads the weapon held, and the next gate boss (floor 2's) starts free (review 2026-11-04).
        [Test] public void AResetUnderAMarkOrInTheRecoveryLeavesNothingHeld()
        {
            var clock=new WardenSlamClock();
            clock.Advance(WardenRule.SlamEvery-WardenRule.Telegraph+.01f,false);
            Assert.That(clock.IsMarked,Is.True);
            clock.Reset();
            Assert.That(clock.IsMarked,Is.False);Assert.That(clock.HoldsWeapon,Is.False);Assert.That(clock.Seconds,Is.Zero);
            clock.Advance(WardenRule.SlamEvery-WardenRule.Telegraph+.01f,false);
            clock.Advance(WardenRule.Telegraph+.01f,false);
            Assert.That(clock.HoldsWeapon,Is.True,"In the recovery.");
            clock.Reset();
            Assert.That(clock.HoldsWeapon,Is.False);Assert.That(clock.Seconds,Is.Zero);
            Assert.That(clock.Advance(WardenRule.SlamEvery-WardenRule.Telegraph-.01f,false),Is.EqualTo(WardenSlamClock.Step.None),
                "The next fight's first mark comes on its own pace.");
            Assert.That(clock.HoldsWeapon,Is.False);
        }

        // Frames of 1/60 s from now to the blow, in seconds; -1 if none lands within ten seconds.
        private static float SecondsToTheBlow(WardenSlamClock clock,bool enraged)
        {
            for(int frame=1;frame<=600;frame++)if(clock.Advance(1f/60f,enraged)==WardenSlamClock.Step.Slammed)return frame/60f;
            return -1f;
        }
    }
}
