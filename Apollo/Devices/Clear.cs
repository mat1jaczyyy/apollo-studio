using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Clear: Device {
        ClearType _mode;
        public ClearType Mode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((ClearViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }
        
        public override Device Clone() => new Clear(Mode) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Clear(ClearType mode = ClearType.Lights): base("clear") => Mode = mode;

        public override void MIDIProcess(Signal n) {
            if (!n.Color.Lit) {
                if (Mode == ClearType.Multi) Multi.InvokeReset();
                else MIDI.ClearState(Mode == ClearType.Both);
            }

            MIDIExit?.Invoke(n);
        }
    }
}