using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Labels' glyphs put in their dynamic font's texture before the labels first show them (2026-11-11: the first choice
    // panel, the HUD's weapon line and level, the loadout row's first badge): every character a label can show, at every
    // size its best fit can choose, at its canvas's scale and in its style (Font.RequestCharactersInTexture). The font
    // grows its texture, renders the glyphs and uploads them at load instead of in the frame a label first appears.
    // Nothing is drawn and nothing changes on screen: a glyph already in the texture is the glyph the label would have put
    // there itself.
    //
    // A label is warmed in the first LateUpdate after it is added - after every Start, with the canvas scaler done - and
    // again only if its canvas's scale changes, for a while after load: a phone's never does; the perf probe's switch of
    // the canvases to its own target does, once. Then the component goes quiet.
    [AddComponentMenu("")]
    public sealed class TextPrewarm : MonoBehaviour
    {
        public const string Digits = "0123456789";
        // Frames after the last warm that a scale change is still looked for.
        private const int WatchFrames = 120;

        // The frame the last warm ran in, for the perf probe: a warm its own canvas switch caused is the probe's work.
        internal static int WarmedFrame = -1;

        private sealed class Entry
        {
            public Text Text;
            public string Characters;
            public Canvas Root;
            public float Scale = -1f;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private int _quietFrames;

        // Glyph sizes requested so far (a size a label and style could be drawn at, its characters all asked for).
        public int SizesRequested { get; private set; }
        public int LabelCount => _entries.Count;

        // The component on host, made if missing.
        internal static TextPrewarm On(GameObject host) =>
            host.TryGetComponent(out TextPrewarm prewarm) ? prewarm : host.AddComponent<TextPrewarm>();

        // The characters of every part, each once, in the order met; line breaks left out.
        internal static string Characters(params string[] parts)
        {
            var seen = new HashSet<char>();
            var characters = new StringBuilder();
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                foreach (char c in part)
                    if (c != '\n' && c != '\r' && seen.Add(c))
                        characters.Append(c);
            }
            return characters.ToString();
        }

        internal void Add(Text text, string characters)
        {
            if (text == null || string.IsNullOrEmpty(characters))
                return;
            _entries.Add(new Entry { Text = text, Characters = characters });
            _quietFrames = 0;
            enabled = true;
        }

        private void LateUpdate()
        {
            bool warmed = false;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry.Text == null)
                    continue;
                if (entry.Root == null)
                    entry.Root = RootOf(entry.Text);
                float scale = entry.Root != null ? entry.Root.scaleFactor : 1f;
                // A canvas not scaled yet is looked at again next frame.
                if (scale == entry.Scale || scale <= 0f)
                    continue;
                entry.Scale = scale;
                SizesRequested += Warm(entry.Text, entry.Characters, scale);
                warmed = true;
            }
            if (warmed)
            {
                WarmedFrame = Time.frameCount;
                _quietFrames = 0;
            }
            else if (++_quietFrames >= WatchFrames)
                enabled = false;
        }

        // The outermost canvas over the label, active or not: an inactive panel's labels have no canvas of their own yet.
        private static Canvas RootOf(Text text)
        {
            Canvas[] canvases = text.GetComponentsInParent<Canvas>(true);
            return canvases.Length > 0 ? canvases[canvases.Length - 1] : null;
        }

        // Every size the label's best fit may choose (its one size without), scaled to the canvas as uGUI scales a dynamic
        // font's size, both roundings where they differ. Returns the sizes requested.
        internal static int Warm(Text text, string characters, float scale)
        {
            Font font = text.font;
            if (font == null || !font.dynamic || string.IsNullOrEmpty(characters))
                return 0;
            int min = text.fontSize, max = text.fontSize;
            if (text.resizeTextForBestFit)
            {
                min = Mathf.Min(text.resizeTextMinSize, text.resizeTextMaxSize);
                max = Mathf.Max(text.resizeTextMinSize, text.resizeTextMaxSize);
            }
            int requested = 0, last = -1;
            for (int size = min; size <= max; size++)
            {
                int down = Mathf.Max(1, (int)(size * scale));
                int near = Mathf.Max(1, Mathf.RoundToInt(size * scale));
                if (down != last)
                {
                    font.RequestCharactersInTexture(characters, down, text.fontStyle);
                    requested++;
                }
                if (near != down)
                {
                    font.RequestCharactersInTexture(characters, near, text.fontStyle);
                    requested++;
                }
                last = near;
            }
            return requested;
        }
    }
}
