using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry's silhouette gate (FoundryVisibilityView with FoundryDepthCover): an enemy, drop or chest body wears the
    // two-pass silhouette material only in a render where a structure may cover it. In the depth proof (DepthProofArrangement)
    // the enemy, the chest and the drop behind the south-west boiler keep their silhouettes for the gameplay camera, and the
    // enemy in the open court in front of the hero draws in one pass, in its own material. A view that is not flat
    // (perspective, tilted, over depth it did not clear), the gate turned off, or a placement made with no camera, puts every
    // body in its silhouette material as before. And the cover holds every structure triangle that may write depth, in the
    // pose it stands in and with the gate's leaves swung open. FoundryXrayGateRenderTests compares the pixels themselves.
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

            visibility.ApplyActorDepth(camera);
            Assert.That(visibility.IsGated, Is.True, "The gameplay camera sees the region flat.");
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
            int registered = visibility.XrayedCount;
            visibility.RestoreActorDepth();
            Assert.That(exposed.sharedMaterial, Is.SameAs(own), "Between renders each body keeps what the last render gave it.");
            Assert.That(hidden.sharedMaterial, Is.SameAs(xray));

            // With no camera known every body wears its silhouette material, as before the gate; the next flat render gives
            // the court enemy its own back and forgets it, so nobody is ever registered twice.
            visibility.ApplyActorDepth();
            Assert.That(visibility.IsGated, Is.False);
            Assert.That(exposed.sharedMaterial, Is.SameAs(xray));
            Assert.That(hidden.sharedMaterial, Is.SameAs(xray));
            visibility.RestoreActorDepth();
            visibility.ApplyActorDepth(camera);
            Assert.That(exposed.sharedMaterial, Is.SameAs(own));
            Assert.That(hidden.sharedMaterial, Is.SameAs(xray));
            Assert.That(visibility.XrayedCount, Is.EqualTo(registered), "Given back, the court enemy is forgotten.");
            visibility.RestoreActorDepth();

            // Views that are not flat, and the gate turned off, put every body in its silhouette material.
            AssertUngated(visibility, camera, () => camera.orthographic = false, () => camera.orthographic = true, exposed, xray,
                "A perspective view");
            Quaternion straight = camera.transform.rotation;
            AssertUngated(visibility, camera, () => camera.transform.rotation = Quaternion.Euler(0f, 2f, 0f),
                () => camera.transform.rotation = straight, exposed, xray, "A tilted view");
            CameraClearFlags clear = camera.clearFlags;
            AssertUngated(visibility, camera, () => camera.clearFlags = CameraClearFlags.Nothing, () => camera.clearFlags = clear,
                exposed, xray, "A view over depth it did not clear");
            AssertUngated(visibility, camera, () => visibility.GateByCover = false, () => visibility.GateByCover = true, exposed, xray,
                "The gate turned off");

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
            TestContext.Out.WriteLine("Depth cover: " + cover.CellCount + " cells of " + FoundryDepthCover.CellSize + " units, " +
                cover.CellCount * 4 / 1024 + " KB.");

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

        // The view in a placement it cannot gate puts the court enemy, as every body, in its silhouette material.
        private static void AssertUngated(FoundryVisibilityView visibility, Camera camera, System.Action change, System.Action undo,
            SpriteRenderer exposed, Material xray, string what)
        {
            change();
            try
            {
                visibility.ApplyActorDepth(camera);
                Assert.That(visibility.IsGated, Is.False, what + " is not gated.");
                Assert.That(exposed.sharedMaterial, Is.SameAs(xray), what + ": every body wears its silhouette material.");
                visibility.RestoreActorDepth();
            }
            finally
            {
                undo();
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

        // The cover's answer for a body, for a failure's message.
        private static string Describe(FoundryVisibilityView visibility, SpriteRenderer body)
        {
            Bounds bounds = body.bounds;
            Vector3 region = visibility.transform.position;
            return "bounds " + bounds.min.ToString("F2") + " to " + bounds.max.ToString("F2") + ", cover " +
                   visibility.DepthCover.MayHide(bounds.min - region, bounds.max - region, bounds.max.z - region.z,
                       FoundryVisibilityView.CoverMargin);
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
