using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Timers;

using Newtonsoft.Json;
using RtMidi.Core;

using Apollo.Elements;

namespace Apollo.Core {
    public static class MIDI {
        public static readonly string Identifier = "midi";

        public static List<Launchpad> Devices = new List<Launchpad>();
        private static Timer timer = new Timer() { Interval = 100 };
        private static bool started = false;

        public static void Start() {
            if (started) new InvalidOperationException("MIDI Rescan Timer is already running");

            timer.Elapsed += Rescan;
            timer.Start();
            started = true;
        }

        private static object locker = new object();

        public static void Rescan(object sender, EventArgs e) {
            lock(locker) {
                foreach (var input in MidiDeviceManager.Default.InputDevices) {
                    foreach (var output in MidiDeviceManager.Default.OutputDevices) {
                        if (input.Name == output.Name) {
                            bool justConnected = true;

                            foreach (Launchpad device in Devices) {
                                if (device.Name == output.Name) {
                                    if (!device.Available) device.Connect(input, output);
                                    
                                    justConnected = false;
                                    break;
                                }
                            }

                            if (justConnected) Devices.Add(new Launchpad(input, output));
                        }
                    }
                }

                foreach (Launchpad device in Devices) {
                    if (device.Available) {
                        bool justDisconnected = true;

                        foreach (var output in MidiDeviceManager.Default.OutputDevices) {
                            if (device.Name == output.Name) {
                                justDisconnected = false;
                                break;
                            }
                        }

                        if (justDisconnected) device.Disconnect();
                    }
                }

                Program.Log($"Rescan");
            }
        }

        public static string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartArray();

                        for (int i = 0; i < Devices.Count; i++)
                            writer.WriteRawValue(Devices[i].Encode());

                    writer.WriteEndArray();

                writer.WriteEndObject();
            }

            return json.ToString();
        }
    }
}
