using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

namespace api {
    public static class MIDI {
        public static List<Launchpad> Devices = new List<Launchpad>();
        private static readonly int refreshRate = 200;
        private static Timer autoRefresh;

        public static void Refresh() {
            IMidiInputDeviceInfo[] inputs = MidiDeviceManager.Default.InputDevices.ToArray();
            IMidiOutputDeviceInfo[] outputs = MidiDeviceManager.Default.OutputDevices.ToArray();

            foreach (IMidiInputDeviceInfo input in inputs) {
                foreach (IMidiOutputDeviceInfo output in outputs) {
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

                    foreach (IMidiOutputDeviceInfo output in outputs) {
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
        }

        private static void Callback(object info) {
            Start();
        }

        public static void Start() {
            Refresh();
            autoRefresh = new Timer(new TimerCallback(Callback), null, refreshRate, System.Threading.Timeout.Infinite);
        }

        public static void Stop() {
            autoRefresh = new Timer(new TimerCallback(Callback), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }
    }
}