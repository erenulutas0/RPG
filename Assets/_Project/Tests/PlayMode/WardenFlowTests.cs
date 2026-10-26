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
    // The foundry's gate boss is a fight (WardenRule): more health than its authored 300, a mark where the hero stands, the
    // hammer landing on it a telegraph later, and faster blows below half health.
    public sealed class WardenFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Warden cleanup"));
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

        private static void ResolveUpgrades(CombatSetup setup)
        {
            for(int i=0;i<8&&setup.Choices.Current!=null&&setup.Choices.Current.Kind==ChoiceKind.Upgrade;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
        }

        private static IEnumerator Until(CombatSetup setup,System.Func<bool> condition,float seconds)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!condition()&&Time.realtimeSinceStartup<deadline){ResolveUpgrades(setup);yield return null;}
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheWardenMarksTheHeroAndItsHammerLandsOnTheMark()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;var warden=setup.Warden;
            var view=setup.GetComponent<WardenSlamView>();
            var heroInput=Object.FindFirstObjectByType<HeroMovementInput>();var hero=heroInput.GetComponent<Health>();
            yield return Until(setup,()=>warden.IsFighting&&!encounter.IsRevealHeld,30);
            Assert.That(warden.IsFighting,Is.True,"The gate boss is fought.");
            Health boss=warden.Boss;
            Assert.That(boss.Maximum,Is.EqualTo(encounter.DefinitionOf(boss).MaximumHealth*WardenRule.GateHealthShare).Within(1f),
                "The gate boss stands longer than its authored health.");
            bool marked=false;warden.Marked+=()=>marked=true;
            yield return Until(setup,()=>marked,WardenRule.SlamEvery+5);
            Assert.That(marked,Is.True);yield return null;
            Assert.That(view.IsMarkShown,Is.True,"The mark shows on the floor.");
            Assert.That(ArenaFloor.FloorDistanceSquared(warden.MarkX,warden.MarkY,hero.transform.position.x,ArenaFloor.FloorY(hero.transform.position.y)),
                Is.LessThan(.01f),"Where the hero stood.");
            float full=hero.Current;bool? hit=null;warden.Slammed+=h=>hit=h;
            yield return Until(setup,()=>hit.HasValue,WardenRule.Telegraph+3);
            Assert.That(hit,Is.True,"A hero who stays on the mark is caught.");
            Assert.That(hero.Current,Is.EqualTo(full-hero.Maximum*WardenRule.HeroShare).Within(.5f));
            // The next mark, and the hero steps off it before the blow.
            marked=false;hit=null;
            yield return Until(setup,()=>marked,WardenRule.SlamEvery+5);
            heroInput.transform.position+=new Vector3(WardenRule.SlamRadius+1f,0,0);
            float before=hero.Current;
            yield return Until(setup,()=>hit.HasValue,WardenRule.Telegraph+3);
            Assert.That(hit,Is.False,"A hero who reads the mark is not.");
            Assert.That(hero.Current,Is.EqualTo(before).Within(.01f));
            // Below half health it is enraged.
            boss.ApplyDamage(new DamageContext(boss.Maximum*.55f));
            yield return null;yield return null;
            Assert.That(warden.IsEnraged,Is.True);Assert.That(encounter.IsBossEnraged,Is.True,"Its room's horde thickens.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHallsWardenFightsAsAuthored()
        {
            yield return Load("hall");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            yield return Until(setup,()=>encounter.AliveEnemyCount>0,20);
            Assert.That(setup.Warden.IsFighting,Is.False,"Only the foundry's gate boss slams.");
            Assert.That(encounter.CurrentEnemy.Maximum,Is.EqualTo(encounter.CurrentDefinition.MaximumHealth).Within(1f));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
