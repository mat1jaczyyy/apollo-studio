using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using Apollo.Platform;

namespace Apollo.PackagingTests {
    static partial class Program {
        static int checks;
        static void Check(bool value, string name) {
            if (!value) throw new Exception(name);
            checks++;
            Console.WriteLine("PASS " + name);
        }
        static void Reject(Action action, string name) {
            bool rejected = false;
            try { action(); } catch (Exception e) when (e is IOException || e is InvalidDataException || e is InvalidOperationException) { rejected = true; }
            Check(rejected, name);
        }
        static void Write(string file, string text) {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, text);
        }
        static void Bundle(string path, Architecture architecture) {
            Write(Path.Combine(path, "Contents", "Info.plist"), new XDocument(new XElement("plist", new XElement("dict",
                new XElement("key", "CFBundleIdentifier"), new XElement("string", MacBundle.Identifier),
                new XElement("key", "CFBundleExecutable"), new XElement("string", "Apollo")))).ToString());
            string host = Path.Combine(path, "Contents", "MacOS", "Apollo");
            Directory.CreateDirectory(Path.GetDirectoryName(host));
            using var writer = new BinaryWriter(File.Create(host));
            writer.Write(0xfeedfacfU);
            writer.Write(architecture == Architecture.Arm64 ? 0x0100000cU : 0x01000007U);
            writer.Write(new byte[24]);
        }
        static void Archive(string entry, bool link = false) {
            using var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
                var item = zip.CreateEntry(entry);
                if (link) item.ExternalAttributes = unchecked((int)0xa1ff0000);
            }
            stream.Position = 0;
            MacBundle.ValidateArchive(stream);
        }
        static int Main(string[] args) {
            if (args.Length == 6 && args[0] == "--native-mac-failures")
                return NativeMacFailures(args[1], args[2], args[3], args[4], args[5]);
            if (args.Length == 2 && args[0] == "--verify-publish") {
                using var file = File.OpenRead(args[1]);
                using var pe = new PEReader(file);
                var metadata = pe.GetMetadataReader();
                var method = metadata.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(
                    pe.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress & 0x00ffffff));
                var owner = metadata.GetTypeDefinition(method.GetDeclaringType());
                var entry = metadata.GetString(owner.Namespace) + "." + metadata.GetString(owner.Name) + "." + metadata.GetString(method.Name);
                Check(entry == "Apollo.Core.Program.Main", "production app entry point");
                foreach (var handle in metadata.TypeDefinitions) {
                    var definition = metadata.GetTypeDefinition(handle);
                    if (metadata.GetString(definition.Namespace).StartsWith("Apollo.Tests"))
                        throw new Exception("Scenario code was included in the production app.");
                }
                Check(true, "production app excludes scenario types");
                return 0;
            }
            // Stage a real, signed local update. The caller runs the staged helper
            // after this process and the desktop test app have exited.
            if (args.Length == 5 && args[0] == "--prepare-mac-update") {
                if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
                var manifest = MacBundle.PrepareUpdate(File.ReadAllBytes(args[2]), Path.GetFullPath(args[1]), Path.GetFullPath(args[3]));
                File.WriteAllText(args[4], manifest);
                Console.WriteLine("PASS native extraction, signature validation and helper staging: " + manifest);
                return 0;
            }
            if (args.Length != 0) throw new ArgumentException("Use --verify-publish <Apollo.dll>, --prepare-mac-update <installed.app> <archive.zip> <profile> <manifest-output>, or no arguments.");
            string root = Path.Combine(Path.GetTempPath(), "apollo-packaging-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                string bundle = Path.Combine(root, "An app with spaces.app");
                Bundle(bundle, Architecture.Arm64);
                Check(MacBundle.Find(Path.Combine(bundle, "Contents", "MacOS") + Path.DirectorySeparatorChar) == bundle, "discover bundle independently of working directory");
                Check(MacBundle.Find(Path.Combine(root, "legacy", "Apollo")) == null, "legacy executable stays outside bundle flow");
                MacBundle.Validate(bundle, Architecture.Arm64);
                Check(true, "matching bundle identity and architecture");
                Reject(() => MacBundle.Validate(bundle, Architecture.X64), "wrong architecture rejected");
                Write(Path.Combine(bundle, "Contents", "Info.plist"), "<plist><dict/></plist>");
                Reject(() => MacBundle.Validate(bundle, Architecture.Arm64), "foreign bundle identity rejected");
                Bundle(bundle, Architecture.Arm64);
                Write(Path.Combine(bundle, "Contents", "MacOS", "Apollo"), "broken");
                Reject(() => MacBundle.Validate(bundle, Architecture.Arm64), "truncated native host rejected");
                Bundle(bundle, Architecture.Arm64);
                string resources = Path.Combine(bundle, "Contents", "Resources", "M4L");
                Write(Path.Combine(resources, "Existing.amxd"), "shipped");
                Write(Path.Combine(resources, "New.amxd"), "new");
                string profile = Path.Combine(root, "profile");
                Write(Path.Combine(profile, "M4L", "Existing.amxd"), "user edited");
                string legacy = Path.Combine(root, "legacy", "M4L");
                Write(Path.Combine(resources, "Legacy.amxd"), "default");
                Write(Path.Combine(legacy, "Legacy.amxd"), "custom legacy");
                string connectors = MacBundle.ConnectorFolder(bundle, profile, legacy);
                Check(File.ReadAllText(Path.Combine(connectors, "Legacy.amxd")) == "custom legacy", "legacy connector imported before defaults");
                Check(File.ReadAllText(Path.Combine(connectors, "Existing.amxd")) == "user edited", "user connector preserved");
                Check(File.ReadAllText(Path.Combine(connectors, "New.amxd")) == "new", "new connector copied outside bundle");
                Check(File.ReadAllText(Path.Combine(resources, "Existing.amxd")) == "shipped", "bundle resources remain unchanged");
                Archive("Apollo Studio.app/Contents/MacOS/Apollo");
                Check(true, "normal bundle ZIP accepted");
                foreach (string name in new[] { "../outside", "/outside", "dir/../outside", "dir\\outside", "C:/outside" })
                    Reject(() => Archive(name), "unsafe ZIP path rejected: " + name);
                Reject(() => Archive("link", true), "ZIP symbolic link rejected");
                string installed = Path.Combine(root, "Installed.app"), candidate = Path.Combine(root, "Candidate.app");
                Write(Path.Combine(installed, "marker"), "old");
                Write(Path.Combine(candidate, "marker"), "new");
                bool launched = false;
                MacBundle.Replace(candidate, installed, path => launched = path == installed);
                Check(launched && File.ReadAllText(Path.Combine(installed, "marker")) == "new", "complete bundle installed and relaunched");
                Check(File.ReadAllText(Path.Combine(root, ".Installed.app.previous", "marker")) == "old", "previous bundle retained for recovery");
                Write(Path.Combine(candidate, "marker"), "bad update");
                Reject(() => MacBundle.Replace(candidate, installed, _ => throw new IOException("open failed")), "relaunch failure reported");
                Check(File.ReadAllText(Path.Combine(installed, "marker")) == "new", "relaunch failure rolls back existing app");
                Check(File.ReadAllText(Path.Combine(candidate, "marker")) == "bad update", "failed update remains available for diagnosis");
                Reject(() => MacBundle.Replace(installed, installed, _ => { }), "same source and destination rejected");
                Reject(() => MacBundle.Replace(Path.Combine(root, "Missing.app"), installed, _ => { }), "missing candidate preserves installation");
                Check(File.ReadAllText(Path.Combine(installed, "marker")) == "new", "failed preparation leaves installation intact");
                string manifest = Path.Combine(root, "space and ' quote", "update.xml");
                var start = MacBundle.UpdateStart(manifest);
                Check(!start.UseShellExecute && start.ArgumentList.Count == 2 && start.ArgumentList[1] == manifest, "updater arguments avoid shell quoting and Terminal");
                Check(start.FileName.EndsWith(Path.Combine("Runner.app", "Contents", "MacOS", "ApolloUpdate")), "updater runs outside replaced bundle");
                string invalidPlan = Path.Combine(root, "bad-update.xml");
                new XDocument(new XElement("Update", new XAttribute("source", bundle), new XAttribute("destination", installed), new XAttribute("pid", Environment.ProcessId))).Save(invalidPlan);
                Reject(() => MacBundle.ApplyUpdate(invalidPlan), "manifest cannot select a payload outside its session");
                Console.WriteLine($"{checks} packaging/update checks passed.");
                return 0;
            } finally {
                Directory.Delete(root, true);
            }
        }
    }
}
