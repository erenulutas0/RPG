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
    // On the foundry a floor's later combat rooms open at their own districts: the room waits, marked, until the hero walks
    // onto it, and its pack then forms up there. The hall keeps every room where the hero stands.
    public sealed class RoomSiteFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Room site cleanup"));
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

        private static void ResolveUpgrades(CombatSetup setup)
        {
            for(int i=0;i<8&&setup.Choices.Current!=null&&setup.Choices.Current.Kind==ChoiceKind.Upgrade;i++)
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
        }

        // Game time until the condition holds, taking every level-up as it opens, with a real-time bound.
        private static IEnumerator Until(CombatSetup setup,System.Func<bool> condition,float seconds)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!condition()&&Time.realtimeSinceStartup<deadline){ResolveUpgrades(setup);yield return null;}
            ResolveUpgrades(setup);
        }

        private static IEnumerator Kill(CombatSetup setup,EncounterController encounter)
        {
            yield return Until(setup,()=>encounter.AliveEnemyCount>0,20);
            Assert.That(encounter.AliveEnemyCount,Is.EqualTo(1));
            encounter.CurrentEnemy.ApplyDamage(new DamageContext(1000000f));
            yield return null;ResolveUpgrades(setup);
        }

        [UnityTest, Timeout(120000)] public IEnumerator LaterHallsWaitAtTheirDistrictsUntilTheHeroWalksOntoThem()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            var view=setup.GetComponent<ExitSealView>();
            var hero=Object.FindFirstObjectByType<HeroMovementInput>().transform;
            // The first hall is fought where the floor begins.
            yield return Kill(setup,encounter);
            yield return Until(setup,()=>encounter.IsRoomSiteOpen,20);
            yield return null;
            Assert.That(encounter.IsRoomSiteOpen,Is.True,"The second hall opens at its district.");
            FoundryMap.RoomSite(1,out float x1,out float y1);
            Assert.That(encounter.RoomSiteFloorX,Is.EqualTo(x1));Assert.That(encounter.RoomSiteFloorY,Is.EqualTo(y1));
            Assert.That(FoundryMap.Contains(x1,y1,HeroMotion.EdgeMargin),Is.True,"The district is walkable floor.");
            Assert.That(encounter.AliveEnemyCount,Is.Zero);Assert.That(view.IsVisible,Is.True,"The district is marked.");
            // Away from the district nothing starts, however long the hero waits.
            float wait=Time.time+2.5f;
            yield return Until(setup,()=>Time.time>=wait,10);
            Assert.That(encounter.IsRoomSiteOpen,Is.True);Assert.That(encounter.AliveEnemyCount,Is.Zero);
            // Walking onto it starts the hall, its pack round the hero there.
            hero.position=new Vector3(x1,ArenaFloor.WorldY(y1),0);
            yield return Until(setup,()=>encounter.AliveEnemyCount>0,10);
            Assert.That(encounter.IsRoomSiteOpen,Is.False);Assert.That(encounter.AliveEnemyCount,Is.EqualTo(1));
            Vector3 enemy=encounter.CurrentEnemy.transform.position;
            Assert.That(ArenaFloor.FloorDistanceSquared(enemy.x,ArenaFloor.FloorY(enemy.y),x1,y1),Is.LessThan(10f*10f),
                "The pack forms up round the hero at the district.");
            yield return null;
            Assert.That(view.IsVisible,Is.False,"The mark goes once the hall begins.");
            // The elite hall opens at the next district.
            encounter.CurrentEnemy.ApplyDamage(new DamageContext(1000000f));yield return null;ResolveUpgrades(setup);
            yield return Until(setup,()=>encounter.IsRoomSiteOpen,20);
            FoundryMap.RoomSite(2,out float x2,out float y2);
            Assert.That(encounter.RoomSiteFloorX,Is.EqualTo(x2));Assert.That(encounter.RoomSiteFloorY,Is.EqualTo(y2));
            hero.position=new Vector3(x2,ArenaFloor.WorldY(y2),0);
            yield return Kill(setup,encounter);
            yield return Until(setup,()=>setup.Run.HasEnded,20);
            Assert.That(setup.Run.HasEnded,Is.True,"The floor clears after its last hall.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)] public IEnumerator TheHallKeepsEveryRoomWhereTheHeroStands()
        {
            yield return Load("hall");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var encounter=setup.Encounters;
            for(int room=0;room<3;room++)
            {
                yield return Kill(setup,encounter);
                Assert.That(encounter.IsRoomSiteOpen,Is.False,"Only the foundry marks districts.");
            }
            yield return Until(setup,()=>setup.Run.HasEnded,20);
            Assert.That(setup.Run.HasEnded,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
