using System;
using System.Collections.Generic;
using Cryptforge.Arena;

namespace Cryptforge.UI
{
    // Where the playback stands within a round: the banner before its first tick, the fight itself, the round's end held,
    // or the whole match played out.
    public enum DuelPhase
    {
        Intro = 0,
        Fight = 1,
        Outro = 2,
        Done = 3
    }

    // A moment of the playback: the round, its phase and the duel's tick within the round (fractional while the fight
    // plays; -1 in the intro, before the round's first tick; the round's last tick in the outro and when done).
    public readonly struct DuelMoment
    {
        public readonly int Round;
        public readonly DuelPhase Phase;
        public readonly double Tick;

        public DuelMoment(int round, DuelPhase phase, double tick)
        {
            Round = round;
            Phase = phase;
            Tick = tick;
        }
    }

    // The playback's time line (docs/55 section 11): the log's rounds laid end to end, each with a banner before it
    // (IntroTicks: the round's number and the boons dealt) and its end held after it (OutroTicks: who took it), the fight
    // between them at the duel's own 60 ticks a second. Read from the log alone - each round's length is its RoundEnd's
    // ticks - so it never runs a duel. Pure: the view, the tests and the captures share it.
    public sealed class DuelTimeline
    {
        public const int TicksPerSecond = DuelRules.TicksPerSecond;
        public const int IntroTicks = 72;
        public const int OutroTicks = 96;

        private readonly List<int> _ticks = new List<int>(DuelRules.MaxRounds);
        private readonly List<int> _winners = new List<int>(DuelRules.MaxRounds);
        private readonly List<int> _starts = new List<int>(DuelRules.MaxRounds);

