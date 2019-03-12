using System;
using System.Diagnostics;
using System.Threading;

using Avalonia;
using Avalonia.Logging.Serilog;

using RtMidi.Core;

namespace Apollo.Core {
    class Program {
        public static bool log = true;

        public static ManualResetEvent close = new ManualResetEvent(false);

        public static Stopwatch logTimer = new Stopwatch();

        public static void Log(string text) {
            if (log)
                Console.WriteLine($"[{logTimer.Elapsed.ToString()}] {text}");
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();

        static void Main(string[] args) {
            logTimer.Start();

            foreach (string arg in args) {
                /*if (arg.Equals("--log")) {
                    log = true;
                }*/
            }
            
            foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                Log($"MIDI API: {api}");

            MIDI.Rescan();

            Log("ready");

            BuildAvaloniaApp().Start<Splash>();

            Log("loaded");

            close.WaitOne();
        }
    }
}
