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

        // A level-up and the card taken for it, step by step (2026-11-11: the perf probe's first level-up and its pick
        // cost 26 ms in the Editor where later ones cost 6-10). Pick is the whole selection from the hand that made it -
        // the choice panel's tap, the autopilot or the probe - the service's own work included; the others are the
        // scene's side of an offer opening or closing and of a card applied, wherever it runs, so a frame's level-up and
        // pick are attributed handler by handler and the service's pure work is what Pick holds beyond them.
        public const string PickName = "Cryptforge.Pick";
        // RunChoiceView showing an offer or hiding it.
        public const string PickPanelName = "Cryptforge.Pick.Panel";
        // RunTelemetry's handlers for an offer, a pick and a weapon taken (its flush at an open choice included).
        public const string PickTelemetryName = "Cryptforge.Pick.Telemetry";
        // Every telemetry write to disk, at a choice, a run's end or the periodic flush.
        public const string TelemetryWriteName = "Cryptforge.TelemetryWrite";
        // RunFeedbackView arming a level's burst, a card's ring or a weapon's embers.
        public const string PickFeedbackName = "Cryptforge.Pick.Feedback";
        // RunFeedbackView moving and drawing its rings and sparks, every frame.
        public const string FeedbackDrawName = "Cryptforge.FeedbackDraw";
        // The hero's attack taking a weapon picked up.
        public const string PickAddWeaponName = "Cryptforge.Pick.AddWeapon";
        // The hero's stat sheet applied to its health, walk and bar after a hero card or tome.
        public const string PickHeroStatsName = "Cryptforge.Pick.HeroStats";
        // The run frozen or released for an open choice.
        public const string PickPauseName = "Cryptforge.Pick.Pause";
        // The HUD's weapon line, legacy badges and danger readout.
        public const string PickHudName = "Cryptforge.Pick.Hud";
        // The loadout row's weapon and tome badges.
        public const string PickLoadoutName = "Cryptforge.Pick.Loadout";

        // Every scope above, for whoever reads them by name (the perf probe). Reading it creates them all.
        public static readonly string[] PickNames =
        {
            PickName, PickPanelName, PickTelemetryName, TelemetryWriteName, PickFeedbackName, FeedbackDrawName,
            PickAddWeaponName, PickHeroStatsName, PickPauseName, PickHudName, PickLoadoutName
        };

        // A wave entering: every enemy instantiated, drawn and set up.
        public static readonly ProfilerMarker StartWave = new ProfilerMarker(ProfilerCategory.Scripts, StartWaveName);
        public static readonly ProfilerMarker BuildBackdrop = new ProfilerMarker(ProfilerCategory.Scripts, BuildBackdropName);
        public static readonly ProfilerMarker BuildPlatform = new ProfilerMarker(ProfilerCategory.Scripts, BuildPlatformName);

        public static readonly ProfilerMarker Pick = new ProfilerMarker(ProfilerCategory.Scripts, PickName);
        public static readonly ProfilerMarker PickPanel = new ProfilerMarker(ProfilerCategory.Scripts, PickPanelName);
        public static readonly ProfilerMarker PickTelemetry = new ProfilerMarker(ProfilerCategory.Scripts, PickTelemetryName);
        public static readonly ProfilerMarker TelemetryWrite = new ProfilerMarker(ProfilerCategory.Scripts, TelemetryWriteName);
        public static readonly ProfilerMarker PickFeedback = new ProfilerMarker(ProfilerCategory.Scripts, PickFeedbackName);
        public static readonly ProfilerMarker FeedbackDraw = new ProfilerMarker(ProfilerCategory.Scripts, FeedbackDrawName);
        public static readonly ProfilerMarker PickAddWeapon = new ProfilerMarker(ProfilerCategory.Scripts, PickAddWeaponName);
        public static readonly ProfilerMarker PickHeroStats = new ProfilerMarker(ProfilerCategory.Scripts, PickHeroStatsName);
        public static readonly ProfilerMarker PickPause = new ProfilerMarker(ProfilerCategory.Scripts, PickPauseName);
        public static readonly ProfilerMarker PickHud = new ProfilerMarker(ProfilerCategory.Scripts, PickHudName);
        public static readonly ProfilerMarker PickLoadout = new ProfilerMarker(ProfilerCategory.Scripts, PickLoadoutName);
    }
}
