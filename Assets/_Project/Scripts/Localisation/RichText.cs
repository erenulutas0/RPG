using System.Collections.Generic;

namespace Cryptforge.Localisation
{
    // uGUI's rich-text tags as the table uses them: <b>, <i>, <size=N>, <color=X>, <material=N> and their closings. A '<'
    // that starts none of them is a plain character. Loc.Upper keeps tags as they are; the table's checks (LocChecks) want
    // every tag closed in order, and the same tags in both languages.
    public static class RichText
    {
        private static readonly string[] Names = { "b", "i", "size", "color", "material" };

        // The length of the tag starting at text[at], or 0 when none does.
        public static int TagLength(string text, int at)
        {
            if (text == null || at < 0 || at >= text.Length || text[at] != '<')
                return 0;
            int i = at + 1;
            bool closing = i < text.Length && text[i] == '/';
            if (closing)
                i++;
            int nameStart = i;
            while (i < text.Length && text[i] >= 'a' && text[i] <= 'z')
                i++;
            if (!IsName(text, nameStart, i - nameStart))
                return 0;
            if (i < text.Length && text[i] == '=' && !closing)
            {
                i++;
                int valueStart = i;
                while (i < text.Length && text[i] != '>' && text[i] != '<' && text[i] != '\n')
                    i++;
                if (i == valueStart)
                    return 0;
            }
            if (i >= text.Length || text[i] != '>')
                return 0;
            return i - at + 1;
        }

        // Every tag of the text in order ("<size=34>", "</size>"), and whether they open and close in order.
        public static bool TryTags(string text, out List<string> tags, out string problem)
        {
            tags = new List<string>();
            problem = null;
            var open = new Stack<string>();
            if (string.IsNullOrEmpty(text))
                return true;
            for (int i = 0; i < text.Length; i++)
            {
                int length = TagLength(text, i);
                if (length == 0)
                    continue;
                string tag = text.Substring(i, length);
                tags.Add(tag);
                string name = NameOf(tag);
                if (tag[1] == '/')
                {
                    if (open.Count == 0 || open.Peek() != name)
                    {
                        problem = open.Count == 0 ? $"{tag} closes nothing" : $"{tag} closes <{open.Peek()}>";
                        return false;
                    }
                    open.Pop();
                }
                else
                {
                    open.Push(name);
                }
                i += length - 1;
            }
            if (open.Count > 0)
            {
                problem = $"<{open.Peek()}> is never closed";
                return false;
            }
            return true;
        }

        private static string NameOf(string tag)
        {
            int start = tag[1] == '/' ? 2 : 1;
            int end = start;
            while (end < tag.Length && tag[end] >= 'a' && tag[end] <= 'z')
                end++;
            return tag.Substring(start, end - start);
        }

        private static bool IsName(string text, int start, int length)
        {
            if (length == 0)
                return false;
            for (int n = 0; n < Names.Length; n++)
            {
                string name = Names[n];
                if (name.Length != length)
                    continue;
                bool same = true;
                for (int i = 0; i < length && same; i++)
                    same = text[start + i] == name[i];
                if (same)
                    return true;
            }
            return false;
        }
    }
}
