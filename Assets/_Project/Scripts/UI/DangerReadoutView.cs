using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The run's difficulty, said on the top panel. Every evolution and every altar makes the enemies last longer and hit
    // harder, and without this the only sign of it was a fight going worse. Hidden at the authored 1.0, it reads
    // "Danger +N%" in ember beside the upgrade badges and swells once each time the number climbs - after any open card
    // offer closes, so the swell is not spent behind the choice. RunHud adds it; it builds its label in code on the HUD's
    // safe area, so the scene does not change.
    public sealed class DangerReadoutView : MonoBehaviour
    {
        public const float PulseSeconds = .6f;
        private const float PulseGrowth = .22f;
        private static readonly Color Ember = new Color(1f, .55f, .22f, 1f);
        private static readonly Color Flare = new Color(1f, .86f, .62f, 1f);
        private CombatSetup _setup;
        private PrototypeTextDefinition _text;
        private Text _label;
        private int _shownPercent;
        private bool _pulsePending;
        private bool _pulsing;
        private float _pulseStart;

        // For tests and for anything that wants to read what the player is being shown.
        public bool IsShown => _label != null && _label.gameObject.activeSelf;
        public string ShownText => _label != null ? _label.text : string.Empty;
        public int ShownPercent => _shownPercent;
        public bool IsPulsePending => _pulsePending;
        public bool IsPulsing => _pulsing;
        public float ShownScale => _label != null ? _label.rectTransform.localScale.x : 1f;

        // The difficulty rounded to the whole percent the readout shows; never negative, so 1.0 and float noise read 0.
        public static int PercentOf(float difficulty) => Mathf.Max(0, Mathf.RoundToInt((difficulty - 1f) * 100f));

        // style is the compact gold count: the readout shares its parent, font and layer.
        internal void Initialize(CombatSetup setup, PrototypeTextDefinition text, Text style)
        {
            _setup = setup; _text = text;
            var labelObject = new GameObject("Danger Readout", typeof(RectTransform));
            labelObject.layer = style.gameObject.layer;
            var rect = (RectTransform)labelObject.transform;
            rect.SetParent(style.rectTransform.parent, false);
            // Right-anchored like the pause button, whose right edge it shares. On the 1080-wide canvas it spans x 752-1040
            // and y 158-230 down from the safe area's top: below the gold (to 106) and the pause button (to 134), right of
            // the relic badge (to 734) and inside the top surface (to 252). The pivot on its right edge makes it swell
            // leftward, away from the screen edge.
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, .5f);
            rect.anchoredPosition = new Vector2(-40f, -194f);
            rect.sizeDelta = new Vector2(288f, 72f);
            _label = labelObject.AddComponent<Text>();
            _label.font = style.font;
            _label.fontSize = 34;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 24;
            _label.resizeTextMaxSize = 34;
            _label.alignment = TextAnchor.MiddleRight;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.raycastTarget = false;
            _label.color = Ember;
            labelObject.SetActive(false);
            _setup.HeroStats.Changed += Refresh;
            Refresh();
        }

        // On the stats' change event only; the string is rebuilt when the shown percent moves, never per frame.
        private void Refresh()
        {
            int percent = PercentOf(_setup.HeroStats.Difficulty);
            if (percent == _shownPercent)
                return;
            if (percent > _shownPercent)
                _pulsePending = true;
            _shownPercent = percent;
            _label.text = string.Format(_text.DangerFormat, percent);
            _label.gameObject.SetActive(percent > 0);
        }

        // Unscaled, because the rise usually lands while a card offer has the game paused.
        private void Update()
        {
            if (_pulsePending && !_setup.Choices.IsOpen)
            {
                _pulsePending = false;
                _pulsing = true;
                _pulseStart = Time.unscaledTime;
            }
            if (!_pulsing)
                return;
            float t = (Time.unscaledTime - _pulseStart) / PulseSeconds;
            if (t >= 1f)
            {
                _pulsing = false;
                _label.rectTransform.localScale = Vector3.one;
                _label.color = Ember;
                return;
            }
            float swell = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float scale = 1f + PulseGrowth * swell;
            _label.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _label.color = Color.Lerp(Ember, Flare, swell);
        }

        private void OnDestroy()
        {
            if (_setup != null && _setup.HeroStats != null)
                _setup.HeroStats.Changed -= Refresh;
        }
    }
}
