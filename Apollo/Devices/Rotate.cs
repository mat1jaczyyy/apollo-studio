using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Rotate: Device {
        RotateType _mode;
        public RotateType Mode {
            get => _mode;
            set {
                _mode = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }

        private bool _bypass;
        public bool Bypass {
            get => _bypass;
            set {
                _bypass = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetBypass(Bypass);
            }
        }

        public override Device Clone() => new Rotate(Mode, Bypass) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Rotate(RotateType mode = RotateType.D90, bool bypass = false): base("rotate") {
            Mode = mode;
            Bypass = bypass;
        }

        public override void MIDIProcess(Signal n) {
            if (Bypass) MIDIExit?.Invoke(n.Clone());
            
            if (Mode == RotateType.D90) {
                n.Index = (byte)((9 - n.Index % 10) * 10 + n.Index / 10);

            } else if (Mode == RotateType.D180) {
                n.Index = (byte)((9 - n.Index / 10) * 10 + 9 - n.Index % 10);

            } else if (Mode == RotateType.D270) {
                n.Index = (byte)((n.Index % 10) * 10 + 9 - n.Index / 10);
            }

            MIDIExit?.Invoke(n);
        }
    }
}