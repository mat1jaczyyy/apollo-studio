#if !LEGACY_AVALONIA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Windows;

namespace Apollo.Tests {
    internal static partial class Scenarios {
        static Project quitProject;
        static string quitSavePath, expectedSavedAuthor;
        static byte[] originalSavedBytes;
        static bool expectUnchangedFile;
        static readonly List<string> replacementsDuringQuit = new();
        static IDisposable quitWindowSubscription;

        static void RequestQuit() => ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).TryShutdown();

        static async Task<MessageWindow> QuitPrompt() {
            RequestQuit();
            await Settle();
            var prompt = App.Windows.OfType<MessageWindow>().Single();
            Check(prompt.Get<TextBlock>("Message").Text.Contains("unsaved changes"), "quit-prompts-for-unsaved-project");
            return prompt;
        }

        static async Task CancelQuit(Window[] editors, bool closePrompt = false) {
            var prompt = await QuitPrompt();
            RequestQuit();
            RequestQuit();
            await Settle();
            Check(App.Windows.OfType<MessageWindow>().Count() == 1 && App.Windows.Contains(prompt), "repeated-quit-reuses-prompt");
            Check(editors.All(App.Windows.Contains) && !quitProject.IsDisposing, "quit-keeps-editors-alive-until-decision");
            if (closePrompt) prompt.Close();
            else Click(prompt, "Cancel");
            await Settle();
            Check(editors.ToHashSet().SetEquals(App.Windows) && editors.All(w => w.IsVisible)
                && ReferenceEquals(Program.Project, quitProject) && !quitProject.Undo.Saved,
                closePrompt ? "closing-prompt-cancels-quit" : "cancel-quit-restores-same-workspace");
        }

