using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

// The release pipeline's pure rules (docs/61): the version file, the signing environment, the settings the build changes
// and the command line. No UnityEditor here: Unity's EditMode suite, Tools/CombatChecks and Tools/Build-Release.ps1 (through
// Add-Type) compile this one file alike, so the wrapper bumps and checks the version with the same parser the build reads.
namespace Cryptforge.Editor.Release
{
    // A release setting, file or argument that is wrong: the message names the problem, never a secret's value.
    public sealed class ReleaseConfigException : Exception
    {
        public ReleaseConfigException(string message) : base(message) { }
    }

    // versionName and versionCode, read from the committed Tools/Release/version.txt. The build itself bumps nothing;
    // Build-Release.ps1 -BumpVersion raises versionCode by one before it builds.
    public sealed class ReleaseVersion
    {
        public const string FilePath = "Tools/Release/version.txt";
        // Google Play's ceiling for versionCode.
        public const int MaxVersionCode = 2100000000;
        const string NameKey = "versionName";
        const string CodeKey = "versionCode";
        static readonly Regex NamePattern = new Regex(@"^(0|[1-9][0-9]{0,3})\.(0|[1-9][0-9]{0,3})\.(0|[1-9][0-9]{0,3})$", RegexOptions.CultureInvariant);
        static readonly Regex CodeLine = new Regex(@"^([ \t]*versionCode[ \t]*=[ \t]*)([0-9]+)", RegexOptions.CultureInvariant | RegexOptions.Multiline);

        public readonly string Name;
        public readonly int Code;

        public ReleaseVersion(string name, int code)
        {
            if (name == null || !NamePattern.IsMatch(name))
                throw new ReleaseConfigException($"versionName \"{name}\" is not MAJOR.MINOR.PATCH (digits, no leading zeros, e.g. 0.1.0).");
            if (code < 1 || code > MaxVersionCode)
                throw new ReleaseConfigException($"versionCode {code.ToString(CultureInfo.InvariantCulture)} is outside 1..{MaxVersionCode.ToString(CultureInfo.InvariantCulture)}.");
            Name = name;
            Code = code;
        }

        public override string ToString() => Name + " (" + Code.ToString(CultureInfo.InvariantCulture) + ")";

        // Blank lines and '#' comments are skipped; every other line is key=value. Both keys are required, once each, and an
        // unknown key is an error, so a typo cannot leave the previous value in force.
        public static ReleaseVersion Parse(string text)
        {
            if (text == null)
                throw new ReleaseConfigException(FilePath + " is missing.");
            string name = null, code = null;
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                int equals = line.IndexOf('=');
                string where = FilePath + " line " + (i + 1).ToString(CultureInfo.InvariantCulture);
                if (equals <= 0)
                    throw new ReleaseConfigException(where + " is not key=value.");
                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                if (string.Equals(key, NameKey, StringComparison.Ordinal))
                {
                    if (name != null)
                        throw new ReleaseConfigException(where + " repeats " + NameKey + ".");
                    name = value;
                }
                else if (string.Equals(key, CodeKey, StringComparison.Ordinal))
                {
                    if (code != null)
                        throw new ReleaseConfigException(where + " repeats " + CodeKey + ".");
                    code = value;
                }
                else
                    throw new ReleaseConfigException(where + " has an unknown key \"" + key + "\" (only " + NameKey + " and " + CodeKey + ").");
            }
            if (name == null)
                throw new ReleaseConfigException(FilePath + " has no " + NameKey + ".");
            if (code == null)
                throw new ReleaseConfigException(FilePath + " has no " + CodeKey + ".");
            if (!int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
                throw new ReleaseConfigException(CodeKey + " \"" + code + "\" is not a whole number in 1.." + MaxVersionCode.ToString(CultureInfo.InvariantCulture) + ".");
            return new ReleaseVersion(name, parsed);
        }

        // The file's text with versionCode raised by one and every other byte (comments, line endings) as it was.
        public static string Bump(string text)
        {
            ReleaseVersion current = Parse(text);
            if (current.Code >= MaxVersionCode)
                throw new ReleaseConfigException(CodeKey + " is already Google Play's ceiling, " + MaxVersionCode.ToString(CultureInfo.InvariantCulture) + ".");
            string next = (current.Code + 1).ToString(CultureInfo.InvariantCulture);
            return CodeLine.Replace(text, match => match.Groups[1].Value + next, 1);
        }
    }

    public enum ReleaseSigningKind { DebugKey, UploadKey }

