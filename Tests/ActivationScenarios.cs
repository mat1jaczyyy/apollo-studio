#if !LEGACY_AVALONIA
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Windows;

namespace Apollo.Tests {
    internal static partial class Scenarios {
        static async Task Until(Func<bool> condition) {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!condition()) {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("Activation did not reach the expected state.");
                await Task.Delay(20);
            }
        }

        static void ActivateFiles(params string[] paths) {
            var files = paths.Select(path => {
                var item = DispatchProxy.Create<IStorageItem, ActivationFile>();
                ((ActivationFile)(object)item).Path = new Uri(path);
                return item;
            }).ToArray();
            ((App)Application.Current).HandleActivation(null, new FileActivatedEventArgs(files));
        }

        static async Task ActivationDone() {
            // HandleActivation posts to the UI dispatcher before starting the queue.
            await Task.Yield();
            await App.Documents.Completion.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Yield();
        }

        static async Task<MessageWindow> ActivationPrompt() {
            await Until(() => App.Windows.OfType<MessageWindow>().Any());
            var prompt = App.Windows.OfType<MessageWindow>().Single();
            await Until(() => prompt.IsLoaded);
            Check(prompt.Get<TextBlock>("Message").Text.Contains("unsaved changes"), "activation-prompts-before-replacing-unsaved-project");
            return prompt;
        }

