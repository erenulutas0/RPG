using System.IO;
using Cryptforge.Save;

namespace Cryptforge.Analytics
{
    // The telemetry folder sits inside the profile folder but apart from the profile files, so the profile's own recovery
    // never sees it, and scene tests that point the profile at a temporary folder isolate (and delete) telemetry with it.
    public static class TelemetryLocation
    {
        public const string FolderName = "telemetry";

        public static string Resolve() => Path.Combine(ProfileLocation.Resolve(), FolderName);
    }
}
