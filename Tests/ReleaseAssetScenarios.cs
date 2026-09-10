#if !LEGACY_AVALONIA
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Apollo.Helpers;

namespace Apollo.Tests {
    internal static partial class Scenarios {
        sealed class ReleaseAssetResult {
            public string scenario { get; set; }
            public bool passed { get; set; }
            public string expected { get; set; }
            public string actual { get; set; }
        }

        static int RunReleaseAssetChecks(string directory) {
            Directory.CreateDirectory(directory);
            var assets = new[] { "Apollo-Mac-arm64.zip", "Apollo-Mac.zip", "Apollo-Win.zip", "Apollo-Mac.pkg" };
            var tests = new[] {
                ("intel-selects-intel", assets, OSPlatform.OSX, Architecture.X64, "Apollo-Mac.zip"),
                ("arm-selects-arm", assets, OSPlatform.OSX, Architecture.Arm64, "Apollo-Mac-arm64.zip"),
                ("windows-selects-windows", assets, OSPlatform.Windows, Architecture.X64, "Apollo-Win.zip"),
                ("linux-has-no-updater", assets, OSPlatform.Linux, Architecture.X64, (string)null),
                ("arm-does-not-fall-back-to-intel", new[] { "Apollo-Mac.zip" }, OSPlatform.OSX, Architecture.Arm64, (string)null),
                ("intel-does-not-select-arm", new[] { "Apollo-Mac-arm64.zip" }, OSPlatform.OSX, Architecture.X64, (string)null),
                ("versioned-legacy-intel-name", new[] { "Apollo-1.8.17-Mac.zip" }, OSPlatform.OSX, Architecture.X64, "Apollo-1.8.17-Mac.zip"),
                ("versioned-arm-name", new[] { "Apollo-1.8.17-Mac-arm64.zip" }, OSPlatform.OSX, Architecture.Arm64, "Apollo-1.8.17-Mac-arm64.zip"),
                ("signature-is-not-an-update", new[] { "Apollo-Mac.zip.asc", "Apollo-Mac.zip.exe" }, OSPlatform.OSX, Architecture.X64, (string)null),
                ("unsupported-cpu-has-no-update", assets, OSPlatform.OSX, Architecture.Arm, (string)null),
                ("case-insensitive-extension", new[] { "Apollo-Mac-arm64.ZIP" }, OSPlatform.OSX, Architecture.Arm64, "Apollo-Mac-arm64.ZIP")
            };
            var checks = new List<ReleaseAssetResult>();
            bool passed = true;
            foreach (var (name, names, platform, architecture, expected) in tests) {
                var actual = Github.DownloadAssetName(names, platform, architecture);
                checks.Add(new ReleaseAssetResult { scenario = name, passed = actual == expected, expected = expected, actual = actual });
                Console.WriteLine((actual == expected ? "PASS " : "FAIL ") + name);
                passed &= actual == expected;
            }
            foreach (var (name, names, architecture, expected) in new[] {
                ("bundle-intel", new[] { "Apollo-Mac.zip", "Apollo-Mac-app.zip" }, Architecture.X64, "Apollo-Mac-app.zip"),
                ("bundle-arm", new[] { "Apollo-Mac-arm64.zip", "Apollo-Mac-arm64-app.zip" }, Architecture.Arm64, "Apollo-Mac-arm64-app.zip"),
                ("bundle-no-legacy-fallback", new[] { "Apollo-Mac.zip" }, Architecture.X64, (string)null),
                ("bundle-no-other-architecture", new[] { "Apollo-Mac-app.zip" }, Architecture.Arm64, (string)null),
                ("bundle-versioned-name", new[] { "Apollo-1.8.17-Mac-arm64-app.zip" }, Architecture.Arm64, "Apollo-1.8.17-Mac-arm64-app.zip")
            }) {
                var actual = Github.DownloadAssetName(names, OSPlatform.OSX, architecture, true);
                checks.Add(new ReleaseAssetResult { scenario = name, passed = actual == expected, expected = expected, actual = actual });
                Console.WriteLine((actual == expected ? "PASS " : "FAIL ") + name);
                passed &= actual == expected;
            }
            File.WriteAllText(Path.Combine(directory, "results.json"), JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true }));
            return passed ? 0 : 1;
        }
    }
}
#endif
