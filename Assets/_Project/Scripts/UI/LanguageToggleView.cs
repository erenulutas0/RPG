using Cryptforge.Art;
using Cryptforge.Localisation;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The language setting (docs/59 decision 1, docs/60): one button that names the language the game speaks in that
    // language's own words - "Language: English", "Dil: Türkçe" - and a tap switches to the other one and stores the choice
    // apart from the save (LocalisationRuntime.Choose). Open views write their words again at once (Loc.Changed); what a
    // scene made in the old language follows on the next load (the next run).
    //
    // Built in code like the hero chooser, on the forge's secondary painted plate (ScreenArtSet; a plain colour without the
    // set), its words in the style of a label it is given. PauseView places one under Resume, and MapChoiceView one in its
    // title's band at the game's start (docs/60 pass 2).
    public sealed class LanguageToggleView : MonoBehaviour
    {
        public const string ObjectName = "Language Toggle";

        private Button _button;
        private Text _label;

        public Button Button => _button;
        public string ShownText => _label != null ? _label.text : string.Empty;

        // "Language: English" in English, "Dil: Türkçe" in Turkish.
        public static string Caption() =>
            Loc.Format("settings.language", "Language: {language}", ("language", NativeName(Loc.Language)));

        // Each language's own name for itself, the same whichever language the game speaks.
        public static string NativeName(Language language) => language == Language.Turkish
            ? Loc.Text("settings.language_turkish", "Türkçe")
            : Loc.Text("settings.language_english", "English");

        // A toggle under parent, placed by the rect given (anchors, pivot, position and size as a RectTransform takes them),
        // its words in labelStyle's font, style and colour at fontSize, best fit down to half of it.
        public static LanguageToggleView Create(RectTransform parent, Text labelStyle, int fontSize, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var root = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.layer = parent.gameObject.layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            var view = root.AddComponent<LanguageToggleView>();
            view._button = root.GetComponent<Button>();
            view._button.targetGraphic = root.GetComponent<Image>();
            PaintPlate(view._button);

            var labelObject = new GameObject(ObjectName + " Label", typeof(RectTransform));
            labelObject.layer = root.layer;
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 10f);
            labelRect.offsetMax = new Vector2(-24f, -10f);
            Text label = labelObject.AddComponent<Text>();
            label.font = labelStyle != null && labelStyle.font != null ? labelStyle.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = labelStyle != null ? labelStyle.fontStyle : FontStyle.Normal;
            label.color = labelStyle != null ? labelStyle.color : new Color(.93f, .94f, .96f, 1f);
            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(14, fontSize / 2);
            label.resizeTextMaxSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            view._label = label;
            LocalisedLabel.Bind(label, Caption);
            view._button.onClick.AddListener(view.Toggle);
            return view;
        }

        // The other language, stored as the player's choice.
        public void Toggle()
        {
            if (_button != null && !_button.interactable)
                return;
            LocalisationRuntime.Choose(Loc.Language == Language.Turkish ? Language.English : Language.Turkish);
        }

        // The forge's secondary plate when the painted screens are on, else a plain dark plate. The map choice's Arena
        // button, the toggle's mirror in the title's band, stands on it too.
        internal static void PaintPlate(Button button)
        {
            var image = (Image)button.targetGraphic;
            var screens = FindFirstObjectByType<ScreenArtView>(FindObjectsInactive.Include);
            ScreenArtSet art = screens != null && screens.Art != null && screens.Art.IsValid ? screens.Art : null;
            if (art == null)
            {
                image.sprite = null;
                image.color = new Color(.22f, .2f, .26f, .94f);
                return;
            }
            image.sprite = art.Secondary;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = art.SecondaryPressed, disabledSprite = art.SecondaryDisabled };
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Toggle);
        }
    }
}
