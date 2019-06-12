using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Rotate: Device {
        public static readonly new string DeviceIdentifier = "rotate";

        public enum RotateType {
            D90,
            D180,
            D270
        }

        RotateType _mode;
        public string Mode {
            get {
                if (_mode == RotateType.D90) return "90°";
                else if (_mode == RotateType.D180) return "180°";
                else if (_mode == RotateType.D270) return "270°";
                return null;
            }
            set {
                if (value == "90°") _mode = RotateType.D90;
                else if (value == "180°") _mode = RotateType.D180;
                else if (value == "270°") _mode = RotateType.D270;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }

        public RotateType GetRotateMode() => _mode;

        private bool _bypass;
        public bool Bypass {
            get => _bypass;
            set {
                _bypass = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetBypass(Bypass);
            }
        }

        public override Device Clone() => new Rotate(_mode, Bypass) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Rotate(RotateType mode = RotateType.D90, bool bypass = false): base(DeviceIdentifier) {
            _mode = mode;
            Bypass = bypass;
        }

        public override void MIDIProcess(Signal n) {
            if (Bypass) MIDIExit?.Invoke(n.Clone());
            
            if (_mode == RotateType.D90) {
                n.Index = (byte)((9 - n.Index % 10) * 10 + n.Index / 10);

            } else if (_mode == RotateType.D180) {
                n.Index = (byte)((9 - n.Index / 10) * 10 + 9 - n.Index % 10);

            } else if (_mode == RotateType.D270) {
                n.Index = (byte)((n.Index % 10) * 10 + 9 - n.Index / 10);
            }

            MIDIExit?.Invoke(n);
        }
    }
}