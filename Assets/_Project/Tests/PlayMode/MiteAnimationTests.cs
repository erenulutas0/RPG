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
    public sealed class MiteAnimationTests
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
                if (look.Look == EnemyLook.Mite) { _look = look; break; }
            Assert.That(_look, Is.Not.Null); Assert.That(_look.PaintedArt, Is.Not.Null);
            _body = _look.transform.Find("Enemy Body").GetComponent<SpriteRenderer>();
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1; Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Mite cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene); TestProfile.End();
        }

        [UnityTest]
        public IEnumerator CrowdBorrowsRegisteredFramesAndReloadDoesNotDestroyThem()
        {
            var set = _look.PaintedArt; int mites = 0;
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
            {
                if (look.Look != EnemyLook.Mite) { Assert.That(look.PaintedArt, Is.Null); continue; }
                mites++; Assert.That(look.PaintedArt, Is.SameAs(set));
                Assert.That(look.transform.Find("Health Bar/Fill").GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
            }
            Assert.That(mites, Is.EqualTo(8));
            Sprite first = set.GetFrame(0, false).Body;
            foreach (bool rear in new[] { false, true }) for (int i = 0; i < 4; i++)
            {
                var frame = set.GetFrame(i, rear);
                Assert.That(frame.Body.rect.size, Is.EqualTo(new Vector2(80, 68)));
                Assert.That(frame.Body.pivot, Is.EqualTo(first.pivot));
                Assert.That(frame.Flash.pivot, Is.EqualTo(first.pivot));
                Assert.That(frame.Body.texture.isReadable, Is.False);
                Assert.That(frame.Body.pixelsPerUnit, Is.EqualTo(128));
            }
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            yield return Resources.UnloadUnusedAssets();
            Assert.That(first != null && first.texture != null, Is.True);
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.Look == EnemyLook.Mite) Assert.That(look.PaintedArt, Is.SameAs(set));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CompactBarKeepsItsLeftEdgeAndHealthIndependentOfItsBody()
        {
            var bar=_look.transform.Find("Health Bar");
            var fill=bar.Find("Fill").GetComponent<SpriteRenderer>();
            float width=bar.Find("Back").GetComponent<SpriteRenderer>().bounds.size.x;
            Assert.That(width, Is.EqualTo(26f/32*.65f).Within(.00001f));
            float left=fill.bounds.min.x;
            var health=_look.GetComponent<Health>(); health.ApplyDamage(new DamageContext(health.Maximum*.5f));
            yield return null;
            Assert.That(fill.bounds.min.x, Is.EqualTo(left).Within(.00001f));
            Assert.That(fill.transform.localScale.x, Is.EqualTo(.5f));
            Assert.That(bar.localScale, Is.EqualTo(new Vector3(.65f,.8f,1)));
            foreach(var other in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if(other.Look!=EnemyLook.Mite) Assert.That(other.transform.Find("Health Bar").localScale, Is.EqualTo(Vector3.one));
            health.ApplyDamage(new DamageContext(health.Current)); yield return null;
            Assert.That(bar.gameObject.activeSelf, Is.False);
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
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, frames);
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
            var target = new GameObject("Mite target").AddComponent<Health>(); target.Initialize(1000);
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
            int experience = setup.Run.Experience;
            int children = GameObject.Find("Combat Effects").transform.childCount;
            health.ApplyDamage(new DamageContext(100000));
            Assert.That(_encounter.AliveEnemyCount, Is.EqualTo(alive - 1));
            Assert.That(setup.Run.Gold, Is.EqualTo(before + reward));
            Assert.That(setup.Run.Experience, Is.EqualTo(experience + 1), "A Mite pays XP immediately, even though its gold reward is zero.");
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

        [UnityTest]
        public IEnumerator SwordAndSparkBorrowImportedArtAndPoolReuseResetsRotationAndTint()
        {
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            Assert.That(effects.UsesPaintedStrikes, Is.True);
            var hero = Object.FindFirstObjectByType<HeroLookView>(); var attack = hero.GetComponent<AttackController>();
            // Hit a living high-health actor so the weapon VFX uses the actual target's position.
            var target = _encounter.WaveEnemyAt(0); target.transform.position = hero.transform.position + Vector3.left * .3f;
            hero.GetComponent<Targeting>().SetCandidates(new[] { target }); attack.enabled = true;
            for (int i = 0; i < 4; i++) yield return null;
            attack.enabled = false; Sprite slash = null;
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite.name.StartsWith("VFX_slash_"))
                { slash = renderer.sprite; Assert.That(Quaternion.Angle(renderer.transform.localRotation, Quaternion.Euler(0, 0, 135)), Is.LessThan(.01f)); }
            Assert.That(slash, Is.Not.Null);
            for (int i = 0; i < 26; i++) _look.GetComponent<Health>().ApplyDamage(new DamageContext(.001f));
            int sparks = 0;
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite.name.StartsWith("VFX_spark_"))
                { sparks++; Assert.That(renderer.transform.localRotation, Is.EqualTo(Quaternion.identity)); Assert.That(renderer.color, Is.EqualTo(Color.white)); Assert.That(renderer.flipX, Is.False); }
            Assert.That(sparks, Is.EqualTo(24));
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            yield return Resources.UnloadUnusedAssets();
            Assert.That(slash != null && slash.texture != null, Is.True); LogAssert.NoUnexpectedReceived();
        }
    }
}
