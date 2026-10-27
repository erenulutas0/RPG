using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry's gate boss wears its painted set (ForgeWardenArtSet): it walks out painted, its bar and shadow sized
    // to it; its hammer goes up while the slam's mark is down and lands on the strike pose with the blow; below half
    // health its visor and seams burn hotter instead of the old enrage tint; and a broken set still falls back to the
    // pixel Warden. The slam's clock and damage are WardenFlowTests'.
    public sealed class WardenLookTests
    {
        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Warden look cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private static IEnumerator Load()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_gate_boss_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
        }

        private static void ResolveUpgrades(CombatSetup setup)
        {
            for (int i = 0; i < 8 && setup.Choices.Current != null && setup.Choices.Current.Kind == ChoiceKind.Upgrade; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
        }

        private static IEnumerator Until(CombatSetup setup, System.Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) { ResolveUpgrades(setup); yield return null; }
        }

        private static IEnumerator Fighting(CombatSetup setup)
        {
            yield return Until(setup, () => setup.Warden.IsFighting && !setup.Encounters.IsRevealHeld, 30);
            Assert.That(setup.Warden.IsFighting, Is.True, "The gate boss is fought.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheGateBossWearsItsPaintedSetAndRaisesItsHammerThroughTheTelegraph()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var warden = setup.Warden;
            yield return Fighting(setup);
            var look = warden.Boss.GetComponent<EnemyLookView>();
            EnemyArtSet set = look.PaintedArt;
            Assert.That(set, Is.Not.Null, "The Warden walks out painted.");
            Assert.That(set.name, Is.EqualTo("ForgeWardenArtSet"));
            Assert.That(look.Look, Is.EqualTo(EnemyLook.Warden));
            Color colour = look.Body.color;
            Assert.That(new Vector3(colour.r, colour.g, colour.b), Is.EqualTo(Vector3.one), "Its own colours, untinted.");
            Assert.That(look.Bar.localPosition.y, Is.EqualTo(set.VisualTop + .12f).Within(1e-4f), "The bar clears its helm.");
            Assert.That(look.transform.Find("Contact Shadow").localScale.x, Is.InRange(.01f, set.ShadowWidth + 1e-4f));

            bool marked = false;
            bool? hit = null;
            float slammedAt = 0f;
            warden.Marked += () => marked = true;
            warden.Slammed += h => { hit = h; slammedAt = Time.time; };
            yield return Until(setup, () => marked, WardenRule.SlamEvery + 5);
            Assert.That(marked, Is.True);
            Assert.That(look.IsWindingUp, Is.True, "The hammer goes up with the mark.");
            Assert.That(look.PaintedFrame, Is.EqualTo(EnemyLookView.WindupFrame));
            Assert.That(look.Body.sprite, Is.SameAs(set.GetWindup(look.RearFacing, look.ShowsEnraged).Body));
            // Turned toward where the blow will land, on the floor (as EnemyLookView faces: a clear sideways lean only).
            Vector3 toMark = new Vector3(warden.MarkX, ArenaFloor.WorldY(warden.MarkY), 0f) - look.transform.position;
            Vector2 onFloor = new Vector2(toMark.x, toMark.y / ArenaFloor.DepthScale).normalized;
            if (Mathf.Abs(onFloor.x) > .2f)
                Assert.That(look.Mirrored, Is.EqualTo(onFloor.x > 0f), "Turned toward where the blow will land.");
            if (Mathf.Abs(onFloor.y) > .2f)
                Assert.That(look.RearFacing, Is.EqualTo(onFloor.y > 0f));
            Assert.That(look.SilhouetteOf(look.Body.sprite), Is.SameAs(set.GetWindup(look.RearFacing).Flash),
                "A hit during the windup flashes its silhouette.");

            // Held through the whole telegraph, whatever the boss does meanwhile.
            bool held = true;
            float deadline = Time.realtimeSinceStartup + WardenRule.Telegraph + 3f;
            while (!hit.HasValue && Time.realtimeSinceStartup < deadline)
            {
                ResolveUpgrades(setup);
                yield return null;
                if (!hit.HasValue)
                    held &= look.IsWindingUp && look.Body.sprite == set.GetWindup(look.RearFacing, look.ShowsEnraged).Body;
            }
            Assert.That(hit.HasValue, Is.True, "The blow lands.");
            Assert.That(held, Is.True, "The windup holds until the blow.");
            Assert.That(look.IsWindingUp, Is.False);
            Assert.That(look.PaintedFrame, Is.EqualTo(3), "The blow lands on the strike pose.");
            Assert.That(look.Body.sprite, Is.SameAs(set.GetFrame(3, look.RearFacing, look.ShowsEnraged).Body));
            yield return null;
            if (Time.time - slammedAt < .3f)
                Assert.That(look.PaintedFrame, Is.EqualTo(3), "Held a beat longer than a swing.");
            // Then back to standing or walking, never stuck in either pose.
            yield return Until(setup, () => look.PaintedFrame != 3, 2f);
            Assert.That(look.PaintedFrame, Is.Not.EqualTo(3).And.Not.EqualTo(EnemyLookView.WindupFrame));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator BelowHalfHealthItsVisorAndSeamsBurnHotterInsteadOfTheTint()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            yield return Fighting(setup);
            Health boss = setup.Warden.Boss;
            var look = boss.GetComponent<EnemyLookView>();
            EnemyArtSet set = look.PaintedArt;
            Assert.That(set.HasEnragedFrames, Is.True);
            Assert.That(look.ShowsEnraged, Is.False);
            boss.ApplyDamage(new DamageContext(boss.Maximum * .55f));
            yield return null;
            yield return null;
            Assert.That(boss.GetComponent<EnrageBehaviour>().IsEnraged, Is.True);
            Assert.That(look.ShowsEnraged, Is.True, "The second phase shows.");
            Sprite expected = look.IsWindingUp ? set.GetWindup(look.RearFacing, true).Body
                : set.GetFrame(look.PaintedFrame, look.RearFacing, true).Body;
            Assert.That(look.Body.sprite, Is.SameAs(expected), "Every pose comes from the hotter frames.");
            Color body = look.Body.color;
            Assert.That(new Vector3(body.r, body.g, body.b), Is.EqualTo(Vector3.one), "No enrage tint over the painted steel.");
            Assert.That(look.SilhouetteOf(look.Body.sprite), Is.Not.Null, "A hit still flashes the hotter frames.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AnIncompleteSetFallsBackToThePixelWarden()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            yield return Until(setup, () => setup.Encounters.GateBoss != null, 30);
            Health gate = setup.Encounters.GateBoss;
            Assert.That(gate, Is.Not.Null);
            Health prefab = setup.Encounters.DefinitionOf(gate).Prefab;
            EnemyArtSet painted = gate.GetComponent<EnemyLookView>().PaintedArt;

            // The Warden's own set with its remains torn out: no longer valid.
            EnemyArtSet broken = Object.Instantiate(painted);
            FieldInfo death = typeof(EnemyArtSet).GetField("_death", BindingFlags.NonPublic | BindingFlags.Instance);
            death.SetValue(broken, new Sprite[0]);
            Assert.That(broken.IsValid, Is.False);

            var holder = new GameObject("Inactive holder");
            holder.SetActive(false);
            Health clone = Object.Instantiate(prefab, holder.transform);
            clone.Initialize(100f);
            var look = clone.GetComponent<EnemyLookView>();
            typeof(EnemyLookView).GetField("_paintedArt", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(look, broken);
            LogAssert.Expect(LogType.Error, "Enemy painted art set is incomplete.");
            clone.transform.SetParent(null, false);
            yield return null;

            Assert.That(look.PaintedArt, Is.Null);
            Assert.That(look.HasSprites, Is.True);
            Assert.That(look.Body.sprite, Is.SameAs(EnemySpriteCache.Get(EnemyLook.Warden).Frame(EnemyPose.IdleA)),
                "The pixel Warden, as before the painted set.");
            Assert.That(look.Bar.localPosition.y, Is.EqualTo(EnemyArt.WardenHeight / 32f + .12f).Within(1e-4f));
            look.WindUp(look.transform.position + Vector3.left);
            Assert.That(look.IsWindingUp, Is.False, "Without a painted windup the telegraph leaves the body alone.");
            Assert.That(look.DrawsEnrage, Is.False);
            Object.Destroy(clone.gameObject);
            Object.Destroy(holder);
            Object.Destroy(broken);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
