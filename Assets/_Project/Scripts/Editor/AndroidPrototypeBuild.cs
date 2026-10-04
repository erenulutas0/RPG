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
