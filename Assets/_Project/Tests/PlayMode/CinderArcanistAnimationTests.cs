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
    // Ilya Kest drawn from the pre-rendered art set (docs/45) in the real scene: development/hero.txt plays the Cinder
    // Arcanist, whose HeroDefinition carries a RenderedCharacterArtSet, and HeroLookView hands the hero to
    // RenderedCharacterAnimator. Presentation only, like the Vanguard's and the puppet's animation tests.
    public sealed class CinderArcanistAnimationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        private RenderedCharacterAnimator _drawn;
        private HeroLookView _look;
        private HeroMovementInput _movement;
        private AttackController _attack;
        private Health _hero;
        private Health _target;

        private IEnumerator Load(bool arcanist = true)
        {
            TestProfile.Begin();
            if (arcanist)
                File.WriteAllText(DevelopmentStart.HeroPath, ArcanistId);
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            _look = Object.FindFirstObjectByType<HeroLookView>();
            _drawn = _look.GetComponent<RenderedCharacterAnimator>();
            _movement = _look.GetComponent<HeroMovementInput>();
            _attack = _look.GetComponent<AttackController>();
            _hero = _look.GetComponent<Health>();
            _target = new GameObject("Arcanist animation target").AddComponent<Health>();
            _target.Initialize(100000);
            _target.transform.position = _look.transform.position + Vector3.right * .3f;
            _look.GetComponent<Targeting>().SetCandidates(new[] { _target });
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Arcanist animation cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator TheArcanistIsDrawnFromItsRenderedSetAndThePaintedLookStepsAside()
        {
            yield return Load();
            Assert.That(_drawn, Is.Not.Null, "The Arcanist's rendered set takes the hero over.");
            Assert.That(_look.enabled, Is.False, "The painted Vanguard look steps aside.");
            Assert.That(_drawn.Art, Is.SameAs(Object.FindFirstObjectByType<CombatSetup>().Hero.RenderedArt));
            Assert.That(_drawn.Art.IsValid, Is.True);
            Assert.That(_drawn.Root.name, Is.EqualTo(RenderedCharacterAnimator.RootName));
            Assert.That(_drawn.Root.parent, Is.SameAs(_look.transform), "Beside the body, so it outlives the death hide.");
            Assert.That(_look.Body.sprite, Is.Null, "The body draws nothing; CombatantView keeps its renderer.");
            Assert.That(_look.Body.gameObject.activeInHierarchy, Is.True);
            yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle));
            Assert.That(_drawn.Renderer.sprite, Is.SameAs(_drawn.Art.Frame(RenderedMotion.Idle, _drawn.Facing, _drawn.FrameIndex)));
            Assert.That(_drawn.Renderer.sortingLayerID, Is.EqualTo(_look.Body.sortingLayerID));
            Assert.That(_drawn.Renderer.sprite.texture.isReadable, Is.False, "Shared imported pixels.");
            // The staff is part of the render: nothing under the body draws, the carried loadout included.
            foreach (SpriteRenderer part in _look.Body.GetComponentsInChildren<SpriteRenderer>())
                Assert.That(part.enabled && part.sprite != null, Is.False, part.name + " still draws.");
            Assert.That(_attack.AttackCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheVanguardKeepsItsPaintedFrames()
        {
            yield return Load(false);
            Assert.That(_drawn, Is.Null);
            Assert.That(GameObject.Find(RenderedCharacterAnimator.RootName), Is.Null);
            Assert.That(_look.enabled, Is.True);
            Assert.That(_look.UsesPaintedArt, Is.True, "The scene's imported Vanguard set, as before.");
            Assert.That(_look.Body.sprite, Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WalkingTurnsTheDrawingToTheTravelAndStepsThroughTheStride()
        {
            yield return Load();
            // Out and back on each axis, so no wall stops the walk.
            var walks = new (Vector2 steer, RenderedFacing facing, bool mirrored)[]
            {
                (Vector2.right, RenderedFacing.East, false), (Vector2.left, RenderedFacing.East, true),
                (Vector2.up, RenderedFacing.North, false), (Vector2.down, RenderedFacing.South, false),
                (new Vector2(1, 1), RenderedFacing.NorthEast, false), (new Vector2(-1, -1), RenderedFacing.SouthEast, true)
            };
            foreach (var walk in walks)
            {
                _movement.Hold(walk.steer);
                var poses = new HashSet<int>();
                for (int i = 0; i < 30; i++)
                {
                    yield return null;
                    if (i < 3)
                        continue;
                    string at = walk.steer + " frame " + i;
                    Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Walk), at);
                    Assert.That(_drawn.Facing, Is.EqualTo(walk.facing), at);
                    Assert.That(_drawn.Mirrored, Is.EqualTo(walk.mirrored), at);
                    Assert.That(_drawn.Root.localScale.x, Is.EqualTo(walk.mirrored ? -1f : 1f), at);
                    Assert.That(_drawn.Renderer.sprite, Is.SameAs(_drawn.Art.Frame(RenderedMotion.Walk, walk.facing,
                        _drawn.FrameIndex)), at);
                    poses.Add(_drawn.FrameIndex);
                }
                Assert.That(poses.Count, Is.GreaterThanOrEqualTo(5), walk.steer + ": the stride steps through its poses.");
                _movement.Release();
                for (int i = 0; i < 12; i++)
                    yield return null;
                Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle), walk.steer + ": standing idles.");
                Assert.That(_drawn.Facing, Is.EqualTo(walk.facing), walk.steer + ": and keeps the facing it walked in.");
            }
            Assert.That(_look.transform.localScale, Is.EqualTo(Vector3.one), "Only the drawing mirrors.");
            Assert.That(_attack.AttackCount, Is.Zero, "The drawing never attacks.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AStandingStrikeShowsAtTheHitFacesTheTargetAndPlaysThrough()
        {
            yield return Load();
            _target.transform.position = _look.transform.position + new Vector3(-.3f, 0f, 0f);
            int strikes = 0;
            RenderedMotion atTheHit = RenderedMotion.Idle;
            // The drawing subscribed first, so by this handler it has already taken the strike.
            _attack.Struck += target => { strikes++; atTheHit = _drawn.Motion; };
            _attack.enabled = true;
            var poses = new HashSet<int>();
            for (int i = 0; i < 90 && strikes < 2; i++)
            {
                yield return null;
                if (_drawn.Motion == RenderedMotion.Strike)
                    poses.Add(_drawn.FrameIndex);
            }
            _attack.enabled = false;
            Assert.That(strikes, Is.GreaterThan(0));
            Assert.That(atTheHit, Is.EqualTo(RenderedMotion.Strike), "The damage has landed: the strike shows at once.");
            Assert.That(poses.Count, Is.GreaterThanOrEqualTo(3), "The strike plays through its frames.");
            Assert.That(_drawn.Facing, Is.EqualTo(RenderedFacing.East));
            Assert.That(_drawn.Mirrored, Is.True, "Turned to the target on its left.");
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle));
            LogAssert.NoUnexpectedReceived();
        }

        // The owner, 2026-10-26: attacks froze the Vanguard's legs and it glided. Walking, Ilya's strikes keep the walk's
        // pose for the distance walked, with the strike's arms (the walk strike).
        [UnityTest]
        public IEnumerator AWalkingStrikeKeepsTheStride()
        {
            yield return Load();
            _attack.enabled = true;
            int walkingStrikes = 0, attacks = _attack.AttackCount;
            for (int i = 0; i < 160; i++)
            {
                if (i % 40 == 0)
                    _movement.Hold((i / 40) % 2 == 0 ? Vector2.right : Vector2.left);
                _target.transform.position = _look.transform.position + Vector3.up * .3f;
                yield return null;
                if (i % 40 < 6 || !_drawn.Player.IsStriking)
                    continue;
                walkingStrikes++;
                Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.WalkStrike), "frame " + i);
                Assert.That(_drawn.FrameIndex, Is.EqualTo(RenderedCharacterFrames.WalkFrame(_drawn.Player.WalkDistance,
                    _drawn.Art.StrideLength, _drawn.Art.FrameCount(RenderedMotion.WalkStrike))), "The legs are the walk's.");
                Assert.That(_drawn.Facing, Is.EqualTo(RenderedFacing.East), "Walking, the travel keeps the facing.");
            }
            _movement.Release();
            Assert.That(_attack.AttackCount, Is.GreaterThan(attacks + 1), "It attacked on the move.");
            Assert.That(walkingStrikes, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheCrucibleVentPlaysTheCastWhole()
        {
            yield return Load();
            yield return null;
            var ability = _hero.GetComponent<AbilityController>();
            Assert.That(ability.TryUse(), Is.True, "The Vent is ready at the start.");
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Cast));
            var poses = new HashSet<int> { _drawn.FrameIndex };
            for (int i = 0; i < 40; i++)
            {
                yield return null;
                if (_drawn.Motion == RenderedMotion.Cast)
                    poses.Add(_drawn.FrameIndex);
            }
            Assert.That(poses.Count, Is.EqualTo(_drawn.Art.FrameCount(RenderedMotion.Cast)), "Every cast frame shows.");
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AHitFlinchesAndFlashesThenDeathPlaysOnceAndLiesStill()
        {
            yield return Load();
            yield return null;
            _hero.ApplyDamage(new DamageContext(1));
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Hit), "A standing hero flinches at the hit.");
            yield return null;
            Assert.That(_drawn.IsFlashing, Is.True, "And flashes while CombatantView does.");
            if (_drawn.FlashRenderer != null)
            {
                Assert.That(_drawn.FlashRenderer.sprite, Is.SameAs(_drawn.Renderer.sprite), "The shown frame's silhouette.");
                Assert.That(_drawn.FlashRenderer.sortingOrder, Is.GreaterThan(_drawn.Renderer.sortingOrder));
                Assert.That(_drawn.FlashRenderer.transform.lossyScale.x * _drawn.Root.lossyScale.x, Is.GreaterThan(0f),
                    "It mirrors with the frame.");
            }
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle));
            Assert.That(_drawn.IsFlashing, Is.False);

            _hero.ApplyDamage(new DamageContext(100000));
            yield return null;
            Assert.That(_look.Body.gameObject.activeInHierarchy, Is.False, "CombatantView hides the body.");
            Assert.That(_drawn.Root.gameObject.activeInHierarchy, Is.True, "The drawing stays to fall.");
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Death));
            int last = -1;
            for (int i = 0; i < 60; i++)
            {
                yield return null;
                Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Death));
                Assert.That(_drawn.FrameIndex, Is.GreaterThanOrEqualTo(last), "It only ever falls further.");
                last = _drawn.FrameIndex;
            }
            Assert.That(last, Is.EqualTo(_drawn.Art.FrameCount(RenderedMotion.Death) - 1), "Then it lies still.");
            Assert.That(_drawn.Renderer.sprite, Is.SameAs(_drawn.Art.Frame(RenderedMotion.Death, _drawn.Facing, last)));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator APausedCombatFreezesTheDrawingMidStride()
        {
            yield return Load();
            _movement.Hold(Vector2.right);
            for (int i = 0; i < 15; i++)
                yield return null;
            Time.timeScale = 0;
            // This frame's late update may still carry the time it began with; from the next frame on time stands.
            yield return null;
            Sprite shown = _drawn.Renderer.sprite;
            int frame = _drawn.FrameIndex;
            RenderedMotion motion = _drawn.Motion;
            Vector3 position = _look.transform.position;
            yield return new WaitForSecondsRealtime(.06f);
            Assert.That(_drawn.Renderer.sprite, Is.SameAs(shown), "A paused combat freezes the frame.");
            Assert.That(_drawn.FrameIndex, Is.EqualTo(frame));
            Assert.That(_drawn.Motion, Is.EqualTo(motion));
            Assert.That(_look.transform.position, Is.EqualTo(position));
            Time.timeScale = 1;
            var after = new HashSet<int>();
            for (int i = 0; i < 12; i++)
            {
                yield return null;
                after.Add(_drawn.FrameIndex);
            }
            Assert.That(after.Count, Is.GreaterThan(1), "And the stride carries on afterwards.");
            _movement.Release();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
