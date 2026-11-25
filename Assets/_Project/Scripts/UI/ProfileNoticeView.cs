using System;
using System.Collections.Generic;
using Cryptforge.Localisation;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The save's two notices to the player (docs/21, 2026-11-26), each one short line on a smoked plate at the top of the
    // screen, over every panel:
    //   - A newer build's save (ProfileLoadStatus.NewerVersion; the arena's store read only): "This save is from a newer
    //     version of the game..." - once a launch for the profile folder, since nothing is saved until the game is updated
    //     and a reload has nothing new to say.
    //   - A save that failed (a full phone): "Could not save your progress..." - once a scene: every change tries again
    //     and would say it again; the next scene, which tries again at once, says it again if that fails too.
    // Non-blocking: nothing of it catches a tap, so whatever stands under it answers as ever. ShowSeconds of real time
    // (the map choice, the result and the arena hold the run's time, never this), faded in and out; a second notice waits
    // for the first. Its words are the language's (docs/60) and follow a switch while it shows.
    // The look is SkillToastView's plate - the smoke (or the painted HUD's boss name plate), the ivory, the scene's font -
    // but not that view: the toast stands in the HUD, under the map choice's and the result's canvases, and only while a
    // run is live. So this one has its own overlay canvas inside the safe area, built the first time a notice shows; a
    // scene with nothing to say makes nothing.
    public sealed class ProfileNoticeView : MonoBehaviour
    {
        public const string CanvasName = "Profile Notice";
        // Over the map choice (MapChoiceView.SortingOrder, 100) and the arena (ArenaScreen.SortingOrder, 110), under the
        // floors' transitions.
        public const int SortingOrder = 120;
        public const float ShowSeconds = 6f;
        private const float FadeIn = .25f;
        private const float FadeOut = .5f;
        // The most real time one frame counts toward ShowSeconds (Step), as RunFeedbackView clamps its own steps.
        internal const float MaxStep = .1f;
        // On the canvas's 1080-wide reference: the plate's gap under the safe area's top, its width, its smallest and
        // tallest height, its padding; the line's size and its smallest best fit.
        internal const float Top = 24f;
        internal const float Width = 960f;
        internal const float MinHeight = 84f;
        internal const float MaxHeight = 240f;
        internal const float PadX = 30f;
        internal const float PadY = 14f;
        internal const int LineSize = 34;
        internal const int LineMinSize = 24;
        private static readonly Color Ivory = new Color(.933f, .910f, .851f, 1f);
        private static readonly Color Smoke = new Color(.07f, .06f, .09f, .82f);

        // The profile folder whose newer build's save this launch has told of.
        private static string _toldNewerVersionFor;

        private readonly List<Func<string>> _pending = new List<Func<string>>(2);
        private Func<string> _line;
        private bool _toldSaveFailed;
        private bool _showing;
        private float _elapsed;
        private bool _subscribed;
        private RectTransform _plate;
        private Image _plateImage;
        private Text _label;
        private CanvasGroup _group;

        public static string NewerVersionText => Loc.Text("save.newer_version", "This save is from a newer version of the game. Update the game to keep playing it - nothing is saved until then.");
        public static string SaveFailedText => Loc.Text("save.failed", "Could not save your progress. Free some space on the phone; the game will try again.");

        // For tests and captures: whether a notice shows and its line, how many this scene has shown, how many wait.
        public bool IsShowing => _showing;
        public string ShownLine { get; private set; } = string.Empty;
        public int ShownCount { get; private set; }
        public int PendingCount => _pending.Count;
        internal RectTransform Plate => _plate;
        internal Text Label => _label;

        // A newer build's save stands in this profile folder: told once a launch, by the host's scene (CombatSetup's object
        // carries the view, made the first time there is something to tell).
        public static void TellNewerVersion(Component host, string directory)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (string.Equals(_toldNewerVersionFor, directory, StringComparison.Ordinal))
                return;
            _toldNewerVersionFor = directory;
            On(host).Tell(() => NewerVersionText);
        }

        // A save could not be written: told once a scene.
        public static void TellSaveFailed(Component host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            ProfileNoticeView view = On(host);
            if (view._toldSaveFailed)
                return;
            view._toldSaveFailed = true;
            view.Tell(() => SaveFailedText);
        }

        private static ProfileNoticeView On(Component host)
        {
            var view = host.GetComponent<ProfileNoticeView>();
            return view != null ? view : host.gameObject.AddComponent<ProfileNoticeView>();
        }

        private void Tell(Func<string> line)
        {
            _pending.Add(line);
            if (!_showing)
                ShowNext();
        }

        private void ShowNext()
        {
            if (_pending.Count == 0 || !Build())
            {
                _pending.Clear();
                enabled = false;
                return;
            }
            _line = _pending[0];
            _pending.RemoveAt(0);
            TakePlate();
            _plate.SetAsLastSibling();
            _plate.gameObject.SetActive(true);
            _group.alpha = 0f;
            Draw();
            ShownCount++;
            _elapsed = 0f;
            _showing = true;
            enabled = true;
        }

        // The line in the language now, and the plate as tall as it wraps at its full size (a longer one shrinks by best
        // fit within the tallest).
        private void Draw()
        {
            ShownLine = _line();
            _label.text = ShownLine;
            TextGenerationSettings settings = _label.GetGenerationSettings(new Vector2(Width - 2f * PadX, 0f));
            settings.resizeTextForBestFit = false;
            settings.fontSize = LineSize;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float scale = _label.pixelsPerUnit > 0f ? _label.pixelsPerUnit : 1f;
            float height = _label.cachedTextGeneratorForLayout.GetPreferredHeight(ShownLine, settings) / scale;
            _plate.sizeDelta = new Vector2(Width, Mathf.Clamp(height + 2f * PadY, MinHeight, MaxHeight));
        }

        private void Update() => Step(Time.unscaledDeltaTime);

        // One frame of the notice's real time, at most MaxStep of it: a lock and unlock, or a trip out of the app, jumps the
        // unscaled clock, and the notice hid before it was read.
        internal void Step(float unscaledDeltaTime)
        {
            if (!_showing)
            {
                enabled = false;
                return;
            }
            _elapsed += Mathf.Min(unscaledDeltaTime, MaxStep);
            if (_elapsed >= ShowSeconds)
            {
                Hide();
                ShowNext();
                return;
            }
            _group.alpha = Mathf.Clamp01(Mathf.Min(_elapsed / FadeIn, (ShowSeconds - _elapsed) / FadeOut));
        }

        private void Hide()
        {
            _showing = false;
            if (_plate != null)
                _plate.gameObject.SetActive(false);
        }

        private void OnLanguageChanged()
        {
            if (_showing)
                Draw();
        }

        // The canvas, its safe area and the plate, built once; false when there is nothing to build them with.
        private bool Build()
        {
            if (_plate != null)
                return true;
            int layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            var canvasObject = new GameObject(CanvasName, typeof(RectTransform));
            canvasObject.layer = layer;
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var area = new GameObject(CanvasName + " Safe Area", typeof(RectTransform));
            area.layer = layer;
            var areaRect = (RectTransform)area.transform;
            areaRect.SetParent(canvasObject.transform, false);
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = Vector2.zero;
            areaRect.offsetMax = Vector2.zero;
            area.AddComponent<SafeAreaFitter>();

            var plate = new GameObject(CanvasName + " Plate", typeof(RectTransform));
            plate.layer = layer;
            _plate = (RectTransform)plate.transform;
            _plate.SetParent(areaRect, false);
            _plate.anchorMin = _plate.anchorMax = _plate.pivot = new Vector2(.5f, 1f);
            _plate.anchoredPosition = new Vector2(0f, -Top);
            _plate.sizeDelta = new Vector2(Width, MinHeight);
            _plateImage = plate.AddComponent<Image>();
            _plateImage.raycastTarget = false;
            _plateImage.color = Smoke;
            _group = plate.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var line = new GameObject(CanvasName + " Line", typeof(RectTransform));
            line.layer = layer;
            var lineRect = (RectTransform)line.transform;
            lineRect.SetParent(_plate, false);
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.one;
            lineRect.offsetMin = new Vector2(PadX, PadY);
            lineRect.offsetMax = new Vector2(-PadX, -PadY);
            _label = line.AddComponent<Text>();
            _label.raycastTarget = false;
            _label.font = SceneFont();
            _label.fontSize = LineSize;
            _label.color = Ivory;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = LineMinSize;
            _label.resizeTextMaxSize = LineSize;
            _label.supportRichText = false;
            plate.SetActive(false);

            Loc.Changed += OnLanguageChanged;
            _subscribed = true;
            return true;
        }

        // A scene label's font (docs/60 section 8: a label built in code copies a scene label's), or the built-in one.
        private Font SceneFont()
        {
            foreach (Text text in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text != _label && text.font != null)
                    return text.font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // The painted HUD's smoked band (the boss's name plate, as the skill toast wears it), or the flat smoke without it.
        private void TakePlate()
        {
            HudArtView hud = FindFirstObjectByType<HudArtView>();
            if (hud == null || !hud.UsesPaintedArt || hud.Art == null || hud.Art.BossNamePlate == null)
                return;
            _plateImage.sprite = hud.Art.BossNamePlate;
            _plateImage.type = Image.Type.Simple;
            _plateImage.color = Color.white;
        }

        private void OnDestroy()
        {
            if (_subscribed)
                Loc.Changed -= OnLanguageChanged;
            _subscribed = false;
        }

        // With domain reload disabled in the Editor, statics survive into the next Play session; a new Play session is a
        // new launch, which has told nothing yet.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewLaunch() => _toldNewerVersionFor = null;
    }
}
