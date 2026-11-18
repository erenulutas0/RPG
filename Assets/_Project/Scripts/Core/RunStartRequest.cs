using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // The map the player chose for the next run (MapChoiceView), and the hero and difficulty tier chosen with it, carried
    // across the scene reload that starts it: a run is composed in CombatSetup.Awake, so a deeper map, another hero or
    // another tier is started by reloading the scene with this set, and Awake takes it.
    // A deliberate exception to the no-static-state rule (07_CODE_STANDARDS), like ProfileLocation.DirectoryOverride:
    // nothing else survives a reload, and the request is consumed by the very next scene load, so it never outlives it.
    public static class RunStartRequest
    {
        private static int _floor;
        private static string _heroId;
        private static int _tier = -1;

        // Whether a choice waits for the next load (for tests).
        public static bool IsPending => _floor > 0;

        // The hero chosen with the waiting map, or null when the player kept the scene's hero (for tests).
        public static string PendingHeroId => _heroId;

        // The tier chosen with the waiting map, or null when the player kept the profile's (for tests).
        public static DifficultyTier? PendingTier => _tier >= 0 ? (DifficultyTier)_tier : (DifficultyTier?)null;

        public static void Set(int floorNumber) => Set(floorNumber, null);

        // A map and the hero to play it with; a hero alone means nothing without a map.
        public static void Set(int floorNumber, string heroId) => Set(floorNumber, heroId, null);

        // A map, the hero and the difficulty tier to play it with (null: the profile's own choice).
        public static void Set(int floorNumber, string heroId, DifficultyTier? tier)
        {
            _floor = floorNumber > 0 ? floorNumber : 0;
            _heroId = _floor > 0 && !string.IsNullOrEmpty(heroId) ? heroId : null;
            _tier = _floor > 0 && tier.HasValue && DifficultyTiers.IsDefined((int)tier.Value) ? (int)tier.Value : -1;
        }

        // The chosen floor, cleared as it is read; 0 when the player chose nothing for this load.
        public static int Take() => Take(out _);

        // The chosen floor and hero (null: the profile's), both cleared as they are read.
        public static int Take(out string heroId) => Take(out heroId, out _);

        // The chosen floor, hero and tier (null: the profile's), all cleared as they are read.
        public static int Take(out string heroId, out DifficultyTier? tier)
        {
            int floor = _floor;
            heroId = _heroId;
            tier = PendingTier;
            Clear();
            return floor;
        }

        public static void Clear()
        {
            _floor = 0;
            _heroId = null;
            _tier = -1;
        }
    }
}
