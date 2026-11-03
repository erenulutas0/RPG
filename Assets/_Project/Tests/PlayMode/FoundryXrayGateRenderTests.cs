using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cryptforge.Art;
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
    // The silhouette gate changes no pixel: the gameplay camera renders each frame twice into the phone's aspect, once with
    // the gate (FoundryVisibilityView.GateByCover, bodies no structure can cover in their own one-pass material) and once
    // without (every enemy, drop and chest body in its two-pass silhouette material, as before the gate), and the two
    // images must match. Every gated render must have been gated (the view's GateRefusal, into a target with depth). The
    // depth proof frames the enemy, chest and drop behind the south-west boiler beside the enemy in the open court; the
    // crowd frames the proof floor's wave, padded to the probe's crowd of 40 (development/horde-probe.txt) and frozen
    // (time scale 0) once it stands, spread over the floor round many points of the map, beside every kind of structure,
    // so that both materials are in play. A differing pixel is reported with whether it lies on a body the gate gave one
    // pass, where a missing silhouette would show. Needs a graphics device, so it runs by name only:
    //   pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -Filter Cryptforge.Tests.FoundryXrayGateRenderTests -Graphics -SkipApk
    [Explicit("Needs a graphics device: run it by name with Tools/Run-UnityTests.ps1 -Graphics.")]
    public sealed class FoundryXrayGateRenderTests
    {
        private const string GameplayScene = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const int Width = 540, Height = 1170;
        // Per channel, out of 255: room for rounding, which is reported but allowed; a silhouette that went missing differs
        // by tens (its fill is 30% ember, its outline 95%) over many pixels.
        private const int Tolerance = 8;
        // The proof floor's wave (10) padded with copies of its first enemy to the scene's cap, as the perf probe's crowd.
        private const int Padding = 30;
        private const int SmallestCrowd = 12;
        private Scene _scene;
        private RenderTexture _target;
        private Texture2D _read;
        private readonly List<RectInt> _onePass = new List<RectInt>();

        [SetUp] public void Begin() => TestProfile.Begin();

        [UnityTearDown]
        public IEnumerator End()
        {
            Camera camera = Camera.main;
            if (camera != null)
                camera.targetTexture = null;
            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }
            if (_read != null)
                Object.Destroy(_read);
            Time.timeScale = 1f;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Silhouette gate render cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            Delete(DevelopmentStart.DepthProofPath);
            Delete(DevelopmentStart.RegionPreviewPath);
            Delete(DevelopmentStart.StartFloorPath);
            Delete(DevelopmentStart.HordeProbePath);
            TestProfile.End();
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator TheDepthProofLooksTheSameGatedAndUngated()
        {
            RequireGraphics();
            Write(DevelopmentStart.RegionPreviewPath, "foundry");
            Write(DevelopmentStart.DepthProofPath, "on");
            LogAssert.Expect(LogType.Log, new Regex(@"^\[DepthProof\] arranged "));
            yield return Load();
            DepthProofArrangement proof = GameObject.Find(DepthProofArrangement.ObjectName).GetComponent<DepthProofArrangement>();
            Camera camera = Camera.main;
            var follow = camera.GetComponent<ArenaCameraFollow>();
            camera.transform.position = follow.Goal();
            var visibility = Object.FindFirstObjectByType<FoundryVisibilityView>();
            var looks = new List<EnemyLookView>(Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None));

            var tally = new Tally();
            Compare(camera, visibility, looks, "the depth proof", tally);
            TestContext.Out.WriteLine(tally.ToString());
            Assert.That(tally.Refusal, Is.Null, "Every gated render was gated: " + tally);
            Assert.That(proof.Hidden.GetComponent<EnemyLookView>().Body.sharedMaterial.shader.name, Is.EqualTo(FoundryVisibilityView.XrayShaderName),
                "Gated, the enemy behind the boiler keeps its silhouette.");
            Assert.That(tally.Plain, Is.GreaterThan(0), "Gated, the court enemy draws in one pass: " + tally);
            Assert.That(tally.Different, Is.Zero, tally.ToString());
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator ACrowdRoundTheWholeFoundryLooksTheSameGatedAndUngated()
        {
            RequireGraphics();
            Write(DevelopmentStart.RegionPreviewPath, "foundry");
            Write(DevelopmentStart.StartFloorPath, "floor_density_proof");
            // The proof floor's one wave is ten strong, all of it on the floor at once: padded to the probe's crowd (which
            // also keeps the hero standing), and frozen as soon as it stands, so that no blow fells anyone while the frames
            // run for every newcomer's Start. Waiting with the fight on, the hero felled the ten and the crowd stood at 0.
            Write(DevelopmentStart.HordeProbePath, Padding.ToString(System.Globalization.CultureInfo.InvariantCulture));
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            for (int frame = 0; frame < 600 && setup.Encounters.AllAliveCount < SmallestCrowd; frame++)
                yield return null;
            Time.timeScale = 0f;
            Assert.That(setup.Encounters.AllAliveCount, Is.GreaterThanOrEqualTo(SmallestCrowd), "The proof floor's crowd stands on the floor.");
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            Camera camera = Camera.main;
            camera.GetComponent<ArenaCameraFollow>().enabled = false;
            var visibility = Object.FindFirstObjectByType<FoundryVisibilityView>();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            var looks = new List<EnemyLookView>();
            foreach (EnemyLookView look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.isActiveAndEnabled && look.GetComponent<Health>().IsAlive)
                    looks.Add(look);
            Vector3 cameraRest = camera.transform.position;
            float halfHeight = camera.orthographicSize, halfWidth = halfHeight * Width / Height;

            // Every walkable point of a coarse grid over the map frames the crowd, spread over the floor in view.
            var tally = new Tally();
            int framings = 0;
            for (int u = FoundryMap.West + 2; u <= FoundryMap.GateLandingEnd; u += 5)
                for (int v = FoundryMap.South + 2; v <= FoundryMap.North - 2; v += 5)
                {
                    if (!FoundryMap.Cell(u, v))
                        continue;
                    FoundryMap.ToFloor(u + .5f, v + .5f, out float x, out float y);
                    var centre = new Vector3(x, ArenaFloor.WorldY(y), 0f);
                    centre.y += FoundrySurface.Lift(centre, arena.transform.position);
                    camera.transform.position = new Vector3(centre.x, centre.y + 1f, cameraRest.z);
                    Spread(looks, centre, halfWidth, halfHeight);
                    Compare(camera, visibility, looks, "design (" + u + ", " + v + ")", tally);
                    framings++;
                }
            TestContext.Out.WriteLine(looks.Count + " bodies over " + framings + " framings: " + tally);
            Assert.That(framings, Is.GreaterThan(20));
            Assert.That(tally.Refusal, Is.Null, "Every gated render was gated: " + tally);
            Assert.That(tally.Plain, Is.GreaterThan(0), "Bodies in the open draw in one pass: " + tally);
            Assert.That(tally.Silhouetted, Is.GreaterThan(0), "Bodies by the structures keep their silhouettes: " + tally);
            Assert.That(tally.Different, Is.Zero, tally.ToString());
        }

        private sealed class Tally
        {
            public int Plain, Silhouetted, Images, Different, OnOnePass, Noisy, Worst;
            // The view's own counts over the gated renders (FoundryVisibilityView.DescribeGate), and the first refusal.
            public int OnePass, Covered, BeyondCover, Unmeasured;
            public string First, Refusal;
            public override string ToString() =>
                Images + " image pairs; " + Different + " pixels differ by more than " + Tolerance + "/255 (worst " + Worst + ", first " +
                First + "; " + OnOnePass + " of them on a body gated to one pass), " + Noisy + " by 1-" + Tolerance +
                "; bodies in view gated to one pass " + Plain + ", in their silhouette " + Silhouetted + "; the view's counts: " +
                OnePass + " one pass, " + Covered + " a structure may cover, " + BeyondCover + " beyond the cover, " +
                Unmeasured + " unmeasured" +
                (Refusal != null ? "; NOT GATED: " + Refusal : string.Empty);
        }

        // The camera's render with the gate and without it, compared; the gated render's bodies counted by material.
        private void Compare(Camera camera, FoundryVisibilityView visibility, List<EnemyLookView> looks, string where, Tally tally)
        {
            if (_target == null)
            {
                _target = new RenderTexture(Width, Height, 24) { name = "Silhouette gate target" };
                _read = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            }
            camera.targetTexture = _target;
            visibility.GateByCover = true;
            Color32[] gated = Render(camera);
            if (tally.Refusal == null && visibility.GateRefusal != null)
                tally.Refusal = where + ": " + visibility.GateRefusal;
            tally.OnePass += visibility.OnePassBodies;
            tally.Covered += visibility.CoveredBodies;
            tally.BeyondCover += visibility.BeyondCoverBodies;
            tally.Unmeasured += visibility.UnmeasuredBodies;
            _onePass.Clear();
            foreach (EnemyLookView look in looks)
            {
                if (look == null || !look.isActiveAndEnabled || !InView(camera, look.Body))
                    continue;
                if (look.Body.sharedMaterial != null && look.Body.sharedMaterial.shader.name == FoundryVisibilityView.XrayShaderName)
                    tally.Silhouetted++;
                else
                {
                    tally.Plain++;
                    _onePass.Add(PixelRect(camera, look.Body));
                }
            }
            visibility.GateByCover = false;
            Color32[] ungated = Render(camera);
            visibility.GateByCover = true;
            tally.Images++;
            for (int i = 0; i < gated.Length; i++)
            {
                int d = Mathf.Max(Mathf.Max(Mathf.Abs(gated[i].r - ungated[i].r), Mathf.Abs(gated[i].g - ungated[i].g)),
                    Mathf.Max(Mathf.Abs(gated[i].b - ungated[i].b), Mathf.Abs(gated[i].a - ungated[i].a)));
                if (d == 0)
                    continue;
                if (d <= Tolerance)
                {
                    tally.Noisy++;
                    continue;
                }
                tally.Different++;
                if (OnOnePassBody(i % Width, i / Width))
                    tally.OnOnePass++;
                if (d > tally.Worst)
                    tally.Worst = d;
                if (tally.First == null)
                    tally.First = where + " pixel (" + i % Width + ", " + i / Width + ")";
            }
        }

        // Where a body the gate gave one pass drew, in the target's pixels: its bounds where it rests, raised by the most a
        // ramp lifts it in the render, and a pixel to spare.
        private static RectInt PixelRect(Camera camera, SpriteRenderer body)
        {
            Bounds bounds = body.bounds;
            Vector3 a = camera.WorldToViewportPoint(bounds.min), b = camera.WorldToViewportPoint(bounds.max + Vector3.up * FoundrySurface.Rise);
            int x0 = Mathf.FloorToInt(a.x * Width) - 1, y0 = Mathf.FloorToInt(a.y * Height) - 1;
            int x1 = Mathf.CeilToInt(b.x * Width) + 1, y1 = Mathf.CeilToInt(b.y * Height) + 1;
            return new RectInt(x0, y0, x1 - x0, y1 - y0);
        }

        private bool OnOnePassBody(int x, int y)
        {
            foreach (RectInt rect in _onePass)
                if (x >= rect.xMin && x < rect.xMax && y >= rect.yMin && y < rect.yMax)
                    return true;
            return false;
        }

        private Color32[] Render(Camera camera)
        {
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            _read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            _read.Apply(false);
            RenderTexture.active = previous;
            return _read.GetPixels32();
        }

        // The wave stood on a grid of walkable floor round the framing's centre, inside the view: some in the open, some by
        // the structures there. Rendering follows at once, so nothing walks in between. No two spots share a height: two
        // overlapping bodies at one height tie in the sort along the camera's up axis, and the order of a tie may fall to
        // their materials, which the gate changes. A walking crowd all but never holds such a tie; the grid would.
        private static void Spread(List<EnemyLookView> looks, Vector3 centre, float halfWidth, float halfHeight)
        {
            int placed = 0, tries = 0;
            int columns = 5;
            while (placed < looks.Count && tries < looks.Count * 8)
            {
                int k = tries++;
                float across = ((k % columns) + .5f) / columns * 2f - 1f;
                float up = ((k / columns) % 9 + .5f) / 9f * 2f - 1f;
                float jitter = (k * 37 % 11) / 11f - .5f;
                var spot = new Vector3(centre.x + across * halfWidth * .9f + jitter * .3f,
                    centre.y + up * halfHeight * .8f + k * .0037f, 0f);
                FoundryMap.ToDesign(spot.x, ArenaFloor.FloorY(spot.y), out float u, out float v);
                if (!FoundryMap.Cell(Mathf.FloorToInt(u), Mathf.FloorToInt(v)))
                    continue;
                EnemyLookView look = looks[placed++];
                if (look != null)
                    look.transform.position = spot;
            }
        }

        private static bool InView(Camera camera, SpriteRenderer body)
        {
            Bounds bounds = body.bounds;
            Vector3 a = camera.WorldToViewportPoint(bounds.min), b = camera.WorldToViewportPoint(bounds.max);
            return b.x >= 0f && a.x <= 1f && b.y >= 0f && a.y <= 1f;
        }

        private static void RequireGraphics()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Needs a graphics device: run with Tools/Run-UnityTests.ps1 -Graphics.");
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(GameplayScene);
            _scene = SceneManager.GetActiveScene();
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
