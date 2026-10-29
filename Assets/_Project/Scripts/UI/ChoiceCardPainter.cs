using Cryptforge.Art;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // RunChoiceView's panel and cards painted (ChoiceCardArtSet, Tools/ArtReview/build_choice_card_art.py). The panel is
    // restyled once: the dim behind it, the surface's frame and a plate with its crest behind the title. A card is painted
    // each time it is shown, in the treatment of what it offers: its own image becomes the treatment's frame, and parts
    // are laid round the scene's own labels and icon - an aura and a crest past the card's edges, a name plate behind the
    // name, a recess behind the description, a well behind the icon, the well's sigil while the card has no icon, a lip
    // over the icon's edge - each fitted to the rect it frames as LayOut left it, so a shortened card's parts shorten
    // with it. The press lies over the frame as the button's graphic: clear at rest, the treatment's light under a finger,
    // a shade while the card waits out its delay. No part is a raycast target, and every rect, label, icon, click and
    // interactable state the view and the tests read stays as it was. A card cloned from a painted one finds its parts
    // again by name.
    internal sealed class ChoiceCardPainter
    {
        internal const string AuraName = "Painted Aura";
        internal const string CrestName = "Painted Crest";
        internal const string PressName = "Painted Press";
        internal const string NamePlateName = "Painted Name Plate";
        internal const string DescriptionName = "Painted Description";
        internal const string WellName = "Painted Well";
        internal const string SigilName = "Painted Sigil";
        internal const string LipName = "Painted Well Lip";
        internal const string TitlePlateName = "Painted Title Plate";
        internal const string TitleCrestName = "Painted Title Crest";

        // How long a glow takes to breathe in and out, in seconds, and how far it dims at its lowest.
        private const float BreathPeriod = 2.4f;
        private const float BreathDepth = .16f;
        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        private readonly ChoiceCardArtSet _art;
        // Each slot's aura and crest, while its card wears them: they breathe while the panel is open.
        private Image[] _glows = new Image[2 * 3];

        internal ChoiceCardPainter(ChoiceCardArtSet art, Image backdrop, Image surface, Text title)
        {
            _art = art;
            // The dim keeps the panel's own image, so it still stops a tap from reaching the run under it.
            if (backdrop != null)
            {
                backdrop.sprite = art.Backdrop;
                backdrop.type = Image.Type.Simple;
                backdrop.color = Color.white;
            }
            if (surface != null)
                Restyle(surface, art.Surface);
            if (title != null)
            {
                RectTransform label = title.rectTransform;
                Image plate = Made(label.parent, TitlePlateName, art.TitlePlate, Image.Type.Sliced);
                PlaceBefore(plate.transform, label);
                Fit(plate.rectTransform, label, art.TitlePlateMargin);
                Image crest = Made(plate.transform, TitleCrestName, art.TitleCrest, Image.Type.Simple);
                RectTransform rect = crest.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0f, art.TitleRuleHeight);
                rect.sizeDelta = NativeSize(art.TitleCrest);
            }
        }

        // The card in the given slot, in the given treatment.
        internal void Paint(int slot, Button card, Text name, Text description, Image icon,
            ChoiceCardArtSet.Treatment look)
        {
            var frame = card.GetComponent<Image>();
            if (frame == null || look == null)
                return;
            Restyle(frame, look.Frame);
            var rect = (RectTransform)card.transform;
            if (_glows.Length < 2 * (slot + 1))
                System.Array.Resize(ref _glows, 2 * (slot + 1));

            Image aura = Find(rect, AuraName, out bool made);
            if (made)
                aura.transform.SetSiblingIndex(0);
            Show(aura, look.Aura, Image.Type.Sliced);
            _glows[2 * slot] = look.Aura != null ? aura : null;
            if (look.Aura != null)
            {
                Vector4 o = look.AuraOutset;
                Stretch(aura.rectTransform, new Vector2(-o.x, -o.y), new Vector2(o.z, o.w));
            }
            Image crest = Find(rect, CrestName, out made);
            if (made)
                crest.transform.SetSiblingIndex(1);
            Show(crest, look.Crest, Image.Type.Simple);
            _glows[2 * slot + 1] = look.Crest != null ? crest : null;
            if (look.Crest != null)
            {
                // Its pivot on the middle of the card's top edge, at its painted size.
                RectTransform c = crest.rectTransform;
                c.anchorMin = c.anchorMax = new Vector2(.5f, 1f);
                c.pivot = new Vector2(look.Crest.pivot.x / look.Crest.rect.width, look.Crest.pivot.y / look.Crest.rect.height);
                c.anchoredPosition = Vector2.zero;
                c.sizeDelta = NativeSize(look.Crest);
            }
            Image press = Find(rect, PressName, out made);
            if (made)
                press.transform.SetSiblingIndex(2);
            Show(press, _art.Press, Image.Type.Sliced);
            Stretch(press.rectTransform, Vector2.zero, Vector2.zero);
            UsePress(card, frame, press, look.PressColour);

            if (name != null)
                Behind(name.rectTransform, NamePlateName, look.NamePlate, _art.NamePlateMargin);
            if (description != null)
                Behind(description.rectTransform, DescriptionName, _art.Description, _art.DescriptionMargin);
            if (icon != null)
            {
                RectTransform target = icon.rectTransform;
                float m = _art.WellMargin;
                Behind(target, WellName, look.Well, new Vector4(m, m, m, m));
                // The sigil only while the icon has nothing to show; between the well and the icon either way.
                Image sigil = Behind(target, SigilName, look.Sigil, Vector4.zero, Image.Type.Simple);
                float inset = -target.rect.width * (1f - _art.SigilScale) / 2f;
                Fit(sigil.rectTransform, target, new Vector4(inset, inset, inset, inset));
                sigil.preserveAspect = true;
                sigil.enabled = !(icon.enabled && icon.sprite != null && icon.gameObject.activeSelf);
                Image lip = Find(target.parent, LipName, out made);
                if (made)
                    PlaceAfter(lip.transform, target);
                Show(lip, _art.WellLip, Image.Type.Sliced);
                Fit(lip.rectTransform, target, Vector4.zero);
            }
        }

        // While the panel is open: the shown cards' auras and crests breathe, each card a little after the last. Only
        // their renderers' alpha moves, so no mesh is rebuilt.
        internal void Tick(float time)
        {
            for (int i = 0; i < _glows.Length; i++)
            {
                if (_glows[i] == null)
                    continue;
                float phase = (time / BreathPeriod + (i / 2) * .23f) * 2f * Mathf.PI;
                _glows[i].canvasRenderer.SetAlpha(1f - BreathDepth * (.5f + .5f * Mathf.Sin(phase)));
            }
        }

        // The press is the button's graphic now: clear at rest, highlighted or selected, lit in the treatment's colour
        // under a finger, the set's shade while the card cannot be tapped. The graphic is swapped before the colours (each
        // setter fades the graphic it finds), then both are set at once: the press to the state the card is in, so a new
        // part never flashes white, and the frame back to its own colours, stopping any fade the button began on it.
        private void UsePress(Button card, Image frame, Image press, Color pressColour)
        {
            card.targetGraphic = press;
            card.transition = Selectable.Transition.ColorTint;
            ColorBlock colours = card.colors;
            colours.normalColor = Clear;
            colours.highlightedColor = Clear;
            colours.selectedColor = Clear;
            colours.pressedColor = pressColour;
            colours.disabledColor = _art.DisabledColour;
            colours.colorMultiplier = 1f;
            card.colors = colours;
            press.CrossFadeColor(card.interactable ? colours.normalColor : colours.disabledColor, 0f, true, true);
            frame.CrossFadeColor(Color.white, 0f, true, true);
        }

        private static Image Behind(RectTransform target, string name, Sprite sprite, Vector4 margin,
            Image.Type type = Image.Type.Sliced)
        {
            Image part = Find(target.parent, name, out bool made);
            if (made)
                PlaceBefore(part.transform, target);
            Show(part, sprite, type);
            Fit(part.rectTransform, target, margin);
            return part;
        }

        private static void Show(Image image, Sprite sprite, Image.Type type)
        {
            image.sprite = sprite;
            image.type = type;
            image.pixelsPerUnitMultiplier = 1f;
            image.enabled = sprite != null;
        }

        private static void Restyle(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
        }

        // A part by its name under the parent, made the first time (not a raycast target, so a tap still reaches the
        // card's button).
        private static Image Find(Transform parent, string name, out bool made)
        {
            Transform found = parent.Find(name);
            made = found == null || !found.TryGetComponent(out Image _);
            if (!made)
                return found.GetComponent<Image>();
            var part = new GameObject(name, typeof(RectTransform));
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);
            var image = part.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Image Made(Transform parent, string name, Sprite sprite, Image.Type type)
        {
            Image image = Find(parent, name, out _);
            Show(image, sprite, type);
            return image;
        }

        // The target's rect, its edges moved out by the margin (left, bottom, right, top; negative moves them in). The
        // part is the target's sibling, so it takes the target's anchors and pivot.
        private static void Fit(RectTransform part, RectTransform target, Vector4 margin)
        {
            part.anchorMin = target.anchorMin;
            part.anchorMax = target.anchorMax;
            part.pivot = target.pivot;
            part.offsetMin = target.offsetMin - new Vector2(margin.x, margin.y);
            part.offsetMax = target.offsetMax + new Vector2(margin.z, margin.w);
        }

        private static void Stretch(RectTransform part, Vector2 min, Vector2 max)
        {
            part.anchorMin = Vector2.zero;
            part.anchorMax = Vector2.one;
            part.pivot = new Vector2(.5f, .5f);
            part.offsetMin = min;
            part.offsetMax = max;
        }

        // A sprite's size on the canvas at one texel a canvas unit, the density the set is painted at.
        private static Vector2 NativeSize(Sprite sprite) => sprite.rect.size * (100f / sprite.pixelsPerUnit);

        private static void PlaceBefore(Transform part, Transform target)
        {
            int at = part.GetSiblingIndex(), before = target.GetSiblingIndex();
            if (at != before - 1)
                part.SetSiblingIndex(at < before ? before - 1 : before);
        }

        private static void PlaceAfter(Transform part, Transform target)
        {
            int at = part.GetSiblingIndex(), after = target.GetSiblingIndex();
            if (at != after + 1)
                part.SetSiblingIndex(at < after ? after : after + 1);
        }
    }
}
