using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Move: Device {
        GridType _gridmode;
        public GridType GridMode {
            get => _gridmode;
            set {
                _gridmode = value;

                if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetGridMode(GridMode);
            }
        }

        public Offset Offset;

        void OffsetChanged(Offset sender) {
            if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetOffset(Offset.X, Offset.Y);
        }

        bool _wrap;
        public bool Wrap {
            get => _wrap;
            set {
                _wrap = value;

                if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetWrap(Wrap);
            }
        }

        public override Device Clone() => new Move(Offset.Clone()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Move(Offset offset = null, GridType gridmode = GridType.Full, bool wrap = false): base("move") {
            Offset = offset?? new Offset();
            GridMode = gridmode;
            Wrap = wrap;

            Offset.Changed += OffsetChanged;
        }

        int ApplyWrap(int coord) => (GridMode == GridType.Square)? ((coord + 7) % 8 + 1) : (coord + 10) % 10;

        bool ApplyOffset(int index, out int result) {
            int x = index % 10;
            int y = index / 10;

            if (GridMode == GridType.Square && (x == 0 || x == 9 || y == 0 || y == 9)) {
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

            if (GridMode == GridType.Full) {
                if (0 <= x && x <= 9 && 0 <= y && y <= 9)
                    return true;
                
                if (y == -1 && 4 <= x && x <= 5) {
                    result = 100;
                    return true;
                }

            } else if (GridMode == GridType.Square)
                if (1 <= x && x <= 8 && 1 <= y && y <= 8)
                    return true;
             
            return false;
        }

        public override void MIDIProcess(Signal n) {
            if (n.Index == 100) {
                MIDIExit?.Invoke(n);
                return;
            }

            if (ApplyOffset(n.Index, out int result)) {
                n.Index = (byte)result;
                MIDIExit?.Invoke(n);
            }
        }

        public override void Dispose() {
            if (Disposed) return;

            Offset.Dispose();
            base.Dispose();
        }
    }
}