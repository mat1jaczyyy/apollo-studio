using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;

using Apollo.Binary;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Windows;

// Suppresses readonly suggestion
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier")]

namespace Apollo.Core {
    class Program {
        public static readonly string Version = "Beta Build 18";

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();
        
        static Stopwatch logTimer = new Stopwatch();
        public static void Log(string text) => Console.WriteLine($"[{logTimer.Elapsed.ToString()}] {text}");

        public delegate void ProjectLoadedEventHandler();
        public static event ProjectLoadedEventHandler ProjectLoaded;

        private static Project _project;
        public static Project Project {
            get => _project;
            set {
                _project?.Dispose();
                _project = value;

                ProjectLoaded?.Invoke();
                ProjectLoaded = null;
            }
        }

        public static void WindowClose(Window sender) {
            if (Project != null) {
                if (Project.Window != null) return;

                foreach (Track track in Project.Tracks)
                    if (track.Window != null) return;
            }

            Type type = sender.GetType();
            
            if (type == typeof(PatternWindow)) return;

            if (type == typeof(ProjectWindow)) {
                Project.Dispose();
                Project = null;

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

        static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                string FilePath = $"{AppDomain.CurrentDomain.BaseDirectory}crashdump-{DateTimeOffset.Now.ToUnixTimeSeconds()}-";

                File.WriteAllText(FilePath + "exception.log", e.ExceptionObject.ToString());

                if (Project != null)
                    File.WriteAllBytes(FilePath + "project.approj", Encoder.Encode(Project).ToArray());
            };

            logTimer.Start();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // USB Driver check
                IEnumerable<int> a = (from j in Directory.GetDirectories(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\DriverStore\FileRepository\"))
                    where Path.GetFileName(j).StartsWith("nvnusbaudio.inf")
                    select Convert.ToInt32((
                        from i in File.ReadAllLines(Path.Combine(j, "nvnusbaudio.inf"))
                            where i.StartsWith("DriverVer=")
                            select i
                    ).First().Substring(10).Split(',')[1].Split('.')[1])
                );

                if (a.Count() == 0) {
                    BuildAvaloniaApp().Start((app, _) => app.Run(new MessageWindow(
                        $"Apollo Studio requires the Novation USB Driver which isn't installed on your\n" +
                        "computer.\n\n" +
                        "Please install at least version 2.7 of the driver before launching Apollo Studio."
                    )), args);
                    return;
                }

                if (a.Max() < 7) {
                    BuildAvaloniaApp().Start((app, _) => app.Run(new MessageWindow(
                        $"Apollo Studio requires a newer version of the Novation USB Driver than is\n" +
                        "installed on your computer.\n\n" +
                        "Please install at least version 2.7 of the driver before launching Apollo Studio."
                    )), args);
                    return;
                }
            }

            if (!AbletonConnector.Connected) {
                BuildAvaloniaApp().Start((app, _) => app.Run(new MessageWindow(
                    $"Another instance of Apollo Studio is currently running.\n\n" +
                    "Please close other instances of Apollo Studio before launching Apollo Studio."
                )), args);
                return;
            }

            Args = args;

            if (Preferences.DiscordPresence) Discord.Set(true);

            MIDI.Start();
            
            Courier autosave = new Courier() { Interval = 180000 };
            autosave.Elapsed += async (_, __) => {
                if (Preferences.Autosave && Program.Project != null && File.Exists(Program.Project.FilePath) && !Program.Project.Undo.Saved) {
                    try {
                        string dir = Path.Combine(Path.GetDirectoryName(Program.Project.FilePath), $"{Program.Project.FileName} Backups");
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                        await Program.Project.WriteFile(
                            Application.Current.MainWindow,
                            Path.Join(dir, $"{Program.Project.FileName} Autosave {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.approj"),
                            false, false
                        );
                    } catch {}
                }
            };
            autosave.Start();

            BuildAvaloniaApp().Start<SplashWindow>();

            autosave.Dispose();
            MIDI.Stop();
            Discord.Set(false);
            AbletonConnector.Dispose();
            logTimer.Stop();
        }
    }
}
