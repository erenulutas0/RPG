using System;
using System.Collections.Generic;
using System.Globalization;
using Cryptforge.Art;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The Forge's Anvil, Efsun and Bag tabs on the phone (docs/54 section 11, phase 4), over the Relic Forge panel on its own
    // overlay canvas, built in code like the map choice (MapChoiceView) and drawn in the choice cards' look
    // (ChoiceCardPainter: the dim, the surface, the title plate; each piece on its rarity's card - Common grey, Rare blue,
    // Epic red, Gold gold) with the forge's painted plates on its buttons (ScreenArtSet); plain colours without either.
    //
    //   - The Forge's title row gains two buttons, Bag on the left and Anvil on the right of the nameplate; each opens this
    //     panel on its tab. The panel's own tab row is Forge (back to the Relic Forge), Anvil, Efsun and Bag.
    //   - Under the tabs: the wallet (gold and the three floor materials) and the boosters, then the equipped pieces - the
    //     forged weapon the next run carries, the armour, the amulet and the ring - each on its rarity's card with its level.
    //     A tap picks the piece the Anvil and Efsun tabs work on (the equipped weapon first).
    //   - Anvil: the piece, its main stat now and at the next level, the chance (the base, the anvil's memory - the pity -
    //     and a Seal, or certain by the ceiling or a Hammer), the memory's bar towards the ceiling, the cost in gold and
    //     material, the Seal and Hammer toggles where they apply, and a strike held for 0.4 s (HoldToStrike). The moment
    //     after: "+8: the strike held" in gold, or "Missed: +7 kept" in grey with the memory a notch fuller. Never a
    //     break, never a lower level.
    //   - Efsun: the piece's lines with their tiers; a reroll (gold, or an Efsun Scroll) shows the new set beside the old,
    //     kept with Keep old or Take new; a Binding Rune adds a line.
    //   - Bag: thirty places on their rarity's ground with each item's level; a tap picks one, then Equip, Melt (a second tap
    //     confirms), Inherit (pick the receiving item of the same slot, a lower level; a second tap pays the 10 %) or Upgrade
    //     (the Anvil tab on that item).
    // Every number is written in the invariant culture (GearText); the Anvil service (CombatSetup.Anvil) does every change.
    public sealed class AnvilView : MonoBehaviour
    {
        public enum Tab
        {
            Forge = 0,
            Anvil = 1,
            Efsun = 2,
            Bag = 3
        }

        public const string CanvasName = "Anvil";
        public const string AnvilEntryName = "Forge Anvil Button";
        public const string BagEntryName = "Forge Bag Button";
        public const int SortingOrder = 90;
        public const float InputDelay = .25f;
        // The forge's two entries: this wide, this far in from the screen's edges.
        public const float EntryWidth = 186f;
        public const float EntryOuter = 36f;
        // How long the strike's moment stays over the piece.
        public const float MomentSeconds = 1.4f;

        // On the 1080-wide canvas: a 992 x 1860 surface (it fits a 16:9 phone's 1920 and scales down on a shorter safe area).
        private const float SurfaceWidth = 992f;
        private const float SurfaceHeight = 1860f;
        private const float Inset = 32f;
        private const float TitleTop = 34f;
        private const float TitleHeight = 96f;
        private const float TabsTop = 168f;
        private const float TabsHeight = 104f;
        private const float WalletTop = 286f;
        private const float WalletHeight = 50f;
        private const float BoostersTop = 338f;
        private const float BoostersHeight = 46f;
        private const float PiecesTop = 400f;
        private const float PieceHeight = 236f;
        private const float PieceGap = 30f;
        private const float PageTop = 656f;
        private const float BottomMargin = 34f;
        private const float FitMargin = 16f;
        private const int Columns = 6;
        private const float TileGap = 10f;

        private static readonly string[] TabNames = { "Forge", "Anvil", "Efsun", "Bag" };
        private static readonly Color Gold = new Color32(238, 191, 108, 255);
        private static readonly Color Muted = new Color32(163, 167, 183, 255);
        private static readonly Color Bright = new Color32(240, 234, 220, 255);
        private static readonly Color Short = new Color32(232, 112, 92, 255);
        private static readonly Color Held = new Color32(255, 214, 120, 255);
        private static readonly Color[] MaterialColours =
        {
            new Color32(244, 140, 72, 255), new Color32(170, 204, 232, 255), new Color32(184, 226, 112, 255)
        };

        private CombatSetup _setup;
        private Text _titleSource;
        private Text _nameSource;
        private Text _descriptionSource;
        private Font _fallback;
        private ScreenArtSet _screens;
        private ChoiceCardArtSet _cards;
        private UpgradeIconSet _icons;
        private ChoiceCardPainter _painter;
        private GameObject _canvas;
        private RectTransform _safe;
        private RectTransform _surface;
        private Text _title;
        private Text _wallet;
        private Text _boosters;
        private readonly Button[] _tabs = new Button[4];
        private readonly Text[] _tabLabels = new Text[4];
        private readonly PieceCard[] _pieces = new PieceCard[4];
        private GameObject _anvilPage;
        private GameObject _efsunPage;
        private GameObject _bagPage;
        private float _inputEnabledAt;
        private bool _subscribed;

        // What the Anvil and Efsun tabs work on: a piece the profile holds, or the equipped weapon before its gear exists.
        private ItemInstance _piece;
        private string _weaponId;

        // Anvil page.
        private PieceCard _anvilCard;
        private Text _mainStat;
        private Text _chanceCaption;
        private Text _chance;
        private Text _breakdown;
        private Image _memoryFill;
        private Text _memory;
        private Text _cost;
        private Button _seal;
        private Text _sealLabel;
        private Button _hammer;
        private Text _hammerLabel;
        private Button _strike;
        private Text _strikeLabel;
        private HoldToStrike _hold;
        private GameObject _moment;
        private Image _momentPlate;
        private Text _momentText;
        private float _momentUntil;
        private bool _sealOn;
        private bool _hammerOn;

        // Efsun page.
        private PieceCard _efsunCard;
        private Text _currentHeader;
        private Text _newHeader;
        private readonly LineRow[] _current = new LineRow[EfsunRule.MaxLines];
        private readonly LineRow[] _shown = new LineRow[EfsunRule.MaxLines];
        private Text _efsunNote;
        private Button _rerollGold;
        private Text _rerollGoldLabel;
        private Button _rerollScroll;
        private Text _rerollScrollLabel;
        private Button _keepOld;
        private Button _takeNew;
        private Button _rune;
        private Text _runeLabel;
        private float _halfWidth;
        private float _fullWidth;

        // Bag page.
        private Text _bagCount;
        private readonly Tile[] _tiles = new Tile[ProfileGear.BagCapacity];
        private Text _detail;
        private Button _equip;
        private Text _equipLabel;
        private Button _melt;
        private Text _meltLabel;
        private Button _inherit;
        private Text _inheritLabel;
        private Button _upgrade;
        private ItemInstance _bagPick;
        private bool _meltArmed;
        private ItemInstance _inheritSource;
        private ItemInstance _inheritTarget;

        private sealed class PieceCard
        {
            public Button Button;
            public Image Frame;
            public Image Icon;
            public Text Level;
            public Text Name;
            public Text Info;
            public Image Mark;
        }

        private sealed class LineRow
        {
            public GameObject Root;
            public Text Text;
            public Image[] Pips;
        }

        private sealed class Tile
        {
            public Button Button;
            public Image Frame;
            public Image Ground;
            public Image Icon;
            public Text Level;
            public Image Mark;
            public ItemInstance Item;
        }

        public Tab Current { get; private set; } = Tab.Anvil;
        public bool IsOpen => _canvas != null && _canvas.activeSelf;
        public GameObject Panel => _canvas;
        public Button AnvilEntry { get; private set; }
        public Button BagEntry { get; private set; }
        // The piece the Anvil and Efsun tabs work on; null while the equipped weapon has no gear yet (WeaponId names it).
        public ItemInstance Selected => _piece;
        public string WeaponId => _weaponId;
        public bool PaintsCards => _painter != null;
        public Button TabButton(Tab tab) => _tabs[(int)tab];
        public Button PieceButton(int slot) => _pieces[slot].Button;
        public string PieceText(int slot) => _pieces[slot].Level.text + " " + _pieces[slot].Name.text;
        public HoldToStrike Strike => _hold;
        public Button StrikeButton => _strike;
        public string StrikeText => _strikeLabel.text;
        public Button SealToggle => _seal;
        public Button HammerToggle => _hammer;
        public string ChanceText => _chance.text;
        public string BreakdownText => _breakdown.text;
        public string CostText => _cost.text;
        public string MainStatText => _mainStat.text;
        public string MemoryText => _memory.text;
        // The strike's moment while it shows ("+8: the strike held" or "Missed: +7 kept"), else empty.
        public string MomentText => _moment != null && _moment.activeSelf ? _momentText.text : string.Empty;
        public Button RerollWithGold => _rerollGold;
        public Button RerollWithScroll => _rerollScroll;
        public Button KeepOld => _keepOld;
        public Button TakeNew => _takeNew;
        public Button AddLine => _rune;
        public string CurrentLinesText => Join(_current);
        public string NewLinesText => Join(_shown);
        public string BagCountText => _bagCount.text;
        public Button TileButton(int index) => _tiles[index].Button;
        public ItemInstance TileItem(int index) => _tiles[index].Item;
        public string DetailText => _detail.text;
        public Button EquipButton => _equip;
        public Button MeltButton => _melt;
        public string MeltText => _meltLabel.text;
        public Button InheritButton => _inherit;
        public string InheritText => _inheritLabel.text;
        public Button UpgradeButton => _upgrade;
        public ItemInstance BagPick => _bagPick;

        // RelicForgeView, as the forge starts: the two entry buttons beside the forge's nameplate. The panel is built the
        // first time one opens it.
        internal void Initialize(CombatSetup setup, Text forgeTitle, Text forgeLabelSource)
        {
            _setup = setup;
            var choices = FindFirstObjectByType<RunChoiceView>(FindObjectsInactive.Include);
            var screens = FindFirstObjectByType<ScreenArtView>(FindObjectsInactive.Include);
            _screens = screens != null && screens.Art != null && screens.Art.IsValid ? screens.Art : null;
            _cards = choices != null && choices.CardArt != null && choices.CardArt.IsValid ? choices.CardArt : null;
            _icons = choices != null ? choices.IconSet : null;
            _titleSource = choices != null ? choices.TitleLabel : forgeTitle;
            _nameSource = choices != null ? choices.FirstNameLabel : forgeLabelSource;
            _descriptionSource = choices != null ? choices.FirstDescriptionLabel : forgeLabelSource;
            _fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (forgeTitle != null)
            {
                // The title keeps the nameplate's 600 units in the row's middle (the plate is centred on it, so it stays
                // put); the two entries take the ends, clear of it.
                RectTransform titleRect = forgeTitle.rectTransform;
                titleRect.offsetMin = new Vector2(EntryOuter + EntryWidth + 14f, titleRect.offsetMin.y);
                titleRect.offsetMax = new Vector2(-(EntryOuter + EntryWidth + 14f), titleRect.offsetMax.y);
                BagEntry = Entry(forgeTitle, BagEntryName, TabNames[(int)Tab.Bag], true, () => Open(Tab.Bag));
                AnvilEntry = Entry(forgeTitle, AnvilEntryName, TabNames[(int)Tab.Anvil], false, () => Open(Tab.Anvil));
            }
        }

        // A button in the forge title's row, at its left or right end, clear of the 600-wide nameplate in its middle.
        private Button Entry(Text title, string name, string text, bool left, Action open)
        {
            RectTransform titleRect = title.rectTransform;
            var entry = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            entry.layer = title.gameObject.layer;
            var rect = (RectTransform)entry.transform;
            rect.SetParent(titleRect.parent, false);
            rect.SetSiblingIndex(titleRect.GetSiblingIndex() + 1);
            rect.anchorMin = new Vector2(left ? 0f : 1f, titleRect.anchorMin.y);
            rect.anchorMax = new Vector2(left ? 0f : 1f, titleRect.anchorMax.y);
            rect.pivot = new Vector2(left ? 0f : 1f, .5f);
            rect.offsetMin = new Vector2(left ? EntryOuter : -(EntryOuter + EntryWidth), 4f);
            rect.offsetMax = new Vector2(left ? EntryOuter + EntryWidth : -EntryOuter, -4f);
            var button = entry.GetComponent<Button>();
            button.targetGraphic = entry.GetComponent<Image>();
            Paint(button, false);
            Text label = Label(rect, name + " Label", _nameSource, 34, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, new Vector2(10f, 6f), new Vector2(-10f, -6f));
            label.text = text;
            button.onClick.AddListener(() => open());
            return button;
        }

        public void Open(Tab tab)
        {
            if (_setup == null || _setup.Anvil == null)
                return;
            if (_canvas == null)
                Build();
            _canvas.SetActive(true);
            if (!_subscribed)
            {
                _setup.Profile.Changed += OnProfileChanged;
                _subscribed = true;
            }
            if (_piece == null && _weaponId == null)
                SelectSlot(0);
            Show(tab == Tab.Forge ? Tab.Anvil : tab);
        }

        public void Close()
        {
            if (_hold != null)
                _hold.Release();
            if (_canvas != null)
                _canvas.SetActive(false);
        }

        // A tab of the panel's row: Forge closes the panel, the others show their page.
        public void Show(Tab tab)
        {
            if (tab == Tab.Forge)
            {
                Close();
                return;
            }
            Current = tab;
            ResetBagState();
            _inputEnabledAt = Time.unscaledTime + InputDelay;
            Refresh();
        }

        // Picks the piece in the equipped row: 0 the weapon the next run carries, 1-3 the armour, amulet and ring.
        public void SelectSlot(int slot)
        {
            if (slot == 0)
            {
                string weapon = EquippedWeaponId();
                _weaponId = weapon;
                _piece = weapon != null ? _setup.Profile.Gear.WeaponGear(weapon) : null;
            }
            else
            {
                ItemInstance item = _setup.Profile.Gear.Equipped((GearSlot)slot);
                if (item == null)
                    return;
                _piece = item;
                _weaponId = null;
            }
            _sealOn = _hammerOn = false;
            if (IsOpen)
                Refresh();
        }

        // Picks a bag item for the Anvil and Efsun tabs (the Bag's Upgrade).
        public void SelectItem(ItemInstance item)
        {
            if (item == null || !_setup.Profile.Gear.Owns(item))
                return;
            _piece = item;
            _weaponId = item.IsWeapon ? item.ItemId : null;
            _sealOn = _hammerOn = false;
            if (IsOpen)
                Refresh();
        }

        private string EquippedWeaponId() => _setup.Weapons != null && _setup.Weapons.Equipped != null ? _setup.Weapons.Equipped.Id : null;

        private void OnProfileChanged()
        {
            if (IsOpen)
                Refresh();
        }

        // ---- Building. ----

        private void Build()
        {
            // A root canvas of its own, in this view's scene but under nothing: the forge's view sits on the result's canvas,
            // and a canvas under it would be a nested one - its scaler ignored, its rect a default 100 x 100 inside the
            // forge (the first capture, 2026-11-17).
            _canvas = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            if (_canvas.scene != gameObject.scene && gameObject.scene.IsValid())
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_canvas, gameObject.scene);
            var canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            Image backdrop = Part(_canvas.transform, "Anvil Backdrop");
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            backdrop.color = new Color(.02f, .02f, .04f, .9f);
            backdrop.raycastTarget = true;

            var safe = new GameObject("Anvil Safe Area", typeof(RectTransform));
            safe.layer = _canvas.layer;
            safe.transform.SetParent(_canvas.transform, false);
            _safe = (RectTransform)safe.transform;
            Stretch(_safe, Vector2.zero, Vector2.zero);
            safe.AddComponent<SafeAreaFitter>();

            Image surface = Part(safe.transform, "Anvil Surface");
            _surface = surface.rectTransform;
            _surface.anchorMin = _surface.anchorMax = new Vector2(.5f, .5f);
            _surface.pivot = new Vector2(.5f, .5f);
            _surface.sizeDelta = new Vector2(SurfaceWidth, SurfaceHeight);
            surface.color = new Color(.09f, .08f, .11f, .97f);
            surface.raycastTarget = true;

            _title = Label(surface.transform, "Anvil Title", _titleSource, 56, TextAnchor.MiddleCenter);
            Row(_title.rectTransform, TitleTop, TitleHeight, 140f);
            _title.text = "The Anvil";
            if (_cards != null)
                _painter = new ChoiceCardPainter(_cards, backdrop, surface, _title);

            float tabWidth = (SurfaceWidth - 2 * Inset - 3 * 14f) / 4f;
            for (int i = 0; i < _tabs.Length; i++)
            {
                var tab = (Tab)i;
                Button button = MakeButton(surface.transform, "Anvil Tab " + TabNames[i], TabNames[i], 36, out Text label);
                Place((RectTransform)button.transform, Inset + i * (tabWidth + 14f), TabsTop, tabWidth, TabsHeight);
                button.onClick.AddListener(() => { if (Ready()) Show(tab); });
                _tabs[i] = button;
                _tabLabels[i] = label;
            }

            _wallet = Label(surface.transform, "Anvil Wallet", _descriptionSource, 32, TextAnchor.MiddleCenter);
            Row(_wallet.rectTransform, WalletTop, WalletHeight, Inset);
            _wallet.supportRichText = true;
            _boosters = Label(surface.transform, "Anvil Boosters", _descriptionSource, 28, TextAnchor.MiddleCenter);
            Row(_boosters.rectTransform, BoostersTop, BoostersHeight, Inset);

            // 30 apart, so the gold card's glow (14 units past its edge) never reaches its neighbour's.
            float pieceWidth = (SurfaceWidth - 2 * Inset - 3 * PieceGap) / 4f;
            for (int i = 0; i < _pieces.Length; i++)
            {
                int slot = i;
                _pieces[i] = MakePieceCard(surface.transform, "Anvil Piece " + GearText.SlotName((GearSlot)i),
                    Inset + i * (pieceWidth + PieceGap), PiecesTop, pieceWidth, PieceHeight, 30, true);
                _pieces[i].Button.onClick.AddListener(() => OnPiece(slot));
            }

            float pageHeight = SurfaceHeight - PageTop - BottomMargin;
            _anvilPage = Page(surface.transform, "Anvil Page", pageHeight);
            _efsunPage = Page(surface.transform, "Efsun Page", pageHeight);
            _bagPage = Page(surface.transform, "Bag Page", pageHeight);
            BuildAnvilPage((RectTransform)_anvilPage.transform, pageHeight);
            BuildEfsunPage((RectTransform)_efsunPage.transform, pageHeight);
            BuildBagPage((RectTransform)_bagPage.transform, pageHeight);
        }

        private void BuildAnvilPage(RectTransform page, float height)
        {
            float width = SurfaceWidth - 2 * Inset;
            _anvilCard = MakePieceCard(page, "Anvil Piece Card", 0f, 0f, width, 210f, 44, false);
            _mainStat = Label(page, "Anvil Main Stat", _descriptionSource, 34, TextAnchor.MiddleCenter);
            Row(_mainStat.rectTransform, 222f, 52f, 0f);
            _chanceCaption = Label(page, "Anvil Chance Caption", _descriptionSource, 28, TextAnchor.MiddleCenter);
            Row(_chanceCaption.rectTransform, 286f, 40f, 0f);
            _chance = Label(page, "Anvil Chance", _titleSource, 112, TextAnchor.MiddleCenter);
            Row(_chance.rectTransform, 322f, 132f, 0f);
            _breakdown = Label(page, "Anvil Chance Breakdown", _descriptionSource, 28, TextAnchor.MiddleCenter);
            Row(_breakdown.rectTransform, 456f, 44f, 0f);

            Image bar = Part(page, "Anvil Memory Bar");
            Place(bar.rectTransform, 80f, 512f, width - 160f, 30f);
            bar.color = new Color(.16f, .14f, .2f, 1f);
            _memoryFill = Part(bar.transform, "Anvil Memory Fill");
            Stretch(_memoryFill.rectTransform, Vector2.zero, Vector2.zero);
            _memoryFill.sprite = WhiteSprite();
            _memoryFill.type = Image.Type.Filled;
            _memoryFill.fillMethod = Image.FillMethod.Horizontal;
            _memoryFill.color = new Color32(214, 150, 70, 255);
            _memory = Label(page, "Anvil Memory", _descriptionSource, 26, TextAnchor.MiddleCenter);
            Row(_memory.rectTransform, 546f, 40f, 0f);

            _cost = Label(page, "Anvil Cost", _descriptionSource, 32, TextAnchor.MiddleCenter);
            Row(_cost.rectTransform, 600f, 56f, 0f);
            _cost.supportRichText = true;

            float half = (width - 20f) / 2f;
            _seal = MakeButton(page, "Anvil Seal Toggle", string.Empty, 30, out _sealLabel);
            Place((RectTransform)_seal.transform, 0f, 676f, half, 112f);
            _seal.onClick.AddListener(() => { if (Ready()) { _sealOn = !_sealOn; _hammerOn &= !_sealOn; Refresh(); } });
            _hammer = MakeButton(page, "Anvil Hammer Toggle", string.Empty, 30, out _hammerLabel);
            Place((RectTransform)_hammer.transform, half + 20f, 676f, half, 112f);
            _hammer.onClick.AddListener(() => { if (Ready()) { _hammerOn = !_hammerOn; _sealOn &= !_hammerOn; Refresh(); } });

            _strike = MakeButton(page, "Anvil Strike Button", string.Empty, 44, out _strikeLabel);
            Place((RectTransform)_strike.transform, 40f, height - 170f, width - 80f, 150f);
            Image fill = Part(_strike.transform, "Anvil Strike Fill");
            Stretch(fill.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));
            fill.sprite = WhiteSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(1f, .78f, .38f, .42f);
            fill.transform.SetSiblingIndex(0);
            _hold = _strike.gameObject.AddComponent<HoldToStrike>();
            _hold.Initialize(_strike, fill);
            _hold.Completed += OnStrike;

            // The strike's moment, over the chance.
            _momentPlate = Part(page, "Anvil Moment");
            _moment = _momentPlate.gameObject;
            Place(_momentPlate.rectTransform, 0f, 286f, width, 214f);
            _momentPlate.color = new Color(.06f, .05f, .08f, .94f);
            if (_screens != null)
            {
                _momentPlate.sprite = _screens.HeaderPlate;
                _momentPlate.type = Image.Type.Sliced;
                _momentPlate.color = Color.white;
            }
            _momentText = Label(_momentPlate.transform, "Anvil Moment Text", _titleSource, 60, TextAnchor.MiddleCenter);
            Stretch(_momentText.rectTransform, new Vector2(40f, 18f), new Vector2(-40f, -18f));
            _moment.SetActive(false);
        }

        private void BuildEfsunPage(RectTransform page, float height)
        {
            float width = SurfaceWidth - 2 * Inset;
            _efsunCard = MakePieceCard(page, "Efsun Piece Card", 0f, 0f, width, 170f, 40, false);
            float half = (width - 24f) / 2f;
            _halfWidth = half;
            _fullWidth = width;
            _currentHeader = Label(page, "Efsun Current Header", _nameSource, 32, TextAnchor.MiddleCenter);
            Place(_currentHeader.rectTransform, 0f, 190f, half, 46f);
            _newHeader = Label(page, "Efsun New Header", _nameSource, 32, TextAnchor.MiddleCenter);
            Place(_newHeader.rectTransform, half + 24f, 190f, half, 46f);
            for (int i = 0; i < EfsunRule.MaxLines; i++)
            {
                _current[i] = MakeLineRow(page, "Efsun Line " + i.ToString(CultureInfo.InvariantCulture), 0f, 246f + i * 82f, half);
                _shown[i] = MakeLineRow(page, "Efsun New Line " + i.ToString(CultureInfo.InvariantCulture), half + 24f, 246f + i * 82f, half);
            }
            _efsunNote = Label(page, "Efsun Note", _descriptionSource, 26, TextAnchor.MiddleCenter);
            Row(_efsunNote.rectTransform, 664f, 70f, 0f);

            float buttonsTop = height - 290f;
            _rerollGold = MakeButton(page, "Efsun Reroll Gold", string.Empty, 32, out _rerollGoldLabel);
            Place((RectTransform)_rerollGold.transform, 0f, buttonsTop, half, 126f);
            _rerollGold.onClick.AddListener(() => { if (Ready()) Reroll(false); });
            _rerollScroll = MakeButton(page, "Efsun Reroll Scroll", string.Empty, 32, out _rerollScrollLabel);
            Place((RectTransform)_rerollScroll.transform, half + 24f, buttonsTop, half, 126f);
            _rerollScroll.onClick.AddListener(() => { if (Ready()) Reroll(true); });
            _keepOld = MakeButton(page, "Efsun Keep Old", "Keep old", 36, out _);
            Place((RectTransform)_keepOld.transform, 0f, buttonsTop, half, 126f);
            _keepOld.onClick.AddListener(() => { if (Ready()) Resolve(false); });
            _takeNew = MakeButton(page, "Efsun Take New", "Take new", 36, out _);
            Place((RectTransform)_takeNew.transform, half + 24f, buttonsTop, half, 126f);
            _takeNew.onClick.AddListener(() => { if (Ready()) Resolve(true); });
            _rune = MakeButton(page, "Efsun Binding Rune", string.Empty, 32, out _runeLabel);
            Place((RectTransform)_rune.transform, 0f, buttonsTop + 146f, width, 126f);
            _rune.onClick.AddListener(() => { if (Ready()) AddRuneLine(); });
        }

        private void BuildBagPage(RectTransform page, float height)
        {
            float width = SurfaceWidth - 2 * Inset;
            _bagCount = Label(page, "Bag Count", _nameSource, 32, TextAnchor.MiddleLeft);
            Place(_bagCount.rectTransform, 8f, 0f, width - 16f, 46f);
            float tile = (width - (Columns - 1) * TileGap) / Columns;
            for (int i = 0; i < _tiles.Length; i++)
            {
                int index = i;
                int column = i % Columns, row = i / Columns;
                _tiles[i] = MakeTile(page, i, column * (tile + TileGap), 58f + row * (tile + TileGap), tile);
                _tiles[i].Button.onClick.AddListener(() => OnTile(index));
            }
            float gridBottom = 58f + 5 * tile + 4 * TileGap;
            _detail = Label(page, "Bag Detail", _descriptionSource, 28, TextAnchor.MiddleCenter);
            Row(_detail.rectTransform, gridBottom + 12f, height - 150f - gridBottom - 24f, 0f);
            float quarter = (width - 3 * 14f) / 4f;
            _equip = MakeButton(page, "Bag Equip", "Equip", 32, out _equipLabel);
            Place((RectTransform)_equip.transform, 0f, height - 140f, quarter, 130f);
            _equip.onClick.AddListener(() => { if (Ready()) EquipPick(); });
            _inherit = MakeButton(page, "Bag Inherit", "Inherit", 30, out _inheritLabel);
            Place((RectTransform)_inherit.transform, quarter + 14f, height - 140f, quarter, 130f);
            _inherit.onClick.AddListener(() => { if (Ready()) InheritPick(); });
            _melt = MakeButton(page, "Bag Melt", "Melt", 30, out _meltLabel);
            Place((RectTransform)_melt.transform, 2 * (quarter + 14f), height - 140f, quarter, 130f);
            _melt.onClick.AddListener(() => { if (Ready()) MeltPick(); });
            _upgrade = MakeButton(page, "Bag Upgrade", "Upgrade", 32, out _);
            Place((RectTransform)_upgrade.transform, 3 * (quarter + 14f), height - 140f, quarter, 130f);
            _upgrade.onClick.AddListener(() => { if (Ready() && _bagPick != null) { SelectItem(_bagPick); Show(Tab.Anvil); } });
        }

        // ---- Showing. ----

        private void Refresh()
        {
            if (_canvas == null)
                return;
            ProfileGear gear = _setup.Profile.Gear;
            // The piece may have gone (melted, inherited away) or come (the weapon's gear made by a strike).
            if (_piece != null && !gear.Owns(_piece))
            {
                _piece = null;
                _weaponId = null;
            }
            if (_piece == null && _weaponId != null)
                _piece = gear.WeaponGear(_weaponId);
            if (_piece == null && _weaponId == null)
            {
                _weaponId = EquippedWeaponId();
                _piece = _weaponId != null ? gear.WeaponGear(_weaponId) : null;
            }
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool active = i == (int)Current;
                Paint(_tabs[i], active);
                _tabLabels[i].color = active ? Bright : Muted;
            }
            _wallet.text = Wallet(gear);
            _boosters.text = Boosters(gear);
            for (int i = 0; i < _pieces.Length; i++)
                ShowPiece(i);
            _anvilPage.SetActive(Current == Tab.Anvil);
            _efsunPage.SetActive(Current == Tab.Efsun);
            _bagPage.SetActive(Current == Tab.Bag);
            if (Current == Tab.Anvil)
                ShowAnvil();
            else if (Current == Tab.Efsun)
                ShowEfsun();
            else
                ShowBag();
        }

        private string Wallet(ProfileGear gear)
        {
            var text = new System.Text.StringBuilder();
            text.Append("<color=#EEBF6C>Gold ").Append(GearText.Gold(_setup.Profile.Gold)).Append("</color>");
            for (int i = 0; i < GearCurrency.MaterialCount; i++)
            {
                var kind = (MaterialKind)i;
                text.Append("    <color=#").Append(ColorUtility.ToHtmlStringRGB(MaterialColours[i])).Append('>')
                    .Append(GearText.MaterialShortName(kind)).Append(' ')
                    .Append(gear.Material(kind).ToString(CultureInfo.InvariantCulture)).Append("</color>");
            }
            return text.ToString();
        }

        private static string Boosters(ProfileGear gear)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < GearCurrency.BoosterCount; i++)
            {
                var kind = (BoosterKind)i;
                if (i > 0)
                    text.Append("   ");
                text.Append(GearText.BoosterName(kind)).Append(' ').Append(gear.Booster(kind).ToString(CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        private void ShowPiece(int slot)
        {
            PieceCard card = _pieces[slot];
            ItemInstance piece;
            string id;
            if (slot == 0)
            {
                id = EquippedWeaponId();
                piece = id != null ? _setup.Profile.Gear.WeaponGear(id) : null;
            }
            else
            {
                piece = _setup.Profile.Gear.Equipped((GearSlot)slot);
                id = piece != null ? piece.ItemId : null;
            }
            bool empty = id == null;
            bool selected = !empty && (slot == 0 ? _weaponId == id && (_piece == null || _piece.IsWeapon) : _piece == piece);
            card.Level.text = empty ? string.Empty : GearText.Level(piece != null ? piece.Level : 0);
            card.Name.text = empty ? "Empty" : _setup.ItemDisplayName(id);
            card.Info.text = GearText.SlotName((GearSlot)slot);
            card.Name.color = selected ? Gold : empty ? Muted : Bright;
            card.Mark.enabled = selected && Current != Tab.Bag;
            card.Icon.sprite = empty ? null : IconFor((GearSlot)slot, id);
            card.Icon.enabled = card.Icon.sprite != null;
            card.Button.interactable = true;
            UpgradeRarity rarity = piece != null ? piece.Rarity : UpgradeRarity.Rare;
            PaintCard(slot, card, empty ? null : (UpgradeRarity?)rarity);
        }

        private void ShowAnvil()
        {
            GearSlot slot = _piece != null ? _piece.Slot : GearSlot.Weapon;
            string id = _piece != null ? _piece.ItemId : _weaponId;
            int level = _piece != null ? _piece.Level : 0;
            int pity = _piece != null ? _piece.Pity : 0;
            UpgradeRarity rarity = _piece != null ? _piece.Rarity : UpgradeRarity.Rare;
            ShowBigCard(_anvilCard, 4, id, slot, level, rarity);
            _mainStat.text = id == null ? string.Empty : GearText.MainStatStep(slot, level);
            ProfileGear gear = _setup.Profile.Gear;

            if (id == null || level >= AnvilTable.MaxLevel)
            {
                _chanceCaption.text = id == null ? string.Empty : "The anvil's work is done";
                _chance.text = id == null ? string.Empty : GearText.Level(AnvilTable.MaxLevel);
                _chance.color = Gold;
                _breakdown.text = string.Empty;
                _memoryFill.fillAmount = 0f;
                _memory.text = string.Empty;
                _cost.text = string.Empty;
                _seal.gameObject.SetActive(false);
                _hammer.gameObject.SetActive(false);
                _strikeLabel.text = id == null ? "Nothing to strike" : "+9: nothing more to strike";
                _strike.interactable = false;
                return;
            }

            int target = level + 1;
            bool certain = AnvilRule.ChancePercent(level, pity, 0, false) >= 100;
            bool sealUseful = !certain && gear.Booster(BoosterKind.ForgeSeal) > 0;
            bool hammerUseful = !certain && AnvilRule.HammerAllowed(level) && gear.Booster(BoosterKind.MastersHammer) > 0;
            _sealOn &= sealUseful;
            _hammerOn &= hammerUseful;
            int chance = AnvilRule.ChancePercent(level, pity, _sealOn ? 1 : 0, _hammerOn);
            _chanceCaption.text = "Chance to reach " + GearText.Level(target);
            _chance.text = GearText.Percent(chance);
            _chance.color = chance >= 100 ? Held : chance >= 60 ? Bright : chance >= 30 ? Gold : Short;
            _breakdown.text = Breakdown(level, pity, certain);

            int ceiling = AnvilTable.CeilingFailures(target);
            _memoryFill.fillAmount = ceiling > 0 ? Mathf.Clamp01(pity / (float)ceiling) : 0f;
            _memory.text = ceiling == 0 ? "This strike always holds"
                : pity >= ceiling ? "Anvil's memory is full: this strike holds"
                : string.Format(CultureInfo.InvariantCulture, "Anvil's memory {0} / {1}: holds for certain in {2}", pity, ceiling, ceiling - pity);

            int gold = AnvilTable.GoldCost(target);
            bool needs = AnvilTable.TryMaterialCost(target, out MaterialKind material, out int amount);
            bool goldShort = _setup.Profile.Gold < gold;
            bool materialShort = needs && gear.Material(material) < amount;
            string goldText = Colour(GearText.Gold(gold) + " gold", goldShort ? Short : Gold);
            _cost.text = needs
                ? goldText + "   " + Colour(string.Format(CultureInfo.InvariantCulture, "{0} {1} (have {2})", amount,
                    GearText.MaterialName(material), gear.Material(material)), materialShort ? Short : MaterialColours[(int)material])
                : goldText;

            _seal.gameObject.SetActive(sealUseful);
            _sealLabel.text = string.Format(CultureInfo.InvariantCulture, "{0} {1} x{2}\n+{3}% this strike", _sealOn ? "Using" : "Use",
                GearText.BoosterName(BoosterKind.ForgeSeal), gear.Booster(BoosterKind.ForgeSeal), AnvilTable.SealBonusPercent);
            Paint(_seal, _sealOn);
            _hammer.gameObject.SetActive(hammerUseful);
            _hammerLabel.text = string.Format(CultureInfo.InvariantCulture, "{0} {1} x{2}\nthis strike holds", _hammerOn ? "Using" : "Use",
                GearText.BoosterName(BoosterKind.MastersHammer), gear.Booster(BoosterKind.MastersHammer));
            Paint(_hammer, _hammerOn);

            bool canStrike = !goldShort && !materialShort && gear.PendingRerollPiece == null;
            _strike.interactable = canStrike;
            _strikeLabel.text = goldShort ? string.Format(CultureInfo.InvariantCulture, "Need {0} more gold", GearText.Gold(gold - _setup.Profile.Gold))
                : materialShort ? string.Format(CultureInfo.InvariantCulture, "Need {0} more {1}", amount - gear.Material(material), GearText.MaterialName(material))
                : gear.PendingRerollPiece != null ? "Keep or discard the reroll first"
                : "Hold to strike " + GearText.Level(target);
        }

        // "Base 30% + memory 6% + Seal 10%", or why the strike is certain.
        private string Breakdown(int level, int pity, bool certain)
        {
            int target = level + 1;
            if (_hammerOn)
                return "Master's Hammer: this strike cannot fail";
            if (certain)
                return AnvilTable.BaseChancePercent(target) >= 100 ? "Up to +3 every strike holds" : "The anvil's memory makes this strike certain";
            var text = new System.Text.StringBuilder("Base ").Append(GearText.Percent(AnvilTable.BaseChancePercent(target)));
            int memory = AnvilTable.PityStepPercent(target) * pity;
            if (memory > 0)
                text.Append("  +  memory ").Append(GearText.Percent(memory));
            if (_sealOn)
                text.Append("  +  Seal ").Append(GearText.Percent(AnvilTable.SealBonusPercent));
            text.Append("    (a miss keeps the level)");
            return text.ToString();
        }

        private void ShowEfsun()
        {
            ProfileGear gear = _setup.Profile.Gear;
            GearSlot slot = _piece != null ? _piece.Slot : GearSlot.Weapon;
            string id = _piece != null ? _piece.ItemId : _weaponId;
            UpgradeRarity rarity = _piece != null ? _piece.Rarity : UpgradeRarity.Rare;
            ShowBigCard(_efsunCard, 5, id, slot, _piece != null ? _piece.Level : 0, rarity);
            bool pendingHere = _piece != null && gear.PendingRerollPiece == _piece;
            bool pendingElsewhere = gear.PendingRerollPiece != null && !pendingHere;
            IReadOnlyList<EfsunLine> lines = _piece != null ? _piece.Lines : (IReadOnlyList<EfsunLine>)Array.Empty<EfsunLine>();
            int expected = _piece != null ? lines.Count : EfsunRule.WeaponLines;
            _currentHeader.text = pendingHere ? "Current" : "Lines";
            _newHeader.text = pendingHere ? "New" : string.Empty;
            // One column across the page, two (the old and the new) while a reroll is shown.
            float columnWidth = pendingHere ? _halfWidth : _fullWidth;
            _currentHeader.rectTransform.sizeDelta = new Vector2(columnWidth, _currentHeader.rectTransform.sizeDelta.y);
            for (int i = 0; i < EfsunRule.MaxLines; i++)
            {
                ((RectTransform)_current[i].Root.transform).sizeDelta = new Vector2(columnWidth, 72f);
                ShowLine(_current[i], i < lines.Count ? lines[i] : (EfsunLine?)null, i < expected && i >= lines.Count);
                ShowLine(_shown[i], pendingHere && i < gear.PendingRerollLines.Count ? gear.PendingRerollLines[i] : (EfsunLine?)null, false);
                _shown[i].Root.SetActive(pendingHere);
            }
            int price = EfsunRule.RerollGold(rarity);
            int scrolls = gear.Booster(BoosterKind.EfsunScroll);
            int runes = gear.Booster(BoosterKind.BindingRune);
            _efsunNote.text = id == null ? string.Empty
                : pendingHere ? "Keep the old lines or take the new: nothing is lost until you choose."
                : pendingElsewhere ? "Another piece's reroll waits for Keep old or Take new."
                : "A reroll draws every line again and shows them beside the old.";
            _rerollGold.gameObject.SetActive(!pendingHere);
            _rerollScroll.gameObject.SetActive(!pendingHere);
            _keepOld.gameObject.SetActive(pendingHere);
            _takeNew.gameObject.SetActive(pendingHere);
            _rerollGoldLabel.text = "Reroll\n" + GearText.Gold(price) + " gold";
            _rerollGold.interactable = id != null && !pendingElsewhere && _setup.Profile.Gold >= price;
            _rerollScrollLabel.text = string.Format(CultureInfo.InvariantCulture, "Reroll with an\nEfsun Scroll ({0})", scrolls);
            _rerollScroll.interactable = id != null && !pendingElsewhere && scrolls > 0;
            int count = _piece != null ? lines.Count : EfsunRule.WeaponLines;
            _runeLabel.text = count >= EfsunRule.MaxLines ? "Five lines: the most a piece can carry"
                : string.Format(CultureInfo.InvariantCulture, "Binding Rune ({0}): add a line", runes);
            _rune.interactable = id != null && !pendingHere && !pendingElsewhere && runes > 0 && count < EfsunRule.MaxLines;
        }

        private void ShowBag()
        {
            ProfileGear gear = _setup.Profile.Gear;
            if (_bagPick != null && !gear.Owns(_bagPick))
                ResetBagState();
            _bagCount.text = string.Format(CultureInfo.InvariantCulture, "Bag  {0} / {1}", gear.Bag.Count, ProfileGear.BagCapacity);
            for (int i = 0; i < _tiles.Length; i++)
            {
                Tile tile = _tiles[i];
                ItemInstance item = i < gear.Bag.Count ? gear.Bag[i] : null;
                tile.Item = item;
                tile.Icon.sprite = item != null ? IconFor(item.Slot, item.ItemId) : null;
                tile.Icon.enabled = tile.Icon.sprite != null;
                tile.Level.text = item != null ? GearText.Level(item.Level) : string.Empty;
                tile.Mark.enabled = item != null && (gear.IsEquipped(item) || item == _bagPick || item == _inheritTarget);
                tile.Mark.color = item == _bagPick ? Gold : item == _inheritTarget ? Held : new Color(.54f, .84f, .7f, 1f);
                bool target = _inheritSource == null || item == null || item == _inheritSource ||
                              (item.Slot == _inheritSource.Slot && item.Level < _inheritSource.Level);
                tile.Button.interactable = item != null && target;
                PaintTile(tile, item != null ? (UpgradeRarity?)item.Rarity : null);
            }
            _detail.text = Detail();
            bool picked = _bagPick != null;
            bool equipped = picked && gear.IsEquipped(_bagPick);
            _equipLabel.text = equipped ? "Equipped" : "Equip";
            _equip.interactable = picked && !equipped && _inheritSource == null;
            _melt.interactable = picked && !equipped && _inheritSource == null;
            _meltLabel.text = _meltArmed && picked
                ? string.Format(CultureInfo.InvariantCulture, "Melt: +{0} gold\n+{1} Ember", AnvilTable.MeltGold(_bagPick.Rarity), AnvilTable.MeltEmber(_bagPick.Rarity))
                : "Melt";
            _inheritLabel.text = _inheritSource == null ? "Inherit"
                : _inheritTarget == null ? "Cancel"
                : string.Format(CultureInfo.InvariantCulture, "Move {0}\n{1} gold", GearText.Level(_inheritSource.Level), GearText.Gold(AnvilRule.InheritGold(_inheritSource.Level)));
            _inherit.interactable = (picked && _bagPick.Level > 0) || _inheritSource != null;
            _upgrade.interactable = picked && _inheritSource == null;
        }

        private string Detail()
        {
            ProfileGear gear = _setup.Profile.Gear;
            if (_inheritSource != null)
            {
                return _inheritTarget == null
                    ? string.Format(CultureInfo.InvariantCulture, "Inherit {0} from {1}: tap a lower {2} to receive it.",
                        GearText.Level(_inheritSource.Level), _setup.ItemDisplayName(_inheritSource.ItemId), GearText.SlotName(_inheritSource.Slot))
                    : string.Format(CultureInfo.InvariantCulture, "{0} becomes {1} and {2} is used up. Tap Move to pay {3} gold.",
                        _setup.ItemDisplayName(_inheritTarget.ItemId), GearText.Level(_inheritSource.Level),
                        _setup.ItemDisplayName(_inheritSource.ItemId), GearText.Gold(AnvilRule.InheritGold(_inheritSource.Level)));
            }
            if (_bagPick == null)
                return gear.Bag.Count == 0 ? "Items drop from bosses, elites and the walk-out. Each is kept, even on a death."
                    : gear.IsBagFull ? "The bag is full: a new drop melts at once. Melt or inherit to make room."
                    : "Tap an item to equip, melt, inherit from or upgrade it.";
            var text = new System.Text.StringBuilder();
            text.Append(_setup.ItemDisplayName(_bagPick.ItemId)).Append("   ").Append(GearText.RarityName(_bagPick.Rarity)).Append(' ')
                .Append(GearText.SlotName(_bagPick.Slot)).Append("   ").Append(GearText.Level(_bagPick.Level));
            if (gear.IsEquipped(_bagPick))
                text.Append("   (equipped)");
            text.Append('\n').Append(GearText.MainStat(_bagPick.Slot, _bagPick.Level));
            for (int i = 0; i < _bagPick.Lines.Count; i++)
                text.Append(i == 0 ? "   |   " : ",  ").Append(GearText.Line(_bagPick.Lines[i]));
            return text.ToString();
        }

        private void ShowBigCard(PieceCard card, int paintSlot, string id, GearSlot slot, int level, UpgradeRarity rarity)
        {
            bool empty = id == null;
            card.Level.text = empty ? string.Empty : GearText.Level(level);
            card.Name.text = empty ? "Nothing equipped" : _setup.ItemDisplayName(id);
            card.Info.text = empty ? string.Empty : GearText.RarityName(rarity) + " " + GearText.SlotName(slot);
            card.Icon.sprite = empty ? null : IconFor(slot, id);
            card.Icon.enabled = card.Icon.sprite != null;
            card.Mark.enabled = false;
            PaintCard(paintSlot, card, empty ? null : (UpgradeRarity?)rarity);
        }

        private static void ShowLine(LineRow row, EfsunLine? line, bool unrolled)
        {
            row.Root.SetActive(true);
            if (line == null)
            {
                row.Text.text = unrolled ? "(drawn on the first touch)" : "-";
                row.Text.color = Muted;
                for (int p = 0; p < row.Pips.Length; p++)
                    row.Pips[p].enabled = false;
                return;
            }
            EfsunLine value = line.Value;
            row.Text.text = GearText.Line(value);
            row.Text.color = Bright;
            for (int p = 0; p < row.Pips.Length; p++)
            {
                row.Pips[p].enabled = true;
                row.Pips[p].color = p < value.Tier ? TierColour(value.Tier) : new Color(.25f, .23f, .3f, 1f);
            }
        }

        private static Color TierColour(int tier) => tier switch
        {
            5 => new Color32(255, 206, 92, 255),
            4 => new Color32(222, 92, 82, 255),
            3 => new Color32(110, 160, 230, 255),
            _ => new Color32(170, 174, 186, 255)
        };

        // ---- Acting. ----

        private void OnPiece(int slot)
        {
            if (!Ready())
                return;
            if (Current == Tab.Bag)
            {
                ItemInstance item = slot == 0 ? null : _setup.Profile.Gear.Equipped((GearSlot)slot);
                if (item != null)
                {
                    ResetBagState();
                    _bagPick = item;
                    Refresh();
                }
                return;
            }
            if (slot > 0 && _setup.Profile.Gear.Equipped((GearSlot)slot) == null)
            {
                Show(Tab.Bag);
                return;
            }
            SelectSlot(slot);
        }

        // The hold is full: strike, then show the moment.
        private void OnStrike()
        {
            if (!IsOpen || Current != Tab.Anvil)
                return;
            ItemInstance piece = _piece;
            if (piece == null && _weaponId != null)
                piece = _setup.Anvil.GearForWeapon(_weaponId);
            if (piece == null)
                return;
            _piece = piece;
            int before = piece.Level;
            bool seal = _sealOn, hammer = _hammerOn;
            _sealOn = _hammerOn = false;
            if (!_setup.Anvil.TryStrike(piece, seal, hammer, out AnvilStrike strike))
            {
                Refresh();
                return;
            }
            ShowMoment(strike.Success
                ? string.Format(CultureInfo.InvariantCulture, "{0}: the strike held!", GearText.Level(piece.Level))
                : string.Format(CultureInfo.InvariantCulture, "Missed: {0} kept\n<size=34>The anvil remembers: +1 memory</size>", GearText.Level(before)),
                strike.Success);
            Refresh();
        }

        private void ShowMoment(string text, bool success)
        {
            _momentText.supportRichText = true;
            _momentText.text = text;
            _momentText.color = success ? Held : Muted;
            _moment.SetActive(true);
            _moment.transform.SetAsLastSibling();
            _momentUntil = Time.unscaledTime + MomentSeconds;
            _momentPlate.rectTransform.localScale = success ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
        }

        private void Reroll(bool scroll)
        {
            ItemInstance piece = _piece;
            if (piece == null && _weaponId != null)
                piece = _setup.Anvil.GearForWeapon(_weaponId);
            if (piece == null)
                return;
            _piece = piece;
            _setup.Anvil.TryReroll(piece, scroll);
            Refresh();
        }

        private void Resolve(bool keepNew)
        {
            _setup.Anvil.ResolveReroll(keepNew);
            Refresh();
        }

        private void AddRuneLine()
        {
            ItemInstance piece = _piece;
            if (piece == null && _weaponId != null)
                piece = _setup.Anvil.GearForWeapon(_weaponId);
            if (piece == null)
                return;
            _piece = piece;
            _setup.Anvil.TryAddLine(piece);
            Refresh();
        }

        private void OnTile(int index)
        {
            if (!Ready())
                return;
            ItemInstance item = _tiles[index].Item;
            if (item == null)
                return;
            if (_inheritSource != null)
            {
                if (item == _inheritSource)
                    ResetBagState();
                else if (item.Slot == _inheritSource.Slot && item.Level < _inheritSource.Level)
                    _inheritTarget = item;
                Refresh();
                return;
            }
            _bagPick = item == _bagPick ? null : item;
            _meltArmed = false;
            Refresh();
        }

        private void EquipPick()
        {
            if (_bagPick != null)
                _setup.Anvil.TryEquip(_bagPick);
            Refresh();
        }

        // The first tap arms the melt and says what it pays; the second melts.
        private void MeltPick()
        {
            if (_bagPick == null)
                return;
            if (!_meltArmed)
            {
                _meltArmed = true;
                Refresh();
                return;
            }
            ItemInstance item = _bagPick;
            ResetBagState();
            _setup.Anvil.TryMelt(item);
            Refresh();
        }

        // Inherit: the first tap makes the picked item the source; a tap on a lower item of its slot picks the target; this
        // button then pays and moves the level. With no target yet it cancels.
        private void InheritPick()
        {
            if (_inheritSource == null)
            {
                if (_bagPick == null || _bagPick.Level == 0)
                    return;
                _inheritSource = _bagPick;
                _inheritTarget = null;
                _meltArmed = false;
                Refresh();
                return;
            }
            if (_inheritTarget == null)
            {
                ResetBagState();
                Refresh();
                return;
            }
            ItemInstance source = _inheritSource, target = _inheritTarget;
            ResetBagState();
            if (_setup.Anvil.TryInherit(source, target))
                _bagPick = target;
            Refresh();
        }

        // For tests and captures: picks a bag item as a tap on its tile would.
        public void PickBagItem(ItemInstance item)
        {
            for (int i = 0; i < _tiles.Length; i++)
                if (_tiles[i].Item == item)
                {
                    OnTile(i);
                    return;
                }
        }

        private void ResetBagState()
        {
            _bagPick = null;
            _meltArmed = false;
            _inheritSource = null;
            _inheritTarget = null;
        }

        private bool Ready() => IsOpen && Time.unscaledTime >= _inputEnabledAt;

        private void Update()
        {
            if (!IsOpen)
                return;
            FitToSafeArea();
            if (_painter != null)
                _painter.Tick(Time.unscaledTime);
            if (_moment != null && _moment.activeSelf)
            {
                float left = _momentUntil - Time.unscaledTime;
                if (left <= 0f)
                    _moment.SetActive(false);
                else
                {
                    float settle = Mathf.Clamp01((MomentSeconds - left) / .25f);
                    float scale = Mathf.Lerp(_momentPlate.rectTransform.localScale.x, 1f, settle);
                    _momentPlate.rectTransform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        // Fits the surface to the safe area the canvas measures now - the capture tool calls it after it redraws the
        // canvases at a phone's size, since Update would only fit them a frame later.
        public void FitNow() => FitToSafeArea();

        private void FitToSafeArea()
        {
            if (_safe == null || _surface == null)
                return;
            Rect area = _safe.rect;
            float scale = Mathf.Min(1f, Mathf.Min((area.height - 2f * FitMargin) / SurfaceHeight, area.width / SurfaceWidth));
            if (scale > 0f && !Mathf.Approximately(_surface.localScale.x, scale))
                _surface.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnDestroy()
        {
            if (_subscribed && _setup != null && _setup.Profile != null)
                _setup.Profile.Changed -= OnProfileChanged;
            if (_hold != null)
                _hold.Completed -= OnStrike;
            if (_white != null)
                Destroy(_white);
            if (_whiteSprite != null)
                Destroy(_whiteSprite);
            if (_canvas != null)
                Destroy(_canvas);
        }

        // ---- Parts. ----

        // The piece's painted card icon (UpgradeIconSet): a weapon's own, and for the dropped slots the card of the stat they
        // carry - armour, attack speed, crit.
        private Sprite IconFor(GearSlot slot, string id)
        {
            if (_icons == null)
                return null;
            switch (slot)
            {
                case GearSlot.Weapon:
                    return _icons.CardIconFor(id);
                case GearSlot.Armour:
                    return _icons.CardIconFor("upgrade_armor");
                case GearSlot.Amulet:
                    return _icons.CardIconFor("upgrade_attack_speed");
                default:
                    return _icons.CardIconFor("upgrade_crit");
            }
        }

        private PieceCard MakePieceCard(Transform parent, string name, float x, float y, float width, float height, int nameSize, bool compact)
        {
            var cardObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            cardObject.layer = parent.gameObject.layer;
            var rect = (RectTransform)cardObject.transform;
            rect.SetParent(parent, false);
            Place(rect, x, y, width, height);
            var card = new PieceCard { Button = cardObject.GetComponent<Button>(), Frame = cardObject.GetComponent<Image>() };
            card.Button.targetGraphic = card.Frame;
            card.Frame.color = new Color(.2f, .17f, .22f, 1f);

            card.Mark = Part(rect, name + " Selected");
            card.Mark.color = Gold;
            float iconSize = compact ? 100f : height - 64f;
            card.Icon = Part(rect, name + " Icon");
            card.Icon.preserveAspect = true;
            card.Level = Label(rect, name + " Level", _titleSource, compact ? 40 : 76, compact ? TextAnchor.UpperRight : TextAnchor.MiddleRight);
            card.Level.color = Gold;
            card.Name = Label(rect, name + " Name", _nameSource, nameSize, compact ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
            card.Info = Label(rect, name + " Info", _descriptionSource, compact ? 24 : 30, compact ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
            if (compact)
            {
                // The icon left of centre, the level badge at the top-right corner over the well's rim.
                PlaceCentred(card.Icon.rectTransform, -34f, 30f, iconSize, iconSize);
                Corner(card.Level.rectTransform, 14f, 14f, 92f, 52f);
                InCard(card.Name.rectTransform, 12f, height - 92f, 12f, 46f);
                InCard(card.Info.rectTransform, 12f, height - 44f, 12f, 30f);
                InCard(card.Mark.rectTransform, 28f, height - 10f, 28f, 6f);
            }
            else
            {
                card.Icon.rectTransform.anchorMin = card.Icon.rectTransform.anchorMax = new Vector2(0f, .5f);
                card.Icon.rectTransform.pivot = new Vector2(0f, .5f);
                card.Icon.rectTransform.anchoredPosition = new Vector2(36f, 0f);
                card.Icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                float left = 36f + iconSize + 32f;
                InCard(card.Name.rectTransform, left, 28f, 200f, height * .45f);
                InCard(card.Info.rectTransform, left, 28f + height * .45f, 200f, height * .3f);
                card.Level.rectTransform.anchorMin = new Vector2(1f, 0f);
                card.Level.rectTransform.anchorMax = new Vector2(1f, 1f);
                card.Level.rectTransform.pivot = new Vector2(1f, .5f);
                card.Level.rectTransform.offsetMin = new Vector2(-190f, 20f);
                card.Level.rectTransform.offsetMax = new Vector2(-34f, -20f);
                card.Mark.enabled = false;
            }
            return card;
        }

        private LineRow MakeLineRow(Transform parent, string name, float x, float y, float width)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            Place(rect, x, y, width, 72f);
            Image back = Part(rect, name + " Plate");
            Stretch(back.rectTransform, Vector2.zero, Vector2.zero);
            back.color = new Color(.13f, .12f, .16f, .9f);
            if (_cards != null && _cards.Description != null)
            {
                back.sprite = _cards.Description;
                back.type = Image.Type.Sliced;
                back.color = Color.white;
            }
            var row = new LineRow { Root = root, Pips = new Image[EfsunRule.TierCount] };
            row.Text = Label(rect, name + " Text", _descriptionSource, 28, TextAnchor.MiddleLeft);
            row.Text.rectTransform.anchorMin = Vector2.zero;
            row.Text.rectTransform.anchorMax = Vector2.one;
            row.Text.rectTransform.offsetMin = new Vector2(18f, 4f);
            row.Text.rectTransform.offsetMax = new Vector2(-18f - EfsunRule.TierCount * 18f, -4f);
            for (int p = 0; p < row.Pips.Length; p++)
            {
                Image pip = Part(rect, name + " Tier " + (p + 1).ToString(CultureInfo.InvariantCulture));
                RectTransform pr = pip.rectTransform;
                pr.anchorMin = pr.anchorMax = new Vector2(1f, .5f);
                pr.pivot = new Vector2(1f, .5f);
                pr.anchoredPosition = new Vector2(-14f - (EfsunRule.TierCount - 1 - p) * 18f, 0f);
                pr.sizeDelta = new Vector2(12f, 12f);
                pr.localRotation = Quaternion.Euler(0f, 0f, 45f);
                row.Pips[p] = pip;
            }
            return row;
        }

        private Tile MakeTile(Transform parent, int index, float x, float y, float size)
        {
            string name = "Bag Slot " + index.ToString("00", CultureInfo.InvariantCulture);
            var tileObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            tileObject.layer = parent.gameObject.layer;
            var rect = (RectTransform)tileObject.transform;
            rect.SetParent(parent, false);
            Place(rect, x, y, size, size);
            var tile = new Tile { Button = tileObject.GetComponent<Button>(), Frame = tileObject.GetComponent<Image>() };
            tile.Button.targetGraphic = tile.Frame;
            tile.Ground = Part(rect, name + " Ground");
            Stretch(tile.Ground.rectTransform, new Vector2(16f, 16f), new Vector2(-16f, -16f));
            tile.Icon = Part(rect, name + " Icon");
            Stretch(tile.Icon.rectTransform, new Vector2(22f, 22f), new Vector2(-22f, -22f));
            tile.Icon.preserveAspect = true;
            tile.Level = Label(rect, name + " Level", _titleSource, 30, TextAnchor.LowerRight);
            Stretch(tile.Level.rectTransform, new Vector2(8f, 6f), new Vector2(-10f, -6f));
            tile.Level.color = Gold;
            tile.Mark = Part(rect, name + " Mark");
            RectTransform mark = tile.Mark.rectTransform;
            mark.anchorMin = mark.anchorMax = new Vector2(0f, 1f);
            mark.pivot = new Vector2(0f, 1f);
            mark.anchoredPosition = new Vector2(12f, -12f);
            mark.sizeDelta = new Vector2(22f, 22f);
            return tile;
        }

        private GameObject Page(Transform surface, string name, float height)
        {
            var page = new GameObject(name, typeof(RectTransform));
            page.layer = surface.gameObject.layer;
            var rect = (RectTransform)page.transform;
            rect.SetParent(surface, false);
            Place(rect, Inset, PageTop, SurfaceWidth - 2 * Inset, height);
            return page;
        }

        private Button MakeButton(Transform parent, string name, string text, int size, out Text label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            Paint(button, false);
            label = Label(buttonObject.transform, name + " Label", _nameSource, size, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -8f));
            label.text = text;
            return button;
        }

        // A button on the forge's painted plates: primary for an active tab or a booster in use, secondary otherwise.
        private void Paint(Button button, bool primary)
        {
            var image = (Image)button.targetGraphic;
            if (_screens == null)
            {
                image.sprite = null;
                image.color = primary ? new Color(.46f, .34f, .16f, 1f) : new Color(.22f, .2f, .26f, 1f);
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

        // A piece card on its rarity's card (null: the forge's neutral card for an empty slot).
        private void PaintCard(int paintSlot, PieceCard card, UpgradeRarity? rarity)
        {
            if (_painter == null)
            {
                card.Frame.color = rarity == null ? new Color(.14f, .13f, .16f, 1f) : Swatch(rarity.Value);
                return;
            }
            ChoiceCardArtSet.Treatment look = rarity != null ? _cards.Tier(rarity.Value) : _cards.Neutral;
            _painter.Paint(paintSlot, card.Button, card.Name, null, card.Icon, look ?? _cards.Neutral);
            // No crest on these cards: the gold clasp stands above a card's top edge, into the row above it.
            Transform crest = card.Button.transform.Find(ChoiceCardPainter.CrestName);
            if (crest != null && crest.TryGetComponent(out Image crestImage))
                crestImage.enabled = false;
        }

        // A bag place: the rarity's frame, its well and its ground under the icon, thinner than a card's at this size;
        // an empty place the neutral frame.
        private void PaintTile(Tile tile, UpgradeRarity? rarity)
        {
            if (_cards == null)
            {
                tile.Frame.color = rarity == null ? new Color(.12f, .11f, .14f, 1f) : Swatch(rarity.Value);
                tile.Ground.enabled = false;
                return;
            }
            ChoiceCardArtSet.Treatment look = rarity != null ? _cards.Tier(rarity.Value) ?? _cards.Neutral : _cards.Neutral;
            tile.Frame.sprite = look.Frame;
            tile.Frame.type = Image.Type.Sliced;
            tile.Frame.pixelsPerUnitMultiplier = 2f;
            tile.Frame.color = rarity == null ? new Color(1f, 1f, 1f, .55f) : Color.white;
            Sprite ground = rarity != null ? look.Ground : null;
            tile.Ground.sprite = ground != null ? ground : look.Well;
            tile.Ground.type = ground != null ? Image.Type.Simple : Image.Type.Sliced;
            tile.Ground.pixelsPerUnitMultiplier = 2f;
            tile.Ground.enabled = tile.Ground.sprite != null;
            tile.Ground.color = rarity == null ? new Color(1f, 1f, 1f, .4f) : Color.white;
        }

        private static Color Swatch(UpgradeRarity rarity) => rarity switch
        {
            UpgradeRarity.Rare => new Color(.24f, .36f, .58f, 1f),
            UpgradeRarity.Epic => new Color(.56f, .2f, .2f, 1f),
            UpgradeRarity.Gold => new Color(.66f, .5f, .2f, 1f),
            _ => new Color(.36f, .36f, .38f, 1f)
        };

        private static string Colour(string text, Color colour) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(colour) + ">" + text + "</color>";

        private static string Join(LineRow[] rows)
        {
            var text = new System.Text.StringBuilder();
            foreach (LineRow row in rows)
            {
                if (row == null || !row.Root.activeSelf || row.Text.text == "-" || row.Text.text.Length == 0 || row.Text.color == Muted)
                    continue;
                if (text.Length > 0)
                    text.Append(" | ");
                text.Append(row.Text.text);
            }
            return text.ToString();
        }

        private Texture2D _white;
        private Sprite _whiteSprite;

        // A white sprite for the filled bars (Image.Type.Filled needs one).
        private Sprite WhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;
            _white = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "Anvil White" };
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            _white.SetPixels32(pixels);
            _white.Apply(false, true);
            _whiteSprite = Sprite.Create(_white, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 100f);
            return _whiteSprite;
        }

        private Image Part(Transform parent, string name)
        {
            var part = new GameObject(name, typeof(RectTransform));
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);
            var image = part.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private Text Label(Transform parent, string name, Text source, int size, TextAnchor alignment)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.AddComponent<Text>();
            label.font = source != null && source.font != null ? source.font : _fallback;
            label.fontStyle = source != null ? source.fontStyle : FontStyle.Normal;
            label.color = source != null ? source.color : Bright;
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

        // A rect at (x, y) from its parent's top-left, this wide and tall.
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // Centred across the parent at (offset x, y from the top).
        private static void PlaceCentred(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // The top-right corner, inset.
        private static void Corner(RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        // Across the parent between left and right insets, from top, this tall.
        private static void InCard(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Row(RectTransform rect, float top, float height, float inset) => InCard(rect, inset, top, inset, height);

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
