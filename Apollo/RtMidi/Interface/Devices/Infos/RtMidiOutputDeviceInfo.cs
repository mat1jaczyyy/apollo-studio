namespace Apollo.RtMidi.Interface.Devices.Infos
{
    internal class RtMidiOutputDeviceInfo : RtMidiDeviceInfo
    {
        internal RtMidiOutputDeviceInfo(uint port, string name) : base(port, name)
        {
        }

        public RtMidiOutputDevice CreateDevice()
        {
            return new RtMidiOutputDevice(Port);
        }
    }
}
