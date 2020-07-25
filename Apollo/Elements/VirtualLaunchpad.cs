using System.Collections.Generic;

using Apollo.Core;
using Apollo.Enums;
using Apollo.Rendering;
using Apollo.RtMidi.Devices.Infos;
using Apollo.Structures;

namespace Apollo.Elements {
    public class VirtualLaunchpad: Launchpad {
        public int VirtualIndex = 0;

        public override void Send(Color[] previous, Color[] snapshot, List<RawUpdate> n) {
            foreach (RawUpdate i in n)
                Window?.Render(i);
        }

        public VirtualLaunchpad(string name, int index) {
            Type = LaunchpadType.Pro;
            Name = name;
            VirtualIndex = index;
        }

        public override void Connect(IMidiInputDeviceInfo input = null, IMidiOutputDeviceInfo output = null) {
            Available = true;

            Program.Log($"MIDI Created {Name}");
        }
        
        public override void Disconnect(bool actuallyClose = true) {
            Program.Log($"MIDI Disconnected {Name}");

            Available = false;
        }
    }
}