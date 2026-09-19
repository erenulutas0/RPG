using System;
using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // Where a run's seed comes from when nothing chose one: the clock and the process uptime, mixed through the run's
    // own generator so two runs started in the same second still differ. The number is not meant to be reproduced from
    // its inputs - it is recorded on run_start, and that record is what reproduces the run's offers.
    public static class RunSeeds
    {
        public static int Fresh() =>
            unchecked((int)RunRandom.Stream((int)DateTime.UtcNow.Ticks, (int)(DateTime.UtcNow.Ticks >> 32), Environment.TickCount & int.MaxValue).Next());

        // A seed from text: a whole number as written, otherwise a stable hash of the text, so a development file may
        // hold "7" or "kite-a" and both mean the same run every time.
        public static int Parse(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            string trimmed = text.Trim();
            if (int.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int number))
                return number;

            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < trimmed.Length; i++)
                    hash = (hash ^ trimmed[i]) * 16777619u;
                return (int)hash;
            }
        }
    }
}
