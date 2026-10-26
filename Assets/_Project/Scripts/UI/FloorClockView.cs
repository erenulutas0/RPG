using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The floor's clock, said under the top panel (HordeClock; the owner, 2026-10-26: "Sürü bassın", five minutes a
    // floor). It counts down to the overrun in parchment, turns ember for the last LastMinuteSeconds with a beat on every
    // second, and once the floor is overrun counts up in red - "OVERRUN +0:12" - beating harder, so the player knows the
    // horde is growing and why. Shown only while a horde runs: hidden in the finale, on the walk up after the gate and on
    // floors without one. When the gate boss stands, its name and bar take the band under the panel and the clock steps
    // below them. RunHud adds it; it builds its label in code on the HUD's safe area, so the scene does not change.
    public sealed class FloorClockView : MonoBehaviour
    {
        public const float LastMinuteSeconds = 60f;
        // On the 1080-wide canvas, centred, 64 tall, from the safe area's top: under the top surface (to 252), or under
        // the boss readout (name 265-307, bar 313-327) while the gate boss stands.
        public const float Top = 262f;
        public const float TopUnderBoss = 338f;
        private const float BeatSeconds = .35f;
        private const float WarningGrowth = .08f;
        private const float OverrunGrowth = .16f;
        private static readonly Color Parchment = new Color(.96f, .9f, .78f, 1f);
        private static readonly Color Ember = new Color(1f, .55f, .22f, 1f);
        private static readonly Color Overrun = new Color(1f, .27f, .2f, 1f);
        private static readonly Color Flare = new Color(1f, .86f, .62f, 1f);
        private CombatSetup _setup;
        private EncounterController _encounters;
        private PrototypeTextDefinition _text;
        private Text _label;
        private Outline _outline;
        // The whole second the label says, and in which mode; the string is rebuilt only when either moves.
        private int _shownSecond = -1;
        private bool _shownOverrun;
        private bool _underBoss;
        private float _beatStart = -1f;

        // For tests and for anything that wants to read what the player is being shown.
        public bool IsShown => _label != null && _label.gameObject.activeSelf;
        public string ShownText => _label != null ? _label.text : string.Empty;
        public bool IsOverrunShown => IsShown && _shownOverrun;
        public bool IsUnderBoss => _underBoss;
        public Color ShownColor => _label != null ? _label.color : Color.clear;

        // style is the compact gold count: the clock shares its font and layer, and sits on the same safe area.
        internal void Initialize(CombatSetup setup, EncounterController encounters, PrototypeTextDefinition text, Text style)
        {
            _setup = setup; _encounters = encounters; _text = text;
            var labelObject = new GameObject("Floor Clock", typeof(RectTransform));
            labelObject.layer = style.gameObject.layer;
            var rect = (RectTransform)labelObject.transform;
            rect.SetParent(style.rectTransform.parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -Top);
            rect.sizeDelta = new Vector2(420f, 64f);
            _label = labelObject.AddComponent<Text>();
            _label.font = style.font;
            _label.fontSize = 44;
            _label.fontStyle = FontStyle.Bold;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 28;
            _label.resizeTextMaxSize = 44;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.raycastTarget = false;
            _label.color = Parchment;
            // The clock stands on the floor's art, not on a panel: a dark rim keeps it legible over lava and stone.
            _outline = labelObject.AddComponent<Outline>();
            _outline.effectColor = new Color(0f, 0f, 0f, .85f);
            _outline.effectDistance = new Vector2(2f, -2f);
            labelObject.SetActive(false);
            Refresh();
        }

        private void Update()
        {
            if (_label == null)
                return;
            Refresh();
            Beat();
        }

        private void Refresh()
        {
            bool shown = _encounters != null && _encounters.IsHordeActive && _setup.Run != null && !_setup.Run.HasEnded;
            if (_label.gameObject.activeSelf != shown)
                _label.gameObject.SetActive(shown);
            if (!shown)
            {
                _shownSecond = -1;
                return;
            }

            bool underBoss = _encounters.GateBoss != null;
            if (underBoss != _underBoss)
            {
                _underBoss = underBoss;
                _label.rectTransform.anchoredPosition = new Vector2(0f, -(underBoss ? TopUnderBoss : Top));
            }

            float clock = _encounters.FloorClockSeconds;
            bool overrun = HordeClock.IsOvertime(clock);
            int second = HordeClock.ShownSecond(clock);
            if (second == _shownSecond && overrun == _shownOverrun)
                return;
            bool lastMinute = !overrun && HordeClock.Remaining(clock) <= LastMinuteSeconds;
            // A beat on each new second of the last minute and of the overrun, not on the label's first showing.
            if (_shownSecond >= 0 && (overrun || lastMinute))
                _beatStart = Time.unscaledTime;
            _shownSecond = second;
            _shownOverrun = overrun;
            _label.text = string.Format(overrun ? _text.OverrunFormat : _text.FloorClockFormat, second / 60, second % 60);
            if (_beatStart < 0f)
                _label.color = Rest(overrun, lastMinute);
        }

        private Color Rest(bool overrun, bool lastMinute) => overrun ? Overrun : lastMinute ? Ember : Parchment;

        // Unscaled time, so the beat finishes behind an open choice instead of freezing half-swollen.
        private void Beat()
        {
            if (_beatStart < 0f)
                return;
            bool lastMinute = !_shownOverrun && HordeClock.Remaining(_encounters.FloorClockSeconds) <= LastMinuteSeconds;
            float t = (Time.unscaledTime - _beatStart) / BeatSeconds;
            if (t >= 1f)
            {
                _beatStart = -1f;
                _label.rectTransform.localScale = Vector3.one;
                _label.color = Rest(_shownOverrun, lastMinute);
                return;
            }
            float swell = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float scale = 1f + (_shownOverrun ? OverrunGrowth : WarningGrowth) * swell;
            _label.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _label.color = Color.Lerp(Rest(_shownOverrun, lastMinute), Flare, swell * (_shownOverrun ? .6f : .4f));
        }
    }
}
