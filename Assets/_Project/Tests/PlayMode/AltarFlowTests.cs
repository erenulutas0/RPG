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
    // The foundry's altars: standing still on one opens a card offer and makes the run harder, once per altar.
    public sealed class AltarFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Altar cleanup"));
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

        private static string TelemetryText()
        {
            var text=new System.Text.StringBuilder();
            string folder=Cryptforge.Analytics.TelemetryLocation.Resolve();
            if(Directory.Exists(folder))foreach(string file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories))text.Append(File.ReadAllText(file));
            return text.ToString();
        }

        private static IEnumerator Wait(float seconds)
        {
            float end=Time.time+seconds,deadline=Time.realtimeSinceStartup+seconds+10;
            while(Time.time<end&&Time.realtimeSinceStartup<deadline)yield return null;
        }

        [UnityTest, Timeout(90000)] public IEnumerator StandingStillOnAnAltarGrantsACardAndMakesTheRunHarder()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            var view=setup.GetComponent<AltarView>();
            var hero=Object.FindFirstObjectByType<HeroMovementInput>().transform;
            Assert.That(setup.Altars.IsActive,Is.True,"The foundry has altars.");
            yield return null;
            Assert.That(view.IsShown(0)&&view.IsShown(1),Is.True,"Both altars glow.");
            float before=setup.HeroStats.Difficulty;
            int bonus=setup.Run.BonusUpgrades;
            FoundryMap.AltarSite(0,out float x,out float y);
            hero.position=new Vector3(x,ArenaFloor.WorldY(y),0);
            yield return Wait(.5f);
            Assert.That(view.ShownProgress,Is.GreaterThan(0f),"The hold shows while the hero stands still.");
            Assert.That(setup.Run.BonusUpgrades,Is.EqualTo(bonus));
            yield return Wait(.7f);yield return null;
            Assert.That(setup.Run.BonusUpgrades,Is.EqualTo(bonus+1),"The altar grants a card.");
            Assert.That(setup.Choices.Current,Is.Not.Null);Assert.That(setup.Choices.Current.Kind,Is.EqualTo(ChoiceKind.Upgrade),"Its offer opens.");
            Assert.That(setup.HeroStats.Difficulty,Is.EqualTo(before+AltarRule.DifficultyCost).Within(1e-5f),"The run grows harder.");
            Assert.That(view.IsShown(0),Is.False,"A taken altar goes dark.");
            // The offer opening flushed the run's log, the altar before it.
            Assert.That(TelemetryText(),Does.Contain("\"altar_taken\""),"The altar is in the run's log.");
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
            yield return Wait(2f);
            Assert.That(setup.Run.BonusUpgrades,Is.EqualTo(bonus+1),"It answers once.");
            Assert.That(setup.HeroStats.Difficulty,Is.EqualTo(before+AltarRule.DifficultyCost).Within(1e-5f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHallHasNoAltars()
        {
            yield return Load("hall");
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(setup.Altars.IsActive,Is.False);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
