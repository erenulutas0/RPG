namespace Cryptforge.Arena
{
    // FNV-1a over integers and strings: the duel's fingerprint, a snapshot's hash and the rules' hash. Integer arithmetic
    // only, so every runtime gives the same value; strings are read char by char (UTF-16 units), never through a culture.
    public static class ArenaHash
    {
        public const ulong Seed = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Mix(ulong hash, long value)
        {
            unchecked
            {
                ulong v = (ulong)value;
                for (int i = 0; i < 8; i++)
                {
                    hash = (hash ^ (v & 0xFF)) * Prime;
                    v >>= 8;
                }
                return hash;
            }
        }

        public static ulong Mix(ulong hash, string text)
        {
            unchecked
            {
                if (text == null)
                    return Mix(hash, -1);
                hash = Mix(hash, text.Length);
                for (int i = 0; i < text.Length; i++)
                    hash = (hash ^ text[i]) * Prime;
                return hash;
            }
        }

        // Sixteen upper-case hex digits, as stored and logged.
        public static string Hex(ulong hash) => hash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
    }
}
