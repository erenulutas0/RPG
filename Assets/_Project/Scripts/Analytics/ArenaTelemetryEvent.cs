using System;
using System.Collections.Generic;
using Cryptforge.Arena;

namespace Cryptforge.Analytics
{
    // The arena_fight event (docs/55 sections 10.8 and 11) on the game's telemetry: the arena's assembly defines the fields
    // (ArenaFightTelemetry, in their order) and cannot see this one, so they are copied into TelemetryFields here, key by
    // key and kind by kind. A fight is no part of a run - the arena is reached between runs - so it is emitted under no run
    // id, on the session the run's telemetry shares (ArenaTelemetryWriter); RunTelemetry is not touched. Pure.
    public static class ArenaTelemetryEvent
    {
        public const string EventName = ArenaFightTelemetry.EventName;

        public static TelemetryFields ToFields(IReadOnlyList<ArenaTelemetryField> fields)
        {
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));
            var payload = new TelemetryFields();
            foreach (ArenaTelemetryField field in fields)
            {
                switch (field.Kind)
                {
                    case ArenaTelemetryFieldKind.Number:
                        payload.Add(field.Key, field.Number);
                        break;
                    case ArenaTelemetryFieldKind.Flag:
                        payload.Add(field.Key, field.Flag);
                        break;
                    default:
                        payload.Add(field.Key, field.Text);
                        break;
                }
            }
            return payload;
        }

        // The fight's fields: its record's fight number is the save's count before it.
        public static TelemetryFields Of(ArenaFightOutcome outcome)
        {
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));
            return ToFields(ArenaFightTelemetry.Fields(outcome, outcome.Record != null ? outcome.Record.fight : 0));
        }

        // One line on the session, outside any run.
        public static void Emit(TelemetrySession session, ArenaFightOutcome outcome)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            session.EmitForRun(null, EventName, Of(outcome));
        }
    }
}
