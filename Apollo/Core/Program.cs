using System;
using System.Diagnostics;
using System.Threading;

using Avalonia;
using Avalonia.Logging.Serilog;

using RtMidi.Core;

using Apollo.Elements;
using Apollo.Windows;

// Suppresses readonly suggestion
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier")]

namespace Apollo.Core {
    class Program {
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();
        
        public static bool log = true;
        static Stopwatch logTimer = new Stopwatch();

        public static void Log(string text) {
            if (log)
                Console.WriteLine($"[{logTimer.Elapsed.ToString()}] {text}");
        }

        public static Project Project;

        static ManualResetEvent close = new ManualResetEvent(false);

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
