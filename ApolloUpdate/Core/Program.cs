using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Logging.Serilog;

using Update.Windows;

namespace Update.Core {
    class Program {
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();

        static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                string crashName = $"{AppDomain.CurrentDomain.BaseDirectory}crash-{DateTimeOffset.Now.ToUnixTimeSeconds()}";

                using (var memoryStream = new MemoryStream()) {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)) {
                        using (Stream log = archive.CreateEntry("exception.log").Open())
                            using (StreamWriter writer = new StreamWriter(log))
                                writer.Write(
                                    $"Operating System: {RuntimeInformation.OSDescription}\n\n" +
                                    e.ExceptionObject.ToString()
                                );
                    }

                    File.WriteAllBytes(crashName + ".zip", memoryStream.ToArray());
                }
            };
            
            BuildAvaloniaApp().Start<MainWindow>();
        }
    }
}
