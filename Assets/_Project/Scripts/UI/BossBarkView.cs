using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Localisation;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Presentation only - the gate boss's lines (docs/44 section 1.8; docs/64 stage 1, 2026-11-23): the Forge Warden and the
    // Glass Regent bark as they walk out of the gate, with the first mark of each phase, and as they fall, each line a short
    // plate over the boss for BossBark.Seconds (fading in and out), the telegraph line at most once a phase (BossBarkMemory).
    // The plate is the HUD's own: the smoked band behind the boss's name (HudArtSet.BossNamePlate, a flat smoked band
    // without the painted HUD), the HUD's font and its ivory, the line in italics so it reads as speech; one line, or two at
    // the plate's widest, its size fitted to the line (the Turkish runs longer: docs/60).
    //
    // It never stands over the telegraph mark: while a mark is down, and through the blow's flash, the plate takes the first
    // of its places - over the head, under the feet, beside the body, above or below the mark - that keeps clear of the
    // Warden's circle or of every Regent lane (BossBark.ClearOfCircle, ClearOfLane), and with none clear it hides until one
    // is. It stays inside the free band between the HUD's panels and the screen's edges.
    //
    // Its own overlay canvas (over the HUD's, under every panel's), made once. It reads the slam service and the boss and
    // writes nothing to either; scaled time, so a pause or a panel holds a bark with the fight. After WardenSlamView (100), so
    // the mark is this frame's.
    [DefaultExecutionOrder(110)]
    public sealed class BossBarkView : MonoBehaviour
    {
        public const string CanvasName = "Boss Bark Canvas";
        // Over the HUD's canvas (0), under the level-up's (10), the result's (20) and the pause's (30).
        internal const int CanvasOrder = 1;
        // The plate, in the HUD's canvas units (1080 across): its line's size and smallest best fit, its widest, its padding.
        internal const int LineSize = 34;
        internal const int LineMinSize = 24;
        internal const float MaxWidth = 640f;
        internal const float PadX = 26f;
        internal const float PadY = 12f;
        // World units: the plate's gap over the head and under the feet, and beside the body.
        private const float HeadGap = .2f;
        private const float FeetGap = .3f;
        private const float SideGap = .8f;
        // The blow's flash after a mark lands (WardenSlamView.FlashSeconds), through which the plate still keeps clear.
        private const float FlashSeconds = .35f;
        private static readonly Color Ivory = new Color(.933f, .910f, .851f, 1f);
        private static readonly Color Smoke = new Color(.07f, .06f, .09f, .82f);

        private CombatSetup _setup;
        private WardenSlamService _service;
        private ArenaView _arena;
        private Camera _camera;
        private ArenaCameraFollow _framing;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _plate;
        private Image _plateImage;
        private Text _label;
        private CanvasGroup _group;
        private readonly TextGenerator _generator = new TextGenerator();
        private readonly Vector3[] _corners = new Vector3[4];
        private bool _paintedPlate;

        // The boss followed, its id, what it has said, and its death handler.
        private Health _boss;
        private string _bossId;
        private EnemyLookView _look;
        private readonly BossBarkMemory _memory = new BossBarkMemory();
        private System.Action _onDied;

        // The bark showing: its age, where it stands (the boss's feet and head height, or where it fell), on which floor.
        private bool _barking;
        private float _elapsed;
        private Health _speaker;
        private Vector3 _anchorFeet;
        private float _anchorTop;
        private int _barkFloor;

        // The mark the plate keeps clear of: the Warden's circle or the Regent's lanes as they went down, until the blow's
        // flash is over.
        private bool _markLive;
        private float _markUntil;
        private bool _markLanes;
        private float _markX, _markY, _markLift;
        private int _laneCount;
        private readonly float[] _laneX = new float[RegentRule.MaxLanes];
        private readonly float[] _laneY = new float[RegentRule.MaxLanes];
        private readonly float[] _laneDirX = new float[RegentRule.MaxLanes];
        private readonly float[] _laneDirY = new float[RegentRule.MaxLanes];
        private readonly float[] _laneLength = new float[RegentRule.MaxLanes];
        private float _laneHalfWidth;

        // For tests and the capture.
        public bool IsBarking => _barking;
        public BarkMoment LastMoment { get; private set; }
        public string LastLine { get; private set; }
        public int TelegraphsSaid => _memory.TelegraphsSaid;
        // How many barks of each moment this scene has shown.
        public int SpawnsSaid { get; private set; }
        public int TelegraphBarks { get; private set; }
        public int DeathsSaid { get; private set; }
        // The plate's opacity now (0 while hidden by a mark it could not clear, or with no bark).
        public float ShownAlpha => _group != null && _plate.gameObject.activeSelf ? _group.alpha : 0f;
        // Whether this frame's plate found no place clear of the mark and hid.
        public bool HiddenByMark { get; private set; }
        internal RectTransform Plate => _plate;
        internal Text Label => _label;
        internal bool UsesPaintedPlate => _paintedPlate;

        internal void Initialize(CombatSetup setup, WardenSlamService service)
        {
            if (_service != null)
                return;
            _setup = setup;
            _service = service;
            _arena = FindFirstObjectByType<ArenaView>();
            _camera = Camera.main;
            _framing = _camera != null ? _camera.GetComponent<ArenaCameraFollow>() : null;
            Build();
            service.Marked += OnMarked;
            service.Slammed += OnSlammed;
        }

        private void Build()
        {
            var canvasObject = new GameObject(CanvasName, typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = CanvasOrder;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            _canvasRect = (RectTransform)canvasObject.transform;

            var plate = new GameObject("Boss Bark", typeof(RectTransform));
            _plate = (RectTransform)plate.transform;
            _plate.SetParent(_canvasRect, false);
            _plate.anchorMin = _plate.anchorMax = _plate.pivot = new Vector2(.5f, .5f);
            _plateImage = plate.AddComponent<Image>();
            _plateImage.raycastTarget = false;
            _plateImage.color = Smoke;
            _group = plate.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var line = new GameObject("Boss Bark Line", typeof(RectTransform));
            var lineRect = (RectTransform)line.transform;
            lineRect.SetParent(_plate, false);
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.one;
            lineRect.offsetMin = new Vector2(PadX, PadY);
            lineRect.offsetMax = new Vector2(-PadX, -PadY);
            _label = line.AddComponent<Text>();
            _label.raycastTarget = false;
            _label.font = SceneFont();
            _label.fontSize = LineSize;
            _label.fontStyle = FontStyle.Italic;
            _label.color = Ivory;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = LineMinSize;
            _label.resizeTextMaxSize = LineSize;
            _label.supportRichText = false;
            plate.SetActive(false);
        }

        // The HUD's own font (docs/60 section 8: a label built in code copies a scene label's), or the built-in one.
        private static Font SceneFont()
        {
            foreach (Text text in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.font != null)
                    return text.font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // The smoked band behind the boss's name, once the painted HUD has laid it (HudArtView's Start); a flat band before.
        private void TakePlate()
        {
            if (_paintedPlate)
                return;
            HudArtView hud = FindFirstObjectByType<HudArtView>();
            if (hud == null || !hud.UsesPaintedArt || hud.Art == null || hud.Art.BossNamePlate == null)
                return;
            _plateImage.sprite = hud.Art.BossNamePlate;
            _plateImage.type = Image.Type.Simple;
            _plateImage.color = Color.white;
            _paintedPlate = true;
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.Marked -= OnMarked;
                _service.Slammed -= OnSlammed;
            }
            Unbind();
        }

        private void OnDisable()
        {
            _barking = false;
            if (_plate != null)
                _plate.gameObject.SetActive(false);
        }

        // A mark is down (the service's Update): keep clear of it, and the phase's first mark says the telegraph line.
        private void OnMarked()
        {
            Follow();
            CaptureMark();
            if (_boss != null && isActiveAndEnabled && _memory.Says(BarkMoment.Telegraph, _service.IsEnraged))
                Say(BarkMoment.Telegraph, _boss);
        }

        private void OnSlammed(bool hit) => _markUntil = _elapsedClock + FlashSeconds;

        // A clock of this view's own (scaled), for the flash after a blow.
        private float _elapsedClock;

        private void CaptureMark()
        {
            _markLive = true;
            _markUntil = float.PositiveInfinity;
            _markX = _service.MarkX;
            _markY = _service.MarkY;
            var p = new Vector3(_markX, ArenaFloor.WorldY(_markY), 0f);
            _markLift = _arena != null && _arena.IsFoundryRegion ? FoundrySurface.Lift(p, _arena.transform.position) : 0f;
            RegentLanes lanes = _service.Lanes;
            _markLanes = _service.MarksLanes && lanes.Count > 0;
            _laneCount = 0;
            if (!_markLanes)
                return;
            _laneHalfWidth = lanes.Width * .5f;
            for (int i = 0; i < lanes.Count && i < _laneX.Length; i++)
            {
                _laneX[i] = lanes.OriginX(i);
                _laneY[i] = lanes.OriginY(i);
                lanes.Direction(i, out _laneDirX[i], out _laneDirY[i]);
                _laneLength[i] = lanes.LengthOf(i);
                _laneCount++;
            }
        }

        // The gate's boss as the service reads it: bound as it walks out - its spawn line - and let go as it falls or the
        // fight changes.
        private void Follow()
        {
            Health boss = _service.Boss;
            if (ReferenceEquals(boss, _boss) || (boss == null && _boss == null))
                return;
            Unbind();
            if (boss == null)
                return;
            EnemyDefinition definition = _setup.Encounters.GateBossDefinition;
            _boss = boss;
            _bossId = definition != null ? definition.Id : null;
            _boss.TryGetComponent(out _look);
            _memory.Reset();
            _markLive = false;
            _boss.Died += _onDied ??= OnBossDied;
            if (isActiveAndEnabled && _memory.Says(BarkMoment.Spawn, false))
                Say(BarkMoment.Spawn, _boss);
        }

        private void Unbind()
        {
            if (!ReferenceEquals(_boss, null))
                _boss.Died -= _onDied;
            _boss = null;
            _look = null;
        }

        private void OnBossDied()
        {
            if (_boss == null)
                return;
            Health fallen = _boss;
            _markLive = false;
            if (isActiveAndEnabled && _memory.Says(BarkMoment.Death, false))
                Say(BarkMoment.Death, fallen);
            // The fallen boss speaks from where it fell.
            _speaker = null;
            Unbind();
        }

        // The boss says its line for the moment: a new bark over it, the one before cut short.
        private void Say(BarkMoment moment, Health speaker)
        {
            if (_bossId == null || !BossBark.TryLine(_bossId, moment, out LocText line))
                return;
            LastMoment = moment;
            if (moment == BarkMoment.Spawn)
                SpawnsSaid++;
            else if (moment == BarkMoment.Telegraph)
                TelegraphBarks++;
            else
                DeathsSaid++;
            Show(line.Get());
            _speaker = speaker;
            Anchor(speaker);
            _barkFloor = _setup.Encounters.FloorNumber;
        }

        // Lays the plate for a line and starts its bark.
        private void Show(string text)
        {
            TakePlate();
            LastLine = text;
            _group.alpha = 0f;
            // Active first, so the line is measured under its canvas's scale.
            _plate.gameObject.SetActive(true);
            Lay(text);
            _barking = true;
            _elapsed = 0f;
        }

        // The boss's line for a moment as a bark again, for a check of its placement through a mark (its memory and counts
        // untouched).
        internal void SayForCheck(BarkMoment moment)
        {
            if (_boss == null || _bossId == null || !BossBark.TryLine(_bossId, moment, out LocText line))
                return;
            Show(line.Get());
            _speaker = _boss;
            Anchor(_boss);
            _barkFloor = _setup.Encounters.FloorNumber;
        }

        // The line's plate for a check (the Turkish fit): laid as a bark lays it, at the screen's middle, shown at full.
        internal void ShowForCheck(LocText line)
        {
            Show(line.Get());
            _barking = false;
            _plate.anchoredPosition = Vector2.zero;
            _group.alpha = 1f;
        }

        // One line at the line's size if it fits the plate's widest, else wrapped at the widest: the plate sized round it.
        // Measured as TurkishScreensTests measures a label (the generator scales by the label's pixels per unit itself).
        private void Lay(string text)
        {
            _label.text = text;
            float ppu = _label.pixelsPerUnit > 0f ? _label.pixelsPerUnit : 1f;
            TextGenerationSettings settings = _label.GetGenerationSettings(Vector2.zero);
            settings.resizeTextForBestFit = false;
            settings.fontSize = LineSize;
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            float width = _generator.GetPreferredWidth(text, settings) / ppu;
            float inner = Mathf.Min(Mathf.Ceil(width) + 4f, MaxWidth - 2f * PadX);
            settings = _label.GetGenerationSettings(new Vector2(inner, 0f));
            settings.resizeTextForBestFit = false;
            settings.fontSize = LineSize;
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            float height = _generator.GetPreferredHeight(text, settings) / ppu;
            _plate.sizeDelta = new Vector2(inner + 2f * PadX, Mathf.Ceil(height) + 2f * PadY + 4f);
        }

        // Where the bark stands: the speaker's feet (lifted onto the foundry's surface) and the top of its body.
        private void Anchor(Health speaker)
        {
            if (speaker == null)
                return;
            Vector3 p = speaker.transform.position;
            if (_arena != null && _arena.IsFoundryRegion)
                p.y += FoundrySurface.Lift(p, _arena.transform.position);
            p.z = 0f;
            _anchorFeet = p;
            EnemyLookView look = speaker == _boss ? _look : speaker.GetComponent<EnemyLookView>();
            EnemyArtSet art = look != null ? look.PaintedArt : null;
            _anchorTop = art != null ? art.VisualTop : 1.6f;
        }

        private void LateUpdate()
        {
            if (_service == null)
                return;
            float dt = Time.deltaTime;
            _elapsedClock += dt;
            Follow();
            if (!_barking)
                return;
            _elapsed += dt;
            if (!BossBark.Shows(_elapsed) || _setup.Run.HasEnded || _setup.Encounters.FloorNumber != _barkFloor)
            {
                _barking = false;
                _plate.gameObject.SetActive(false);
                HiddenByMark = false;
                return;
            }
            if (_speaker != null && _speaker.IsAlive)
                Anchor(_speaker);
            bool placed = Place();
            HiddenByMark = !placed;
            _group.alpha = placed ? BossBark.Alpha(_elapsed) : 0f;
        }

        // Whether a mark is down now, or its blow's flash still shows.
        private bool MarkShown =>
            _markLive && (_service.IsFighting && _service.Clock.IsMarked || _elapsedClock < _markUntil);

        // The first of the plate's places that keeps clear of the mark (any, with none down), inside the free band; false
        // with none clear.
        private bool Place()
        {
            if (_camera == null)
                return false;
            if (_markLive && !MarkShown)
                _markLive = false;
            float unit = WorldPerCanvasUnit();
            float halfW = _plate.sizeDelta.x * .5f * unit, halfH = _plate.sizeDelta.y * .5f * unit;
            Band(out float left, out float right, out float bottom, out float top);
            float x = _anchorFeet.x, feet = _anchorFeet.y, head = feet + _anchorTop;
            float markTop = ArenaFloor.WorldY(_markY + WardenRule.SlamRadius + BossBark.MarkMargin) + _markLift;
            float markBottom = ArenaFloor.WorldY(_markY - WardenRule.SlamRadius - BossBark.MarkMargin) + _markLift;
            for (int place = 0; place < 9; place++)
            {
                float cx = x, cy;
                switch (place)
                {
                    case 0: cy = head + HeadGap + halfH; break;
                    case 1: cy = feet - FeetGap - halfH; break;
                    case 2: cx = x + SideGap + halfW; cy = feet + _anchorTop * .6f; break;
                    case 3: cx = x - SideGap - halfW; cy = feet + _anchorTop * .6f; break;
                    case 4: cx = x + SideGap + halfW; cy = head + HeadGap + halfH; break;
                    case 5: cx = x - SideGap - halfW; cy = head + HeadGap + halfH; break;
                    case 6: cy = markTop + halfH; break;
                    case 7: cy = markBottom - halfH; break;
                    default: cy = head + HeadGap + halfH + 2f * halfH; break;
                }
                if (!_markLive && place > 0)
                    break;
                cx = Mathf.Clamp(cx, left + halfW, Mathf.Max(left + halfW, right - halfW));
                cy = Mathf.Clamp(cy, bottom + halfH, Mathf.Max(bottom + halfH, top - halfH));
                if (_markLive && !ClearOfMark(cx - halfW, cx + halfW, cy - halfH, cy + halfH))
                    continue;
                Vector3 screen = _camera.WorldToScreenPoint(new Vector3(cx, cy, 0f));
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out Vector2 local))
                    _plate.anchoredPosition = local;
                return true;
            }
            return false;
        }

        // Whether a plate over [left, right] x [bottom, top] in world units keeps clear of the mark down.
        internal bool ClearOfMark(float left, float right, float bottom, float top)
        {
            if (!_markLanes)
                return BossBark.ClearOfCircle(left, right, bottom, top, _markLift, _markX, _markY, WardenRule.SlamRadius);
            for (int i = 0; i < _laneCount; i++)
                if (!BossBark.ClearOfLane(left, right, bottom, top, _markLift, _laneX[i], _laneY[i], _laneDirX[i],
                        _laneDirY[i], _laneLength[i], _laneHalfWidth))
                    return false;
            return true;
        }

        // The plate's corners in world units now (for checks): false while it is hidden.
        internal bool TryPlateWorldRect(out float left, out float right, out float bottom, out float top)
        {
            left = right = bottom = top = 0f;
            if (_plate == null || !_plate.gameObject.activeInHierarchy || _camera == null)
                return false;
            _plate.GetWorldCorners(_corners);
            Vector3 a = _camera.ScreenToWorldPoint(_corners[0]), b = _camera.ScreenToWorldPoint(_corners[2]);
            left = Mathf.Min(a.x, b.x);
            right = Mathf.Max(a.x, b.x);
            bottom = Mathf.Min(a.y, b.y);
            top = Mathf.Max(a.y, b.y);
            return true;
        }

        // How many world units one canvas unit spans on screen (the camera is orthographic).
        private float WorldPerCanvasUnit()
        {
            float scale = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            return Screen.height > 0 ? scale * 2f * _camera.orthographicSize / Screen.height : .01f;
        }

        // The free band in world units: between the HUD's panels (ArenaCameraFollow's rects, as the exit seal reads them),
        // a little inside the screen's sides.
        private void Band(out float left, out float right, out float bottom, out float top)
        {
            float low = .2f, high = .8f;
            if (_framing != null && _framing.TopHud != null && _framing.BottomHud != null && Screen.height > 0)
            {
                _framing.TopHud.GetWorldCorners(_corners);
                high = _corners[0].y / Screen.height - .01f;
                _framing.BottomHud.GetWorldCorners(_corners);
                low = _corners[1].y / Screen.height + .01f;
                if (high <= low)
                {
                    low = .2f;
                    high = .8f;
                }
            }
            Vector3 a = _camera.ViewportToWorldPoint(new Vector3(.03f, low, 0f));
            Vector3 b = _camera.ViewportToWorldPoint(new Vector3(.97f, high, 0f));
            left = a.x;
            bottom = a.y;
            right = b.x;
            top = b.y;
        }
    }
}
