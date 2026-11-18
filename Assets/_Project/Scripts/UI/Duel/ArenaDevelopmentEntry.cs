using System;
using System.Collections;
using System.IO;
using System.Security;
using Cryptforge.Arena;
using Cryptforge.Core;
using Cryptforge.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cryptforge.UI
{
    // The arena's development entry until the map choice carries its button (docs/55 section 11): any text in
    // <profile folder>/development/arena.txt opens the arena over the gameplay scene as it loads - over the map choice,
    // which holds the run - in the Editor and a development build only, as development/skills.txt arms the skills. "open"
    // as its first word opens the open arena's offers, anything else the training ladder. A release build never looks,
    // and no file, an empty one or an unreadable one opens nothing. Never throws and never logs: scene tests point the
    // profile at a folder of their own, so the entry stays shut in them.
    //
    //   adb shell "mkdir -p /sdcard/Android/data/com.cryptforge.prototype/files/development && echo training > /sdcard/Android/data/com.cryptforge.prototype/files/development/arena.txt"
    //   adb shell rm /sdcard/Android/data/com.cryptforge.prototype/files/development/arena.txt
    public static class ArenaDevelopmentEntry
    {
        public const string FileName = "arena.txt";

        private static bool _hooked;

        public static string Path => System.IO.Path.Combine(ProfileLocation.Resolve(), DevelopmentStart.FolderName, FileName);

        // Whether the entry is armed, and which arena it opens.
        public static bool Requested(out ArenaMode mode)
        {
            mode = ArenaMode.Ranked;
            if (!Application.isEditor && !Debug.isDebugBuild)
                return false;
            string line = ReadFirstLine(Path);
            if (string.IsNullOrEmpty(line))
                return false;
            string first = line.Split(' ', '\t')[0];
            mode = string.Equals(first, "open", StringComparison.OrdinalIgnoreCase) ? ArenaMode.Open : ArenaMode.Ranked;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _hooked = true;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        // With domain reload off, a new Play session starts unhooked.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (_hooked)
                SceneManager.sceneLoaded -= OnSceneLoaded;
            _hooked = false;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !Requested(out ArenaMode arena))
                return;
            var setup = UnityEngine.Object.FindFirstObjectByType<CombatSetup>();
            if (setup == null || setup.gameObject.scene != scene)
                return;
            // A frame later, once the scene's Start has built the map choice the arena opens over.
            setup.StartCoroutine(OpenNextFrame(setup, arena));
        }

        private static IEnumerator OpenNextFrame(CombatSetup setup, ArenaMode arena)
        {
            yield return null;
            if (setup != null && ArenaScreen.Current == null)
                ArenaScreen.Open(setup, null, arena);
        }

        private static string ReadFirstLine(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                foreach (string line in File.ReadAllLines(path))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length > 0)
                        return trimmed;
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
