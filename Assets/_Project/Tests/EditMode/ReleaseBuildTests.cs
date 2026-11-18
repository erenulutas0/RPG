using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Cryptforge.Editor.Release;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The release pipeline's pure rules (docs/61): the version file, the signing environment, the settings plan, the
    // command line, the AAB's protobuf manifest and the JAR signature's entries. The Unity EditMode suite and
    // Tools/CombatChecks run these alike; ReleaseBuildSettingsTests (EditMode only) hold the Editor's settings table to the plan.
    public sealed class ReleaseBuildTests
    {
        // --- the version file ---

        [Test]
        public void TheCommittedVersionFileParses()
        {
            string path = FindUp(ReleaseVersion.FilePath);
            ReleaseVersion version = ReleaseVersion.Parse(File.ReadAllText(path));
            Assert.That(version.Code, Is.GreaterThanOrEqualTo(1));
            Assert.That(version.Name.Split('.').Length, Is.EqualTo(3));
        }

        [Test]
        public void TheVersionFileReadsKeysAroundCommentsBlankLinesAndEitherLineEnding()
        {
            ReleaseVersion version = ReleaseVersion.Parse("# a comment\r\n\r\n  versionName = 1.20.3  \r\n# versionCode=99\r\nversionCode=42\r\n");
            Assert.That(version.Name, Is.EqualTo("1.20.3"));
            Assert.That(version.Code, Is.EqualTo(42));
            Assert.That(ReleaseVersion.Parse("versionCode=7\nversionName=0.1.0").Code, Is.EqualTo(7));
        }

        [Test]
        public void TheVersionFileRefusesWhatCouldLeaveAWrongVersionInForce()
        {
            AssertRefused(null, "missing");
            AssertRefused("versionName=0.1.0\n", "no versionCode");
            AssertRefused("versionCode=3\n", "no versionName");
            AssertRefused("versionName=0.1.0\nversionCode=3\nversionCode=4\n", "repeats versionCode");
            AssertRefused("versionName=0.1.0\nversionName=0.1.1\nversionCode=3\n", "repeats versionName");
            AssertRefused("versionName=0.1.0\nversioncode=3\n", "unknown key");
            AssertRefused("versionName=0.1.0\nversionCode 3\n", "not key=value");
        }

        [Test]
        public void TheVersionCodeIsAWholeNumberInPlaysRange()
        {
            foreach (string code in new[] { "0", "-1", "+5", "1.5", "1,5", "abc", "", "2100000001", "99999999999" })
                AssertRefused("versionName=0.1.0\nversionCode=" + code + "\n", "versionCode");
            Assert.That(ReleaseVersion.Parse("versionName=0.1.0\nversionCode=2100000000").Code, Is.EqualTo(ReleaseVersion.MaxVersionCode));
        }

        [Test]
        public void TheVersionNameIsMajorMinorPatch()
        {
            foreach (string name in new[] { "1.0", "01.0.0", "1.02.0", "v1.0.0", "1.0.0-rc1", "1.0.0.0", "", "1..0", "١.٠.٠" })
                AssertRefused("versionName=" + name + "\nversionCode=1\n", "versionName");
            Assert.That(ReleaseVersion.Parse("versionName=10.0.12\nversionCode=1").Name, Is.EqualTo("10.0.12"));
        }

        [Test]
        public void TheBumpRaisesTheCodeByOneAndKeepsEveryOtherByte()
        {
            const string text = "# keep me\r\n# versionCode=99\r\nversionName=0.3.1\r\n  versionCode = 41\r\n";
            string bumped = ReleaseVersion.Bump(text);
            Assert.That(bumped, Is.EqualTo("# keep me\r\n# versionCode=99\r\nversionName=0.3.1\r\n  versionCode = 42\r\n"));
            Assert.That(ReleaseVersion.Parse(bumped).Code, Is.EqualTo(42));
            Assert.That(ReleaseVersion.Bump("versionCode=9\nversionName=1.0.0"), Is.EqualTo("versionCode=10\nversionName=1.0.0"));
            AssertRefused("versionName=0.1.0\nversionCode=2100000000\n", "ceiling", bump: true);
        }

        // --- the signing environment ---

        const string Secret = "s3cret-Do-Not-Print";

        [Test]
        public void NoSigningVariableMeansUnitysDebugKeyAndTheStamp()
        {
            ReleaseSigning signing = ReleaseSigning.Resolve(EnvironmentOf(), path => true);
            Assert.That(signing.Kind, Is.EqualTo(ReleaseSigningKind.DebugKey));
            Assert.That(signing.KeystorePath, Is.Null);
            Assert.That(signing.Describe(), Does.Contain("NOT FOR PLAY: debug key"));
        }

        [Test]
        public void AllFourVariablesMeanTheUploadKeyAndNoPasswordIsKept()
        {
            ReleaseSigning signing = ReleaseSigning.Resolve(EnvironmentOf(
                ReleaseSigning.KeystorePathVariable, " D:/Keys/cryptforge-upload.jks ",
                ReleaseSigning.KeystorePassVariable, Secret,
                ReleaseSigning.KeyAliasVariable, "cryptforge-upload ",
                ReleaseSigning.KeyPassVariable, Secret), path => path == "D:/Keys/cryptforge-upload.jks");
            Assert.That(signing.Kind, Is.EqualTo(ReleaseSigningKind.UploadKey));
            Assert.That(signing.KeystorePath, Is.EqualTo("D:/Keys/cryptforge-upload.jks"));
            Assert.That(signing.KeyAlias, Is.EqualTo("cryptforge-upload"));
            Assert.That(signing.Describe(), Does.Not.Contain(Secret));
            Assert.That(signing.Describe(), Does.Not.Contain("NOT FOR PLAY"));
            foreach (var field in typeof(ReleaseSigning).GetFields())
                Assert.That(Convert.ToString(field.GetValue(field.IsStatic ? null : signing)), Does.Not.Contain(Secret), field.Name);
        }

        [Test]
        public void AHalfConfiguredKeyNamesTheMissingVariablesAndNeverAValue()
        {
            var error = Assert.Throws<ReleaseConfigException>(() => ReleaseSigning.Resolve(EnvironmentOf(
                ReleaseSigning.KeystorePathVariable, "D:/Keys/k.jks",
                ReleaseSigning.KeystorePassVariable, Secret), path => true));
            Assert.That(error.Message, Does.Contain(ReleaseSigning.KeyAliasVariable).And.Contain(ReleaseSigning.KeyPassVariable));
            Assert.That(error.Message, Does.Not.Contain(Secret));
            Assert.That(error.Message, Does.Not.Contain(ReleaseSigning.KeystorePassVariable + ","));

            // Blank counts as missing: one blank variable among four is still half configured.
            error = Assert.Throws<ReleaseConfigException>(() => ReleaseSigning.Resolve(EnvironmentOf(
                ReleaseSigning.KeystorePathVariable, "D:/Keys/k.jks",
                ReleaseSigning.KeystorePassVariable, Secret,
                ReleaseSigning.KeyAliasVariable, "  ",
                ReleaseSigning.KeyPassVariable, Secret), path => true));
            Assert.That(error.Message, Does.Contain(ReleaseSigning.KeyAliasVariable + " is not set"));
            Assert.That(error.Message, Does.Not.Contain(Secret));
        }

        [Test]
        public void AKeystorePathThatNamesNoFileIsAnError()
        {
            var error = Assert.Throws<ReleaseConfigException>(() => ReleaseSigning.Resolve(EnvironmentOf(
                ReleaseSigning.KeystorePathVariable, "D:/Keys/missing.jks",
                ReleaseSigning.KeystorePassVariable, Secret,
                ReleaseSigning.KeyAliasVariable, "upload",
                ReleaseSigning.KeyPassVariable, Secret), path => false));
            Assert.That(error.Message, Does.Contain("D:/Keys/missing.jks"));
            Assert.That(error.Message, Does.Not.Contain(Secret));
        }

        // --- the settings plan, the package and the command line ---

        [Test]
        public void ThePlanSetsEachNamedSettingOnceInOrder()
        {
            List<KeyValuePair<string, string>> plan = ReleaseSettings.Plan(new ReleaseVersion("0.1.0", 1), ReleaseSettings.DefaultPackageName);
            CollectionAssert.AreEqual(ReleaseSettings.Names, plan.Select(step => step.Key).ToArray());
            Assert.That(ReleaseSettings.Names.Distinct().Count(), Is.EqualTo(ReleaseSettings.Names.Length));
        }

        [Test]
        public void ThePlanIsAReleaseAabForArm64AtSdk36()
        {
            Dictionary<string, string> plan = ReleaseSettings.Plan(new ReleaseVersion("2.3.4", 56), "com.example.game")
                .ToDictionary(step => step.Key, step => step.Value);
            Assert.That(plan[ReleaseSettings.BuildAppBundle], Is.EqualTo("true"));
            Assert.That(plan[ReleaseSettings.Development], Is.EqualTo("false"));
            Assert.That(plan[ReleaseSettings.AndroidBuildType], Is.EqualTo("Release"));
            Assert.That(plan[ReleaseSettings.ExportAsGoogleAndroidProject], Is.EqualTo("false"));
            Assert.That(plan[ReleaseSettings.ScriptingBackend], Is.EqualTo("IL2CPP"));
            Assert.That(plan[ReleaseSettings.Il2CppConfiguration], Is.EqualTo("Release"));
            Assert.That(plan[ReleaseSettings.TargetArchitectures], Is.EqualTo("ARM64"));
            Assert.That(plan[ReleaseSettings.TargetSdkVersion], Is.EqualTo("36"));
            Assert.That(plan[ReleaseSettings.MinSdkVersion], Is.EqualTo("26"));
            Assert.That(plan[ReleaseSettings.UseCustomKeystore], Is.EqualTo("false"), "Unity never holds the upload key's passwords.");
            Assert.That(plan[ReleaseSettings.Orientation], Is.EqualTo("Portrait"));
            Assert.That(plan[ReleaseSettings.ApplicationIdentifier], Is.EqualTo("com.example.game"));
            Assert.That(plan[ReleaseSettings.VersionName], Is.EqualTo("2.3.4"));
            Assert.That(plan[ReleaseSettings.VersionCode], Is.EqualTo("56"));
        }

        [Test]
        public void PackageNamesFollowPlaysRule()
        {
            foreach (string good in new[] { "com.cryptforge.prototype", "com.cryptforge.game", "a.b", "tr.com.studio_x.game2" })
                Assert.That(ReleaseSettings.IsValidPackageName(good), Is.True, good);
            foreach (string bad in new[] { null, "", "cryptforge", "com.", ".com.x", "com..x", "com.1game", "com.crypt-forge", "com.cryptförge", "com. x" })
                Assert.That(ReleaseSettings.IsValidPackageName(bad), Is.False, bad ?? "null");
            Assert.Throws<ReleaseConfigException>(() => ReleaseSettings.Plan(new ReleaseVersion("0.1.0", 1), "not a package"));
            Assert.That(ReleaseSettings.DefaultPackageName, Is.EqualTo("com.cryptforge.prototype"), "The permanent name is chosen with the owner (docs/59).");
        }

        [Test]
        public void TheCommandLineGivesThePackageOrTheDefault()
        {
            Assert.That(ReleaseArguments.PackageNameFrom(new[] { "Unity.exe", "-batchmode", "-quit" }), Is.EqualTo(ReleaseSettings.DefaultPackageName));
            Assert.That(ReleaseArguments.PackageNameFrom(new[] { "Unity.exe", "-PACKAGENAME", "com.example.game", "-quit" }), Is.EqualTo("com.example.game"));
            Assert.Throws<ReleaseConfigException>(() => ReleaseArguments.PackageNameFrom(new[] { "Unity.exe", "-packageName" }));
            Assert.Throws<ReleaseConfigException>(() => ReleaseArguments.PackageNameFrom(new[] { "Unity.exe", "-packageName", "-quit" }));
            Assert.Throws<ReleaseConfigException>(() => ReleaseArguments.PackageNameFrom(new[] { "Unity.exe", "-packageName", "Cryptforge" }));
        }

        [Test]
        public void ADebugKeyAabSaysSoInItsName()
        {
            var version = new ReleaseVersion("0.1.0", 7);
            Assert.That(ReleaseSettings.AabFileName(version, ReleaseSigningKind.UploadKey), Is.EqualTo("Cryptforge-0.1.0-7.aab"));
            Assert.That(ReleaseSettings.AabFileName(version, ReleaseSigningKind.DebugKey), Is.EqualTo("Cryptforge-0.1.0-7-debugkey.aab"));
        }

        [Test]
        public void TheSummaryReadsBackWhatTheBuildWrote()
        {
            string text = ReleaseSummary.Write(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("aab", "E:/MobileGame/Builds/Release/Cryptforge-0.1.0-1-debugkey.aab"),
                new KeyValuePair<string, string>("signing_note", "NOT FOR PLAY: debug key (a=b)"),
            });
            Dictionary<string, string> rows = ReleaseSummary.Read(text.Replace("\n", "\r\n"));
            Assert.That(rows["aab"], Is.EqualTo("E:/MobileGame/Builds/Release/Cryptforge-0.1.0-1-debugkey.aab"));
            Assert.That(rows["signing_note"], Is.EqualTo("NOT FOR PLAY: debug key (a=b)"));
        }

        // --- the AAB's manifest and layout ---

        // aapt2 36.0.0 `link --proto-format` of a Unity-like release manifest (com.cryptforge.prototype, 0.1.0, code 7, SDK
        // 26..36, INTERNET), the format AGP stores as base/manifest/AndroidManifest.xml in a bundle.
        const string ReleaseManifest =
            "CrgHCjkKB2FuZHJvaWQSKmh0dHA6Ly9zY2hlbWFzLmFuZHJvaWQuY29tL2Fway9yZXMvYW5kcm9pZBoCCAIaCG1hbmlmZXN0IiMSB3Bh" +
            "Y2thZ2UaGGNvbS5jcnlwdGZvcmdlLnByb3RvdHlwZSJLCipodHRwOi8vc2NoZW1hcy5hbmRyb2lkLmNvbS9hcGsvcmVzL2FuZHJvaWQS" +
            "C3ZlcnNpb25Db2RlGgE3IgIIAiibhIQIMgQ6AjAHIkUKKmh0dHA6Ly9zY2hlbWFzLmFuZHJvaWQuY29tL2Fway9yZXMvYW5kcm9pZBIL" +
            "dmVyc2lvbk5hbWUaBTAuMS4wKJyEhAgiUgoqaHR0cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBrL3Jlcy9hbmRyb2lkEhFjb21waWxl" +
            "U2RrVmVyc2lvbhoCMzYiAggCKPKKhAgyBDoCMCQiKBIYcGxhdGZvcm1CdWlsZFZlcnNpb25Db2RlGgIzNiICCAIyBDoCMCQiUAoqaHR0" +
            "cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBrL3Jlcy9hbmRyb2lkEhljb21waWxlU2RrVmVyc2lvbkNvZGVuYW1lGgIxNijzioQIIigS" +
            "GHBsYXRmb3JtQnVpbGRWZXJzaW9uTmFtZRoCMTYiAggCMgQ6AjAQKg4SBQogICAgGgUIAhCcASq2AQqtARoIdXNlcy1zZGsiTgoqaHR0" +
            "cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBrL3Jlcy9hbmRyb2lkEg1taW5TZGtWZXJzaW9uGgIyNiICCAMojISECDIEOgIwGiJRCipo" +
            "dHRwOi8vc2NoZW1hcy5hbmRyb2lkLmNvbS9hcGsvcmVzL2FuZHJvaWQSEHRhcmdldFNka1ZlcnNpb24aAjM2IgIIAyjwhIQIMgQ6AjAk" +
            "GgQIAxAEKg0SBQogICAgGgQIAxBJKm8KZxoPdXNlcy1wZXJtaXNzaW9uIlQKKmh0dHA6Ly9zY2hlbWFzLmFuZHJvaWQuY29tL2Fway9y" +
            "ZXMvYW5kcm9pZBIEbmFtZRobYW5kcm9pZC5wZXJtaXNzaW9uLklOVEVSTkVUKIOAhAgaBAgEEAQqDRIFCiAgICAaBAgEEEIqWwpTGgth" +
            "cHBsaWNhdGlvbiJECipodHRwOi8vc2NoZW1hcy5hbmRyb2lkLmNvbS9hcGsvcmVzL2FuZHJvaWQSBWxhYmVsGgpDcnlwdGZvcmdlKIGA" +
            "hAgaBAgFEAQqCRIBChoECAUQLhoCCAI=";

        // The same tool on a debuggable manifest: com.example.game, 12.0.3, code 2100000000, SDK 24..35.
        const string DebuggableManifest =
            "CscGCjkKB2FuZHJvaWQSKmh0dHA6Ly9zY2hlbWFzLmFuZHJvaWQuY29tL2Fway9yZXMvYW5kcm9pZBoCCAIaCG1hbmlmZXN0IhsSB3Bh" +
            "Y2thZ2UaEGNvbS5leGFtcGxlLmdhbWUiWAoqaHR0cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBrL3Jlcy9hbmRyb2lkEgt2ZXJzaW9u" +
            "Q29kZRoKMjEwMDAwMDAwMCICCAIom4SECDIIOgYwgOqt6QciRgoqaHR0cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBrL3Jlcy9hbmRy" +
            "b2lkEgt2ZXJzaW9uTmFtZRoGMTIuMC4zKJyEhAgiUgoqaHR0cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBrL3Jlcy9hbmRyb2lkEhFj" +
            "b21waWxlU2RrVmVyc2lvbhoCMzYiAggCKPKKhAgyBDoCMCQiKBIYcGxhdGZvcm1CdWlsZFZlcnNpb25Db2RlGgIzNiICCAIyBDoCMCQi" +
            "UAoqaHR0cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBrL3Jlcy9hbmRyb2lkEhljb21waWxlU2RrVmVyc2lvbkNvZGVuYW1lGgIxNijz" +
            "ioQIIigSGHBsYXRmb3JtQnVpbGRWZXJzaW9uTmFtZRoCMTYiAggCMgQ6AjAQKg4SBQogICAgGgUIAhCeASq2AQqtARoIdXNlcy1zZGsi" +
            "TgoqaHR0cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBrL3Jlcy9hbmRyb2lkEg1taW5TZGtWZXJzaW9uGgIyNCICCAMojISECDIEOgIw" +
            "GCJRCipodHRwOi8vc2NoZW1hcy5hbmRyb2lkLmNvbS9hcGsvcmVzL2FuZHJvaWQSEHRhcmdldFNka1ZlcnNpb24aAjM1IgIIAyjwhIQI" +
            "MgQ6AjAjGgQIAxAEKg0SBQogICAgGgQIAxBJKmQKXBoLYXBwbGljYXRpb24iTQoqaHR0cDovL3NjaGVtYXMuYW5kcm9pZC5jb20vYXBr" +
            "L3Jlcy9hbmRyb2lkEgpkZWJ1Z2dhYmxlGgR0cnVlIgIIBCiPgIQIMgQ6AkABGgQIBBAEKgkSAQoaBAgEEC0aAggC";

        [Test]
        public void TheProtobufManifestGivesPackageVersionSdksAndPermissions()
        {
            AabManifest manifest = AabManifest.Parse(Convert.FromBase64String(ReleaseManifest));
            Assert.That(manifest.Package, Is.EqualTo("com.cryptforge.prototype"));
            Assert.That(manifest.VersionName, Is.EqualTo("0.1.0"));
            Assert.That(manifest.VersionCode, Is.EqualTo("7"));
            Assert.That(manifest.MinSdk, Is.EqualTo("26"));
            Assert.That(manifest.TargetSdk, Is.EqualTo("36"));
            Assert.That(manifest.Debuggable, Is.False);
            CollectionAssert.AreEqual(new[] { "android.permission.INTERNET" }, manifest.Permissions);

            AabManifest debuggable = AabManifest.Parse(Convert.FromBase64String(DebuggableManifest));
            Assert.That(debuggable.Package, Is.EqualTo("com.example.game"));
            Assert.That(debuggable.VersionCode, Is.EqualTo("2100000000"));
            Assert.That(debuggable.VersionName, Is.EqualTo("12.0.3"));
            Assert.That(debuggable.MinSdk, Is.EqualTo("24"));
            Assert.That(debuggable.TargetSdk, Is.EqualTo("35"));
            Assert.That(debuggable.Debuggable, Is.True);
            Assert.That(debuggable.Permissions, Is.Empty);
        }

        [Test]
        public void AnAttributeWithoutItsSourceTextFallsBackToItsCompiledInteger()
        {
            // XmlNode { element { name "manifest", attribute { android:versionCode, compiled_item { prim { int_decimal 42 } } } } }
            byte[] attribute = Concat(Text(1, AabManifest.AndroidNamespace), Text(2, "versionCode"),
                Message(6, Message(7, Varint(6, 42))));
            byte[] node = Message(1, Concat(Text(3, "manifest"), Message(4, attribute)));
            AabManifest manifest = AabManifest.Parse(node);
            Assert.That(manifest.VersionCode, Is.EqualTo("42"));
            Assert.That(manifest.Package, Is.Null);
        }

        [Test]
        public void AManifestThatIsNotOneIsRefused()
        {
            byte[] good = Convert.FromBase64String(ReleaseManifest);
            Assert.Throws<FormatException>(() => AabManifest.Parse(new byte[0]));
            Assert.Throws<FormatException>(() => AabManifest.Parse(good.Take(good.Length / 2).ToArray()));
            Assert.Throws<FormatException>(() => AabManifest.Parse(Encoding.UTF8.GetBytes("<manifest package=\"a.b\"/>")));
            Assert.Throws<FormatException>(() => AabManifest.Parse(Message(1, Text(3, "application"))));
        }

        [Test]
        public void TheLayoutListsTheBaseModulesAbisAndLibraries()
        {
            var names = new[]
            {
                "BundleConfig.pb", "base/manifest/AndroidManifest.xml", "base/lib/arm64-v8a/libunity.so", "base/lib/arm64-v8a/libmain.so",
                "base/lib/arm64-v8a/notes.txt", "base/lib/arm64-v8a/sub/libx.so", "base/lib/armeabi-v7a/libunity.so",
                "feature/lib/x86_64/liby.so", "base/root/lib/arm64-v8a/libz.so",
            };
            CollectionAssert.AreEqual(new[] { "arm64-v8a", "armeabi-v7a" }, AabLayout.Abis(names));
            CollectionAssert.AreEqual(new[] { "base/lib/arm64-v8a/libmain.so", "base/lib/arm64-v8a/libunity.so" },
                AabLayout.NativeLibraries(names, "arm64-v8a"));
        }

        // --- the JAR signature ---

        [Test]
        public void TheSignatureIsTheTopLevelManifestAndSignerFilesOnly()
        {
            foreach (string entry in new[] { "META-INF/MANIFEST.MF", "META-INF/ANDROIDD.SF", "META-INF/ANDROIDD.RSA", "META-INF/KEY.DSA",
                "META-INF/KEY.EC", "META-INF/SIG-KEY", "meta-inf/upload.sf" })
                Assert.That(JarSignature.IsSignatureEntry(entry), Is.True, entry);
            foreach (string entry in new[] { "META-INF/", "META-INF/services/x.SF", "base/root/META-INF/MANIFEST.MF", "BundleConfig.pb",
                "META-INF/version-control-info.textproto", "META-INF/com/android/build/gradle/app-metadata.properties" })
                Assert.That(JarSignature.IsSignatureEntry(entry), Is.False, entry);
        }

        [Test]
        public void StrippingTheSignatureKeepsEveryOtherEntryAndItsBytes()
        {
            string folder = Path.Combine(Path.GetTempPath(), "cryptforge-release-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string signed = Path.Combine(folder, "signed.aab"), stripped = Path.Combine(folder, "stripped.aab");
                var contents = new Dictionary<string, byte[]>
                {
                    { "META-INF/MANIFEST.MF", Encoding.ASCII.GetBytes("Manifest-Version: 1.0\r\n") },
                    { "META-INF/ANDROIDD.SF", Encoding.ASCII.GetBytes("Signature-Version: 1.0\r\n") },
                    { "META-INF/ANDROIDD.RSA", new byte[] { 0x30, 0x82, 1, 2 } },
                    { "BundleConfig.pb", new byte[] { 8, 1, 16, 2 } },
                    { "base/lib/arm64-v8a/libmain.so", Enumerable.Range(0, 5000).Select(i => (byte)(i * 7)).ToArray() },
                    { "base/root/META-INF/MANIFEST.MF", Encoding.ASCII.GetBytes("kept: it ships inside the APKs\r\n") },
                };
                using (var file = new FileStream(signed, FileMode.Create))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    foreach (KeyValuePair<string, byte[]> entry in contents)
                    {
                        CompressionLevel level = entry.Key.EndsWith(".so", StringComparison.Ordinal) ? CompressionLevel.NoCompression : CompressionLevel.Optimal;
                        using (Stream stream = zip.CreateEntry(entry.Key, level).Open())
                            stream.Write(entry.Value, 0, entry.Value.Length);
                    }
                }
                var compressed = new Dictionary<string, long>();
                using (var file = new FileStream(signed, FileMode.Open, FileAccess.Read))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                        compressed[entry.FullName] = entry.CompressedLength;
                }

                Assert.That(JarSignature.StripTo(signed, stripped), Is.EqualTo(3));
                Assert.That(File.Exists(signed), Is.True, "The source is left as it was.");
                using (var file = new FileStream(stripped, FileMode.Open, FileAccess.Read))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                {
                    CollectionAssert.AreEquivalent(new[] { "BundleConfig.pb", "base/lib/arm64-v8a/libmain.so", "base/root/META-INF/MANIFEST.MF" },
                        zip.Entries.Select(entry => entry.FullName).ToArray());
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        using (Stream stream = entry.Open())
                        using (var bytes = new MemoryStream())
                        {
                            stream.CopyTo(bytes);
                            CollectionAssert.AreEqual(contents[entry.FullName], bytes.ToArray(), entry.FullName);
                        }
                        Assert.That(entry.CompressedLength, Is.EqualTo(compressed[entry.FullName]), "Compressed bytes kept: " + entry.FullName);
                    }
                }
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void TheFingerprintIsReadFromKeytoolsOutputInEitherSpelling()
        {
            const string hex = "E9:1D:3F:7C:5E:EE:F1:59:10:46:38:AB:3E:16:30:94:5D:28:2A:D0:EC:E9:90:7B:9C:FD:7E:50:A7:4A:BF:AC";
            Assert.That(JdkTools.FingerprintIn("upload, Nov 19, 2026, PrivateKeyEntry, \nCertificate fingerprint (SHA-256): " + hex + "\n"), Is.EqualTo(hex));
            Assert.That(JdkTools.FingerprintIn("Certificate fingerprints:\n\t SHA1: 00:11\n\t SHA256: " + hex + "\nSignature algorithm name: SHA256withRSA"), Is.EqualTo(hex));
            Assert.That(JdkTools.FingerprintIn("jar is unsigned."), Is.Null);
            Assert.That(JdkTools.FingerprintIn(null), Is.Null);
        }

        // --- helpers ---

        static void AssertRefused(string text, string fragment, bool bump = false)
        {
            var error = Assert.Throws<ReleaseConfigException>(() =>
            {
                if (bump)
                    ReleaseVersion.Bump(text);
                else
                    ReleaseVersion.Parse(text);
            }, text);
            Assert.That(error.Message, Does.Contain(fragment), text);
        }

        static Func<string, string> EnvironmentOf(params string[] pairs)
        {
            var values = new Dictionary<string, string>();
            for (int i = 0; i + 1 < pairs.Length; i += 2)
                values[pairs[i]] = pairs[i + 1];
            return name => values.TryGetValue(name, out string value) ? value : null;
        }

        static string FindUp(string relative)
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (var folder = new DirectoryInfo(start); folder != null; folder = folder.Parent)
                {
                    string candidate = Path.Combine(folder.FullName, relative);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            throw new FileNotFoundException("No folder above " + Directory.GetCurrentDirectory() + " holds " + relative + ".");
        }

        // A few protobuf fields by hand: a varint field, a string field, a message field.
        static byte[] Varint(int field, ulong value) => Concat(Key(field, 0), Raw(value));
        static byte[] Text(int field, string value) => Message(field, Encoding.UTF8.GetBytes(value));
        static byte[] Message(int field, byte[] payload) => Concat(Key(field, 2), Raw((ulong)payload.Length), payload);
        static byte[] Key(int field, int wireType) => Raw((ulong)((field << 3) | wireType));

        static byte[] Raw(ulong value)
        {
            var bytes = new List<byte>();
            do
            {
                byte next = (byte)(value & 0x7F);
                value >>= 7;
                bytes.Add(value != 0 ? (byte)(next | 0x80) : next);
            } while (value != 0);
            return bytes.ToArray();
        }

        static byte[] Concat(params byte[][] parts) => parts.SelectMany(part => part).ToArray();
    }
}
