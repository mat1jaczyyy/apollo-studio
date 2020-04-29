using System;
using System.Linq;

using Apollo.RtMidi.Unmanaged.API;

namespace Apollo.RtMidi.Unmanaged.Devices.Infos {
    public class RtMidiDeviceInfo {
        internal RtMidiDeviceInfo(uint port, string name) {
            Port = port;

            /*
                https://github.com/micdah/RtMidi.Core/issues/21
                RtMidi adds port number on Windows to "ensure uniqueness".
            
                While it does succeed in ensuring it, disconnecting/reconnecting devices will change other devices'
                port numbers, so now it's impossible to tell who's who.
                
                Additionally, on Windows, Microsoft GS Wavetable Synth is always assigned port number 0, so device
                in/out port names will always mismatch. On macOS they at least match, but the issue above remains
                unsolved.

                It seems to go all the way to the OS' APIs, but Ableton Live somehow solved this... libusb?
                
                https://github.com/micdah/rtmidi/blob/473ddc10aafd935d6d5d16e8876a566aa7c55a20/RtMidi.cpp#L2638-L2643

                For now, remove the port number to ensure in/out names match, as it's more important.
            */
            Name = name.EndsWith(port.ToString())
                ? name.Substring(0, name.LastIndexOf(port.ToString(), StringComparison.Ordinal))
                : name;
            
            if (MidiDeviceManager.Default.GetAvailableMidiApis().FirstOrDefault() == RtMidiApi.RT_MIDI_API_LINUX_ALSA)
                Name = Name.Split(':', 2).Last();
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
