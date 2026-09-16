using System.Collections;
using System.Collections.Generic;
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
    public sealed class VanguardAnimationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private HeroLookView _look;
        private HeroMovementInput _movement;
        private AttackController _attack;
        private SpriteRenderer _body;
        private Health _target;

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Bind();
        }

        private void Bind()
        {
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            _look = Object.FindFirstObjectByType<HeroLookView>();
            _movement = _look.GetComponent<HeroMovementInput>();
            _attack = _look.GetComponent<AttackController>();
            _body = GameObject.Find("Hero Body").GetComponent<SpriteRenderer>();
            _target = new GameObject("Animation test target").AddComponent<Health>();
            _target.Initialize(100000);
            _target.transform.position = _look.transform.position + Vector3.right * .3f;
            _look.GetComponent<Targeting>().SetCandidates(new[] { _target });
            Assert.That(_look.UsesPaintedArt, Is.True);
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Vanguard cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator MovementTraversesFourFramesStopsAndFreezesWithCombat()
        {
            var seen = new HashSet<int>();
            Vector3 start = _look.transform.position;
            _movement.Hold(Vector2.right);
            for (int i = 0; i < 36; i++) { yield return null; seen.Add(_look.PaintedFrame); }
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, seen);
            Assert.That(_look.transform.position.x, Is.GreaterThan(start.x));
            int frame = _look.PaintedFrame;
            Vector3 position = _look.transform.position;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.06f);
            Assert.That(_look.PaintedFrame, Is.EqualTo(frame));
            Assert.That(_look.transform.position, Is.EqualTo(position));
            _movement.Release(); Time.timeScale = 1;
            yield return null; yield return null;
            Assert.That(_look.PaintedFrame, Is.Zero, "Releasing movement returns to idle.");
            Assert.That(_attack.AttackCount, Is.Zero, "Animation must not attack on its own.");
        }

        [UnityTest]
        public IEnumerator SwordStrikeMatchesDamageThenRecoversAndAnticipatesNextHit()
        {
            int strikes = 0;
            _attack.Attacked += () =>
            {
                strikes++;
                Assert.That(_look.PaintedFrame, Is.EqualTo(6), "The damage event shows the strike immediately.");
                Assert.That(_target.Current, Is.LessThan(_target.Maximum));
            };
            _attack.enabled = true;
            bool recovered = false, anticipated = false;
            for (int i = 0; i < 90 && strikes < 2; i++)
            {
                yield return null;
                recovered |= _look.PaintedFrame == 7;
                anticipated |= _look.PaintedFrame == 5;
            }
            Assert.That(strikes, Is.EqualTo(2));
            Assert.That(_attack.AttackCount, Is.EqualTo(2));
            Assert.That(recovered && anticipated, Is.True, "Recovery and pre-hit windup must both be reachable.");
        }

        [UnityTest]
        public IEnumerator FlashTracksMovingBodyDeathHidesEquipmentAndReloadKeepsSharedAssets()
        {
            VanguardArtSet set = _look.PaintedArt;
            Sprite idle = set.GetFrame(0).Body;
            _movement.Hold(Vector2.right);
            yield return null;
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(1));
            var flash = _body.transform.Find("Flash").GetComponent<SpriteRenderer>();
            for (int i = 0; i < 3; i++)
            {
                yield return null;
                Assert.That(flash.sprite, Is.SameAs(_look.SilhouetteOf(_body.sprite)));
            }
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(100000));
            yield return null;
            Assert.That(_body.gameObject.activeInHierarchy, Is.False);
            Assert.That(_look.GetComponentInChildren<ContactShadowView>().Renderer.enabled, Is.False);
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            yield return Resources.UnloadUnusedAssets();
            var next = Object.FindFirstObjectByType<HeroLookView>();
            Assert.That(next.PaintedArt, Is.SameAs(set));
            Assert.That(next.PaintedArt.GetFrame(0).Body, Is.SameAs(idle));
            Assert.That(idle.texture.isReadable, Is.False, "Runtime cannot rewrite shared imported pixels.");
            Assert.That(GameObject.Find("Hero Body").activeInHierarchy, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OwnedStaffAndDaggersAttackAndWalkWithTheImportedBody()
        {
            foreach (string id in new[] { "weapon_staff", "weapon_daggers" })
            {
                TestProfile.Begin(new PlayerProfile(0, null, null, 0, new[] { id }, id));
                yield return SceneManager.LoadSceneAsync(ScenePath);
                yield return null;
                Bind();
                _attack.enabled = true;
                for (int i = 0; i < 4; i++) yield return null;
                Assert.That(_attack.AttackCount, Is.EqualTo(1));
                Assert.That(_target.Current, Is.LessThan(_target.Maximum));
                Assert.That(_look.IsSwinging, Is.True);
                Assert.That(_look.PaintedFrame, Is.Zero, "Non-sword attacks keep the appropriate neutral arm pose.");
                _attack.enabled = false;
                _movement.Hold(Vector2.right);
                bool walked = false;
                for (int i = 0; i < 30; i++) { yield return null; walked |= _look.PaintedFrame >= 1 && _look.PaintedFrame <= 4; }
                Assert.That(walked, Is.True);
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}
