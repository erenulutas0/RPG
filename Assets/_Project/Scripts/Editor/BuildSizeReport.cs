using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Cryptforge.Editor
{
    // Where the bytes of the last player build went. Run right after a build, in batch mode:
    //   Unity.exe -batchmode -quit -projectPath E:/MobileGame -buildTarget Android
    //     -executeMethod Cryptforge.Editor.BuildSizeReport.Write -logFile TestResults/build-size.log
    // An incremental build that reused the player data lists no packed assets; BuildCleanAndWrite rebuilds the data
    // (AndroidPrototypeBuild.BuildClean) and then writes the report.
    // Output: TestResults/build-size.tsv (every packed asset, every output file, every APK entry and the APK's dead
    // bytes) and TestResults/build-size-summary.txt (totals by category, the 40 largest assets, the APK's slack).
    public static class BuildSizeReport
    {
        const string TsvPath = "TestResults/build-size.tsv";
        const string SummaryPath = "TestResults/build-size-summary.txt";
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [MenuItem("Cryptforge/Build Size Report")]
        public static void Write()
        {
            BuildReport report = BuildReport.GetLatestReport();
            if (report == null)
                throw new InvalidOperationException("No build report: Library/LastBuild.buildreport is missing; build first.");
            Write(report);
        }

        public static void BuildCleanAndWrite()
        {
            AndroidPrototypeBuild.BuildClean();
            Write();
        }

        sealed class AssetRow
        {
            public string Path, Type, Category, Detail;
            public ulong Packed;
            public string Bundle;
        }

        static void Write(BuildReport report)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(root, "TestResults"));

            var assets = new Dictionary<string, AssetRow>();
            foreach (PackedAssets packed in report.packedAssets)
            {
                foreach (PackedAssetInfo info in packed.contents)
                {
                    string source = string.IsNullOrEmpty(info.sourceAssetPath) ? "(no source asset)" : info.sourceAssetPath;
                    string type = info.type != null ? info.type.Name : "?";
                    string key = source + "|" + type;
                    if (!assets.TryGetValue(key, out AssetRow row))
                    {
                        row = new AssetRow { Path = source, Type = type, Category = Category(type, source), Bundle = packed.shortPath };
                        assets.Add(key, row);
                    }
                    row.Packed += info.packedSize;
                }
            }
            foreach (AssetRow row in assets.Values)
                row.Detail = Detail(row.Path, row.Type);

            BuildFile[] files = report.GetFiles();
            var apks = files.Where(f => f.path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) && File.Exists(f.path))
                .Select(f => f.path).ToList();
            string outputApk = report.summary.outputPath;
            if (!string.IsNullOrEmpty(outputApk) && outputApk.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)
                && File.Exists(outputApk) && !apks.Contains(outputApk))
                apks.Add(outputApk);
            var apkLayouts = apks.Select(ApkLayout.Read).ToList();

            var tsv = new StringBuilder();
            tsv.Append("kind\tpath\ttype\tcategory\tbytes\tcompressed\tdetail\n");
            foreach (AssetRow row in assets.Values.OrderByDescending(r => r.Packed))
                tsv.Append("asset\t").Append(row.Path).Append('\t').Append(row.Type).Append('\t').Append(row.Category).Append('\t')
                    .Append(row.Packed.ToString(Inv)).Append("\t\t").Append(row.Detail).Append(" in ").Append(row.Bundle).Append('\n');
            foreach (BuildFile file in files.OrderByDescending(f => f.size))
                tsv.Append("file\t").Append(file.path).Append('\t').Append(file.role).Append("\t\t")
                    .Append(file.size.ToString(Inv)).Append("\t\t\n");
            foreach (ApkLayout apk in apkLayouts)
            {
                foreach (ApkLayout.Entry entry in apk.Entries.OrderByDescending(e => e.Size))
                    tsv.Append("apk-entry\t").Append(entry.Name).Append('\t').Append(entry.Stored ? "stored" : "deflated").Append("\t\t")
                        .Append(entry.Size.ToString(Inv)).Append('\t').Append(entry.CompressedSize.ToString(Inv)).Append('\t')
                        .Append(Path.GetFileName(apk.Path)).Append('\n');
                foreach (ApkLayout.Gap gap in apk.Gaps.OrderByDescending(g => g.Length))
                    tsv.Append("apk-gap\t").Append(Path.GetFileName(apk.Path)).Append("\toffset ").Append(gap.Offset.ToString(Inv))
                        .Append("\t\t").Append(gap.Length.ToString(Inv)).Append("\t\tdead bytes before ").Append(gap.Before).Append('\n');
            }
            File.WriteAllText(Path.Combine(root, TsvPath), tsv.ToString().Replace("\r\n", "\n"));

            var summary = new StringBuilder();
            summary.Append("Build: ").Append(report.summary.platform).Append(' ').Append(report.summary.result)
                .Append(", ").Append(report.summary.options).Append('\n');
            summary.Append("Output: ").Append(report.summary.outputPath).Append('\n');
            summary.Append("Ended: ").Append(report.summary.buildEndedAt.ToString("u", Inv)).Append('\n');
            summary.Append("Report totalSize: ").Append(report.summary.totalSize.ToString(Inv)).Append(" bytes\n\n");

            foreach (ApkLayout apk in apkLayouts)
            {
                summary.Append("APK ").Append(apk.Path).Append('\n');
                summary.Append("  file bytes            ").Append(apk.FileSize.ToString(Inv)).Append('\n');
                summary.Append("  live entry bytes      ").Append(apk.LiveBytes.ToString(Inv))
                    .Append(" (").Append(apk.Entries.Count).Append(" entries, local headers and data)\n");
                summary.Append("  dead bytes (holes)    ").Append(apk.Gaps.Sum(g => g.Length).ToString(Inv))
                    .Append(" in ").Append(apk.Gaps.Count).Append(" gaps\n");
                summary.Append("  central directory etc ").Append(apk.TailBytes.ToString(Inv)).Append("\n\n");
            }

            if (assets.Count == 0)
            {
                summary.Append("No packed assets: the player data was not rebuilt (incremental build). Run\n")
                    .Append("Cryptforge.Editor.BuildSizeReport.BuildCleanAndWrite for the per-asset list.\n\n");
            }
            else
            {
                summary.Append("Packed bytes by category:\n");
                foreach (var group in assets.Values.GroupBy(r => r.Category).OrderByDescending(g => g.Sum(r => (double)r.Packed)))
                    summary.Append("  ").Append(group.Key.PadRight(10)).Append(' ')
                        .Append(((ulong)group.Sum(r => (double)r.Packed)).ToString(Inv).PadLeft(12))
                        .Append("  (").Append(group.Count()).Append(" assets)\n");
                summary.Append("  ").Append("total".PadRight(10)).Append(' ')
                    .Append(((ulong)assets.Values.Sum(r => (double)r.Packed)).ToString(Inv).PadLeft(12)).Append("\n\n");
                summary.Append("40 largest assets:\n");
                foreach (AssetRow row in assets.Values.OrderByDescending(r => r.Packed).Take(40))
                    summary.Append(row.Packed.ToString(Inv).PadLeft(11)).Append("  ").Append(row.Type.PadRight(14)).Append(' ')
                        .Append(row.Path).Append("  [").Append(row.Detail).Append("]\n");
                summary.Append('\n');
            }

            summary.Append("Largest output files:\n");
            foreach (BuildFile file in files.OrderByDescending(f => f.size).Take(15))
                summary.Append(file.size.ToString(Inv).PadLeft(11)).Append("  ").Append(file.role.PadRight(18)).Append(' ').Append(file.path).Append('\n');

            File.WriteAllText(Path.Combine(root, SummaryPath), summary.ToString().Replace("\r\n", "\n"));
            Debug.Log($"Build size report: {TsvPath} ({assets.Count} assets, {files.Length} files), {SummaryPath}");
        }

        static string Category(string type, string path)
        {
            switch (type)
            {
                case "Texture2D": case "Sprite": case "Cubemap": case "Texture2DArray": case "Texture3D": case "RenderTexture":
                    return "textures";
                case "Mesh": return "meshes";
                case "Shader": case "ComputeShader": case "ShaderVariantCollection": return "shaders";
                case "MonoScript": case "MonoBehaviour" when path.EndsWith(".cs", StringComparison.Ordinal): return "scripts";
                case "Font": return "fonts";
                case "AudioClip": return "audio";
                case "AnimationClip": case "AnimatorController": return "animation";
                case "Material": return "materials";
            }
            if (path.EndsWith(".asset", StringComparison.Ordinal) && path.Contains("SDF")) return "fonts";
            return "other";
        }

        static string Detail(string path, string type)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) && !path.StartsWith("Packages/", StringComparison.Ordinal))
                return "";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                TextureImporterPlatformSettings fallback = importer.GetDefaultPlatformTextureSettings();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                string imported = texture != null
                    ? $"{texture.width}x{texture.height} {texture.format} {texture.mipmapCount} mips"
                    : "not a Texture2D";
                string requested = android.overridden
                    ? $"Android override {android.format} max {android.maxTextureSize}"
                    : $"no Android override ({fallback.textureCompression}, max {fallback.maxTextureSize}, crunch {fallback.crunchedCompression})";
                return $"{imported}; {importer.textureType}, mips {(importer.mipmapEnabled ? "on" : "off")}, {requested}";
            }
            if (type == "Mesh" && AssetImporter.GetAtPath(path) is ModelImporter model)
                return $"model, compression {model.meshCompression}, read/write {model.isReadable}";
            return "";
        }

        // The zip layout of an APK: the bytes its entries occupy, and the bytes between them that no entry owns.
        sealed class ApkLayout
        {
            public struct Entry { public string Name; public long Size, CompressedSize, Offset, End; public bool Stored; }
            public struct Gap { public long Offset, Length; public string Before; }

            public string Path;
            public long FileSize, LiveBytes, TailBytes;
            public readonly List<Entry> Entries = new List<Entry>();
            public readonly List<Gap> Gaps = new List<Gap>();

            public static ApkLayout Read(string path)
            {
                var layout = new ApkLayout { Path = path };
                byte[] bytes = File.ReadAllBytes(path);
                layout.FileSize = bytes.LongLength;
                int eocd = -1;
                for (int i = bytes.Length - 22; i >= Math.Max(0, bytes.Length - 65557); i--)
                    if (U32(bytes, i) == 0x06054b50) { eocd = i; break; }
                if (eocd < 0)
                    return layout;
                int count = U16(bytes, eocd + 10);
                long cd = U32(bytes, eocd + 16);
                long p = cd;
                for (int n = 0; n < count && p + 46 <= bytes.Length && U32(bytes, (int)p) == 0x02014b50; n++)
                {
                    int method = U16(bytes, (int)p + 10);
                    long compressed = U32(bytes, (int)p + 20), size = U32(bytes, (int)p + 24);
                    int nameLength = U16(bytes, (int)p + 28), extra = U16(bytes, (int)p + 30), comment = U16(bytes, (int)p + 32);
                    long local = U32(bytes, (int)p + 42);
                    string name = Encoding.UTF8.GetString(bytes, (int)p + 46, nameLength);
                    int localName = U16(bytes, (int)local + 26), localExtra = U16(bytes, (int)local + 28);
                    long end = local + 30 + localName + localExtra + compressed;
                    if ((U16(bytes, (int)local + 6) & 8) != 0)
                        end += 16;
                    layout.Entries.Add(new Entry { Name = name, Size = size, CompressedSize = compressed, Offset = local, End = end, Stored = method == 0 });
                    p += 46 + nameLength + extra + comment;
                }
                long cursor = 0;
                foreach (Entry entry in layout.Entries.OrderBy(e => e.Offset))
                {
                    if (entry.Offset > cursor)
                        layout.Gaps.Add(new Gap { Offset = cursor, Length = entry.Offset - cursor, Before = entry.Name });
                    layout.LiveBytes += entry.End - entry.Offset;
                    cursor = Math.Max(cursor, entry.End);
                }
                // Anything between the last entry and the central directory is the APK signing block.
                layout.TailBytes = layout.FileSize - cursor;
                return layout;
            }

            static int U16(byte[] b, int i) => b[i] | (b[i + 1] << 8);
            static long U32(byte[] b, int i) => (uint)(b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24));
        }
    }
}
