using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Cryptforge.Tests
{
    // A measurement, not a check (the 2026-10-29 performance audit): the heaviest fight the Foundry stages, with the hero
    // kept on its feet, measured frame by frame into TestResults/perf-probe.txt (and every frame into
    // perf-probe-frames.tsv). For each hero - the Vanguard as its cut-out puppet, Ilya in its rendered frames - it plays:
    //   horde  - the Foundry's first floor under the development autopilot, the first wave padded to a crowd of 40
    //            (development/horde-probe.txt) and the floor's clock started at 4:50 (floor-clock.txt), so the horde
    //            overruns at the scene's cap of 40 ten seconds in: a crowd walking in and dying in waves, burning (Ilya's
    //            Ember Memory) and erupting (its Vent, the autopilot's choice), Staff rings, the Forge Burst, numerals,
    //            remains and coins, level-ups and picks;
    //   warden - the gate boss proof floor: the Forge Warden's marks, windups and slams, its second phase (taken below
    //            half its health by the probe) and its own fall (felled by the probe).
    // Every frame is a fixed 1/60 s of game time (Time.captureDeltaTime) and the run seed is fixed, so a run replays the
    // same fight and two builds compare frame for frame; offers are answered with their first card at once and the hero is
    // healed whenever it drops below 70%, so nothing but the fight is measured. Per frame: the wall time, the profiler's
    // main-thread time, GC bytes and allocations, its draw calls, batches, SetPass calls, triangles and vertices,
    // UnityStats' (Editor), the main player-loop and script markers the Editor offers, and the crowd and its events; a
    // census of what the camera sees (PerfCensus) every two seconds and a timing of the foundry's depth pass at the middle
    // of each window. Frames the probe works in, and paused ones, are left out of the summary. Its GC lines give the quiet
    // frames apart - none of the fight's events: the steady fight, whose garbage a frame should be flat whatever the crowd
    // (docs/41 step 1) - and the frames with each event.
    //
    // Run it by name. Draw calls, batches and SetPass calls need a graphics device: Tools/Run-PerfProbe.ps1 runs it in a
    // graphics-enabled batch Editor, which renders the camera into a 1080x2340 target each frame (the batch Editor has no
    // Game view); Tools/Run-UnityTests.ps1 -Platform PlayMode -Filter Cryptforge.Tests.PerfProbe runs it -nographics,
    // where nothing renders and only the CPU, GC and census figures mean anything. In the interactive Test Runner the
    // Game view renders (set it to 1080x2340). Knobs, one value per line, in TestResults/: perf-probe-heroes.txt (hero ids,
    // "vanguard" for the default hero), perf-probe-phases.txt (horde, warden), perf-probe-seconds.txt (the horde window),
    // perf-probe-seed.txt, perf-probe-prejit.txt (any line: the game compiled before the first phase, PreCompile),
    // perf-probe-warmup.txt ("on": each phase's scene has its shader variants warmed as it loads, ShaderWarmup),
    // perf-probe-floors.txt ("1,2": every phase played on floor 1 and again on floor 2, DressIce; floor 1 alone by default,
    // so earlier summaries compare as they did). Floor 2 is the same scene, seed and fight with the floor's Ice dress put on
    // in place as the descent puts it on (ArenaView.ApplyFloorTheme, frosted enemies), its one-time cost timed marker by
    // marker, and a comparison line per metric, floor 1 against floor 2, closes the summary. Every
    // time marker on offer is listed in perf-probe-markers.txt, and the summary breaks the slowest frames and every
    // level-up or pick down marker by marker (the game's own scopes: PerformanceMarkers). It changes no game code and
    // asserts nothing about speed.
    [Explicit("Measurement: run it by name.")]
    public sealed class PerfProbe
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const float Step = 1f / 60f;
        private const int PhoneWidth = 1080;
        private const int PhoneHeight = 2340;
        private const float PhoneAspect = PhoneWidth / (float)PhoneHeight;
        // The phone's free band between the HUD blocks, as ArtHudFlowTests and the roster capture frame it.
        private const float BandBottom = 285f;
        private const float BandTop = PhoneHeight - 100f - 327f;
        private const int DefaultSeed = 11;
        private const float DefaultHordeSeconds = 30f;
        private const int Crowd = 40;
        private const float FloorClockStart = 290f;
        private const int WarmCrowd = 30;
        private const float WarmLimitSeconds = 45f;
        private const float WardenWaitSeconds = 60f;
        private const float WardenFirstSeconds = 16f;
        private const float WardenSecondSeconds = 10f;
        private const float WardenFallSeconds = 4f;
        private const float HealBelow = .7f;
        private const int CensusEvery = 120;
        private const int DepthRuns = 50;
        // The per-frame marker lines (AppendFrameMarkers): how many of the slowest frames, how many lines at most, and the
        // least time a marker needs on a frame to be named.
        private const int SlowestBrokenDown = 6;
        private const int MostBrokenDown = 24;
        private const float MarkerFloorMs = .05f;
        private static readonly string[] DefaultHeroes = { "vanguard", "hero_cinder_arcanist" };
        private static readonly string[] DefaultPhases = { "horde", "warden" };
        private static readonly string[] DefaultFloors = { "1" };
        private const string FrameColumns = "hero\tphase\tframe\tflags\twall_ms\tmain_ms\tgc_bytes\tgc_allocs\tdraw\tbatches\t" +
            "setpass\ttris\tverts\tstats_draw\tstats_batches\tstats_setpass\tstats_dyn_draws\tstats_dyn_batches\talive\thorde\t" +
            "burning\teffects\tspawns\twaves\tdeaths\tstrikes\tability\tvents\tlevels\tpicks\tslams\tprogress\tburn_ticks";

        // One hero's phase: what it measured, written into the report before its scene goes.
        private sealed class Run
        {
            public string Hero;
            public string Phase;
            public int Seed;
            public float HordeSeconds;
            public string HeroName = "?";
            public bool Graphics;
            public bool ManualRender;
            public float WarmSeconds;
            public bool EndedEarly;
            public string Note = string.Empty;
            public float DepthMicros = -1f;
            public int DepthPlaced;
            // The silhouette gate's verdict on the timed placement (FoundryVisibilityView.DescribeGate).
            public string DepthGate = "na";
            // What the shader warm-up warmed as the scene loaded (perf-probe-warmup.txt), "off" without it.
            public string Warmup = "off";
            // 1, or 2: the Ice dress put on in place before the fight (DressIce).
            public int Floor = 1;
            // The floor change's one-time cost, written by DressIce ("" on floor 1).
            public string FloorChange = string.Empty;
            // The figures the closing comparison lines put side by side, in report order.
            public readonly List<Metric> Metrics = new List<Metric>();
            public string Label => Floor == 1 ? Phase : Phase + "-floor" + Floor;
            public void Add(string name, float value, int decimals) =>
                Metrics.Add(new Metric { Name = name, Value = value, Decimals = decimals });
            public readonly List<PerfCensus> Censuses = new List<PerfCensus>();
            public readonly StringBuilder Text = new StringBuilder();
            public readonly StringBuilder Frames = new StringBuilder();
        }

        private struct Metric
        {
            public string Name;
            public float Value;
            public int Decimals;
        }

        private struct Spread
        {
            public int Count;
            public float Median;
            public float P95;
            public float P99;
            public float Max;
            public float Mean;
            public float Sum;
        }

        private static string ResultsFolder => Application.isEditor
            ? Path.Combine(Application.dataPath, "..", "TestResults")
            : Path.Combine(Application.persistentDataPath, "TestResults");

        // What a phase changes beyond its own scene's lifetime or for its camera, kept until the phase puts it back
        // (RestorePhase): the frame rate cap, the camera's target (a 1080x2340 texture the Editor would keep alive) and the
        // overlay canvases drawn through the camera. Static, as Play is; a phase that throws or times out leaves them to
        // End, so a failed run leaves the Editor as it found it.
        // perf-probe-warmup.txt reads "on": every phase's scene has its shader variants warmed as it loads (ShaderWarmup).
        private static bool _warmup;
        private static bool _frameRateTaken;
        private static int _frameRateBefore;
        private static Camera _targetCamera;
        private static RenderTexture _targetBefore;
        private static RenderTexture _target;
        private static readonly List<Canvas> Canvases = new List<Canvas>();
        private static readonly List<RenderMode> CanvasModes = new List<RenderMode>();
        private static readonly List<Camera> CanvasCameras = new List<Camera>();
        private static readonly List<float> CanvasDistances = new List<float>();
        // Floor 2's copy of the scene's floor with the Ice theme (DressIce), destroyed after its phase.
        private static FloorDefinition _iceFloor;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTearDown]
        public IEnumerator End()
        {
            RestorePhase();
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Perf probe cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            DropIceFloor();
            TestProfile.End();
        }

        [UnityTest, Timeout(3600000)]
        public IEnumerator TheHeavyFightIsMeasured()
        {
            string[] heroes = Lines("perf-probe-heroes.txt", DefaultHeroes);
            string[] phases = Lines("perf-probe-phases.txt", DefaultPhases);
            int seed = Mathf.RoundToInt(Number("perf-probe-seed.txt", DefaultSeed));
            float hordeSeconds = Mathf.Max(1f, Number("perf-probe-seconds.txt", DefaultHordeSeconds));
            var report = new StringBuilder();
            var frames = new StringBuilder();
            string compiled = Lines("perf-probe-prejit.txt", new string[0]).Length > 0 ? PreCompile() : "off";
            string[] warmup = Lines("perf-probe-warmup.txt", new string[0]);
            _warmup = warmup.Length > 0 && string.Equals(warmup[0], "on", System.StringComparison.OrdinalIgnoreCase);
            int[] floors = Floors();
            Header(report, seed, hordeSeconds, compiled, floors);
            frames.AppendLine(FrameColumns);
            var runs = new List<Run>();
            foreach (string hero in heroes)
                foreach (string phase in phases)
                    foreach (int floor in floors)
                    {
                        var run = new Run { Hero = hero, Phase = phase, Seed = seed, HordeSeconds = hordeSeconds, Floor = floor };
                        yield return Play(run);
                        runs.Add(run);
                        report.Append(run.Text);
                        frames.Append(run.Frames);
                        // After every phase, so a run cut short keeps what it measured.
                        Write(report, frames);
                    }
            if (System.Array.IndexOf(floors, 2) >= 0)
            {
                string comparison = Compare(runs);
                report.Append(comparison);
                Write(report, frames);
                File.WriteAllText(Path.Combine(ResultsFolder, "perf-probe-floors-compare.txt"), comparison);
            }
            Debug.Log("[PerfProbe] summary written to " + Path.Combine(ResultsFolder, "perf-probe.txt") + "\n" + report);
        }

        private static IEnumerator Play(Run run)
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.AutopilotPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.AutopilotPath, "on");
            File.WriteAllText(DevelopmentStart.RunSeedPath, run.Seed.ToString(CultureInfo.InvariantCulture));
            if (run.Hero != "vanguard")
                File.WriteAllText(DevelopmentStart.HeroPath, run.Hero);
            if (run.Phase == "warden")
                File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_gate_boss_proof");
            else
            {
                File.WriteAllText(DevelopmentStart.HordeProbePath, Crowd.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(DevelopmentStart.FloorClockPath, FloorClockStart.ToString(CultureInfo.InvariantCulture));
            }
            Time.timeScale = 1;
            Time.captureDeltaTime = Step;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            // The A/B knob (perf-probe-warmup.txt "on"): the scene's shader variants warmed as it loads, before the frame
            // the probe has always waited, so the measured frames keep their numbers whether it is on or off.
            if (_warmup)
            {
                ShaderWarmup.Result warmed = ShaderWarmup.WarmScene(SceneManager.GetActiveScene());
                run.Warmup = warmed.Materials + " materials of " + warmed.Shaders + " shaders, " + warmed.Variants +
                    " variants warmed (" + warmed.Refused + " refused) in " + F((float)warmed.Milliseconds, 1) + " ms: " +
                    warmed.ShaderNames;
            }
            yield return null;

            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var movement = Object.FindFirstObjectByType<HeroMovementInput>();
            Health hero = movement != null ? movement.GetComponent<Health>() : null;
            Camera camera = Camera.main;
            var follow = Object.FindFirstObjectByType<ArenaCameraFollow>();
            run.HeroName = setup.Hero != null ? setup.Hero.DisplayName : "?";
            run.Graphics = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            run.ManualRender = run.Graphics && Application.isBatchMode && camera != null;

            // The phone's framing, and in a batch Editor the phone's target with every overlay canvas drawn through the
            // camera, so the HUD's draws count as they do on the device. Everything changed here is recorded for
            // RestorePhase.
            if (run.ManualRender)
            {
                _targetCamera = camera;
                _targetBefore = camera.targetTexture;
                _target = new RenderTexture(PhoneWidth, PhoneHeight, 24) { name = "Perf probe target" };
                camera.targetTexture = _target;
                foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        continue;
                    Canvases.Add(canvas);
                    CanvasModes.Add(canvas.renderMode);
                    CanvasCameras.Add(canvas.worldCamera);
                    CanvasDistances.Add(canvas.planeDistance);
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                }
            }
            if (follow != null)
                follow.Frame(PhoneWidth, PhoneHeight, BandBottom, BandTop);
            // CombatSetup caps the frame rate at 60; game time is fixed by captureDeltaTime, so the Editor runs uncapped
            // and the wall time is the frame's own work rather than a wait for the next 1/60 s.
            _frameRateBefore = Application.targetFrameRate;
            _frameRateTaken = true;
            Application.targetFrameRate = -1;
            var recorder = new GameObject("Perf Probe Recorder").AddComponent<PerfProbeRecorder>();
            recorder.Initialize(setup, hero, camera, run.ManualRender);
            // Floor 2: the Ice dress on, after the recorder so the change's first frame renders as every other does.
            if (run.Floor == 2)
                yield return DressIce(run, setup);

            if (run.Phase == "warden")
                yield return Warden(run, setup, hero, camera, follow, recorder);
            else
                yield return Horde(run, setup, hero, camera, recorder);
            recorder.End();
            Summarize(run, recorder);
            if (recorder.MarkersOnOffer.Count > 0)
            {
                Directory.CreateDirectory(ResultsFolder);
                File.WriteAllLines(Path.Combine(ResultsFolder, "perf-probe-markers.txt"), recorder.MarkersOnOffer);
            }
            RestorePhase();
            Time.captureDeltaTime = 0f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Perf probe between " + run.Hero + " " + run.Label));
            yield return SceneManager.UnloadSceneAsync(old);
            DropIceFloor();
            TestProfile.End();
        }

        // Floor 2 as the descent dresses it (EncounterController.EnterFloor), in the floor-1 scene so the fight, its seed
        // and its rooms stay the same and only the dress differs: the encounter's floor becomes a copy of its own with the
        // Ice theme (so every body it stands up from here asks for the frosted palette, as on floor 2), the arena takes the
        // theme (ArenaView.ApplyFloorTheme: re-tint, texture swaps, the crystal mesh, the backdrop), and the bodies already
        // standing take the frosted set. Timed by the wall clock and by the game's Cryptforge.Theme.* markers, read a frame
        // later; then the rest of that frame, its first render on ice included. Before the window, so nothing of it is in
        // the frames measured.
        private static IEnumerator DressIce(Run run, CombatSetup setup)
        {
            var arena = Object.FindFirstObjectByType<ArenaView>();
            EncounterController encounters = setup.Encounters;
            FloorDefinition authored = encounters != null ? encounters.Floor : null;
            FieldInfo themeField = typeof(FloorDefinition).GetField("_theme", Hidden);
            FieldInfo floorField = typeof(EncounterController).GetField("_currentFloor", Hidden);
            MethodInfo useFrost = typeof(EnemyLookView).GetMethod("UseFrost", Hidden);
            if (arena == null || authored == null || themeField == null || floorField == null || useFrost == null)
            {
                run.Note = "floor 2 NOT dressed: " + (arena == null ? "no ArenaView" : authored == null ? "no floor"
                    : "the theme, floor or frost member was not found by reflection");
                run.FloorChange = "na - " + run.Note;
                yield break;
            }
            _iceFloor = Object.Instantiate(authored);
            _iceFloor.name = authored.name + " (perf probe ice)";
            themeField.SetValue(_iceFloor, FloorTheme.Ice);
            floorField.SetValue(encounters, _iceFloor);

            string[] names = PerformanceMarkers.ThemeNames;
            var markers = new ProfilerRecorder[names.Length];
            for (int i = 0; i < names.Length; i++)
                markers[i] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, names[i], 1);
            // A frame for the recorders to start collecting in.
            yield return null;

            int texturesBefore = Resources.FindObjectsOfTypeAll<Texture>().Length;
            ulong textureBytesBefore = Texture.currentTextureMemory;
            long gcBefore = System.GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            arena.ApplyFloorTheme(FloorTheme.Ice);
            watch.Stop();
            float applyMs = (float)watch.Elapsed.TotalMilliseconds;
            long gcApply = System.GC.GetAllocatedBytesForCurrentThread() - gcBefore;
            // The frost creep the descent plays as it lands on floor 2 (EncounterController.EnterFloor); it runs out over
            // the walk-in, before the window.
            watch.Restart();
            bool transition = arena.PlayFrostTransition();
            watch.Stop();
            float transitionMs = (float)watch.Elapsed.TotalMilliseconds;
            int frosted = 0;
            watch.Restart();
            foreach (EnemyLookView look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
            {
                useFrost.Invoke(look, new object[] { true });
                frosted++;
            }
            watch.Stop();
            float frostMs = (float)watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            yield return null;
            watch.Stop();
            float firstFrameMs = (float)watch.Elapsed.TotalMilliseconds;

            var line = new StringBuilder();
            line.Append("ApplyFloorTheme(Ice) ").Append(F(applyMs, 2)).Append(" ms wall, ").Append(F(gcApply / 1024f, 1))
                .Append(" KB managed; frost transition ").Append(transition ? F(transitionMs, 2) + " ms" : "not played")
                .Append("; ").Append(frosted).Append(" standing bodies frosted in ").Append(F(frostMs, 2))
                .Append(" ms; the rest of that frame, its first render on ice included, ").Append(F(firstFrameMs, 1))
                .Append(" ms; markers (ms):");
            run.Add("floor change: ApplyFloorTheme wall ms", applyMs, 2);
            for (int i = 0; i < names.Length; i++)
            {
                float ms = markers[i].Valid && markers[i].Count > 0 ? markers[i].LastValue / 1e6f : float.NaN;
                markers[i].Dispose();
                string shortName = names[i].Substring("Cryptforge.".Length);
                line.Append(' ').Append(shortName).Append(' ').Append(Shown(ms, 2));
                if (names[i] != PerformanceMarkers.ThemeFrostName)
                    run.Add("floor change: " + shortName + " ms", ms, 2);
            }
            run.Add("floor change: PlayFrostTransition wall ms", transition ? transitionMs : float.NaN, 2);
            run.Add("floor change: frosting standing bodies ms", frostMs, 2);
            run.Add("floor change: managed KB allocated", gcApply / 1024f, 1);
            run.Add("floor change: first frame after ms", firstFrameMs, 1);
            int texturesAfter = Resources.FindObjectsOfTypeAll<Texture>().Length;
            float textureMb = (float)(((double)Texture.currentTextureMemory - textureBytesBefore) / 1048576d);
            line.Append("; textures loaded ").Append(texturesAfter - texturesBefore >= 0 ? "+" : string.Empty)
                .Append(texturesAfter - texturesBefore).Append(", texture memory ").Append(textureMb >= 0f ? "+" : string.Empty)
                .Append(F(textureMb, 1)).Append(" MB");
            run.Add("floor change: textures loaded", texturesAfter - texturesBefore, 0);
            run.Add("floor change: texture MB", textureMb, 1);
            run.FloorChange = line.ToString();
        }

        private static void DropIceFloor()
        {
            if (_iceFloor != null)
                Object.Destroy(_iceFloor);
            _iceFloor = null;
        }

        // Puts back what a phase changed (see the fields above): the frame rate cap, the canvases' render mode, camera and
        // distance, the camera's target, and the phone's target released and destroyed. Safe to call twice and after a
        // phase that never got as far as changing anything; a camera or canvas already gone with its scene is skipped.
        private static void RestorePhase()
        {
            for (int i = 0; i < Canvases.Count; i++)
            {
                Canvas canvas = Canvases[i];
                if (canvas == null)
                    continue;
                canvas.renderMode = CanvasModes[i];
                canvas.worldCamera = CanvasCameras[i];
                canvas.planeDistance = CanvasDistances[i];
            }
            Canvases.Clear();
            CanvasModes.Clear();
            CanvasCameras.Clear();
            CanvasDistances.Clear();
            if (_targetCamera != null && _targetCamera.targetTexture == _target)
                _targetCamera.targetTexture = _targetBefore;
            _targetCamera = null;
            _targetBefore = null;
            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }
            _target = null;
            if (_frameRateTaken)
                Application.targetFrameRate = _frameRateBefore;
            _frameRateTaken = false;
        }

        // The crowd walks in - the first wave padded to the crowd, then the horde - and the window opens once it stands.
        private static IEnumerator Horde(Run run, CombatSetup setup, Health hero, Camera camera, PerfProbeRecorder recorder)
        {
            EncounterController encounters = setup.Encounters;
            int frame = 0;
            for (; frame < WarmLimitSeconds / Step && encounters.AllAliveCount < WarmCrowd && !setup.Run.HasEnded; frame++)
            {
                Keep(setup, hero);
                yield return null;
            }
            run.WarmSeconds = frame * Step;
            yield return Measure(run, setup, hero, camera, recorder, Mathf.RoundToInt(run.HordeSeconds / Step), null);
        }

        // The Warden's reveal, then its first phase, its second (the probe takes it below half its health) and its fall.
        private static IEnumerator Warden(Run run, CombatSetup setup, Health hero, Camera camera, ArenaCameraFollow follow,
            PerfProbeRecorder recorder)
        {
            EncounterController encounters = setup.Encounters;
            int frame = 0;
            for (; frame < WardenWaitSeconds / Step && !(setup.Warden.IsFighting && !encounters.IsRevealHeld) &&
                !setup.Run.HasEnded; frame++)
            {
                Keep(setup, hero);
                yield return null;
            }
            run.WarmSeconds = frame * Step;
            if (!setup.Warden.IsFighting)
            {
                run.Note = "the Warden never fought; nothing measured";
                yield break;
            }
            // The boss's reveal hands the camera back framed for the batch window.
            if (follow != null)
                follow.Frame(PhoneWidth, PhoneHeight, BandBottom, BandTop);
            int first = Mathf.RoundToInt(WardenFirstSeconds / Step);
            int second = Mathf.RoundToInt(WardenSecondSeconds / Step);
            int fall = Mathf.RoundToInt(WardenFallSeconds / Step);
            yield return Measure(run, setup, hero, camera, recorder, first + second + fall, step =>
            {
                Health boss = setup.Warden.Boss;
                if (boss == null)
                    return;
                if (step == first && boss.Current > boss.Maximum * .45f)
                    boss.ApplyDamage(new DamageContext(boss.Current - boss.Maximum * .45f));
                else if (step == first + second)
                    boss.ApplyDamage(new DamageContext(boss.Current + 1f));
            });
        }

        private static IEnumerator Measure(Run run, CombatSetup setup, Health hero, Camera camera, PerfProbeRecorder recorder,
            int frames, System.Action<int> step)
        {
            recorder.Begin();
            var visibility = Object.FindFirstObjectByType<FoundryVisibilityView>();
            for (int i = 0; i < frames && !setup.Run.HasEnded; i++)
            {
                yield return null;
                Keep(setup, hero);
                step?.Invoke(i);
                if (camera != null && i % CensusEvery == CensusEvery / 2)
                {
                    recorder.MarkProbeWork();
                    run.Censuses.Add(PerfCensus.Take(camera, PhoneAspect, recorder.CurrentFrame, setup.Encounters.AllAliveCount));
                }
                if (i == frames / 2 && visibility != null)
                {
                    recorder.MarkProbeWork();
                    run.DepthMicros = TimeActorDepth(visibility, camera, out run.DepthPlaced);
                    run.DepthGate = visibility.DescribeGate();
                }
            }
            run.EndedEarly = setup.Run.HasEnded;
        }

        // The run goes on for the measurement: offers are answered with their first card and the hero is kept standing.
        private static void Keep(CombatSetup setup, Health hero)
        {
            for (int i = 0; i < 8 && setup.Choices.Current != null && setup.Choices.Current.Kind == ChoiceKind.Upgrade; i++)
                using (PerformanceMarkers.Pick.Auto())
                    setup.Choices.TrySelect(setup.Choices.Current, 0);
            if (hero != null && hero.IsAlive && hero.Current < hero.Maximum * HealBelow)
                hero.Heal(hero.Maximum);
        }

        // The foundry's depth pass with everything on show now, as the gameplay camera's render pays it (its silhouette
        // gate included): once to warm, then timed.
        private static float TimeActorDepth(FoundryVisibilityView visibility, Camera camera, out int placed)
        {
            visibility.ApplyActorDepth(camera);
            placed = visibility.PlacedCount;
            visibility.RestoreActorDepth();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < DepthRuns; i++)
            {
                visibility.ApplyActorDepth(camera);
                visibility.RestoreActorDepth();
            }
            watch.Stop();
            return (float)(watch.Elapsed.TotalMilliseconds * 1000d / DepthRuns);
        }

        // A diagnostic, off unless TestResults/perf-probe-prejit.txt holds a line: the Editor's Mono compiles each method
        // the first time it runs, which a phone's IL2CPP player never does, so a first level-up's one-off cost can be the
        // compiler's alone. Every method of the game's and uGUI's assemblies that can be compiled ahead of its first call
        // is compiled here, before anything is measured (open generics cannot be, and are left to their first use); a
        // first-time cost that stays with this on is not the compiler's.
        private static string PreCompile()
        {
            const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic;
            var watch = Stopwatch.StartNew();
            int compiled = 0, failed = 0;
            foreach (Assembly assembly in new[] { typeof(CombatSetup).Assembly, typeof(UnityEngine.UI.Text).Assembly })
            {
                foreach (System.Type type in assembly.GetTypes())
                {
                    if (type.ContainsGenericParameters)
                        continue;
                    var methods = new List<MethodBase>(type.GetMethods(Declared));
                    methods.AddRange(type.GetConstructors(Declared));
                    foreach (MethodBase method in methods)
                    {
                        if (method.IsAbstract || method.ContainsGenericParameters ||
                            (method.Attributes & MethodAttributes.PinvokeImpl) != 0 ||
                            (method.MethodImplementationFlags & MethodImplAttributes.InternalCall) != 0)
                            continue;
                        try
                        {
                            method.MethodHandle.GetFunctionPointer();
                            compiled++;
                        }
                        catch (System.Exception)
                        {
                            failed++;
                        }
                    }
                }
            }
            watch.Stop();
            return compiled + " methods of the game's and uGUI's assemblies in " + F((float)watch.Elapsed.TotalSeconds, 1) +
                " s (" + failed + " refused)";
        }

        private static void Header(StringBuilder report, int seed, float hordeSeconds, string compiled, int[] floors)
        {
            bool graphics = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            string rendering = !graphics ? "no graphics device: nothing renders, render counts are na"
                : Application.isBatchMode ? "batch Editor: the probe renders the camera into " + PhoneWidth + "x" + PhoneHeight
                    + " each frame, overlay canvases drawn through it"
                : "the Game view renders (set it to " + PhoneWidth + "x" + PhoneHeight + ")";
            report.Append("Cryptforge perf probe, ").Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                .Append(", Unity ").Append(Application.unityVersion).Append(", ")
                .Append(Application.isEditor ? "Editor" : "player").Append(Application.isBatchMode ? " (batch)" : string.Empty)
                .Append(", ").Append(SystemInfo.graphicsDeviceType).Append(" on ").Append(SystemInfo.processorType.Trim())
                .AppendLine();
            report.Append("rendering: ").AppendLine(rendering);
            report.Append("scenario: seed ").Append(seed).Append("; horde window ").Append(F(hordeSeconds, 0))
                .Append(" s after the crowd of ").Append(WarmCrowd).Append(" stands (first wave padded to ").Append(Crowd)
                .Append(", floor clock from ").Append(F(FloorClockStart, 0))
                .Append(" s, overrun at 300 s; the padding flag also gives the hero +1,000,000 health, as on the phone); Warden ")
                .Append(F(WardenFirstSeconds, 0)).Append(" s + ").Append(F(WardenSecondSeconds, 0)).Append(" s phase 2 + ")
                .Append(F(WardenFallSeconds, 0)).Append(" s fall; autopilot on; offers answered with the first card at once; ")
                .Append("hero healed below ").Append(F(HealBelow * 100f, 0))
                .AppendLine("%; frames are 1/60 s of game time, run uncapped (targetFrameRate -1 while measuring).");
            report.AppendLine("left out: frames the probe worked in (census every " + CensusEvery +
                " frames, the depth timing) and paused frames (an open choice). The recorders' values are the frame's own.");
            report.AppendLine("caution: Editor CPU and GC are the Editor's, not the S23's - compare builds against each other " +
                "on this machine, and read the phone with development/render-stats.txt. ms are milliseconds of wall or main-thread time.");
            report.Append("pre-compiled before the first phase (perf-probe-prejit.txt): ").AppendLine(compiled);
            report.Append("shader warm-up as each phase's scene loads (perf-probe-warmup.txt): ").AppendLine(_warmup
                ? "on - what each warmed, and how long it took, is under its phase" : "off");
            report.Append("floors (perf-probe-floors.txt): ").Append(string.Join(",", floors));
            report.AppendLine(System.Array.IndexOf(floors, 2) >= 0
                ? " - floor 2 is the same scene, seed and fight with the Ice dress put on in place before the walk-in " +
                  "(ApplyFloorTheme, frosted enemies); the comparison lines close the summary"
                : string.Empty);
            report.AppendLine();
        }

        private static void Summarize(Run run, PerfProbeRecorder r)
        {
            StringBuilder t = run.Text;
            int included = 0, paused = 0, probe = 0;
            for (int f = 0; f < r.Count; f++)
            {
                if (r.IsIncluded(f)) included++;
                if ((r.Flags[f] & PerfProbeRecorder.Paused) != 0) paused++;
                if ((r.Flags[f] & PerfProbeRecorder.ProbeWork) != 0) probe++;
            }
            t.Append("== ").Append(run.HeroName).Append(" (").Append(run.Hero).Append(") / ").Append(run.Phase)
                .Append(run.Floor == 1 ? string.Empty : " / floor " + run.Floor + " (Ice)").Append(": ")
                .Append(included).Append(" frames measured of ").Append(r.Count).Append(" (").Append(paused).Append(" paused, ")
                .Append(probe).Append(" the probe's), ").Append(F(r.Count * Step, 1)).Append(" s of game time after ")
                .Append(F(run.WarmSeconds, 1)).Append(" s walking in");
            if (run.EndedEarly) t.Append("; THE RUN ENDED inside the window");
            if (run.Note.Length > 0) t.Append("; ").Append(run.Note);
            t.AppendLine();
            t.Append("   shader warm-up as the scene loaded (perf-probe-warmup.txt): ").AppendLine(run.Warmup);
            if (run.Floor != 1)
                t.Append("   floor change, once, before the walk-in: ").AppendLine(run.FloorChange);
            if (r.Count == 0)
            {
                t.AppendLine();
                return;
            }

            Spread alive = Of(r, r.Alive), horde = Of(r, r.Horde), burning = Of(r, r.Burning), effects = Of(r, r.Effects);
            t.Append("   crowd: enemies alive ").Append(Triple(alive, 0)).Append(" | horde ").Append(Triple(horde, 0))
                .Append(" | burning ").Append(Triple(burning, 0)).Append(" | effects on show ").Append(Triple(effects, 0))
                .AppendLine("   (median / p95 / max)");
            t.Append("   events: ").Append(Total(r, r.Spawns)).Append(" horde spawns, ").Append(Total(r, r.Waves))
                .Append(" waves, ").Append(Total(r, r.Deaths)).Append(" deaths, ").Append(Total(r, r.Strikes))
                .Append(" hero strikes, ").Append(Total(r, r.AbilityUses)).Append(" ability uses (").Append(Total(r, r.Vents))
                .Append(" Vent eruptions), ").Append(Total(r, r.LevelUps)).Append(" level-ups, ").Append(Total(r, r.Picks))
                .Append(" picks, ").Append(Total(r, r.Slams)).Append(" slams, ").Append(Total(r, r.BurnTicks))
                .Append(" burn ticks, ").Append(Total(r, r.Progress)).Append(" HUD progress events (")
                .Append(F(Total(r, r.Progress) / Mathf.Max(Step, r.Count * Step), 1)).AppendLine("/s)");

            Spread wall = Of(r, r.WallMs);
            run.Add("frame ms median", wall.Median, 2);
            run.Add("frame ms p95", wall.P95, 2);
            run.Add("frame ms max", wall.Max, 2);
            run.Add("frames over 16.7 ms", CountOver(r, r.WallMs, 1000f / 60f), 0);
            if (r.MainThreadValid)
            {
                Spread main = Of(r, r.MainThreadMs);
                run.Add("main thread ms median", main.Median, 2);
                run.Add("main thread ms p95", main.P95, 2);
            }
            t.Append("   frame ms (wall): ").Append(Full(wall, 2)).Append(" | over 16.7 ms: ")
                .Append(CountOver(r, r.WallMs, 1000f / 60f)).Append(", over 33.3 ms: ").Append(CountOver(r, r.WallMs, 1000f / 30f))
                .AppendLine();
            t.Append("   main thread ms: ").AppendLine(r.MainThreadValid ? Full(Of(r, r.MainThreadMs), 2) : "na");
            if (r.GcValid)
            {
                Spread gc = Of(r, r.GcBytes), allocs = Of(r, r.GcCount);
                run.Add("GC KB per frame mean", gc.Mean / 1024f, 2);
                run.Add("GC KB per frame median", gc.Median / 1024f, 2);
                run.Add("GC KB per frame p95", gc.P95 / 1024f, 2);
                run.Add("GC KB per frame max", gc.Max / 1024f, 1);
                run.Add("GC allocations per frame mean", allocs.Mean, 1);
                run.Add("GC.Collect runs", r.CollectionsAtEnd - r.CollectionsAtBegin, 0);
                int allocating = 0;
                for (int f = 0; f < r.Count; f++)
                    if (r.IsIncluded(f) && r.GcBytes[f] > 0f) allocating++;
                t.Append("   GC per frame: mean ").Append(F(gc.Mean / 1024f, 2)).Append(" KB, median ")
                    .Append(F(gc.Median / 1024f, 2)).Append(" KB, p95 ").Append(F(gc.P95 / 1024f, 2)).Append(" KB, max ")
                    .Append(F(gc.Max / 1024f, 1)).Append(" KB; ").Append(F(100f * allocating / Mathf.Max(1, gc.Count), 0))
                    .Append("% of frames allocate; ").Append(F(gc.Sum / 1048576f, 2)).Append(" MB in the window; allocations a frame: ")
                    .Append(allocs.Count > 0 ? F(allocs.Mean, 1) + " mean, " + F(allocs.Max, 0) + " max" : "na")
                    .Append("; GC.Collect runs ").Append(r.CollectionsAtEnd - r.CollectionsAtBegin).AppendLine();
                // The steady fight, whose garbage a frame should be flat whatever the crowd (docs/41 step 1): the frames
                // with none of the events, by crowd too; then the frames with each, as the wall times are given below.
                Spread quiet = Of(r, r.GcBytes, f => Quiet(r, f)), quietAllocs = Of(r, r.GcCount, f => Quiet(r, f));
                t.Append("   GC on quiet frames (none of the events below): ").Append(quiet.Count).Append(" frames, median ")
                    .Append(F(quiet.Median, 0)).Append(" B and ").Append(F(quietAllocs.Median, 0)).Append(" allocations, mean ")
                    .Append(F(quiet.Mean, 0)).Append(" B, p95 ").Append(F(quiet.P95, 0)).Append(" B, max ").Append(F(quiet.Max, 0))
                    .Append(" B; by crowd (median B / allocations, frames): up to 20 alive ")
                    .Append(GcWith(r, f => Quiet(r, f) && r.Alive[f] <= 20f)).Append(", 30 and up ")
                    .Append(GcWith(r, f => Quiet(r, f) && r.Alive[f] >= 30f)).AppendLine();
                t.Append("   GC a frame with (median B / allocations, frames): none of these ").Append(GcWith(r, f => Quiet(r, f)))
                    .Append(" | horde spawn ").Append(GcWith(r, f => r.Spawns[f] > 0f)).Append(" | wave ")
                    .Append(GcWith(r, f => r.Waves[f] > 0f)).Append(" | death ").Append(GcWith(r, f => r.Deaths[f] > 0f))
                    .Append(" | level-up or pick ").Append(GcWith(r, f => r.LevelUps[f] + r.Picks[f] > 0f))
                    .Append(" | ability ").Append(GcWith(r, f => r.AbilityUses[f] > 0f)).Append(" | slam ")
                    .Append(GcWith(r, f => r.Slams[f] > 0f)).Append(" | hero strike ").Append(GcWith(r, f => r.Strikes[f] > 0f))
                    .Append(" | HUD progress ").Append(GcWith(r, f => r.Progress[f] > 0f)).AppendLine();
            }
            else
                t.AppendLine("   GC per frame: na (no GC Allocated In Frame counter)");
            if (r.RenderCountersValid && Of(r, r.DrawCalls).Max > 0f)
            {
                AddTriple(run, "draw calls", Of(r, r.DrawCalls));
                AddTriple(run, "batches", Of(r, r.Batches));
                AddTriple(run, "SetPass", Of(r, r.SetPassCalls));
                AddTriple(run, "triangles", Of(r, r.Triangles));
            }
            if (r.RenderCountersValid && Of(r, r.DrawCalls).Max > 0f)
                t.Append("   render (profiler counters): draw calls ").Append(Triple(Of(r, r.DrawCalls), 0)).Append(" | batches ")
                    .Append(Triple(Of(r, r.Batches), 0)).Append(" | SetPass ").Append(Triple(Of(r, r.SetPassCalls), 0))
                    .Append(" | triangles ").Append(Triple(Of(r, r.Triangles), 0)).Append(" | vertices ")
                    .Append(Triple(Of(r, r.Vertices), 0)).AppendLine();
            else
                t.AppendLine("   render (profiler counters): na - nothing rendered, or this Editor reports no render counters");
#if UNITY_EDITOR
            if (Of(r, r.StatsDraw).Max > 0f)
            {
                AddTriple(run, "UnityStats draw calls", Of(r, r.StatsDraw));
                AddTriple(run, "UnityStats batches", Of(r, r.StatsBatches));
                AddTriple(run, "UnityStats SetPass", Of(r, r.StatsSetPass));
            }
            if (Of(r, r.StatsDraw).Max > 0f)
                t.Append("   render (UnityStats): draw calls ").Append(Triple(Of(r, r.StatsDraw), 0)).Append(" | batches ")
                    .Append(Triple(Of(r, r.StatsBatches), 0)).Append(" | SetPass ").Append(Triple(Of(r, r.StatsSetPass), 0))
                    .Append(" | dynamic-batched draws ").Append(Triple(Of(r, r.StatsDynamicDraws), 0))
                    .Append(" | dynamic batches ").Append(Triple(Of(r, r.StatsDynamicBatches), 0)).AppendLine();
            else
                t.AppendLine("   render (UnityStats): na - nothing rendered, or the stats only follow the Game view");
#endif
            t.Append("   frames with (median wall ms, count): none of these ").Append(With(r, f => Quiet(r, f)))
                .Append(" | horde spawn ").Append(With(r, f => r.Spawns[f] > 0f)).Append(" | wave ").Append(With(r, f => r.Waves[f] > 0f))
                .Append(" | death ").Append(With(r, f => r.Deaths[f] > 0f)).Append(" | 5+ deaths ").Append(With(r, f => r.Deaths[f] >= 5f))
                .Append(" | level-up or pick ").Append(With(r, f => r.LevelUps[f] + r.Picks[f] > 0f))
                .Append(" | ability ").Append(With(r, f => r.AbilityUses[f] > 0f)).Append(" | slam ").Append(With(r, f => r.Slams[f] > 0f))
                .AppendLine();
            AppendSlowest(t, r);
            AppendMarkers(t, r);
            AppendFrameMarkers(t, r);
            AppendCensus(t, run);
            t.Append("   foundry depth pass (apply + restore for the gameplay camera, all on show): ")
                .Append(run.DepthMicros >= 0f ? F(run.DepthMicros, 1) + " us for " + run.DepthPlaced + " transforms placed, mean of " + DepthRuns : "na")
                .AppendLine();
            t.Append("   silhouette gate on that placement: ").AppendLine(run.DepthGate);
            int textures = Resources.FindObjectsOfTypeAll<Texture>().Length;
            t.Append("   memory at the end: total allocated ").Append(F(Profiler.GetTotalAllocatedMemoryLong() / 1048576f, 0))
                .Append(" MB, managed heap ").Append(F(Profiler.GetMonoHeapSizeLong() / 1048576f, 0)).Append(" MB (")
                .Append(F(Profiler.GetMonoUsedSizeLong() / 1048576f, 0)).Append(" used), graphics driver ")
                .Append(F(Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f, 0)).Append(" MB, textures ")
                .Append(F(Texture.currentTextureMemory / 1048576f, 0)).Append(" MB in ").Append(textures)
                .AppendLine(" textures loaded (the Editor's own included)");
            run.Add("end: textures loaded", textures, 0);
            run.Add("end: texture MB", Texture.currentTextureMemory / 1048576f, 1);
            run.Add("end: total allocated MB", Profiler.GetTotalAllocatedMemoryLong() / 1048576f, 1);
            run.Add("end: managed heap used MB", Profiler.GetMonoUsedSizeLong() / 1048576f, 1);
            run.Add("end: graphics driver MB", Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f, 1);
            t.AppendLine();
            AppendFrames(run, r);
        }

        private static void AddTriple(Run run, string name, Spread spread)
        {
            run.Add(name + " median", spread.Median, 0);
            run.Add(name + " p95", spread.P95, 0);
            run.Add(name + " max", spread.Max, 0);
        }

        // One line a metric, floor 1 against floor 2 for each hero and phase both were played for: the change and its
        // share of floor 1. Floor 1 never changes floor, so its floor-change figures are 0; "na" where a side lacks one.
        private static string Compare(List<Run> runs)
        {
            var t = new StringBuilder();
            t.AppendLine("== floor 1 (Foundry) vs floor 2 (Ice): one line a metric (floor 1 | floor 2 | change)");
            foreach (Run ice in runs)
            {
                if (ice.Floor != 2)
                    continue;
                Run foundry = runs.Find(x => x.Floor == 1 && x.Hero == ice.Hero && x.Phase == ice.Phase);
                string who = ice.Hero + "/" + ice.Phase;
                if (foundry == null)
                {
                    t.Append("   ").Append(who).AppendLine(": no floor-1 run to compare with");
                    continue;
                }
                if (foundry.EndedEarly || ice.EndedEarly)
                    t.Append("   ").Append(who).AppendLine(": CAUTION a run ended inside its window; the windows differ");
                var names = new List<string>();
                foreach (Metric m in foundry.Metrics) if (!names.Contains(m.Name)) names.Add(m.Name);
                foreach (Metric m in ice.Metrics) if (!names.Contains(m.Name)) names.Add(m.Name);
                foreach (string name in names)
                {
                    bool change = name.StartsWith("floor change:", System.StringComparison.Ordinal);
                    int decimals = 2;
                    float b = Find(ice, name, float.NaN, ref decimals);
                    float a = Find(foundry, name, change ? 0f : float.NaN, ref decimals);
                    t.Append("   compare ").Append(who).Append(' ').Append(name.PadRight(42)).Append(" floor 1 ")
                        .Append(Shown(a, decimals)).Append(" | floor 2 ").Append(Shown(b, decimals)).Append(" | ");
                    if (float.IsNaN(a) || float.IsNaN(b))
                    {
                        t.AppendLine("change na");
                        continue;
                    }
                    float d = b - a;
                    t.Append(d >= 0f ? "+" : string.Empty).Append(F(d, decimals));
                    t.AppendLine(a != 0f ? " (" + (d >= 0f ? "+" : string.Empty) + F(100f * d / Mathf.Abs(a), 1) + "%)" : string.Empty);
                }
            }
            t.AppendLine("   (the Editor stands in for the phone, whose S23 frame row is unmeasured: read the change, not the absolute)");
            t.AppendLine();
            return t.ToString();
        }

        private static float Find(Run run, string name, float missing, ref int decimals)
        {
            foreach (Metric m in run.Metrics)
                if (m.Name == name)
                {
                    decimals = m.Decimals;
                    return m.Value;
                }
            return missing;
        }

        private static string Shown(float value, int decimals) => float.IsNaN(value) ? "na" : F(value, decimals);

        // perf-probe-floors.txt: floor numbers, by comma or by line ("1,2"); 1 and 2 are played, floor 1 alone by default.
        private static int[] Floors()
        {
            var floors = new List<int>();
            foreach (string line in Lines("perf-probe-floors.txt", DefaultFloors))
                foreach (string part in line.Split(','))
                    if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int floor) &&
                        (floor == 1 || floor == 2) && !floors.Contains(floor))
                        floors.Add(floor);
            if (floors.Count == 0)
                floors.Add(1);
            floors.Sort();
            return floors.ToArray();
        }

        private static void AppendSlowest(StringBuilder t, PerfProbeRecorder r)
        {
            var frames = new List<int>();
            for (int f = 0; f < r.Count; f++)
                if (r.IsIncluded(f)) frames.Add(f);
            frames.Sort((a, b) => r.WallMs[b].CompareTo(r.WallMs[a]));
            t.Append("   slowest frames:");
            for (int i = 0; i < frames.Count && i < 8; i++)
            {
                int f = frames[i];
                t.Append(i > 0 ? ";" : string.Empty).Append(" #").Append(f).Append(' ').Append(F(r.WallMs[f], 1)).Append(" ms");
                if (r.GcBytes[f] > 0f) t.Append(", gc ").Append(F(r.GcBytes[f] / 1024f, 1)).Append(" KB");
                t.Append(", ").Append(F(r.Alive[f], 0)).Append(" alive");
                Tag(t, r.Spawns[f], "spawn");
                Tag(t, r.Waves[f], "wave");
                Tag(t, r.Deaths[f], "death");
                Tag(t, r.LevelUps[f], "level");
                Tag(t, r.Picks[f], "pick");
                Tag(t, r.AbilityUses[f], "ability");
                Tag(t, r.Slams[f], "slam");
            }
            t.AppendLine();
        }

        private static void Tag(StringBuilder t, float count, string name)
        {
            if (count > 0f)
                t.Append(", ").Append(F(count, 0)).Append(' ').Append(name);
        }

        private static void AppendMarkers(StringBuilder t, PerfProbeRecorder r)
        {
            if (r.MarkerNames.Count == 0)
            {
                t.AppendLine("   markers: none on offer");
                return;
            }
            var spreads = new List<KeyValuePair<string, Spread>>();
            for (int m = 0; m < r.MarkerNames.Count; m++)
            {
                Spread spread = Of(r, r.MarkerMs[m]);
                if (spread.Count > 0 && spread.Max > 0f)
                    spreads.Add(new KeyValuePair<string, Spread>(r.MarkerNames[m], spread));
            }
            spreads.Sort((a, b) => b.Value.Mean.CompareTo(a.Value.Mean));
            t.Append("   markers on offer: ").Append(r.MarkerNames.Count).Append(", with time in the window: ").Append(spreads.Count)
                .AppendLine(" (ms a frame: mean / median / p95 / max)");
            foreach (var pair in spreads)
                t.Append("      ").Append(pair.Key.PadRight(58)).Append(' ').Append(F(pair.Value.Mean, 3)).Append(" / ")
                    .Append(F(pair.Value.Median, 3)).Append(" / ").Append(F(pair.Value.P95, 3)).Append(" / ")
                    .Append(F(pair.Value.Max, 2)).AppendLine();
        }

        // Where single frames went (the first level-up's pick, 2026-11-11): the slowest frames and every frame with a
        // level-up or a pick, in frame order, each with the markers that took at least MarkerFloorMs of it, largest first.
        // A marker's value is its frame's own player loop; the frame's wall time also holds the previous frame's
        // PostLateUpdate. Cryptforge.Pick is the whole selection and holds the Cryptforge.Pick.* scopes run inside it.
        private static void AppendFrameMarkers(StringBuilder t, PerfProbeRecorder r)
        {
            if (r.MarkerNames.Count == 0)
                return;
            var slowest = new List<int>();
            for (int f = 0; f < r.Count; f++)
                if (r.IsIncluded(f)) slowest.Add(f);
            slowest.Sort((a, b) => r.WallMs[b].CompareTo(r.WallMs[a]));
            var shown = new List<int>();
            for (int i = 0; i < slowest.Count && i < SlowestBrokenDown; i++)
                shown.Add(slowest[i]);
            // The last frame's markers were never read (PerfProbeRecorder.IsIncluded).
            for (int f = 0; f < r.Count - 1 && shown.Count < MostBrokenDown; f++)
                if (r.LevelUps[f] + r.Picks[f] > 0f && !shown.Contains(f))
                    shown.Add(f);
            shown.Sort();
            t.Append("   markers a frame, the ").Append(SlowestBrokenDown).Append(" slowest and every level-up or pick (ms, ")
                .Append(F(MarkerFloorMs, 2)).AppendLine(" and up, largest first):");
            var entries = new List<KeyValuePair<string, float>>();
            foreach (int f in shown)
            {
                entries.Clear();
                for (int m = 0; m < r.MarkerNames.Count; m++)
                    if (r.MarkerMs[m][f] >= MarkerFloorMs)
                        entries.Add(new KeyValuePair<string, float>(r.MarkerNames[m], r.MarkerMs[m][f]));
                entries.Sort((a, b) => b.Value.CompareTo(a.Value));
                t.Append("      #").Append(f).Append(' ').Append(F(r.WallMs[f], 1)).Append(" ms");
                if (r.GcBytes[f] > 0f) t.Append(", gc ").Append(F(r.GcBytes[f] / 1024f, 1)).Append(" KB");
                Tag(t, r.Spawns[f], "spawn");
                Tag(t, r.Waves[f], "wave");
                Tag(t, r.Deaths[f], "death");
                Tag(t, r.LevelUps[f], "level");
                Tag(t, r.Picks[f], "pick");
                Tag(t, r.AbilityUses[f], "ability");
                Tag(t, r.Slams[f], "slam");
                if (!r.IsIncluded(f)) t.Append(", left out of the summary");
                t.Append(':');
                for (int i = 0; i < entries.Count; i++)
                    t.Append(i > 0 ? "," : string.Empty).Append(' ').Append(entries[i].Key).Append(' ')
                        .Append(F(entries[i].Value, 2));
                if (r.FontRebuilds[f] > 0f)
                    t.Append(" | font atlases rebuilt ").Append(F(r.FontRebuilds[f], 0)).Append(" (")
                        .Append(r.FontRebuildNames[f]).Append(')');
                AppendCounters(t, r, f);
                t.AppendLine();
            }
            var rebuilt = new List<int>();
            int rebuilds = 0;
            for (int f = 0; f < r.Count; f++)
            {
                if (r.FontRebuilds[f] <= 0f)
                    continue;
                rebuilds += Mathf.RoundToInt(r.FontRebuilds[f]);
                rebuilt.Add(f);
            }
            t.Append("   font atlases rebuilt (Font.textureRebuilt): ").Append(rebuilds).Append(" in the window");
            for (int i = 0; i < rebuilt.Count && i < 12; i++)
                t.Append(i == 0 ? ", on frames #" : ", #").Append(rebuilt[i]).Append(" (").Append(r.FontRebuildNames[rebuilt[i]])
                    .Append(')');
            t.AppendLine();
        }

        // The engine counters that moved on the frame (PerfProbeRecorder.CounterNames): a frame's own amount where it has
        // one, otherwise the change from the frame before; bytes in KB.
        private static void AppendCounters(StringBuilder t, PerfProbeRecorder r, int f)
        {
            bool any = false;
            for (int i = 0; i < PerfProbeRecorder.CounterNames.Length; i++)
            {
                string name = PerfProbeRecorder.CounterNames[i];
                float[] values = r.CounterValues[i];
                if (values == null || values[f] < 0f)
                    continue;
                bool perFrame = name.IndexOf("In Frame", System.StringComparison.Ordinal) >= 0;
                float shown = values[f];
                if (!perFrame)
                {
                    if (f == 0 || values[f - 1] < 0f || values[f] == values[f - 1])
                        continue;
                    shown = values[f] - values[f - 1];
                }
                else if (shown <= 0f)
                    continue;
                bool bytes = name.IndexOf("Bytes", System.StringComparison.Ordinal) >= 0 ||
                             name.IndexOf("Memory", System.StringComparison.Ordinal) >= 0;
                t.Append(any ? ", " : " | counters: ").Append(name).Append(' ').Append(!perFrame && shown > 0f ? "+" : string.Empty)
                    .Append(bytes ? F(shown / 1024f, 1) + " KB" : F(shown, 0));
                any = true;
            }
        }

        private static void AppendCensus(StringBuilder t, Run run)
        {
            if (run.Censuses.Count == 0)
            {
                t.AppendLine("   census: none taken");
                return;
            }
            PerfCensus peak = run.Censuses[0];
            foreach (PerfCensus census in run.Censuses)
                if (census.Alive > peak.Alive || (census.Alive == peak.Alive && census.Visible > peak.Visible))
                    peak = census;
            t.Append("   census at the largest crowd (frame ").Append(peak.Frame).Append("): ");
            peak.AppendTo(t);
            var batches = new List<float>();
            var coverage = new List<float>();
            var visible = new List<float>();
            foreach (PerfCensus census in run.Censuses)
            {
                batches.Add(census.SpriteRuns);
                coverage.Add(census.Coverage);
                visible.Add(census.Visible);
            }
            t.Append("   census medians over ").Append(run.Censuses.Count).Append(" samples: visible ")
                .Append(F(Median(visible), 0)).Append(", estimated sprite batches ").Append(F(Median(batches), 0))
                .Append(", alpha coverage ").Append(F(Median(coverage), 2)).AppendLine(" screens");
        }

        private static void AppendFrames(Run run, PerfProbeRecorder r)
        {
            StringBuilder s = run.Frames;
            for (int f = 0; f < r.Count; f++)
            {
                s.Append(run.Hero).Append('\t').Append(run.Label).Append('\t').Append(f).Append('\t').Append(r.Flags[f]);
                foreach (float[] column in new[]
                {
                    r.WallMs, r.MainThreadMs, r.GcBytes, r.GcCount, r.DrawCalls, r.Batches, r.SetPassCalls, r.Triangles,
                    r.Vertices, r.StatsDraw, r.StatsBatches, r.StatsSetPass, r.StatsDynamicDraws, r.StatsDynamicBatches,
                    r.Alive, r.Horde, r.Burning, r.Effects, r.Spawns, r.Waves, r.Deaths, r.Strikes, r.AbilityUses, r.Vents,
                    r.LevelUps, r.Picks, r.Slams, r.Progress, r.BurnTicks
                })
                    s.Append('\t').Append(column[f].ToString(column[f] == Mathf.Round(column[f]) ? "0" : "0.###",
                        CultureInfo.InvariantCulture));
                s.AppendLine();
            }
        }

        // Over the frames the summary keeps; a value below zero is one the recorder did not have.
        private static Spread Of(PerfProbeRecorder r, float[] values) => Of(r, values, f => true);

        // The same, over the kept frames the condition holds for.
        private static Spread Of(PerfProbeRecorder r, float[] values, System.Func<int, bool> where)
        {
            var kept = new List<float>(r.Count);
            for (int f = 0; f < r.Count; f++)
                if (r.IsIncluded(f) && values[f] >= 0f && where(f))
                    kept.Add(values[f]);
            return SpreadOf(kept);
        }

        // A frame of the steady fight: no horde spawn, wave, death, level-up, pick, eruption, ability or slam in it.
        private static bool Quiet(PerfProbeRecorder r, int f) =>
            r.Spawns[f] + r.Waves[f] + r.Deaths[f] + r.LevelUps[f] + r.Picks[f] + r.Vents[f] + r.AbilityUses[f] + r.Slams[f] == 0f;

        // The median GC bytes and allocations of the kept frames the condition holds for, and how many those are.
        private static string GcWith(PerfProbeRecorder r, System.Func<int, bool> where)
        {
            var bytes = new List<float>();
            var allocations = new List<float>();
            for (int f = 0; f < r.Count; f++)
            {
                if (!r.IsIncluded(f) || r.GcBytes[f] < 0f || !where(f))
                    continue;
                bytes.Add(r.GcBytes[f]);
                allocations.Add(r.GcCount[f]);
            }
            return bytes.Count == 0 ? "-"
                : F(SpreadOf(bytes).Median, 0) + " / " + F(SpreadOf(allocations).Median, 0) + " (" + bytes.Count + ")";
        }

        private static Spread SpreadOf(List<float> values)
        {
            var spread = new Spread { Count = values.Count };
            if (values.Count == 0)
                return spread;
            values.Sort();
            foreach (float value in values)
                spread.Sum += value;
            spread.Mean = spread.Sum / values.Count;
            spread.Median = Rank(values, .5f);
            spread.P95 = Rank(values, .95f);
            spread.P99 = Rank(values, .99f);
            spread.Max = values[values.Count - 1];
            return spread;
        }

        // Nearest rank on sorted values.
        private static float Rank(List<float> sorted, float share) =>
            sorted[Mathf.Clamp(Mathf.CeilToInt(share * sorted.Count) - 1, 0, sorted.Count - 1)];

        private static float Median(List<float> values) => SpreadOf(new List<float>(values)).Median;

        private static string With(PerfProbeRecorder r, System.Func<int, bool> where)
        {
            var kept = new List<float>();
            for (int f = 0; f < r.Count; f++)
                if (r.IsIncluded(f) && where(f))
                    kept.Add(r.WallMs[f]);
            return kept.Count == 0 ? "-" : F(SpreadOf(kept).Median, 2) + " (" + kept.Count + ")";
        }

        private static int CountOver(PerfProbeRecorder r, float[] values, float limit)
        {
            int over = 0;
            for (int f = 0; f < r.Count; f++)
                if (r.IsIncluded(f) && values[f] > limit)
                    over++;
            return over;
        }

        // Every frame of the window counts its events, kept or not: an event is the fight's whatever the probe did.
        private static int Total(PerfProbeRecorder r, float[] counts)
        {
            float total = 0f;
            for (int f = 0; f < r.Count; f++)
                total += counts[f];
            return Mathf.RoundToInt(total);
        }

        private static string Triple(Spread spread, int decimals) => spread.Count == 0 ? "na"
            : F(spread.Median, decimals) + " / " + F(spread.P95, decimals) + " / " + F(spread.Max, decimals);

        private static string Full(Spread spread, int decimals) => spread.Count == 0 ? "na"
            : "median " + F(spread.Median, decimals) + ", p95 " + F(spread.P95, decimals) + ", p99 " + F(spread.P99, decimals) +
              ", max " + F(spread.Max, decimals) + ", mean " + F(spread.Mean, decimals) + " (" + spread.Count + " frames, " +
              F(spread.Mean > 0f ? 1000f / spread.Mean : 0f, 1) + " fps at the mean)";

        private static string F(float value, int decimals) =>
            value.ToString(decimals == 0 ? "0" : "0." + new string('0', decimals), CultureInfo.InvariantCulture);

        private static void Write(StringBuilder report, StringBuilder frames)
        {
            Directory.CreateDirectory(ResultsFolder);
            File.WriteAllText(Path.Combine(ResultsFolder, "perf-probe.txt"), report.ToString());
            File.WriteAllText(Path.Combine(ResultsFolder, "perf-probe-frames.tsv"), frames.ToString());
        }

        // TestResults/<name>, one value per line; the defaults when it is missing or empty.
        private static string[] Lines(string name, string[] defaults)
        {
            string path = Path.Combine(ResultsFolder, name);
            if (!File.Exists(path))
                return defaults;
            var lines = new List<string>();
            foreach (string line in File.ReadAllLines(path))
                if (line.Trim().Length > 0)
                    lines.Add(line.Trim());
            return lines.Count > 0 ? lines.ToArray() : defaults;
        }

        private static float Number(string name, float fallback)
        {
            string[] lines = Lines(name, new string[0]);
            return lines.Length > 0 && float.TryParse(lines[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value : fallback;
        }
    }
}
