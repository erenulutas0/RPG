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
    // The foundry's floor ends at its gate: the boss walks out of it, and once it falls the floor clears only when the hero
    // walks to the opened gate and holds still there.
    public sealed class GateBossFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Gate boss cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        private static IEnumerator Load(string region)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath,"floor_gate_boss_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,region);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=false;
        }

        // The boss's experience levels the hero up; an open choice pauses the game, so take the first card each time.
        private static void ResolveUpgrades(CombatSetup setup)
        {
            for(int i=0;i<8&&setup.Choices.Current!=null&&setup.Choices.Current.Kind==ChoiceKind.Upgrade;i++)
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True,"The first upgrade card can be taken.");
            Assert.That(setup.Choices.Current==null||setup.Choices.Current.Kind!=ChoiceKind.Upgrade,Is.True,"No upgrade is left open.");
        }

        // Game time passing, with every level-up taken as it opens and a real-time bound, so a pause can never hang a run.
        private static IEnumerator Hold(CombatSetup setup,float seconds)
        {
            float end=Time.time+seconds,deadline=Time.realtimeSinceStartup+seconds+20;
            while(Time.time<end&&Time.realtimeSinceStartup<deadline){ResolveUpgrades(setup);yield return null;}
            ResolveUpgrades(setup);
            Assert.That(Time.time,Is.GreaterThanOrEqualTo(end),"Game time must pass; nothing may stay paused.");
        }

        private static IEnumerator WaitForBoss(EncounterController encounter)
        {
            for(int i=0;i<600&&encounter.AliveEnemyCount==0;i++)yield return null;
            Assert.That(encounter.AliveEnemyCount,Is.EqualTo(1),"The gate floor opens with its single boss.");
        }

        [UnityTest, Timeout(90000)] public IEnumerator BossWalksOutOfTheGateAndTheOpenedGateIsTheWayUp()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var view=setup.GetComponent<ExitSealView>();
            var hero=Object.FindFirstObjectByType<HeroMovementInput>().transform;
            yield return WaitForBoss(encounter);
            var placements=new EntryPlacement[1];FoundryMap.PlaceAtGate(1,placements);
            Vector3 boss=encounter.CurrentEnemy.transform.position;
            Assert.That(boss.x,Is.EqualTo(placements[0].X).Within(.3f),"The boss stands at the gate, not round the hero.");
            Assert.That(ArenaFloor.FloorY(boss.y),Is.EqualTo(placements[0].Y).Within(.3f));
            Assert.That(encounter.IsGateExitOpen,Is.False);Assert.That(view.IsVisible,Is.False,"The gate is shut while its boss stands.");
            Assert.That(Object.FindFirstObjectByType<FoundryGateDoorMotion>().Opening,Is.Zero,"The leaves stay shut while the boss stands.");

            encounter.CurrentEnemy.ApplyDamage(new DamageContext(1000000f));yield return null;ResolveUpgrades(setup);yield return null;ResolveUpgrades(setup);yield return null;
            Assert.That(encounter.IsFloorCleared,Is.True);
            Assert.That(encounter.IsGateExitOpen,Is.True,"The boss's fall opens the gate.");
            Assert.That(setup.Run.HasEnded,Is.False,"The floor waits for the hero to walk out.");
            Assert.That(view.IsVisible,Is.True);
            FoundryMap.GateExitSpot(out float gx,out float gy);
            Assert.That(encounter.ExitFloorX,Is.EqualTo(gx));Assert.That(encounter.ExitFloorY,Is.EqualTo(gy));

            var door=Object.FindFirstObjectByType<FoundryGateDoorMotion>();
            Assert.That(door,Is.Not.Null,"The foundry's gate has swinging leaves.");
            // Standing anywhere else, however long, leaves the floor open; meanwhile the leaves swing fully open.
            yield return Hold(setup,FoundryGateDoorMotion.SwingSeconds+.1f);
            Assert.That(encounter.IsGateExitOpen,Is.True);Assert.That(setup.Run.HasEnded,Is.False);
            Assert.That(door.Opening,Is.EqualTo(1f),"The gate stands open once its boss has fallen.");

            // The gate's hold is wider than the finale's: 1.1 units off its centre still counts.
            Assert.That(ExitRule.GateRadius,Is.GreaterThan(1.1f));Assert.That(encounter.ExitRadius,Is.EqualTo(ExitRule.GateRadius));
            hero.position=new Vector3(gx+1.1f,ArenaFloor.WorldY(gy),0);yield return Hold(setup,.4f);
            Assert.That(encounter.IsGateExitOpen,Is.True,"The hold takes a full second.");
            yield return Hold(setup,1f);yield return null;
            Assert.That(encounter.IsGateExitOpen,Is.False);
            Assert.That(setup.Run.HasEnded,Is.True,"Without a floor above, walking out of the gate ends the run.");
            Assert.That(setup.Run.Outcome,Is.EqualTo(RunOutcome.Victory));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheCameraTurnsToTheArrivingBossWhileTheFightWaits()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var reveal=setup.GetComponent<BossRevealView>();
            var camera=Camera.main;var follow=camera.GetComponent<ArenaCameraFollow>();
            yield return WaitForBoss(encounter);
            Assert.That(reveal.IsRevealing,Is.True,"The boss walking out of the gate is presented.");
            Assert.That(encounter.IsRevealHeld,Is.True,"Nothing is fought while the camera is away.");
            Assert.That(follow.enabled,Is.False);
            Vector3 boss=encounter.CurrentEnemy.transform.position;
            yield return Hold(setup,BossRevealView.TurnSeconds+.3f);
            Assert.That(reveal.Turn,Is.EqualTo(1f).Within(.001f),"The camera has turned fully to the gate.");
            Assert.That(camera.transform.position.x,Is.EqualTo(boss.x).Within(.01f),"It frames the boss.");
            Assert.That(encounter.CurrentEnemy.transform.position,Is.EqualTo(boss),"The boss waits at the gate meanwhile.");
            yield return Hold(setup,BossRevealView.HoldSeconds+BossRevealView.TurnSeconds);
            Assert.That(reveal.IsRevealing,Is.False);Assert.That(encounter.IsRevealHeld,Is.False);
            Assert.That(follow.enabled,Is.True,"The camera follows the hero again.");
            yield return Hold(setup,.5f);
            Assert.That(encounter.CurrentEnemy.transform.position,Is.Not.EqualTo(boss),"Then the boss walks.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHallKeepsItsBossAroundTheHeroAndClearsOnTheKill()
        {
            yield return Load("hall");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            yield return WaitForBoss(encounter);
            encounter.CurrentEnemy.ApplyDamage(new DamageContext(1000000f));yield return null;ResolveUpgrades(setup);yield return null;ResolveUpgrades(setup);yield return null;
            Assert.That(encounter.IsGateExitOpen,Is.False,"Only the foundry has a gate.");
            Assert.That(setup.GetComponent<BossRevealView>().IsRevealing,Is.False,"The hall's boss is not presented.");
            Assert.That(Object.FindFirstObjectByType<FoundryGateDoorMotion>(),Is.Null);
            Assert.That(setup.Run.HasEnded,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
