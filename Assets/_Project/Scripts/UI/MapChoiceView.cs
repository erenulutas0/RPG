using System;
using System.Collections.Generic;
using System.Globalization;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Localisation;
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
    // hero reloads the scene once, which then begins with that hero on that floor (with its catch-up). A deeper map opens
    // only once the profile has reached it (MapUnlock: the Vaults after floor 1 is cleared, the Gardens after floor 2);
    // until then its card is shaded like a locked hero's, says how to open it and cannot be tapped. The hero stands
    // still while it is open. With one hero in the roster there is no hero row. CombatSetup adds it when
    // DevelopmentStart.OffersMapChoice says so: every run start of a release build, never with map-choice.txt "off" (the
    // scene tests) or another development start.
    // Between the heroes and the maps stands the difficulty tier row (docs/54 section 21): Forged, Tempered, Inferno and
    // Abyssal side by side, the chosen one lit in its own tier's card (green, orange, red and gold, the run's choice cards'
    // ladder) and the others on the neutral card, a locked tier shaded and saying how it opens ("Locked: Complete the Ascent
    // on Forged"); under the row a line on the chosen tier - its enemies, its rewards, its item luck and the gear it is
    // built for beside the chosen hero's own. A tier card chooses the tier and the profile remembers it as it remembers the
    // hero; the map cards follow it, since each tier opens its deeper starts once a run on that tier has reached them. A
    // tier other than the scene's reloads the scene with the map, as another hero does.
    // Every word is the language's (docs/60 pass 2), a hero's through HeroText and a tier's through TierText, and the
    // language toggle stands in the title's band (the game's start; the pause overlay has the other): a tap writes the
    // panel's words again at once, and the run then starts in a scene loaded in that language (CombatSetup.StartOnMap).
    // The arena's entry (docs/55 section 11.5) mirrors the toggle at the band's left, the title centred between them: a tap
    // opens P0's arena (ArenaScreen, its canvas above this one), and closing it leaves the panel open as it was.
    public sealed class MapChoiceView : MonoBehaviour
    {
        public const string CanvasName = "Map Choice";
        public const int SortingOrder = 100;
        // Taps are ignored this long after the panel shows, as on the game's other panels, so a tap aimed elsewhere at load
        // cannot choose.
        public const float InputDelay = .35f;

        // On the 1080-wide canvas (CanvasScaler 1080x1920, width matched): a 992-wide surface as the choice panel's; under
        // the title a row of 420-tall hero cards side by side, then the tier row, then three 300-tall map cards (thumb-sized
        // on a 1080x2340 phone; 1900 units in all, inside a 16:9 phone's 1920), the preview in each map card's well on its
        // left. A screen shorter than the panel scales it down whole (FitToSafeArea).
        private const float SurfaceWidth = 992f;
        private const float CardInset = 40f;
        private const float CardHeight = 300f;
        private const float CardGap = 30f;
        private const float TitleTop = 44f;
        private const float TitleHeight = 100f;
        private const float HeroRowTop = 180f;
        public const float HeroCardHeight = 420f;
        private const float HeroCardGap = 28f;
        private const float CardsTopWithoutHeroes = 190f;
        // The tier row (docs/54 section 21): four 190-tall cards across the surface's inner width under the hero row (or the
        // title) - room for a locked tier's three lines of hint at 22 - the chosen tier's two lines under them, then the
        // maps: 1900 units in all with the hero row, so the panel fits a 16:9 phone's 1920 (scaled to 99 % inside its
        // 16-unit margins) and stands whole on the S23's 2340.
        public const float TierCardHeight = 190f;
        private const float TierCardGap = 16f;
        private const float TierRowGapAfterHeroes = 24f;
        private const float TierDetailGap = 6f;
        private const float TierDetailHeight = 56f;
        private const float TierRowGapBeforeMaps = 24f;
        private const float BottomMargin = 40f;
        private const float PreviewWidth = 280f;
        private const float PreviewHeight = 236f;
        private const float TextLeft = 350f;
        private const float FitMargin = 16f;
        // The language toggle (docs/60 pass 2; the pause overlay has the other): in the title's band at its right, so the
        // panel keeps its 1900 units. The Arena button (docs/55 section 11.5) is its mirror at the band's left, the same
        // size on the same plate, and the title stands centred between the two, 352 wide: its best fit may take it to two
        // lines, and it fits at its smallest size (the Turkish tests measure it).
        private const float LanguageWidth = 260f;
        private const float LanguageHeight = 84f;
        private const float LanguageGap = 20f;
        private const int LanguageFontSize = 32;
        public const string ArenaButtonName = "Arena Button";

        // The panel's words, in the language the game speaks (docs/60).
        public static string MapTitle => Loc.Text("map.title", "Choose your map");
        public static string HeroAndMapTitle => Loc.Text("map.title_with_heroes", "Choose your hero and map");
        public static string SelectedText => Loc.Text("map.selected", "Selected");
        public static string ChooseText => Loc.Text("map.tap_to_choose", "Tap to choose");
        public static string LockedWord => Loc.Text("map.locked", "Locked");
        private static readonly Color SelectedColour = new Color32(238, 191, 108, 255);
        private static readonly Color IdleColour = new Color32(190, 194, 208, 255);
        private static readonly Color LockedColour = new Color32(163, 167, 183, 255);

        // A line about each map, by floor number.
        private static string Flavour(int floorNumber) => floorNumber switch
        {
            1 => Loc.Text("map.flavour_1", "Lava-lit halls where every Ascent begins. The Forge Warden keeps the gate."),
            2 => Loc.Text("map.flavour_2", "Frozen vaults of quicksilver. Mirror Sentries turn your blows back on you."),
            3 => Loc.Text("map.flavour_3", "A dusk garden grown over the ash. The last floor before the Final Swarm."),
            _ => string.Empty
        };

        private CombatSetup _setup;
        private HeroMovementInput _movement;
        private bool _movementWasEnabled;
        private GameObject _canvas;
        private ChoiceCardPainter _painter;
        private readonly List<Button> _cards = new List<Button>(3);
        private readonly List<int> _floors = new List<int>(3);
        private readonly List<FloorDefinition> _floorDefinitions = new List<FloorDefinition>(3);
        private readonly List<Text> _names = new List<Text>(3);
        private readonly List<Text> _mapStates = new List<Text>(3);
        private readonly List<bool> _mapOpen = new List<bool>(3);
        private readonly List<Sprite> _made = new List<Sprite>(3);
        private readonly List<HeroCard> _heroCards = new List<HeroCard>(3);
        private readonly List<Text> _descriptions = new List<Text>(3);
        private readonly List<Image> _previews = new List<Image>(3);
        private readonly List<TierCard> _tierCards = new List<TierCard>(DifficultyTiers.Count);
        private Text _tierDetail;
        private LanguageToggleView _language;
        private Button _arena;
        private Text _arenaLabel;
        private bool _listening;
        private ChoiceCardArtSet _art;
        private Text _title;
        // The words and the canvas scale the title's plate was last fitted to.
        private string _fittedTitle;
        private float _fittedTitleScale;
        private RectTransform _safe;
        private RectTransform _surface;
        private float _height;
        private float _inputEnabledAt;
        private bool _chosen;
        // Whether a hero may be chosen: CombatSetup.IsHeroPlayable, or a test's stand-in (TreatHeroesAs).
        private Func<HeroDefinition, bool> _isPlayable;

        private sealed class TierCard
        {
            public DifficultyTier Tier;
            public Button Button;
            public Image Frame;
            public Text Name;
            public Text Reward;
            public Text State;
        }

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
        // The lines the title's plate was last fitted to (0 before a painted plate is fitted).
        public int TitleLines { get; private set; }
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
        // Whether a map card may start the run (MapUnlock: the Foundry always, a deeper map once the floor above it is
        // cleared), and what it says when it may not ("Locked: Clear floor 1 to unlock"); empty while it is open.
        public bool IsMapOpenAt(int slot) => _mapOpen[slot];
        public string MapStateAt(int slot) => _mapStates[slot].text;
        public bool PaintsCards => _painter != null;
        // The panel's root, for tests and captures.
        public GameObject Panel => _canvas;
        // The tier row (docs/54 section 21), Forged first: each card's tier, button, name and state ("Selected", "Tap to
        // choose", or how a locked tier opens), whether it may be chosen, and the line under the row on the chosen tier.
        public int TierCardCount => _tierCards.Count;
        public Button TierCardButton(int slot) => _tierCards[slot].Button;
        public DifficultyTier TierAt(int slot) => _tierCards[slot].Tier;
        public string TierNameAt(int slot) => _tierCards[slot].Name.text;
        public string TierRewardAt(int slot) => _tierCards[slot].Reward.text;
        public string TierStateAt(int slot) => _tierCards[slot].State.text;
        public bool IsTierOpenAt(int slot) => _setup.IsTierOpen(_tierCards[slot].Tier);
        public string TierDetailText => _tierDetail != null ? _tierDetail.text : string.Empty;
        // The tier a map card starts the run on: the scene's own until a tier card is tapped.
        public DifficultyTier SelectedTier { get; private set; }
        // The language toggle in the title's band (docs/60), for tests and captures.
        public LanguageToggleView LanguageToggle => _language;
        // The arena's entry at the band's left (docs/55 section 11.5) and the word it shows, for tests and captures.
        public Button ArenaButton => _arena;
        public string ArenaLabelText => _arenaLabel != null ? _arenaLabel.text : string.Empty;
        public float InputEnabledAt => _inputEnabledAt;

        public void Initialize(CombatSetup setup, HeroMovementInput movement)
        {
            _setup = setup;
            _movement = movement;
            _isPlayable = setup.IsHeroPlayable;
            SelectedHero = setup.Hero;
            SelectedTier = setup.Tier;
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

            float tierTop = heroRow ? HeroRowTop + HeroCardHeight + TierRowGapAfterHeroes : CardsTopWithoutHeroes;
            float cardsTop = tierTop + TierCardHeight + TierDetailGap + TierDetailHeight + TierRowGapBeforeMaps;
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

            // The title in the band's middle, the Arena button at its left (docs/55 section 11.5), the language toggle at its
            // right (docs/60).
            Text title = Label(surface.transform, "Map Choice Title", titleSource, fallback, 56, TextAnchor.MiddleCenter);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(.5f, 1f);
            titleRect.offsetMin = new Vector2(CardInset + LanguageWidth + LanguageGap, -(TitleTop + TitleHeight));
            titleRect.offsetMax = new Vector2(-(CardInset + LanguageWidth + LanguageGap), -TitleTop);
            LocalisedLabel.Bind(title, () => heroRow ? HeroAndMapTitle : MapTitle);
            _title = title;

            if (art != null)
            {
                _painter = new ChoiceCardPainter(art, backdrop, surface, title);
                FitTitlePlate();
            }
            _language = LanguageToggleView.Create(surfaceRect, descriptionSource, LanguageFontSize, Vector2.one, Vector2.one,
                Vector2.one, new Vector2(-CardInset, -(TitleTop + (TitleHeight - LanguageHeight) / 2f)),
                new Vector2(LanguageWidth, LanguageHeight));
            AddArenaButton(surfaceRect, descriptionSource, fallback);

            FloorDefinition floor = _setup.Encounters != null ? _setup.Encounters.Floor : null;
            for (int number = DescentStart.FirstFloor; number <= DescentStart.DeepestStart && floor != null; number++)
            {
                AddCard(surface.transform, cardsTop, number, floor, arena, nameSource, descriptionSource, fallback);
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
            for (int i = 0; i < DifficultyTiers.Count; i++)
                AddTierCard(surface.transform, tierTop, (DifficultyTier)i, nameSource, descriptionSource, fallback);
            _tierDetail = Label(surface.transform, "Tier Detail", descriptionSource, fallback, 24, TextAnchor.MiddleCenter);
            Place(_tierDetail.rectTransform, tierTop + TierCardHeight + TierDetailGap, TierDetailHeight, CardInset);
            _tierDetail.supportRichText = true;
            _inputEnabledAt = Time.unscaledTime + InputDelay;
            SetInteractable(false);
            RefreshHeroCards();
            RefreshTierCards();
            Loc.Changed += OnLanguageChanged;
            _listening = true;
        }

        // The language changed (the toggle here, docs/60): every card's words are written again in it at once.
        private void OnLanguageChanged()
        {
            if (_canvas == null)
                return;
            RefreshHeroCards();
            RefreshTierCards();
            RefreshMapCards();
        }

        // The arena's entry (docs/55 section 11.5): the language toggle's mirror at the title band's left - its size, its
        // plate (LanguageToggleView.PaintPlate), its label's style and inset - naming the arena in the language the game
        // speaks ("Arena" in both).
        private void AddArenaButton(RectTransform surface, Text labelStyle, Font fallback)
        {
            var buttonObject = new GameObject(ArenaButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.layer = surface.gameObject.layer;
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(surface, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(CardInset, -(TitleTop + (TitleHeight - LanguageHeight) / 2f));
            rect.sizeDelta = new Vector2(LanguageWidth, LanguageHeight);
            _arena = buttonObject.GetComponent<Button>();
            _arena.targetGraphic = buttonObject.GetComponent<Image>();
            LanguageToggleView.PaintPlate(_arena);

            _arenaLabel = Label(rect, ArenaButtonName + " Label", labelStyle, fallback, LanguageFontSize, TextAnchor.MiddleCenter);
            Stretch(_arenaLabel.rectTransform, new Vector2(24f, 10f), new Vector2(-24f, -10f));
            LocalisedLabel.Bind(_arenaLabel, () => ArenaText.Title);
            _arena.onClick.AddListener(OpenArena);
        }

        // The Arena button tapped, after the input delay and while no map is chosen: P0's arena opens over the panel, its
        // canvas (ArenaScreen.SortingOrder, 110) above this one's 100 and taking every touch. The panel holds the run
        // meanwhile, as it does while the player chooses.
        public void OpenArena()
        {
            if (_chosen || !IsOpen || Time.unscaledTime < _inputEnabledAt)
                return;
            if (ArenaScreen.Current != null && ArenaScreen.Current.IsOpen)
                return;
            ArenaScreen.Open(_setup, OnArenaClosed);
        }

        // The arena closed: the panel stands as it was, its input delay begun again - the arena's Close sits over this
        // band's left, so a second tap on it cannot open the arena again or reach another control.
        private void OnArenaClosed()
        {
            if (!IsOpen || _chosen)
                return;
            _inputEnabledAt = Time.unscaledTime + InputDelay;
            SetInteractable(false);
        }

        // A tier's card in the row: its name, its reward and its state, the row's cards sharing the surface's inner width.
        private void AddTierCard(Transform surface, float top, DifficultyTier tier, Text nameSource, Text descriptionSource,
            Font fallback)
        {
            int slot = _tierCards.Count;
            float inner = SurfaceWidth - 2 * CardInset;
            float width = (inner - (DifficultyTiers.Count - 1) * TierCardGap) / DifficultyTiers.Count;
            var cardObject = new GameObject("Tier Card " + DifficultyTiers.Id(tier), typeof(RectTransform), typeof(Image), typeof(Button));
            cardObject.layer = surface.gameObject.layer;
            var rect = (RectTransform)cardObject.transform;
            rect.SetParent(surface, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(CardInset + slot * (width + TierCardGap), -top);
            rect.sizeDelta = new Vector2(width, TierCardHeight);
            var frame = cardObject.GetComponent<Image>();
            var button = cardObject.GetComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => ChooseTier(slot));

            // The name and the reward are written by RefreshTierCards, in the language the game speaks.
            Text name = Label(rect, "Tier Name", nameSource, fallback, 36, TextAnchor.MiddleCenter);
            Place(name.rectTransform, 14f, 46f, 14f);

            Text reward = Label(rect, "Tier Reward", descriptionSource, fallback, 22, TextAnchor.MiddleCenter);
            Place(reward.rectTransform, 60f, 30f, 12f);

            Text state = Label(rect, "Tier State", descriptionSource, fallback, 22, TextAnchor.MiddleCenter);
            Place(state.rectTransform, 94f, 84f, 10f);

            _tierCards.Add(new TierCard { Tier = tier, Button = button, Frame = frame, Name = name, Reward = reward, State = state });
        }

        // Each tier card as the choice stands: its name and reward, the chosen one lit in its own tier's card and saying so,
        // an open one inviting a tap on the neutral card, a locked one shaded and saying how it opens; then the line on the
        // chosen tier.
        private void RefreshTierCards()
        {
            PrototypeTextDefinition text = _setup.Text;
            for (int i = 0; i < _tierCards.Count; i++)
            {
                TierCard card = _tierCards[i];
                bool open = _setup.IsTierOpen(card.Tier);
                bool selected = card.Tier == SelectedTier;
                card.Name.text = TierText.Name(card.Tier);
                card.Reward.text = Loc.Format("tier.card_reward", "Rewards {0}", TierText.Reward(card.Tier));
                if (!open)
                {
                    string hint = TierText.UnlockHint(card.Tier);
                    card.State.text = text != null
                        ? string.Format(Loc.Numbers, text.HeroLockedFormat, LockedWord, hint)
                        : LockedWord + ": " + hint;
                    card.State.color = LockedColour;
                }
                else
                {
                    card.State.text = selected ? SelectedText : ChooseText;
                    card.State.color = selected ? SelectedColour : IdleColour;
                }
                card.Button.interactable = open && !_chosen && Time.unscaledTime >= _inputEnabledAt;
                card.Frame.color = selected ? TierSwatch(card.Tier) : open ? new Color(.2f, .17f, .22f, 1f) : new Color(.12f, .11f, .13f, 1f);
                if (_painter != null)
                {
                    ChoiceCardArtSet.Treatment look = selected ? _art.Tier(TierLook(card.Tier)) ?? _art.Climb : _art.Neutral;
                    // After the map cards' and the hero cards' slots, so each card breathes in its own phase.
                    _painter.Paint(DescentStart.DeepestStart + _heroCards.Count + i, card.Button, card.Name, card.Reward, null, look);
                    // No crest on these cards: the gold clasp would stand above the card's top edge, into the hero row.
                    Transform crest = card.Button.transform.Find(ChoiceCardPainter.CrestName);
                    if (crest != null && crest.TryGetComponent(out Image crestImage))
                        crestImage.enabled = false;
                }
            }
            if (_tierDetail != null)
                _tierDetail.text = DescribeTier(SelectedTier, _setup.GearPowerFor(SelectedHero));
        }

        // The run's choice cards' ladder for the tiers: Forged green, Tempered orange, Inferno red, Abyssal gold.
        private static UpgradeRarity TierLook(DifficultyTier tier) => (UpgradeRarity)(int)tier;

        private static Color TierSwatch(DifficultyTier tier) => tier switch
        {
            DifficultyTier.Tempered => new Color(.62f, .38f, .16f, 1f),
            DifficultyTier.Inferno => new Color(.56f, .2f, .2f, 1f),
            DifficultyTier.Abyssal => new Color(.66f, .5f, .2f, 1f),
            _ => new Color(.26f, .44f, .26f, 1f)
        };

        // The two lines under the tier row: what the chosen tier does to the foes and pays (gold, materials and boosters, and
        // the items' luck), then the gear it is built for beside the chosen hero's own. In the language the game speaks,
        // its numbers too: the formats sit in the templates, so Turkish puts the percent sign before the number ("+%14,7").
        public static string DescribeTier(DifficultyTier tier, int gearPower)
        {
            double yours = gearPower / 100.0;
            if (tier == DifficultyTier.Forged)
                return Loc.Format("tier.detail_forged",
                    "{0}: the Ascent as it was first forged; rewards x1\nBuilt for no gear. Your gear: {1:+0.#}%",
                    TierText.Name(tier), yours);
            return Loc.Format("tier.detail",
                "{0}: foes {1:+0.#}% health, {2:+0.#}% damage; rewards {3}; item luck +{4:0}\nBuilt for gear {5:+0}% ({6}). Your gear: {7:+0.#}%",
                TierText.Name(tier), (DifficultyTiers.EnemyHealth(tier) - 1.0) * 100.0,
                (DifficultyTiers.EnemyDamage(tier) - 1.0) * 100.0, TierText.Reward(tier), DifficultyTiers.ItemLuck(tier),
                DifficultyTiers.MatchedGearPower(tier) / 100.0, MatchedSet(tier), yours);
        }

        // The set docs/54 section 7.2 models at the tier's gear power. Forged's line names none.
        private static string MatchedSet(DifficultyTier tier) => tier switch
        {
            DifficultyTier.Tempered => Loc.Text("tier.set_tempered", "a +5 set"),
            DifficultyTier.Inferno => Loc.Text("tier.set_inferno", "a +9 set"),
            DifficultyTier.Abyssal => Loc.Text("tier.set_abyssal", "+9 with good efsun"),
            _ => string.Empty
        };

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

            // The name, role, weapon and state are written by RefreshHeroCards, in the language the game speaks.
            Text name = Label(rect, "Hero Name", nameSource, fallback, 38, TextAnchor.MiddleCenter);
            Place(name.rectTransform, 196f, 66f, 20f);
            name.supportRichText = true;

            Text role = Label(rect, "Hero Role", descriptionSource, fallback, 26, TextAnchor.MiddleCenter);
            Place(role.rectTransform, 270f, 64f, 24f);

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

        // Each hero card as the choice stands: its name (with the character's own under it), role and weapon, the chosen one
        // lit (the gold tier's treatment, or a warm frame unpainted) and saying so, a playable one inviting a tap, a locked
        // one shaded and saying how to unlock it (PrototypeTextDefinition.HeroLockedFormat with the hero's unlock hint, as
        // the forge's chooser says it). A hero's words through HeroText.
        private void RefreshHeroCards()
        {
            PrototypeTextDefinition text = _setup.Text;
            for (int i = 0; i < _heroCards.Count; i++)
            {
                HeroCard card = _heroCards[i];
                bool playable = _isPlayable(card.Hero);
                bool selected = card.Hero == SelectedHero;
                string subtitle = HeroText.Subtitle(card.Hero);
                card.Name.text = subtitle.Length > 0
                    ? Loc.Format("map.hero_name", "{0}\n<size=26>{1}</size>", HeroText.Name(card.Hero), subtitle)
                    : HeroText.Name(card.Hero);
                card.Role.text = HeroText.Role(card.Hero);
                WeaponDefinition weapon = _setup.WeaponFor(card.Hero);
                card.Weapon.text = weapon != null ? Loc.Format("map.hero_weapon", "Weapon: {0}", weapon.DisplayName) : string.Empty;
                if (!playable)
                {
                    string hint = _setup.UnlockHint(card.Hero);
                    card.State.text = hint.Length > 0 && text != null
                        ? string.Format(Loc.Numbers, text.HeroLockedFormat, LockedWord, hint) : LockedWord;
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

        private void AddCard(Transform surface, float cardsTop, int number, FloorDefinition floor, ArenaView arena,
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
            // A map not yet reached on the chosen tier is shaded like a locked hero card and says how to open it (MapUnlock;
            // RefreshMapCards draws it as the tier stands).
            var frame = cardObject.GetComponent<Image>();
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

            // The name and the description are written by RefreshMapCard, in the language the game speaks.
            Text description = Label(rect, "Map Description", descriptionSource, fallback, 30, TextAnchor.UpperLeft);
            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = Vector2.zero;
            descriptionRect.anchorMax = Vector2.one;
            descriptionRect.offsetMax = new Vector2(-CardInset, -112f);

            // The locked line, worded as a locked hero's (PrototypeTextDefinition.HeroLockedFormat); empty while open.
            Text state = Label(rect, "Map State", descriptionSource, fallback, 28, TextAnchor.MiddleLeft);
            RectTransform stateRect = state.rectTransform;
            stateRect.anchorMin = Vector2.zero;
            stateRect.anchorMax = new Vector2(1f, 0f);
            stateRect.pivot = new Vector2(.5f, 0f);
            stateRect.offsetMin = new Vector2(TextLeft, 24f);
            stateRect.offsetMax = new Vector2(-CardInset, 64f);
            state.color = LockedColour;

            _cards.Add(button);
            _floors.Add(number);
            _floorDefinitions.Add(floor);
            _names.Add(name);
            _mapStates.Add(state);
            _mapOpen.Add(false);
            _descriptions.Add(description);
            _previews.Add(preview);
            RefreshMapCard(slot);
        }

        private void RefreshMapCards()
        {
            for (int i = 0; i < _cards.Count; i++)
                RefreshMapCard(i);
        }

        // A map card as the chosen tier stands: its name and description, open, or shaded with its hint (the description
        // giving up its bottom 44 units to that line), interactable only while open and the panel takes taps.
        private void RefreshMapCard(int slot)
        {
            int number = _floors[slot];
            bool open = _setup.IsMapOpen(number, SelectedTier);
            _mapOpen[slot] = open;
            _names[slot].text = _floorDefinitions[slot].DisplayName;
            _descriptions[slot].text = Describe(number);
            _cards[slot].GetComponent<Image>().color = open ? new Color(.2f, .17f, .22f, 1f) : new Color(.12f, .11f, .13f, 1f);
            RectTransform descriptionRect = _descriptions[slot].rectTransform;
            descriptionRect.offsetMin = new Vector2(TextLeft, open ? 26f : 70f);
            PrototypeTextDefinition text = _setup.Text;
            string hint = MapUnlock.Hint(number);
            _mapStates[slot].text = open ? string.Empty
                : text != null ? string.Format(Loc.Numbers, text.HeroLockedFormat, LockedWord, hint) : LockedWord;
            _cards[slot].interactable = open && !_chosen && Time.unscaledTime >= _inputEnabledAt;
            if (_painter != null)
                _painter.Paint(slot, _cards[slot], _names[slot], _descriptions[slot], _previews[slot],
                    number == DescentStart.FirstFloor || !open ? _art.Neutral : _art.Climb);
        }

        // A card's text: the map's line, then what a hero starting there begins with, in the language the game speaks.
        public static string Describe(int floorNumber)
        {
            string flavour = Flavour(floorNumber);
            int level = DescentStart.CatchUpLevel(floorNumber);
            string start = level > 0
                ? Loc.Format("map.start_catch_up", "Floor {0}. Start at level {1}: choose {1} level-ups first.", floorNumber, level)
                : Loc.Format("map.start_fresh", "Floor {0}. A fresh hero, the whole Ascent ahead.", floorNumber);
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
            FitTitlePlate();
            if (_painter != null)
                _painter.Tick(Time.unscaledTime);
        }

        // Fits the surface to the safe area the canvas measures now - a capture tool calls it after it redraws the canvases
        // at a phone's size, since Update would only fit them a frame later (as AnvilView.FitNow) - and the title's plate
        // to its lines.
        public void FitNow()
        {
            FitToSafeArea();
            FitTitlePlate();
        }

        // The painted title plate round the title's lines (ChoiceCardPainter.FitTitleToLines): the title wraps to two lines
        // in Turkish and the plate's rules and crest, laid for one, crossed the second. Measured again only when the words
        // (a language change) or the canvas's scale change.
        private void FitTitlePlate()
        {
            if (_painter == null || _title == null)
                return;
            float scale = _title.pixelsPerUnit;
            if (_fittedTitle == _title.text && _fittedTitleScale == scale)
                return;
            _fittedTitle = _title.text;
            _fittedTitleScale = scale;
            TitleLines = _painter.FitTitleToLines();
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
                _cards[i].interactable = interactable && _mapOpen[i];
            for (int i = 0; i < _heroCards.Count; i++)
                _heroCards[i].Button.interactable = interactable && _isPlayable(_heroCards[i].Hero);
            for (int i = 0; i < _tierCards.Count; i++)
                _tierCards[i].Button.interactable = interactable && _setup.IsTierOpen(_tierCards[i].Tier);
            if (_language != null)
                _language.Button.interactable = interactable;
            if (_arena != null)
                _arena.interactable = interactable;
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
            // The line under the tier row names this hero's gear.
            RefreshTierCards();
        }

        // A tier card tapped, after the input delay: an open tier becomes the run's, and the profile remembers it as it
        // remembers the hero; the map cards follow it (each tier opens its own deeper starts). A locked tier, or the one
        // already chosen, changes nothing.
        public void ChooseTier(int slot)
        {
            if (_chosen || !IsOpen || slot < 0 || slot >= _tierCards.Count || Time.unscaledTime < _inputEnabledAt)
                return;
            DifficultyTier tier = _tierCards[slot].Tier;
            if (tier == SelectedTier || !_setup.IsTierOpen(tier))
                return;
            SelectedTier = tier;
            _setup.Profile.SelectTier(tier);
            RefreshTierCards();
            RefreshMapCards();
        }

        // A map card tapped: once, after the input delay, and only a map the profile has reached on the chosen tier. The
        // Foundry with the scene's own hero, tier and language closes the panel and the run begins; a deeper map, another
        // hero, another tier or another language (the toggle) reloads the scene with the choice, the panel standing until
        // it goes.
        public void Choose(int slot)
        {
            if (_chosen || !IsOpen || slot < 0 || slot >= _cards.Count || Time.unscaledTime < _inputEnabledAt || !_mapOpen[slot])
                return;
            _chosen = true;
            SetInteractable(false);
            int floor = _floors[slot];
            HeroDefinition hero = SelectedHero;
            bool otherHero = hero != null && hero != _setup.Hero && _setup.IsHeroPlayable(hero);
            bool otherTier = SelectedTier != _setup.Tier;
            if (floor == DescentStart.FirstFloor && !otherHero && !otherTier && !_setup.LanguageChangedSinceLoad)
                Close();
            _setup.StartOnMap(floor, otherHero ? hero : null, SelectedTier);
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
            if (_listening)
                Loc.Changed -= OnLanguageChanged;
            _listening = false;
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
