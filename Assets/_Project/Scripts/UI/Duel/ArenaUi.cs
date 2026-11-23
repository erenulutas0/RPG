using System;
using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The arena screen's parts, built in code as the Anvil builds its own (AnvilView): legacy uGUI on the built-in
    // LegacyRuntime font, rects placed from their parent's top-left on the 1080-wide canvas, the forge's painted plates on
    // the buttons where the scene has them (ScreenArtSet) and plain colours where it does not. The generated sprites (a
    // white square, a soft disc, a ring, a fade) and the rendered heroes' flash material are made once per screen and
    // destroyed with it.
    internal sealed class ArenaUi : IDisposable
    {
        public static readonly Color Gold = new Color32(238, 191, 108, 255);
        public static readonly Color Muted = new Color32(163, 167, 183, 255);
        public static readonly Color Bright = new Color32(240, 234, 220, 255);
        public static readonly Color Ally = new Color32(108, 176, 255, 255);
        public static readonly Color Foe = new Color32(240, 104, 88, 255);
        public static readonly Color Surface = new Color(.09f, .08f, .11f, .97f);
        public static readonly Color Card = new Color(.14f, .13f, .17f, 1f);

        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        private readonly ScreenArtSet _screens;

        public ArenaUi(Font font, ScreenArtSet screens)
        {
            Font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _screens = screens != null && screens.IsValid ? screens : null;
            White = MakeSprite("Arena White", 4, (x, y) => 1f);
            Disc = MakeSprite("Arena Disc", 64, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                return Mathf.Clamp01((1f - d) * 3f) * Mathf.Lerp(1f, .55f, d);
            });
            Ring = MakeSprite("Arena Ring", 128, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                return Mathf.Clamp01(1f - Mathf.Abs(d - .9f) * 14f);
            });
            Fade = MakeSprite("Arena Fade", 32, (x, y) => (y + 1f) * .5f);
        }

        public Font Font { get; }
        public Sprite White { get; }
        public Sprite Disc { get; }
        public Sprite Ring { get; }
        // Opaque along its top edge, clear along its bottom: a shade laid over an edge.
        public Sprite Fade { get; }
        public bool Painted => _screens != null;

        private Material _flash;
        private bool _flashLoaded;

        // The rendered heroes' hit flash in the lane: the run's silhouette shader (Resources/RenderedCharacterFlash, as
        // RenderedCharacterAnimator draws it), made the first time it is asked for. Null where the shader is missing or
        // unsupported; the figure then tints its frame instead.
        public Material FlashMaterial
        {
            get
            {
                if (_flashLoaded)
                    return _flash;
                _flashLoaded = true;
                var shader = Resources.Load<Shader>("RenderedCharacterFlash");
                if (shader != null && shader.isSupported)
                {
                    _flash = new Material(shader) { name = "Arena hero flash" };
                    _owned.Add(_flash);
                }
                return _flash;
            }
        }

        // A square sprite drawn by alpha(x, y) over -1..1 on both axes, white.
        private Sprite MakeSprite(string name, int size, Func<float, float, float> alpha)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f) / size * 2f - 1f;
                    float v = (y + .5f) / size * 2f - 1f;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha(u, v))));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
            sprite.name = name;
            _owned.Add(texture);
            _owned.Add(sprite);
            return sprite;
        }

        public void Dispose()
        {
            foreach (UnityEngine.Object owned in _owned)
            {
                if (owned != null)
                    UnityEngine.Object.Destroy(owned);
            }
            _owned.Clear();
        }

        public static RectTransform Node(Transform parent, string name)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.layer = parent.gameObject.layer;
            node.transform.SetParent(parent, false);
            return (RectTransform)node.transform;
        }

        public Image Part(Transform parent, string name, Color colour, Sprite sprite = null)
        {
            RectTransform rect = Node(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        public Text Label(Transform parent, string name, int size, TextAnchor alignment, Color colour, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = Node(parent, name);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontStyle = style;
            label.color = colour;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(14, size / 2);
            label.resizeTextMaxSize = size;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = true;
            label.raycastTarget = false;
            return label;
        }

        public Button Button(Transform parent, string name, int size, out Text label, Action click)
        {
            RectTransform rect = Node(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Paint(button, false);
            label = Label(rect, name + " Label", size, TextAnchor.MiddleCenter, Bright);
            Stretch(label.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -8f));
            if (click != null)
                button.onClick.AddListener(() => click());
            return button;
        }

        // The forge's painted plates: primary for a chosen or the main action, secondary otherwise; plain colours without
        // the art.
        public void Paint(Button button, bool primary)
        {
            var image = (Image)button.targetGraphic;
            if (_screens == null)
            {
                image.sprite = null;
                image.color = primary ? new Color(.46f, .34f, .16f, 1f) : new Color(.22f, .2f, .26f, 1f);
                button.transition = Selectable.Transition.ColorTint;
                return;
            }
            Sprite normal = primary ? _screens.Primary : _screens.Secondary;
            if (image.sprite == normal && button.transition == Selectable.Transition.SpriteSwap)
                return;
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                pressedSprite = primary ? _screens.PrimaryPressed : _screens.SecondaryPressed,
                disabledSprite = primary ? _screens.PrimaryDisabled : _screens.SecondaryDisabled
            };
        }

        // A rect at (x, y) from its parent's top-left, this wide and tall.
        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        // A rect centred on a point of its parent measured from the parent's top-left.
        public static void Centre(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static string Colour(string text, Color colour) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(colour) + ">" + text + "</color>";
    }
}
