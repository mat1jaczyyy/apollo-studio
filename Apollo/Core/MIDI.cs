using System;
using System.Collections.Generic;

using RtMidi.Core;
using RtMidi.Core.Devices.Infos;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Core {
    public static class MIDI {
        public delegate void DevicesUpdatedEventHandler();
        public static event DevicesUpdatedEventHandler DevicesUpdated;

        public static void DoneIdentifying() => DevicesUpdated?.Invoke();

        private static List<Launchpad> _devices = new List<Launchpad>();
        public static List<Launchpad> Devices {
            get => _devices;
            set {
                if (_devices != value) {
                    _devices = value;
                    
                    DevicesUpdated?.Invoke();
                }
            }
        }
        
        private static Courier courier = new Courier() { Interval = 100 };
        private static bool started = false;

        public static void Start() {
            if (started) new InvalidOperationException("MIDI Rescan Timer is already running");

            courier.Elapsed += Rescan;
            courier.Start();
            started = true;
        }

        private static bool updated = false;

        public static void Update() {
            lock (locker) {
                if (updated) {
                    updated = false;
                    DevicesUpdated?.Invoke();
                }
            }
        }

        public static Launchpad Connect(IMidiInputDeviceInfo input = null, IMidiOutputDeviceInfo output = null) {
            Launchpad ret;

            if (input == null || output == null) {
                Devices.Add(ret = new VirtualLaunchpad());
                updated = true;
                return ret;
            }

            foreach (Launchpad device in Devices) {
                if (device.Name == input.Name) {
                    if (!device.Available)
                        device.Connect(input, output);
                    
                    ret = device;
                    updated |= !device.Available;
                    return ret;
                }
            }

            Devices.Add(ret = new Launchpad(input, output));
            updated = true;
            return ret;
        }

        private static object locker = new object();

        public static void Rescan(object sender, EventArgs e) {
            lock (locker) {
                foreach (IMidiInputDeviceInfo input in MidiDeviceManager.Default.InputDevices)
                    foreach (IMidiOutputDeviceInfo output in MidiDeviceManager.Default.OutputDevices)
                        if (input.Name.Replace("MIDIIN", "") == output.Name.Replace("MIDIOUT", ""))
                            Connect(input, output);

                foreach (Launchpad device in Devices) {
                    if (device.GetType() != typeof(VirtualLaunchpad) && device.Available) {
                        bool justDisconnected = true;

                        foreach (IMidiOutputDeviceInfo output in MidiDeviceManager.Default.OutputDevices) {
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

                if (updated) Update();
            }
        }
    }
}
