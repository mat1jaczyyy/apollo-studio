using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Helpers {
    public static class DriverChecker {
        static readonly string[] Drivers = new [] { "novationusbmidi.inf", "nvnusbaudio.inf" };

        class DriverVersion: IComparable {
            public int[] Version;

            public DriverVersion(int[] version) {
                Version = version;
            }

            public int CompareTo(object obj) {
                DriverVersion that = (DriverVersion)obj;
                
                for (int i = 0; i < Math.Max(Version.Length, that.Version.Length); i++) {
                    if (i >= Version.Length) return -1;
                    if (i >= that.Version.Length) return 1;

                    if (Version[i] < that.Version[i]) return -1;
                    if (Version[i] > that.Version[i]) return 1;
                }

                return 0;
            }

            public static bool operator <(DriverVersion a, DriverVersion b)
                => a.CompareTo(b) == -1;

            public static bool operator >(DriverVersion a, DriverVersion b) 
                => a.CompareTo(b) == 1;
        }

        static MessageWindow CreateDriverError(bool IsOldVersion) {
            MessageWindow ret = new MessageWindow(
                (IsOldVersion
                    ? "Apollo Studio requires a newer version of the Novation USB Driver than is\n" +
                      "installed on your computer.\n\n"
                    : "Apollo Studio requires the Novation USB Driver which isn't installed on your\n" +
                      "computer.\n\n"
                ) +
                "Please install at least version 2.22.0.10 of the driver before using Apollo Studio.",
                new string[] {"Download Driver", "OK"},
                result => {
                    if (result == "Download Driver")
                        App.URL("https://github.com/mat1jaczyyy/apollo-studio/raw/master/Publish/novationusbmidi.exe");
                }
            );

            return ret;
        }

        static List<DriverVersion> GetDrivers() {
            string[] directories = Directory.GetDirectories(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\DriverStore\FileRepository\"));
            List<DriverVersion> ret = new List<DriverVersion>();
            
            foreach (var driver in Drivers) {
                foreach (var directory in directories.Where(i => Path.GetFileName(i).StartsWith(driver))) {
                    ret.Add(new DriverVersion(
                        File.ReadAllLines(Path.Combine(directory, driver))
                            .Where(i => i.StartsWith("DriverVer="))
                            .First().Substring(10).Split(',')[1].Split('.')
                            .Select(x => Convert.ToInt32(x))
                            .ToArray()
                    ));
                }
            }

            return ret;
        }

        public static bool Run(out MessageWindow error) {
            error = null;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return true;

            List<DriverVersion> drivers = GetDrivers();

            if (drivers.Count == 0) {
                error = CreateDriverError(false);
                return false;
            }

            if (drivers.Max() < new DriverVersion(new int[] {2, 22, 0, 10})) { // 2.22.0.10 required
                error = CreateDriverError(true);
                return false;
            }

            return true;
        }
    }
}
