using System;

namespace Apollo.RtMidi.Unmanaged.Devices.Infos{
    public class RtMidiDeviceInfo {
        internal RtMidiDeviceInfo(uint port, string name) {
            Port = port;

            /*
                RtMidi may add port number to "ensure uniqueness"
                Disconnecting/reconnecting devices changes their port numbers, so it really can't ensure uniqueness
                Additionally on Windows Microsoft GS Wavetable Synth always gets 0 so they always mismatch

                https://github.com/micdah/rtmidi/blob/473ddc10aafd935d6d5d16e8876a566aa7c55a20/RtMidi.cpp#L2638-L2643

                TODO Can we solve uniqueness on macOS?
            */
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