    // Which key signs the AAB, from four environment variables. None set: Unity's debug key, and the report says so. All four
    // set: the owner's upload key. Some set: an error that names the missing ones, so a half-configured shell never yields a
    // debug-key build by accident. The passwords are only tested for presence: they stay in the environment and reach keytool
    // and jarsigner by name (-storepass:env), so no password is ever held here, written to a file or logged.
    public sealed class ReleaseSigning
    {
        public const string KeystorePathVariable = "CRYPTFORGE_KEYSTORE_PATH";
        public const string KeystorePassVariable = "CRYPTFORGE_KEYSTORE_PASS";
        public const string KeyAliasVariable = "CRYPTFORGE_KEY_ALIAS";
        public const string KeyPassVariable = "CRYPTFORGE_KEY_PASS";
        public const string DebugKeyStamp = "NOT FOR PLAY: debug key";
        public static readonly string[] Variables = { KeystorePathVariable, KeystorePassVariable, KeyAliasVariable, KeyPassVariable };

        public readonly ReleaseSigningKind Kind;
        public readonly string KeystorePath;
        public readonly string KeyAlias;

        ReleaseSigning(ReleaseSigningKind kind, string keystorePath, string keyAlias)
        {
            Kind = kind;
            KeystorePath = keystorePath;
            KeyAlias = keyAlias;
        }

        public static ReleaseSigning Resolve(Func<string, string> environment, Func<string, bool> fileExists)
        {
            var missing = new List<string>();
            foreach (string variable in Variables)
            {
                if (string.IsNullOrWhiteSpace(environment(variable)))
                    missing.Add(variable);
            }
            if (missing.Count == Variables.Length)
                return new ReleaseSigning(ReleaseSigningKind.DebugKey, null, null);
            if (missing.Count > 0)
                throw new ReleaseConfigException("The upload key is half configured: " + string.Join(", ", missing.ToArray()) +
                    (missing.Count == 1 ? " is" : " are") + " not set. Set all four CRYPTFORGE_* variables, or none for a debug-key build (docs/61).");
            string path = environment(KeystorePathVariable).Trim();
            if (!fileExists(path))
                throw new ReleaseConfigException(KeystorePathVariable + " names no file: " + path);
            return new ReleaseSigning(ReleaseSigningKind.UploadKey, path, environment(KeyAliasVariable).Trim());
        }

        public string Describe() => Kind == ReleaseSigningKind.DebugKey
            ? DebugKeyStamp + " (none of the CRYPTFORGE_* variables is set)"
            : "upload key, alias " + KeyAlias + " in " + KeystorePath;
    }

    // Every setting the release build changes, by name, and the value it sets. AndroidReleaseBuild applies a plan only through
    // its table of settings, saves every entry of that table before and restores every entry in a finally block, and the
    // EditMode suite holds the table's names to Names: the build cannot change a setting it does not restore.
    public static class ReleaseSettings
    {
        // The package stays the prototype's until the owner chooses the permanent one: it can never change after the first
        // upload (docs/59 section 2). One constant, and -packageName for a trial build.
        public const string DefaultPackageName = "com.cryptforge.prototype";
        public const int TargetSdk = 36;
        public const int MinSdk = 26;

        public const string ScriptingBackend = "PlayerSettings.ScriptingBackend(Android)";
        public const string Il2CppConfiguration = "PlayerSettings.Il2CppCompilerConfiguration(Android)";
        public const string TargetArchitectures = "PlayerSettings.Android.targetArchitectures";
        public const string MinSdkVersion = "PlayerSettings.Android.minSdkVersion";
        public const string TargetSdkVersion = "PlayerSettings.Android.targetSdkVersion";
        public const string ApplicationIdentifier = "PlayerSettings.ApplicationIdentifier(Android)";
        public const string VersionName = "PlayerSettings.bundleVersion";
        public const string VersionCode = "PlayerSettings.Android.bundleVersionCode";
        public const string UseCustomKeystore = "PlayerSettings.Android.useCustomKeystore";
        public const string Orientation = "PlayerSettings.defaultInterfaceOrientation";
        public const string BuildAppBundle = "EditorUserBuildSettings.buildAppBundle";
        public const string ExportAsGoogleAndroidProject = "EditorUserBuildSettings.exportAsGoogleAndroidProject";
        public const string Development = "EditorUserBuildSettings.development";
        public const string AndroidBuildType = "EditorUserBuildSettings.androidBuildType";

