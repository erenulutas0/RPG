using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
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
    // to it; its hammer goes up while the slam's mark is down - standing, or carried at a walk by the distance it
    // travels - and lands on the strike pose with the blow; below half health its visor and seams burn hotter instead
    // of the old enrage tint, its raised hammer too; it falls in its own beat, in the facing it fell in, while its
    // rewards and the open gate come at once; and a set without those drawings winds up and falls as before, a broken
    // one still falling back to the pixel Warden. The slam's clock and damage are WardenFlowTests'.
    public sealed class WardenLookTests
    {
        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
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

        private static IEnumerator GateBoss(CombatSetup setup)
        {
            yield return Until(setup, () => setup.Encounters.GateBoss != null, 30);
            Assert.That(setup.Encounters.GateBoss, Is.Not.Null);
            yield return null;
        }

        // The raised hammer on show: the standing windup, or the walking windup's quarter.
        private static EnemyArtSet.Frame Raised(EnemyArtSet set, EnemyLookView look, bool enraged) =>
            look.WindupStep < 0 ? set.GetWindup(look.RearFacing, enraged) : set.GetWalkingWindup(look.WindupStep, look.RearFacing, enraged);

        // Another Warden from the gate boss's own definition, as the encounter spawns one, its attack off; set (when
        // given) replaces its painted art before it wakes.
        private static EnemyLookView SpawnWarden(CombatSetup setup, Vector3 at, EnemyArtSet set = null)
        {
            EnemyDefinition definition = setup.Encounters.DefinitionOf(setup.Encounters.GateBoss);
            Assert.That(definition, Is.Not.Null);
            var holder = new GameObject("Inactive holder");
            holder.SetActive(false);
            Health warden = Object.Instantiate(definition.Prefab, at, Quaternion.identity, holder.transform);
            warden.Initialize(definition.MaximumHealth);
            var attack = warden.GetComponent<AttackController>();
            attack.Initialize(definition.Weapon.CreateRuntime());
            attack.enabled = false;
            var look = warden.GetComponent<EnemyLookView>();
            if (set != null)
                typeof(EnemyLookView).GetField("_paintedArt", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(look, set);
            warden.transform.SetParent(null, true);
            Object.Destroy(holder);
            return look;
        }

        // The Warden's set without the walking windup and the fall: what the Warden wore before them.
        private static EnemyArtSet WithoutTheNewFrames(EnemyArtSet set)
        {
            EnemyArtSet bare = Object.Instantiate(set);
            foreach (string field in new[] { "_windupWalk", "_enragedWindupWalk", "_fallFront", "_fallRear", "_fallSeconds" })
                typeof(EnemyArtSet).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(bare, null);
            Assert.That(bare.IsValid && bare.HasWindup && bare.HasEnragedFrames, Is.True);
            Assert.That(bare.HasWalkingWindup(false) || bare.HasFall, Is.False);
            return bare;
        }

        // Walks a look along the floor a fixed step a frame for this many frames, recording the windup's quarter.
        private static IEnumerator Walk(EnemyLookView look, Vector3 step, int frames, List<int> steps, System.Action check)
        {
            for (int i = 0; i < frames; i++)
            {
                look.transform.position += step;
                yield return null;
                steps.Add(look.WindupStep);
                check();
            }
        }

        // The pooled renderer drawing a sprite, if any.
        private static SpriteRenderer Showing(Sprite sprite)
        {
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite == sprite) return renderer;
            return null;
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
            Assert.That(look.Body.sprite, Is.SameAs(Raised(set, look, look.ShowsEnraged).Body));
            // Turned toward where the blow will land, on the floor (as EnemyLookView faces: a clear sideways lean only).
            Vector3 toMark = new Vector3(warden.MarkX, ArenaFloor.WorldY(warden.MarkY), 0f) - look.transform.position;
            Vector2 onFloor = new Vector2(toMark.x, toMark.y / ArenaFloor.DepthScale).normalized;
            if (Mathf.Abs(onFloor.x) > .2f)
                Assert.That(look.Mirrored, Is.EqualTo(onFloor.x > 0f), "Turned toward where the blow will land.");
            if (Mathf.Abs(onFloor.y) > .2f)
                Assert.That(look.RearFacing, Is.EqualTo(onFloor.y > 0f));
            Assert.That(look.SilhouetteOf(look.Body.sprite), Is.SameAs(Raised(set, look, false).Flash),
                "A hit during the windup flashes its silhouette.");

            // Held through the whole telegraph, standing or stepping, whatever the boss does meanwhile.
            bool held = true;
            float deadline = Time.realtimeSinceStartup + WardenRule.Telegraph + 3f;
            while (!hit.HasValue && Time.realtimeSinceStartup < deadline)
            {
                ResolveUpgrades(setup);
                yield return null;
                if (!hit.HasValue)
                    held &= look.IsWindingUp && look.Body.sprite == Raised(set, look, look.ShowsEnraged).Body;
            }
            Assert.That(hit.HasValue, Is.True, "The blow lands.");
            Assert.That(held, Is.True, "The raised hammer holds until the blow.");
            Assert.That(look.IsWindingUp, Is.False);
            Assert.That(look.WindupStep, Is.EqualTo(-1));
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
        public IEnumerator UnderTheRaisedHammerTheLegsStepByDistanceAndStandWhenItStops()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            yield return GateBoss(setup);
            Time.captureDeltaTime = 1f / 60;
            Vector3 start = setup.Encounters.GateBoss.transform.position + new Vector3(2.5f, -1.5f, 0f);
            EnemyLookView look = SpawnWarden(setup, start);
            yield return null;
            EnemyArtSet set = look.PaintedArt;
            Assert.That(set.HasWalkingWindup(false), Is.True);

            // The mark down to the left: standing, the hammer goes up on the standing drawing, facing it.
            look.WindUp(start + Vector3.left * 3f);
            Assert.That(look.IsWindingUp, Is.True);
            Assert.That(look.WindupStep, Is.EqualTo(-1), "Standing, it raises the hammer standing.");
            Assert.That(look.Body.sprite, Is.SameAs(set.GetWindup(look.RearFacing).Body));
            bool mirrored = look.Mirrored, rear = look.RearFacing;
            Assert.That(mirrored, Is.False, "Toward the mark on its left.");

            // Walking on toward the mark the legs step through the walk's four quarters in order, by the distance
            // travelled, under the hammer still raised, every drawing with its own silhouette for a hit.
            var steps = new List<int>();
            bool drawn = true;
            yield return Walk(look, Vector3.left * .02f, 90, steps, () =>
            {
                drawn &= look.IsWindingUp && look.WindupStep >= 0 && look.Mirrored == mirrored && look.RearFacing == rear &&
                         look.Body.sprite == set.GetWalkingWindup(look.WindupStep, rear).Body &&
                         look.SilhouetteOf(look.Body.sprite) == set.GetWalkingWindup(look.WindupStep, rear).Flash;
            });
            Assert.That(drawn, Is.True, "Each frame of the walk is a drawing of the raised hammer at a walk.");
            var order = new List<int>();
            foreach (int quarter in steps)
                if (order.Count == 0 || order[order.Count - 1] != quarter) order.Add(quarter);
            CollectionAssert.IsSupersetOf(order, new[] { 0, 1, 2, 3 }, "Every quarter of the stride shows.");
            for (int i = 1; i < order.Count; i++)
                Assert.That(order[i], Is.EqualTo((order[i - 1] + 1) % 4), "The quarters follow the stride in order.");
            // 1.8 units at 1.075 a cycle: the quarters change about seven times - by distance, not by time.
            Assert.That(order.Count, Is.InRange(6, 9));

            // Half as fast, half as many changes over as many frames.
            steps.Clear();
            yield return Walk(look, Vector3.left * .01f, 90, steps, () => { });
            order.Clear();
            foreach (int quarter in steps)
                if (order.Count == 0 || order[order.Count - 1] != quarter) order.Add(quarter);
            Assert.That(order.Count, Is.InRange(3, 5), "The stride follows the distance travelled.");

            // Stopping, it stands with the hammer up, however long the mark is down.
            bool standing = true;
            for (int i = 0; i < 40; i++)
            {
                yield return null;
                standing &= look.IsWindingUp && look.WindupStep == -1 && look.Body.sprite == set.GetWindup(rear).Body;
            }
            Assert.That(standing, Is.True, "Still, the standing windup holds.");
            // The standing drawing is the stride's contact B, so the two are one figure.
            Assert.That(set.GetWalkingWindup(2, rear).Body, Is.SameAs(set.GetWindup(rear).Body));

            look.Slam(look.transform.position + Vector3.left);
            Assert.That(look.WindupStep, Is.EqualTo(-1));
            Assert.That(look.PaintedFrame, Is.EqualTo(3));
            Object.Destroy(look.gameObject);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheSecondPhaseCarriesTheHotterRaisedHammer()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            yield return GateBoss(setup);
            Time.captureDeltaTime = 1f / 60;
            Vector3 start = setup.Encounters.GateBoss.transform.position + new Vector3(2.5f, -1.5f, 0f);
            EnemyLookView look = SpawnWarden(setup, start);
            yield return null;
            EnemyArtSet set = look.PaintedArt;
            Assert.That(set.HasWalkingWindup(true), Is.True);

            // Stepping under the raised hammer, the enrage swaps the stride's drawing for its hotter one in step.
            look.WindUp(start + Vector3.left * 3f);
            var steps = new List<int>();
            yield return Walk(look, Vector3.left * .02f, 20, steps, () => { });
            int quarter = look.WindupStep;
            Assert.That(quarter, Is.GreaterThanOrEqualTo(0));
            Assert.That(look.Body.sprite, Is.SameAs(set.GetWalkingWindup(quarter, look.RearFacing).Body));
            Health health = look.GetComponent<Health>();
            health.ApplyDamage(new DamageContext(health.Maximum * .55f));
            Assert.That(look.GetComponent<EnrageBehaviour>().IsEnraged, Is.True);
            Assert.That(look.ShowsEnraged, Is.True);
            Assert.That(look.WindupStep, Is.EqualTo(quarter), "The enrage keeps the stride where it was.");
            Assert.That(look.Body.sprite, Is.SameAs(set.GetWalkingWindup(quarter, look.RearFacing, true).Body));

            bool hot = true;
            var seen = new HashSet<int>();
            yield return Walk(look, Vector3.left * .02f, 70, steps, () =>
            {
                seen.Add(look.WindupStep);
                EnemyArtSet.Frame cool = set.GetWalkingWindup(look.WindupStep, look.RearFacing);
                EnemyArtSet.Frame burning = set.GetWalkingWindup(look.WindupStep, look.RearFacing, true);
                hot &= look.Body.sprite == burning.Body && burning.Body != cool.Body && burning.Flash == cool.Flash &&
                       look.SilhouetteOf(look.Body.sprite) == cool.Flash;
            });
            Assert.That(hot, Is.True, "The second phase's raised hammer at a walk, wearing the first phase's silhouettes.");
            CollectionAssert.IsSupersetOf(seen, new[] { 0, 1, 2, 3 });
            Color body = look.Body.color;
            Assert.That(new Vector3(body.r, body.g, body.b), Is.EqualTo(Vector3.one), "No enrage tint over the painted steel.");

            // Standing, the hotter standing windup.
            yield return null;
            yield return null;
            Assert.That(look.WindupStep, Is.EqualTo(-1));
            Assert.That(look.Body.sprite, Is.SameAs(set.GetWindup(look.RearFacing, true).Body));
            Object.Destroy(look.gameObject);
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
            Sprite expected = look.IsWindingUp ? Raised(set, look, true).Body
                : set.GetFrame(look.PaintedFrame, look.RearFacing, true).Body;
            Assert.That(look.Body.sprite, Is.SameAs(expected), "Every pose comes from the hotter frames.");
            Color body = look.Body.color;
            Assert.That(new Vector3(body.r, body.g, body.b), Is.EqualTo(Vector3.one), "No enrage tint over the painted steel.");
            Assert.That(look.SilhouetteOf(look.Body.sprite), Is.Not.Null, "A hit still flashes the hotter frames.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheBossFallsInItsOwnBeatWhileItsRewardsComeAtOnce()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounters = setup.Encounters;
            yield return GateBoss(setup);
            Time.captureDeltaTime = 1f / 60;
            Health gate = encounters.GateBoss;
            var look = gate.GetComponent<EnemyLookView>();
            EnemyArtSet set = look.PaintedArt;
            Assert.That(set.HasFall && set.HasRearFall, Is.True);
            bool rear = look.RearFacing, mirrored = look.Mirrored;
            Sprite[] fall = set.GetFall(rear);
            int drops = setup.Drops.Count, reward = encounters.GoldRewardOf(gate);

            gate.ApplyDamage(new DamageContext(gate.Maximum * 10f));
            // Everything the death pays happens on the blow, not after the fall: the floor is clear, the gold lies where
            // the boss fell, the gate stands open.
            Assert.That(encounters.GateBoss, Is.Null);
            Assert.That(encounters.AliveEnemyCount, Is.Zero);
            Assert.That(encounters.IsFloorCleared, Is.True);
            Assert.That(encounters.IsGateExitOpen, Is.True, "The gate opens on the blow.");
            Assert.That(setup.Drops.Count, Is.EqualTo(drops + 1));
            Assert.That(setup.Drops.At(setup.Drops.Count - 1).Gold, Is.EqualTo(reward));
            Assert.That(look.Body.gameObject.activeSelf, Is.False, "The body gives way to its fall.");
            ResolveUpgrades(setup);

            SpriteRenderer remains = Showing(fall[0]);
            Assert.That(remains, Is.Not.Null, "The fall begins where the boss stood, in the facing it fell in.");
            Assert.That(remains.flipX, Is.EqualTo(mirrored));
            Assert.That(remains.sprite, Is.Not.SameAs(set.Death[0]), "Its own fall, not the two remains.");

            // A crowd's effects meanwhile - twice the shared pool's worth - never cut it short.
            MethodInfo show = typeof(CombatEffectsView).GetMethod("ShowMoving", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(show, Is.Not.Null, "The shared pool's entry point.");
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            var spark = new[] { set.GetFrame(0, false).Flash };

            var shown = new List<Sprite>();
            var frames = new List<int>();
            int frame = 0;
            while (remains.enabled && frame < 200)
            {
                if (frame % 2 == 0)
                    for (int i = 0; i < 4; i++)
                        show.Invoke(effects, new object[] { spark, .05f, .3f, Vector3.zero, 5, Vector3.zero, 0f, false, 1f });
                if (shown.Count == 0 || shown[shown.Count - 1] != remains.sprite) { shown.Add(remains.sprite); frames.Add(0); }
                frames[frames.Count - 1]++;
                yield return null;
                ResolveUpgrades(setup);
                frame++;
            }
            Assert.That(remains.enabled, Is.False, "Then the remains are gone.");
            CollectionAssert.AreEqual(fall, shown, "Every drawing of the fall in order, none cut short by the pool.");
            float seconds = frame / 60f;
            Assert.That(seconds, Is.EqualTo(set.FallSeconds).Within(3f / 60), "The fall plays its whole beat.");
            Assert.That(set.FallSeconds, Is.InRange(1f, 1.5f));
            for (int i = 0; i < fall.Length - 1; i++)
                Assert.That(frames[i] / 60f, Is.EqualTo(set.FallHold(i)).Within(2.5f / 60), "Drawing " + i + " holds its time.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ASetWithoutTheNewFramesWindsUpStandingAndFallsAsBefore()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            yield return GateBoss(setup);
            Time.captureDeltaTime = 1f / 60;
            Health gate = setup.Encounters.GateBoss;
            EnemyArtSet bare = WithoutTheNewFrames(gate.GetComponent<EnemyLookView>().PaintedArt);

            // Winding up and walking on, it holds the standing windup throughout, as the Warden did before.
            Vector3 start = gate.transform.position + new Vector3(2.5f, -1.5f, 0f);
            EnemyLookView look = SpawnWarden(setup, start, bare);
            yield return null;
            Assert.That(look.PaintedArt, Is.SameAs(bare));
            look.WindUp(start + Vector3.left * 3f);
            var steps = new List<int>();
            bool standing = true;
            yield return Walk(look, Vector3.left * .02f, 60, steps, () =>
                standing &= look.IsWindingUp && look.WindupStep == -1 && look.Body.sprite == bare.GetWindup(look.RearFacing).Body);
            Assert.That(standing, Is.True, "Without the walking drawings the raised hammer stands.");
            Object.Destroy(look.gameObject);

            // The gate boss wearing it falls in the two remains every painted enemy leaves, over their 0.4 s.
            var gateLook = gate.GetComponent<EnemyLookView>();
            typeof(EnemyLookView).GetField("_paintedArt", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(gateLook, bare);
            Assert.That(gateLook.PaintedArt, Is.SameAs(bare));
            gate.ApplyDamage(new DamageContext(gate.Maximum * 10f));
            ResolveUpgrades(setup);
            SpriteRenderer remains = Showing(bare.Death[0]);
            Assert.That(remains, Is.Not.Null, "The first of the two remains.");
            for (int i = 0; i < 12; i++) { yield return null; ResolveUpgrades(setup); }
            Assert.That(remains.sprite, Is.SameAs(bare.Death[1]));
            for (int i = 0; i < 16; i++) { yield return null; ResolveUpgrades(setup); }
            Assert.That(remains.enabled, Is.False, "Gone at 0.4 s, as before.");
            Object.Destroy(bare);
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
