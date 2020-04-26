using System.Linq;
using Apollo.RtMidi.Interface.Devices;

namespace Apollo.RtMidi.Devices
{
    internal class MidiOutputDevice : MidiDevice, IMidiOutputDevice
    {
        private readonly RtMidiOutputDevice _outputDevice;
        
        public MidiOutputDevice(RtMidiOutputDevice outputDevice, string name) : base(outputDevice, name)
        {
            _outputDevice = outputDevice;
        }

        public bool Send(byte[] data)
            => _outputDevice.SendMessage(data);
        
        public bool Send(MidiMessage data)
            => Send(data.Data);
    }
}
