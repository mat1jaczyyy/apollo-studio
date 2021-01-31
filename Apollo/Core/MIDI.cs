using System.Collections.Generic;
using System.Linq;

using Apollo.Devices;
using Apollo.Elements;
using Apollo.RtMidi;
using Apollo.RtMidi.Devices.Infos;
using Apollo.Structures;

namespace Apollo.Core {
    public static class MIDI {
        public delegate void DevicesUpdatedEventHandler();
        public static event DevicesUpdatedEventHandler DevicesUpdated;

        public static void DoneIdentifying() => DevicesUpdated?.Invoke();

        static List<Launchpad> _devices = new();
        public static List<Launchpad> Devices {
            get => _devices;
            set {
                if (_devices != value) {
                    _devices = value;
                    
                    DevicesUpdated?.Invoke();
                }
            }
        }

        public static List<Launchpad> UsableDevices => Devices.Where(i => i.Usable).ToList();

        public static readonly Launchpad NoOutput = new VirtualLaunchpad("No Output", 0);

        public static void ClearState(bool manual = true, bool multi = true, bool force = false) {
            if (Program.Project?.Tracks != null)
                foreach (Track track in Program.Project.Tracks)
                    track.Chain.MIDIEnter(StopSignal.Instance);
            
            if (multi) Multi.InvokeReset();
            Preview.InvokeClear();

            foreach (Launchpad lp in MIDI.UsableDevices)
                if (force) lp.ForceClear();
                else lp.Clear(manual);
        }
        
        static Courier courier;
        static bool started = false;

        public static void Start() {
            if (started) return;

            if (!NoOutput.Available)
                NoOutput.Connect(null, null);

            started = true;
            courier = new Courier(100, _ => Rescan(), repeat: true);
        }

        public static void Stop() {
            if (!started) return;

            if (!NoOutput.Available)
                NoOutput.Connect(null, null);

            courier.Dispose();
            started = false;
        }

        static object locker = new object();
        static bool updated = false;

        public static void Update() {
            lock (locker) {
                if (updated) {
                    updated = false;

                    foreach (Launchpad lp in Devices) {
                        lp.Reconnect();
                    }

                    DevicesUpdated?.Invoke();
                }
            }
        }

        public static VirtualLaunchpad ConnectVirtual(int start = 1) {
            lock (locker) {
                Launchpad ret = null;
            
                for (int i = start; true; i++) {
                    string name = $"Virtual Launchpad {i}";

                    ret = Devices.Find((lp) => lp.Name == name);
                    if (ret != null) {
                        if (ret is VirtualLaunchpad vlp && !vlp.Available) {
                            vlp.Connect(null, null);
                            updated = true;
                            return vlp;
                        }

                    } else {
                        Devices.Add(ret = new VirtualLaunchpad(name, i));
                        ret.Connect(null, null);
                        updated = true;
                        return (VirtualLaunchpad)ret;
                    }
                }
            }
        }

        public static AbletonLaunchpad ConnectAbleton(int version) {
            lock (locker) {
                Launchpad ret = null;
            
                for (int i = 1; true; i++) {
                    string name = $"Ableton Connector {i}";

                    ret = Devices.Find((lp) => lp.Name == name);
                    if (ret != null) {
                        if (ret is AbletonLaunchpad alp && !alp.Available) {
                            alp.Version = version;
                            alp.Connect(null, null);
                            updated = true;
                            return alp;
                        }

                    } else {
                        Devices.Add(ret = new AbletonLaunchpad(name) { Version = version });
                        ret.Connect(null, null);
                        updated = true;
                        return (AbletonLaunchpad)ret;
                    }
                }
            }
        }

        static bool PortsMatch(string input, string output)
            => input.ToUpper().Replace("IN", "").Replace("OUT", "") == output.ToUpper().Replace("IN", "").Replace("OUT", "");

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
                        if (PortsMatch(lp.Name, output.Name)) return;

                lp.Disconnect();
                updated = true;
            }
        }

        public static void Rescan() {
            lock (locker) {
                foreach (IMidiInputDeviceInfo input in MidiDeviceManager.Default.InputDevices)
                    foreach (IMidiOutputDeviceInfo output in MidiDeviceManager.Default.OutputDevices)
                        if (PortsMatch(input.Name, output.Name))
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
