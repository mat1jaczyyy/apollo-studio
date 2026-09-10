using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Apollo.Platform {
    // Shared by the app and its small, separately staged update process.
    internal static class MacBundle {
        internal const string Name = "Apollo Studio.app";
        internal const string Identifier = "com.mat1jaczyyy.apollostudio";

        internal static string Find(string baseDirectory) {
            var executable = new DirectoryInfo(Path.GetFullPath(baseDirectory));
            return executable.Name == "MacOS" && executable.Parent?.Name == "Contents"
                && executable.Parent.Parent?.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase) == true
                ? executable.Parent.Parent.FullName : null;
        }

        internal static string ConnectorFolder(string bundle, string userPath, string legacy = null) {
            string destination = Path.Combine(userPath, "M4L");
            Directory.CreateDirectory(destination);
            foreach (string directory in new[] { legacy, Path.Combine(bundle, "Contents", "Resources", "M4L") }) {
                if (directory == null || !Directory.Exists(directory)) continue;
                foreach (string source in Directory.GetFiles(directory, "*.amxd")) {
                    string target = Path.Combine(destination, Path.GetFileName(source));
                    // Existing user files win, then legacy connectors, then defaults.
                    if (!File.Exists(target)) File.Copy(source, target);
                }
            }
            return destination;
        }

        internal static void Run(string executable, params string[] arguments) {
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardError = true };
            foreach (string argument in arguments) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new IOException("Could not start " + executable);
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000)) {
                process.Kill();
                throw new IOException(executable + " timed out.");
            }
            if (process.ExitCode != 0) throw new IOException(executable + ": " + error.GetAwaiter().GetResult().Trim());
        }

        internal static void ValidateArchive(Stream archive) {
            using var zip = new ZipArchive(archive, ZipArchiveMode.Read, true);
            foreach (var entry in zip.Entries) {
                string name = entry.FullName;
                if (name.StartsWith('/') || name.Contains('\\') || name.Contains(':') || name.Split('/').Contains("..")
                    || ((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000)
                    throw new InvalidDataException("The update archive contains an unsafe path or symbolic link.");
            }
        }

        internal static void Validate(string bundle, Architecture architecture) {
            var dictionary = XDocument.Load(Path.Combine(bundle, "Contents", "Info.plist")).Root?.Element("dict");
            string Value(string key) => dictionary?.Elements("key").FirstOrDefault(item => item.Value == key)
                ?.ElementsAfterSelf().FirstOrDefault()?.Value;
            if (Value("CFBundleIdentifier") != Identifier || Value("CFBundleExecutable") != "Apollo")
                throw new InvalidDataException("The update is not an Apollo Studio app bundle.");
            using var reader = new BinaryReader(File.OpenRead(Path.Combine(bundle, "Contents", "MacOS", "Apollo")));
            uint cpu = architecture == Architecture.Arm64 ? 0x0100000cU
                : architecture == Architecture.X64 ? 0x01000007U : throw new PlatformNotSupportedException();
            if (reader.BaseStream.Length < 32 || reader.ReadUInt32() != 0xfeedfacf || reader.ReadUInt32() != cpu)
                throw new InvalidDataException("The update is for a different processor architecture.");
        }

        internal static string PrepareUpdate(byte[] archive, string bundle, string userPath) {
            if (bundle.StartsWith("/Volumes/", StringComparison.Ordinal) || bundle.Contains("/AppTranslocation/"))
                throw new IOException("Move Apollo Studio to Applications or your home Applications folder before updating.");
            // Fail while the app is still open if its installation cannot be replaced.
            string probe = Path.Combine(Path.GetDirectoryName(bundle), ".Apollo-write-" + Guid.NewGuid().ToString("N"));
            using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
            using (var stream = new MemoryStream(archive)) ValidateArchive(stream);
            string session = Path.Combine(userPath, "Updates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(session);
            string zip = Path.Combine(session, "update.zip");
            File.WriteAllBytes(zip, archive);
            string unpacked = Path.Combine(session, "Payload");
            Run("/usr/bin/ditto", "-x", "-k", "--rsrc", zip, unpacked);
            string source = Path.Combine(unpacked, Name);
            Validate(source, RuntimeInformation.ProcessArchitecture);
            Run("/usr/bin/codesign", "--verify", "--deep", "--strict", source);
            // Run the current updater outside the installation being replaced.
            Run("/usr/bin/ditto", Path.Combine(bundle, "Contents", "Helpers", "Apollo Updater.app"), Path.Combine(session, "Runner.app"));
            string manifest = Path.Combine(session, "update.xml");
            new XDocument(new XElement("Update",
                new XAttribute("source", source), new XAttribute("destination", bundle),
                new XAttribute("pid", Environment.ProcessId))).Save(manifest);
            return manifest;
        }

        internal static ProcessStartInfo UpdateStart(string manifest) {
            var start = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(manifest), "Runner.app", "Contents", "MacOS", "ApolloUpdate")) {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(manifest)
            };
            start.ArgumentList.Add("--mac-bundle-update");
            start.ArgumentList.Add(manifest);
            return start;
        }

        // candidate must be a verified copy on the destination volume. Retain a backup
        // until the next successful update; a failed rename/relaunch restores it.
        internal static void Replace(string candidate, string destination, Action<string> launch) {
            candidate = Path.GetFullPath(candidate);
            destination = Path.GetFullPath(destination);
            if (candidate == destination || Path.GetDirectoryName(candidate) != Path.GetDirectoryName(destination)
                || !candidate.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
                || !destination.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("App replacement requires separate sibling bundles.");
            if (!Directory.Exists(candidate) || !Directory.Exists(destination))
                throw new DirectoryNotFoundException("The existing app or staged update is missing.");
            string backup = Path.Combine(Path.GetDirectoryName(destination), "." + Path.GetFileName(destination) + ".previous");
            if (Directory.Exists(backup)) Directory.Delete(backup, true);
            Directory.Move(destination, backup);
            try {
                Directory.Move(candidate, destination);
                launch(destination);
            } catch {
                if (Directory.Exists(destination)) Directory.Move(destination, candidate);
                Directory.Move(backup, destination);
                throw;
            }
        }

        internal static void ApplyUpdate(string manifest) {
            var plan = XDocument.Load(manifest).Root ?? throw new InvalidDataException("Missing update plan.");
            string session = Path.GetDirectoryName(Path.GetFullPath(manifest));
            string source = Path.GetFullPath((string)plan.Attribute("source") ?? throw new InvalidDataException());
            string destination = Path.GetFullPath((string)plan.Attribute("destination") ?? throw new InvalidDataException());
            if (source != Path.Combine(session, "Payload", Name)
                || destination.StartsWith(session + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidDataException("The update payload is outside its staging directory.");
            int pid = (int)plan.Attribute("pid");
            Validate(source, RuntimeInformation.ProcessArchitecture);
            Validate(destination, RuntimeInformation.ProcessArchitecture);
            Run("/usr/bin/codesign", "--verify", "--deep", "--strict", source);
            if (pid == Environment.ProcessId) throw new InvalidDataException("Invalid update process.");
            try {
                using var parent = Process.GetProcessById(pid);
                if (!parent.WaitForExit(60000)) throw new IOException("Apollo Studio did not close. Please quit it and try again.");
            } catch (ArgumentException) { /* The app already exited. */ }
            string candidate = Path.Combine(Path.GetDirectoryName(destination), ".Apollo-update-" + Guid.NewGuid().ToString("N") + ".app");
            Run("/usr/bin/ditto", source, candidate);
            Run("/usr/bin/codesign", "--verify", "--deep", "--strict", candidate);
            Replace(candidate, destination, path => Run("/usr/bin/open", "-n", path));
            // The runner remains mapped until this process exits; reclaim the much
            // larger downloaded payload without deleting our running helper.
            try {
                File.Delete(Path.Combine(session, "update.zip"));
                Directory.Delete(Path.Combine(session, "Payload"), true);
            } catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { }
        }
    }
}
