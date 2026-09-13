using UnityEngine;

namespace Cryptforge.Save
{
    public static class ProfileLocation
    {
        // Test seam and a deliberate exception to the no-static-state rule (07_CODE_STANDARDS): scene tests point the
        // profile at a temporary folder so they never read or overwrite a player's progress. Game code never sets it.
        public static string DirectoryOverride { get; set; }

        public static string Resolve() =>
            string.IsNullOrEmpty(DirectoryOverride) ? Application.persistentDataPath : DirectoryOverride;
    }
}
