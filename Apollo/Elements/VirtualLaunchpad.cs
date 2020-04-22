using RtMidi.Core.Devices.Infos;

using Apollo.Core;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Elements {
    public class VirtualLaunchpad: Launchpad {
        public int VirtualIndex = 0;

        public override void Send(Signal n) => Window?.SignalRender(n);

        public VirtualLaunchpad(string name, int index) {
            Type = LaunchpadType.PRO;
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