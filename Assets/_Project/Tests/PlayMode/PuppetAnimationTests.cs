using System.Collections;
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
    // The development cut-out puppet (docs/45 section 4 B) in the real scene: development/puppet.txt hands the hero to
    // PuppetAnimator with the Vanguard's painted parts (VanguardPuppetArtSet, on HeroLookView). Presentation only, like the
    // Vanguard's animation tests.
    public sealed class PuppetAnimationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private PuppetAnimator _puppet;
        private HeroLookView _look;
        private HeroMovementInput _movement;
        private AttackController _attack;
        private SpriteRenderer _body;
        private Health _target;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return LoadPuppet(null);
        }

        // A fresh profile folder holding the flag (TestProfile.Begin creates the development folder), then the scene.
        private IEnumerator LoadPuppet(PlayerProfile seed)
        {
            TestProfile.Begin(seed);
            File.WriteAllText(DevelopmentStart.PuppetPath, string.Empty);
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
            _puppet = Object.FindFirstObjectByType<PuppetAnimator>();
            Assert.That(_puppet, Is.Not.Null, "development/puppet.txt hands the hero to the puppet.");
            _look = _puppet.GetComponent<HeroLookView>();
            _movement = _puppet.GetComponent<HeroMovementInput>();
            _attack = _puppet.GetComponent<AttackController>();
            _body = _look.Body;
            _target = new GameObject("Puppet test target").AddComponent<Health>();
            _target.Initialize(100000);
            _target.transform.position = _puppet.transform.position + Vector3.right * .3f;
            _puppet.GetComponent<Targeting>().SetCandidates(new[] { _target });
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Puppet cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private static Transform BoneOf(PuppetRig rig, PuppetBone bone) => rig.Bone(rig.IndexOf(bone));

        [UnityTest]
        public IEnumerator TheFlagBuildsAPuppetBesideTheBodyWithTheWeaponsInItsFists()
        {
            Assert.That(_look.enabled, Is.False, "The look steps aside so the two never drive the same renderers.");
            Assert.That(_look.UsesPaintedArt, Is.False, "No painted Vanguard frames under the puppet.");
            Assert.That(_puppet.UsesPlaceholder, Is.False, "The scene's painted Vanguard parts, not the placeholders.");
            Assert.That(_puppet.Art.IsValid, Is.True);
            Assert.That(_puppet.Root.name, Is.EqualTo(PuppetAnimator.RootName));
            Assert.That(_puppet.Root.parent, Is.SameAs(_puppet.transform), "Beside the body, so it outlives the death hide.");
            Assert.That(_body.sprite, Is.Null, "The body draws nothing; CombatantView keeps its renderer.");
            Assert.That(_body.gameObject.activeInHierarchy, Is.True);
            Assert.That(_puppet.FrontFacing, Is.False);
            Assert.That(_puppet.RearRig.Root.activeSelf, Is.True);
            Assert.That(_puppet.FrontRig.Root.activeSelf, Is.False, "One facing shows at a time.");

            yield return null;
            PuppetRig rig = _puppet.ActiveRig;
            Assert.That(rig.PartCount, Is.EqualTo(_puppet.Art.Rear.Parts.Length));
            for (int i = 0; i < rig.PartCount; i++)
            {
                SpriteRenderer part = rig.Renderer(i);
                Assert.That(part.sprite, Is.SameAs(_puppet.Art.Rear.Parts[i].Sprite), part.name);
                Assert.That(part.sortingLayerID, Is.EqualTo(_body.sortingLayerID), part.name);
                Assert.That(part.sortingOrder, Is.EqualTo(_body.sortingOrder + _puppet.Art.Rear.Parts[i].Sorting), part.name);
            }
            var sword = GameObject.Find("Sword Placeholder").GetComponent<SpriteRenderer>();
            var shield = GameObject.Find("Shield Placeholder").GetComponent<SpriteRenderer>();
            Assert.That(Vector2.Distance(sword.transform.position, rig.RightGrip.position), Is.LessThan(1e-4f), "In the right fist.");
            Assert.That(Vector2.Distance(shield.transform.position, rig.LeftGrip.position), Is.LessThan(1e-4f), "In the left fist.");
            Assert.That(sword.sprite, Is.SameAs(_puppet.Art.Sword), "The painted sword.");
            int torso = rig.Renderer(rig.IndexOf(PuppetBone.Torso)).sortingOrder;
            // The painted gauntlet is on the forearm: the blade sits under it, so the fist closes over the grip, and in
            // front of the body.
            Assert.That(sword.sortingOrder, Is.InRange(torso + 1, rig.Renderer(_puppet.Art.Rear.RightHand).sortingOrder - 1),
                "Between the body and the gauntlet.");
            Assert.That(shield.sortingOrder, Is.LessThan(torso), "Seen from behind the shield is behind the body.");
            for (int i = 0; i < rig.PartCount; i++)
                Assert.That(rig.Renderer(i).sprite, Is.Not.Null, rig.Renderer(i).name);
            Assert.That(rig.Legs.Right.IsSet && rig.Legs.Left.IsSet, Is.True, "The bob stands on the painted legs.");
            Assert.That(_puppet.GaitInput.Legs.Right.KneeY, Is.EqualTo(rig.Legs.Right.KneeY));
            Assert.That(_attack.AttackCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WithoutTheFlagTheHeroKeepsItsPaintedFrames()
        {
            TestProfile.Begin();
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<PuppetAnimator>(), Is.Null);
            Assert.That(GameObject.Find(PuppetAnimator.RootName), Is.Null);
            var look = Object.FindFirstObjectByType<HeroLookView>();
            Assert.That(look.enabled, Is.True);
            Assert.That(look.UsesPaintedArt, Is.True, "The scene's imported Vanguard set, as before.");
            Assert.That(look.Body.sprite, Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WalkingSwingsTheLegsThroughTheStrideAndFacesTheTravel()
        {
            Transform thigh = BoneOf(_puppet.RearRig, PuppetBone.ThighLeft);
            float hipLow = float.MaxValue, hipHigh = float.MinValue, turnLow = float.MaxValue, turnHigh = float.MinValue;
            bool leftSwung = false, rightSwung = false;
            int askedAt = -1, shownAt = -1;
            float narrowest = 1f;
            Vector3 start = _puppet.transform.position;
            _movement.Hold(Vector2.right);
            // Out and back, the span the Vanguard's tests walk unblocked: over a stride and a half once the walk is full.
            for (int i = 0; i < 80; i++)
            {
                if (i == 40)
                {
                    Assert.That(_puppet.transform.position.x, Is.GreaterThan(start.x + .5f), "The hero walked.");
                    Assert.That(_puppet.Mirrored, Is.False);
                    Assert.That(_puppet.Root.localScale.x, Is.EqualTo(1f));
                    _movement.Hold(Vector2.left);
                }
                yield return null;
                // The reversal: the facing asked for flips at once, the one on show follows through a short turn,
                // narrowing the puppet and the body (so the weapons) together.
                if (i >= 40)
                {
                    if (askedAt < 0 && _puppet.Mirrored)
                    {
                        askedAt = i;
                        Assert.That(_puppet.ShownMirrored, Is.False, "The figure on show is still turning.");
                    }
                    if (shownAt < 0 && _puppet.ShownMirrored)
                        shownAt = i;
                    narrowest = Mathf.Min(narrowest, Mathf.Abs(_puppet.Root.localScale.x));
                    Assert.That(_body.transform.localScale, Is.EqualTo(_puppet.Root.localScale), "The weapons turn with it.");
                }
                if (i < 10 || (i >= 40 && i < 44)) continue;
                PuppetPose pose = _puppet.Pose;
                Assert.That(_puppet.IsWalking, Is.True, "frame " + i);
                hipLow = Mathf.Min(hipLow, pose.HipLeft);
                hipHigh = Mathf.Max(hipHigh, pose.HipLeft);
                float turn = Mathf.DeltaAngle(0f, thigh.localEulerAngles.z);
                turnLow = Mathf.Min(turnLow, turn);
                turnHigh = Mathf.Max(turnHigh, turn);
                leftSwung |= pose.KneeLeft > pose.KneeRight + 15f;
                rightSwung |= pose.KneeRight > pose.KneeLeft + 15f;
            }
            Assert.That(hipHigh - hipLow, Is.GreaterThan(30f), "The thighs swing through the stride.");
            Assert.That(turnHigh - turnLow, Is.GreaterThan(30f), "and the thigh's transform with them.");
            Assert.That(askedAt, Is.GreaterThan(0), "Leftward travel asks for the mirror.");
            Assert.That(shownAt - askedAt, Is.InRange(1, 4), "The figure on show swaps a few frames later, mid-turn.");
            Assert.That(narrowest, Is.LessThan(.6f), "through a narrow width.");
            Assert.That(leftSwung && rightSwung, Is.True, "Each foot swings through in turn.");
            Assert.That(_puppet.Mirrored, Is.True, "Leftward travel mirrors the puppet.");
            Assert.That(_puppet.Root.localScale.x, Is.EqualTo(-1f));
            Assert.That(_body.transform.localScale.x, Is.EqualTo(-1f), "and the body, so the weapons mirror with it.");
            Assert.That(_puppet.transform.localScale, Is.EqualTo(Vector3.one), "Only the presentation mirrors.");
            Assert.That(_puppet.FrontFacing, Is.False, "Level travel keeps the facing it had.");

            _movement.Hold(new Vector2(1f, -1f));
            for (int i = 0; i < 4; i++) yield return null;
            Assert.That(_puppet.FrontFacing && !_puppet.Mirrored, Is.True, "Travel toward the camera shows the front.");
            Assert.That(_puppet.FrontRig.Root.activeSelf && !_puppet.RearRig.Root.activeSelf, Is.True);
            Assert.That(_puppet.GaitInput.Legs.Right.KneeY, Is.EqualTo(_puppet.FrontRig.Legs.Right.KneeY),
                "The bob stands on the shown facing's legs.");
            _movement.Release();
            Assert.That(_attack.AttackCount, Is.Zero, "The puppet never attacks on its own.");
            LogAssert.NoUnexpectedReceived();
        }

        // The owner, 2026-10-26: an attack froze the Vanguard's legs and it glided. The puppet strikes with its arms only.
        [UnityTest]
        public IEnumerator AttackingWhileWalkingKeepsTheLegsWalking()
        {
            foreach (string id in new[] { "weapon_sword", "weapon_staff", "weapon_daggers" })
            {
                yield return LoadPuppet(new PlayerProfile(0, null, null, 0, new[] { id }, id));
                PuppetStrike expected = id == "weapon_staff" ? PuppetStrike.Cast
                    : id == "weapon_daggers" ? PuppetStrike.Thrust : PuppetStrike.Swing;
                _attack.enabled = true;
                int striking = 0, armsStrike = 0, attacks = _attack.AttackCount;
                float hipLow = float.MaxValue, hipHigh = float.MinValue;
                for (int i = 0; i < 160; i++)
                {
                    // Back and forth, so no wall stops the walk; the target walks beside the hero so the weapon keeps
                    // finding it.
                    if (i % 40 == 0) _movement.Hold((i / 40) % 2 == 0 ? Vector2.right : Vector2.left);
                    _target.transform.position = _puppet.transform.position + Vector3.up * .3f;
                    yield return null;
                    if (i % 40 < 10 || !_puppet.IsAttacking) continue;
                    striking++;
                    PuppetGaitInput input = _puppet.GaitInput;
                    PuppetGaitInput unarmed = input;
                    unarmed.Strike = PuppetStrike.None;
                    PuppetPose legs = PuppetGait.Evaluate(unarmed);
                    PuppetPose pose = _puppet.Pose;
                    string at = id + " at frame " + i;
                    Assert.That(input.Strike, Is.EqualTo(expected), at);
                    Assert.That(input.WalkWeight, Is.GreaterThan(.5f), at + ": it walks while it strikes.");
                    Assert.That(pose.HipLeft, Is.EqualTo(legs.HipLeft), at);
                    Assert.That(pose.HipRight, Is.EqualTo(legs.HipRight), at);
                    Assert.That(pose.KneeLeft, Is.EqualTo(legs.KneeLeft), at);
                    Assert.That(pose.KneeRight, Is.EqualTo(legs.KneeRight), at);
                    Assert.That(pose.Lift, Is.EqualTo(legs.Lift), at);
                    hipLow = Mathf.Min(hipLow, pose.HipLeft);
                    hipHigh = Mathf.Max(hipHigh, pose.HipLeft);
                    if (Mathf.Abs(pose.ShoulderRight - legs.ShoulderRight) > 5f || Mathf.Abs(pose.WristRight - legs.WristRight) > 5f)
                        armsStrike++;
                }
                _movement.Release();
                Assert.That(_attack.AttackCount, Is.GreaterThan(attacks + 1), id + " attacked on the move.");
                Assert.That(striking, Is.GreaterThan(0), id);
                Assert.That(armsStrike, Is.GreaterThan(0), id + ": the arms strike.");
                Assert.That(hipHigh - hipLow, Is.GreaterThan(5f), id + ": the legs kept striding through the strikes.");
                LogAssert.NoUnexpectedReceived();
            }
        }

        [UnityTest]
        public IEnumerator StandingStillSettlesIntoTheIdle()
        {
            Transform thigh = BoneOf(_puppet.RearRig, PuppetBone.ThighRight);
            _movement.Hold(Vector2.right);
            for (int i = 0; i < 30; i++) yield return null;
            Assert.That(_puppet.GaitInput.WalkWeight, Is.EqualTo(1f));
            _movement.Release();
            for (int i = 0; i < 20; i++) yield return null;
            Assert.That(_puppet.IsWalking, Is.False);
            Assert.That(_puppet.GaitInput.WalkWeight, Is.Zero, "The stride has eased out.");
            // The stop's lurch rocks the body and swings the cape; a second later both have settled.
            for (int i = 0; i < 60; i++) yield return null;

            PuppetPose rest = PuppetGait.Rest;
            for (int i = 0; i < 60; i++)
            {
                yield return null;
                PuppetPose pose = _puppet.Pose;
                Assert.That(pose, Is.EqualTo(PuppetGait.Evaluate(_puppet.GaitInput)), "The pose is its clocks'.");
                Assert.That(Mathf.Abs(pose.HipLeft - rest.HipLeft), Is.LessThan(1.5f), "Only the weight shift leans the legs.");
                Assert.That(Mathf.Abs(pose.HipRight - rest.HipRight), Is.LessThan(1.5f));
                Assert.That(pose.KneeLeft, Is.InRange(PuppetGait.KneeRest - .01f, PuppetGait.KneeRest + PuppetGait.WeightKnee + .2f));
                Assert.That(pose.KneeRight, Is.InRange(PuppetGait.KneeRest - .01f, PuppetGait.KneeRest + PuppetGait.WeightKnee + .2f));
                Assert.That(Mathf.Abs(pose.Lift), Is.LessThan(.004f), "The boots stay planted.");
                Assert.That(Mathf.Abs(pose.Lean), Is.LessThan(.2f), "The lurch has settled.");
                Assert.That(pose.Breath, Is.InRange(-PuppetGait.BreathLift, PuppetGait.BreathLift), "The chest breathes.");
                Assert.That(Mathf.Abs(pose.Shift), Is.LessThanOrEqualTo(PuppetGait.WeightShift + 1e-4f), "The weight shifts.");
                Assert.That(Mathf.Abs(pose.ShoulderRight - rest.ShoulderRight), Is.LessThanOrEqualTo(PuppetGait.BreathShoulder + .3f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, thigh.localEulerAngles.z)), Is.LessThan(1.5f), "The thigh hangs straight.");
            }
            LogAssert.NoUnexpectedReceived();
        }

        // The Forge Burst (AbilityController.Used): its shockwave leaves on the press, so the slam's impact is on screen
        // within two frames, then the hero rises with the weapon in front; all over the stride.
        [UnityTest]
        public IEnumerator TheAbilitySlamsDownAtOnceOverTheStride()
        {
            var ability = _puppet.GetComponent<AbilityController>();
            _movement.Hold(Vector2.right);
            for (int i = 0; i < 20; i++) yield return null;
            float before = _puppet.Pose.Lift;
            Assert.That(ability.TryUse(), Is.True);
            Assert.That(_puppet.IsCasting, Is.True, "The slam starts with the ability.");
            float highest = float.MinValue, lowest = 0f, hipLow = float.MaxValue, hipHigh = float.MinValue;
            int frames = Mathf.CeilToInt(PuppetGait.CastSeconds * 60f) + 2;
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                PuppetPose pose = _puppet.Pose;
                if (i == 1)
                    Assert.That(pose.Lift, Is.LessThan(before - .015f), "Two frames after the press the hero is in the slam.");
                highest = Mathf.Max(highest, pose.ShoulderRight);
                lowest = Mathf.Min(lowest, pose.Lift);
                hipLow = Mathf.Min(hipLow, pose.HipLeft);
                hipHigh = Mathf.Max(hipHigh, pose.HipLeft);
                Assert.That(_puppet.IsWalking, Is.True, "frame " + i);
            }
            _movement.Release();
            Assert.That(_puppet.IsCasting, Is.False, "and ends.");
            Assert.That(lowest, Is.LessThan(-.02f), "The slam crouched the hero.");
            Assert.That(highest, Is.LessThan(90f), "and it rose with the weapon in front, never back over the helmet.");
            Assert.That(hipHigh - hipLow, Is.GreaterThan(5f), "while its legs kept striding.");
            Assert.That(_attack.AttackCount, Is.Zero, "The puppet never attacks on its own.");
            LogAssert.NoUnexpectedReceived();
        }

        // The anticipation reads the weapon's cooldown, as VanguardAnimator's did: the sword rises before the hit.
        [UnityTest]
        public IEnumerator TheSwordIsDrawnBackBeforeEachStrikeLands()
        {
            _attack.enabled = true;
            int attacks = _attack.AttackCount;
            float drawn = 0f;
            int drawnStrikes = 0;
            for (int i = 0; i < 150; i++)
            {
                _target.transform.position = _puppet.transform.position + Vector3.right * .3f;
                yield return null;
                if (_attack.AttackCount > attacks)
                {
                    attacks = _attack.AttackCount;
                    Assert.That(_puppet.GaitInput.Strike, Is.EqualTo(PuppetStrike.Swing));
                    Assert.That(_puppet.GaitInput.WindupProgress, Is.Zero, "The strike spends the draw.");
                    if (drawn > .5f)
                        drawnStrikes++;
                    drawn = 0f;
                }
                else if (_puppet.GaitInput.Windup == PuppetStrike.Swing)
                    drawn = Mathf.Max(drawn, _puppet.GaitInput.WindupProgress);
            }
            Assert.That(drawnStrikes, Is.GreaterThan(0), "Every strike after the first was drawn back first.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CombatPauseFreezesThePuppetMidStride()
        {
            Transform thigh = BoneOf(_puppet.RearRig, PuppetBone.ThighLeft);
            var sword = GameObject.Find("Sword Placeholder").transform;
            _movement.Hold(Vector2.right);
            for (int i = 0; i < 20; i++) yield return null;
            Time.timeScale = 0;
            // This frame's late update may still carry the time it began with; from the next frame on time stands.
            yield return null;
            PuppetPose frozen = _puppet.Pose;
            Quaternion turn = thigh.localRotation;
            Vector3 held = sword.localPosition;
            Vector3 position = _puppet.transform.position;
            yield return new WaitForSecondsRealtime(.06f);
            Assert.That(_puppet.Pose, Is.EqualTo(frozen), "A paused combat freezes the pose.");
            Assert.That(thigh.localRotation, Is.EqualTo(turn));
            Assert.That(sword.localPosition, Is.EqualTo(held));
            Assert.That(_puppet.transform.position, Is.EqualTo(position));

            Time.timeScale = 1;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(_puppet.Pose.HipLeft, Is.Not.EqualTo(frozen.HipLeft), "and the stride carries on afterwards.");
            _movement.Release();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AHitFlinchesTheUpperBodyAndFlashesEveryPart()
        {
            PuppetPose rest = _puppet.Pose;
            var sword = GameObject.Find("Sword Placeholder").GetComponent<SpriteRenderer>();
            var shield = GameObject.Find("Shield Placeholder").GetComponent<SpriteRenderer>();
            Material swordMaterial = sword.sharedMaterial;
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(1));
            Assert.That(_puppet.GaitInput.Flinch, Is.EqualTo(1f), "The hit starts the flinch at once.");
            yield return null;
            PuppetRig rig = _puppet.ActiveRig;
            Assert.That(_puppet.Pose.Lean, Is.LessThan(rest.Lean - 5f), "The torso snaps back.");
            for (int i = 0; i < rig.PartCount; i++)
            {
                Assert.That(rig.Renderer(i).sprite, Is.SameAs(_puppet.Art.Rear.Parts[i].Flash), rig.Renderer(i).name);
                Assert.That(rig.Renderer(i).color, Is.EqualTo(Color.white));
            }
            // The weapons have no white copies: they flash through the silhouette material instead.
            foreach (SpriteRenderer weapon in new[] { sword, shield })
            {
                Assert.That(weapon.sharedMaterial.shader.name, Is.EqualTo("Cryptforge/Rendered Character Flash"), weapon.name);
                Assert.That(weapon.color, Is.EqualTo(Color.white), weapon.name);
            }
            for (int i = 0; i < 30; i++) yield return null;
            Assert.That(_puppet.GaitInput.Flinch, Is.Zero);
            Assert.That(_puppet.Pose.Lean, Is.EqualTo(rest.Lean).Within(1e-4f), "It recovers.");
            for (int i = 0; i < rig.PartCount; i++)
                Assert.That(rig.Renderer(i).sprite, Is.SameAs(_puppet.Art.Rear.Parts[i].Sprite), "The flash ends.");
            Assert.That(sword.sharedMaterial, Is.SameAs(swordMaterial), "and the weapons have their own material back.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DeathHidesTheWeaponsAndThePuppetCollapsesThenLiesStill()
        {
            var sword = GameObject.Find("Sword Placeholder");
            _movement.Hold(Vector2.right);
            for (int i = 0; i < 15; i++) yield return null;
            _look.GetComponent<Health>().ApplyDamage(new DamageContext(100000));
            yield return null;
            Assert.That(_body.gameObject.activeInHierarchy, Is.False, "CombatantView hides the body.");
            Assert.That(sword.activeInHierarchy, Is.False, "and the weapons under it.");
            Assert.That(_puppet.Root.gameObject.activeInHierarchy, Is.True, "The puppet stays to fall.");
            Assert.That(_puppet.IsDying, Is.True);
            _movement.Release();
            float previous = -1f;
            for (int i = 0; i < 60; i++)
            {
                yield return null;
                Assert.That(_puppet.Pose.Fall, Is.GreaterThanOrEqualTo(previous), "It only ever falls further.");
                previous = _puppet.Pose.Fall;
            }
            Assert.That(_puppet.Pose.Fall, Is.EqualTo(PuppetGait.FallAngle).Within(1e-3f));
            Transform fallen = _puppet.ActiveRig.Root.transform;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, fallen.localEulerAngles.z)), Is.EqualTo(PuppetGait.FallAngle).Within(.01f));
            Quaternion lying = fallen.localRotation;
            for (int i = 0; i < 10; i++) yield return null;
            Assert.That(fallen.localRotation, Is.EqualTo(lying), "Then it lies still.");
            Assert.That(_attack.AttackCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
