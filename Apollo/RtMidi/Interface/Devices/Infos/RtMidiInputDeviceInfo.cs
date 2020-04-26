namespace Apollo.RtMidi.Interface.Devices.Infos
{
    internal class RtMidiInputDeviceInfo : RtMidiDeviceInfo
    {
        internal RtMidiInputDeviceInfo(uint port, string name) : base(port, name)
        {
        }

        public RtMidiInputDevice CreateDevice()
        {
            return new RtMidiInputDevice(Port);
        }
    }
}
