using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

using api.Devices;
using Newtonsoft.Json;

namespace api {
    class Program {
        public static bool log = true;
        public static ManualResetEvent close = new ManualResetEvent(false);

        public static Stopwatch logTimer = new Stopwatch();

        public static void Log(string text) {
            if (log)
                Console.WriteLine($"[{logTimer.Elapsed.ToString()}] {text}");
        }

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
            Set.New();

            Communication.Server.Start();

            Log("ready");

            close.WaitOne();
        }
    }
}
