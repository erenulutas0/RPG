using System;
using System.Collections.Generic;
using System.Text;

// What an Android App Bundle says about itself (docs/61): its manifest and its native libraries' ABIs. The build checks the
// AAB it made against its plan, and Tools/Build-Release.ps1 (through Add-Type) reads the same values for its report, from the
// file that is uploaded rather than from a Gradle intermediate that may be stale. No UnityEditor here.
namespace Cryptforge.Editor.Release
{
    // base/manifest/AndroidManifest.xml of an AAB is aapt2's protobuf XML (frameworks/base/tools/aapt2/Resources.proto:
    // XmlNode, XmlElement, XmlAttribute), which `aapt2 dump` reads from an APK but not from a bundle. This walks the few fields
    // it needs by number. An attribute keeps its source text in `value`; a compiled integer is the fallback.
    public sealed class AabManifest
    {
        public const string EntryName = "base/manifest/AndroidManifest.xml";
        public const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

        public string Package, VersionName, VersionCode, MinSdk, TargetSdk;
        public bool Debuggable;
        public readonly List<string> Permissions = new List<string>();

        public static AabManifest Parse(byte[] proto)
        {
            if (proto == null || proto.Length == 0)
                throw new FormatException("The manifest is empty.");
            Element root = ReadNode(proto, 0, proto.Length);
            if (root == null || root.Name != "manifest")
                throw new FormatException("The protobuf manifest's root is not <manifest>.");
            var manifest = new AabManifest
            {
                Package = root.Attribute("", "package"),
                VersionCode = root.Attribute(AndroidNamespace, "versionCode"),
                VersionName = root.Attribute(AndroidNamespace, "versionName"),
            };
            foreach (Element child in root.Children)
            {
                if (child.Name == "uses-sdk")
                {
                    manifest.MinSdk = child.Attribute(AndroidNamespace, "minSdkVersion");
                    manifest.TargetSdk = child.Attribute(AndroidNamespace, "targetSdkVersion");
                }
                else if (child.Name == "uses-permission")
                {
                    string permission = child.Attribute(AndroidNamespace, "name");
                    if (permission != null)
                        manifest.Permissions.Add(permission);
                }
                else if (child.Name == "application")
                {
                    string debuggable = child.Attribute(AndroidNamespace, "debuggable");
                    manifest.Debuggable = debuggable == "true" || debuggable == "1";
                }
            }
            return manifest;
        }

        sealed class Element
        {
            public string Name;
            public readonly List<string[]> Attributes = new List<string[]>();
            public readonly List<Element> Children = new List<Element>();

            public string Attribute(string ns, string name)
            {
                foreach (string[] attribute in Attributes)
                {
                    if (attribute[0] == ns && attribute[1] == name)
                        return attribute[2];
                }
                return null;
            }
        }

        // XmlNode: 1 element, 2 text, 3 source.
        static Element ReadNode(byte[] b, int start, int end)
        {
            Element element = null;
            for (int p = start; p < end;)
            {
                Field f = Field.Read(b, ref p, end);
                if (f.Number == 1 && f.WireType == 2)
                    element = ReadElement(b, f.Start, f.End);
            }
            return element;
        }

        // XmlElement: 1 namespace declaration, 2 namespace uri, 3 name, 4 attribute, 5 child node.
        static Element ReadElement(byte[] b, int start, int end)
        {
            var element = new Element();
            for (int p = start; p < end;)
            {
                Field f = Field.Read(b, ref p, end);
                if (f.WireType != 2)
                    continue;
                if (f.Number == 3)
                    element.Name = f.String(b);
                else if (f.Number == 4)
                    element.Attributes.Add(ReadAttribute(b, f.Start, f.End));
                else if (f.Number == 5)
                {
                    Element child = ReadNode(b, f.Start, f.End);
                    if (child != null)
                        element.Children.Add(child);
                }
            }
            return element;
        }

