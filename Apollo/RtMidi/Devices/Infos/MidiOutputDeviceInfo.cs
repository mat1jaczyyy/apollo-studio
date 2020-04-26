using Apollo.RtMidi.Interface.Devices.Infos;
namespace Apollo.RtMidi.Devices.Infos
{
    internal class MidiOutputDeviceInfo : MidiDeviceInfo<RtMidiOutputDeviceInfo>, IMidiOutputDeviceInfo
    {
        public MidiOutputDeviceInfo(RtMidiOutputDeviceInfo rtMidiDeviceInfo) : base(rtMidiDeviceInfo)
        {
        }

        public IMidiOutputDevice CreateDevice()
        {
            return new MidiOutputDevice(RtMidiDeviceInfo.CreateDevice(), Name);
        }
    }
}
