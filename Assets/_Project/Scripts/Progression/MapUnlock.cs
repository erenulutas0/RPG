using System.Globalization;

namespace Cryptforge.Progression
{
    // Which map starts the map choice (MapChoiceView, docs/53) offers: a deeper map opens once the player has reached it
    // in a real run - the Quicksilver Vaults once floor 1 is cleared, the Gloam Gardens once floor 2 is
    // (PlayerProfile.DeepestFloorCleared). The Foundry is always open; anything that is not a start never is.
    // Development starts (start-floor.txt, a test's RunStartRequest) do not ask: only the panel and StartOnMap do.
    public static class MapUnlock
    {
        public const string HintFormat = "Clear floor {0} to unlock";

        public static bool IsOpen(int floorNumber, int deepestFloorCleared) =>
            DescentStart.IsStart(floorNumber) &&
            (floorNumber == DescentStart.FirstFloor || deepestFloorCleared >= floorNumber - 1);

        // How a locked map is opened: "Clear floor 1 to unlock" for the Vaults. Numbers in the invariant culture.
        public static string Hint(int floorNumber) =>
            string.Format(CultureInfo.InvariantCulture, HintFormat, floorNumber - 1);
    }
}
