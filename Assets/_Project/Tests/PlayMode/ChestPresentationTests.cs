using System.Collections;
using Cryptforge.Combat;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class ChestPresentationTests
    {
        [UnitySetUp] public IEnumerator Load()
        {
            TestProfile.Begin(); Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Chest presentation cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }
        [UnityTest] public IEnumerator RewardIsImmediateAndSingleWhileOpeningHonorsPause()
        {
            var chest=Object.FindFirstObjectByType<ChestSpawner>();
            var renderer=GameObject.Find("Chest").GetComponent<SpriteRenderer>();
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            var health=hero.GetComponent<Health>();
            var movement=hero.GetComponent<HeroMovementInput>();
            Assert.That(chest.UsesPaintedArt,Is.True);
            var closed=renderer.sprite;
            Assert.That(closed.texture.isReadable,Is.False);
            Assert.That(closed.texture.width,Is.EqualTo(192));
            Assert.That(closed.texture.format,Is.EqualTo(TextureFormat.ASTC_4x4));
            health.ApplyDamage(new DamageContext(40));
            int events=0; chest.Opened+=(reward,position)=>events++;
            movement.Hold(Vector2.right);
            float deadline=Time.realtimeSinceStartup+5;
            while(!chest.IsOpen&&Time.realtimeSinceStartup<deadline)yield return null;
            movement.Release();
            Assert.That(chest.IsOpen,Is.True);
            Assert.That(events,Is.EqualTo(1));
            Assert.That(health.Current,Is.EqualTo(85f).Within(.001f));
            Assert.That(chest.IsOpening,Is.True);
            var opening=renderer.sprite;
            Assert.That(opening.name,Is.EqualTo("Chest Opening"));
            Time.timeScale=0;yield return new WaitForSecondsRealtime(.25f);
            Assert.That(renderer.sprite,Is.SameAs(opening));
            Assert.That(chest.IsOpening,Is.True);
            Time.timeScale=1;yield return new WaitForSeconds(.25f);
            Assert.That(renderer.sprite.name,Is.EqualTo("Chest Open"));
            Assert.That(chest.IsOpening,Is.False);
            Assert.That(events,Is.EqualTo(1));
            Assert.That(renderer.sprite.pivot,Is.EqualTo(closed.pivot));
            Assert.That(renderer.sprite.bounds.size,Is.EqualTo(closed.bounds.size));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ReloadBorrowsSpritesAndActorsSortAtGroundRoots()
        {
            var chest=GameObject.Find("Chest").GetComponent<SpriteRenderer>();
            var sprite=chest.sprite;var texture=sprite.texture;
            var group=chest.GetComponent<SortingGroup>();
            Assert.That(group.sortingOrder,Is.EqualTo(1));
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            Assert.That(hero.GetComponent<SortingGroup>().sortingOrder,Is.EqualTo(group.sortingOrder));
            var ring=Object.FindFirstObjectByType<AbilityRingView>().Ring.GetComponent<SortingGroup>();
            Assert.That(ring.sortAtRoot,Is.True);
            Assert.That(ring.sortingOrder,Is.LessThan(group.sortingOrder),"Ground ring must escape the hero group.");
            foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None))
            {
                var ground=shadow.GetComponent<SortingGroup>();
                Assert.That(ground.sortAtRoot,Is.True);
                Assert.That(ground.sortingOrder,Is.LessThan(group.sortingOrder));
            }
            foreach(var enemy in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
            {
                Assert.That(enemy.GetComponent<SortingGroup>().sortingOrder,Is.EqualTo(1));
                var bar=enemy.transform.Find("Health Bar").GetComponent<SortingGroup>();
                Assert.That(bar.sortAtRoot,Is.True);
                Assert.That(bar.sortingOrder,Is.GreaterThan(1));
            }
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            var next=GameObject.Find("Chest").GetComponent<SpriteRenderer>();
            Assert.That(sprite!=null&&texture!=null,Is.True,"Views must not destroy borrowed assets.");
            Assert.That(next.sprite,Is.SameAs(sprite));
            Assert.That(next.sprite.texture,Is.SameAs(texture));
            Assert.That(next.GetComponents<SortingGroup>().Length,Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
