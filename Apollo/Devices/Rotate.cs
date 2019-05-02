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
            }
        }

        public RotateType GetRotateMode() => _mode;

        public bool Bypass;

        public override Device Clone() => new Rotate(_mode, Bypass);

        public Rotate(RotateType mode = RotateType.D90, bool bypass = false): base(DeviceIdentifier) {
            _mode = mode;
            Bypass = bypass;
        }

        public override void MIDIEnter(Signal n) {
            if (Bypass) MIDIExit?.Invoke(n.Clone());
            
            int x = n.Index % 10;
            int y = n.Index / 10;

            if (_mode == RotateType.D90) {
                int temp = y;
                y = 9 - x;
                x = temp;

            } else if (_mode == RotateType.D180) {
                x = 9 - x;
                y = 9 - y;

            } else if (_mode == RotateType.D270) {
                int temp = x;
                x = 9 - y;
                y = temp;
            }

            int result = y * 10 + x;
            
            n.Index = (byte)result;
            MIDIExit?.Invoke(n);
        }
    }
}