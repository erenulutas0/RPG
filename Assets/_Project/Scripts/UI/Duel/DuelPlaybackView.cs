using System;
using System.Collections.Generic;
using Cryptforge.Arena;
using Cryptforge.Art;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The art one side of a duel is drawn with: the hero's rendered set (Ilya), else the painted Vanguard.
    internal readonly struct DuelSideArt
    {
        public readonly VanguardArtSet Painted;
        public readonly RenderedCharacterArtSet Rendered;

        public DuelSideArt(VanguardArtSet painted, RenderedCharacterArtSet rendered)
        {
            Painted = painted;
            Rendered = rendered;
        }
    }

    // The duel played from its log (docs/55 section 11), the heart of P0's verdict on whether watching is fun. The lane runs
    // across the portrait screen - its 12 units 960 UI units wide, the walls at its ends, the heroes entering at +-4 - with
    // the two heroes in the run's own art over coloured floor marks (the player's blue, the rival's red, so a mirror reads),
    // their health bars, names, boons and the round's score above, and under it the last skills used. Everything it draws
    // comes from DuelPlaybackState at the playback clock's tick (DuelTimeline, DuelPlaybackClock): each event crossed is
    // shown once - a damage number (gold for a crit or the Daggers' rhythm, amber for what a ward absorbed, orange for a
    // burn), a hit flash, a strike, a skill's call-out and its effect (the Burst's ring, the Vent's eruption, the Rush's
    // leap and impact, the guard's ring, the ward's bubble, the glyph on the floor where the log puts it, a daze, the burn's
    // stacks) - and the banner between rounds names the round and who took it. x1 or x2 and Skip; Watch again replays the
    // same log. Presentation only: it reads the log and never runs a duel.
    internal sealed class DuelPlaybackView
    {
        // The lane on the 1000-wide surface: its top, its height and the feet's line within it; 80 UI units a lane unit.
        public const float LaneTop = 340f;
        public const float LaneHeight = 1120f;
        public const float FloorY = 800f;
        public const float UnitWidth = 80f;
        public const float LaneCentre = 500f;
        private const int NumberCount = 24;
        private const int FlareCount = 12;
        private const int FeedLines = 3;
        private const float NumberTicks = 54f;
        private const float CalloutTicks = 66f;
        // A frame longer than this is played as this long, so a hitch never jumps the duel.
        private const float MaxFrameSeconds = .1f;

        private readonly ArenaUi _ui;
        private readonly RectTransform _page;
        private readonly RectTransform _lane;
        private readonly Text _round;
        private readonly Text _score;
        private readonly Text _banner;
        private readonly Text _bannerSub;
        private readonly Button _speed;
        private readonly Text _speedLabel;
        private readonly Button _skip;
        private readonly Text _skipLabel;
        private readonly SideView[] _sides = new SideView[2];
        private readonly Number[] _numbers = new Number[NumberCount];
        private readonly Flare[] _flares = new Flare[FlareCount];
        private readonly Text[] _feed = new Text[FeedLines];
        private readonly List<string> _feedLines = new List<string>(FeedLines);
        private readonly List<DuelEvent> _crossed = new List<DuelEvent>(64);
        private DuelPlaybackState _state;
        private DuelPlaybackClock _clock;
        private ArenaRules _rules = ArenaRules.Current;
        private int _numberCursor;
        private int _flareCursor;
        private int _shownRound = -1;
        private DuelPhase _shownPhase = DuelPhase.Done;
        private int _speedChoice = DuelPlaybackClock.Fast; // x2 first (the lead, 2026-11-19): a best of three at x1 is 60-80 s
        private string[] _names = new string[2];

        private sealed class SideView
        {
            public Text Name;
            public Text Identity;
            public Image BarTrail;
            public Image BarFill;
            public Text Health;
            public Text Boon;
            public Image Mark;
            public Image Guard;
            public Image Ward;
            public Image Sigil;
            public Image SigilRing;
            public Image[] Daze;
            public Image[] Burn;
            public Text Callout;
            public float CalloutAt = -1000f;
            public DuelFigure Figure;
            public float LastX;
            public float Trail = 1f;
            public int ShownHealth = -1;
            public int ShownBoon = -2;
        }

        private sealed class Number
        {
            public Text Text;
            public float At = -1000f;
            public Vector2 From;
        }

        private sealed class Flare
        {
            public Image Image;
            public float At = -1000f;
            public float Ticks;
            public Vector2 Centre;
            public Vector2 FromSize;
            public Vector2 ToSize;
            public Color Colour;
        }

        public DuelPlaybackView(ArenaUi ui, RectTransform page)
        {
            _ui = ui;
            _page = page;
            _round = ui.Label(page, "Arena Round", 52, TextAnchor.MiddleCenter, ArenaUi.Gold, FontStyle.Bold);
            ArenaUi.Place(_round.rectTransform, 300f, 16f, 400f, 64f);
            _score = ui.Label(page, "Arena Score", 72, TextAnchor.MiddleCenter, ArenaUi.Bright, FontStyle.Bold);
            ArenaUi.Place(_score.rectTransform, 350f, 78f, 300f, 84f);
            for (int side = 0; side < 2; side++)
                _sides[side] = BuildSide(side);

            _lane = ArenaUi.Node(page, "Arena Lane");
            ArenaUi.Place(_lane, 0f, LaneTop, 1000f, LaneHeight);
            Image floor = ui.Part(_lane, "Arena Lane Floor", new Color(.16f, .13f, .14f, 1f), ui.White);
            ArenaUi.Place(floor.rectTransform, LaneCentre - 6 * UnitWidth, FloorY - 40f, 12 * UnitWidth, 110f);
            Image edge = ui.Part(_lane, "Arena Lane Edge", new Color(.42f, .3f, .2f, 1f), ui.White);
            ArenaUi.Place(edge.rectTransform, LaneCentre - 6 * UnitWidth, FloorY - 44f, 12 * UnitWidth, 4f);
            for (int wall = -1; wall <= 1; wall += 2)
            {
                Image post = ui.Part(_lane, wall < 0 ? "Arena Lane West Wall" : "Arena Lane East Wall", new Color(.42f, .3f, .2f, 1f), ui.White);
                ArenaUi.Centre(post.rectTransform, LaneCentre + wall * 6 * UnitWidth, FloorY - 60f, 10f, 220f);
            }
            for (int side = 0; side < 2; side++)
            {
                SideView view = _sides[side];
                view.Sigil = ui.Part(_lane, "Arena Sigil " + side, new Color(1f, .38f, .12f, .38f), ui.Disc);
                view.SigilRing = ui.Part(_lane, "Arena Sigil Ring " + side, new Color(1f, .62f, .24f, .9f), ui.Ring);
                view.Mark = ui.Part(_lane, "Arena Mark " + side, side == 0 ? ArenaUi.Ally : ArenaUi.Foe, ui.Disc);
            }
            for (int i = 0; i < FlareCount; i++)
            {
                _flares[i] = new Flare { Image = ui.Part(_lane, "Arena Flare " + i, Color.white, ui.Ring) };
                _flares[i].Image.enabled = false;
            }

            _banner = ui.Label(page, "Arena Banner", 96, TextAnchor.MiddleCenter, ArenaUi.Gold, FontStyle.Bold);
            ArenaUi.Place(_banner.rectTransform, 40f, LaneTop + 120f, 920f, 130f);
            _banner.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .8f);
            _bannerSub = ui.Label(page, "Arena Banner Detail", 40, TextAnchor.UpperCenter, ArenaUi.Bright);
            ArenaUi.Place(_bannerSub.rectTransform, 40f, LaneTop + 256f, 920f, 120f);
            _bannerSub.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .8f);

            for (int i = 0; i < FeedLines; i++)
            {
                _feed[i] = ui.Label(page, "Arena Feed " + i, 34, TextAnchor.MiddleCenter, ArenaUi.Muted);
                ArenaUi.Place(_feed[i].rectTransform, 40f, LaneTop + LaneHeight + 8f + i * 46f, 920f, 44f);
            }

            _speed = ui.Button(page, "Arena Speed Button", 48, out _speedLabel, ToggleSpeed);
            ArenaUi.Place((RectTransform)_speed.transform, 20f, 1660f, 300f, 120f);
            _skip = ui.Button(page, "Arena Skip Button", 44, out _skipLabel, Skip);
            ArenaUi.Place((RectTransform)_skip.transform, 680f, 1660f, 300f, 120f);
        }

        public DuelPlaybackClock Clock => _clock;
        public DuelPlaybackState State => _state;
        public DuelMoment Moment { get; private set; }
        public bool Finished => _clock != null && _clock.Finished;
        public Button SpeedButton => _speed;
        public Button SkipButton => _skip;
        public string RoundText => _round.text;
        public string ScoreText => _score.text;
        public string BannerText => _banner.enabled ? _banner.text : string.Empty;
        public string HealthText(int side) => _sides[side].Health.text;
        public string BoonText(int side) => _sides[side].Boon.text;
        public string CalloutText(int side) => _sides[side].Callout.enabled ? _sides[side].Callout.text : string.Empty;
        public float HealthFill(int side) => _sides[side].BarFill.fillAmount;
        public string FeedText => string.Join("\n", _feedLines);
        public bool GuardShown(int side) => _sides[side].Guard.enabled;
        public bool WardShown(int side) => _sides[side].Ward.enabled;
        public bool SigilShown(int side) => _sides[side].Sigil.enabled;
        public bool DazeShown(int side) => _sides[side].Daze[0].enabled;
        public int BurnShown(int side)
        {
            int shown = 0;
            foreach (Image pip in _sides[side].Burn)
                shown += pip.enabled ? 1 : 0;
            return shown;
        }
        public DuelFigure Figure(int side) => _sides[side].Figure;
        // How many damage numbers and flares are on screen.
        public int NumbersShown
        {
            get
            {
                int shown = 0;
                foreach (Number number in _numbers)
                    shown += number != null && number.Text.enabled ? 1 : 0;
                return shown;
            }
        }

        private SideView BuildSide(int side)
        {
            var view = new SideView();
            float x = side == 0 ? 20f : 520f;
            TextAnchor anchor = side == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            Color colour = side == 0 ? ArenaUi.Ally : ArenaUi.Foe;
            view.Name = _ui.Label(_page, "Arena Name " + side, 44, anchor, colour, FontStyle.Bold);
            ArenaUi.Place(view.Name.rectTransform, x, 150f, 460f, 52f);
            view.Identity = _ui.Label(_page, "Arena Identity " + side, 30, anchor, ArenaUi.Muted);
            ArenaUi.Place(view.Identity.rectTransform, x, 200f, 460f, 38f);
            Image back = _ui.Part(_page, "Arena Bar " + side, new Color(.05f, .05f, .07f, 1f), _ui.White);
            ArenaUi.Place(back.rectTransform, x, 242f, 460f, 46f);
            view.BarTrail = _ui.Part(back.transform, "Arena Bar Trail " + side, new Color(.95f, .9f, .78f, .8f), _ui.White);
            ArenaUi.Stretch(view.BarTrail.rectTransform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            Fill(view.BarTrail, side);
            view.BarFill = _ui.Part(back.transform, "Arena Bar Fill " + side, side == 0 ? new Color(.3f, .62f, 1f, 1f) : new Color(.9f, .3f, .24f, 1f), _ui.White);
            ArenaUi.Stretch(view.BarFill.rectTransform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            Fill(view.BarFill, side);
            view.Health = _ui.Label(back.transform, "Arena Health " + side, 30, TextAnchor.MiddleCenter, ArenaUi.Bright, FontStyle.Bold);
            ArenaUi.Stretch(view.Health.rectTransform, Vector2.zero, Vector2.zero);
            view.Health.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .9f);
            view.Boon = _ui.Label(_page, "Arena Boon " + side, 30, anchor, ArenaUi.Gold);
            ArenaUi.Place(view.Boon.rectTransform, x, 292f, 460f, 40f);
            return view;
        }

        private static void Fill(Image image, int side)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = side == 0 ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
            image.fillAmount = 1f;
        }

        // A new duel to play: the log, the two sides' names, their identities (hero and weapon) and art. The speed chosen
        // for the last duel stays.
        public void Play(DuelLog log, string[] names, string[] identities, string[] weaponIds, DuelSideArt[] art)
        {
            _state = new DuelPlaybackState(log);
            _clock = new DuelPlaybackClock(_state.Timeline.TotalTicks, _speedChoice);
            _names = (string[])names.Clone();
            for (int side = 0; side < 2; side++)
            {
                SideView view = _sides[side];
                view.Name.text = names[side];
                view.Identity.text = identities[side];
                if (view.Figure != null)
                    UnityEngine.Object.Destroy(view.Figure.Root.gameObject);
                RebuildEffects(view, side);
                view.Figure = new DuelFigure(_ui, _lane, side == 0 ? "Arena Player" : "Arena Rival", weaponIds[side], art[side].Painted,
                    art[side].Rendered);
                view.Trail = 1f;
                view.ShownHealth = -1;
                view.ShownBoon = -2;
            }
            Layer();
            Restart();
        }

        // The lane's draw order, back to front: the floor, the glyphs and the floor marks; the rival, then the player in
        // front of it where they meet; each side's ward, guard, daze and burn; the flares; the call-outs; the numbers.
        private void Layer()
        {
            _sides[1].Figure.Root.SetAsLastSibling();
            _sides[0].Figure.Root.SetAsLastSibling();
            foreach (SideView view in _sides)
            {
                view.Ward.transform.SetAsLastSibling();
                view.Guard.transform.SetAsLastSibling();
                foreach (Image dot in view.Daze)
                    dot.transform.SetAsLastSibling();
                foreach (Image pip in view.Burn)
                    pip.transform.SetAsLastSibling();
            }
            foreach (Flare flare in _flares)
                flare.Image.transform.SetAsLastSibling();
            foreach (SideView view in _sides)
                view.Callout.transform.SetAsLastSibling();
            foreach (Number number in _numbers)
                number.Text.transform.SetAsLastSibling();
        }

        // The pieces that stand over a hero, made once (Layer puts them in front of the figures).
        private void RebuildEffects(SideView view, int side)
        {
            if (view.Guard != null)
                return;
            view.Guard = _ui.Part(_lane, "Arena Guard " + side, new Color(.62f, .8f, 1f, .95f), _ui.Ring);
            view.Ward = _ui.Part(_lane, "Arena Ward " + side, new Color(1f, .55f, .2f, .32f), _ui.Disc);
            view.Daze = new Image[3];
            for (int i = 0; i < view.Daze.Length; i++)
                view.Daze[i] = _ui.Part(_lane, "Arena Daze " + side + " " + i, new Color(1f, .9f, .4f, 1f), _ui.Disc);
            view.Burn = new Image[DuelRules.BurnMaxStacks];
            for (int i = 0; i < view.Burn.Length; i++)
                view.Burn[i] = _ui.Part(_lane, "Arena Burn " + side + " " + i, new Color(1f, .5f, .12f, 1f), _ui.Disc);
            view.Callout = _ui.Label(_lane, "Arena Callout " + side, 40, TextAnchor.MiddleCenter, ArenaUi.Gold, FontStyle.Bold);
            view.Callout.horizontalOverflow = HorizontalWrapMode.Overflow;
            view.Callout.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .9f);
            if (_numbers[0] == null)
            {
                for (int i = 0; i < NumberCount; i++)
                {
                    Text text = _ui.Label(_lane, "Arena Number " + i, 52, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.resizeTextForBestFit = false;
                    text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .9f);
                    ArenaUi.Centre(text.rectTransform, 0f, 0f, 240f, 80f);
                    text.enabled = false;
                    _numbers[i] = new Number { Text = text };
                }
            }
        }

        // From the top: the first round's banner, the bars full, the feed empty.
        public void Restart()
        {
            if (_clock == null)
                return;
            _clock.Restart();
            _feedLines.Clear();
            foreach (Number number in _numbers)
            {
                number.At = -1000f;
                number.Text.enabled = false;
            }
            foreach (Flare flare in _flares)
            {
                flare.At = -1000f;
                flare.Image.enabled = false;
            }
            foreach (SideView view in _sides)
            {
                view.CalloutAt = -1000f;
                view.Trail = 1f;
            }
            _shownRound = -1;
            _shownPhase = DuelPhase.Done;
            _state.AdvanceTo(0, -1);
            Moment = _state.Timeline.At(0);
            for (int side = 0; side < 2; side++)
            {
                _sides[side].Figure.Begin(side == 0 ? 1 : -1);
                _sides[side].LastX = (float)_state.Position(side);
            }
            Render(0f);
        }

        public void ToggleSpeed()
        {
            if (_clock == null)
                return;
            _clock.ToggleSpeed();
            _speedChoice = _clock.Speed;
            Render(0f);
        }

        public void Skip()
        {
            if (_clock == null)
                return;
            _clock.Skip();
            _state.AdvanceToEnd();
            Moment = _state.Timeline.At(_clock.Ticks);
        }

        // One frame of real time (unscaled seconds): the clock moves at its speed, the log is played to the clock's moment
        // and every event crossed is shown once.
        public void Tick(float seconds)
        {
            if (_clock == null || _clock.Finished)
                return;
            Played((float)_clock.Advance(Mathf.Min(seconds, MaxFrameSeconds)) / DuelTimeline.TicksPerSecond);
        }

        // To a moment of the time line (playback ticks), frame by frame at 60 a second as a phone would play it, so what
        // stands there - the numbers, the flares, the call-outs - is what the player sees then: the captures' frames.
        public void Seek(double playbackTicks)
        {
            if (_clock == null)
                return;
            if (playbackTicks < _clock.Ticks)
                Restart();
            while (_clock.Ticks < playbackTicks - 1e-6 && !_clock.Finished)
                Played((float)_clock.AdvanceTicks(Math.Min(1.0, playbackTicks - _clock.Ticks)) / DuelTimeline.TicksPerSecond);
        }

        private void Played(float played)
        {
            Moment = _state.Timeline.At(_clock.Ticks);
            _crossed.Clear();
            _state.AdvanceTo(Moment, _crossed);
            for (int i = 0; i < _crossed.Count; i++)
                Show(_crossed[i]);
            Render(played);
        }

        private float Now => _clock != null ? (float)_clock.Ticks : 0f;

        private void Show(DuelEvent e)
        {
            int side = e.Side;
            switch (e.Kind)
            {
                case DuelEventKind.RoundStart:
                    _sides[side].Figure.Begin(side == 0 ? 1 : -1);
                    _sides[side].LastX = (float)_state.Position(side);
                    _sides[side].Trail = 1f;
                    break;
                case DuelEventKind.Move:
                    if (e.Aux == 1)
                        Burst(Feet(e.Value / (float)DuelPlaybackState.MicroPerUnit), 40f, 220f, 14f, new Color(.85f, .85f, .9f, .9f), true);
                    break;
                case DuelEventKind.Strike:
                    _sides[side].Figure.Strike();
                    _sides[1 - side].Figure.Hit();
                    Blow(1 - side, e);
                    break;
                case DuelEventKind.Hit:
                    _sides[1 - side].Figure.Hit();
                    Blow(1 - side, e);
                    if (e.Effect == ArenaEffect.ShieldRush)
                        Burst(Chest(1 - side), 60f, 240f, 14f, new Color(1f, .95f, .8f, 1f), true);
                    else if (e.Effect == ArenaEffect.CrucibleVent)
                    {
                        Burst(Chest(1 - side), 80f, 300f, 24f, new Color(1f, .45f, .1f, .85f), false);
                        Burst(Chest(1 - side), 60f, 260f, 20f, new Color(1f, .7f, .3f, 1f), true);
                    }
                    break;
                case DuelEventKind.Burn:
                    ShowNumber(1 - side, DuelNumbers.Points(e.Value).ToString(), new Color(1f, .58f, .2f, 1f), 40);
                    break;
                case DuelEventKind.Skill:
                    _sides[side].Figure.Cast();
                    Callout(side, ArenaText.SkillName(e.Effect));
                    Feed(side, ArenaText.SkillName(e.Effect));
                    if (e.Effect == ArenaEffect.ForgeBurst)
                    {
                        ArenaSkill burst = _rules.Skill(ArenaRules.ForgeBurst);
                        float across = 2f * (burst != null ? burst.Radius / 1000f : 2.5f) * UnitWidth;
                        Burst(Chest(side), 60f, across, 20f, new Color(1f, .78f, .3f, 1f), true);
                    }
                    break;
                case DuelEventKind.Daze:
                    Callout(side, ArenaText.Dazed);
                    break;
                case DuelEventKind.RoundEnd:
                    for (int s = 0; s < 2; s++)
                    {
                        if (_state.Side(s).Health <= 0)
                            _sides[s].Figure.Fall();
                    }
                    break;
            }
        }

        // A blow's numbers over the victim: the health it took (gold and larger for a crit or the rhythm) and what a ward
        // absorbed beside it; a small spark where it landed.
        private void Blow(int victim, DuelEvent e)
        {
            bool crit = (e.Aux & 1) != 0 || (e.Aux & 2) != 0;
            if (e.Value > 0)
                ShowNumber(victim, DuelNumbers.Points(e.Value).ToString(), crit ? ArenaUi.Gold : Color.white, crit ? 66 : 50);
            if (e.Extra > 0)
                ShowNumber(victim, "(" + DuelNumbers.Points(e.Extra) + ")", new Color(1f, .74f, .4f, 1f), 40);
            Burst(Chest(victim), 30f, crit ? 150f : 100f, 8f, crit ? ArenaUi.Gold : new Color(1f, 1f, 1f, .9f), false);
        }

        private void ShowNumber(int side, string text, Color colour, int size)
        {
            Number number = _numbers[_numberCursor];
            _numberCursor = (_numberCursor + 1) % NumberCount;
            number.Text.text = text;
            number.Text.color = colour;
            number.Text.fontSize = size;
            number.At = Now;
            // Spread so blows a tick apart do not stack exactly: a fixed pattern, never a random draw.
            float spread = (_numberCursor % 5 - 2) * 26f;
            number.From = Head(side) + new Vector2(spread, 10f);
            number.Text.enabled = true;
        }

        private void Burst(Vector2 centre, float from, float to, float ticks, Color colour, bool ring)
        {
            Flare flare = _flares[_flareCursor];
            _flareCursor = (_flareCursor + 1) % FlareCount;
            flare.Image.sprite = ring ? _ui.Ring : _ui.Disc;
            flare.At = Now;
            flare.Ticks = ticks;
            flare.Centre = centre;
            flare.FromSize = new Vector2(from, from);
            flare.ToSize = new Vector2(to, to);
            flare.Colour = colour;
            flare.Image.enabled = true;
        }

        private void Callout(int side, string text)
        {
            SideView view = _sides[side];
            view.Callout.text = text;
            view.CalloutAt = Now;
        }

        private void Feed(int side, string skill)
        {
            string line = ArenaUi.Colour(_names[side], side == 0 ? ArenaUi.Ally : ArenaUi.Foe) + "   " + skill;
            _feedLines.Add(line);
            while (_feedLines.Count > FeedLines)
                _feedLines.RemoveAt(0);
        }

        // Lane positions: the feet at a lane x (units), the chest and the head of a side as drawn now.
        private static Vector2 Feet(float x) => new Vector2(LaneCentre + x * UnitWidth, -FloorY);
        private Vector2 Chest(int side) => _sides[side].Figure.Root.anchoredPosition + new Vector2(0f, _sides[side].Figure.Height * .5f);
        private Vector2 Head(int side) => _sides[side].Figure.Root.anchoredPosition + new Vector2(0f, _sides[side].Figure.Height + 20f);

        private void Render(float played)
        {
            DuelMoment moment = Moment;
            float now = Now;
            for (int side = 0; side < 2; side++)
            {
                SideView view = _sides[side];
                DuelSideState state = _state.Side(side);
                float x = (float)_state.Position(side, out double leap);
                view.Figure.Place(Feet(x), _state.Facing(side), (float)leap * 90f);
                view.Figure.Step(played, moment.Phase == DuelPhase.Fight ? x - view.LastX : 0f);
                view.LastX = x;
                view.Figure.Flash(_state.Flashing(side));
                Vector2 feet = Feet(x);
                Vector2 chest = Chest(side);
                Vector2 head = Head(side);

                ArenaUi.Centre(view.Mark.rectTransform, feet.x, FloorY, 170f, 44f);
                view.Mark.color = side == 0 ? new Color(ArenaUi.Ally.r, ArenaUi.Ally.g, ArenaUi.Ally.b, .55f) : new Color(ArenaUi.Foe.r, ArenaUi.Foe.g, ArenaUi.Foe.b, .55f);

                view.Guard.enabled = _state.Guarded(side);
                if (view.Guard.enabled)
                {
                    float pulse = 1f + .04f * Mathf.Sin(now * .25f);
                    ArenaUi.Centre(view.Guard.rectTransform, chest.x, -chest.y, 250f * pulse, 300f * pulse);
                }
                view.Ward.enabled = _state.Warded(side);
                if (view.Ward.enabled)
                    ArenaUi.Centre(view.Ward.rectTransform, chest.x, -chest.y, 290f, 330f);

                bool sigil = _state.Sigil(side);
                view.Sigil.enabled = view.SigilRing.enabled = sigil;
                if (sigil)
                {
                    ArenaSkill glyph = _rules.Skill(ArenaRules.FlameSigil);
                    float across = 2f * (glyph != null ? glyph.Radius / 1000f : 2.5f) * UnitWidth;
                    float sx = LaneCentre + (float)(state.SigilX / DuelPlaybackState.MicroPerUnit) * UnitWidth;
                    float glow = .8f + .2f * Mathf.Sin(now * .2f);
                    ArenaUi.Centre(view.Sigil.rectTransform, sx, FloorY, across, across * .26f);
                    ArenaUi.Centre(view.SigilRing.rectTransform, sx, FloorY, across, across * .26f);
                    view.Sigil.color = new Color(1f, .38f, .12f, .4f * glow);
                }

                bool dazed = _state.Dazed(side);
                for (int i = 0; i < view.Daze.Length; i++)
                {
                    view.Daze[i].enabled = dazed;
                    if (!dazed)
                        continue;
                    float angle = now * .18f + i * 2.094f;
                    ArenaUi.Centre(view.Daze[i].rectTransform, head.x + Mathf.Cos(angle) * 54f, -(head.y + 6f + Mathf.Sin(angle) * 12f), 22f, 22f);
                }

                for (int i = 0; i < view.Burn.Length; i++)
                {
                    bool lit = i < state.BurnStacks && moment.Phase != DuelPhase.Intro;
                    view.Burn[i].enabled = lit;
                    if (lit)
                    {
                        float flicker = 1f + .15f * Mathf.Sin(now * .5f + i);
                        ArenaUi.Centre(view.Burn[i].rectTransform, head.x + (i - (state.BurnStacks - 1) * .5f) * 26f, -(head.y + 34f), 22f * flicker, 28f * flicker);
                    }
                }

                float since = now - view.CalloutAt;
                view.Callout.enabled = since >= 0f && since < CalloutTicks;
                if (view.Callout.enabled)
                {
                    float rise = Mathf.Min(1f, since / 10f);
                    ArenaUi.Centre(view.Callout.rectTransform, Mathf.Clamp(head.x, 170f, 830f), -(head.y + 70f + 20f * rise), 340f, 56f);
                    Color c = view.Callout.color;
                    view.Callout.color = new Color(c.r, c.g, c.b, since > CalloutTicks * .7f ? 1f - (since - CalloutTicks * .7f) / (CalloutTicks * .3f) : 1f);
                }

                // The bar: the fill at once, the trail following it down.
                float share = (float)state.HealthShare;
                view.BarFill.fillAmount = share;
                view.Trail = share >= view.Trail ? share : Mathf.MoveTowards(view.Trail, share, played * .9f + .002f);
                view.BarTrail.fillAmount = view.Trail;
                int health = DuelNumbers.Points(Mathf.Max(0, state.Health));
                if (health != view.ShownHealth)
                {
                    view.ShownHealth = health;
                    view.Health.text = health + " / " + DuelNumbers.Points(state.MaxHealth);
                }
                if (state.Boon != view.ShownBoon)
                {
                    view.ShownBoon = state.Boon;
                    view.Boon.text = state.Boon >= 0 && state.Boon < _rules.Boons.Count ? ArenaText.Boon(_rules.Boons[state.Boon]) : string.Empty;
                }
            }

            foreach (Number number in _numbers)
            {
                float age = now - number.At;
                number.Text.enabled = age >= 0f && age < NumberTicks;
                if (!number.Text.enabled)
                    continue;
                float p = age / NumberTicks;
                number.Text.rectTransform.anchoredPosition = number.From + new Vector2(0f, 120f * Mathf.Sqrt(p));
                Color c = number.Text.color;
                number.Text.color = new Color(c.r, c.g, c.b, p < .6f ? 1f : 1f - (p - .6f) / .4f);
            }
            foreach (Flare flare in _flares)
            {
                float age = now - flare.At;
                flare.Image.enabled = age >= 0f && age < flare.Ticks;
                if (!flare.Image.enabled)
                    continue;
                float p = age / flare.Ticks;
                Vector2 size = Vector2.Lerp(flare.FromSize, flare.ToSize, 1f - (1f - p) * (1f - p));
                flare.Image.rectTransform.anchorMin = flare.Image.rectTransform.anchorMax = new Vector2(0f, 1f);
                flare.Image.rectTransform.pivot = new Vector2(.5f, .5f);
                flare.Image.rectTransform.anchoredPosition = flare.Centre;
                flare.Image.rectTransform.sizeDelta = size;
                flare.Image.color = new Color(flare.Colour.r, flare.Colour.g, flare.Colour.b, flare.Colour.a * (1f - p));
            }

            for (int i = 0; i < FeedLines; i++)
            {
                string line = i < _feedLines.Count ? _feedLines[i] : string.Empty;
                if (_feed[i].text != line)
                    _feed[i].text = line;
            }

            if (moment.Round != _shownRound || moment.Phase != _shownPhase)
            {
                _shownRound = moment.Round;
                _shownPhase = moment.Phase;
                _round.text = ArenaText.Round(moment.Round + 1);
                _score.text = _state.Side(0).Wins + " - " + _state.Side(1).Wins;
                _banner.enabled = _bannerSub.enabled = moment.Phase == DuelPhase.Intro || moment.Phase == DuelPhase.Outro || moment.Phase == DuelPhase.Done;
                if (moment.Phase == DuelPhase.Intro)
                {
                    _banner.text = ArenaText.Round(moment.Round + 1);
                    _bannerSub.text = BoonLine(0) + "\n" + BoonLine(1);
                }
                else if (moment.Phase != DuelPhase.Fight)
                {
                    int winner = _state.Timeline.RoundWinner(moment.Round);
                    _banner.text = winner < 0 ? ArenaText.RoundDrawn : winner == 0 ? ArenaText.RoundToYou : ArenaText.RoundTo(_names[winner]);
                    _bannerSub.text = string.Empty;
                }
            }
            string speed = ArenaText.Speed(_clock != null ? _clock.Speed : _speedChoice);
            if (_speedLabel.text != speed)
                _speedLabel.text = speed;
            string skip = ArenaText.Skip;
            if (_skipLabel.text != skip)
                _skipLabel.text = skip;
        }

        private string BoonLine(int side)
        {
            int boon = _state.Side(side).Boon;
            if (boon < 0 || boon >= _rules.Boons.Count)
                return string.Empty;
            return ArenaUi.Colour(_names[side], side == 0 ? ArenaUi.Ally : ArenaUi.Foe) + "  " + ArenaText.Boon(_rules.Boons[boon]);
        }

        // The language changed: the words written once are written again.
        public void Relabel()
        {
            _shownRound = -1;
            foreach (SideView view in _sides)
                view.ShownBoon = -2;
            if (_clock != null)
                Render(0f);
        }
    }
}
