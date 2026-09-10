#if !LEGACY_AVALONIA
using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Apollo.Core;
using Apollo.Helpers;
using Apollo.Windows;

namespace Apollo.Tests {
    internal static partial class Scenarios {
        sealed class FailedUpdateWindow : UpdateWindow {
            readonly Func<Task<byte[]>> download;
            internal FailedUpdateWindow(Func<Task<byte[]>> download) => this.download = download;
            protected override Task<byte[]> Download() => download == null ? base.Download() : download();
        }

        static async Task ExerciseUpdateFailures() {
            // Seed only the release metadata cache: a missing matching asset must not
            // contact GitHub, crash, or launch the installer.
            var cache = typeof(Github).GetField("release", BindingFlags.NonPublic | BindingFlags.Static);
            var previous = cache.GetValue(null);
            cache.SetValue(null, System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Octokit.Release)));
            try {
                foreach (var test in new (string name, Func<Task<byte[]>> download)[] {
                    ("missing-compatible-asset", null),
                    ("download-failure", () => Task.FromException<byte[]>(new WebException("Test offline"))),
                    ("download-cancelled", () => Task.FromCanceled<byte[]>(new CancellationToken(true))),
                    ("invalid-zip", () => Task.FromResult(new byte[] { 1, 2, 3 }))
                }) {
                    var update = new FailedUpdateWindow(test.download);
                    update.Show();
                    await Settle();
                    var close = update.Get<Button>("CloseButton");
                    Check(close.IsVisible && update.Get<TextBlock>("State").Text.StartsWith("Update failed:")
                        && !Program.LaunchUpdater, "update-" + test.name + "-is-recoverable");
                    Click(update, "Close");
                    Check(!App.Windows.Contains(update), "update-" + test.name + "-can-close");
                }
            } finally { cache.SetValue(null, previous); }

            var unpacked = Path.Combine(output, "legacy-update-fixture");
            Directory.CreateDirectory(Path.Combine(unpacked, "__MACOSX"));
            foreach (var folder in new[] { "Apollo", "Update", "M4L" })
                Directory.CreateDirectory(Path.Combine(unpacked, "osx-arm64", folder));
            Check(UpdateWindow.FindLegacyPayload(unpacked) == Path.Combine(unpacked, "osx-arm64"),
                "legacy-update-ignores-metadata-directory");
            foreach (var folder in new[] { "Apollo", "Update", "M4L" })
                Directory.CreateDirectory(Path.Combine(unpacked, "duplicate", folder));
            bool ambiguous = false;
            try { UpdateWindow.FindLegacyPayload(unpacked); }
            catch (InvalidDataException) { ambiguous = true; }
            Check(ambiguous, "legacy-update-rejects-ambiguous-payloads");
            foreach (var path in new[] { "Apollo/Apollo/file", "../escape", "Apollo/Update/../../escape", "/absolute", "C:/escape", "Apollo/Update/..\\escape" }) {
                using var stream = new MemoryStream();
                using (var writer = new ZipArchive(stream, ZipArchiveMode.Create, true)) writer.CreateEntry(path);
                stream.Position = 0;
                using var reader = new ZipArchive(stream, ZipArchiveMode.Read);
                bool rejected = false;
                try { UpdateWindow.ValidateArchive(reader); }
                catch (InvalidDataException) { rejected = true; }
                Check(rejected == (path != "Apollo/Apollo/file"), "update-archive-path-" + path);
            }
        }
    }
}
#endif