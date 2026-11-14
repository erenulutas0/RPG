using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cryptforge.Art;
using Cryptforge.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The level-up panel's Auto-pick, while a deeper start's catch-up level-ups are queued (docs/53): a wide button under
    // the cards, in the panel's painted look (the checkpoint's Climb frame; plain colours without the set), that takes every
    // remaining level-up as the careful autopilot would (CombatSetup.AutoPickCatchUp, seeded by the run's offers) and then
    // says what it took in one line over the run for a few seconds. The cards stay tappable beside it. CombatSetup adds it
    // only to a deeper start; the run's own level-ups never show it, and it switches itself off once the catch-up is over.
    public sealed class CatchUpAutoPickView : MonoBehaviour
    {
        public const string ButtonName = "Catch-up Auto-pick";
        public const string SummaryName = "Catch-up Summary";
        // As the panel's cards: a tap that lands as an offer appears does not act.
        public const float InputDelay = .3f;
        public const float SummarySeconds = 4f;
        private const float ButtonWidth = 720f;
        private const float ButtonHeight = 150f;
        private const float ButtonBottom = 150f;

        private CombatSetup _setup;
        private RunChoiceView _choices;
        private GameObject _button;
        private Text _label;
        private int _shownPending = -1;
        private float _readyAt;
        private GameObject _summaryCanvas;
        private float _summaryUntil;
        private bool _done;

        // The button is on show, and what the last auto-pick said (empty before one).
        public bool IsShown => _button != null && _button.activeInHierarchy;
        public Button Button => _button != null ? _button.GetComponent<Button>() : null;
        public string Summary { get; private set; } = string.Empty;

        public void Initialize(CombatSetup setup) => _setup = setup;

        private void Update()
        {
            if (_setup == null)
                return;
            if (_summaryCanvas != null && Time.unscaledTime >= _summaryUntil)
                Destroy(_summaryCanvas);
            if (_done)
            {
                if (_summaryCanvas == null)
                    enabled = false;
                return;
            }
            bool catching = _setup.IsCatchingUp;
            if (catching && _button == null)
                Build();
            if (_button == null)
            {
                // No offer to stand on (no level-up left to offer, or no panel): nothing to show, ever.
                _done = !catching;
                return;
            }
            if (_button.activeSelf != catching)
                _button.SetActive(catching);
            if (!catching)
            {
                // The last level-up was picked by hand: the catch-up is over.
                _done = _setup.Run.PendingUpgrades <= 0 || !_setup.Choices.IsOpen;
                return;
            }
            int pending = _setup.Run.PendingUpgrades;
            if (pending != _shownPending)
            {
                // A new offer: the count, and the tap delay again.
                _shownPending = pending;
                _label.text = string.Format(CultureInfo.InvariantCulture, "Auto-pick {0} level-ups", pending);
                _readyAt = Time.unscaledTime + InputDelay;
            }
        }

        private void Build()
        {
            _choices = FindFirstObjectByType<RunChoiceView>(FindObjectsInactive.Include);
            GameObject panel = _choices != null ? _choices.Panel : null;
            if (panel == null)
                return;
            ChoiceCardArtSet art = _choices.CardArt != null && _choices.CardArt.IsValid ? _choices.CardArt : null;
            Text source = _choices.TitleLabel;

            _button = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            _button.layer = panel.layer;
            var rect = (RectTransform)_button.transform;
            rect.SetParent(panel.transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, ButtonBottom);
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            var image = _button.GetComponent<Image>();
            if (art != null && art.Climb != null && art.Climb.Frame != null)
            {
                image.sprite = art.Climb.Frame;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(.32f, .24f, .14f, 1f);
            }
            var button = _button.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnAutoPick);

            var labelObject = new GameObject("Catch-up Auto-pick Label", typeof(RectTransform));
            labelObject.layer = panel.layer;
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(28f, 14f);
            labelRect.offsetMax = new Vector2(-28f, -14f);
            _label = Label(labelObject, source, 46, TextAnchor.MiddleCenter);
            _readyAt = Time.unscaledTime + InputDelay;
        }

        public void OnAutoPick()
        {
            if (_done || _setup == null || !_setup.IsCatchingUp || Time.unscaledTime < _readyAt)
                return;
            _done = true;
            int levels = _setup.Run.PendingUpgrades;
            List<string> taken = _setup.AutoPickCatchUp();
            if (_button != null)
                _button.SetActive(false);
            Summary = SummaryOf(levels, taken);
            ShowSummary();
        }

        // "Auto-picked 44 level-ups: Sword x8, Force x8, ..." - each card once, in the order first taken, with how often.
        public static string SummaryOf(int levels, IReadOnlyList<string> taken)
        {
            var order = new List<string>();
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < taken.Count; i++)
            {
                if (!counts.ContainsKey(taken[i]))
                {
                    counts[taken[i]] = 0;
                    order.Add(taken[i]);
                }
                counts[taken[i]]++;
            }
            var line = new StringBuilder();
            line.Append(string.Format(CultureInfo.InvariantCulture, "Auto-picked {0} level-ups", taken.Count));
            if (taken.Count < levels)
                line.Append(string.Format(CultureInfo.InvariantCulture, " of {0}", levels));
            for (int i = 0; i < order.Count; i++)
            {
                line.Append(i == 0 ? ": " : ", ").Append(order[i]);
                if (counts[order[i]] > 1)
                    line.Append(string.Format(CultureInfo.InvariantCulture, " x{0}", counts[order[i]]));
            }
            return line.ToString();
        }

        // The line over the run, at the top, for a few seconds; it takes no taps.
        private void ShowSummary()
        {
            _summaryCanvas = new GameObject(SummaryName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _summaryCanvas.transform.SetParent(transform, false);
            var canvas = _summaryCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = MapChoiceView.SortingOrder - 10;
            var scaler = _summaryCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            var safe = new GameObject("Catch-up Summary Safe Area", typeof(RectTransform));
            safe.transform.SetParent(_summaryCanvas.transform, false);
            var safeRect = (RectTransform)safe.transform;
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
            safe.AddComponent<SafeAreaFitter>();

            var plate = new GameObject("Catch-up Summary Plate", typeof(RectTransform), typeof(Image));
            var plateRect = (RectTransform)plate.transform;
            plateRect.SetParent(safe.transform, false);
            plateRect.anchorMin = new Vector2(0f, 1f);
            plateRect.anchorMax = new Vector2(1f, 1f);
            plateRect.pivot = new Vector2(.5f, 1f);
            plateRect.offsetMin = new Vector2(40f, -420f);
            plateRect.offsetMax = new Vector2(-40f, -200f);
            var plateImage = plate.GetComponent<Image>();
            plateImage.color = new Color(.05f, .04f, .07f, .85f);
            plateImage.raycastTarget = false;

            var labelObject = new GameObject("Catch-up Summary Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(plateRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 16f);
            labelRect.offsetMax = new Vector2(-24f, -16f);
            Text label = Label(labelObject, _choices != null ? _choices.FirstDescriptionLabel ?? _choices.TitleLabel : null, 34,
                TextAnchor.MiddleCenter);
            label.color = new Color(.96f, .9f, .76f, 1f);
            label.text = Summary;
            _summaryUntil = Time.unscaledTime + SummarySeconds;
        }

        private static Text Label(GameObject host, Text source, int size, TextAnchor alignment)
        {
            var label = host.AddComponent<Text>();
            label.font = source != null ? source.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = source != null ? source.fontStyle : FontStyle.Normal;
            label.color = source != null ? source.color : new Color(.95f, .9f, .8f, 1f);
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(14, size / 2);
            label.resizeTextMaxSize = size;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        private void OnDestroy()
        {
            if (_button != null)
                Button.onClick.RemoveListener(OnAutoPick);
        }
    }
}
