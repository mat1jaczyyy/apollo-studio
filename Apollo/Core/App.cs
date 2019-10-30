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

using Apollo.Elements;
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
        public static void Shutdown() => ((ClassicDesktopStyleApplicationLifetime)instance.ApplicationLifetime).Shutdown();
        
        public static readonly InputModifiers ControlInput = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)? InputModifiers.Windows : InputModifiers.Control;
        public static readonly KeyModifiers ControlKey = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)? KeyModifiers.Meta : KeyModifiers.Control;

        public static bool Dragging = false;

        class DriverVersion: IComparable {
            public int Minor { get; private set; }
            public int Build { get; private set; }

            public DriverVersion(int[] version) {
                Minor = version[0];
                Build = version[1];
            }

            public int CompareTo(object obj) {
                DriverVersion that = (DriverVersion)obj;

                if (this.Minor == that.Minor) {
                    if (this.Build == that.Build) return 0;
                    return (this.Build < that.Build)? -1 : 1;
                }
                
                return (this.Minor < that.Minor)? -1 : 1;
            }

            public static bool operator <(DriverVersion a, DriverVersion b)
                => a.CompareTo(b) == -1;

            public static bool operator >(DriverVersion a, DriverVersion b) 
                => a.CompareTo(b) == 1;
        }

        MessageWindow CreateDriverError(bool IsOldVersion) {
            MessageWindow ret = new MessageWindow(
                (IsOldVersion
                    ? "Apollo Studio requires a newer version of the Novation USB Driver than is\n" +
                      "installed on your computer.\n\n"
                    : "Apollo Studio requires the Novation USB Driver which isn't installed on your\n" +
                      "computer.\n\n"
                ) +
                "Please install at least version 2.15.5 of the driver before launching Apollo\n" +
                "Studio.",
                new string[] {"Download Driver", "OK"}
            );

            ret.Completed.Task.ContinueWith(result => {
                if (result.Result == "Download Driver")
                    URL("https://customer.novationmusic.com/sites/customer/files/downloads/Novation%20USB%20Driver-2.15.5.exe");
            });

            return ret;
        }

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
            if (Program.Project != null) {
                if (Program.Project.Window != null) return;

                foreach (Track track in Program.Project.Tracks)
                    if (track.Window != null) return;
            }

            Type type = sender.GetType();
            
            if (type == typeof(PatternWindow)) return;

            if (type == typeof(ProjectWindow)) {
                Program.Project = null;

                SplashWindow splash = new SplashWindow() {
                    Owner = sender,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                splash.Show();
                splash.Owner = null;

            } else if (type == typeof(TrackWindow)) {
                ProjectWindow.Create(sender);
            
            } else if (type == typeof(SplashWindow)) {
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

        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);

            instance = this;

            if (Preferences.Theme == ThemeType.Dark) Styles.Add(new Dark());
            else if (Preferences.Theme == ThemeType.Light) Styles.Add(new Light());
        }

        public override void OnFrameworkInitializationCompleted() {
            if (!(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)) throw new ApplicationException("Invalid ApplicationLifetime");

            if (Args.Length > 0 && Args[0] == "--update") lifetime.MainWindow = new UpdateWindow();
            else {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // USB Driver check
                    IEnumerable<DriverVersion> a = (from j in Directory.GetDirectories(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\DriverStore\FileRepository\"))
                        where Path.GetFileName(j).StartsWith("nvnusbaudio.inf")
                        select new DriverVersion((
                            from i in File.ReadAllLines(Path.Combine(j, "nvnusbaudio.inf"))
                                where i.StartsWith("DriverVer=")
                                select i
                        ).First().Substring(10).Split(',')[1].Split('.').Where((x, i) => i == 1 || i == 3).Select(x => Convert.ToInt32(x)).ToArray())
                    );

                    if (a.Count() == 0) {
                        lifetime.MainWindow = CreateDriverError(false);

                        base.OnFrameworkInitializationCompleted();
                        return;
                    }

                    if (a.Max() < new DriverVersion(new int[] {15, 5})) {
                        lifetime.MainWindow = CreateDriverError(true);

                        base.OnFrameworkInitializationCompleted();
                        return;
                    }
                }

                if (!AbletonConnector.Connected) {
                    lifetime.MainWindow = new MessageWindow(
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
                
                Courier autosave = new Courier() { Interval = 180000 };
                autosave.Elapsed += async (_, __) => {
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
                };
                autosave.Start();

                lifetime.Exit += (_, __) => {
                    autosave.Dispose();
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
