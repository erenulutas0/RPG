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
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The Vanguard drawn from its Blender-rendered set (docs/08, the owner 2026-11-22: it replaces the painted frames and
    // the cut-out puppet) in the real scene, as CinderArcanistAnimationTests follows Ilya: HeroLookView hands the hero to
    // RenderedCharacterAnimator, every motion plays from the set in the facing it is drawn in, the Shield Rush plays the
    // set's dash, and the Vanguard always starts with its sword and buckler (the owner, 2026-11-22) - the weapon is in
    // the render. Presentation only: the drawing never moves the hero, ticks a weapon or deals damage.
    //
    // As in Ilya's tests the hero is walked by moving its transform a known floor distance a frame, HeroMovementInput
    // off, so nothing round the spawn can turn a walk aside; the Rush's case switches it back on, since the dash is the
    // movement's.
    public sealed class VanguardRenderedAnimationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // Floor units a second: the foundry's walking pace (HeroMovementInput, 2.5 x FoundryPace).
        private const float Speed = 3.5f;
        private CombatSetup _setup;
        private RenderedCharacterAnimator _drawn;
        private HeroLookView _look;
        private AttackController _attack;
        private Health _hero;
        private Health _target;

        // skills: development/skills.txt ("3" carries the Rush); profile: a seed whose weapon the Vanguard must not start
        // with.
        private IEnumerator Load(string skills = null, PlayerProfile profile = null)
        {
            TestProfile.Begin(profile);
            if (skills != null)
            {
                File.WriteAllText(DevelopmentStart.SkillsPath, skills);
                // The development file's skills are said in the log (CombatSetup).
                UnityEngine.TestTools.LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"^\[Skills\] the run carries"));
            }
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _look = Object.FindFirstObjectByType<HeroLookView>();
            _look.GetComponent<HeroMovementInput>().enabled = false;
            _drawn = _look.GetComponent<RenderedCharacterAnimator>();
            _attack = _look.GetComponent<AttackController>();
            _hero = _look.GetComponent<Health>();
            _target = new GameObject("Vanguard animation target").AddComponent<Health>();
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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Vanguard rendered animation cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // One frame of walking along a floor direction at Speed, landing before this frame's LateUpdate.
        private IEnumerator Step(Vector2 floorDirection)
        {
            Vector2 step = floorDirection.normalized * (Speed * Time.deltaTime);
            _look.transform.position += new Vector3(step.x, ArenaFloor.WorldY(step.y), 0f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheVanguardIsDrawnFromItsRenderedSetWithItsSwordInTheRender()
        {
            yield return Load();
            Assert.That(_setup.Hero.Id, Is.EqualTo(HeroLookView.VanguardId));
            Assert.That(_drawn, Is.Not.Null, "The Vanguard's rendered set takes the hero over.");
            Assert.That(_look.enabled, Is.False, "The painted look steps aside.");
            Assert.That(_look.GetComponent<PuppetAnimator>(), Is.Null, "And no puppet.");
            Assert.That(_look.UsesPaintedArt, Is.False);
            Assert.That(_drawn.Art, Is.SameAs(_setup.Hero.RenderedArt));
            Assert.That(_drawn.Art.IsValid && _drawn.Art.DrawsWest, Is.True, "All eight facings rendered.");
            Assert.That(_drawn.Player.WestDrawn, Is.True, "Never mirrored: the sword stays in the right hand.");
            Assert.That(_drawn.Player.DrawsDash, Is.True, "The Shield Rush is drawn.");
            Assert.That(_drawn.Root.parent, Is.SameAs(_look.transform), "Beside the body, so it outlives the death hide.");
            Assert.That(_look.Body.sprite, Is.Null, "The body draws nothing; CombatantView keeps its renderer.");
            yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle));
            Assert.That(_drawn.Renderer.sprite, Is.SameAs(_drawn.Art.Frame(RenderedMotion.Idle, _drawn.Facing, _drawn.FrameIndex)));
            Assert.That(_drawn.Renderer.sprite.texture.name, Does.StartWith("CHR_VanguardBlender_"));
            Assert.That(_drawn.Renderer.sortingLayerID, Is.EqualTo(_look.Body.sortingLayerID));
            // The sword and buckler are in the render: no loadout under the body draws.
            foreach (SpriteRenderer part in _look.Body.GetComponentsInChildren<SpriteRenderer>())
                Assert.That(part.enabled && part.sprite != null, Is.False, part.name + " still draws.");
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_sword"));
            // The idle breathes through its frames.
            var poses = new HashSet<int>();
            for (int i = 0; i < 100; i++)
            {
                yield return null;
                Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle));
                poses.Add(_drawn.FrameIndex);
            }
            Assert.That(poses.Count, Is.EqualTo(_drawn.Art.FrameCount(RenderedMotion.Idle)), "Every idle frame shows.");
            Assert.That(_attack.AttackCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        // The owner, 2026-11-22: the Vanguard always starts with its sword and buckler, as Ilya with its Staff, whatever
        // weapon an older profile holds equipped; the forge's rack offers it the Sword alone.
        [UnityTest]
        public IEnumerator TheVanguardStartsWithItsSwordWhateverTheProfileEquips()
        {
            yield return Load(null, new PlayerProfile(0, null, null, 0, new[] { "weapon_daggers" }, "weapon_daggers"));
            Assert.That(_setup.Hero.Id, Is.EqualTo(HeroLookView.VanguardId));
            Assert.That(_setup.Hero.StartsWithItsOwnWeaponOnly, Is.True);
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_sword"));
            Assert.That(_setup.Weapons.OnlyStartingWeapon, Is.True);
            Assert.That(_setup.Weapons.Equipped.Id, Is.EqualTo("weapon_sword"));
            Assert.That(_setup.Weapons.NextUnlock, Is.Null, "Nothing on its rack to forge toward.");
            Assert.That(TestProfile.ReadSaved().EquippedWeaponId, Is.EqualTo("weapon_daggers"), "The saved choice is left as it was.");
            Assert.That(_drawn, Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WalkingTurnsTheDrawingToTheTravelInEveryFacingAndStepsThroughTheStride()
        {
            yield return Load();
            var walks = new (Vector2 steer, RenderedFacing facing)[]
            {
                (Vector2.right, RenderedFacing.East), (Vector2.left, RenderedFacing.West), (Vector2.up, RenderedFacing.North),
                (Vector2.down, RenderedFacing.South), (new Vector2(1, 1), RenderedFacing.NorthEast),
                (new Vector2(-1, -1), RenderedFacing.SouthWest), (new Vector2(-1, 1), RenderedFacing.NorthWest),
                (new Vector2(1, -1), RenderedFacing.SouthEast)
            };
            int stride = _drawn.Art.FrameCount(RenderedMotion.Walk);
            foreach (var walk in walks)
            {
                var poses = new HashSet<int>();
                for (int i = 0; i < 40; i++)
                {
                    yield return Step(walk.steer);
                    string at = walk.steer + " frame " + i;
                    Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Walk), at);
                    Assert.That(_drawn.Facing, Is.EqualTo(walk.facing), at);
                    Assert.That(_drawn.Mirrored, Is.False, at);
                    Assert.That(_drawn.Root.localScale.x, Is.EqualTo(1f), at);
                    Assert.That(_drawn.Renderer.sprite, Is.SameAs(_drawn.Art.Frame(RenderedMotion.Walk, walk.facing,
                        _drawn.FrameIndex)), at);
                    Assert.That(_drawn.FrameIndex, Is.EqualTo(RenderedCharacterFrames.WalkFrame(_drawn.Player.WalkDistance,
                        _drawn.Art.StrideLength, stride)), at + ": the pose follows the distance walked.");
                    poses.Add(_drawn.FrameIndex);
                }
                // 40 frames at 3.5 a second walk 2.33 units: two strides of 1.0, every pose.
                Assert.That(poses.Count, Is.EqualTo(stride), walk.steer + ": the whole stride.");
            }
            for (int i = 0; i < 12; i++)
                yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle), "Standing again, it idles.");
            Assert.That(_drawn.Facing, Is.EqualTo(RenderedFacing.SouthEast), "Facing where it last walked.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AStandingStrikeShowsAtTheHitFacesTheTargetAndPlaysThrough()
        {
            yield return Load();
            _target.transform.position = _look.transform.position + new Vector3(-.3f, 0f, 0f);
            int strikes = 0;
            RenderedMotion atTheHit = RenderedMotion.Idle;
            _attack.Struck += target => { strikes++; atTheHit = _drawn.Motion; };
            _attack.enabled = true;
            var poses = new HashSet<int>();
            for (int i = 0; i < 120 && strikes < 2; i++)
            {
                yield return null;
                if (_drawn.Motion == RenderedMotion.Strike)
                    poses.Add(_drawn.FrameIndex);
            }
            _attack.enabled = false;
            Assert.That(strikes, Is.GreaterThan(0));
            Assert.That(atTheHit, Is.EqualTo(RenderedMotion.Strike), "The damage has landed: the strike shows at once.");
            Assert.That(poses.Count, Is.GreaterThanOrEqualTo(3), "The cut plays through its frames.");
            Assert.That(_drawn.Facing, Is.EqualTo(RenderedFacing.West), "Turned to the target on its left.");
            Assert.That(_drawn.Mirrored, Is.False, "As rendered.");
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle));
            LogAssert.NoUnexpectedReceived();
        }

        // The owner, 2026-10-26: attacks froze the Vanguard's legs and it glided. Walking, its cuts keep the walk's pose.
        [UnityTest]
        public IEnumerator AWalkingStrikeKeepsTheStride()
        {
            yield return Load();
            _attack.enabled = true;
            int walkingStrikes = 0, attacks = _attack.AttackCount;
            for (int i = 0; i < 160; i++)
            {
                _target.transform.position = _look.transform.position + Vector3.up * .3f;
                bool east = (i / 40) % 2 == 0;
                yield return Step(east ? Vector2.right : Vector2.left);
                if (!_drawn.Player.IsStriking)
                    continue;
                walkingStrikes++;
                Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.WalkStrike), "frame " + i);
                Assert.That(_drawn.FrameIndex, Is.EqualTo(RenderedCharacterFrames.WalkFrame(_drawn.Player.WalkDistance,
                    _drawn.Art.StrideLength, _drawn.Art.FrameCount(RenderedMotion.WalkStrike))), "The legs are the walk's.");
                Assert.That(_drawn.Facing, Is.EqualTo(east ? RenderedFacing.East : RenderedFacing.West));
            }
            Assert.That(_attack.AttackCount, Is.GreaterThan(attacks + 1), "It attacked on the move.");
            Assert.That(walkingStrikes, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheForgeBurstPlaysTheCastWhole()
        {
            yield return Load();
            yield return null;
            var ability = _hero.GetComponent<AbilityController>();
            Assert.That(ability.TryUse(), Is.True, "The Forge Burst is ready at the start.");
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

        // docs/57's Shield Rush (skill 3, carried with development/skills.txt): the dash's clip plays at the press, over the
        // travel the dash throws the hero along, facing it, then the Vanguard stands again.
        [UnityTest]
        public IEnumerator TheShieldRushPlaysTheDashFacingItsTravel()
        {
            yield return Load("3");
            var ability = _hero.GetComponent<AbilityController>();
            Assert.That(ability.SkillAt(3).Kind, Is.EqualTo(AbilityKind.Rush));
            // Nothing on the Rush's path: the target stands well off it, on the right.
            _target.transform.position = _look.transform.position + new Vector3(3f, ArenaFloor.WorldY(2f), 0f);
            _look.GetComponent<HeroMovementInput>().enabled = true;
            yield return null;
            float startX = _look.transform.position.x;
            Assert.That(ability.TryUse(3, Vector2.left), Is.True);
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Dash), "The Rush shows at the press.");
            Assert.That(_drawn.Player.IsDashing, Is.True);
            var poses = new HashSet<int> { _drawn.FrameIndex };
            int frames = 0;
            while (_drawn.Player.IsDashing && frames < 60)
            {
                yield return null;
                frames++;
                if (!_drawn.Player.IsDashing)
                    break;
                Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Dash), "frame " + frames);
                Assert.That(_drawn.Renderer.sprite, Is.SameAs(_drawn.Art.Frame(RenderedMotion.Dash, _drawn.Facing,
                    _drawn.FrameIndex)), "frame " + frames);
                poses.Add(_drawn.FrameIndex);
            }
            Assert.That(poses.Count, Is.EqualTo(_drawn.Art.FrameCount(RenderedMotion.Dash)), "Brace and three strides.");
            Assert.That(frames, Is.InRange(17, 21), "About 0.3 s, docs/57's dash.");
            Assert.That(startX - _look.transform.position.x, Is.GreaterThan(1f), "The dash threw the hero west.");
            Assert.That(_drawn.Facing, Is.EqualTo(RenderedFacing.West), "Facing the dash.");
            for (int i = 0; i < 20; i++)
                yield return null;
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Idle), "Then it stands again.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AHitFlinchesAndFlashesThenDeathPlaysOnceAndLiesStill()
        {
            yield return Load();
            yield return null;
            _hero.ApplyDamage(new DamageContext(1));
            Assert.That(_drawn.Motion, Is.EqualTo(RenderedMotion.Hit), "A standing hero flinches at the hit.");
            var flinch = new HashSet<int> { _drawn.FrameIndex };
            yield return null;
            Assert.That(_drawn.IsFlashing, Is.True, "And flashes while CombatantView does.");
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                if (_drawn.Motion == RenderedMotion.Hit)
                    flinch.Add(_drawn.FrameIndex);
            }
            Assert.That(flinch.Count, Is.EqualTo(_drawn.Art.FrameCount(RenderedMotion.Hit)), "Every flinch frame shows.");
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
    }
}
