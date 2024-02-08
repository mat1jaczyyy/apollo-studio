using System.Collections.Generic;

using Apollo.Core;
using Apollo.Enums;
using Apollo.Rendering;
using Apollo.RtMidi.Devices.Infos;
using Apollo.Structures;

namespace Apollo.Elements.Launchpads {
    public class VirtualLaunchpad: Launchpad {
        public int VirtualIndex = 0;

        public override void Send(List<RawUpdate> n, Color[] snapshot) {
            foreach (RawUpdate i in n)
                Window?.Render(i);
        }

        public override void ForceClear() {
            if (!Usable) return;
            
            CreateScreen();

            Window?.Clear();
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