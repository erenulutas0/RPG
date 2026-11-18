using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Cryptforge.Editor.Release;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Cryptforge.Editor
{
    // The release AAB for Google Play (docs/59 section 3 item 4, docs/61), beside AndroidPrototypeBuild's development APK.
    // Tools/Build-Release.ps1 runs it and writes the report:
    //   Unity.exe -batchmode -quit -projectPath <root> -buildTarget Android
    //     -executeMethod Cryptforge.Editor.AndroidReleaseBuild.Build [-packageName com.example.game] [-cleanBuild] -logFile <log>
    // A release (non-development) IL2CPP build for arm64-v8a, an AAB, target SDK 36 and min SDK 26, the version from
    // Tools/Release/version.txt. Every setting it changes is in Settings below, saved first and restored in a finally block;
    // nothing saves the settings asset, so ProjectSettings/ keeps its bytes. Unity signs with its debug key; with the four
    // CRYPTFORGE_* variables set, the upload key replaces that signature through jarsigner (AabSigner) and the passwords
    // never reach Unity's settings, Library/ or a log.
    public static class AndroidReleaseBuild
    {
        const string GradleProject = "Library/Bee/Android/Prj/IL2CPP/Gradle";
        public const string CleanBuildArgument = "-cleanBuild";

        // Removed before each build: the previous bundle, and the release manifest the report cross-checks, so whatever is
        // there afterwards was written by this build.
        static readonly string[] StaleGradleOutputs =
        {
            GradleProject + "/launcher/build/outputs/bundle",
            GradleProject + "/launcher/build/intermediates/merged_manifest/release",
        };

        // One setting the release build may change: how to read it, write it and parse the plan's value for it.
        public sealed class Setting
        {
            public readonly string Name;
            readonly Func<object> get;
            readonly Action<object> set;
            readonly Func<string, object> parse;

            public Setting(string name, Func<object> get, Action<object> set, Func<string, object> parse)
            {
                Name = name;
                this.get = get;
                this.set = set;
                this.parse = parse;
            }

            public object Get() => get();
            public void Set(object value) => set(value);
            public object Parse(string text) => parse(text);
        }

        static Setting Of<T>(string name, Func<T> get, Action<T> set, Func<string, T> parse) =>
            new Setting(name, () => get(), value => set((T)value), text => parse(text));

        static T EnumValue<T>(string text) where T : struct => (T)Enum.Parse(typeof(T), text);
        static int Integer(string text) => int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);
        static bool Boolean(string text) => bool.Parse(text);
        static string Text(string text) => text;

        // Every setting the build changes and restores. ReleaseSettings.Names lists the same names; the EditMode suite holds
        // the two lists equal and round-trips every entry.
        static readonly Setting[] Settings =
        {
            Of<ScriptingImplementation>(ReleaseSettings.ScriptingBackend,
                () => PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android),
                value => PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, value), EnumValue<ScriptingImplementation>),
            Of<Il2CppCompilerConfiguration>(ReleaseSettings.Il2CppConfiguration,
                () => PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget.Android),
                value => PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, value), EnumValue<Il2CppCompilerConfiguration>),
            Of<AndroidArchitecture>(ReleaseSettings.TargetArchitectures,
                () => PlayerSettings.Android.targetArchitectures,
                value => PlayerSettings.Android.targetArchitectures = value, EnumValue<AndroidArchitecture>),
            Of<AndroidSdkVersions>(ReleaseSettings.MinSdkVersion,
                () => PlayerSettings.Android.minSdkVersion,
                value => PlayerSettings.Android.minSdkVersion = value, text => (AndroidSdkVersions)Integer(text)),
            Of<AndroidSdkVersions>(ReleaseSettings.TargetSdkVersion,
                () => PlayerSettings.Android.targetSdkVersion,
                value => PlayerSettings.Android.targetSdkVersion = value, text => (AndroidSdkVersions)Integer(text)),
            Of<string>(ReleaseSettings.ApplicationIdentifier,
                () => PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                value => PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, value), Text),
            Of<string>(ReleaseSettings.VersionName,
                () => PlayerSettings.bundleVersion,
                value => PlayerSettings.bundleVersion = value, Text),
            Of<int>(ReleaseSettings.VersionCode,
                () => PlayerSettings.Android.bundleVersionCode,
                value => PlayerSettings.Android.bundleVersionCode = value, Integer),
            Of<bool>(ReleaseSettings.UseCustomKeystore,
                () => PlayerSettings.Android.useCustomKeystore,
                value => PlayerSettings.Android.useCustomKeystore = value, Boolean),
            Of<UIOrientation>(ReleaseSettings.Orientation,
                () => PlayerSettings.defaultInterfaceOrientation,
                value => PlayerSettings.defaultInterfaceOrientation = value, EnumValue<UIOrientation>),
            Of<bool>(ReleaseSettings.BuildAppBundle,
                () => EditorUserBuildSettings.buildAppBundle,
                value => EditorUserBuildSettings.buildAppBundle = value, Boolean),
            Of<bool>(ReleaseSettings.ExportAsGoogleAndroidProject,
                () => EditorUserBuildSettings.exportAsGoogleAndroidProject,
                value => EditorUserBuildSettings.exportAsGoogleAndroidProject = value, Boolean),
            Of<bool>(ReleaseSettings.Development,
                () => EditorUserBuildSettings.development,
                value => EditorUserBuildSettings.development = value, Boolean),
            Of<AndroidBuildType>(ReleaseSettings.AndroidBuildType,
                () => EditorUserBuildSettings.androidBuildType,
                value => EditorUserBuildSettings.androidBuildType = value, EnumValue<AndroidBuildType>),
        };

        public static List<string> SettingNames => Settings.Select(setting => setting.Name).ToList();

        // Every setting's value before the build.
        public sealed class Snapshot
        {
            readonly List<KeyValuePair<Setting, object>> saved = new List<KeyValuePair<Setting, object>>();

            internal Snapshot()
            {
                foreach (Setting setting in Settings)
                    saved.Add(new KeyValuePair<Setting, object>(setting, setting.Get()));
            }

            public List<string> Names => saved.Select(pair => pair.Key.Name).ToList();

            // In reverse order of the plan (the architectures before the backend); one failure does not stop the rest.
            // Returns the names that could not be restored.
            public List<string> Restore()
            {
                var failed = new List<string>();
                for (int i = saved.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (!Equals(saved[i].Key.Get(), saved[i].Value))
                            saved[i].Key.Set(saved[i].Value);
                    }
                    catch (Exception exception)
                    {
                        failed.Add(saved[i].Key.Name);
                        Debug.LogError("Release build: could not restore " + saved[i].Key.Name + ": " + exception.Message);
                    }
                }
                return failed;
            }

            // The settings whose value is not the saved one.
            public List<string> Changed() =>
                saved.Where(pair => !Equals(pair.Key.Get(), pair.Value)).Select(pair => pair.Key.Name).ToList();
        }

        public static Snapshot Save() => new Snapshot();

        // Sets the plan's values, only through Settings: a name outside it is refused before anything changes.
        public static void Apply(IList<KeyValuePair<string, string>> plan)
        {
            var steps = new List<KeyValuePair<Setting, object>>();
            foreach (KeyValuePair<string, string> step in plan)
            {
                Setting setting = Settings.FirstOrDefault(candidate => candidate.Name == step.Key);
                if (setting == null)
                    throw new BuildFailedException("The release plan names " + step.Key + ", which the build cannot restore.");
                steps.Add(new KeyValuePair<Setting, object>(setting, setting.Parse(step.Value)));
            }
            // A value already in place is not written again: PlayerSettings serialises an explicit entry for it (an
            // il2cppCompilerConfiguration of Release where the default was {}), which would change ProjectSettings' bytes.
            foreach (KeyValuePair<Setting, object> step in steps)
                if (!Equals(step.Key.Get(), step.Value))
                    step.Key.Set(step.Value);
        }

        [MenuItem("Cryptforge/Build Android Release (AAB)")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Select Android before building, or launch Unity with -buildTarget Android.");
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled gameplay scene in build settings.");

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] arguments = Environment.GetCommandLineArgs();
            ReleaseVersion version = Checked(() => ReleaseVersion.Parse(ReadOrNull(Path.Combine(root, ReleaseVersion.FilePath))));
            string packageName = Checked(() => ReleaseArguments.PackageNameFrom(arguments));
            ReleaseSigning signing = Checked(() => ReleaseSigning.Resolve(Environment.GetEnvironmentVariable, File.Exists));
            bool clean = arguments.Any(argument => string.Equals(argument, CleanBuildArgument, StringComparison.OrdinalIgnoreCase));
            Debug.Log("Release build: " + version + ", " + packageName + ", " + signing.Describe() + (clean ? ", clean build" : ""));

            var jdk = new JdkTools(JdkBin(), message => Debug.Log("Release build: " + message));
            string uploadFingerprint = null;
            if (signing.Kind == ReleaseSigningKind.UploadKey)
                uploadFingerprint = Checked(() => AabSigner.CheckUploadKey(jdk, signing));

            string outputFolder = Path.Combine(root, ReleaseSettings.OutputFolder);
            string workFolder = Path.Combine(outputFolder, "work");
            string summaryPath = Path.Combine(root, ReleaseSettings.SummaryFile);
            string finalAab = Path.Combine(outputFolder, ReleaseSettings.AabFileName(version, signing.Kind));
            string unityAab = signing.Kind == ReleaseSigningKind.UploadKey
                ? Path.Combine(workFolder, Path.GetFileNameWithoutExtension(finalAab) + "-unity.aab")
                : finalAab;
            Directory.CreateDirectory(outputFolder);
            if (Directory.Exists(workFolder))
                Directory.Delete(workFolder, true);
            Directory.CreateDirectory(workFolder);
            DeleteFile(finalAab);
            DeleteFile(summaryPath);
            foreach (string stale in StaleGradleOutputs)
            {
                string folder = Path.Combine(root, stale);
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }

            var clock = Stopwatch.StartNew();
            Snapshot saved = Save();
            BuildReport report;
            List<string> unrestored;
            try
            {
                Apply(ReleaseSettings.Plan(version, packageName));
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = unityAab,
                    target = BuildTarget.Android,
                    options = clean ? BuildOptions.CleanBuildCache : BuildOptions.None
                });
            }
            finally
            {
                // Never AssetDatabase.SaveAssets() here or anywhere in this build: the settings return to their values in
                // memory, and Tools/Build-Release.ps1 compares ProjectSettings/ byte for byte before and after.
                unrestored = saved.Restore();
            }
            List<string> changed = saved.Changed();
            if (unrestored.Count > 0 || changed.Count > 0)
                throw new BuildFailedException("Settings not restored after the build: " + string.Join(", ", unrestored.Union(changed)));
            Debug.Log("Release build: restored " + saved.Names.Count + " settings: " + string.Join(", ", saved.Names));
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android release build failed: {report.summary.result}, {report.summary.totalErrors} errors.");

            string fingerprint = signing.Kind == ReleaseSigningKind.UploadKey
                ? Checked(() => AabSigner.Sign(jdk, signing, unityAab, finalAab, workFolder, uploadFingerprint))
                : AabSigner.Signer(jdk, finalAab)[1];

            List<string> problems = Problems(finalAab, version, packageName);
            if (problems.Count > 0)
                throw new BuildFailedException("The AAB does not match the release plan:\n  " + string.Join("\n  ", problems));

            long bytes = new FileInfo(finalAab).Length;
            File.WriteAllText(summaryPath, ReleaseSummary.Write(new List<KeyValuePair<string, string>>
            {
                Row("aab", finalAab.Replace('\\', '/')),
                Row("bytes", bytes.ToString(CultureInfo.InvariantCulture)),
                Row("signing", signing.Kind == ReleaseSigningKind.UploadKey ? "upload" : "debug"),
                Row("signing_note", signing.Describe()),
                Row("signer_sha256", fingerprint ?? ""),
                Row("package", packageName),
                Row("versionName", version.Name),
                Row("versionCode", version.Code.ToString(CultureInfo.InvariantCulture)),
                Row("clean_build", clean ? "true" : "false"),
                Row("settings_restored", saved.Names.Count.ToString(CultureInfo.InvariantCulture)),
                Row("unity_seconds", ((int)clock.Elapsed.TotalSeconds).ToString(CultureInfo.InvariantCulture)),
            }));
            Debug.Log($"Android release built: {finalAab} ({bytes} bytes), {signing.Describe()}");
        }

        // The AAB the build made, against its plan: the manifest Play reads and the one ABI.
        static List<string> Problems(string aab, ReleaseVersion version, string packageName)
        {
            var problems = new List<string>();
            byte[] proto = ZipEntries.Read(aab, AabManifest.EntryName);
            if (proto == null)
                return new List<string> { "no " + AabManifest.EntryName };
            AabManifest manifest = AabManifest.Parse(proto);
            Expect(problems, "package", manifest.Package, packageName);
            Expect(problems, "versionName", manifest.VersionName, version.Name);
            Expect(problems, "versionCode", manifest.VersionCode, version.Code.ToString(CultureInfo.InvariantCulture));
            Expect(problems, "minSdkVersion", manifest.MinSdk, ReleaseSettings.MinSdk.ToString(CultureInfo.InvariantCulture));
            Expect(problems, "targetSdkVersion", manifest.TargetSdk, ReleaseSettings.TargetSdk.ToString(CultureInfo.InvariantCulture));
            if (manifest.Debuggable)
                problems.Add("the application is debuggable");
            Expect(problems, "ABIs", string.Join(",", AabLayout.Abis(ZipEntries.Names(aab))), "arm64-v8a");
            return problems;
        }

        static void Expect(List<string> problems, string what, string actual, string expected)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                problems.Add(what + " is " + (actual ?? "missing") + ", not " + expected);
        }

        static KeyValuePair<string, string> Row(string key, string value) => new KeyValuePair<string, string>(key, value);

        // Unity's embedded OpenJDK (the one its Gradle build uses), else JAVA_HOME's.
        static string JdkBin()
        {
            string embedded = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "OpenJDK", "bin");
            if (Directory.Exists(embedded))
                return embedded;
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(javaHome) && Directory.Exists(Path.Combine(javaHome, "bin")))
                return Path.Combine(javaHome, "bin");
            throw new BuildFailedException("No JDK: neither " + embedded + " nor JAVA_HOME/bin exists.");
        }

        static T Checked<T>(Func<T> step)
        {
            try
            {
                return step();
            }
            catch (ReleaseConfigException exception)
            {
                throw new BuildFailedException(exception.Message);
            }
        }

        static string ReadOrNull(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

        static void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
