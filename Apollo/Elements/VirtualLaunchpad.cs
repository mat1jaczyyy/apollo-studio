using RtMidi.Core.Devices.Infos;

using Apollo.Core;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Elements {
    public class VirtualLaunchpad: Launchpad {
        public override void Send(Signal n) => Window?.SignalRender(n);

        public override void Clear(bool manual = false) {
            if (!Available || (manual && PatternWindow != null)) return;
            
            CreateScreen();

            Signal n = new Signal(this, this, 0, new Color(0));

            for (int i = 0; i < 101; i++) {
                n.Index = (byte)i;
                Window?.SignalRender(n.Clone());
            }
        }

        public VirtualLaunchpad(string name) {
            Type = LaunchpadType.PRO;
            Name = name;
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