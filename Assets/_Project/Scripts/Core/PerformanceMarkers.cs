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

        // Every view's LateUpdate, one scope a view whatever its instances, and the foundry's depth placement and restore in
        // the camera's own callbacks (2026-11-11: the first level-up's frame held about 7 ms of LateUpdate that no marker
        // named, and a spawn's and a quiet frame 4 ms each).
        public const string LateCameraName = "Cryptforge.Late.Camera";
        public const string LateEffectsName = "Cryptforge.Late.Effects";
        public const string LateShadowsName = "Cryptforge.Late.Shadows";
        public const string LateExitSealName = "Cryptforge.Late.ExitSeal";
        public const string LateHudArtName = "Cryptforge.Late.HudArt";
        public const string LatePitName = "Cryptforge.Late.Pit";
        public const string LatePuppetName = "Cryptforge.Late.Puppet";
        public const string LateFramesName = "Cryptforge.Late.RenderedFrames";
        public const string LateAltarName = "Cryptforge.Late.Altar";
        public const string LateWardenName = "Cryptforge.Late.Warden";
        public const string LateScreenArtName = "Cryptforge.Late.ScreenArt";
        public const string LateVisibilityName = "Cryptforge.Late.Visibility";
        public const string LateBackdropName = "Cryptforge.Late.Backdrop";
        public const string DepthPlaceName = "Cryptforge.Depth.Place";
        public const string DepthRestoreName = "Cryptforge.Depth.Restore";

        // Every scope above, for whoever reads them by name (the perf probe). Reading it creates them all.
        public static readonly string[] PickNames =
        {
            PickName, PickPanelName, PickTelemetryName, TelemetryWriteName, PickFeedbackName, FeedbackDrawName,
            PickAddWeaponName, PickHeroStatsName, PickPauseName, PickHudName, PickLoadoutName,
            LateCameraName, LateEffectsName, LateShadowsName, LateExitSealName, LateHudArtName, LatePitName, LatePuppetName,
            LateFramesName, LateAltarName, LateWardenName, LateScreenArtName, LateVisibilityName, LateBackdropName,
            DepthPlaceName, DepthRestoreName
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

        public static readonly ProfilerMarker LateCamera = new ProfilerMarker(ProfilerCategory.Scripts, LateCameraName);
        public static readonly ProfilerMarker LateEffects = new ProfilerMarker(ProfilerCategory.Scripts, LateEffectsName);
        public static readonly ProfilerMarker LateShadows = new ProfilerMarker(ProfilerCategory.Scripts, LateShadowsName);
        public static readonly ProfilerMarker LateExitSeal = new ProfilerMarker(ProfilerCategory.Scripts, LateExitSealName);
        public static readonly ProfilerMarker LateHudArt = new ProfilerMarker(ProfilerCategory.Scripts, LateHudArtName);
        public static readonly ProfilerMarker LatePit = new ProfilerMarker(ProfilerCategory.Scripts, LatePitName);
        public static readonly ProfilerMarker LatePuppet = new ProfilerMarker(ProfilerCategory.Scripts, LatePuppetName);
        public static readonly ProfilerMarker LateFrames = new ProfilerMarker(ProfilerCategory.Scripts, LateFramesName);
        public static readonly ProfilerMarker LateAltar = new ProfilerMarker(ProfilerCategory.Scripts, LateAltarName);
        public static readonly ProfilerMarker LateWarden = new ProfilerMarker(ProfilerCategory.Scripts, LateWardenName);
        public static readonly ProfilerMarker LateScreenArt = new ProfilerMarker(ProfilerCategory.Scripts, LateScreenArtName);
        public static readonly ProfilerMarker LateVisibility = new ProfilerMarker(ProfilerCategory.Scripts, LateVisibilityName);
        public static readonly ProfilerMarker LateBackdrop = new ProfilerMarker(ProfilerCategory.Scripts, LateBackdropName);
        public static readonly ProfilerMarker DepthPlace = new ProfilerMarker(ProfilerCategory.Scripts, DepthPlaceName);
        public static readonly ProfilerMarker DepthRestore = new ProfilerMarker(ProfilerCategory.Scripts, DepthRestoreName);
    }
}
