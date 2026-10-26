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
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
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
