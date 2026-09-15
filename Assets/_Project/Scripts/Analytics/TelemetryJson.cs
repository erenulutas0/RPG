using System;
using System.Globalization;
using System.Text;

namespace Cryptforge.Analytics
{
    // Writes a record as one JSON Lines entry. The envelope order is fixed so a line can be read by eye and diffed, and
    // every number goes through the invariant culture so a phone set to Turkish still writes "1.500", not "1,500".
    public static class TelemetryJson
    {
        public const int SchemaVersion = 1;

        // One JSON object on one line without a trailing newline: "v", "seq", "t", "st", "session", "run" (only inside a
        // run), "event", then the fields in the order they were added.
        public static string Line(TelemetryRecord record)
        {
            if (record.SessionId == null || record.Name == null)
                throw new ArgumentException("An empty telemetry record cannot be written.", nameof(record));

            TelemetryFields fields = record.Fields;
            int fieldCount = fields?.Count ?? 0;
            var builder = new StringBuilder(128 + fieldCount * 24);
            builder.Append("{\"v\":").Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"seq\":").Append(record.Sequence.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"t\":\"").Append(Timestamp(record.UtcTime)).Append('"');
            builder.Append(",\"st\":").Append(Number(record.SessionSeconds));
            builder.Append(",\"session\":");
            AppendQuoted(builder, record.SessionId);
            if (record.RunId != null)
            {
                builder.Append(",\"run\":");
                AppendQuoted(builder, record.RunId);
            }
            builder.Append(",\"event\":");
            AppendQuoted(builder, record.Name);

            for (int i = 0; i < fieldCount; i++)
            {
                // Keys are validated snake_case, so they never need escaping.
                builder.Append(",\"").Append(fields.KeyAt(i)).Append("\":").Append(fields.JsonValueAt(i));
            }
            builder.Append('}');
            return builder.ToString();
        }

        // A JSON string literal: quotes, backslashes and control characters escaped per RFC 8259, and a lone surrogate
        // escaped too so the UTF-8 file never replaces it with a question mark.
        public static string Quote(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var builder = new StringBuilder(value.Length + 2);
            AppendQuoted(builder, value);
            return builder.ToString();
        }

        // Three decimals, invariant culture, or null for NaN and infinities. A negative value that rounds to zero is
        // written as "0.000" so the sign never depends on rounding noise.
        public static string Number(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "null";

            string text = value.ToString("F3", CultureInfo.InvariantCulture);
            return text == "-0.000" ? "0.000" : text;
        }

        // UTC with milliseconds, "yyyy-MM-ddTHH:mm:ss.fffZ". A local time is converted; an unspecified one is taken as UTC.
        public static string Timestamp(DateTime utcTime)
        {
            DateTime utc = utcTime.Kind == DateTimeKind.Local ? utcTime.ToUniversalTime() : utcTime;
            return utc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'", CultureInfo.InvariantCulture);
        }

        private static void AppendQuoted(StringBuilder builder, string value)
        {
            builder.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"')
                {
                    builder.Append("\\\"");
                }
                else if (c == '\\')
                {
                    builder.Append("\\\\");
                }
                else if (c < ' ')
                {
                    AppendUnicodeEscape(builder, c);
                }
                else if (char.IsHighSurrogate(c))
                {
                    if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                    {
                        builder.Append(c).Append(value[i + 1]);
                        i++;
                    }
                    else
                    {
                        AppendUnicodeEscape(builder, c);
                    }
                }
                else if (char.IsLowSurrogate(c))
                {
                    AppendUnicodeEscape(builder, c);
                }
                else
                {
                    builder.Append(c);
                }
            }
            builder.Append('"');
        }

        private static void AppendUnicodeEscape(StringBuilder builder, char c)
        {
            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
        }
    }
}
