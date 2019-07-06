using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ApolloUpdate {
    class Program {
        static string GetBaseFolder(string folder) => Path.Combine(
            Directory.GetParent(
                Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)
            ).FullName,
            folder
        );

        static string Handle64Path => $"{AppDomain.CurrentDomain.BaseDirectory}handle64.exe";

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
                                    $"CWD: {AppDomain.CurrentDomain.BaseDirectory}\n\n" +
                                    $"Operating System: {RuntimeInformation.OSDescription}\n\n" +
                                    e.ExceptionObject.ToString()
                                );

                    File.WriteAllBytes(crashName + ".zip", memoryStream.ToArray());
                }
            };

            Thread.Sleep(2000);
            
            string apollopath = Program.GetBaseFolder("Apollo");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Process handle64 = Process.Start(new ProcessStartInfo(Handle64Path, "-p ApolloUpdate.exe -nobanner") {
                    RedirectStandardOutput = true
                });
                handle64.WaitForExit();
                
                IEnumerable<string> strings = handle64.StandardOutput.ReadToEnd().Split('\n');

                string pid = strings.FirstOrDefault(i => i.Contains("pid"));
                string handle = strings.FirstOrDefault(i => i.Contains(apollopath));

                if (handle != null)
                    Process.Start(new ProcessStartInfo(Handle64Path, $"-p {pid.Trim().Split(' ')[2]} -c {handle.Trim().Split(':')[0]} -y -nobanner")).WaitForExit();
            }

            Thread.Sleep(1000);

            if (Directory.Exists(apollopath))
                while (true)
                    try {
                        Directory.Delete(apollopath, true);
                        break;
                    } catch {
                        Thread.Sleep(1000);
                    }
            
            string temppath = Program.GetBaseFolder("Temp");
            Directory.Move(temppath, apollopath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start(new ProcessStartInfo(
                    "chmod", $"+x \"{Path.Combine(apollopath, "Apollo")}\""
                )).WaitForExit();

            Process.Start(Path.Combine(apollopath, "Apollo" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)? ".exe" : "")));
        }
    }
}
