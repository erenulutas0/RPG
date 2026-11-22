using System.Collections;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // docs/62's P0a behind development/outworks.txt: floor 1 of the foundry begins with the Outworks phase - its anvils
    // capture into a card offer, the Keeper walks on and its fall hands over the key, a pad carries the hero over the pit
    // untouched, the Bell raises the swarm and opens the stair, and the stair held leads into the floor's first hall on the
    // same clock. Without the file the floor begins with its first hall as shipped.
    public sealed class OutworksFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Outworks flow cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        // The shipped floor 1 on the foundry, with the switch on or not; nothing strikes and the stick is the test's.
        private static IEnumerator Load(bool outworks,bool walks=false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.OutworksPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            if(outworks)File.WriteAllText(DevelopmentStart.OutworksPath,"on");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=walks;
        }

        // Game time until the condition holds, with a real-time bound; the hero healed every frame so the horde's walk-ins
        // (their strikes on) never end the test, and no offer left open.
        private static IEnumerator Until(CombatSetup setup,System.Func<bool> condition,float seconds,bool answer=true)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            Health hero=setup.Encounters!=null?Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>():null;
            while(!condition()&&Time.realtimeSinceStartup<deadline)
            {
                if(hero!=null&&hero.IsAlive)hero.Heal(hero.Maximum);
                if(answer)Answer(setup);
                yield return null;
            }
            if(answer)Answer(setup);
        }

        private static void Answer(CombatSetup setup)
        {
            for(int i=0;i<8&&setup.Choices.Current!=null&&setup.Choices.Current.Kind==ChoiceKind.Upgrade;i++)
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
        }

        private static IEnumerator Seconds(CombatSetup setup,float seconds)
        {
            float end=Time.time+seconds;
            yield return Until(setup,()=>Time.time>=end,seconds+10f);
        }

        private static void Put(Transform hero,float x,float y)=>hero.position=new Vector3(x,ArenaFloor.WorldY(y),hero.position.z);

        private static string TelemetryText()
        {
            var text=new System.Text.StringBuilder();
            string folder=Cryptforge.Analytics.TelemetryLocation.Resolve();
            // Sharing write access: the scene still recording holds the current file open (TelemetryRuntime.HoldFile).
            if(Directory.Exists(folder))foreach(string file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories))
                using(var reader=new StreamReader(new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)))text.Append(reader.ReadToEnd());
            return text.ToString();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheOutworksBeginsFloorOneBeforeItsFirstHall()
        {
            yield return Load(true);
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            Assert.That(encounter.PlaysOutworks,Is.True,"The file turns the probe on.");
            Assert.That(encounter.IsInOutworks,Is.True,"The floor begins with the Outworks.");
            Assert.That(encounter.Leap,Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<HeroMovementInput>().Leap,Is.SameAs(encounter.Leap),"The pads are the hero's walk's.");
            Assert.That(encounter.RoomNumber,Is.Zero,"No hall yet.");
            Assert.That(encounter.AliveEnemyCount,Is.Zero,"No wave stands.");
            yield return Seconds(setup,1.5f);
            Assert.That(encounter.IsHordeActive,Is.True,"The floor's horde walks the court from the first frame.");
            Assert.That(encounter.Outworks.Seconds,Is.GreaterThan(1f),"The phase's clock runs.");
            Assert.That(encounter.FloorClockSeconds,Is.EqualTo(encounter.Outworks.Seconds).Within(.1f),"One clock for the floor.");
            var view=setup.GetComponent<OutworksView>();
            Assert.That(view,Is.Not.Null);Assert.That(view.IsShown,Is.True,"The phase is drawn.");
            for(int i=0;i<OutworksRule.AnvilCount;i++)Assert.That(view.IsAnvilShown(i),Is.True,"Anvil "+i+" stands.");
            var clock=Object.FindFirstObjectByType<FloorClockView>();
            Assert.That(clock.ShownText,Does.StartWith("Bell in"),"The clock counts to the Bell.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator StandingInAnAnvilsRingCapturesItIntoACardOffer()
        {
            yield return Load(true);
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var view=setup.GetComponent<OutworksView>();
            var hero=Object.FindFirstObjectByType<HeroMovementInput>().transform;
            int captured=-1;encounter.AnvilCaptured+=anvil=>captured=anvil;
            int bonus=setup.Run.BonusUpgrades;
            float difficulty=setup.HeroStats.Difficulty;
            OutworksRule.AnvilSite(0,out float x,out float y);
            Put(hero,x+.5f,y);
            yield return Seconds(setup,OutworksRule.AnvilChargeSeconds*.5f);
            Assert.That(encounter.Outworks.Anvils.Standing,Is.EqualTo(0),"The hero stands in its ring.");
            Assert.That(view.ShownCharge(0),Is.GreaterThan(0f),"Its charge shows.");
            Assert.That(captured,Is.EqualTo(-1));
            yield return Until(setup,()=>captured>=0,OutworksRule.AnvilChargeSeconds+10f,false);
            Assert.That(captured,Is.EqualTo(0),"The anvil is captured.");
            Assert.That(setup.Run.BonusUpgrades,Is.EqualTo(bonus+1),"Its capture is a card offer.");
            yield return null;
            Assert.That(setup.Choices.Current,Is.Not.Null);
            Assert.That(setup.Choices.Current.Kind,Is.EqualTo(ChoiceKind.Upgrade),"The level-up's card panel opens.");
            Assert.That(setup.HeroStats.Difficulty,Is.EqualTo(difficulty),"An anvil costs no danger, as an altar does.");
            // The offer opening flushed the run's log, the capture before it.
            Assert.That(TelemetryText(),Does.Contain("\"outworks_step\""),"The capture is in the run's log.");
            Answer(setup);
            yield return Seconds(setup,1f);
            Assert.That(encounter.Outworks.Anvils.IsCaptured(0),Is.True);
            Assert.That(view.IsAnvilShown(0),Is.False,"A captured anvil goes dark.");
            Assert.That(setup.Run.BonusUpgrades,Is.EqualTo(bonus+1),"It answers once.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheKeeperWalksOnAndItsFallHandsOverTheStairKey()
        {
            yield return Load(true);
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var seal=setup.GetComponent<ExitSealView>();
            bool fell=false;encounter.KeeperFell+=()=>fell=true;
            Assert.That(encounter.IsStairOpen,Is.False,"The stair is sealed.");
            encounter.SkipOutworksClock(OutworksRule.KeeperSpawnSeconds);
            yield return Until(setup,()=>encounter.KeeperLeader!=null,10f);
            Health keeper=encounter.KeeperLeader;
            Assert.That(keeper,Is.Not.Null,"The Keeper walks on.");
            Assert.That(encounter.OutworksKindOf(keeper),Is.EqualTo("keeper"));
            Assert.That(encounter.EliteLeader,Is.SameAs(keeper),"The Keeper is the elite room's Captain.");
            Assert.That(encounter.Outworks.Keeper,Is.EqualTo(KeeperState.Patrolling));
            Assert.That(encounter.AliveEnemyCount,Is.GreaterThan(1),"It walks with its escort.");
            Assert.That(setup.GetComponent<OutworksView>().IsLanternShown,Is.True,"Its lantern marks it.");
            keeper.ApplyDamage(new DamageContext(10000000f));
            yield return null;
            Assert.That(fell,Is.True);
            Assert.That(encounter.Outworks.KeyHeld,Is.True,"Its fall hands over the Stair Key.");
            Assert.That(encounter.IsStairOpen,Is.True,"The key opens the stair before the Bell.");
            Assert.That(encounter.Outworks.BellRung,Is.False);
            yield return null;
            Assert.That(seal.IsVisible,Is.True,"The open stair's seal stands.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator APadCarriesTheHeroOverThePitWhereNoBlowLands()
        {
            yield return Load(true,true);
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var movement=Object.FindFirstObjectByType<HeroMovementInput>();
            var health=movement.GetComponent<Health>();
            var view=setup.GetComponent<OutworksView>();
            OutworksRule.PadLaunch(0,out float lx,out float ly);
            OutworksRule.PadLanding(0,out float tx,out float ty);
            // Two units short of the west pad, on the far side from the pit, then walked onto it.
            FoundryMap.ToFloor(-19.4f,8f,out float sx,out float sy);
            movement.Motion.Place(sx,sy,ArenaGeometry.Foundry);
            Put(movement.transform,sx,sy);
            movement.Hold(new Vector2(lx-sx,ly-sy).normalized);
            yield return Until(setup,()=>encounter.Leap.IsAirborne,10f);
            movement.Release();
            Assert.That(encounter.Leap.IsAirborne,Is.True,"Walking onto the pad launches the hero.");
            Assert.That(encounter.Leap.Pad,Is.EqualTo(0));
            bool overThePit=false,lifted=false,untouched=true;
            float deadline=Time.realtimeSinceStartup+10f;
            while(encounter.Leap.IsAirborne&&Time.realtimeSinceStartup<deadline)
            {
                float before=health.Current;
                health.ApplyDamage(new DamageContext(5f));
                untouched&=health.Current==before;
                overThePit|=!FoundryMap.Contains(movement.Motion.X,movement.Motion.Y,0f);
                lifted|=view.ShownLift>0f;
                yield return null;
            }
            Assert.That(encounter.Leap.IsAirborne,Is.False,"The leap lands.");
            Assert.That(overThePit,Is.True,"The arc crosses the pit.");
            Assert.That(lifted,Is.True,"The body rises off its shadow.");
            Assert.That(untouched,Is.True,"No blow lands on the hero mid-arc.");
            Assert.That(movement.Motion.X,Is.EqualTo(tx).Within(1e-3f));
            Assert.That(movement.Motion.Y,Is.EqualTo(ty).Within(1e-3f));
            Assert.That(movement.transform.position.x,Is.EqualTo(tx).Within(1e-3f),"The hero stands on the landing.");
            Assert.That(encounter.Leap.Launches,Is.EqualTo(1));
            Assert.That(view.ShownLift,Is.EqualTo(0f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheBellRaisesTheSwarmAndOpensTheStair()
        {
            yield return Load(true);
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            bool rang=false;encounter.BellRang+=()=>rang=true;
            yield return Seconds(setup,.5f);
            encounter.SkipOutworksClock(OutworksRule.BellSeconds);
            Assert.That(rang,Is.True,"The Bell rings at its time.");
            Assert.That(encounter.Outworks.BellRung,Is.True);
            Assert.That(encounter.IsBellSwarm,Is.True,"The swarm rises.");
            Assert.That(encounter.IsStairOpen,Is.True,"The stair opens for everyone.");
            Assert.That(encounter.AliveEnemyCount,Is.EqualTo(OutworksRule.BellWaveSize(0)),"The ladder's first wave walks in.");
            for(int i=0;i<encounter.WaveEnemyCount;i++)
                Assert.That(encounter.OutworksKindOf(encounter.WaveEnemyAt(i)),Is.EqualTo("bell"));
            yield return Seconds(setup,1f);
            Assert.That(encounter.IsInOutworks,Is.True,"The stair must be held to leave.");
            var clock=Object.FindFirstObjectByType<FloorClockView>();
            Assert.That(clock.ShownText,Is.EqualTo("THE BELL - Reach the stair"));
            // A fallen Bell enemy's place fills again after its breath, as a swarm's does.
            Health first=encounter.WaveEnemyAt(0);
            first.ApplyDamage(new DamageContext(10000000f));
            int alive=encounter.AliveEnemyCount;
            yield return Seconds(setup,SwarmRule.RespawnSeconds+.5f);
            Assert.That(encounter.AliveEnemyCount,Is.GreaterThan(alive),"Its place filled again.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)] public IEnumerator HoldingTheStairLeadsIntoTheFirstHallOnTheSameClock()
        {
            yield return Load(true);
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var hero=Object.FindFirstObjectByType<HeroMovementInput>().transform;
            bool left=false;encounter.OutworksLeft+=()=>left=true;
            yield return Seconds(setup,.5f);
            encounter.SkipOutworksClock(OutworksRule.BellSeconds);
            OutworksRule.StairSpot(out float x,out float y);
            Put(hero,x+.3f,y);
            yield return Until(setup,()=>left,ExitRule.DwellSeconds+10f);
            Assert.That(left,Is.True,"Standing still on the open stair leaves the Outworks.");
            Assert.That(encounter.IsInOutworks,Is.False);
            Assert.That(encounter.IsStairOpen,Is.False);
            Assert.That(encounter.RoomNumber,Is.EqualTo(1),"The floor's first hall begins.");
            Assert.That(encounter.AliveEnemyCount,Is.GreaterThan(0),"Its wave forms up round the hero.");
            for(int i=0;i<encounter.WaveEnemyCount;i++)
                Assert.That(encounter.OutworksKindOf(encounter.WaveEnemyAt(i)),Is.Null,"No Bell enemy stays.");
            Assert.That(encounter.IsHordeActive,Is.True,"The horde runs on.");
            Assert.That(encounter.FloorClockSeconds,Is.GreaterThanOrEqualTo(OutworksRule.BellSeconds),"On the same floor clock.");
            Assert.That(setup.GetComponent<OutworksView>().IsShown,Is.False,"The phase is no longer drawn.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator WithoutTheFileFloorOneBeginsWithItsFirstHall()
        {
            yield return Load(false);
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            yield return null;
            Assert.That(encounter.PlaysOutworks,Is.False);
            Assert.That(encounter.IsInOutworks,Is.False);
            Assert.That(encounter.Leap,Is.Null);
            Assert.That(Object.FindFirstObjectByType<HeroMovementInput>().Leap,Is.Null);
            Assert.That(setup.GetComponent<OutworksView>(),Is.Null);
            Assert.That(encounter.RoomNumber,Is.EqualTo(1),"The first hall, as shipped.");
            Assert.That(encounter.AliveEnemyCount,Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
