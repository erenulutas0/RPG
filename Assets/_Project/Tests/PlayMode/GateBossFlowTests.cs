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
using UnityEngine.UI;

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

        private static IEnumerator Load(string region,string floor="floor_gate_boss_proof")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath,floor);
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

        // The hero's ability is part of the fight the reveal holds, as its weapons are (AttackController.FightHold): while the
        // camera is at the gate the button stands greyed and a use is refused, and once the camera is back on the hero the
        // ability fires (the run flow's review, 2026-11-11: a ready ability fired off screen).
        [UnityTest, Timeout(90000)] public IEnumerator TheAbilityWaitsWhileTheCameraIsAtTheGate()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var ability=Object.FindFirstObjectByType<AbilityController>();var button=Object.FindFirstObjectByType<AbilityButtonView>();
            Assert.That(button,Is.Not.Null,"The run's ability button.");
            yield return WaitForBoss(encounter);
            Assert.That(encounter.IsRevealHeld,Is.True,"The camera turns to the arriving boss.");
            Assert.That(ability.Ability.IsReady,Is.True,"The ability is ready from the start.");
            Assert.That(ability.IsHeld,Is.True,"The reveal holds it.");
            Assert.That(button.IsUsable,Is.False,"The button stands greyed while the camera is away.");
            Assert.That(ability.TryUse(),Is.False,"A use is refused.");
            Assert.That(ability.UseCount,Is.Zero);
            yield return Hold(setup,BossRevealView.HoldSeconds+2*BossRevealView.TurnSeconds+.3f);
            Assert.That(encounter.IsRevealHeld,Is.False,"The camera is back on the hero.");
            Assert.That(ability.IsHeld,Is.False);
            Assert.That(button.IsUsable,Is.True,"The button answers again.");
            Assert.That(ability.TryUse(),Is.True,"And the ability fires.");
            Assert.That(ability.UseCount,Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        // A tap on the greyed ability button skips the reveal, where a tap on the pause button stays the pause button's
        // (the run flow's review, 2026-11-11: a tap on the greyed button did nothing at all). The legacy Input cannot be
        // driven from a test, so RevealSkipTests holds the rule; here the reveal hands RevealSkip the ability button while
        // the camera is at the gate, a tap at the button's middle is on it and one at the pause button's is not, and once
        // the camera is back the button answers its own taps again.
        [UnityTest, Timeout(90000)] public IEnumerator TheRevealTakesATapOnTheGreyedAbilityButton()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var reveal=setup.GetComponent<BossRevealView>();var button=Object.FindFirstObjectByType<AbilityButtonView>();
            var pause=GameObject.Find("Pause Button").GetComponent<RectTransform>();
            yield return WaitForBoss(encounter);
            Assert.That(reveal.IsRevealing,Is.True,"The boss walking out of the gate is presented.");
            Assert.That(button.IsUsable,Is.False,"The ability button stands greyed.");
            RectTransform held=reveal.HeldButton;
            Assert.That(held,Is.Not.Null,"The reveal holds the ability button.");
            Assert.That(held,Is.SameAs(button.ButtonRect));
            Assert.That(RevealSkip.IsOn(held,ScreenMiddle(held)),Is.True,"A tap at its middle is on it.");
            Assert.That(RevealSkip.IsOn(held,ScreenMiddle(pause)),Is.False,"A tap on the pause button is not.");
            yield return Hold(setup,BossRevealView.HoldSeconds+2*BossRevealView.TurnSeconds+.3f);
            Assert.That(reveal.IsRevealing,Is.False,"The camera is back on the hero.");
            Assert.That(reveal.HeldButton,Is.Null,"The button answers its own taps again.");
            LogAssert.NoUnexpectedReceived();
        }

        // Where a UI element's middle stands on screen: the HUD's canvas is an overlay, where world and screen are one.
        private static Vector2 ScreenMiddle(RectTransform rect)=>
            RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));

        // The reveal frames the gate's boss, not whichever of its wave stands nearest the hero: floor 2's Warden walks out
        // between two Cinder Mites, and from the floor's start a Mite stands nearer, so the camera turned to the Mite (the
        // run flow's review, 2026-11-11). Resources/Development/Floor_GateEscortProof: the gate's room as floor 2 plays it.
        [UnityTest, Timeout(90000)] public IEnumerator TheRevealFramesTheWardenNotItsNearestEscort()
        {
            yield return Load("foundry","floor_gate_escort_proof");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var reveal=setup.GetComponent<BossRevealView>();var camera=Camera.main;
            for(int i=0;i<600&&encounter.AliveEnemyCount==0;i++)yield return null;
            Assert.That(encounter.AliveEnemyCount,Is.EqualTo(3),"The Warden walks out with its two Mites.");
            Health warden=encounter.GateBoss;
            Assert.That(warden,Is.Not.Null,"The wave's first enemy is the gate's boss.");
            Assert.That(encounter.CurrentEnemy,Is.Not.SameAs(warden),"From the floor's start a Mite stands nearer the hero.");
            Assert.That(reveal.IsRevealing,Is.True,"The boss walking out of the gate is presented.");
            Vector3 wardenAt=warden.transform.position;
            yield return Hold(setup,BossRevealView.TurnSeconds+.3f);
            Assert.That(reveal.Turn,Is.EqualTo(1f).Within(.001f),"The camera has turned fully to the gate.");
            Assert.That(camera.transform.position.x,Is.EqualTo(wardenAt.x).Within(.01f),"It frames the Warden, not the Mite.");
            LogAssert.NoUnexpectedReceived();
        }

        // The boss banner and its bar name the gate's boss while it stands, not the nearest of its wave: floor 2's Mites
        // reach the hero first, and the banner read "Cinder Mite" until both fell (the pre-push review, 2026-11-12). Once the
        // Warden falls with a Mite still standing, the bar goes back to the hero's target.
        [UnityTest, Timeout(90000)] public IEnumerator TheBossBannerNamesTheWardenNotItsNearestEscort()
        {
            yield return Load("foundry","floor_gate_escort_proof");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var hud=Object.FindFirstObjectByType<RunHud>();
            Text banner=GameObject.Find("Boss Name").GetComponent<Text>();
            for(int i=0;i<600&&encounter.AliveEnemyCount==0;i++)yield return null;
            Assert.That(encounter.AliveEnemyCount,Is.EqualTo(3),"The Warden walks out with its two Mites.");
            Health warden=encounter.GateBoss;
            Assert.That(warden,Is.Not.Null,"The wave's first enemy is the gate's boss.");
            Assert.That(encounter.CurrentEnemy,Is.Not.SameAs(warden),"From the floor's start a Mite stands nearer the hero.");
            string wardenName=encounter.GateBossDefinition.DisplayName;
            Assert.That(encounter.CurrentDefinition.DisplayName,Is.Not.EqualTo(wardenName),"The nearest enemy is an escort.");
            yield return null;
            Assert.That(banner.text,Is.EqualTo(wardenName),"The banner names the Warden.");
            Assert.That(hud.ShownEnemy,Is.SameAs(warden),"The bar is the Warden's.");
            yield return Hold(setup,BossRevealView.HoldSeconds+2*BossRevealView.TurnSeconds+.3f);
            Assert.That(banner.text,Is.EqualTo(wardenName),"Still the Warden's after the reveal, with the Mites at the hero.");
            Assert.That(hud.ShownEnemy,Is.SameAs(warden));

            warden.ApplyDamage(new DamageContext(PackTestUtility.Lethal));yield return null;ResolveUpgrades(setup);yield return null;
            Assert.That(encounter.GateBoss,Is.Null,"The Warden has fallen.");
            if(encounter.AliveEnemyCount>0)
                Assert.That(hud.ShownEnemy,Is.SameAs(encounter.CurrentEnemy),"The bar goes back to the hero's target.");
            LogAssert.NoUnexpectedReceived();
        }

        // The floor after the gate has no gate boss of its own: a swarm walks in without a wave's spawn, and the gate wave's
        // flag, once left standing, made the swarm's first enemy the Warden - its marks and slams on the hero, the Sword's
        // share on it (the review of 2026-11-11; the Descent's finale after floor 2 on the foundry). The flag goes with the
        // wave now. Resources/Development/Floor_GateThenSwarmProof: the proof's gate, then Floor_SwarmProof.
        [UnityTest, Timeout(90000)] public IEnumerator TheSwarmAfterTheGateHasNoWarden()
        {
            yield return Load("foundry","floor_gate_then_swarm_proof");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var hero=Object.FindFirstObjectByType<HeroMovementInput>().transform;var heroHealth=hero.GetComponent<Health>();
            yield return WaitForBoss(encounter);
            Assert.That(encounter.GateBoss,Is.Not.Null,"The gate's boss is the gate boss.");
            encounter.CurrentEnemy.ApplyDamage(new DamageContext(1000000f));yield return null;ResolveUpgrades(setup);yield return null;ResolveUpgrades(setup);yield return null;
            Assert.That(encounter.IsGateExitOpen,Is.True,"The boss's fall opens the gate.");
            FoundryMap.GateExitSpot(out float gx,out float gy);
            hero.position=new Vector3(gx,ArenaFloor.WorldY(gy),0);
            float deadline=Time.realtimeSinceStartup+20f;
            while(!encounter.IsInSwarmRoom&&!setup.Run.HasEnded&&Time.realtimeSinceStartup<deadline)
            {
                ResolveUpgrades(setup);
                var prompt=setup.Choices.Current;
                if(prompt!=null&&prompt.Kind==ChoiceKind.Checkpoint)Assert.That(setup.Choices.TrySelect(prompt,1),Is.True,"Descend.");
                yield return null;
            }
            Assert.That(setup.Run.HasEnded,Is.False);
            Assert.That(encounter.IsInSwarmRoom,Is.True,"Walking out of the gate and descending opens the swarm.");
            float end=Time.time+2f;deadline=Time.realtimeSinceStartup+20f;
            while(Time.time<end&&Time.realtimeSinceStartup<deadline)
            {
                ResolveUpgrades(setup);
                if(heroHealth.IsAlive)heroHealth.Heal(heroHealth.Maximum);
                Assert.That(encounter.GateBoss,Is.Null,"No enemy of the swarm is the gate boss.");
                Assert.That(setup.Warden.IsFighting,Is.False,"The Warden's slam has nobody to swing.");
                yield return null;
            }
            Assert.That(Time.time,Is.GreaterThanOrEqualTo(end),"The swarm ran.");
            LogAssert.NoUnexpectedReceived();
        }

        // The Warden's fall opens the gate and releases the floor's horde, whose bodies Destroy takes away only at the
        // frame's end: until the run flow's review of 2026-11-11 a released horde enemy kept its AttackController for the
        // rest of that frame, and one in reach could still strike the hero after the Warden had fallen. Released, it leaves
        // at once, as the simulation drops it. The Warden falls from EarlyUpdateHook, before the frame's other Updates, with
        // the hero standing on a horde Mite whose bite is ready (a Mite's has no first delay, so one still walking in holds
        // it): were the Mite still on the floor, it would bite in that same frame. The Warden's own escort is not released
        // by its fall - the gate opens only once the whole wave is down - so the horde is what the fall lets go.
        // Resources/Development/Floor_GateHoldProof: a hall, the gate at 0:12, a horde of Mites.
        [UnityTest, Timeout(90000)] public IEnumerator TheHordeTheWardensFallReleasesStrikesNoMoreThatFrame()
        {
            yield return Load("foundry","floor_gate_hold_proof");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var hero=Object.FindFirstObjectByType<HeroMovementInput>().transform;var heroHealth=hero.GetComponent<Health>();
            float deadline=Time.realtimeSinceStartup+40f;
            while((encounter.GateBoss==null||encounter.IsRevealHeld)&&!setup.Run.HasEnded&&Time.realtimeSinceStartup<deadline)
            {
                ResolveUpgrades(setup);
                if(heroHealth.IsAlive)heroHealth.Heal(heroHealth.Maximum);
                if(encounter.CurrentRoom!=null&&encounter.CurrentRoom.Kind==RoomKind.Combat)PackTestUtility.KillWave(encounter);
                yield return null;
            }
            Assert.That(encounter.GateBoss,Is.Not.Null,"The Warden walks out once the floor's clock allows.");
            Assert.That(encounter.IsRevealHeld,Is.False,"The camera is back on the hero.");
            deadline=Time.realtimeSinceStartup+20f;
            while(ReadyHordeEnemy(encounter,hero)==null&&Time.realtimeSinceStartup<deadline)
            {
                ResolveUpgrades(setup);
                if(heroHealth.IsAlive)heroHealth.Heal(heroHealth.Maximum);
                yield return null;
            }
            Assert.That(ReadyHordeEnemy(encounter,hero),Is.Not.Null,"A horde Mite walks in with its bite ready.");

            Health mite=null;AttackController bite=null;int bitesBefore=-1;float healthBefore=-1f;bool activeAfterFall=true;
            var hook=setup.gameObject.AddComponent<EarlyUpdateHook>();
            hook.Next=()=>
            {
                mite=ReadyHordeEnemy(encounter,hero);
                if(mite==null)return;
                bite=mite.GetComponent<AttackController>();bitesBefore=bite.AttackCount;
                heroHealth.Heal(heroHealth.Maximum);healthBefore=heroHealth.Current;
                // The hero on the Mite, inside its reach.
                hero.position=mite.transform.position;
                encounter.GateBoss.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
                activeAfterFall=mite.gameObject.activeInHierarchy;
            };
            yield return null;
            Assert.That(mite,Is.Not.Null,"The Warden fell with a ready Mite on the floor.");
            Assert.That(encounter.IsGateExitOpen,Is.True,"The Warden's fall opens the gate.");
            Assert.That(encounter.IsHordeActive,Is.False,"And releases the horde.");
            Assert.That(bite.AttackCount,Is.EqualTo(bitesBefore),"The released Mite strikes no more that frame.");
            Assert.That(heroHealth.Current,Is.EqualTo(healthBefore),"The hero takes no blow after the Warden's fall.");
            Assert.That(activeAfterFall,Is.False,"A released enemy leaves the floor at once.");
            LogAssert.NoUnexpectedReceived();
        }

        // A living horde enemy whose strike is ready and whose reach does not hold the hero, so nothing has spent it yet.
        private static Health ReadyHordeEnemy(EncounterController encounter,Transform hero)
        {
            for(int i=0;i<encounter.HordeEnemyCount;i++)
            {
                Health enemy=encounter.HordeEnemyAt(i);
                if(enemy==null||!enemy.IsAlive)continue;
                var attack=enemy.GetComponent<AttackController>();
                if(attack==null||!attack.enabled||attack.Weapon==null||attack.Weapon.CooldownRemaining>0f)continue;
                float reach=attack.Weapon.Range;
                Vector3 at=enemy.transform.position;
                if(ArenaFloor.FloorDistanceSquared(at.x,ArenaFloor.FloorY(at.y),hero.position.x,ArenaFloor.FloorY(hero.position.y))<=reach*reach)continue;
                return enemy;
            }
            return null;
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
