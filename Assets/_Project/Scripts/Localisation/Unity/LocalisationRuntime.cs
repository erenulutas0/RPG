using System.Globalization;
using UnityEngine;

namespace Cryptforge.Localisation
{
    // The localisation's Unity side (docs/60): at boot it pins the invariant culture for every thread, loads the string
    // table from Resources and sets the language - the player's stored choice, else the phone's system language (Turkish
    // for a Turkish phone, English otherwise).
    //
    // The choice lives in PlayerPrefs under its code ("en", "tr"), apart from the profile save, whose version it never
    // touches. In the Editor the system language is not asked: the Editor starts in English unless a choice was stored,
    // and a batch run (the test runner, the captures) starts in English unless CRYPTFORGE_LANGUAGE names a capture's
    // language, so the suites read the English they were written against on a Turkish machine (and pin it besides,
    // EditModeLanguagePin and PlayModeLanguagePin); a capture sets Loc.Override from it (CaptureLanguage).
    //
    // The culture: a Turkish phone writes 1.5 as "1,5" and upper-cases "i" to "İ" in its own culture. Data - the save,
    // telemetry, the [Perf] log lines Tools/Device reads - must read the same on every phone, so the threads' culture is the
    // invariant one from the first moment (and each writer names it as well). The player's numbers are written in the
    // chosen language on purpose (Loc.Numbers), never through the thread's culture.
    public static class LocalisationRuntime
    {
        public const string TableResource = "Localisation/Strings";
        public const string LanguageKey = "cryptforge.language";
        // A capture's language for a batch run (Tools/ArtReview's -Language): "en" or "tr".
        public const string CaptureLanguageVariable = "CRYPTFORGE_LANGUAGE";

        private static bool _loaded;
#if UNITY_EDITOR
        private static CultureInfo _editorCulture;
        private static CultureInfo _editorUiCulture;
        private static bool _restoreEditorCulture;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void PinCultureAtBoot()
        {
#if UNITY_EDITOR
            // The Editor's own culture is put back when play stops, so edit mode is as it was.
            if (!_restoreEditorCulture)
            {
                _editorCulture = CultureInfo.CurrentCulture;
                _editorUiCulture = CultureInfo.CurrentUICulture;
                _restoreEditorCulture = true;
                UnityEditor.EditorApplication.playModeStateChanged += RestoreEditorCulture;
            }
#endif
            PinInvariantCulture();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadAtBoot()
        {
            EnsureLoaded();
            Loc.SetLanguage(StartLanguage());
        }

        // The invariant culture on this thread and every thread made from now on.
        public static void PinInvariantCulture()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        // The table from Resources, once; a missing or damaged table leaves the game in English and says why.
        public static void EnsureLoaded()
        {
            if (_loaded && Loc.Table.Count > 0)
                return;
            _loaded = true;
            Loc.Warn = message => Debug.LogWarning(message);
            var asset = Resources.Load<TextAsset>(TableResource);
            if (asset == null)
            {
                Debug.LogError($"Localisation: Resources/{TableResource} is missing; the game speaks English only.");
                return;
            }
            LocTable table = LocTable.Parse(asset.text);
            for (int i = 0; i < table.Problems.Count; i++)
                Debug.LogWarning("Localisation: " + table.Problems[i]);
            Loc.UseTable(table);
            Resources.UnloadAsset(asset);
        }

        // The language a session starts in: the stored choice, else the phone's (docs/59 decision 1); in the Editor English
        // unless a choice was stored, and always English in a batch run, whose language is set by Loc.Override.
        public static Language StartLanguage()
        {
            if (Application.isBatchMode)
                return CaptureLanguage(out Language capture) ? capture : Language.English;
            if (TryStoredChoice(out Language stored))
                return stored;
            return Application.isEditor ? Language.English : SystemDefault(Application.systemLanguage);
        }

        public static Language SystemDefault(SystemLanguage system) => system == SystemLanguage.Turkish ? Language.Turkish : Language.English;

        public static bool TryStoredChoice(out Language language)
        {
            language = Language.English;
            return PlayerPrefs.HasKey(LanguageKey) && LanguageCodes.TryParse(PlayerPrefs.GetString(LanguageKey, string.Empty), out language);
        }

        // The player's choice (LanguageToggleView): the game speaks it at once and every later session starts in it.
        public static void Choose(Language language)
        {
            PlayerPrefs.SetString(LanguageKey, LanguageCodes.Code(language));
            PlayerPrefs.Save();
            Loc.SetLanguage(language);
        }

        // The language a capture names (CRYPTFORGE_LANGUAGE, set by Tools/ArtReview's runners), if any.
        public static bool CaptureLanguage(out Language language)
        {
            string code = System.Environment.GetEnvironmentVariable(CaptureLanguageVariable);
            return LanguageCodes.TryParse(code, out language) && !string.IsNullOrEmpty(code);
        }

#if UNITY_EDITOR
        private static void RestoreEditorCulture(UnityEditor.PlayModeStateChange change)
        {
            if (change != UnityEditor.PlayModeStateChange.EnteredEditMode || !_restoreEditorCulture)
                return;
            CultureInfo.DefaultThreadCurrentCulture = null;
            CultureInfo.DefaultThreadCurrentUICulture = null;
            CultureInfo.CurrentCulture = _editorCulture;
            CultureInfo.CurrentUICulture = _editorUiCulture;
            _restoreEditorCulture = false;
            UnityEditor.EditorApplication.playModeStateChanged -= RestoreEditorCulture;
            // Edit mode keeps no language from play (the domain is not reloaded on the way out): English, no table, as an
            // EditMode test expects to find it.
            Loc.Reset();
            Loc.Warn = null;
            _loaded = false;
        }
#endif
    }
}
