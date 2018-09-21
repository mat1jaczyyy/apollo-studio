using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

namespace api {
    public static class MIDI {
        public static List<Launchpad> Devices = new List<Launchpad>();
        private static bool started = false;

        public static async void Start() {
            if (started) {
                throw new InvalidOperationException("MIDI.Start: Called twice.");
            }

            started = true;

            await Task.Run(() => {
                while (true) {
                    foreach (var input in MidiDeviceManager.Default.InputDevices) {
                        foreach (var output in MidiDeviceManager.Default.OutputDevices) {
                            if (input.Name == output.Name) {
                                bool justConnected = true;

                                foreach (Launchpad device in Devices) {
                                    if (device.Name == output.Name) {
                                        if (!device.Available) {
                                            device.Connect(input, output);
                                        }
                                        justConnected = false;
                                        break;
                                    }
                                }

                                if (justConnected) {
                                    Devices.Add(new Launchpad(input, output));
                                }
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

                            if (justDisconnected) {
                                device.Disconnect();
                            }
                        }
                    }

                    // Using an async timer doesn't work for some reason.
                    // https://github.com/micdah/RtMidi.Core/issues/18 could make this event-based instead in the future.
                    Thread.Sleep(200); 
                }
            });
        }
    }
}