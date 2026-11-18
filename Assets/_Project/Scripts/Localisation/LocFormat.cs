using System;
using System.Collections.Generic;
using System.Text;

namespace Cryptforge.Localisation
{
    // The table's formatting, plural-free: a template's placeholders filled by position ("{0}", "{1:0.0}", as string.Format
    // reads them) or by name ("{gold}", "{chance:0}"), "{{" and "}}" for braces. Numbers and anything else IFormattable are
    // written with the provider given - the chosen language's (Loc.Numbers) - and the format after the colon.
    //
    // Unlike string.Format it never throws: a malformed template, or one naming a placeholder it was not given, returns
    // false, and Loc falls back to the English. A name is lower-case snake_case; an alignment ("{0,5}") is not supported.
    public static class LocFormat
    {
        public readonly struct Placeholder
        {
            public Placeholder(string name, string format)
            {
                Name = name;
                Format = format;
            }

            // The digits of an indexed placeholder, or the name of a named one.
            public string Name { get; }
            // The format after the colon, or empty.
            public string Format { get; }
            public bool IsIndexed => Name.Length > 0 && Name[0] >= '0' && Name[0] <= '9';

            public override string ToString() => Format.Length > 0 ? "{" + Name + ":" + Format + "}" : "{" + Name + "}";
        }

        public static bool TryApply(string template, IFormatProvider provider, object[] indexed,
            (string Name, object Value)[] named, out string text)
        {
            text = null;
            if (template == null)
                return false;
            if (template.IndexOf('{') < 0 && template.IndexOf('}') < 0)
            {
                text = template;
                return true;
            }
            var builder = new StringBuilder(template.Length + 16);
            int at = 0;
            while (at < template.Length)
            {
                if (!TryNext(template, ref at, builder, out Placeholder placeholder, out bool found))
                    return false;
                if (!found)
                    continue;
                if (!TryValue(placeholder, indexed, named, out object value))
                    return false;
                Write(builder, value, placeholder.Format, provider);
            }
            text = builder.ToString();
            return true;
        }

        // Every placeholder of the template in order, or null when the template is malformed.
        public static List<Placeholder> Placeholders(string template)
        {
            var placeholders = new List<Placeholder>();
            if (string.IsNullOrEmpty(template))
                return placeholders;
            int at = 0;
            while (at < template.Length)
            {
                if (!TryNext(template, ref at, null, out Placeholder placeholder, out bool found))
                    return null;
                if (found)
                    placeholders.Add(placeholder);
            }
            return placeholders;
        }

        // Reads from at to the end of the next placeholder (found) or of the text before it, appending literal text to
        // builder when one is given. False for a stray brace or a malformed placeholder.
        private static bool TryNext(string template, ref int at, StringBuilder builder, out Placeholder placeholder, out bool found)
        {
            placeholder = default;
            found = false;
            while (at < template.Length)
            {
                char c = template[at];
                if (c == '}')
                {
                    if (at + 1 < template.Length && template[at + 1] == '}')
                    {
                        builder?.Append('}');
                        at += 2;
                        continue;
                    }
                    return false;
                }
                if (c != '{')
                {
                    builder?.Append(c);
                    at++;
                    continue;
                }
                if (at + 1 < template.Length && template[at + 1] == '{')
                {
                    builder?.Append('{');
                    at += 2;
                    continue;
                }
                int close = template.IndexOf('}', at + 1);
                if (close < 0)
                    return false;
                string inner = template.Substring(at + 1, close - at - 1);
                if (inner.IndexOf('{') >= 0)
                    return false;
                int colon = inner.IndexOf(':');
                string name = colon >= 0 ? inner.Substring(0, colon) : inner;
                string format = colon >= 0 ? inner.Substring(colon + 1) : string.Empty;
                if (!IsName(name))
                    return false;
                placeholder = new Placeholder(name, format);
                found = true;
                at = close + 1;
                return true;
            }
            return true;
        }

        // Digits (an index), or a lower-case snake_case name starting with a letter.
        private static bool IsName(string name)
        {
            if (name.Length == 0)
                return false;
            bool digits = name[0] >= '0' && name[0] <= '9';
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool valid = digits ? c >= '0' && c <= '9' : (c >= 'a' && c <= 'z') || c == '_' || (i > 0 && c >= '0' && c <= '9');
                if (!valid)
                    return false;
            }
            return true;
        }

        private static bool TryValue(Placeholder placeholder, object[] indexed, (string Name, object Value)[] named, out object value)
        {
            value = null;
            if (placeholder.IsIndexed)
            {
                if (indexed == null || placeholder.Name.Length > 6)
                    return false;
                int index = 0;
                for (int i = 0; i < placeholder.Name.Length; i++)
                    index = index * 10 + (placeholder.Name[i] - '0');
                if (index >= indexed.Length)
                    return false;
                value = indexed[index];
                return true;
            }
            if (named == null)
                return false;
            for (int i = 0; i < named.Length; i++)
            {
                if (string.Equals(named[i].Name, placeholder.Name, StringComparison.Ordinal))
                {
                    value = named[i].Value;
                    return true;
                }
            }
            return false;
        }

        private static void Write(StringBuilder builder, object value, string format, IFormatProvider provider)
        {
            if (value == null)
                return;
            if (value is IFormattable formattable)
                builder.Append(formattable.ToString(format.Length > 0 ? format : null, provider));
            else
                builder.Append(value);
        }
    }
}
