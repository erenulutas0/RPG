using System;
using System.IO;
using Cryptforge.Core;
using Cryptforge.Save;

namespace Cryptforge.Tests
{
    // Gives each scene test its own profile folder, optionally seeded, so tests start from known progress and never
    // read or overwrite the Editor's real persistent data. Call Begin before loading the scene and End after unloading.
    internal static class TestProfile
    {
        public static string Folder { get; private set; }

        public static void Begin(PlayerProfile seed = null)
        {
            End();
            Folder = Path.Combine(Path.GetTempPath(), "cryptforge-scene-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);
            ProfileLocation.DirectoryOverride = Folder;
            // The game plays the foundry by default. Scene tests that measure the legacy hall's geometry keep it by
            // naming it; foundry tests overwrite this with "foundry" after Begin.
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "hall");
            // Every scene test starts its run at once, as before the map choice (MapChoiceView); the map choice's own tests
            // delete this. No map chosen by an earlier test carries into this one either.
            File.WriteAllText(DevelopmentStart.MapChoicePath, "off");
            RunStartRequest.Clear();
            if (seed != null)
                new ProfileStore(Folder).Save(seed);
        }

        // Starts the run's hero with this weapon instead of its own (development/start-weapon.txt), after Begin: every
        // hero starts with its own weapon only since 2026-11-22, so a scene test that plays the Vanguard with the Staff or
        // the Daggers names it here as well as seeding the profile. Null or empty does nothing.
        public static void StartWith(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartWeaponPath));
            File.WriteAllText(DevelopmentStart.StartWeaponPath, weaponId);
        }

        public static PlayerProfile ReadSaved() => new ProfileStore(Folder).Load();

        public static bool HasSavedFile => File.Exists(Path.Combine(Folder, ProfileStore.FileName));

        public static void End()
        {
            ProfileLocation.DirectoryOverride = null;
            if (Folder != null && Directory.Exists(Folder))
                Directory.Delete(Folder, true);
            Folder = null;
        }
    }
}
