namespace Cryptforge.Core
{
    // The map the player chose for the next run (MapChoiceView), carried across the scene reload that starts it: a run is
    // composed in CombatSetup.Awake, so a deeper map is started by reloading the scene with this set, and Awake takes it.
    // A deliberate exception to the no-static-state rule (07_CODE_STANDARDS), like ProfileLocation.DirectoryOverride:
    // nothing else survives a reload, and the request is consumed by the very next scene load, so it never outlives it.
    public static class RunStartRequest
    {
        private static int _floor;

        // Whether a choice waits for the next load (for tests).
        public static bool IsPending => _floor > 0;

        public static void Set(int floorNumber) => _floor = floorNumber > 0 ? floorNumber : 0;

        // The chosen floor, cleared as it is read; 0 when the player chose nothing for this load.
        public static int Take()
        {
            int floor = _floor;
            _floor = 0;
            return floor;
        }

        public static void Clear() => _floor = 0;
    }
}
