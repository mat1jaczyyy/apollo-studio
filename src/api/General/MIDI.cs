using System;
using System.Collections.Generic;
using System.Linq;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

namespace api {
    public static class MIDI {
        public static List<Launchpad> Devices = new List<Launchpad>();

        public static void Refresh() {
            foreach (Launchpad device in Devices)
                device.Dispose();

            Devices = new List<Launchpad>();

            IMidiInputDeviceInfo[] inputs = MidiDeviceManager.Default.InputDevices.ToArray();
            IMidiOutputDeviceInfo[] outputs = MidiDeviceManager.Default.OutputDevices.ToArray();

            foreach (IMidiInputDeviceInfo input in inputs)
                foreach (IMidiOutputDeviceInfo output in outputs)
                    if (input.Name == output.Name)
                        Devices.Add(new Launchpad(input, output));
        }
    }
}