        // XmlAttribute: 1 namespace uri, 2 name, 3 value, 4 source, 5 resource id, 6 compiled item.
        static string[] ReadAttribute(byte[] b, int start, int end)
        {
            string ns = "", name = "", value = null, compiled = null;
            for (int p = start; p < end;)
            {
                Field f = Field.Read(b, ref p, end);
                if (f.WireType != 2)
                    continue;
                if (f.Number == 1)
                    ns = f.String(b);
                else if (f.Number == 2)
                    name = f.String(b);
                else if (f.Number == 3)
                    value = f.String(b);
                else if (f.Number == 6)
                    compiled = CompiledInteger(b, f.Start, f.End);
            }
            return new[] { ns, name, string.IsNullOrEmpty(value) ? compiled : value };
        }

        // Item: 7 prim. Primitive: 6 int_decimal_value, 7 int_hexadecimal_value, 8 boolean_value.
        static string CompiledInteger(byte[] b, int start, int end)
        {
            for (int p = start; p < end;)
            {
                Field item = Field.Read(b, ref p, end);
                if (item.Number != 7 || item.WireType != 2)
                    continue;
                for (int q = item.Start; q < item.End;)
                {
                    Field prim = Field.Read(b, ref q, item.End);
                    if (prim.WireType != 0)
                        continue;
                    if (prim.Number == 6 || prim.Number == 7)
                        return unchecked((int)prim.Varint).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (prim.Number == 8)
                        return prim.Varint != 0 ? "true" : "false";
                }
            }
            return null;
        }

        // One protobuf field: its number, wire type and, for length-delimited fields, where its bytes lie.
        struct Field
        {
            public int Number, WireType, Start, End;
            public ulong Varint;

            public string String(byte[] b) => Encoding.UTF8.GetString(b, Start, End - Start);

            public static Field Read(byte[] b, ref int p, int end)
            {
                ulong key = ReadVarint(b, ref p, end);
                var f = new Field { Number = (int)(key >> 3), WireType = (int)(key & 7) };
                switch (f.WireType)
                {
                    case 0:
                        f.Varint = ReadVarint(b, ref p, end);
                        break;
                    case 1:
                        p = Skip(p, 8, end);
                        break;
                    case 2:
                        ulong length = ReadVarint(b, ref p, end);
                        if (length > (ulong)(end - p))
                            throw new FormatException("A protobuf field runs past its message.");
                        f.Start = p;
                        f.End = p + (int)length;
                        p = f.End;
                        break;
                    case 5:
                        p = Skip(p, 4, end);
                        break;
                    default:
                        throw new FormatException("Unsupported protobuf wire type " + f.WireType + ".");
                }
                return f;
            }

            static int Skip(int p, int count, int end)
            {
                if (p + count > end)
                    throw new FormatException("A protobuf field runs past its message.");
                return p + count;
            }

            static ulong ReadVarint(byte[] b, ref int p, int end)
            {
                ulong value = 0;
                for (int shift = 0; shift < 64; shift += 7)
                {
                    if (p >= end)
                        throw new FormatException("A protobuf varint runs past its message.");
                    byte next = b[p++];
                    value |= (ulong)(next & 0x7F) << shift;
                    if ((next & 0x80) == 0)
                        return value;
                }
                throw new FormatException("A protobuf varint is longer than ten bytes.");
            }
        }
    }

    // The ABIs and native libraries of a bundle's base module, from its entry names (base/lib/<abi>/<library>.so).
    public static class AabLayout
    {
        public const string LibPrefix = "base/lib/";

        public static List<string> Abis(IEnumerable<string> entryNames)
        {
            var abis = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string name in entryNames)
            {
                if (!name.StartsWith(LibPrefix, StringComparison.Ordinal))
                    continue;
                int slash = name.IndexOf('/', LibPrefix.Length);
                if (slash > LibPrefix.Length)
                    abis.Add(name.Substring(LibPrefix.Length, slash - LibPrefix.Length));
            }
            return new List<string>(abis);
        }

        public static List<string> NativeLibraries(IEnumerable<string> entryNames, string abi)
        {
            string prefix = LibPrefix + abi + "/";
            var libraries = new List<string>();
            foreach (string name in entryNames)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".so", StringComparison.Ordinal) &&
                    name.IndexOf('/', prefix.Length) < 0)
                    libraries.Add(name);
            }
            libraries.Sort(StringComparer.Ordinal);
            return libraries;
        }
    }
}
