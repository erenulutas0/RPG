using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cryptforge.Analytics
{
    // The ordered payload of one telemetry event. Each value becomes its JSON token as it is added, so a bad key fails
    // where the event is built rather than when the log is read, and serializing a record neither boxes nor reformats.
    public sealed class TelemetryFields
    {
        // The envelope already writes these keys; a field with the same name would make the line an ambiguous object.
        private static readonly string[] EnvelopeKeys = { "v", "seq", "t", "st", "session", "run", "event" };

        private readonly List<string> _keys = new List<string>();
        private readonly List<string> _tokens = new List<string>();

        public int Count => _keys.Count;

        // A null value is kept as JSON null so an absent id (no relic equipped) still has its column.
        public TelemetryFields Add(string key, string value)
        {
            return AddToken(key, value == null ? "null" : TelemetryJson.Quote(value));
        }

        public TelemetryFields Add(string key, long value)
        {
            return AddToken(key, value.ToString(CultureInfo.InvariantCulture));
        }

        // Three decimals, invariant; NaN and infinities have no JSON number and are written as null.
        public TelemetryFields Add(string key, double value)
        {
            return AddToken(key, TelemetryJson.Number(value));
        }

        public TelemetryFields Add(string key, bool value)
        {
            return AddToken(key, value ? "true" : "false");
        }

        public string KeyAt(int index)
        {
            if (index < 0 || index >= _keys.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _keys[index];
        }

        // The value exactly as it appears in the line: a quoted, escaped string, a number, true/false or null.
        public string JsonValueAt(int index)
        {
            if (index < 0 || index >= _tokens.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _tokens[index];
        }

        // The position of a key, or -1.
        public int IndexOf(string key)
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                if (string.Equals(_keys[i], key, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        // snake_case: a lower-case ASCII letter, then lower-case ASCII letters, digits or underscores. Checked by character
        // range, never by culture-aware casing, so a Turkish locale cannot change which keys pass.
        public static bool IsValidKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            if (key[0] < 'a' || key[0] > 'z')
                return false;
            for (int i = 1; i < key.Length; i++)
            {
                char c = key[i];
                bool valid = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!valid)
                    return false;
            }
            return true;
        }

        // Adds an already serialized JSON token; the session uses it to copy caller fields behind "schema".
        internal TelemetryFields AddToken(string key, string token)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!IsValidKey(key))
                throw new ArgumentException($"Telemetry field key '{key}' is not snake_case.", nameof(key));
            if (Array.IndexOf(EnvelopeKeys, key) >= 0)
                throw new ArgumentException($"Telemetry field key '{key}' is reserved for the envelope.", nameof(key));
            if (IndexOf(key) >= 0)
                throw new ArgumentException($"Telemetry field key '{key}' was already added.", nameof(key));

            _keys.Add(key);
            _tokens.Add(token);
            return this;
        }
    }
}
