using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
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
    // of each window. Frames the probe works in, and paused ones, are left out of the summary.
    //
    // Run it by name. Draw calls, batches and SetPass calls need a graphics device: Tools/Run-PerfProbe.ps1 runs it in a
    // graphics-enabled batch Editor, which renders the camera into a 1080x2340 target each frame (the batch Editor has no
    // Game view); Tools/Run-UnityTests.ps1 -Platform PlayMode -Filter Cryptforge.Tests.PerfProbe runs it -nographics,
    // where nothing renders and only the CPU, GC and census figures mean anything. In the interactive Test Runner the
    // Game view renders (set it to 1080x2340). Knobs, one value per line, in TestResults/: perf-probe-heroes.txt (hero ids,
    // "vanguard" for the default hero), perf-probe-phases.txt (horde, warden), perf-probe-seconds.txt (the horde window),
    // perf-probe-seed.txt. It changes no game code and asserts nothing about speed.
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
        private static readonly string[] DefaultHeroes = { "vanguard", "hero_cinder_arcanist" };
        private static readonly string[] DefaultPhases = { "horde", "warden" };
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
            public readonly List<PerfCensus> Censuses = new List<PerfCensus>();
            public readonly StringBuilder Text = new StringBuilder();
            public readonly StringBuilder Frames = new StringBuilder();
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

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Perf probe cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
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
            Header(report, seed, hordeSeconds);
            frames.AppendLine(FrameColumns);
            foreach (string hero in heroes)
                foreach (string phase in phases)
                {
                    var run = new Run { Hero = hero, Phase = phase, Seed = seed, HordeSeconds = hordeSeconds };
                    yield return Play(run);
                    report.Append(run.Text);
                    frames.Append(run.Frames);
                    // After every phase, so a run cut short keeps what it measured.
                    Write(report, frames);
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
            // camera, so the HUD's draws count as they do on the device.
            RenderTexture target = null;
            var canvases = new List<Canvas>();
            var modes = new List<RenderMode>();
            if (run.ManualRender)
            {
                target = new RenderTexture(PhoneWidth, PhoneHeight, 24) { name = "Perf probe target" };
                camera.targetTexture = target;
                foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        continue;
                    canvases.Add(canvas);
                    modes.Add(canvas.renderMode);
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                }
            }
            if (follow != null)
                follow.Frame(PhoneWidth, PhoneHeight, BandBottom, BandTop);
            // CombatSetup caps the frame rate at 60; game time is fixed by captureDeltaTime, so the Editor runs uncapped
            // and the wall time is the frame's own work rather than a wait for the next 1/60 s.
            int targetFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = -1;
            var recorder = new GameObject("Perf Probe Recorder").AddComponent<PerfProbeRecorder>();
            recorder.Initialize(setup, hero, camera, run.ManualRender);

            if (run.Phase == "warden")
                yield return Warden(run, setup, hero, camera, follow, recorder);
            else
                yield return Horde(run, setup, hero, camera, recorder);
            recorder.End();
            Summarize(run, recorder);
            Application.targetFrameRate = targetFrameRate;

            for (int i = 0; i < canvases.Count; i++)
                if (canvases[i] != null)
                    canvases[i].renderMode = modes[i];
            if (camera != null)
                camera.targetTexture = null;
            if (target != null)
            {
                target.Release();
                Object.Destroy(target);
            }
            Time.captureDeltaTime = 0f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Perf probe between " + run.Hero + " " + run.Phase));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
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
                    run.DepthMicros = TimeActorDepth(visibility, out run.DepthPlaced);
                }
            }
            run.EndedEarly = setup.Run.HasEnded;
        }

        // The run goes on for the measurement: offers are answered with their first card and the hero is kept standing.
        private static void Keep(CombatSetup setup, Health hero)
        {
            for (int i = 0; i < 8 && setup.Choices.Current != null && setup.Choices.Current.Kind == ChoiceKind.Upgrade; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            if (hero != null && hero.IsAlive && hero.Current < hero.Maximum * HealBelow)
                hero.Heal(hero.Maximum);
        }

        // The foundry's depth pass with everything on show now, as every camera render pays it: once to warm, then timed.
        private static float TimeActorDepth(FoundryVisibilityView visibility, out int placed)
        {
            visibility.ApplyActorDepth();
            placed = visibility.PlacedCount;
            visibility.RestoreActorDepth();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < DepthRuns; i++)
            {
                visibility.ApplyActorDepth();
                visibility.RestoreActorDepth();
            }
            watch.Stop();
            return (float)(watch.Elapsed.TotalMilliseconds * 1000d / DepthRuns);
        }

        private static void Header(StringBuilder report, int seed, float hordeSeconds)
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
                .Append(", floor clock from ").Append(F(FloorClockStart, 0)).Append(" s, overrun at 300 s); Warden ")
                .Append(F(WardenFirstSeconds, 0)).Append(" s + ").Append(F(WardenSecondSeconds, 0)).Append(" s phase 2 + ")
                .Append(F(WardenFallSeconds, 0)).Append(" s fall; autopilot on; offers answered with the first card at once; ")
                .Append("hero healed below ").Append(F(HealBelow * 100f, 0))
                .AppendLine("%; frames are 1/60 s of game time, run uncapped (targetFrameRate -1 while measuring).");
            report.AppendLine("left out: frames the probe worked in (census every " + CensusEvery +
                " frames, the depth timing) and paused frames (an open choice). The recorders' values are the frame's own.");
            report.AppendLine("caution: Editor CPU and GC are the Editor's, not the S23's - compare builds against each other " +
                "on this machine, and read the phone with development/render-stats.txt. ms are milliseconds of wall or main-thread time.");
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
            t.Append("== ").Append(run.HeroName).Append(" (").Append(run.Hero).Append(") / ").Append(run.Phase).Append(": ")
                .Append(included).Append(" frames measured of ").Append(r.Count).Append(" (").Append(paused).Append(" paused, ")
                .Append(probe).Append(" the probe's), ").Append(F(r.Count * Step, 1)).Append(" s of game time after ")
                .Append(F(run.WarmSeconds, 1)).Append(" s walking in");
            if (run.EndedEarly) t.Append("; THE RUN ENDED inside the window");
            if (run.Note.Length > 0) t.Append("; ").Append(run.Note);
            t.AppendLine();
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
            t.Append("   frame ms (wall): ").Append(Full(wall, 2)).Append(" | over 16.7 ms: ")
                .Append(CountOver(r, r.WallMs, 1000f / 60f)).Append(", over 33.3 ms: ").Append(CountOver(r, r.WallMs, 1000f / 30f))
                .AppendLine();
            t.Append("   main thread ms: ").AppendLine(r.MainThreadValid ? Full(Of(r, r.MainThreadMs), 2) : "na");
            if (r.GcValid)
            {
                Spread gc = Of(r, r.GcBytes), allocs = Of(r, r.GcCount);
                int allocating = 0;
                for (int f = 0; f < r.Count; f++)
                    if (r.IsIncluded(f) && r.GcBytes[f] > 0f) allocating++;
                t.Append("   GC per frame: mean ").Append(F(gc.Mean / 1024f, 2)).Append(" KB, median ")
                    .Append(F(gc.Median / 1024f, 2)).Append(" KB, p95 ").Append(F(gc.P95 / 1024f, 2)).Append(" KB, max ")
                    .Append(F(gc.Max / 1024f, 1)).Append(" KB; ").Append(F(100f * allocating / Mathf.Max(1, gc.Count), 0))
                    .Append("% of frames allocate; ").Append(F(gc.Sum / 1048576f, 2)).Append(" MB in the window; allocations a frame: ")
                    .Append(allocs.Count > 0 ? F(allocs.Mean, 1) + " mean, " + F(allocs.Max, 0) + " max" : "na")
                    .Append("; GC.Collect runs ").Append(r.CollectionsAtEnd - r.CollectionsAtBegin).AppendLine();
            }
            else
                t.AppendLine("   GC per frame: na (no GC Allocated In Frame counter)");
            if (r.RenderCountersValid && Of(r, r.DrawCalls).Max > 0f)
                t.Append("   render (profiler counters): draw calls ").Append(Triple(Of(r, r.DrawCalls), 0)).Append(" | batches ")
                    .Append(Triple(Of(r, r.Batches), 0)).Append(" | SetPass ").Append(Triple(Of(r, r.SetPassCalls), 0))
                    .Append(" | triangles ").Append(Triple(Of(r, r.Triangles), 0)).Append(" | vertices ")
                    .Append(Triple(Of(r, r.Vertices), 0)).AppendLine();
            else
                t.AppendLine("   render (profiler counters): na - nothing rendered, or this Editor reports no render counters");
