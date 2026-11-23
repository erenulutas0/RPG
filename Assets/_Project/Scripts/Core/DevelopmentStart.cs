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
        // anything else plays the floor; a cohort's name among the words ("standing", "first-card", "clumsy",
        // "auto-farmer"; AutopilotCohort.FromSwitch) says who plays it, the careful pilot when none does.
        public static string AutopilotMode() => Autopilot() ? (ReadFirstLine(AutopilotPath) ?? string.Empty).Trim() : string.Empty;

        // A whole number in it pads the foundry's first wave with that many more of its first enemy: a device probe of the
        // frame time at the horde's crowd before the horde itself exists (docs/41). Nothing without the file.
        public static string HordeProbePath => Path.Combine(ProfileLocation.Resolve(), FolderName, "horde-probe.txt");
        public static int HordeProbe()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return 0;
            return int.TryParse(ReadFirstLine(HordeProbePath), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0 ? count : 0;
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

        // docs/57's skills 2 and 3 before phase 4 opens them: "2" in it gives a run the hero's skill 2 beside its first, "3"
        // its skills 2 and 3 - the Editor's measurement with the skills on (HordePacingMeasurement, the parity table), and a
        // device session's. Only the Editor and a development build look; anything else, or no file, is skill 1 alone, as
        // every release build plays until the first clear and the Forge open them.
        public static string SkillsPath => Path.Combine(ProfileLocation.Resolve(), FolderName, "skills.txt");
        public static int SkillSlots()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return 1;
            return int.TryParse(ReadFirstLine(SkillsPath), out int slots) && slots >= 2 && slots <= 3 ? slots : 1;
        }

        // docs/62's P0a, the Outworks mechanics probe: any text in it but "off" has a run that begins on floor 1 of the foundry
        // play the Outworks phase in the court before the floor's halls (EncounterController.Outworks.cs) - the Tithe Anvils,
        // the Keeper and its key, the pads over the pit, the Bell and the Ascent Stair - with floor 1's halls trimmed as
        // OutworksRule says. Only the Editor and a development build look; nothing without the file, so a release build and
        // the default batch play the shipped floor.
        public const string OutworksFileName = "outworks.txt";
        public static string OutworksPath => Path.Combine(ProfileLocation.Resolve(), FolderName, OutworksFileName);
        public static bool Outworks()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return false;
            string line = ReadFirstLine(OutworksPath);
            return FoundryRegion() && !string.IsNullOrEmpty(line) && !string.Equals(line, "off", StringComparison.Ordinal);
        }

        // docs/63's weapon roster, a research lever: any text in it but "off" has a foundry run's cards draw from the roster -
        // the hero's own weapon, then the shared weapons (the Daggers, the Magma Mortar, the Forge Bow, the Cinder Saw), every other hero's
        // own weapon never (CombatSetup, WeaponRoster). Only the Editor and a development build look; nothing without the
        // file, so a release build and the default batch play today's catalogue.
        public const string WeaponRosterFileName = "weapon-roster.txt";
        public static string WeaponRosterPath => Path.Combine(ProfileLocation.Resolve(), FolderName, WeaponRosterFileName);
        public static bool WeaponRoster()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return false;
            string line = ReadFirstLine(WeaponRosterPath);
            return FoundryRegion() && !string.IsNullOrEmpty(line) && !string.Equals(line, "off", StringComparison.Ordinal);
        }

        // A hero id in it plays that hero whatever the profile chose (docs/45). Nothing without the file.
        public static string HeroPath => Path.Combine(ProfileLocation.Resolve(), FolderName, "hero.txt");
        public static string HeroId() => Application.isEditor || Debug.isDebugBuild ? ReadFirstLine(HeroPath) : null;

        // Development builds and the Editor may play any hero, locked or not, so the owner can try one early.
        public static bool AllHeroesOpen() => Application.isEditor || Debug.isDebugBuild;

        // The Vanguard wears its Blender-rendered set (RenderedCharacterAnimator; the owner, 2026-11-22), which replaced the
        // cut-out puppet (PuppetAnimator, the owner's 2026-10-29 choice) and the painted frames (VanguardAnimator) before
        // it. The older looks stay for comparison: this file existing, whatever it holds (an empty one from
        // `adb shell touch` counts), gives the Vanguard back its painted frames; puppet.txt (below) its puppet. They are
        // the Vanguard's looks alone and leave every other hero as it is. Nothing without the files.
        public const string PaintedFramesFileName = "painted-frames.txt";
        public static string PaintedFramesPath => Path.Combine(ProfileLocation.Resolve(), FolderName, PaintedFramesFileName);
        public static bool PaintedFrames() => (Application.isEditor || Debug.isDebugBuild) && Exists(PaintedFramesPath);
        public const string PuppetFileName = "puppet.txt";
        public static string PuppetPath => Path.Combine(ProfileLocation.Resolve(), FolderName, PuppetFileName);
        public static bool Puppet() => (Application.isEditor || Debug.isDebugBuild) && Exists(PuppetPath);

        // A weapon id in it starts the run's hero with that weapon instead of its own (the Editor or a development build
        // only). Every hero starts with its own weapon only since 2026-11-22 (the Vanguard its Sword, Ilya its Staff), so
        // this is how the scene tests that pin the Staff's and the Daggers' runs to the simulation, and the research's
        // M1 (horde-weapon.txt), still start the Vanguard with them. An unknown id, an empty or a missing file changes
        // nothing.
        public const string StartWeaponFileName = "start-weapon.txt";
        public static string StartWeaponPath => Path.Combine(ProfileLocation.Resolve(), FolderName, StartWeaponFileName);
        public static string StartWeaponId()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return null;
            string id = ReadFirstLine(StartWeaponPath)?.Trim();
            return string.IsNullOrEmpty(id) ? null : id;
        }

        // docs/58 7.4: how the offline fake rewarded video ends, so every state of the result screen's offer can be captured
        // - "completed" (also no file), "skipped", "failed" (no fault of the player's: one retry, then the offer ends) or
        // "noad" (no video ever fills: the button says so). Read as each run begins (MonetisationRuntime.BeginRun), in
        // lower case; anything else is "completed". Only the Editor and a development build look.
        public const string AdsFileName = "ads.txt";
        public const string AdsCompleted = "completed";
        public const string AdsSkipped = "skipped";
        public const string AdsFailed = "failed";
        public const string AdsNoAd = "noad";
        public static string AdsPath => Path.Combine(ProfileLocation.Resolve(), FolderName, AdsFileName);
        public static string AdsOutcome()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return null;
            string line = ReadFirstLine(AdsPath);
            return string.IsNullOrEmpty(line) ? null : line.ToLowerInvariant();
        }

        public static string DepthProofPath => Path.Combine(ProfileLocation.Resolve(), FolderName, DepthProofFileName);

        // Under the seed's rules: only the Editor and a development build look, and a missing, empty or unreadable file
        // leaves the run alone. It only arranges the foundry, so it means nothing without the foundry preview.
        public static bool DepthProof() => (Application.isEditor || Debug.isDebugBuild) &&
            !string.IsNullOrEmpty(ReadFirstLine(DepthProofPath));

        // "off" in it keeps the map choice (MapChoiceView) from opening before a run: scene tests (TestProfile writes it)
        // and device sessions that need the run to start by itself. Only the Editor and a development build look.
        public const string MapChoiceFileName = "map-choice.txt";
        public static string MapChoicePath => Path.Combine(ProfileLocation.Resolve(), FolderName, MapChoiceFileName);
        public static bool MapChoiceOff() => (Application.isEditor || Debug.isDebugBuild) &&
            string.Equals(ReadFirstLine(MapChoicePath), "off", StringComparison.Ordinal);

        // Whether the start-floor file names a floor (the Editor or a development build only): such a run starts where the
        // file says, and no map is offered.
        public static bool NamesStartFloor() => (Application.isEditor || Debug.isDebugBuild) &&
            !string.IsNullOrEmpty(ReadStartFloorId());

        // Whether a run starts with the map choice: on the foundry, always in a release build; in the Editor and a
        // development build unless a development file already decides how the run starts or plays (a start floor, the
        // arrival, the depth proof, the autopilot, the density probe) or map-choice.txt says "off".
        public static bool OffersMapChoice() => FoundryRegion() && !MapChoiceOff() && !NamesStartFloor() &&
            !GateArrival() && !DepthProof() && !Autopilot() && HordeProbe() == 0;

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
        // matches a floor of the authored Descent (floor 2, floor 3...) or one in Resources/Development; the authored floor
        // in every other case. Resources are only touched
        // when a file actually named something, so a normal run pays nothing for this hook at startup.
        public static FloorDefinition FirstFloor(FloorDefinition authored)
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return authored;

            string id = ReadStartFloorId();
            if (string.IsNullOrEmpty(id))
                return authored;

            // A floor of the authored Descent itself (floor 2, floor 3...), reached without playing the ones before it.
            var seen = new System.Collections.Generic.HashSet<FloorDefinition>();
            for (FloorDefinition floor = authored; floor != null && seen.Add(floor); floor = floor.NextFloor)
            {
                if (string.Equals(floor.Id, id, StringComparison.Ordinal))
                    return floor;
            }

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

        // Whether the file is there, under the same never-throw rule as ReadFirstLine.
        private static bool Exists(string path)
        {
            try
            {
                return File.Exists(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                exception is ArgumentException || exception is NotSupportedException || exception is SecurityException)
            {
                return false;
            }
        }

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
