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

        public override void MIDIProcess(Signal n) {
            if (n.Index == 100) {
                InvokeExit(n);
                return;
            }

            if (Offset.Apply(n.Index, GridMode, Wrap, out int x, out int y, out int result)) {
                n.Index = (byte)result;
                InvokeExit(n);
            }
        }

        public override void Dispose() {
            if (Disposed) return;

            Offset.Dispose();
            base.Dispose();
        }
    }
}