#if UNITY_EDITOR
            if (Of(r, r.StatsDraw).Max > 0f)
                t.Append("   render (UnityStats): draw calls ").Append(Triple(Of(r, r.StatsDraw), 0)).Append(" | batches ")
                    .Append(Triple(Of(r, r.StatsBatches), 0)).Append(" | SetPass ").Append(Triple(Of(r, r.StatsSetPass), 0))
                    .Append(" | dynamic-batched draws ").Append(Triple(Of(r, r.StatsDynamicDraws), 0))
                    .Append(" | dynamic batches ").Append(Triple(Of(r, r.StatsDynamicBatches), 0)).AppendLine();
            else
                t.AppendLine("   render (UnityStats): na - nothing rendered, or the stats only follow the Game view");
#endif
            t.Append("   frames with (median wall ms, count): none of these ").Append(With(r, f =>
                    r.Spawns[f] + r.Waves[f] + r.Deaths[f] + r.LevelUps[f] + r.Picks[f] + r.Vents[f] + r.AbilityUses[f] +
                    r.Slams[f] == 0f))
                .Append(" | horde spawn ").Append(With(r, f => r.Spawns[f] > 0f)).Append(" | wave ").Append(With(r, f => r.Waves[f] > 0f))
                .Append(" | death ").Append(With(r, f => r.Deaths[f] > 0f)).Append(" | 5+ deaths ").Append(With(r, f => r.Deaths[f] >= 5f))
                .Append(" | level-up or pick ").Append(With(r, f => r.LevelUps[f] + r.Picks[f] > 0f))
                .Append(" | ability ").Append(With(r, f => r.AbilityUses[f] > 0f)).Append(" | slam ").Append(With(r, f => r.Slams[f] > 0f))
                .AppendLine();
            AppendSlowest(t, r);
            AppendMarkers(t, r);
            AppendCensus(t, run);
            t.Append("   foundry depth pass (apply + restore, all on show): ")
                .Append(run.DepthMicros >= 0f ? F(run.DepthMicros, 1) + " us for " + run.DepthPlaced + " transforms placed, mean of " + DepthRuns : "na")
                .AppendLine();
            t.Append("   memory at the end: total allocated ").Append(F(Profiler.GetTotalAllocatedMemoryLong() / 1048576f, 0))
                .Append(" MB, managed heap ").Append(F(Profiler.GetMonoHeapSizeLong() / 1048576f, 0)).Append(" MB (")
                .Append(F(Profiler.GetMonoUsedSizeLong() / 1048576f, 0)).Append(" used), graphics driver ")
                .Append(F(Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f, 0)).Append(" MB, textures ")
                .Append(F(Texture.currentTextureMemory / 1048576f, 0)).AppendLine(" MB (the Editor's own included)");
            t.AppendLine();
            AppendFrames(run, r);
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
                s.Append(run.Hero).Append('\t').Append(run.Phase).Append('\t').Append(f).Append('\t').Append(r.Flags[f]);
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
        private static Spread Of(PerfProbeRecorder r, float[] values)
        {
            var kept = new List<float>(r.Count);
            for (int f = 0; f < r.Count; f++)
                if (r.IsIncluded(f) && values[f] >= 0f)
                    kept.Add(values[f]);
            return SpreadOf(kept);
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
