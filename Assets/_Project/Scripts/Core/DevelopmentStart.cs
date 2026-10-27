using System;
using System.IO;
using System.Security;
using Cryptforge.Content;
using Cryptforge.Save;
using UnityEngine;

namespace Cryptforge.Core
{
    // Lets a development build or a scene test start the run on a floor other than the one Gameplay.unity carries, without
    // editing the scene: a line holding a floor id in <profile folder>/development/start-floor.txt selects a floor from
    // Resources/Development. Only the Editor and a development build ever look, so a player's release build always starts
    // on the authored floor whatever the file says, and the proof floor stays unreachable. A missing, empty, unreadable or
    // unknown id leaves the authored floor in place too. EncounterController.Initialize asks once, inside Awake: this must
    // therefore never throw and never write to the console, since a scene test asserts no unexpected logs.
    public static class DevelopmentStart
    {
        public const string FolderName = "development";
        public const string StartFloorFileName = "start-floor.txt";
        // A run seed, so two development runs can be handed the same offers: a whole number, or any text, hashed.
        public const string RunSeedFileName = "run-seed.txt";
        // Any text in it arms the depth proof on the foundry map (DepthProofArrangement), a device check of structure depth.
        public const string DepthProofFileName = "depth-proof.txt";
        // The Resources subfolder the development-only floors live in; nothing else may live there, because every floor in
        // it is a candidate and Resources folders ship with the build whether or not anything reads them.
        public const string ResourcesFolder = "Development";

        // Inside the profile folder but apart from the profile files, like the telemetry folder, so a scene test that
        // points the profile at a temporary folder writes and deletes this with it.
        public static string StartFloorPath => Path.Combine(ProfileLocation.Resolve(), FolderName, StartFloorFileName);
        public static string RunSeedPath => Path.Combine(ProfileLocation.Resolve(), FolderName, RunSeedFileName);
        public static string RegionPreviewPath => Path.Combine(ProfileLocation.Resolve(), FolderName, "region-preview.txt");

        // Separate from floor selection: this changes only the arena, not authored progression. The foundry is the game's
        // first floor and every release build plays it. The Editor and a development build may still name the legacy
        // "hall" (the scene tests' default arena, see TestProfile) or the "wide" preview; "foundry" or no file is the
        // foundry.
        public static bool WideRegionPreview() => (Application.isEditor || Debug.isDebugBuild) &&
            string.Equals(ReadFirstLine(RegionPreviewPath), "wide", StringComparison.Ordinal);
        public static bool HallRegion() => (Application.isEditor || Debug.isDebugBuild) &&
            string.Equals(ReadFirstLine(RegionPreviewPath), "hall", StringComparison.Ordinal);
        public static bool FoundryRegion() => !WideRegionPreview() && !HallRegion();

        public static string GateArrivalPath => Path.Combine(ProfileLocation.Resolve(), FolderName, "gate-arrival.txt");
        public static bool GateArrival() => FoundryRegion() && !DepthProof() &&
            !string.IsNullOrEmpty(ReadFirstLine(GateArrivalPath));

        // Any text in it hands the foundry run to DevelopmentAutopilot, a hands-off player for device checks.
        public static string AutopilotPath => Path.Combine(ProfileLocation.Resolve(), FolderName, "autopilot.txt");
        public static bool Autopilot() => (Application.isEditor || Debug.isDebugBuild) && FoundryRegion() &&
            !string.IsNullOrEmpty(ReadFirstLine(AutopilotPath));

        // How the autopilot plays, from the same file: "linger" and "idle" are measurement modes (DevelopmentAutopilot),
        // anything else plays the floor.
        public static string AutopilotMode() => Autopilot() ? (ReadFirstLine(AutopilotPath) ?? string.Empty).Trim() : string.Empty;

        // A whole number in it pads the foundry's first wave with that many more of its first enemy: a device probe of the
        // frame time at the horde's crowd before the horde itself exists (docs/41). Nothing without the file.
        public static string HordeProbePath => Path.Combine(ProfileLocation.Resolve(), FolderName, "horde-probe.txt");
        public static int HordeProbe()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return 0;
            return int.TryParse(ReadFirstLine(HordeProbePath), out int count) && count > 0 ? count : 0;
        }

        // A number of seconds in it starts each foundry floor's clock that far on (HordeClock): a look at the last minute
        // and the overrun on a device or in a test without waiting five minutes. Nothing without the file.
        public static string FloorClockPath => Path.Combine(ProfileLocation.Resolve(), FolderName, "floor-clock.txt");
        public static float FloorClockStart()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return 0f;
            return float.TryParse(ReadFirstLine(FloorClockPath), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float seconds) && seconds > 0f ? seconds : 0f;
        }

        // A hero id in it plays that hero whatever the profile chose (docs/45). Nothing without the file.
        public static string HeroPath => Path.Combine(ProfileLocation.Resolve(), FolderName, "hero.txt");
        public static string HeroId() => Application.isEditor || Debug.isDebugBuild ? ReadFirstLine(HeroPath) : null;

        // Development builds and the Editor may play any hero, locked or not, so the owner can try one early.
        public static bool AllHeroesOpen() => Application.isEditor || Debug.isDebugBuild;

        public static string DepthProofPath => Path.Combine(ProfileLocation.Resolve(), FolderName, DepthProofFileName);

        // Under the seed's rules: only the Editor and a development build look, and a missing, empty or unreadable file
        // leaves the run alone. It only arranges the foundry, so it means nothing without the foundry preview.
        public static bool DepthProof() => (Application.isEditor || Debug.isDebugBuild) &&
            !string.IsNullOrEmpty(ReadFirstLine(DepthProofPath));

        // The seed the file names, under the same rules as the floor: only the Editor and a development build look, and
        // a missing, empty or unreadable file means no seed was chosen.
        public static bool TryRunSeed(out int seed)
        {
            seed = 0;
            if (!Application.isEditor && !Debug.isDebugBuild)
                return false;

            string text = ReadFirstLine(RunSeedPath);
            if (string.IsNullOrEmpty(text))
                return false;
            seed = RunSeeds.Parse(text);
            return true;
        }

        // The floor a run starts on: the one the file names, when this is the Editor or a development build and the id
        // matches a floor in Resources/Development; the authored floor in every other case. Resources are only touched
        // when a file actually named something, so a normal run pays nothing for this hook at startup.
        public static FloorDefinition FirstFloor(FloorDefinition authored)
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return authored;

            string id = ReadStartFloorId();
            if (string.IsNullOrEmpty(id))
                return authored;

            FloorDefinition[] candidates = Resources.LoadAll<FloorDefinition>(ResourcesFolder);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && string.Equals(candidates[i].Id, id, StringComparison.Ordinal))
                    return candidates[i];
            }
            return authored;
        }

        // The first non-empty trimmed line of the file, or null when there is no file or nothing readable in it. Every
        // failure a path from outside this code can produce is swallowed, not only the two the storage itself raises:
        // an exception here would end the run inside Awake, before the player ever sees the arena.
        private static string ReadStartFloorId() => ReadFirstLine(StartFloorPath);

        private static string ReadFirstLine(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string id = lines[i].Trim();
                    if (id.Length > 0)
                        return id;
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                exception is ArgumentException || exception is NotSupportedException || exception is SecurityException)
            {
                return null;
            }
            return null;
        }
    }
}
