using System;
using System.Collections.Generic;
using System.Text;

namespace Cryptforge.Localisation
{
    // One row of the string table: a key, its English (a copy of the code's or the asset's, for review), its Turkish and a
    // note for the translator.
    public sealed class LocEntry
    {
        public LocEntry(string key, string english, string turkish, string note, int line)
        {
            Key = key;
            English = english ?? string.Empty;
            Turkish = turkish ?? string.Empty;
            Note = note ?? string.Empty;
            Line = line;
        }

        public string Key { get; }
        public string English { get; }
        public string Turkish { get; }
        public string Note { get; }
        // The row's line in the file, for messages.
        public int Line { get; }

        public string For(Language language) => language == Language.Turkish ? Turkish : English;
    }

    // The string table (docs/60): one tab-separated file, Resources/Localisation/Strings.txt, with the columns key, en, tr
    // and note under a header row. Blank lines and lines starting with '#' are skipped, so the file can be grouped. A cell
    // writes a line break as \n, a tab as \t and a backslash as \\; a cell a spreadsheet wrapped in double quotes is
    // unwrapped. Keys are lower-case snake_case words joined by dots ("anvil.title", "enemy_grunt.name").
    //
    // A malformed row is skipped and named in Problems rather than thrown: a broken translation must never stop the game.
    public sealed class LocTable
    {
        public const string Header = "key\ten\ttr\tnote";
        public static readonly LocTable Empty = new LocTable();

        private readonly Dictionary<string, LocEntry> _entries = new Dictionary<string, LocEntry>(StringComparer.Ordinal);
        private readonly List<LocEntry> _ordered = new List<LocEntry>();
        private readonly List<string> _problems = new List<string>();

        public int Count => _ordered.Count;
        public IReadOnlyList<LocEntry> Entries => _ordered;
        public IReadOnlyList<string> Problems => _problems;

        public bool TryGet(string key, out LocEntry entry)
        {
            if (key == null)
            {
                entry = null;
                return false;
            }
            return _entries.TryGetValue(key, out entry);
        }

        public bool Contains(string key) => key != null && _entries.ContainsKey(key);

        public static LocTable Parse(string text)
        {
            var table = new LocTable();
            if (string.IsNullOrEmpty(text))
            {
                table._problems.Add("The table is empty.");
                return table;
            }
            if (text[0] == '\uFEFF')
                text = text.Substring(1);
            string[] lines = text.Split('\n');
            bool headerSeen = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Trim().Length == 0 || line[0] == '#')
                    continue;
                int number = i + 1;
                string[] cells = line.Split('\t');
                if (!headerSeen)
                {
                    headerSeen = true;
                    if (cells.Length >= 3 && cells[0] == "key" && cells[1] == "en" && cells[2] == "tr" &&
                        (cells.Length < 4 || cells[3] == "note"))
                        continue;
                    table._problems.Add($"Line {number}: the first row must be the header \"key<TAB>en<TAB>tr<TAB>note\".");
                }
                if (cells.Length < 3)
                {
                    table._problems.Add($"Line {number}: {cells.Length} cell(s); a row is key, en, tr and an optional note, tab-separated.");
                    continue;
                }
                bool extra = false;
                for (int c = 4; c < cells.Length; c++)
                    extra |= cells[c].Trim().Length > 0;
                if (extra)
                {
                    table._problems.Add($"Line {number}: text after the note column (a tab inside a cell is written \\t).");
                    continue;
                }
                string key = cells[0].Trim();
                if (!IsKey(key))
                {
                    table._problems.Add($"Line {number}: \"{key}\" is not a key (lower-case snake_case words joined by dots).");
                    continue;
                }
                if (table._entries.ContainsKey(key))
                {
                    table._problems.Add($"Line {number}: {key} is already on line {table._entries[key].Line}.");
                    continue;
                }
                var entry = new LocEntry(key, Cell(cells[1]), Cell(cells[2]), cells.Length > 3 ? Cell(cells[3]) : string.Empty, number);
                table._entries.Add(key, entry);
                table._ordered.Add(entry);
            }
            if (!headerSeen)
                table._problems.Add("The table has no header row.");
            return table;
        }

        // Lower-case ASCII letters, digits and underscores, at least two parts joined by dots. Checked by character range,
        // never by culture-aware casing.
        public static bool IsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            int parts = 1;
            bool partEmpty = true;
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (c == '.')
                {
                    if (partEmpty)
                        return false;
                    parts++;
                    partEmpty = true;
                    continue;
                }
                bool valid = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!valid)
                    return false;
                partEmpty = false;
            }
            return parts >= 2 && !partEmpty;
        }

        // A cell's text: a spreadsheet's double quotes taken off, then \n, \t and \\ read.
        private static string Cell(string cell) => Unescape(Unquote(cell));

        private static string Unquote(string cell)
        {
            if (cell.Length >= 2 && cell[0] == '"' && cell[cell.Length - 1] == '"')
                return cell.Substring(1, cell.Length - 2).Replace("\"\"", "\"");
            return cell;
        }

        public static string Unescape(string cell)
        {
            if (cell.IndexOf('\\') < 0)
                return cell;
            var text = new StringBuilder(cell.Length);
            for (int i = 0; i < cell.Length; i++)
            {
                char c = cell[i];
                if (c == '\\' && i + 1 < cell.Length)
                {
                    char next = cell[i + 1];
                    if (next == 'n') { text.Append('\n'); i++; continue; }
                    if (next == 't') { text.Append('\t'); i++; continue; }
                    if (next == '\\') { text.Append('\\'); i++; continue; }
                }
                text.Append(c);
            }
            return text.ToString();
        }

        // The cell for a text: the inverse of Unescape, for tools that write the table.
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            return text.Replace("\\", "\\\\").Replace("\r", string.Empty).Replace("\n", "\\n").Replace("\t", "\\t");
        }
    }
}
