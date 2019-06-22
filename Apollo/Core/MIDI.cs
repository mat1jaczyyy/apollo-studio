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

        public static readonly Launchpad NoOutput = new VirtualLaunchpad("No Output");
        
        private static Courier courier;
        private static bool started = false;

        public static void Start() {
            if (started) return;

            if (!NoOutput.Available)
                NoOutput.Connect(null, null);

            courier = new Courier() { Interval = 100 };
            courier.Elapsed += Rescan;
            courier.Start();
            started = true;
        }

        public static void Stop() {
            if (!started) return;

            if (!NoOutput.Available)
                NoOutput.Connect(null, null);

            courier.Dispose();
            started = false;
        }

        private static object locker = new object();
        private static bool updated = false;

        public static void Update() {
            lock (locker) {
                if (updated) {
                    updated = false;
                    DevicesUpdated?.Invoke();
                }
            }
        }

        public static VirtualLaunchpad ConnectVirtual() {
            lock (locker) {
                Launchpad ret = null;
            
                for (int i = 1; true; i++) {
                    string name = $"Virtual Launchpad {i}";

                    ret = Devices.Find((lp) => lp.Name == name);
                    if (ret != null) {
                        if (ret is VirtualLaunchpad vlp && !vlp.Available) {
                            vlp.Connect(null, null);
                            updated = true;
                            return vlp;
                        }

                    } else {
                        Devices.Add(ret = new VirtualLaunchpad(name));
                        ret.Connect(null, null);
                        updated = true;
                        return (VirtualLaunchpad)ret;
                    }
                }
            }
        }

        public static AbletonLaunchpad ConnectAbleton() {
            lock (locker) {
                Launchpad ret = null;
            
                for (int i = 1; true; i++) {
                    string name = $"Ableton Connector {i}";

                    ret = Devices.Find((lp) => lp.Name == name);
                    if (ret != null) {
                        if (ret is AbletonLaunchpad alp && !alp.Available) {
                            alp.Connect(null, null);
                            updated = true;
                            return alp;
                        }

                    } else {
                        Devices.Add(ret = new AbletonLaunchpad(name));
                        ret.Connect(null, null);
                        updated = true;
                        return (AbletonLaunchpad)ret;
                    }
                }
            }
        }

        public static Launchpad Connect(IMidiInputDeviceInfo input = null, IMidiOutputDeviceInfo output = null) {
            lock (locker) {
                Launchpad ret = null;

                foreach (Launchpad device in Devices) {
                    if (device.Name == input.Name) {
                        ret = device;
                        updated |= !device.Available;

                        if (!device.Available)
                            device.Connect(input, output);
                        
                        return ret;
                    }
                }

                Devices.Add(ret = new Launchpad(input, output));
                updated = true;
                return ret;
            }
        }

        public static void Disconnect(Launchpad lp) {
            lock (locker) {
                if (lp.GetType() != typeof(VirtualLaunchpad))
                    foreach (IMidiOutputDeviceInfo output in MidiDeviceManager.Default.OutputDevices)
                        if (lp.Name.Replace("MIDIIN", "") == output.Name.Replace("MIDIOUT", "")) return;

                lp.Disconnect();
                updated = true;
            }
        }

        public static void Rescan(object sender, EventArgs e) {
            lock (locker) {
                foreach (IMidiInputDeviceInfo input in MidiDeviceManager.Default.InputDevices)
                    foreach (IMidiOutputDeviceInfo output in MidiDeviceManager.Default.OutputDevices)
                        if (input.Name.Replace("MIDIIN", "") == output.Name.Replace("MIDIOUT", ""))
                            Connect(input, output);

                foreach (Launchpad device in Devices)
                    if (device.GetType() == typeof(Launchpad) && device.Available)
                        Disconnect(device);

                Program.Log($"Rescan");

                if (updated) Update();
            }
        }
    }
}
