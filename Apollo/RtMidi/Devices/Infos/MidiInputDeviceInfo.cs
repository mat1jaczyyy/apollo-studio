using Apollo.RtMidi.Interface.Devices.Infos;
namespace Apollo.RtMidi.Devices.Infos
{
    internal class MidiInputDeviceInfo : MidiDeviceInfo<RtMidiInputDeviceInfo>, IMidiInputDeviceInfo
    {
        public MidiInputDeviceInfo(RtMidiInputDeviceInfo rtMidiDeviceInfo) : base(rtMidiDeviceInfo)
        {
        }

        public IMidiInputDevice CreateDevice()
        {
            return new MidiInputDevice(RtMidiDeviceInfo.CreateDevice(), Name);
        }
    }
}