        static async Task RunQuit(string name) {
            await Settle();
            var splash = App.Windows.OfType<SplashWindow>().Single();
            if (name != "splash") {
                Click(splash, "New Project");
                await Settle();
                quitProject = Program.Project;
                TrackWindow.Create(quitProject[0], quitProject.Window);
                var pattern = (Pattern)Device.Create(typeof(Pattern), PurposeType.Active, quitProject[0].Chain);
                quitProject[0].Chain.Add(pattern);
                PatternWindow.Create(pattern, quitProject[0].Window);
                UndoWindow.Create(quitProject.Window);
                quitSavePath = Path.Combine(output, "quit.approj");
                Check(await quitProject.WriteFile(quitProject.Window, quitSavePath), "quit-fixture-saved");
                originalSavedBytes = File.ReadAllBytes(quitSavePath);
            }
            Window owner = quitProject?.Window ?? (Window)splash;
            PreferencesWindow.Create(owner);
            LaunchpadWindow.Create(MIDI.ConnectVirtual(), owner);
            MIDI.Update();
            await Settle();
            Check(App.Windows.Count >= (quitProject == null ? 3 : 6), "quit-fixture-includes-auxiliary-windows");

            var originalWindows = App.Windows.ToHashSet();
            quitWindowSubscription = Window.WindowOpenedEvent.AddClassHandler(typeof(Window), (sender, _) => {
                if (sender is Window window && !(window is MessageWindow) && !originalWindows.Contains(window)) replacementsDuringQuit.Add(window.GetType().Name);
            });
            if (name == "splash" || name == "saved") {
                expectUnchangedFile = name == "saved";
                finished = true;
                RequestQuit();
                return;
            }

            if (name == "save") {
                quitProject.Window.Activate();
                var author = quitProject.Window.Get<TextBox>("Author");
                author.Focus();
                author.Text = "Typed immediately before Quit";
                Check(author.IsFocused && quitProject.Undo.Saved, "quit-fixture-has-uncommitted-text-edit");
            } else {
                quitProject.Undo.AddAndExecute(new Project.AuthorChangedUndoEntry(quitProject.Author, "Changed before Quit"));
            }
            if (name == "track-only") {
                quitProject.Window.Close();
                await Settle();
                Check(quitProject.Window == null && quitProject[0].Window != null, "quit-track-only-workspace");
            }
            var editors = App.Windows.ToArray();
            if (name == "discard" || name == "track-only") {
                await CancelQuit(editors);
                await CancelQuit(editors, closePrompt: true);
            }
            if (name == "pending-close") {
                _ = quitProject.AskClose(quitProject.Window);
                await Settle();
                var existing = App.Windows.OfType<MessageWindow>().Single();
                RequestQuit();
                await Settle();
                Check(App.Windows.OfType<MessageWindow>().Single() == existing, "quit-respects-existing-close-prompt");
                Click(existing, "Cancel");
                await Settle();
                Check(editors.ToHashSet().SetEquals(App.Windows), "existing-close-cancel-preserves-workspace");
            }
            if (name == "picker-cancel") {
                quitProject.FilePath = "";
                // Test-only platform seam: real Save flow, deterministic picker cancellation.
                var providerField = typeof(TopLevel).GetField("_storageProvider", BindingFlags.NonPublic | BindingFlags.Instance);
                var oldProviders = editors.ToDictionary(w => w, w => providerField.GetValue(w));
                var picker = (CancelSavePicker)(object)DispatchProxy.Create<IStorageProvider, CancelSavePicker>();
                foreach (var editor in editors) providerField.SetValue(editor, picker);
                var prompt = await QuitPrompt();
                Click(prompt, "Yes");
                await Settle();
                Check(picker.Calls == 1 && !quitProject.IsDisposing, "quit-waits-for-save-picker");
                RequestQuit();
                await Settle();
                Check(picker.Calls == 1 && !App.Windows.OfType<MessageWindow>().Any(), "quit-during-save-does-not-open-second-prompt");
                picker.Result.SetResult(null);
                await Settle();
                foreach (var editor in editors) providerField.SetValue(editor, oldProviders[editor]);
                Check(editors.ToHashSet().SetEquals(App.Windows) && editors.All(w => w.IsVisible)
                    && !quitProject.Undo.Saved, "cancel-save-picker-cancels-quit");
            }
            if (name == "save-error") {
                // A locked destination exercises IOException, without filesystem permissions changes.
                using (var locked = new FileStream(quitSavePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                    var prompt = await QuitPrompt();
                    Click(prompt, "Yes");
                    await Settle();
                    var error = App.Windows.OfType<MessageWindow>().Single();
                    Check(error.Get<TextBlock>("Message").Text.Contains("error occurred while writing"), "quit-reports-save-error");
                    Click(error, "OK");
                    await Settle();
                    Check(editors.ToHashSet().SetEquals(App.Windows) && !quitProject.Undo.Saved
                        && !quitProject.IsDisposing, "save-error-aborts-quit-with-workspace-intact");
                }
            }

            var finalPrompt = await QuitPrompt();
            Check(editors.All(App.Windows.Contains), "final-quit-still-has-all-editors");
            bool save = name == "save" || name == "save-error";
            if (save) expectedSavedAuthor = quitProject.Author;
            else expectUnchangedFile = true;
            finished = true;
            Click(finalPrompt, save ? "Yes" : "No");
        }

        static void CheckQuitExit() {
            quitWindowSubscription?.Dispose();
            Check(App.Windows.Count == 0 && replacementsDuringQuit.Count == 0, "quit-closes-all-windows-without-replacements");
            Check(Program.Project == null && (quitProject == null || quitProject.IsDisposing), "quit-disposes-project");
            Check(!File.Exists(Program.CrashProject), "quit-removes-project-crash-backup");
            if (expectUnchangedFile) Check(originalSavedBytes.SequenceEqual(File.ReadAllBytes(quitSavePath)), "quit-leaves-saved-file-unchanged");
            if (expectedSavedAuthor != null) {
                using var file = File.OpenRead(quitSavePath);
                var saved = Apollo.Binary.Decoder.Decode<Project>(file, PurposeType.Passive).GetAwaiter().GetResult();
                Check(saved.Author == expectedSavedAuthor, "quit-saves-latest-project-data-before-exit");
                saved.Dispose();
            }
        }

        // Runtime proxy keeps the fake in test code; Avalonia seals the interface to
        // source implementations. Only the public save-picker API is intercepted.
        public class CancelSavePicker : DispatchProxy {
            public int Calls;
            public TaskCompletionSource<IStorageFile> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            protected override object Invoke(MethodInfo method, object[] args) {
                if (method.Name == "SaveFilePickerAsync") { Calls++; return Result.Task; }
                throw new NotSupportedException(method.Name);
            }
        }
    }
}
#endif