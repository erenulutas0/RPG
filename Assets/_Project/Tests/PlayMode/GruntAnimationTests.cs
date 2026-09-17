using System.Collections;
using System.Collections.Generic;
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
    public sealed class GruntAnimationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private EnemyLookView _look;
        private EncounterController _encounter;
        private SpriteRenderer _body;

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.timeScale = 1; Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            _encounter = Object.FindFirstObjectByType<EncounterController>(); _encounter.enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.Look == EnemyLook.Grunt) { _look = look; break; }
            Assert.That(_look, Is.Not.Null); Assert.That(_look.PaintedArt, Is.Not.Null);
            _body = _look.transform.Find("Enemy Body").GetComponent<SpriteRenderer>();
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1; Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Grunt cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene); TestProfile.End();
        }

        [UnityTest]
        public IEnumerator SharedSixPoseSetUsesVisualTopAndSurvivesReload()
        {
            var set=_look.PaintedArt;Assert.That(set.HasPassingFrames,Is.True);
            Sprite first=set.GetFrame(0,false).Body;
            foreach(bool rear in new[]{false,true})for(int i=0;i<6;i++)
            {
                var frame=set.GetFrame(i,rear);
                Assert.That(frame.Body.rect.size,Is.EqualTo(new Vector2(180,180)));
                Assert.That(frame.Body.pivot,Is.EqualTo(new Vector2(90,18)));
                Assert.That(frame.Body.texture.isReadable,Is.False);
                Assert.That(frame.Flash.pivot,Is.EqualTo(frame.Body.pivot));
                Assert.That(set.FlashOf(frame.Body),Is.SameAs(frame.Flash));
            }
            int count=0;
            foreach(var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
            {
                if(look.Look!=EnemyLook.Grunt)continue;count++;
                Assert.That(look.PaintedArt,Is.SameAs(set));
                Assert.That(look.transform.Find("Health Bar").localPosition.y,Is.EqualTo(1.17f).Within(.0001f));
                Assert.That(look.transform.Find("Contact Shadow").localScale.x,Is.EqualTo(.78f).Within(.0001f));
            }
            Assert.That(count,Is.EqualTo(2));
            yield return SceneManager.LoadSceneAsync(ScenePath);yield return null;yield return Resources.UnloadUnusedAssets();
            Assert.That(first!=null&&first.texture!=null,Is.True);
            foreach(var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if(look.Look==EnemyLook.Grunt)Assert.That(look.PaintedArt,Is.SameAs(set));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TravelSelectsFourFacingsWithoutMirroringTheCombatRootAndPauseFreezesPose()
        {
            foreach (var direction in new[] { new Vector3(1, .5f), new Vector3(-1, .5f), new Vector3(1, -.5f), new Vector3(-1, -.5f) })
            {
                var frames = new HashSet<int>();
                for (int i = 0; i < 24; i++)
                {
                    _look.transform.position += direction * .04f; yield return null;
                    frames.Add(_look.PaintedFrame);
                }
                CollectionAssert.AreEquivalent(new[] { 1, 4, 2, 5 }, frames);
                Assert.That(_look.RearFacing, Is.EqualTo(direction.y > 0));
                Assert.That(_look.Mirrored, Is.EqualTo(direction.x > 0));
                Assert.That(_look.transform.localScale, Is.EqualTo(Vector3.one));
                Time.timeScale = 0; Sprite pose = _body.sprite;
                yield return new WaitForSecondsRealtime(.08f);
                Assert.That(_body.sprite, Is.SameAs(pose)); Time.timeScale = 1;
                yield return null;
                Assert.That(_look.PaintedFrame, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator ActualStrikeFacesItsTargetAndHitFlashUsesThatFrame()
        {
            var target = new GameObject("Grunt target").AddComponent<Health>(); target.Initialize(1000);
            target.transform.position = _look.transform.position + new Vector3(.1f, .05f);
            _look.GetComponent<Targeting>().SetCandidates(new[] { target });
            var attack = _look.GetComponent<AttackController>(); attack.enabled = true;
            for (int i = 0; i < 5 && attack.AttackCount == 0; i++) yield return null;
            Assert.That(target.Current, Is.LessThan(1000));
            Assert.That(_look.RearFacing && _look.Mirrored, Is.True);
            Assert.That(_look.PaintedFrame, Is.EqualTo(3)); attack.enabled = false;
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(1)); yield return null;
            Assert.That(_body.transform.Find("Flash").GetComponent<SpriteRenderer>().sprite, Is.SameAs(_look.SilhouetteOf(_body.sprite)));
            Assert.That(_body.transform.localScale.y, Is.LessThan(1));
            Object.Destroy(target.gameObject); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DeathRewardsImmediatelyAndPooledRemainsSurviveActorRemovalThenExpire()
        {
            var set = _look.PaintedArt; var setup = Object.FindFirstObjectByType<CombatSetup>();
            var health = _look.GetComponent<Health>(); int alive = _encounter.AliveEnemyCount;
            int before = setup.Run.Gold, reward = _encounter.GoldRewardOf(health);

            int children = GameObject.Find("Combat Effects").transform.childCount;
            health.ApplyDamage(new DamageContext(100000));
            Assert.That(_encounter.AliveEnemyCount, Is.EqualTo(alive - 1));
            Assert.That(setup.Run.Gold, Is.EqualTo(before + reward));
            Assert.That(_body.gameObject.activeSelf, Is.False);
            SpriteRenderer remains = null;
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite == set.Death[0]) remains = renderer;
            Assert.That(remains, Is.Not.Null);
            Object.Destroy(_look.gameObject); Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(remains.enabled, Is.True); Assert.That(remains.sprite, Is.SameAs(set.Death[0]));
            Time.timeScale = 1;
            for (int i = 0; i < 12; i++) yield return null;
            Assert.That(remains.sprite, Is.SameAs(set.Death[1]));
            for (int i = 0; i < 16; i++) yield return null;
            Assert.That(remains.enabled, Is.False);
            Assert.That(GameObject.Find("Combat Effects").transform.childCount, Is.EqualTo(children));
            Assert.That(set.Death[0] != null, Is.True); LogAssert.NoUnexpectedReceived();
        }
    }
}
