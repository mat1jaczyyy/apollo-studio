using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Decoder = Apollo.Binary.Decoder;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Windows;

namespace Apollo.Helpers {
    // OS activation can arrive before the first window opens, or during another
    // close/save decision. Keep those requests on the UI thread and in order.
    internal sealed class DocumentOpen {
        readonly Queue<string> requests = new();
        readonly HashSet<string> pending = new(StringComparer.Ordinal);
        bool ready;
        bool draining;
        internal bool IsOpening { get; private set; }
        internal Task Completion { get; private set; } = Task.CompletedTask;

        static string CanonicalPath(string path) {
            var full = Path.GetFullPath(path);
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? full.Normalize(NormalizationForm.FormC) : full;
        }

        internal void Enqueue(IEnumerable<string> paths) {
            Dispatcher.UIThread.VerifyAccess();
            if (App.IsQuitting) return;
            foreach (var path in paths) {
                if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".approj", StringComparison.OrdinalIgnoreCase)) continue;
                string full;
                try { full = CanonicalPath(path); }
                catch (Exception error) when (error is ArgumentException || error is NotSupportedException || error is PathTooLongException) { continue; }
                if (pending.Add(full)) requests.Enqueue(full);
            }
            Drain();
        }

        internal void Start() { ready = true; Drain(); }

        void Drain() {
            if (!ready || draining || requests.Count == 0) return;
            draining = true;
            Completion = DrainAsync();
        }

        async Task DrainAsync() {
            try {
                while (requests.Count > 0 && !App.IsQuitting) {
                    var message = App.Windows.OfType<MessageWindow>().LastOrDefault();
                    if (message != null) {
                        message.Activate();
                        await message.Completed.Task;
                        await Task.Yield(); // Its button still needs to close the window.
                        continue;
                    }
                    // Keep the startup recovery offer intact. Opening a project
                    // writes a new crash snapshot, so wait for Restore/Ignore.
                    if (App.IsQuitPending || FileDialogs.IsOpen
                        || (Program.Project == null && Program.HadCrashed && File.Exists(Program.CrashProject))) {
                        await Task.Delay(25);
                        continue;
                    }
                    string path = requests.Peek();
                    IsOpening = true;
                    bool proceed;
                    try { proceed = await OpenAsync(path); }
                    finally { IsOpening = false; }
                    requests.Dequeue();
                    pending.Remove(path);
                    // Cancel means keep this workspace; don't immediately prompt
                    // for another document from the same queued batch.
                    if (!proceed) break;
                }
            } finally {
                requests.Clear();
                pending.Clear();
                draining = false;
            }
        }

        internal static void BringForward() {
            var window = App.Windows.OfType<MessageWindow>().LastOrDefault()
                ?? (Window)Program.Project?.Window
                ?? App.Windows.FirstOrDefault(window => window.IsActive)
                ?? App.Windows.FirstOrDefault();
            if (window == null) return;
            if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
            window.Show();
            window.Activate();
        }

        async Task<bool> OpenAsync(string path) {
            var previous = Program.Project;
            var owner = App.Windows.FirstOrDefault(window => window.IsActive)
                ?? App.Windows.FirstOrDefault(window => window.IsVisible) ?? App.MainWindow;
            if (!string.IsNullOrEmpty(previous?.FilePath) && CanonicalPath(previous.FilePath) == path) {
                if (previous.Window == null) ProjectWindow.Create(owner);
                BringForward();
                return true;
            }

            owner?.Focus(); // Commit a text edit before asking about saved state.
            if (previous != null && !await previous.ConfirmClose(owner)) return false;
            await Task.Yield();
            if (App.IsQuitting) return false;

            Project loaded;
            string crashPath = Preferences.CrashPath;
            try {
                using var file = File.OpenRead(path);
                loaded = await Decoder.Decode<Project>(file, PurposeType.Active);
            } catch {
                await MessageWindow.CreateReadError(owner);
                return true;
            } finally {
                // Project construction sets CrashPath; a rejected/invalid open
                // must not change recovery metadata for the current workspace.
                Preferences.CrashPath = crashPath;
            }

            var lifetime = (IClassicDesktopStyleApplicationLifetime)App.Current.ApplicationLifetime;
            var mode = lifetime.ShutdownMode;
            lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            App.IsReplacingProject = true;
            try {
                previous?.Window?.CloseForReplacement();
                loaded.FilePath = path;
                loaded.Undo.SavePosition();
                Program.Project = loaded; // Disposes old editors and writes the new recovery snapshot.
                Preferences.RecentsAdd(path);
                ProjectWindow.Create(null);
                foreach (var splash in App.Windows.OfType<SplashWindow>().ToArray()) splash.Close();
                BringForward();
            } finally {
                App.IsReplacingProject = false;
                lifetime.ShutdownMode = mode;
            }
            return true;
        }
    }
}
