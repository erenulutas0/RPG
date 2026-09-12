using System;

namespace Cryptforge.Combat
{
    public static class EncounterSchedule
    {
        // Encounter numbers start at 1; the authored sequence repeats once exhausted.
        public static int IndexFor(int encounterNumber, int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (encounterNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(encounterNumber));

            return (encounterNumber - 1) % count;
        }
    }
}
