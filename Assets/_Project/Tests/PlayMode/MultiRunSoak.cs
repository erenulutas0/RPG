using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Cryptforge.Analytics;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // A soak, not a balance check: the development autopilot plays several whole Descents of the foundry back to back, as
    // the owner plays several runs in a row on the phone, and each run is restarted the way a player restarts it - the
    // result screen's Restart button, which reloads the gameplay scene. One profile for the whole soak, so the gold, the
    // telemetry file and its run ordinal carry from run to run as they do on the phone.
    //
    // Several things outlive one enemy or one run: the horde's and the Final Swarm's body pools (EncounterController's
    // shelves), the held telemetry file (TelemetryRuntime's hold counter), the static registries (BurnStatus.Live,
    // GroundedSorting's actor roots) and the x-ray registry (FoundryVisibilityView), and the load-time pre-warms
    // (RunChoiceView.Prewarm, TextPrewarm's font atlas, the ground projection's pre-warm). Each run is measured twice: two
    // frames after its scene loads (the start_ columns) and at its result screen after GC.Collect and
    // Resources.UnloadUnusedAssets (the rest). One row a run goes to TestResults/soak.txt, rewritten after every run, so a
    // soak that stops part way still leaves the runs it played.
    //
    // Asserted only where a leak has no other reading: shelves empty and no burn registered as a run starts; no destroyed
    // burn left in BurnStatus.Live; the telemetry file held by one scene at most; and from run 3 on, against run 2 (run 1
    // pays the first load's one-time work), no growth beyond a small margin in the scene's GameObjects, Health components
    // and live GroundedSorting roots - at the start always, at the result screen when both runs ended alike (the same
    // seed under a fixed 1/60 s step replays the same Descent). Memory, textures, materials, meshes, the font atlas and
    // the x-ray registry are reported, not asserted.
    //
    // Knobs: TestResults/soak-runs.txt, the number of runs (3 when absent); TestResults/soak-seeds.txt, one seed a line,
    // used in turn (11 for every run when absent).
    [Explicit("Soak: run it by name.")]
    public sealed class MultiRunSoak
    {
        private const string GameplayScene = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string RestartButtonPath = "Result Panel/Safe Area/Restart Button";
        // A fixed step of game time a frame, as HordePacingMeasurement plays (Time.captureDeltaTime): the time scale stays 1
        // and a seed replays the same Descent.
        private const float Step = 1f / 60f;
        // HordePacingMeasurement's whole-Descent limit and its stuck guard (a choice nobody answers).
        private const float DescentLimitSeconds = 840f;
        private const int StuckFrames = 36000;
        private const int DefaultRuns = 3;
        private const int DefaultSeed = 11;
        // Margins for the growth checks against run 2.
        private const int StartGameObjectMargin = 16;
        private const int StartHealthMargin = 2;
        private const int StartSortingMargin = 2;
        private const int EndGameObjectMargin = 32;
        private const float EndGameObjectShare = .03f;
        private const int EndHealthMargin = 6;
        private const int EndSortingMargin = 6;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Regex AutopilotLine = new Regex(@"^\[Autopilot\] driving the hero");

        private sealed class Row
        {
            public int Run, Seed, Floors;
            public string Result = "-";
            public float DescentSeconds, WallSeconds;
            public int StartGameObjects, StartHealth, StartBurn, StartBurnDead, StartSorting, StartSortingRaw, StartShelved;
            public long GcBytes, MonoUsed, Allocated, Reserved;
            public int GameObjects, DontDestroy, Health, Burn, BurnDead, Sorting, SortingRaw, Xray, Shelved, Leaving;
            public int TelemetryHolds;
            public bool TelemetryOpen, ResultShown;
            public int Textures, Materials, Meshes;
            public string FontTextures = "-";
        }

        [UnityTearDown] public IEnumerator End()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Soak cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private static string ResultsPath(string name) => Path.Combine(Application.dataPath, "..", "TestResults", name);

        private static int Runs()
        {
            string path = ResultsPath("soak-runs.txt");
            string text = File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
            if (text.Length == 0)
                return DefaultRuns;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int runs) ? runs : -1;
        }

        private static int[] Seeds()
        {
            string path = ResultsPath("soak-seeds.txt");
            var seeds = new List<int>();
            if (File.Exists(path))
                foreach (string line in File.ReadAllLines(path))
                    if (int.TryParse(line.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
                        seeds.Add(seed);
            if (seeds.Count == 0)
                seeds.Add(DefaultSeed);
            return seeds.ToArray();
        }

        // Four hours: a whole Descent takes the Editor about 12 to 13 minutes at the fixed step, so the default three runs
        // about 40 minutes.
        [UnityTest, Timeout(14400000)] public IEnumerator SeveralDescentsInARowLeaveNothingBehind()
        {
            int runs = Runs();
            Assert.That(runs, Is.GreaterThanOrEqualTo(1), "soak-runs.txt holds a number of runs, 1 or more");
            int[] seeds = Seeds();
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.AutopilotPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.AutopilotPath, "on");
            string reportPath = ResultsPath("soak.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var rows = new List<Row>();
            Time.captureDeltaTime = Step;
            CombatSetup previous = null;
            for (int run = 1; run <= runs; run++)
            {
                var row = new Row { Run = run, Seed = seeds[(run - 1) % seeds.Length] };
                File.WriteAllText(DevelopmentStart.RunSeedPath, row.Seed.ToString(CultureInfo.InvariantCulture));
                LogAssert.Expect(LogType.Log, AutopilotLine);
                if (run == 1)
                    yield return SceneManager.LoadSceneAsync(GameplayScene);
                else
                    yield return Restart(previous);
                yield return null;
                yield return null;
                CombatSetup setup = Object.FindFirstObjectByType<CombatSetup>();
                Assert.That(setup, Is.Not.Null, $"Run {run}: the gameplay scene loaded");
                Assert.That(ReferenceEquals(setup, previous), Is.False, $"Run {run}: a fresh scene, not the last run's");
                EncounterController encounters = setup.Encounters;
                Health hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
                MeasureStart(row, encounters);

                // The whole Descent, to the hero's fall or the finale's way out.
                int floors = 0;
                encounters.FloorCleared += () => floors++;
                float start = Time.time, wallStart = Time.realtimeSinceStartup;
                int still = 0;
                while (!setup.Run.HasEnded && Time.time - start < DescentLimitSeconds && still < StuckFrames)
                {
                    yield return null;
                    still = Time.deltaTime <= 0f ? still + 1 : 0;
                }
                row.Floors = floors;
                row.DescentSeconds = Time.time - start;
                row.WallSeconds = Time.realtimeSinceStartup - wallStart;
                if (setup.Run.HasEnded)
                    row.Result = setup.Run.Outcome.ToString().ToLowerInvariant();
                else
                {
                    // Ended by hand so the soak still reaches a result screen and its restart.
                    row.Result = still >= StuckFrames ? "stuck" : "timeout";
                    hero.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
                    yield return null;
                }

                // The result screen, as the player sees it before tapping Restart.
                RunResultView result = Object.FindFirstObjectByType<RunResultView>();
                float deadline = Time.realtimeSinceStartup + 15f;
                while ((result == null || !result.IsOpen) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                row.ResultShown = result != null && result.IsOpen;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
                MeasureEnd(row, encounters);
                rows.Add(row);
                File.WriteAllText(reportPath, Report(rows, runs, seeds));
                Assert.That(row.ResultShown, Is.True, $"Run {run}: the result screen opened ({row.Result})");
                previous = setup;
            }

            var failures = new List<string>();
            foreach (Row row in rows)
            {
                if (row.StartShelved != 0)
                    failures.Add($"run {row.Run}: {row.StartShelved} bodies shelved or leaving as the run started");
                if (row.StartBurn != 0)
                    failures.Add($"run {row.Run}: BurnStatus.Live held {row.StartBurn} burns as the run started");
                if (row.StartBurnDead != 0 || row.BurnDead != 0)
                    failures.Add($"run {row.Run}: BurnStatus.Live held destroyed burns (start {row.StartBurnDead}, end {row.BurnDead})");
                if (row.TelemetryHolds > 1)
                    failures.Add($"run {row.Run}: the telemetry file held by {row.TelemetryHolds} scenes");
                if (row.TelemetryHolds != rows[0].TelemetryHolds)
                    failures.Add($"run {row.Run}: telemetry holds {row.TelemetryHolds}, run 1 {rows[0].TelemetryHolds}");
            }
            if (rows.Count >= 3)
            {
                Row second = rows[1];
                for (int i = 2; i < rows.Count; i++)
                {
                    Row row = rows[i];
                    Grew(failures, row, "start GameObjects", row.StartGameObjects, second.StartGameObjects, StartGameObjectMargin);
                    Grew(failures, row, "start Health", row.StartHealth, second.StartHealth, StartHealthMargin);
                    Grew(failures, row, "start GroundedSorting roots", row.StartSorting, second.StartSorting, StartSortingMargin);
                    if (row.Result != second.Result || row.Floors != second.Floors)
                        continue;
                    int goMargin = EndGameObjectMargin + Mathf.CeilToInt(second.GameObjects * EndGameObjectShare);
                    Grew(failures, row, "result-screen GameObjects", row.GameObjects, second.GameObjects, goMargin);
                    Grew(failures, row, "result-screen Health", row.Health, second.Health, EndHealthMargin);
                    Grew(failures, row, "result-screen GroundedSorting roots", row.Sorting, second.Sorting, EndSortingMargin);
                }
            }
            Assert.That(failures, Is.Empty, "TestResults/soak.txt:\n" + string.Join("\n", failures));
        }

        private static void Grew(List<string> failures, Row row, string what, int value, int second, int margin)
        {
            if (value > second + margin)
                failures.Add($"run {row.Run}: {what} {value}, run 2 {second} (margin {margin})");
        }

        // The player's tap on the result screen's Restart, once its input delay has passed; then the reloaded scene.
        private static IEnumerator Restart(CombatSetup previous)
        {
            RunResultView result = Object.FindFirstObjectByType<RunResultView>();
            Assert.That(result, Is.Not.Null, "The result screen to restart from");
            Transform found = result.transform.Find(RestartButtonPath);
            Assert.That(found, Is.Not.Null, "The result screen's Restart button");
            var restart = found.GetComponent<UnityEngine.UI.Button>();
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!restart.interactable && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(restart.interactable, Is.True, "Restart takes taps once its input delay is over");
            int loads = 0;
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> counted = (scene, mode) => loads++;
            SceneManager.sceneLoaded += counted;
            try
            {
                ExecuteEvents.Execute(restart.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                deadline = Time.realtimeSinceStartup + 60f;
                while ((loads == 0 || previous != null) && Time.realtimeSinceStartup < deadline)
                    yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= counted;
            }
            Assert.That(loads, Is.EqualTo(1), "One tap reloads the gameplay scene once");
            Assert.That(previous == null, Is.True, "The last run's scene is gone");
        }

        private static void MeasureStart(Row row, EncounterController encounters)
        {
            row.StartGameObjects = SceneObjects(out _);
            row.StartHealth = SceneComponents<Health>();
            row.StartBurn = Registry(BurnLive(), out row.StartBurnDead);
            row.StartSorting = Registry(SortingRoots(), out int deadRoots);
            row.StartSortingRaw = row.StartSorting + deadRoots;
            row.StartShelved = IntProperty(encounters, "ShelvedBodyCount") + IntProperty(encounters, "LeavingBodyCount");
        }

        private static void MeasureEnd(Row row, EncounterController encounters)
        {
            row.GcBytes = GC.GetTotalMemory(false);
            row.MonoUsed = Profiler.GetMonoUsedSizeLong();
            row.Allocated = Profiler.GetTotalAllocatedMemoryLong();
            row.Reserved = Profiler.GetTotalReservedMemoryLong();
            row.GameObjects = SceneObjects(out row.DontDestroy);
            row.Health = SceneComponents<Health>();
            row.Burn = Registry(BurnLive(), out row.BurnDead);
            row.Sorting = Registry(SortingRoots(), out int deadRoots);
            row.SortingRaw = row.Sorting + deadRoots;
            var visibility = Object.FindFirstObjectByType<FoundryVisibilityView>();
            row.Xray = visibility != null ? IntProperty(visibility, "XrayedCount") : -1;
            row.Shelved = IntProperty(encounters, "ShelvedBodyCount");
            row.Leaving = IntProperty(encounters, "LeavingBodyCount");
            FieldInfo holds = typeof(TelemetryRuntime).GetField("_holds", Static);
            FieldInfo store = typeof(TelemetryRuntime).GetField("_store", Static);
            Assert.That(holds, Is.Not.Null, "TelemetryRuntime._holds");
            Assert.That(store, Is.Not.Null, "TelemetryRuntime._store");
            row.TelemetryHolds = (int)holds.GetValue(null);
            var file = store.GetValue(null) as FileTelemetryStore;
            row.TelemetryOpen = file != null && file.HoldsAFile;
            row.Textures = Resources.FindObjectsOfTypeAll<Texture>().Length;
            row.Materials = Resources.FindObjectsOfTypeAll<Material>().Length;
            row.Meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length;
            row.FontTextures = FontTextures();
        }

        // Every GameObject in a loaded scene, inactive ones (shelved bodies) and DontDestroyOnLoad's among them; prefabs and
        // other assets have no valid scene.
        private static int SceneObjects(out int dontDestroy)
        {
            int count = 0;
            dontDestroy = 0;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                Scene scene = go.scene;
                if (!scene.IsValid())
                    continue;
                count++;
                if (scene.name == "DontDestroyOnLoad")
                    dontDestroy++;
            }
            return count;
        }

        private static int SceneComponents<T>() where T : Component
        {
            int count = 0;
            foreach (T component in Resources.FindObjectsOfTypeAll<T>())
                if (component.gameObject.scene.IsValid())
                    count++;
            return count;
        }

        // A registry's entries still standing; dead, those destroyed but still listed.
        private static int Registry(IList list, out int dead)
        {
            int live = 0;
            dead = 0;
            foreach (object entry in list)
            {
                if (entry is Object unity && unity != null)
                    live++;
                else
                    dead++;
            }
            return live;
        }

        private static IList BurnLive()
        {
            FieldInfo field = typeof(BurnStatus).GetField("Live", Static);
            Assert.That(field, Is.Not.Null, "BurnStatus.Live");
            return (IList)field.GetValue(null);
        }

        private static IList SortingRoots()
        {
            Type sorting = typeof(RunResultView).Assembly.GetType("Cryptforge.UI.GroundedSorting");
            Assert.That(sorting, Is.Not.Null, "Cryptforge.UI.GroundedSorting");
            FieldInfo field = sorting.GetField("ActorRoots", Static);
            Assert.That(field, Is.Not.Null, "GroundedSorting.ActorRoots");
            return (IList)field.GetValue(null);
        }

        private static int IntProperty(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(name, Instance);
            Assert.That(property, Is.Not.Null, target.GetType().Name + "." + name);
            return (int)property.GetValue(target);
        }

        // The atlas of every font a scene label draws with, as name:widthxheight - TextPrewarm grows it at load.
        private static string FontTextures()
        {
            var fonts = new List<Font>();
            foreach (UnityEngine.UI.Text text in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>())
                if (text.gameObject.scene.IsValid() && text.font != null && !fonts.Contains(text.font))
                    fonts.Add(text.font);
            if (fonts.Count == 0)
                return "-";
            var parts = new List<string>();
            foreach (Font font in fonts)
            {
                Texture texture = font.material != null ? font.material.mainTexture : null;
                string size = texture != null
                    ? texture.width.ToString(CultureInfo.InvariantCulture) + "x" + texture.height.ToString(CultureInfo.InvariantCulture)
                    : "none";
                parts.Add(font.name.Replace(' ', '_') + ":" + size);
            }
            parts.Sort(StringComparer.Ordinal);
            return string.Join(",", parts);
        }

        private static string Report(List<Row> rows, int runs, int[] seeds)
        {
            var report = new StringBuilder();
            var seedText = new List<string>();
            foreach (int seed in seeds)
                seedText.Add(seed.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("soak runs " + runs.ToString(CultureInfo.InvariantCulture) + " seeds " + string.Join(",", seedText) +
                " step 1/60 descent_limit_s " + DescentLimitSeconds.ToString("0", CultureInfo.InvariantCulture) +
                " (start_ columns two frames after the scene loads; the rest at the result screen after GC.Collect and Resources.UnloadUnusedAssets)");
            report.AppendLine("run\tseed\tresult\tfloors\tdescent_s\twall_s\tstart_go\tstart_health\tstart_burn\tstart_burn_dead\t" +
                "start_sorting\tstart_sorting_raw\tstart_shelved\tgc_mb\tmono_mb\talloc_mb\treserved_mb\tgo\tddol_go\thealth\tburn\t" +
                "burn_dead\tsorting\tsorting_raw\txray\tshelved\tleaving\ttel_holds\ttel_open\ttextures\tmaterials\tmeshes\tfont_tex\tresult_shown");
            foreach (Row r in rows)
            {
                report.AppendLine(string.Join("\t",
                    I(r.Run), I(r.Seed), r.Result, I(r.Floors), F(r.DescentSeconds), F(r.WallSeconds),
                    I(r.StartGameObjects), I(r.StartHealth), I(r.StartBurn), I(r.StartBurnDead), I(r.StartSorting),
                    I(r.StartSortingRaw), I(r.StartShelved), Mb(r.GcBytes), Mb(r.MonoUsed), Mb(r.Allocated), Mb(r.Reserved),
                    I(r.GameObjects), I(r.DontDestroy), I(r.Health), I(r.Burn), I(r.BurnDead), I(r.Sorting), I(r.SortingRaw),
                    r.Xray >= 0 ? I(r.Xray) : "-", I(r.Shelved), I(r.Leaving), I(r.TelemetryHolds), r.TelemetryOpen ? "1" : "0",
                    I(r.Textures), I(r.Materials), I(r.Meshes), r.FontTextures, r.ResultShown ? "1" : "0"));
            }
            return report.ToString();
        }

        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string F(float value) => value.ToString("0", CultureInfo.InvariantCulture);
        private static string Mb(long bytes) => (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture);
    }
}
