using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // A measurement, not a check: the hero's first entry onto the foundry's gate ramp, frame by frame, where the S23 once
    // stalled 216.9 ms (docs/23, 2026-10-09) - the frame the ground projection (FoundryGroundProjection) made its material
    // and grids and first drew them. The foundry loads with time stopped, so nothing walks or fights and every frame is the
    // presentation's own; the camera frames the phone's band (as PerfProbe frames it) and follows the hero by hand. The test
    // walks the hero along the causeway's centre line at a walking pace from the causeway's foot up onto the ramp, back
    // down and up again: the first entry, then a second for comparison. Every frame the camera renders - by hand in a batch
    // Editor, into a 1080x2340 target, after the sorting groups are brought forward (PerfProbeRecorder, as PerfProbe does)
    // - and the recorder keeps the frame's wall and main-thread time and every marker on offer: the ground projection's own
    // (PerformanceMarkers.Ground*), the depth pass, the render, the Editor's compiler, and the engine's first-use work where
    // the Editor names it (a GPU program made, a texture or mesh uploaded; FirstUseNames). The ground work done during the
    // load, before the first frame measured, is summed apart. Writes TestResults/ramp-entry.txt and logs one "[RampEntry]"
    // message. Run it by name, with a graphics device for the draws:
    //   pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -Filter Cryptforge.Tests.FoundryRampEntryMeasurement -Graphics -SkipApk
    // The foundry pre-warms its first-use work as it loads (FoundryVisibilityView.PrewarmFirstUse); a line "off" in
    // TestResults/ramp-entry-prewarm.txt loads it as before, for the comparison - one Editor process each way, since a
    // shader's GPU program, once made, lasts the process.
    // Without one nothing renders and only the script side (material, grids, projection) means anything. The Editor's
    // D3D11 makes a GPU program in well under a millisecond; a phone's Vulkan pipeline or GLES link is what this frame
    // cost there, so the Editor shows where the work lands, not what it costs on the S23.
    [Explicit("Measurement: run it by name.")]
    public sealed class FoundryRampEntryMeasurement
    {
        private const string GameplayScene = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const int PhoneWidth = 1080, PhoneHeight = 2340;
        // The phone's free band between the HUD blocks, as PerfProbe frames it.
        private const float BandBottom = 285f, BandTop = PhoneHeight - 100f - 327f;
        // The causeway's centre line, from a unit past its foot onto the ramp's upper half, at a walking pace.
        private const float LaneV = FoundryMap.GateExitV;
        private const float FootU = FoundryMap.East + 1f;
        private const float TopU = FoundrySurface.RampEnd - .5f;
        private const float StepU = 3.5f / 60f;
        private const int SettleFrames = 20, StayFrames = 40;
        // A second entry counts once nothing was projected for this many frames.
        private const int ClearFrames = 20;
        // Frames shown marker by marker round each entry, and the least time a marker needs on a frame to be named.
        private const int Before = 2, After = 4;
        private const float MarkerFloorMs = .05f;
        // The window round an entry whose slowest frame is reported.
        private const int EntryWindow = 30;
        // The engine's first-use work, by the names a Unity 6 Editor may give it; one not on offer is skipped. Read beside
        // the recorder's own markers, summed over every thread.
        private static readonly string[] FirstUseNames =
        {
            "Shader.CreateGPUProgram", "CreateGPUProgram", "CreateGpuProgram", "Shader.CompileGPUProgram",
            "Shader.DynamicLoadGPUProgram", "Shader.Parse", "Shader.ParseMainThread", "Shader.ParseThreaded",
            "ApplyDeferredShaderLoadActions", "ApplyPass.PrepareKeywords", "Material.SetPassFast", "Gfx.UploadTexture",
            "Gfx.UploadTextureData", "Gfx.CreateTexture", "Gfx.CreateTextureThreaded", "Texture.AwakeFromLoad",
            "Mesh.CreateMesh", "Mesh.UploadMeshData", "SpriteRenderer.OnChangeSprite", "Gfx.WaitForRenderThread",
            "Resources.Load", "Loading.ReadObject", "Loading.AwakeFromLoad", "Mono.JIT",
        };

        private Scene _scene;
        private Camera _camera;
        private RenderTexture _before;
        private RenderTexture _target;
        private ArenaCameraFollow _follow;
        private PerfProbeRecorder _recorder;
        private int _frameRate;
        private bool _frameRateTaken;
        private readonly List<ProfilerRecorder> _extra = new List<ProfilerRecorder>();
        private readonly List<ProfilerRecorder> _load = new List<ProfilerRecorder>();

        [UnityTearDown]
        public IEnumerator End()
        {
            if (_recorder != null)
                _recorder.End();
            Dispose(_extra);
            Dispose(_load);
            if (_camera != null && _camera.targetTexture == _target)
                _camera.targetTexture = _before;
            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }
            if (_follow != null)
                _follow.enabled = true;
            if (_frameRateTaken)
                Application.targetFrameRate = _frameRate;
            Time.timeScale = 1f;
            FoundryVisibilityView.PrewarmFirstUse = true;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Ramp entry cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            if (File.Exists(DevelopmentStart.RegionPreviewPath))
                File.Delete(DevelopmentStart.RegionPreviewPath);
            TestProfile.End();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator TheFirstRampEntryIsMeasured()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            string knob = Path.Combine(ResultsFolder, "ramp-entry-prewarm.txt");
            bool prewarm = !(File.Exists(knob) && File.ReadAllText(knob).Trim() == "off");
            FoundryVisibilityView.PrewarmFirstUse = prewarm;
            // The ground's scopes summed over the load: reading the names makes the markers, so they can be recorded first.
            Assert.That(PerformanceMarkers.PickNames, Is.Not.Empty);
            string[] loadNames =
            {
                PerformanceMarkers.GroundPrewarmName, PerformanceMarkers.GroundMaterialName, PerformanceMarkers.GroundGridName
            };
            foreach (string name in loadNames)
                _load.Add(ProfilerRecorder.StartNew(ProfilerCategory.Scripts, name, 4096));

            yield return SceneManager.LoadSceneAsync(GameplayScene);
            _scene = SceneManager.GetActiveScene();
            yield return null;
            Time.timeScale = 0f;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena != null && arena.IsFoundryRegion && arena.Region != null, Is.True, "The foundry loaded.");
            var visibility = arena.Region.GetComponent<FoundryVisibilityView>();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var hero = Object.FindFirstObjectByType<HeroLookView>();
            _camera = Camera.main;
            _follow = _camera.GetComponent<ArenaCameraFollow>();
            bool graphics = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            bool manual = graphics && Application.isBatchMode;
            if (manual)
            {
                _before = _camera.targetTexture;
                _target = new RenderTexture(PhoneWidth, PhoneHeight, 24) { name = "Ramp entry target" };
                _camera.targetTexture = _target;
            }
            _follow.Frame(PhoneWidth, PhoneHeight, BandBottom, BandTop);
            _follow.enabled = false;
            _frameRate = Application.targetFrameRate;
            _frameRateTaken = true;
            Application.targetFrameRate = -1;

            var loadMs = new float[loadNames.Length];
            for (int i = 0; i < loadNames.Length; i++)
                loadMs[i] = Sum(_load[i]);
            Dispose(_load);
            Material materialAtStart = visibility.GroundMaterial;
            int gridsAtStart = visibility.GroundGridCount;
            int projectionsAtStart = visibility.GroundProjections;
            string warmAtStart = "prewarm " + (prewarm ? "on" : "off (ramp-entry-prewarm.txt)") + ", warm-up renderers waiting " +
                visibility.WarmPending + ", drawn in " + visibility.WarmDraws + " renders";

            _recorder = new GameObject("Ramp entry recorder").AddComponent<PerfProbeRecorder>();
            _recorder.Initialize(setup, hero.GetComponent<Health>(), _camera, manual);
            _recorder.Begin();
            var extraNames = new List<string>();
            StartExtra(extraNames);
            int capacity = PerfProbeRecorder.Capacity;
            var extraMs = new float[extraNames.Count][];
            for (int i = 0; i < extraMs.Length; i++)
                extraMs[i] = new float[capacity];
            var heroU = new float[capacity];
            var projected = new int[capacity];
            int lastProjections = visibility.GroundProjections;

            // The walk: settle at the foot, up onto the ramp, stay, down to the foot, stay, up again, stay.
            var path = new List<float>();
            for (int i = 0; i < SettleFrames; i++) path.Add(FootU);
            Walk(path, FootU, TopU);
            for (int i = 0; i < StayFrames; i++) path.Add(TopU);
            Walk(path, TopU, FootU);
            for (int i = 0; i < StayFrames; i++) path.Add(FootU);
            Walk(path, FootU, TopU);
            for (int i = 0; i < StayFrames; i++) path.Add(TopU);

            Vector3 region = arena.Region.transform.position;
            for (int step = 0; step <= path.Count; step++)
            {
                // The values read now are the last frame's: its render ran in its LateUpdate (the recorder's).
                int previous = _recorder.CurrentFrame - 1;
                if (previous >= 0 && previous < capacity)
                {
                    int now = visibility.GroundProjections;
                    projected[previous] = now - lastProjections;
                    lastProjections = now;
                    for (int i = 0; i < _extra.Count; i++)
                        extraMs[i][previous] = _extra[i].Valid && _extra[i].Count > 0 ? _extra[i].LastValue / 1e6f : 0f;
                }
                if (step == path.Count)
                    break;
                float u = path[step];
                FoundryMap.ToFloor(u, LaneV, out float x, out float y);
                hero.transform.position = region + new Vector3(x, y * .5f, 0f);
                _camera.transform.position = _follow.Goal();
                int current = _recorder.CurrentFrame;
                if (current >= 0 && current < capacity)
                    heroU[current] = u;
                yield return null;
            }
            yield return null;
            _recorder.End();

            string report = Report(_recorder, extraNames, extraMs, heroU, projected, graphics, manual, loadNames, loadMs,
                warmAtStart, materialAtStart != null, gridsAtStart, projectionsAtStart, visibility);
            Directory.CreateDirectory(ResultsFolder);
            File.WriteAllText(Path.Combine(ResultsFolder, "ramp-entry.txt"), report);
            TestContext.Out.WriteLine(report);
            LogAssert.Expect(LogType.Log, new Regex(@"^\[RampEntry\] "));
            Debug.Log(report);
            Assert.That(visibility.GroundProjections, Is.GreaterThan(projectionsAtStart), "The hero's floor sprites reached the ramp.");
        }

        private static void Walk(List<float> path, float from, float to)
        {
            int steps = Mathf.CeilToInt(Mathf.Abs(to - from) / StepU);
            for (int i = 1; i <= steps; i++)
                path.Add(Mathf.Lerp(from, to, i / (float)steps));
        }

        // Every name of FirstUseNames on offer that the recorder does not read already.
        private void StartExtra(List<string> names)
        {
            var wanted = new HashSet<string>(FirstUseNames);
            wanted.ExceptWith(_recorder.MarkerNames);
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds || !wanted.Remove(description.Name))
                    continue;
                var recorder = new ProfilerRecorder(handle, 1, ProfilerRecorderOptions.Default);
                recorder.Start();
                _extra.Add(recorder);
                names.Add(description.Name);
            }
        }

        private static string Report(PerfProbeRecorder r, List<string> extraNames, float[][] extraMs, float[] heroU,
            int[] projected, bool graphics, bool manual, string[] loadNames, float[] loadMs, string warmAtStart,
            bool materialAtStart, int gridsAtStart, int projectionsAtStart, FoundryVisibilityView visibility)
        {
            int count = r.Count - 1;
            int first = -1, second = -1, clear = 0;
            for (int f = 0; f < count; f++)
            {
                if (projected[f] > 0)
                {
                    if (first < 0)
                        first = f;
                    else if (second < 0 && clear >= ClearFrames && f > first)
                        second = f;
                    clear = 0;
                }
                else
                    clear++;
            }
            var court = new List<float>();
            for (int f = 1; f < (first > 0 ? first : count); f++)
                court.Add(r.WallMs[f]);
            court.Sort();
            var t = new StringBuilder();
            t.Append("[RampEntry] first entry: ").Append(Entry(r, first, count)).Append("; second entry: ")
                .Append(Entry(r, second, count)).Append("; causeway before it, median ")
                .Append(F(court.Count > 0 ? court[court.Count / 2] : 0f, 2)).Append(" ms over ").Append(court.Count)
                .Append(" frames; frame 0, the first render of the scene in a batch Editor, main thread ")
                .Append(F(count > 0 ? r.MainThreadMs[0] : 0f, 1)).Append(" ms").AppendLine();
            t.Append("   ").Append(Application.unityVersion).Append(", ").Append(SystemInfo.graphicsDeviceType)
                .Append(graphics ? manual ? ", batch Editor: the camera rendered by hand into " + PhoneWidth + "x" + PhoneHeight
                    : ", the Game view renders" : ", no graphics device: nothing drew").AppendLine();
            t.Append("   at load, before the first frame measured: ");
            for (int i = 0; i < loadNames.Length; i++)
                t.Append(i > 0 ? ", " : string.Empty).Append(loadNames[i]).Append(' ').Append(F(loadMs[i], 2)).Append(" ms");
            t.Append("; ground material made ").Append(materialAtStart ? "yes" : "no").Append(", grids ").Append(gridsAtStart)
                .Append(", projections ").Append(projectionsAtStart).Append("; ").Append(warmAtStart).AppendLine();
            t.Append("   at the end: ground material ").Append(visibility.GroundMaterial != null ? "made" : "none").Append(", grids ")
                .Append(visibility.GroundGridCount).Append(", projections ").Append(visibility.GroundProjections)
                .Append("; warm-up renderers waiting ").Append(visibility.WarmPending).Append(", drawn in ")
                .Append(visibility.WarmDraws).Append(" renders").AppendLine();
            t.Append("   markers a frame round each entry (ms, ").Append(F(MarkerFloorMs, 2))
                .AppendLine(" and up, largest first; u is the hero's design u, the ramp starts at " +
                    F(FoundrySurface.RampStart, 0) + "):");
            AppendFrames(t, r, extraNames, extraMs, heroU, projected, first, count);
            AppendFrames(t, r, extraNames, extraMs, heroU, projected, second, count);
            t.AppendLine("   every frame (frame, u, projected, wall ms, main ms):");
            for (int f = 0; f < count; f++)
                t.Append("      ").Append(f).Append('\t').Append(F(heroU[f], 2)).Append('\t').Append(projected[f]).Append('\t')
                    .Append(F(r.WallMs[f], 2)).Append('\t').Append(F(r.MainThreadMs[f], 2)).AppendLine();
            return t.ToString();
        }

        // The entry's frame, its wall and main-thread time, and the slowest frame of the window from two before it.
        private static string Entry(PerfProbeRecorder r, int entry, int count)
        {
            if (entry < 0)
                return "none";
            int worst = entry;
            for (int f = Mathf.Max(0, entry - Before); f < count && f <= entry + EntryWindow; f++)
                if (r.WallMs[f] > r.WallMs[worst])
                    worst = f;
            return "#" + entry + " " + F(r.WallMs[entry], 2) + " ms (main " + F(r.MainThreadMs[entry], 2) + "), slowest of #" +
                Mathf.Max(0, entry - Before) + "-#" + Mathf.Min(count - 1, entry + EntryWindow) + " #" + worst + " " +
                F(r.WallMs[worst], 2) + " ms";
        }

        private static void AppendFrames(StringBuilder t, PerfProbeRecorder r, List<string> extraNames, float[][] extraMs,
            float[] heroU, int[] projected, int entry, int count)
        {
            if (entry < 0)
                return;
            var entries = new List<KeyValuePair<string, float>>();
            for (int f = Mathf.Max(0, entry - Before); f < count && f <= entry + After; f++)
            {
                entries.Clear();
                for (int m = 0; m < r.MarkerNames.Count; m++)
                    if (r.MarkerMs[m][f] >= MarkerFloorMs)
                        entries.Add(new KeyValuePair<string, float>(r.MarkerNames[m], r.MarkerMs[m][f]));
                for (int m = 0; m < extraNames.Count; m++)
                    if (extraMs[m][f] >= MarkerFloorMs)
                        entries.Add(new KeyValuePair<string, float>(extraNames[m], extraMs[m][f]));
                entries.Sort((a, b) => b.Value.CompareTo(a.Value));
                t.Append("      #").Append(f).Append(f == entry ? " (entry)" : string.Empty).Append(" u ").Append(F(heroU[f], 2))
                    .Append(", projected ").Append(projected[f]).Append(", wall ").Append(F(r.WallMs[f], 2)).Append(" ms, main ")
                    .Append(F(r.MainThreadMs[f], 2)).Append(", gc ").Append(F(r.GcBytes[f] / 1024f, 1)).Append(" KB:");
                for (int i = 0; i < entries.Count; i++)
                    t.Append(i > 0 ? "," : string.Empty).Append(' ').Append(entries[i].Key).Append(' ').Append(F(entries[i].Value, 2));
                t.AppendLine();
            }
        }

        private static float Sum(ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
                return 0f;
            long total = 0;
            for (int i = 0; i < recorder.Count; i++)
                total += recorder.GetSample(i).Value;
            return total / 1e6f;
        }

        private static void Dispose(List<ProfilerRecorder> recorders)
        {
            foreach (ProfilerRecorder recorder in recorders)
                recorder.Dispose();
            recorders.Clear();
        }

        private static string ResultsFolder => Path.Combine(Application.dataPath, "..", "TestResults");

        private static string F(float value, int decimals) =>
            value.ToString(decimals == 0 ? "0" : "0." + new string('0', decimals), CultureInfo.InvariantCulture);
    }
}
