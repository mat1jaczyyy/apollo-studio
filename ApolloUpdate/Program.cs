using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ApolloUpdate {
    class Program {
        public static string GetBaseFolder(string folder) => Path.Combine(
            Directory.GetParent(
                Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)
            ).FullName,
            folder
        );

        static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                string crashDir = GetBaseFolder("Crashes");

                if (!Directory.Exists(crashDir)) Directory.CreateDirectory(crashDir);
                string crashName = Path.Combine(crashDir, $"Crash-{DateTimeOffset.Now.ToUnixTimeSeconds()}");

                using (var memoryStream = new MemoryStream()) {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                        using (Stream log = archive.CreateEntry("exception.log").Open())
                            using (StreamWriter writer = new StreamWriter(log))
                                writer.Write(
                                    $"Operating System: {RuntimeInformation.OSDescription}\n\n" +
                                    e.ExceptionObject.ToString()
                                );

                    File.WriteAllBytes(crashName + ".zip", memoryStream.ToArray());
                }
            };
            
            
        }
    }
}
