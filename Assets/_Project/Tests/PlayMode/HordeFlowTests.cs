using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry floor's continuous horde (docs/27 decision 9): it walks in beside the node waves, the hero fights it as
    // it fights them, it pays a share of its experience and no gold, and it never holds a wave's end back.
    public sealed class HordeFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;Time.captureDeltaTime=0;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Horde cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        private static IEnumerator Load(string region)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,region);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=false;
        }

        private static IEnumerator Until(System.Func<bool> condition,float seconds)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHordeWalksInBesideTheWavesAndPaysExperienceButNoGold()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounters=setup.Encounters;
            yield return Until(()=>encounters.HordeAliveCount>=3,15);
            Assert.That(encounters.IsHordeActive,Is.True,"The foundry floor has a horde.");
            Assert.That(encounters.HordeAliveCount,Is.GreaterThanOrEqualTo(3),"It walks in on its own.");
            Assert.That(encounters.HordeAliveCount,Is.LessThanOrEqualTo(encounters.HordeTarget));
            Assert.That(encounters.AllAliveCount,Is.LessThanOrEqualTo(HordeRule.SceneCap),"Never past the phone's cap.");
            int waveAlive=encounters.AliveEnemyCount;
            Health horde=null;
            for(int i=0;i<encounters.HordeEnemyCount&&horde==null;i++){var h=encounters.HordeEnemyAt(i);if(h!=null&&h.IsAlive)horde=h;}
            Assert.That(horde,Is.Not.Null);
            Assert.That(encounters.GoldRewardOf(horde),Is.Zero,"The horde pays no gold.");
            int authored=encounters.DefinitionOf(horde).ExperienceReward;
            Assert.That(encounters.ExperienceRewardOf(horde),Is.EqualTo(HordeRule.Experience(authored)));
            int drops=setup.Drops.Count;int hordeAlive=encounters.HordeAliveCount;
            horde.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(encounters.HordeAliveCount,Is.EqualTo(hordeAlive-1));
            Assert.That(encounters.AliveEnemyCount,Is.EqualTo(waveAlive),"A horde kill is not the wave's.");
            Assert.That(setup.Drops.Count,Is.EqualTo(drops+1),"It drops where it fell.");
            var drop=setup.Drops.At(setup.Drops.Count-1);
            Assert.That((drop.Experience,drop.Gold),Is.EqualTo((HordeRule.Experience(authored),0)));
            // The wave ends with the horde still standing: the floor moves on to its next hall.
            int room=encounters.RoomNumber;
            float deadline=Time.realtimeSinceStartup+30;
            while(encounters.RoomNumber==room&&Time.realtimeSinceStartup<deadline)
            {
                if(setup.Choices.IsOpen)setup.Choices.TrySelect(setup.Choices.Current,0);else PackTestUtility.KillWave(encounters);
                yield return null;
            }
            Assert.That(encounters.RoomNumber,Is.EqualTo(room+1),"The halls advance though the horde stands.");
            Assert.That(encounters.IsHordeActive,Is.True);
            // With the wave gone the hero's targeting finds the horde.
            var targeting=Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Targeting>();
            yield return Until(()=>encounters.HordeAliveCount>0,10);
            Health target=targeting.Acquire(1000f);
            Assert.That(target,Is.Not.Null);Assert.That(encounters.ExperienceRewardOf(target),Is.EqualTo(HordeRule.Experience(encounters.DefinitionOf(target).ExperienceReward)),"The hero aims at the horde.");
            LogAssert.NoUnexpectedReceived();
        }

        // The floor's clock (HordeClock, the owner's five-minute floor): the HUD counts down under the top panel, in ember
        // through the last minute, and once the floor is overrun counts up in red while the horde aims for the scene's cap.
        // The development flag starts the clock three seconds short of the overrun.
        [UnityTest, Timeout(60000)] public IEnumerator TheFloorClockCountsDownAndTheOverrunFloodsTheFloor()
        {
            File.WriteAllText(DevelopmentStart.FloorClockPath,"297");
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounters=setup.Encounters;
            var clock=Object.FindFirstObjectByType<RunHud>().FloorClock;
            Assert.That(clock,Is.Not.Null,"The HUD carries the floor's clock.");
            yield return Until(()=>encounters.IsHordeActive&&clock.IsShown,10);
            Assert.That(clock.IsShown,Is.True,"Shown wherever a horde runs.");
            Assert.That(encounters.FloorClockSeconds,Is.InRange(297f,300f));
            Assert.That(clock.ShownText,Does.Match("^0:0[0-3]$"),"Seconds left before the overrun.");
            Assert.That(clock.IsOverrunShown,Is.False);
            float before=encounters.FloorClockSeconds;
            yield return Until(()=>clock.IsOverrunShown,10);
            Assert.That(encounters.FloorClockSeconds,Is.GreaterThan(before),"The clock runs with the fight.");
            Assert.That(encounters.IsFloorOvertime,Is.True);
            Assert.That(clock.IsOverrunShown,Is.True,"Overrun: the clock says so.");
            Assert.That(clock.ShownText,Does.StartWith("OVERRUN +0:0"));
            Assert.That(encounters.HordeTarget,Is.EqualTo(HordeRule.SceneCap),"The horde aims for the scene's cap.");
            Assert.That(encounters.FloorClockRemaining,Is.Zero);
            // A horde enemy that walked in after the overrun pays nothing.
            // Slots are reused, so the newcomer is read by the slot its arrival names.
            int lateSlot=-1;System.Action<int> arrived=slot=>lateSlot=slot;encounters.HordeEnemySpawned+=arrived;
            yield return Until(()=>lateSlot>=0,10);
            encounters.HordeEnemySpawned-=arrived;
            Assert.That(lateSlot,Is.GreaterThanOrEqualTo(0),"The overrun keeps walking in.");
            Health late=encounters.HordeEnemyAt(lateSlot);
            Assert.That(late,Is.Not.Null);
            Assert.That(encounters.ExperienceRewardOf(late),Is.Zero,"The overrun is a reason to leave, not a farm.");
            LogAssert.NoUnexpectedReceived();
        }

        // Presentation only: a newcomer fades in rather than popping into being, and a fallen one's bar goes at once while
        // its pooled remains fade (and settle) away before the encounter retires the body.
        [UnityTest, Timeout(90000)] public IEnumerator AHordeEnemyFadesInAndAFallenOneFadesOut()
        {
            yield return Load("foundry");
            // Even frames, so a slow batch frame cannot step over the short fades this watches.
            Time.captureDeltaTime=1f/60;
            var encounters=Object.FindFirstObjectByType<CombatSetup>().Encounters;
            Health fresh=null;float startAlpha=-1f,spawnedAt=0f;
            System.Action<int> spawned=slot=>
            {
                if(fresh!=null)return;
                fresh=encounters.HordeEnemyAt(slot);
                startAlpha=fresh.GetComponent<EnemyLookView>().Body.color.a;
                spawnedAt=Time.time;
            };
            // The effects view listened first, so the arrival has begun by the time this hears of the newcomer.
            encounters.HordeEnemySpawned+=spawned;
            yield return Until(()=>fresh!=null,15);
            encounters.HordeEnemySpawned-=spawned;
            Assert.That(fresh,Is.Not.Null,"A horde enemy walks in.");
            var look=fresh.GetComponent<EnemyLookView>();
            Assert.That(startAlpha,Is.LessThan(1f),"It appears faded, not whole.");
            float last=startAlpha;bool between=false;
            float deadline=Time.realtimeSinceStartup+10;
            while(Time.time-spawnedAt<.4f&&Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                float alpha=look.Body.color.a;
                Assert.That(alpha,Is.GreaterThanOrEqualTo(last),"The arrival only ever brightens.");
                between|=alpha>0f&&alpha<1f;
                last=alpha;
            }
            Assert.That(Time.time-spawnedAt,Is.GreaterThanOrEqualTo(.4f),"The arrival had its game time.");
            Assert.That(between,Is.True,"It fades in over frames.");
            Assert.That(look.Body.color.a,Is.EqualTo(1f),"Whole within 0.4 s.");
            Assert.That(look.ArrivalProgress,Is.EqualTo(1f));
            Assert.That(look.Body.transform.localPosition,Is.EqualTo(Vector3.zero),"Risen to its rest.");
            foreach(var part in look.Bar.GetComponentsInChildren<SpriteRenderer>())Assert.That(part.color.a,Is.EqualTo(1f));

            var set=look.PaintedArt;
            Assert.That(set,Is.Not.Null,"The foundry's horde wears painted art, whose remains are its fall.");
            Assert.That(fresh.IsAlive,Is.True);
            fresh.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(look.Bar.gameObject.activeSelf,Is.False,"The bar hides at once.");
            Assert.That(look.Body.gameObject.activeSelf,Is.False,"The standing body gives way to its remains.");
            SpriteRenderer remains=null;
            foreach(var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if(renderer.enabled&&renderer.sprite==set.Death[0])remains=renderer;
            Assert.That(remains,Is.Not.Null,"The fall shows where it happened.");
            float fallAlpha=remains.color.a,fallY=remains.transform.position.y,fellAt=Time.time;
            float faded=fallAlpha,lowest=fallY;
            deadline=Time.realtimeSinceStartup+10;
            while(remains.enabled&&Time.time-fellAt<HordeRule.RetireSeconds&&Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                if(!remains.enabled)break;
                Assert.That(remains.color.a,Is.LessThanOrEqualTo(faded),"The remains only ever fade.");
                faded=remains.color.a;lowest=Mathf.Min(lowest,remains.transform.position.y);
            }
            Assert.That(faded,Is.LessThan(fallAlpha),"The remains fade out.");
            Assert.That(lowest,Is.LessThan(fallY),"They settle into the floor as they go.");
            Assert.That(remains.enabled,Is.False,"Gone before the encounter retires the body.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHallArenaHasNoHorde()
        {
            yield return Load("hall");
            var encounters=Object.FindFirstObjectByType<CombatSetup>().Encounters;
            float until=Time.realtimeSinceStartup+3;
            while(Time.realtimeSinceStartup<until)yield return null;
            Assert.That(encounters.IsHordeActive,Is.False);Assert.That(encounters.HordeAliveCount,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
