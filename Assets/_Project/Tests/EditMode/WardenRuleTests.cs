using Cryptforge.Combat;
using Cryptforge.Progression;
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

        // The Sword is the Warden's weapon (docs/41, status 2026-11-10): what it lands on the foundry's gate boss deals half
        // again, and nothing else moves - not its landings on anything else, not another weapon's on the gate boss.
        [Test] public void TheSwordDealsTheGateBossHalfAgainAndEveryOtherWeaponItsOwn()
        {
            Assert.That(WardenRule.SwordId,Is.EqualTo("weapon_sword"),"The Sword's WeaponDefinition id.");
            Assert.That(WardenRule.SwordShare,Is.EqualTo(1.5f));
            Assert.That(WardenRule.GateBossShareOf(WardenRule.SwordId),Is.EqualTo(WardenRule.SwordShare));
            Assert.That(WardenRule.GateBossShareOf("weapon_staff"),Is.EqualTo(1f));
            Assert.That(WardenRule.GateBossShareOf("weapon_daggers"),Is.EqualTo(1f));
            Assert.That(WardenRule.GateBossShareOf(null),Is.EqualTo(1f));
            Assert.That(WardenRule.DamageOn(18f,WardenRule.SwordShare,true),Is.EqualTo(27f),"The Sword on the gate boss.");
            Assert.That(WardenRule.DamageOn(18f,WardenRule.SwordShare,false),Is.EqualTo(18f),"The Sword on anything else.");
            Assert.That(WardenRule.DamageOn(24f,WardenRule.GateBossShareOf("weapon_staff"),true),Is.EqualTo(24f),"The Staff on the gate boss.");
            Assert.That(WardenRule.DamageOn(7f,WardenRule.GateBossShareOf("weapon_daggers"),true),Is.EqualTo(7f),"The Daggers on the gate boss.");
        }

        // The same rule through the strike the scene's AttackController and the foundry simulation both fire
        // (WeaponRuntime.TryAttack), with the share the loadout hands each weapon by its id.
        [Test] public void ThroughTheStrikeOnlyTheSwordsLandingsOnTheGateBossTakeTheShare()
        {
            var loadout=new WeaponLoadout(7);
            WeaponRuntime sword=Held(loadout,WardenRule.SwordId,new AttackPattern(WeaponBehavior.Cleave,2f,.5f));
            WeaponRuntime staff=Held(loadout,"weapon_staff",new AttackPattern(WeaponBehavior.Area,3.5f,.5f));
            WeaponRuntime daggers=Held(loadout,"weapon_daggers",new AttackPattern(WeaponBehavior.DirectHit));
            Assert.That(sword.GateBossShare,Is.EqualTo(WardenRule.SwordShare));
            Assert.That(staff.GateBossShare,Is.EqualTo(1f));Assert.That(daggers.GateBossShare,Is.EqualTo(1f));

            // The Sword strikes the gate boss and cleaves the elite beside it, then strikes the elite and cleaves the boss.
            var boss=new HealthState(1000f);var elite=new HealthState(1000f);
            Assert.That(sword.TryAttack(boss,null,new IDamageable[]{boss,elite},gateBoss:boss),Is.True);
            Assert.That(Lost(boss),Is.EqualTo(15f),"Its strike on the gate boss, half again.");
            Assert.That(Lost(elite),Is.EqualTo(5f),"Its cleave on an elite, as authored.");
            sword.Tick(1f);
            Assert.That(sword.TryAttack(elite,null,new IDamageable[]{elite,boss},gateBoss:boss),Is.True);
            Assert.That(Lost(elite),Is.EqualTo(15f),"Its strike on an elite with the gate boss standing, as authored.");
            Assert.That(Lost(boss),Is.EqualTo(22.5f),"Its cleave on the gate boss, half again.");

            // Its procs too: the fourth strike's pulse round the hero reaches the gate boss and the elite.
            sword.AddModifier(UpgradeStat.PulseDamage,new StatModifier(ModifierOperation.Flat,.5f));
            for(int i=0;i<2;i++){sword.Tick(1f);sword.TryAttack(elite,null,null,new IDamageable[]{boss,elite},gateBoss:boss);}
            Assert.That(sword.PulsesFired,Is.EqualTo(1));
            Assert.That(Lost(boss),Is.EqualTo(22.5f+7.5f),"The pulse on the gate boss, half again.");
            Assert.That(Lost(elite),Is.EqualTo(15f+20f+5f),"The pulse on an elite, as authored.");

            // The Staff and the Daggers deal the gate boss their own.
            var other=new HealthState(1000f);
            Assert.That(staff.TryAttack(other,null,new IDamageable[]{other},gateBoss:other),Is.True);
            Assert.That(Lost(other),Is.EqualTo(10f),"The Staff on the gate boss, as authored.");
            Assert.That(daggers.TryAttack(other,null,null,gateBoss:other),Is.True);
            Assert.That(Lost(other),Is.EqualTo(20f),"The Daggers on the gate boss, as authored.");
        }

        // Where no gate boss stands - the hall arena, the Descent, the foundry's halls - and on a weapon no loadout holds
        // (every enemy's, the hall arena's hero), the Sword deals what it is authored to.
        [Test] public void WithoutAGateBossOrALoadoutTheSwordDealsItsOwn()
        {
            WeaponRuntime sword=Held(new WeaponLoadout(7),WardenRule.SwordId,new AttackPattern(WeaponBehavior.Cleave,2f,.5f));
            var boss=new HealthState(1000f);
            Assert.That(sword.TryAttack(boss),Is.True);
            Assert.That(Lost(boss),Is.EqualTo(10f),"No gate boss handed to the strike.");

            var unheld=new WeaponRuntime(10f,.8f,2.1f,0f,new AttackPattern(WeaponBehavior.Cleave,2f,.5f));
            Assert.That(unheld.GateBossShare,Is.EqualTo(1f));
            var warden=new HealthState(1000f);
            Assert.That(unheld.TryAttack(warden,null,null,gateBoss:warden),Is.True);
            Assert.That(Lost(warden),Is.EqualTo(10f),"A Sword no loadout holds.");
            Assert.That(()=>unheld.GateBossShare=0f,Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        // A floor's own gate share (FloorDefinition.FoundryGateHealthShare): 0 takes GateHealthShare, as floor 1 does; a
        // share above 0 is the floor's, as floor 2's 8.5 / 1.4; NaN, a negative or an infinite share is refused.
        [Test] public void AFloorsOwnGateShareStandsInForTheRulesAndZeroKeepsTheRules()
        {
            Assert.That(WardenRule.GateHealthShareOn(0f),Is.EqualTo(WardenRule.GateHealthShare));
            Assert.That(WardenRule.GateHealthShareOn(6.071429f),Is.EqualTo(8.5f/1.4f),"floor 2's asset value is 8.5 / 1.4 as a float");
            Assert.That(WardenRule.GateHealthShareOn(12f),Is.EqualTo(12f));
            foreach(float share in new[]{float.NaN,-1f,-.01f,float.PositiveInfinity,float.NegativeInfinity})
            {
                Assert.That(WardenRule.IsFloorGateHealthShare(share),Is.False,share.ToString());
                Assert.That(()=>WardenRule.GateHealthShareOn(share),Throws.InstanceOf<System.ArgumentOutOfRangeException>(),share.ToString());
            }
            Assert.That(WardenRule.IsFloorGateHealthShare(0f),Is.True);
            Assert.That(WardenRule.IsFloorGateHealthShare(8.5f/1.4f),Is.True);
        }

        private static WeaponRuntime Held(WeaponLoadout loadout,string id,AttackPattern pattern)
        {
            var runtime=new WeaponRuntime(10f,.8f,2.1f,0f,pattern);
            Assert.That(loadout.TryAdd(id,runtime),Is.True,id);
            return runtime;
        }

        private static float Lost(HealthState health)=>health.Maximum-health.Current;

        // Frames of 1/60 s from now to the blow, in seconds; -1 if none lands within ten seconds.
        private static float SecondsToTheBlow(WardenSlamClock clock,bool enraged)
        {
            for(int frame=1;frame<=600;frame++)if(clock.Advance(1f/60f,enraged)==WardenSlamClock.Step.Slammed)return frame/60f;
            return -1f;
        }
    }
}
