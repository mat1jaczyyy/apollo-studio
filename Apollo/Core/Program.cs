using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

using Avalonia;

using Apollo.Binary;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Windows;

// Suppresses readonly suggestion
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier")]

namespace Apollo.Core {
    class Program {
        public static readonly string Version = "Version 1.0.3";

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect();

        public static string GetBaseFolder(string folder) => Path.Combine(
            Directory.GetParent(
                Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)
            ).FullName,
            folder
        );

        public static readonly string UserPath = Path.Combine(Environment.GetEnvironmentVariable(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)? "USERPROFILE" : "HOME"
        ), ".apollostudio");

        public static readonly string CrashDir = Path.Combine(Program.UserPath, "Crashes");

        public static bool LaunchAdmin = false;
        public static bool LaunchUpdater = false;
        
        public static Stopwatch TimeSpent = new Stopwatch();
        public static void Log(string text) {
            if (text == "") Console.Write(text);
            else Console.WriteLine($"[{TimeSpent.Elapsed.ToString()}] {text}");
        }

        public delegate void ProjectLoadedEventHandler();
        public static event ProjectLoadedEventHandler ProjectLoaded;

        static Project _project;
        public static Project Project {
            get => _project;
            set {
                _project?.Dispose();
                _project = value;

                ProjectLoaded?.Invoke();
                ProjectLoaded = null;
            }
        }

        [STAThread]
        static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                if (!Directory.Exists(CrashDir)) Directory.CreateDirectory(CrashDir);
                string crashName = Path.Combine(CrashDir, $"Crash-{DateTimeOffset.Now.ToUnixTimeSeconds()}");

                using (MemoryStream memoryStream = new MemoryStream()) {
                    using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)) {
                        if (Project != null) {
                            byte[] project = null;

                            File.WriteAllBytes(crashName + ".approj", project = Encoder.Encode(Project).ToArray());

                            if (project != null)
                                using (Stream writer = archive.CreateEntry("project.approj").Open())
                                    writer.Write(project);
                        }

                        using (Stream log = archive.CreateEntry("exception.log").Open())
                            using (StreamWriter writer = new StreamWriter(log))
                                writer.Write(
                                    $"Apollo Version: {Version}\n" +
                                    $"Operating System: {RuntimeInformation.OSDescription}\n\n" +
                                    e.ExceptionObject.ToString()
                                );
                    }

                    File.WriteAllBytes(crashName + ".zip", memoryStream.ToArray());
                }

                if (TimeSpent.IsRunning) TimeSpent.Stop();

                if (e.IsTerminating && Project != null) {
                    Preferences.CrashName = crashName;
                    Preferences.CrashPath = Project.FilePath;
                }

                Preferences.Save();
            };

            TimeSpent.Start();
            
            App.Args = args;
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(null);

            TimeSpent.Stop();

            if (LaunchAdmin && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(
                    $"{AppDomain.CurrentDomain.BaseDirectory}elevate.exe",
                    $"\"{Path.Combine(Program.GetBaseFolder("Apollo"), "Apollo.exe")}\" --update"
                );

            else if (LaunchUpdater) {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Process.Start(
                        $"{AppDomain.CurrentDomain.BaseDirectory}elevate.exe",
                        $"\"{Path.Combine(Program.GetBaseFolder("Update"), "ApolloUpdate.exe")}\""
                    );
                
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start(Path.Combine(Program.GetBaseFolder("Update"), "ApolloUpdate"));
            }
        }
    }
}
