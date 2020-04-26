using System;

using Apollo.RtMidi.Unmanaged.Devices.Infos;

namespace Apollo.RtMidi.Devices.Infos {
    public interface IMidiDeviceInfo {
        string Name { get; }
    }

    internal class MidiDeviceInfo<T>: IMidiDeviceInfo where T: RtMidiDeviceInfo {
        protected readonly T RtMidiDeviceInfo;

        public MidiDeviceInfo(T rtMidiDeviceInfo) {
            RtMidiDeviceInfo = rtMidiDeviceInfo?? throw new ArgumentNullException(nameof(rtMidiDeviceInfo));
        }

        public string Name => RtMidiDeviceInfo.Name;
    }

    public interface IMidiInputDeviceInfo: IMidiDeviceInfo {
        IMidiInputDevice CreateDevice();
    }

    internal class MidiInputDeviceInfo: MidiDeviceInfo<RtMidiInputDeviceInfo>, IMidiInputDeviceInfo {
        public MidiInputDeviceInfo(RtMidiInputDeviceInfo rtMidiDeviceInfo)
        : base(rtMidiDeviceInfo) {}

        public IMidiInputDevice CreateDevice()
            => new MidiInputDevice(RtMidiDeviceInfo.CreateDevice(), Name);
    }

    public interface IMidiOutputDeviceInfo: IMidiDeviceInfo {
        IMidiOutputDevice CreateDevice();
    }

    internal class MidiOutputDeviceInfo: MidiDeviceInfo<RtMidiOutputDeviceInfo>, IMidiOutputDeviceInfo {
        public MidiOutputDeviceInfo(RtMidiOutputDeviceInfo rtMidiDeviceInfo)
        : base(rtMidiDeviceInfo) {}

        public IMidiOutputDevice CreateDevice()
            => new MidiOutputDevice(RtMidiDeviceInfo.CreateDevice(), Name);
    }
}
