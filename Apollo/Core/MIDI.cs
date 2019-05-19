using System;
using System.Collections.Generic;

using RtMidi.Core;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Core {
    public static class MIDI {
        public delegate void DevicesUpdatedEventHandler();
        public static event DevicesUpdatedEventHandler DevicesUpdated;

        public static void DoneIdentifying() => DevicesUpdated?.Invoke();

        public static List<Launchpad> Devices = new List<Launchpad>();
        private static Courier courier = new Courier() { Interval = 100 };
        private static bool started = false;

        public static void Start() {
            if (started) new InvalidOperationException("MIDI Rescan Timer is already running");

            courier.Elapsed += Rescan;
            courier.Start();
            started = true;
        }

        private static object locker = new object();

        public static void Rescan(object sender, EventArgs e) {
            lock (locker) {
                bool updated = false;

                foreach (var input in MidiDeviceManager.Default.InputDevices) {
                    foreach (var output in MidiDeviceManager.Default.OutputDevices) {
                        if (input.Name.Replace("MIDIIN", "") == output.Name.Replace("MIDIOUT", "")) {
                            bool justConnected = true;

                            foreach (Launchpad device in Devices) {
                                if (device.Name == input.Name) {
                                    if (!device.Available) {
                                        device.Connect(input, output);
                                        updated = true;
                                    }
                                    
                                    justConnected = false;
                                    break;
                                }
                            }

                            if (justConnected) {
                                Devices.Add(new Launchpad(input, output));
                                updated = true;
                            }
                        }
                    }
                }

                foreach (Launchpad device in Devices) {
                    if (device.Available) {
                        bool justDisconnected = true;

                        foreach (var output in MidiDeviceManager.Default.OutputDevices) {
                            if (device.Name.Replace("MIDIIN", "") == output.Name.Replace("MIDIOUT", "")) {
                                justDisconnected = false;
                                break;
                            }
                        }

                        if (justDisconnected) {
                            device.Disconnect();
                            updated = true;
                        }
                    }
                }

                Program.Log($"Rescan");

                if (updated) DevicesUpdated?.Invoke();
            }
        }
    }
}
