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
    // The game-feel pass, stage 1 (docs/43 section 1, 2026-11-22): a weapon fired from slot 1 or 2 shows a cue of its own
    // on the hero at the moment it strikes (HeroFeelView) - the Sword a flourish, the Staff a pulse, the Daggers a flick -
    // while the first weapon's strikes stay the body's swing alone. The cues are pooled under the hero, drawn behind the
    // figure or thin over it, and leave nothing behind across scene loads.
    public sealed class SlotCueTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private static readonly WeaponBehavior[] Behaviors = { WeaponBehavior.Cleave, WeaponBehavior.Area, WeaponBehavior.DirectHit };
        private HeroFeelView _feel;
        private AttackController _attack;
        private Health _target;
        private SpriteRenderer _body;

        [UnitySetUp] public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Slot cue cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private IEnumerator Load()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>();
            hero.enabled = false;
            _attack = hero.GetComponent<AttackController>();
            _feel = hero.GetComponent<HeroFeelView>();
            _body = hero.GetComponent<HeroLookView>().Body;
            _target = new GameObject("Slot cue target").AddComponent<Health>();
            _target.Initialize(1000000f);
            _target.transform.position = hero.transform.position + Vector3.right * .6f;
            hero.GetComponent<Targeting>().SetCandidates(new[] { _target });
        }

        // Every hero weapon kind the first one is not, put in the later slots: a quick interval, a reach past the target.
        private List<WeaponRuntime> AddTheOtherWeapons()
        {
            var added = new List<WeaponRuntime>();
            SlotCue primary = GameFeel.CueFor(_attack.Weapon.Pattern.Behavior);
            foreach (WeaponBehavior behavior in Behaviors)
            {
                if (GameFeel.CueFor(behavior) == primary)
                    continue;
                bool splash = behavior != WeaponBehavior.DirectHit;
                var runtime = new WeaponRuntime(1f, .45f, 3f, 0f,
                    new AttackPattern(behavior, splash ? 1f : 0f, splash ? .5f : 0f));
                _attack.AddWeapon(runtime);
                added.Add(runtime);
            }
            return added;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ALaterSlotsWeaponCuesOnItsOwnStrikeAndTheFirstWeaponsStrikeDoesNot()
        {
            yield return Load();
            Assert.That(_feel, Is.Not.Null, "The hero carries the game-feel view.");
            SlotCue primary = GameFeel.CueFor(_attack.Weapon.Pattern.Behavior);
            List<WeaponRuntime> added = AddTheOtherWeapons();
            Assert.That(added.Count, Is.EqualTo(2));
            var strikes = new Dictionary<WeaponRuntime, int>();
            int primaryStrikes = 0;
            bool cuedAtOnce = true;
            System.Action<Health, WeaponRuntime> struck = (target, weapon) =>
            {
                if (ReferenceEquals(weapon, _attack.Weapon))
                {
                    primaryStrikes++;
                    return;
                }
                strikes[weapon] = strikes.TryGetValue(weapon, out int n) ? n + 1 : 1;
                // The view heard the strike first (it subscribed as the scene was set up): its cue is under way now.
                cuedAtOnce &= _feel.IsCueShowing(GameFeel.CueFor(weapon.Pattern.Behavior));
            };
            _attack.StruckWith += struck;
            _attack.enabled = true;
            for (int i = 0; i < 120; i++)
                yield return null;
            _attack.enabled = false;
            _attack.StruckWith -= struck;

            Assert.That(primaryStrikes, Is.GreaterThan(0), "The first weapon struck too.");
            Assert.That(_feel.CuesShown(primary), Is.Zero, "The first weapon's strikes are the body's swing, not a cue.");
            foreach (WeaponRuntime weapon in added)
            {
                SlotCue cue = GameFeel.CueFor(weapon.Pattern.Behavior);
                Assert.That(strikes.ContainsKey(weapon), Is.True, cue + "'s weapon struck.");
                Assert.That(_feel.CuesShown(cue), Is.EqualTo(strikes[weapon]), cue + ": one cue a strike.");
            }
            Assert.That(cuedAtOnce, Is.True, "Each cue begins on the frame of its strike.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ACueNeverHidesTheHeroAndEndsBeforeItsNextStrike()
        {
            yield return Load();
            List<WeaponRuntime> added = AddTheOtherWeapons();
            _attack.enabled = true;
            bool seen = false;
            for (int i = 0; i < 90; i++)
            {
                yield return null;
                foreach (WeaponRuntime weapon in added)
                {
                    SlotCue cue = GameFeel.CueFor(weapon.Pattern.Behavior);
                    if (!_feel.IsCueShowing(cue))
                        continue;
                    seen = true;
                    Assert.That(GameFeel.CueSeconds(cue, weapon.Interval), Is.LessThan(weapon.Interval),
                        cue + " is over before its weapon strikes again.");
                    foreach (SpriteRenderer sprite in _feel.CueSprites(cue))
                    {
                        if (!sprite.enabled)
                            continue;
                        Assert.That(sprite.color.a, Is.LessThanOrEqualTo(.9f + 1e-4f), cue + ": translucent.");
                        if (sprite.sprite != FeelArt.Disc())
                            Assert.That(sprite.sortingOrder, Is.LessThan(_body.sortingOrder), cue + ": its ghost behind the figure.");
                        Assert.That(sprite.transform.localPosition.magnitude, Is.LessThan(2f), cue + ": on the hero.");
                    }
                    foreach (LineRenderer line in _feel.CueLines(cue))
                    {
                        if (!line.enabled)
                            continue;
                        Assert.That(line.endColor.a, Is.LessThanOrEqualTo(.7f + 1e-4f), cue + ": a thin translucent trail.");
                        Assert.That(Mathf.Max(line.startWidth, line.endWidth), Is.LessThanOrEqualTo(.06f), cue + ": thin.");
                    }
                }
            }
            Assert.That(seen, Is.True);
            _attack.enabled = false;
            for (int i = 0; i < 24; i++)
                yield return null;
            foreach (SlotCue cue in new[] { SlotCue.Flourish, SlotCue.Pulse, SlotCue.Flick })
            {
                Assert.That(_feel.IsCueShowing(cue), Is.False, cue + " ends.");
                foreach (Renderer renderer in _feel.Root.GetComponentsInChildren<Renderer>())
                    Assert.That(renderer.enabled, Is.False, renderer.name + " is hidden once its cue ends.");
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheCuesArePooledAndNothingIsLeftBehindAcrossRuns()
        {
            yield return Load();
            Sprite disc = FeelArt.Disc();
            int renderers = _feel.Root.GetComponentsInChildren<Renderer>(true).Length;
            int children = _feel.Root.childCount;
            var rigs = new List<Object>();
            foreach (SlotCue cue in new[] { SlotCue.Flourish, SlotCue.Pulse, SlotCue.Flick })
            {
                rigs.AddRange(_feel.CueSprites(cue));
                rigs.AddRange(_feel.CueLines(cue));
            }
            Assert.That(rigs.Count, Is.EqualTo(renderers), "Every renderer under the root belongs to a cue's rig.");
            AddTheOtherWeapons();
            _attack.enabled = true;
            for (int i = 0; i < 150; i++)
                yield return null;
            _attack.enabled = false;
            Assert.That(_feel.Root.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(renderers), "No renderer made per strike.");
            Assert.That(_feel.Root.childCount, Is.EqualTo(children));
            int at = 0;
            foreach (SlotCue cue in new[] { SlotCue.Flourish, SlotCue.Pulse, SlotCue.Flick })
            {
                foreach (SpriteRenderer sprite in _feel.CueSprites(cue))
                    Assert.That(sprite, Is.SameAs(rigs[at++]), "The same rig, reused.");
                foreach (LineRenderer line in _feel.CueLines(cue))
                    Assert.That(line, Is.SameAs(rigs[at++]), "The same rig, reused.");
            }

            // Two runs more, as a restart reloads the scene: one view and one root a scene, the shared disc never remade.
            for (int run = 0; run < 2; run++)
            {
                yield return SceneManager.LoadSceneAsync(ScenePath);
                yield return null;
                Assert.That(Object.FindObjectsByType<HeroFeelView>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                int roots = 0;
                foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if (t.name == HeroFeelView.RootName)
                        roots++;
                Assert.That(roots, Is.EqualTo(1), "The last run's cues went with its scene.");
                var feel = Object.FindFirstObjectByType<HeroFeelView>();
                Assert.That(feel.Root.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(renderers));
                Assert.That(FeelArt.Disc(), Is.SameAs(disc), "One disc a session.");
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
