using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cryptforge.Monetisation
{
    // The JSON of the monetisation's two small files (rewards.json, entitlements.json): one object of whole numbers,
    // strings, booleans and arrays of strings, camelCase keys in the writer's fixed order, every number through the
    // invariant culture, so a phone set to Turkish writes and reads the same bytes (docs/60 section 6). Reading follows
    // ArenaSaveJson: a strict parser, forgiving fields - unknown keys ignored, a missing or mistyped value read as absent -
    // and text that is not one JSON object read as null.
    internal static class FlatJson
    {
        internal sealed class Writer
        {
            private readonly StringBuilder _text = new StringBuilder(256);
            private bool _first = true;

            public Writer()
            {
                _text.Append('{');
            }

            public Writer Field(string key, long value)
            {
                Key(key);
                _text.Append(value.ToString(CultureInfo.InvariantCulture));
                return this;
            }

            public Writer Field(string key, string value)
            {
                Key(key);
                if (value == null)
                    _text.Append("null");
                else
                    Quote(value);
                return this;
            }

            public Writer Field(string key, bool value)
            {
                Key(key);
                _text.Append(value ? "true" : "false");
                return this;
            }

            public Writer Field(string key, IReadOnlyList<string> values)
            {
                Key(key);
                _text.Append('[');
                if (values != null)
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        if (i > 0)
                            _text.Append(", ");
                        if (values[i] == null)
                            _text.Append("null");
                        else
                            Quote(values[i]);
                    }
                }
                _text.Append(']');
                return this;
            }

            // The finished object with a closing newline.
            public string Finish()
            {
                _text.Append(_first ? "}\n" : "\n}\n");
                return _text.ToString();
            }

            private void Key(string key)
            {
                _text.Append(_first ? "\n  " : ",\n  ");
                _first = false;
                Quote(key);
                _text.Append(": ");
            }

            private void Quote(string value)
            {
                _text.Append('"');
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"':
                            _text.Append("\\\"");
                            break;
                        case '\\':
                            _text.Append("\\\\");
                            break;
                        case '\n':
                            _text.Append("\\n");
                            break;
                        case '\r':
                            _text.Append("\\r");
                            break;
                        case '\t':
                            _text.Append("\\t");
                            break;
                        default:
                            if (c < ' ')
                                _text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                _text.Append(c);
                            break;
                    }
                }
                _text.Append('"');
            }
        }

        // The object the text holds, or null for anything else (empty, malformed, an array, trailing text).
        public static Dictionary<string, object> Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return new Reader(json).Document() as Dictionary<string, object>;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static long Long(Dictionary<string, object> o, string key, long fallback) =>
            o.TryGetValue(key, out object value) && value is long number ? number : fallback;

        public static int Int(Dictionary<string, object> o, string key, int fallback)
        {
            long number = Long(o, key, fallback);
            return number < int.MinValue || number > int.MaxValue ? fallback : (int)number;
        }

        public static string String(Dictionary<string, object> o, string key) =>
            o.TryGetValue(key, out object value) ? value as string : null;

        public static bool Bool(Dictionary<string, object> o, string key, bool fallback) =>
            o.TryGetValue(key, out object value) && value is bool b ? b : fallback;

        // The array's non-empty strings in order; anything else in it is skipped.
        public static List<string> Strings(Dictionary<string, object> o, string key)
        {
            var strings = new List<string>();
            if (!o.TryGetValue(key, out object value) || !(value is List<object> list))
                return strings;
            foreach (object item in list)
            {
                if (item is string text && text.Length > 0)
                    strings.Add(text);
            }
            return strings;
        }

        // Objects, arrays, strings, whole numbers (as long), true, false and null; a fraction or an exponent is read past
        // and kept as text, which no field accepts.
        private sealed class Reader
        {
            private const int MaxDepth = 16;
            private readonly string _s;
            private int _i;
            private int _depth;

            public Reader(string s)
            {
                _s = s;
            }

            public object Document()
            {
                object value = Value();
                Skip();
                if (_i != _s.Length)
                    throw new FormatException("Trailing text after the document.");
                return value;
            }

            private object Value()
            {
                Skip();
                if (_i >= _s.Length)
                    throw new FormatException("Unexpected end.");
                switch (_s[_i])
                {
                    case '{':
                        return Object();
                    case '[':
                        return Array();
                    case '"':
                        return String();
                    case 't':
                        Word("true");
                        return true;
                    case 'f':
                        Word("false");
                        return false;
                    case 'n':
                        Word("null");
                        return null;
                    default:
                        return Number();
                }
            }

            private Dictionary<string, object> Object()
            {
                Enter();
                var o = new Dictionary<string, object>(StringComparer.Ordinal);
                _i++;
                Skip();
                if (Peek() == '}')
                {
                    _i++;
                    _depth--;
                    return o;
                }
                while (true)
                {
                    Skip();
                    if (Peek() != '"')
                        throw new FormatException("A key must be a string.");
                    string key = String();
                    Skip();
                    Expect(':');
                    o[key] = Value();
                    Skip();
                    char c = Next();
                    if (c == '}')
                    {
                        _depth--;
                        return o;
                    }
                    if (c != ',')
                        throw new FormatException("Expected , or }.");
                }
            }

            private List<object> Array()
            {
                Enter();
                var list = new List<object>();
                _i++;
                Skip();
                if (Peek() == ']')
                {
                    _i++;
                    _depth--;
                    return list;
                }
                while (true)
                {
                    list.Add(Value());
                    Skip();
                    char c = Next();
                    if (c == ']')
                    {
                        _depth--;
                        return list;
                    }
                    if (c != ',')
                        throw new FormatException("Expected , or ].");
                }
            }

            private void Enter()
            {
                if (++_depth > MaxDepth)
                    throw new FormatException("Nested too deep.");
            }

            private string String()
            {
                Expect('"');
                var text = new StringBuilder();
                while (true)
                {
                    char c = Next();
                    if (c == '"')
                        return text.ToString();
                    if (c != '\\')
                    {
                        text.Append(c);
                        continue;
                    }
                    char e = Next();
                    switch (e)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            text.Append(e);
                            break;
                        case 'n':
                            text.Append('\n');
                            break;
                        case 'r':
                            text.Append('\r');
                            break;
                        case 't':
                            text.Append('\t');
                            break;
                        case 'b':
                            text.Append('\b');
                            break;
                        case 'f':
                            text.Append('\f');
                            break;
                        case 'u':
                            if (_i + 4 > _s.Length ||
                                !int.TryParse(_s.Substring(_i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                                throw new FormatException("A bad \\u escape.");
                            text.Append((char)code);
                            _i += 4;
                            break;
                        default:
                            throw new FormatException("An unknown escape.");
                    }
                }
            }

            private object Number()
            {
                int start = _i;
                if (Peek() == '-')
                    _i++;
                while (_i < _s.Length && _s[_i] >= '0' && _s[_i] <= '9')
                    _i++;
                if (_i == start || (_i == start + 1 && _s[start] == '-'))
                    throw new FormatException("Expected a value.");
                bool whole = true;
                while (_i < _s.Length && (_s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '+' || _s[_i] == '-' ||
                                          (_s[_i] >= '0' && _s[_i] <= '9')))
                {
                    whole = false;
                    _i++;
                }
                string text = _s.Substring(start, _i - start);
                if (whole && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value))
                    return value;
                return text;
            }

            private void Word(string word)
            {
                if (_i + word.Length > _s.Length || string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0)
                    throw new FormatException("Expected " + word + ".");
                _i += word.Length;
            }

            private void Skip()
            {
                while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\n' || _s[_i] == '\r' || _s[_i] == '\t'))
                    _i++;
            }

            private char Peek() => _i < _s.Length ? _s[_i] : '\0';

            private char Next()
            {
                if (_i >= _s.Length)
                    throw new FormatException("Unexpected end.");
                return _s[_i++];
            }

            private void Expect(char c)
            {
                if (Next() != c)
                    throw new FormatException("Expected " + c + ".");
            }
        }
    }
}
