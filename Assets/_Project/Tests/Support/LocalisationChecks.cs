using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cryptforge.Localisation;

namespace Cryptforge.Tests
{
    // The string table's checks (docs/60 section 5), pure so the Unity EditMode suite and Tools/CombatChecks run the same
    // ones over the same files:
    //   - every row has its English and its Turkish;
    //   - both carry the same placeholders ("{0}", "{gold}") and the same rich-text tags, every tag closed in order, and
    //     the same spaces at their ends;
    //   - every key the code names (Loc.Text("key", ...), Loc.Format("key", ...), new LocText("key", ...)) is in the table,
    //     with the table's English equal to the literal beside it;
    //   - every table key is named by the code, or derived from a content asset (its id, or a floor's room name).
    internal static class LocalisationChecks
    {
        public const string TablePath = "Assets/_Project/Resources/Localisation/Strings.txt";
        public const string ScriptsPath = "Assets/_Project/Scripts";
        public const string DataPath = "Assets/_Project/Data";

        // The fields a content asset's keys are made of (ContentText): "<id>.name", "<id>.description", the hero's own.
        public static readonly string[] ContentFields = { "name", "description", "subtitle", "role", "unlock_hint" };

        private static readonly Regex CodeUse = new Regex(
            @"(?:\bLoc\.(?:Text|Format)|\bnew\s+LocText)\(\s*""(?<key>[a-z0-9_]+(?:\.[a-z0-9_]+)+)""(?:\s*,\s*(?<english>""(?:[^""\\\r\n]|\\.)*""))?",
            RegexOptions.CultureInvariant);
        private static readonly Regex AssetId = new Regex(@"^\s*_id: '?(?<id>[a-z0-9_]+)'?\s*$", RegexOptions.CultureInvariant | RegexOptions.Multiline);
        private static readonly Regex RoomName = new Regex(@"^  - _displayName: (?<name>.+?)\s*$", RegexOptions.CultureInvariant | RegexOptions.Multiline);

        public sealed class CodeKey
        {
            public string Key;
            // The literal beside the key, unescaped; null when the English is not a literal (an asset's field).
            public string English;
            public string File;
            public int Line;

            public override string ToString() => $"{Key} ({File}:{Line})";
        }

