using System;
using System.Diagnostics;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;

using RtMidi.Core;

using Apollo.Binary;
using Apollo.Elements;
using Apollo.Windows;

// Suppresses readonly suggestion
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier")]

namespace Apollo.Core {
    class Program {
        public static readonly string Version = "Beta Build 6";

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();
        
        public static bool log = false;
        static Stopwatch logTimer = new Stopwatch();

        public static void Log(string text) {
            if (log)
                Console.WriteLine($"[{logTimer.Elapsed.ToString()}] {text}");
        }

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

        static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                string FilePath = $"{AppDomain.CurrentDomain.BaseDirectory}crashdump-{DateTimeOffset.Now.ToUnixTimeSeconds()}-";

                File.WriteAllText(FilePath + "exception.log", e.ExceptionObject.ToString());

                if (Project != null)
                    File.WriteAllBytes(FilePath + "project.approj", Encoder.Encode(Project).ToArray());
            };

            if (Preferences.DiscordPresence) Discord.Set(true);

            logTimer.Start();

            foreach (string arg in args) {
                /*if (arg.Equals("--log")) {
                    log = true;
                }*/
            }
            
            foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                Log($"MIDI API: {api}");

            MIDI.Start();

            Log("MIDI Ready");

            BuildAvaloniaApp().Start<SplashWindow>();
        }
    }
}
