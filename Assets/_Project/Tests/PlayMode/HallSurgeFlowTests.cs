using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // On the foundry a combat room sends its floor's surges after its authored waves, each bigger than the last; the hall
    // arena plays the authored waves only (FloorFlowTests pins those).
    public sealed class HallSurgeFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Hall surge cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheFirstHallSendsTwoGrowingSurgesAfterItsThreeWaves()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounters=setup.Encounters;
            Assert.That(encounters.CurrentRoom.WaveCount,Is.EqualTo(3),"Ember Hall authors three waves.");
            Assert.That(encounters.WaveCount,Is.EqualTo(5),"The foundry adds two surges.");
            float start=Time.realtimeSinceStartup+20;
            while(encounters.AliveEnemyCount==0&&Time.realtimeSinceStartup<start)yield return null;
            var seen=new List<string>{PackTestUtility.Describe(encounters)};
            encounters.EncounterStarted+=()=>seen.Add(PackTestUtility.Describe(encounters));
            float deadline=Time.realtimeSinceStartup+40;
            while(encounters.RoomNumber==1&&Time.realtimeSinceStartup<deadline)
            {
                if(setup.Choices.IsOpen)setup.Choices.TrySelect(setup.Choices.Current,0);
                else PackTestUtility.KillWave(encounters);
                yield return null;
            }
            Assert.That(seen.GetRange(0,5),Is.EqualTo(new[]
            {
                "1.1:Ember Hall:Grunt+Cinder Mite","1.2:Ember Hall:Cinder Mite+Cinder Mite+Cinder Mite",
                "1.3:Ember Hall:Grunt+Cinder Mite+Cinder Mite+Cinder Mite",
                "1.4:Ember Hall:Grunt+Cinder Mite+Cinder Mite+Cinder Mite+Cinder Mite",
                "1.5:Ember Hall:Cinder Mite+Cinder Mite+Cinder Mite+Cinder Mite+Cinder Mite"
            }),"The surges repeat the last waves, one enemy bigger each time.");
            Assert.That(encounters.RoomNumber,Is.EqualTo(2),"After the surges the floor moves on.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
