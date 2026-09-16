using Unity.Profiling;

namespace Cryptforge.Core
{
    // Profiler markers around the work a performance session wants timed on its own. They cost nothing outside development
    // builds; FrameTimeProbe reads them on a device and the Unity Profiler shows them in the Editor.
    public static class PerformanceMarkers
    {
        public const string StartWaveName = "Cryptforge.StartWave";
        public const string BuildBackdropName = "Cryptforge.BuildBackdrop";
        public const string BuildPlatformName = "Cryptforge.BuildPlatform";

        // A wave entering: every enemy instantiated, drawn and set up.
        public static readonly ProfilerMarker StartWave = new ProfilerMarker(ProfilerCategory.Scripts, StartWaveName);
        public static readonly ProfilerMarker BuildBackdrop = new ProfilerMarker(ProfilerCategory.Scripts, BuildBackdropName);
        public static readonly ProfilerMarker BuildPlatform = new ProfilerMarker(ProfilerCategory.Scripts, BuildPlatformName);
    }
}
