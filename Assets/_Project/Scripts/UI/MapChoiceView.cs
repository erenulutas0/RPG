using System;
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
    // The hero and the map a run starts with (docs/53): before the first wave, a row of hero cards - one per hero of the
    // roster, each with its portrait, name, one-line role and weapon, the chosen one lit, a locked one saying how to unlock
    // it - above three map cards - the Foundry (floor 1), the Quicksilver Vaults (floor 2), the Gloam Gardens (floor 3) -
    // each with its name, a line about it, what a hero starting there begins with and a crop of the floor's own backdrop
    // plate. Drawn in the run's choice cards' look (ChoiceCardPainter with RunChoiceView's ChoiceCardArtSet; plain colours
    // without one) on its own overlay canvas above the HUD, sized for a thumb on a portrait phone. A hero card chooses the
    // hero and the profile remembers it, as the forge's chooser does (HeroChoiceView); a map card starts the run: the
    // Foundry with the scene's own hero begins here, as it always began (CombatSetup.StartOnMap); a deeper map or another
    // hero reloads the scene once, which then begins with that hero on that floor (with its catch-up). The hero stands
    // still while it is open. With one hero in the roster there is no hero row. CombatSetup adds it when
    // DevelopmentStart.OffersMapChoice says so: every run start of a release build, never with map-choice.txt "off" (the
    // scene tests) or another development start.
    public sealed class MapChoiceView : MonoBehaviour
    {
        public const string CanvasName = "Map Choice";
        public const int SortingOrder = 100;
        // Taps are ignored this long after the panel shows, as on the game's other panels, so a tap aimed elsewhere at load
        // cannot choose.
        public const float InputDelay = .35f;

        // On the 1080-wide canvas (CanvasScaler 1080x1920, width matched): a 992-wide surface as the choice panel's; under
        // the title a row of 420-tall hero cards side by side, then three 300-tall map cards (thumb-sized on a 1080x2340
        // phone; 1644 units in all, inside a 16:9 phone's 1920), the preview in each map card's well on its left. A screen
        // shorter than the panel scales it down whole (FitToSafeArea).
        private const float SurfaceWidth = 992f;
        private const float CardInset = 40f;
        private const float CardHeight = 300f;
        private const float CardGap = 30f;
        private const float TitleTop = 44f;
        private const float TitleHeight = 100f;
        private const float HeroRowTop = 180f;
        public const float HeroCardHeight = 420f;
        private const float HeroCardGap = 28f;
        private const float HeroRowGap = 44f;
        private const float CardsTopWithoutHeroes = 190f;
        private const float BottomMargin = 40f;
        private const float PreviewWidth = 280f;
        private const float PreviewHeight = 236f;
        private const float TextLeft = 350f;
        private const float FitMargin = 16f;

        public const string MapTitle = "Choose your map";
        public const string HeroAndMapTitle = "Choose your hero and map";
        public const string SelectedText = "Selected";
        public const string ChooseText = "Tap to choose";
        public const string LockedWord = "Locked";
        private const string WeaponFormat = "Weapon: {0}";
        private const string SubtitleFormat = "{0}\n<size=26>{1}</size>";
        private static readonly Color SelectedColour = new Color32(238, 191, 108, 255);
        private static readonly Color IdleColour = new Color32(190, 194, 208, 255);
        private static readonly Color LockedColour = new Color32(163, 167, 183, 255);

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
        private readonly List<HeroCard> _heroCards = new List<HeroCard>(3);
        private ChoiceCardArtSet _art;
        private Text _title;
        private RectTransform _safe;
        private RectTransform _surface;
        private float _height;
        private float _inputEnabledAt;
        private bool _chosen;
        // Whether a hero may be chosen: CombatSetup.IsHeroPlayable, or a test's stand-in (TreatHeroesAs).
        private Func<HeroDefinition, bool> _isPlayable;

        private sealed class HeroCard
        {
            public HeroDefinition Hero;
            public Button Button;
            public Image Frame;
            public Image Portrait;
            public Text Name;
            public Text Role;
            public Text Weapon;
            public Text State;
        }

        public bool IsOpen => _canvas != null && _canvas.activeSelf;
        public string TitleText => _title != null ? _title.text : string.Empty;
        // The hero row, in the roster's order; empty while the roster has one hero.
        public int HeroCardCount => _heroCards.Count;
        public Button HeroCardButton(int slot) => _heroCards[slot].Button;
        public HeroDefinition HeroAt(int slot) => _heroCards[slot].Hero;
        public string HeroNameAt(int slot) => _heroCards[slot].Name.text;
        public string HeroRoleAt(int slot) => _heroCards[slot].Role.text;
        public string HeroWeaponAt(int slot) => _heroCards[slot].Weapon.text;
        // "Selected", "Tap to choose", or how a locked hero is unlocked.
        public string HeroStateAt(int slot) => _heroCards[slot].State.text;
        public Sprite HeroPortraitAt(int slot) => _heroCards[slot].Portrait.enabled ? _heroCards[slot].Portrait.sprite : null;
        // The hero a map card starts the run with: the scene's own until a hero card is tapped.
        public HeroDefinition SelectedHero { get; private set; }
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
            _isPlayable = setup.IsHeroPlayable;
            SelectedHero = setup.Hero;
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
            _art = art;
            IReadOnlyList<HeroDefinition> roster = _setup.Heroes;
            bool heroRow = roster != null && roster.Count > 1;
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
            _safe = (RectTransform)safe.transform;
            Stretch(_safe, Vector2.zero, Vector2.zero);
            safe.AddComponent<SafeAreaFitter>();

            float cardsTop = heroRow ? HeroRowTop + HeroCardHeight + HeroRowGap : CardsTopWithoutHeroes;
            float height = cardsTop + 3 * CardHeight + 2 * CardGap + BottomMargin;
            _height = height;
            Image surface = Part(safe.transform, "Map Choice Surface");
            RectTransform surfaceRect = surface.rectTransform;
            _surface = surfaceRect;
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
            title.text = heroRow ? HeroAndMapTitle : MapTitle;
            _title = title;

            if (art != null)
                _painter = new ChoiceCardPainter(art, backdrop, surface, title);

            FloorDefinition floor = _setup.Encounters != null ? _setup.Encounters.Floor : null;
            for (int number = DescentStart.FirstFloor; number <= DescentStart.DeepestStart && floor != null; number++)
            {
                AddCard(surface.transform, cardsTop, number, floor, arena, art, nameSource, descriptionSource, fallback);
                floor = floor.NextFloor;
            }
            if (heroRow)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    if (roster[i] != null)
                        AddHeroCard(surface.transform, roster[i], roster.Count, nameSource, descriptionSource, fallback);
                }
            }
            _inputEnabledAt = Time.unscaledTime + InputDelay;
            SetInteractable(false);
            RefreshHeroCards();
        }

        // A hero's card in the row: the portrait in the card's well at the top, then its name (and the character's own
        // name), its role, its weapon and its state. The row's cards share the surface's width.
        private void AddHeroCard(Transform surface, HeroDefinition hero, int count, Text nameSource, Text descriptionSource,
            Font fallback)
        {
            int slot = _heroCards.Count;
            float inner = SurfaceWidth - 2 * CardInset;
            float width = (inner - (count - 1) * HeroCardGap) / count;
            var cardObject = new GameObject("Hero Card " + hero.Id, typeof(RectTransform), typeof(Image), typeof(Button));
            cardObject.layer = surface.gameObject.layer;
            var rect = (RectTransform)cardObject.transform;
            rect.SetParent(surface, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(CardInset + slot * (width + HeroCardGap), -HeroRowTop);
            rect.sizeDelta = new Vector2(width, HeroCardHeight);
            var frame = cardObject.GetComponent<Image>();
            var button = cardObject.GetComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => ChooseHero(slot));

            Image portrait = Part(rect, "Hero Portrait");
            portrait.preserveAspect = true;
            Place(portrait.rectTransform, 18f, 168f, 64f);
            portrait.sprite = hero.Portrait;
            portrait.enabled = hero.Portrait != null;

            Text name = Label(rect, "Hero Name", nameSource, fallback, 38, TextAnchor.MiddleCenter);
            Place(name.rectTransform, 196f, 66f, 20f);
            name.supportRichText = true;
            name.text = hero.Subtitle.Length > 0
                ? string.Format(CultureInfo.InvariantCulture, SubtitleFormat, hero.DisplayName, hero.Subtitle)
                : hero.DisplayName;

            Text role = Label(rect, "Hero Role", descriptionSource, fallback, 26, TextAnchor.MiddleCenter);
            Place(role.rectTransform, 270f, 64f, 24f);
            role.text = hero.Role;

            Text weapon = Label(rect, "Hero Weapon", descriptionSource, fallback, 26, TextAnchor.MiddleCenter);
            Place(weapon.rectTransform, 336f, 32f, 20f);

            Text state = Label(rect, "Hero State", descriptionSource, fallback, 26, TextAnchor.MiddleCenter);
            Place(state.rectTransform, 370f, 36f, 16f);

            _heroCards.Add(new HeroCard
            {
                Hero = hero, Button = button, Frame = frame, Portrait = portrait, Name = name, Role = role, Weapon = weapon,
                State = state
            });
        }

        // Each hero card as the choice stands: the chosen one lit (the gold tier's treatment, or a warm frame unpainted)
        // and saying so, a playable one inviting a tap, a locked one shaded and saying how to unlock it
        // (PrototypeTextDefinition.HeroLockedFormat with the hero's UnlockHint, as the forge's chooser says it).
        private void RefreshHeroCards()
        {
            PrototypeTextDefinition text = _setup.Text;
            for (int i = 0; i < _heroCards.Count; i++)
            {
                HeroCard card = _heroCards[i];
                bool playable = _isPlayable(card.Hero);
                bool selected = card.Hero == SelectedHero;
                WeaponDefinition weapon = _setup.WeaponFor(card.Hero);
                card.Weapon.text = weapon != null
                    ? string.Format(CultureInfo.InvariantCulture, WeaponFormat, weapon.DisplayName) : string.Empty;
                if (!playable)
                {
                    string hint = card.Hero.UnlockHint;
                    card.State.text = hint.Length > 0 && text != null
                        ? string.Format(CultureInfo.InvariantCulture, text.HeroLockedFormat, LockedWord, hint) : LockedWord;
                    card.State.color = LockedColour;
                }
                else
                {
                    card.State.text = selected ? SelectedText : ChooseText;
                    card.State.color = selected ? SelectedColour : IdleColour;
                }
                // Interactable before the paint, so the press takes the state the card is in.
                card.Button.interactable = playable && !_chosen && Time.unscaledTime >= _inputEnabledAt;
                card.Frame.color = selected ? new Color(.42f, .32f, .15f, 1f)
                    : playable ? new Color(.2f, .17f, .22f, 1f) : new Color(.12f, .11f, .13f, 1f);
                if (_painter != null)
                {
                    ChoiceCardArtSet.Treatment look = selected ? _art.Tier(UpgradeRarity.Gold) ?? _art.Climb : _art.Neutral;
                    // After the map cards' slots, so each card breathes in its own phase.
                    _painter.Paint(DescentStart.DeepestStart + i, card.Button, card.Name, card.Role, card.Portrait, look);
                }
            }
        }

        // For tests: decide which heroes may be chosen as a release build would, and redraw the row.
        internal void TreatHeroesAs(Func<HeroDefinition, bool> isPlayable)
        {
            _isPlayable = isPlayable ?? _setup.IsHeroPlayable;
            RefreshHeroCards();
        }

        // A rect across the card (inset on both sides) from top, this tall.
        private static void Place(RectTransform rect, float top, float height, float inset)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.offsetMin = new Vector2(inset, -(top + height));
            rect.offsetMax = new Vector2(-inset, -top);
        }

        private void AddCard(Transform surface, float cardsTop, int number, FloorDefinition floor, ArenaView arena, ChoiceCardArtSet art,
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
            float top = cardsTop + slot * (CardHeight + CardGap);
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

            Text name = Label(rect, "Map Name", nameSource, fallback, 46, TextAnchor.MiddleLeft);
            RectTransform nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(.5f, 1f);
            nameRect.offsetMin = new Vector2(TextLeft, -100f);
            nameRect.offsetMax = new Vector2(-CardInset, -28f);
            name.text = floor.DisplayName;

            Text description = Label(rect, "Map Description", descriptionSource, fallback, 30, TextAnchor.UpperLeft);
            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = Vector2.zero;
            descriptionRect.anchorMax = Vector2.one;
            descriptionRect.offsetMin = new Vector2(TextLeft, 26f);
            descriptionRect.offsetMax = new Vector2(-CardInset, -112f);
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
            FitToSafeArea();
            if (_painter != null)
                _painter.Tick(Time.unscaledTime);
        }

        // A screen whose safe area is shorter (or narrower) than the panel shows it whole, scaled down; never up.
        private void FitToSafeArea()
        {
            if (_safe == null || _surface == null || _height <= 0f)
                return;
            Rect area = _safe.rect;
            float scale = Mathf.Min(1f, Mathf.Min((area.height - 2f * FitMargin) / _height, area.width / SurfaceWidth));
            if (scale <= 0f)
                return;
            if (!Mathf.Approximately(_surface.localScale.x, scale))
                _surface.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetInteractable(bool interactable)
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].interactable = interactable;
            for (int i = 0; i < _heroCards.Count; i++)
                _heroCards[i].Button.interactable = interactable && _isPlayable(_heroCards[i].Hero);
        }

        // A hero card tapped, after the input delay: a playable hero becomes the run's, and the profile remembers it as the
        // forge's chooser does (the default hero as no choice). A locked hero, or the one already chosen, changes nothing.
        public void ChooseHero(int slot)
        {
            if (_chosen || !IsOpen || slot < 0 || slot >= _heroCards.Count || Time.unscaledTime < _inputEnabledAt)
                return;
            HeroDefinition hero = _heroCards[slot].Hero;
            if (hero == SelectedHero || !_isPlayable(hero))
                return;
            SelectedHero = hero;
            _setup.Profile.SelectHero(hero == _setup.Heroes[0] ? null : hero.Id);
            RefreshHeroCards();
        }

        // A map card tapped: once, after the input delay. The Foundry with the scene's own hero closes the panel and the run
        // begins; a deeper map or another hero reloads the scene with the choice, the panel standing until it goes.
        public void Choose(int slot)
        {
            if (_chosen || !IsOpen || slot < 0 || slot >= _cards.Count || Time.unscaledTime < _inputEnabledAt)
                return;
            _chosen = true;
            SetInteractable(false);
            int floor = _floors[slot];
            HeroDefinition hero = SelectedHero;
            bool otherHero = hero != null && hero != _setup.Hero && _setup.IsHeroPlayable(hero);
            if (floor == DescentStart.FirstFloor && !otherHero)
                Close();
            _setup.StartOnMap(floor, otherHero ? hero : null);
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
