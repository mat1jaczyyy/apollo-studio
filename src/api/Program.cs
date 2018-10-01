using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

using api.Devices;
using Newtonsoft.Json;

namespace api {
    class Program {
        public static bool log = false;
        public static ManualResetEvent close = new ManualResetEvent(false);

        public static Stopwatch logTimer = new Stopwatch();

        public static void Log(string text) {
            Console.WriteLine($"[{logTimer.Elapsed.ToString()}] {text}");
        }

        static void Main(string[] args) {
            logTimer.Start();

            foreach (string arg in args)
                if (arg.Equals("--log"))
                    log = true;
            
            if (log)
                foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                    Log($"MIDI API: {api}");

            MIDI.Rescan();
            Set.New();
            
            /* Go Higher */
            Console.ReadLine();

            Set.BPM = 160;
            MIDI.Devices[0].InputFormat = Launchpad.InputType.DrumRack;
            Set.Tracks[0] = new Track(MIDI.Devices[0]);

            Set.Tracks[0].Chain.Add(new Group(new Chain[] {
                new Chain(new Device[] {
                    new Filter(new bool[] {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false, false, true, false, true, true, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false}),
                    new Fade(1500, new List<Color> {
                        new Color(63, 63, 63),
                        new Color(63, 63, 0),
                        new Color(63, 15, 0),
                        new Color(63, 0, 0),
                        new Color(0, 0, 0),
                    }, new List<double> {
                        0, 0.1, 0.4, 0.6, 1
                    }),
                }),
                new Chain(new Device[] {
                    new Filter(new bool[] {false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, false, false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false}),
                    new Fade(600, new List<Color> {
                        new Color(0, 0, 0),
                        new Color(15, 0, 0),
                        new Color(63, 0, 0),
                        new Color(63, 0, 63),
                        new Color(7, 0, 63),
                        new Color(0, 0, 0)
                    }, new List<double> {
                        0, 0.1, 0.2, 0.4, 0.6, 0.9
                    })
                })
            }));

            Set.Save("/Users/mat1jaczyyy/Code/GoHigher.aps");
            //Set.Open("/Users/mat1jaczyyy/Code/GoHigher.aps");

            Log("ready");
            close.WaitOne();
        }
    }
}
