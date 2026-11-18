using System.Collections.Generic;
using System.Linq;
using Cryptforge.Editor;
using Cryptforge.Editor.Release;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace Cryptforge.Tests
{
    // AndroidReleaseBuild's settings table against the pure plan (docs/61): the build changes settings only through the
    // table, saves the whole table before and restores it after, so the list it changes is exactly the list it restores.
    // Editor only (UnityEditor's PlayerSettings): not in Tools/CombatChecks. The round trip changes the settings in memory
    // and restores them in a finally block; nothing saves the settings asset.
    public sealed class ReleaseBuildSettingsTests
    {
        [Test]
        public void TheTableTheBuildRestoresIsExactlyTheListThePlanChanges()
        {
            CollectionAssert.AreEqual(ReleaseSettings.Names, AndroidReleaseBuild.SettingNames);
            CollectionAssert.AreEqual(AndroidReleaseBuild.SettingNames, AndroidReleaseBuild.Save().Names);
            List<string> planned = ReleaseSettings.Plan(new ReleaseVersion("0.1.0", 1), ReleaseSettings.DefaultPackageName)
                .Select(step => step.Key).ToList();
            CollectionAssert.AreEqual(AndroidReleaseBuild.SettingNames, planned);
        }

        [Test]
        public void ApplyingAPlanThenRestoringLeavesEverySettingAsItWas()
        {
            string versionName = PlayerSettings.bundleVersion;
            int versionCode = PlayerSettings.Android.bundleVersionCode;
            string package = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            AndroidReleaseBuild.Snapshot saved = AndroidReleaseBuild.Save();
            List<string> changed;
            try
            {
                AndroidReleaseBuild.Apply(ReleaseSettings.Plan(new ReleaseVersion("9.8.7", 987654), "com.cryptforge.roundtrip"));
                changed = saved.Changed();
                Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("9.8.7"));
                Assert.That(PlayerSettings.Android.bundleVersionCode, Is.EqualTo(987654));
                Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), Is.EqualTo("com.cryptforge.roundtrip"));
                Assert.That(PlayerSettings.Android.targetSdkVersion, Is.EqualTo((AndroidSdkVersions)36));
                Assert.That(PlayerSettings.Android.minSdkVersion, Is.EqualTo((AndroidSdkVersions)26));
                Assert.That(EditorUserBuildSettings.buildAppBundle, Is.True);
                Assert.That(EditorUserBuildSettings.development, Is.False);
                Assert.That(PlayerSettings.Android.useCustomKeystore, Is.False);
            }
            finally
            {
                List<string> failed = saved.Restore();
                Assert.That(failed, Is.Empty, "Restore failed for " + string.Join(", ", failed));
            }
            CollectionAssert.Contains(changed, ReleaseSettings.VersionName);
            CollectionAssert.Contains(changed, ReleaseSettings.VersionCode);
            CollectionAssert.Contains(changed, ReleaseSettings.ApplicationIdentifier);
            Assert.That(saved.Changed(), Is.Empty, "Every setting is back: " + string.Join(", ", saved.Changed()));
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo(versionName));
            Assert.That(PlayerSettings.Android.bundleVersionCode, Is.EqualTo(versionCode));
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), Is.EqualTo(package));
        }

        [Test]
        public void APlanNamingASettingOutsideTheTableChangesNothing()
        {
            AndroidReleaseBuild.Snapshot saved = AndroidReleaseBuild.Save();
            var plan = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(ReleaseSettings.VersionName, "9.9.9"),
                new KeyValuePair<string, string>("PlayerSettings.companyName", "Someone"),
            };
            try
            {
                Assert.Throws<BuildFailedException>(() => AndroidReleaseBuild.Apply(plan));
                Assert.That(saved.Changed(), Is.Empty, "The plan is checked whole before any setting changes.");
            }
            finally
            {
                saved.Restore();
            }
        }
    }
}
