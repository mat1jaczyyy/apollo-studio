using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using Apollo.Platform;

namespace Apollo.PackagingTests {
    static partial class Program {
        // Uses signed local payloads and actual macOS tools, but never launches or
        // replaces a user installation. All writable fixtures stay under output.
        static int NativeMacFailures(string bundle, string archive, string otherArchitecture, string dmg, string output) {
            if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
            output = Path.GetFullPath(output);
            if (Directory.Exists(output)) throw new IOException("Use a fresh output directory.");
            Directory.CreateDirectory(output);
            string parent = Path.Combine(output, "Installation with spaces"), profile = Path.Combine(output, "profile");
            string installed = Path.Combine(parent, MacBundle.Name);
            MacBundle.Run("/usr/bin/ditto", Path.GetFullPath(bundle), installed);
            string host = Path.Combine(installed, "Contents", "MacOS", "Apollo");
            byte[] before = SHA256.HashData(File.ReadAllBytes(host));
            byte[] payload = File.ReadAllBytes(archive);

            void Intact(string name) {
                MacBundle.Run("/usr/bin/codesign", "--verify", "--deep", "--strict", installed);
                Check(before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(host))), name);
            }
            void Failure(Action action, string name) {
                Exception rejected = null;
                try { action(); }
                catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is InvalidOperationException) { rejected = error; }
                Check(rejected != null, name);
                Console.WriteLine("  " + rejected.GetType().Name + ": " + rejected.Message);
                Intact(name + " preserves signed installation");
            }

            var mode = File.GetUnixFileMode(parent);
            try {
                File.SetUnixFileMode(parent, UnixFileMode.UserRead | UnixFileMode.UserExecute);
                Failure(() => MacBundle.PrepareUpdate(payload, installed, profile), "read-only installation rejected before staging");
                Check(!Directory.Exists(profile), "read-only failure does not create staging");
            } finally { File.SetUnixFileMode(parent, mode); }

            string mount = "/Volumes/Apollo-validation-" + Guid.NewGuid().ToString("N");
            MacBundle.Run("/usr/bin/hdiutil", "attach", "-readonly", "-nobrowse", "-mountpoint", mount, Path.GetFullPath(dmg));
            try {
                Check(File.Exists(Path.Combine(mount, MacBundle.Name, "Contents", "Info.plist")), "real DMG mounted for update rejection");
                Failure(() => MacBundle.PrepareUpdate(payload, Path.Combine(mount, MacBundle.Name), profile), "mounted-DMG installation rejected");
            } finally { MacBundle.Run("/usr/bin/hdiutil", "detach", mount); }

            Failure(() => MacBundle.PrepareUpdate(payload, Path.Combine(output, "AppTranslocation", MacBundle.Name), profile),
                "App Translocation path guard rejects before staging (synthetic path)");
            Failure(() => MacBundle.PrepareUpdate(File.ReadAllBytes(otherArchitecture), installed, profile), "signed opposite-architecture payload rejected");
            // Failed payloads remain for diagnostics in production; reclaim this
            // known disposable session before another full extraction.
            if (Directory.Exists(profile)) Directory.Delete(profile, true);
            Failure(() => MacBundle.PrepareUpdate(new byte[] { 1, 2, 3 }, installed, profile), "corrupt ZIP rejected before staging");

            string manifest = MacBundle.PrepareUpdate(payload, installed, profile);
            Check(File.Exists(manifest) && File.Exists(MacBundle.UpdateStart(manifest).FileName), "signed payload and real helper staged");
            Intact("stopping before helper handoff leaves current installation usable");
            var plan = XDocument.Load(manifest);
            string source = (string)plan.Root.Attribute("source");
            File.AppendAllText(Path.Combine(source, "Contents", "Resources", "Apollo.icns"), "tampered fixture");
            Failure(() => MacBundle.ApplyUpdate(manifest), "tampered staged signature rejected before replacement");
            Check(!Directory.Exists(Path.Combine(parent, "." + MacBundle.Name + ".previous")), "failed staging never moved existing installation");
            Directory.Delete(profile, true);

            // Exercise a real rename failure with signed sibling bundles. A file
            // occupying the backup pathname prevents the first rename.
            string candidate = Path.Combine(parent, "Candidate.app"), backup = Path.Combine(parent, "." + MacBundle.Name + ".previous");
            MacBundle.Run("/usr/bin/ditto", installed, candidate);
            File.WriteAllText(backup, "replacement blocker");
            bool launched = false;
            Failure(() => MacBundle.Replace(candidate, installed, _ => launched = true), "blocked backup rename preserves installation");
            Check(!launched && Directory.Exists(candidate), "rename failure does not launch or consume candidate");
            File.Delete(backup);
            Failure(() => MacBundle.Replace(candidate, installed, _ => throw new IOException("injected relaunch failure")),
                "relaunch callback failure restores signed previous bundle");
            Check(Directory.Exists(candidate), "failed candidate retained for diagnosis");
            Console.WriteLine($"{checks} native macOS failure/recovery checks passed.");
            return 0;
        }
    }
}
