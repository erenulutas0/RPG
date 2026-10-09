using System.Collections;
using System.Collections.Generic;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Opt-in development-foundry entry, before EncounterController begins its first wave.
    // No proximity trigger, save mutation or global time-scale ownership.
    public sealed class GateArrivalView : MonoBehaviour
    {
        private CombatSetup _setup;
        private HeroMovementInput _movement;
        private bool _movementEnabled;
        private Camera _camera;
        private ArenaCameraFollow _follow;
        private bool _followEnabled;
        private Vector3 _position;
        private float _size;
        private readonly List<Canvas> _canvases = new List<Canvas>();
        private readonly List<bool> _canvasStates = new List<bool>();
        private bool _captured, _skip, _finished, _viewRestored, _interrupted, _ownsMovement;
        private UnityEngine.EventSystems.EventSystem _events;
        private bool _eventsEnabled;
        private GUIStyle _hintStyle;
        public bool IsPresenting { get; private set; }
        public void Initialize(CombatSetup setup, HeroMovementInput movement)
        {
            _setup = setup; _movement = movement;
            if (_movement != null) { _movementEnabled = _movement.enabled; _ownsMovement = true; _movement.Release(); _movement.enabled = false; }
            IsPresenting = true;
        }
        public void Skip() => _skip = true;
        private IEnumerator Start()
        {
            // An interrupted component may be re-enabled before its first Start. Only its
            // cancellation handback owns completion in that case; never start a second reveal.
            if (_finished || _interrupted) yield break;
            // Other scene Start methods finish laying out the HUD before we capture its state.
            yield return null;
            if (_finished || _interrupted) yield break;
            _camera = Camera.main;
            _follow = _camera != null ? _camera.GetComponent<ArenaCameraFollow>() : null;
            if (_setup == null || _camera == null || _follow == null) { Finish(); yield break; }
            _position = _camera.transform.position; _size = _camera.orthographicSize;
            _followEnabled = _follow.enabled;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.isRootCanvas) { _canvases.Add(canvas); _canvasStates.Add(canvas.enabled); canvas.enabled = false; }
            _events = UnityEngine.EventSystems.EventSystem.current;
            if (_events != null) { _eventsEnabled = _events.enabled; _events.enabled = false; }
            _captured = true; _follow.enabled = false;
            // The frame before capture may contain expensive scene construction. Its
            // unscaledDeltaTime is not time spent presenting this view: start our clock here.
            double began = Time.realtimeSinceStartupAsDouble;
            while (!_skip && !_setup.Run.HasEnded)
            {
                double elapsedTime = Time.realtimeSinceStartupAsDouble - began;
                if (elapsedTime >= 4d) break;
                float elapsed = (float)elapsedTime;
                float blend = elapsed < 1f ? Mathf.SmoothStep(0, 1, elapsed) :
                    elapsed < 3f ? 1f : 1f - Mathf.SmoothStep(0, 1, elapsed - 3f);
                _camera.transform.position = Vector3.Lerp(_position, new Vector3(19, 16.4f, _position.z), blend);
                float revealSize = 17.6f / (2f * Mathf.Max(.1f, _camera.aspect));
                _camera.orthographicSize = Mathf.Lerp(_size, revealSize, blend);
                yield return null;
            }
            RestoreView();
            // Consume the skip gesture through release before movement and ability UI resume.
            // Canvases remain hidden during this short handback so a held tap cannot activate a button.
            while (GestureHeld()) yield return null;
            Finish();
        }
        private void Update()
        {
            if (IsPresenting && (Input.touchCount > 0 || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) Skip();
        }
        private void RestoreView()
        {
            if (!_captured || _viewRestored) return;
            _viewRestored = true;
            if (_camera != null) { _camera.transform.position = _position; _camera.orthographicSize = _size; }
            if (_follow != null)
            {
                _follow.enabled = _followEnabled;
                // Refit through the live follow next frame for changed safe areas/aspect ratios.
            }
        }
        private void RestoreAll()
        {
            RestoreView();
            for (int i = 0; i < _canvases.Count; i++) if (_canvases[i] != null) _canvases[i].enabled = _canvasStates[i];
            _canvases.Clear(); _canvasStates.Clear(); _captured = false;
            if (_events != null) { _events.enabled = _eventsEnabled; _events = null; }
            if (_ownsMovement && _movement != null) { _movement.Release(); _movement.enabled = _movementEnabled; }
            _ownsMovement = false;
            IsPresenting = false;
        }
        private void Finish()
        {
            if (_finished) return;
            _finished = true; RestoreAll();
            if (_setup != null && _setup.Run != null && !_setup.Run.HasEnded) _setup.Encounters.BeginFirstEncounter();
        }
        private void OnApplicationPause(bool paused) { if (paused) Skip(); }
        private void OnApplicationFocus(bool focused) { if (!focused) Skip(); }
        private static bool GestureHeld() => Input.touchCount > 0 || Input.GetMouseButton(0) ||
            Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space);
        private void OnDisable()
        {
            if (_finished) return;
            StopAllCoroutines(); _interrupted = true; RestoreAll();
        }
        private void OnEnable()
        {
            if (_interrupted && !_finished && _setup != null) StartCoroutine(CompleteInterrupted());
        }
        private IEnumerator CompleteInterrupted()
        {
            // A disabled/unloading view never spawns; an explicitly re-enabled view hands back once.
            yield return null;
            while (GestureHeld()) yield return null;
            Finish();
        }
        private void OnGUI()
        {
            if (!IsPresenting || !_captured) return;
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                _hintStyle.normal.textColor = new Color(.88f,.77f,.58f);
            }
            _hintStyle.fontSize = Mathf.RoundToInt(18 * Screen.width / 540f);
            GUI.Label(new Rect(0, Screen.height * .86f, Screen.width, Screen.height * .06f), "Tap to continue", _hintStyle);
        }
    }
}
