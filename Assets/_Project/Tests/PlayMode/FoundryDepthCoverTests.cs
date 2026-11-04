using System.Collections.Generic;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The silhouette gate's geometry on its own, without a scene (review 2026-11-04): the depth cover never answers Open
    // over a point a depth-writing triangle covers nearer than the body, it answers Unknown for anything it cannot vouch
    // for, and the box FoundryGateDoorMotion.Sweep gives a gate-leaf vertex holds every pose the leaf takes as it swings.
    public sealed class FoundryDepthCoverTests
    {
        // Random triangles, large and small, over a cover; every sampled point inside each must read at most the
        // triangle's nearest depth, and a body standing just behind it there must not be answered Open.
        [Test]
        public void ATriangleCoversEveryPointInsideItAtItsNearestDepth()
        {
            var random = new System.Random(7);
            int checks = 0;
            for (int trial = 0; trial < 1500; trial++)
            {
                var cover = new FoundryDepthCover(new Vector2(-10f, -10f), new Vector2(10f, 10f));
                Vector3 a = Point(random), b = Point(random), c = Point(random);
                if (trial % 3 == 0)
                {
                    // A small triangle, inside a cell or two: the cover's shortcut marks its whole box.
                    b = new Vector3(a.x + (float)random.NextDouble() * .4f, a.y + (float)random.NextDouble() * .4f, b.z);
                    c = new Vector3(a.x + (float)random.NextDouble() * .4f, a.y - (float)random.NextDouble() * .4f, c.z);
                }
                cover.AddTriangle(a, b, c);
                cover.Complete = true;
                float near = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
                for (int s = 0; s < 60; s++)
                {
                    double u = random.NextDouble(), v = random.NextDouble();
                    if (u + v > 1d) { u = 1d - u; v = 1d - v; }
                    var p = new Vector2((float)(a.x + u * (b.x - a.x) + v * (c.x - a.x)),
                        (float)(a.y + u * (b.y - a.y) + v * (c.y - a.y)));
                    Assert.That(cover.NearestAt(p), Is.LessThanOrEqualTo(near),
                        $"Triangle {a} {b} {c}: the point {p.x}, {p.y} is under it.");
                    Assert.That(cover.Ask(p - Vector2.one * .01f, p + Vector2.one * .01f, near + .05f, FoundryVisibilityView.CoverMargin),
                        Is.EqualTo(FoundryDepthCover.Answer.Covered), $"Triangle {a} {b} {c}: a body just behind it at {p.x}, {p.y}.");
                    checks++;
                }
            }
            Assert.That(checks, Is.EqualTo(1500 * 60));
        }

        [Test]
        public void TheCoverAnswersUnknownForWhatItCannotVouchFor()
        {
            var cover = new FoundryDepthCover(Vector2.zero, Vector2.one);
            Vector2 min = new Vector2(.2f, .2f), max = new Vector2(.3f, .3f);
            Assert.That(cover.Ask(min, max, 50f, .1f), Is.EqualTo(FoundryDepthCover.Answer.Unknown), "Unfinished.");
            cover.Complete = true;
            Assert.That(cover.Ask(min, max, 50f, .1f), Is.EqualTo(FoundryDepthCover.Answer.Open), "Nothing writes depth there.");
            Assert.That(cover.Ask(min, new Vector2(5f, .3f), 50f, .1f), Is.EqualTo(FoundryDepthCover.Answer.Unknown),
                "A box reaching beyond the cells.");
            Assert.That(cover.Ask(new Vector2(float.NaN, .2f), max, 50f, .1f), Is.EqualTo(FoundryDepthCover.Answer.Unknown), "NaN corner.");
            Assert.That(cover.Ask(min, max, float.NaN, .1f), Is.EqualTo(FoundryDepthCover.Answer.Unknown), "NaN depth.");
            cover.AddBox(new Vector2(.25f, .25f), new Vector2(.26f, .26f), 50f);
            Assert.That(cover.Ask(min, max, 49.95f, .1f), Is.EqualTo(FoundryDepthCover.Answer.Covered),
                "A box at 50 over a body at 49.95: within the margin.");
            Assert.That(cover.Ask(min, max, 49.85f, .1f), Is.EqualTo(FoundryDepthCover.Answer.Open),
                "The same box over a body more than the margin nearer.");
        }

        // The leaves as the gateway lays them (FoundryRegionView.BuildGateDoorway): each jamb's hinge, vertices from the
        // hinge to the seam, at the landing and up the leaf.
        [Test]
        public void EveryPoseOfAGateLeafStaysInsideItsSweep()
        {
            float face = FoundryRegionView.GateDoorFaceU;
            float leftHinge = 11f - 12f * FoundryRegionView.GateDoorLeftU, rightHinge = 11f - 12f * FoundryRegionView.GateDoorRightU;
            float seam = 11f - 12f * FoundryRegionView.GateDoorSeamU;
            var leaves = new List<FoundryDoorVertex>();
            var shut = new List<Vector3>();
            foreach (float height in new[] { 0f, 1.3f, 2.6f })
                foreach (float along in new[] { 0f, .5f, 1f })
                {
                    AddLeaf(leaves, shut, face, leftHinge, (seam - leftHinge) * along, height);
                    AddLeaf(leaves, shut, face, rightHinge, (seam - rightHinge) * along, height);
                }
            var mesh = new Mesh { name = "Gate leaf sweep check" };
            mesh.SetVertices(shut);
            var holder = new GameObject("Gate leaf sweep check");
            try
            {
                var motion = holder.AddComponent<FoundryGateDoorMotion>();
                motion.Initialize(mesh, leaves, face);
                for (int step = 0; step <= 40; step++)
                {
                    motion.Pose(step / 40f);
                    Vector3[] posed = mesh.vertices;
                    foreach (FoundryDoorVertex leaf in leaves)
                    {
                        FoundryGateDoorMotion.Sweep(leaf, face, out Vector2 low, out Vector2 high);
                        Vector3 p = posed[leaf.Index], rest = shut[leaf.Index];
                        // MovingReach unions the sweep with the vertex's rest; the swing never moves it in depth.
                        low = Vector2.Min(low, rest); high = Vector2.Max(high, rest);
                        const float slack = 1e-4f;
                        Assert.That(p.x >= low.x - slack && p.x <= high.x + slack && p.y >= low.y - slack && p.y <= high.y + slack,
                            Is.True, $"Leaf vertex {leaf.Index} at opening {step / 40f}: {p} outside {low} to {high}.");
                        Assert.That(p.z, Is.EqualTo(rest.z), "The swing never changes a leaf vertex's depth.");
                    }
                }
            }
            finally
            {
                Object.Destroy(holder);
                Object.Destroy(mesh);
            }
        }

        private static void AddLeaf(List<FoundryDoorVertex> leaves, List<Vector3> shut, float face, float hinge, float reach,
            float height)
        {
            leaves.Add(new FoundryDoorVertex(shut.Count, hinge, reach, height));
            shut.Add(FoundrySurface.Project(face, hinge + reach) + Vector3.up * height + Vector3.forward * 52f);
        }

        private static Vector3 Point(System.Random random) =>
            new Vector3((float)(random.NextDouble() * 16d - 8d), (float)(random.NextDouble() * 16d - 8d),
                (float)(random.NextDouble() * 10d + 45d));
    }
}
