using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Cryptforge.Editor
{
    public static class AndroidPrototypeBuild
    {
        // The Gradle project under Library/Bee is reused between builds, and the Android Gradle plugin packages a
        // debug APK incrementally: it rewrites the previous launcher-debug.apk in place, and every entry that changes
        // size leaves a hole of dead bytes behind. Holes accumulate from build to build (10.3 MB of 47 MB on
        // 2026-11-11), so the same commit gave a bigger APK each time. Deleting the previous APK makes Gradle write
        // a fresh one with no holes.
        const string GradleApkOutput = "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/apk";

        [MenuItem("Cryptforge/Build Android Prototype")]
        public static void Build() => Build(BuildOptions.None);

        // Rebuilds the player data too, so that the build report lists every packed asset (BuildSizeReport).
        public static void BuildClean() => Build(BuildOptions.CleanBuildCache);

        static void Build(BuildOptions extraOptions)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Select Android before building, or launch Unity with -buildTarget Android.");

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled gameplay scene in build settings.");

            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Android"));
            Directory.CreateDirectory(outputDirectory);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            string staleApks = Path.GetFullPath(Path.Combine(Application.dataPath, "..", GradleApkOutput));
            if (Directory.Exists(staleApks))
                Directory.Delete(staleApks, true);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, "Cryptforge-Prototype.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.Development | extraOptions
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android build failed: {report.summary.result}, {report.summary.totalErrors} errors.");

            // Report the Android-imported texture, not only its requested importer override.
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art/Props/Foundry" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Debug.Log($"Android texture: {path} {texture.width}x{texture.height} {texture.format}, {texture.mipmapCount} mips");
            }

            Debug.Log($"Android prototype built: {report.summary.outputPath} ({report.summary.totalSize} bytes)");
        }
    }
}
