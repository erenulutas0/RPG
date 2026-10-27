using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cryptforge.Tests
{
    // The Runner, the Tank and the Grunt Captain in the Gameplay scene, each spawned from its EnemyDefinition the way the
    // encounter spawns one (its prefab instantiated, its health set, its weapon given): it wears its own painted set
    // (docs/44's Foundry roster), hangs its bar from the set's visual top over the set's contact shadow, walks the
    // Grunt's four-pose gait in four facings by travelled distance, strikes facing its actual target with the strike's
    // own flash silhouette, and borrows its frames, which outlive the enemy and a reload.
    public sealed class FoundryEnemyAnimationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string Art = "Assets/_Project/Art/Characters/";
        private static readonly (string definition, string set, EnemyLook look)[] Roster =
        {
            ("Enemy_Runner", "Runner/RunnerArtSet", EnemyLook.Runner),
            ("Enemy_Tank", "Tank/TankArtSet", EnemyLook.Tank),
            ("Enemy_GruntCaptain", "GruntCaptain/GruntCaptainArtSet", EnemyLook.Captain),
        };
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Vector3 _hero;

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.timeScale = 1; Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            _hero = GameObject.Find("Vanguard").transform.position;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            foreach (var spawned in _spawned) if (spawned != null) Object.Destroy(spawned);
            _spawned.Clear();
            Time.timeScale = 1; Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry enemy cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene); TestProfile.End();
        }

        // One enemy as EncounterController.CreateEnemy makes it, standing at the hero's side on open floor, its attack
        // off until a test gives it a target.
        private EnemyLookView Spawn(int index, Vector3 offset)
        {
#if UNITY_EDITOR
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/" + Roster[index].definition + ".asset");
            Assert.That(definition, Is.Not.Null, Roster[index].definition);
            Health enemy = Object.Instantiate(definition.Prefab, _hero + offset, Quaternion.identity);
            enemy.Initialize(definition.MaximumHealth);
            var attack = enemy.GetComponent<AttackController>();
            attack.Initialize(definition.Weapon.CreateRuntime());
            attack.enabled = false;
            _spawned.Add(enemy.gameObject);
            return enemy.GetComponent<EnemyLookView>();
#else
            Assert.Ignore("The roster's definitions are loaded through the editor's asset database.");
            return null;
#endif
        }

        private static EnemyArtSet SetOf(int index)
        {
#if UNITY_EDITOR
            var set = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(Art + Roster[index].set + ".asset");
            Assert.That(set, Is.Not.Null, Roster[index].set);
            return set;
#else
            return null;
#endif
        }

        private static SpriteRenderer BodyOf(EnemyLookView look) => look.transform.Find("Enemy Body").GetComponent<SpriteRenderer>();

        [UnityTest]
        public IEnumerator EachSpawnsWearingItsOwnPaintedSetWithItsBarAndShadow()
        {
            var looks = new List<EnemyLookView>();
            for (int i = 0; i < Roster.Length; i++)
                looks.Add(Spawn(i, new Vector3(-1.6f + 1.6f * i, .9f)));
            yield return null;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            var sets = new HashSet<EnemyArtSet>();
            for (int i = 0; i < Roster.Length; i++)
            {
                EnemyLookView look = looks[i];
                EnemyArtSet set = SetOf(i);
                string at = Roster[i].definition;
                Assert.That(look.Look, Is.EqualTo(Roster[i].look), at);
                Assert.That(look.PaintedArt, Is.SameAs(set), at + " wears its own painted set.");
                Assert.That(sets.Add(look.PaintedArt), Is.True, at + " shares no set with the others.");
                Assert.That(look.HasSprites, Is.True, at);
                SpriteRenderer body = BodyOf(look);
                Assert.That(body.sprite, Is.SameAs(set.GetFrame(0, false).Body), at + " stands in its front idle.");
                Assert.That(body.color, Is.EqualTo(Color.white), at + ": no prefab tint over the painted colour.");
                Assert.That(body.sprite.texture.isReadable, Is.False, at);
                Transform bar = look.transform.Find("Health Bar");
                Assert.That(bar.localPosition.y, Is.EqualTo(set.VisualTop + .12f).Within(.0001f), at + "'s bar hangs over its head.");
                Assert.That(bar.localScale, Is.EqualTo(Vector3.one), at + " keeps the full bar (only the Mite's is compact).");
                // The set's shadow width, trimmed only where the arena's edge would cut it (ContactShadowView.Refresh).
                Vector3 floor = look.transform.position - arena.transform.position;
                float fit = ContactShadowArt.Fit(arena.Geometry, floor.x, floor.y, set.ShadowWidth);
                Assert.That(fit, Is.GreaterThan(.5f), at + " stands on open floor.");
                Assert.That(look.transform.Find("Contact Shadow").localScale.x, Is.EqualTo(set.ShadowWidth * fit).Within(.0001f), at);
            }
            // None borrows the Grunt's frames any more, and the Grunts in the scene keep theirs.
            foreach (var other in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (other.Look == EnemyLook.Grunt) Assert.That(sets.Contains(other.PaintedArt), Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TravelWalksTheFourPoseGaitInFourFacingsAndPauseFreezesIt()
        {
            for (int i = 0; i < Roster.Length; i++)
            {
                EnemyLookView look = Spawn(i, new Vector3(0, -.6f));
                SpriteRenderer body = BodyOf(look);
                yield return null;
                foreach (var direction in new[] { new Vector3(1, .5f), new Vector3(-1, .5f), new Vector3(1, -.5f), new Vector3(-1, -.5f) })
                {
                    var frames = new HashSet<int>();
                    // Far enough to cover the slowest cadence's whole cycle (the Runner's one unit) in every direction.
                    for (int k = 0; k < 30; k++)
                    {
                        look.transform.position += direction * .04f; yield return null;
                        frames.Add(look.PaintedFrame);
                    }
                    string at = $"{Roster[i].definition} toward {direction}";
                    CollectionAssert.AreEquivalent(new[] { 1, 4, 2, 5 }, frames, at);
                    Assert.That(look.RearFacing, Is.EqualTo(direction.y > 0), at);
                    Assert.That(look.Mirrored, Is.EqualTo(direction.x > 0), at);
                    Assert.That(body.sprite, Is.SameAs(look.PaintedArt.GetFrame(look.PaintedFrame, look.RearFacing).Body), at);
                    Assert.That(Mathf.Sign(body.transform.localScale.x), Is.EqualTo(direction.x > 0 ? -1f : 1f), at + ": mirrored by the body only.");
                    Assert.That(look.transform.localScale, Is.EqualTo(Vector3.one), at);
                    Time.timeScale = 0; Sprite pose = body.sprite;
                    yield return new WaitForSecondsRealtime(.08f);
                    Assert.That(body.sprite, Is.SameAs(pose), at + ": a pause holds the pose."); Time.timeScale = 1;
                    yield return null;
                    Assert.That(look.PaintedFrame, Is.Zero, at + ": standing still is the idle.");
                }
                Object.Destroy(look.gameObject);
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AnActualStrikeFacesItsTargetAndTheHitFlashIsThatFramesSilhouette()
        {
            for (int i = 0; i < Roster.Length; i++)
            {
                EnemyLookView look = Spawn(i, new Vector3(-1.2f + 1.2f * i, -.8f));
                var target = new GameObject("Strike target").AddComponent<Health>(); target.Initialize(1000);
                _spawned.Add(target.gameObject);
                target.transform.position = look.transform.position + new Vector3(.1f, .05f);
                look.GetComponent<Targeting>().SetCandidates(new[] { target });
                var attack = look.GetComponent<AttackController>(); attack.enabled = true;
                // The Tank winds up for a second and a half before its first blow.
                for (int k = 0; k < 240 && attack.AttackCount == 0; k++) yield return null;
                string at = Roster[i].definition;
                Assert.That(target.Current, Is.LessThan(1000), at + " struck.");
                Assert.That(look.RearFacing && look.Mirrored, Is.True, at + " faces its target, up and to the right.");
                Assert.That(look.PaintedFrame, Is.EqualTo(3), at + " shows its strike.");
                attack.enabled = false;
                SpriteRenderer body = BodyOf(look);
                Assert.That(body.sprite, Is.SameAs(look.PaintedArt.GetFrame(3, true).Body), at);
                look.GetComponent<Health>().ApplyDamage(new DamageContext(1)); yield return null;
                Assert.That(body.transform.Find("Flash").GetComponent<SpriteRenderer>().sprite, Is.SameAs(look.SilhouetteOf(body.sprite)), at);
                Assert.That(look.SilhouetteOf(body.sprite), Is.SameAs(look.PaintedArt.FlashOf(body.sprite)), at);
                Assert.That(body.transform.localScale.y, Is.LessThan(1), at + " gives under the blow.");
                Object.Destroy(target.gameObject);
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DeathHidesBarAndBodyAndTheSharedFramesOutliveTheEnemyAndAReload()
        {
            var first = new List<Sprite>();
            for (int i = 0; i < Roster.Length; i++)
            {
                EnemyLookView look = Spawn(i, new Vector3(-1.6f + 1.6f * i, .9f));
                yield return null;
                EnemyArtSet set = look.PaintedArt;
                first.Add(set.GetFrame(0, false).Body);
                Assert.That(set.Death.Length, Is.EqualTo(2), Roster[i].definition);
                Health health = look.GetComponent<Health>();
                health.ApplyDamage(new DamageContext(100000)); yield return null;
                Assert.That(look.transform.Find("Health Bar").gameObject.activeSelf, Is.False, Roster[i].definition + "'s bar hides at once.");
                Assert.That(BodyOf(look).gameObject.activeSelf, Is.False, Roster[i].definition + "'s body gives way to its remains.");
                Object.Destroy(look.gameObject);
            }
            yield return null;
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            yield return Resources.UnloadUnusedAssets();
            for (int i = 0; i < first.Count; i++)
                Assert.That(first[i] != null && first[i].texture != null, Is.True, Roster[i].set + " is borrowed, never destroyed.");
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
