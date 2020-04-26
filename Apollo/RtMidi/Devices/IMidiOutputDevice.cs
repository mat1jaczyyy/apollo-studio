using Apollo.RtMidi;

namespace Apollo.RtMidi.Devices
{
    public interface IMidiOutputDevice : IMidiDevice
    {
        bool Send(byte[] data);
        bool Send(MidiMessage data);
    }
}