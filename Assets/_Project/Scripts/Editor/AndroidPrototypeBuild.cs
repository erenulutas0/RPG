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
        [MenuItem("Cryptforge/Build Android Prototype")]
        public static void Build()
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

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, "Cryptforge-Prototype.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android build failed: {report.summary.result}, {report.summary.totalErrors} errors.");

            Debug.Log($"Android prototype built: {report.summary.outputPath} ({report.summary.totalSize} bytes)");
        }
    }
}