        static async Task RunActivation() {
            await Until(() => App.Windows.OfType<SplashWindow>().Any(window => window.IsLoaded));
            string first = Path.Combine(output, "First project ž.approj"), second = Path.Combine(output, "Second project.approj");
            foreach (var (path, bpm) in new[] { (first, 121), (second, 142) }) {
                using var unused = File.Create(path);
                var fixture = new Project(bpm: bpm, author: "Finder activation fixture");
                unused.Write(Apollo.Binary.Encoder.Encode(fixture));
                fixture.Dispose();
            }

            var startup = new DocumentOpen();
            startup.Enqueue(new[] { first, first });
            Check(Program.Project == null, "activation-before-ready-remains-queued");
            Directory.CreateDirectory(Program.CrashDir);
            File.Copy(second, Program.CrashProject, true);
            var priorRecovery = File.ReadAllBytes(Program.CrashProject);
            Program.HadCrashed = true;
            startup.Start();
            await Task.Yield();
            Check(Program.Project == null && !startup.Completion.IsCompleted
                && priorRecovery.SequenceEqual(File.ReadAllBytes(Program.CrashProject)),
                "startup-activation-waits-for-existing-crash-recovery-choice");
            Program.HadCrashed = false; // The user has resolved Restore/Ignore.
            await startup.Completion;
            Check(Program.Project?.BPM == 121 && Program.Project.Undo.Saved
                && App.Windows.Count == 1 && !App.Windows.OfType<SplashWindow>().Any(), "startup-activation-opens-one-project-with-unicode-path");
            using (var backup = File.OpenRead(Program.CrashProject)) {
                var recovered = await Apollo.Binary.Decoder.Decode<Project>(backup, PurposeType.Passive);
                Check(recovered.BPM == 121 && recovered.Author == Program.Project.Author,
                    "activation-writes-opened-project-to-recovery-snapshot");
                recovered.Dispose();
            }
            // File paths belong to preferences, not the project file format.
            Preferences.CrashPath = Program.Project.FilePath;
            var original = Program.Project;
            original.Undo.AddAndExecute(new Project.AuthorChangedUndoEntry(original.Author, "Unsaved original"));
            ActivateFiles(first, first);
            await ActivationDone();
            Check(ReferenceEquals(original, Program.Project) && !original.Undo.Saved
                && !App.Windows.OfType<MessageWindow>().Any(), "repeated-activation-of-current-file-preserves-unsaved-edits");

            ActivateFiles(second, second, first);
            var prompt = await ActivationPrompt();
            ActivateFiles(second);
            await Task.Yield();
            Check(App.Windows.OfType<MessageWindow>().Count() == 1 && !original.IsDisposing, "queued-activation-reuses-one-decision");
            RequestQuit();
            Check(!App.IsQuitting && App.Windows.Contains(prompt), "quit-during-activation-defers-to-existing-decision");
            Click(prompt, "Cancel");
            await ActivationDone();
            Check(ReferenceEquals(original, Program.Project) && original.Window.IsVisible
                && !App.Windows.OfType<MessageWindow>().Any(), "cancel-activation-keeps-workspace-and-clears-batch");

            TrackWindow.Create(original[0], original.Window);
            var pattern = (Apollo.Devices.Pattern)Device.Create(typeof(Apollo.Devices.Pattern), PurposeType.Active, original[0].Chain);
            original[0].Chain.Add(pattern);
            PatternWindow.Create(pattern, original[0].Window);
            UndoWindow.Create(original.Window);
            var oldEditors = App.Windows.ToArray();
            await Until(() => oldEditors.All(window => window.IsLoaded));
            ActivateFiles(second);
            Click(await ActivationPrompt(), "Yes");
            await ActivationDone();
            Check(original.IsDisposing && Program.Project.BPM == 142 && Program.Project.Undo.Saved,
                "save-before-activation-disposes-old-project-and-opens-next");
            Check(oldEditors.All(window => !App.Windows.Contains(window)) && App.Windows.Count == 1,
                "activation-closes-old-project-track-pattern-and-undo-windows");
            using (var savedFile = File.OpenRead(first)) {
                var saved = await Apollo.Binary.Decoder.Decode<Project>(savedFile, PurposeType.Passive);
                Check(saved.Author == "Unsaved original", "activation-saves-latest-data-to-original-file");
                saved.Dispose();
            }

            original = Program.Project;
            string crashPath = Preferences.CrashPath = original.FilePath;
            var recovery = File.ReadAllBytes(Program.CrashProject);
            foreach (string invalid in new[] { Path.Combine(output, "Invalid.approj"), Path.Combine(output, "Missing.approj") }) {
                if (invalid.Contains("Invalid")) File.WriteAllText(invalid, "invalid project");
                ActivateFiles(invalid);
                await Until(() => App.Windows.OfType<MessageWindow>().Any(window => window.IsLoaded));
                var error = App.Windows.OfType<MessageWindow>().Single();
                Check(error.Get<TextBlock>("Message").Text.Contains("error occurred while reading"), "activation-reports-unreadable-file-" + Path.GetFileName(invalid));
                Click(error, "OK");
                await ActivationDone();
                Check(ReferenceEquals(Program.Project, original) && !original.IsDisposing
                    && original.Window.IsVisible && Preferences.CrashPath == crashPath
                    && recovery.SequenceEqual(File.ReadAllBytes(Program.CrashProject)), "failed-activation-preserves-project-and-recovery-" + Path.GetFileName(invalid));
            }

            // A native Save As picker is asynchronous. Activation must wait for its
            // completion before disposing the window that owns it.
            var providerField = typeof(TopLevel).GetField("_storageProvider", BindingFlags.NonPublic | BindingFlags.Instance);
            var provider = providerField.GetValue(original.Window);
            var picker = (CancelSavePicker)(object)DispatchProxy.Create<IStorageProvider, CancelSavePicker>();
            providerField.SetValue(original.Window, picker);
            var save = original.Save(original.Window, true);
            ActivateFiles(first);
            await Task.Yield();
            Check(FileDialogs.IsOpen && ReferenceEquals(original, Program.Project) && !original.IsDisposing,
                "activation-waits-for-existing-save-picker");
            picker.Result.SetResult(null);
            await save;
            providerField.SetValue(original.Window, provider);
            await ActivationDone();
            Check(Program.Project.BPM == 121 && original.IsDisposing, "activation-resumes-after-picker-cancellation");

            original = Program.Project;
            original.FilePath = "";
            original.Undo.AddAndExecute(new Project.AuthorChangedUndoEntry(original.Author, "Unsaved new project"));
            provider = providerField.GetValue(original.Window);
            picker = (CancelSavePicker)(object)DispatchProxy.Create<IStorageProvider, CancelSavePicker>();
            providerField.SetValue(original.Window, picker);
            ActivateFiles(second);
            Click(await ActivationPrompt(), "Yes");
            await Until(() => picker.Calls == 1);
            Check(ReferenceEquals(original, Program.Project) && !original.IsDisposing, "activation-save-picker-keeps-original-alive");
            picker.Result.SetResult(null);
            await ActivationDone();
            providerField.SetValue(original.Window, provider);
            Check(ReferenceEquals(original, Program.Project) && !original.Undo.Saved,
                "cancelling-activation-save-picker-cancels-open");

            TrackWindow.Create(original[0], original.Window);
            var oldTrack = original[0].Window;
            original.Window.Close();
            await Until(() => original.Window == null);
            Check(oldTrack.IsVisible, "activation-fixture-has-track-only-workspace");
            ActivateFiles(second);
            Click(await ActivationPrompt(), "No");
            await ActivationDone();
            Check(original.IsDisposing && !App.Windows.Contains(oldTrack) && App.Windows.Count == 1
                && Program.Project.BPM == 142, "activation-replaces-track-only-workspace-without-exiting");

            var focused = Program.Project;
            focused.Window.Activate();
            var author = focused.Window.Get<TextBox>("Author");
            author.Focus();
            author.Text = "Focused edit before activation";
            Check(focused.Undo.Saved && author.IsFocused, "activation-fixture-has-uncommitted-text-edit");
            ActivateFiles(first);
            Click(await ActivationPrompt(), "Cancel");
            await ActivationDone();
            Check(ReferenceEquals(focused, Program.Project) && focused.Author == "Focused edit before activation"
                && !focused.Undo.Saved, "activation-commits-focused-text-before-unsaved-decision");
            ActivateFiles(first, second);
            Click(await ActivationPrompt(), "No");
            await ActivationDone();
            Check(Program.Project.BPM == 142 && !ReferenceEquals(focused, Program.Project)
                && App.Windows.Count == 1, "multiple-file-activation-drains-in-order");

            Program.Project.Window.WindowState = Avalonia.Controls.WindowState.Minimized;
            ((App)Application.Current).HandleActivation(null, new ActivatedEventArgs(ActivationKind.Reopen));
            await Until(() => Program.Project.Window.WindowState == Avalonia.Controls.WindowState.Normal);
            Check(Program.Project.Window.IsVisible, "reopen-restores-minimized-project");
            ActivateFiles(Path.Combine(output, "ignored.txt"));
            await ActivationDone();
            Check(Program.Project.BPM == 142 && !App.Windows.OfType<MessageWindow>().Any(), "activation-ignores-unsupported-file-types");
            Program.Project.Window.Close();
            await Until(() => Program.Project == null && App.Windows.OfType<SplashWindow>().Any());
            Check(App.Windows.Count == 1, "activation-project-close-returns-to-splash");
        }

        public class ActivationFile : DispatchProxy {
            public Uri Path;
            protected override object Invoke(MethodInfo method, object[] args) {
                if (method.Name == "get_Path") return Path;
                throw new NotSupportedException(method.Name);
            }
        }
    }
}
#endif
