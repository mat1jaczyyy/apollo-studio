using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Helpers {
    public static class DriverChecker {
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

        static MessageWindow CreateDriverError(bool IsOldVersion) {
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
                    App.URL("https://customer.novationmusic.com/sites/customer/files/downloads/Novation%20USB%20Driver-2.15.5.exe");
            });

            return ret;
        }

        public static bool Run(out MessageWindow error) {
            error = null;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return true;

            IEnumerable<DriverVersion> a = (from j in Directory.GetDirectories(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\DriverStore\FileRepository\"))
                where Path.GetFileName(j).StartsWith("nvnusbaudio.inf")
                select new DriverVersion((
                    from i in File.ReadAllLines(Path.Combine(j, "nvnusbaudio.inf"))
                        where i.StartsWith("DriverVer=")
                        select i
                ).First().Substring(10).Split(',')[1].Split('.').Where((x, i) => i == 1 || i == 3).Select(x => Convert.ToInt32(x)).ToArray())
            );

            if (a.Count() == 0) {
                error = CreateDriverError(false);
                return false;
            }

            if (a.Max() < new DriverVersion(new int[] {15, 5})) { // 2.15.5 required
                error = CreateDriverError(true);
                return false;
            }

            return true;
        }
    }
}