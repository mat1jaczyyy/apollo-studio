using System;
using System.Collections.Generic;
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

        private static void CLI() {
            while (true) {
                Console.Write("> ");
                string[] cmd = Console.ReadLine().Split(' ');
                
                switch (cmd[0]) {
                    case "t":
                        if (cmd.Length == 1)
                            Console.WriteLine(Set.Tracks.Count);
                        else
                            Console.WriteLine(Set.Tracks[Convert.ToInt32(cmd[1])].ToString());
                        break;
                    
                    case "mr":
                        MIDI.Refresh();
                        break;
                }
            }
        }

        static void Main(string[] args) {
            foreach (string arg in args)
                if (arg.Equals("--log"))
                    log = true;
            
            if (log)
                foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                    Console.WriteLine($"MIDI API: {api}");

            MIDI.Refresh();
            Set.New();
            
            Set.Open("/Users/mat1jaczyyy/Code/studiotest.xxx");

            CLI();
            close.WaitOne();
        }
    }
}
