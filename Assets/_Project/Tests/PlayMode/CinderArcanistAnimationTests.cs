using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // Ilya Kest drawn from the pre-rendered art set (docs/45) in the real scene: development/hero.txt plays the Cinder
    // Arcanist, whose HeroDefinition carries a RenderedCharacterArtSet, and HeroLookView hands the hero to
    // RenderedCharacterAnimator. Presentation only, like the Vanguard's and the puppet's animation tests.
    //
    // The drawing only observes the hero's floor displacement, so these tests walk the hero by moving its transform a
    // known floor distance each frame, with HeroMovementInput switched off: no rim, pit wall or other obstacle round the
    // spawn can stop or turn a walk aside.
    //
    // Since 2026-10-29 Ilya's set draws all eight facings, so a west heading shows the west frames as rendered, never
    // mirrored; a set of only the five east facings (any other rendered set) still mirrors them for the west.
    public sealed class CinderArcanistAnimationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        // Floor units a second: the foundry's walking pace (HeroMovementInput, 2.5 x FoundryPace).
        private const float Speed = 3.5f;
        private RenderedCharacterAnimator _drawn;
        private HeroLookView _look;
        private AttackController _attack;
        private Health _hero;
        private Health _target;
        private RenderedCharacterArtSet _fiveFacings;

        private IEnumerator Load(bool arcanist = true, bool puppet = false)
        {
            TestProfile.Begin();
            if (arcanist)
                File.WriteAllText(DevelopmentStart.HeroPath, ArcanistId);
            if (puppet)
                File.WriteAllText(DevelopmentStart.PuppetPath, "on");
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            _look = Object.FindFirstObjectByType<HeroLookView>();
            _look.GetComponent<HeroMovementInput>().enabled = false;
            _drawn = _look.GetComponent<RenderedCharacterAnimator>();
            _attack = _look.GetComponent<AttackController>();
            _hero = _look.GetComponent<Health>();
            _target = new GameObject("Arcanist animation target").AddComponent<Health>();
            _target.Initialize(100000);
            _target.transform.position = _look.transform.position + Vector3.right * .3f;
            _look.GetComponent<Targeting>().SetCandidates(new[] { _target });
        }

        // One frame of walking along a floor direction at Speed: the step lands before this frame's LateUpdate, where
        // the drawing reads it. A paused combat (no scaled time) steps nowhere.
        private IEnumerator Step(Vector2 floorDirection)
        {
            Vector2 step = floorDirection.normalized * (Speed * Time.deltaTime);
            _look.transform.position += new Vector3(step.x, ArenaFloor.WorldY(step.y), 0f);
            yield return null;
        }

        // development/puppet.txt is for the Vanguard: a hero with its own rendered art keeps it.
        [UnityTest, Timeout(60000)]
        public IEnumerator ThePuppetFlagLeavesIlyaItsOwnArt()
        {
            yield return Load(true, true);
            Assert.That(_drawn, Is.Not.Null, "Ilya is drawn from its rendered art.");
            Assert.That(_look.GetComponent<PuppetAnimator>(), Is.Null, "The puppet never takes a hero with its own art.");
            Assert.That(_look.enabled, Is.False, "The painted view stepped aside.");
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            if (_fiveFacings != null)
                Object.Destroy(_fiveFacings);
            _fiveFacings = null;
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
            Assert.That(_drawn.Art.DrawsWest, Is.True, "All eight facings rendered.");
            Assert.That(_drawn.Player.WestDrawn, Is.True, "So the player never mirrors.");
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
            var walks = new (Vector2 steer, RenderedFacing facing, bool mirrored)[]
            {
                (Vector2.right, RenderedFacing.East, false), (Vector2.left, RenderedFacing.West, false),
                (Vector2.up, RenderedFacing.North, false), (Vector2.down, RenderedFacing.South, false),
                (new Vector2(1, 1), RenderedFacing.NorthEast, false), (new Vector2(-1, -1), RenderedFacing.SouthWest, false),
                (new Vector2(-1, 1), RenderedFacing.NorthWest, false), (new Vector2(1, -1), RenderedFacing.SouthEast, false)
            };
            int stride = _drawn.Art.FrameCount(RenderedMotion.Walk);
            foreach (var walk in walks)
            {
                // Two strides each way: every pose shows, drawn in the facing of the travel.
                var poses = new HashSet<int>();
                for (int i = 0; i < 40; i++)
                {
                    yield return Step(walk.steer);
                    string at = walk.steer + " frame " + i;
                    Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Walk), at);
                    Assert.That(_drawn.Facing, Is.EqualTo(walk.facing), at);
                    Assert.That(_drawn.Mirrored, Is.EqualTo(walk.mirrored), at);
                    Assert.That(_drawn.Root.localScale.x, Is.EqualTo(walk.mirrored ? -1f : 1f), at);
                    Assert.That(_drawn.Renderer.sprite, Is.SameAs(_drawn.Art.Frame(RenderedMotion.Walk, walk.facing,
                        _drawn.FrameIndex)), at);
                    Assert.That(_drawn.FrameIndex, Is.EqualTo(RenderedCharacterFrames.WalkFrame(_drawn.Player.WalkDistance,
                        _drawn.Art.StrideLength, stride)), at + ": the pose follows the distance walked.");
                    poses.Add(_drawn.FrameIndex);
                }
                Assert.That(poses.Count, Is.EqualTo(stride), walk.steer + ": every pose of the stride shows.");
            }
            for (int i = 0; i < 12; i++)
                yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle), "Standing idles.");
            Assert.That(_drawn.Facing, Is.EqualTo(RenderedFacing.SouthEast), "And keeps the facing it last walked in.");
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
            Assert.That(_drawn.Facing, Is.EqualTo(RenderedFacing.West), "Turned to the target on its left.");
            Assert.That(_drawn.Mirrored, Is.False, "As rendered: the staff stays in the right hand.");
            Assert.That(_drawn.Root.localScale.x, Is.EqualTo(1f));
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
                // The target walks beside the hero, so the staff keeps finding it.
                _target.transform.position = _look.transform.position + Vector3.up * .3f;
                bool east = (i / 40) % 2 == 0;
                yield return Step(east ? Vector2.right : Vector2.left);
                if (!_drawn.Player.IsStriking)
                    continue;
                walkingStrikes++;
                Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.WalkStrike), "frame " + i);
                Assert.That(_drawn.FrameIndex, Is.EqualTo(RenderedCharacterFrames.WalkFrame(_drawn.Player.WalkDistance,
                    _drawn.Art.StrideLength, _drawn.Art.FrameCount(RenderedMotion.WalkStrike))), "The legs are the walk's.");
                Assert.That(_drawn.Facing, Is.EqualTo(east ? RenderedFacing.East : RenderedFacing.West),
                    "Walking, the travel keeps the facing.");
                Assert.That(_drawn.Mirrored, Is.False, "The west walking strike is drawn, not mirrored.");
            }
            Assert.That(_attack.AttackCount, Is.GreaterThan(attacks + 1), "It attacked on the move.");
            Assert.That(walkingStrikes, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }

        // The mirrored west flipped the light and put the staff in the other hand. Walking, standing, striking and
        // casting to the west, the drawing shows the west frames as rendered and never mirrors.
        [UnityTest]
        public IEnumerator AWestHeadingShowsTheWestFramesUnmirrored()
        {
            yield return Load();
            RenderedCharacterArtSet art = _drawn.Art;
            var poses = new HashSet<int>();
            for (int i = 0; i < 40; i++)
            {
                yield return Step(Vector2.left);
                string at = "west frame " + i;
                Assert.That(_drawn.Facing, Is.EqualTo(RenderedFacing.West), at);
                Assert.That(_drawn.Mirrored, Is.False, at);
                Assert.That(_drawn.Root.localScale, Is.EqualTo(Vector3.one), at + ": nothing is flipped.");
                Assert.That(_drawn.Renderer.sprite, Is.SameAs(art.Frame(RenderedMotion.Walk, RenderedFacing.West,
                    _drawn.FrameIndex)), at);
                Assert.That(_drawn.Renderer.sprite, Is.Not.SameAs(art.Frame(RenderedMotion.Walk, RenderedFacing.East,
                    _drawn.FrameIndex)), at + ": its own drawing, not the east one.");
                Assert.That(_drawn.Renderer.flipX, Is.False, at);
                poses.Add(_drawn.FrameIndex);
            }
            Assert.That(poses.Count, Is.EqualTo(art.FrameCount(RenderedMotion.Walk)), "The west walk steps the whole stride.");
            for (int i = 0; i < 12; i++)
                yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle));
            Assert.That(_drawn.Renderer.sprite, Is.SameAs(art.Frame(RenderedMotion.Idle, RenderedFacing.West, _drawn.FrameIndex)),
                "Standing west idles in the west drawing.");

            var ability = _hero.GetComponent<AbilityController>();
            Assert.That(ability.TryUse(), Is.True, "The Vent is ready at the start.");
            for (int i = 0; i < 3; i++)
            {
                Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Cast));
                Assert.That(_drawn.Renderer.sprite, Is.SameAs(art.Frame(RenderedMotion.Cast, RenderedFacing.West,
                    _drawn.FrameIndex)), "The Vent cast to the west, frame " + i);
                Assert.That(_drawn.Root.localScale.x, Is.EqualTo(1f));
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        // Any set with only the five east facings - Ilya's until 2026-10-29 - is drawn exactly as before: the west
        // mirrors the east twin by flipping the drawing, and only the drawing.
        [UnityTest]
        public IEnumerator AFiveFacingSetStillMirrorsTheWest()
        {
            yield return Load();
            _fiveFacings = EastFacingsOf(_drawn.Art);
            Assert.That(_fiveFacings.IsValid && !_fiveFacings.DrawsWest, Is.True);
            Object.Destroy(_drawn.Root.gameObject);
            Object.Destroy(_drawn);
            yield return null;
            _drawn = RenderedCharacterAnimator.Attach(_look.gameObject, _look.Body, null, _attack, _fiveFacings);
            Assert.That(_drawn.Art, Is.SameAs(_fiveFacings));
            Assert.That(_drawn.Player.WestDrawn, Is.False);
            var walks = new (Vector2 steer, RenderedFacing facing, bool mirrored)[]
            {
                (Vector2.left, RenderedFacing.East, true), (new Vector2(-1, 1), RenderedFacing.NorthEast, true),
                (new Vector2(-1, -1), RenderedFacing.SouthEast, true), (Vector2.right, RenderedFacing.East, false)
            };
            foreach (var walk in walks)
                for (int i = 0; i < 20; i++)
                {
                    yield return Step(walk.steer);
                    string at = walk.steer + " frame " + i;
                    Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Walk), at);
                    Assert.That(_drawn.Facing, Is.EqualTo(walk.facing), at);
                    Assert.That(_drawn.Mirrored, Is.EqualTo(walk.mirrored), at);
                    Assert.That(_drawn.Root.localScale.x, Is.EqualTo(walk.mirrored ? -1f : 1f), at);
                    Assert.That(_drawn.Renderer.sprite, Is.SameAs(_fiveFacings.Frame(RenderedMotion.Walk, walk.facing,
                        _drawn.FrameIndex)), at);
                }
            Assert.That(_look.transform.localScale, Is.EqualTo(Vector3.one), "Only the drawing mirrors.");
            LogAssert.NoUnexpectedReceived();
        }

        // A runtime copy of a set with its five east facings only.
        private static RenderedCharacterArtSet EastFacingsOf(RenderedCharacterArtSet set)
        {
            const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
            var copy = ScriptableObject.CreateInstance<RenderedCharacterArtSet>();
            foreach (string field in new[] { "_strideLength", "_timings" })
                typeof(RenderedCharacterArtSet).GetField(field, Private).SetValue(copy,
                    typeof(RenderedCharacterArtSet).GetField(field, Private).GetValue(set));
            FieldInfo clips = typeof(RenderedCharacterArtSet).GetField("_clips", Private);
            clips.SetValue(copy, Array.FindAll((RenderedCharacterArtSet.Clip[])clips.GetValue(set),
                clip => !RenderedCharacterFrames.IsWest(clip.Facing)));
            return copy;
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
                Assert.That(_drawn.FlashRenderer.transform.parent, Is.SameAs(_drawn.Root), "It mirrors with the frame.");
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
            for (int i = 0; i < 15; i++)
                yield return Step(Vector2.right);
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Walk));
            Time.timeScale = 0;
            // This frame's late update may still carry the time it began with; from the next frame on time stands.
            yield return Step(Vector2.right);
            Sprite shown = _drawn.Renderer.sprite;
            int frame = _drawn.FrameIndex;
            Vector3 position = _look.transform.position;
            for (int i = 0; i < 6; i++)
                yield return Step(Vector2.right);
            Assert.That(_drawn.Renderer.sprite, Is.SameAs(shown), "A paused combat freezes the frame.");
            Assert.That(_drawn.FrameIndex, Is.EqualTo(frame));
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Walk), "Mid-stride, not dropped to the idle.");
            Assert.That(_look.transform.position, Is.EqualTo(position));
            Time.timeScale = 1;
            var after = new HashSet<int>();
            for (int i = 0; i < 12; i++)
            {
                yield return Step(Vector2.right);
                after.Add(_drawn.FrameIndex);
            }
            Assert.That(after.Count, Is.GreaterThan(1), "And the stride carries on afterwards.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
