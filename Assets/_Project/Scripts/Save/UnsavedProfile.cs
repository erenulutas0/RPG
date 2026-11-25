using System;
using UnityEngine;

namespace Cryptforge.Save
{
    // The profile this session could not write, carried across scene reloads until a save lands. A run's banked gold, its
    // items, floor clears, unlocks and forge buys live only in memory while every save fails (the phone's storage full) or
    // while a newer build's file stands (ProfileStore.IsReadOnly: nothing is written), and every Restart, another map, hero
    // or tier, and a language change reloads the scene, whose load would read the older file and lose them without a word.
    // ProfileStore.Save holds the profile here when it could not write it, ProfileStore.LoadSession hands it to the next
    // scene in place of the disk's, and the first save that lands releases it.
    // A deliberate exception to the no-static-state rule (07_CODE_STANDARDS), like RunStartRequest: nothing else survives
    // a reload. It is held as the JSON the save would have written (so each load restores a fresh profile, with no
    // listener of the scene that went away), keyed by the profile folder (a test's folder never hands its profile to
    // another's), and it never outlives the app.
    public static class UnsavedProfile
    {
        private static string _directory;
        private static string _json;
        private static int _revision;

        // Whether a profile waits for a save to land (for tests and the log).
        public static bool IsHeld => _json != null;

        public static bool IsHeldFor(string directory) =>
            _json != null && string.Equals(_directory, directory, StringComparison.Ordinal);

        // The profile a save could not write, as the JSON it would have written, and the revision the store had written or
        // read before it (the next save writes above it).
        public static void Hold(string directory, string json, int revision)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("A profile directory is required.", nameof(directory));
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("The profile's JSON is required.", nameof(json));
            _directory = directory;
            _json = json;
            _revision = revision;
        }

        // The profile held for this folder; false when none is (or one is held for another folder).
        public static bool TryGet(string directory, out string json, out int revision)
        {
            if (!IsHeldFor(directory))
            {
                json = null;
                revision = 0;
                return false;
            }
            json = _json;
            revision = _revision;
            return true;
        }

        // A save of this folder landed: the profile held for it is on disk now.
        public static void Release(string directory)
        {
            if (IsHeldFor(directory))
                Clear();
        }

        public static void Clear()
        {
            _directory = null;
            _json = null;
            _revision = 0;
        }

        // With domain reload disabled in the Editor, statics survive into the next Play session; a new Play session is a
        // new launch (as MonetisationRuntime's): nothing of the previous one is held, the arena's unsaved save included.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewLaunch()
        {
            Clear();
            ArenaSaveStore.ForgetUnsaved();
        }
    }
}
