using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

using Avalonia;

using Apollo.Binary;
using Apollo.Elements;
using Apollo.Platform;

// Suppresses readonly suggestion
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier")]

namespace Apollo.Core {
    class Program {
        public static readonly string Version = "Version 1.8.17";

        internal static AppBuilder BuildAvaloniaApp(bool useGpu) {
            var builder = AppBuilder.Configure<App>().UsePlatformDetect();
            #if DEBUG
                builder.WithDeveloperTools();
            #endif
            if (!useGpu) {
                builder.With(new AvaloniaNativePlatformOptions { RenderingMode = new[] { AvaloniaNativeRenderingMode.Software } });
                builder.With(new Win32PlatformOptions { RenderingMode = new[] { Win32RenderingMode.Software } });
                builder.With(new X11PlatformOptions { RenderingMode = new[] { X11RenderingMode.Software } });
            }
            return builder;
        }

        internal static string BundlePath => RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? MacBundle.Find(AppDomain.CurrentDomain.BaseDirectory) : null;

        public static string GetBaseFolder(string folder) => folder == "M4L" && BundlePath != null
            ? MacBundle.ConnectorFolder(BundlePath, UserPath, "/Applications/Apollo Studio/M4L") : Path.Combine(
            Directory.GetParent(
                Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)
            ).FullName,
            folder
        );

        // The scenario entry point supplies a process-local path before Apollo starts.
        public static readonly string UserPath = AppContext.GetData("Apollo.UserPath") as string ?? Path.Combine(Environment.GetEnvironmentVariable(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)? "USERPROFILE" : "HOME"
        ), ".apollostudio"
            #if PRERELEASE
                + "-prerelease"
            #endif
        );

        public static readonly string CrashDir = Path.Combine(UserPath, "Crashes");
        public static readonly string CrashProject = Path.Combine(CrashDir, "crash.approj");

        public static bool HadCrashed = false;

        public static bool LaunchAdmin = false;
        public static bool LaunchUpdater = false;
        internal static string BundleUpdateManifest;
        
        public static Stopwatch TimeSpent = new Stopwatch();
        public static void Log(string text) => Console.WriteLine($"[{TimeSpent.Elapsed.ToString()}] {text}");

        static bool DebugLogging = false;
        public static void DebugLog(string text) {
            if (DebugLogging) Log(text);
        }

        public delegate void ProjectLoadedEventHandler();
        public static event ProjectLoadedEventHandler ProjectLoaded;

        static Project _project;
        public static Project Project {
            get => _project;
            set {
                MIDI.ClearState(force: true);

                _project?.Dispose();

                if ((_project = value) == null) {
                    if (Directory.Exists(Program.CrashDir))
                        File.Delete(Program.CrashProject);
                
                } else {
                    _project.WriteCrashBackup();

                    ProjectLoaded?.Invoke();
                }
                
                ProjectLoaded = null;
            }
        }

        [STAThread]
        static void Main(string[] args) {
            if (args.Contains("--debug")) DebugLogging = true;

            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                if (!Directory.Exists(CrashDir)) Directory.CreateDirectory(CrashDir);
                
                using (MemoryStream memoryStream = new MemoryStream()) {
                    string crashName = Path.Combine(CrashDir, $"Crash-{DateTimeOffset.Now.ToUnixTimeSeconds()}");

                    using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)) {
                        string additional = "";
                        
                        if (Project != null) {
                            try {
                                byte[] project = Encoder.Encode(Project);

                                File.WriteAllBytes(CrashProject, project);

                                if (project != null)
                                    using (Stream writer = archive.CreateEntry("project.approj").Open())
                                        writer.Write(project);

                            } catch (Exception ex) {
                                additional = "\r\n\r\n" + 
                                    "There was an additional exception while attempting to store the project for the crash log:\r\n\r\n" +
                                    ex.ToString();
                            }
                        }

                        using (Stream log = archive.CreateEntry("exception.log").Open())
                            using (StreamWriter writer = new StreamWriter(log)) {
                                writer.Write(
                                    $"Apollo Version: {Version}\r\n" +
                                    $"Operating System: {RuntimeInformation.OSDescription}\r\n\r\n" +
                                    e.ExceptionObject.ToString() +
                                    additional
                                );
                            }
                    }

                    File.WriteAllBytes(crashName + ".zip", memoryStream.ToArray());
                }

                if (TimeSpent.IsRunning) TimeSpent.Stop();

                if (e.IsTerminating && Project != null) {
                    Preferences.Crashed = true;
                    Preferences.CrashPath = Project.FilePath;
                }

                Preferences.Save();
            };

            bool useGpu = !args.Contains("--nogpu");
            if (!useGpu) {
                Program.Log("Running without GPU");
                args = args.Where(i => i != "--nogpu").ToArray();
            }

            App.Args = args;

            TimeSpent.Start();

            BuildAvaloniaApp(useGpu).StartWithClassicDesktopLifetime(null);

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
                    Process.Start(BundleUpdateManifest != null ? MacBundle.UpdateStart(BundleUpdateManifest)
                        : new ProcessStartInfo(Path.Combine(Program.GetBaseFolder("Update"), "ApolloUpdate")) { UseShellExecute = false });
            }
        }
    }
}
