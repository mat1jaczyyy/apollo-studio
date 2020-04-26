namespace Apollo.RtMidi.Devices
{
    public interface IMidiInputDevice : IMidiDevice
    {
        event MidiMessageHandler Received;
    }
    
    public delegate void MidiMessageHandler(MidiMessage msg);
}
