using Apollo.RtMidi.Unmanaged.Devices;

namespace Apollo.RtMidi.Devices {
    public interface IMidiOutputDevice: IMidiDevice {
        bool Send(byte[] data);
        bool Send(MidiMessage data);
    }

    internal class MidiOutputDevice: MidiDevice, IMidiOutputDevice {
        readonly RtMidiOutputDevice _outputDevice;
        
        public MidiOutputDevice(RtMidiOutputDevice outputDevice, string name): base(outputDevice, name)
            => _outputDevice = outputDevice;

        public bool Send(byte[] data)
            => _outputDevice.SendMessage(data);
        
        public bool Send(MidiMessage data)
            => Send(data.Data);
    }
}