        // In the order they are applied: the backend before the architectures (ARM64 needs IL2CPP).
        public static readonly string[] Names =
        {
            ScriptingBackend, Il2CppConfiguration, TargetArchitectures, MinSdkVersion, TargetSdkVersion, ApplicationIdentifier,
            VersionName, VersionCode, UseCustomKeystore, Orientation, BuildAppBundle, ExportAsGoogleAndroidProject, Development,
            AndroidBuildType
        };

        // The values as plain strings; the Editor's table parses each into its Unity type. Unity never signs with the upload
        // key (UseCustomKeystore false): Unity 6 writes a custom keystore's passwords in plain text into Library/Bee (the
        // build program's input data and the generated launcher/build.gradle), so the AAB leaves Unity with the debug key and
        // the upload key replaces it afterwards through jarsigner (AabSigning).
        public static List<KeyValuePair<string, string>> Plan(ReleaseVersion version, string packageName)
        {
            RequireValidPackageName(packageName);
            return new List<KeyValuePair<string, string>>
            {
                Pair(ScriptingBackend, "IL2CPP"),
                Pair(Il2CppConfiguration, "Release"),
                Pair(TargetArchitectures, "ARM64"),
                Pair(MinSdkVersion, MinSdk.ToString(CultureInfo.InvariantCulture)),
                Pair(TargetSdkVersion, TargetSdk.ToString(CultureInfo.InvariantCulture)),
                Pair(ApplicationIdentifier, packageName),
                Pair(VersionName, version.Name),
                Pair(VersionCode, version.Code.ToString(CultureInfo.InvariantCulture)),
                Pair(UseCustomKeystore, "false"),
                Pair(Orientation, "Portrait"),
                Pair(BuildAppBundle, "true"),
                Pair(ExportAsGoogleAndroidProject, "false"),
                Pair(Development, "false"),
                Pair(AndroidBuildType, "Release"),
            };
        }

        static KeyValuePair<string, string> Pair(string name, string value) => new KeyValuePair<string, string>(name, value);

        // Play's rule for an application id: two or more segments, each a letter then letters, digits or underscores.
        static readonly Regex PackagePattern = new Regex(@"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$", RegexOptions.CultureInvariant);

        public static bool IsValidPackageName(string name) => name != null && name.Length <= 150 && PackagePattern.IsMatch(name);

        public static void RequireValidPackageName(string name)
        {
            if (!IsValidPackageName(name))
                throw new ReleaseConfigException("\"" + name + "\" is not an Android package name (two or more dot-separated segments, each a letter then letters, digits or _).");
        }

        // Builds/Release/Cryptforge-0.1.0-7.aab, or ...-debugkey.aab: a debug-key AAB says so in its own name.
        public const string OutputFolder = "Builds/Release";
        public const string SummaryFile = "Builds/Release/last-build.txt";

        public static string AabFileName(ReleaseVersion version, ReleaseSigningKind signing) =>
            "Cryptforge-" + version.Name + "-" + version.Code.ToString(CultureInfo.InvariantCulture) +
            (signing == ReleaseSigningKind.DebugKey ? "-debugkey" : "") + ".aab";
    }

    // The batch command line: -packageName com.example.game.
    public static class ReleaseArguments
    {
        public const string PackageName = "-packageName";

        // The value after the named option, or null when the option is absent.
        public static string Value(string[] arguments, string option)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!string.Equals(arguments[i], option, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (i + 1 >= arguments.Length || arguments[i + 1].StartsWith("-", StringComparison.Ordinal))
                    throw new ReleaseConfigException(option + " needs a value.");
                return arguments[i + 1];
            }
            return null;
        }

        public static string PackageNameFrom(string[] arguments)
        {
            string name = Value(arguments, PackageName) ?? ReleaseSettings.DefaultPackageName;
            ReleaseSettings.RequireValidPackageName(name);
            return name;
        }
    }

    // key=value lines, the build's summary for the wrapper (Builds/Release/last-build.txt).
    public static class ReleaseSummary
    {
        public static string Write(IList<KeyValuePair<string, string>> rows)
        {
            var text = new StringBuilder();
            foreach (KeyValuePair<string, string> row in rows)
                text.Append(row.Key).Append('=').Append(row.Value).Append('\n');
            return text.ToString();
        }

        public static Dictionary<string, string> Read(string text)
        {
            var rows = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                int equals = raw.IndexOf('=');
                if (equals > 0)
                    rows[raw.Substring(0, equals)] = raw.Substring(equals + 1);
            }
            return rows;
        }
    }
}
