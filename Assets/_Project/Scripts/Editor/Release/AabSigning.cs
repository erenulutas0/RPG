using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

// The upload key's signature on the release AAB (docs/61). Unity builds the AAB with its debug key and never sees the upload
// key's passwords; this removes the debug signature and signs with the upload key through the JDK's jarsigner, which reads
// both passwords from the environment by name (-storepass:env, -keypass:env). No password passes through this code, a
// command line, a file or a log. No UnityEditor here: AndroidReleaseBuild passes the JDK's folder.
namespace Cryptforge.Editor.Release
{
    // An AAB is a ZIP signed the JAR way (v1): META-INF/MANIFEST.MF and a .SF/.RSA pair per signer, at the archive's top.
    public static class JarSignature
    {
        public static bool IsSignatureEntry(string entryName)
        {
            const string folder = "META-INF/";
            if (!entryName.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                return false;
            string file = entryName.Substring(folder.Length);
            if (file.Length == 0 || file.IndexOf('/') >= 0)
                return false;
            string upper = file.ToUpperInvariant();
            return upper == "MANIFEST.MF" || upper.StartsWith("SIG-", StringComparison.Ordinal) ||
                upper.EndsWith(".SF", StringComparison.Ordinal) || upper.EndsWith(".RSA", StringComparison.Ordinal) ||
                upper.EndsWith(".DSA", StringComparison.Ordinal) || upper.EndsWith(".EC", StringComparison.Ordinal);
        }

        // Copies the archive without its signature entries and returns how many it removed. ZipArchive's update mode writes
        // every other entry's compressed bytes back unchanged.
        public static int StripTo(string source, string destination)
        {
            File.Copy(source, destination, true);
            using (var stream = new FileStream(destination, FileMode.Open, FileAccess.ReadWrite))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Update))
            {
                var doomed = new List<ZipArchiveEntry>();
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if (IsSignatureEntry(entry.FullName))
                        doomed.Add(entry);
                }
                foreach (ZipArchiveEntry entry in doomed)
                    entry.Delete();
                return doomed.Count;
            }
        }
    }

    // keytool and jarsigner from a JDK's bin folder (Unity's embedded OpenJDK), in English whatever the system language, so
    // their output parses on a Turkish Windows too.
    public sealed class JdkTools
    {
        const string English = "-J-Duser.language=en -J-Duser.country=US";
        // keytool -list prints "Certificate fingerprint (SHA-256): AB:..", keytool -printcert "SHA256: AB:..".
        static readonly Regex Sha256 = new Regex(@"SHA-?256\)?:\s*((?:[0-9A-F]{2}:){31}[0-9A-F]{2})", RegexOptions.CultureInvariant);

        readonly string bin;
        readonly Action<string> log;

        public JdkTools(string jdkBin, Action<string> log)
        {
            bin = jdkBin;
            this.log = log;
        }

        public string Tool(string name)
        {
            string exe = Path.Combine(bin, name + (Path.DirectorySeparatorChar == '\\' ? ".exe" : ""));
            if (!File.Exists(exe))
                throw new ReleaseConfigException("No " + name + " at " + exe + ".");
            return exe;
        }

        public static string Quote(string argument) => "\"" + argument + "\"";

        // The first SHA-256 certificate fingerprint in keytool's output, or null.
        public static string FingerprintIn(string output)
        {
            Match match = Sha256.Match(output ?? "");
            return match.Success ? match.Groups[1].Value : null;
        }

        // Runs a JDK tool; stdin is closed, so a tool that wants to prompt fails instead of waiting.
        public string Run(string tool, string arguments, bool mustSucceed = true)
        {
            string exe = Tool(tool);
            var info = new ProcessStartInfo(exe, English + " " + arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            // The arguments hold paths, an alias and the names of the password variables, never a password.
            log?.Invoke(tool + " " + arguments);
            using (Process process = Process.Start(info))
            {
                process.StandardInput.Close();
                var errors = process.StandardError.ReadToEndAsync();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                string all = (output + errors.Result).Trim();
                if (mustSucceed && process.ExitCode != 0)
                    throw new ReleaseConfigException(tool + " failed (exit " + process.ExitCode + "): " + all);
                return all;
            }
        }
    }

    public static class AabSigner
    {
        // keytool -list: the keystore opens with the password, the alias holds a private key. Returns the key's certificate
        // fingerprint. Run before the build, so a wrong password fails in seconds, not after the build.
        public static string CheckUploadKey(JdkTools jdk, ReleaseSigning signing)
        {
            string output = jdk.Run("keytool", "-list -keystore " + JdkTools.Quote(signing.KeystorePath) + " -alias " +
                JdkTools.Quote(signing.KeyAlias) + " -storepass:env " + ReleaseSigning.KeystorePassVariable);
            if (output.IndexOf("PrivateKeyEntry", StringComparison.Ordinal) < 0)
                throw new ReleaseConfigException("The alias " + signing.KeyAlias + " holds no private key.");
            string fingerprint = JdkTools.FingerprintIn(output);
            if (fingerprint == null)
                throw new ReleaseConfigException("keytool printed no SHA-256 fingerprint for " + signing.KeyAlias + ".");
            return fingerprint;
        }

        // Unity's debug-signed AAB in, the upload-signed AAB out (the unsigned copy stays in workFolder). Returns the signer's
        // certificate fingerprint as the signed file reports it, which must be the upload key's.
        public static string Sign(JdkTools jdk, ReleaseSigning signing, string unityAab, string signedAab, string workFolder, string expectedFingerprint)
        {
            Directory.CreateDirectory(workFolder);
            string unsigned = Path.Combine(workFolder, Path.GetFileNameWithoutExtension(signedAab) + "-unsigned.aab");
            JarSignature.StripTo(unityAab, unsigned);
            if (File.Exists(signedAab))
                File.Delete(signedAab);
            // SHA-256 throughout, as the Android Gradle plugin signs a bundle with an RSA key (docs/61's keytool command
            // makes one); jarsigner's own default for a 4096-bit key would be SHA-384.
            jdk.Run("jarsigner", "-sigalg SHA256withRSA -digestalg SHA-256 -keystore " + JdkTools.Quote(signing.KeystorePath) +
                " -storepass:env " + ReleaseSigning.KeystorePassVariable + " -keypass:env " + ReleaseSigning.KeyPassVariable +
                " -signedjar " + JdkTools.Quote(signedAab) + " " + JdkTools.Quote(unsigned) + " " + JdkTools.Quote(signing.KeyAlias));
            string verify = jdk.Run("jarsigner", "-verify " + JdkTools.Quote(signedAab));
            if (verify.IndexOf("jar verified", StringComparison.Ordinal) < 0)
                throw new ReleaseConfigException("jarsigner -verify did not verify " + signedAab + ": " + verify);
            string fingerprint = JdkTools.FingerprintIn(jdk.Run("keytool", "-printcert -jarfile " + JdkTools.Quote(signedAab)));
            if (fingerprint == null || !string.Equals(fingerprint, expectedFingerprint, StringComparison.Ordinal))
                throw new ReleaseConfigException("The signed AAB's certificate (" + (fingerprint ?? "none") + ") is not the upload key's (" + expectedFingerprint + ").");
            return fingerprint;
        }

        // The certificate an AAB is signed with, as keytool -printcert -jarfile reports it: the owner line and the SHA-256.
        public static string[] Signer(JdkTools jdk, string aab)
        {
            string output = jdk.Run("keytool", "-printcert -jarfile " + JdkTools.Quote(aab), mustSucceed: false);
            Match owner = Regex.Match(output, @"Owner:\s*(.+)", RegexOptions.CultureInvariant);
            return new[] { owner.Success ? owner.Groups[1].Value.Trim() : null, JdkTools.FingerprintIn(output) };
        }
    }

    // The AAB's entry names and one entry's bytes, for the build's check of what it made.
    public static class ZipEntries
    {
        public static List<string> Names(string path)
        {
            var names = new List<string>();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                    names.Add(entry.FullName);
            }
            return names;
        }

        public static byte[] Read(string path, string entryName)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                ZipArchiveEntry entry = zip.GetEntry(entryName);
                if (entry == null)
                    return null;
                using (Stream stream = entry.Open())
                using (var bytes = new MemoryStream())
                {
                    stream.CopyTo(bytes);
                    return bytes.ToArray();
                }
            }
        }
    }
}
