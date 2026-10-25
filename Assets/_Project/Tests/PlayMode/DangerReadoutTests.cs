using System.Collections;
using System.IO;
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
    // The run's difficulty on the top panel: hidden at the authored 1.0, "Danger +N%" once an altar or an evolution
    // raises it, with a swell that waits for any open card offer to close.
    public sealed class DangerReadoutTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Danger cleanup"));
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

        // An altar's worth, written out so the "+10%" below does not follow a retune of the altar.
        private static void Harden(CombatSetup setup)=>
            setup.HeroStats.AddModifier(UpgradeStat.Difficulty,new StatModifier(ModifierOperation.Flat,.1f));

        private static IEnumerator WaitForSwell(DangerReadoutView view)
        {
            float deadline=Time.realtimeSinceStartup+3;
            while(view.ShownScale<=1f&&Time.realtimeSinceStartup<deadline)yield return null;
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheReadoutAppearsAndSwellsWhenTheRunGrowsHarder()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            var view=Object.FindFirstObjectByType<DangerReadoutView>();
            Assert.That(view,Is.Not.Null,"The HUD builds the readout.");
            Assert.That(setup.HeroStats.Difficulty,Is.EqualTo(1f));
            Assert.That(view.IsShown,Is.False,"There is no danger to report at the authored difficulty.");
            Harden(setup);
            Assert.That(view.IsShown,Is.True,"The rise shows at once.");
            Assert.That(view.ShownText,Does.Contain("+10%"));
            yield return WaitForSwell(view);
            Assert.That(view.ShownScale,Is.GreaterThan(1f),"It swells so the rise is noticed.");
            yield return new WaitForSecondsRealtime(DangerReadoutView.PulseSeconds+.2f);
            Assert.That(view.IsPulsing,Is.False);
            Assert.That(view.ShownScale,Is.EqualTo(1f),"The swell settles back.");
            Harden(setup);
            Assert.That(view.ShownText,Does.Contain("+20%"),"Each rise adds up.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheSwellWaitsForAnOpenCardOffer()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            var view=Object.FindFirstObjectByType<DangerReadoutView>();
            setup.Run.GrantBonusUpgrade();
            yield return null;
            Assert.That(setup.Choices.IsOpen,Is.True,"A card offer is open.");
            Harden(setup);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(view.ShownText,Does.Contain("+10%"));
            Assert.That(view.IsPulsePending,Is.True,"The swell is held while the offer covers the game.");
            Assert.That(view.ShownScale,Is.EqualTo(1f));
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
            yield return WaitForSwell(view);
            Assert.That(view.ShownScale,Is.GreaterThan(1f),"It swells once the offer closes.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
