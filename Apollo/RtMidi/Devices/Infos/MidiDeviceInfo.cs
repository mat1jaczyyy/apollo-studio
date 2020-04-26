using System;
using Apollo.RtMidi.Interface.Devices.Infos;

namespace Apollo.RtMidi.Devices.Infos
{
    internal class MidiDeviceInfo<TRtMidiDeviceInfo> : IMidiDeviceInfo
        where TRtMidiDeviceInfo : RtMidiDeviceInfo
    {
        protected readonly TRtMidiDeviceInfo RtMidiDeviceInfo;

        public MidiDeviceInfo(TRtMidiDeviceInfo rtMidiDeviceInfo)
        {
            RtMidiDeviceInfo = rtMidiDeviceInfo ?? throw new ArgumentNullException(nameof(rtMidiDeviceInfo));
        }

        public string Name => RtMidiDeviceInfo.Name;
    }
}