        public DuelTimeline(DuelLog log)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));
            int start = 0;
            for (int i = 0; i < log.Count; i++)
            {
                DuelEvent e = log[i];
                if (e.Kind != DuelEventKind.RoundEnd)
                    continue;
                _starts.Add(start);
                _ticks.Add(e.Aux);
                _winners.Add(e.Side == 2 ? -1 : e.Side);
                start += IntroTicks + e.Aux + OutroTicks;
            }
            TotalTicks = start;
        }

        public int RoundCount => _ticks.Count;

        // The playback's whole length in ticks: every round's banner, fight and end.
        public int TotalTicks { get; }

        // The duel's ticks in a round (its RoundEnd's).
        public int RoundTicks(int round) => _ticks[round];

        // The round's winner: 0 the player (side A), 1 the rival (side B), -1 a drawn round.
        public int RoundWinner(int round) => _winners[round];

        // Where the round's banner begins on the time line.
        public int RoundStart(int round) => _starts[round];

        public DuelMoment At(double playbackTicks)
        {
            if (_ticks.Count == 0)
                return new DuelMoment(0, DuelPhase.Done, 0);
            if (playbackTicks >= TotalTicks)
            {
                int last = _ticks.Count - 1;
                return new DuelMoment(last, DuelPhase.Done, _ticks[last]);
            }
            double at = Math.Max(0, playbackTicks);
            int round = 0;
            while (round < _ticks.Count - 1 && at >= _starts[round + 1])
                round++;
            double within = at - _starts[round];
            if (within < IntroTicks)
                return new DuelMoment(round, DuelPhase.Intro, -1);
            within -= IntroTicks;
            if (within < _ticks[round])
                return new DuelMoment(round, DuelPhase.Fight, within);
            return new DuelMoment(round, DuelPhase.Outro, _ticks[round]);
        }
    }

    // The playback's clock: real seconds into the time line's ticks at 60 a second, times the speed (x1 or x2; the speed
    // button toggles it), never past the end; Skip goes to the end at once. Pure: the view feeds it unscaled time.
    public sealed class DuelPlaybackClock
    {
        public const int Normal = 1;
        public const int Fast = 2;

        public DuelPlaybackClock(int totalTicks, int speed = Normal)
        {
            if (totalTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(totalTicks));
            TotalTicks = totalTicks;
            Speed = speed == Fast ? Fast : Normal;
        }

        public int TotalTicks { get; }
        public int Speed { get; private set; }
        public double Ticks { get; private set; }
        public bool Skipped { get; private set; }
        public bool Finished => Ticks >= TotalTicks;

        // Real seconds to playback ticks at the current speed.
        public double Advance(double seconds)
        {
            if (!(seconds > 0) || Finished)
                return 0;
            double before = Ticks;
            Ticks = Math.Min(TotalTicks, Ticks + seconds * DuelTimeline.TicksPerSecond * Speed);
            return Ticks - before;
        }

        // Playback ticks straight on, whatever the speed (a capture's or a test's frame).
        public double AdvanceTicks(double ticks)
        {
            if (!(ticks > 0) || Finished)
                return 0;
            double before = Ticks;
            Ticks = Math.Min(TotalTicks, Ticks + ticks);
            return Ticks - before;
        }

        public void ToggleSpeed() => Speed = Speed == Fast ? Normal : Fast;

        public void Skip()
        {
            Skipped = true;
            Ticks = TotalTicks;
        }

        // Back to the start, at the speed it has (Watch again).
        public void Restart()
        {
            Skipped = false;
            Ticks = 0;
        }
    }

    // One side as the log tells it at the playback's tick: its arena health, where it stands and walks, the boon it was
    // dealt, its burn stacks, the windows its guard, ward, glyph and daze last, and when it last struck, was struck, used
    // a skill or leapt - everything the view draws, from the log alone.
    public sealed class DuelSideState
    {
        public int MaxHealth;
        public int Health;
        // Where it stood at Since (micro-units) and its step a tick from there.
        public long X;
        public long Step;
        public int Since;
        // The boon dealt this round (the rules' index), -1 none.
        public int Boon = -1;
        public int BurnStacks;
        // The first tick each window no longer covers; -1 never opened this round.
        public int GuardUntil = -1;
        public int WardUntil = -1;
        public int DazedUntil = -1;
        public int SigilUntil = -1;
        public long SigilX;
        // A Shield Rush's leap: the tick it was used and where it left from (micro-units); -1 none this round.
        public int LeapTick = -1;
        public long LeapFrom;
        public int StruckTick = -1;
        public int StrikeTick = -1;
        public int SkillTick = -1;
        public ArenaEffect LastSkill;
        // Rounds taken so far in the match.
        public int Wins;

        public double HealthShare => MaxHealth > 0 ? Math.Max(0, Math.Min(1.0, Health / (double)MaxHealth)) : 0;

        internal void Begin(DuelEvent e)
        {
            MaxHealth = e.Value;
            Health = e.Value;
            X = e.Extra;
            Step = 0;
            Since = 0;
            Boon = e.Aux - 1;
            BurnStacks = 0;
            GuardUntil = WardUntil = DazedUntil = SigilUntil = -1;
            SigilX = 0;
            LeapTick = -1;
            LeapFrom = 0;
            StruckTick = StrikeTick = SkillTick = -1;
        }
    }

    // The log played to a moment (docs/55 section 11): AdvanceTo walks the events in their order up to a round and tick
    // and keeps each side's state, handing back the events it crossed so the view can show each one once (a number, a
    // flash, a call-out). Positions follow DuelReplay's reading - from a Move a side stands at its Value and adds its step
    // every tick after - so the view walks exactly where the duel did; a Shield Rush is drawn as a leap, an arc over
    // LeapTicks from where it stood to where the log lands it (where it stood, when it rushed from within its reach). Going
    // back replays from the start. Reads only the log; never runs a duel.
    public sealed class DuelPlaybackState
    {
        // A Shield Rush's leap is drawn over this many ticks from the tick it landed.
        public const int LeapTicks = 12;
        // A hit flash and a strike's pose last this many ticks.
        public const int FlashTicks = 6;
        public const int StrikeTicks = 14;
        public const double MicroPerUnit = 1_000_000.0;

        private readonly DuelLog _log;
        private readonly DuelTimeline _timeline;
        private readonly DuelSideState[] _sides = { new DuelSideState(), new DuelSideState() };
        private int _next;

        public DuelPlaybackState(DuelLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _timeline = new DuelTimeline(log);
            Round = 0;
            Tick = -1;
        }

        public DuelTimeline Timeline => _timeline;
        public int Round { get; private set; }
        public double Tick { get; private set; }
        // How many of the log's events have been played.
        public int Played => _next;
        public DuelSideState Side(int side) => _sides[side];

        // Plays every event up to the round and tick (the round's RoundStart events at once, then each event whose tick is
        // at or before the tick), adding the events crossed to `crossed` in the log's order when it is given.
        public void AdvanceTo(int round, double tick, List<DuelEvent> crossed = null)
        {
            if (round < Round || (round == Round && tick < Tick))
                Rewind();
            Round = round;
            Tick = tick;
            int whole = tick < 0 ? -1 : (int)Math.Floor(tick);
            while (_next < _log.Count)
            {
                DuelEvent e = _log[_next];
                bool due = e.Round < round || (e.Round == round && (e.Kind == DuelEventKind.RoundStart || e.Tick <= whole));
                if (!due)
                    break;
                Apply(e);
                crossed?.Add(e);
                _next++;
            }
        }

        // The whole match played: the last round's end.
        public void AdvanceToEnd(List<DuelEvent> crossed = null)
        {
            int last = Math.Max(0, _timeline.RoundCount - 1);
            AdvanceTo(last, _timeline.RoundCount > 0 ? _timeline.RoundTicks(last) : 0, crossed);
        }

        public void AdvanceTo(DuelMoment moment, List<DuelEvent> crossed = null) => AdvanceTo(moment.Round, moment.Tick, crossed);

        private void Rewind()
        {
            _next = 0;
            Round = 0;
            Tick = -1;
            for (int i = 0; i < _sides.Length; i++)
            {
                _sides[i] = new DuelSideState();
            }
        }

        private void Apply(DuelEvent e)
        {
            DuelSideState s = e.Side >= 0 && e.Side < 2 ? _sides[e.Side] : null;
            switch (e.Kind)
            {
                case DuelEventKind.RoundStart:
                    s.Begin(e);
                    break;
                case DuelEventKind.Move:
                    if (e.Aux == 1)
                    {
                        s.LeapFrom = PositionAt(s, e.Round, e.Tick);
                        s.LeapTick = e.Tick;
                    }
                    else
                    {
                        s.Step = e.Extra;
                    }
                    s.X = e.Value;
                    s.Since = e.Tick;
                    break;
                case DuelEventKind.Strike:
                case DuelEventKind.Hit:
                case DuelEventKind.Burn:
                    DuelSideState victim = _sides[1 - e.Side];
                    victim.Health -= e.Value;
                    if (e.Kind != DuelEventKind.Burn)
                    {
                        victim.StruckTick = e.Tick;
                        if (e.Kind == DuelEventKind.Strike)
                            s.StrikeTick = e.Tick;
                    }
                    break;
                case DuelEventKind.Skill:
                    s.SkillTick = e.Tick;
                    s.LastSkill = e.Effect;
                    // Every Shield Rush is drawn as a leap at the foe: from where it stands to where it lands - the same
                    // place when it rushed from within its reach, as nearly every Rush does (docs/55 section 11), or where a
                    // Move with Aux 1 later in the tick puts it.
                    if (e.Effect == ArenaEffect.ShieldRush)
                    {
                        s.LeapFrom = PositionAt(s, e.Round, e.Tick);
                        s.LeapTick = e.Tick;
                    }
                    if (e.Effect == ArenaEffect.IronResolve)
                        s.GuardUntil = e.Tick + e.Extra;
                    else if (e.Effect == ArenaEffect.CinderWard)
                        s.WardUntil = e.Tick + e.Extra;
                    else if (e.Effect == ArenaEffect.FlameSigil)
                    {
                        s.SigilUntil = e.Tick + e.Extra;
                        s.SigilX = e.Aux;
                    }
                    break;
                case DuelEventKind.Stacks:
                    s.BurnStacks = e.Value;
                    break;
                case DuelEventKind.Daze:
                    s.DazedUntil = Math.Max(s.DazedUntil, e.Tick + e.Value);
                    break;
                case DuelEventKind.RoundEnd:
                    if (e.Side == 0 || e.Side == 1)
                        _sides[e.Side].Wins++;
                    break;
            }
        }

        // Where a side stands at a tick of the current round, micro-units: from its last Move on, its step every tick,
        // until the round's last tick (the round's end walks no more).
        private long PositionAt(DuelSideState s, int round, double tick)
        {
            double last = _timeline.RoundCount > round ? _timeline.RoundTicks(round) - 1 : tick;
            double t = Math.Min(tick, last);
            if (t <= s.Since)
                return s.X;
            return s.X + (long)Math.Round(s.Step * (t - s.Since));
        }

        // Where a side has walked to at the playback's tick, micro-units, as DuelReplay reads the log (a leap at once).
        public long Walked(int side) => PositionAt(_sides[side], Round, Tick);

        // Where a side is drawn at the playback's tick, in lane units (the walls at +-6): its walk, or a leap's arc from
        // where it left to where it walks now.
        public double Position(int side) => Position(side, out _);

        // The same, with the leap's height (0 to 1, a half sine over LeapTicks; 0 when not leaping).
        public double Position(int side, out double leap)
        {
            DuelSideState s = _sides[side];
            double x = PositionAt(s, Round, Tick);
            leap = 0;
            if (s.LeapTick >= 0 && Tick >= s.LeapTick && Tick < s.LeapTick + LeapTicks)
            {
                double p = (Tick - s.LeapTick) / LeapTicks;
                x = s.LeapFrom + (x - s.LeapFrom) * p;
                leap = Math.Sin(Math.PI * p);
            }
            return x / MicroPerUnit;
        }

        // Which way a side faces: toward the foe, the attacker (0) to the right at the start.
        public int Facing(int side)
        {
            double mine = Position(side);
            double theirs = Position(1 - side);
            if (Math.Abs(theirs - mine) < 1e-6)
                return side == 0 ? 1 : -1;
            return theirs > mine ? 1 : -1;
        }

        // Whether a window that ends before `until` covers the playback's tick.
        public bool Covers(int until) => until >= 0 && Tick >= 0 && Tick < until;

        public bool Guarded(int side) => Covers(_sides[side].GuardUntil);
        public bool Warded(int side) => Covers(_sides[side].WardUntil);
        public bool Dazed(int side) => Covers(_sides[side].DazedUntil);
        public bool Sigil(int side) => Covers(_sides[side].SigilUntil);
        public bool Flashing(int side) => Recent(_sides[side].StruckTick, FlashTicks);
        public bool Striking(int side) => Recent(_sides[side].StrikeTick, StrikeTicks);

        private bool Recent(int tick, int span) => tick >= 0 && Tick >= tick && Tick < tick + span;
    }

    // The numbers the playback writes: a blow's health in whole points (milli-points rounded, never 0 for a blow that took
    // anything), a gear power or a line's value (hundredths of a percent) as a percent, a chance per mille as a percent.
    public static class DuelNumbers
    {
        public static int Points(long milli)
        {
            if (milli <= 0)
                return 0;
            long points = (milli + 500) / 1000;
            return (int)Math.Max(1, points);
        }

        public static double Percent(int hundredths) => hundredths / 100.0;

        public static int ChancePercent(int permille) => (int)Math.Round(permille / 10.0, MidpointRounding.AwayFromZero);
    }
}
