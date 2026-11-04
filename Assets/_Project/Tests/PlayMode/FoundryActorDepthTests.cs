using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Cryptforge.Tests
{
    // The foundry places every actor on show in depth for each camera render (FoundryVisibilityView.ApplyActorDepth) and
    // puts it back after (RestoreActorDepth), so the pass runs every frame for the hero, every enemy and every drop. Once
    // its lists have grown and its silhouette materials exist it allocates nothing: reading each child's name to find the
    // hero's ability ring allocated a string for every child of every actor, a few kilobytes a frame with a crowd on the
    // floor. That holds for the cameraless placement and for the gameplay camera's, which decides each body's material
    // from the depth cover. That the ring and the shadows still lie on the floor is FoundryElevationTests' (the ramp).
    //
    // The allocations are counted as Unity's Is.Not.AllocatingGCMemory counts them (GC.Alloc samples on this thread), for
    // three passes of each placement in a row, and again right after a full collection; and the pass's calls are counted
    // piece by piece over the same actors and bodies. All of it is printed, and a failure carries it, with a reading:
    //   - the passes allocate every time and a piece does too: that piece's calls allocate on every call;
    //   - only the first pass after the warm-up allocates: something is made once - an object made lazily, or the JIT
    //     compiling the measured call site there (Mono allocates a temporary string for every string literal it compiles
    //     in, inlined callees' included: so ApplyActorDepth() read FoundryVisibilityView.NoCamera as a constant, and the
    //     first measured cameraless pass counted 1 until the refusal reasons became static fields);
    //   - the passes allocate nothing, but do right after a collection: Unity let native objects' managed wrappers go and
    //     made them again, so a collection that falls between a warm-up and its measurement shows up as an allocation.
    // A collection runs before each warm-up, so that none falls inside a measurement by chance.
    public sealed class FoundryActorDepthTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const int Passes = 3;
        // Everything a measured piece reads ends up here, so no read is idle.
        private static float _sink;

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry actor depth cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator PlacingTheActorsInDepthAllocatesNothing()
        {
            var encounters = Object.FindFirstObjectByType<EncounterController>();
            for (int frame = 0; frame < 600 && encounters.AllAliveCount == 0; frame++)
                yield return null;
            Assert.That(encounters.AllAliveCount, Is.GreaterThan(0), "The proof floor's wave stands on the floor.");
            // Every newcomer's Start has run: its contact shadow and its bar are in place.
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            var visibility = Object.FindFirstObjectByType<FoundryVisibilityView>();
            Assert.That(visibility, Is.Not.Null, "The foundry places its actors in depth.");
            Transform hero = Object.FindFirstObjectByType<HeroLookView>().transform;
            Assert.That(hero.Find(AbilityRingView.RingName), Is.Not.Null, "The hero carries its ability ring.");
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);

            // A message logged while the passes are measured would allocate in the log's handlers: counted, with the first.
            _logs = 0;
            _firstLog = null;
            Application.logMessageReceived += CountLog;
            try
            {
                MeasureEverything(encounters, visibility, camera);
            }
            finally
            {
                Application.logMessageReceived -= CountLog;
            }
        }

        private static int _logs;
        private static string _firstLog;

        private static void CountLog(string message, string stackTrace, LogType type)
        {
            _logs++;
            if (_firstLog == null)
                _firstLog = type + ": " + message;
        }

        private static void MeasureEverything(EncounterController encounters, FoundryVisibilityView visibility, Camera camera)
        {
            // The first pass makes the silhouette materials and grows the lists; every later frame pays the steady state.
            Collect();
            visibility.ApplyActorDepth();
            int placed = visibility.PlacedCount;
            visibility.RestoreActorDepth();
            Assert.That(placed, Is.GreaterThanOrEqualTo(encounters.AllAliveCount * 3),
                "Each enemy's body, bar and contact shadow are placed in depth.");
            int[] cameraless = Measure(visibility, null);
            visibility.ApplyActorDepth();
            Assert.That(visibility.PlacedCount, Is.EqualTo(placed), "The same actors and effects are placed on every pass.");
            visibility.RestoreActorDepth();

            // The pass as the gameplay camera's render makes it, deciding each body's material from the depth cover
            // (FoundryDepthCover); its first pass gives the bodies in the open their own material back.
            Collect();
            visibility.ApplyActorDepth(camera);
            TestContext.Out.WriteLine("The proof floor's wave for the gameplay camera: " + visibility.DescribeGate());
            Assert.That(visibility.IsGated, Is.True,
                "The gameplay camera sees the region flat, and the cover is made: " + visibility.DescribeGate());
            Assert.That(visibility.ShownBodies, Is.GreaterThanOrEqualTo(encounters.AllAliveCount));
            visibility.RestoreActorDepth();
            int[] gated = Measure(visibility, camera);

            // Right after a full collection, each placement warmed by a pass of its own first (which also puts the bodies
            // back in that placement's materials).
            Pass(visibility, null);
            Collect();
            int cameralessAfterCollection = Allocations(() => Pass(visibility, null));
            Pass(visibility, camera);
            Collect();
            int gatedAfterCollection = Allocations(() => Pass(visibility, camera));

            string report = "GC allocations (GC.Alloc samples) - the cameraless pass x" + Passes + ": " + Join(cameraless) +
                            "; the gameplay camera's pass x" + Passes + ": " + Join(gated) + "; right after a collection: " +
                            cameralessAfterCollection + " cameraless, " + gatedAfterCollection + " gated; messages logged " +
                            "while measuring: " + _logs + (_firstLog != null ? " (first: " + _firstLog + ")" : string.Empty) +
                            "\n" + Pieces(visibility, camera) +
                            Reading(cameraless, gated, cameralessAfterCollection, gatedAfterCollection, _logs);
            TestContext.Out.WriteLine(report);
            Assert.That(Sum(cameraless), Is.Zero, "The cameraless pass allocates once its lists have grown.\n" + report);
            Assert.That(Sum(gated), Is.Zero, "The gameplay camera's pass allocates once its lists have grown.\n" + report);
        }

        private static void Pass(FoundryVisibilityView visibility, Camera camera)
        {
            if (camera != null)
                visibility.ApplyActorDepth(camera);
            else
                visibility.ApplyActorDepth();
            visibility.RestoreActorDepth();
        }

        // Passes of one placement in a row, each counted on its own.
        private static int[] Measure(FoundryVisibilityView visibility, Camera camera)
        {
            var counts = new int[Passes];
            System.Action pass = () => Pass(visibility, camera);
            for (int i = 0; i < counts.Length; i++)
                counts[i] = Allocations(pass);
            return counts;
        }

        // GC.Alloc samples on this thread while the action runs, exactly as Unity's AllocatingGCMemoryConstraint counts
        // them: disabling the recorder flushes what it held, so only the action's own samples are read back.
        private static int Allocations(System.Action action)
        {
            Recorder recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                action();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }
            return recorder.sampleBlockCount;
        }

        private static void Collect()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }

        // The pass's calls, piece by piece, over the actors, children and bodies it walks (Piece counts each).
        private static string Pieces(FoundryVisibilityView visibility, Camera camera)
        {
            var roots = new List<Transform>();
            var looks = new List<EnemyLookView>();
            var children = new List<Transform>();
            var bodies = new List<SpriteRenderer>();
            foreach (Transform actor in GroundedSorting.Actors)
            {
                if (actor == null || !actor.gameObject.activeInHierarchy)
                    continue;
                roots.Add(actor);
                actor.TryGetComponent(out EnemyLookView look);
                looks.Add(look);
                if (look != null && look.Body != null)
                    bodies.Add(look.Body);
                for (int c = 0; c < actor.childCount; c++)
                    children.Add(actor.GetChild(c));
            }
            var chest = Object.FindFirstObjectByType<ChestSpawner>();
            if (chest != null && chest.Body != null)
                bodies.Add(chest.Body);
            var drops = Object.FindFirstObjectByType<GroundDropView>();
            for (int i = 0; drops != null && i < drops.ShownCount; i++)
                bodies.Add(drops.ShownBody(i));
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            Vector3 region = visibility.transform.position;
            FoundryDepthCover cover = visibility.DepthCover;

            var t = new StringBuilder();
            t.Append("the pass's calls over its ").Append(roots.Count).Append(" actors, ").Append(children.Count)
                .Append(" children and ").Append(bodies.Count).AppendLine(" bodies, piece by piece:");
            Piece(t, "actors: gameObject.activeInHierarchy", () =>
            {
                for (int i = 0; i < roots.Count; i++)
                    if (roots[i].gameObject.activeInHierarchy) _sink++;
            });
            Piece(t, "actors: TryGetComponent<EnemyLookView>", () =>
            {
                for (int i = 0; i < roots.Count; i++)
                    if (roots[i].TryGetComponent(out EnemyLookView _)) _sink++;
            });
            Piece(t, "actors: position, childCount, GetChild", () =>
            {
                for (int i = 0; i < roots.Count; i++)
                {
                    _sink += roots[i].position.y;
                    for (int c = 0; c < roots[i].childCount; c++)
                        if (roots[i].GetChild(c) != null) _sink++;
                }
            });
            Piece(t, "actors without a look: Find(the ability ring)", () =>
            {
                for (int i = 0; i < roots.Count; i++)
                    if (looks[i] == null && roots[i].Find(AbilityRingView.RingName) != null) _sink++;
            });
            Piece(t, "enemy looks: Bar, Body, Body.transform", () =>
            {
                for (int i = 0; i < looks.Count; i++)
                {
                    EnemyLookView look = looks[i];
                    if (look == null)
                        continue;
                    if (look.Bar != null) _sink++;
                    if (look.Body != null && look.Body.transform != null) _sink++;
                }
            });
            Piece(t, "children: TryGetComponent<ContactShadowView>, TryGetComponent<SpriteRenderer>", () =>
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].TryGetComponent(out ContactShadowView _)) _sink++;
                    if (children[i].TryGetComponent(out SpriteRenderer _)) _sink++;
                }
            });
            Piece(t, "children: localPosition, position", () =>
            {
                for (int i = 0; i < children.Count; i++)
                    _sink += children[i].localPosition.x + children[i].position.y;
            });
            Piece(t, "children: position and localPosition written back as they are", () =>
            {
                for (int i = 0; i < children.Count; i++)
                {
                    Transform child = children[i];
                    Vector3 local = child.localPosition;
                    child.position = child.position;
                    child.localPosition = local;
                }
            });
            Piece(t, "bodies: sharedMaterial", () =>
            {
                for (int i = 0; i < bodies.Count; i++)
                    if (bodies[i].sharedMaterial != null) _sink++;
            });
            Piece(t, "bodies: sprite, drawMode, flipX, flipY, sprite.bounds", () =>
            {
                for (int i = 0; i < bodies.Count; i++)
                {
                    SpriteRenderer body = bodies[i];
                    Sprite sprite = body.sprite;
                    if (sprite != null) _sink += sprite.bounds.extents.x;
                    if (body.drawMode == SpriteDrawMode.Simple) _sink++;
                    if (body.flipX || body.flipY) _sink++;
                }
            });
            Piece(t, "bodies: transform.localToWorldMatrix", () =>
            {
                for (int i = 0; i < bodies.Count; i++)
                    _sink += bodies[i].transform.localToWorldMatrix.m23;
            });
            Piece(t, "bodies: FoundryVisibilityView.Footprint, FoundryDepthCover.Ask", () =>
            {
                for (int i = 0; i < bodies.Count; i++)
                    if (FoundryVisibilityView.Footprint(bodies[i], region, out Vector2 min, out Vector2 max, out float far) &&
                        cover != null && cover.Ask(min, max, far, FoundryVisibilityView.CoverMargin) == FoundryDepthCover.Answer.Open)
                        _sink++;
            });
            Piece(t, "view: CameraRefusal(the gameplay camera), CoverRefusal()", () =>
            {
                if (FoundryVisibilityView.CameraRefusal(camera) == null) _sink++;
                if (visibility.CoverRefusal() == null) _sink++;
            });
            Piece(t, "effects: every slot's SurfaceEffect and SurfaceNumber", () =>
            {
                if (effects == null)
                    return;
                for (int i = 0; i < effects.EffectSlotCount; i++)
                    if (effects.SurfaceEffect(i, out Vector3 _, out bool _, out SpriteRenderer _) != null) _sink++;
                for (int i = 0; i < effects.NumberSlotCount; i++)
                    if (effects.SurfaceNumber(i, out Vector3 _) != null) _sink++;
            });
            Piece(t, "drops and chest: ShownCount, ShownBody, Body", () =>
            {
                for (int i = 0; drops != null && i < drops.ShownCount; i++)
                    if (drops.ShownBody(i) != null) _sink++;
                if (chest != null && chest.Body != null) _sink++;
            });
            return t.ToString();
        }

        // A piece counted right after a full collection (what Unity makes again once it has let wrappers go), then once
        // more at once (what the calls allocate every time).
        private static void Piece(StringBuilder t, string name, System.Action action)
        {
            action();
            Collect();
            int afterCollection = Allocations(action);
            int again = Allocations(action);
            t.Append("   ").Append(name).Append(": ").Append(afterCollection).Append(" after a collection, then ").Append(again)
                .AppendLine();
        }

        // What the counts say, in the terms of the class comment.
        private static string Reading(int[] cameraless, int[] gated, int cameralessAfterCollection, int gatedAfterCollection,
            int logs)
        {
            if (logs > 0 && Sum(cameraless) + Sum(gated) > 0)
                return "reading: messages were logged while the passes were measured, and the log's handlers allocate - " +
                       "find what logs (the first message above) before reading the counts";
            bool every = cameraless[Passes - 1] > 0 || gated[Passes - 1] > 0;
            bool firstOnly = !every && (cameraless[0] > 0 || gated[0] > 0);
            bool collected = cameralessAfterCollection > 0 || gatedAfterCollection > 0;
            if (every)
                return "reading: the passes allocate on every call - the pieces above that allocate on their second count " +
                       "name the calls";
            if (firstOnly)
                return "reading: only the first pass after the warm-up allocates - something is made once: an object made " +
                       "lazily, or the JIT compiling the measured call site (a string literal compiled into it, an inlined " +
                       "callee's included, allocates a temporary string)";
            if (collected)
                return "reading: the passes allocate nothing in a row, but do right after a collection - Unity made native " +
                       "objects' managed wrappers again; a collection between a warm-up and its measurement shows up so";
            return "reading: nothing allocates, in a row or after a collection";
        }

        private static int Sum(int[] counts)
        {
            int sum = 0;
            foreach (int count in counts)
                sum += count;
            return sum;
        }

        private static string Join(int[] counts)
        {
            var t = new StringBuilder();
            for (int i = 0; i < counts.Length; i++)
                t.Append(i > 0 ? ", " : string.Empty).Append(counts[i]);
            return t.ToString();
        }
    }
}
