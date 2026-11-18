using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cryptforge.Arena
{
    // One field of a telemetry event: a snake_case key and a string, an integer or a boolean.
    public readonly struct ArenaTelemetryField
    {
        public readonly string Key;
        public readonly string Text;
        public readonly long Number;
        public readonly bool Flag;
        public readonly ArenaTelemetryFieldKind Kind;

        private ArenaTelemetryField(string key, string text, long number, bool flag, ArenaTelemetryFieldKind kind)
        {
            Key = key;
            Text = text;
            Number = number;
            Flag = flag;
            Kind = kind;
        }

        public static ArenaTelemetryField Of(string key, string text) =>
            new ArenaTelemetryField(key, text, 0, false, ArenaTelemetryFieldKind.Text);

        public static ArenaTelemetryField Of(string key, long number) =>
            new ArenaTelemetryField(key, null, number, false, ArenaTelemetryFieldKind.Number);

        public static ArenaTelemetryField Of(string key, bool flag) =>
            new ArenaTelemetryField(key, null, 0, flag, ArenaTelemetryFieldKind.Flag);
    }

    public enum ArenaTelemetryFieldKind
    {
        Text = 0,
        Number = 1,
        Flag = 2
    }

    // The arena_fight event (docs/55 section 6, P0's telemetry): one line a ladder fight, in this fixed key order, for the
    // local log (docs/10; nothing leaves the device). This assembly cannot see the game's telemetry, so it defines the
    // fields and the view's wiring hands them to RunTelemetry's TelemetryFields (Add per Kind) under the event name. The
    // keys are snake_case and never an envelope key (ArenaFightTelemetryTests checks them against TelemetryFields).
    //
    //   mode                 "ranked" or "open"
    //   rules_version        the duel rules' version; rules_hash the table's content hash (16 hex digits)
    //   seed                 the fight's seed; fight the save's fight count before it
    //   hero_id, weapon_id, policy, skill_ids, line_ids   the player's build (line_ids empty in the open arena)
    //   gear_power           the player's power index in hundredths of a percent (the common level's and the lines' in
    //                        ranked, the gear's in the open arena)
    //   rival_id, rival_past_self, rival_hero_id, rival_weapon_id, rival_policy, rival_line_ids, rival_gear_power,
    //   rival_rating         the rival as it fought (the rating before the fight)
    //   result               "win", "loss" or "draw"; rounds, rounds_won, rounds_lost
    //   duel_sec             the rounds' fighting time, seconds (ticks / 60, rounded down)
    //   rating_before, rating_after, rating_delta
    //   why                  the summary's effect ids, comma separated, strongest first (DuelSummary.Id)
    //   events               the log's length; fingerprint the match's fingerprint (16 hex digits)
    public static class ArenaFightTelemetry
    {
        public const string EventName = "arena_fight";

        public static List<ArenaTelemetryField> Fields(ArenaFightOutcome outcome, int fight)
        {
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));
            DuelMatch m = outcome.Match;
            ArenaFighter p = outcome.Player;
            ArenaFighter f = outcome.Foe;
            var why = new StringBuilder();
            foreach (DuelWhy line in m.Why)
            {
                if (why.Length > 0)
                    why.Append(',');
                why.Append(DuelSummary.Id(line.Effect));
            }
            return new List<ArenaTelemetryField>
            {
                ArenaTelemetryField.Of("mode", ArenaSnapshots.ModeId(outcome.Mode)),
                ArenaTelemetryField.Of("rules_version", m.RulesVersion),
                ArenaTelemetryField.Of("rules_hash", ArenaHash.Hex(m.RulesHash)),
                ArenaTelemetryField.Of("seed", m.Seed),
                ArenaTelemetryField.Of("fight", fight),
                ArenaTelemetryField.Of("hero_id", p.Hero.Id),
                ArenaTelemetryField.Of("weapon_id", p.Weapon.Id),
                ArenaTelemetryField.Of("policy", ArenaSnapshots.PolicyId(p.Policy)),
                ArenaTelemetryField.Of("skill_ids", Join(p.Skills)),
                ArenaTelemetryField.Of("line_ids", Join(p.LineTypes)),
                ArenaTelemetryField.Of("gear_power", p.GearPower),
                ArenaTelemetryField.Of("rival_id", outcome.Rival != null ? outcome.Rival.id : null),
                ArenaTelemetryField.Of("rival_past_self", outcome.Rival != null && outcome.Rival.pastSelf),
                ArenaTelemetryField.Of("rival_hero_id", f.Hero.Id),
                ArenaTelemetryField.Of("rival_weapon_id", f.Weapon.Id),
                ArenaTelemetryField.Of("rival_policy", ArenaSnapshots.PolicyId(f.Policy)),
                ArenaTelemetryField.Of("rival_line_ids", Join(f.LineTypes)),
                ArenaTelemetryField.Of("rival_gear_power", f.GearPower),
                ArenaTelemetryField.Of("rival_rating", outcome.RivalRatingBefore),
                ArenaTelemetryField.Of("result", m.Winner == 0 ? "win" : m.Winner == 1 ? "loss" : "draw"),
                ArenaTelemetryField.Of("rounds", m.Rounds.Count),
                ArenaTelemetryField.Of("rounds_won", m.WinsA),
                ArenaTelemetryField.Of("rounds_lost", m.WinsB),
                ArenaTelemetryField.Of("duel_sec", m.TotalTicks / DuelRules.TicksPerSecond),
                ArenaTelemetryField.Of("rating_before", outcome.RatingBefore),
                ArenaTelemetryField.Of("rating_after", outcome.RatingAfter),
                ArenaTelemetryField.Of("rating_delta", outcome.RatingAfter - outcome.RatingBefore),
                ArenaTelemetryField.Of("why", why.ToString()),
                ArenaTelemetryField.Of("events", m.Events),
                ArenaTelemetryField.Of("fingerprint", ArenaHash.Hex(m.Fingerprint))
            };
        }

        private static string Join(IReadOnlyList<ArenaSkill> skills)
        {
            var text = new StringBuilder();
            foreach (ArenaSkill skill in skills)
            {
                if (text.Length > 0)
                    text.Append(',');
                text.Append(skill.Id);
            }
            return text.ToString();
        }

        private static string Join(IReadOnlyList<ArenaStat> stats)
        {
            var text = new StringBuilder();
            foreach (ArenaStat stat in stats)
            {
                if (text.Length > 0)
                    text.Append(',');
                text.Append(ArenaGearTable.Id(stat));
            }
            return text.ToString();
        }

        // For a debug line: key=value pairs, invariant.
        public static string Describe(List<ArenaTelemetryField> fields)
        {
            var text = new StringBuilder();
            foreach (ArenaTelemetryField field in fields)
            {
                if (text.Length > 0)
                    text.Append(' ');
                text.Append(field.Key).Append('=');
                switch (field.Kind)
                {
                    case ArenaTelemetryFieldKind.Number:
                        text.Append(field.Number.ToString(CultureInfo.InvariantCulture));
                        break;
                    case ArenaTelemetryFieldKind.Flag:
                        text.Append(field.Flag ? "true" : "false");
                        break;
                    default:
                        text.Append(field.Text ?? "null");
                        break;
                }
            }
            return text.ToString();
        }
    }
}
