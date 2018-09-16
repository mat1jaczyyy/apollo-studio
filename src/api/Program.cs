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
        public static List<Track> tracks = new List<Track>();
        public static bool log = false;
        public static ManualResetEvent close = new ManualResetEvent(false);

        private static void CLI() {
            while (true) {
                Console.Write("> ");
                string[] cmd = Console.ReadLine().Split(' ');
                
                switch (cmd[0]) {
                    case "t":
                        if (cmd.Length == 1)
                            Console.WriteLine(tracks.Count);
                        else
                            Console.WriteLine(tracks[Convert.ToInt32(cmd[1])].ToString());
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

            tracks.Add(new Track());
            tracks[0].Chain.Add(
                new Group(new Chain[] {
                    new Chain(new Range(11, 11), new Device[] {
                        new Translation(-127),
                        new Translation(44),
                        new Infinity(),
                        new Group(new Chain[] {
                            new Chain(),
                            new Chain(new Device[] {
                                new Delay(100),
                                new Translation(-1),
                                new Duplication(new int[] {-9})
                            }),
                            new Chain(new Device[] {
                                new Delay(200),
                                new Translation(-2),
                                new Duplication(new int[] {-9, -18})
                            }),
                            new Chain(new Device[] {
                                new Delay(300),
                                new Translation(-3),
                                new Duplication(new int[] {-9, -18, -27})
                            }),
                            new Chain(new Device[] {
                                new Delay(400),
                                new Translation(-13),
                                new Duplication(new int[] {-9, -18})
                            }),
                            new Chain(new Device[] {
                                new Delay(500),
                                new Translation(-23),
                                new Duplication(new int[] {-9})
                            }),
                            new Chain(new Device[] {
                                new Delay(600),
                                new Translation(-33)
                            })
                        }),
                        new Iris(100, new Color[] {
                            new Color(63, 0, 0),
                            new Color(63, 15, 0),
                            new Color(63, 63, 0),
                            new Color(0, 63, 0),
                            new Color(0, 63, 63),
                            new Color(0, 0, 63),
                            new Color(7, 0, 63)
                        })
                    }),
                    new Chain(new Range(51, 88), new Device[] {new Layer(1)}),
                    new Chain(new Range(18, 18), new Device[] {
                        new Lightweight("/Users/mat1jaczyyy/Downloads/break2.mid"),
                        new Layer(-1)
                    })
                })
            );

            /*tracks.Add(new Track());
            tracks[1].Chain = tracks[0].Chain.Clone();*/

            Console.WriteLine(tracks[0].Encode());

            CLI();
            close.WaitOne();
        }
    }
}
