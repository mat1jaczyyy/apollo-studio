using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;

using Apollo.Elements;
using Apollo.Elements.Launchpads;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Themes;
using Apollo.Windows;

namespace Apollo.Core {
    public class App: Application {
        static App instance;

        public static Window MainWindow => ((ClassicDesktopStyleApplicationLifetime)instance.ApplicationLifetime).MainWindow;
        public static IReadOnlyList<Window> Windows => ((ClassicDesktopStyleApplicationLifetime)instance.ApplicationLifetime).Windows;
        public static void Shutdown() {
            IsQuitting = true;
            ((ClassicDesktopStyleApplicationLifetime)instance.ApplicationLifetime).Shutdown();
        }

        internal static bool IsQuitting { get; private set; }
        bool quitPending;

        void HandleShutdownRequested(object sender, ShutdownRequestedEventArgs e) {
            if (IsQuitting || Windows.Count == 0) return;

            // Native Quit is synchronous. Cancel this request while the user decides,
            // then finish shutdown after the prompt and any save picker have closed.
            e.Cancel = true;
            var message = Windows.OfType<MessageWindow>().LastOrDefault();
            if (quitPending || message != null) {
                message?.Activate();
                return;
            }

            quitPending = true;
            Dispatcher.UIThread.Post(async () => {
                try {
                    var owner = Windows.FirstOrDefault(window => window.IsActive)
                        ?? Windows.FirstOrDefault(window => window.IsVisible);
                    // Commit text edits before inspecting the project's saved state.
                    owner?.Focus();
                    var project = Program.Project;
                    if (project != null && !await project.ConfirmClose(owner)) return;

                    // MessageWindow completes its task before its button handler closes
                    // the window. Avoid re-entering that close handler during shutdown.
                    await System.Threading.Tasks.Task.Yield();
                    Shutdown();
                } finally {
                    quitPending = false;
                }
            });
        }

        public static Avalonia.Input.Platform.IClipboard Clipboard =>
            (Windows.FirstOrDefault(window => window.IsActive) ?? Windows.FirstOrDefault(window => window.IsVisible))?.Clipboard
            ?? throw new InvalidOperationException("No window is available for the clipboard.");
        
        public static readonly KeyModifiers ControlKey = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)? KeyModifiers.Meta : KeyModifiers.Control;

        public static bool Dragging = false;

        public static bool WindowKey(Window sender, KeyEventArgs e) {
            if (e.KeyModifiers == ControlKey) {
                if (e.Key == Key.W) sender.Close();
                else if (e.Key == Key.OemComma) PreferencesWindow.Create(sender);
                else if (e.Key == Key.M) sender.WindowState = WindowState.Minimized;
                else return false;

            } else return false;

            return true;
        }

        public static void WindowClosed(Window sender) {
            if (IsQuitting) return;

            if (Program.Project != null) {
                if (Program.Project.Window != null) return;

                foreach (Track track in Program.Project.Tracks)
                    if (track.Window != null) return;
            }
            
            if (sender is PatternWindow) return;

            if (sender is ProjectWindow) {
                Program.Project = null;

                SplashWindow.Create(sender);

            } else if (sender is TrackWindow) {
                ProjectWindow.Create(sender);
            
            } else if (sender is SplashWindow) {
                Preferences.Window?.Close();
                
                foreach (Launchpad lp in MIDI.Devices)
                    lp.Window?.Close();
            }
        }

        public static string[] Args;

        public static void URL(string url) => Process.Start(new ProcessStartInfo() {
            FileName = url,
            UseShellExecute = true
        });

        public static object FindResource(string key) {
            if (Current.TryGetResource(key, Current.ActualThemeVariant, out var value)) return value;
            throw new KeyNotFoundException($"Application resource '{key}' was not found.");
        }

        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);

            instance = this;

            RequestedThemeVariant = Preferences.Theme == ThemeType.Light ? ThemeVariant.Light : ThemeVariant.Dark;
            if (Preferences.Theme == ThemeType.Dark) Styles.Add(new Dark());
            else if (Preferences.Theme == ThemeType.Light) Styles.Add(new Light());
        }

        public override void OnFrameworkInitializationCompleted() {
            if (!(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)) throw new ApplicationException("Invalid ApplicationLifetime");

            // Project and track windows replace one another; the original splash is not
            // the lifetime owner. Keep running until the final window has closed.
            lifetime.ShutdownMode = ShutdownMode.OnLastWindowClose;
            lifetime.ShutdownRequested += HandleShutdownRequested;

            if (Args.Length > 0 && Args[0] == "--update") lifetime.MainWindow = new UpdateWindow();
            else {
                if (!DriverChecker.Run(out MessageWindow driverError)) {
                    lifetime.MainWindow = driverError;

                    base.OnFrameworkInitializationCompleted();
                    return;
                }

                if (!AbletonConnector.Connected) {
                    if (Args.Length > 0) {
                        AbletonConnector.NewInstanceFile(App.Args[0]);
                        Environment.Exit(0);

                    } else lifetime.MainWindow = new MessageWindow(
                        $"Another instance of Apollo Studio is currently running.\n\n" +
                        "Please close other instances of Apollo Studio before launching Apollo Studio."
                    );

                    base.OnFrameworkInitializationCompleted();
                    return;
                }

                Program.HadCrashed = Preferences.Crashed;
                Preferences.Crashed = true;

                if (Preferences.DiscordPresence) Discord.Set(true);

                MIDI.Start();

                foreach (int i in Preferences.VirtualLaunchpads) {
                    LaunchpadWindow.Create(MIDI.ConnectVirtual(i), null);
                    MIDI.Update();
                }
                
                Courier autosave = new Courier(180000, async _ => {
                    if (Preferences.Autosave && Program.Project != null && File.Exists(Program.Project.FilePath) && !Program.Project.Undo.Saved) {
                        try {
                            string dir = Path.Combine(Path.GetDirectoryName(Program.Project.FilePath), $"{Program.Project.FileName} Backups");
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                            await Program.Project.WriteFile(
                                null,
                                Path.Join(dir, $"{Program.Project.FileName} Autosave {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.approj"),
                                false
                            );
                        } catch {}
                    }

                    Preferences.Save();
                });

                lifetime.Exit += (_, __) => {
                    autosave.Dispose();
                    Program.Project = null;
                    MIDI.Stop();
                    Discord.Set(false);
                    AbletonConnector.Dispose();
                    Preferences.Crashed = Program.HadCrashed;

                    Preferences.Save();
                };

                lifetime.MainWindow = new SplashWindow();
                base.OnFrameworkInitializationCompleted();
            }
        }
    }
}
