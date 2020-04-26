using System;

namespace Apollo.RtMidi.Interface.Devices.Infos
{
    public class RtMidiDeviceInfo
    {
        internal RtMidiDeviceInfo(uint port, string name)
        {
            Port = port;

            // RtMidi may add port number to end of name to ensure uniqueness
            Name = name.EndsWith(port.ToString())
                ? name.Substring(0, name.LastIndexOf(port.ToString(), StringComparison.Ordinal))
                : name;
        }

        public uint Port { get; }
        public string Name { get; }
    }
}
