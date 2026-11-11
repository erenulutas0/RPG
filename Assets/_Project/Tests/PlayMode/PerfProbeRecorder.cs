using System.Collections.Generic;
using System.Diagnostics;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cryptforge.Tests
{
    // PerfProbe's frame log: the last LateUpdate of every frame, after every view has placed what it draws. In a
    // graphics-enabled batch Editor nothing renders on its own (there is no Game view), so it renders the gameplay
    // camera here into the probe's phone-sized target, which puts the camera's culling, batching and the foundry's depth
    // pass into the frame as a device has them. Each frame it stores, indexed by the frame:
    //   - the wall time between this LateUpdate and the last one (the whole frame as the Editor ran it);
    //   - the profiler's main-thread time, GC bytes and allocation count, draw calls, batches, SetPass calls,
    //     triangles and vertices of the frame (the recorders' last value, read one frame later, so stored one back);
    //   - UnityStats' draw calls, batches, SetPass calls and dynamic batching (Editor only, read after the render);
    //   - the scenario: enemies alive, horde alive, burning enemies, active effects; and the events of the frame.
    // The probe flags the frames it did work of its own in (ProbeWork) and those the run stood still in (Paused), and
    // the summary leaves both out. Allocates nothing per frame once begun; arrays are made up front.
    [AddComponentMenu("")]
    [DefaultExecutionOrder(32000)]
    internal sealed class PerfProbeRecorder : MonoBehaviour
    {
        internal const int Capacity = 12000;
        internal const byte ProbeWork = 1;
        internal const byte Paused = 2;
        private const int MaxMarkers = 64;
        private static readonly double MillisecondsPerTick = 1000d / Stopwatch.Frequency;

        // Exact names worth a line of their own, with PerformanceMarkers.PickNames (DiscoverMarkers); any marker naming one
        // of ScriptNames is taken too. The engine's first-use work - a shader variant's GPU program, a hierarchy switched
        // on or off - is asked for by the names the Editor may give it; one it does not offer is skipped, and every name on
        // offer is listed in TestResults/perf-probe-markers.txt.
        private static readonly string[] WantedMarkers =
        {
            "PlayerLoop", "Update.ScriptRunBehaviourUpdate", "BehaviourUpdate",
            "PreLateUpdate.ScriptRunBehaviourLateUpdate", "LateBehaviourUpdate", "Update.ScriptRunDelayedDynamicFrameRate",
            "CoroutinesDelayedCalls", "PostLateUpdate.UpdateAllRenderers", "PostLateUpdate.PlayerUpdateCanvases",
            "Canvas.SendWillRenderCanvases", "Canvas.BuildBatch", "UGUI.Rendering.RenderOverlays", "Canvas.RenderOverlays",
            "PostLateUpdate.FinishFrameRendering", "Camera.Render", "Culling", "Drawing", "Render.OpaqueGeometry",
            "Render.TransparentGeometry", "RenderForward.RenderLoopJob", "Batch.DrawDynamic", "Batch.DrawStatic",
            "Gfx.WaitForPresentOnGfxThread", "GC.Collect", "Instantiate", "Destroy", PerformanceMarkers.StartWaveName,
            "PostLateUpdate.UpdateSortingGroups", "SortingGroupManager.Update", "PostLateUpdate.UpdateRectTransform",
            "WaitForTargetFPS", "Gfx.WaitForPresent", "EditorLoop",
            "Shader.CreateGPUProgram", "CreateGPUProgram", "Shader.Parse", "GameObject.Activate", "GameObject.Deactivate",
            "Font.RequestCharactersInTexture", "TextGenerator.Populate", "Canvas.SendWillRenderCanvases.Layout",
        };
        internal static readonly string[] ScriptNames =
        {
            "EnemyLookView", "ContactShadowView", "CombatantView", "CombatEffectsView", "EnemyBarTrail", "BurnStatus",
            "AttackController", "Targeting", "EncounterController", "PuppetAnimator", "RenderedCharacterAnimator",
            "HeroLookView", "HeroMovementInput", "RunFeedbackView", "HudArtView", "RunHud", "AbilityButtonView",
            "AbilityRingView", "AbilityController", "GroundDropView", "FoundryVisibilityView", "FoundryAtmosphereView",
            "FoundryFlameMotion", "FoundryBannerMotion", "FoundryGateDoorMotion", "ArenaCameraFollow", "WardenSlamView",
            "WardenSlamService", "PitEruptionService", "PitEruptionView", "AltarView", "AltarService", "ExitSealView",
            "FloorClockView", "DangerReadoutView", "HeroStaminaView", "RunTelemetryBridge", "ChestSpawner",
            "DevelopmentAutopilot", "SafeAreaFitter", "BossRevealView", "LoadoutBadgesView", "RunChoiceView",
        };

        internal readonly float[] WallMs = new float[Capacity];
        internal readonly float[] MainThreadMs = new float[Capacity];
        internal readonly float[] GcBytes = new float[Capacity];
        internal readonly float[] GcCount = new float[Capacity];
        internal readonly float[] DrawCalls = new float[Capacity];
        internal readonly float[] Batches = new float[Capacity];
        internal readonly float[] SetPassCalls = new float[Capacity];
        internal readonly float[] Triangles = new float[Capacity];
        internal readonly float[] Vertices = new float[Capacity];
        internal readonly float[] StatsDraw = new float[Capacity];
        internal readonly float[] StatsBatches = new float[Capacity];
        internal readonly float[] StatsSetPass = new float[Capacity];
        internal readonly float[] StatsDynamicDraws = new float[Capacity];
        internal readonly float[] StatsDynamicBatches = new float[Capacity];
        internal readonly float[] Alive = new float[Capacity];
        internal readonly float[] Horde = new float[Capacity];
        internal readonly float[] Burning = new float[Capacity];
        internal readonly float[] Effects = new float[Capacity];
        internal readonly byte[] Flags = new byte[Capacity];
        internal readonly float[] Spawns = new float[Capacity];
        internal readonly float[] Waves = new float[Capacity];
        internal readonly float[] Deaths = new float[Capacity];
        internal readonly float[] Strikes = new float[Capacity];
        internal readonly float[] AbilityUses = new float[Capacity];
        internal readonly float[] Vents = new float[Capacity];
        internal readonly float[] LevelUps = new float[Capacity];
        internal readonly float[] Picks = new float[Capacity];
        internal readonly float[] Slams = new float[Capacity];
        internal readonly float[] Progress = new float[Capacity];
        internal readonly float[] BurnTicks = new float[Capacity];

        // Discovered at Begin: the markers this Editor or player provides, and each one's time per frame.
        internal readonly List<string> MarkerNames = new List<string>();
        // Every time marker on offer at Begin, chosen or not ("category: name"), for TestResults/perf-probe-markers.txt.
        internal readonly List<string> MarkersOnOffer = new List<string>();
        internal float[][] MarkerMs = new float[0][];
        private ProfilerRecorder[] _markers = new ProfilerRecorder[0];

        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _gcBytes;
        private ProfilerRecorder _gcCount;
        private ProfilerRecorder _draw;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _vertices;
        private bool _recorders;
        // What the recorders offered, kept at End: the summary reads them after End has disposed the recorders, when every
        // recorder reads invalid (and the summary said "na" for the main thread, the GC and the render counts it had).
        private bool _mainThreadOffered;
        private bool _gcOffered;
        private bool _renderOffered;

        private CombatSetup _setup;
        private EncounterController _encounters;
        private RunChoices _choices;
        private CombatEffectsView _effects;
        private AttackController _heroAttack;
        private AbilityController _ability;
        private Camera _camera;
        // Rendering by hand from Initialize to End; the probe takes the camera's target away after End.
        private bool _manualRender;
        private bool _recording;
        private bool _subscribed;
        private int _base;
        private long _lastStamp;

        // Frames recorded so far, from the frame Begin was called in (index 0), and the index of the frame running now.
        internal int Count { get; private set; }
        internal int CurrentFrame => Time.frameCount - _base;
        internal int CollectionsAtBegin { get; private set; }
        internal int CollectionsAtEnd { get; private set; }
        internal bool MainThreadValid => _recorders ? _mainThread.Valid : _mainThreadOffered;
        internal bool GcValid => _recorders ? _gcBytes.Valid : _gcOffered;
        internal bool RenderCountersValid => _recorders ? _draw.Valid : _renderOffered;

        internal void Initialize(CombatSetup setup, Health hero, Camera camera, bool manualRender)
        {
            _setup = setup;
            _encounters = setup.Encounters;
            _choices = setup.Choices;
            _effects = FindFirstObjectByType<CombatEffectsView>();
            _heroAttack = hero != null ? hero.GetComponent<AttackController>() : null;
            _ability = hero != null ? hero.GetComponent<AbilityController>() : null;
            _camera = camera;
            _manualRender = manualRender;
        }

        // From this frame on: the recorders run and the events count. Frame 0 is this one, flagged as the probe's own.
        internal void Begin()
        {
            _base = Time.frameCount;
            Count = 0;
            System.Array.Clear(Flags, 0, Flags.Length);
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            _gcBytes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _gcCount = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocation In Frame Count");
            _draw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            _recorders = true;
            DiscoverMarkers();
            Subscribe();
            CollectionsAtBegin = System.GC.CollectionCount(0);
            _recording = true;
            MarkProbeWork();
        }

        internal void End()
        {
            _manualRender = false;
            if (!_recording)
                return;
            _recording = false;
            CollectionsAtEnd = System.GC.CollectionCount(0);
            _mainThreadOffered = _mainThread.Valid;
            _gcOffered = _gcBytes.Valid;
            _renderOffered = _draw.Valid;
            Unsubscribe();
            DisposeRecorders();
        }

        // The probe did work of its own this frame (a census, a timing, its own bookkeeping): leave the frame out.
        internal void MarkProbeWork()
        {
            int frame = Time.frameCount - _base;
            if (frame >= 0 && frame < Capacity)
                Flags[frame] |= ProbeWork;
        }

        // Kept by the summary: neither the probe's nor paused, timed, and not the last, whose recorder values would only
        // have been read in the frame after End.
        internal bool IsIncluded(int frame) => frame >= 0 && frame < Count - 1 && Flags[frame] == 0 && WallMs[frame] > 0f;

        private void OnDestroy()
        {
            Unsubscribe();
            DisposeRecorders();
        }

        private void LateUpdate()
        {
            // No Game view renders in batch mode: the probe's camera draws the frame here, after every LateUpdate.
            // The player loop updates the sorting groups only in PostLateUpdate, after this: a body deactivated this frame
            // (EncounterController.RemoveBody, a wave released as the next spawns) left a stale group that Unity's renderer
            // asserted on ("Invalid sorting group ID"), failing the probe from 4db8f5b6 on. A frame drawn by the engine
            // itself renders after that update, so the probe brings it forward to draw what a phone would.
            if (_manualRender && _camera != null && _camera.isActiveAndEnabled)
            {
                UnityEngine.Rendering.SortingGroup.UpdateAllSortingGroups();
                _camera.Render();
            }
            long now = Stopwatch.GetTimestamp();
            if (_recording)
                Record(now);
            _lastStamp = now;
        }

        private void Record(long now)
        {
            int frame = Time.frameCount - _base;
            if (frame >= 0 && frame < Capacity)
            {
                if (_lastStamp != 0)
                    WallMs[frame] = (float)((now - _lastStamp) * MillisecondsPerTick);
                if (Time.deltaTime <= 0f || (_choices != null && _choices.IsOpen))
                    Flags[frame] |= Paused;
                Alive[frame] = _encounters != null ? _encounters.AllAliveCount : 0;
                Horde[frame] = _encounters != null ? _encounters.HordeAliveCount : 0;
                Burning[frame] = _effects != null ? _effects.ShownFlameCount : 0;
                Effects[frame] = _effects != null ? _effects.ActiveEffectCount : 0;
#if UNITY_EDITOR
                // After this frame's render in batch mode; the last Game view render otherwise.
                StatsDraw[frame] = UnityStats.drawCalls;
                StatsBatches[frame] = UnityStats.batches;
                StatsSetPass[frame] = UnityStats.setPassCalls;
                StatsDynamicDraws[frame] = UnityStats.dynamicBatchedDrawCalls;
                StatsDynamicBatches[frame] = UnityStats.dynamicBatches;
#endif
                Count = frame + 1;
            }
            // The recorders' last value is the frame before this one.
            int previous = frame - 1;
            if (previous < 0 || previous >= Capacity)
                return;
            MainThreadMs[previous] = Milliseconds(_mainThread);
            GcBytes[previous] = Last(_gcBytes);
            GcCount[previous] = Last(_gcCount);
            DrawCalls[previous] = Last(_draw);
            Batches[previous] = Last(_batches);
            SetPassCalls[previous] = Last(_setPass);
            Triangles[previous] = Last(_triangles);
            Vertices[previous] = Last(_vertices);
            for (int i = 0; i < _markers.Length; i++)
                MarkerMs[i][previous] = Milliseconds(_markers[i]);
        }

        private static float Last(in ProfilerRecorder recorder) =>
            recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1f;

        private static float Milliseconds(in ProfilerRecorder recorder) =>
            recorder.Valid && recorder.Count > 0 ? recorder.LastValue / 1e6f : -1f;

        // The markers on offer, by name: the wanted ones and every one naming a script of the game's that runs per frame.
        private void DiscoverMarkers()
        {
            // Reading the names creates the game's own markers, so they are on offer even before their first use.
            var wanted = new HashSet<string>(WantedMarkers);
            wanted.UnionWith(PerformanceMarkers.PickNames);
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var chosen = new List<ProfilerRecorderHandle>();
            MarkerNames.Clear();
            MarkersOnOffer.Clear();
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.UnitType == ProfilerMarkerDataUnit.TimeNanoseconds && !string.IsNullOrEmpty(description.Name))
                    MarkersOnOffer.Add(description.Category.Name + ": " + description.Name);
            }
            MarkersOnOffer.Sort(System.StringComparer.Ordinal);
            // Wanted names first, so the cap never pushes them out; then the scripts', in the order found.
            for (int pass = 0; pass < 2 && chosen.Count < MaxMarkers; pass++)
            {
                foreach (ProfilerRecorderHandle handle in handles)
                {
                    if (chosen.Count >= MaxMarkers)
                        break;
                    ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                    if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds)
                        continue;
                    string name = description.Name;
                    if (string.IsNullOrEmpty(name) || MarkerNames.Contains(name))
                        continue;
                    bool take = pass == 0 ? wanted.Contains(name) : NamesAScript(name);
                    if (!take)
                        continue;
                    chosen.Add(handle);
                    MarkerNames.Add(name);
                }
            }
            _markers = new ProfilerRecorder[chosen.Count];
            MarkerMs = new float[chosen.Count][];
            for (int i = 0; i < chosen.Count; i++)
            {
                _markers[i] = new ProfilerRecorder(chosen[i], 1, ProfilerRecorderOptions.Default);
                _markers[i].Start();
                MarkerMs[i] = new float[Capacity];
            }
        }

        private static bool NamesAScript(string name)
        {
            if (name.StartsWith("Cryptforge.", System.StringComparison.Ordinal))
                return true;
            foreach (string script in ScriptNames)
                if (name.IndexOf(script, System.StringComparison.Ordinal) >= 0)
                    return true;
            return false;
        }

        private void DisposeRecorders()
        {
            if (!_recorders)
                return;
            _recorders = false;
            _mainThread.Dispose();
            _gcBytes.Dispose();
            _gcCount.Dispose();
            _draw.Dispose();
            _batches.Dispose();
            _setPass.Dispose();
            _triangles.Dispose();
            _vertices.Dispose();
            for (int i = 0; i < _markers.Length; i++)
                _markers[i].Dispose();
            _markers = new ProfilerRecorder[0];
        }

        private void Subscribe()
        {
            if (_subscribed || _encounters == null)
                return;
            _encounters.HordeEnemySpawned += OnSpawned;
            _encounters.EncounterStarted += OnWave;
            _encounters.EnemyDefeated += OnDefeated;
            _encounters.ProgressChanged += OnProgress;
            if (_heroAttack != null)
                _heroAttack.Struck += OnStruck;
            if (_ability != null)
            {
                _ability.Used += OnAbility;
                _ability.Vented += OnVented;
            }
            _setup.Run.LevelChanged += OnLevel;
            _setup.Upgrades.Selected += OnPicked;
            if (_setup.Warden != null)
                _setup.Warden.Slammed += OnSlammed;
            BurnStatus.Burned += OnBurned;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            _subscribed = false;
            BurnStatus.Burned -= OnBurned;
            if (_encounters != null)
            {
                _encounters.HordeEnemySpawned -= OnSpawned;
                _encounters.EncounterStarted -= OnWave;
                _encounters.EnemyDefeated -= OnDefeated;
                _encounters.ProgressChanged -= OnProgress;
            }
            if (_heroAttack != null)
                _heroAttack.Struck -= OnStruck;
            if (_ability != null)
            {
                _ability.Used -= OnAbility;
                _ability.Vented -= OnVented;
            }
            if (_setup != null && _setup.Run != null)
                _setup.Run.LevelChanged -= OnLevel;
            if (_setup != null && _setup.Upgrades != null)
                _setup.Upgrades.Selected -= OnPicked;
            if (_setup != null && _setup.Warden != null)
                _setup.Warden.Slammed -= OnSlammed;
        }

        private void Bump(float[] counts)
        {
            int frame = Time.frameCount - _base;
            if (_recording && frame >= 0 && frame < Capacity)
                counts[frame]++;
        }

        private void OnSpawned(int slot) => Bump(Spawns);
        private void OnWave() => Bump(Waves);
        private void OnDefeated(Health enemy) => Bump(Deaths);
        private void OnProgress() => Bump(Progress);
        private void OnStruck(Health target) => Bump(Strikes);
        private void OnAbility() => Bump(AbilityUses);
        private void OnVented(Health enemy, int stacks) => Bump(Vents);
        private void OnLevel() => Bump(LevelUps);
        private void OnPicked(UpgradeOption option, int slot) => Bump(Picks);
        private void OnSlammed(bool hit) => Bump(Slams);
        private void OnBurned(Health enemy, float damage) => Bump(BurnTicks);
    }
}
