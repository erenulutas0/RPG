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
    public sealed class ExitSealPresentationTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Exit cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }
        private IEnumerator Load(bool swarm)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            if(swarm)File.WriteAllText(DevelopmentStart.StartFloorPath,"floor_swarm_proof");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=false;
        }
        [UnityTest] public IEnumerator DescentHidesSealAndReloadCleansUp()
        {
            yield return Load(false);var view=Object.FindFirstObjectByType<ExitSealView>();
            Assert.That(view.IsVisible,Is.False);Assert.That(view.ShowsDirection,Is.False);
            yield return Load(true);Assert.That(view==null,Is.True);
            Assert.That(Object.FindFirstObjectByType<ExitSealView>().IsVisible,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RealHoldPauseInterruptionAndCompletionFollowEncounter()
        {
            yield return Load(true);var setup=Object.FindFirstObjectByType<CombatSetup>();
            var view=setup.GetComponent<ExitSealView>();var encounter=setup.Encounters;
            var hero=Object.FindFirstObjectByType<HeroMovementInput>().transform;
            var exit=new Vector3(encounter.ExitFloorX,ArenaFloor.WorldY(encounter.ExitFloorY),0);
            Assert.That(view.ExitPosition,Is.EqualTo(exit));
            hero.position=exit;yield return new WaitForSeconds(.35f);yield return null;
            Assert.That(view.ShownProgress,Is.GreaterThan(.1f).And.LessThan(.8f));
            Assert.That(view.ShownProgress,Is.EqualTo(encounter.ExitHeldSeconds/ExitRule.DwellSeconds).Within(.02f));
            Time.timeScale=0;float held=view.ShownProgress;yield return new WaitForSecondsRealtime(.2f);
            Assert.That(view.ShownProgress,Is.EqualTo(held).Within(.02f));
            Time.timeScale=1;hero.position=exit+Vector3.right*2;yield return null;yield return null;
            Assert.That(view.ShownProgress,Is.Zero);Assert.That(setup.Run.HasEnded,Is.False);
            hero.position=exit;yield return new WaitForSeconds(1.2f);yield return null;
            Assert.That(encounter.IsFloorCleared,Is.True);Assert.That(view.IsVisible,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator OffscreenCueDoesNotMoveTheRealSeal()
        {
            yield return Load(true);var view=Object.FindFirstObjectByType<ExitSealView>();
            var camera=Camera.main;camera.GetComponent<ArenaCameraFollow>().enabled=false;
            Vector3 original=view.ExitPosition;camera.transform.position+=Vector3.right*30;
            yield return null;yield return null;
            Assert.That(view.ShowsDirection,Is.True);Assert.That(view.ExitPosition,Is.EqualTo(original));
            view.enabled=false;Assert.That(view.IsVisible,Is.False);Assert.That(view.ShowsDirection,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
