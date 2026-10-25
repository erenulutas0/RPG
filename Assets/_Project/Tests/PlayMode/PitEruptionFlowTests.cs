using System.Collections;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry pit erupts during a fight: a warning glow on its rim's band, then a burn for whoever stands in it.
    public sealed class PitEruptionFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Pit eruption cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        private static IEnumerator Load(string region)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath,"floor_room_sites_proof");
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

        [UnityTest, Timeout(90000)] public IEnumerator TheRimBurnsWhoeverStandsInItWhenThePitSpills()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var view=setup.GetComponent<PitEruptionView>();
            var heroInput=Object.FindFirstObjectByType<HeroMovementInput>();var hero=heroInput.GetComponent<Health>();
            Assert.That(setup.Pit.IsActive,Is.True);
            yield return Until(()=>encounter.AliveEnemyCount>0,20);
            Assert.That(setup.Pit.IsRunning,Is.True,"The clock runs while a wave stands.");
            FoundryMap.ToFloor(FoundryMap.PitWest-1.2f,8,out float x,out float y);
            heroInput.transform.position=new Vector3(x,ArenaFloor.WorldY(y),0);
            float full=hero.Current;
            yield return Until(()=>setup.Pit.Clock.IsWarning,20);
            yield return null;
            Assert.That(view.ShownAlpha,Is.GreaterThan(0f),"The band glows through the warning.");
            FoundryMap.ToFloor(FoundryMap.PitWest-1f,6,out float bx,out float by);
            Assert.That(encounter.PackHeld(bx,by),Is.True,"Through the warning the band holds the pack.");
            Assert.That(encounter.PackHeld(0,0),Is.False,"Away from the rim nothing is held.");
            Assert.That(hero.Current,Is.EqualTo(full),"Nothing burns before the spill.");
            bool erupted=false;setup.Pit.Erupted+=()=>erupted=true;
            yield return Until(()=>erupted,5);
            Assert.That(erupted,Is.True);
            Assert.That(encounter.PackHeld(bx,by),Is.False,"The spill lets the pack go.");
            Assert.That(hero.Current,Is.EqualTo(full-hero.Maximum*PitEruptionRule.HeroShare).Within(.01f),"The hero in the band is burnt.");
            // Out of the band the next spill passes it by.
            heroInput.transform.position=new Vector3(0,0,0);
            float after=hero.Current;erupted=false;
            yield return Until(()=>erupted,15);
            Assert.That(erupted,Is.True);Assert.That(hero.Current,Is.EqualTo(after),"Out of the band nothing burns.");
            LogAssert.NoUnexpectedReceived();
        }

        // Living wave enemies whose floor point lies in the band - the ones the warning holds.
        private static int InBand(EncounterController encounter)
        {
            int n=0;
            for(int i=0;i<encounter.WaveEnemyCount;i++)
            {
                var enemy=encounter.WaveEnemyAt(i);
                if(enemy!=null&&enemy.IsAlive&&encounter.TryFloorPositionOf(enemy,out float x,out float y)&&PitEruptionRule.InBand(x,y))n++;
            }
            return n;
        }

        [UnityTest, Timeout(90000)] public IEnumerator EveryEnemyTheRimHoldsShowsAGripThroughTheWarning()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var view=setup.GetComponent<PitEruptionView>();var heroInput=Object.FindFirstObjectByType<HeroMovementInput>();
            yield return Until(()=>encounter.AliveEnemyCount>0,20);
            Assert.That(view.ShownHeldCount,Is.EqualTo(0),"No grip before the warning.");
            // The hero waits at the rim, so the pack walks into the band.
            FoundryMap.ToFloor(FoundryMap.PitWest-1.2f,8,out float x,out float y);
            heroInput.transform.position=new Vector3(x,ArenaFloor.WorldY(y),0);
            yield return Until(()=>InBand(encounter)>0,20);
            yield return Until(()=>setup.Pit.Clock.IsWarning,20);
            Assert.That(setup.Pit.Clock.IsWarning,Is.True);
            // Freeze the moment so the view's last frame and the count read here see the same pack.
            Time.timeScale=0;yield return null;yield return null;
            int held=InBand(encounter);
            Assert.That(held,Is.GreaterThan(0),"The pack reached the rim before the warning.");
            Assert.That(view.ShownHeldCount,Is.EqualTo(held),"Every held enemy shows a grip, and no other.");
            Time.timeScale=1;
            bool erupted=false;setup.Pit.Erupted+=()=>erupted=true;
            yield return Until(()=>erupted,5);
            yield return null;
            Assert.That(erupted,Is.True);Assert.That(view.ShownHeldCount,Is.EqualTo(0),"The spill lets the grips go.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
