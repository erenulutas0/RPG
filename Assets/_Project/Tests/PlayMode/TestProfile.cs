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
            if (seed != null)
                new ProfileStore(Folder).Save(seed);
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
