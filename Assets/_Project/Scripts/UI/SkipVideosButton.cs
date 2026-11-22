using Cryptforge.Localisation;
using Cryptforge.Monetisation;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // "Skip reward videos" (docs/58 section 1; the lead's answer to 7.7 question 1: both places, low key): the one product
    // the store sells, as a small link under the result screen's offer and as an entry in the settings (the pause overlay,
    // beside the language toggle). A tap opens the store's purchase (MonetisationRuntime.BuySkip; with the offline fake it
    // is bought at once); the words say "owned" once it is, and plainly how a purchase that did not grant it ended. The
    // price comes from the store at SDK integration.
    //
    // Built in code like LanguageToggleView: on the forge's secondary plate in the settings, or as a bare line of text (a
    // link) on the result screen. The view that places it gates its input (SetInputEnabled: the same delay as its other
    // buttons); an owner's, or one with a purchase open, takes no tap.
    public sealed class SkipVideosButton : MonoBehaviour
    {
        public const string ObjectName = "Skip Videos Button";
        // The link's words: the result screen's quieter gold, under the offer.
        private static readonly Color LinkColour = new Color(.86f, .78f, .58f, .9f);

        private Button _button;
        private Text _label;
        private bool _inputEnabled = true;
        private bool _subscribed;

        public Button Button => _button;
        public string ShownText => _label != null ? _label.text : string.Empty;
        public Text Label => _label;

        // A button under parent, placed by the rect given (anchors, pivot, position and size as a RectTransform takes them),
        // its words in labelStyle's font and style at fontSize, best fit down to half of it. plate: the settings' painted
        // plate (LanguageToggleView's); otherwise a bare link whose words take the tap.
        public static SkipVideosButton Create(RectTransform parent, string name, Text labelStyle, int fontSize, bool plate,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.layer = parent.gameObject.layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            var view = root.AddComponent<SkipVideosButton>();
            view._button = root.GetComponent<Button>();
            var image = root.GetComponent<Image>();
            view._button.targetGraphic = image;

            var labelObject = new GameObject(name + " Label", typeof(RectTransform));
            labelObject.layer = root.layer;
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = plate ? new Vector2(24f, 10f) : new Vector2(8f, 2f);
            labelRect.offsetMax = plate ? new Vector2(-24f, -10f) : new Vector2(-8f, -2f);
            Text label = labelObject.AddComponent<Text>();
            label.font = labelStyle != null && labelStyle.font != null ? labelStyle.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = labelStyle != null ? labelStyle.fontStyle : FontStyle.Normal;
            label.color = plate ? labelStyle != null ? labelStyle.color : new Color(.93f, .94f, .96f, 1f) : LinkColour;
            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(14, fontSize / 2);
            label.resizeTextMaxSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            view._label = label;

            if (plate)
            {
                LanguageToggleView.PaintPlate(view._button);
            }
            else
            {
                // A clear plate catches the tap across the whole line; the words dim while it takes none.
                image.sprite = null;
                image.color = new Color(0f, 0f, 0f, 0f);
                view._button.transition = Selectable.Transition.ColorTint;
                view._button.targetGraphic = label;
            }

            view._button.onClick.AddListener(view.Tap);
            MonetisationRuntime.Changed += view.Refresh;
            Loc.Changed += view.Refresh;
            view._subscribed = true;
            view.Refresh();
            return view;
        }

        // The owning view's input gate: its delay after opening.
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            Refresh();
        }

        public void Refresh()
        {
            if (_label == null || _button == null)
                return;
            bool owned = MonetisationRuntime.OwnsSkip;
            bool purchasing = MonetisationRuntime.IsPurchasing;
            string words = RewardOfferText.Skip(owned, purchasing, MonetisationRuntime.LastPurchase);
            if (_label.text != words)
                _label.text = words;
            _button.interactable = _inputEnabled && !owned && !purchasing;
        }

        private void Tap()
        {
            if (_button == null || !_button.interactable)
                return;
            MonetisationRuntime.BuySkip();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Tap);
            if (!_subscribed)
                return;
            _subscribed = false;
            MonetisationRuntime.Changed -= Refresh;
            Loc.Changed -= Refresh;
        }
    }
}
