using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Messages;

using Apollo.Core;
using Apollo.Structures;

namespace Apollo.Elements {
    public class AbletonLaunchpad: Launchpad {
        public Launchpad Target = MIDI.NoOutput;

        public override void Send(Signal n) {}

        public override void Clear() => Target?.Clear();

        public override void Render(Signal n) => Target?.Render(n);

        public AbletonLaunchpad(string name) {
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