        // The repository's root: the folder above the working directory (or the given start) holding Assets/_Project.
        public static string Root(string start = null)
        {
            foreach (string from in new[] { start, Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                if (string.IsNullOrEmpty(from))
                    continue;
                for (var folder = new DirectoryInfo(from); folder != null; folder = folder.Parent)
                {
                    if (File.Exists(Path.Combine(folder.FullName, TablePath)) || Directory.Exists(Path.Combine(folder.FullName, "Assets/_Project/Scripts")))
                        return folder.FullName;
                }
            }
            throw new DirectoryNotFoundException("No folder above " + Directory.GetCurrentDirectory() + " holds Assets/_Project.");
        }

        public static LocTable LoadTable(string root) => LocTable.Parse(File.ReadAllText(Path.Combine(root, TablePath), Encoding.UTF8));

        // Problems of the rows themselves: parse problems, a missing language, placeholders, tags and spacing.
        public static List<string> RowProblems(LocTable table)
        {
            var problems = new List<string>(table.Problems);
            foreach (LocEntry entry in table.Entries)
            {
                string at = $"{entry.Key} (line {entry.Line})";
                if (entry.English.Length == 0)
                    problems.Add(at + " has no English.");
                if (entry.Turkish.Length == 0)
                    problems.Add(at + " has no Turkish.");
                if (entry.English.Length == 0 || entry.Turkish.Length == 0)
                    continue;

                List<LocFormat.Placeholder> en = LocFormat.Placeholders(entry.English);
                List<LocFormat.Placeholder> tr = LocFormat.Placeholders(entry.Turkish);
                if (en == null)
                    problems.Add(at + ": the English is not a valid template (a stray brace?).");
                if (tr == null)
                    problems.Add(at + ": the Turkish is not a valid template (a stray brace?).");
                if (en != null && tr != null)
                {
                    string enNames = string.Join(" ", en.Select(p => "{" + p.Name + "}").OrderBy(n => n, StringComparer.Ordinal));
                    string trNames = string.Join(" ", tr.Select(p => "{" + p.Name + "}").OrderBy(n => n, StringComparer.Ordinal));
                    if (enNames != trNames)
                        problems.Add($"{at}: the placeholders differ - English {Or(enNames)}, Turkish {Or(trNames)}.");
                }

                bool enTags = RichText.TryTags(entry.English, out List<string> enTagList, out string enProblem);
                bool trTags = RichText.TryTags(entry.Turkish, out List<string> trTagList, out string trProblem);
                if (!enTags)
                    problems.Add($"{at}: the English's tags are unbalanced ({enProblem}).");
                if (!trTags)
                    problems.Add($"{at}: the Turkish's tags are unbalanced ({trProblem}).");
                string enTagSet = string.Join(" ", enTagList.OrderBy(t => t, StringComparer.Ordinal));
                string trTagSet = string.Join(" ", trTagList.OrderBy(t => t, StringComparer.Ordinal));
                if (enTagSet != trTagSet)
                    problems.Add($"{at}: the rich-text tags differ - English {Or(enTagSet)}, Turkish {Or(trTagSet)}.");

                if (Leading(entry.English) != Leading(entry.Turkish) || Trailing(entry.English) != Trailing(entry.Turkish))
                    problems.Add(at + ": the Turkish starts or ends with different spaces or line breaks than the English.");
            }
            return problems;
        }

        // Every key named in the game's scripts, with the English literal beside it.
        public static List<CodeKey> ScanScripts(string root)
        {
            var keys = new List<CodeKey>();
            string scripts = Path.Combine(root, ScriptsPath);
            foreach (string file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
                keys.AddRange(Scan(File.ReadAllText(file), file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/')));
            return keys;
        }

        public static List<CodeKey> Scan(string source, string file)
        {
            var keys = new List<CodeKey>();
            source = WithoutComments(source);
            foreach (Match match in CodeUse.Matches(source))
            {
                Group english = match.Groups["english"];
                keys.Add(new CodeKey
                {
                    Key = match.Groups["key"].Value,
                    English = english.Success ? UnescapeLiteral(english.Value) : null,
                    File = file,
                    Line = 1 + source.Take(match.Index).Count(c => c == '\n')
                });
            }
            return keys;
        }

        // The keys a content asset's text can be looked up by: "<id>.<field>" for every id in Data, and "room.<slug>.name"
        // for every room a floor names.
        public static HashSet<string> AssetKeys(string root)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(Path.Combine(root, DataPath), "*.asset", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                foreach (Match id in AssetId.Matches(text))
                    foreach (string field in ContentFields)
                        keys.Add(id.Groups["id"].Value + "." + field);
                foreach (Match room in RoomName.Matches(text))
                    keys.Add(ContentKeys.Room(UnquoteYaml(room.Groups["name"].Value)));
            }
            return keys;
        }

        // Code keys missing from the table, English that differs from the code's literal, and table keys nothing names.
        public static List<string> CodeProblems(LocTable table, IReadOnlyList<CodeKey> uses, ISet<string> assetKeys)
        {
            var problems = new List<string>();
            var named = new HashSet<string>(StringComparer.Ordinal);
            var english = new Dictionary<string, CodeKey>(StringComparer.Ordinal);
            foreach (CodeKey use in uses)
            {
                named.Add(use.Key);
                if (!table.TryGet(use.Key, out LocEntry entry))
                {
                    problems.Add($"{use} is used in the code but missing from the table.");
                    continue;
                }
                if (use.English == null)
                    continue;
                if (english.TryGetValue(use.Key, out CodeKey other) && other.English != use.English)
                    problems.Add($"{use} has other English than {other}: \"{use.English}\" and \"{other.English}\".");
                english[use.Key] = use;
                if (entry.English != use.English)
                    problems.Add($"{use}: the table's English \"{LocTable.Escape(entry.English)}\" (line {entry.Line}) is not the code's \"{LocTable.Escape(use.English)}\".");
            }
            foreach (LocEntry entry in table.Entries)
            {
                if (!named.Contains(entry.Key) && (assetKeys == null || !assetKeys.Contains(entry.Key)))
                    problems.Add($"{entry.Key} (line {entry.Line}) is in the table but no code names it and no content asset derives it.");
            }
            return problems;
        }

        // The source with its comments blanked (line breaks kept, so line numbers hold): an example in a comment names no key.
        public static string WithoutComments(string source)
        {
            var text = new StringBuilder(source.Length);
            bool inString = false, inChar = false, lineComment = false, blockComment = false, verbatim = false;
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';
                if (lineComment)
                {
                    if (c == '\n')
                    {
                        lineComment = false;
                        text.Append(c);
                    }
                    continue;
                }
                if (blockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        blockComment = false;
                        i++;
                    }
                    else if (c == '\n')
                    {
                        text.Append(c);
                    }
                    continue;
                }
                if (inString)
                {
                    text.Append(c);
                    if (verbatim && c == '"' && next == '"')
                    {
                        text.Append(next);
                        i++;
                    }
                    else if (!verbatim && c == '\\' && next != '\0')
                    {
                        text.Append(next);
                        i++;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                    continue;
                }
                if (inChar)
                {
                    text.Append(c);
                    if (c == '\\' && next != '\0')
                    {
                        text.Append(next);
                        i++;
                    }
                    else if (c == '\'')
                    {
                        inChar = false;
                    }
                    continue;
                }
                if (c == '/' && next == '/')
                {
                    lineComment = true;
                    i++;
                    continue;
                }
                if (c == '/' && next == '*')
                {
                    blockComment = true;
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    inString = true;
                    verbatim = i > 0 && source[i - 1] == '@';
                }
                else if (c == '\'')
                {
                    inChar = true;
                }
                text.Append(c);
            }
            return text.ToString();
        }

        // A C# regular string literal's value, quotes included in the input.
        public static string UnescapeLiteral(string literal)
        {
            var text = new StringBuilder(literal.Length);
            for (int i = 1; i < literal.Length - 1; i++)
            {
                char c = literal[i];
                if (c != '\\' || i + 1 >= literal.Length - 1)
                {
                    text.Append(c);
                    continue;
                }
                char next = literal[++i];
                switch (next)
                {
                    case 'n': text.Append('\n'); break;
                    case 't': text.Append('\t'); break;
                    case 'r': text.Append('\r'); break;
                    case '0': text.Append('\0'); break;
                    case '"': text.Append('"'); break;
                    case '\'': text.Append('\''); break;
                    case '\\': text.Append('\\'); break;
                    case 'u':
                        text.Append((char)Convert.ToInt32(literal.Substring(i + 1, 4), 16));
                        i += 4;
                        break;
                    default: text.Append('\\').Append(next); break;
                }
            }
            return text.ToString();
        }

        // A one-line YAML scalar as Unity writes it: plain, or single-quoted with '' for a quote.
        public static string UnquoteYaml(string value)
        {
            value = value.Trim();
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                return value.Substring(1, value.Length - 2);
            return value;
        }

        private static string Or(string list) => list.Length > 0 ? list : "none";

        private static string Leading(string text)
        {
            int i = 0;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
            return text.Substring(0, i);
        }

        private static string Trailing(string text)
        {
            int i = text.Length;
            while (i > 0 && char.IsWhiteSpace(text[i - 1]))
                i--;
            return text.Substring(i);
        }
    }
}
