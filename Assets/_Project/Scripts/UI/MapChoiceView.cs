using System.Collections.Generic;
using System.Globalization;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The map a run starts on (docs/53): before the first wave, a panel of three cards - the Foundry (floor 1), the
    // Quicksilver Vaults (floor 2), the Gloam Gardens (floor 3) - each with its name, a line about it, what a hero starting
    // there begins with and a crop of the floor's own backdrop plate. Drawn in the run's choice cards' look (ChoiceCardPainter
    // with RunChoiceView's ChoiceCardArtSet; plain colours without one) on its own overlay canvas above the HUD, its cards
    // stacked full width for a thumb on a portrait phone. A tap on the Foundry begins the run here, as it always began
    // (CombatSetup.StartOnMap); a deeper map reloads the scene, which then begins on that floor with its catch-up. The hero
    // stands still while it is open. CombatSetup adds it when DevelopmentStart.OffersMapChoice says so: every run start of
    // a release build, never with map-choice.txt "off" (the scene tests) or another development start.
    public sealed class MapChoiceView : MonoBehaviour
    {
        public const string CanvasName = "Map Choice";
        public const int SortingOrder = 100;
        // Taps are ignored this long after the panel shows, as on the game's other panels, so a tap aimed elsewhere at load
        // cannot choose.
        public const float InputDelay = .35f;

        // On the 1080-wide canvas (CanvasScaler 1080x1920, width matched): a 992-wide surface as the choice panel's, three
        // 380-tall cards (thumb-sized on a 1080x2340 phone) under the title, the preview in each card's well on its left.
        private const float SurfaceWidth = 992f;
        private const float CardInset = 40f;
        private const float CardHeight = 380f;
        private const float CardGap = 36f;
        private const float TitleTop = 44f;
        private const float TitleHeight = 112f;
        private const float CardsTop = 200f;
        private const float BottomMargin = 48f;
        private const float PreviewWidth = 330f;
        private const float PreviewHeight = 300f;
        private const float TextLeft = 410f;

        // A line about each map, by floor number.
        private static readonly string[] Flavours =
        {
            "Lava-lit halls where every Descent begins. The Forge Warden keeps the gate.",
            "Frozen vaults of quicksilver. Mirror Sentries turn your blows back on you.",
            "A dusk garden grown over the ash. The last floor before the Final Swarm."
        };

        private CombatSetup _setup;
        private HeroMovementInput _movement;
        private bool _movementWasEnabled;
        private GameObject _canvas;
        private ChoiceCardPainter _painter;
        private readonly List<Button> _cards = new List<Button>(3);
        private readonly List<int> _floors = new List<int>(3);
        private readonly List<Text> _names = new List<Text>(3);
        private readonly List<Sprite> _made = new List<Sprite>(3);
        private float _inputEnabledAt;
        private bool _chosen;

        public bool IsOpen => _canvas != null && _canvas.activeSelf;
        public int CardCount => _cards.Count;
        public Button CardButton(int slot) => _cards[slot];
        // The floor number a card starts the run on, and the name it shows.
        public int FloorAt(int slot) => _floors[slot];
        public string NameAt(int slot) => _names[slot].text;
        public bool PaintsCards => _painter != null;
        // The panel's root, for tests and captures.
        public GameObject Panel => _canvas;

        public void Initialize(CombatSetup setup, HeroMovementInput movement)
        {
            _setup = setup;
            _movement = movement;
            // The hero stands while the player chooses, as at the gate's arrival.
            if (_movement != null)
            {
                _movementWasEnabled = _movement.enabled;
                _movement.Release();
                _movement.enabled = false;
            }
            Build();
        }

        private void Build()
        {
            var choices = FindFirstObjectByType<RunChoiceView>(FindObjectsInactive.Include);
            var arena = FindFirstObjectByType<ArenaView>();
            ChoiceCardArtSet art = choices != null ? choices.CardArt : null;
            if (art != null && !art.IsValid)
                art = null;
            Text titleSource = choices != null ? choices.TitleLabel : null;
            Text nameSource = choices != null ? choices.FirstNameLabel : null;
            Text descriptionSource = choices != null ? choices.FirstDescriptionLabel : null;
            Font fallback = titleSource == null || nameSource == null || descriptionSource == null
                ? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") : null;

            _canvas = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            _canvas.transform.SetParent(transform, false);
            var canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            // The dim over the run: it also stops a tap from reaching the HUD under it.
            Image backdrop = Part(_canvas.transform, "Map Choice Backdrop");
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            backdrop.color = new Color(.02f, .02f, .04f, .82f);
            backdrop.raycastTarget = true;

            var safe = new GameObject("Map Choice Safe Area", typeof(RectTransform));
            safe.layer = _canvas.layer;
            safe.transform.SetParent(_canvas.transform, false);
            Stretch((RectTransform)safe.transform, Vector2.zero, Vector2.zero);
            safe.AddComponent<SafeAreaFitter>();

            float height = CardsTop + 3 * CardHeight + 2 * CardGap + BottomMargin;
            Image surface = Part(safe.transform, "Map Choice Surface");
            RectTransform surfaceRect = surface.rectTransform;
            surfaceRect.anchorMin = surfaceRect.anchorMax = new Vector2(.5f, .5f);
            surfaceRect.pivot = new Vector2(.5f, .5f);
            surfaceRect.sizeDelta = new Vector2(SurfaceWidth, height);
            surface.color = new Color(.09f, .08f, .11f, .97f);
            surface.raycastTarget = true;

            Text title = Label(surface.transform, "Map Choice Title", titleSource, fallback, 56, TextAnchor.MiddleCenter);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(.5f, 1f);
            titleRect.offsetMin = new Vector2(120f, -(TitleTop + TitleHeight));
            titleRect.offsetMax = new Vector2(-120f, -TitleTop);
            title.text = "Choose your map";

            if (art != null)
                _painter = new ChoiceCardPainter(art, backdrop, surface, title);

            FloorDefinition floor = _setup.Encounters != null ? _setup.Encounters.Floor : null;
            for (int number = DescentStart.FirstFloor; number <= DescentStart.DeepestStart && floor != null; number++)
            {
                AddCard(surface.transform, number, floor, arena, art, nameSource, descriptionSource, fallback);
                floor = floor.NextFloor;
            }
            _inputEnabledAt = Time.unscaledTime + InputDelay;
            SetInteractable(false);
        }

        private void AddCard(Transform surface, int number, FloorDefinition floor, ArenaView arena, ChoiceCardArtSet art,
            Text nameSource, Text descriptionSource, Font fallback)
        {
            int slot = _cards.Count;
            var cardObject = new GameObject("Map Card " + number.ToString(CultureInfo.InvariantCulture),
                typeof(RectTransform), typeof(Image), typeof(Button));
            cardObject.layer = surface.gameObject.layer;
            var rect = (RectTransform)cardObject.transform;
            rect.SetParent(surface, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            float top = CardsTop + slot * (CardHeight + CardGap);
            rect.offsetMin = new Vector2(CardInset, -(top + CardHeight));
            rect.offsetMax = new Vector2(-CardInset, -top);
            var frame = cardObject.GetComponent<Image>();
            frame.color = new Color(.2f, .17f, .22f, 1f);
            var button = cardObject.GetComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => Choose(slot));

            Image preview = Part(rect, "Map Preview");
            RectTransform previewRect = preview.rectTransform;
            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0f, .5f);
            previewRect.pivot = new Vector2(0f, .5f);
            previewRect.anchoredPosition = new Vector2(CardInset, 0f);
            previewRect.sizeDelta = new Vector2(PreviewWidth, PreviewHeight);
            preview.sprite = Crop(PlateOf(floor, arena), PreviewWidth / PreviewHeight);
            preview.color = preview.sprite != null ? Color.white : Swatch(floor.Theme);
            preview.enabled = true;

            Text name = Label(rect, "Map Name", nameSource, fallback, 50, TextAnchor.MiddleLeft);
            RectTransform nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(.5f, 1f);
            nameRect.offsetMin = new Vector2(TextLeft, -128f);
            nameRect.offsetMax = new Vector2(-CardInset, -44f);
            name.text = floor.DisplayName;

            Text description = Label(rect, "Map Description", descriptionSource, fallback, 32, TextAnchor.UpperLeft);
            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = Vector2.zero;
            descriptionRect.anchorMax = Vector2.one;
            descriptionRect.offsetMin = new Vector2(TextLeft, 40f);
            descriptionRect.offsetMax = new Vector2(-CardInset, -150f);
            description.text = Describe(number);

            _cards.Add(button);
            _floors.Add(number);
            _names.Add(name);
            if (_painter != null)
                _painter.Paint(slot, button, name, description, preview,
                    number == DescentStart.FirstFloor ? art.Neutral : art.Climb);
        }

        // A card's text: the map's line, then what a hero starting there begins with. Numbers in the invariant culture.
        public static string Describe(int floorNumber)
        {
            string flavour = floorNumber >= 1 && floorNumber <= Flavours.Length ? Flavours[floorNumber - 1] : string.Empty;
            int level = DescentStart.CatchUpLevel(floorNumber);
            string start = level > 0
                ? string.Format(CultureInfo.InvariantCulture, "Floor {0}. Start at level {1}: choose {1} level-ups first.", floorNumber, level)
                : string.Format(CultureInfo.InvariantCulture, "Floor {0}. A fresh hero, the whole Descent ahead.", floorNumber);
            return flavour + "\n" + start;
        }

        // The floor's own plate: the foundry's cavern, or the painted backdrop of its theme (PlatformMaterialSet).
        private static Sprite PlateOf(FloorDefinition floor, ArenaView arena)
        {
            if (arena == null)
                return null;
            if (floor.Theme == FloorTheme.Foundry)
                return arena.CavernBackdrop;
            return arena.PlatformMaterials != null ? arena.PlatformMaterials.BackdropFor(floor.Theme) : null;
        }

        // The middle of the plate at the preview's shape, as a sprite on the plate's own texture (nothing is copied).
        private Sprite Crop(Sprite plate, float aspect)
        {
            if (plate == null || plate.texture == null || (plate.packed && plate.packingMode == SpritePackingMode.Tight))
                return null;
            Rect source = plate.packed ? plate.textureRect : plate.rect;
            float width = source.width, height = source.height;
            if (width / height > aspect)
                width = height * aspect;
            else
                height = width / aspect;
            var rect = new Rect(source.x + (source.width - width) / 2f, source.y + (source.height - height) / 2f, width, height);
            Sprite crop = Sprite.Create(plate.texture, rect, new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            crop.name = plate.name + " (map preview)";
            _made.Add(crop);
            return crop;
        }

        // Without a plate, the floor's colour.
        private static Color Swatch(FloorTheme theme) => theme switch
        {
            FloorTheme.Ice => new Color(.55f, .72f, .86f, 1f),
            FloorTheme.Gloam => new Color(.42f, .36f, .58f, 1f),
            _ => new Color(.78f, .36f, .16f, 1f)
        };

        private void Update()
        {
            if (!IsOpen)
                return;
            if (Time.unscaledTime >= _inputEnabledAt && !_chosen && _cards.Count > 0 && !_cards[0].interactable)
                SetInteractable(true);
            if (_painter != null)
                _painter.Tick(Time.unscaledTime);
        }

        private void SetInteractable(bool interactable)
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].interactable = interactable;
        }

        // A card tapped: once, after the input delay.
        public void Choose(int slot)
        {
            if (_chosen || !IsOpen || slot < 0 || slot >= _cards.Count || Time.unscaledTime < _inputEnabledAt)
                return;
            _chosen = true;
            SetInteractable(false);
            int floor = _floors[slot];
            if (floor == DescentStart.FirstFloor)
                Close();
            _setup.StartOnMap(floor);
        }

        private void Close()
        {
            if (_canvas != null)
                _canvas.SetActive(false);
            if (_movement != null)
            {
                _movement.Release();
                _movement.enabled = _movementWasEnabled;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _made.Count; i++)
                if (_made[i] != null)
                    Destroy(_made[i]);
            _made.Clear();
        }

        private static Image Part(Transform parent, string name)
        {
            var part = new GameObject(name, typeof(RectTransform));
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);
            var image = part.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(Transform parent, string name, Text source, Font fallback, int size, TextAnchor alignment)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.AddComponent<Text>();
            label.font = source != null ? source.font : fallback;
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

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = min;
            rect.offsetMax = max;
        }
    }
}
