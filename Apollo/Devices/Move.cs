using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Move: Device {
        public static readonly new string DeviceIdentifier = "move";

        public enum GridType {
            Full,
            Square
        }

        public string GridMode {
            get {
                if (_gridmode == GridType.Full) return "10x10";
                else if (_gridmode == GridType.Square) return "8x8";
                return null;
            }
            set {
                if (value == "10x10") _gridmode = GridType.Full;
                else if (value == "8x8") _gridmode = GridType.Square;

                if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetGridMode(GridMode);
            }
        }

        GridType _gridmode;

        public GridType GetGridMode() => _gridmode;

        public Offset Offset;

        private void OffsetChanged(Offset sender) {
            if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetOffset(Offset.X, Offset.Y);
        }

        private bool _wrap;
        public bool Wrap {
            get => _wrap;
            set {
                _wrap = value;

                if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetWrap(Wrap);
            }
        }

        public override Device Clone() => new Move(Offset.Clone());

        public Move(Offset offset = null, GridType gridmode = GridType.Full, bool wrap = false): base(DeviceIdentifier) {
            Offset = offset?? new Offset();
            _gridmode = gridmode;
            Wrap = wrap;

            Offset.Changed += OffsetChanged;
        }

        private int ApplyWrap(int coord) => (_gridmode == GridType.Square)? ((coord + 7) % 8 + 1) : (coord + 10) % 10;

        private bool ApplyOffset(int index, out int result) {
            int x = index % 10;
            int y = index / 10;

            if (_gridmode == GridType.Square && (x == 0 || x == 9 || y == 0 || y == 9)) {
                result = 0;
                return false;
            }

            x += Offset.X;
            y += Offset.Y;

            if (Wrap) {
                x = ApplyWrap(x);
                y = ApplyWrap(y);
            }

            result = y * 10 + x;

            if (_gridmode == GridType.Full) {
                if (0 <= x && x <= 9 && 0 <= y && y <= 9 && 1 <= result && result <= 98 && result != 9 && result != 90)
                    return true;
                
                if (y == -1 && 4 <= x && x <= 5) {
                    result = 99;
                    return true;
                }

            } else if (_gridmode == GridType.Square)
                if (1 <= x && x <= 8 && 1 <= y && y <= 8)
                    return true;
             
            return false;
        }

        public override void MIDIEnter(Signal n) {
            if (n.Index == 99) {
                MIDIExit?.Invoke(n);
                return;
            }

            if (ApplyOffset(n.Index, out int result)) {
                n.Index = (byte)result;
                MIDIExit?.Invoke(n);
            }
        }
    }
}