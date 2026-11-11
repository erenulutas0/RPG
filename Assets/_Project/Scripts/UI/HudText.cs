using System;

namespace Cryptforge.UI
{
    // The HUD's whole-number lines, made as string.Format makes them with one allocation, the line itself: string.Format
    // boxes every number and makes a string of each before it makes the line, seven allocations for the experience line,
    // which is made again for every experience drop picked up. Only a format of plain text and plain placeholders ("{0}",
    // "{1}", "{2}") over numbers from zero up is made here, as the gold and experience lines are; anything else - any
    // other placeholder (an alignment, a format string, a leading zero), an escaped or a stray brace, a placeholder past
    // the arguments, a negative number, no format at all - is handed to string.Format, so the text, or the exception, is
    // always string.Format's (HudTextTests). The main thread's alone: one buffer serves every call.
    internal static class HudText
    {
        // Small counts - a cooldown's seconds - each made once, so a countdown makes no string as it ticks.
        private const int CachedCounts = 100;
        private static readonly string[] Counts = new string[CachedCounts];
        private static char[] _buffer = new char[64];

        internal static string Format(string format, int arg0, int arg1) =>
            TryFormat(format, 2, arg0, arg1, 0, out string text) ? text : string.Format(format, arg0, arg1);

        internal static string Format(string format, int arg0, int arg1, int arg2) =>
            TryFormat(format, 3, arg0, arg1, arg2, out string text) ? text : string.Format(format, arg0, arg1, arg2);

        // value.ToString(): from zero to 99 the same string every time.
        internal static string Count(int value)
        {
            if (value < 0 || value >= CachedCounts)
                return value.ToString();
            return Counts[value] ??= value.ToString();
        }

        private static bool TryFormat(string format, int count, int arg0, int arg1, int arg2, out string text)
        {
            text = null;
            if (format == null || arg0 < 0 || arg1 < 0 || arg2 < 0)
                return false;
            int length = 0;
            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];
                // A closing brace outside a placeholder is an escape ("}}") or an error: string.Format's either way.
                if (c == '}')
                    return false;
                if (c != '{')
                {
                    Put(ref length, c);
                    continue;
                }
                // "{", one digit naming an argument, "}": anything else in a placeholder - more digits, a leading zero,
                // spaces, an alignment, a format string - is string.Format's.
                if (i + 2 >= format.Length || format[i + 2] != '}')
                    return false;
                int index = format[i + 1] - '0';
                if (index < 0 || index >= count)
                    return false;
                PutNumber(ref length, index == 0 ? arg0 : index == 1 ? arg1 : arg2);
                i += 2;
            }
            text = new string(_buffer, 0, length);
            return true;
        }

        private static void Put(ref int length, char c)
        {
            if (length == _buffer.Length)
                Array.Resize(ref _buffer, _buffer.Length * 2);
            _buffer[length++] = c;
        }

        // A number from zero up as int.ToString() writes it in every culture: its digits alone.
        private static void PutNumber(ref int length, int value)
        {
            int digits = 1;
            for (int rest = value / 10; rest > 0; rest /= 10)
                digits++;
            if (length + digits > _buffer.Length)
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, length + digits));
            for (int at = length + digits - 1; at >= length; at--)
            {
                _buffer[at] = (char)('0' + value % 10);
                value /= 10;
            }
            length += digits;
        }
    }
}
