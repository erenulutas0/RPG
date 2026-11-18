using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cryptforge.Localisation
{
    // The game's words in the player's language (docs/59 decision 1; docs/60 is the guide and the glossary).
    //
    // English stays where it always was - in the code beside each key (Loc.Text("anvil.title", "The Anvil")), or in the
    // content asset beside a key derived from its id (EnemyDefinition: "<id>.name") - so the English the game shows never
    // depends on the table: it is the text it always showed, byte for byte. The table (LocTable, Resources/Localisation/
    // Strings.txt) carries the Turkish beside a copy of that English for the owner's review; LocalisationTableTests keep the
    // copy equal to its source.
    //
    // In Turkish a lookup takes the table's Turkish, then the English given, then the table's English, then the key itself,
    // and says which key fell back once (Warn). An empty English is an empty text in both languages: nothing to translate.
    //
    // Numbers shown to the player are written in the chosen language's format on purpose (Numbers): "1,300.5" in English,
    // "1.300,5" in Turkish. The save, telemetry and the logs never come through here; they write the invariant culture
    // (LocalisationRuntime pins it at boot, and each writes it explicitly).
    //
    // The main thread's, like the rest of the game's state. The Descent simulation's threads only ever read it in English,
    // where nothing is written.
    public static class Loc
    {
        private static LocTable _table = LocTable.Empty;
        private static Language _chosen = Language.English;
        private static Language? _override;
        private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);
        private static readonly NumberFormatInfo TurkishNumbers = CreateTurkishNumbers();

        // The language changed (the player's choice, a test's or a capture's override) or a table was loaded: open views
        // write their words again (LocalisedLabel, and the views that refresh themselves).
        public static event Action Changed;

        // Where a fallback is said, once per key and language; LocalisationRuntime points it at the Unity log. Null says
        // nothing (the pure tests, the simulation).
        public static Action<string> Warn { get; set; }

        // The language the game speaks: an override (tests, captures) before the player's choice.
        public static Language Language => _override ?? _chosen;
        public static Language Chosen => _chosen;
        public static bool IsTurkish => Language == Language.Turkish;
        public static LocTable Table => _table;

        // A language forced over the player's choice, never stored: the tests pin English, a capture names its language.
        public static Language? Override
        {
            get => _override;
            set
            {
                Language before = Language;
                _override = value;
                if (Language != before)
                    Changed?.Invoke();
            }
        }

        public static void SetLanguage(Language language)
        {
            Language before = Language;
            _chosen = language;
            if (Language != before)
                Changed?.Invoke();
        }

        public static void UseTable(LocTable table)
        {
            _table = table ?? LocTable.Empty;
            Warned.Clear();
            Changed?.Invoke();
        }

        // For tests: English, no override, no table, no warnings said.
        public static void Reset()
        {
            _table = LocTable.Empty;
            _chosen = Language.English;
            _override = null;
            Warned.Clear();
        }

        // The format numbers shown to the player are written in: the invariant culture's for English ("1,300.5", as the
        // game always wrote them), a Turkish one for Turkish ("1.300,5"). Built here rather than read from the phone's
        // culture data, so every phone writes the same.
        public static IFormatProvider Numbers => NumbersFor(Language);

        public static IFormatProvider NumbersFor(Language language) =>
            language == Language.Turkish ? TurkishNumbers : (IFormatProvider)CultureInfo.InvariantCulture;

        // The text for key in the game's language. english is what the game shows in English: a literal beside the key in
        // the code, or the asset's own text - returned as it is, null and empty included, when there is nothing authored to
        // translate.
        public static string Text(string key, string english)
        {
            if (string.IsNullOrEmpty(english) || Language == Language.English)
                return english;
            if (_table.TryGet(key, out LocEntry entry) && entry.Turkish.Length > 0)
                return entry.Turkish;
            WarnOnce(key, "has no Turkish; the English stands in");
            return english;
        }

        // A key whose English is the table's own (tools and tests; the game always passes its English): the language's
        // text, then the table's English, then the key itself.
        public static string Text(string key)
        {
            if (_table.TryGet(key, out LocEntry entry))
            {
                string text = entry.For(Language);
                if (text.Length > 0)
                    return text;
                if (entry.English.Length > 0)
                {
                    WarnOnce(key, "has no Turkish; the English stands in");
                    return entry.English;
                }
            }
            WarnOnce(key, "is not in the table; the key stands in");
            return key ?? string.Empty;
        }

        // The text for key with its placeholders filled by position ("{0}", "{1:0.0}"), numbers in the chosen language's
        // format. A translation that cannot be filled falls back to the English.
        public static string Format(string key, string english, params object[] args) => Fill(key, english, args, null);

        // The same by name ("{gold}", "{level}"): Loc.Format("anvil.need_gold", "Need {gold} more gold", ("gold", 300)).
        public static string Format(string key, string english, params (string Name, object Value)[] args) =>
            Fill(key, english, null, args);

        private static string Fill(string key, string english, object[] indexed, (string Name, object Value)[] named)
        {
            string template = Text(key, english);
            IFormatProvider numbers = Numbers;
            if (LocFormat.TryApply(template, numbers, indexed, named, out string text))
                return text;
            if (english != null && !ReferenceEquals(template, english) && LocFormat.TryApply(english, numbers, indexed, named, out text))
            {
                WarnOnce(key, "has a Turkish whose placeholders cannot be filled; the English stands in");
                return text;
            }
            WarnOnce(key, "cannot be filled; its template is shown as it is");
            return template ?? string.Empty;
        }

        // Upper case for display in the game's language: Turkish puts the dot on the capital i ("İ") and takes it off the
        // dotless one ("I"); English is the invariant culture's. Rich-text tags are kept as they are.
        public static string Upper(string text) => UpperFor(Language, text);

        public static string Lower(string text) => LowerFor(Language, text);

        public static string UpperFor(Language language, string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                int tag = RichText.TagLength(text, i);
                if (tag > 0)
                {
                    builder.Append(text, i, tag);
                    i += tag - 1;
                    continue;
                }
                char c = text[i];
                if (language == Language.Turkish && c == 'i')
                    builder.Append('İ');
                else if (language == Language.Turkish && c == 'ı')
                    builder.Append('I');
                else
                    builder.Append(char.ToUpperInvariant(c));
            }
            return builder.ToString();
        }

        public static string LowerFor(Language language, string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                int tag = RichText.TagLength(text, i);
                if (tag > 0)
                {
                    builder.Append(text, i, tag);
                    i += tag - 1;
                    continue;
                }
                char c = text[i];
                if (language == Language.Turkish && c == 'I')
                    builder.Append('ı');
                else if (c == 'İ')
                    builder.Append('i');
                else
                    builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }

        private static void WarnOnce(string key, string what)
        {
            if (Warn == null || key == null)
                return;
            if (Warned.Add(LanguageCodes.Code(Language) + ":" + what + ":" + key))
                Warn("Localisation: " + key + " (" + LanguageCodes.Code(Language) + ") " + what + ".");
        }

        private static NumberFormatInfo CreateTurkishNumbers()
        {
            var numbers = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            numbers.NumberDecimalSeparator = ",";
            numbers.NumberGroupSeparator = ".";
            numbers.PercentDecimalSeparator = ",";
            numbers.PercentGroupSeparator = ".";
            numbers.CurrencyDecimalSeparator = ",";
            numbers.CurrencyGroupSeparator = ".";
            // "%25" for the standard "P" format, as Turkish writes a percentage; a custom format puts the sign where it is.
            numbers.PercentPositivePattern = 2;
            numbers.PercentNegativePattern = 2;
            return NumberFormatInfo.ReadOnly(numbers);
        }
    }
}
