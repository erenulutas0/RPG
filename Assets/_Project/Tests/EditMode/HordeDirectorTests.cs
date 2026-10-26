using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The continuous horde's rules (docs/27, the owner's hybrid of 2026-10-26): each one something the scene and the
    // phone's frame budget rely on.
    public sealed class HordeDirectorTests
    {
        private const float Frame = 1f / 60f;

        private static int Fill(HordeDirector director, float deltaTime, int alive, int otherAlive, List<HordeDirector.Spawn> spawns)
        {
            director.Tick(deltaTime, alive, otherAlive, spawns);
            return spawns.Count;
        }

        [Test] public void TheStateFollowsTheNodesAndTheGateOverridesThem()
        {
            Assert.That(HordeRule.StateFor(0,false),Is.EqualTo(HordeState.Ramp));
            Assert.That(HordeRule.StateFor(1,false),Is.EqualTo(HordeState.Horde));
            Assert.That(HordeRule.StateFor(2,false),Is.EqualTo(HordeState.Pressure));
            Assert.That(HordeRule.StateFor(5,false),Is.EqualTo(HordeState.Pressure),"Past two it only presses.");
            Assert.That(HordeRule.StateFor(0,true),Is.EqualTo(HordeState.Boss),"The gate opening is the whole story.");
            Assert.That(()=>HordeRule.StateFor(-1,false),Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(HordeRule.TargetByState,Is.EqualTo(new[]{4,10,16,6}));
            Assert.That(HordeRule.TargetByState[(int)HordeState.Boss],Is.LessThan(HordeRule.TargetByState[(int)HordeState.Horde]),
                "The boss room thins the horde so the boss can be read.");
        }

        [Test] public void TheTargetRisesWithTheKillRatioAndNeverPassesTheCap()
        {
            Assert.That(HordeRule.EffectiveTarget(HordeState.Pressure,0,10,40),Is.EqualTo(16),"No kills: the plain target.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Pressure,5,10,40),Is.EqualTo(20),"Half the arrivals killed: 16 * 1.25.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Pressure,10,10,40),Is.EqualTo(24),"Killing as fast as they come: half again.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Pressure,50,10,40),Is.EqualTo(24),"The ratio clamps at one.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Pressure,10,10,20),Is.EqualTo(20),"Never above the cap.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Ramp,3,0,40),Is.EqualTo(6),"Kills with no spawns read against one.");
            Assert.That(()=>HordeRule.EffectiveTarget(HordeState.Ramp,0,0,0),Throws.InstanceOf<ArgumentOutOfRangeException>());

            var director = new HordeDirector(40,2,7);
            var spawns = new List<HordeDirector.Spawn>();
            director.SetState(HordeState.Pressure);
            Assert.That(director.TargetNow,Is.EqualTo(16));
            director.Tick(3f,0,0,spawns);
            Assert.That(director.SpawnsInWindow,Is.EqualTo(spawns.Count));
            for (int i = 0; i < spawns.Count; i++) director.Fell();
            Assert.That(director.KillsInWindow,Is.EqualTo(spawns.Count));
            Assert.That(director.TargetNow,Is.EqualTo(24),"Every arrival killed: the crowd aims half again higher.");
            var small = new HordeDirector(10,2,7);
            small.SetState(HordeState.Pressure);
            Assert.That(small.TargetNow,Is.EqualTo(10),"A ten-enemy floor aims for ten.");
        }

        [Test] public void NodeWaveEnemiesTakeCapSpaceButNotTargetSpace()
        {
            var director = new HordeDirector(10,1,1);
            var spawns = new List<HordeDirector.Spawn>();
            director.SetState(HordeState.Pressure);
            Assert.That(director.TargetNow,Is.EqualTo(10));
            Assert.That(Fill(director,2f,0,8,spawns),Is.EqualTo(2),"Eight hall enemies leave room for two.");
            Assert.That(Fill(director,2f,2,8,spawns),Is.Zero,"And then for none.");
            Assert.That(Fill(director,2f,2,0,spawns),Is.EqualTo(3),"The hall cleared: the credit cap's worth walks in.");

            var ramp = new HordeDirector(40,1,1);
            Assert.That(Fill(ramp,2f,3,30,spawns),Is.EqualTo(1),"Three of a four-target: one more, whatever the halls hold.");
            Assert.That(Fill(ramp,2f,4,0,spawns),Is.Zero,"At target nothing walks in and no credit grows.");
            Assert.That(Fill(ramp,2f,0,0,spawns),Is.EqualTo(3),"Dropping to nothing: at most the credit cap, never the deficit.");
            Assert.That(()=>ramp.Tick(float.NaN,0,0,spawns),Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(()=>ramp.Tick(-Frame,0,0,spawns),Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(()=>ramp.Tick(Frame,-1,0,spawns),Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(()=>ramp.Tick(Frame,0,0,null),Throws.InstanceOf<ArgumentNullException>());
            Assert.That(()=>new HordeDirector(0,1,1),Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(()=>new HordeDirector(1,0,1),Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test] public void AFrozenFrameOwesACappedCreditNotTheWholeStall()
        {
            var director = new HordeDirector(40,3,3);
            var spawns = new List<HordeDirector.Spawn>();
            director.SetState(HordeState.Pressure);
            director.Tick(10f,0,0,spawns);
            int most = (int)(HordeRule.CreditCapSeconds * HordeRule.MaxSpawnsPerSecond);
            Assert.That(spawns.Count,Is.EqualTo(most),"Ten seconds frozen: a second and a half of spawns, not twenty.");
            Assert.That(director.Spawned,Is.EqualTo(most));
            Assert.That(Fill(director,Frame,most,0,spawns),Is.Zero,"The credit was spent; a frame earns a thirtieth of a spawn.");
            Assert.That(director.Seconds,Is.EqualTo(10f + Frame).Within(1e-5f));
        }

        [Test] public void SpawnsKeepTheirGapAcrossFramesAndTheRateHolds()
        {
            var director = new HordeDirector(40,1,11);
            var spawns = new List<HordeDirector.Spawn>();
            director.SetState(HordeState.Pressure);
            var times = new List<float>();
            for (int f = 0; f < (int)(10f / Frame); f++)
            {
                director.Tick(Frame,0,0,spawns);
                Assert.That(spawns.Count,Is.LessThanOrEqualTo(1),"A normal frame never places two.");
                if (spawns.Count == 1) times.Add(director.Seconds);
            }
            for (int i = 1; i < times.Count; i++)
                Assert.That(times[i] - times[i-1],Is.GreaterThanOrEqualTo(HordeRule.MinSpawnGapSeconds - 1e-4f),"Never closer than the gap.");
            Assert.That(times.Count,Is.EqualTo(20).Within(1),"Two a second for ten seconds, the crowd never reported as standing.");

            // Credit banked while the crowd stood one short of target, then the crowd gone: the bank drains one spawn per
            // gap, not all in the frame the crowd fell.
            var burst = new HordeDirector(40,1,11);
            burst.SetState(HordeState.Pressure);
            Assert.That(Fill(burst,2f,HordeRule.TargetByState[(int)HordeState.Pressure]-1,0,spawns),Is.EqualTo(1),"Room for one; the rest of the credit waits.");
            Assert.That(Fill(burst,Frame,0,0,spawns),Is.EqualTo(1),"A frame places one, however much is owed.");
            Assert.That(Fill(burst,Frame,1,0,spawns),Is.Zero,"The gap is not up.");
            Assert.That(Fill(burst,HordeRule.MinSpawnGapSeconds,1,0,spawns),Is.EqualTo(1),"Now it is.");
        }

        [Test] public void RolesFollowTheirWeightsAndTheSeedDecidesThem()
        {
            const int Draws = 10000;
            var director = new HordeDirector(1000,3,42);
            var twin = new HordeDirector(1000,3,42);
            var other = new HordeDirector(1000,3,43);
            var weights = new[]{1,2,7};
            director.SetRoleWeights(HordeState.Ramp,weights);
            twin.SetRoleWeights(HordeState.Ramp,weights);
            other.SetRoleWeights(HordeState.Ramp,weights);
            var spawns = new List<HordeDirector.Spawn>();
            var twinSpawns = new List<HordeDirector.Spawn>();
            var otherSpawns = new List<HordeDirector.Spawn>();
            var counts = new int[3];
            bool differs = false;
            while (director.Spawned < Draws)
            {
                // The crowd never reported as standing, so each 1.5 s frame places the credit cap's three.
                director.Tick(1.5f,0,0,spawns);
                twin.Tick(1.5f,0,0,twinSpawns);
                other.Tick(1.5f,0,0,otherSpawns);
                Assert.That(spawns.Count,Is.EqualTo(3));
                for (int i = 0; i < spawns.Count; i++)
                {
                    Assert.That(spawns[i].Ordinal,Is.EqualTo(director.Spawned - spawns.Count + i),"Ordinals count the director's life.");
                    Assert.That((twinSpawns[i].Role,twinSpawns[i].Ordinal),Is.EqualTo((spawns[i].Role,spawns[i].Ordinal)),"Same seed, same horde.");
                    differs |= otherSpawns[i].Role != spawns[i].Role;
                    counts[spawns[i].Role]++;
                }
            }
            Assert.That(differs,Is.True,"Another seed, another horde.");
            int drawn = director.Spawned;
            Assert.That(counts[0],Is.EqualTo(drawn / 10).Within(drawn / 50),"About one in ten.");
            Assert.That(counts[1],Is.EqualTo(drawn / 5).Within(drawn / 50),"About two in ten.");
            Assert.That(counts[2],Is.EqualTo(drawn * 7 / 10).Within(drawn / 50),"About seven in ten.");

            // A zero weight is never drawn, and the weights are the state's: another state keeps its own.
            var only = new HordeDirector(40,3,5);
            only.SetRoleWeights(HordeState.Ramp,new[]{0,0,1});
            only.Tick(1.5f,0,0,spawns);
            foreach (var spawn in spawns) Assert.That(spawn.Role,Is.EqualTo(2));
            only.SetState(HordeState.Horde);
            only.Tick(1.5f,0,0,spawns);
            Assert.That(spawns.Count,Is.EqualTo(3));
            Assert.That(()=>only.SetRoleWeights(HordeState.Ramp,new[]{1,1}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>only.SetRoleWeights(HordeState.Ramp,new[]{0,0,0}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>only.SetRoleWeights(HordeState.Ramp,new[]{1,-1,1}),Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(()=>only.SetRoleWeights(HordeState.Ramp,null),Throws.InstanceOf<ArgumentNullException>());
        }

        [Test] public void TheWindowForgetsWhatIsOlderThanItself()
        {
            var director = new HordeDirector(40,1,1);
            var spawns = new List<HordeDirector.Spawn>();
            director.Fell(); director.Fell(); director.Fell();
            Assert.That(director.KillsInWindow,Is.EqualTo(3));
            Assert.That(director.TargetNow,Is.EqualTo(6),"Three kills against no spawns: the full boost.");
            director.Tick(HordeRule.WindowSeconds - 0.5f,40,0,spawns);
            Assert.That(director.KillsInWindow,Is.EqualTo(3),"Still inside the window.");
            director.Tick(0.5f,40,0,spawns);
            Assert.That(director.KillsInWindow,Is.Zero,"WindowSeconds on, the kills are forgotten.");
            Assert.That(director.TargetNow,Is.EqualTo(4),"And the target settles back.");

            // Spawns are remembered the same way, and a stall longer than the window clears everything at once.
            var busy = new HordeDirector(40,1,1);
            busy.Tick(1.5f,0,0,spawns);
            Assert.That(busy.SpawnsInWindow,Is.EqualTo(3));
            busy.Fell();
            busy.Tick(HordeRule.WindowSeconds * 3f,40,0,spawns);
            Assert.That((busy.KillsInWindow,busy.SpawnsInWindow),Is.EqualTo((0,0)));
            Assert.That(busy.Spawned,Is.EqualTo(3),"What was spawned stays spawned.");
        }

        [Test] public void TheOpeningLeansOnTheLightestRoleAndThePressureOnTheHeavier()
        {
            Assert.That((HordeRule.Weight(HordeState.Ramp,0),HordeRule.Weight(HordeState.Ramp,1),HordeRule.Weight(HordeState.Ramp,2)),Is.EqualTo((2,1,0)),"The heaviest role waits.");
            Assert.That((HordeRule.Weight(HordeState.Pressure,0),HordeRule.Weight(HordeState.Pressure,1),HordeRule.Weight(HordeState.Pressure,2)),Is.EqualTo((4,3,2)));
            Assert.That(HordeRule.Weight(HordeState.Boss,2),Is.EqualTo(HordeRule.Weight(HordeState.Ramp,2)),"Boss minions stay light.");
            Assert.That(HordeRule.Experience(10),Is.EqualTo(2));Assert.That(HordeRule.Experience(1),Is.EqualTo(1),"Never nothing for a kill.");
            Assert.That(HordeRule.Experience(0),Is.Zero);
            Assert.That(HordeRule.SceneCap,Is.InRange(30,PackLayout.MaxPackSize),"The owner's first range, inside the pack.");
            Assert.That(HordeRule.HealthShare,Is.InRange(0.05f,1f));Assert.That(HordeRule.DamageShare,Is.InRange(0.05f,1f),"Fodder, but never harmless.");
        }

        [Test] public void TheClockGrowsTheHordeForFiveMinutesThenStepsItUp()
        {
            Assert.That(HordeClock.TargetSeconds,Is.EqualTo(300f),"The owner's five-minute floor.");
            Assert.That(HordeClock.HealthMultiplier(0f),Is.EqualTo(1f),"The floor opens at its authored horde.");
            Assert.That(HordeClock.HealthMultiplier(150f),Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(HordeClock.HealthMultiplier(299.9f),Is.LessThan(2f));
            Assert.That(HordeClock.HealthMultiplier(300f),Is.EqualTo(2f).Within(1e-5f),"Twice the health at five minutes.");
            Assert.That(HordeClock.DamageMultiplier(300f),Is.EqualTo(1.5f).Within(1e-5f),"Half again the damage.");
            Assert.That(HordeClock.HealthMultiplier(314.9f),Is.EqualTo(2f).Within(1e-5f),"Overtime grows in whole steps.");
            Assert.That(HordeClock.HealthMultiplier(315f),Is.EqualTo(2f*1.16f).Within(1e-4f));
            Assert.That(HordeClock.HealthMultiplier(360f),Is.EqualTo(3.62f).Within(.01f),"A minute overrun: 3.6 times the opening.");
            Assert.That(HordeClock.DamageMultiplier(360f),Is.EqualTo(2.2f).Within(.01f));
            Assert.That(HordeClock.HealthMultiplier(420f),Is.EqualTo(6.55f).Within(.02f),"Two minutes: standing still is death.");
            Assert.That(HordeClock.OvertimeSteps(1e7f),Is.EqualTo(HordeClock.MaxOvertimeSteps),"Hours on a floor never overflow.");
            Assert.That(float.IsInfinity(HordeClock.HealthMultiplier(1e7f)),Is.False);
            Assert.That(HordeClock.SpawnRate(0f),Is.EqualTo(HordeRule.MaxSpawnsPerSecond));
            Assert.That(HordeClock.SpawnRate(150f),Is.EqualTo(3f).Within(1e-5f));
            Assert.That(HordeClock.SpawnRate(300f),Is.EqualTo(HordeClock.OvertimeRate),"Overrun: the crowd floods in.");
            Assert.That(HordeClock.SpawnGap(299f),Is.EqualTo(HordeRule.MinSpawnGapSeconds),"The phone's gap holds until the rate outruns it.");
            Assert.That(HordeClock.SpawnGap(300f),Is.EqualTo(1f/HordeClock.OvertimeRate).Within(1e-6f));
            Assert.That((HordeClock.Remaining(100f),HordeClock.Remaining(400f)),Is.EqualTo((200f,0f)));
            Assert.That((HordeClock.IsOvertime(299.99f),HordeClock.IsOvertime(300f)),Is.EqualTo((false,true)));
            Assert.That((HordeClock.PaysExperience(299.99f),HordeClock.PaysExperience(300f)),Is.EqualTo((true,false)),
                "The overrun is not a farm.");
            Assert.That(()=>HordeClock.HealthMultiplier(float.NaN),Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(()=>HordeClock.OvertimeSteps(-1f),Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test] public void TheClockedTargetGrowsAndTheOverrunAimsForTheCapButNotInTheBossRoom()
        {
            Assert.That(HordeRule.EffectiveTarget(HordeState.Pressure,0,10,40,0f),Is.EqualTo(HordeRule.EffectiveTarget(HordeState.Pressure,0,10,40)),
                "At the floor's first second the clock changes nothing.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Pressure,0,10,40,150f),Is.EqualTo(20),"16 grown by a quarter.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Pressure,10,10,40,150f),Is.EqualTo(30),"Then the reactive half again.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Horde,0,10,40,299f),Is.EqualTo(15));
            Assert.That(HordeRule.EffectiveTarget(HordeState.Ramp,0,10,40,300f),Is.EqualTo(40),"Overrun: the scene's cap, whatever the state.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Horde,0,10,30,600f),Is.EqualTo(30),"And never past it.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Boss,0,10,40,600f),Is.EqualTo(6),"The boss room stays readable.");
            Assert.That(HordeRule.EffectiveTarget(HordeState.Boss,10,10,40,600f),Is.EqualTo(9));
            Assert.That(()=>HordeRule.EffectiveTarget(HordeState.Horde,0,0,40,float.NaN),Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test] public void AClockedDirectorFloodsTheOverrunFloorAndAnUnclockedOneNeverDoes()
        {
            var clocked = new HordeDirector(40,1,9,true);
            var plain = new HordeDirector(40,1,9);
            var spawns = new List<HordeDirector.Spawn>();
            clocked.SetState(HordeState.Horde);
            plain.SetState(HordeState.Horde);
            Assert.That((clocked.IsClocked,plain.IsClocked),Is.EqualTo((true,false)));
            Assert.That(clocked.HealthMultiplier,Is.EqualTo(1f),"The floor opens at its authored horde.");
            // Five minutes with the crowd at the cap: no credit is owed, only the clock runs.
            for (int i = 0; i < 300; i++)
            {
                clocked.Tick(1f,40,0,spawns);
                plain.Tick(1f,40,0,spawns);
            }
            Assert.That((clocked.IsOvertime,plain.IsOvertime),Is.EqualTo((true,false)));
            Assert.That(clocked.HealthMultiplier,Is.EqualTo(2f).Within(1e-4f));
            Assert.That(clocked.DamageMultiplier,Is.EqualTo(1.5f).Within(1e-4f));
            Assert.That((plain.HealthMultiplier,plain.DamageMultiplier),Is.EqualTo((1f,1f)),"An unclocked horde never grows.");
            Assert.That(clocked.TargetNow,Is.EqualTo(40),"Overrun: the crowd aims for the cap.");
            Assert.That(plain.TargetNow,Is.EqualTo(HordeRule.TargetByState[(int)HordeState.Horde]));

            var times = new List<float>();
            int plainSpawns = 0;
            for (int f = 0; f < (int)(2f / Frame); f++)
            {
                clocked.Tick(Frame,0,0,spawns);
                Assert.That(spawns.Count,Is.LessThanOrEqualTo(1),"A frame still places one at most.");
                if (spawns.Count == 1) times.Add(clocked.Seconds);
                plainSpawns += Fill(plain,Frame,0,0,spawns);
            }
            for (int i = 1; i < times.Count; i++)
                Assert.That(times[i] - times[i-1],Is.GreaterThanOrEqualTo(HordeClock.SpawnGap(300f) - Frame),
                    "Spaced on the director's clock: an eighth of a second is seven or eight frames, never fewer.");
            Assert.That(times.Count,Is.InRange(14,17),"Eight a second, after the credit's first spawn.");
            Assert.That(plainSpawns,Is.InRange(3,5),"Two a second, as ever.");
        }

        [Test] public void SkippingTheClockMovesTheFloorOnWithoutWalkingAnyoneIn()
        {
            var director = new HordeDirector(40,1,9,true);
            var spawns = new List<HordeDirector.Spawn>();
            director.SetState(HordeState.Horde);
            director.Fell();
            director.SkipClock(297f);
            Assert.That(director.Seconds,Is.EqualTo(297f));
            Assert.That((director.Spawned,director.KillsInWindow),Is.EqualTo((0,0)),"Nobody walked in; the old kill is forgotten.");
            Assert.That(director.HealthMultiplier,Is.EqualTo(1.99f).Within(1e-4f));
            director.Tick(3f,0,0,spawns);
            Assert.That(director.IsOvertime,Is.True);
            Assert.That(spawns.Count,Is.GreaterThan(0),"The jump leaves the gap behind it.");
            Assert.That(()=>director.SkipClock(-1f),Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test] public void TheFloorClockReadsDownToTheOverrunThenUp()
        {
            Assert.That(HordeClock.ShownSecond(0f),Is.EqualTo(300),"5:00 at the first step.");
            Assert.That(HordeClock.ShownSecond(.2f),Is.EqualTo(300),"Rounded up, so a full second shows a full second.");
            Assert.That(HordeClock.ShownSecond(299.5f),Is.EqualTo(1));
            Assert.That(HordeClock.ShownSecond(300f),Is.EqualTo(0),"OVERRUN +0:00.");
            Assert.That(HordeClock.ShownSecond(372.9f),Is.EqualTo(72),"OVERRUN +1:12.");
        }

        [Test] public void HordeSpotsLieOnTheFloorAtTheEdgeOfTheView()
        {
            Cryptforge.Art.FoundryMap.ToFloor(0,0,out float hx,out float hy);
            int found=0;
            for(int attempt=0;attempt<Cryptforge.Art.FoundryMap.HordeSpotAttempts;attempt++)
            {
                if(!Cryptforge.Art.FoundryMap.HordeSpot(3,attempt,hx,hy,out float x,out float y))continue;
                found++;
                Assert.That(Cryptforge.Art.FoundryMap.Contains(x,y,HeroMotion.EdgeMargin),Is.True);
                float d=(float)Math.Sqrt((x-hx)*(x-hx)+(y-hy)*(y-hy));
                Assert.That(d,Is.InRange(Cryptforge.Art.FoundryMap.HordeSpotNear-1e-3f,Cryptforge.Art.FoundryMap.HordeSpotFar+1e-3f));
            }
            Assert.That(found,Is.GreaterThan(Cryptforge.Art.FoundryMap.HordeSpotAngles),"The court's middle has room all round.");
        }
    }
}
