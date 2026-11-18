using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // What the view draws (docs/55 section 2.1: "watch the replay" is "play the log"). The log's schema is versioned apart
    // from the rules (DuelLog.SchemaVersion) and changes rarely. Side is 0 for the attacker (A), 1 for the defender (B).
    public enum DuelEventKind
    {
        // A round begins: Side's arena maximum health (Value), its entry position in micro-units (Extra) and the boon it was
        // dealt (Aux: its index in the rules' boons plus one; 0 none).
        RoundStart = 0,
        // Side's step changed: its position after this tick (Value) and its step a tick (Extra), both in micro-units
        // (millionths of a unit), held until its next Move. Aux 1: a Shield Rush's leap (Value is where it lands).
        Move = 1,
        // A weapon strike landed: Side struck; Value is the health it took, Extra what the foe's ward absorbed, Aux the
        // flags (1 a crit, 2 the weapon's rhythm).
        Strike = 2,
        // A skill was used: Side, its Effect, its slot (Value); Extra its lasting ticks (a guard, a glyph, a ward) and Aux
        // the glyph's position in micro-units.
        Skill = 3,
        // A skill's blow landed (Effect the skill): as Strike.
        Hit = 4,
        // A burn ticked: Side is the burner (whose Ember Memory lit it); Value the health it took, Extra what the ward
        // absorbed, Aux the stacks that ticked.
        Burn = 5,
        // Side's burn stacks changed to Value.
        Stacks = 6,
        // Side was dazed for Value ticks.
        Daze = 7,
        // A round ended: Side the winner (0, 1, or 2 for a draw); Value and Extra each side's health left (milli-points,
        // not below zero); Aux the round's ticks.
        RoundEnd = 8
    }

    public readonly struct DuelEvent
    {
        public readonly int Round;
        public readonly int Tick;
        public readonly DuelEventKind Kind;
        public readonly int Side;
        public readonly ArenaEffect Effect;
        public readonly int Value;
        public readonly int Extra;
        public readonly int Aux;

        public DuelEvent(int round, int tick, DuelEventKind kind, int side, ArenaEffect effect, int value, int extra, int aux)
        {
            Round = round;
            Tick = tick;
            Kind = kind;
            Side = side;
            Effect = effect;
            Value = value;
            Extra = extra;
            Aux = aux;
        }

        // The fingerprint's step: every field, in order.
        public ulong MixInto(ulong hash)
        {
            hash = ArenaHash.Mix(hash, Round);
            hash = ArenaHash.Mix(hash, Tick);
            hash = ArenaHash.Mix(hash, (int)Kind);
            hash = ArenaHash.Mix(hash, Side);
            hash = ArenaHash.Mix(hash, (int)Effect);
            hash = ArenaHash.Mix(hash, Value);
            hash = ArenaHash.Mix(hash, Extra);
            return ArenaHash.Mix(hash, Aux);
        }
    }

    // A match's events in the order the duel made them. Its fingerprint is the duel's own (DuelMatch.Fingerprint): the
    // FNV-1a of every event, so a stored log can be checked against the result it claims.
    public sealed class DuelLog
    {
        public const int SchemaVersion = 1;

        private readonly List<DuelEvent> _events = new List<DuelEvent>(512);

        public int Count => _events.Count;
        public DuelEvent this[int index] => _events[index];
        public IReadOnlyList<DuelEvent> Events => _events;

        public void Add(DuelEvent e) => _events.Add(e);

        public void Clear() => _events.Clear();

        public ulong Fingerprint()
        {
            ulong hash = ArenaHash.Seed;
            for (int i = 0; i < _events.Count; i++)
                hash = _events[i].MixInto(hash);
            return hash;
        }
    }

    // A round as the log alone tells it: each side's health from its maximum less every blow and burn the foe landed, the
    // winner by the duel's rule (a fallen side loses; at the limit the larger share of health left wins), each side's
    // position from its moves (micro-units). DuelReplay.Play reads only the log; the tests compare it with the duel's own result.
    public readonly struct DuelReplayRound
    {
        public readonly int Winner;
        public readonly int Ticks;
        public readonly int HealthA;
        public readonly int HealthB;
        public readonly int PositionA;
        public readonly int PositionB;

        public DuelReplayRound(int winner, int ticks, int healthA, int healthB, int positionA, int positionB)
        {
            Winner = winner;
            Ticks = ticks;
            HealthA = healthA;
            HealthB = healthB;
            PositionA = positionA;
            PositionB = positionB;
        }
    }

    public static class DuelReplay
    {
        public static List<DuelReplayRound> Play(DuelLog log)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));
            var rounds = new List<DuelReplayRound>();
            var max = new long[2];
            var health = new long[2];
            var x = new long[2];
            var step = new long[2];
            var since = new int[2];
            for (int i = 0; i < log.Count; i++)
            {
                DuelEvent e = log[i];
                switch (e.Kind)
                {
                    case DuelEventKind.RoundStart:
                        max[e.Side] = e.Value;
                        health[e.Side] = e.Value;
                        x[e.Side] = e.Extra;
                        step[e.Side] = 0;
                        since[e.Side] = 0;
                        break;
                    case DuelEventKind.Move:
                        // From this tick the side stands at Value and adds its step every tick after; a leap keeps the step.
                        x[e.Side] = e.Value;
                        since[e.Side] = e.Tick;
                        if (e.Aux == 0)
                            step[e.Side] = e.Extra;
                        break;
                    case DuelEventKind.Strike:
                    case DuelEventKind.Hit:
                    case DuelEventKind.Burn:
                        health[1 - e.Side] -= e.Value;
                        break;
                    case DuelEventKind.RoundEnd:
                        int last = e.Aux - 1;
                        long a = x[0] + step[0] * (last - since[0]);
                        long b = x[1] + step[1] * (last - since[1]);
                        rounds.Add(new DuelReplayRound(Winner(health, max), e.Aux, (int)Math.Max(0, health[0]),
                            (int)Math.Max(0, health[1]), (int)a, (int)b));
                        break;
                }
            }
            return rounds;
        }

        private static int Winner(long[] health, long[] max)
        {
            if (health[0] <= 0 && health[1] <= 0)
                return -1;
            if (health[1] <= 0)
                return 0;
            if (health[0] <= 0)
                return 1;
            long shareA = health[0] * max[1];
            long shareB = health[1] * max[0];
            return shareA > shareB ? 0 : shareB > shareA ? 1 : -1;
        }
    }
}
