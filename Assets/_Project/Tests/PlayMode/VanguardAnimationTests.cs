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
        public IEnumerator FourFacingsFollowTravelRetainIdleAndMirrorOnlyPresentation()
        {
            Assert.That(_look.PaintedArt.HasFrontFrames, Is.True);
            foreach (var direction in new[] { new Vector2(1,1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(-1,-1) })
            {
                _movement.Hold(direction);
                for(int i=0;i<4;i++) yield return null;
                Assert.That(_look.FrontFacing, Is.EqualTo(direction.y<0));
                Assert.That(_look.Mirrored, Is.EqualTo(direction.x<0));
                Assert.That(_body.sprite, Is.SameAs(_look.PaintedArt.GetFrame(_look.PaintedFrame,direction.y<0).Body));
                Assert.That(_body.transform.localScale.x, Is.EqualTo(direction.x<0?-1:1));
                Assert.That(_look.transform.localScale, Is.EqualTo(Vector3.one));
                _movement.Release(); yield return null; yield return null;
                Assert.That(_look.FrontFacing, Is.EqualTo(direction.y<0));
                Assert.That(_look.Mirrored, Is.EqualTo(direction.x<0));
            }
            // A nearly horizontal direction must retain the last vertical facing instead of flickering.
            _movement.Hold(new Vector2(-1,.01f)); yield return null; yield return null;
            Assert.That(_look.FrontFacing, Is.True);
            Time.timeScale=0;
            _movement.Hold(Vector2.one);
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(_look.FrontFacing && _look.Mirrored, Is.True);
            Assert.That(_attack.AttackCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LethalStrikeFacesActualTargetAndFrontFlashSurvivesReload()
        {
            _target.transform.position=_look.transform.position+new Vector3(-.2f,-.2f,0);
            _target.ApplyDamage(new DamageContext(_target.Current-1));
            _attack.enabled=true;
            for(int i=0;i<4;i++) yield return null;
            Assert.That(_target.IsAlive, Is.False);
            Assert.That(_look.FrontFacing && _look.Mirrored, Is.True, "Do not reacquire after the lethal hit.");
            _attack.enabled=false;
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(1));
            yield return null;
            var flash=_body.transform.Find("Flash").GetComponent<SpriteRenderer>();
            Assert.That(flash.sprite, Is.SameAs(_look.PaintedArt.GetFrame(_look.PaintedFrame,true).Flash));
            Assert.That(flash.transform.lossyScale.x, Is.LessThan(0));
            var front=_look.PaintedArt.GetFrame(0,true).Body;
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            yield return Resources.UnloadUnusedAssets();
            var next=Object.FindFirstObjectByType<HeroLookView>();
            Assert.That(next.PaintedArt.GetFrame(0,true).Body, Is.SameAs(front));
            Assert.That(front.texture.isReadable, Is.False);
            LogAssert.NoUnexpectedReceived();
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
            Vector3 resting = _body.transform.localPosition;
            _attack.Attacked += () =>
            {
                strikes++;
                Assert.That(_look.PaintedFrame, Is.EqualTo(6), "The damage event shows the strike immediately.");
                Assert.That(_target.Current, Is.LessThan(_target.Maximum));
                Assert.That(_body.transform.localPosition, Is.EqualTo(resting), "Imported sword attacks keep floor contact.");
            };
            _attack.enabled = true;
            bool recovered = false, anticipated = false;
            for (int i = 0; i < 90 && strikes < 2; i++)
            {
                yield return null;
                recovered |= _look.PaintedFrame == 7;
                anticipated |= _look.PaintedFrame == 5;
                Assert.That(_body.transform.localPosition, Is.EqualTo(resting));
            }
            Assert.That(strikes, Is.EqualTo(2));
            Assert.That(_attack.AttackCount, Is.EqualTo(2));
            Assert.That(recovered && anticipated, Is.True, "Recovery and pre-hit windup must both be reachable.");
        }

        [UnityTest]
        public IEnumerator FrontSwordGripTracksEachPoseMirrorsFlashesAndHidesOnDeath()
        {
            var grip = _body.transform.Find("Sword Grip").GetComponent<SpriteRenderer>();
            _movement.Hold(new Vector2(-1,-1));
            yield return null; yield return null;
            for (int i=0;i<25;i++)
            {
                yield return null;
                var frame=_look.PaintedArt.GetFrame(_look.PaintedFrame,true);
                Assert.That(grip.enabled, Is.True);
                Assert.That(grip.sprite, Is.SameAs(frame.Grip));
                Assert.That(grip.transform.localPosition, Is.EqualTo((Vector3)frame.GripOffset));
                Assert.That(grip.transform.lossyScale.x, Is.LessThan(0));
                Assert.That(grip.sprite.texture.isReadable, Is.False);
            }
            _movement.Release(); yield return null; yield return null;
            _target.transform.position=_look.transform.position+new Vector3(-.2f,-.1f);
            _attack.enabled=true;
            var seen=new HashSet<int>();
            for(int i=0;i<80;i++)
            {
                yield return null; seen.Add(_look.PaintedFrame);
                Assert.That(grip.sprite, Is.SameAs(_look.PaintedArt.GetFrame(_look.PaintedFrame,true).Grip));
            }
            CollectionAssert.IsSubsetOf(new[]{5,6,7},seen);
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(1)); yield return null;
            Assert.That(_body.transform.Find("Flash").GetComponent<SpriteRenderer>().sortingOrder, Is.GreaterThan(grip.sortingOrder));
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(100000)); yield return null;
            Assert.That(grip.gameObject.activeInHierarchy, Is.False);
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
        public IEnumerator PaintedStaffKeepsGripAcrossFacingsAndCastAndSurvivesReload()
        {
            TestProfile.Begin(new PlayerProfile(0, null, null, 0, new[] { "weapon_staff" }, "weapon_staff"));
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null; Bind();
            var staff=GameObject.Find("Staff Shaft").GetComponent<SpriteRenderer>();
            var orb=GameObject.Find("Staff Orb").GetComponent<SpriteRenderer>();
            var sprite=_look.PaintedArt.Staff;
            Assert.That(sprite, Is.Not.Null);
            Assert.That(staff.sprite, Is.SameAs(sprite));
            Assert.That(orb.enabled, Is.False, "The whole imported staff already contains the crystal.");
            foreach(var direction in new[]{new Vector2(1,1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(-1,-1)})
            {
                _movement.Hold(direction);
                for(int i=0;i<12;i++)
                {
                    yield return null;
                    var frame=_look.PaintedArt.GetFrame(_look.PaintedFrame,_look.FrontFacing);
                    Assert.That(staff.transform.localPosition,Is.EqualTo((Vector3)frame.RightHand));
                    Assert.That(staff.sortingOrder,Is.EqualTo(_body.sortingOrder+(_look.FrontFacing?2:-1)));
                }
                _movement.Release(); yield return null; yield return null;
            }
            Assert.That(_body.transform.Find("Sword Grip").GetComponent<SpriteRenderer>().enabled,Is.True);
            _target.transform.position=_look.transform.position+new Vector3(-.2f,-.1f);
            _attack.enabled=true;
            for(int i=0;i<4;i++) yield return null;
            Assert.That(_attack.AttackCount,Is.EqualTo(1));
            Assert.That(Quaternion.Angle(staff.transform.localRotation,Quaternion.identity),Is.GreaterThan(1));
            var rotation=staff.transform.localRotation;
            Time.timeScale=0; yield return new WaitForSecondsRealtime(.05f);
            Assert.That(staff.transform.localRotation,Is.EqualTo(rotation));
            Assert.That(orb.enabled,Is.False);
            Time.timeScale=1; _attack.enabled=false;
            for(int i=0;i<20;i++) yield return null;
            Assert.That(staff.transform.localRotation,Is.EqualTo(Quaternion.identity));
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            yield return Resources.UnloadUnusedAssets();
            Assert.That(Object.FindFirstObjectByType<HeroLookView>().PaintedArt.Staff,Is.SameAs(sprite));
            Assert.That(sprite.texture.isReadable,Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PaintedDaggersKeepBothHandsThroughTravelCastPauseAndReload()
        {
            TestProfile.Begin(new PlayerProfile(0,null,null,0,new[]{"weapon_daggers"},"weapon_daggers"));
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null; Bind();
            var left=GameObject.Find("Dagger Left").GetComponent<SpriteRenderer>();
            var right=GameObject.Find("Dagger Right").GetComponent<SpriteRenderer>();
            var shared=_look.PaintedArt.Dagger;
            Assert.That(shared,Is.Not.Null);
            Assert.That(left.sprite,Is.SameAs(shared)); Assert.That(right.sprite,Is.SameAs(shared));
            foreach(var direction in new[]{new Vector2(1,1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(-1,-1)})
            {
                _movement.Hold(direction);
                for(int i=0;i<12;i++)
                {
                    yield return null;
                    var frame=_look.PaintedArt.GetFrame(_look.PaintedFrame,_look.FrontFacing);
                    Assert.That(left.transform.localPosition,Is.EqualTo((Vector3)frame.LeftHand));
                    Assert.That(right.transform.localPosition,Is.EqualTo((Vector3)frame.RightHand));
                    Assert.That(left.sortingOrder,Is.LessThan(_body.sortingOrder));
                    Assert.That(right.sortingOrder,Is.LessThan(_body.sortingOrder));
                }
                _movement.Release(); yield return null; yield return null;
            }
            _target.transform.position=_look.transform.position+new Vector3(-.2f,-.1f);
            _attack.enabled=true;
            for(int i=0;i<4;i++) yield return null;
            Assert.That(_attack.AttackCount,Is.EqualTo(1));
            Assert.That(_target.Current,Is.LessThan(_target.Maximum));
            var neutral=_look.PaintedArt.GetFrame(0,_look.FrontFacing);
            Assert.That(left.transform.localPosition,Is.EqualTo((Vector3)neutral.LeftHand));
            Assert.That(right.transform.localPosition,Is.EqualTo((Vector3)neutral.RightHand));
            Assert.That(Quaternion.Angle(right.transform.localRotation,Quaternion.identity),Is.GreaterThan(36));
            var rotation=right.transform.localRotation;
            Time.timeScale=0; yield return new WaitForSecondsRealtime(.05f);
            Assert.That(right.transform.localRotation,Is.EqualTo(rotation));
            _attack.enabled=false; Time.timeScale=1;
            for(int i=0;i<20;i++) yield return null;
            Assert.That(Quaternion.Angle(right.transform.localRotation,Quaternion.identity),Is.EqualTo(35).Within(.01));
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(100000)); yield return null;
            Assert.That(left.gameObject.activeInHierarchy,Is.False);
            Assert.That(right.gameObject.activeInHierarchy,Is.False);
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            yield return Resources.UnloadUnusedAssets();
            Assert.That(Object.FindFirstObjectByType<HeroLookView>().PaintedArt.Dagger,Is.SameAs(shared));
            Assert.That(shared.texture.isReadable,Is.False);
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
                Assert.That(_body.transform.Find("Sword Grip").GetComponent<SpriteRenderer>().enabled, Is.False);
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
