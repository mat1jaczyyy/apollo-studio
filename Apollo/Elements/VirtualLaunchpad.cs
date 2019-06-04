using System;
using System.Linq;

using RtMidi.Core.Devices.Infos;

using Apollo.Core;
using Apollo.Structures;
using Apollo.Windows;

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

        public VirtualLaunchpad(string name = "") {
            Type = LaunchpadType.PRO;

            if (name == "") name = $"Virtual Launchpad {MIDI.Devices.Count((lp) => lp.GetType() == typeof(VirtualLaunchpad)) + 1}";
            Name = name;
            
            Available = true;

            Program.Log($"MIDI Created {Name}");
        }

        public override void Connect(IMidiInputDeviceInfo input, IMidiOutputDeviceInfo output) => new InvalidOperationException("A Virtual Launchpad holds no reference to MIDI ports.");
        public override void Disconnect() => new InvalidOperationException("A Virtual Launchpad holds no reference to MIDI ports.");
    }
}