using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The in-run HUD painted (HudArtSet, Tools/ArtReview/build_hud_art.py). It restyles the scene's own images in place -
    // the two panels and the pause button in their forged frames, the room pips, the ability's ring and its cooldown
    // band - and lays the painted bars round the fills RunHud drives: a trough under each fill, the pale chunk a hit
    // takes (health and the boss), a light at the fill's end and a frame over the fill's edges, and a smoked plate behind
    // the boss's name. The loadout row's plates become sockets once RunHud has built the row. Every rect the layout, the
    // camera's bands and the tests read stays where it was (a frame may reach past its bar, never past its panel, and
    // the boss bar's frame keeps to its rect's foot, so its fill is lifted inside it), RunHud and AbilityButtonView keep
    // setting the same fill amounts and colours, and no text changes. Without the set, or with a broken one, nothing is
    // touched: the flat HUD is the fallback.
    public sealed class HudArtView : MonoBehaviour
    {
        // One painted bar: the parts made round the fill, and the chunk it shows.
        public sealed class PaintedBar
        {
            internal PaintedBar(Image fill, Image trough, Image trail, Image glint, Image frame)
            {
                Fill = fill;
                Trough = trough;
                Trail = trail;
                Glint = glint;
                Frame = frame;
            }

            public Image Fill { get; }
            public Image Trough { get; }
            // Null on a bar that only grows.
            public Image Trail { get; }
            public Image Glint { get; }
            public Image Frame { get; }
            internal BarTrail Chunk;
            internal float ShownFill = -1f;
        }

        [SerializeField] private HudArtSet _art;
        [SerializeField] private Image _topSurface;
        [SerializeField] private Image _lowerSurface;
        [SerializeField] private Image _pauseButton;
        [SerializeField] private Image[] _roomPips;
        // The fills RunHud drives: the hero's health, his experience and the boss's (the HUD's enemy bar).
        [SerializeField] private Image _heroFill;
        [SerializeField] private Image _experienceFill;
        [SerializeField] private Image _bossFill;
        [SerializeField] private Text _bossName;
        [SerializeField] private Image _abilityBezel;
        [SerializeField] private Image _abilityCooldown;
        // Its loadout row, built in its Start on a category-mode run.
        [SerializeField] private RunHud _hud;
        // Whose health the boss bar showed at the last reading.
        private Health _bossSubject;
        private bool _loadoutChecked;
        // How many of RunHud's pips are painted: it copies the last for a floor with more rooms than the scene has pips.
        private int _hudPipsPainted;

        public bool UsesPaintedArt { get; private set; }
        public HudArtSet Art => _art;
        public PaintedBar Hero { get; private set; }
        public PaintedBar Experience { get; private set; }
        public PaintedBar Boss { get; private set; }
        public Image BossNamePlate { get; private set; }
        // The loadout row wears the painted sockets.
        public bool PaintsLoadout { get; private set; }

        private void Start()
        {
            if (_art == null || !_art.IsValid)
            {
                enabled = false;
                return;
            }
            Restyle(_topSurface, _art.Panel);
            Restyle(_lowerSurface, _art.Panel);
            Restyle(_pauseButton, _art.Button);
            if (_roomPips != null)
                foreach (Image pip in _roomPips)
                    PaintPip(pip);
            Hero = PaintBar(_heroFill, _art.Hero);
            Experience = PaintBar(_experienceFill, _art.Experience);
            Boss = PaintBar(_bossFill, _art.Boss);
            if (_bossName != null)
                BossNamePlate = PlateBehind(_bossName.rectTransform, _art.BossNamePlate);
            if (_abilityBezel != null)
            {
                _abilityBezel.sprite = _art.AbilityRing;
                _abilityBezel.type = Image.Type.Simple;
                _abilityBezel.color = Color.white;
            }
            // Still the radial fill AbilityButtonView drains, in the scene's cooldown colour: only its shape is painted.
            if (_abilityCooldown != null)
                _abilityCooldown.sprite = _art.AbilityCooldown;
            UsesPaintedArt = true;
        }

        // After every Start (so RunHud's row exists) and after the frame's damage (so a hit's chunk shows the frame it
        // lands): the chunks drain and the lights follow the fills' ends. A fill is only read; nothing is allocated.
        private void LateUpdate()
        {
            if (!_loadoutChecked)
            {
                _loadoutChecked = true;
                if (_hud != null && _hud.LoadoutBadges != null)
                {
                    _hud.LoadoutBadges.UsePaintedPlates(_art.WeaponSlot, _art.TomeSlot, _art.SlotAccent);
                    PaintsLoadout = true;
                }
            }
            // The pips RunHud added since the last frame, before the frame that first shows them is drawn.
            if (_hud != null && _hud.RoomPipCount > _hudPipsPainted)
            {
                for (int i = _hudPipsPainted; i < _hud.RoomPipCount; i++)
                    PaintPip(_hud.RoomPipAt(i));
                _hudPipsPainted = _hud.RoomPipCount;
            }
            float deltaTime = Time.deltaTime;
            Track(Hero, deltaTime);
            Track(Experience, deltaTime);
            // The boss bar shows whichever enemy is nearest: a new one is a new bar, not a hit on the old.
            Health shown = _hud != null ? _hud.ShownEnemy : null;
            if (Boss != null && !ReferenceEquals(shown, _bossSubject))
            {
                _bossSubject = shown;
                Boss.ShownFill = -1f;
            }
            Track(Boss, deltaTime);
        }

        private static void Track(PaintedBar bar, float deltaTime)
        {
            if (bar == null)
                return;
            float fill = bar.Fill.fillAmount;
            if (fill != bar.ShownFill)
            {
                // The first reading is where the bar starts, not a hit.
                if (bar.ShownFill < 0f)
                    bar.Chunk.Reset(fill);
                else
                    bar.Chunk.Follow(fill);
                bar.ShownFill = fill;
                bool inside = fill > 0f && fill < 1f;
                bar.Glint.enabled = inside;
                if (inside)
                    PlaceGlint(bar.Glint.rectTransform, fill);
            }
            if (bar.Trail == null)
                return;
            bar.Chunk.Tick(deltaTime);
            bool showing = bar.Chunk.IsMoving;
            if (bar.Trail.enabled != showing)
                bar.Trail.enabled = showing;
            if (showing && bar.Trail.fillAmount != bar.Chunk.Shown)
                bar.Trail.fillAmount = bar.Chunk.Shown;
        }

        // The light's anchors: across, the fill's end; up and down, the fill's foot and top, so it spans the fill.
        private static void PlaceGlint(RectTransform glint, float fill)
        {
            glint.anchorMin = new Vector2(fill, 0f);
            glint.anchorMax = new Vector2(fill, 1f);
        }

        // RunHud keeps tinting each pip in the room's colour.
        private void PaintPip(Image pip)
        {
            if (pip == null)
                return;
            pip.sprite = _art.Pip;
            pip.type = Image.Type.Sliced;
            pip.pixelsPerUnitMultiplier = 1f;
        }

        private static void Restyle(Image image, Sprite sprite)
        {
            if (image == null)
                return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
        }

        // The fill moves to the set's window inside its bar (the boss's is lifted over its frame's foot); the bar's flat
        // backing gives way to the painted trough, the chunk lies under the fill, the light rides the fill's end and the
        // frame lies over all of it. The bar's own rect is never touched.
        private static PaintedBar PaintBar(Image fill, HudArtSet.Bar art)
        {
            if (fill == null || !(fill.rectTransform.parent is RectTransform bar))
                return null;
            Vector4 window = art.Window;
            Vector2 min = new Vector2(-window.x, -window.y), max = new Vector2(window.z, window.w);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = min;
            fillRect.offsetMax = max;
            fill.sprite = art.Fill;
            fill.color = Color.white;
            var backing = bar.GetComponent<Image>();
            if (backing != null)
                backing.enabled = false;

            Image trough = Child("Painted Trough", bar, art.Trough, Image.Type.Sliced, min, max);
            trough.transform.SetSiblingIndex(0);
            Image trail = null;
            if (art.Trail != null)
            {
                trail = Child("Painted Trail", bar, art.Trail, Image.Type.Filled, min, max);
                trail.fillMethod = Image.FillMethod.Horizontal;
                trail.fillOrigin = (int)Image.OriginHorizontal.Left;
                trail.fillAmount = fill.fillAmount;
                trail.enabled = false;
                trail.transform.SetSiblingIndex(1);
            }
            // The light stands 6 taller than the fill, 3 past its foot and its top. Anchored to both, it keeps that
            // through any later relayout (the canvas or the safe area resizing the bar) with nothing done per frame.
            Image glint = Child("Painted Glint", fillRect, art.Glint, Image.Type.Simple, Vector2.zero, Vector2.zero);
            RectTransform glintRect = glint.rectTransform;
            PlaceGlint(glintRect, fill.fillAmount);
            glintRect.sizeDelta = new Vector2(art.GlintWidth, 6f);
            glint.enabled = false;
            Vector4 outset = art.FrameOutset;
            Image frame = Child("Painted Frame", bar, art.Frame, Image.Type.Sliced, min - new Vector2(outset.x, outset.y),
                max + new Vector2(outset.z, outset.w));
            frame.transform.SetAsLastSibling();
            return new PaintedBar(fill, trough, trail, glint, frame);
        }

        // An image stretched over its parent's rect, offset by min (bottom left) and max (top right).
        private static Image Child(string name, RectTransform parent, Sprite sprite, Image.Type type, Vector2 min, Vector2 max)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.layer = parent.gameObject.layer;
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = min;
            rect.offsetMax = max;
            var image = child.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        // A plate drawn just before the label, over its rect, so the label reads on it.
        private static Image PlateBehind(RectTransform label, Sprite sprite)
        {
            var plate = new GameObject("Painted Name Plate", typeof(RectTransform));
            plate.layer = label.gameObject.layer;
            var rect = (RectTransform)plate.transform;
            rect.SetParent(label.parent, false);
            rect.SetSiblingIndex(label.GetSiblingIndex());
            rect.anchorMin = label.anchorMin;
            rect.anchorMax = label.anchorMax;
            rect.pivot = label.pivot;
            rect.anchoredPosition = label.anchoredPosition;
            rect.sizeDelta = label.sizeDelta;
            var image = plate.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }
    }
}
