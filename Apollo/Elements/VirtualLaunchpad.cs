using RtMidi.Core.Devices.Infos;

using Apollo.Core;
using Apollo.Structures;

namespace Apollo.Elements {
    public class VirtualLaunchpad: Launchpad {
        public override void Send(Signal n) {
            Program.Log($"OUT <- {n.ToString()}");
            Window?.SignalRender(n);
        }

        public override void Clear() {
            for (int i = 0; i < 100; i++)
                screen[i] = new Pixel() {MIDIExit = Send};

            Send(new Signal(this, 99, new Color(0)));
        }

        public VirtualLaunchpad(string name) {
            Type = LaunchpadType.PRO;
            Name = name;
        }

        public override void Connect(IMidiInputDeviceInfo input = null, IMidiOutputDeviceInfo output = null) {
            Available = true;

            Program.Log($"MIDI Created {Name}");
        }
        
        public override void Disconnect() {
            Program.Log($"MIDI Disconnected {Name}");

            Available = false;
        }
    }
}