using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry's silhouette gate (FoundryVisibilityView with FoundryDepthCover): an enemy, drop or chest body wears the
    // two-pass silhouette material only in a render where a structure may cover it. In the depth proof (DepthProofArrangement)
    // the enemy, the chest and the drop behind the south-west boiler keep their silhouettes for the gameplay camera, and the
    // enemy in the open court in front of the hero draws in one pass, in its own material; so it does for the perf probe's
    // phone target (checked with a graphics device only). A view that is not flat (perspective, tilted, over depth it did
    // not clear, into a target without depth), the gate turned off, or a placement made with no camera, puts every body in
    // its silhouette material as before, and the view names the condition that failed (GateRefusal). Every body's footprint
    // holds every vertex its sprite draws, at the body's own depth. And the cover holds every structure triangle that may
    // write depth, in the pose it stands in and with the gate's leaves swung open. FoundryXrayGateRenderTests compares the
    // pixels themselves.
    public sealed class FoundryXrayGateTests
    {
        private const string GameplayScene = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string CutawayShader = "Cryptforge/Foundry Cutaway";
        private Scene _scene;

        [SetUp] public void Begin() => TestProfile.Begin();

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1f;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Silhouette gate cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            // The profile folder goes with TestProfile.End; the flags go first, so no later load can meet them.
            Delete(DevelopmentStart.DepthProofPath);
            Delete(DevelopmentStart.RegionPreviewPath);
            TestProfile.End();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator OnlyTheBodiesAStructureMayCoverWearTheSilhouetteForTheGameplayCamera()
        {
            Write(DevelopmentStart.RegionPreviewPath, "foundry");
            Write(DevelopmentStart.DepthProofPath, "on");
            LogAssert.Expect(LogType.Log, new Regex(@"^\[DepthProof\] arranged "));
            yield return Load();

            DepthProofArrangement proof = GameObject.Find(DepthProofArrangement.ObjectName).GetComponent<DepthProofArrangement>();
            Assert.That(proof.Hidden, Is.Not.Null);
            Assert.That(proof.Exposed, Is.Not.Null);
            SpriteRenderer hidden = proof.Hidden.GetComponent<EnemyLookView>().Body;
            SpriteRenderer exposed = proof.Exposed.GetComponent<EnemyLookView>().Body;
            SpriteRenderer chest = Object.FindFirstObjectByType<ChestSpawner>().Body;
            var drops = Object.FindFirstObjectByType<GroundDropView>();
            Assert.That(drops.ShownCount, Is.EqualTo(1), "The proof's drop is on show.");
            SpriteRenderer drop = drops.ShownBody(0);
            var visibility = Object.FindFirstObjectByType<FoundryVisibilityView>();
            Camera camera = Camera.main;
            Assert.That(visibility.DepthCover, Is.Not.Null, "The region makes its depth cover.");
            Assert.That(visibility.DepthCover.Complete, Is.True);

            // Each body's own material: what the view gives back when it stops.
            visibility.enabled = false;
            Material own = exposed.sharedMaterial;
            Material hiddenOwn = hidden.sharedMaterial, chestOwn = chest.sharedMaterial, dropOwn = drop.sharedMaterial;
            Assert.That(own.shader.name, Is.EqualTo("Sprites/Default"));
            Assert.That(own.passCount, Is.EqualTo(1));
            visibility.enabled = true;

            // The gameplay camera as the scene has it: orthographic, clearing to a colour, straight along +z, onto the screen.
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.clearFlags, Is.Not.EqualTo(CameraClearFlags.Nothing));
            Assert.That(camera.targetTexture, Is.Null);
            Assert.That(Vector3.Angle(camera.transform.forward, Vector3.forward), Is.LessThan(1e-3f));
            visibility.ApplyActorDepth(camera);
            TestContext.Out.WriteLine("The gameplay camera on the depth proof: " + visibility.DescribeGate());
            Assert.That(visibility.GateRefusal, Is.Null, visibility.DescribeGate());
            Assert.That(visibility.IsGated, Is.True, "The gameplay camera sees the region flat.");
            AssertFootprints(visibility, hidden, exposed, chest, drop);
            Material xray = hidden.sharedMaterial;
            Assert.That(xray.shader.name, Is.EqualTo(FoundryVisibilityView.XrayShaderName),
                "The boiler stands in front of the hidden enemy: it keeps its silhouette.");
            Assert.That(xray.passCount, Is.EqualTo(2));
            Assert.That(exposed.sharedMaterial, Is.SameAs(own),
                "No structure can cover the court enemy (" + Describe(visibility, exposed) + "): one pass, in its own material.");
            Material loot = chest.sharedMaterial;
            Assert.That(loot.shader.name, Is.EqualTo(FoundryVisibilityView.XrayShaderName), "The chest behind the boiler keeps its loot silhouette.");
            Assert.That(loot, Is.Not.SameAs(xray));
            Assert.That(drop.sharedMaterial, Is.SameAs(loot), "So does the drop behind it.");
            // The view's own count agrees: the three behind the boiler covered and the court enemy in one pass (the wave's
            // others count where they stopped), and the four are measured inside the cover.
            Assert.That(visibility.CoveredBodies, Is.GreaterThanOrEqualTo(3), visibility.DescribeGate());
            Assert.That(visibility.OnePassBodies, Is.GreaterThanOrEqualTo(1), visibility.DescribeGate());
            Assert.That(visibility.ShownBodies,
                Is.EqualTo(visibility.CoveredBodies + visibility.OnePassBodies + visibility.BeyondCoverBodies +
                           visibility.UnmeasuredBodies));
            foreach (SpriteRenderer body in new[] { hidden, exposed, chest, drop })
            {
                FoundryVisibilityView.Footprint(body, visibility.transform.position, out Vector2 min, out Vector2 max, out float far);
                Assert.That(visibility.DepthCover.Ask(min, max, far, FoundryVisibilityView.CoverMargin),
                    Is.Not.EqualTo(FoundryDepthCover.Answer.Unknown), body.name + " stands inside the cover: " + Describe(visibility, body));
            }
            int registered = visibility.XrayedCount;
            visibility.RestoreActorDepth();
            Assert.That(exposed.sharedMaterial, Is.SameAs(own), "Between renders each body keeps what the last render gave it.");
            Assert.That(hidden.sharedMaterial, Is.SameAs(xray));

            // With no camera known every body wears its silhouette material, as before the gate; the next flat render gives
            // the court enemy its own back and forgets it, so nobody is ever registered twice.
            visibility.ApplyActorDepth();
            Assert.That(visibility.IsGated, Is.False);
            Assert.That(visibility.GateRefusal, Is.EqualTo(FoundryVisibilityView.NoCamera));
            Assert.That(exposed.sharedMaterial, Is.SameAs(xray));
            Assert.That(hidden.sharedMaterial, Is.SameAs(xray));
            visibility.RestoreActorDepth();
            visibility.ApplyActorDepth(camera);
            Assert.That(exposed.sharedMaterial, Is.SameAs(own));
            Assert.That(hidden.sharedMaterial, Is.SameAs(xray));
            Assert.That(visibility.XrayedCount, Is.EqualTo(registered), "Given back, the court enemy is forgotten.");
            visibility.RestoreActorDepth();

            // The perf probe's phone target (1080 by 2340 with a 24-bit depth buffer) is gated as the screen is, and a target
            // without depth is not. Only with a graphics device (Tools/Run-UnityTests.ps1 -Graphics), where a camera may own
            // a target; the render tests draw into one.
            bool graphics = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            if (graphics)
            {
                var phone = new RenderTexture(1080, 2340, 24) { name = "Silhouette gate phone target" };
                try
                {
                    camera.targetTexture = phone;
                    visibility.ApplyActorDepth(camera);
                    Assert.That(visibility.GateRefusal, Is.Null, "The probe's target: " + visibility.DescribeGate());
                    Assert.That(exposed.sharedMaterial, Is.SameAs(own));
                    visibility.RestoreActorDepth();
                }
                finally
                {
                    camera.targetTexture = null;
                    Object.Destroy(phone);
                }
                var flatTarget = new RenderTexture(64, 64, 0) { name = "Silhouette gate target without depth" };
                try
                {
                    AssertUngated(visibility, camera, () => camera.targetTexture = flatTarget, () => camera.targetTexture = null,
                        exposed, xray, FoundryVisibilityView.TargetWithoutDepth);
                }
                finally
                {
                    Object.Destroy(flatTarget);
                }
            }

            // Views that are not flat, and the gate turned off, put every body in its silhouette material and say why.
            AssertUngated(visibility, camera, () => camera.orthographic = false, () => camera.orthographic = true, exposed, xray,
                FoundryVisibilityView.PerspectiveCamera);
            Quaternion straight = camera.transform.rotation;
            AssertUngated(visibility, camera, () => camera.transform.rotation = Quaternion.Euler(0f, 2f, 0f),
                () => camera.transform.rotation = straight, exposed, xray, FoundryVisibilityView.TurnedCamera);
            CameraClearFlags clear = camera.clearFlags;
            AssertUngated(visibility, camera, () => camera.clearFlags = CameraClearFlags.Nothing, () => camera.clearFlags = clear,
                exposed, xray, FoundryVisibilityView.UnclearedCamera);
            AssertUngated(visibility, camera, () => visibility.GateByCover = false, () => visibility.GateByCover = true, exposed, xray,
                FoundryVisibilityView.GateOff);

            // Stopped, the view gives every body its own material back, whichever it wore.
            visibility.ApplyActorDepth(camera);
            Assert.That(exposed.sharedMaterial, Is.SameAs(own));
            visibility.RestoreActorDepth();
            visibility.enabled = false;
            Assert.That(hidden.sharedMaterial, Is.SameAs(hiddenOwn));
            Assert.That(exposed.sharedMaterial, Is.SameAs(own));
            Assert.That(chest.sharedMaterial, Is.SameAs(chestOwn));
            Assert.That(drop.sharedMaterial, Is.SameAs(dropOwn));
            visibility.enabled = true;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheCoverHoldsEveryStructureTriangleThatMayWriteDepth()
        {
            Write(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return Load();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.Region, Is.Not.Null);
            var visibility = arena.Region.GetComponent<FoundryVisibilityView>();
            FoundryDepthCover cover = visibility.DepthCover;
            Assert.That(cover, Is.Not.Null);
            Assert.That(cover.Complete, Is.True);
            Vector2 size = cover.Max - cover.Min;
            TestContext.Out.WriteLine("Depth cover: " + cover.CellCount + " cells of " + FoundryDepthCover.CellSize + " units, " +
                cover.CellCount * 4 / 1024 + " KB, region-local " + cover.Min.ToString("F1") + " to " + cover.Max.ToString("F1") +
                " (" + (size.x * size.y).ToString("F0") + " square units against the cap of " + 200 * 200 + ").");
            // Only the structure ranks span it: the distant works beyond the island (FoundryRegionDistantWorks) are far
            // scenery ranks that write no depth, so they do not stretch it towards the cap that would abandon it.
            Assert.That(size.x * size.y, Is.LessThan(200f * 200f));

            // The flames and banners in a few of the poses their motion gives them.
            int triangles = AssertCovered(arena, cover, "now");
            Assert.That(triangles, Is.GreaterThan(500), "The foundry's structures write depth.");
            for (int pose = 0; pose < 4; pose++)
            {
                for (int frame = 0; frame < 7; frame++)
                    yield return null;
                AssertCovered(arena, cover, "after " + (pose + 1) * 7 + " more frames");
            }
            // The gate's leaves half and fully open.
            var doors = arena.Region.GetComponentsInChildren<FoundryGateDoorMotion>();
            Assert.That(doors, Is.Not.Empty, "The gate has its leaves.");
            foreach (float opening in new[] { .5f, 1f })
            {
                foreach (FoundryGateDoorMotion door in doors)
                    door.Pose(opening);
                AssertCovered(arena, cover, "with the gate's leaves " + opening * 100f + "% open");
            }
            foreach (FoundryGateDoorMotion door in doors)
                door.Pose(0f);
        }

        // The view in a placement it cannot gate puts the court enemy, as every body, in its silhouette material, and names
        // the condition that failed.
        private static void AssertUngated(FoundryVisibilityView visibility, Camera camera, System.Action change, System.Action undo,
            SpriteRenderer exposed, Material xray, string refusal)
        {
            change();
            try
            {
                visibility.ApplyActorDepth(camera);
                Assert.That(visibility.IsGated, Is.False, refusal + ": not gated.");
                Assert.That(visibility.GateRefusal, Is.EqualTo(refusal));
                Assert.That(exposed.sharedMaterial, Is.SameAs(xray), refusal + ": every body wears its silhouette material.");
                Assert.That(visibility.OnePassBodies + visibility.CoveredBodies + visibility.BeyondCoverBodies +
                            visibility.UnmeasuredBodies, Is.Zero);
                visibility.RestoreActorDepth();
            }
            finally
            {
                undo();
            }
        }

        // Where the view says each body draws (FoundryVisibilityView.Footprint), as the placement has just put it: every
        // vertex of the sprite the body shows, flipped as Sprites/Default flips it and through the body's transform, lies
        // inside the footprint's box and no farther than its depth, and an unturned body's depth is its own z. The bodies
        // named and every enemy body in the scene.
        private static void AssertFootprints(FoundryVisibilityView visibility, params SpriteRenderer[] named)
        {
            var bodies = new List<SpriteRenderer>(named);
            foreach (EnemyLookView look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.Body != null && !bodies.Contains(look.Body))
                    bodies.Add(look.Body);
            Vector3 region = visibility.transform.position;
            foreach (SpriteRenderer body in bodies)
            {
                Assert.That(FoundryVisibilityView.Footprint(body, region, out Vector2 min, out Vector2 max, out float far), Is.True,
                    body.name + " is measured.");
                Matrix4x4 toWorld = body.transform.localToWorldMatrix;
                var flip = new Vector2(body.flipX ? -1f : 1f, body.flipY ? -1f : 1f);
                foreach (Vector2 vertex in body.sprite.vertices)
                {
                    Vector3 p = toWorld.MultiplyPoint3x4(new Vector3(vertex.x * flip.x, vertex.y * flip.y, 0f)) - region;
                    Assert.That(p.x >= min.x - 1e-4f && p.x <= max.x + 1e-4f && p.y >= min.y - 1e-4f && p.y <= max.y + 1e-4f &&
                                p.z <= far + 1e-4f, Is.True,
                        body.name + " vertex " + p.ToString("F4") + " lies outside its footprint " + min.ToString("F4") + " to " +
                        max.ToString("F4") + ", depth " + far.ToString("F4"));
                }
                Assert.That(far, Is.EqualTo(body.transform.position.z - region.z).Within(1e-4f), body.name + "'s footprint depth.");
                // Unity's own bounds, for the record: the same box, a tenth of a unit deep either way.
                Bounds bounds = body.bounds;
                TestContext.Out.WriteLine(body.name + ": footprint " + min.ToString("F3") + " to " + max.ToString("F3") + " at z " +
                    far.ToString("F3") + "; Renderer.bounds " + (bounds.min - region).ToString("F3") + " to " +
                    (bounds.max - region).ToString("F3"));
            }
        }

        // Every structure triangle that may write depth (a vertex's UV1.x above 0) as it stands now - its corners, its centre
        // and its edges' middles - lies in cells whose nearest depth is no farther than its own nearest corner. Returns how
        // many triangles were looked at.
        private static int AssertCovered(ArenaView arena, FoundryDepthCover cover, string when)
        {
            Vector3 region = arena.Region.transform.position;
            var flags = new List<Vector2>();
            var points = new Vector3[7];
            int triangles = 0, missed = 0;
            string first = null;
            foreach (MeshRenderer renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                Material material = renderer.sharedMaterial;
                if (material == null || material.shader == null || material.shader.name != CutawayShader)
                    continue;
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Vector3[] vertices = mesh.vertices;
                int[] indices = mesh.triangles;
                mesh.GetUVs(1, flags);
                Assert.That(flags.Count, Is.EqualTo(vertices.Length), renderer.name + " carries its depth flags.");
                Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;
                for (int t = 0; t + 2 < indices.Length; t += 3)
                {
                    int a = indices[t], b = indices[t + 1], c = indices[t + 2];
                    if (flags[a].x <= 0f && flags[b].x <= 0f && flags[c].x <= 0f)
                        continue;
                    Vector3 p = toWorld.MultiplyPoint3x4(vertices[a]) - region;
                    Vector3 q = toWorld.MultiplyPoint3x4(vertices[b]) - region;
                    Vector3 r = toWorld.MultiplyPoint3x4(vertices[c]) - region;
                    float near = Mathf.Min(p.z, Mathf.Min(q.z, r.z));
                    points[0] = p; points[1] = q; points[2] = r; points[3] = (p + q + r) / 3f;
                    points[4] = (p + q) * .5f; points[5] = (q + r) * .5f; points[6] = (r + p) * .5f;
                    triangles++;
                    foreach (Vector3 point in points)
                    {
                        float covered = cover.NearestAt(new Vector2(point.x, point.y));
                        if (covered <= near + 1e-4f)
                            continue;
                        missed++;
                        if (first == null)
                            first = renderer.name + " triangle " + t / 3 + " at " + point.ToString("F3") + ": nearest " + near +
                                ", cover " + covered;
                    }
                }
            }
            Assert.That(missed, Is.Zero, missed + " points of structure triangles lie outside the depth cover " + when + "; first " + first);
            return triangles;
        }

        // The cover's answer for a body's footprint, beside its Renderer.bounds and the placement's counts, for a
        // failure's message.
        private static string Describe(FoundryVisibilityView visibility, SpriteRenderer body)
        {
            Bounds bounds = body.bounds;
            Vector3 region = visibility.transform.position;
            if (!FoundryVisibilityView.Footprint(body, region, out Vector2 min, out Vector2 max, out float far))
                return "no footprint; " + visibility.DescribeGate();
            return "footprint " + min.ToString("F2") + " to " + max.ToString("F2") + " at z " + far.ToString("F2") + ", cover " +
                   visibility.DepthCover.Ask(min, max, far, FoundryVisibilityView.CoverMargin) + "; bounds " +
                   bounds.min.ToString("F2") + " to " + bounds.max.ToString("F2") + "; " + visibility.DescribeGate();
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(GameplayScene);
            _scene = SceneManager.GetActiveScene();
            // The depth proof is arranged in the first frame's update, before anything else moves.
            yield return null;
            yield return null;
        }

        private static void Write(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        private static void Delete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
