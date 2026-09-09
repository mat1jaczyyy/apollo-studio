using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Tests {
    // Compiled into a separate test build only, including the unmodified master sources.
    // Invoke the real entry point so startup, desktop lifetime and exit cleanup all run.
    internal static partial class Scenarios {
        sealed class ScenarioResult {
            public string scenario { get; set; }
            public bool passed { get; set; }
            public WindowState[] windows { get; set; }
            public bool? crashFlag { get; set; }
            public int? remainingWindows { get; set; }
        }
        sealed class WindowState {
            public string type { get; set; }
            public bool visible { get; set; }
        }
        static string output;
        static readonly List<object> results = new List<object>();
        static bool finished;
        static Exception failure;

        [STAThread]
        static int Main(string[] args) {
            if (args.Length != 1) throw new ArgumentException("Pass an empty test output directory.");
            output = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(output);
            var profile = Path.Combine(output, "profile");
            Directory.CreateDirectory(profile);
            // Process-local redirection before any Apollo static initialization.
            Environment.SetEnvironmentVariable("USERPROFILE", profile);
            Preferences.Theme = Environment.GetEnvironmentVariable("APOLLO_TEST_THEME") == "Light"
                ? Apollo.Enums.ThemeType.Light : Apollo.Enums.ThemeType.Dark;
            Preferences.DiscordPresence = false;
            Preferences.CheckForUpdates = false;
            Preferences.Backup = false;
            Preferences.Autosave = false;
            Preferences.AlwaysOnTop = false;

            Console.WriteLine("Scenario runner starting with profile " + profile);
            _ = Task.Run(async () => {
                while (Application.Current?.ApplicationLifetime == null) await Task.Delay(20);
                Dispatcher.UIThread.Post(async () => {
                    try {
                        MIDI.Stop();
                        await Run();
                        finished = true;
                        // The last splash close must end the real desktop loop on its own.
                        App.Windows.OfType<SplashWindow>().Single().Close();
                    } catch (Exception ex) {
                        failure = ex;
                        Console.Error.WriteLine(ex);
                        App.Shutdown();
                    }
                });
            });

            try {
                #if HEADLESS_AVALONIA
                StartHeadless();
                #else
                typeof(Program).GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { Environment.GetEnvironmentVariable("APOLLO_TEST_SOFTWARE") == "1" ? new[] { "--nogpu" } : Array.Empty<string>() });
                #endif
            } catch (Exception ex) {
                failure = ex;
                Console.Error.WriteLine(ex);
            }
            results.Add(new ScenarioResult { scenario = "process-exit", passed = finished && failure == null && !Preferences.Crashed && App.Windows.Count == 0,
                crashFlag = Preferences.Crashed, remainingWindows = App.Windows.Count });
            File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results,
                new JsonSerializerOptions { WriteIndented = true }));
            return finished && failure == null && !Preferences.Crashed && App.Windows.Count == 0 ? 0 : 1;
        }

        static void Check(bool condition, string description) {
            results.Add(new ScenarioResult { scenario = description, passed = condition,
                windows = App.Windows.Select(w => new WindowState { type = w.GetType().Name, visible = w.IsVisible }).ToArray() });
            Console.WriteLine((condition ? "PASS " : "FAIL ") + description);
            if (!condition) throw new InvalidOperationException(description);
        }

        static async Task Settle() => await Task.Delay(150);

        static void Click(Window window, string text) {
            var button = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, text));
            #if HEADLESS_AVALONIA
            ClickHeadless(window, button);
            #else
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            #endif
        }

        static async Task Capture(Window window, string name) {
            await Settle();
            using var bitmap = new RenderTargetBitmap(
                new PixelSize((int)Math.Ceiling(window.Bounds.Width), (int)Math.Ceiling(window.Bounds.Height)),
                new Vector(96, 96));
            bitmap.Render(window);
            bitmap.Save(Path.Combine(output, name + ".png"));
        }

        static async Task Run() {
            await Settle();
            Check(App.Windows.Count == 1 && App.Windows[0] is SplashWindow, "startup-splash");
            var splash = App.Windows.OfType<SplashWindow>().Single();
            await Capture(splash, "01-splash");
            splash.Get<TabControl>("TabControl").SelectedIndex = 1;
            await Capture(splash, "01b-learn");
            splash.Get<TabControl>("TabControl").SelectedIndex = 0;
            PreferencesWindow.Create(splash);
            var preferences = Preferences.Window;
            PreferencesWindow.Create(splash);
            Check(ReferenceEquals(preferences, Preferences.Window) && App.Windows.Count == 2, "preferences-singleton");
            await Capture(preferences, "02-preferences");
            preferences.Close();
            Check(Preferences.Window == null && App.Windows.Count == 1, "preferences-close");

            Click(splash, "New Project");
            await Settle();
            Check(Program.Project != null && Program.Project.Window != null && !App.Windows.OfType<SplashWindow>().Any(), "new-project-replaces-splash");
            var project = Program.Project;
            var projectWindow = project.Window;
            await Capture(projectWindow, "03-project");
            Check(Apollo.Helpers.Importer.FramesFromImage(System.IO.Path.Combine(output, "03-project.png"), out var frames) && frames.Count == 1 && frames[0].Screen.Length == 101, "image-import");
            TrackWindow.Create(project[0], projectWindow);
            var trackWindow = project[0].Window;
            TrackWindow.Create(project[0], projectWindow);
            Check(ReferenceEquals(trackWindow, project[0].Window), "track-singleton");
            await Capture(trackWindow, "04-track");
            await ExerciseEditors(project, trackWindow);
            projectWindow.Close();
            await Settle();
            Check(project.Window == null && ReferenceEquals(Program.Project, project) && trackWindow.IsVisible, "project-close-keeps-open-track-and-project-data");
            trackWindow.Close();
            await Settle();
            Check(project.Window != null && project[0].Window == null && App.Windows.Count == 1, "last-track-close-reopens-project");

            projectWindow = project.Window;
            projectWindow.Close();
            await Settle();
            var message = App.Windows.OfType<MessageWindow>().Single();
            Check(!projectWindow.IsVisible, "unsaved-prompt-hides-project");
            await Capture(message, "05-unsaved-prompt");
            Click(message, "Cancel");
            await Settle();
            Check(ReferenceEquals(project.Window, projectWindow) && projectWindow.IsVisible && ReferenceEquals(Program.Project, project), "cancel-close-restores-project");

            var path = Path.Combine(output, "roundtrip.approj");
            project.Author = "Migration scenario";
            project.BPM = 173;
            Check(await project.WriteFile(projectWindow, path), "save-project");
            projectWindow.Close();
            await Settle();
            Check(Program.Project == null && App.Windows.Count == 1 && App.Windows[0] is SplashWindow, "saved-project-close-reopens-splash");
            splash = App.Windows.OfType<SplashWindow>().Single();
            splash.ReadFile(path);
            await Settle();
            Check(Program.Project?.BPM == 173 && Program.Project.Author == "Migration scenario" && Program.Project.Count == 1 && Program.Project.Undo.Saved, "saved-project-roundtrip");
            Program.Project.Window.Close();
            await Settle();
            splash = App.Windows.OfType<SplashWindow>().Single();
            PreferencesWindow.Create(splash);
            var launchpad = MIDI.ConnectVirtual();
            LaunchpadWindow.Create(launchpad, splash);
            MIDI.Update();
            await Capture(launchpad.Window, "08-virtual-launchpad");
            Check(App.Windows.Count == 3, "preferences-and-virtual-launchpad-open-at-exit");
        }
    }
}
