using System;

namespace Apollo.RtMidi.Unmanaged.Devices.Infos{
    public class RtMidiDeviceInfo {
        internal RtMidiDeviceInfo(uint port, string name) {
            Port = port;

            // RtMidi may add port number to end of name to ensure uniqueness
            Name = name.EndsWith(port.ToString())
                ? name.Substring(0, name.LastIndexOf(port.ToString(), StringComparison.Ordinal))
                : name;
        }

        public uint Port { get; private set; }
        public string Name { get; private set; }
    }

    internal class RtMidiInputDeviceInfo: RtMidiDeviceInfo {
        internal RtMidiInputDeviceInfo(uint port, string name)
        : base(port, name) {}

        public RtMidiInputDevice CreateDevice()
            => new RtMidiInputDevice(Port);
    }

    internal class RtMidiOutputDeviceInfo: RtMidiDeviceInfo {
        internal RtMidiOutputDeviceInfo(uint port, string name)
        : base(port, name) {}

        public RtMidiOutputDevice CreateDevice()
            => new RtMidiOutputDevice(Port);
    }
}
